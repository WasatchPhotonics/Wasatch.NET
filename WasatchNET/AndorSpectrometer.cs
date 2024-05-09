#if WIN32 || x64
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Threading.Tasks;
#if WIN32
using ATMCD32CS;
#else
using ATMCD64CS;
#endif

namespace WasatchNET
{
    public class AndorSpectrometer : Spectrometer
    {
#if WIN32
        internal AndorSDK andorDriver = new ATMCD32CS.AndorSDK();
#elif x64
        internal AndorSDK andorDriver = new ATMCD64CS.AndorSDK();
#endif
        internal int specIndex;
        int cameraHandle = 0;
        int yPixels;

        //see page 330 of Andor SDK documentation
        public const int DRV_SUCCESS = 20002;

        // account for mechanical shutter stabilization time
        const int SHUTTER_SPEED_MS = 50;
        public const int BINNING = 1;

        internal AndorSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            isAndor = true;

            prioritizeVirtualEEPROM = true;

            // internal "step x" numbers are intended to synchronize with matching
            // steps in Wasatch.PY's wasatch.AndorDevice
            
            int minTemp = 0;
            int maxTemp = 0;
            int xPixels = 0;
            uint errorValue = 0;
            AndorSDK.AndorCapabilities capabilities = new AndorSDK.AndorCapabilities();

            // basic cycle to setup camera
            specIndex = index;
            andorDriver.GetCameraHandle(specIndex, ref cameraHandle);   // step 1
            andorDriver.SetCurrentCamera(cameraHandle);                 // step 2
            andorDriver.Initialize("");                                 // step 3

            // set temperature to midpoint, plus get detector information and set acquisition mode to single scan
            andorDriver.GetCapabilities(ref capabilities);              // step 4 (ENLIGHTEN doesn't do this)
            andorDriver.GetTemperatureRange(ref minTemp, ref maxTemp);  // step 5
            andorDriver.SetTemperature((minTemp + maxTemp) / 2);        // step 6 (note, ENLIGHTEN sets to EEPROM startup value)
            andorDriver.GetDetector(ref xPixels, ref yPixels);          // step 7
            andorDriver.CoolerON();                                     // step 8
            andorDriver.SetAcquisitionMode(1);                          // step 9
            andorDriver.SetTriggerMode(0);                              // step 10

            // Set readout mode to full vertical binning (step 11)
            errorValue = andorDriver.SetReadMode(0);

            // Set Vertical speed to recommended (step 12)
            float speed = 0;
            if (yPixels > 1)
            {
                int VSnumber = 0;
                andorDriver.GetFastestRecommendedVSSpeed(ref VSnumber, ref speed);  
                errorValue = andorDriver.SetVSSpeed(VSnumber);
            }

            // Set Horizontal Speed to max (step 13)
            float STemp = 0;
            int HSnumber = 0;
            int ADnumber = 0;
            int nAD = 0;
            int sIndex = 0;
            errorValue = andorDriver.GetNumberADChannels(ref nAD); // 13.1
            if (errorValue != DRV_SUCCESS)
            {

            }
            else
            {
                for (int iAD = 0; iAD < nAD; iAD++)
                {
                    andorDriver.GetNumberHSSpeeds(iAD, 0, ref sIndex); // 13.2
                    for (int iSpeed = 0; iSpeed < sIndex; iSpeed++)
                    {
                        andorDriver.GetHSSpeed(iAD, 0, iSpeed, ref speed); // 13.3
                        if (speed > STemp)
                        {
                            STemp = speed;
                            HSnumber = iSpeed;
                            ADnumber = iAD;
                        }
                    }
                }
            }

            errorValue = andorDriver.SetADChannel(ADnumber); // 13.4
            errorValue = andorDriver.SetHSSpeed(0, HSnumber); // 13.5

            // Set shutter to fully automatic external with internal always open (step 14)
            andorDriver.SetShutterEx(1, 1, SHUTTER_SPEED_MS, SHUTTER_SPEED_MS, 0);

            // set exposure time to 1ms (step 15) (ENLIGHTEN sets to EEPROM startup value)
            andorDriver.SetExposureTime(0.001f);

            // read actual integration time, and use that in internal driver state (ENLIGHTEN doesn't do this)
            float exposure = 0;
            float accumulate = 0;
            float kinetic = 0;
            andorDriver.GetAcquisitionTimings(ref exposure, ref accumulate, ref kinetic);

            // get camera serial number (step 16)
            int sn = 0;
            errorValue = andorDriver.GetCameraSerialNumber(ref sn);

            integrationTimeMS = (uint)exposure;
            pixels = (uint)xPixels;
            eeprom = new AndorEEPROM(this);

            // step 17: ENLIGHTEN then uses GetNumberPreAmpGains and GetPreAmpGain to support high-gain mode
        }
        override internal bool open()
        {
            Task<bool> task = Task.Run(async () => await openAsync());
            return task.Result;
        }

        override internal async Task<bool> openAsync()
        {
            eeprom = new AndorEEPROM(this);

            logger.info("found spectrometer with {0} pixels", pixels);

            if (!(await eeprom.readAsync()))
            {
                logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                //wrapper.shutdown();
                close();
                return false;
            }
            logger.debug("back from reading EEPROM");

            regenerateWavelengths();
            //detectorTECSetpointDegC = 15.0f;

            logger.info("Opened Andor Spectrometer with index {0}", specIndex);

            return true;
            
        }

        public override void close()
        {
            Task task = Task.Run(async () => await closeAsync());
            task.Wait();
        }
        public async override Task closeAsync()
        {
            //wrapper.shutdown();
            await Task.Run(() => andorDriver.SetCurrentCamera(cameraHandle));
            await Task.Run(() => andorDriver.ShutDown());
        }

        public override bool loadFromJSON(string pathname)
        {
            AndorEEPROM ee = eeprom as AndorEEPROM;
            if (!ee.loadFromJSON(pathname))
                return false;
            regenerateWavelengths();
            return true;
        }

        // will eventually need to override getAreaScanLightweight() and/or getFrame()
        // at that point will need to add calls to change acquisition mode/read mode here
        public override double[] getSpectrum(bool forceNew = false)
        {
            lock (acquisitionLock)
            {
                Task<double[]> task = Task.Run(async () => await getSpectrumAsync(forceNew));
                return task.Result;
            }
        }
        public override async Task<double[]> getSpectrumAsync(bool forceNew = false)
        {
            int temp = 0;
            andorDriver.GetTemperature(ref temp);
            lastDetectorTemperatureDegC = temp;

            double[] sum = await getSpectrumRawAsync();
            if (sum == null)
            {
                logger.error("getSpectrum: getSpectrumRaw returned null");
                return null;
            }
            logger.debug("getSpectrum: received {0} pixels", sum.Length);

            if (scanAveraging_ > 1)
            {
                // logger.debug("getSpectrum: getting additional spectra for averaging");
                for (uint i = 1; i < scanAveraging_; i++)
                {
                    double[] tmp = await getSpectrumRawAsync();
                    if (tmp == null)
                        return null;

                    for (int px = 0; px < pixels; px++)
                        sum[px] += tmp[px];
                }

                for (int px = 0; px < pixels; px++)
                    sum[px] /= scanAveraging_;
            }

            if (dark != null && dark.Length == sum.Length)
                for (int px = 0; px < pixels; px++)
                    sum[px] -= dark_[px];

            correctBadPixels(ref sum);

            if (boxcarHalfWidth > 0)
            {
                // logger.debug("getSpectrum: returning boxcar");
                return Util.applyBoxcar(boxcarHalfWidth, sum);
            }
            else
            {
                // logger.debug("getSpectrum: returning sum");
                return sum;
            }
        }
        // returns vertically-binned 1D array
        protected override double[] getSpectrumRaw(bool skipTrigger = false)
        {
            Task<double[]> task = Task.Run(async () => await getSpectrumRawAsync(skipTrigger));
            return task.Result;
        }
        protected override async Task<double[]> getSpectrumRawAsync(bool skipTrigger = false)
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            if (!areaScanEnabled)
            {
                int[] spec = new int[pixels]; //defaults to all zeros
                andorDriver.StartAcquisition();
                andorDriver.WaitForAcquisition();
                uint success = await Task.Run(() => andorDriver.GetAcquiredData(spec, (uint)(pixels)));

                if (success != DRV_SUCCESS)
                    return null;

                double[] convertedSpec = Array.ConvertAll(spec, item => (double)item);


                if (eeprom.featureMask.invertXAxis)
                    Array.Reverse(convertedSpec);

                logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
                return convertedSpec;
            }
            else
            {
                int[] spec = new int[yPixels * pixels / BINNING]; //defaults to all zeros
                andorDriver.StartAcquisition();
                andorDriver.WaitForAcquisition();
                uint success = andorDriver.GetAcquiredData(spec, (uint)(yPixels * pixels / BINNING));

                if (success != DRV_SUCCESS)
                    return null;

                double[] convertedSpec = Array.ConvertAll(spec, item => (double)item);

                if (eeprom.featureMask.invertXAxis)
                    Array.Reverse(convertedSpec);

                logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
                return convertedSpec;
            }

        }


        public override bool areaScanEnabled
        {
            get
            {
                return areaScanEnabled_;
            }
            set
            {
                areaScanEnabled_ = value;
                lock (acquisitionLock)
                {
                    if (value)
                    {
                        uint errorValue = andorDriver.SetReadMode(4);
                        errorValue = andorDriver.SetImage(1, BINNING, 1, (int)pixels, 1, yPixels);
                        if (errorValue != DRV_SUCCESS)
                            areaScanEnabled_ = false;
                        andorDriver.SetShutterEx(1, 1, SHUTTER_SPEED_MS, SHUTTER_SPEED_MS, 0);

                    }
                    else
                    {
                        // Set readout mode to full vertical binning
                        uint errorValue = andorDriver.SetReadMode(0);
                        andorDriver.SetShutterEx(1, 1, SHUTTER_SPEED_MS, SHUTTER_SPEED_MS, 0);
                    }
                }
            }
        }

        public override bool highGainModeEnabled
        {
            get { return false; }
            set { return; }
        }

        public override bool laserEnabled
        {
            get { return false; }
            set { laserEnabled_ = value; }
        }

        public override uint integrationTimeMS
        {
            get
            {
                return (uint)integrationTime_;
            }
            set
            {
                lock (acquisitionLock)
                {
                    float exposure = 0;
                    float accumulate = 0;
                    float kinetic = 0;
                    andorDriver.SetExposureTime((float)value / 1000);
                    andorDriver.GetAcquisitionTimings(ref exposure, ref accumulate, ref kinetic);

                    long time = (long)Math.Round(exposure * 1000);

                    // above logic can result in values of < 1ms, which can have a negative impact on downstream Apps
                    if (time == 0)
                        time = 1;

                    integrationTime_ = time;
                }
                
            }
        }
        long integrationTime_;

        public override bool hasLaser
        {
            get => false;
        }

        public override bool setLaserPowerPercentage(float perc)
        {
            return false;
        }

        public override LaserPowerResolution laserPowerResolution
        {
            get
            {
                return LaserPowerResolution.LASER_POWER_RESOLUTION_MANUAL;
            }
        }

        public override bool laserInterlockEnabled { get => false; }

        public override UInt64 laserModulationPeriod { get => 100; }

        public override ulong laserModulationPulseWidth { get => 0; set { } }

        public override float detectorGain { get => 0; }

        public override float detectorGainOdd { get => 0; }

        public override short detectorOffset { get => 0; }

        public override short detectorOffsetOdd { get => 0; }

        public override bool isARM => false;

        // This won't actually do anything until we add code to load the virtual EEPROM from AWS
        public override bool isInGaAs => eeprom.detectorName.Contains("DU490");

        public override TRIGGER_SOURCE triggerSource
        {
            get => TRIGGER_SOURCE.EXTERNAL;
            set
            {

            }
        }

        public override float laserTemperatureDegC { get => 0; }

        public override ushort laserTemperatureRaw { get => 0; }

        public override ushort laserTemperatureSetpointRaw { get => 0; }

        public override float batteryPercentage
        {
            get
            {
                return 0.0f;
            }
        }

        public override bool batteryCharging { get => false; }
        public override bool detectorTECEnabled
        {
            get
            {
                return tecEnabled_;
            }
            set
            {
                if (value)
                    andorDriver.CoolerON();
                else
                    andorDriver.CoolerOFF();
                tecEnabled_ = value;
            }
        }
        bool tecEnabled_ = false;

        public override float detectorTemperatureDegC
        {
            get
            {
                // get a new value if possible, but if a spectrum is being collected just
                // return the cached value
                if (Monitor.TryEnter(acquisitionLock))
                {
                    int temp = 0;
                    andorDriver.GetTemperature(ref temp);
                    lastDetectorTemperatureDegC = temp;
                    Monitor.Exit(acquisitionLock);
                }

                return lastDetectorTemperatureDegC;
            }
        }

        public override ushort detectorTECSetpointRaw
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        public override float detectorTECSetpointDegC
        {
            get => base.detectorTECSetpointDegC;
            set
            {
                andorDriver.SetTemperature((int)value);
                detectorTECSetpointDegC_ = value;
            }

        }

        public override ushort secondaryADC
        {
            get
            {
                return 0;
            }
        }

        public override string firmwareRevision
        {
            get
            {
                string retval = "";

                return retval;
            }
        }



        public override string fpgaRevision
        {
            get
            {
                string retval = "";

                return retval;
            }
        }

        public override float excitationWavelengthNM
        {
            get
            {
                return eeprom.laserExcitationWavelengthNMFloat;
            }
            set
            {
                eeprom.excitationNM = (ushort)value;
                eeprom.laserExcitationWavelengthNMFloat = value;
            }
        }

        public override bool continuousAcquisitionEnable { get => false; set { } }
        public override byte continuousFrames { get => 0; set { } }
        public override ushort detectorTemperatureRaw { get => 0; }
    }
}
#endif
