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

        // not sure where this comes from...ask Caleb - TS
        const int SHUTTER_SPEED_MS = 50;
        public const int BINNING = 1;

        internal AndorSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            int minTemp = 0;
            int maxTemp = 0;
            int xPixels = 0;
            uint errorValue = 0;
            AndorSDK.AndorCapabilities capabilities = new AndorSDK.AndorCapabilities();

            // basic cycle to setup camera
            specIndex = index;
            andorDriver.GetCameraHandle(specIndex, ref cameraHandle);
            andorDriver.SetCurrentCamera(cameraHandle);
            andorDriver.Initialize("");

            // set temperature to midpoint, plus get detector information and set acquisition mode to single scan
            andorDriver.GetCapabilities(ref capabilities);
            andorDriver.GetTemperatureRange(ref minTemp, ref maxTemp);
            andorDriver.SetTemperature((minTemp + maxTemp) / 2);
            andorDriver.GetDetector(ref xPixels, ref yPixels);
            andorDriver.CoolerON();
            andorDriver.SetAcquisitionMode(1);
            andorDriver.SetTriggerMode(0);

            // Set readout mode to full vertical binning
            errorValue = andorDriver.SetReadMode(0);

            // Set Vertical speed to recommended
            int VSnumber = 0;
            float speed = 0;
            andorDriver.GetFastestRecommendedVSSpeed(ref VSnumber, ref speed);
            errorValue = andorDriver.SetVSSpeed(VSnumber);

            // Set Horizontal Speed to max
            float STemp = 0;
            int HSnumber = 0;
            int ADnumber = 0;
            int nAD = 0;
            int sIndex = 0;
            errorValue = andorDriver.GetNumberADChannels(ref nAD);
            if (errorValue != DRV_SUCCESS)
            {

            }
            else
            {
                for (int iAD = 0; iAD < nAD; iAD++)
                {
                    andorDriver.GetNumberHSSpeeds(iAD, 0, ref sIndex);
                    for (int iSpeed = 0; iSpeed < sIndex; iSpeed++)
                    {
                        andorDriver.GetHSSpeed(iAD, 0, iSpeed, ref speed);
                        if (speed > STemp)
                        {
                            STemp = speed;
                            HSnumber = iSpeed;
                            ADnumber = iAD;
                        }
                    }
                }
            }

            errorValue = andorDriver.SetADChannel(ADnumber);
            errorValue = andorDriver.SetHSSpeed(0, HSnumber);

            // Set shutter to fully automatic external with internal always open
            andorDriver.SetShutterEx(1, 1, SHUTTER_SPEED_MS, SHUTTER_SPEED_MS, 0);

            // set exposure time to 1ms
            andorDriver.SetExposureTime(0.001f);
            float exposure = 0;
            float accumulate = 0;
            float kinetic = 0;
            andorDriver.GetAcquisitionTimings(ref exposure, ref accumulate, ref kinetic);

            //get camera serial number
            int sn = 0;
            errorValue = andorDriver.GetCameraSerialNumber(ref sn);

            integrationTimeMS = (uint)exposure;
            pixels = (uint)xPixels;
            eeprom = new AndorEEPROM(this);
        }

        override internal async Task<bool> open()
        {
            eeprom = new AndorEEPROM(this);

            logger.info("found spectrometer with {0} pixels", pixels);

            if (!(await eeprom.read()))
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
            //wrapper.shutdown();
            andorDriver.SetCurrentCamera(cameraHandle);
            andorDriver.ShutDown();
        }

        // will eventually need to override getAreaScanLightweight() and/or getFrame()
        // at that point will need to add calls to change acquisition mode/read mode here
        public override async Task<double[]> getSpectrum(bool forceNew = false)
        {
            double[] sum = await getSpectrumRaw();
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
                    double[] tmp = await getSpectrumRaw();
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
        protected override async Task<double[]> getSpectrumRaw(bool skipTrigger = false)
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            if (!areaScanEnabled)
            {
                int[] spec = new int[pixels];

                // ask for spectrum then collect, NOT multithreaded (though we should look into that!), blocks
                spec = new int[pixels];     //defaults to all zeros
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
                int[] spec = new int[yPixels * pixels / BINNING];

                // ask for spectrum then collect, NOT multithreaded (though we should look into that!), blocks
                spec = new int[yPixels * pixels / BINNING];     //defaults to all zeros
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
        public override bool isInGaAs => false;
        public override TRIGGER_SOURCE triggerSource
        {
            get => TRIGGER_SOURCE.EXTERNAL;
            set
            {

            }
        }

        public override float laserTemperatureDegC { get => 0; }

        public override ushort laserTemperatureRaw { get => 0; }

        public override byte laserTemperatureSetpointRaw { get => 0; }

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
                lock (acquisitionLock)
                {
                    int temp = 0;
                    andorDriver.GetTemperature(ref temp);
                    return temp;
                }
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

    }
}
#endif