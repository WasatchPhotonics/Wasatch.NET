using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using LibUsbDotNet.Main;
using MPSSELight;
using System.Runtime.Remoting.Messaging;

namespace WasatchNET
{
    public class SPISpectrometer : Spectrometer
    {
        //internal Wrapper wrapper;
        //internal SeaBreezeWrapper wrapper;
        internal int specIndex;

        internal MpsseDevice mpsse;
        internal SpiDevice spi;

        //
        //  Most of the commands here are GET commands with the set being the same
        //  command with the MSBit set (for example see GET vs. SET integration time)
        //
        //  These commands are pulled from the ENG-0072 Documentation
        //

        const byte START_CMD = 0x3C;
        const byte END_CMD = 0x3E;

        //const byte ACQUIRE_RESET = 0x09;
        //const byte ACQUIRE_IMAGE = 0x0A;
        const byte RESET_FPGA = 0x0B;//not implemented
        //const byte GET_POLL_READY = 0x0C;
        //const byte GET_FIRMWARE_REV = 0x0D;
        //const byte ARM_STATUS = 0x0E;
        //const byte RESET_ARM = 0x0F;
        const byte GET_FPGA_REV = 0x10;
        const byte GET_INTEGRATION_TIME = 0x11;//not implemented
        const byte FPGA_CONFIG_REGISTER = 0x12;//not implemented
        const byte CCD_OFFSET = 0x13;
        const byte CCD_GAIN = 0x14;
        const byte GET_PIXEL_COUNT = 0x15;
        const byte LASER_TEMP_SET_POINT = 0x16; //not implemented
        const byte LASER_MOD_DURATION = 0x17; //not implemented
        const byte LASER_MOD_PULSE_DELAY = 0x18;//not implemented
        const byte LASER_MOD_PERIOD = 0x19;//not implemented
        const byte FRAMES_TO_ACQUIRE = 0x1A;//not implemented
        const byte CCD_THRESHOLD = 0x1B;//not implemented
        const byte FPGA_STATUS = 0x1C;//not implemented
        const byte LASER_TEMP = 0x1D;//not implemented
        const byte LASER_MOD_PULSE_WIDTH = 0x1E;//not implemented
        const byte GET_ACTUAL_INTEGRATION_TIME = 0x1F;//not implemented
        const byte GET_ACTUAL_FRAME_COUNT = 0x20;//not implemented
        const byte LASER_TRANSITION_1 = 0x21;//not implemented
        const byte LASER_TRANSITION_2 = 0x22;//not implemented
        const byte LASER_TRANSITION_3 = 0x23;//not implemented
        const byte LASER_TRANSITION_4 = 0x24;//not implemented
        const byte LASER_TRANSITION_5 = 0x25;//not implemented
        const byte LASER_TRANSITION_6 = 0x26;//not implemented
        const byte HORIZ_BINNING = 0x27;//not implemented
        const byte TRIGGER_DELAY = 0x28;//not implemented
        const byte GET_CCD_TEMP = 0x49;//not implemented
        //public const byte OUTPUT_TEST_PATTERN = 0x30;
        const byte PREP_FPGA = 0x30;
        const byte READ_EEPROM_BUFFER = 0x31;
        const byte EEPROM_OPS = 0xB0;
        const byte WRITE_EEPROM_BUFFER = 0xB1;
        //const byte COMP_ENGINE_OUT_ENABLE = 0x31;
        const byte SELECT_USB_FS = 0x32;//not implemented
        const byte LASER_MODULATION = 0x33;//not implemented
        const byte LASER_ON = 0x34;//not implemented
        const byte CONTINUOUS_READ_CCD = 0x35;//not implemented
        const byte CCD_THRESHOLD_SENSE = 0x36;//not implemented
        const byte CCD_EXTERNAL_TRIGGER = 0x37;//not implemented
        const byte CCD_TEMP_CONTROL = 0x38;//not implemented
        const byte LASER_MOD_LINKED_INT_TIME = 0x39;//not implemented
        const byte EXTERNAL_OUT_TRIGGER_SRC = 0x3A;//not implemented
        const byte LASER_POWER_RAMP = 0x3B;//not implemented
        const byte CCD_AREA_SCAN = 0x3C;//not implemented
        const byte ALTERNATE_LASER = 0x3D;//not implemented
        const byte LAST_PIXEL_CRC16 = 0x3F;//not implemented

        const byte SET_SETTINGS = 0x92;
        const byte SET_INTEGRATION_TIME = 0x91;
        const byte SET_CCD_GAIN = 0x94;
        const byte SET_CCD_OFFSET = 0x93;

        const int STANDARD_PADDING = 40;

        bool edgeTrigger;
        bool firmwareThrowaway;

        internal SPISpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            isSPI = true;
            triggerSource = TRIGGER_SOURCE.EXTERNAL;
            specIndex = index;

            edgeTrigger = true;
            firmwareThrowaway = true;

            integrationTimeMS_ = 3;
        }

        public override void changeSPITrigger(bool edge, bool firmwareThrow)
        {
            if (firmwareThrow)
            {
                byte[] transmitData = new byte[1] { 0x01 };

                transmitData = wrapCommand(0xB2, transmitData, STANDARD_PADDING);

                byte[] result = spi.readWrite(transmitData);
            }

            else
            {
                byte[] transmitData = new byte[1] { 0x00 };

                transmitData = wrapCommand(0xB2, transmitData, STANDARD_PADDING);

                byte[] result = spi.readWrite(transmitData);

            }

            firmwareThrowaway = firmwareThrow;

            if (edge)
            {
                byte[] transmitData = new byte[2] { 0x86, 0x40 };

                transmitData = wrapCommand(SET_SETTINGS, transmitData, STANDARD_PADDING);

                byte[] result = spi.readWrite(transmitData);
            }

            else
            {
                byte[] transmitData = new byte[2] { 0x86, 0xC0 };

                transmitData = wrapCommand(SET_SETTINGS, transmitData, STANDARD_PADDING);

                byte[] result = spi.readWrite(transmitData);
            }

            edgeTrigger = edge;

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
            eeprom = new SPIEEPROM(this);

            string ftdi = null;
            try
            {
                ftdi = FtdiInventory.DeviceListInfo();
            }
            catch
            {
                logger.debug("Unable to generate FTDI list");
                return false;
            }

            if (ftdi.Length == 0)
            {
                logger.debug("Unable to find any SPI spectrometer");
                return false;
            }

            string snPattern = "Serial Number: ";

            int start = ftdi.IndexOf(snPattern, 0);

            start = start + 15; // TS - I'm not positive on how static this will be, may be worth changing down the line

            string devSerialNumber = ftdi.Substring(start, 8);

            MpsseDevice.MpsseParams mpsseParams = new MpsseDevice.MpsseParams();
            mpsseParams.clockDevisor = 1;

            try
            {
                mpsse = new FT232H(devSerialNumber, mpsseParams);
            }
            catch 
            {
                logger.debug("Unable to create MPSSE connection with board. May be missing drivers");
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
            catch
            {
                logger.debug("Unable to create SPI connection with board. May be missing drivers");
                return false;
            }
            
            /*
            if (!eeprom.read())
            {
                logger.info("Spectrometer: failed to GET_MODEL_CONFIG");
                close();
                return false;
            }
            */

            mpsse.SetDataBitsHighByte(FtdiPin.None, FtdiPin.GPIOH0);

            logger.debug("Trying to get pixel count");

            byte[] payload = new byte[0];
            byte[] command = wrapCommand(GET_PIXEL_COUNT, payload, STANDARD_PADDING);

            /*
            byte[] result = spi.readWrite(command);

            logger.debug("pixel response: ");

            foreach (byte b in result)
                logger.debug( "{0}", b);

            byte[] pixelBytes = new byte[2];

            int index = 0;

            while (index < result.Length)
            {
                if (result[index] == START_CMD)
                    break;
                ++index;
            }

            while (result[index] == START_CMD)
                ++index;
            --index;

            pixelBytes[1] = result[index + 5];
            pixelBytes[0] = result[index + 4];

            pixels = (ushort)Unpack.toShort(pixelBytes);

            if (pixels > 10000)
                return false;
            */

            //sets firmware throwaway
            byte[] transmitData = new byte[1] { 0x01 };
            
            
            command = wrapCommand(0xB2, transmitData, STANDARD_PADDING);

            byte[] result = spi.readWrite(command);

            //sets edge trigger
            transmitData = new byte[2] { 0x86, 0x40 };

            command = wrapCommand(SET_SETTINGS, transmitData, STANDARD_PADDING);

            result = spi.readWrite(command);
            
            int index = 0;
            while (index < result.Length)
            {
                if (result[index] == START_CMD)
                    break;
                ++index;
            }

            if (index == result.Length || result[index + 3] != 0)
                return false;

            logger.debug("All SPI comm successful, trying to gen wavelengths now");
            
            regenerateWavelengths();

            logger.debug("Successfully connected to SPI Spectrometer through adafruit board with serial number {0}", devSerialNumber);

            return true;
        }

        public override void close()
        {
            mpsse.Dispose();
        }

        public override double[] getSpectrum(bool forceNew = false)
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

        public List<byte[]> getEEPROMPages()
        {

            List<byte[]> pages = new List<byte[]>();

            //CHANGES
            for (ushort page = 0; page < EEPROM.MAX_PAGES; page++)
            {
                
                byte[] transmitData = new byte[0];
                byte[] command = wrapCommand(PREP_FPGA, transmitData, STANDARD_PADDING);
                logger.hexdump(command, String.Format("preparing fpga to read {0}: ", page));
                byte[] result = spi.readWrite(command);
                byte currPage = (byte)(0x40 | (uint)page);

                logger.hexdump(result, "response: ");
                //Thread.Sleep(100);

                transmitData = new byte[1]{ currPage };
                command = wrapCommand(EEPROM_OPS, transmitData, STANDARD_PADDING);
                logger.hexdump(command, String.Format("sending first b0 command to read {0}: ", page));
                result = spi.readWrite(command);

                logger.hexdump(result, "response: ");

                /*
                transmitData = new byte[1] { currPage };
                command = wrapCommand(EEPROM_OPS, transmitData, STANDARD_PADDING);
                logger.hexdump(command, String.Format("sending second b0 command to read {0}: ", page));
                result = spi.readWrite(command);

                logger.hexdump(result, "response: ");
                //Thread.Sleep(100);

                transmitData = new byte[0];
                command = wrapCommand(PREP_FPGA, transmitData, STANDARD_PADDING);
                logger.hexdump(command, String.Format("preparing fpga to read {0}: ", page));
                result = spi.readWrite(command);

                logger.hexdump(result, "response: ");
                */
                Thread.Sleep(2);

                transmitData = new byte[0];
                command = wrapCommand(READ_EEPROM_BUFFER, transmitData, 100);
                logger.hexdump(command, String.Format("reading buffer for page {0}: ", page));
                result = spi.readWrite(command);

                logger.hexdump(result, "response: ");
                //Thread.Sleep(100);

                int index = 0;

                while (index < result.Length)
                {
                    if (result[index] == START_CMD)
                        break;
                    ++index;
                }

                while (index < result.Length && result[index] == START_CMD)
                    ++index;
                --index;

                if (index > result.Length - 67)
                    index = result.Length - 67;

                pages.Add(result.Skip(index + 4).Take(64).ToArray());
            }

            return pages;
        }

        public bool writeEEPROM(List<byte[]> pages)
        {
            for (ushort page = 0; page < pages.Count; ++page)
            {
                byte[] transmitData = pages[page];


                logger.hexdump(transmitData, String.Format("preparing to write page {0}: ", page));

                //we've had issues with writing EEPROM using SPI so the attempt here is to give extra time before telling the FPGA to write.
                //a thread sleep might be more prudent, but this needs work in some way regardless
                byte[] command = wrapCommand(WRITE_EEPROM_BUFFER, transmitData, STANDARD_PADDING * 4);

                logger.hexdump(command, String.Format("write command for page {0}: ", page));

                byte[] result = spi.readWrite(command);

                logger.hexdump(result, "response: ");

                //Thread.Sleep(100);

                Thread.Sleep(2);

                /*
                transmitData = new byte[0];
                command = wrapCommand(READ_EEPROM_BUFFER, transmitData, 100); 
                logger.hexdump(command, String.Format("buffer read back command for page {0}: ", page));
                result = spi.readWrite(command);

                logger.hexdump(result, "response: ");
                */

                //Thread.Sleep(100);

                byte currPage = (byte)(0x80 | (uint)page);

                transmitData = new byte[1] { currPage };
                command = wrapCommand(EEPROM_OPS, transmitData, STANDARD_PADDING);
                logger.hexdump(command, String.Format("commit to EEPROM command for page {0}: ", page));
                result = spi.readWrite(command);

                logger.hexdump(result, "response: ");

                Thread.Sleep(2);
            }

            return true;
        }


        protected override double[] getSpectrumRaw(bool skipTrigger=false)
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            double[] spec = new double[pixels]; 

            mpsse.SetDataBitsHighByte(FtdiPin.GPIOH0, FtdiPin.GPIOH0);
            if (edgeTrigger)
                Thread.Sleep(1);
            else
                Thread.Sleep((int)integrationTimeMS);
            mpsse.SetDataBitsHighByte(FtdiPin.None, FtdiPin.GPIOH0);

            byte read = mpsse.ReadDataBitsHighByte();
            while ((read & 0b0010) != 0b0010)
            {
                read = mpsse.ReadDataBitsHighByte();
            }

            byte[] command = padding((int)pixels * 2 + STANDARD_PADDING * 2);

            //actual result
            byte[] result = spi.readWrite(command);

            //unpack pixels
            for (int i = 0; i < pixels; ++i)
            {
                int msb = result[i * 2 + 1];
                int lsb = result[i * 2 + 2];

                UInt16 pixel = (ushort)((msb << 8) | lsb);

                spec[i] = pixel;

            }

            if (eeprom.featureMask.invertXAxis)
                Array.Reverse(spec);

            return spec;
            
        }

        public override bool areaScanEnabled
        {
            get
            {
                return areaScanEnabled_;
            }
            set
            {

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

        public override string serialNumber
        {
            get => eeprom.serialNumber;
        }

        public override uint boxcarHalfWidth
        {
            get { return boxcarHalfWidth_; }
            set { lock (acquisitionLock) boxcarHalfWidth_ = value; }
        }

        public override uint integrationTimeMS
        {
            get
            {
                return (uint)integrationTimeMS_;
            }
            set
            {
                lock (acquisitionLock)
                {
                    byte[] payload = new byte[3];

                    uint integTimeMS = (uint)value;

                    payload[0] = (byte)(0xFF & integTimeMS);
                    payload[1] = (byte)((0xFF00 & integTimeMS) >> 8);
                    payload[2] = (byte)((0xFF0000 & integTimeMS) >> 16);
                    byte[] command = wrapCommand(SET_INTEGRATION_TIME, payload, STANDARD_PADDING);

                    byte[] result = spi.readWrite(command);

                    integrationTimeMS_ = integTimeMS;

                }
            }
        }

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

        public override float detectorGain 
        {
            get // TS - this should probably be cached
            {
                byte[] transmitData = new byte[0];

                transmitData = wrapCommand(CCD_GAIN, transmitData, STANDARD_PADDING);

                byte[] result = padding(10);
                    
                result = spi.readWrite(transmitData);

                byte[] gainBytes = new byte[2];

                int index = 0;

                while (index < result.Length)
                {
                    if (result[index] == START_CMD)
                        break;
                    ++index;
                }

                while (result[index] == START_CMD)
                    ++index;
                --index;

                gainBytes[1] = result[index + 5];
                gainBytes[0] = result[index + 4]; 

                return FunkyFloat.toFloat(Unpack.toUshort(gainBytes));
            }
            set
            {
                ushort funkyTransform = FunkyFloat.fromFloat(value);
                byte[] transmitData = new byte[2];

                transmitData[1] = (byte)(((uint)funkyTransform & 0xFF00) >> 8);
                transmitData[0] = (byte)((uint)funkyTransform & 0x00FF);

                byte[] command = wrapCommand(SET_CCD_GAIN, transmitData, STANDARD_PADDING);

                byte[] result = spi.readWrite(command);
            }
        }

        public override float detectorGainOdd { get => 0; set{ } }

        public override short detectorOffset 
        {
            get
            {
                byte[] transmitData = new byte[0];

                transmitData = wrapCommand(CCD_OFFSET, transmitData, STANDARD_PADDING);

                byte[] result = padding(10);

                result = spi.readWrite(transmitData);

                byte[] offsetBytes = new byte[2];//{ result[0], result[1] };

                int index = 0;

                while (index < result.Length)
                {
                    if (result[index] == START_CMD)
                        break;
                    ++index;
                }

                while (result[index] == START_CMD)
                    ++index;
                --index;

                offsetBytes[1] = result[index + 5];
                offsetBytes[0] = result[index + 4];

                //code to deal with our sign bit short (not 2's complement)
                short wrongForm = Unpack.toShort(offsetBytes);
                short finalVal = (short)(0x7FFF & wrongForm);
                if ((wrongForm & 0x8000) == 0x8000)
                    finalVal *= -1;

                return finalVal;

            }
            set
            {
                //code to deal with our sign bit short (not 2's complement)
                ushort transform = (ushort)Math.Abs(value);
                if (value < 0)
                    transform |= 0x8000;

                byte[] transmitData = new byte[2];

                transmitData[1] = (byte)((transform & 0xFF00) >> 8);
                transmitData[0] = (byte)(transform & 0x00FF);

                byte[] command = wrapCommand(SET_CCD_OFFSET, transmitData, STANDARD_PADDING);

                byte[] result = spi.readWrite(command);
            }
        }

        public override short detectorOffsetOdd { get => 0; set { } }

        public override bool isARM => false;
        public override bool isInGaAs => eeprom.detectorName.StartsWith("g", StringComparison.CurrentCultureIgnoreCase);
        public override TRIGGER_SOURCE triggerSource
        {
            get => TRIGGER_SOURCE.EXTERNAL;
            set
            {
                if (value != TRIGGER_SOURCE.EXTERNAL)
                    logger.error("TRIGGER_SOURCE {0} not supported in SPISpectrometer", value.ToString());
            }
        }

        public override float laserTemperatureDegC { get => 0; }

        public override ushort laserTemperatureRaw { get => 0; }

        public override byte laserTemperatureSetpointRaw { get => 0; }

        public override float batteryPercentage
        {
            get
            {
                return 50.0f; // TS - this is really stupid and needs to be fixed someday
            }
        }

        public override bool batteryCharging { get => false; }

        public override bool detectorTECEnabled
        {
            get
            {
                return detectorTECEnabled_;
            }
            set
            {
                detectorTECEnabled_ = value;
            }
        }

        public override float detectorTemperatureDegC
        {
            get => 0;
        }

        public override ushort detectorTECSetpointRaw
        {
            get => 0;
            set
            {
                logger.error("detectorTECSetpoint not supported via SPI");
            }
        }

        public override float detectorTECSetpointDegC
        {
            get => base.detectorTECSetpointDegC;
            set
            {
                detectorTECSetpointDegC_ = value;
            }

        }

        public override ushort secondaryADC
        {
            get => 0;
        }

        public override string firmwareRevision => "";

        public override string fpgaRevision
        {
            get
            {
                byte[] payload = new byte[0];

                byte[] command = wrapCommand(GET_FPGA_REV, payload, STANDARD_PADDING);
                byte[] result = spi.readWrite(command);

                byte[] lenBytes = new byte[2];//{ result[0], result[1] };

                int index = 0;

                while (index < result.Length)
                {
                    if (result[index] == START_CMD)
                        break;
                    ++index;
                }

                while (result[index] == START_CMD)
                    ++index;
                --index;

                lenBytes[0] = result[index + 2];
                lenBytes[1] = result[index + 1];

                int count = (ushort)Unpack.toShort(lenBytes);

                index = index + 4;

                string rev = "";

                for (int i = 0; i < (count - 1); ++i)
                {
                    rev += ((char)result[index + i]).ToString();
                }
                
                return rev;

            }
        }

        public override float excitationWavelengthNM
        {
            get => eeprom.laserExcitationWavelengthNMFloat;
        }
    }
}
