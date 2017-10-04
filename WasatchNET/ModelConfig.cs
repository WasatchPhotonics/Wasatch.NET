using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    public class ModelConfig
    {
        Spectrometer spectrometer;
        Logger logger = Logger.getInstance();

        // Page 0 
        public string model { get; private set; }
        public string serialNumber { get; private set; }
        public int baudRate { get; private set; }
        public bool hasCooling { get; private set; }
        public bool hasBattery { get; private set; }
        public bool hasLaser { get; private set; }
        public short excitationNM { get; private set; }
        public short slitSizeUM { get; private set; }

        // Page 1
        public float[] wavecalCoeffs { get; private set; }
        public float[] detectorTempCoeffs { get; private set; }
        public float detectorTempMin { get; private set; }
        public float detectorTempMax { get; private set; }
        public float[] adcCoeffs { get; private set; }
        public short thermistorResistanceAt298K { get; private set; }
        public short thermistorBeta { get; private set; }
        public string calibrationDate { get; private set; }
        public string calibrationBy { get; private set; }

        // Page 2
        public string detectorName { get; private set; }
        public short activePixelsHoriz { get; private set; }
        public short activePixelsVert { get; private set; }
        public ushort minIntegrationTimeMS { get; private set; }
        public ushort maxIntegrationTimeMS { get; private set; }
        public short actualHoriz { get; private set; }
        public short ROIHorizStart { get; private set; }
        public short ROIHorizEnd { get; private set; }
        public short[] ROIVertRegionStart { get; private set; }
        public short[] ROIVertRegionEnd { get; private set; }
        // public float[] linearityCoeffs { get; private set; }

        // Page 3
        // public int deviceLifetimeOperationMinutes { get; private set; }
        // public int laserLifetimeOperationMinutes { get; private set; }
        // public short laserTemperatureMax { get; private set; }
        // public short laserTemperatureMin { get; private set; }

        // Page 4
        public byte[] userData { get; private set; } 
        public string userText { get { return ParseData.toString(userData); } }

        // Page 5
        public short[] badPixels { get; private set; }

        // Pages 6-7 unallocated

        public ModelConfig(Spectrometer spec)
        {
            spectrometer = spec;

            wavecalCoeffs = new float[4];
            detectorTempCoeffs = new float[3];
            adcCoeffs = new float[3];
            ROIVertRegionStart = new short[3];
            ROIVertRegionEnd = new short[3];
            badPixels = new short[15];
            // linearityCoeffs = new float[5];
        }

        public bool read()
        {
            List<byte[]> pages = new List<byte[]>();
            List<byte> format = new List<byte>();

            for (ushort page = 0; page < 6; page++)
            {
                byte[] buf = spectrometer.getCmd2(Opcodes.GET_MODEL_CONFIG, 64, wIndex: page);
                if (buf == null)
                    return false;

                pages.Add(buf);
                format.Add(buf[63]); // page format is always last byte
            }

            try 
            {
                model                   = ParseData.toString(pages[0],  0, 16);
                serialNumber            = ParseData.toString(pages[0], 16, 16);
                baudRate                = ParseData.toInt32 (pages[0], 32); 
                hasCooling              = ParseData.toBool  (pages[0], 36);
                hasBattery              = ParseData.toBool  (pages[0], 37);
                hasLaser                = ParseData.toBool  (pages[0], 38);
                excitationNM            = ParseData.toInt16 (pages[0], 39);
                slitSizeUM              = ParseData.toInt16 (pages[0], 41);

                wavecalCoeffs[0]        = ParseData.toFloat (pages[1],  0);
                wavecalCoeffs[1]        = ParseData.toFloat (pages[1],  4);
                wavecalCoeffs[2]        = ParseData.toFloat (pages[1],  8);
                wavecalCoeffs[3]        = ParseData.toFloat (pages[1], 12);
                detectorTempCoeffs[0]   = ParseData.toFloat (pages[1], 16);
                detectorTempCoeffs[1]   = ParseData.toFloat (pages[1], 20);
                detectorTempCoeffs[2]   = ParseData.toFloat (pages[1], 24);
                detectorTempMax         = ParseData.toInt16 (pages[1], 28);
                detectorTempMin         = ParseData.toInt16 (pages[1], 30);
                adcCoeffs[0]            = ParseData.toFloat(pages[1], 32);
                adcCoeffs[1]            = ParseData.toFloat(pages[1], 36);
                adcCoeffs[2]            = ParseData.toFloat(pages[1], 40);
                thermistorResistanceAt298K = ParseData.toInt16(pages[1], 44);
                thermistorBeta          = ParseData.toInt16(pages[1], 46);
                calibrationDate         = ParseData.toString(pages[1], 48, 12);
                calibrationBy           = ParseData.toString(pages[1], 60, 3);

                detectorName            = ParseData.toString(pages[2], 0, 16);
                activePixelsHoriz       = ParseData.toInt16(pages[2], 16); // note: byte 18 apparently unused
                activePixelsVert        = ParseData.toInt16(pages[2], 19);
                minIntegrationTimeMS    = ParseData.toUInt16(pages[2], 21);
                maxIntegrationTimeMS    = ParseData.toUInt16(pages[2], 23);
                actualHoriz             = ParseData.toInt16(pages[2], 25);
                ROIHorizStart           = ParseData.toInt16(pages[2], 27);
                ROIHorizEnd             = ParseData.toInt16(pages[2], 29);
                ROIVertRegionStart[0]   = ParseData.toInt16(pages[2], 31);
                ROIVertRegionEnd[0]     = ParseData.toInt16(pages[2], 33);
                ROIVertRegionStart[1]   = ParseData.toInt16(pages[2], 35);
                ROIVertRegionEnd[1]     = ParseData.toInt16(pages[2], 37);
                ROIVertRegionStart[2]   = ParseData.toInt16(pages[2], 39);
                ROIVertRegionEnd[2]     = ParseData.toInt16(pages[2], 41);
                // linearityCoeff[0]    = ParseData.toFloat(pages[2], 43);
                // linearityCoeff[1]    = ParseData.toFloat(pages[2], 47);
                // linearityCoeff[2]    = ParseData.toFloat(pages[2], 51);
                // linearityCoeff[3]    = ParseData.toFloat(pages[2], 55);
                // linearityCoeff[4]    = ParseData.toFloat(pages[2], 59);

                // deviceLifetimeOperationMinutes = ParseData.toInt32(pages[3], 0);
                // laserLifetimeOperationMinutes = ParseData.toInt32(pages[3], 4);
                // laserTemperatureMax  = ParseData.toInt16(pages[3], 8);
                // laserTemperatureMin  = ParseData.toInt16(pages[3], 10);
                // laserTemperatureMax  = ParseData.toInt16(pages[3], 12); // dupe
                // laserTemperatureMin  = ParseData.toInt16(pages[3], 14); // dupe

                userData = new byte[63];
                Array.Copy(pages[4], userData, userData.Length);

                for (int i = 0; i < 15; i++)
                    badPixels[i] = ParseData.toInt16(pages[5], i * 2);
            }
            catch (Exception ex)
            {
                logger.error("ModelConfig: caught exception: {0}", ex.Message);
                return false;
            }

            if (logger.debugEnabled())
                dump();

            return true;
        }

        void dump()
        {
            logger.debug("Model             = {0}", model);
            logger.debug("serialNumber      = {0}", serialNumber);
            logger.debug("baudRate          = {0}", baudRate);
            logger.debug("hasCooling        = {0}", hasCooling);
            logger.debug("hasBattery        = {0}", hasBattery);
            logger.debug("hasLaser          = {0}", hasLaser);
            logger.debug("excitationNM      = {0}", excitationNM);
            logger.debug("slitSizeUM        = {0}", slitSizeUM);

            for (int i = 0; i < wavecalCoeffs.Length; i++)
                logger.debug("wavecalCoeffs[{0}]  = {1}", i, wavecalCoeffs[i]);
            for (int i = 0; i < detectorTempCoeffs.Length; i++)
                logger.debug("detectorTempCoeffs[{0}] = {1}", i, detectorTempCoeffs[i]);
            logger.debug("detectorTempMin   = {0}", detectorTempMin);
            logger.debug("detectorTempMax   = {0}", detectorTempMax);
            for (int i = 0; i < adcCoeffs.Length; i++)
                logger.debug("adcCoeffs[{0}]      = {1}", i, detectorTempCoeffs[i]);
            logger.debug("thermistorResistanceAt298K = {0}", thermistorResistanceAt298K);
            logger.debug("thermistorBeta    = {0}", thermistorBeta);
            logger.debug("calibrationDate   = {0}", calibrationDate);
            logger.debug("calibrationBy     = {0}", calibrationBy);

            logger.debug("detectorName      = {0}", detectorName);
            logger.debug("activePixelsHoriz = {0}", activePixelsHoriz);
            logger.debug("activePixelsVert  = {0}", activePixelsVert);
            logger.debug("minIntegrationTimeMS = {0}", minIntegrationTimeMS);
            logger.debug("maxIntegrationTimeMS = {0}", maxIntegrationTimeMS);
            logger.debug("actualHoriz       = {0}", actualHoriz);
            logger.debug("ROIHorizStart     = {0}", ROIHorizStart);
            logger.debug("ROIHorizEnd       = {0}", ROIHorizEnd);
            for (int i = 0; i < ROIVertRegionStart.Length; i++)
                logger.debug("ROIVertRegionStart[{0}] = {1}", i, ROIVertRegionStart[i]);
            for (int i = 0; i < ROIVertRegionEnd.Length; i++)
                logger.debug("ROIVertRegionEnd[{0}]   = {1}", i, ROIVertRegionEnd[i]);

            logger.debug("userText          = {0}", userText);

            for (int i = 0; i < badPixels.Length; i++)
                logger.debug("badPixels[{0}]      = {1}", i, badPixels[i]);
        }
    }
}
