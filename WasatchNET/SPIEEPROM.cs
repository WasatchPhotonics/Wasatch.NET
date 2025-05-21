using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class SPIEEPROM : EEPROM
    {
        internal SPIEEPROM(SPISpectrometer spec) : base(spec)
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
            if (pages is null || pages.Count != MAX_PAGES)
            {
                logger.error("EEPROM.write: need to perform a read first");
                return false;
            }

            if (!writeParse())
                return false;

            SPISpectrometer a = spectrometer as SPISpectrometer;

            bool writeOk = a.writeEEPROM(pages);
            if (writeOk)
                defaultValues = false;

            return writeOk;
        }

        public override async Task<bool> readAsync()
        {
            SPISpectrometer a = spectrometer as SPISpectrometer;

            pages = a.getEEPROMPages();

            format = pages[0][63];

            //this if block checks for unwritten EEPROM (indicated by 0xff) and fills our virtual EEPROM with sane default values
            //this will prevent us from upping the format to version 255(6?) but the tradeoff seems worth it
            if (format == 0xff)
            {
                setDefault(spectrometer);

                calibrationDate = "01/01/2020";
                calibrationBy = "RSC";

                activePixelsHoriz = (ushort)a.pixels;
                activePixelsVert = 0;
                actualPixelsHoriz = (ushort)a.pixels;
            }

            //if the format type has been written we will assume sane EEPROM values
            else
            {
                base.read();
            }

            if (logger.debugEnabled())
                dump();

            enforceReasonableDefaults();
            defaultValues = false;
            featureMask.gen15 = false;

            format = FORMAT;

            return true;
        }

    }
}
