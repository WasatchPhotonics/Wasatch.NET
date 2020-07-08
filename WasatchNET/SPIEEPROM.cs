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

        public override bool write()
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

        public override bool read()
        {
            SPISpectrometer a = spectrometer as SPISpectrometer;

            pages = a.getEEPROMPages();

            format = pages[0][63];

            //this if block checks for unwritten EEPROM (indicated by 0xff) and fills our virtual EEPROM with sane default values
            //this will prevent us from upping the format to version 255(6?) but the tradeoff seems worth it
            if (format == 0xff)
            {
                model = "";



                serialNumber = a.serialNumber;


                baudRate = 0;

                hasCooling = false;
                hasBattery = false;
                hasLaser = false;

                excitationNM = 0;

                slitSizeUM = 0;

                byte[] buffer = new byte[16];

                string test = buffer.ToString();


                wavecalCoeffs = new float[] { 0, 1, 0, 0, 0 };


                startupIntegrationTimeMS = 0;
                double temp = 0;
                startupDetectorTemperatureDegC = (short)temp;
                if (startupDetectorTemperatureDegC >= 99)
                    startupDetectorTemperatureDegC = 15;
                else if (startupDetectorTemperatureDegC <= -50)
                    startupDetectorTemperatureDegC = 15;
                startupTriggeringMode = 2;
                detectorGain = a.detectorGain;
                detectorOffset = a.detectorOffset;
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
                calibrationDate = "01/01/2020";
                calibrationBy = "RSC";

                detectorName = "";
                activePixelsHoriz = (ushort)a.pixels;
                activePixelsVert = 0;
                minIntegrationTimeMS = 1;
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

                laserExcitationWavelengthNMFloat = 785.0f;

                avgResolution = 0.0f;

                userData = new byte[63];

                badPixelSet = new SortedSet<short>();
                productConfiguration = "";

                //needs work
                intensityCorrectionOrder = 0;
            }

            //if the format type has been written we will assume sane EEPROM values
            else
            {
                try
                {
                    model = ParseData.toString(pages[0], 0, 16);
                    serialNumber = ParseData.toString(pages[0], 16, 16);
                    baudRate = ParseData.toUInt32(pages[0], 32);
                    hasCooling = ParseData.toBool(pages[0], 36);
                    hasBattery = ParseData.toBool(pages[0], 37);
                    hasLaser = ParseData.toBool(pages[0], 38);
                    excitationNM = ParseData.toUInt16(pages[0], 39);
                    slitSizeUM = ParseData.toUInt16(pages[0], 41);

                    startupIntegrationTimeMS = ParseData.toUInt16(pages[0], 43);
                    startupDetectorTemperatureDegC = ParseData.toInt16(pages[0], 45);
                    startupTriggeringMode = ParseData.toUInt8(pages[0], 47);
                    detectorGain = ParseData.toFloat(pages[0], 48); // "even pixels" for InGaAs
                    detectorOffset = ParseData.toInt16(pages[0], 52); // "even pixels" for InGaAs
                    detectorGainOdd = ParseData.toFloat(pages[0], 54); // InGaAs-only
                    detectorOffsetOdd = ParseData.toInt16(pages[0], 58); // InGaAs-only

                    wavecalCoeffs[0] = ParseData.toFloat(pages[1], 0);
                    wavecalCoeffs[1] = ParseData.toFloat(pages[1], 4);
                    wavecalCoeffs[2] = ParseData.toFloat(pages[1], 8);
                    wavecalCoeffs[3] = ParseData.toFloat(pages[1], 12);
                    wavecalCoeffs[4] = 0;
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
                    laserExcitationWavelengthNMFloat = ParseData.toFloat(pages[3], 36);
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

                    if (format >= 6)
                    {
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
                        subformat = (PAGE_SUBFORMAT)ParseData.toUInt8(pages[5], 63);
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
                    }
                    else
                    {
                        if (format >= 6)
                            subformat = PAGE_SUBFORMAT.INTENSITY_CALIBRATION;
                        else
                            subformat = PAGE_SUBFORMAT.USER_DATA;
                    }


                }
                catch (Exception ex)
                {
                    logger.error("EEPROM: caught exception: {0}", ex.Message);
                    return false;
                }
            }
            if (logger.debugEnabled())
                dump();

            enforceReasonableDefaults();

            format = FORMAT;

            return true;
        }

    }
}
