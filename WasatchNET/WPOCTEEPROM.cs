using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class WPOCTEEPROM : EEPROM
    {
        IWPOCTCamera camera = null;

        internal WPOCTEEPROM(WPOCTSpectrometer spec, IWPOCTCamera camera) : base(spec)
        {
            spectrometer = spec;
            this.camera = camera;

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

        public override bool read(bool skipRead = false)
        {
            Task<bool> task = Task.Run(async () => await readAsync());
            return task.Result;
        }

        public override async Task<bool> writeAsync(bool allPages = false)
        {
            byte[] buffer = new byte[32];

            if (!ParseData.writeString(serialNumber, buffer, 0, 16)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[0], buffer, 16)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[1], buffer, 20)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[2], buffer, 24)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[3], buffer, 28)) return false;

            bool writeOK = true; //HOCTSpectrometer.OctUsb.WriteCalibration(0, buffer);
            return writeOK;
        }

        public override async Task<bool> readAsync(bool skipRead = false)
        {
            WPOCTSpectrometer a = spectrometer as WPOCTSpectrometer;
            setDefault(spectrometer);
            serialNumber = detectorSerialNumber = a.camID;

            double temp = a.detectorTemperatureDegC;
            TECSetpoint = (short)temp;
            if (TECSetpoint >= 99)
                TECSetpoint = 15;
            else if (TECSetpoint <= -50)
                TECSetpoint = 15;
            startupTriggeringMode = 0;
            detectorGain = 0;
            detectorOffset = 0;

            activePixelsHoriz = (ushort)a.pixels;
            activePixelsVert = (ushort)camera.GetScanHeight();
            minIntegrationTimeMS = 13;
            maxIntegrationTimeMS = 655;
            actualPixelsHoriz = (ushort)a.pixels;
            ROIHorizStart = 0;
            ROIHorizEnd = (ushort)(a.pixels - 1);

            laserExcitationWavelengthNMFloat = 0.0f;
            featureMask.gen15 = false;

            return true;
        }
    }
}
