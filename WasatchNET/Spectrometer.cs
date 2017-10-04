using System;
using System.Collections.Generic;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace WasatchNET
{
    /// <summary>
    /// Encapsulates a logical Wasatch Photonics spectrometer for communication. 
    /// </summary>
    ///
    /// <remarks>
    /// For WP spectrometer API, see "WP Raman USB Interface Specification r1.4"
    ///
    /// NOT rigorously tested for thread-safety. It MAY be thread-safe, 
    /// it WILL be thread-safe, but currently it is not GUARANTEED thread-safe.
    /// The only synchronization at present are some locks around getSpectrum()
    /// and clearly-related properties like integrationTimeMS, dark, 
    /// scansToAverage, boxcarHalfWidth etc. 
    ///
    /// Currently the only supported bus is USB (using LibUsbDotNet). There are
    /// relatively few accesses to the raw USB objects in this implementation, 
    /// so it should be relatively easy to refactor to support Bluetooth, 
    /// Ethernet etc when I get test units.
    ///
    /// Most features are provided as methods, with only a few acquisition-related
    /// features provided as properties. I don't have a strong reason for this,
    /// and may change things. Basically, I wanted to distinguish between 
    /// properties that I knew would be accessed frequently from application code
    /// (e.g. pixels, wavelengths, integrationTime) and should therefore be cached in
    /// the driver, versus settings that should always be communicated 
    /// explicitly with the spectrometer (e.g. laser enable status). Also, it 
    /// seemed efficient to unpack GetModelConfig and FPGACompilationOptions 
    /// into cached properties rather than making each into duplicate API calls.
    ///
    /// Nevertheless...it seems more ".NET"-ish to expose most get/set pairs as
    /// properties.  This clay remains soft.
    ///
    /// KNOWN ISSUES:
    /// - Locking doesn't seem sufficient to synchronize parallel calls from
    ///   Settings.updateAll() and BackgroundWorkerAcquisition in WinFormDemo.
    ///   Not sure what I'm missing.
    /// </remarks>
    public class Spectrometer
    {
        ////////////////////////////////////////////////////////////////////////
        // Constants
        ////////////////////////////////////////////////////////////////////////

        public const byte HOST_TO_DEVICE = 0x40;
        public const byte DEVICE_TO_HOST = 0xc0;

        ////////////////////////////////////////////////////////////////////////
        // Inner types
        ////////////////////////////////////////////////////////////////////////

        public class AcquisitionStatus
        {
            public ushort checksum;
            public ushort frame;
            public uint integTimeMS;
        }

        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        UsbRegistry usbRegistry;
        UsbDevice usbDevice;
        UsbEndpointReader spectralReader;
        UsbEndpointReader statusReader;

        Dictionary<Opcodes, byte> cmd = OpcodeUtil.getDictionary();
        Logger logger = Logger.getInstance();

        object acquisitionLock = new object();
        object commsLock = new object();

        #region properties

        ////////////////////////////////////////////////////////////////////////
        // Public properties
        ////////////////////////////////////////////////////////////////////////

        public uint pixels { get; private set; }
        public double[] wavelengths { get; private set; }
        public double[] wavenumbers { get; private set; }

        // Feature Identification API
        FeatureIdentification featureIdentification;

        // FPGA Compilation Options
        public FPGAOptions fpgaOptions;

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
        public byte[] userData { get; private set; } 
        public string userText { get { return ParseData.toString(userData); } }

        // GET_MODEL_CONFIG (page 5)
        public short[] badPixels { get; private set; }

        ////////////////////////////////////////////////////////////////////////
        // complex properties
        ////////////////////////////////////////////////////////////////////////

        // Each of these has to have an explicit private version, because 
        // synchronization requires an explicit setter and C# doesn't create 
        // implicit private attibutes if you have an explicit accessor.

        /// <summary>
        /// Current integration time in milliseconds. Reading this property
        /// returns a CACHED value for performance reasons; use getIntegrationTimeMS
        /// to read from spectrometer.
        /// </summary>
        public uint integrationTimeMS
        {
            get { return integrationTimeMS_; }
            set { lock (acquisitionLock) setIntegrationTimeMS(value); }
        }
        private uint integrationTimeMS_;

        /// <summary>
        /// How many acquisitions to average together (zero for no averaging)
        /// </summary>
        public uint scanAveraging
        {
            get { return scanAveraging_; }
            set { lock(acquisitionLock) scanAveraging_ = value; }
        }
        private uint scanAveraging_;

        /// <summary>
        /// Perform post-acquisition high-frequency smoothing by averaging
        /// together "n" pixels to either side of each acquired pixel; zero
        /// to disable (default).
        /// </summary>
        public uint boxcarHalfWidth
        {
            get { return boxcarHalfWidth_;  }
            set { lock (acquisitionLock) boxcarHalfWidth_ = value; }
        }
        private uint boxcarHalfWidth_;

        /// <summary>
        /// Perform automatic dark subtraction by setting this property to
        /// an acquired dark spectrum; leave "null" to disable.
        /// </summary>
        public double[] dark
        {
            get { return dark_; }
            set { lock(acquisitionLock) dark_ = value; }
        }
        private double[] dark_;
        #endregion

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        #region lifecycle
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
            featureIdentification = new FeatureIdentification(usbRegistry.Pid);

            // load EEPROM configuration
            if (!getModelConfig())
            {
                logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                usbDevice.Close();
                return false;
            }

            // see how the FPGA was compiled
            fpgaOptions = new FPGAOptions(this);

            // MustardTree uses 2048-pixel version of the S11510, and all InGaAs are 512
            pixels = (uint) activePixelsHoriz;

            wavelengths = Util.generateWavelengths(pixels, wavecalCoeffs);
            if (excitationNM > 0)
                wavenumbers = Util.wavelengthsToWavenumbers(excitationNM, wavelengths);

            spectralReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
            statusReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep06);

            // by default, integration time is zero in HW
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
        #endregion

        ////////////////////////////////////////////////////////////////////////
        // Utilities
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Execute a request-response control transaction using the given opcode.
        /// </summary>
        /// <param name="opcode">the opcode of the desired request</param>
        /// <param name="len">the number of needed return bytes</param>
        /// <param name="wIndex">an optional numeric argument used by some opcodes</param>
        /// <param name="fullLen">the actual number of expected return bytes (not all needed)</param>
        /// <returns>the array of returned bytes (null on error)</returns>
        internal byte[] getCmd(Opcodes opcode, int len, ushort wIndex = 0, int fullLen = 0)
        {
            int bytesToRead = fullLen == 0 ? len : fullLen;
            byte[] buf = new byte[bytesToRead];

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                DEVICE_TO_HOST, // bRequestType
                cmd[opcode],    // bRequest
                0,              // wValue
                wIndex,         // wIndex
                bytesToRead);   // wLength

            // Question: if the device returns 6 bytes on Endpoint 0, but I only
            // need the first so pass byte[1], are the other 5 bytes discarded or
            // queued?
            lock (commsLock)
            {
                if (!usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead) || bytesRead < bytesToRead)
                {
                    logger.error("getCmd: failed to get {0} (0x{1:x2}) with index 0x{2:x4} via DEVICE_TO_HOST ({3} bytes read)",
                        opcode.ToString(), cmd[opcode], wIndex, bytesRead);
                    return null;
                }
            }

            if (fullLen == 0)
                return buf;

            // extract just the bytes we really needed
            byte[] tmp = new byte[len];
            Array.Copy(buf, tmp, len);
            return tmp;
        }

        /// <summary>
        /// Execute a request-response transfer with a "second-tier" request.
        /// </summary>
        /// <param name="opcode">the wValue to send along with the "second-tier" command</param>
        /// <param name="len">how many bytes of response are expected</param>
        /// <returns>array of returned bytes (null on error)</returns>
        internal byte[] getCmd2(Opcodes opcode, int len, ushort wIndex = 0)
        {
            byte[] buf = new byte[len];

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                DEVICE_TO_HOST,                     // bRequestType
                cmd[Opcodes.SECOND_TIER_COMMAND],   // bRequest
                cmd[opcode],                        // wValue
                wIndex,                             // wIndex
                len);                               // wLength

            lock (commsLock)
            {
                if (!usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead) || bytesRead < len)
                {
                    logger.error("getCmd2: failed to get SECOND_TIER_COMMAND {0} (0x{1:x4}) via DEVICE_TO_HOST ({2} bytes read)",
                        opcode.ToString(), cmd[opcode], bytesRead);
                    return null;
                }
            }
            return buf;
        }

        /// <summary>
        /// send a single control transfer command (response not checked)
        /// </summary>
        /// <param name="opcode">the desired command</param>
        /// <param name="wValue">an optional secondary argument used by some commands</param>
        /// <param name="wIndex">an optional tertiary argument used by some commands</param>
        /// <param name="buf">an data buffer used by some commands</param>
        /// <returns>true on success, false on error</returns>
        bool sendCmd(Opcodes opcode, ushort wValue = 0, ushort wIndex = 0, byte[] buf = null)
        {
            ushort wLength = (ushort)((buf == null) ? 0 : buf.Length);

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                HOST_TO_DEVICE, // bRequestType
                cmd[opcode],    // bRequest
                wValue,         // wValue
                wIndex,         // wIndex
                wLength);       // wLength

            lock (commsLock)
            {
                if (!usbDevice.ControlTransfer(ref setupPacket, buf, wLength, out int bytesWritten))
                {
                    logger.error("sendCmd: failed to send {0} (0x{1:x2}) (wValue 0x{2:x4}, wIndex 0x{3:x4}, wLength 0x{4:x4})",
                        opcode.ToString(), cmd[opcode], wValue, wIndex, wLength);
                    return false;
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // GET_MODEL_CONFIG
        ////////////////////////////////////////////////////////////////////////

        #region GET_MODEL_CONFIG
        bool getModelConfig()
        {
            List<byte[]> pages = new List<byte[]>();
            List<byte> format = new List<byte>();

            for (ushort page = 0; page < 6; page++)
            {
                byte[] buf = getCmd2(Opcodes.GET_MODEL_CONFIG, 64, wIndex: page);
                if (buf == null)
                    return false;

                pages.Add(buf);
                format.Add(buf[63]); // page format is always last byte
            }

            try 
            {
                model                   = ParseData.toString(pages[0],  0, 16);
                serialNumber            = ParseData.toString(pages[0], 16, 16);
                baudRate                = ParseData.toInt32 (pages[0], 32); 
                hasCooling              = ParseData.toBool  (pages[0], 36);
                hasBattery              = ParseData.toBool  (pages[0], 37);
                hasLaser                = ParseData.toBool  (pages[0], 38);
                excitationNM            = ParseData.toInt16 (pages[0], 39);
                slitSizeUM              = ParseData.toInt16 (pages[0], 41);

                wavecalCoeffs[0]        = ParseData.toFloat (pages[1],  0);
                wavecalCoeffs[1]        = ParseData.toFloat (pages[1],  4);
                wavecalCoeffs[2]        = ParseData.toFloat (pages[1],  8);
                wavecalCoeffs[3]        = ParseData.toFloat (pages[1], 12);
                detectorTempCoeffs[0]   = ParseData.toFloat (pages[1], 16);
                detectorTempCoeffs[1]   = ParseData.toFloat (pages[1], 20);
                detectorTempCoeffs[2]   = ParseData.toFloat (pages[1], 24);
                detectorTempMax         = ParseData.toInt16 (pages[1], 28);
                detectorTempMin         = ParseData.toInt16 (pages[1], 30);
                adcCoeffs[0]            = ParseData.toFloat(pages[1], 32);
                adcCoeffs[1]            = ParseData.toFloat(pages[1], 36);
                adcCoeffs[2]            = ParseData.toFloat(pages[1], 40);
                thermistorResistanceAt298K = ParseData.toInt16(pages[1], 44);
                thermistorBeta          = ParseData.toInt16(pages[1], 46);
                calibrationDate         = ParseData.toString(pages[1], 48, 12);
                calibrationBy           = ParseData.toString(pages[1], 60, 3);

                detectorName            = ParseData.toString(pages[2], 0, 16);
                activePixelsHoriz       = ParseData.toInt16(pages[2], 16); // note: byte 18 apparently unused
                activePixelsVert        = ParseData.toInt16(pages[2], 19);
                minIntegrationTimeMS    = ParseData.toUInt16(pages[2], 21);
                maxIntegrationTimeMS    = ParseData.toUInt16(pages[2], 23);
                actualHoriz             = ParseData.toInt16(pages[2], 25);
                ROIHorizStart           = ParseData.toInt16(pages[2], 27);
                ROIHorizEnd             = ParseData.toInt16(pages[2], 29);
                ROIVertRegionStart[0]   = ParseData.toInt16(pages[2], 31);
                ROIVertRegionEnd[0]     = ParseData.toInt16(pages[2], 33);
                ROIVertRegionStart[1]   = ParseData.toInt16(pages[2], 35);
                ROIVertRegionEnd[1]     = ParseData.toInt16(pages[2], 37);
                ROIVertRegionStart[2]   = ParseData.toInt16(pages[2], 39);
                ROIVertRegionEnd[2]     = ParseData.toInt16(pages[2], 41);
                // linearityCoeff[0]    = ParseData.toFloat(pages[2], 43);
                // linearityCoeff[1]    = ParseData.toFloat(pages[2], 47);
                // linearityCoeff[2]    = ParseData.toFloat(pages[2], 51);
                // linearityCoeff[3]    = ParseData.toFloat(pages[2], 55);
                // linearityCoeff[4]    = ParseData.toFloat(pages[2], 59);

                // deviceLifetimeOperationMinutes = ParseData.toInt32(pages[3], 0);
                // laserLifetimeOperationMinutes = ParseData.toInt32(pages[3], 4);
                // laserTemperatureMax  = ParseData.toInt16(pages[3], 8);
                // laserTemperatureMin  = ParseData.toInt16(pages[3], 10);
                // laserTemperatureMax  = ParseData.toInt16(pages[3], 12); // dupe
                // laserTemperatureMin  = ParseData.toInt16(pages[3], 14); // dupe

                userData = new byte[63];
                Array.Copy(pages[4], userData, userData.Length);

                for (int i = 0; i < 15; i++)
                    badPixels[i] = ParseData.toInt16(pages[5], i * 2);
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
        

        #region spec_comms

        ////////////////////////////////////////////////////////////////////////
        // Complex settors
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// set the acquisition time in milliseconds
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

            ushort LSW = (ushort) (ms % 65536);
            ushort MSW = (ushort) (ms / 65536);

            // cache for performance 
            if (sendCmd(Opcodes.SET_INTEGRATION_TIME, LSW, MSW))
                integrationTimeMS_ = ms;
        }

        public void setExternalTriggerOutput(EXTERNAL_TRIGGER_OUTPUT value)
        {
            if (value != EXTERNAL_TRIGGER_OUTPUT.ERROR)
                sendCmd(Opcodes.SET_EXTERNAL_TRIGGER_OUTPUT, (ushort) value);
        }

        /// <summary>
        /// Sets the laser modulation duration to the given 40-bit value.
        /// </summary>
        /// <remarks>
        /// The LSB represents 1 microsecond (us).  Therefore, the precision is
        /// approximately 1/256 us (4ns).  It's a 40-bit value, so the range
        /// is 2^32 us (about 70min).
        /// </remarks>
        /// <param name="value">40-bit duration</param>
        public void setLaserModulationDuration(UInt64 value)
        {
            try
            {
                UInt40 val = new UInt40(value);
                sendCmd(Opcodes.SET_LASER_MOD_DUR, val.LSW, val.MidW, val.buf);
            }
            catch(Exception ex)
            {
                logger.error("WasatchNET.setLaserModulationDuration: {0}", ex.Message);
                return;
            }
        }

        /// <summary>
        /// Sets the laser modulation duration to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit duration</param>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        public void setLaserModulationPulseWidth(UInt64 value)
        {
            try
            {
                UInt40 val = new UInt40(value);
                sendCmd(Opcodes.SET_LASER_MOD_PULSE_WIDTH, val.LSW, val.MidW, val.buf);
            }
            catch(Exception ex)
            {
                logger.error("WasatchNET.setLaserModulationPulseWidth: {0}", ex.Message);
                return;
            }
        }

        /// <summary>
        /// Sets the laser modulation period to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit period</param>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        public void setLaserModulationPeriod(UInt64 value)
        {
            try
            {
                UInt40 val = new UInt40(value);
                sendCmd(Opcodes.SET_MOD_PERIOD, val.LSW, val.MidW, val.buf);
            }
            catch(Exception ex)
            {
                logger.error("WasatchNET.setLaserModulationPeriod: {0}", ex.Message);
                return;
            }
        }

        /// <summary>
        /// Sets the laser modulation pulse delay to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit period</param>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        public void setLaserModulationPulseDelay(UInt64 value)
        {
            try
            {
                UInt40 val = new UInt40(value);
                sendCmd(Opcodes.SET_MOD_PULSE_DELAY, val.LSW, val.MidW, val.buf);
            }
            catch(Exception ex)
            {
                logger.error("WasatchNET.setLaserModulationPulseDelay: {0}", ex.Message);
                return;
            }
        }

        public void setLaserTemperatureSetpoint(byte value)
        {
            const byte MAX = 127;
            if (value > MAX)
            {
                logger.error("WasatchNET.setLaserTemperatureSetpoint: value {0} exceeded max {1}", value, MAX);
                return;
            }
            sendCmd(Opcodes.SET_LASER_TEMP_SETPOINT, value);
        }

        /// <summary>
        /// Set the trigger delay on supported models.
        /// </summary>
        /// <param name="value">24-bit value (0.5us)</param>
        /// <remarks>
        /// The value is in 0.5 microseconds, and supports a 24-bit unsigned value,
        /// so the total range is 2^23 microseconds (about 8.3 sec).
        /// </remarks>
        public void setTriggerDelay(uint value)
        {
            if (featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.RAMAN_FX2)
            {
                logger.error("setTriggerDelay not supported on {0}", featureIdentification.boardType);
                return;
            }

            ushort lsw = (ushort)(value & 0xffff);
            byte msb = (byte)(value >> 16);
            sendCmd(Opcodes.SET_TRIGGER_DELAY, lsw, msb);
        }

        ////////////////////////////////////////////////////////////////////////
        // Complex Getters
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Actually reads integration time from the spectrometer.
        /// </summary>
        /// <returns>integration time in milliseconds</returns>
        public uint getIntegrationTimeMS()
        {
            byte[] buf = getCmd(Opcodes.GET_INTEGRATION_TIME, 3, fullLen: 6);
            if (buf == null)
                return 0;

            int ms =  buf[0]
                   + (buf[1] << 8)
                   + (buf[2] << 16);

            return integrationTimeMS_ = (uint)ms;
        }

        public string getFPGARev()
        {
            byte[] buf = getCmd(Opcodes.GET_FPGA_REV, 7);

            string s = "";
            for (uint i = 0; i < 7; i++)
                s += (char)buf[i];

            return s.TrimEnd();
        }

        public string getFirmwareRev()
        {
            byte[] buf = getCmd(Opcodes.GET_CODE_REVISION, 4);
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

        public CCD_TRIGGER_SOURCE getCCDTriggerSource()
        {
            byte[] buf = getCmd(Opcodes.GET_CCD_TRIGGER_SOURCE, 1);
            if (buf != null)
            {
                switch(buf[0])
                {
                    case 0: return CCD_TRIGGER_SOURCE.USB;
                    case 1: return CCD_TRIGGER_SOURCE.EXTERNAL;
                }
            }
            return CCD_TRIGGER_SOURCE.ERROR;
        }

        public EXTERNAL_TRIGGER_OUTPUT getExternalTriggerOutput()
        {
            byte[] buf = getCmd(Opcodes.GET_EXTERNAL_TRIGGER_OUTPUT, 1);
            if (buf != null)
            {
                switch(buf[0])
                {
                    case 0: return EXTERNAL_TRIGGER_OUTPUT.LASER_MODULATION;
                    case 1: return EXTERNAL_TRIGGER_OUTPUT.INTEGRATION_ACTIVE_PULSE;
                }
            }
            return EXTERNAL_TRIGGER_OUTPUT.ERROR;
        }

        public HORIZ_BINNING getHorizBinning()
        {
            byte[] buf = getCmd(Opcodes.GET_EXTERNAL_TRIGGER_OUTPUT, 1);
            if (buf != null)
            {
                switch (buf[0])
                {
                    case 0: return HORIZ_BINNING.NONE;
                    case 1: return HORIZ_BINNING.TWO_PIXEL;
                    case 2: return HORIZ_BINNING.FOUR_PIXEL;
                }
            }
            return HORIZ_BINNING.ERROR;
        }

        ////////////////////////////////////////////////////////////////////////
        // Trivial Getters
        ////////////////////////////////////////////////////////////////////////

        public ushort getActualFrames()                 { return Unpack.toUshort(getCmd(Opcodes.GET_ACTUAL_FRAMES,              2)); }
        public ushort getCCDTemperature()               { return Unpack.toUshort(getCmd(Opcodes.GET_CCD_TEMP,                   2)); } 
        public float  getCCDGain()                      { return FunkyFloat.toFloat(Unpack.toUshort(getCmd(Opcodes.GET_CCD_GAIN,2)));}
        public short  getCCDOffset()                    { return Unpack.toShort (getCmd(Opcodes.GET_CCD_OFFSET,                 2)); }
        public ushort getCCDSensingThreshold()          { return Unpack.toUshort(getCmd(Opcodes.GET_CCD_SENSING_THRESHOLD,      2)); }
        public bool   getCCDThresholdSensingEnabled()   { return Unpack.toBool  (getCmd(Opcodes.GET_CCD_THRESHOLD_SENSING_MODE, 1)); }
        public bool   getCCDTempEnabled()               { return Unpack.toBool  (getCmd(Opcodes.GET_CCD_TEMP_ENABLE,            1)); }
        public uint   getCCDTriggerDelay()              { return Unpack.toUint  (getCmd(Opcodes.GET_TRIGGER_DELAY,              3, fullLen: 6)); }
        public ushort getDAC()                          { return Unpack.toUshort(getCmd(Opcodes.GET_CCD_TEMP_SETPOINT,          2, 1)); }
        public bool   getInterlockEnabled()             { return Unpack.toBool  (getCmd(Opcodes.GET_INTERLOCK,                  1)); }
        public bool   getLaserEnabled()                 { return Unpack.toBool  (getCmd(Opcodes.GET_LASER,                      1)); }
        public bool   getLaserModulationEnabled()       { return Unpack.toBool  (getCmd(Opcodes.GET_LASER_MOD,                  1)); }
        public UInt64 getLaserModulationDuration()      { return Unpack.toUint64(getCmd(Opcodes.GET_MOD_DURATION,               5)); }
        public UInt64 getLaserModulationPeriod()        { return Unpack.toUint64(getCmd(Opcodes.GET_MOD_PERIOD,                 5)); }
        public UInt64 getLaserModulationPulseDelay()    { return Unpack.toUint64(getCmd(Opcodes.GET_MOD_PULSE_DELAY,            5)); }
        public UInt64 getLaserModulationPulseWidth()    { return Unpack.toUint64(getCmd(Opcodes.GET_LASER_MOD_PULSE_WIDTH,      5)); }
        public bool   getLaserRampingEnabled()          { return Unpack.toBool  (getCmd(Opcodes.GET_LASER_RAMPING_MODE,         1)); }
        public ushort getLaserTemperatureRaw()          { return Unpack.toUshort(getCmd(Opcodes.GET_LASER_TEMP,                 2)); } 
        public byte   getLaserTemperatureSetpoint()     { return Unpack.toByte  (getCmd(Opcodes.GET_LASER_TEMP_SETPOINT,        1, fullLen: 6)); } // MZ: unit? 
        public uint   getLineLength()                   { return Unpack.toUshort(getCmd2(Opcodes.GET_LINE_LENGTH,               2)); }
        public byte   getSelectedLaser()                { return Unpack.toByte  (getCmd(Opcodes.GET_SELECTED_LASER,             1)); }
        public bool   getLaserModulationLinkedToIntegrationTime() { return Unpack.toBool(getCmd(Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME, 1)); }

        // These USB calls duplicate FPGAOptions (for non-FPGA spectrometers?)
        // For variety, and to handle misconfigured spectrometers, return
        // these as ints rather than enums.
        public int  getOptIntTimeRes()        { return Unpack.toInt (getCmd2(Opcodes.OPT_INT_TIME_RES, 1)); }
        public int  getOptDataHdrTab()        { return Unpack.toInt (getCmd2(Opcodes.OPT_DATA_HDR_TAB, 1)); }
        public bool getOptCFSelect()          { return Unpack.toBool(getCmd2(Opcodes.OPT_CF_SELECT, 1)); } 
        public int  getOptLaserType()         { return Unpack.toInt (getCmd2(Opcodes.OPT_LASER, 1)); }
        public int  getOptLaserControl()      { return Unpack.toInt (getCmd2(Opcodes.OPT_LASER_CONTROL, 1)); } 
        public bool getOptAreaScan()          { return Unpack.toBool(getCmd2(Opcodes.OPT_AREA_SCAN, 1)); } 
        public bool getOptActIntTime()        { return Unpack.toBool(getCmd2(Opcodes.OPT_ACT_INT_TIME, 1)); } 
        public bool getOptHorizontalBinning() { return Unpack.toBool(getCmd2(Opcodes.OPT_AREA_SCAN, 1)); }
        
        // TODO: something's buggy with these
        public uint   getActualIntegrationTime()        { return Unpack.toUint(getCmd(Opcodes.GET_ACTUAL_INTEGRATION_TIME, 3, fullLen: 6)); }
        public ushort getCCDTempSetpoint()              { return Unpack.toUshort(getCmd(Opcodes.GET_CCD_TEMP_SETPOINT, 2, wIndex: 0)); }

        ////////////////////////////////////////////////////////////////////////
        // Trivial setters
        ////////////////////////////////////////////////////////////////////////

        public void setCCDGain              (float gain)        { sendCmd(Opcodes.SET_CCD_GAIN,                   FunkyFloat.fromFloat(gain)); } 
        public void setCCDTriggerSource     (ushort source)     { sendCmd(Opcodes.SET_CCD_TRIGGER_SOURCE,         source); } 
        public void setCCDTemperatureEnable (bool flag)         { sendCmd(Opcodes.SET_CCD_TEMP_ENABLE,            (ushort) (flag ? 1 : 0)); } 
        public void setCCDTemperatureSetpoint(ushort word)      { sendCmd(Opcodes.SET_CCD_TEMP_SETPOINT,          word, 0); }
        public void setDAC                  (ushort word)       { sendCmd(Opcodes.SET_DAC,                        word, 1); } // external laser power
        public void setLaserEnable          (bool flag)         { sendCmd(Opcodes.SET_LASER,                      (ushort) (flag ? 1 : 0)); } 
        public void setLaserModulationEnable(bool flag)         { sendCmd(Opcodes.SET_LASER_MOD,                  (ushort) (flag ? 1 : 0)); } 
        public void setLaserRampingEnable   (bool flag)         { sendCmd(Opcodes.SET_LASER_RAMPING_MODE,         (ushort) (flag ? 1 : 0)); } 
        public void setHorizontalBinning    (HORIZ_BINNING mode){ sendCmd(Opcodes.SELECT_HORIZ_BINNING,           (ushort) mode); }
        public void setSelectedLaser        (byte id)           { sendCmd(Opcodes.SELECT_LASER,                   id); }
        public void setCCDOffset            (ushort value)      { sendCmd(Opcodes.SET_CCD_OFFSET,                 value); }
        public void setCCDSensingThreshold  (ushort value)      { sendCmd(Opcodes.SET_CCD_SENSING_THRESHOLD,      value); }
        public void setCCDThresholdSensingEnable(bool flag)     { sendCmd(Opcodes.SET_CCD_THRESHOLD_SENSING_MODE, (ushort)(flag ? 1 : 0)); }
        public void linkLaserModToIntegrationTime(bool flag)    { sendCmd(Opcodes.LINK_LASER_MOD_TO_INTEGRATION_TIME, (ushort) (flag ? 1 : 0)); } 

        #endregion  

        ////////////////////////////////////////////////////////////////////////
        // getSpectrum
        ////////////////////////////////////////////////////////////////////////

        #region getspectrum
        // includes scan averaging, boxcar and dark subtraction
        public double[] getSpectrum()
        {
            lock (acquisitionLock)
            {
                double[] sum = getSpectrumRaw();
                if (sum == null)
                    return null;

                if (scanAveraging_ > 0)
                {
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

                if (boxcarHalfWidth_ > 0)
                    return Util.applyBoxcar(boxcarHalfWidth_, sum);
                else
                    return sum;
            }
        }

        // just the bytes, ma'am
        double[] getSpectrumRaw()
        {
            // STEP ONE: request a spectrum
            if (!sendCmd(Opcodes.ACQUIRE_CCD))
                return null;

            // STEP TWO: wait for acquisition to complete

            // rather than banging at the control endpoint, sleep for most of it
            if (integrationTimeMS_ > 5)
                Thread.Sleep((int)(integrationTimeMS_ - 5));

            if (!blockUntilDataReady())
                return null;

            // STEP THREE: read spectrum
            //
            // NOTE: API recommends reading this in four 512-byte chunks. I got
            //       occasional timeout errors on the last chunk when following
            //       that procedure. Checking Enlighten source code, it seemed
            //       to perform the read in a single 2048-byte block. That seems
            //       to work well here too.

            // note: hardcoded to 16-bit
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

            // STEP FOUR: read status
            // AcquisitionStatus status = getAcquisitionStatus();
            // if (status != null)
            //     if (sum != status.checksum)
            //         logger.error("getSpectrum: sum {0} != checksum {1}", sum, status.checksum);

            return spec;
        }

        // spectrum status is currently unimplemented in firmware
        public AcquisitionStatus getAcquisitionStatus()
        {
            const int timeoutMS = 2;
            byte[] buf = new byte[8];
            try
            {
                int bytesRead = 0;
                ErrorCode err = statusReader.Read(buf, timeoutMS, out bytesRead);
                if (bytesRead >= 7 && err == ErrorCode.Ok)
                {
                    AcquisitionStatus status = new AcquisitionStatus();
                    status.checksum = (ushort)(buf[0] + (buf[1] << 8));
                    status.frame = (ushort)(buf[2] + (buf[3] << 8));
                    status.integTimeMS = (uint)(buf[4] + (buf[5] << 8) + (buf[6] << 16));
                    return status;
                }
                else
                    logger.error("Could not read acquisition status; error = {0}", err);
            }
            catch (Exception ex)
            {
                logger.error("Could not read acquisition status: exception = {0}", ex.Message);
            }
            return null;
        }

        public bool blockUntilDataReady()
        {
            // give it an extra 100ms buffer before we give up
            uint timeoutMS = integrationTimeMS_ + 100;
            DateTime expiration = DateTime.Now.AddMilliseconds(timeoutMS);

            while (DateTime.Now < expiration)
            {
                // poll the spectrometer to see if spectral data is waiting to be read
                byte[] buf = getCmd(Opcodes.POLL_DATA, 4);
                if (buf == null)
                    return false;

                if (buf[0] != 0)
                    return true;
            }
            logger.error("blockUntilDataReady timed-out after {0}ms", timeoutMS);
            return false;
        }
        #endregion
    }
}