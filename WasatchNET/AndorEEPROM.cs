#if WIN32 || x64
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if WIN32
using ATMCD32CS;
#elif x64
using ATMCD64CS;
#endif

namespace WasatchNET
{
    /// <summary>
    /// Virtualizes the EEPROM fields normally used with physical EEPROM on other spectrometers
    /// </summary>
    /// <remarks>
    /// Our Andor camera spectrometers have no physical EEPROM. All fields here exist only in
    /// the computer's memory and other systems must be used to maintain these values properly.
    /// 
    /// </remarks>
    public class AndorEEPROM : EEPROM
    {
        private AndorSDK andorDriver;

        const short effectiveMinTemp = -60;
        const short effectiveMaxTemp = 10;

        internal AndorEEPROM(AndorSpectrometer spec) : base(spec)
        {
            spectrometer = spec;
            defaultValues = false;
            andorDriver = spec.andorDriver;

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
        public override bool read()
        {
            AndorSpectrometer a = spectrometer as AndorSpectrometer;
            model = "";

            serialNumber = "";

            wavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };

            baudRate = 0;

            hasCooling = true;
            hasBattery = false;
            hasLaser = false;

            excitationNM = 0;

            slitSizeUM = 0;

            int minTemp = 0;
            int maxTemp = 0;
            int xPixels = 0;
            int yPixels = 0;
            andorDriver.GetTemperatureRange(ref minTemp, ref maxTemp);
            andorDriver.GetDetector(ref xPixels, ref yPixels);

            startupIntegrationTimeMS = (ushort)a.integrationTimeMS;
            double temp = a.detectorTECSetpointDegC;
            startupDetectorTemperatureDegC = (short)temp;
            startupTriggeringMode = 0;
            detectorGain = 0;
            detectorOffset = 0;
            detectorGainOdd = 0;
            detectorOffsetOdd = 0;

            degCToDACCoeffs[0] = 0;
            degCToDACCoeffs[1] = 0;
            degCToDACCoeffs[2] = 0;

            //the min and max temps from the driver are known to be inaccurate, so we use const values
            detectorTempMax = effectiveMaxTemp;
            detectorTempMin = effectiveMinTemp;
            //detectorTempMax = (short)maxTemp;
            //detectorTempMin = (short)minTemp;
            adcToDegCCoeffs[0] = 0;
            adcToDegCCoeffs[1] = 0;
            adcToDegCCoeffs[2] = 0;
            thermistorResistanceAt298K = 0;
            thermistorBeta = 0;
            calibrationDate = "";
            calibrationBy = "";
            int cameraSerial = 0;
            uint error = andorDriver.GetCameraSerialNumber(ref cameraSerial);
            if (error != AndorSpectrometer.DRV_SUCCESS)
                detectorSerialNumber = "";
            else
                detectorSerialNumber = "CCD-" + cameraSerial.ToString();
            detectorName = "iDus";
            activePixelsHoriz = (ushort)xPixels;
            activePixelsVert = (ushort)(yPixels / AndorSpectrometer.BINNING);
            minIntegrationTimeMS = a.integrationTimeMS;
            maxIntegrationTimeMS = uint.MaxValue;
            actualPixelsHoriz = (ushort)xPixels;
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

            laserExcitationWavelengthNMFloat = 830.0f;

            avgResolution = 0.0f;

            userData = new byte[63];
            subformat = PAGE_SUBFORMAT.INTENSITY_CALIBRATION;

            badPixelSet = new SortedSet<short>();
            productConfiguration = "";
            intensityCorrectionOrder = 0;
            featureMask.gen15 = false;

            return true;
        }

        public override bool write(bool allPages = false)
        {
            defaultValues = false;
            return true;
        }

        public override async Task<bool> readAsync()
        {
            AndorSpectrometer a = spectrometer as AndorSpectrometer;
            model = "";

            serialNumber = "";

            wavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };

            baudRate = 0;

            hasCooling = true;
            hasBattery = false;
            hasLaser = false;

            excitationNM = 0;

            slitSizeUM = 0;

            int minTemp = 0;
            int maxTemp = 0;
            int xPixels = 0;
            int yPixels = 0;
            andorDriver.GetTemperatureRange(ref minTemp, ref maxTemp);
            andorDriver.GetDetector(ref xPixels, ref yPixels);

            startupIntegrationTimeMS = (ushort)a.integrationTimeMS;
            double temp = a.detectorTECSetpointDegC;
            startupDetectorTemperatureDegC = (short)temp;
            startupTriggeringMode = 0;
            detectorGain = 0;
            detectorOffset = 0;
            detectorGainOdd = 0;
            detectorOffsetOdd = 0;

            degCToDACCoeffs[0] = 0;
            degCToDACCoeffs[1] = 0;
            degCToDACCoeffs[2] = 0;

            //the min and max temps from the driver are known to be inaccurate, so we use const values
            detectorTempMax = effectiveMaxTemp;
            detectorTempMin = effectiveMinTemp;
            //detectorTempMax = (short)maxTemp;
            //detectorTempMin = (short)minTemp;
            adcToDegCCoeffs[0] = 0;
            adcToDegCCoeffs[1] = 0;
            adcToDegCCoeffs[2] = 0;
            thermistorResistanceAt298K = 0;
            thermistorBeta = 0;
            calibrationDate = "";
            calibrationBy = "";
            int cameraSerial = 0;
            uint error = andorDriver.GetCameraSerialNumber(ref cameraSerial);
            if (error != AndorSpectrometer.DRV_SUCCESS)
                detectorSerialNumber = "";
            else
                detectorSerialNumber = "CCD-" + cameraSerial.ToString();
            detectorName = "iDus";
            activePixelsHoriz = (ushort)xPixels;
            activePixelsVert = (ushort)(yPixels / AndorSpectrometer.BINNING);
            minIntegrationTimeMS = a.integrationTimeMS;
            maxIntegrationTimeMS = uint.MaxValue;
            actualPixelsHoriz = (ushort)xPixels;
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

            laserExcitationWavelengthNMFloat = 830.0f;

            avgResolution = 0.0f;

            userData = new byte[63];
            subformat = PAGE_SUBFORMAT.INTENSITY_CALIBRATION;

            badPixelSet = new SortedSet<short>();
            productConfiguration = "";
            intensityCorrectionOrder = 0;
            featureMask.gen15 = false;

            return true;
        }

        public override async Task<bool> writeAsync(bool allPages = false)
        {
            defaultValues = false;
            return true;
        }

        public string detectorSerialNumber
        {
            get { return _detectorSerialNumber; }
            set
            {
                _detectorSerialNumber = value;
                base.OnEEPROMChanged(new EventArgs());
            }
        }

        string _detectorSerialNumber;

        /// <summary>
        /// Load an external JSON file containing key EEPROM attributes.
        /// </summary>
        /// <param name="pathname"></param>
        /// <returns>true on success</returns>
        /// <remarks>
        /// Sample JSON:
        /// {
        ///   "detector_serial_number": "CCD-26826",
        ///   "excitation_nm_float": 1063.83,
        ///   "raman_intensity_calibration_order": 5,
        ///   "raman_intensity_coeffs": [ 0.005259877, -0.001453733, 2.027716e-05, -1.015509e-07, 2.142931e-10, -1.533194e-13 ],
        ///   "wavelength_coeffs": [ 1081.88, 0.6353086, -7.287224e-06, -2.989192e-08, 0.0 ],
        ///   "wp_model": "WP-1064XL-F15-XR-IC",
        ///   "wp_serial_number": "WP-01265",
        ///   "invert_x_axis": true
        /// }
        /// </remarks>
        internal bool loadFromJSON(string pathname)
        {
            AndorEEPROMJSON json = null;
            try
            {
                string text = File.ReadAllText(pathname);
                json = JsonConvert.DeserializeObject<AndorEEPROMJSON>(text);
                logger.debug("successfully deserialized AndorEEPROMJSON");
            }
            catch (JsonReaderException)
            {
                logger.error($"unable to load or parse {pathname}");
                return false;
            }

            model = json.wp_model;
            serialNumber = json.wp_serial_number;
            detectorName = json.detector_type;
            detectorSerialNumber = json.detector_serial_number;
            laserExcitationWavelengthNMFloat = (float)json.excitation_nm_float;

            wavecalCoeffs = json.wavelength_coeffs.Select(d => (float)d).ToArray();
            if (json.raman_intensity_coeffs.Length > 0)
            {
                intensityCorrectionCoeffs = json.raman_intensity_coeffs.Select(d => (float)d).ToArray();
                intensityCorrectionOrder = (byte)(intensityCorrectionCoeffs.Length - 1);
            }
            else
            {
                intensityCorrectionOrder = 0;
                intensityCorrectionCoeffs = null;
            }

            return true;
        }
    }
}
#endif
