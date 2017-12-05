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
    /// For WP spectrometer API, see ENG-0001 "WP Raman USB Interface Specification r1.4"
    ///
    /// NOT rigorously tested for thread-safety. It MAY be thread-safe, 
    /// it WILL be thread-safe, but currently it is not GUARANTEED thread-safe.
    ///
    /// Currently the only supported bus is USB (using LibUsbDotNet). There are
    /// relatively few accesses to the raw USB objects in this implementation, 
    /// so it should be fairly easy to refactor to support Bluetooth, Ethernet 
    /// etc when I get test units.
    ///
    /// Most features are provided as methods, with only a few acquisition-related
    /// features provided as properties. I don't have a strong reason for this,
    /// and may change things. Basically, I wanted to distinguish between 
    /// properties that I knew would be accessed frequently from application code
    /// (e.g. pixels, wavelengths, integrationTime) and should therefore be cached in
    /// the driver, versus settings that should always be communicated 
    /// explicitly with the spectrometer (e.g. laser enable status). Also, it 
    /// seemed efficient to unpack ModelConfig and FPGAOptions into cached 
    /// properties rather than making each into duplicate API calls.
    ///
    /// Nevertheless...it seems more ".NET"-ish to expose most get/set pairs as
    /// properties.  This clay remains soft.
    ///
    /// Trying to avoid making this a "God-class", but it's still a bit heavy.
    /// </remarks>
    public class Spectrometer
    {
        ////////////////////////////////////////////////////////////////////////
        // Constants
        ////////////////////////////////////////////////////////////////////////

        public const byte HOST_TO_DEVICE = 0x40;
        public const byte DEVICE_TO_HOST = 0xc0;

        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        float detectorSetpointDegC = 0;

        UsbRegistry usbRegistry;
        UsbDevice usbDevice;
        UsbEndpointReader spectralReader;
        UsbEndpointReader statusReader;

        Dictionary<Opcodes, byte> cmd = OpcodeHelper.getInstance().getDict();
        HashSet<Opcodes> armInvertedRetvals = OpcodeHelper.getInstance().getArmInvertedRetvals();

        Logger logger = Logger.getInstance();

        object acquisitionLock = new object();
        object commsLock = new object();

        #region properties

        ////////////////////////////////////////////////////////////////////////
        // Public properties
        ////////////////////////////////////////////////////////////////////////

        /// <summary>how many pixels does the spectrometer have (spectrum length)</summary>
        public uint pixels { get; private set; }

        /// <summary>pre-populated array of wavelengths (nm) by pixel, generated from ModelConfig.wavecalCoeffs</summary>
        /// <remarks>see Util.generateWavelengths</remarks>
        public double[] wavelengths { get; private set; }

        /// <summary>pre-populated array of Raman shifts in wavenumber (1/cm) by pixel, generated from wavelengths[] and excitationNM</summary>
        /// <remarks>see Util.wavelengthsToWavenumbers</remarks>
        public double[] wavenumbers { get; private set; }

        /// <summary>spectrometer model</summary>
        public string model;

        /// <summary>spectrometer serial number</summary>
        public string serialNumber;

        /// <summary>metadata inferred from the spectrometer's USB PID</summary>
        public FeatureIdentification featureIdentification;

        /// <summary>set of compilation options used to compile the FPGA firmware in this spectrometer</summary>
        public FPGAOptions fpgaOptions;

        /// <summary>configuration settings stored in the spectrometer's EEPROM</summary>
        public ModelConfig modelConfig;

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
        /// <see cref="getIntegrationTimeMS"/>
        public uint integrationTimeMS
        {
            get { return integrationTimeMS_; }
            set { lock (acquisitionLock) setIntegrationTimeMS(value); }
        }
        uint integrationTimeMS_;

        /// <summary>
        /// How many acquisitions to average together (zero for no averaging)
        /// </summary>
        public uint scanAveraging
        {
            get { return scanAveraging_; }
            set { lock(acquisitionLock) scanAveraging_ = value; }
        }
        uint scanAveraging_ = 1;

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
        uint boxcarHalfWidth_;

        /// <summary>
        /// Perform automatic dark subtraction by setting this property to
        /// an acquired dark spectrum; leave "null" to disable.
        /// </summary>
        public double[] dark
        {
            get { return dark_; }
            set { lock(acquisitionLock) dark_ = value; }
        }
        double[] dark_;
        #endregion

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        #region lifecycle
        internal Spectrometer(UsbRegistry usbReg)
        {
            usbRegistry = usbReg;
            pixels = 0;
        }

        internal bool open()
        {
            if (!usbRegistry.Open(out usbDevice))
            {
                logger.error("Spectrometer: failed to open UsbRegistry");
                return false;
            }

            // derive some values from PID
            featureIdentification = new FeatureIdentification(usbRegistry.Pid);
            if (!featureIdentification.isSupported)
                return false;

            // load EEPROM configuration
            modelConfig = new ModelConfig(this);
            if (!modelConfig.read())
            {
                logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                usbDevice.Close();
                return false;
            }
            model = modelConfig.model;
            serialNumber = modelConfig.serialNumber;

            // see how the FPGA was compiled
            fpgaOptions = new FPGAOptions(this);

            // MustardTree uses 2048-pixel version of the S11510, and all InGaAs are 512
            pixels = (uint) modelConfig.activePixelsHoriz;
            if (pixels > 2048)
            {
                logger.error("Unlikely pixels count found ({0}); defaulting to {1}", 
                    modelConfig.activePixelsHoriz, featureIdentification.defaultPixels);
                pixels = featureIdentification.defaultPixels;
            }

            wavelengths = Util.generateWavelengths(pixels, modelConfig.wavecalCoeffs);
            if (modelConfig.excitationNM > 0)
                wavenumbers = Util.wavelengthsToWavenumbers(modelConfig.excitationNM, wavelengths);

            spectralReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
            statusReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep06);

            // by default, integration time is zero in HW
            setIntegrationTimeMS(modelConfig.minIntegrationTimeMS);

            // MZ: base on A/R/C?
            if (featureIdentification.defaultTECSetpointDegC.HasValue)
                detectorSetpointDegC = featureIdentification.defaultTECSetpointDegC.Value;
            else if (modelConfig.detectorName.Contains("S11511"))
                detectorSetpointDegC = 10;
            else if (modelConfig.detectorName.Contains("S10141"))
                detectorSetpointDegC = -15;
            else if (modelConfig.detectorName.Contains("G9214"))
                detectorSetpointDegC = -15;

            if (hasLaser())
            {
                logger.debug("unlinking laser modulation from integration time");
                linkLaserModToIntegrationTime(false);

                logger.debug("disabling laser modulation");
                setLaserModulationEnable(false);

                logger.debug("disabling laser");
                setLaserEnable(false);
            }

            if (modelConfig.hasCooling)
            {
                // MZ: TEC doesn't do anything unless you give it a temperature first
                logger.debug("setting TEC setpoint to {0} deg C", detectorSetpointDegC);
                setCCDTemperatureSetpointDegC(detectorSetpointDegC);

                logger.debug("enabling detector TEC");
                setCCDTemperatureEnable(true);
            }

            return true;
        }

        public void close()
        {
            if (usbDevice != null)
            {
                if (usbDevice.IsOpen)
                {
                    if (hasLaser())
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
        // Convenience Accessors
        ////////////////////////////////////////////////////////////////////////

        public bool isARM() { return featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.STROKER_ARM; }
        public bool hasLaser()
        {
            return modelConfig.hasLaser 
                && (fpgaOptions.laserType == FPGA_LASER_TYPE.INTERNAL || fpgaOptions.laserType == FPGA_LASER_TYPE.EXTERNAL);
        }

        ////////////////////////////////////////////////////////////////////////
        // Utilities
        ////////////////////////////////////////////////////////////////////////

        // TODO: refactor this into Bus, UsbBus etc

        /// <summary>
        /// Execute a request-response control transaction using the given opcode.
        /// </summary>
        /// <param name="opcode">the opcode of the desired request</param>
        /// <param name="len">the number of needed return bytes</param>
        /// <param name="wIndex">an optional numeric argument used by some opcodes</param>
        /// <param name="fullLen">the actual number of expected return bytes (not all needed)</param>
        /// <remarks>not sure fullLen is actually required...testing</remarks>
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

            bool expectedSuccessResult = true;
            if (isARM())
                expectedSuccessResult = armInvertedRetvals.Contains(opcode);

            // Question: if the device returns 6 bytes on Endpoint 0, but I only
            // need the first so pass byte[1], are the other 5 bytes discarded or
            // queued?
            lock (commsLock)
            {
                if (featureIdentification.usbDelayMS > 0)
                    Thread.Sleep((int) featureIdentification.usbDelayMS);

                if (expectedSuccessResult != usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead) || bytesRead < bytesToRead)
                {
                    logger.error("getCmd: failed to get {0} (0x{1:x2}) with index 0x{2:x4} via DEVICE_TO_HOST ({3} bytes read)",
                        opcode.ToString(), cmd[opcode], wIndex, bytesRead);
                    return null;
                }
            }

            if (logger.debugEnabled())
            {
                string prefix = String.Format("getCmd: {0} (0x{1:x2}) index 0x{2:x4} ->", opcode.ToString(), cmd[opcode], wIndex);
                logger.hexdump(buf, prefix);
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

            bool expectedSuccessResult = true;
            if (isARM())
                expectedSuccessResult = armInvertedRetvals.Contains(opcode) ? false : true;

            lock (commsLock)
            {
                if (featureIdentification.usbDelayMS > 0)
                    Thread.Sleep((int) featureIdentification.usbDelayMS);

                bool result = usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead);
                if (result != expectedSuccessResult || bytesRead < len)
                {
                    logger.error("getCmd2: failed to get SECOND_TIER_COMMAND {0} (0x{1:x4}) via DEVICE_TO_HOST ({2} bytes read, expected {3}, got {4})",
                        opcode.ToString(), cmd[opcode], bytesRead, expectedSuccessResult, result);
                    return null;
                }
            }

            if (logger.debugEnabled())
            {
                string prefix = String.Format("getCmd: {0} (0x{1:x2}) index 0x{2:x4} ->", opcode.ToString(), cmd[opcode], wIndex);
                logger.hexdump(buf, prefix);
            }

            return buf;
        }

        /// <summary>
        /// send a single control transfer command (response not checked)
        /// </summary>
        /// <param name="opcode">the desired command</param>
        /// <param name="wValue">an optional secondary argument used by some commands</param>
        /// <param name="wIndex">an optional tertiary argument used by some commands</param>
        /// <param name="buf">a data buffer used by some commands</param>
        /// <returns>true on success, false on error</returns>
        /// <todo>should support return code checking...most cmd opcodes return a success/failure byte</todo>
        internal bool sendCmd(Opcodes opcode, ushort wValue = 0, ushort wIndex = 0, byte[] buf = null)
        {
            ushort wLength = (ushort)((buf == null) ? 0 : buf.Length);

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                HOST_TO_DEVICE, // bRequestType
                cmd[opcode],    // bRequest
                wValue,         // wValue
                wIndex,         // wIndex
                wLength);       // wLength

            bool expectedSuccessResult = true;
            if (isARM())
                expectedSuccessResult = armInvertedRetvals.Contains(opcode);

            lock (commsLock)
            {
                if (featureIdentification.usbDelayMS > 0)
                    Thread.Sleep((int) featureIdentification.usbDelayMS);

                if (buf != null)
                    logger.hexdump(buf, String.Format("sendCmd({0}, {1}, {2}, {3}): ", opcode, wValue, wIndex, wLength));
                if (expectedSuccessResult != usbDevice.ControlTransfer(ref setupPacket, buf, wLength, out int bytesWritten))
                {
                    logger.error("sendCmd: failed to send {0} (0x{1:x2}) (wValue 0x{2:x4}, wIndex 0x{3:x4}, wLength 0x{4:x4})",
                        opcode.ToString(), cmd[opcode], wValue, wIndex, wLength);
                    return false;
                }
            }
            return true;
        }

        #region spec_comms

        ////////////////////////////////////////////////////////////////////////
        // Settors
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// set the acquisition time in milliseconds
        /// </summary>
        /// <param name="ms">integration time in milliseconds</param>        
        /// <remarks>
        /// Does not currently support microsecond resolution.
        /// Method is private because attribute is public.
        /// </remarks>
        void setIntegrationTimeMS(uint ms)
        {
            if (ms < modelConfig.minIntegrationTimeMS)
            {
                logger.error("rounded integration time {0}ms up to min {1}ms", ms, modelConfig.minIntegrationTimeMS);
                ms = modelConfig.minIntegrationTimeMS;
            }
            else if (ms > modelConfig.maxIntegrationTimeMS)
            {
                logger.error("rounded integration time {0}ms down to max {1}ms", ms, modelConfig.maxIntegrationTimeMS);
                ms = modelConfig.maxIntegrationTimeMS;
            }

            ushort LSW = (ushort) (ms % 65536);
            ushort MSW = (ushort) (ms / 65536);

            // cache for performance 
            sendCmd(Opcodes.SET_INTEGRATION_TIME, LSW, MSW);

            // assume success
            integrationTimeMS_ = ms;
        }

        public bool setExternalTriggerOutput(EXTERNAL_TRIGGER_OUTPUT value)
        {
            if (value == EXTERNAL_TRIGGER_OUTPUT.ERROR)
                return false;
            return sendCmd(Opcodes.SET_EXTERNAL_TRIGGER_OUTPUT, (ushort) value);
        }

        /// <summary>
        /// Set the trigger delay on supported models.
        /// </summary>
        /// <param name="value">24-bit value (0.5us)</param>
        /// <returns>true on success</returns>
        /// <remarks>
        /// The value is in 0.5 microseconds, and supports a 24-bit unsigned value,
        /// so the total range is 2^23 microseconds (about 8.3 sec).
        /// </remarks>
        public bool setTriggerDelay(uint value)
        {
            if (featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.RAMAN_FX2)
            {
                logger.debug("trigger delay not supported on {0}", featureIdentification.boardType);
                return false;
            }

            ushort lsw = (ushort)(value & 0xffff);
            byte msb = (byte)(value >> 16);
            return sendCmd(Opcodes.SET_TRIGGER_DELAY, lsw, msb);
        }

        public bool setLaserRampingEnable(bool flag)
        {
            if (featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.RAMAN_FX2)
            {
                logger.debug("laser ramping not supported on {0}", featureIdentification.boardType);
                return false;
            }
            return sendCmd(Opcodes.SET_LASER_RAMPING_MODE, (ushort) (flag ? 1 : 0));
        } 

        public bool setHorizontalBinning(HORIZ_BINNING mode)
        {
            if (featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.RAMAN_FX2)
            {
                logger.debug("horizontal binning not supported on {0}", featureIdentification.boardType);
                return false;
            }
            return sendCmd(Opcodes.SELECT_HORIZ_BINNING, (ushort) mode);
        }

        /// <summary>
        /// Set the detector's thermoelectric cooler (TEC) to the desired setpoint in degrees Celsius.
        /// </summary>
        /// <param name="degC">Desired temperature in Celsius.</param>
        /// <returns>true on success</returns>
        public bool setCCDTemperatureSetpointDegC(float degC)
        {
            if (degC < modelConfig.detectorTempMin)
            {
                logger.info("WARNING: rounding detector setpoint {0} deg C up to configured min {1}", degC, modelConfig.detectorTempMin);
                degC = modelConfig.detectorTempMin;
            }
            else if (degC > modelConfig.detectorTempMax)
            {
                logger.info("WARNING: rounding detector setpoint {0} deg C down to configured max {1}", degC, modelConfig.detectorTempMax);
                degC = modelConfig.detectorTempMax;
            }

            float dac = modelConfig.degCToDACCoeffs[0]
                      + modelConfig.degCToDACCoeffs[1] * degC
                      + modelConfig.degCToDACCoeffs[2] * degC * degC;
            ushort word = (ushort)dac;

            logger.debug("setting CCD TEC setpoint to {0:f2} deg C (DAC 0x{1:x4})", degC, word);

            return sendCmd(Opcodes.SET_CCD_TEMP_SETPOINT, word, 0);
        }

        public bool setDFUMode(bool flag)
        {
            if (!flag)
            {
                logger.error("Don't know how to unset / exit DFU mode (power-cycle spectrometer)");
                return false;
            }

            if (!isARM())
            {
                logger.error("This command is believed only applicable to ARM-based spectrometers (not {0})", 
                    featureIdentification.boardType);
                return false;
            }

            logger.info("Setting DFU mode");
            return sendCmd(Opcodes.SET_DFU_MODE);
        }

        public bool setHighGainModeEnabled(bool flag)
        {
            if (featureIdentification.boardType != FeatureIdentification.BOARD_TYPES.INGAAS_FX2)
            {
                logger.debug("high gain mode not supported on {0}", featureIdentification.boardType);
                return false;
            }
            return sendCmd(Opcodes.SET_CF_SELECT, (ushort) (flag ? 1 : 0));
        }

        public bool setCCDGain              (float gain)        { return sendCmd(Opcodes.SET_CCD_GAIN,                   FunkyFloat.fromFloat(gain)); } 
        public bool setCCDTriggerSource     (ushort source)     { return sendCmd(Opcodes.SET_CCD_TRIGGER_SOURCE,         source); } 
        public bool setCCDTemperatureEnable (bool flag)         { return sendCmd(Opcodes.SET_CCD_TEC_ENABLE,             (ushort) (flag ? 1 : 0)); } 
        public bool setDAC                  (ushort word)       { return sendCmd(Opcodes.SET_DAC,                        word, 1); } // external laser power
        public bool setCCDOffset            (ushort value)      { return sendCmd(Opcodes.SET_CCD_OFFSET,                 value); }
        public bool setCCDSensingThreshold  (ushort value)      { return sendCmd(Opcodes.SET_CCD_SENSING_THRESHOLD,      value); }
        public bool setCCDThresholdSensingEnable(bool flag)     { return sendCmd(Opcodes.SET_CCD_THRESHOLD_SENSING_MODE, (ushort)(flag ? 1 : 0)); }

        ////////////////////////////////////////////////////////////////////////
        // Getters
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Actually reads integration time from the spectrometer.
        /// </summary>
        /// <returns>integration time in milliseconds</returns>
        public uint getIntegrationTimeMS()
        {
            byte[] buf = getCmd(Opcodes.GET_INTEGRATION_TIME, 3, fullLen:6); // fullLen: 6?
            if (buf == null)
                return 0;
            return integrationTimeMS_ = Unpack.toUint(buf);
        }

        public string getFPGARev()
        {
            byte[] buf = getCmd(Opcodes.GET_FPGA_REV, 7);

            if (buf == null)
                return "UNKNOWN";

            string s = "";
            for (uint i = 0; i < 7; i++)
                s += (char)buf[i];

            return s.TrimEnd();
        }

        public string getFirmwareRev()
        {
            // purportedly non-FID devices return 2 bytes, but we're not supporting those
            byte[] buf = getCmd(Opcodes.GET_CODE_REVISION, 4);
            if (buf == null)
                return "ERROR";

            // iterate backwards (MSB to LSB)
            string s = "";
            for (int i = 3; i >= 0; i--)
            {
                s += String.Format("{0}", buf[i]);
                if (i > 0)
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
            if (featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.RAMAN_FX2)
            {
                logger.debug("horizontal binning not supported on {0}", featureIdentification.boardType);
                return HORIZ_BINNING.NONE;
            }

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

        /// <summary>
        /// Return integration time + clock-out time (and laser pulse time if externally triggered).
        /// </summary>
        /// <remarks>buggy? still testing</remarks>
        /// <returns>actual integration time in microseconds (zero on error)</returns>
        public uint getActualIntegrationTimeUS()
        {
            uint value = Unpack.toUint(getCmd(Opcodes.GET_ACTUAL_INTEGRATION_TIME, 6));
            return (value == 0xffffff) ? 0 : value;
        }

        public bool getLaserRampingEnabled()
        {
            if (featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.RAMAN_FX2 ||
                featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.INGAAS_FX2)
            {
                logger.debug("laser ramping not supported on {0}", featureIdentification.boardType);
                return false;
            }
            return Unpack.toBool(getCmd(Opcodes.GET_LASER_RAMPING_MODE, 1));
        }

        public bool getHighGainModeEnabled()
        {
            if (featureIdentification.boardType != FeatureIdentification.BOARD_TYPES.INGAAS_FX2)
            {
                logger.debug("high gain mode not supported on {0}", featureIdentification.boardType);
                return false;
            }
            return Unpack.toBool(getCmd(Opcodes.GET_CF_SELECT, 1));
        }

        public uint getCCDTriggerDelay()
        { 
            if (featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.RAMAN_FX2)
            {
                logger.debug("trigger delay not supported on {0}", featureIdentification.boardType);
                return 0;
            }
            return Unpack.toUint(getCmd(Opcodes.GET_TRIGGER_DELAY, 3)); // fullLen: 6?
        } 

        public ushort getCCDTemperatureRaw()
        {
            return swapBytes(Unpack.toUshort(getCmd(Opcodes.GET_CCD_TEMP, 2)));
            // return Unpack.toUshort(getCmd(Opcodes.GET_CCD_TEMP, 2));
        }

        /// <summary>
        /// Although most values read from the spectrometer are little-endian by
        /// design, a couple big-endians slipped in there.
        /// </summary>
        /// <param name="raw">input value in one endian</param>
        /// <returns>same value with the bytes reversed</returns>
        ushort swapBytes(ushort raw)
        {
            byte lsb = (byte)(raw & 0xff);
            byte msb = (byte)((raw >> 8) & 0xff);
            return (ushort) ((lsb << 8) | msb);
        }

        /// <summary>
        /// You generally would not want to use this function (it's better to use
        /// the EEPROM coefficients), but if your EEPROM is blown this can give 
        /// you a reasonable approximation.
        /// </summary>
        /// <param name="raw">12-bit ADC value (ensure it was read as big-endian, or swap with swapBytes())</param>
        /// <remarks>
        /// Borrowed from Enlighten's fid_hardware.get_ccd_temperature(). 
        ///
        /// I *think* that the laser version would be this:
        /// 
        /// Thermistor_Voltage = (raw/4096)*2.468;
        /// Thermistor_Resistance = Thermistor_Voltage/((2.468-Thermistor_Voltage)/21450));
        /// Temp_in_C = 3977/(log(Thermistor_Resistance/10000) + 3977/(25+273) – 273;
        /// </remarks>
        /// <returns></returns>
        float adcToDegC(ushort raw)
        {
            // Scale to a voltage level from 0 to 1.5VDC
            double vdc = 1.5 * raw / 4096.0;

            // Convert to resistance
            double resistance = 10000 * vdc / (2 - vdc);

            // Find the log of the resistance with a 10kOHM resistor
            double logVal = Math.Log(resistance / 10000);
            double insideMain = logVal + (3977.0 / (25 + 273.0));
            return (float)((3977.0 / insideMain) - 273.0);
        }

        public float getCCDTemperatureDegC()
        {
            ushort raw = getCCDTemperatureRaw();
            float degC = modelConfig.adcToDegCCoeffs[0]
                       + modelConfig.adcToDegCCoeffs[1] * raw
                       + modelConfig.adcToDegCCoeffs[2] * raw * raw;
            logger.debug("getCCDTemperatureDegC: raw 0x{0:x4}, coeff {1:f2}, {2:f2}, {3:f2} = {4:f2}",
                    raw,
                    modelConfig.adcToDegCCoeffs[0],
                    modelConfig.adcToDegCCoeffs[1],
                    modelConfig.adcToDegCCoeffs[2],
                    degC);
            return degC; 
        }

        public byte getLaserTemperatureSetpoint()
        {
            if (featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.RAMAN_FX2)
            {
                logger.debug("laser setpoint not readable on {0}", featureIdentification.boardType);
                return 0;
            }
            return Unpack.toByte(getCmd(Opcodes.GET_LASER_TEMP_SETPOINT, 1)); // fullLen: 6? unit?
        } 

        public ushort getActualFrames()                 { return Unpack.toUshort(getCmd(Opcodes.GET_ACTUAL_FRAMES,              2)); }
        public float  getCCDGain()                      { return FunkyFloat.toFloat(Unpack.toUshort(getCmd(Opcodes.GET_CCD_GAIN,2)));}
        public short  getCCDOffset()                    { return Unpack.toShort (getCmd(Opcodes.GET_CCD_OFFSET,                 2)); }
        public ushort getCCDSensingThreshold()          { return Unpack.toUshort(getCmd(Opcodes.GET_CCD_SENSING_THRESHOLD,      2)); }
        public bool   getCCDThresholdSensingEnabled()   { return Unpack.toBool  (getCmd(Opcodes.GET_CCD_THRESHOLD_SENSING_MODE, 1)); }
        public bool   getCCDTempEnabled()               { return Unpack.toBool  (getCmd(Opcodes.GET_CCD_TEMP_ENABLE,            1)); }
        public ushort getDAC()                          { return Unpack.toUshort(getCmd(Opcodes.GET_CCD_TEMP_SETPOINT,          2, 1)); }
        public bool   getInterlockEnabled()             { return Unpack.toBool  (getCmd(Opcodes.GET_INTERLOCK,                  1)); }
        public bool   getLaserEnabled()                 { return Unpack.toBool  (getCmd(Opcodes.GET_LASER_ENABLED,              1)); }
        public bool   getLaserModulationEnabled()       { return Unpack.toBool  (getCmd(Opcodes.GET_LASER_MOD_ENABLED,          1)); }
        public UInt64 getLaserModulationDuration()      { return Unpack.toUint64(getCmd(Opcodes.GET_LASER_MOD_DURATION,         5)); }
        public UInt64 getLaserModulationPeriod()        { return Unpack.toUint64(getCmd(Opcodes.GET_MOD_PERIOD,                 5)); }
        public UInt64 getLaserModulationPulseDelay()    { return Unpack.toUint64(getCmd(Opcodes.GET_MOD_PULSE_DELAY,            5)); }
        public UInt64 getLaserModulationPulseWidth()    { return Unpack.toUint64(getCmd(Opcodes.GET_LASER_MOD_PULSE_WIDTH,      5)); }
        public uint   getLineLength()                   { return Unpack.toUshort(getCmd2(Opcodes.GET_LINE_LENGTH,               2)); }
        public byte   getSelectedLaser()                { return Unpack.toByte  (getCmd(Opcodes.GET_SELECTED_LASER,             1)); } 
        public bool   getLaserModulationLinkedToIntegrationTime() { return Unpack.toBool(getCmd(Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME, 1)); }

        // These USB opcodes seem to duplicate the parameters of FPGAOptions (perhaps for non-FPGA spectrometers?)
        public bool   getOptCFSelect()                  { return Unpack.toBool(getCmd2(Opcodes.OPT_CF_SELECT,                   1)); } 
        public bool   getOptAreaScan()                  { return Unpack.toBool(getCmd2(Opcodes.OPT_AREA_SCAN,                   1)); } 
        public bool   getOptActIntTime()                { return Unpack.toBool(getCmd2(Opcodes.OPT_ACT_INT_TIME,                1)); } 
        public bool   getOptHorizontalBinning()         { return Unpack.toBool(getCmd2(Opcodes.OPT_AREA_SCAN,                   1)); }
        public FPGA_INTEG_TIME_RES  getOptIntTimeRes()  { return fpgaOptions.parseResolution  (Unpack.toInt(getCmd2(Opcodes.OPT_INT_TIME_RES, 1))); }
        public FPGA_DATA_HEADER     getOptDataHdrTab()  { return fpgaOptions.parseDataHeader  (Unpack.toInt(getCmd2(Opcodes.OPT_DATA_HDR_TAB, 1))); }
        public FPGA_LASER_TYPE      getOptLaserType()   { return fpgaOptions.parseLaserType   (Unpack.toInt(getCmd2(Opcodes.OPT_LASER, 1))); }
        public FPGA_LASER_CONTROL   getOptLaserControl(){ return fpgaOptions.parseLaserControl(Unpack.toInt(getCmd2(Opcodes.OPT_LASER_CONTROL, 1))); } 

        public ushort getLaserTemperatureRaw()
        {
            return swapBytes(Unpack.toUshort(getCmd(Opcodes.GET_LASER_TEMP, 2)));
        } 

        /// <summary>
        /// convert the raw laser temperature reading into degrees centigrade
        /// </summary>
        /// <returns>laser temperature in &deg;C</returns>
        /// <remarks>
        /// Note that the adcToDegCCoeffs are NOT used in this method; those
        /// coefficients ONLY apply to the detector.  At this time, all laser
        /// temperature math is hardcoded (confirmed with Jason 22-Nov-2017).
        /// </remarks>
        public float getLaserTemperatureDegC()
        {
            double raw = getLaserTemperatureRaw();
            double voltage    = 2.5 * raw / 4096;
            double resistance = 21450.0 * voltage / (2.5 - voltage);
            double logVal     = Math.Log(resistance / 10000);
            double insideMain = logVal + 3977.0 / (25 + 273.0);
            double degC = 3977.0 / insideMain - 273.0;
            
            logger.debug("getLaserTemperatureDegC: {0:f2} deg C (raw 0x{1:x4})", degC, raw);

            return (float) degC;
        }

        // TODO: something's buggy with this?
        // MZ: not sure we can return this in deg C (our adctoDegC coeffs are for the thermistor, not the TEC...I THINK)
        public ushort getDetectorSetpointRaw()
        {
            return Unpack.toUshort(getCmd(Opcodes.GET_CCD_TEMP_SETPOINT, 2, wIndex: 0));
        }

        /// <summary>
        /// When using external triggering, perform multiple acquisitions on a single inbound trigger event.
        /// </summary>
        /// <param name="flag">whether to acquire multiple spectra per trigger</param>
        public void setContinuousCCDEnable(bool flag)
        {
            sendCmd(Opcodes.VR_SET_CONTINUOUS_CCD, (ushort)(flag ? 1 : 0));
        }

        /// <summary>
        /// Determine whether continuous acquisition is enabled
        /// </summary>
        /// <returns>whether continuous acquisition is enabled</returns>
        public bool getContinuousCCDEnable()
        {
            return Unpack.toBool(getCmd(Opcodes.VR_SET_CONTINUOUS_CCD, 1));
        }

        /// <summary>
        /// When using "continous CCD" acquisitions with external triggers, how many spectra to acquire per trigger event.
        /// </summary>
        /// <param name="n">how many spectra to acquire</param>
        public void setContinuousCCDFrames(byte n)
        {
            sendCmd(Opcodes.VR_SET_NUM_FRAMES, n);
        }

        /// <summary>
        /// When using "continuous CCD" acquisitions with external triggering, how many spectra are being acquired per trigger event.
        /// </summary>
        /// <returns>number of spectra</returns>
        public byte getContinuousCCDFrames()
        {
            return Unpack.toByte(getCmd(Opcodes.VR_GET_NUM_FRAMES, 1));
        }

        #endregion  

        ////////////////////////////////////////////////////////////////////////
        // laser
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set laser power to the specified percentage.
        /// </summary>
        /// <param name="perc">value from 0 to 1.0</param>
        /// <returns>true on success</returns>
        /// <remarks>
        /// The "fake" buffers being send with the commands relate to a legacy
        /// bug in which some firmware mistakenly checked the "payload length"
        /// rather than "wValue" for their key parameter.  It would be good to
        /// document which firmware versions exhibit this behavior, so we do not
        /// propogate an inefficient and unnecessary patch to newer models which
        /// do not require it.
        /// </remarks>
        public bool setLaserPowerPercentage(float perc)
        {
            if (perc < 0 || perc > 1)
            {
                logger.error("invalid laser power percentage (should be in range (0, 1)): {0}", perc);
                return false;
            }

            ushort century = (ushort) Math.Round(perc * 100);

            // Turn off modulation at full laser power, exit
            if (century >= 100)
            {
                logger.debug("Turning off laser modulation (full power)");
                return setLaserModulationEnable(false);
            }

            // Change the pulse period to 100us
            byte[] fake = new byte[100];
            if (!sendCmd(Opcodes.SET_MOD_PERIOD, 100, buf: fake))
            {
                logger.error("Hardware Failure to send laser mod. pulse period");
                return false;
            }

            // Set the pulse width to the 0-100 percentage of power (in microsec)
            fake = new byte[century];
            if (!sendCmd(Opcodes.SET_LASER_MOD_PULSE_WIDTH, century, buf: fake))
            {
                logger.error("Hardware Failure to send pulse width");
                return false;
            }

            // Enable modulation
            fake = new byte[8];
            if (!sendCmd(Opcodes.SET_LASER_MOD_ENABLED, 1, buf: fake))
            {
                logger.error("Hardware Failure to send laser modulation");
                return false;
            }

            logger.debug("Laser power set to: {0}%", century);
            return true;
        }

        /// <summary>
        /// Sets the laser modulation duration to the given 40-bit value (microseconds).
        /// </summary>
        /// <param name="us">duration in microseconds</param>
        /// <returns>true on success</returns>
        public bool setLaserModulationDurationMicrosec(UInt64 us)
        {
            try
            {
                UInt40 value = new UInt40(us);
                return sendCmd(Opcodes.SET_LASER_MOD_DURATION, value.LSW, value.MidW, value.buf);
            }
            catch(Exception ex)
            {
                logger.error("WasatchNET.setLaserModulationDuration: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets the laser modulation duration to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit duration</param>
        /// <returns>true on success</returns>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        public bool setLaserModulationPulseWidth(UInt64 value)
        {
            try
            {
                UInt40 val = new UInt40(value);
                return sendCmd(Opcodes.SET_LASER_MOD_PULSE_WIDTH, val.LSW, val.MidW, val.buf);
            }
            catch(Exception ex)
            {
                logger.error("WasatchNET.setLaserModulationPulseWidth: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets the laser modulation period to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit period</param>
        /// <returns>true on success</returns>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        public bool setLaserModulationPeriod(UInt64 value)
        {
            try
            {
                UInt40 val = new UInt40(value);
                return sendCmd(Opcodes.SET_MOD_PERIOD, val.LSW, val.MidW, val.buf);
            }
            catch(Exception ex)
            {
                logger.error("WasatchNET.setLaserModulationPeriod: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets the laser modulation pulse delay to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit period</param>
        /// <returns>true on success</returns>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        public bool setLaserModulationPulseDelay(UInt64 value)
        {
            try
            {
                UInt40 val = new UInt40(value);
                return sendCmd(Opcodes.SET_MOD_PULSE_DELAY, val.LSW, val.MidW, val.buf);
            }
            catch(Exception ex)
            {
                logger.error("WasatchNET.setLaserModulationPulseDelay: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// This is probably inadvisable.  Unclear when the user would want to do this.
        /// </summary>
        /// <param name="value"></param>
        public bool setLaserTemperatureSetpoint(byte value)
        {
            const byte MAX = 127;
            if (value > MAX)
            {
                logger.error("WasatchNET.setLaserTemperatureSetpoint: value {0} exceeded max {1}", value, MAX);
                return false;
            }
            return sendCmd(Opcodes.SET_LASER_TEMP_SETPOINT, value);
        }

        public bool setLaserEnable          (bool flag)         { return sendCmd(Opcodes.SET_LASER_ENABLED,              (ushort) (flag ? 1 : 0)); } 
        public bool setLaserModulationEnable(bool flag)         { return sendCmd(Opcodes.SET_LASER_MOD_ENABLED,          (ushort) (flag ? 1 : 0)); } 
        public bool setSelectedLaser        (byte id)           { return sendCmd(Opcodes.SELECT_LASER,                   id); }
        public bool linkLaserModToIntegrationTime(bool flag)    { return sendCmd(Opcodes.LINK_LASER_MOD_TO_INTEGRATION_TIME, (ushort) (flag ? 1 : 0)); } 

        ////////////////////////////////////////////////////////////////////////
        // getSpectrum
        ////////////////////////////////////////////////////////////////////////

        #region getspectrum
        
        /// <summary>
        /// Take a single complete spectrum, including any configured scan 
        /// averaging, boxcar and dark subtraction.
        /// </summary>
        /// <returns>The acquired spectrum as an array of doubles</returns>
        public double[] getSpectrum()
        {
            lock (acquisitionLock)
            {
                double[] sum = getSpectrumRaw();
                if (sum == null)
                    return null;

                if (scanAveraging_ > 1)
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
            logger.debug("requesting spectrum");
            if (!sendCmd(Opcodes.ACQUIRE_CCD))
                return null;

            // STEP TWO: wait for acquisition to complete

            // rather than banging at the control endpoint, sleep for most of it
            if (integrationTimeMS_ > 5)
            {
                int ms = (int)integrationTimeMS_ - 5;
                // logger.debug("sleeping {0}ms", ms);
                Thread.Sleep(ms);
            }

            if (!isARM())
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
            byte[] response = new byte[featureIdentification.spectraBlockSize]; 

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
                    // logger.debug("reading {0} bytes of spectrum", response.Length);
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
                        logger.debug("getSpectrum: read different number of bytes than expected (pixel {0}, bytesRead {1})", pixel, bytesRead);

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
            // try
            // {
            //      AcquisitionStatus status = new AcquisitionStatus(statusReader);
            //      if (sum != status.checksum)
            //         logger.error("getSpectrum: sum {0} != checksum {1}", sum, status.checksum);
            // }
            // catch (Exception ex)
            // {
            //      logger.error(ex.Message);
            // }

            return spec;
        }

        public bool blockUntilDataReady()
        {
            logger.debug("polling until data ready");

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
