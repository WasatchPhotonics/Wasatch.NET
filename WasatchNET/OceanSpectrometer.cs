using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace WasatchNET
{
    public class OceanSpectrometer : Spectrometer
    {
        //internal Wrapper wrapper;
        //internal SeaBreezeWrapper wrapper;
        internal int specIndex;
        
        internal OceanSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            excitationWavelengthNM = 0;
            triggerSource = TRIGGER_SOURCE.EXTERNAL;
            specIndex = index;
            int errorReader = 0;
            integrationTime_ = SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(specIndex, ref errorReader) / 1000;
            if (errorReader != 0)
                integrationTime_ = 8;
        }

        override internal bool open()
        {
            eeprom = new EEPROM(this);

            int errorReader = 0;

            if (SeaBreezeWrapper.seabreeze_open_spectrometer(specIndex, ref errorReader) == 0)
            {
                pixels = (uint)SeaBreezeWrapper.seabreeze_get_formatted_spectrum_length(specIndex, ref errorReader);

                logger.info("found spectrometer with {0} pixels", pixels);

                if (!eeprom.read())
                {
                    logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                    //wrapper.shutdown();
                    close();
                    return false;
                }
                logger.debug("back from reading EEPROM");

                regenerateWavelengths();

                logger.info("Opened Ocean Spectrometer with index {0}", specIndex);

                return true;
            }

            else
            {
                logger.debug("Unable to open Ocean spectrometer with index {0}", specIndex);
                return false;
            }

        }

        public override void close()
        {
            //wrapper.shutdown();
            int errorReader = 0;
            SeaBreezeWrapper.seabreeze_close_spectrometer(specIndex, ref errorReader);
        }

        public override double[] getSpectrum()
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

        protected override double[] getSpectrumRaw()
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            double[] spec = new double[pixels]; // default to all zeros
            int errorReader = 0;

            SeaBreezeWrapper.seabreeze_get_formatted_spectrum(specIndex, ref errorReader, ref spec[0], (int)pixels);

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
            return spec;
        }

        public override bool laserEnabled
        {
            get { return false; }
            set { laserEnabled_ = value; }
        }

        public override string serialNumber
        {
            get
            {
                byte[] serial = new byte[16];
                int errorReader = 0;
                //wrapper.getSerialNumber(16, ref serial);
                //SeaBreezeWrapper.seabreeze_get_serial_number(specIndex, ref errorReader, ref serial, 16);
                return "";//serial.ToString();
            }
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
                lock (acquisitionLock)
                {
                    int errorReader = 0;
                    SeaBreezeWrapper.seabreeze_set_integration_time_microsec(specIndex, ref errorReader, (long)(value * 1000));
                    if (errorReader == 0)
                        integrationTime_ = value;
                    
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

        public override UInt64 laserModulationPeriod { get => 0; }

        public override float detectorGain { get => 0; }

        public override float detectorGainOdd { get => 0; }

        public override short detectorOffset { get => 0; }

        public override short detectorOffsetOdd { get => 0; }

        public override bool isARM => false;

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
                return 50.0f;//(float)wrapper.getBatteryChargePercent();
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
                //wrapper.setSpectrometerTECEnabled(value);
                int errorReader = 0;
                if (value)
                    SeaBreezeWrapper.seabreeze_set_tec_enable(specIndex, ref errorReader, 1);
                else
                    SeaBreezeWrapper.seabreeze_set_tec_enable(specIndex, ref errorReader, 0);
                if (errorReader == 0)
                    tecEnabled_ = value;
            }
        }
        bool tecEnabled_ = false;

        public override float detectorTemperatureDegC
        {
            get
            {
                int errorReader = 0;
                return 0;
                //return (float)SeaBreezeWrapper.seabreeze_read_tec_temperature(specIndex, ref errorReader);
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
                detectorTECSetpointDegC_ = value;
                int errorReader = 0;
                SeaBreezeWrapper.seabreeze_set_tec_temperature(specIndex, ref errorReader, detectorTECSetpointDegC_);
            }
            
        }

        public override ushort secondaryADC
        {
            get
            {
                return 0;
            }
        }

        public override string firmwareRevision => "";

        public override string fpgaRevision => "";

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
