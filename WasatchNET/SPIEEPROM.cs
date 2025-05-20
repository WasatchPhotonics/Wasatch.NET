using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public override bool read(bool skipRead = false)
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
                parsePages();
            }

            if (logger.debugEnabled())
                dump();

            enforceReasonableDefaults();
            defaultValues = false;
            featureMask.gen15 = false;

            format = FORMAT;

            return true;
        }

        bool parsePages()
        {

            format = pages[0][63];

            if (format > FORMAT)
            {
                // log but optimistically proceed
                logger.error("WasatchNET {0} was built and tested against EEPROM formats {1} and below.",
                    Assembly.GetExecutingAssembly().GetName().Version.ToString(), FORMAT);
                logger.error("EEPROM format {0} may require a newer version of WasatchNET for proper operation.", format);
            }

            if (format >= 8)
                subformat = (PAGE_SUBFORMAT)ParseData.toUInt8(pages[5], 63);
            else if (format >= 6)
                subformat = PAGE_SUBFORMAT.INTENSITY_CALIBRATION;
            else
                subformat = PAGE_SUBFORMAT.USER_DATA;

            ////////////////////////////////////////////////////////////////
            // newer formats support more pages
            ////////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////
            // parse pages according to format and subformat
            ////////////////////////////////////////////////////////////////

            try
            {
                model = ParseData.toString(pages[0], 0, 16);
                serialNumber = ParseData.toString(pages[0], 16, 16);
                baudRate = ParseData.toUInt32(pages[0], 32);
                hasCooling = ParseData.toBool(pages[0], 36);
                hasBattery = ParseData.toBool(pages[0], 37);
                hasLaser = ParseData.toBool(pages[0], 38);
                excitationNM = ParseData.toUInt16(pages[0], 39); // for old formats, first read this as excitation
                slitSizeUM = ParseData.toUInt16(pages[0], 41);

                startupIntegrationTimeMS = ParseData.toUInt16(pages[0], 43);
                TECSetpoint = ParseData.toInt16(pages[0], 45);
                startupTriggeringMode = ParseData.toUInt8(pages[0], 47);
                detectorGain = ParseData.toFloat(pages[0], 48); // "even pixels" for InGaAs
                detectorOffset = ParseData.toInt16(pages[0], 52); // "even pixels" for InGaAs
                detectorGainOdd = ParseData.toFloat(pages[0], 54); // InGaAs-only
                detectorOffsetOdd = ParseData.toInt16(pages[0], 58); // InGaAs-only
                if (format >= 16)
                    laserTECSetpoint = ParseData.toUInt16(pages[0], 60);
                else
                    laserTECSetpoint = 800;

                wavecalCoeffs[0] = ParseData.toFloat(pages[1], 0);
                wavecalCoeffs[1] = ParseData.toFloat(pages[1], 4);
                wavecalCoeffs[2] = ParseData.toFloat(pages[1], 8);
                wavecalCoeffs[3] = ParseData.toFloat(pages[1], 12);
                degCToDACCoeffs[0] = ParseData.toFloat(pages[1], 16);
                degCToDACCoeffs[1] = ParseData.toFloat(pages[1], 20);
                degCToDACCoeffs[2] = ParseData.toFloat(pages[1], 24);
                detectorTempMax = ParseData.toInt16(pages[1], 28);
                detectorTempMin = ParseData.toInt16(pages[1], 30);
                adcToDegCCoeffs[0] = ParseData.toFloat(pages[1], 32);
                adcToDegCCoeffs[1] = ParseData.toFloat(pages[1], 36);
                adcToDegCCoeffs[2] = ParseData.toFloat(pages[1], 40);
                thermistorResistanceAt298K = ParseData.toInt16(pages[1], 44);
                thermistorBeta = ParseData.toInt16(pages[1], 46);
                calibrationDate = ParseData.toString(pages[1], 48, 12);
                calibrationBy = ParseData.toString(pages[1], 60, 3);

                detectorName = ParseData.toString(pages[2], 0, 16);
                activePixelsHoriz = ParseData.toUInt16(pages[2], 16); // note: byte 18 unused
                activePixelsVert = ParseData.toUInt16(pages[2], 19);
                minIntegrationTimeMS = ParseData.toUInt16(pages[2], 21); // will overwrite if 
                maxIntegrationTimeMS = ParseData.toUInt16(pages[2], 23); //   format >= 5
                actualPixelsHoriz = ParseData.toUInt16(pages[2], 25);
                ROIHorizStart = ParseData.toUInt16(pages[2], 27);
                ROIHorizEnd = ParseData.toUInt16(pages[2], 29);
                ROIVertRegionStart[0] = ParseData.toUInt16(pages[2], 31);
                ROIVertRegionEnd[0] = ParseData.toUInt16(pages[2], 33);
                ROIVertRegionStart[1] = ParseData.toUInt16(pages[2], 35);
                ROIVertRegionEnd[1] = ParseData.toUInt16(pages[2], 37);
                ROIVertRegionStart[2] = ParseData.toUInt16(pages[2], 39);
                ROIVertRegionEnd[2] = ParseData.toUInt16(pages[2], 41);
                linearityCoeffs[0] = ParseData.toFloat(pages[2], 43);
                linearityCoeffs[1] = ParseData.toFloat(pages[2], 47);
                linearityCoeffs[2] = ParseData.toFloat(pages[2], 51);
                linearityCoeffs[3] = ParseData.toFloat(pages[2], 55);
                linearityCoeffs[4] = ParseData.toFloat(pages[2], 59);

                // deviceLifetimeOperationMinutes = ParseData.toInt32(pages[3], 0);
                // laserLifetimeOperationMinutes = ParseData.toInt32(pages[3], 4);
                // laserTemperatureMax  = ParseData.toInt16(pages[3], 8);
                // laserTemperatureMin  = ParseData.toInt16(pages[3], 10);

                laserPowerCoeffs[0] = ParseData.toFloat(pages[3], 12);
                laserPowerCoeffs[1] = ParseData.toFloat(pages[3], 16);
                laserPowerCoeffs[2] = ParseData.toFloat(pages[3], 20);
                laserPowerCoeffs[3] = ParseData.toFloat(pages[3], 24);
                maxLaserPowerMW = ParseData.toFloat(pages[3], 28);
                minLaserPowerMW = ParseData.toFloat(pages[3], 32);

                // correct laser excitation across formats
                if (format >= 4)
                {
                    laserExcitationWavelengthNMFloat = ParseData.toFloat(pages[3], 36);
                    excitationNM = (ushort)Math.Round(laserExcitationWavelengthNMFloat);
                }
                else
                {
                    laserExcitationWavelengthNMFloat = excitationNM;
                }

                if (format >= 5)
                {
                    minIntegrationTimeMS = ParseData.toUInt32(pages[3], 40);
                    maxIntegrationTimeMS = ParseData.toUInt32(pages[3], 44);
                }

                userData = format < 4 ? new byte[63] : new byte[64];
                Array.Copy(pages[4], userData, userData.Length);

                badPixelSet = new SortedSet<short>();
                for (int i = 0; i < 15; i++)
                {
                    short pixel = ParseData.toInt16(pages[5], i * 2);
                    badPixels[i] = pixel;
                    if (pixel >= 0)
                        badPixelSet.Add(pixel); // does not throw
                }
                badPixelList = new List<short>(badPixelSet);

                if (format >= 5)
                    productConfiguration = ParseData.toString(pages[5], 30, 16);
                else
                    productConfiguration = "";

                if (format >= 6 && (subformat == PAGE_SUBFORMAT.INTENSITY_CALIBRATION ||
                                    subformat == PAGE_SUBFORMAT.UNTETHERED_DEVICE))
                {
                    // load Raman Intensity Correction whether subformat is 1 or 3
                    logger.debug("loading Raman Intensity Correction");
                    intensityCorrectionOrder = ParseData.toUInt8(pages[6], 0);
                    uint numCoeffs = (uint)intensityCorrectionOrder + 1;

                    if (numCoeffs > 8)
                        numCoeffs = 0;

                    intensityCorrectionCoeffs = numCoeffs > 0 ? new float[numCoeffs] : null;

                    for (int i = 0; i < numCoeffs; ++i)
                    {
                        intensityCorrectionCoeffs[i] = ParseData.toFloat(pages[6], 1 + 4 * i);
                    }
                }
                else
                {
                    intensityCorrectionOrder = 0;
                }

                if (format >= 7)
                {
                    avgResolution = ParseData.toFloat(pages[3], 48);
                }
                else
                {
                    avgResolution = 0.0f;
                }

                if (format >= 8)
                {
                    wavecalCoeffs[4] = ParseData.toFloat(pages[2], 21);
                    if (subformat == PAGE_SUBFORMAT.USER_DATA)
                    {
                        intensityCorrectionOrder = 0;
                        intensityCorrectionCoeffs = null;

                        userData = new byte[192];
                        //Array.Copy(pages[4], userData, userData.Length);
                        Array.Copy(pages[4], 0, userData, 0, 64);
                        Array.Copy(pages[6], 0, userData, 64, 64);
                        Array.Copy(pages[7], 0, userData, 128, 64);
                    }

                    /*
                    userData = new byte[16000];
                    Array.Copy(pages[4], 0, userData, 0, 64);
                    Array.Copy(pages[7], 0, userData, 64, 64);

                    for (int k = 8; k < 256; ++k)
                    {
                        Array.Copy(pages[k], 0, userData, 64 * (k - 6), 64);
                    }
                    */
                }

                if (format >= 9)
                    featureMask = new FeatureMask(ParseData.toUInt16(pages[0], 39));

                if (format >= 15)
                {
                    laserWatchdogTimer = ParseData.toUInt16(pages[3], 52);
                    lightSourceType = (LIGHT_SOURCE_TYPE)ParseData.toUInt8(pages[3], 54);
                }
                else
                {
                    laserWatchdogTimer = 0;
                    lightSourceType = 0;
                }

                if (format >= 16)
                {
                    powerWatchdogTimer = ParseData.toUInt16(pages[3], 55);
                    detectorTimeout = ParseData.toUInt16(pages[3], 57);
                    horizontalBinningMethod = (HORIZONTAL_BINNING_METHOD)ParseData.toUInt8(pages[3], 59);
                }
                else
                {
                    powerWatchdogTimer = 0;
                    detectorTimeout = 0;
                    horizontalBinningMethod = HORIZONTAL_BINNING_METHOD.BIN_2X2;
                }


                if (format < 12)
                    featureMask.evenOddHardwareCorrected = false;
                if (format >= 10)
                    laserWarmupSec = pages[2][18];
                else
                    laserWarmupSec = 20;
                if (format < 15)
                    featureMask.hasShutter = false;
                if (format < 16)
                {
                    featureMask.disableBLEPower = false;
                    featureMask.disableLaserArmedIndication = false;
                }


                if (format >= 11)
                {
                    if (subformat == PAGE_SUBFORMAT.DETECTOR_REGIONS)
                    {
                        region1WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };
                        region2WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };
                        region3WavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };

                        region1HorizStart = ParseData.toUInt16(pages[6], 0);
                        region1HorizEnd = ParseData.toUInt16(pages[6], 2);
                        region1WavecalCoeffs[0] = ParseData.toFloat(pages[6], 4);
                        region1WavecalCoeffs[1] = ParseData.toFloat(pages[6], 8);
                        region1WavecalCoeffs[2] = ParseData.toFloat(pages[6], 12);
                        region1WavecalCoeffs[3] = ParseData.toFloat(pages[6], 16);

                        region2HorizStart = ParseData.toUInt16(pages[6], 20);
                        region2HorizEnd = ParseData.toUInt16(pages[6], 22);
                        region2WavecalCoeffs[0] = ParseData.toFloat(pages[6], 24);
                        region2WavecalCoeffs[1] = ParseData.toFloat(pages[6], 28);
                        region2WavecalCoeffs[2] = ParseData.toFloat(pages[6], 32);
                        region2WavecalCoeffs[3] = ParseData.toFloat(pages[6], 36);

                        region3VertStart = ParseData.toUInt16(pages[6], 40);
                        region3VertEnd = ParseData.toUInt16(pages[6], 42);
                        region3HorizStart = ParseData.toUInt16(pages[6], 44);
                        region3HorizEnd = ParseData.toUInt16(pages[6], 46);
                        region3WavecalCoeffs[0] = ParseData.toFloat(pages[6], 48);
                        region3WavecalCoeffs[1] = ParseData.toFloat(pages[6], 52);
                        region3WavecalCoeffs[2] = ParseData.toFloat(pages[6], 56);
                        region3WavecalCoeffs[3] = ParseData.toFloat(pages[6], 60);

                        regionCount = ParseData.toUInt8(pages[7], 0);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.error("EEPROM: caught exception: {0}", ex.Message);
                return false;
            }

            if (logger.debugEnabled())
                dump();

            enforceReasonableDefaults();

            format = FORMAT;

            return true;

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

        public override async Task<bool> readAsync(bool skipRead = false)
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
