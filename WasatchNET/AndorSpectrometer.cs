using System;
using System.Reflection;
using System.Collections.Generic;
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
        private AndorSDK andorDriver = new ATMCD32CS.AndorSDK();
#else
        private AndorSDK andorDriver = new ATMCD64CS.AndorSDK();
#endif
        internal int specIndex;
        int cameraHandle = 0;

        internal AndorSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            int minTemp = 0;
            int maxTemp = 0;
            AndorSDK.AndorCapabilities capabilities = new AndorSDK.AndorCapabilities();

            //excitationWavelengthNM = 0;
            //triggerSource = TRIGGER_SOURCE.INTERNAL;
            specIndex = index;
            andorDriver.GetCameraHandle(specIndex, ref cameraHandle);
            andorDriver.SetCurrentCamera(cameraHandle);
            andorDriver.Initialize("");

            andorDriver.GetCapabilities(ref capabilities);
            andorDriver.GetTemperatureRange(ref minTemp, ref maxTemp);
            andorDriver.SetTemperature(minTemp);
            andorDriver.CoolerON();
            andorDriver.SetExposureTime(0.1f);

        }

        public override void close()
        {
            //wrapper.shutdown();
            if (!commError)
            {
                lock (acquisitionLock)
                {
                    var task = Task.Run(async () => await closeSpectrometerAsync());
                    task.Wait();
                }
            }
        }

        public override double[] getSpectrum(bool forceNew = false)
        {
            if (!commError)
            {
                lock (acquisitionLock)
                {
                    double[] sum = getSpectrumRaw();
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
                            double[] tmp = getSpectrumRaw();
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

                    if (correctPixels)
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
            }
            else
                return new double[pixels];

        }

        protected override double[] getSpectrumRaw(bool skipTrigger = false)
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            double[] spec = new double[pixels]; // default to all zeros

            var task = Task.Run(async () => spec = await getSpectrumAsync());
            task.Wait();

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
            return spec;
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
                //return (uint)wrapper.getIntegrationTimeMillisec();
                return (uint)integrationTime_;
            }
            set
            {
                //if (value < wrapper.getMaxIntegrationTimeMillisec())
                //wrapper.setIntegrationTimeMillisec((long)value);
                if (!commError)
                {
                    lock (acquisitionLock)
                    {
                        int errorReader = 0;
                        var task = Task.Run(async () => errorReader = await setIntegrationAsync(value));
                        task.Wait();

                        if (errorReader == 0)
                            integrationTime_ = value;

                    }
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
                //return wrapper.getSpectrometerTECEnabled();
            }
            set
            {
                bool ok = enableDetectorTEC(value);
                if (ok)
                    tecEnabled_ = value;
            }
        }
        bool tecEnabled_ = false;

        public override float detectorTemperatureDegC
        {
            get
            {
                int temp = 0;
                andorDriver.GetTemperature(ref temp);
                return temp;
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
                if (value != detectorTECSetpointDegC_)
                    setDetectorTECSetpointDegreesC(value);
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

                if (!commError)
                {
                    lock (acquisitionLock)
                    {
                        var task = Task.Run(async () => retval = await getFirmwareRevAsync());
                        task.Wait();
                    }
                }
                return retval;
            }
        }



        public override string fpgaRevision
        {
            get
            {
                lock (acquisitionLock)
                {
                    byte[] cmd = new byte[2];
                    cmd[0] = 0x6b; // read FPGA register
                    cmd[1] = 0x04; // read FPGA version number

                    sbWrite(cmd);
                    byte[] response = sbRead(3);

                    string formatted = "";

                    if (response != null)
                    {
                        UInt16 bytes = (UInt16)((response[2] << 8) | response[1]);

                        int major = (bytes >> 12) & 0x0f;
                        int minor = (bytes >> 4) & 0xff;
                        int build = (bytes) & 0x0f;

                        formatted = String.Format("{0:x1}.{1:x2}.{2:x1}", major, minor, build);
                        logger.debug("converted raw FPGA version {0:x4} to {1}", bytes, formatted);
                    }

                    return formatted;
                }
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

            }
        }

    }
}
