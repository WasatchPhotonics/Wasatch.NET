using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
    /// </remarks>
    [ComVisible(true)]
    [Guid("06DF0AB6-741E-43D8-92EF-E14CB74070D7")]
    [ProgId("WasatchNET.Spectrometer")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Spectrometer : ISpectrometer
    {
        ////////////////////////////////////////////////////////////////////////
        // Constants
        ////////////////////////////////////////////////////////////////////////

        public const byte HOST_TO_DEVICE = 0x40;
        public const byte DEVICE_TO_HOST = 0xc0;
        public const float UNINITIALIZED_TEMPERATURE_DEG_C = -999;
        public const int LEGACY_VERTICAL_PIXELS = 70;
        public const ushort SPECTRUM_START_MARKER = 0xffff;

        ////////////////////////////////////////////////////////////////////////
        // data types
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When setting laser power as a percentage (see setLaserPowerPercentage), 
        /// this enum determines the possible resolution (granularity) of the 
        /// selectable power.
        /// </summary>
        /// <remarks>
        /// - 100 = 1% resolution (74.4% would round down to 74% power), using pulse period of 100us
        /// - 1000 = 0.1% resolution (74.4% would yield 74.4% power), use pulse period of 1000us
        /// - MANUAL = "use whatever laserModulationPulseWidth has been set by the caller"
        /// </remarks>
        public enum LaserPowerResolution { LASER_POWER_RESOLUTION_100, LASER_POWER_RESOLUTION_1000, LASER_POWER_RESOLUTION_MANUAL }

        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        UsbRegistry usbRegistry;
        UsbDevice usbDevice;
        IUsbDevice wholeUsbDevice;

        // technically these are Read Endpoints 2 and 6, but keeping naming
        // consistent with Wasatch.PY
        UsbEndpointReader spectralReader82;
        UsbEndpointReader spectralReader86;
        bool usingDualEndpoints;

        internal Dictionary<Opcodes, byte> cmd = OpcodeHelper.getInstance().getDict();
        HashSet<Opcodes> armInvertedRetvals = OpcodeHelper.getInstance().getArmInvertedRetvals();

        protected Logger logger = Logger.getInstance();

        protected object adcLock = new object();
        protected object acquisitionLock = new object();
        object commsLock = new object();
        DateTime lastUsbTimestamp = DateTime.Now;
        internal bool shuttingDown = false;

        /// <summary>
        /// Whether to synchronize all spectral reads with a class-level (static) 
        /// mutex.  
        /// </summary>
        ///
        /// <remarks>
        /// This doesn't include the sending of ACQUIRE software triggers, just 
        /// the bulk readouts over Ep2 and Ep6.  As USB is fundamentally serial,
        /// this probably doesn't change behavior much, but it will ensure whole
        /// whole spectra are transferred atomically, even when spanning endpoints.
        /// 
        /// This is a developmental test feature, and not intended for production
        /// applications.
        /// </remarks>
        public bool useReadoutMutex { get; set; }
        static Mutex readoutMutex = new Mutex();

        List<UsbEndpointReader> endpoints = new List<UsbEndpointReader>();
        int pixelsPerEndpoint = 0;
        ulong throwawaySum = 0;

        ////////////////////////////////////////////////////////////////////////
        // Convenience lookups
        ////////////////////////////////////////////////////////////////////////

        /// <summary>how many pixels does the spectrometer have (spectrum length)</summary>
        public uint pixels { get; protected set; }

        /// <summary>pre-populated array of wavelengths (nm) by pixel, generated from eeprom.wavecalCoeffs</summary>
        /// <remarks>see Util.generateWavelengths</remarks>
        public double[] wavelengths { get; protected set; }

        /// <summary>pre-populated array of Raman shifts in wavenumber (1/cm) by pixel, generated from wavelengths[] and excitationNM</summary>
        /// <remarks>see Util.wavelengthsToWavenumbers</remarks>
        public double[] wavenumbers { get; protected set; }

        /// <summary>
        /// convenience accesor for pixel axis (lazy-loaded), for parallelism
        /// with wavelengths and wavenumbers
        /// </summary>
        public double[] pixelAxis
        {
            get
            {
                if (_pixelAxis != null)
                    return _pixelAxis;
                _pixelAxis = new double[pixels];
                for (int i = 0; i < pixels; i++)
                    _pixelAxis[i] = i;
                return _pixelAxis;
            }
        }
        double[] _pixelAxis = null;

        /// <summary>
        /// Useful if you lost the results of getSpectrum, or if you want to 
        /// peek into ongoing multi-acquisition tasks like scan averaging or 
        /// optimization.
        /// </summary>
        public double[] lastSpectrum { get; protected set; }

        /// <summary>
        /// Whether the spectrometer uses Serial Peripheral Interface
        /// (as opposed to USB for instance).
        /// </summary>
        public bool isSPI { get; protected set; } = false;

        /// <summary>
        /// Stroker is a legacy board firmware with older PID (not 0x1000, 0x2000 
        /// or 0x4000), doesn't conform to Feature Identification Device (FID) 
        /// Protocol, and lacking an EEPROM.
        /// </summary>
        public bool isStroker { get; protected set; } = false;

        /// <summary>
        /// Optical Coherence Tomography (OCT) spectrometers differ from "standard"
        /// spectrometers in several respects, such as producing both 2D and 3D imagery.
        /// </summary>
        public bool isOCT { get; protected set; } = false;

        /// <summary>
        /// Some spectrometers send a start-of-frame marker in the first pixel of
        /// the spectrum.
        /// </summary>
        public bool hasMarker
        {
            get
            {
                // if we decide to keep this, change to EEPROM.featureMask.hasFraming
                return _hasMarker || eeprom.model == "WPX-8CHANNEL";
            }
            set
            {
                _hasMarker = value;
            }
        }
        bool _hasMarker;

        /// <summary>spectrometer serial number</summary>
        public virtual string serialNumber
        {
            get { return eeprom.serialNumber; }
        }

        /// <summary>spectrometer model</summary>
        public string model
        {
            get { return eeprom.model; }
        }

        /// <summary>couples serial number with channel position</summary>
        public string id
        {
            get
            {
                if (multiChannelPosition > -1)
                    return $"{serialNumber} (pos {multiChannelPosition})";
                else
                    return serialNumber;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Spectrometer Components
        ////////////////////////////////////////////////////////////////////////

        /// <summary>metadata inferred from the spectrometer's USB PID</summary>
        public FeatureIdentification featureIdentification { get; set; }

        /// <summary>set of compilation options used to compile the FPGA firmware in this spectrometer</summary>
        public FPGAOptions fpgaOptions { get; private set; }

        /// <summary>configuration settings stored in the spectrometer's EEPROM</summary>
        public EEPROM eeprom { get; protected set; }

        ////////////////////////////////////////////////////////////////////////
        // internal driver attributes (no direct corresponding HW component)
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// If there is an error reading one of the bulk endpoints, pause this 
        /// many milliseconds in hopes of the bus resetting itself (does not
        /// automatically retry).
        /// </summary>
        public int delayAfterBulkEndpointErrorMS = 100;

        /// <summary>
        /// For spectrometer firmware providing "start of spectrum markers", 
        /// provides a count of how many INCORRECT markers were found in the MOST
        /// RECENT spectrum.
        /// </summary>
        public int shiftedMarkerCount { get; private set; } = 0;

        /// <summary>
        /// Whether the driver should automatically perform a throwaway ADC read
        /// when changing the selected ADC.  (defaults true)
        /// </summary>
        bool throwawayADCRead { get; set; } = true;

        /// <summary>
        /// Whether the driver should automatically generate a throwaway 
        /// "stability" measurement after changing integration time.
        /// (defaults false)
        /// </summary>
        ///
        /// <todo>
        /// Change such that only used if the trigger source is internal (SW-triggered)
        /// and autoTrigger is enabled (both the default).
        /// </todo>
        public bool throwawayAfterIntegrationTime { get; set; }

        /// <summary>
        /// Whether the driver should automatically send a software trigger on
        /// calls to getSpectrum() when triggerSource is set to INTERNAL.
        /// (defaults true)
        /// </summary>
        ///
        /// <remarks>
        /// This is provided in case the caller wishes to call sendSWTrigger()
        /// explicitly, for instance to send software triggers to a series of
        /// devices at once, before beginning reads on any of them.
        /// </remarks>
        public bool autoTrigger 
        {
            get => _autoTrigger;
            set
            {
                logger.debug($"Spectrometer.autoTrigger -> {value}");
                _autoTrigger = value;
            }
        }
        bool _autoTrigger = true;

        /// <summary>
        /// Whether the "scanAveraging" property should automatically configure
        /// continuousAcquisitionEnable and continuousFrames.
        /// (default false)
        /// </summary>
        ///
        /// <remarks>
        /// EXPERIMENTAL -- NOT RECOMMENDED FOR PRODUCTION APPLICATIONS.
        /// 
        /// Setting false will automatically reset continuousAcquisitionEnable
        /// and continuousFrames to default (off) values.
        /// </remarks>
        public bool scanAveragingIsContinuous
        {
            get => scanAveragingIsContinuous_;
            set
            {
                scanAveragingIsContinuous_ = value;

                if (value)
                {
                    configureContinuousAcquisition();
                }
                else
                {
                    continuousAcquisitionEnable = false;
                    continuousFrames = 1;
                }
            }
        }
        bool scanAveragingIsContinuous_;

        /// <summary>
        /// How many acquisitions to average together (0 or 1 for no averaging).
        /// (default 1)
        /// </summary>
        ///
        /// <remarks>
        /// Note that while SW triggering supports 2^16 scans to average, HW 
        /// triggering (necessarily using continuousFrames) is limited to 255.
        /// </remarks>
        public virtual uint scanAveraging
        {
            get { return scanAveraging_; }
            set 
            {
                lock (acquisitionLock)
                {
                    scanAveraging_ = value;
                    configureContinuousAcquisition();
                }
            }
        }
        protected uint scanAveraging_ = 1;

        void configureContinuousAcquisition()
        {
            if (!scanAveragingIsContinuous)
                return;

            if (scanAveraging > 1 && !continuousAcquisitionEnable)
            {
                logger.debug("auto-enabling continuous acquisition");
                continuousAcquisitionEnable = true;
            }
            else if (scanAveraging <= 1 && continuousAcquisitionEnable)
            {
                logger.debug("auto-disabling continuous acquisition");
                continuousAcquisitionEnable = false;
                continuousFrames = 1;
            }

            if (continuousAcquisitionEnable)
            {
                if (scanAveraging <= 0xff)
                {
                    logger.debug($"auto-setting continuousFrames to {scanAveraging}");
                    continuousFrames = (byte)scanAveraging;
                }
                else
                {
                    logger.error($"can't auto-configure continuousFrames ({scanAveraging} out of range)");
                }
            }
        }

        /// <summary>
        /// Perform post-acquisition high-frequency smoothing by averaging
        /// together "n" pixels to either side of each acquired pixel; zero
        /// to disable (default).
        /// </summary>
        public virtual uint boxcarHalfWidth
        {
            get { return boxcarHalfWidth_; }
            set { lock (acquisitionLock) boxcarHalfWidth_ = value; }
        }
        protected uint boxcarHalfWidth_;

        /// <summary>
        /// Perform automatic dark subtraction by setting this property to
        /// an acquired dark spectrum; leave "null" to disable.
        /// </summary>
        public virtual double[] dark
        {
            get { return dark_; }
            set { lock (acquisitionLock) dark_ = value; }
        }
        protected double[] dark_;

        /// <summary>
        /// Simplify reference-based techniques (absorbance, reflectance, 
        /// transmission etc) by allowing a reference to be stored with the 
        /// Spectrometer, similar to dark.  
        /// 
        /// Unlike dark, which will be automatically subtracted from Raw to form
        /// Processed, no automatic processing is performed with the Reference, 
        /// as different techniques use it differently.  This is a convenience 
        /// attribute for application programmers.
        /// </summary>
        public virtual double[] reference
        {
            get { return reference_; }
            set { reference_ = value; }
        }
        protected double[] reference_;

        /// <summary>
        /// If the spectrometer is deployed in a multi-channel configuration,
        /// this provides a place to store an integral position in the Spectrometer
        /// object.  (defaults to -1, an invalid position)
        /// </summary>
        ///
        /// <remarks>
        /// This may be populated from EEPROM.UserText or other sources.
        /// This value is not used by anything except MultiChannelWrapper and 
        /// end-user code.
        /// </remarks>
        public int multiChannelPosition = -1;

        /// <summary>
        /// Multichannel convenience accessor (default false)
        /// </summary>
        public bool multiChannelSelected { get; set; }

        /// <summary>
        /// Whether an ERROR should be logged on a timeout event (default true)
        /// </summary>
        public bool errorOnTimeout { get; set; } = true;

        /// <summary>
        /// If enabled, Wasatch.NET will automatically read the detector temperature
        /// following every successful call to getSpectrum().  That temperature 
        /// can then be read from lastDetectorTemperatureDegC, without inducing
        /// any spectrometer communications.
        /// </summary>
        ///
        /// <remarks>
        /// Many customer applications wish to monitor detector temperature.
        /// However, attempting to read temperature asynchronously from the 
        /// spectrometer over USB can complicate timing in the midst of 
        /// acquisitions.  This provides a controlled process to obtain fairly-
        /// current temperature without adding interrupts to the USB 
        /// communciation cycle.  (It's also very similar to what ENLIGHTEN does)
        /// </remarks>
        public bool readTemperatureAfterSpectrum = false;

        ////////////////////////////////////////////////////////////////////////
        // property caching 
        ////////////////////////////////////////////////////////////////////////

        private HashSet<Opcodes> readOnce = new HashSet<Opcodes>();
        private HashSet<Opcodes> noCache = new HashSet<Opcodes>();

        public void useCache(Opcodes op) { noCache.Remove(op); }
        public void dontCache(Opcodes op) { noCache.Add(op); }
        public bool haveCache(Opcodes op) { return readOnce.Contains(op) && !noCache.Contains(op); }

        ////////////////////////////////////////////////////////////////////////
        // device properties (please maintain in alphabetical order)
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Convenience accessor to set an explicit acquisition timeout.  
        /// </summary>
        /// <remarks>
        /// - If no timeout is set by the user, an internal timeout will be 
        ///   dynamically generated for software-triggered acquisitions.
        /// </remarks>
        public uint? acquisitionTimeoutMS { get; set; }

        /// <summary>
        /// Allows the NEXT acquisition timeout to be set relative to "now" as an
        /// offiset in milliseconds.
        /// </summary>
        /// <remarks>
        /// - If no timeout is set by the user, an internal timeout will be 
        ///   dynamically generated for software-triggered acquisitions.
        /// - This timeout applies to external hardware triggers as well (which
        ///   have no default internal timeout.)
        /// </remarks>
        public uint acquisitionTimeoutRelativeMS
        {
            set
            {
                if (value > 0)
                    acquisitionTimeoutTimestamp = DateTime.Now.AddMilliseconds(value);
                else
                    acquisitionTimeoutTimestamp = null;
            }
        }

        /// <summary>
        /// Allows the NEXT acquisition timeout to be set to an explicit objective
        /// future timestamp.
        /// </summary>
        /// <remarks>
        /// - If no timeout is set by the user, an internal timeout will be 
        ///   dynamically generated for software-triggered acquisitions.
        /// - This timeout applies to external hardware triggers as well (which
        ///   have no default internal timeout.)
        /// </remarks>
        public DateTime? acquisitionTimeoutTimestamp { get; set; } = null;

        public ushort actualFrames
        {
            get
            {
                return Unpack.toUshort(getCmd(Opcodes.GET_ACTUAL_FRAMES, 2));
            }
        }

        public uint actualIntegrationTimeUS
        {
            get
            {
                uint value = Unpack.toUint(getCmd(Opcodes.GET_ACTUAL_INTEGRATION_TIME, 6));
                return (value == 0xffffff) ? 0 : value;
            }
        }

        /// <warning>
        /// The photodiode (calling this as secondary ADC) should NOT swap the byte order!
        /// Does laserTemperatureRaw require the byte order to be swapped?!?
        /// </warning>
        /// <remarks>protected because caller should access via primaryADC or secondaryADC</remarks>
        protected ushort adcRaw
        {
            get
            {
                //if (!adcHasBeenSelected_)
                //    return 0;
                if (isSiG)
                    return 0;
                ushort orig = Unpack.toUshort(getCmd(Opcodes.GET_ADC_RAW, 2));
                // ushort corrected = swapBytes(orig);
                ushort retval = (ushort)(orig & 0xfff);
                logger.debug("adcRaw: raw 0x{0:x4} ({0,4})  retval 0x{1:x4} ({1,4})",
                       orig, retval);
                return retval;
            }
        }

        public uint batteryStateRaw
        {
            get
            {
                if (!eeprom.hasBattery)
                    return 0;

                // don't check the battery faster than 1Hz
                DateTime now = DateTime.Now;
                if (batteryStateTimestamp_ != null)
                    if (now < batteryStateTimestamp_.Value.AddSeconds(1))
                        return batteryStateRaw_;

                // Unpack.toUint assumes little-endian order, but this is a custom 
                // register, so let's re-order the received bytes to match the ICD
                uint tmp = Unpack.toUint(getCmd2(Opcodes.GET_BATTERY_STATE, 3));
                uint lsb = (byte)(tmp & 0xff);
                uint msb = (byte)((tmp >>  8) & 0xff);
                uint chg = (byte)((tmp >> 16) & 0xff);
                batteryStateRaw_ = (lsb << 16) | (msb << 8) | chg;

                batteryStateTimestamp_ = now;
                return batteryStateRaw_;
            }
        }
        DateTime? batteryStateTimestamp_ = null;
        uint batteryStateRaw_ = 0;

        public virtual float batteryPercentage
        {
            get
            {
                if (!eeprom.hasBattery)
                    return 0;

                uint raw = batteryStateRaw;
                byte lsb = (byte)((raw >> 16) & 0xff);
                byte msb = (byte)((raw >>  8) & 0xff);
                return ((float)(1.0 * msb)) + ((float)(1.0 * lsb / 256.0));
            }
        }

        public virtual bool batteryCharging
        {
            get
            {
                if (!eeprom.hasBattery)
                    return false;

                return 0 != (batteryStateRaw & 0xff);
            }
        }

        public bool continuousAcquisitionEnable
        {
            get
            {
                const Opcodes op = Opcodes.GET_CONTINUOUS_ACQUISITION;
                if (haveCache(op))
                    return continuousAcquisitionEnable_;
                readOnce.Add(op);
                return continuousAcquisitionEnable_ = Unpack.toBool(getCmd(op, 1));
            }
            set
            {
                sendCmd(Opcodes.SET_CONTINUOUS_ACQUISITION, (ushort)((continuousAcquisitionEnable_ = value) ? 1 : 0));
                readOnce.Add(Opcodes.GET_CONTINUOUS_ACQUISITION);
            }
        }
        bool continuousAcquisitionEnable_;

        public byte continuousFrames
        {
            get
            {
                const Opcodes op = Opcodes.GET_CONTINUOUS_FRAMES;
                if (haveCache(op))
                    return continuousFrames_;
                readOnce.Add(op);
                return continuousFrames_ = Unpack.toByte(getCmd(op, 1));
            }
            set
            {
                sendCmd(Opcodes.SET_CONTINUOUS_FRAMES, continuousFrames_ = value);
                readOnce.Add(Opcodes.GET_CONTINUOUS_FRAMES);
            }
        }
        byte continuousFrames_;

        public virtual float detectorGain
        {
            get
            {
                const Opcodes op = Opcodes.GET_DETECTOR_GAIN;
                if (haveCache(op))
                    return detectorGain_;
                readOnce.Add(op);
                return detectorGain_ = FunkyFloat.toFloat(Unpack.toUshort(getCmd(op, 2)));
            }
            set
            {
                readOnce.Add(Opcodes.GET_DETECTOR_GAIN);
                sendCmd(Opcodes.SET_DETECTOR_GAIN, FunkyFloat.fromFloat(detectorGain_ = value));
            }
        }
        float detectorGain_;

        public virtual float detectorGainOdd
        {
            get
            {
                if (featureIdentification.boardType != BOARD_TYPES.INGAAS_FX2)
                {
                    logger.debug("detectorGainOdd not supported on non-InGaAs detectors");
                    return 0;
                }
                const Opcodes op = Opcodes.GET_DETECTOR_GAIN_ODD;
                if (haveCache(op))
                    return detectorGainOdd_;
                readOnce.Add(op);
                return detectorGainOdd_ = FunkyFloat.toFloat(Unpack.toUshort(getCmd(op, 2)));
            }
            set
            {
                if (featureIdentification.boardType != BOARD_TYPES.INGAAS_FX2)
                {
                    logger.debug("detectorGainOdd not supported on non-InGaAs detectors");
                    return;
                }
                readOnce.Add(Opcodes.GET_DETECTOR_GAIN_ODD);
                sendCmd(Opcodes.SET_DETECTOR_GAIN_ODD, FunkyFloat.fromFloat(detectorGainOdd_ = value));
            }
        }
        float detectorGainOdd_;

        public virtual short detectorOffset
        {
            get
            {
                const Opcodes op = Opcodes.GET_DETECTOR_OFFSET;
                if (haveCache(op))
                    return detectorOffset_;
                readOnce.Add(op);
                return detectorOffset_ = Unpack.toShort(getCmd(op, 2));
            }
            set
            {
                readOnce.Add(Opcodes.GET_DETECTOR_OFFSET);
                sendCmd(Opcodes.SET_DETECTOR_OFFSET, ParseData.shortAsUshort(detectorOffset_ = value));
            }
        }
        short detectorOffset_;

        public virtual short detectorOffsetOdd
        {
            get
            {
                if (featureIdentification.boardType != BOARD_TYPES.INGAAS_FX2)
                {
                    logger.debug("detectorOffsetOdd not supported on non-InGaAs detectors");
                    return 0;
                }
                const Opcodes op = Opcodes.GET_DETECTOR_OFFSET_ODD;
                if (haveCache(op))
                    return detectorOffsetOdd_;
                readOnce.Add(op);
                return detectorOffsetOdd_ = Unpack.toShort(getCmd(op, 2));
            }
            set
            {
                if (featureIdentification.boardType != BOARD_TYPES.INGAAS_FX2)
                {
                    logger.debug("detectorOffsetOdd not supported on non-InGaAs detectors");
                    return;
                }
                readOnce.Add(Opcodes.GET_DETECTOR_OFFSET_ODD);
                sendCmd(Opcodes.SET_DETECTOR_OFFSET_ODD, ParseData.shortAsUshort(detectorOffsetOdd_ = value));
            }
        }
        short detectorOffsetOdd_;

        public bool detectorSensingThresholdEnabled
        {
            get
            {
                const Opcodes op = Opcodes.GET_DETECTOR_SENSING_THRESHOLD_ENABLE;
                if (haveCache(op))
                    return detectorSensingThresholdEnabled_;
                readOnce.Add(op);
                return detectorSensingThresholdEnabled_ = Unpack.toBool(getCmd(op, 1));
            }
            set
            {
                readOnce.Add(Opcodes.GET_DETECTOR_SENSING_THRESHOLD_ENABLE);
                sendCmd(Opcodes.SET_DETECTOR_SENSING_THRESHOLD_ENABLE, (ushort)((detectorSensingThresholdEnabled_ = value) ? 1 : 0));
            }
        }
        bool detectorSensingThresholdEnabled_;

        public ushort detectorSensingThreshold
        {
            get
            {
                const Opcodes op = Opcodes.GET_DETECTOR_SENSING_THRESHOLD;
                if (haveCache(op))
                    return detectorSensingThreshold_;
                readOnce.Add(op);
                return detectorSensingThreshold_ = Unpack.toUshort(getCmd(op, 2));
            }
            set
            {
                readOnce.Add(Opcodes.GET_DETECTOR_SENSING_THRESHOLD);
                sendCmd(Opcodes.SET_DETECTOR_SENSING_THRESHOLD, detectorSensingThreshold_ = value);
            }
        }
        ushort detectorSensingThreshold_;

        public virtual bool detectorTECEnabled
        {
            get
            {
                const Opcodes op = Opcodes.GET_DETECTOR_TEC_ENABLE;
                if (!eeprom.hasCooling)
                    return false;
                if (haveCache(op))
                    return detectorTECEnabled_;
                readOnce.Add(op);
                return detectorTECEnabled_ = Unpack.toBool(getCmd(op, 1));
            }
            set
            {
                if (eeprom.hasCooling)
                {
                    readOnce.Add(Opcodes.GET_DETECTOR_TEC_ENABLE);
                    sendCmd(Opcodes.SET_DETECTOR_TEC_ENABLE, (ushort)((detectorTECEnabled_ = value) ? 1 : 0));
                }
            }
        }
        protected bool detectorTECEnabled_;

        public virtual float detectorTECSetpointDegC
        {
            get
            {
                // Normal cache doesn't work, because there is no opcode to read
                // TEC setpoint in DegC, because that isn't a spectrometer property.
                return detectorTECSetpointDegC_;
            }
            set
            {
                // generate and cache the DegC version
                detectorTECSetpointDegC_ = Math.Max(eeprom.detectorTempMin, Math.Min(eeprom.detectorTempMax, value));

                // convert to raw and apply
                float dac = eeprom.degCToDACCoeffs[0]
                          + eeprom.degCToDACCoeffs[1] * detectorTECSetpointDegC_
                          + eeprom.degCToDACCoeffs[2] * detectorTECSetpointDegC_ * detectorTECSetpointDegC_;
                detectorTECSetpointRaw = Math.Min((ushort)0xfff, (ushort)Math.Round(dac));
            }
        }
        protected float detectorTECSetpointDegC_ = UNINITIALIZED_TEMPERATURE_DEG_C;

        public virtual ushort detectorTECSetpointRaw
        {
            get
            {
                const Opcodes op = Opcodes.GET_DETECTOR_TEC_SETPOINT;
                if (!eeprom.hasCooling)
                    return 0;
                if (haveCache(op))
                    return detectorTECSetpointRaw_;
                readOnce.Add(op);
                return detectorTECSetpointRaw_ = Unpack.toUshort(getCmd(op, 2, wIndex: 0));
            }
            set
            {
                if (eeprom.hasCooling)
                {
                    sendCmd(Opcodes.SET_DETECTOR_TEC_SETPOINT, detectorTECSetpointRaw_ = value);
                    readOnce.Add(Opcodes.GET_DETECTOR_TEC_SETPOINT);
                }
            }
        }
        protected ushort detectorTECSetpointRaw_;

        public virtual float detectorTemperatureDegC
        {
            get
            {
                ushort raw = detectorTemperatureRaw;
                float degC = eeprom.adcToDegCCoeffs[0]
                           + eeprom.adcToDegCCoeffs[1] * raw
                           + eeprom.adcToDegCCoeffs[2] * raw * raw;
                logger.debug("getDetectorTemperatureDegC: raw 0x{0:x4}, coeff {1:f2}, {2:f2}, {3:f2} = {4:f2}",
                        raw,
                        eeprom.adcToDegCCoeffs[0],
                        eeprom.adcToDegCCoeffs[1],
                        eeprom.adcToDegCCoeffs[2],
                        degC);
                return lastDetectorTemperatureDegC = degC;
            }
        }


        /// <summary>
        /// A cached value of the last-measured detector temperature.  This
        /// is automatically updated following spectral reads if 
        /// readTemperatureAfterSpectrum is set.
        /// </summary>
        public float lastDetectorTemperatureDegC = UNINITIALIZED_TEMPERATURE_DEG_C;

        public ushort detectorTemperatureRaw
        {
            get
            {
                if (isSiG)
                    return 0;
                if (!eeprom.hasCooling)
                    return 0;
                return swapBytes(Unpack.toUshort(getCmd(Opcodes.GET_DETECTOR_TEMPERATURE, 2)));
            }
        }

        public virtual string firmwareRevision
        {
            get
            {
                const Opcodes op = Opcodes.GET_FIRMWARE_REVISION;
                if (haveCache(op))
                    return firmwareRevision_;
                byte[] buf = getCmd(op, 4);
                if (buf is null)
                    return "ERROR";
                string s = "";
                for (int i = 3; i >= 0; i--)
                {
                    s += String.Format("{0}", buf[i]);
                    if (i > 0)
                        s += ".";
                }
                readOnce.Add(op);
                return firmwareRevision_ = s;
            }
        }
        string firmwareRevision_;

        public virtual string fpgaRevision
        {
            get
            {
                const Opcodes op = Opcodes.GET_FPGA_REVISION;
                if (haveCache(op))
                    return fpgaRevision_;
                byte[] buf = getCmd(op, 7);
                if (buf is null)
                    return "UNKNOWN";
                string s = "";
                for (uint i = 0; i < 7; i++)
                    s += (char)buf[i];
                readOnce.Add(op);
                return fpgaRevision_ = s.TrimEnd();
            }
        }
        string fpgaRevision_;

        public bool highGainModeEnabled
        {
            get
            {
                if (featureIdentification.boardType != BOARD_TYPES.INGAAS_FX2)
                    return false;
                const Opcodes op = Opcodes.GET_CF_SELECT;
                if (haveCache(op))
                    return highGainModeEnabled_;
                readOnce.Add(op);
                return highGainModeEnabled_ = Unpack.toBool(getCmd(op, 1));
            }
            set
            {
                if (featureIdentification.boardType != BOARD_TYPES.INGAAS_FX2)
                    return;
                readOnce.Add(Opcodes.GET_CF_SELECT);
                sendCmd(Opcodes.SET_CF_SELECT, (ushort)((highGainModeEnabled_ = value) ? 1 : 0));
            }
        }
        bool highGainModeEnabled_;

        public HORIZONTAL_BINNING horizontalBinning
        {
            get
            {
                if (featureIdentification.boardType == BOARD_TYPES.RAMAN_FX2)
                    return HORIZONTAL_BINNING.ERROR;

                const Opcodes op = Opcodes.GET_HORIZONTAL_BINNING;
                if (haveCache(op))
                    return horizontalBinning_;
                horizontalBinning_ = HORIZONTAL_BINNING.ERROR;
                byte[] buf = getCmd(op, 1);
                if (buf != null)
                    switch (buf[0])
                    {
                        case 0: horizontalBinning_ = HORIZONTAL_BINNING.NONE; break;
                        case 1: horizontalBinning_ = HORIZONTAL_BINNING.TWO_PIXEL; break;
                        case 2: horizontalBinning_ = HORIZONTAL_BINNING.FOUR_PIXEL; break;
                    }
                if (horizontalBinning_ != HORIZONTAL_BINNING.ERROR)
                    readOnce.Add(op);
                return horizontalBinning_;
            }
            set
            {
                if (featureIdentification.boardType == BOARD_TYPES.RAMAN_FX2 || value == HORIZONTAL_BINNING.ERROR)
                    return;
                sendCmd(Opcodes.SET_HORIZONTAL_BINNING, (ushort)(horizontalBinning_ = value));
                readOnce.Add(Opcodes.GET_HORIZONTAL_BINNING);
            }
        }
        HORIZONTAL_BINNING horizontalBinning_;

        public virtual uint integrationTimeMS
        {
            get
            {
                const Opcodes op = Opcodes.GET_INTEGRATION_TIME;
                if (haveCache(op))
                    return integrationTimeMS_;
                byte[] buf = getCmd(op, 3, fullLen: 6);
                if (buf is null)
                    return 0;
                readOnce.Add(op);
                return integrationTimeMS_ = Unpack.toUint(buf);
            }
            set
            {
                // noop if already set 
                const Opcodes op = Opcodes.GET_INTEGRATION_TIME;
                if (haveCache(op) && integrationTimeMS_ == value)
                {
                    logger.debug($"ignoring request to re-set existing integration time {value}");
                    return;
                }

                lock (acquisitionLock)
                {
                    // temporarily disabled EEPROM range-checking by customer 
                    // request; range limits in EEPROM are defined as 16-bit 
                    // values, while integration time is actually a 24-bit value,
                    // such that the EEPROM is artificially limiting our range.
                    //
                    // uint ms = Math.Max(eeprom.minIntegrationTimeMS, Math.Min(eeprom.maxIntegrationTimeMS, value));

                    uint ms = value;
                    ushort lsw = (ushort)(ms & 0xffff);
                    ushort msw = (ushort)((ms >> 16) & 0x00ff);

                    // logger.debug("setIntegrationTimeMS: {0} ms = lsw {1:x4} msw {2:x4}", ms, lsw, msw);
                    byte[] buf = null;
                    if (isARM || isStroker)
                        buf = new byte[8];
                    sendCmd(Opcodes.SET_INTEGRATION_TIME, lsw, msw, buf: buf);
                    integrationTimeMS_ = ms;
                    readOnce.Add(op);

                    if (throwawayAfterIntegrationTime)
                        performThrowawaySpectrum();
                }
            }
        }
        protected uint integrationTimeMS_;

        public virtual bool laserEnabled // dangerous one to cache...
        {
            get
            {
                const Opcodes op = Opcodes.GET_LASER_ENABLE;
                if (haveCache(op))
                    return laserEnabled_;
                readOnce.Add(op);
                return laserEnabled_ = Unpack.toBool(getCmd(op, 1));
            }
            set
            {
                var buf = isARM ? new byte[8] : new byte[0];
                readOnce.Add(Opcodes.GET_LASER_ENABLE);
                sendCmd(Opcodes.SET_LASER_ENABLE, (ushort)((laserEnabled_ = value) ? 1 : 0), buf: buf);
                if (value)
                    laserHasFired_ = true;
            }
        }
        protected bool laserEnabled_;
        protected bool laserHasFired_;

        public bool laserModulationEnabled
        {
            get
            {
                const Opcodes op = Opcodes.GET_LASER_MOD_ENABLE;
                if (haveCache(op))
                    return laserModulationEnabled_;
                readOnce.Add(op);
                return laserModulationEnabled_ = Unpack.toBool(getCmd(op, 1));
            }
            set
            {
                readOnce.Add(Opcodes.GET_LASER_MOD_ENABLE);
                sendCmd(Opcodes.SET_LASER_MOD_ENABLE, (ushort)((laserModulationEnabled_ = value) ? 1 : 0)); // TODO: missing fake 8-byte buf?
            }
        }
        bool laserModulationEnabled_;

        public virtual bool laserInterlockEnabled
        {
            get
            {
                if (isARM)
                {
                    logger.debug("GET_LASER_INTERLOCK not supported on ARM");
                    return false;
                }
                return Unpack.toBool(getCmd(Opcodes.GET_LASER_INTERLOCK, 1));
            }
        }

        public bool laserModulationLinkedToIntegrationTime
        {
            get
            {
                const Opcodes op = Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME;
                if (haveCache(op))
                    return laserModulationLinkedToIntegrationTime_;
                readOnce.Add(op);
                return laserModulationLinkedToIntegrationTime_ = Unpack.toBool(getCmd(op, 1));
            }
            set
            {
                readOnce.Add(Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME);
                sendCmd(Opcodes.SET_LINK_LASER_MOD_TO_INTEGRATION_TIME, (ushort)((laserModulationLinkedToIntegrationTime_ = value) ? 1 : 0));
            }
        }
        bool laserModulationLinkedToIntegrationTime_;

        public UInt64 laserModulationPulseDelay
        {
            get
            {
                const Opcodes op = Opcodes.GET_LASER_MOD_PULSE_DELAY;
                if (haveCache(op))
                    return laserModulationPulseDelay_;
                readOnce.Add(op);
                return Unpack.toUint64(getCmd(op, 5));
            }
            set
            {
                readOnce.Add(Opcodes.GET_LASER_MOD_PULSE_DELAY);
                UInt40 val = new UInt40(laserModulationPulseDelay_ = value);
                sendCmd(Opcodes.SET_LASER_MOD_PULSE_DELAY, val.LSW, val.MidW, val.buf);
            }
        }
        UInt64 laserModulationPulseDelay_;

        public UInt64 laserModulationDuration
        {
            get
            {
                const Opcodes op = Opcodes.GET_LASER_MOD_DURATION;
                if (haveCache(op))
                    return laserModulationDuration_;
                readOnce.Add(op);
                return laserModulationDuration_ = Unpack.toUint64(getCmd(op, 5));
            }
            set
            {
                readOnce.Add(Opcodes.GET_LASER_MOD_DURATION);
                UInt40 val = new UInt40(laserModulationDuration_ = value);
                sendCmd(Opcodes.SET_LASER_MOD_DURATION, val.LSW, val.MidW, val.buf);
            }
        }
        UInt64 laserModulationDuration_;

        public virtual UInt64 laserModulationPeriod
        {
            get
            {
                const Opcodes op = Opcodes.GET_LASER_MOD_PERIOD;
                if (haveCache(op))
                    return laserModulationPeriod_;
                readOnce.Add(op);
                return laserModulationPeriod_ = Unpack.toUint64(getCmd(op, 5));
            }
            set
            {
                readOnce.Add(Opcodes.GET_LASER_MOD_PERIOD);
                UInt40 val = new UInt40(laserModulationPeriod_ = value);
                sendCmd(Opcodes.SET_LASER_MOD_PERIOD, val.LSW, val.MidW, val.buf);
            }
        }
        protected UInt64 laserModulationPeriod_;

        public virtual UInt64 laserModulationPulseWidth
        {
            get
            {
                const Opcodes op = Opcodes.GET_LASER_MOD_PULSE_WIDTH;
                if (haveCache(op))
                    return laserModulationPulseWidth_;
                readOnce.Add(op);
                return laserModulationPulseWidth_ = Unpack.toUint64(getCmd(op, 5));
            }
            set
            {
                readOnce.Add(Opcodes.GET_LASER_MOD_PULSE_WIDTH);
                UInt40 val = new UInt40(laserModulationPulseWidth_ = value);
                sendCmd(Opcodes.SET_LASER_MOD_PULSE_WIDTH, val.LSW, val.MidW, val.buf);
            }
        }
        protected UInt64 laserModulationPulseWidth_;

        /// <summary>disabled to deconflict area scan</summary>
        public bool laserRampingEnabled
        {
            get
            {
                if (fpgaOptions.hasAreaScan)
                {
                    logger.debug("laserRampingEnabled feature currently disabled");
                    return false; // disabled
                }
                else
                    return false;
                /*
                if (featureIdentification.boardType != BOARD_TYPES.ARM)
                    return false;
                const Opcodes op = Opcodes.GET_LASER_RAMPING_MODE;
                if (haveCache(op))
                    return laserRampingEnabled_;
                readOnce.Add(op);
                return laserRampingEnabled_ = Unpack.toBool(getCmd(op, 1));
                */
            }
            set
            {
                logger.error("laserRampingEnabled feature currently disabled");
                /*
                if (featureIdentification.boardType != BOARD_TYPES.RAMAN_FX2)
                    return;

                // should we check for fpgaOptions.laserControl == FPGA_LASER_CONTROL.RAMPING here?

                readOnce.Add(Opcodes.GET_LASER_RAMPING_MODE);

                // sendCmd(Opcodes.SET_LASER_RAMPING_MODE, (ushort)((laserRampingEnabled_ = value) ? 1 : 0));
                */
            }
        }
        // bool laserRampingEnabled_;

        public bool areaScanEnabled
        {
            get
            {
                /*
                if (!fpgaOptions.hasAreaScan)
                {
                    logger.debug("laserRampingEnabled feature currently disabled");
                    return false; 
                }
                */
                //else
                // {

                return _areaScanEnabled;

                /*
                 byte[] pack = getCmd(Opcodes.GET_AREA_SCAN_ENABLE, 1);
                 return Unpack.toBool(pack);
                 */
                //}
            }
            set
            {
                sendCmd(Opcodes.SET_AREA_SCAN_ENABLE, (ushort)((_areaScanEnabled = value) ? 1 : 0), 0, new byte[]{0,0,0,0,0,0,0,0,0,0});
            }
        }
        bool _areaScanEnabled = false;

        public virtual float laserTemperatureDegC
        {
            get
            {
                ushort raw = laserTemperatureRaw;
                if (raw == 0)
                {
                    logger.debug("laserTemperatureDegC.get: can't take log of zero");
                    return 0;
                }

                double rawD = raw;

                // should this be 2.468? (see Dash3/WasatchDevices/Stroker785L_LaserTempSetpoint.py)
                double voltage = 2.5 * rawD / 4096;
                double resistance = 21450.0 * voltage / (2.5 - voltage);
                if (resistance <= 0)
                {
                    logger.error("laserTemperatureDegC.get: invalid resistance (raw {0:x4}, voltage {1}, resistance {2:f2} ohms)", 
                        raw, voltage, resistance);
                    return 0;
                }

                // Original Dash / ENLIGHTEN math:
                //
                // double logVal = Math.Log(resistance / 10000);
                // double insideMain = logVal + 3977.0 / (25 + 273.0);
                // double degC = 3977.0 / insideMain - 273.0;

                double C1 = 0.00113;
                double C2 = 0.000234;
                double C3 = 8.78e-8;
                double lnOhms = Math.Log(resistance);
                double degC = 1.0 / (  C1
                                     + C2 * lnOhms
                                     + C3 * Math.Pow(lnOhms, 3)
                                    ) - 273.15;

                logger.debug("laserTemperatureDegC.get: {0:f2} deg C (raw 0x{1:x4}, resistance {2:f2} ohms)", degC, raw, resistance);

                return (float)degC;
            }
        }

        public virtual ushort laserTemperatureRaw => primaryADC;

        public virtual byte laserTemperatureSetpointRaw
        {
            get
            {
                // if (!laserHasFired_) return 0;

                if (!eeprom.hasLaser)
                    return 0;

                const Opcodes op = Opcodes.GET_LASER_TEC_SETPOINT;
                if (haveCache(op))
                    return laserTemperatureSetpointRaw_;
                if (isSiG) // || featureIdentification.boardType == BOARD_TYPES.RAMAN_FX2)
                    return 0;
                readOnce.Add(op);
                return laserTemperatureSetpointRaw_ = Unpack.toByte(getCmd(op, 1));
            }
            set
            {
                if (!eeprom.hasLaser)
                    return;

                sendCmd(Opcodes.SET_LASER_TEC_SETPOINT, laserTemperatureSetpointRaw_ = Math.Min((byte)127, value));
                readOnce.Add(Opcodes.GET_LASER_TEC_SETPOINT);
            }
        }
        protected byte laserTemperatureSetpointRaw_;

        public uint lineLength
        {
            get
            {
                const Opcodes op = Opcodes.GET_LINE_LENGTH;
                if (haveCache(op))
                    return lineLength_;
                readOnce.Add(op);
                return lineLength_ = Unpack.toUshort(getCmd2(op, 2));
            }
        }
        uint lineLength_;

        public bool optAreaScan
        {
            get
            {
                const Opcodes op = Opcodes.GET_OPT_AREA_SCAN;
                if (haveCache(op))
                    return optAreaScan_;
                readOnce.Add(op);
                return optAreaScan_ = Unpack.toBool(getCmd2(op, 1));
            }
        }
        bool optAreaScan_;

        public bool optActualIntegrationTime
        {
            get
            {
                const Opcodes op = Opcodes.GET_OPT_ACTUAL_INTEGRATION_TIME;
                if (haveCache(op))
                    return optActualIntegrationTime_;
                readOnce.Add(op);
                return optActualIntegrationTime_ = Unpack.toBool(getCmd2(op, 1));
            }
        }
        bool optActualIntegrationTime_;

        public bool optCFSelect
        {
            get
            {
                const Opcodes op = Opcodes.GET_OPT_CF_SELECT;
                if (haveCache(op))
                    return optCFSelect_;
                readOnce.Add(op);
                return optCFSelect_ = Unpack.toBool(getCmd2(op, 1));
            }
        }
        bool optCFSelect_;

        public FPGA_DATA_HEADER optDataHeaderTag
        {
            get
            {
                const Opcodes op = Opcodes.GET_OPT_DATA_HEADER_TAG;
                if (haveCache(op))
                    return optDataHeaderTag_;
                readOnce.Add(op);
                return optDataHeaderTag_ = fpgaOptions.parseDataHeader(Unpack.toInt(getCmd2(op, 1)));
            }
        }
        FPGA_DATA_HEADER optDataHeaderTag_ = FPGA_DATA_HEADER.ERROR;

        public bool optHorizontalBinning
        {
            get
            {
                const Opcodes op = Opcodes.GET_OPT_HORIZONTAL_BINNING;
                if (haveCache(op))
                    return optHorizontalBinning_;
                readOnce.Add(op);
                return optHorizontalBinning_ = Unpack.toBool(getCmd2(op, 1));
            }
        }
        bool optHorizontalBinning_;

        public FPGA_INTEG_TIME_RES optIntegrationTimeResolution
        {
            get
            {
                const Opcodes op = Opcodes.GET_OPT_INTEGRATION_TIME_RESOLUTION;
                if (haveCache(op))
                    return optIntegrationTimeResolution_;
                readOnce.Add(op);
                return optIntegrationTimeResolution_ = fpgaOptions.parseResolution(Unpack.toInt(getCmd2(op, 1)));
            }
        }
        FPGA_INTEG_TIME_RES optIntegrationTimeResolution_ = FPGA_INTEG_TIME_RES.ERROR;

        public FPGA_LASER_CONTROL optLaserControl
        {
            get
            {
                const Opcodes op = Opcodes.GET_OPT_LASER_CONTROL;
                if (haveCache(op))
                    return optLaserControl_;
                readOnce.Add(op);
                return optLaserControl_ = fpgaOptions.parseLaserControl(Unpack.toInt(getCmd2(op, 1)));
            }
        }
        FPGA_LASER_CONTROL optLaserControl_ = FPGA_LASER_CONTROL.ERROR;

        public FPGA_LASER_TYPE optLaserType
        {
            get
            {
                const Opcodes op = Opcodes.GET_OPT_LASER_TYPE;
                if (haveCache(op))
                    return optLaserType_;
                readOnce.Add(op);
                return optLaserType_ = fpgaOptions.parseLaserType(Unpack.toInt(getCmd2(op, 1)));
            }
        }
        FPGA_LASER_TYPE optLaserType_ = FPGA_LASER_TYPE.ERROR;

        public ushort primaryADC
        {
            get
            {
                lock (adcLock)
                {
                    if (selectedADC != 0)
                        selectedADC = 0;
                    return adcRaw;
                }
            }
        }

        public bool hasSecondaryADC { get; set; } = false;

        public virtual ushort secondaryADC
        {
            get
            {
                if (!hasSecondaryADC)
                    return 0;
                lock (adcLock)
                {
                    if (selectedADC != 1)
                        selectedADC = 1;
                    return adcRaw;
                }
            }
        }

        protected byte selectedADC
        {
            get
            {
                if (!adcHasBeenSelected_)
                    return 0;
                const Opcodes op = Opcodes.GET_SELECTED_ADC;
                if (haveCache(op))
                    return selectedADC_;
                readOnce.Add(op);
                return selectedADC_ = Unpack.toByte(getCmd(op, 1));
            }
            set
            {
                readOnce.Add(Opcodes.SET_SELECTED_ADC);
                sendCmd(Opcodes.SET_SELECTED_ADC, selectedADC_ = value);
                if (throwawayADCRead)
                    throwawaySum += adcRaw;
                adcHasBeenSelected_ = true;
            }
        }
        byte selectedADC_;
        bool adcHasBeenSelected_;

        public virtual TRIGGER_SOURCE triggerSource
        {
            get
            {
                if (featureIdentification.boardType != BOARD_TYPES.ARM)
                {
                    logger.debug("GET_TRIGGER_SOURCE disabled for boardType {0}", featureIdentification.boardType.ToString());
                    return TRIGGER_SOURCE.INTERNAL;
                }
                const Opcodes op = Opcodes.GET_TRIGGER_SOURCE;
                if (haveCache(op))
                    return triggerSource_;
                byte[] buf = getCmd(Opcodes.GET_TRIGGER_SOURCE, 1);
                if (buf is null || buf[0] > 2)
                    return TRIGGER_SOURCE.ERROR;
                readOnce.Add(op);
                return triggerSource_ = buf[0] == 0 ? TRIGGER_SOURCE.INTERNAL : TRIGGER_SOURCE.EXTERNAL;
            }
            set
            {
                if (value == TRIGGER_SOURCE.ERROR)
                    return;
                UInt40 val = new UInt40((ushort)(triggerSource_ = value));
                readOnce.Add(Opcodes.GET_TRIGGER_SOURCE);
                if (featureIdentification.boardType != BOARD_TYPES.ARM)
                    sendCmd(Opcodes.SET_TRIGGER_SOURCE, val.LSW, val.MidW, val.buf);
                else
                    logger.debug("not sending SET_TRIGGER_SOURCE (0x{0:x2}) -> {1} because ARM", cmd[Opcodes.SET_TRIGGER_SOURCE], triggerSource_);
            }
        }
        protected TRIGGER_SOURCE triggerSource_ = TRIGGER_SOURCE.INTERNAL; // not ERROR

        // MZ: I'm not sure what GPIO pin would support this triggerOutput...
        //     on one recent "outbound" triggering project, we ended up using
        //     laserEnable as the outbound trigger because we couldn't find a
        //     usable GPIO.
        public EXTERNAL_TRIGGER_OUTPUT triggerOutput
        {
            get
            {
                if (featureIdentification.boardType != BOARD_TYPES.ARM)
                {
                    logger.debug("GET_TRIGGER_OUTPUT disabled for boardType {0}", featureIdentification.boardType.ToString());
                    return EXTERNAL_TRIGGER_OUTPUT.ERROR;
                }
                const Opcodes op = Opcodes.GET_TRIGGER_OUTPUT;
                if (haveCache(op))
                    return triggerOutput_;
                triggerOutput_ = EXTERNAL_TRIGGER_OUTPUT.ERROR;
                byte[] buf = getCmd(Opcodes.GET_TRIGGER_OUTPUT, 1);
                if (buf != null)
                {
                    switch (buf[0])
                    {
                        case 0: triggerOutput_ = EXTERNAL_TRIGGER_OUTPUT.LASER_MODULATION; break;
                        case 1: triggerOutput_ = EXTERNAL_TRIGGER_OUTPUT.INTEGRATION_ACTIVE_PULSE; break;
                    }
                }
                if (triggerOutput_ != EXTERNAL_TRIGGER_OUTPUT.ERROR)
                    readOnce.Add(op);
                return triggerOutput_;
            }
            set
            {
                if (value == EXTERNAL_TRIGGER_OUTPUT.ERROR)
                    return;
                readOnce.Add(Opcodes.GET_TRIGGER_OUTPUT);
                sendCmd(Opcodes.SET_TRIGGER_OUTPUT, (ushort)(triggerOutput_ = value));
            }
        }
        EXTERNAL_TRIGGER_OUTPUT triggerOutput_ = EXTERNAL_TRIGGER_OUTPUT.ERROR;

        public uint triggerDelay
        {
            get
            {
                if (featureIdentification.boardType != BOARD_TYPES.ARM)
                {
                    logger.debug("GET_TRIGGER_DELAY disabled for boardType {0}", featureIdentification.boardType.ToString());
                    return 0;
                }
                const Opcodes op = Opcodes.GET_TRIGGER_DELAY;
                if (haveCache(op))
                    return triggerDelay_;
                readOnce.Add(op);
                return triggerDelay_ = Unpack.toUint(getCmd(op, 3));
            }
            set
            {
                if (featureIdentification.boardType == BOARD_TYPES.RAMAN_FX2)
                    return;
                ushort lsw = (ushort)((triggerDelay_ = value) & 0xffff);
                byte msb = (byte)(value >> 16);
                sendCmd(Opcodes.SET_TRIGGER_DELAY, lsw, msb);
                readOnce.Add(Opcodes.GET_TRIGGER_DELAY);
            }
        }
        uint triggerDelay_;

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        internal Spectrometer(UsbRegistry usbReg)
        {
            usbRegistry = usbReg;
            pixels = 0;
        }

        virtual internal bool open()
        {
            logger.header($"Spectrometer.open: VID = 0x{usbRegistry.Vid:x4}, PID = 0x{usbRegistry.Pid:x4}");

            // clear cache
            readOnce.Clear();

            if (!reconnect())
                return logger.error("Spectrometer.open: couldn't reconnect");

            // derive some values from VID/PID
            featureIdentification = new FeatureIdentification(usbRegistry.Vid, usbRegistry.Pid);
            if (!featureIdentification.isSupported)
                return false;
            if (featureIdentification.boardType == BOARD_TYPES.STROKER)
                isStroker = true;
            else
                isStroker = false;

            // load EEPROM configuration
            logger.debug("reading EEPROM");
            eeprom = new EEPROM(this);
            if (!eeprom.read())
            {
                logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                usbDevice.Close();
                return false;
            }
            logger.debug("back from reading EEPROM");

            // see how the FPGA was compiled
            logger.debug("reading FPGA Options");
            fpgaOptions = new FPGAOptions(this);
            logger.debug("back from FPGA Options");

            logger.debug($"firmwareRevision = {firmwareRevision}");
            logger.debug($"fpgaRevision = {fpgaRevision}");

            // MustardTree uses 2048-pixel version of the S11510, and all InGaAs are 512
            pixels = (uint)eeprom.activePixelsHoriz;
            if (pixels > 2048)
            {
                logger.error("Unlikely pixels count found ({0}); defaulting to {1}",
                    eeprom.activePixelsHoriz, featureIdentification.defaultPixels);
                pixels = featureIdentification.defaultPixels;
            }

            // figure out what endpoints we'll use, and sizes for each
            pixelsPerEndpoint = (int)pixels;
            endpoints.Add(spectralReader82);
            if (pixels == 2048 && !isARM)
            {
                logger.debug("splitting over two endpoints");
                endpoints.Add(spectralReader86);
                pixelsPerEndpoint = 1024;
                usingDualEndpoints = true;
            }

            // flush anything left-over from prior exchanges
            spectralReader82.ReadFlush();
            if (usingDualEndpoints)
                spectralReader86.ReadFlush();
            else
                spectralReader86 = null;

            regenerateWavelengths();

            // by default, integration time is zero in HW, so set to something
            // (ignore startup value if it's unreasonable)
            if (eeprom.startupIntegrationTimeMS >= eeprom.minIntegrationTimeMS &&
                eeprom.startupIntegrationTimeMS < 5000)
                integrationTimeMS = eeprom.startupIntegrationTimeMS;
            else
                integrationTimeMS = eeprom.minIntegrationTimeMS;

            // MZ: base on A/R/C?
            // use cache variable because why not
            // TS: I get the intent here but we should be using the EEPROM field for this...
            float degC = UNINITIALIZED_TEMPERATURE_DEG_C;
            if (featureIdentification.hasDefaultTECSetpointDegC)
                degC = featureIdentification.defaultTECSetpointDegC;
            else if (Regex.IsMatch(eeprom.detectorName, @"S10141|G9214", RegexOptions.IgnoreCase))
                degC = -15;
            else if (Regex.IsMatch(eeprom.detectorName, @"S11511|S11850|S13971|S7031", RegexOptions.IgnoreCase))
                degC = 10;

            if (eeprom.startupDetectorTemperatureDegC >= eeprom.detectorTempMin && eeprom.startupDetectorTemperatureDegC <= eeprom.detectorTempMax)
                degC = eeprom.startupDetectorTemperatureDegC;

            if (hasLaser)
            {
                // ENLIGHTEN doesn't do this
                logger.debug("unlinking laser modulation from integration time");
                laserModulationLinkedToIntegrationTime = false;

                logger.debug("disabling laser modulation");
                laserModulationEnabled = false;

                logger.debug("disabling laser");
                laserEnabled = false;
            }

            if (eeprom.hasCooling && degC != UNINITIALIZED_TEMPERATURE_DEG_C)
            {
                // TEC doesn't do anything unless you give it a temperature first
                logger.debug("setting TEC setpoint to {0} deg C", degC);
                detectorTECSetpointDegC = degC;

                logger.debug("enabling detector TEC");
                detectorTECEnabled = true;
            }

            // if we're using a modern EEPROM format, automatically apply the stored gain/offset values
            if (eeprom.format >= 4)
            {
                detectorGain = eeprom.detectorGain;
                detectorOffset = eeprom.detectorOffset;
                if (featureIdentification.boardType == BOARD_TYPES.INGAAS_FX2)
                {
                    detectorGainOdd = eeprom.detectorGainOdd;
                    detectorOffsetOdd = eeprom.detectorOffsetOdd;
                }
            }

            logger.debug("Spectrometer.open: complete");
            return true;
        }

        public virtual void close()
        {
            shuttingDown = true;
            logger.debug("throwawaySum = {0}", throwawaySum); // just make sure it gets used

            if (usbDevice != null)
            {
                if (usbDevice.IsOpen)
                {
                    if (hasLaser)
                        laserEnabled = false;

                    IUsbDevice wholeUsbDevice = usbDevice as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                        wholeUsbDevice.ReleaseInterface(0);
                    usbDevice.Close();
                }
                usbDevice = null;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Convenience Accessors
        ////////////////////////////////////////////////////////////////////////

        public virtual bool isARM => featureIdentification.boardType == BOARD_TYPES.ARM;
        public bool isSiG => eeprom.model.ToLower().Contains("sig") || eeprom.detectorName.ToLower().Contains("imx");
        
        public virtual bool hasLaser
        {
            get
            {
                return eeprom.hasLaser;
                // return eeprom.hasLaser && (fpgaOptions.laserType == FPGA_LASER_TYPE.INTERNAL || fpgaOptions.laserType == FPGA_LASER_TYPE.EXTERNAL);
            }
        }

        public virtual float excitationWavelengthNM
        {
            get
            {
                float old = eeprom.excitationNM;
                float newer = eeprom.laserExcitationWavelengthNMFloat;

                // if float is corrupt or zero, return original EEPROM field
                if (Double.IsNaN(newer) || newer == 0.0)
                    return old;

                // if float looks valid, use it
                if (200 <= newer && newer <= 2500)
                    return newer;

                // default to old value
                return old;
            }

            set
            {
                eeprom.excitationNM = (ushort) value;
                eeprom.laserExcitationWavelengthNMFloat = value;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Utilities
        ////////////////////////////////////////////////////////////////////////

        public virtual void regenerateWavelengths()
        {
            wavelengths = Util.generateWavelengths(pixels, eeprom.wavecalCoeffs);
            if (excitationWavelengthNM > 0)
                wavenumbers = Util.wavelengthsToWavenumbers(excitationWavelengthNM, wavelengths);
        }

        string stringifyPacket(UsbSetupPacket packet)
        {
            return String.Format("bRequestType: 0x{0:x2}, bRequest: 0x{1:x4}, wValue: 0x{2:x4}, wIndex: 0x{3:x4}, wLength: 0x{4:x4}",
                packet.RequestType, packet.Request, packet.Value, packet.Index, packet.Length);
        }

        /// <summary>
        /// Although most values read from the spectrometer are little-endian by
        /// design, a few are big-endian.
        /// </summary>
        /// <param name="raw">input value in one endian</param>
        /// <returns>same value with the bytes reversed</returns>
        ushort swapBytes(ushort raw)
        {
            byte lsb = (byte)(raw & 0xff);
            byte msb = (byte)((raw >> 8) & 0xff);
            return (ushort)((lsb << 8) | msb);
        }

        public virtual void changeSPITrigger(bool edge, bool firmwareThrow)
        {

        }

        void waitForUsbAvailable()
        {
            if (featureIdentification != null && featureIdentification.usbDelayMS > 0)
            {
                var usbDelayMS = featureIdentification.usbDelayMS;
                DateTime nextUsbTimestamp = lastUsbTimestamp.AddMilliseconds(usbDelayMS);
                int delayMS = (int)(nextUsbTimestamp - DateTime.Now).TotalMilliseconds;
                if (delayMS > 0)
                {
                    do
                    {
                        logger.debug("sleeping {0} ms to enforce {1} ms USB interval", delayMS, usbDelayMS);
                        Thread.Sleep(delayMS);
                    } while (!usbDevice.IsOpen);
                }
                // else
                //     logger.debug($"no need to sleep, as last {lastUsbTimestamp} is more than {usbDelayMS}ms before next {nextUsbTimestamp}");
            }
            lastUsbTimestamp = DateTime.Now;
        }

        public virtual bool reconnect()
        {
            logger.debug("Spectrometer.reconnect: starting");

            // clear the info so far
            if (usbDevice != null)
            {
                logger.debug("Spectrometer.reconnect: clearing");
                spectralReader82.Dispose();
                if (spectralReader86 != null)
                    spectralReader86.Dispose();
                // statusReader.Dispose();
                wholeUsbDevice.ReleaseInterface(0);
                wholeUsbDevice.Close();
                usbDevice.Close();
                UsbDevice.Exit();

                usbDevice = null;
                wholeUsbDevice = null;
                // statusReader = null;
                spectralReader82 = null;
                spectralReader86 = null;

                Thread.Sleep(10);
            }

            // now start over
            logger.debug("Spectrometer.reconnect: opening");
            if (!usbRegistry.Open(out usbDevice))
                return logger.error("Spectrometer: failed to re-open UsbRegistry");

            wholeUsbDevice = usbDevice as IUsbDevice;
            if (wholeUsbDevice != null)
            {
                logger.debug("Spectrometer.reconnect: claiming interface");
                wholeUsbDevice.SetConfiguration(1);
                wholeUsbDevice.ClaimInterface(0);
            }
            else
            {
                logger.debug("Spectrometer.reconnect: WinUSB detected");
            }

            // should this be moved to AFTER we've read the EEPROM and know what
            // we need?  We would then have to handle in-flight reconnects; however,
            // this is a little fudgy right now in that it instantiates endpoint 6
            // on spectrometers which may not actually have an endpoint 6.  It seems
            // to be okay, as long as we don't actually use the extra endpoint for
            // anything (including ReadFlush).
            logger.debug("Spectrometer.reconnect: creating readers");
            spectralReader82 = usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
            spectralReader86 = usbDevice.OpenEndpointReader(ReadEndpointID.Ep06);

            logger.debug("Spectrometer.reconnect: done");
            return true;
        }

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
            int bytesToRead = Math.Max(len, fullLen);
            if (isARM || isStroker) // ARM should always read at least 8 bytes
                bytesToRead = Math.Min(8, bytesToRead);
            byte[] buf = new byte[bytesToRead];

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                DEVICE_TO_HOST, // bRequestType
                cmd[opcode],    // bRequest
                0,              // wValue
                wIndex,         // wIndex
                bytesToRead);   // wLength

            bool expectedSuccessResult = true;
            if (isARM && armInvertedRetvals.Contains(opcode))
                expectedSuccessResult = !expectedSuccessResult;

            // Question: if the device returns 6 bytes on Endpoint 0, but I only
            // need the first so pass byte[1], are the other 5 bytes discarded or
            // queued?
            lock (commsLock)
            {
                waitForUsbAvailable();
                logger.debug("getCmd: about to send {0} ({1}) with buffer length {2}", opcode.ToString(), stringifyPacket(setupPacket), buf.Length);
                bool result = usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead);

                if (result != expectedSuccessResult || bytesRead < len)
                {
                    logger.error("getCmd: failed to get {0} (0x{1:x4}) via DEVICE_TO_HOST ({2} of {3} bytes read, expected {4} got {5})",
                        opcode.ToString(), cmd[opcode], bytesRead, len, expectedSuccessResult, result);
                    return null;
                }
            }

            if (logger.debugEnabled())
                logger.hexdump(buf, String.Format("getCmd: {0} (0x{1:x2}) index 0x{2:x4} ->", opcode.ToString(), cmd[opcode], wIndex));

            // extract just the bytes we really needed
            return Util.truncateArray(buf, len);
        }

        /// <summary>
        /// Execute a request-response transfer with a "second-tier" request.
        /// </summary>
        /// <param name="opcode">the wValue to send along with the "second-tier" command</param>
        /// <param name="len">how many bytes of response are expected</param>
        /// <returns>array of returned bytes (null on error)</returns>
        internal byte[] getCmd2(Opcodes opcode, int len, ushort wIndex = 0, int fakeBufferLengthARM = 0)
        {
            int bytesToRead = len;
            if (isARM || isStroker)
                bytesToRead = Math.Max(bytesToRead, fakeBufferLengthARM);

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                DEVICE_TO_HOST,                     // bRequestType
                cmd[Opcodes.SECOND_TIER_COMMAND],   // bRequest
                cmd[opcode],                        // wValue
                wIndex,                             // wIndex
                bytesToRead);                       // wLength

            byte[] buf = new byte[bytesToRead];

            bool expectedSuccessResult = true;
            if (isARM && armInvertedRetvals.Contains(opcode))
                expectedSuccessResult = !expectedSuccessResult;

            bool result = false;
            lock (commsLock)
            {
                waitForUsbAvailable();
                logger.debug("getCmd2: about to send {0} ({1}) with buffer length {2}", opcode.ToString(), stringifyPacket(setupPacket), buf.Length);
                result = usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead);

                if (result != expectedSuccessResult || bytesRead < len)
                {
                    logger.error("getCmd2: failed to get SECOND_TIER_COMMAND {0} (0x{1:x4}) via DEVICE_TO_HOST ({2} of {3} bytes read, expected {4} got {5})",
                        opcode.ToString(), cmd[opcode], bytesRead, len, expectedSuccessResult, result);
                    logger.hexdump(buf, $"{opcode} result");
                    return null;
                }
            }

            if (logger.debugEnabled())
                logger.hexdump(buf, String.Format("getCmd2: {0} (0x{1:x2}) index 0x{2:x4} (result {3}, expected {4}) ->",
                    opcode.ToString(), cmd[opcode], wIndex, result, expectedSuccessResult));

            // extract just the bytes we really needed
            return Util.truncateArray(buf, len);
        }

        /// <summary>
        /// send a single control transfer command (response not checked)
        /// </summary>
        /// <param name="opcode">the desired command</param>
        /// <param name="wValue">an optional secondary argument used by most commands</param>
        /// <param name="wIndex">an optional tertiary argument used by some commands</param>
        /// <param name="buf">a data buffer used by some commands</param>
        /// <returns>true on success, false on error</returns>
        /// <todo>should support return code checking...most cmd opcodes return a success/failure byte</todo>
        internal bool sendCmd(Opcodes opcode, ushort wValue = 0, ushort wIndex = 0, byte[] buf = null)
        {
            if ((isARM || isStroker) && buf is null)
                buf = new byte[8];

            ushort wLength = (ushort)((buf is null) ? 0 : buf.Length);

            UsbSetupPacket packet = new UsbSetupPacket(
                HOST_TO_DEVICE, // bRequestType
                cmd[opcode],    // bRequest
                wValue,         // wValue
                wIndex,         // wIndex
                wLength);       // wLength

            bool? expectedSuccessResult = true;
            if (isARM)
            {
                if (opcode != Opcodes.SECOND_TIER_COMMAND)
                    expectedSuccessResult = armInvertedRetvals.Contains(opcode);
                else
                    expectedSuccessResult = null; // no easy way to know, as we don't pass wValue as enum
            }

            lock (commsLock)
            {
                // don't enforce USB delay on laser commands...that could be dangerous
                // or on acquire commands, which would disrupt integration throwaways 
                // and soft synchronization
                if (opcode != Opcodes.SET_LASER_ENABLE && opcode != Opcodes.ACQUIRE_SPECTRUM)
                    waitForUsbAvailable();

                logger.debug("sendCmd: about to send {0} ({1}) ({2})", opcode, stringifyPacket(packet), id);

                bool result = usbDevice.ControlTransfer(ref packet, buf, wLength, out int bytesWritten);

                if (expectedSuccessResult != null && expectedSuccessResult.Value != result)
                {
                    logger.error("sendCmd: failed to send {0} (0x{1:x2}) (wValue 0x{2:x4}, wIndex 0x{3:x4}, wLength 0x{4:x4}) (received {5}, expected {6})",
                        opcode.ToString(), cmd[opcode], wValue, wIndex, wLength, result, expectedSuccessResult);
                    return false;
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // laser
        ////////////////////////////////////////////////////////////////////////

        public virtual LaserPowerResolution laserPowerResolution { get; set; } = LaserPowerResolution.LASER_POWER_RESOLUTION_1000;

        /// <param name="perc">a normalized floating-point percentage from 0.0 to 1.0 (100%)</param>
        /// <remarks>
        /// Not implemented as a property because it truly isn't; it's a complex 
        /// combination of actual spectrometer properties.
        ///
        /// Technically you don't need to call this function at all, and can set 
        /// the laserModulationPulseWidth, laserModulationPulsePeriod and 
        /// laserModulationEnabled properties directly if you wish.
        /// </remarks>
        public virtual bool setLaserPowerPercentage(float perc)
        {
            if (perc < 0 || perc > 1)
                return logger.error("invalid laser power percentage (should be in range (0, 1)): {0}", perc);

            UInt64 periodUS = laserModulationPeriod;
            if (laserPowerResolution == LaserPowerResolution.LASER_POWER_RESOLUTION_100)
                periodUS = 100;
            else if (laserPowerResolution == LaserPowerResolution.LASER_POWER_RESOLUTION_1000)
                periodUS = 1000;

            if (periodUS == 0)
                return logger.error("unsupported laser modulation pulse width {0}", periodUS);

            UInt64 widthUS = (UInt64)Math.Round(perc * periodUS, 0);

            // Turn off modulation at full laser power, exit
            if (widthUS >= periodUS)
            {
                logger.debug("Turning off laser modulation (full power)");
                laserModulationEnabled = false;
                return true;
            }

            if (laserModulationPeriod != periodUS)
                laserModulationPeriod = periodUS;
            UInt64 actualPeriodUS = laserModulationPeriod;
            if (actualPeriodUS != periodUS) // double-check
                logger.error("Set laserModulationPeriod {0}, but actual value {1}", periodUS, actualPeriodUS);

            if (laserModulationPulseWidth != widthUS)
                laserModulationPulseWidth = widthUS;
            UInt64 actualWidthUS = laserModulationPulseWidth;
            if (actualWidthUS != widthUS) // double-check
                logger.error("Set laserModulationPulseWidth {0}, but actual value {1}", widthUS, actualWidthUS);

            if (!laserModulationEnabled)
                laserModulationEnabled = true;

            logger.debug("Laser power set to: {0:f2}% ({1} / {2})", 
                (float)(100.0 * widthUS / periodUS), 
                widthUS, 
                periodUS);
            return true;
        }

        public ushort getDAC_UNUSED() { return Unpack.toUshort(getCmd(Opcodes.GET_DETECTOR_TEC_SETPOINT, 2, 1)); }

        // this is not a Property because it has no value and cannot be undone
        public bool setDFUMode()
        {
            if (!isARM)
                return logger.error("setDFUMode only applicable to ARM-based spectrometers (not {0})", featureIdentification.boardType);

            logger.info("Setting DFU mode");
            return sendCmd(Opcodes.SET_DFU_MODE);
        }

        // this is not a Property because it has no value and cannot be undone
        public bool resetFPGA()
        {
            logger.info("Resetting FPGA");
            return sendCmd(Opcodes.FPGA_RESET);
        }

        ////////////////////////////////////////////////////////////////////////
        // getSpectrum
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// If a spectrometer has bad_pixels configured in the EEPROM, then average 
        /// over them in the driver.
        /// </summary> 
        protected void correctBadPixels(ref double[] spectrum)
        {
            if (eeprom.badPixelList.Count == 0)
                return;

            if (spectrum is null || spectrum.Length == 0)
                return;

            // iterate over each bad pixel
            int i = 0;
            while (i < eeprom.badPixelList.Count)
            {
                short badPix = eeprom.badPixelList[i];

                if (badPix == 0)
                {
                    // handle the left edge
                    short nextGood = (short)(badPix + 1);
                    while (eeprom.badPixelSet.Contains(nextGood) && nextGood < spectrum.Length)
                    {
                        nextGood++;
                        i++;
                    }
                    if (nextGood < spectrum.Length)
                        for (int j = 0; j < nextGood; j++)
                            spectrum[j] = spectrum[nextGood];
                }
                else
                {
                    // find previous good pixel
                    short prevGood = (short)(badPix - 1);
                    while (eeprom.badPixelSet.Contains(prevGood) && prevGood >= 0)
                        prevGood -= 1;

                    if (prevGood >= 0)
                    {
                        // find next good pixel
                        short nextGood = (short)(badPix + 1);
                        while (eeprom.badPixelSet.Contains(nextGood) && nextGood < spectrum.Length)
                        {
                            nextGood += 1;
                            i += 1;
                        }

                        if (nextGood < spectrum.Length)
                        {
                            // For now, draw a line between previous and next good pixels.
                            //
                            // Note that obviously this is in pixel-space, and not non-linear
                            // wavelength or wavenumber space.  It is debateable as to which
                            // would be more accurate...but the difference would only matter
                            // if we had multiple consecutative bad pixels which didn't fall
                            // on an edge of the spectrum, which should not be a common 
                            // allowed circumstance.
                            //
                            // TODO: consider some kind of curve-fit instead of this linear
                            //       interpolation (THAT could matter in non-linear space).
                            //       Note that if chosen, we'd still want that to be done
                            //       BEFORE boxcar etc.
                            double delta = spectrum[nextGood] - spectrum[prevGood];
                            int rng = nextGood - prevGood;
                            double step = delta / rng;
                            for (int j = 0; j < rng - 1; j++)
                                spectrum[prevGood + j + 1] = spectrum[prevGood] + step * (j + 1);
                        }
                        else
                        {
                            // we ran off the high end, so copy-right
                            for (short j = badPix; j < spectrum.Length; j++)
                                spectrum[j] = spectrum[prevGood];
                        }
                    }

                    // advance to next bad pixel
                    i++;
                }
            }
        }

        /// <summary>
        /// Take a single complete spectrum, including any configured scan 
        /// averaging, boxcar and dark subtraction.
        /// </summary>
        ///
        /// <param name="forceNew">not used in base class (provided for specialized subclasses)</param>
        ///
        /// <returns>The acquired spectrum as an array of doubles</returns>
        public virtual double[] getSpectrum(bool forceNew=false)
        {
            lock (acquisitionLock)
            {
                currentAcquisitionCancelled = false;

                double[] sum = getSpectrumRaw();
                if (sum is null)
                {
                    if (!currentAcquisitionCancelled && errorOnTimeout)
                        logger.error($"getSpectrum: getSpectrumRaw returned null ({id})");
                    return null;
                }
                logger.debug("getSpectrum: received {0} pixels", sum.Length);

                if (scanAveraging_ > 1)
                {
                    // logger.debug("getSpectrum: getting additional spectra for averaging");
                    for (uint i = 1; i < scanAveraging_; i++)
                    {
                        // don't send a new SW trigger if using continuous acquisition
                        double[] tmp = getSpectrumRaw(skipTrigger: scanAveragingIsContinuous);
                        if (tmp is null)
                            return null;

                        for (int px = 0; px < pixels; px++)
                            sum[px] += tmp[px];
                    }

                    for (int px = 0; px < pixels; px++)
                        sum[px] /= scanAveraging_;
                }

                correctBadPixels(ref sum);

                if (dark != null && dark.Length == sum.Length)
                    for (int px = 0; px < pixels; px++)
                        sum[px] -= dark_[px];

                // this should be enough to update the cached value
                if (readTemperatureAfterSpectrum && eeprom.hasCooling)
                    _ = detectorTemperatureDegC;

                if (boxcarHalfWidth_ > 0)
                {
                    // logger.debug("getSpectrum: returning boxcar");
                    return Util.applyBoxcar(boxcarHalfWidth_, sum);
                }
                else
                {
                    // logger.debug("getSpectrum: returning sum");
                    return sum;
                }
            }
        }

        public bool sendSWTrigger()
        {
            byte[] buf = null;
            if (isARM)
                buf = new byte[8];

            logger.debug("sending SW trigger");
            return sendCmd(Opcodes.ACQUIRE_SPECTRUM, buf: buf);
        }

        /// <summary>
        /// Generate a throwaway spectrum, such as following a change in 
        /// integration time on spectrometers requiring such.
        /// </summary>
        ///
        /// <remarks>
        /// If the caller doesn't want to block on this, they can always change
        /// integrationTimeMS within a Task.Run closure.
        /// </remarks>
        ///
        /// <todo>
        /// Consider simply disabling this feature (returning from this function)
        /// if autoTrigger is disabled or using HW triggering.  In such cases, if
        /// the user wants a throwaway, they can generate it themselves.
        /// </todo>
        void performThrowawaySpectrum()
        {
            logger.debug("generating throwaway spectrum");
            // send a trigger if getSpectrumRaw won't
            if (!autoTrigger || triggerSource_ != TRIGGER_SOURCE.INTERNAL)
                sendSWTrigger();
            getSpectrumRaw();
        }

        public void flushReaders()
        {
            foreach (UsbEndpointReader spectralReader in endpoints)
                spectralReader.ReadFlush();
        }

        /// <summary>
        /// just the bytes, ma'am
        /// </summary> 
        ///
        /// <param name="skipTrigger">
        /// allows getSpectrum to suppress SW triggers when scanAveraging, on scans after
        /// the first, if scanAveragingIsContinuous
        /// </param>
        protected virtual double[] getSpectrumRaw(bool skipTrigger=false)
        {
            logger.debug($"getSpectrumRaw: requesting spectrum {id}");
            byte[] buf = null;
            if (isARM)
                buf = new byte[8];

            // request a spectrum
            if (triggerSource_ == TRIGGER_SOURCE.INTERNAL && autoTrigger && !skipTrigger)
                sendSWTrigger();

            if (isStroker)
            {
                logger.debug("getSpectrumRaw: extra Stroker delay");
                Thread.Sleep((int)integrationTimeMS_ + 5);
            }

            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            if (useReadoutMutex)
                readoutMutex.WaitOne();

            double[] spec = new double[pixels]; // default to all zeros

            int pixelsRead = 0;
            foreach (UsbEndpointReader spectralReader in endpoints)
            {
                // read all expected pixels from the endpoint
                uint[] subspectrum = null;

                // with retry logic
                const int maxRetries = 3;
                int retries = 0;
                while (true)
                {
                    try
                    {
                        // read all expected pixels from the endpoint
                        if (isStroker && retries == 0)
                        {
                            subspectrum = readSubspectrumStroker(spectralReader, pixelsPerEndpoint);
                            if (areaScanEnabled)
                                pixelsPerEndpoint *= LEGACY_VERTICAL_PIXELS;
                            spec = new double[pixelsPerEndpoint];
                        }
                        else
                            subspectrum = readSubspectrum(spectralReader, pixelsPerEndpoint);
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.error($"{id} Caught exception in WasatchNET.Spectrometer.getSpectrumRaw: {ex}");
                        retries++;
                        if (retries >= maxRetries)
                        {
                            logger.error($"giving up after {retries} retries");
                            if (useReadoutMutex)
                                readoutMutex.ReleaseMutex();
                            return null;
                        }

                        logger.error("reconnecting");
                        var ok = reconnect();
                        if (ok)
                        {
                            logger.error("reconnection succeeded, retrying read");
                            continue;
                        }
                        else
                        {
                            logger.error("reconnection failed, giving up");
                            if (useReadoutMutex)
                                readoutMutex.ReleaseMutex();
                            return null;
                        }
                    }
                }

                // verify that exactly the number expected were received
                if (subspectrum is null || subspectrum.Length != pixelsPerEndpoint)
                {
                    if (!currentAcquisitionCancelled && errorOnTimeout)
                        logger.error($"failed when reading subspectrum from 0x{spectralReader.EpNum:x2} ({id})");
                    Thread.Sleep(delayAfterBulkEndpointErrorMS);
                    if (isStroker && areaScanEnabled)
                        pixelsPerEndpoint /= LEGACY_VERTICAL_PIXELS;
                    if (useReadoutMutex)
                        readoutMutex.ReleaseMutex();
                    return null;
                }

                // append while converting to double
                for (int i = 0; i < pixelsPerEndpoint; i++)
                    spec[i + pixelsRead] = subspectrum[i];

                pixelsRead += pixelsPerEndpoint;
            }

            // release the mutex...other spectrometers can proceed with their reads
            if (useReadoutMutex)
                readoutMutex.ReleaseMutex();

            if (isStroker && areaScanEnabled)
                pixelsPerEndpoint /= LEGACY_VERTICAL_PIXELS;

            if (hasMarker)
            {
                shiftedMarkerCount = 0;
                if (spec[0] != SPECTRUM_START_MARKER)
                    logger.error($"MARKER: first pixel does not match marker ({id})");

                for (int i = 1; i < spec.Length; i++)
                {
                    if (spec[i] == SPECTRUM_START_MARKER)
                    {
                        logger.error($"MARKER found at pixel {i} ({id})");
                        shiftedMarkerCount++;
                    }
                }

                // regardless, overwrite the marker now that we've processed it
                spec[0] = spec[1];
            }

            if (eeprom.featureMask.invertXAxis)
                Array.Reverse(spec);

            if (isSiG)
            {
                // overwrite last pixel
                spec[pixels - 1] = spec[pixels - 2];
            }

            if (eeprom.featureMask.bin2x2)
            {
                var smoothed = new double[spec.Length];
                for (int i = 0; i < spec.Length - 1; i++)
                    smoothed[i] = (spec[i] + spec[i + 1]) / 2.0;
                smoothed[spec.Length - 1] = spec[spec.Length - 1];
                spec = smoothed;
            }

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);

            // logger.debug("getSpectrumRaw({0}): {1}", id, string.Join<double>(", ", spec));

            lastSpectrum = spec;
            return spec;
        }

        public virtual ushort[] getFrame()
        {
            return null;
        }

        uint[] readImage(UsbEndpointReader spectralReader, int pixelsPerEndpoint)
        {
            ////////////////////////////////////////////////////////////////////
            // Read all the expected bytes.  Don't mess with demarshalling into
            // pixels yet, because we might get them in odd-sized batches.
            ////////////////////////////////////////////////////////////////////

            int chunk_size = pixelsPerEndpoint / 2;
            int maxLines = 150;

            int bytesPerEndpoint = pixelsPerEndpoint * 2 * maxLines;
            bool triggerWasExternal = triggerSource == TRIGGER_SOURCE.EXTERNAL;

            byte[] subspectrumBytes = new byte[bytesPerEndpoint];  // initialize to zeros

            int bytesReadThisEndpoint = 0;
            int bytesRemainingToRead = bytesPerEndpoint;
            while (bytesReadThisEndpoint < bytesPerEndpoint)
            {
                // compute this inside the loop, just in case (if doing external
                // triggering), someone changes integration time during trigger wait
                int timeoutMS = generateTimeoutMS();

                // read the next block of data
                ErrorCode err = new ErrorCode();
                int bytesRead = 0;
                try
                {
                    int bytesToRead = chunk_size - bytesReadThisEndpoint;
                    logger.debug("readImage: attempting to read {0} bytes of spectrum from endpoint {1} with timeout {2}ms", bytesToRead, spectralReader, timeoutMS);
                    err = spectralReader.Read(subspectrumBytes, bytesReadThisEndpoint, chunk_size, timeoutMS, out bytesRead);
                    logger.debug("readImage: read {0} bytes of spectrum from endpoint {1} (ErrorCode {2})", bytesRead, spectralReader, err.ToString());
                }
                catch (Exception ex)
                {
                    logger.error("readImage: caught exception reading endpoint: {0}", ex.Message);
                    return null; 
                }

                bytesReadThisEndpoint += bytesRead;
                logger.debug("readImage: bytesReadThisEndpoint now {0}", bytesReadThisEndpoint);

                if (bytesReadThisEndpoint == 0 && !triggerWasExternal)
                {
                    logger.error("readImage: read nothing (timeout?)");
                    return null; 
                }

                if (bytesReadThisEndpoint > bytesPerEndpoint)
                {
                    logger.error("readImage: read too many bytes on endpoint {0} (read {1} of expected {2})", spectralReader, bytesReadThisEndpoint, bytesPerEndpoint);
                    break;
                }

                if (triggerWasExternal && triggerSource != TRIGGER_SOURCE.EXTERNAL)
                {
                    // need to do this so software can send an ACQUIRE command, else we'll
                    // loop forever
                    logger.debug("triggering switched from external to internal...resetting");
                    return null; 
                }

                if (triggerSource == TRIGGER_SOURCE.EXTERNAL && !shuttingDown)
                {
                    // we don't know how long we'll have to wait for the trigger, so just loop and hope
                    // (should probably only catch Timeout exceptions...)
                    logger.debug("readImage: still waiting for external trigger");
                }
            }

            ////////////////////////////////////////////////////////////////////
            // To get here, we should have exactly the expected number of bytes
            ////////////////////////////////////////////////////////////////////

            // demarshall into pixels
            uint[] subspectrum = new uint[pixelsPerEndpoint];
            for (int i = 0; i < pixelsPerEndpoint; i++)
                subspectrum[i] = (uint)(subspectrumBytes[i * 2] | (subspectrumBytes[i * 2 + 1] << 8));  // LSB-MSB

            logger.debug("readImage: returning subspectrum");
            return subspectrum;
        }

        uint[] readSubspectrumStroker(UsbEndpointReader spectralReader, int pixelsPerEndpoint)
        {
            ////////////////////////////////////////////////////////////////////
            // Read all the expected bytes.  Don't mess with demarshalling into
            // pixels yet, because we might get them in odd-sized batches.
            ////////////////////////////////////////////////////////////////////

            int chunk_size = pixelsPerEndpoint / 2;

            int maxLines = LEGACY_VERTICAL_PIXELS;

            int bytesPerEndpoint;
            if (!areaScanEnabled)
                bytesPerEndpoint = pixelsPerEndpoint * 2;
            else
            {
                bytesPerEndpoint = pixelsPerEndpoint * 2 * maxLines;
                pixelsPerEndpoint *= maxLines;
            }

            chunk_size = bytesPerEndpoint;

            bool triggerWasExternal = triggerSource == TRIGGER_SOURCE.EXTERNAL;

            byte[] subspectrumBytes = new byte[bytesPerEndpoint];  // initialize to zeros

            int bytesReadThisEndpoint = 0;
            int bytesRemainingToRead = bytesPerEndpoint;
            while (bytesReadThisEndpoint < bytesPerEndpoint)
            {
                // compute this inside the loop, just in case (if doing external
                // triggering), someone changes integration time during trigger wait
                int timeoutMS = generateTimeoutMS();

                // read the next block of data
                ErrorCode err = new ErrorCode();
                int bytesRead = 0;
                try
                {
                    int bytesToRead = chunk_size - bytesReadThisEndpoint;
                    logger.debug("readSubspectrumStroker: attempting to read {0} bytes of spectrum from endpoint {1} with timeout {2}ms", bytesToRead, spectralReader, timeoutMS);
                    err = spectralReader.Read(subspectrumBytes, bytesReadThisEndpoint, chunk_size, timeoutMS, out bytesRead);
                    logger.debug("readSubspectrumStroker: read {0} bytes of spectrum from endpoint {1} (ErrorCode {2})", bytesRead, spectralReader, err.ToString());
                }
                catch (Exception ex)
                {
                    logger.error("readSubspectrumStroker: caught exception reading endpoint: {0}", ex.Message);
                    return null; //  break;  // should be return null;
                }

                bytesReadThisEndpoint += bytesRead;
                logger.debug("readSubspectrumStroker: bytesReadThisEndpoint now {0}", bytesReadThisEndpoint);

                if (bytesReadThisEndpoint == 0 && !triggerWasExternal)
                {
                    logger.error("readSubspectrumStroker: read nothing (timeout?)");
                    return null;
                }

                if (bytesReadThisEndpoint > bytesPerEndpoint)
                {
                    logger.error("readSubspectrumStroker: read too many bytes on endpoint {0} (read {1} of expected {2})", spectralReader, bytesReadThisEndpoint, bytesPerEndpoint);
                    break;
                }

                if (triggerWasExternal && triggerSource != TRIGGER_SOURCE.EXTERNAL)
                {
                    // need to do this so software can send an ACQUIRE command, else we'll
                    // loop forever
                    logger.debug("readSubspectrumStroker: triggering switched from external to internal...resetting");
                    return null; 
                }

                if (triggerSource == TRIGGER_SOURCE.EXTERNAL && !shuttingDown)
                {
                    // we don't know how long we'll have to wait for the trigger, so just loop and hope
                    // (should probably only catch Timeout exceptions...)
                    logger.debug("readSubspectrumStroker: still waiting for external trigger");
                }
            }

            ////////////////////////////////////////////////////////////////////
            // To get here, we should have exactly the expected number of bytes
            ////////////////////////////////////////////////////////////////////

            // demarshall into pixels
            uint[] subspectrum = new uint[pixelsPerEndpoint];
            for (int i = 0; i < pixelsPerEndpoint; i++)
                subspectrum[i] = (uint)(subspectrumBytes[i * 2] | (subspectrumBytes[i * 2 + 1] << 8));  // LSB-MSB

            logger.debug("readSubspectrumStroker: returning subspectrum");
            return subspectrum;
        }

        int generateTimeoutMS()
        {
            ////////////////////////////////////////////////////////////////////
            // if an explicit timeout was provided, use that
            ////////////////////////////////////////////////////////////////////

            if (acquisitionTimeoutMS != null)
                return (int)acquisitionTimeoutMS;
            else if (acquisitionTimeoutTimestamp != null)
            {
                var now = DateTime.Now;
                DateTime then = (DateTime)acquisitionTimeoutTimestamp;
                if (acquisitionTimeoutTimestamp > now)
                    return (int)(then - now).TotalMilliseconds;
            }

            ////////////////////////////////////////////////////////////////////
            // compute a default timeout
            ////////////////////////////////////////////////////////////////////

            // give SiG more time, as it may need to do 6 internal throwaway
            // if it's coming back from a power-save mode
            int acquisitions = isSiG ? 8 : 2;

            // give more time if we have lots of connected spectrometers, as 
            // USB is ultimately serial
            const int windowMS = 500;

            return (int)(integrationTimeMS_ * acquisitions +
                         windowMS * Driver.getInstance().getNumberOfSpectrometers());
        }

        // Given one endpoint (e.g. Ep02 or Ep06), read exactly the number of pixels 
        // expected on the endpoint.  Try to do it in one go, but loop around if
        // it comes out in chunks.  Log an error and return NULL if anything goes
        // wrong (timeout in non-triggering context, or reading too many bytes).
        //
        // MZ: I don't like that "pixelsPerEndpoint" (which is already a Spectrometer
        //     attribute) is here being "shadowed" (overwritten) by a local parameter
        //     name.  Recommend picking a different parameter name.
        uint[] readSubspectrum(UsbEndpointReader spectralReader, int pixelsPerEndpoint)
        {
            ////////////////////////////////////////////////////////////////////
            // Read all the expected bytes.  Don't mess with demarshalling into
            // pixels yet, because we might get them in odd-sized batches.
            ////////////////////////////////////////////////////////////////////

            int bytesPerEndpoint = pixelsPerEndpoint * 2;
            bool triggerWasExternal = triggerSource == TRIGGER_SOURCE.EXTERNAL;

            byte[] subspectrumBytes = new byte[bytesPerEndpoint];  // initialize to zeros

            int bytesReadThisEndpoint = 0;
            int bytesRemainingToRead = bytesPerEndpoint;

            while (bytesReadThisEndpoint < bytesPerEndpoint)
            {
                // compute this inside the loop, just in case (if doing external
                // triggering), someone changes integration time during trigger wait
                int timeoutMS = generateTimeoutMS();

                // read the next block of data
                ErrorCode err = new ErrorCode();
                int bytesRead = 0;
                try
                {
                    int bytesToRead = bytesPerEndpoint - bytesReadThisEndpoint;
                    logger.debug($"readSubspectrum: attempting to read {bytesToRead} bytes of spectrum from endpoint 0x{spectralReader.EpNum:x2} with timeout {timeoutMS}ms ({id})");
                    err = spectralReader.Read(subspectrumBytes, bytesReadThisEndpoint, bytesPerEndpoint - bytesReadThisEndpoint, timeoutMS, out bytesRead);
                    logger.debug($"readSubspectrum: read {bytesRead} bytes of spectrum from endpoint 0x{spectralReader.EpNum:x2} ({err}) ({id})");
                }
                catch (Exception ex)
                {
                    logger.error("readSubspectrum: caught exception reading endpoint ({id}): {0}", ex.Message);
                    return null; 
                }

                bytesReadThisEndpoint += bytesRead;
                logger.debug($"readSubspectrum: bytesReadThisEndpoint now {bytesReadThisEndpoint} ({id})");
                if (bytesReadThisEndpoint == bytesPerEndpoint)
                    break;

                if (bytesReadThisEndpoint == 0 && !triggerWasExternal)
                {
                    logger.error($"readSubspectrum: read nothing (timeout?) ({id})");
                    return null; 
                }

                if (bytesReadThisEndpoint > bytesPerEndpoint)
                {
                    logger.error($"readSubspectrum: read too many bytes on endpoint 0x{spectralReader.EpNum:x2} (read {bytesReadThisEndpoint} of expected {bytesPerEndpoint}) ({id})");
                    break; 
                }

                if (triggerWasExternal && triggerSource != TRIGGER_SOURCE.EXTERNAL)
                {
                    // need to do this so software can send an ACQUIRE command, else we'll
                    // loop forever
                    logger.debug($"triggering switched from external to internal...resetting ({id})");
                    return null; 
                }

                if (currentAcquisitionCancelled)
                {
                    logger.debug("readSubspectrum: current acquisition cancelled");
                    return null;
                }

                if (triggerSource == TRIGGER_SOURCE.EXTERNAL && !shuttingDown)
                {
                    // Note that we may fall down this path following a SW-triggered
                    // throwaway initiated by integration time change, even if the overall
                    // triggering strategy is external.  In that case the following error
                    // is misleading (it wasn't externally-triggered) but valid (it did
                    // timeout).

                    // if we were given an explicit timeout, give up
                    if (acquisitionTimeoutMS != null || acquisitionTimeoutTimestamp != null)
                    {
                        if (errorOnTimeout)
                            logger.error("failed to receive externally-triggered spectrum within explicit timeout");
                        acquisitionTimeoutTimestamp = null;
                        return null;
                    }
                    else
                    {
                        logger.debug($"readSubspectrum: still waiting for external trigger ({id})");
                    }
                }
            }

            ////////////////////////////////////////////////////////////////////
            // To get here, we should have exactly the expected number of bytes
            ////////////////////////////////////////////////////////////////////

            // demarshall into pixels
            uint[] subspectrum = new uint[pixelsPerEndpoint];
            for (int i = 0; i < pixelsPerEndpoint; i++)
                subspectrum[i] = (uint)(subspectrumBytes[i * 2] | (subspectrumBytes[i * 2 + 1] << 8));  // LSB-MSB

            logger.debug("readSubspectrum: returning subspectrum");
            return subspectrum;
        }

        public void cancelCurrentAcquisition() => currentAcquisitionCancelled = true;
        protected bool currentAcquisitionCancelled;
    }
}
