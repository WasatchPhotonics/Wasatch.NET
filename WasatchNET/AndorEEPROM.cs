﻿#if WIN32 || x64
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