using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using MPSSELight;
using FTD2XX_NET;

namespace WasatchNET
{
    public class TetonSpectrometer : Spectrometer
    {
        //internal Wrapper wrapper;
        //internal SeaBreezeWrapper wrapper;
        internal int specIndex;

        internal MpsseDevice mpsse;
        internal SpiDevice spi;

        const byte START_CMD = 0x3C;
        const byte END_CMD = 0x3E;
        const byte ACQUIRE_RESET = 0x09;
        const byte ACQUIRE_IMAGE = 0x0A;
        const byte RESET_FPGA = 0x0B;
        const byte GET_POLL_READY = 0x0C;
        const byte GET_FIRMWARE_REV = 0x0D;
        const byte ARM_STATUS = 0x0E;
        const byte RESET_ARM = 0x0F;
        const byte GET_FPGA_REV = 0x10;
        const byte GET_INTEGRATION_TIME = 0x11;
        const byte FPGA_CONFIG_REGISTER = 0x12;
        const byte CCD_OFFSET = 0x13;
        const byte CCD_GAIN = 0x14;
        const byte GET_PIXEL_COUNT = 0x15;
        const byte LASER_TEMP_SET_POINT = 0x16;
        const byte LASER_MOD_DURATION = 0x17;
        const byte LASER_MOD_PULSE_DELAY = 0x18;
        const byte LASER_MOD_PERIOD = 0x19;
        const byte FRAMES_TO_ACQUIRE = 0x1A;
        const byte CCD_THRESHOLD = 0x1B;
        const byte FPGA_STATUS = 0x1C;
        const byte LASER_TEMP = 0x1D;
        const byte LASER_MOD_PULSE_WIDTH = 0x1E;
        const byte GET_ACTUAL_INTEGRATION_TIME = 0x1F;
        const byte GET_ACTUAL_FRAME_COUNT = 0x20;
        const byte LASER_TRANSITION_1 = 0x21;
        const byte LASER_TRANSITION_2 = 0x22;
        const byte LASER_TRANSITION_3 = 0x23;
        const byte LASER_TRANSITION_4 = 0x24;
        const byte LASER_TRANSITION_5 = 0x25;
        const byte LASER_TRANSITION_6 = 0x26;
        const byte HORIZ_BINNING = 0x27;
        const byte TRIGGER_DELAY = 0x28;
        const byte GET_CCD_TEMP = 0x49;
        const byte OUTPUT_TEST_PATTERN = 0x30;
        const byte COMP_ENGINE_OUT_ENABLE = 0x31;
        const byte SELECT_USB_FS = 0x32;
        const byte LASER_MODULATION = 0x33;
        const byte LASER_ON = 0x34;
        const byte CONTINUOUS_READ_CCD = 0x35;
        const byte CCD_THRESHOLD_SENSE = 0x36;
        const byte CCD_EXTERNAL_TRIGGER = 0x37;
        const byte CCD_TEMP_CONTROL = 0x38;
        const byte LASER_MOD_LINKED_INT_TIME = 0x39;
        const byte EXTERNAL_OUT_TRIGGER_SRC = 0x3A;
        const byte LASER_POWER_RAMP = 0x3B;
        const byte CCD_AREA_SCAN = 0x3C;
        const byte ALTERNATE_LASER = 0x3D;
        const byte LAST_PIXEL_CRC16 = 0x3F;

        const byte SET_SETTINGS = 0x92;
        const byte SET_INTEGRATION_TIME = 0x91;


        internal TetonSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            excitationWavelengthNM = 0;
            triggerSource = TRIGGER_SOURCE.EXTERNAL;
            specIndex = index;
            int errorReader = 0;
            //integrationTime_ = SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(specIndex, ref errorReader) / 1000;
            integrationTime_ = 6;
            if (errorReader != 0)
                integrationTime_ = 80;
        }

        static byte Calc_CRC_8(byte[] DataArray, int Length)
        {
            byte[] CRC_8_TABLE =
            {
              0, 94,188,226, 97, 63,221,131,194,156,126, 32,163,253, 31, 65,
            157,195, 33,127,252,162, 64, 30, 95,  1,227,189, 62, 96,130,220,
             35,125,159,193, 66, 28,254,160,225,191, 93,  3,128,222, 60, 98,
            190,224,  2, 92,223,129, 99, 61,124, 34,192,158, 29, 67,161,255,
             70, 24,250,164, 39,121,155,197,132,218, 56,102,229,187, 89,  7,
            219,133,103, 57,186,228,  6, 88, 25, 71,165,251,120, 38,196,154,
            101, 59,217,135,  4, 90,184,230,167,249, 27, 69,198,152,122, 36,
            248,166, 68, 26,153,199, 37,123, 58,100,134,216, 91,  5,231,185,
            140,210, 48,110,237,179, 81, 15, 78, 16,242,172, 47,113,147,205,
             17, 79,173,243,112, 46,204,146,211,141,111, 49,178,236, 14, 80,
            175,241, 19, 77,206,144,114, 44,109, 51,209,143, 12, 82,176,238,
             50,108,142,208, 83, 13,239,177,240,174, 76, 18,145,207, 45,115,
            202,148,118, 40,171,245, 23, 73,  8, 86,180,234,105, 55,213,139,
             87,  9,235,181, 54,104,138,212,149,203, 41,119,244,170, 72, 22,
            233,183, 85, 11,136,214, 52,106, 43,117,151,201, 74, 20,246,168,
            116, 42,200,150, 21, 75,169,247,182,232, 10, 84,215,137,107, 53
            };

            int i;
            byte CRC;

            CRC = 0;

            for (i = 0; i < Length; i++)
                CRC = CRC_8_TABLE[CRC ^ DataArray[i]];

            return CRC;
        }

        static byte[] wrapCommand(byte command, byte[] payload, int padding = 0)
        {
            byte[] wrapped = new byte[payload.Length + 6 + padding];
            uint cmdLength = (ushort)(1 + payload.Length);
            uint len1 = cmdLength & 0xFF;
            uint len0 = (cmdLength >> 8) & 0xFF;
            wrapped[0] = START_CMD;
            wrapped[1] = (byte)len0;
            wrapped[2] = (byte)len1;
            wrapped[3] = command;
            int index = 4;
            for (int i = 0; i < payload.Length; ++i)
            {
                wrapped[i + 4] = payload[i];
                ++index;
            }

            byte[] crcLoad = new byte[payload.Length + 3];

            for (int i = 0; i < payload.Length + 3; ++i)
            {
                crcLoad[i] = wrapped[i + 1];
            }

            wrapped[index++] = Calc_CRC_8(crcLoad, crcLoad.Length);
            wrapped[index] = END_CMD;

            return wrapped;
        }

        static byte[] padding(int size)
        {
            byte[] padding = new byte[size];
            for (int i = 0; i < size; ++i)
                padding[i] = 0x00;

            return padding;
        }

        override internal bool open()
        {
            eeprom = new EEPROM(this);

            int errorReader = 0;

            string ftdi = FtdiInventory.DeviceListInfo();
            if (ftdi.Length == 0)
            {
                logger.info("Unable to find any Teton spectrometer, try again");
                return false;
            }
            else
            {
                string snPattern = "Serial Number: ";

                int start = ftdi.IndexOf(snPattern, 0);

                start = start + 15;

                string devSerialNumber = ftdi.Substring(start, 8);

                MpsseDevice.MpsseParams mpsseParams = new MpsseDevice.MpsseParams();
                mpsseParams.clockDevisor = 1;

                try
                {
                    mpsse = new FT232H(devSerialNumber, mpsseParams);
                }
                catch (Exception e)
                {
                    logger.info("Unable to create MPSSE connection with board. May be missing drivers");
                    return false;
                }

                try
                {
                    spi = new SpiDevice(mpsse,
                             new SpiDevice.SpiParams
                             {
                                 Mode = SpiDevice.SpiMode.Mode0,
                                 ChipSelect = FtdiPin.CS,
                                 ChipSelectPolicy = SpiDevice.CsPolicy.CsActiveLow
                             });
                }
                catch (Exception e)
                {
                    logger.info("Unable to create SPI connection with board. May be missing drivers");
                    return false;
                }

                pixels = 1024;

                if (!eeprom.read())
                {
                    logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                    //wrapper.shutdown();
                    close();
                    return false;
                }

                regenerateWavelengths();

                logger.info("Successfully connected to Teton Spectrometer through adafruit board with serial number {0}", devSerialNumber);

                return true;
            }
        }

        public override void close()
        {
            //wrapper.shutdown();
            //int errorReader = 0;
            //SeaBreezeWrapper.seabreeze_close_spectrometer(specIndex, ref errorReader);
            mpsse.Dispose();
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

            mpsse.SetDataBitsHighByte(FtdiPin.GPIOH0, FtdiPin.GPIOH0);
            Thread.Sleep(100);
            mpsse.SetDataBitsHighByte(FtdiPin.None, FtdiPin.GPIOH0);
            
            byte read = mpsse.ReadDataBitsHighByte();
            while ((read & 0b0010) != 0b0010)
            {
                read = mpsse.ReadDataBitsHighByte();
            }

            byte[] command = padding((int)pixels * 2 + 20);

            byte[] result = spi.readWrite(command);

            for (int i = 0; i < pixels; ++i)
            {
                int msb = result[i * 2 + 1];
                int lsb = result[i * 2 + 2];

                UInt16 pixel = (ushort)((msb << 8) | lsb);

                spec[i] = pixel;

            }

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

        public override uint boxcarHalfWidth
        {
            get { return boxcarHalfWidth_; }
            set { lock (acquisitionLock) boxcarHalfWidth_ = value; }
        }
        uint boxcarHalfWidth_;

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
                    /*
                    int errorReader = 0;
                    if (value > (SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(specIndex, ref errorReader) / 1000))
                    {
                        SeaBreezeWrapper.seabreeze_set_integration_time_microsec(specIndex, ref errorReader, (long)(value * 1000));
                        if (errorReader == 0)
                            integrationTime_ = value;
                    }*/
                    byte[] payload = new byte[3];

                    ushort integTime = (ushort)value;

                    payload[0] = (byte)(0xFF & integTime);
                    payload[1] = (byte)((0xFF00 & integTime) >> 8);
                    payload[2] = 0x00;
                    byte[] command = wrapCommand(SET_INTEGRATION_TIME, payload, 20);

                    byte[] result = spi.readWrite(command);


                    integrationTime_ = integTime;
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
