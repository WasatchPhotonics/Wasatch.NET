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
            BoulderSpectrometer a = spectrometer as BoulderSpectrometer;

            setDefault(spectrometer);

            serialNumber = "";

            hasCooling = true;
            int errorReader = 0;
            startupIntegrationTimeMS = (ushort)(SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(a.specIndex, ref errorReader) / 1000);
            double temp = a.detectorTemperatureDegC;
            TECSetpoint = (short)temp;
            if (TECSetpoint >= 99)
                TECSetpoint = 15;
            else if (TECSetpoint <= -50)
                TECSetpoint = 15;
            detectorGain = 0;
            detectorOffset = 0;

            detectorTempMax = 25;
            detectorTempMin = 10;
            activePixelsHoriz = (ushort)a.pixels;
            activePixelsVert = 0;
            minIntegrationTimeMS = (ushort)(SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(a.specIndex, ref errorReader) / 1000);
            maxIntegrationTimeMS = 1000000;
            actualPixelsHoriz = (ushort)a.pixels;
            laserExcitationWavelengthNMFloat = 830.0f;

            featureMask.gen15 = false;

            return true;
        }

        public override async Task<bool> writeAsync(bool allPages=false)
        {
            defaultValues = false;
            return true;
        }

    }
}
