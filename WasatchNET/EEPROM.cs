using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace WasatchNET
{
    /// <summary>
    /// Encapsulates access to the spectrometer's writable but non-volatile EEPROM.
    /// </summary>
    /// <remarks>
    /// While users are freely encouraged to read and parse EEPROM contents, they
    /// are STRONGLY ADVISED to exercise GREAT CAUTION in changing or writing 
    /// EEPROM values. It is entirely possible that an erroneous or corrupted 
    /// write operation could "brick" your spectrometer, requiring RMA to the 
    /// manufacturer. It is MORE likely that inappropriate values in some fields
    /// could lead to subtly malformed or biased spectral readings, which could
    /// taint or invalidate your measurement results.
    /// </remarks>
    /// <see cref="http://ww1.microchip.com/downloads/en/DeviceDoc/20006270A.pdf"/>
    [ComVisible(true)]
    [Guid("A224D5A7-A0E0-4AAC-8489-4BB0CED3171B")]
    [ProgId("WasatchNET.EEPROM")]
    [ClassInterface(ClassInterfaceType.None)]
    public class EEPROM : IEEPROM
    {
        /////////////////////////////////////////////////////////////////////////       
        // private attributes
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// How many pages (64-bytes each) are available to be read from the EEPROM
        /// </summary>
        /// <remarks>
        /// This has been verified to be physically 512, all of which are 
        /// accessible from existing firmware on our ARM models.  A future update
        /// to /FX2 firmware will make them available on all spectrometers.
        /// </remarks>
        internal const int MAX_PAGES = 8;
        internal const int MAX_PAGES_REAL = 512;
        internal const int MAX_NAME_LEN = 16;
        internal const int MAX_LIB_ENTRIES = 8;
        internal const int LIBRARY_SIZE_PIXELS = 1952;

        internal const int LIBRARY_START_PAGE = 10;
        internal const int LIBRARY_STOP_PAGE = 506;

        /// <summary>
        /// Current EEPROM format
        /// </summary>
        /// <remarks>
        /// - rev 14
        ///     - adds SiG laser TEC and Has interlock feedback to feature mask
        /// </remarks>
        protected const byte FORMAT = 14;

        protected Spectrometer spectrometer;
        protected Logger logger = Logger.getInstance();

        public List<byte[]> pages { get; protected set; }
        public event EventHandler EEPROMChanged;
        public enum PAGE_SUBFORMAT { USER_DATA, INTENSITY_CALIBRATION, WAVECAL_SPLINES, UNTETHERED_DEVICE, DETECTOR_REGIONS };
        protected virtual void OnEEPROMChanged(EventArgs e)
        {
            EEPROMChanged?.Invoke(this, e);
        }

        /////////////////////////////////////////////////////////////////////////       
        //
        // public attributes
        //
        /////////////////////////////////////////////////////////////////////////       

        public bool defaultValues;

        /////////////////////////////////////////////////////////////////////////       
        // Page 0 
        /////////////////////////////////////////////////////////////////////////       

        public byte format
        {
            get { return _format; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _format = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        byte _format;
        /// <summary>spectrometer model</summary>
        public string model
        {
            get { return _model; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _model = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        string _model;
        /// <summary>spectrometer serialNumber</summary>
        public string serialNumber
        {
            get { return _serialNumber; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _serialNumber = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        string _serialNumber;
        /// <summary>baud rate (bits/sec) for serial communications</summary>
        public uint baudRate
        {
            get { return _baudRate; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _baudRate = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        uint _baudRate;
        /// <summary>whether the spectrometer has an on-board TEC for cooling the detector</summary>
        public bool hasCooling
        {
            get { return _hasCooling; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _hasCooling = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        bool _hasCooling;
        /// <summary>whether the spectrometer has an on-board battery</summary>
        public bool hasBattery
        {
            get { return _hasBattery; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _hasBattery = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        bool _hasBattery;
        /// <summary>whether the spectrometer has an integrated laser</summary>
        public bool hasLaser
        {
            get { return _hasLaser; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _hasLaser = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        bool _hasLaser;
        /// <summary>the integral center wavelength of the laser in nanometers, if present</summary>
        /// <remarks>user-writable</remarks>
        /// <see cref="Util.wavelengthsToWavenumbers(double, double[])"/>
        public ushort excitationNM
        {
            get { return _excitationNM; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _excitationNM = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        ushort _excitationNM;
        /// <summary>the slit width in µm</summary>
        public ushort slitSizeUM
        {
            get { return _slitSizeUM; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _slitSizeUM = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        ushort _slitSizeUM;
        // these will come with ENG-0034 Rev 4
        public ushort startupIntegrationTimeMS
        {
            get { return _startupIntegrationTimeMS; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _startupIntegrationTimeMS = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        ushort _startupIntegrationTimeMS;
        public short startupDetectorTemperatureDegC
        {
            get { return _startupDetectorTemperatureDegC; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _startupDetectorTemperatureDegC = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        short _startupDetectorTemperatureDegC;
        public byte startupTriggeringMode
        {
            get { return _startupTriggeringMode; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _startupTriggeringMode = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        byte _startupTriggeringMode;
        public float detectorGain
        {
            get { return _detectorGain; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _detectorGain = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        float _detectorGain;
        public short detectorOffset
        {
            get { return _detectorOffset; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _detectorOffset = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        short _detectorOffset;
        public float detectorGainOdd
        {
            get { return _detectorGainOdd; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _detectorGainOdd = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        float _detectorGainOdd;
        public short detectorOffsetOdd
        {
            get { return _detectorOffsetOdd; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _detectorOffsetOdd = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        short _detectorOffsetOdd;

        public FeatureMask featureMask = new FeatureMask();

        /////////////////////////////////////////////////////////////////////////       
        // Page 1
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>coefficients of a 3rd-order polynomial representing the configured wavelength calibration</summary>
        /// <remarks>
        /// These are automatically expanded into an accessible array in 
        /// Spectrometer.wavelengths.  Also see Util.generateWavelengths() for 
        /// the process of expanding the polynomial.
        ///
        /// user-writable
        ///
        /// MZ: I think the current logic in the setter which invokes the 
        ///     EEPROMChanged event has a big hole: namely, that the getter
        ///     returns a reference to the entire array object.  That means
        ///     the caller (customer software) can change the values of individual
        ///     elements within the array at-will, and neither the setter nor
        ///     event will ever fire.  I'm not sure that's entirely a bug,
        ///     but wanted to document it.
        /// </remarks>
        /// <see cref="Spectrometer.wavelengths"/>
        /// <see cref="Util.generateWavelengths(uint, float[])"/>
        public float[] wavecalCoeffs
        {
            get { return _wavecalCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                if (wavecalCoeffs is null || value.Length == wavecalCoeffs.Length)
                    _wavecalCoeffs = value;
                else
                {
                    int index = 0;
                    while (index < value.Length)
                    {
                        _wavecalCoeffs[index] = value[index];
                        ++index;
                    }
                    while (index < wavecalCoeffs.Length)
                    {
                        _wavecalCoeffs[index] = 0;
                        ++index;
                    }
                }
                handler?.Invoke(this, new EventArgs());

            }
        }

        float[] _wavecalCoeffs;

        /// <summary>
        /// These are used to convert the user's desired setpoint in degrees 
        /// Celsius to raw 12-bit DAC inputs for passing to the detector's 
        /// Thermo-Electric Cooler (TEC).
        /// </summary>
        /// <remarks>
        /// These correspond to the fields "Temp to TEC Cal" in Wasatch Model Configuration GUI.
        ///
        /// Use these when setting the TEC setpoint.
        ///
        /// Note that the TEC is a "write-only" device: you can tell it what temperature you
        /// WANT, but you can't read what temperature it IS.  (For that, use the thermistor.)
        /// </remarks>
        public float[] degCToDACCoeffs
        {
            get { return _degCToDACCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _degCToDACCoeffs = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        float[] _degCToDACCoeffs;
        public short detectorTempMin
        {
            get { return _detectorTempMin; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _detectorTempMin = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        short _detectorTempMin;
        public short detectorTempMax
        {
            get { return _detectorTempMax; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _detectorTempMax = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        short _detectorTempMax;
        /// <summary>
        /// These are used to convert 12-bit raw ADC temperature readings from the detector
        /// thermistor into degrees Celsius.
        /// </summary>
        /// <remarks>
        /// These correspond to the fields "Therm to Temp Cal" in Wasatch Model Configuration GUI.
        /// 
        /// Use these when reading the detector temperature.
        ///
        /// Note that the detector thermistor is a read-only device: you can read what temperature
        /// it IS, but you can't tell it what temperature you WANT.  (For that, use the TEC.)
        ///
        /// Note that there is also a thermistor on the laser.  These calibrated coefficients 
        /// are for the detector thermistor; the laser thermistor uses hard-coded coefficients
        /// which aren't calibrated or stored on the EEPROM.
        /// </remarks>
        public float[] adcToDegCCoeffs
        {
            get { return _adcToDegCCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _adcToDegCCoeffs = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        float[] _adcToDegCCoeffs;
        public short thermistorResistanceAt298K
        {
            get { return _thermistorResistanceAt298K; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _thermistorResistanceAt298K = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        short _thermistorResistanceAt298K;
        public short thermistorBeta
        {
            get { return _thermistorBeta; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _thermistorBeta = value;
                handler?.Invoke(this, new EventArgs());

            }
        }

        short _thermistorBeta;
        /// <summary>when the unit was last calibrated (unstructured 12-char field)</summary>
        /// <remarks>user-writable</remarks>
        public string calibrationDate
        {
            get { return _calibrationDate; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _calibrationDate = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        string _calibrationDate;
        /// <summary>whom the unit was last calibrated by (unstructured 3-char field)</summary>
        /// <remarks>user-writable</remarks>
        public string calibrationBy
        {
            get { return _calibrationBy; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _calibrationBy = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        string _calibrationBy;
        /////////////////////////////////////////////////////////////////////////       
        // Page 2
        /////////////////////////////////////////////////////////////////////////       

        public string detectorName
        {
            get { return _detectorName; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _detectorName = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        string _detectorName;
        public ushort activePixelsHoriz
        {
            get { return _activePixelsHoriz; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _activePixelsHoriz = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        ushort _activePixelsHoriz;
        public ushort activePixelsVert
        {
            get { return _activePixelsVert; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _activePixelsVert = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        ushort _activePixelsVert;
        public byte laserWarmupSec
        {
            get { return _laserWarmupSec; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _laserWarmupSec = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        byte _laserWarmupSec;

        public uint minIntegrationTimeMS
        {
            get { return _minIntegrationTimeMS; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _minIntegrationTimeMS = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        uint _minIntegrationTimeMS;
        public uint maxIntegrationTimeMS
        {
            get { return _maxIntegrationTimeMS; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _maxIntegrationTimeMS = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        uint _maxIntegrationTimeMS;
        public ushort actualPixelsHoriz
        {
            get { return _actualPixelsHoriz; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _actualPixelsHoriz = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        ushort _actualPixelsHoriz;
        // writable
        public ushort ROIHorizStart
        {
            get { return _ROIHorizStart; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _ROIHorizStart = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        ushort _ROIHorizStart;
        public ushort ROIHorizEnd
        {
            get { return _ROIHorizEnd; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _ROIHorizEnd = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        ushort _ROIHorizEnd;
        public ushort[] ROIVertRegionStart
        {
            get { return _ROIVertRegionStart; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _ROIVertRegionStart = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        ushort[] _ROIVertRegionStart;
        public ushort[] ROIVertRegionEnd
        {
            get { return _ROIVertRegionEnd; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _ROIVertRegionEnd = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        ushort[] _ROIVertRegionEnd;
        /// <summary>
        /// These are reserved for a non-linearity calibration,
        /// but may be harnessed by users for other purposes.
        /// </summary>
        /// <remarks>user-writable</remarks>
        public float[] linearityCoeffs
        {
            get { return _linearityCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _linearityCoeffs = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        float[] _linearityCoeffs;
        /////////////////////////////////////////////////////////////////////////       
        // Page 3
        /////////////////////////////////////////////////////////////////////////       

        // public int deviceLifetimeOperationMinutes { get; private set; }
        // public int laserLifetimeOperationMinutes { get; private set; }
        // public short laserTemperatureMax { get; private set; }
        // public short laserTemperatureMin { get; private set; }
        public float maxLaserPowerMW
        {
            get { return _maxLaserPowerMW; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _maxLaserPowerMW = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        float _maxLaserPowerMW;
        public float minLaserPowerMW
        {
            get { return _minLaserPowerMW; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _minLaserPowerMW = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        float _minLaserPowerMW;
        public float laserExcitationWavelengthNMFloat
        {
            get { return _laserExcitationWavelengthNMFloat; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _laserExcitationWavelengthNMFloat = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        float _laserExcitationWavelengthNMFloat;
        public float[] laserPowerCoeffs
        {
            get { return _laserPowerCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _laserPowerCoeffs = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        float[] _laserPowerCoeffs;

        public float avgResolution
        {
            get { return _avgResolution; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _avgResolution = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        float _avgResolution;
        /////////////////////////////////////////////////////////////////////////       
        // Page 4
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// 64 bytes of unstructured space which the user is free to use however
        /// they see fit.
        /// </summary>
        /// <remarks>
        /// For convenience, the same raw storage space is also accessible as a 
        /// null-terminated string via userText. 
        ///
        /// EEPROM versions prior to 4 only had 63 bytes of user data.
        /// </remarks>
        public byte[] userData
        {
            get { return _userData; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _userData = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        byte[] _userData;

        /// <summary>
        /// a stringified version of the 64-byte raw data block provided by userData
        /// </summary>
        /// <remarks>accessible as a null-terminated string via userText</remarks>
        public string userText
        {
            get
            {
                return ParseData.toString(userData);
            }

            set
            {
                EventHandler handler = EEPROMChanged;
                userData = new byte[value.Length + 1];
                for (int i = 0; i < value.Length; i++)
                    userData[i] = (byte)value[i];
                userData[userData.Length - 1] = 0x0;
                handler?.Invoke(this, new EventArgs());
            }
        }

        /////////////////////////////////////////////////////////////////////////       
        // Page 5
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// array of up to 15 "bad" (hot or dead) pixels which software may wish
        /// to skip or "average over" during spectral post-processing.
        /// </summary>
        /// <remarks>bad pixels are identified by pixel number; empty slots are indicated by -1</remarks>
        public short[] badPixels
        {
            get { return _badPixels; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _badPixels = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        short[] _badPixels;
        // read-only containers for expedited processing
        public List<short> badPixelList { get; protected set; }
        public SortedSet<short> badPixelSet { get; protected set; }

        public string productConfiguration
        {
            get { return _productConfiguration; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _productConfiguration = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        string _productConfiguration;

        public PAGE_SUBFORMAT subformat
        {
            get { return _subformat; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _subformat = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        PAGE_SUBFORMAT _subformat;

        /////////////////////////////////////////////////////////////////////////       
        // Page 6
        /////////////////////////////////////////////////////////////////////////  

        public float[] intensityCorrectionCoeffs
        {
            get { return _intensityCorrectionCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _intensityCorrectionCoeffs = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        float[] _intensityCorrectionCoeffs;

        public byte intensityCorrectionOrder
        {
            get { return _intensityCorrectionOrder; }
            set
            {
                EventHandler handler = EEPROMChanged;
                if (_intensityCorrectionOrder != value)
                {
                    float[] temp = new float[_intensityCorrectionCoeffs.Length];
                    _intensityCorrectionCoeffs.CopyTo(temp, 0);
                    _intensityCorrectionCoeffs = new float[value + 1];
                    for (int i = 0; (i < temp.Length && i < _intensityCorrectionCoeffs.Length); ++i)
                        _intensityCorrectionCoeffs[i] = temp[i];
                }
                _intensityCorrectionOrder = value;
                handler?.Invoke(this, new EventArgs());
            }
        }

        byte _intensityCorrectionOrder;

        /////////////////////////////////////////////////////////////////////////       
        // Page 6 & 7 (subformat DETECTOR_REGIONS)
        /////////////////////////////////////////////////////////////////////////  

        public float[] region1WavecalCoeffs
        {
            get { return _region1WavecalCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                if (region1WavecalCoeffs is null || value.Length == region1WavecalCoeffs.Length)
                    _region1WavecalCoeffs = value;
                else
                {
                    int index = 0;
                    while (index < value.Length)
                    {
                        _region1WavecalCoeffs[index] = value[index];
                        ++index;
                    }
                    while (index < region1WavecalCoeffs.Length)
                    {
                        _region1WavecalCoeffs[index] = 0;
                        ++index;
                    }
                }
                handler?.Invoke(this, new EventArgs());

            }
        }
        float[] _region1WavecalCoeffs;

        public float[] region2WavecalCoeffs
        {
            get { return _region2WavecalCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                if (region2WavecalCoeffs is null || value.Length == region2WavecalCoeffs.Length)
                    _region2WavecalCoeffs = value;
                else
                {
                    int index = 0;
                    while (index < value.Length)
                    {
                        _region2WavecalCoeffs[index] = value[index];
                        ++index;
                    }
                    while (index < region2WavecalCoeffs.Length)
                    {
                        _region2WavecalCoeffs[index] = 0;
                        ++index;
                    }
                }
                handler?.Invoke(this, new EventArgs());

            }
        }
        float[] _region2WavecalCoeffs;

        public float[] region3WavecalCoeffs
        {
            get { return _region3WavecalCoeffs; }
            set
            {
                EventHandler handler = EEPROMChanged;
                if (region3WavecalCoeffs is null || value.Length == region3WavecalCoeffs.Length)
                    _region3WavecalCoeffs = value;
                else
                {
                    int index = 0;
                    while (index < value.Length)
                    {
                        _region3WavecalCoeffs[index] = value[index];
                        ++index;
                    }
                    while (index < region3WavecalCoeffs.Length)
                    {
                        _region3WavecalCoeffs[index] = 0;
                        ++index;
                    }
                }
                handler?.Invoke(this, new EventArgs());

            }
        }
        float[] _region3WavecalCoeffs;

        public ushort region1HorizStart
        {
            get { return _region1HorizStart; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _region1HorizStart = value;
                handler?.Invoke(this, new EventArgs());
            }
        }
        ushort _region1HorizStart;

        public ushort region1HorizEnd
        {
            get { return _region1HorizEnd; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _region1HorizEnd = value;
                handler?.Invoke(this, new EventArgs());
            }
        }
        ushort _region1HorizEnd;

        public ushort region2HorizStart
        {
            get { return _region2HorizStart; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _region2HorizStart = value;
                handler?.Invoke(this, new EventArgs());
            }
        }
        ushort _region2HorizStart;

        public ushort region2HorizEnd
        {
            get { return _region2HorizEnd; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _region2HorizEnd = value;
                handler?.Invoke(this, new EventArgs());
            }
        }
        ushort _region2HorizEnd;
        
        public ushort region3HorizStart
        {
            get { return _region3HorizStart; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _region3HorizStart = value;
                handler?.Invoke(this, new EventArgs());
            }
        }
        ushort _region3HorizStart;

        public ushort region3HorizEnd
        {
            get { return _region3HorizEnd; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _region3HorizEnd = value;
                handler?.Invoke(this, new EventArgs());
            }
        }
        ushort _region3HorizEnd;
        
        public ushort region3VertStart
        {
            get { return _region3VertStart; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _region3VertStart = value;
                handler?.Invoke(this, new EventArgs());
            }
        }
        ushort _region3VertStart;

        public ushort region3VertEnd
        {
            get { return _region3VertEnd; }
            set
            {
                EventHandler handler = EEPROMChanged;
                _region3VertEnd = value;
                handler?.Invoke(this, new EventArgs());
            }
        }
        ushort _region3VertEnd;

        public byte regionCount
        {
            get { return _regionCount; }
            set
            {
                _regionCount = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _regionCount;

        /////////////////////////////////////////////////////////////////////////       
        // Page 7 Handheld Devices
        /////////////////////////////////////////////////////////////////////////  

        public byte libraryType
        {
            get { return _libraryType; }
            set
            {
                _libraryType = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _libraryType;

        public UInt16 libraryID
        {
            get { return _libraryID; }
            set
            {
                _libraryID = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        UInt16 _libraryID;

        public byte startupScansToAverage
        {
            get { return _startupScansToAverage; }
            set
            {
                _startupScansToAverage = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _startupScansToAverage;

        public byte matchingMinRampPixels
        {
            get { return _matchingMinRampPixels; }            
            set
            {
                _matchingMinRampPixels = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _matchingMinRampPixels = 10;

        public UInt16 matchingMinPeakHeight
        {
            get { return _matchingMinPeakHeight; }
            set
            {
                _matchingMinPeakHeight = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        UInt16 _matchingMinPeakHeight = 500;

        public byte matchingThreshold
        {
            get { return _matchingThreshold; }            
            set
            {
                _matchingThreshold = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _matchingThreshold = 90;

        public byte throwAwayCount
        {
            get { return _throwAwayCount; }
            set
            {
                _throwAwayCount = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _throwAwayCount;

        public byte untetheredFeatureMask
        {
            get { return _untetheredFeatureMask;  }
            set
            {
                _untetheredFeatureMask = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _untetheredFeatureMask;

        public byte librarySpectraNum
        {
            get { return _librarySpectraNum; }
            set
            {
                _librarySpectraNum = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _librarySpectraNum = 1;

        /////////////////////////////////////////////////////////////////////////
        // Pages 10-73 (subformat UNTETHERED_DEVICE)
        /////////////////////////////////////////////////////////////////////////

        public List<UInt16> librarySpectrum
        {
            get { return _librarySpectrum; }
            set
            {
                _librarySpectrum = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        List<UInt16> _librarySpectrum;

        /////////////////////////////////////////////////////////////////////////
        // Compound Fields
        /////////////////////////////////////////////////////////////////////////

        public string fullModel
        {
            get { return _model + _productConfiguration; }
            set
            {
                if (value.Length > 16)
                {
                    model = value.Substring(0, 16);
                    productConfiguration = value.Substring(16);
                }

                else
                    model = value;
            }
        }

        public List<string> libNames
        {
            get { return _libNames; }
            set
            {
                _libNames = value;
                EEPROMChanged?.Invoke(this, new EventArgs());
            }
        }
        List<string> _libNames;

        /////////////////////////////////////////////////////////////////////////       
        //
        // public methods
        //
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// Save updated EEPROM fields to the device.
        /// </summary>
        ///
        /// <remarks>
        /// Only a handful of fields are recommended to be changed by users: 
        ///
        /// - excitationNM
        /// - wavecalCoeffs
        /// - calibrationDate
        /// - calibrationBy
        /// - ROI
        /// - linearityCoeffs (not currently used)
        /// - userData
        /// - badPixels
        /// 
        /// Note that the EEPROM isn't an SSD...it's not terribly fast, and there
        /// are a finite number of lifetime writes, so use sparingly.
        ///
        /// Due to the high risk of bricking a unit through a failed / bad EEPROM
        /// write, all internal calls bail at the first error in hopes of salvaging
        /// the unit if at all possible.
        ///
        /// That said, if you do frag your EEPROM, Wasatch has a "Model Configuration"
        /// utility to let you manually write EEPROM fields; contact your sales rep
        /// for a copy.
        /// </remarks>
        ///
        /// <todo>
        /// add an optional "indexes" argument which, if provided, only 
        /// writes the requested page numbers.
        /// </todo>
        ///
        /// <returns>true on success, false on failure</returns>
        public virtual bool write(bool allPages=false)
        {
            ////////////////////////////////////////////////////////////////
            //                                                            //
            //            "Regular" USB Wasatch Spectrometers             //
            //                                                            //
            ////////////////////////////////////////////////////////////////

            if (pages is null || pages.Count < MAX_PAGES)
            {
                logger.error("EEPROM.write: need to perform a read first");
                return false;
            }

            if (!writeParse())
                return false;

            int pagesToWrite = allPages ? pages.Count : MAX_PAGES;
            for (short page = 0; page < pagesToWrite; page++)
            {
                bool ok = false;
                if (spectrometer.isARM)
                {
                    logger.hexdump(pages[page], String.Format("writing page {0} [ARM]: ", page));
                    ok = spectrometer.sendCmd(
                        opcode: Opcodes.SECOND_TIER_COMMAND,
                        wValue: spectrometer.cmd[Opcodes.SET_MODEL_CONFIG_ARM],
                        wIndex: (ushort)page,
                        buf: pages[page]);
                }
                else
                {
                    const uint DATA_START = 0x3c00; // from Wasatch Stroker Console's EnhancedStroker.SetModelInformation()
                    ushort pageOffset = (ushort)(DATA_START + page * 64);
                    logger.hexdump(pages[page], String.Format("writing page {0} to offset {1} [FX2]: ", page, pageOffset));
                    ok = spectrometer.sendCmd(
                        opcode: Opcodes.SET_MODEL_CONFIG_FX2,
                        wValue: pageOffset,
                        wIndex: 0,
                        buf: pages[page]);
                }
                if (!ok)
                {
                    logger.error("EEPROM.write: failed to save page {0}", page);
                    return false;
                }
                logger.debug("EEPROM: wrote EEPROM page {0}", page);
            }
            defaultValues = false;
            return true;
        }

        /////////////////////////////////////////////////////////////////////////       
        // private methods
        /////////////////////////////////////////////////////////////////////////       

        internal EEPROM(Spectrometer spec)
        {
            spectrometer = spec;

            defaultValues = false;

            wavecalCoeffs = new float[5];
            degCToDACCoeffs = new float[3];
            adcToDegCCoeffs = new float[3];
            ROIVertRegionStart = new ushort[3];
            ROIVertRegionEnd = new ushort[3];
            badPixels = new short[15];
            linearityCoeffs = new float[5];
            laserPowerCoeffs = new float[4];
            intensityCorrectionCoeffs = new float[12];

            badPixelList = new List<short>();
            badPixelSet = new SortedSet<short>();
        }

        public virtual bool read()
        {
            ////////////////////////////////////////////////////////////////
            //                                                            //
            //            "Regular" USB Wasatch Spectrometers             //
            //                                                            //
            ////////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////
            // read all (standard) pages into cache
            ////////////////////////////////////////////////////////////////

            pages = new List<byte[]>();
            for (ushort page = 0; page < MAX_PAGES; page++)
            {
                byte[] buf = spectrometer.getCmd2(Opcodes.GET_MODEL_CONFIG, 64, wIndex: page, fakeBufferLengthARM: 8);
                if (buf is null)
                {
                    try
                    {
                        setDefault(spectrometer);
                    }
                    catch
                    {
                        return false;
                    }
                    return true;
                }
                pages.Add(buf);
                logger.hexdump(buf, String.Format("read page {0}: ", page));
            }

            ////////////////////////////////////////////////////////////////
            // determine format and subformat early
            ////////////////////////////////////////////////////////////////

            format = pages[0][63];
            if (format >= 8)
                subformat = (PAGE_SUBFORMAT)ParseData.toUInt8(pages[5], 63);
            else if (format >= 6)
                subformat = PAGE_SUBFORMAT.INTENSITY_CALIBRATION;
            else
                subformat = PAGE_SUBFORMAT.USER_DATA;

            ////////////////////////////////////////////////////////////////
            // newer formats support more pages
            ////////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////
            // parse pages according to format and subformat
            ////////////////////////////////////////////////////////////////

            if (subformat == PAGE_SUBFORMAT.UNTETHERED_DEVICE)
            {
                // read pages 8-73 (no need to do all MAX_PAGES_REAL)
                for (ushort page = MAX_PAGES; page <= LIBRARY_STOP_PAGE; page++)
                {
                    byte[] buf = spectrometer.getCmd2(Opcodes.GET_MODEL_CONFIG, 64, wIndex: page, fakeBufferLengthARM: 8);
                    pages.Add(buf);
                    logger.hexdump(buf, String.Format("read extra page {0}: ", page));
                }
            }

            try
            {
                model = ParseData.toString(pages[0], 0, 16);
                serialNumber = ParseData.toString(pages[0], 16, 16);
                baudRate = ParseData.toUInt32(pages[0], 32);
                hasCooling = ParseData.toBool(pages[0], 36);
                hasBattery = ParseData.toBool(pages[0], 37);
                hasLaser = ParseData.toBool(pages[0], 38);
                excitationNM = ParseData.toUInt16(pages[0], 39); // for old formats, first read this as excitation
                slitSizeUM = ParseData.toUInt16(pages[0], 41);

                startupIntegrationTimeMS = ParseData.toUInt16(pages[0], 43);
                startupDetectorTemperatureDegC = ParseData.toInt16(pages[0], 45);
                startupTriggeringMode = ParseData.toUInt8(pages[0], 47);
                detectorGain = ParseData.toFloat(pages[0], 48); // "even pixels" for InGaAs
                detectorOffset = ParseData.toInt16(pages[0], 52); // "even pixels" for InGaAs
                detectorGainOdd = ParseData.toFloat(pages[0], 54); // InGaAs-only
                detectorOffsetOdd = ParseData.toInt16(pages[0], 58); // InGaAs-only

                wavecalCoeffs[0] = ParseData.toFloat(pages[1], 0);
                wavecalCoeffs[1] = ParseData.toFloat(pages[1], 4);
                wavecalCoeffs[2] = ParseData.toFloat(pages[1], 8);
                wavecalCoeffs[3] = ParseData.toFloat(pages[1], 12);
                degCToDACCoeffs[0] = ParseData.toFloat(pages[1], 16);
                degCToDACCoeffs[1] = ParseData.toFloat(pages[1], 20);
                degCToDACCoeffs[2] = ParseData.toFloat(pages[1], 24);
                detectorTempMax = ParseData.toInt16(pages[1], 28);
                detectorTempMin = ParseData.toInt16(pages[1], 30);
                adcToDegCCoeffs[0] = ParseData.toFloat(pages[1], 32);
                adcToDegCCoeffs[1] = ParseData.toFloat(pages[1], 36);
                adcToDegCCoeffs[2] = ParseData.toFloat(pages[1], 40);
                thermistorResistanceAt298K = ParseData.toInt16(pages[1], 44);
                thermistorBeta = ParseData.toInt16(pages[1], 46);
                calibrationDate = ParseData.toString(pages[1], 48, 12);
                calibrationBy = ParseData.toString(pages[1], 60, 3);

                detectorName = ParseData.toString(pages[2], 0, 16);
                activePixelsHoriz = ParseData.toUInt16(pages[2], 16); // note: byte 18 unused
                activePixelsVert = ParseData.toUInt16(pages[2], 19);
                minIntegrationTimeMS = ParseData.toUInt16(pages[2], 21); // will overwrite if 
                maxIntegrationTimeMS = ParseData.toUInt16(pages[2], 23); //   format >= 5
                actualPixelsHoriz = ParseData.toUInt16(pages[2], 25);
                ROIHorizStart = ParseData.toUInt16(pages[2], 27);
                ROIHorizEnd = ParseData.toUInt16(pages[2], 29);
                ROIVertRegionStart[0] = ParseData.toUInt16(pages[2], 31);
                ROIVertRegionEnd[0] = ParseData.toUInt16(pages[2], 33);
                ROIVertRegionStart[1] = ParseData.toUInt16(pages[2], 35);
                ROIVertRegionEnd[1] = ParseData.toUInt16(pages[2], 37);
                ROIVertRegionStart[2] = ParseData.toUInt16(pages[2], 39);
                ROIVertRegionEnd[2] = ParseData.toUInt16(pages[2], 41);
                linearityCoeffs[0] = ParseData.toFloat(pages[2], 43);
                linearityCoeffs[1] = ParseData.toFloat(pages[2], 47);
                linearityCoeffs[2] = ParseData.toFloat(pages[2], 51);
                linearityCoeffs[3] = ParseData.toFloat(pages[2], 55);
                linearityCoeffs[4] = ParseData.toFloat(pages[2], 59);

                // deviceLifetimeOperationMinutes = ParseData.toInt32(pages[3], 0);
                // laserLifetimeOperationMinutes = ParseData.toInt32(pages[3], 4);
                // laserTemperatureMax  = ParseData.toInt16(pages[3], 8);
                // laserTemperatureMin  = ParseData.toInt16(pages[3], 10);

                laserPowerCoeffs[0] = ParseData.toFloat(pages[3], 12);
                laserPowerCoeffs[1] = ParseData.toFloat(pages[3], 16);
                laserPowerCoeffs[2] = ParseData.toFloat(pages[3], 20);
                laserPowerCoeffs[3] = ParseData.toFloat(pages[3], 24);
                maxLaserPowerMW = ParseData.toFloat(pages[3], 28);
                minLaserPowerMW = ParseData.toFloat(pages[3], 32);

                // correct laser excitation across formats
                if (format >= 4)
                {
                    laserExcitationWavelengthNMFloat = ParseData.toFloat(pages[3], 36);
                    excitationNM = (ushort)Math.Round(laserExcitationWavelengthNMFloat);
                }
                else
                {
                    laserExcitationWavelengthNMFloat = excitationNM;
                }
                    
                if (format >= 5)
                {
                    minIntegrationTimeMS = ParseData.toUInt32(pages[3], 40);
                    maxIntegrationTimeMS = ParseData.toUInt32(pages[3], 44);
                }

                userData = format < 4 ? new byte[63] : new byte[64];
                Array.Copy(pages[4], userData, userData.Length);

                badPixelSet = new SortedSet<short>();
                for (int i = 0; i < 15; i++)
                {
                    short pixel = ParseData.toInt16(pages[5], i * 2);
                    badPixels[i] = pixel;
                    if (pixel >= 0)
                        badPixelSet.Add(pixel); // does not throw
                }
                badPixelList = new List<short>(badPixelSet);

                if (format >= 5)
                    productConfiguration = ParseData.toString(pages[5], 30, 16);
                else
                    productConfiguration = "";

                if (format >= 6 && (subformat == PAGE_SUBFORMAT.INTENSITY_CALIBRATION || 
                                    subformat == PAGE_SUBFORMAT.UNTETHERED_DEVICE))
                {
                    // load Raman Intensity Correction whether subformat is 1 or 3
                    logger.debug("loading Raman Intensity Correction");
                    intensityCorrectionOrder = ParseData.toUInt8(pages[6], 0);
                    uint numCoeffs = (uint)intensityCorrectionOrder + 1;

                    if (numCoeffs > 8)
                        numCoeffs = 0;

                    intensityCorrectionCoeffs = numCoeffs > 0 ? new float[numCoeffs] : null;

                    for (int i = 0; i < numCoeffs; ++i)
                    {
                        intensityCorrectionCoeffs[i] = ParseData.toFloat(pages[6], 1 + 4 * i);
                    }
                }
                else
                {
                    intensityCorrectionOrder = 0;
                }

                if (format >= 7)
                {
                    avgResolution = ParseData.toFloat(pages[3], 48);
                }
                else
                {
                    avgResolution = 0.0f;
                }

                if (format >= 8)
                {
                    wavecalCoeffs[4] = ParseData.toFloat(pages[2], 21);
                    if (subformat == PAGE_SUBFORMAT.USER_DATA)
                    {
                        intensityCorrectionOrder = 0;
                        intensityCorrectionCoeffs = null;

                        userData = new byte[192];
                        //Array.Copy(pages[4], userData, userData.Length);
                        Array.Copy(pages[4], 0, userData, 0, 64);
                        Array.Copy(pages[6], 0, userData, 64, 64);
                        Array.Copy(pages[7], 0, userData, 128, 64);
                    }

                    /*
                    userData = new byte[16000];
                    Array.Copy(pages[4], 0, userData, 0, 64);
                    Array.Copy(pages[7], 0, userData, 64, 64);

                    for (int k = 8; k < 256; ++k)
                    {
                        Array.Copy(pages[k], 0, userData, 64 * (k - 6), 64);
                    }
                    */
                }

                if (format >= 9)
                    featureMask = new FeatureMask(ParseData.toUInt16(pages[0], 39));
                if (format < 12)
                    featureMask.evenOddHardwareCorrected = false;
                if (format >= 10)
                    laserWarmupSec = pages[2][18];
                else
                    laserWarmupSec = 20;

                if (format >= 11)
                {
                    if (subformat == PAGE_SUBFORMAT.UNTETHERED_DEVICE)
                    {
                        logger.debug("loading untethered device configuration");
                        libraryType = ParseData.toUInt8(pages[7], 0);
                        libraryID = ParseData.toUInt16(pages[7], 1);
                        startupScansToAverage = ParseData.toUInt8(pages[7], 3);
                        matchingMinRampPixels = ParseData.toUInt8(pages[7], 4);
                        matchingMinPeakHeight = ParseData.toUInt16(pages[7], 5);
                        matchingThreshold = ParseData.toUInt8(pages[7], 7);
                        if (format >= 12)
                        {
                            librarySpectraNum = ParseData.toUInt8(pages[7], 8);
                            List<string> readNames = new List<string>();
                            int namesWritten = 0;
                            Dictionary<string, int> pageIdxs = new Dictionary<string, int>();
                            for(int i = 0; i < librarySpectraNum; i++)
                            {
                                pageIdxs = namePageIndices(namesWritten);
                                readNames.Add(ParseData.toString(pages[pageIdxs["namePage"]],pageIdxs["startIndex"],16));
                                namesWritten++;
                            }
                            libNames = readNames;
                            throwAwayCount = ParseData.toUInt8(pages[7], 9);
                            untetheredFeatureMask = ParseData.toUInt8(pages[7], 10);
                        }
                    }
                    else if (subformat == PAGE_SUBFORMAT.DETECTOR_REGIONS)
                    {
                        region1WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };
                        region2WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };
                        region3WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };

                        region1HorizStart = ParseData.toUInt16(pages[6], 0);
                        region1HorizEnd = ParseData.toUInt16(pages[6], 2);
                        region1WavecalCoeffs[0] = ParseData.toFloat(pages[6], 4);
                        region1WavecalCoeffs[1] = ParseData.toFloat(pages[6], 8);
                        region1WavecalCoeffs[2] = ParseData.toFloat(pages[6], 12);
                        region1WavecalCoeffs[3] = ParseData.toFloat(pages[6], 16);

                        region2HorizStart = ParseData.toUInt16(pages[6], 20);
                        region2HorizEnd = ParseData.toUInt16(pages[6], 22);
                        region2WavecalCoeffs[0] = ParseData.toFloat(pages[6], 24);
                        region2WavecalCoeffs[1] = ParseData.toFloat(pages[6], 28);
                        region2WavecalCoeffs[2] = ParseData.toFloat(pages[6], 32);
                        region2WavecalCoeffs[3] = ParseData.toFloat(pages[6], 36);

                        region3VertStart = ParseData.toUInt16(pages[6], 40);
                        region3VertEnd = ParseData.toUInt16(pages[6], 42);
                        region3HorizStart = ParseData.toUInt16(pages[6], 44);
                        region3HorizEnd = ParseData.toUInt16(pages[6], 46);
                        region3WavecalCoeffs[0] = ParseData.toFloat(pages[6], 48);
                        region3WavecalCoeffs[1] = ParseData.toFloat(pages[6], 52);
                        region3WavecalCoeffs[2] = ParseData.toFloat(pages[6], 56);
                        region3WavecalCoeffs[3] = ParseData.toFloat(pages[6], 60);

                        regionCount = ParseData.toUInt8(pages[7], 0);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.error("EEPROM: caught exception: {0}", ex.Message);
                return false;
            }

            if (logger.debugEnabled())
                dump();

            enforceReasonableDefaults();

            format = FORMAT;

            return true;
            
        }

        public void setDefault(Spectrometer a)
        {
            model = "";

            serialNumber = a.serialNumber;

            baudRate = 0;

            hasCooling = false;
            hasBattery = false;
            hasLaser = false;

            excitationNM = 0;

            slitSizeUM = 0;

            byte[] buffer = new byte[16];

            string test = buffer.ToString();

            wavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };

            startupIntegrationTimeMS = 0;
            double temp = 0;
            startupDetectorTemperatureDegC = (short)temp;
            if (startupDetectorTemperatureDegC >= 99)
                startupDetectorTemperatureDegC = 15;
            else if (startupDetectorTemperatureDegC <= -50)
                startupDetectorTemperatureDegC = 15;
            startupTriggeringMode = 2;
            detectorGain = a.detectorGain;
            detectorOffset = a.detectorOffset;
            detectorGainOdd = 0;
            detectorOffsetOdd = 0;

            degCToDACCoeffs[0] = 0;
            degCToDACCoeffs[1] = 0;
            degCToDACCoeffs[2] = 0;
            detectorTempMax = 0;
            detectorTempMin = 0;
            adcToDegCCoeffs[0] = 0;
            adcToDegCCoeffs[1] = 0;
            adcToDegCCoeffs[2] = 0;
            thermistorResistanceAt298K = 0;
            thermistorBeta = 0;
            calibrationDate = "";
            calibrationBy = "";

            detectorName = "";
            activePixelsHoriz = (ushort)1024;
            activePixelsVert = 70;
            minIntegrationTimeMS = 1;
            maxIntegrationTimeMS = 1000000;
            actualPixelsHoriz = (ushort)1024;
            ROIHorizStart = 0;
            ROIHorizEnd = 0;
            ROIVertRegionStart[0] = 0;
            ROIVertRegionEnd[0] = 0;
            ROIVertRegionStart[1] = 0;
            ROIVertRegionEnd[1] = 0;
            ROIVertRegionStart[2] = 0;
            ROIVertRegionEnd[2] = 0;
            linearityCoeffs[0] = 0;
            linearityCoeffs[1] = 0;
            linearityCoeffs[2] = 0;
            linearityCoeffs[3] = 0;
            linearityCoeffs[4] = 0;

            laserPowerCoeffs[0] = 0;
            laserPowerCoeffs[1] = 0;
            laserPowerCoeffs[2] = 0;
            laserPowerCoeffs[3] = 0;
            maxLaserPowerMW = 0;
            minLaserPowerMW = 0;

            laserExcitationWavelengthNMFloat = 785.0f;

            avgResolution = 0.0f;

            userData = new byte[63];

            badPixelSet = new SortedSet<short>();
            productConfiguration = "";

            //needs work
            intensityCorrectionOrder = 0;

            featureMask = new FeatureMask();
        }
        public void setFromJSON(EEPROMJSON json)
        {
            if (json.Serial != null)
                serialNumber = json.Serial;
            if (json.Model != null)
                fullModel = json.Model;
            slitSizeUM = (ushort)json.SlitWidth;
            baudRate = (uint)json.BaudRate;
            excitationNM = (ushort)json.ExcitationWavelengthNM;
            hasBattery = json.IncBattery;
            hasCooling = json.IncCooling;
            hasLaser = json.IncLaser;

            startupIntegrationTimeMS = (ushort)json.StartupIntTimeMS;
            startupDetectorTemperatureDegC = (short)json.StartupTempC;
            startupTriggeringMode = (byte)json.StartupTriggerMode;
            detectorGain = (float)json.DetectorGain;
            detectorGainOdd = (float)json.DetectorGainOdd;
            detectorOffset = (Int16)json.DetectorOffset;
            detectorOffsetOdd = (Int16)json.DetectorOffsetOdd;

            featureMask.bin2x2 = json.Bin2x2;
            featureMask.invertXAxis = json.FlipXAxis;
            featureMask.gen15 = json.Gen15;
            featureMask.cutoffInstalled = json.CutoffFilter;
            featureMask.evenOddHardwareCorrected = json.EvenOddHardwareCorrected;
            featureMask.sigLaserTEC = json.SigLaserTEC;
            featureMask.hasInterlockFeedback = json.HasInterlockFeedback;

            wavecalCoeffs[0] = (float)json.WavecalCoeffs[0];
            wavecalCoeffs[1] = (float)json.WavecalCoeffs[1];
            wavecalCoeffs[2] = (float)json.WavecalCoeffs[2];
            wavecalCoeffs[3] = (float)json.WavecalCoeffs[3];
            wavecalCoeffs[4] = (float)json.WavecalCoeffs[4];
            degCToDACCoeffs[0] = (float)json.TempToDACCoeffs[0];
            degCToDACCoeffs[1] = (float)json.TempToDACCoeffs[1];
            degCToDACCoeffs[2] = (float)json.TempToDACCoeffs[2];
            adcToDegCCoeffs[0] = (float)json.ADCToTempCoeffs[0];
            adcToDegCCoeffs[1] = (float)json.ADCToTempCoeffs[1];
            adcToDegCCoeffs[2] = (float)json.ADCToTempCoeffs[2];
            detectorTempMax = (Int16)json.DetectorTempMax;
            detectorTempMin = (Int16)json.DetectorTempMin;
            thermistorBeta = (Int16)json.ThermistorBeta;
            thermistorResistanceAt298K = (Int16)json.ThermistorResAt298K;

            if (json.CalibrationDate != null)
                calibrationDate = json.CalibrationDate;
            if (json.CalibrationBy != null)
                calibrationBy = json.CalibrationBy;

            if (json.DetectorName != null)
                detectorName = json.DetectorName;
            actualPixelsHoriz = (ushort)json.ActualPixelsHoriz;
            activePixelsHoriz = (UInt16)json.ActivePixelsHoriz;
            activePixelsVert = (UInt16)json.ActivePixelsVert;
            minIntegrationTimeMS = (UInt32)json.MinIntegrationTimeMS;
            maxIntegrationTimeMS = (UInt32)json.MaxIntegrationTimeMS;
            ROIHorizStart = (UInt16)json.ROIHorizStart;
            ROIHorizEnd = (UInt16)json.ROIHorizEnd;
            ROIVertRegionStart[0] = (UInt16)json.ROIVertRegionStarts[0];
            ROIVertRegionEnd[0] = (UInt16)json.ROIVertRegionEnds[0];
            ROIVertRegionStart[1] = (UInt16)json.ROIVertRegionStarts[1];
            ROIVertRegionEnd[1] = (UInt16)json.ROIVertRegionEnds[1];
            ROIVertRegionStart[2] = (UInt16)json.ROIVertRegionStarts[2];
            ROIVertRegionEnd[2] = (UInt16)json.ROIVertRegionEnds[2];
            linearityCoeffs[0] = (float)json.LinearityCoeffs[0];
            linearityCoeffs[1] = (float)json.LinearityCoeffs[1];
            linearityCoeffs[2] = (float)json.LinearityCoeffs[2];
            linearityCoeffs[3] = (float)json.LinearityCoeffs[3];
            linearityCoeffs[4] = (float)json.LinearityCoeffs[4];

            maxLaserPowerMW = (float)json.MaxLaserPowerMW;
            minLaserPowerMW = (float)json.MinLaserPowerMW;
            laserWarmupSec = json.LaserWarmupS;
            laserExcitationWavelengthNMFloat = (float)json.ExcitationWavelengthNM;
            avgResolution = (float)json.AvgResolution;
            laserPowerCoeffs[0] = (float)json.LaserPowerCoeffs[0];
            laserPowerCoeffs[1] = (float)json.LaserPowerCoeffs[1];
            laserPowerCoeffs[2] = (float)json.LaserPowerCoeffs[2];
            laserPowerCoeffs[3] = (float)json.LaserPowerCoeffs[3];

            if (json.UserText != null)
                userText = json.UserText;

            badPixels[0] = (Int16)json.BadPixels[0];
            badPixels[1] = (Int16)json.BadPixels[1];
            badPixels[2] = (Int16)json.BadPixels[2];
            badPixels[3] = (Int16)json.BadPixels[3];
            badPixels[4] = (Int16)json.BadPixels[4];
            badPixels[5] = (Int16)json.BadPixels[5];
            badPixels[6] = (Int16)json.BadPixels[6];
            badPixels[7] = (Int16)json.BadPixels[7];
            badPixels[8] = (Int16)json.BadPixels[8];
            badPixels[9] = (Int16)json.BadPixels[9];
            badPixels[10] = (Int16)json.BadPixels[10];
            badPixels[11] = (Int16)json.BadPixels[11];
            badPixels[12] = (Int16)json.BadPixels[12];
            badPixels[13] = (Int16)json.BadPixels[13];
            badPixels[14] = (Int16)json.BadPixels[14];

            if (json.ProductConfig != null)
                productConfiguration = json.ProductConfig;

            PAGE_SUBFORMAT jsonSubformat = (PAGE_SUBFORMAT)json.Subformat;

            if (jsonSubformat == PAGE_SUBFORMAT.INTENSITY_CALIBRATION || jsonSubformat == PAGE_SUBFORMAT.UNTETHERED_DEVICE)
            {
                intensityCorrectionOrder = (byte)json.RelIntCorrOrder;
                if (json.RelIntCorrOrder > 0)
                {
                    intensityCorrectionCoeffs = new float[intensityCorrectionOrder + 1];
                    subformat = PAGE_SUBFORMAT.INTENSITY_CALIBRATION;

                    for (int i = 0; i < intensityCorrectionCoeffs.Length; ++i)
                        intensityCorrectionCoeffs[i] = (float)json.RelIntCorrCoeffs[i];
                }
            }

            if (jsonSubformat == PAGE_SUBFORMAT.DETECTOR_REGIONS)
            {
                region1WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };
                region2WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };
                region3WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };


                region1WavecalCoeffs[0] = (float)json.Region1WavecalCoeffs[0];
                region1WavecalCoeffs[1] = (float)json.Region1WavecalCoeffs[1];
                region1WavecalCoeffs[2] = (float)json.Region1WavecalCoeffs[2];
                region1WavecalCoeffs[3] = (float)json.Region1WavecalCoeffs[3];

                region2WavecalCoeffs[0] = (float)json.Region2WavecalCoeffs[0];
                region2WavecalCoeffs[1] = (float)json.Region2WavecalCoeffs[1];
                region2WavecalCoeffs[2] = (float)json.Region2WavecalCoeffs[2];
                region2WavecalCoeffs[3] = (float)json.Region2WavecalCoeffs[3];

                region3WavecalCoeffs[0] = (float)json.Region3WavecalCoeffs[0];
                region3WavecalCoeffs[1] = (float)json.Region3WavecalCoeffs[1];
                region3WavecalCoeffs[2] = (float)json.Region3WavecalCoeffs[2];
                region3WavecalCoeffs[3] = (float)json.Region3WavecalCoeffs[3];

                region1HorizStart = (ushort)json.Region1HorizStart;
                region1HorizEnd = (ushort)json.Region1HorizEnd;
                region2HorizStart = (ushort)json.Region2HorizStart;
                region2HorizEnd = (ushort)json.Region2HorizEnd;
                region3HorizStart = (ushort)json.Region3HorizStart;
                region3HorizEnd = (ushort)json.Region3HorizEnd;

                regionCount = json.RegionCount;
            }


        }

        public EEPROMJSON toJSON()
        {
            EEPROMJSON json = new EEPROMJSON();

            json.Serial = serialNumber;
            json.Model = fullModel;
            json.SlitWidth = slitSizeUM;
            json.BaudRate = baudRate;
            json.IncBattery = hasBattery;
            json.IncCooling = hasCooling;
            json.IncLaser = hasLaser;
            json.StartupIntTimeMS = startupIntegrationTimeMS;
            json.StartupTempC = startupDetectorTemperatureDegC;
            json.StartupTriggerMode = startupTriggeringMode;
            json.DetectorGain = detectorGain;
            json.DetectorGainOdd = detectorGainOdd;
            json.DetectorOffset = detectorOffset;
            json.DetectorOffsetOdd = detectorOffsetOdd;
            json.WavecalCoeffs = new double[5];
            if (wavecalCoeffs != null)
            {
                json.WavecalCoeffs[0] = wavecalCoeffs[0];
                json.WavecalCoeffs[1] = wavecalCoeffs[1];
                json.WavecalCoeffs[2] = wavecalCoeffs[2];
                json.WavecalCoeffs[3] = wavecalCoeffs[3];
                json.WavecalCoeffs[4] = wavecalCoeffs[4];
            }
            json.TempToDACCoeffs = new double[3];
            if (degCToDACCoeffs != null)
            {
                json.TempToDACCoeffs[0] = degCToDACCoeffs[0];
                json.TempToDACCoeffs[1] = degCToDACCoeffs[1];
                json.TempToDACCoeffs[2] = degCToDACCoeffs[2];
            }
            json.ADCToTempCoeffs = new double[3];
            if (adcToDegCCoeffs != null)
            {
                json.ADCToTempCoeffs[0] = adcToDegCCoeffs[0];
                json.ADCToTempCoeffs[1] = adcToDegCCoeffs[1];
                json.ADCToTempCoeffs[2] = adcToDegCCoeffs[2];
            }
            json.LinearityCoeffs = new double[5];
            if (linearityCoeffs != null)
            {
                json.LinearityCoeffs[0] = linearityCoeffs[0];
                json.LinearityCoeffs[1] = linearityCoeffs[1];
                json.LinearityCoeffs[2] = linearityCoeffs[2];
                json.LinearityCoeffs[3] = linearityCoeffs[3];
                json.LinearityCoeffs[4] = linearityCoeffs[4];
            }
            json.DetectorTempMax = detectorTempMax;
            json.DetectorTempMin = detectorTempMin;
            json.ThermistorBeta = thermistorBeta;
            json.ThermistorResAt298K = thermistorResistanceAt298K;
            json.CalibrationDate = calibrationDate;
            json.CalibrationBy = calibrationBy;
            json.DetectorName = detectorName;
            json.ActualPixelsHoriz = actualPixelsHoriz;
            json.ActivePixelsHoriz = activePixelsHoriz;
            json.ActivePixelsVert = activePixelsVert;
            json.MinIntegrationTimeMS = (int)minIntegrationTimeMS;
            json.MaxIntegrationTimeMS = (int)maxIntegrationTimeMS;
            json.ROIHorizStart = ROIHorizStart;
            json.ROIHorizEnd = ROIHorizEnd;
            json.ROIVertRegionStarts = new int[3];
            if (ROIVertRegionStart != null)
            {
                json.ROIVertRegionStarts[0] = ROIVertRegionStart[0];
                json.ROIVertRegionStarts[1] = ROIVertRegionStart[1];
                json.ROIVertRegionStarts[2] = ROIVertRegionStart[2];
            }
            json.ROIVertRegionEnds = new int[3];
            if (ROIVertRegionEnd != null)
            {
                json.ROIVertRegionEnds[0] = ROIVertRegionEnd[0];
                json.ROIVertRegionEnds[1] = ROIVertRegionEnd[1];
                json.ROIVertRegionEnds[2] = ROIVertRegionEnd[2];
            }
            json.LaserPowerCoeffs = new double[4];
            if (laserPowerCoeffs != null)
            {
                json.LaserPowerCoeffs[0] = laserPowerCoeffs[0];
                json.LaserPowerCoeffs[1] = laserPowerCoeffs[1];
                json.LaserPowerCoeffs[2] = laserPowerCoeffs[2];
                json.LaserPowerCoeffs[3] = laserPowerCoeffs[3];
            }
            json.MaxLaserPowerMW = maxLaserPowerMW;
            json.MinLaserPowerMW = minLaserPowerMW;
            json.ExcitationWavelengthNM = laserExcitationWavelengthNMFloat;
            json.AvgResolution = avgResolution;
            json.BadPixels = new int[15];
            if (badPixels != null)
            {
                json.BadPixels[0] = badPixels[0];
                json.BadPixels[1] = badPixels[1];
                json.BadPixels[2] = badPixels[2];
                json.BadPixels[3] = badPixels[3];
                json.BadPixels[4] = badPixels[4];
                json.BadPixels[5] = badPixels[5];
                json.BadPixels[6] = badPixels[6];
                json.BadPixels[7] = badPixels[7];
                json.BadPixels[8] = badPixels[8];
                json.BadPixels[9] = badPixels[9];
                json.BadPixels[10] = badPixels[10];
                json.BadPixels[11] = badPixels[11];
                json.BadPixels[12] = badPixels[12];
                json.BadPixels[13] = badPixels[13];
                json.BadPixels[14] = badPixels[14];
            }
            json.UserText = userText;
            json.ProductConfig = productConfiguration;
            if (subformat == PAGE_SUBFORMAT.INTENSITY_CALIBRATION || subformat == PAGE_SUBFORMAT.UNTETHERED_DEVICE)
            {
                json.RelIntCorrOrder = intensityCorrectionOrder;
                if (json.RelIntCorrOrder > 0 && intensityCorrectionCoeffs != null)
                {
                    json.RelIntCorrCoeffs = new double[intensityCorrectionCoeffs.Length];
                    for (int i = 0; i <= json.RelIntCorrOrder; ++i)
                    {
                        if (i < intensityCorrectionCoeffs.Length)
                        {
                            json.RelIntCorrCoeffs[i] = intensityCorrectionCoeffs[i];
                        }
                    }
                }
            }

            if (featureMask != null)
            {
                json.Bin2x2 = featureMask.bin2x2;
                json.FlipXAxis = featureMask.invertXAxis;
                json.Gen15 = featureMask.gen15;
                json.CutoffFilter = featureMask.cutoffInstalled;
                json.EvenOddHardwareCorrected = featureMask.evenOddHardwareCorrected;
                json.SigLaserTEC = featureMask.sigLaserTEC;
                json.HasInterlockFeedback = featureMask.hasInterlockFeedback;
            }

            json.LaserWarmupS = laserWarmupSec;
            json.Subformat = (byte)subformat;

            if (subformat == PAGE_SUBFORMAT.DETECTOR_REGIONS)
            {
                json.Region1WavecalCoeffs = new double[4];
                if (region1WavecalCoeffs != null)
                {
                    json.Region1WavecalCoeffs[0] = region1WavecalCoeffs[0];
                    json.Region1WavecalCoeffs[1] = region1WavecalCoeffs[1];
                    json.Region1WavecalCoeffs[2] = region1WavecalCoeffs[2];
                    json.Region1WavecalCoeffs[3] = region1WavecalCoeffs[3];
                }
                json.Region2WavecalCoeffs = new double[4];
                if (region2WavecalCoeffs != null)
                {
                    json.Region2WavecalCoeffs[0] = region2WavecalCoeffs[0];
                    json.Region2WavecalCoeffs[1] = region2WavecalCoeffs[1];
                    json.Region2WavecalCoeffs[2] = region2WavecalCoeffs[2];
                    json.Region2WavecalCoeffs[3] = region2WavecalCoeffs[3];
                }
                json.Region3WavecalCoeffs = new double[4];
                if (region3WavecalCoeffs != null)
                {
                    json.Region3WavecalCoeffs[0] = region3WavecalCoeffs[0];
                    json.Region3WavecalCoeffs[1] = region3WavecalCoeffs[1];
                    json.Region3WavecalCoeffs[2] = region3WavecalCoeffs[2];
                    json.Region3WavecalCoeffs[3] = region3WavecalCoeffs[3];
                }

                json.Region1HorizStart = region1HorizStart;
                json.Region1HorizEnd = region1HorizEnd;
                json.Region2HorizStart = region2HorizStart;
                json.Region2HorizEnd = region2HorizEnd;
                json.Region3HorizStart = region3HorizStart;
                json.Region3HorizEnd = region3HorizEnd;
                json.Region3HorizStart = region3HorizStart;
                json.Region3HorizEnd = region3HorizEnd;
                json.RegionCount = regionCount;
            }

            return json;
        }

        /// <summary>
        /// Reports whether the Spectrometer's EEPROM contains four valid 
        /// coefficients for converting a requested output power setpoint in 
        /// milliWatts into the calibrated fractional PWM duty cycle as a 
        /// percentage of full (continuous) power.
        /// </summary>
        ///
        /// <remarks>
        /// All this does is confirm that four floating-point values were 
        /// successfully demarshalled from the appropriate 16 bytes of the EEPROM
        /// page, and that at least one of them is non-zero and positive.  It 
        /// does not guarantee that the floats together represent an accurate 
        /// laser power calibration for the current spectrometer.
        /// </remarks>
        ///
        /// <returns>whether a seeming-valid calibration was found</returns>
        public bool hasLaserPowerCalibration()
        {
            if (maxLaserPowerMW <= 0)
                return false;

            if (laserPowerCoeffs is null || laserPowerCoeffs.Length < 4)
                return false;

            bool onePos = false;
            foreach (double d in laserPowerCoeffs)
                if (Double.IsNaN(d))
                    return false;
                else if (d > 0)
                    onePos = true;

            return onePos;
        }

        protected void enforceReasonableDefaults()
        {
            ////////////////////////////////////////////////////////////////////
            // wavecal (only check first 4)
            ////////////////////////////////////////////////////////////////////

            bool defaultWavecal = false;
            for (int i = 0; i < 4; i++)
                if (Double.IsNaN(wavecalCoeffs[i]))
                    defaultWavecal = true;

            if (defaultWavecal || format > (FORMAT + 1))
            {
                logger.error("EEPROM appears to be default");
                defaultValues = true;
                wavecalCoeffs = new float[5];
                wavecalCoeffs[0] = 0;
                wavecalCoeffs[1] = 1;
                wavecalCoeffs[2] = 0;
                wavecalCoeffs[3] = 0;
                wavecalCoeffs[4] = 0;
            }

            // if there's a 5th element, make double-sure it's not corrupt
            if (wavecalCoeffs.Length == 5)
            {
                double t = wavecalCoeffs[wavecalCoeffs.Length - 1];
                if (double.IsNaN(t) || double.IsInfinity(t))
                    wavecalCoeffs[wavecalCoeffs.Length - 1] = 0;

                // it's just not reasonable for the x^4 coeff to have magnitude > 1
                if (Math.Abs(wavecalCoeffs[4]) > 1.0)
                {
                    logger.error("truncating wavecalCoeff[4] {0} to zero", wavecalCoeffs[4]);
                    wavecalCoeffs[4] = 0;
                }
            }

            ////////////////////////////////////////////////////////////////////
            // other coeffs
            ////////////////////////////////////////////////////////////////////

            linearityCoeffs = Util.cleanNan(linearityCoeffs);
            laserPowerCoeffs = Util.cleanNan(laserPowerCoeffs);

            ////////////////////////////////////////////////////////////////////
            // integration time
            ////////////////////////////////////////////////////////////////////

            if (minIntegrationTimeMS < 1 || minIntegrationTimeMS > 1000)
            {
                logger.error("invalid minIntegrationTimeMS found ({0}), defaulting to 1", minIntegrationTimeMS);
                minIntegrationTimeMS = 1;
                maxIntegrationTimeMS = 60000;
            }

            ////////////////////////////////////////////////////////////////////
            // other min/max
            ////////////////////////////////////////////////////////////////////

            // you'd think this could be done with generics but no...
            if (minIntegrationTimeMS > maxIntegrationTimeMS)
            {
                var t = minIntegrationTimeMS;
                minIntegrationTimeMS = maxIntegrationTimeMS;
                maxIntegrationTimeMS = t;
            }

            if (minLaserPowerMW > maxLaserPowerMW)
            {
                var t = minLaserPowerMW;
                minLaserPowerMW = maxLaserPowerMW;
                maxLaserPowerMW = t;
            }

            if (detectorTempMin > detectorTempMax)
            {
                var t = detectorTempMin;
                detectorTempMin = detectorTempMax;
                detectorTempMax = t;
            }
        }

        protected bool writeParse()
        {
            if (!ParseData.writeString(model, pages[0], 0, 16)) return false;
            if (!ParseData.writeString(serialNumber, pages[0], 16, 16)) return false;
            if (!ParseData.writeUInt32(baudRate, pages[0], 32)) return false;
            if (!ParseData.writeBool(hasCooling, pages[0], 36)) return false;
            if (!ParseData.writeBool(hasBattery, pages[0], 37)) return false;
            if (!ParseData.writeBool(hasLaser, pages[0], 38)) return false;

            if (format >= 9)
            {
                if (!ParseData.writeUInt16(featureMask.toUInt16(), pages[0], 39)) return false;
            }
            else
            {
                if (!ParseData.writeUInt16(excitationNM, pages[0], 39)) return false;
            }

            if (!ParseData.writeUInt16(slitSizeUM, pages[0], 41)) return false;
            if (!ParseData.writeUInt16(startupIntegrationTimeMS, pages[0], 43)) return false;
            if (!ParseData.writeInt16(startupDetectorTemperatureDegC, pages[0], 45)) return false;
            if (!ParseData.writeByte(startupTriggeringMode, pages[0], 47)) return false;
            if (!ParseData.writeFloat(detectorGain, pages[0], 48)) return false;
            if (!ParseData.writeInt16(detectorOffset, pages[0], 52)) return false;
            if (!ParseData.writeFloat(detectorGainOdd, pages[0], 54)) return false;
            if (!ParseData.writeInt16(detectorOffsetOdd, pages[0], 58)) return false;

            if (!ParseData.writeFloat(wavecalCoeffs[0], pages[1], 0)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[1], pages[1], 4)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[2], pages[1], 8)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[3], pages[1], 12)) return false;
            if (!ParseData.writeFloat(degCToDACCoeffs[0], pages[1], 16)) return false;
            if (!ParseData.writeFloat(degCToDACCoeffs[1], pages[1], 20)) return false;
            if (!ParseData.writeFloat(degCToDACCoeffs[2], pages[1], 24)) return false;
            if (!ParseData.writeInt16(detectorTempMax, pages[1], 28)) return false;
            if (!ParseData.writeInt16(detectorTempMin, pages[1], 30)) return false;
            if (!ParseData.writeFloat(adcToDegCCoeffs[0], pages[1], 32)) return false;
            if (!ParseData.writeFloat(adcToDegCCoeffs[1], pages[1], 36)) return false;
            if (!ParseData.writeFloat(adcToDegCCoeffs[2], pages[1], 40)) return false;
            if (!ParseData.writeInt16(thermistorResistanceAt298K, pages[1], 44)) return false;
            if (!ParseData.writeInt16(thermistorBeta, pages[1], 46)) return false;
            if (!ParseData.writeString(calibrationDate, pages[1], 48, 12)) return false;
            if (!ParseData.writeString(calibrationBy, pages[1], 60, 3)) return false;

            if (!ParseData.writeString(detectorName, pages[2], 0, 16)) return false;
            if (!ParseData.writeUInt16(activePixelsHoriz, pages[2], 16)) return false;
            pages[2][18] = laserWarmupSec;
            if (!ParseData.writeUInt16(activePixelsVert, pages[2], 19)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[4], pages[2], 21)) return false;

            if (!ParseData.writeUInt16(actualPixelsHoriz, pages[2], 25)) return false;
            if (!ParseData.writeUInt16(ROIHorizStart, pages[2], 27)) return false;
            if (!ParseData.writeUInt16(ROIHorizEnd, pages[2], 29)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionStart[0], pages[2], 31)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionEnd[0], pages[2], 33)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionStart[1], pages[2], 35)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionEnd[1], pages[2], 37)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionStart[2], pages[2], 39)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionEnd[2], pages[2], 41)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[0], pages[2], 43)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[1], pages[2], 47)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[2], pages[2], 51)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[3], pages[2], 55)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[4], pages[2], 59)) return false;

            if (!ParseData.writeFloat(laserPowerCoeffs[0], pages[3], 12)) return false;
            if (!ParseData.writeFloat(laserPowerCoeffs[1], pages[3], 16)) return false;
            if (!ParseData.writeFloat(laserPowerCoeffs[2], pages[3], 20)) return false;
            if (!ParseData.writeFloat(laserPowerCoeffs[3], pages[3], 24)) return false;
            if (!ParseData.writeFloat(maxLaserPowerMW, pages[3], 28)) return false;
            if (!ParseData.writeFloat(minLaserPowerMW, pages[3], 32)) return false;



            if (format >= 4)
            {
                if (!ParseData.writeFloat(laserExcitationWavelengthNMFloat, pages[3], 36)) return false;
                if (!ParseData.writeUInt32(minIntegrationTimeMS, pages[3], 40)) return false;
                if (!ParseData.writeUInt32(maxIntegrationTimeMS, pages[3], 44)) return false;
            }

            if (format >= 7)
                if (!ParseData.writeFloat(avgResolution, pages[3], 48)) return false;

            byte[] userDataChunk2 = new byte[64];
            byte[] userDataChunk3 = new byte[64];

            if (format >= 8)
            {

                // The user has unfettered access to userData and can make it as long as they want, this breaks it up into chunks
                // to write to the different places in EEPROM we write user data to, and throws away bytes above 192 rather
                // than try to write them.
                //
                // Should protect users without restricting them
                if (userData.Length <= 64)
                {
                    Array.Copy(userData, pages[4], userData.Length);
                }
                else
                {
                    Array.Copy(userData, pages[4], 64);
                    if (userData.Length <= 128)
                    {
                        Array.Copy(userData, 64, userDataChunk2, 0, userData.Length - 64);

                    }
                    else if (userData.Length <= 192)
                    {
                        Array.Copy(userData, 64, userDataChunk2, 0, 64);
                        Array.Copy(userData, 128, userDataChunk3, 0, userData.Length - 128);
                    }
                    else
                    {
                        Array.Copy(userData, 64, userDataChunk2, 0, 64);
                        Array.Copy(userData, 128, userDataChunk3, 0, 64);
                    }
                }
            }

            // note that we write the positional, error-prone array (which is 
            // user -writable), not the List or SortedSet caches
            for (int i = 0; i < badPixels.Length; i++)
                if (!ParseData.writeInt16(badPixels[i], pages[5], i * 2))
                    return false;

            if (format >= 5)
                if (!ParseData.writeString(productConfiguration, pages[5], 30, 16)) return false;

            //subformat = (PAGE_SUBFORMAT)ParseData.toUInt8(pages[5], 63);
            if (!ParseData.writeByte((byte)subformat, pages[5], 63)) return false;

            if (format >= 8)
            {
                if (!ParseData.writeByte((byte)subformat, pages[5], 63)) return false;

                if (subformat == PAGE_SUBFORMAT.USER_DATA)
                {
                    Array.Copy(userDataChunk2, 0, pages[6], 0, 64);
                    Array.Copy(userDataChunk3, 0, pages[7], 0, 64);
                }
                
                if (subformat == PAGE_SUBFORMAT.INTENSITY_CALIBRATION || 
                    subformat == PAGE_SUBFORMAT.UNTETHERED_DEVICE)
                {
                    if (!ParseData.writeByte(intensityCorrectionOrder, pages[6], 0)) return false;
                    if (intensityCorrectionCoeffs != null && intensityCorrectionOrder < 8)
                    {
                        for (int i = 0; i <= intensityCorrectionOrder; ++i)
                        {
                            if (!ParseData.writeFloat(intensityCorrectionCoeffs[i], pages[6], 1 + 4 * i)) return false;
                        }
                    }
                }

                if (subformat == PAGE_SUBFORMAT.UNTETHERED_DEVICE)
                {
                    if (!ParseData.writeByte(libraryType,               pages[7], 0)) return false;
                    if (!ParseData.writeUInt16(libraryID,               pages[7], 1)) return false;
                    if (!ParseData.writeByte(startupScansToAverage,     pages[7], 3)) return false;
                    if (!ParseData.writeByte(matchingMinRampPixels,     pages[7], 4)) return false;
                    if (!ParseData.writeUInt16(matchingMinPeakHeight,   pages[7], 5)) return false;
                    if (!ParseData.writeByte(matchingThreshold,         pages[7], 7)) return false;
                    if (!ParseData.writeByte(librarySpectraNum,               pages[7], 8)) return false;
                    if (!ParseData.writeByte(throwAwayCount,               pages[7], 9)) return false;
                    if (!ParseData.writeByte(untetheredFeatureMask,               pages[7], 10)) return false;
                    int namesWritten = 0;
                    Dictionary<string, int> pageIdx = new Dictionary<string, int>();
                    foreach(string libName in libNames)
                    {
                        pageIdx = namePageIndices(namesWritten);
                        // fill-in any pages that weren't loaded/created at start
                        // (e.g. if a different subformat had been in effect)
                        while (pageIdx["namePage"] >= pages.Count)
                        {
                            logger.debug("appending new page {0}", pages.Count);
                            pages.Add(new byte[64]);
                        }
                        if (!ParseData.writeString(libName, pages[pageIdx["namePage"]], pageIdx["startIndex"], libName.Length - 1)) return false;
                        namesWritten++;
                    }
                }

                if (subformat == PAGE_SUBFORMAT.DETECTOR_REGIONS)
                {
                    if (!ParseData.writeUInt16(region1HorizStart, pages[6], 0)) return false;
                    if (!ParseData.writeUInt16(region1HorizEnd, pages[6], 2)) return false;
                    if (!ParseData.writeFloat(region1WavecalCoeffs[0], pages[6], 4)) return false;
                    if (!ParseData.writeFloat(region1WavecalCoeffs[1], pages[6], 8)) return false;
                    if (!ParseData.writeFloat(region1WavecalCoeffs[2], pages[6], 12)) return false;
                    if (!ParseData.writeFloat(region1WavecalCoeffs[3], pages[6], 16)) return false;

                    if (!ParseData.writeUInt16(region2HorizStart, pages[6], 20)) return false;
                    if (!ParseData.writeUInt16(region2HorizEnd, pages[6], 22)) return false;
                    if (!ParseData.writeFloat(region2WavecalCoeffs[0], pages[6], 24)) return false;
                    if (!ParseData.writeFloat(region2WavecalCoeffs[1], pages[6], 28)) return false;
                    if (!ParseData.writeFloat(region2WavecalCoeffs[2], pages[6], 32)) return false;
                    if (!ParseData.writeFloat(region2WavecalCoeffs[3], pages[6], 36)) return false;

                    if (!ParseData.writeUInt16(region3VertStart, pages[6], 40)) return false;
                    if (!ParseData.writeUInt16(region3VertEnd, pages[6], 42)) return false;
                    if (!ParseData.writeUInt16(region3HorizStart, pages[6], 44)) return false;
                    if (!ParseData.writeUInt16(region3HorizEnd, pages[6], 46)) return false;
                    if (!ParseData.writeFloat(region3WavecalCoeffs[0], pages[6], 48)) return false;
                    if (!ParseData.writeFloat(region3WavecalCoeffs[1], pages[6], 52)) return false;
                    if (!ParseData.writeFloat(region3WavecalCoeffs[2], pages[6], 56)) return false;
                    if (!ParseData.writeFloat(region3WavecalCoeffs[3], pages[6], 60)) return false;

                    if (!ParseData.writeByte(regionCount, pages[7], 0)) return false;
                }

            }

            else if (format >= 6)
            {
                if (!ParseData.writeByte(intensityCorrectionOrder, pages[6], 0)) return false;
                if (intensityCorrectionCoeffs != null && intensityCorrectionOrder < 8)
                {
                    for (int i = 0; i <= intensityCorrectionOrder; ++i)
                    {
                        if (!ParseData.writeFloat(intensityCorrectionCoeffs[i], pages[6], 1 + 4 * i)) return false;
                    }
                }
            }

            pages[0][63] = format;

            return true;
        }

        private Dictionary<string,int> namePageIndices(int namesWritten)
        {
            Dictionary<string, int> pageIndecies = new Dictionary<string, int>();
            int namePage;
            int startIndex;

            if(namesWritten < 4)
            {
                namePage = 8;
            }
            else
            {
                namePage = 9;
            }
            /*
            * Compacted this with some math so thought a comment was good
            * i % 4 because each page holds 4 names so at 4 the index becomes 0 again
            * and then each length is 16 so that's constant and the start just gets
            * mulitplied by 16 each time
            * */
            startIndex = (namesWritten % 4) * MAX_NAME_LEN;
            pageIndecies.Add("namePage", namePage);
            pageIndecies.Add("startIndex", startIndex);
            return pageIndecies;
        }

        protected void dump()
        {
            logger.debug("format                = {0}", format);
            logger.debug("model                 = {0}", model);
            logger.debug("serialNumber          = {0}", serialNumber);
            logger.debug("baudRate              = {0}", baudRate);
            logger.debug("hasCooling            = {0}", hasCooling);
            logger.debug("hasBattery            = {0}", hasBattery);
            logger.debug("hasLaser              = {0}", hasLaser);
            logger.debug("excitationNM          = {0}", excitationNM);
            logger.debug("slitSizeUM            = {0}", slitSizeUM);

            logger.debug("startupIntegrationTimeMS = {0}", startupIntegrationTimeMS);
            logger.debug("startupDetectorTempDegC = {0}", startupDetectorTemperatureDegC);
            logger.debug("startupTriggeringMode = {0}", startupTriggeringMode);
            logger.debug("detectorGain          = {0:f2}", detectorGain);
            logger.debug("detectorOffset        = {0}", detectorOffset);
            logger.debug("detectorGainOdd       = {0:f2}", detectorGainOdd);
            logger.debug("detectorOffsetOdd     = {0}", detectorOffsetOdd);

            for (int i = 0; i < wavecalCoeffs.Length; i++)
                logger.debug("wavecalCoeffs[{0}]      = {1}", i, wavecalCoeffs[i]);
            for (int i = 0; i < degCToDACCoeffs.Length; i++)
                logger.debug("degCToDACCoeffs[{0}]    = {1}", i, degCToDACCoeffs[i]);
            logger.debug("detectorTempMin       = {0}", detectorTempMin);
            logger.debug("detectorTempMax       = {0}", detectorTempMax);
            for (int i = 0; i < adcToDegCCoeffs.Length; i++)
                logger.debug("adcToDegCCoeffs[{0}]    = {1}", i, adcToDegCCoeffs[i]);
            logger.debug("thermistorResistanceAt298K = {0}", thermistorResistanceAt298K);
            logger.debug("thermistorBeta        = {0}", thermistorBeta);
            logger.debug("calibrationDate       = {0}", calibrationDate);
            logger.debug("calibrationBy         = {0}", calibrationBy);
                                               
            logger.debug("detectorName          = {0}", detectorName);
            logger.debug("activePixelsHoriz     = {0}", activePixelsHoriz);
            logger.debug("activePixelsVert      = {0}", activePixelsVert);
            logger.debug("minIntegrationTimeMS  = {0}", minIntegrationTimeMS);
            logger.debug("maxIntegrationTimeMS  = {0}", maxIntegrationTimeMS);
            logger.debug("actualPixelsHoriz     = {0}", actualPixelsHoriz);
            logger.debug("ROIHorizStart         = {0}", ROIHorizStart);
            logger.debug("ROIHorizEnd           = {0}", ROIHorizEnd);
            for (int i = 0; i < ROIVertRegionStart.Length; i++)
                logger.debug("ROIVertRegionStart[{0}] = {1}", i, ROIVertRegionStart[i]);
            for (int i = 0; i < ROIVertRegionEnd.Length; i++)
                logger.debug("ROIVertRegionEnd[{0}]   = {1}", i, ROIVertRegionEnd[i]);
            for (int i = 0; i < linearityCoeffs.Length; i++)
                logger.debug("linearityCoeffs[{0}]    = {1}", i, linearityCoeffs[i]);

            for (int i = 0; i < laserPowerCoeffs.Length; i++)
                logger.debug("laserPowerCoeffs[{0}]   = {1}", i, laserPowerCoeffs[i]);
            logger.debug("maxLaserPowerMW       = {0}", maxLaserPowerMW);
            logger.debug("minLaserPowerMW       = {0}", minLaserPowerMW);
            logger.debug("laserExcitationNMFloat= {0}", laserExcitationWavelengthNMFloat);

            logger.debug("userText              = {0}", userText);

            for (int i = 0; i < badPixels.Length; i++)
                logger.debug("badPixels[{0,2}]         = {1}", i, badPixels[i]);

            logger.debug("productConfiguration  = {0}", productConfiguration);
            logger.debug("subformat             = {0}", subformat);
        }
    }
}
