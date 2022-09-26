#if WIN32 || x64
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Task<bool> task = Task.Run(async () => await readAsync());
            return task.Result;
        }

        public override bool write(bool allPages = false)
        {
            Task<bool> task = Task.Run(async () => await writeAsync(allPages));
            return task.Result;
        }

        public override async Task<bool> readAsync()
        {
            AndorSpectrometer a = spectrometer as AndorSpectrometer;

            setDefault(spectrometer);
            serialNumber = "";
            hasCooling = true;

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

            //the min and max temps from the driver are known to be inaccurate, so we use const values
            detectorTempMax = effectiveMaxTemp;
            detectorTempMin = effectiveMinTemp;
            //detectorTempMax = (short)maxTemp;
            //detectorTempMin = (short)minTemp;

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

    }
}
#endif