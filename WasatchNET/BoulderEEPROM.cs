using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class BoulderEEPROM : EEPROM
    {
        internal BoulderEEPROM(BoulderSpectrometer spec) : base(spec)
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

        public override bool read()
        {
            BoulderSpectrometer a = spectrometer as BoulderSpectrometer;
            model = "";

            serialNumber = "";

            baudRate = 0;

            hasCooling = true;
            hasBattery = false;
            hasLaser = false;

            excitationNM = 0;

            slitSizeUM = 0;

            byte[] buffer = new byte[16];
            int errorReader = 0;

            string test = buffer.ToString();

            startupIntegrationTimeMS = (ushort)(SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(a.specIndex, ref errorReader) / 1000);
            double temp = a.detectorTemperatureDegC;
            startupDetectorTemperatureDegC = (short)temp;
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
            calibrationDate = "01/01/2020";
            calibrationBy = "RSC";

            detectorName = "";
            activePixelsHoriz = (ushort)a.pixels;
            activePixelsVert = 0;
            minIntegrationTimeMS = (ushort)(SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(a.specIndex, ref errorReader) / 1000);
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

        public override bool write()
        {
            defaultValues = false;
            return true;
        }

    }
}
