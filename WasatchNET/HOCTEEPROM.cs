using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class HOCTEEPROM : EEPROM
    {
        internal HOCTEEPROM(HOCTSpectrometer spec) : base(spec)
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
            byte[] buffer = new byte[32];

            if (!ParseData.writeString(serialNumber, buffer, 0, 16)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[0], buffer, 16)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[1], buffer, 20)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[2], buffer, 24)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[3], buffer, 28)) return false;

            bool writeOK = HOCTSpectrometer.OctUsb.WriteCalibration(0, buffer);
            return writeOK;
        }

        public override bool read()
        {
            HOCTSpectrometer a = spectrometer as HOCTSpectrometer;
            model = "";

            serialNumber = "";

            baudRate = 0;

            hasCooling = false;
            hasBattery = false;
            hasLaser = false;

            excitationNM = 0;

            slitSizeUM = 0;

            bool readOk = false;
            byte[] buffer = HOCTSpectrometer.OctUsb.ReadCalibration(ref readOk);

            if (!readOk)
            {
                serialNumber = ParseData.toString(buffer, 0, 16);

                wavecalCoeffs[0] = ParseData.toFloat(buffer, 16);
                wavecalCoeffs[1] = ParseData.toFloat(buffer, 20);
                wavecalCoeffs[2] = ParseData.toFloat(buffer, 24);
                wavecalCoeffs[3] = ParseData.toFloat(buffer, 28);
            }

            startupIntegrationTimeMS = (ushort)HOCTSpectrometer.OctUsb.DefaultIntegrationTime();
            double temp = a.detectorTemperatureDegC;
            startupDetectorTemperatureDegC = (short)temp;
            if (startupDetectorTemperatureDegC >= 99)
                startupDetectorTemperatureDegC = 15;
            else if (startupDetectorTemperatureDegC <= -50)
                startupDetectorTemperatureDegC = 15;
            startupTriggeringMode = 0;
            detectorGain = 0;
            detectorOffset = 0;
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
            activePixelsHoriz = (ushort)a.pixels;
            activePixelsVert = (ushort)HOCTSpectrometer.OctUsb.NUM_OF_LINES_PER_FRAME;
            minIntegrationTimeMS = 98;
            maxIntegrationTimeMS = 33600;
            actualPixelsHoriz = (ushort)a.pixels;
            ROIHorizStart = 0;
            ROIHorizEnd = (ushort)(a.pixels - 1);
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
            laserExcitationWavelengthNMFloat = 0.0f;

            avgResolution = 0.0f;

            userData = new byte[63];

            badPixelSet = new SortedSet<short>();
            productConfiguration = "";

            intensityCorrectionOrder = 0;

            return true;
        }

    }
}
