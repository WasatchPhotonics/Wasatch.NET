using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using LibUsbDotNet.LudnMonoLibUsb;

namespace WasatchNET
{
    // For WP spectrometer API, see "WP Raman USB Interface Specification r1.4"
    public class Spectrometer
    {
        ////////////////////////////////////////////////////////////////////////
        // Inner types
        ////////////////////////////////////////////////////////////////////////

        public enum FPGA_INTEG_TIME_RES { ONE_MS, TEN_MS, SWITCHABLE };
        public enum FPGA_DATA_HEADER { NONE, OCEAN_OPTICS, WASATCH };
        public enum FPGA_LASER_TYPE { NONE, INTERNAL, EXTERNAL };
        public enum FPGA_LASER_CONTROL { MODULATION, TRANSITION_POINTS, RAMPING };

        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        UsbRegistry usbRegistry;
        UsbDevice usbDevice;
        UsbEndpointReader spectralReader;
        UsbEndpointReader statusReader;

        Logger logger = Logger.getInstance();

        public uint integrationTimeMS = 0;

        ////////////////////////////////////////////////////////////////////////
        // Public properties
        ////////////////////////////////////////////////////////////////////////

        public uint pixels { get; private set; }
        public double[] wavelengths { get; private set; }
        public double[] wavenumbers { get; private set; }

        // post-processing processing
        public uint scanAveraging { get; set; }
        public uint boxcarHalfWidth { get; set; }
        public double[] dark { get; set; }

        // Feature Identification API
        public string descriptorSerialNum;
        public string firmwarePartNum;
        public string firmwareDesc;

        // FPGA compilation options
        public FPGA_INTEG_TIME_RES fpgaIntegrationTimeResolution { get; private set; }
        public FPGA_DATA_HEADER fpgaDataHeader { get; private set; }
        public bool fpgaHasCFSelect { get; private set; }
        public FPGA_LASER_TYPE fpgaLaserType { get; private set; }
        public FPGA_LASER_CONTROL fpgaLaserControl { get; private set; }
        public bool fpgaHasAreaScan { get; private set; }
        public bool fpgaHasActualIntegTime { get; private set; }
        public bool fpgaHasHorizBinning { get; private set; }

        // GET_MODEL_CONFIG (page 0)
        public string model { get; private set; }
        public string serialNumber { get; private set; }
        public int baudRate { get; private set; }
        public bool hasCooling { get; private set; }
        public bool hasBattery { get; private set; }
        public bool hasLaser { get; private set; }
        public short excitationNM { get; private set; }
        public short slitSizeUM { get; private set; }

        // GET_MODEL_CONFIG (page 1)
        public float[] wavecalCoeffs { get; private set; }
        public float[] detectorTempCoeffs { get; private set; }
        public float detectorTempMin { get; private set; }
        public float detectorTempMax { get; private set; }
        public float[] adcCoeffs { get; private set; }
        public short thermistorResistanceAt298K { get; private set; }
        public short thermistorBeta { get; private set; }
        public string calibrationDate { get; private set; }
        public string calibrationBy { get; private set; }

        // GET_MODEL_CONFIG (page 2)
        public string detectorName { get; private set; }
        public short activePixelsHoriz { get; private set; }
        public short activePixelsVert { get; private set; }
        public ushort minIntegrationTimeMS { get; private set; }
        public ushort maxIntegrationTimeMS { get; private set; }
        public short actualHoriz { get; private set; }
        public short ROIHorizStart { get; private set; }
        public short ROIHorizEnd { get; private set; }
        public short[] ROIVertRegionStart { get; private set; }
        public short[] ROIVertRegionEnd { get; private set; }
        // public float[] linearityCoeffs { get; private set; }

        // GET_MODEL_CONFIG (page 3)
        // public int deviceLifetimeOperationMinutes { get; private set; }
        // public int laserLifetimeOperationMinutes { get; private set; }
        // public short laserTemperatureMax { get; private set; }
        // public short laserTemperatureMin { get; private set; }

        // GET_MODEL_CONFIG (page 4)
        public string userText { get; private set; } // TODO: writeable

        // GET_MODEL_CONFIG (page 5)
        public short[] badPixels { get; private set; }

        ////////////////////////////////////////////////////////////////////////
        // Firmware Constants
        ////////////////////////////////////////////////////////////////////////

        #region constants
        // communication direction
        const byte HOST_TO_DEVICE = 0x40;
        const byte DEVICE_TO_HOST = 0xc0;

        // bRequest commands
        const byte ACQUIRE_CCD                              = 0xad; // impl
        const byte GET_ACTUAL_FRAMES                        = 0xe4;
        const byte GET_ACTUAL_INTEGRATION_TIME              = 0xdf;
        const byte GET_CCD_GAIN                             = 0xc5;
        const byte GET_CCD_OFFSET                           = 0xc4;
        const byte GET_CCD_SENSING_THRESHOLD                = 0xd1;
        const byte GET_CCD_TEMP                             = 0xd7; // impl
        const byte GET_CCD_TEMP_ENABLE                      = 0xda;
        const byte GET_CCD_TEMP_SETPOINT                    = 0xd9; // alias of GET_DAC
        const byte GET_CCD_THRESHOLD_SENSING_MODE           = 0xcf;
        const byte GET_CCD_TRIGGER_SOURCE                   = 0xd3;
        const byte GET_CODE_REVISION                        = 0xc0; // impl
        const byte GET_DAC                                  = 0xd9; // alias of GET_CCD_TEMP_SETPOINT
        const byte GET_EXTERNAL_TRIGGER_OUTPUT              = 0xe1;
        const byte GET_FPGA_REV                             = 0xb4; // impl
        const byte GET_HORIZ_BINNING                        = 0xbc;
        const byte GET_INTEGRATION_TIME                     = 0xbf; // impl
        const byte GET_INTERLOCK                            = 0xef;
        const byte GET_LASER                                = 0xe2;
        const byte GET_LASER_MOD                            = 0xe3;
        const byte GET_LASER_MOD_PULSE_WIDTH                = 0xdc;
        const byte GET_LASER_RAMPING_MODE                   = 0xea;
        const byte GET_LASER_TEMP                           = 0xd5; // impl
        const byte GET_LASER_TEMP_SETPOINT                  = 0xe8;
        const byte GET_LINK_LASER_MOD_TO_INTEGRATION_TIME   = 0xde;
        const byte GET_MOD_DURATION                         = 0xc3;
        const byte GET_MOD_PERIOD                           = 0xcb;
        const byte GET_MOD_PULSE_DELAY                      = 0xca;
        const byte GET_SELECTED_LASER                       = 0xee;
        const byte GET_TRIGGER_DELAY                        = 0xab;
        const byte LINK_LASER_MOD_TO_INTEGRATION_TIME       = 0xdd;
        const byte POLL_DATA                                = 0xd4; // impl
        const byte SECOND_TIER_COMMAND                      = 0xff; // impl
        const byte SELECT_HORIZ_BINNING                     = 0xb8;
        const byte SELECT_LASER                             = 0xed;
        const byte SET_CCD_GAIN                             = 0xb7;
        const byte SET_CCD_OFFSET                           = 0xb6;
        const byte SET_CCD_SENSING_THRESHOLD                = 0xd0;
        const byte SET_CCD_TEMP_ENABLE                      = 0xd6; // impl
        const byte SET_CCD_TEMP_SETPOINT                    = 0xd8; // alias of SET_DAC
        const byte SET_CCD_THRESHOLD_SENSING_MODE           = 0xce;
        const byte SET_CCD_TRIGGER_SOURCE                   = 0xd2; // impl
        const byte SET_DAC                                  = 0xd8; // alias of SET_CCD_TEMP_SETPOINT
        const byte SET_EXTERNAL_TRIGGER_OUTPUT              = 0xe0;
        const byte SET_INTEGRATION_TIME                     = 0xb2; // impl
        const byte SET_LASER                                = 0xbe; // impl
        const byte SET_LASER_MOD                            = 0xbd; // impl
        const byte SET_LASER_MOD_DUR                        = 0xb9;
        const byte SET_LASER_MOD_PULSE_WIDTH                = 0xdb;
        const byte SET_LASER_RAMPING_MODE                   = 0xe9;
        const byte SET_LASER_TEMP_SETPOINT                  = 0xe7;
        const byte SET_MOD_PERIOD                           = 0xc7;
        const byte SET_MOD_PULSE_DELAY                      = 0xc6;
        const byte SET_TRIGGER_DELAY                        = 0xaa;
        const byte VR_ENABLE_CCD_TEMP_CONTROL               = 0xd6;
        const byte VR_GET_CCD_TEMP_CONTROL                  = 0xda;
        const byte VR_GET_CONTINUOUS_CCD                    = 0xcc;
        const byte VR_GET_LASER_TEMP                        = 0xd5;
        const byte VR_GET_NUM_FRAMES                        = 0xcd;
        const byte VR_READ_CCD_TEMPERATURE                  = 0xd7;
        const byte VR_SET_CONTINUOUS_CCD                    = 0xc8;
        const byte VR_SET_NUM_FRAMES                        = 0xc9;

        // wValue for SECOND_TIER_COMMAND
        const byte GET_MODEL_CONFIG                         = 0x01; // impl
        const byte SET_MODEL_CONFIG                         = 0x02;
        const byte GET_LINE_LENGTH                          = 0x03;
        const byte READ_COMPILATION_OPTIONS                 = 0x04; // impl
        const byte OPT_INT_TIME_RES                         = 0x05;
        const byte OPT_DATA_HDR_TAB                         = 0x06;
        const byte OPT_CF_SELECT                            = 0x07;
        const byte OPT_LASER                                = 0x08;
        const byte OPT_LASER_CONTROL                        = 0x09;
        const byte OPT_AREA_SCAN                            = 0x0a;
        const byte OPT_ACT_INT_TIME                         = 0x0b;
        const byte OPT_HORIZONTAL_BINNING                   = 0x0c;
        #endregion

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public Spectrometer(UsbRegistry usbReg)
        {
            usbRegistry = usbReg;
            pixels = 0;

            wavecalCoeffs = new float[4];
            detectorTempCoeffs = new float[3];
            adcCoeffs = new float[3];
            ROIVertRegionStart = new short[3];
            ROIVertRegionEnd = new short[3];
            badPixels = new short[15];
            // linearityCoeffs = new float[5];
        }

        public bool open()
        {
            if (!usbRegistry.Open(out usbDevice))
            {
                logger.error("Spectrometer: failed to open UsbRegistry");
                return false;
            }

            // derive some values from PID
            performFeatureIdentification();

            // load EEPROM configuration
            if (!getModelConfig())
            {
                logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                usbDevice.Close();
                return false;
            }

            // see how the FPGA was compiled
            readCompilationOptions();

            // MustardTree uses 2048-pixel version of the S11510, and all InGaAs are 512
            pixels = (uint) activePixelsHoriz;

            wavelengths = Util.generateWavelengths(pixels, wavecalCoeffs);
            if (excitationNM > 0)
                wavenumbers = Util.wavelengthsToWavenumbers(excitationNM, wavelengths);

            spectralReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
            statusReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep06);

            // MZ: do we need something like this?
            setIntegrationTimeMS(minIntegrationTimeMS);

            return true;
        }

        public void close()
        {
            if (usbDevice != null)
            {
                if (usbDevice.IsOpen)
                {
                    setLaserEnable(false);

                    IUsbDevice wholeUsbDevice = usbDevice as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                        wholeUsbDevice.ReleaseInterface(0);
                    usbDevice.Close();
                }
                usbDevice = null;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Feature Identification API
        ////////////////////////////////////////////////////////////////////////

        void performFeatureIdentification()
        {
            if (usbRegistry.Pid == 0x1000)
            {
                descriptorSerialNum = "Raman FX2";
                firmwarePartNum = "170003";
                firmwareDesc = "Stroker USB Board FX2 Code";
            }
            else if (usbRegistry.Pid == 0x2000)
            {
                descriptorSerialNum = "InGaAs FX2";
                firmwarePartNum = "170037";
                firmwareDesc = "Hamamatsu InGaAs USB Board FX2 Code";
            }
            else if (usbRegistry.Pid == 0x3000)
            {
                descriptorSerialNum = "Dragster FX3";
                firmwarePartNum = "170001";
                firmwareDesc = "Dragster USB Board FX3 Code";
            }
            else if (usbRegistry.Pid == 0x4000)
            {
                descriptorSerialNum = "Stroker ARM";
                firmwarePartNum = "170019";
                firmwareDesc = "Stroker ARM USB Board";
            }
            else
            {
                logger.error("Unrecognized PID {0:x4}", usbRegistry.Pid);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // GET_MODEL_CONFIG
        ////////////////////////////////////////////////////////////////////////

        #region GET_MODEL_CONFIG
        bool getModelConfig()
        {
            List<byte[]> pages = new List<byte[]>();
            List<byte> format = new List<byte>();

            for (int page = 0; page < 6; page++)
            {
                byte[] buf = new byte[64];

                UsbSetupPacket setupPacket = new UsbSetupPacket(
                    DEVICE_TO_HOST,             // bRequestType
                    SECOND_TIER_COMMAND,        // bRequest,
                    GET_MODEL_CONFIG,           // wValue,
                    page,                       // wIndex
                    buf.Length);                // wLength

                if (!usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead))
                {
                    logger.error("failed GET_MODEL_CONFIG (page {0}, {1} bytes read)", page, bytesRead);
                    return false;
                }
                pages.Add(buf);
                format.Add(buf[63]); // page format is always last byte
            }

            try 
            {
                model                   = parseString(pages[0],  0, 16);
                serialNumber            = parseString(pages[0], 16, 16);
                baudRate                = parseInt32 (pages[0], 32); 
                hasCooling              = parseBool  (pages[0], 36);
                hasBattery              = parseBool  (pages[0], 37);
                hasLaser                = parseBool  (pages[0], 38);
                excitationNM            = parseInt16 (pages[0], 39);
                slitSizeUM              = parseInt16 (pages[0], 41);

                wavecalCoeffs[0]        = parseFloat (pages[1],  0);
                wavecalCoeffs[1]        = parseFloat (pages[1],  4);
                wavecalCoeffs[2]        = parseFloat (pages[1],  8);
                wavecalCoeffs[3]        = parseFloat (pages[1], 12);
                detectorTempCoeffs[0]   = parseFloat (pages[1], 16);
                detectorTempCoeffs[1]   = parseFloat (pages[1], 20);
                detectorTempCoeffs[2]   = parseFloat (pages[1], 24);
                detectorTempMax         = parseInt16 (pages[1], 28);
                detectorTempMin         = parseInt16 (pages[1], 30);
                adcCoeffs[0]            = parseFloat(pages[1], 32);
                adcCoeffs[1]            = parseFloat(pages[1], 36);
                adcCoeffs[2]            = parseFloat(pages[1], 40);
                thermistorResistanceAt298K = parseInt16(pages[1], 44);
                thermistorBeta          = parseInt16(pages[1], 46);
                calibrationDate         = parseString(pages[1], 48, 12);
                calibrationBy           = parseString(pages[1], 60, 3);

                detectorName            = parseString(pages[2], 0, 16);
                activePixelsHoriz       = parseInt16(pages[2], 16); // note: byte 18 apparently unused
                activePixelsVert        = parseInt16(pages[2], 19);
                minIntegrationTimeMS    = parseUInt16(pages[2], 21);
                maxIntegrationTimeMS    = parseUInt16(pages[2], 23);
                actualHoriz             = parseInt16(pages[2], 25);
                ROIHorizStart           = parseInt16(pages[2], 27);
                ROIHorizEnd             = parseInt16(pages[2], 29);
                ROIVertRegionStart[0]   = parseInt16(pages[2], 31);
                ROIVertRegionEnd[0]     = parseInt16(pages[2], 33);
                ROIVertRegionStart[1]   = parseInt16(pages[2], 35);
                ROIVertRegionEnd[1]     = parseInt16(pages[2], 37);
                ROIVertRegionStart[2]   = parseInt16(pages[2], 39);
                ROIVertRegionEnd[2]     = parseInt16(pages[2], 41);
                // linearityCoeff[0]    = parseFloat(pages[2], 43);
                // linearityCoeff[1]    = parseFloat(pages[2], 47);
                // linearityCoeff[2]    = parseFloat(pages[2], 51);
                // linearityCoeff[3]    = parseFloat(pages[2], 55);
                // linearityCoeff[4]    = parseFloat(pages[2], 59);

                // deviceLifetimeOperationMinutes = parseInt32(pages[3], 0);
                // laserLifetimeOperationMinutes = parseInt32(pages[3], 4);
                // laserTemperatureMax  = parseInt16(pages[3], 8);
                // laserTemperatureMin  = parseInt16(pages[3], 10);
                // laserTemperatureMax  = parseInt16(pages[3], 12); // dupe
                // laserTemperatureMin  = parseInt16(pages[3], 14); // dupe

                userText = parseString(pages[4], 0, 63); // note: right-trimmed

                for (int i = 0; i < 15; i++)
                    badPixels[i] = parseInt16(pages[5], i * 2);
            }
            catch (Exception ex)
            {
                logger.error("getModelConfig: caught exception: {0}", ex.Message);
                return false;
            }

            if (logger.debugEnabled())
                debugModelConfig();

            return true;
        }

        void debugModelConfig()
        {
            logger.debug("Model             = {0}", model);
            logger.debug("serialNumber      = {0}", serialNumber);
            logger.debug("baudRate          = {0}", baudRate);
            logger.debug("hasCooling        = {0}", hasCooling);
            logger.debug("hasBattery        = {0}", hasBattery);
            logger.debug("hasLaser          = {0}", hasLaser);
            logger.debug("excitationNM      = {0}", excitationNM);
            logger.debug("slitSizeUM        = {0}", slitSizeUM);

            for (int i = 0; i < wavecalCoeffs.Length; i++)
                logger.debug("wavecalCoeffs[{0}]  = {1}", i, wavecalCoeffs[i]);
            for (int i = 0; i < detectorTempCoeffs.Length; i++)
                logger.debug("detectorTempCoeffs[{0}] = {1}", i, detectorTempCoeffs[i]);
            logger.debug("detectorTempMin   = {0}", detectorTempMin);
            logger.debug("detectorTempMax   = {0}", detectorTempMax);
            for (int i = 0; i < adcCoeffs.Length; i++)
                logger.debug("adcCoeffs[{0}]      = {1}", i, detectorTempCoeffs[i]);
            logger.debug("thermistorResistanceAt298K = {0}", thermistorResistanceAt298K);
            logger.debug("thermistorBeta    = {0}", thermistorBeta);
            logger.debug("calibrationDate   = {0}", calibrationDate);
            logger.debug("calibrationBy     = {0}", calibrationBy);

            logger.debug("detectorName      = {0}", detectorName);
            logger.debug("activePixelsHoriz = {0}", activePixelsHoriz);
            logger.debug("activePixelsVert  = {0}", activePixelsVert);
            logger.debug("minIntegrationTimeMS = {0}", minIntegrationTimeMS);
            logger.debug("maxIntegrationTimeMS = {0}", maxIntegrationTimeMS);
            logger.debug("actualHoriz       = {0}", actualHoriz);
            logger.debug("ROIHorizStart     = {0}", ROIHorizStart);
            logger.debug("ROIHorizEnd       = {0}", ROIHorizEnd);
            for (int i = 0; i < ROIVertRegionStart.Length; i++)
                logger.debug("ROIVertRegionStart[{0}] = {1}", i, ROIVertRegionStart[i]);
            for (int i = 0; i < ROIVertRegionEnd.Length; i++)
                logger.debug("ROIVertRegionEnd[{0}]   = {1}", i, ROIVertRegionEnd[i]);

            logger.debug("userText          = {0}", userText);

            for (int i = 0; i < badPixels.Length; i++)
                logger.debug("badPixels[{0}]      = {1}", i, badPixels[i]);
        }
#endregion

        #region parsers
        String parseString(byte[] buf, int index, int len)
        {
            String s = "";
            for (int i = 0; i < len; i++)
            {
                int pos = index + i;
                if (buf[pos] == 0)
                    break;
                s += (char)buf[pos];
            }
            return s.TrimEnd();
        }

        bool parseBool(byte[] buf, int index)
        {
            return buf[index] != 0;
        }

        float parseFloat(byte[] buf, int index)
        {
            return System.BitConverter.ToSingle(buf, index);
        }

        short parseInt16(byte[] buf, int index)
        {
            return System.BitConverter.ToInt16(buf, index);
        }

        ushort parseUInt16(byte[] buf, int index)
        {
            return System.BitConverter.ToUInt16(buf, index);
        }

        int parseInt32(byte[] buf, int index)
        {
            return System.BitConverter.ToInt32(buf, index);
        }

        uint parseUInt32(byte[] buf, int index)
        {
            return System.BitConverter.ToUInt32(buf, index);
        }
        #endregion

        /// <summary>
        /// Read FPGA compiler options; for values, see ENG-0034.
        /// </summary>
        void readCompilationOptions()
        {
            byte[] buf = getCmd2(READ_COMPILATION_OPTIONS, 2);
            if (buf == null)
                return;

            ushort word = (ushort) (buf[0] | (buf[1] << 8));
            logger.debug("FPGA compiler options: 0x{0:x4}", word);

            // bits 0-2: 0000 0000 0000 0111 fpgaIntegrationTimeResolution
            // bit  3-5: 0000 0000 0011 1000 fpgaDataHeader
            // bit    6: 0000 0000 0100 0000 fpgaHasCFSelect
            // bit  7-8: 0000 0001 1000 0000 fpgaLaserType
            // bit 9-11: 0000 1110 0000 0000 fpgaLaserControl
            // bit   12: 0001 0000 0000 0000 fpgaHasAreaScan
            // bit   13: 0010 0000 0000 0000 fpgaHasActualIntegTime
            // bit   14: 0100 0000 0000 0000 fpgaHasHorizBinning

            fpgaIntegrationTimeResolution = (FPGA_INTEG_TIME_RES) (word & 0x07);
            fpgaDataHeader = (FPGA_DATA_HEADER) ((word & 0x0038) >> 3);
            fpgaHasCFSelect = (word & 0x0040) != 0;
            fpgaLaserType = (FPGA_LASER_TYPE)((word & 0x0180) >> 7);
            fpgaLaserControl = (FPGA_LASER_CONTROL)((word & 0x0e00) >> 9);
            fpgaHasAreaScan = (word & 0x1000) != 0;
            fpgaHasActualIntegTime = (word & 0x2000) != 0;
            fpgaHasHorizBinning = (word & 0x4000) != 0;

            if (logger.debugEnabled())
            {
                logger.debug("  fpgaIntegrationTimeResolution = {0}", fpgaIntegrationTimeResolution);
                logger.debug("  fpgaDataHeader                = {0}", fpgaDataHeader);
                logger.debug("  fpgaHasCFSelect               = {0}", fpgaHasCFSelect);
                logger.debug("  fpgaLaserType                 = {0}", fpgaLaserType);
                logger.debug("  fpgaLaserControl              = {0}", fpgaLaserControl);
                logger.debug("  fpgaHasAreaScan               = {0}", fpgaHasAreaScan);
                logger.debug("  fpgaHasActualIntegTime        = {0}", fpgaHasActualIntegTime);
                logger.debug("  fpgaHasHorizBinning           = {0}", fpgaHasHorizBinning);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Utilities
        ////////////////////////////////////////////////////////////////////////

        byte[] getCmd(byte bRequest, int len)
        {
            byte[] buf = new byte[len];

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                DEVICE_TO_HOST,             // bRequestType
                bRequest,                   // bRequest,
                0,                          // wValue,
                0,                          // wIndex
                len);                       // wLength

            if (!usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead) || bytesRead < len)
            {
                logger.error("getCmd: failed to get 0x{0:x2} via DEVICE_TO_HOST ({1} bytes read)", bRequest, bytesRead);
                return null;
            }
            return buf;
        }

        byte[] getCmd2(int wValue, int len)
        {
            byte[] buf = new byte[len];

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                DEVICE_TO_HOST,             // bRequestType
                SECOND_TIER_COMMAND,        // bRequest,
                wValue,                     // wValue,
                0,                          // wIndex
                len);                       // wLength

            if (!usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead) || bytesRead < len)
            {
                logger.error("getCmd2: failed to get SECOND_TIER_COMMAND 0x{0:x2} via DEVICE_TO_HOST ({1} bytes read)", wValue, bytesRead);
                return null;
            }
            return buf;
        }

        bool sendCmd(byte bRequest, int wValue = 0, int wIndex = 0)
        {
            UsbSetupPacket setupPacket = new UsbSetupPacket(
                HOST_TO_DEVICE,             // bRequestType
                bRequest,                   // bRequest,
                wValue,                     // wValue,
                wIndex,                     // wIndex
                0);                         // wLength

            if (!usbDevice.ControlTransfer(ref setupPacket, null, 0, out int bytesWritten))
            {
                logger.error("sendCmd: failed to send 0x{0:x2} (0x{1:x2}, 0x{2:x2})", bRequest, wValue, wIndex);
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // Spectrometer Comms
        ////////////////////////////////////////////////////////////////////////

        /// </summary>
        /// <param name="ms">integration time in milliseconds</param>        
        /// <remarks>Does not currently support microsecond resolution</remarks>
        public void setIntegrationTimeMS(uint ms)
        {
            if (ms < minIntegrationTimeMS)
            {
                logger.error("rounded integration time {0}ms up to min {1}ms", ms, minIntegrationTimeMS);
                ms = minIntegrationTimeMS;
            }
            else if (ms > maxIntegrationTimeMS)
            {
                logger.error("rounded integration time {0}ms down to max {1}ms", ms, maxIntegrationTimeMS);
                ms = maxIntegrationTimeMS;
            }

            uint LSW = ms % 65536;
            uint MSW = ms / 65536;

            // cache for convenience
            if (sendCmd(SET_INTEGRATION_TIME, (int) LSW, (int) MSW))
                integrationTimeMS = ms;
        }

        public void linkLaserModToIntegrationTime(bool flag)
        {
            sendCmd(LINK_LASER_MOD_TO_INTEGRATION_TIME, flag ? 1 : 0);
        }

        public int getIntegrationTimeMS()
        {
            byte[] buf = getCmd(GET_INTEGRATION_TIME, 6);
            if (buf == null)
                return -1;

            int ms =  buf[0]
                   + (buf[1] << 8)
                   + (buf[2] << 16);

            integrationTimeMS = (uint)ms;

            return ms;
        }

        public string getFPGARev()
        {
            byte[] buf = getCmd(GET_FPGA_REV, 7);

            string s = "";
            for (uint i = 0; i < 7; i++)
                s += (char)buf[i];

            return s.TrimEnd();
        }

        public string getFirmwareRev()
        {
            byte[] buf = getCmd(GET_CODE_REVISION, 4);
            if (buf == null)
                return "ERROR";

            string s = "";
            for (uint i = 0; i < 4; i++)
            {
                s += String.Format("{0}", buf[i]);
                if (i < 3)
                    s += ".";
            }

            return s;
        }

        public ushort getLaserTemperatureRaw()
        {
            byte[] buf = getCmd(GET_LASER_TEMP, 2);
            if (buf == null)
                return 0;

            return (ushort) (buf[0] + (buf[1] << 8));
        }

        public void setDetectorTECEnable(bool flag)
        {
            sendCmd(SET_CCD_TEMP_ENABLE, flag ? 1 : 0);
        }

        public ushort getDetectorTemperatureRaw()
        {
            byte[] buf = getCmd(GET_CCD_TEMP, 2);
            if (buf == null)
                return 0;

            return (ushort) (buf[0] + (buf[1] << 8));
        }

        public void setLaserEnable(bool flag)
        {
            sendCmd(SET_LASER, flag ? 1 : 0);
        }

        public void setLaserMod(bool flag)
        {
            sendCmd(SET_LASER_MOD, flag ? 1 : 0);
        }

        public void setCCDTriggerSource(ushort source)
        {
            sendCmd(SET_CCD_TRIGGER_SOURCE, source);
        }

        ////////////////////////////////////////////////////////////////////////
        // getSpectrum
        ////////////////////////////////////////////////////////////////////////

        // includes scan averaging, boxcar and dark subtraction
        public double[] getSpectrum()
        {
            double[] sum = getSpectrumRaw();
            if (sum == null)
                return null;

            if (scanAveraging > 0)
            {
                for (uint i = 1; i < scanAveraging; i++)
                {
                    double[] tmp = getSpectrumRaw();
                    if (tmp == null)
                        return null;

                    for (int px = 0; px < pixels; px++)
                        sum[px] += tmp[px];
                }

                for (int px = 0; px < pixels; px++)
                    sum[px] /= scanAveraging;
            }

            // TODO: we have a race condition here, if a caller sets dark to null in the middle of this loop
            double[] tmpDark = dark;
            if (tmpDark != null && tmpDark.Length == sum.Length)
                for (int px = 0; px < pixels; px++)
                    sum[px] -= tmpDark[px];

            if (boxcarHalfWidth > 0)
                return Util.applyBoxcar(boxcarHalfWidth, sum);
            else
                return sum;
        }

        // just the bytes, ma'am
        double[] getSpectrumRaw()
        {
            // TODO: refactor into smaller methods

            ////////////////////////////////////////////////////////////////////
            // STEP ONE: request a spectrum
            ////////////////////////////////////////////////////////////////////

            DateTime timeRequest = DateTime.Now;
            UsbSetupPacket setupPacket = new UsbSetupPacket(
                HOST_TO_DEVICE,             // bRequestType
                ACQUIRE_CCD,                // bRequest,
                0,                          // wValue,
                0,                          // wIndex
                0);                         // wLength

            int bytesWritten = 0;
            if (!usbDevice.ControlTransfer(ref setupPacket, null, 0, out bytesWritten))
            {
                logger.error("failed to send ACQUIRE_CCD");
                return null;
            }

            ////////////////////////////////////////////////////////////////////
            // STEP TWO: wait for acquisition to complete
            ////////////////////////////////////////////////////////////////////

            if (!blockUntilDataReady())
                return null;
            DateTime timeComplete = DateTime.Now;

            ////////////////////////////////////////////////////////////////////
            // STEP THREE: read spectrum
            ////////////////////////////////////////////////////////////////////

            // NOTE: API recommends reading this in four 512-byte chunks. I got
            //       occasional timeout errors on the last chunk when following
            //       that procedure. Checking Enlighten source code, it seemed
            //       to perform the read in a single 2048-byte block. That seems
            //       to work well here too.
            byte[] response = new byte[pixels * 2]; 

            double[] spec = new double[pixels];
            int timeoutMS = (int)100;
            int bytesRead = 0;

            uint pixel = 0;
            ushort sum = 0;
            while (pixel < pixels)
            {
                // read the next block of data
                ErrorCode err;
                try
                {
                    err = spectralReader.Read(response, timeoutMS, out bytesRead);
                }
                catch (Exception ex)
                {
                    logger.error("getSpectrum: {0}", ex.Message);
                    return null;
                }

                if (err == ErrorCode.Ok)
                {
                    if (bytesRead != response.Length)
                    {
                        logger.error("getSpectrum: read different number of bytes than expected (pixel {0}, bytesRead {1})", pixel, bytesRead);
                        return null;
                    }

                    for (uint i = 0; i < bytesRead && pixel < pixels; i += 2)
                    {
                        ushort value = (ushort)(response[i] + (response[i + 1] << 8));
                        spec[pixel++] = value;
                        sum += value;
                    }
                }
                else
                {
                    logger.error("getSpectrum: received ErrorCode {0} while reading spectrum from pixel {1}", err, pixel);
                    return null;
                }
            }

            ////////////////////////////////////////////////////////////////////
            // STEP FOUR: read status
            ////////////////////////////////////////////////////////////////////

            // spectrum status is currently unimplemented in firmware
            if (false)
            {
                byte[] status = new byte[8];
                try
                {
                    bytesRead = 0;
                    ErrorCode err = statusReader.Read(status, timeoutMS, out bytesRead);

                    if (bytesRead >= 7 && err == ErrorCode.Ok)
                    {
                        ushort checksum = (ushort)(status[0] + (status[1] << 8));
                        ushort frame = (ushort)(status[2] + (status[3] << 8));
                        uint integTimeMS = (uint)(status[4] + (status[5] << 8) + (status[6] << 16));

                        logger.debug("getSpectrum: status = 0x{0:x2}{1:x2} {2:x2}{3:x2} {4:x2}{5:x2}{6:x2} (checksum {8}, frame {9}, integTimeMS {10})",
                            status[0], status[1], status[2], status[3], status[4], status[5], status[6], status[7],
                            checksum, frame, integTimeMS);

                        if (sum != checksum)
                            logger.error("getSpectrum: sum {0} != checksum {1}", sum, checksum);
                    }
                    else
                    {
                        logger.error("getSpectrum: only read {0} bytes of status (ErrorCode {1})", bytesRead, err);
                    }
                }
                catch (Exception ex)
                {
                    logger.error("getSpectrum: caught exception when reading status: {0}", ex.Message);
                }
            }

            if (false && logger.debugEnabled())
            {
                TimeSpan withReadout = DateTime.Now - timeRequest;
                TimeSpan actualIntegration = timeComplete - timeRequest;
                logger.debug("getSpectrum: read {0} pixels in {1:f2}ms ({2:f2}ms integration)", 
                    pixels, withReadout.TotalMilliseconds, actualIntegration.TotalMilliseconds);
            }

            return spec;
        }

        public bool blockUntilDataReady()
        {
            // Empirically found adequate in developer testing
            const int SLEEP_UNDERRUN = 5;

            // rather than banging at the control endpoint, sleep for most of it
            if (integrationTimeMS > SLEEP_UNDERRUN)
                Thread.Sleep((int)(integrationTimeMS - SLEEP_UNDERRUN));

            // construct a poll packet for re-use
            byte[] pollResponse = new byte[4];
            UsbSetupPacket pollPacket = new UsbSetupPacket(
                DEVICE_TO_HOST,             // bRequestType
                POLL_DATA,                  // bRequest,
                0,                          // wValue,
                0,                          // wIndex
                0);                         // wLength

            // give it an extra 100ms buffer before we give up
            uint timeoutMS = integrationTimeMS + 100;
            DateTime expiration = DateTime.Now.AddMilliseconds(timeoutMS);

            while (DateTime.Now < expiration)
            {
                // poll the spectrometer to see if spectral data is waiting to be read
                int bytesWritten = 0;
                if (!usbDevice.ControlTransfer(ref pollPacket, pollResponse, pollResponse.Length, out bytesWritten) || bytesWritten == 0)
                {
                    logger.error("failed to send POLL_DATA");
                    return false;
                }

                if (pollResponse[0] != 0)
                    return true;
            }
            logger.error("blockUntilDataReady timed-out after {0}ms", timeoutMS);
            return false;
        }
    }
}
