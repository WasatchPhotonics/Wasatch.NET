using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    class MockSpectrometerJSON
    {
        public Dictionary<string, SortedDictionary<int, double[]>> measurements;
        public EEPROMJSON EEPROM;
    }

    public class EEPROMJSON
    {
        public string Serial;
        public string Model;
        public int SlitWidth;
        public int BaudRate;
        public bool IncBattery;
        public bool IncCooling;
        public bool IncLaser;
        public int StartupIntTimeMS;
        public int StartupTempC;
        public int StartupTriggerMode;
        public double DetectorGain;
        public double DetectorGainOdd;
        public int DetectorOffset;
        public int DetectorOffsetOdd;
        public double[] WavecalCoeffs;
        public double[] TempToDACCoeffs;
        public double[] ADCToTempCoeffs;
        public double[] LinearityCoeffs;
        public int DetectorTempMax;
        public int DetectorTempMin;
        public int ThermistorBeta;
        public int ThermistorResAt298K;
        public string CalibrationDate;
        public string CalibrationBy;
        public string DetectorName;
        public int ActualPixelsHoriz;
        public int ActivePixelsHoriz;
        public int ActivePixelsVert;
        public int MinIntegrationTimeMS;
        public int MaxIntegrationTimeMS;
        public int ROIHorizStart;
        public int ROIHorizEnd;
        public int[] ROIVertRegionStarts;
        public int[] ROIVertRegionEnds;
        public double MaxLaserPowerMW;
        public double MinLaserPowerMW;
        public double ExcitationWavelengthNM;
        public int[] BadPixels;
        public string UserText;
        public string ProductConfig;
        public int RelIntCorrOrder;
        public double[] RelIntCorrCoeff;
    }

}
