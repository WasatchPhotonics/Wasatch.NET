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

        public override bool write(bool allPages = false)
        {
            Task<bool> task = Task.Run(async () => await writeAsync(allPages));
            return task.Result;
        }

        public override bool read()
        {
            Task<bool> task = Task.Run(async () => await readAsync());
            return task.Result;
        }

        public override async Task<bool> writeAsync(bool allPages=false)
        {
            return true;
        }

        public override async Task<bool> readAsync()
        {
            MockSpectrometer a = spectrometer as MockSpectrometer;
            setDefault(spectrometer);
            serialNumber = "";
            hasCooling = true;
            startupIntegrationTimeMS = 8;
            //double temp = a.detectorTemperatureDegC;
            startupDetectorTemperatureDegC = 15;
            detectorGain = 0;
            detectorOffset = 0;

            detectorTempMax = 25;
            detectorTempMin = 10;

            detectorName = "";
            activePixelsHoriz = (ushort)a.pixels;
            activePixelsVert = 0;
            minIntegrationTimeMS = 8;
            maxIntegrationTimeMS = 1000000;
            actualPixelsHoriz = (ushort)a.pixels;
            laserExcitationWavelengthNMFloat = 830.0f;

            featureMask.gen15 = false;
            format = FORMAT;

            return true;
        }

    }
}
