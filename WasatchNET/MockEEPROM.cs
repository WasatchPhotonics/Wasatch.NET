using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class MockEEPROM : EEPROM
    {
        internal MockEEPROM(MockSpectrometer spec) : base(spec)
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

        public override bool write(bool allPages=false)
        {
            return true;
        }

        public override bool read()
        {
            MockSpectrometer a = spectrometer as MockSpectrometer;
            model = "";

            serialNumber = "";

            baudRate = 0;

            hasCooling = true;
            hasBattery = false;
            hasLaser = false;

            excitationNM = 0;

            slitSizeUM = 0;

            byte[] buffer = new byte[16];

            string test = buffer.ToString();

            startupIntegrationTimeMS = 8;
            //double temp = a.detectorTemperatureDegC;
            startupDetectorTemperatureDegC = 15;
            if (startupDetectorTemperatureDegC >= 99)
                startupDetectorTemperatureDegC = 15;
            else if (startupDetectorTemperatureDegC <= -50)
                startupDetectorTemperatureDegC = 15;
            startupTriggeringMode = 2;
            detectorGain = 0;
            detectorOffset = 0;
            detectorGainOdd = 0;
            detectorOffsetOdd = 0;

            degCToDACCoeffs[0] = 0;
            degCToDACCoeffs[1] = 0;
            degCToDACCoeffs[2] = 0;
            detectorTempMax = 25;
            detectorTempMin = 10;
            adcToDegCCoeffs[0] = 0;
            adcToDegCCoeffs[1] = 0;
            adcToDegCCoeffs[2] = 0;
            thermistorResistanceAt298K = 0;
            thermistorBeta = 0;
            calibrationDate = "";
            calibrationBy = "";

            detectorName = "";
            activePixelsHoriz = (ushort)a.pixels;
            activePixelsVert = 0;
            minIntegrationTimeMS = 8;
            maxIntegrationTimeMS = 1000000;
            actualPixelsHoriz = (ushort)a.pixels;
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

            badPixelSet = new SortedSet<short>();
            productConfiguration = "";

            intensityCorrectionOrder = 0;

            return true;
        }

    }
}
