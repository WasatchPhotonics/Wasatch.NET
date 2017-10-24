using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    /// <summary>
    /// Encapsulates access to the spectrometer's writable but non-volatile EEPROM.
    /// </summary>
    /// <remarks>
    /// While users are freely encouraged to read and parse EEPROM contents, they
    /// are STRONGLY ADVISED to exercise GREAT CAUTION in changing or writing 
    /// EEPROM values. It is entirely possible that an erroneous or corrupted 
    /// write operation could "brick" your spectrometer, requiring RMA to the 
    /// manufacturer. It is MORE likely that inappropriate values in some fields
    /// could lead to subtly malformed or biased spectral readings, which could
    /// taint or invalidate your measurement results.
    /// </remarks>
    public class ModelConfig
    {
        /////////////////////////////////////////////////////////////////////////       
        // private attributes
        /////////////////////////////////////////////////////////////////////////       

        const int MAX_PAGES = 6; // really 8, but last 2 are unallocated

        Spectrometer spectrometer;
        Logger logger = Logger.getInstance();

        public List<byte[]> pages;
        List<byte> format;
        
        /////////////////////////////////////////////////////////////////////////       
        //
        // public attributes
        //
        /////////////////////////////////////////////////////////////////////////       

        /////////////////////////////////////////////////////////////////////////       
        // Page 0 
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>spectrometer model</summary>
        public string model { get; private set; }

        /// <summary>spectrometer serialNumber</summary>
        public string serialNumber { get; private set; }

        /// <summary>baud rate (bits/sec) for serial communications</summary>
        public int baudRate { get; private set; }

        /// <summary>whether the spectrometer has an on-board TEC for cooling the detector</summary>
        public bool hasCooling { get; private set; }

        /// <summary>whether the spectrometer has an on-board battery</summary>
        public bool hasBattery { get; private set; }

        /// <summary>whether the spectrometer has an integrated laser</summary>
        public bool hasLaser { get; private set; }

        /// <summary>the integral center wavelength of the laser in nanometers, if present</summary>
        /// <remarks>user-writable</remarks>
        /// <see cref="Util.wavelengthsToWavenumbers(double, double[])"/>
        public short excitationNM { get; set; }

        /// <summary>the slit width in µm</summary>
        public short slitSizeUM { get; private set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 1
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>coefficients of a 3rd-order polynomial representing the configured wavelength calibration</summary>
        /// <remarks>
        /// These are automatically expanded into an accessible array in 
        /// Spectrometer.wavelengths.  Also see Util.generateWavelengths() for 
        /// the process of expanding the polynomial.
        ///
        /// user-writable
        /// </remarks>
        /// <see cref="Spectrometer.wavelengths"/>
        /// <see cref="Util.generateWavelengths(uint, float[])"/>
        public float[] wavecalCoeffs { get; set; }

        /// <summary>
        /// These are used to convert the user's desired setpoint in degrees Celsius to raw 12-bit DAC inputs.
        /// </summary>
        /// <remarks>These correspond to the fields "Temp to TEC Cal" in Wasatch Model Configuration GUI</remarks>
        public float[] degCToDACCoeffs { get; private set; }
        public float detectorTempMin { get; private set; }
        public float detectorTempMax { get; private set; }

        /// <summary>
        /// These are used to convert 12-bit raw ADC temperature readings into degrees Celsius.
        /// </summary>
        /// <remarks>These correspond to the fields "Therm to Temp Cal" in Wasatch Model Configuration GUI</remarks>
        public float[] adcToDegCCoeffs { get; private set; }
        public short thermistorResistanceAt298K { get; private set; }
        public short thermistorBeta { get; private set; }

        /// <summary>when the unit was last calibrated (unstructured 12-char field)</summary>
        /// <remarks>user-writable</remarks>
        public string calibrationDate { get; set; }

        /// <summary>whom the unit was last calibrated by (unstructured 3-char field)</summary>
        /// <remarks>user-writable</remarks>
        public string calibrationBy { get; set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 2
        /////////////////////////////////////////////////////////////////////////       

        public string detectorName { get; private set; }
        public short activePixelsHoriz { get; private set; }
        public short activePixelsVert { get; private set; }
        public ushort minIntegrationTimeMS { get; private set; }
        public ushort maxIntegrationTimeMS { get; private set; }
        public short actualHoriz { get; private set; }

        // writable
        public short ROIHorizStart { get; set; }
        public short ROIHorizEnd { get; set; }
        public short[] ROIVertRegionStart { get; private set; }
        public short[] ROIVertRegionEnd { get; private set; }

        /// <summary>
        /// These are reserved for a non-linearity calibration,
        /// but may be harnessed by users for other purposes.
        /// </summary>
        /// <remarks>user-writable</remarks>
        public float[] linearityCoeffs { get; set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 3
        /////////////////////////////////////////////////////////////////////////       

        // public int deviceLifetimeOperationMinutes { get; private set; }
        // public int laserLifetimeOperationMinutes { get; private set; }
        // public short laserTemperatureMax { get; private set; }
        // public short laserTemperatureMin { get; private set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 4
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// 63 bytes of unstructured space which the user is free to use however
        /// they see fit.
        /// </summary>
        /// <remarks>
        /// For convenience, the same raw storage space is also accessible as a 
        /// null-terminated string via userText. 
        ///
        /// Unfortunately, the 64th byte (you knew there had to be one) is used
        /// internally to represent EEPROM page format version.
        /// </remarks>
        public byte[] userData { get; private set; } 

        /// <summary>
        /// a stringified version of the 63-byte raw data block provided by userData
        /// </summary>
        /// <remarks>accessible as a null-terminated string via userText</remarks>
        public string userText
        {
            get
            {
                return ParseData.toString(userData);
            }

            set
            {
                for (int i = 0; i < userData.Length; i++)
                    if (i < value.Length)
                        userData[i] = (byte) value[i];
                    else
                        userData[i] = 0;
            }
        }

        /////////////////////////////////////////////////////////////////////////       
        // Page 5
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// array of up to 15 "bad" (hot or dead) pixels which software may wish
        /// to skip or "average over" during spectral post-processing.
        /// </summary>
        /// <remarks>bad pixels are identified by pixel number; empty slots are indicated by -1</remarks>
        public short[] badPixels { get; private set; }

        /////////////////////////////////////////////////////////////////////////       
        // Pages 6-7 unallocated
        /////////////////////////////////////////////////////////////////////////       

        /////////////////////////////////////////////////////////////////////////       
        //
        // public methods
        //
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// Save updated EEPROM fields to the device.
        /// </summary>
        /// <remarks>
        /// Only a handful of fields are recommended to be changed by users: 
        ///
        /// - excitationNM
        /// - wavecalCoeffs
        /// - calibrationDate
        /// - calibrationBy
        /// - ROI
        /// - linearityCoeffs (not currently used)
        /// - userData
        /// - badPixels
        /// 
        /// Note that the EEPROM isn't an SSD...it's not terribly fast, and there
        /// are a finite number of lifetime writes, so use sparingly.
        ///
        /// Due to the high risk of bricking a unit through a failed / bad EEPROM
        /// write, all internal calls bail at the first error in hopes of salvaging
        /// the unit if at all possible.
        ///
        /// That said, if you do frag your EEPROM, Wasatch has a "Model Configuration"
        /// utility to let you manually write EEPROM fields; contact your sales rep
        /// for a copy.
        /// </remarks>
        /// <returns>true on success, false on failure</returns>
        public bool write()
        {
            if (pages == null || pages.Count != MAX_PAGES)
            {
                logger.error("ModelConfig.write: need to perform a read first");
                return false;
            }

            if (!ParseData.writeInt16(excitationNM,          pages[0], 39)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[0],      pages[1],  0)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[1],      pages[1],  4)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[2],      pages[1],  8)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[3],      pages[1], 12)) return false;
            if (!ParseData.writeString(calibrationDate,      pages[1], 48, 12)) return false;
            if (!ParseData.writeString(calibrationBy,        pages[1], 60,  3)) return false;
            if (!ParseData.writeInt16(ROIHorizStart,         pages[2], 27)) return false;
            if (!ParseData.writeInt16(ROIHorizEnd,           pages[2], 29)) return false;
            if (!ParseData.writeInt16(ROIVertRegionStart[0], pages[2], 31)) return false;
            if (!ParseData.writeInt16(ROIVertRegionEnd  [0], pages[2], 33)) return false;
            if (!ParseData.writeInt16(ROIVertRegionStart[1], pages[2], 35)) return false;
            if (!ParseData.writeInt16(ROIVertRegionEnd  [1], pages[2], 37)) return false;
            if (!ParseData.writeInt16(ROIVertRegionStart[2], pages[2], 39)) return false;
            if (!ParseData.writeInt16(ROIVertRegionEnd  [2], pages[2], 41)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[0],    pages[2], 43)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[1],    pages[2], 47)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[2],    pages[2], 51)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[3],    pages[2], 55)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[4],    pages[2], 59)) return false;

            Array.Copy(userData, pages[4], userData.Length);

            for (int i = 0; i < badPixels.Length; i++)
                if (!ParseData.writeInt16(badPixels[i], pages[5], i * 2))
                    return false;

            // we'll need this to send commands
            Dictionary<Opcodes, byte> cmd = OpcodeHelper.getInstance().getDict();

            // Deliberately write pages in reverse order (least important to most),
            // so if there are any errors, hopefully we won't completely brick 
            // the unit.
            for (short page = (short)(pages.Count - 1); page >= 0; page--)
            {
                const uint DATA_START = 0x3c00; // from Wasatch Stroker Console's EnhancedStroker.setModelInformation()
                ushort pageOffset = (ushort) (DATA_START + page * 64);

                logger.hexdump(pages[page], String.Format("writing page {0} at offset {1:x4}: ", page, pageOffset));

                if (!spectrometer.sendCmd(Opcodes.SET_MODEL_CONFIG_REAL, pageOffset, 0, pages[page]))
                {
                    logger.error("ModelConfig.write: failed to save page {0}", page);
                    return false;
                }
                logger.debug("ModelConfig: wrote EEPROM page {0}", page);
            }
            return true;
        }

        /////////////////////////////////////////////////////////////////////////       
        // private methods
        /////////////////////////////////////////////////////////////////////////       

        internal ModelConfig(Spectrometer spec)
        {
            spectrometer = spec;

            wavecalCoeffs = new float[4];
            degCToDACCoeffs = new float[3];
            adcToDegCCoeffs = new float[3];
            ROIVertRegionStart = new short[3];
            ROIVertRegionEnd = new short[3];
            badPixels = new short[15];
            linearityCoeffs = new float[5];
        }

        internal bool read()
        {
            // read all pages into cache
            pages = new List<byte[]>();
            format = new List<byte>();
            for (ushort page = 0; page < MAX_PAGES; page++)
            {
                byte[] buf = spectrometer.getCmd2(Opcodes.GET_MODEL_CONFIG, 64, wIndex: page);
                if (buf == null)
                    return false;
                pages.Add(buf);
                format.Add(buf[63]); // page format is always last byte

                logger.hexdump(buf, String.Format("read page {0}: ", page));
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
                degCToDACCoeffs[0]      = ParseData.toFloat (pages[1], 16);
                degCToDACCoeffs[1]      = ParseData.toFloat (pages[1], 20);
                degCToDACCoeffs[2]      = ParseData.toFloat (pages[1], 24);
                detectorTempMax         = ParseData.toInt16 (pages[1], 28);
                detectorTempMin         = ParseData.toInt16 (pages[1], 30);
                adcToDegCCoeffs[0]      = ParseData.toFloat (pages[1], 32); 
                adcToDegCCoeffs[1]      = ParseData.toFloat (pages[1], 36);
                adcToDegCCoeffs[2]      = ParseData.toFloat (pages[1], 40);
             thermistorResistanceAt298K = ParseData.toInt16 (pages[1], 44);
                thermistorBeta          = ParseData.toInt16 (pages[1], 46);
                calibrationDate         = ParseData.toString(pages[1], 48, 12);
                calibrationBy           = ParseData.toString(pages[1], 60, 3);

                detectorName            = ParseData.toString(pages[2],  0, 16);
                activePixelsHoriz       = ParseData.toInt16 (pages[2], 16); // note: byte 18 apparently unused
                activePixelsVert        = ParseData.toInt16 (pages[2], 19);
                minIntegrationTimeMS    = ParseData.toUInt16(pages[2], 21);
                maxIntegrationTimeMS    = ParseData.toUInt16(pages[2], 23);
                actualHoriz             = ParseData.toInt16 (pages[2], 25);
                ROIHorizStart           = ParseData.toInt16 (pages[2], 27);
                ROIHorizEnd             = ParseData.toInt16 (pages[2], 29);
                ROIVertRegionStart[0]   = ParseData.toInt16 (pages[2], 31);
                ROIVertRegionEnd[0]     = ParseData.toInt16 (pages[2], 33);
                ROIVertRegionStart[1]   = ParseData.toInt16 (pages[2], 35);
                ROIVertRegionEnd[1]     = ParseData.toInt16 (pages[2], 37);
                ROIVertRegionStart[2]   = ParseData.toInt16 (pages[2], 39);
                ROIVertRegionEnd[2]     = ParseData.toInt16 (pages[2], 41);
                linearityCoeffs[0]      = ParseData.toFloat (pages[2], 43);
                linearityCoeffs[1]      = ParseData.toFloat (pages[2], 47);
                linearityCoeffs[2]      = ParseData.toFloat (pages[2], 51);
                linearityCoeffs[3]      = ParseData.toFloat (pages[2], 55);
                linearityCoeffs[4]      = ParseData.toFloat (pages[2], 59);

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
            logger.debug("Model                 = {0}", model);
            logger.debug("serialNumber          = {0}", serialNumber);
            logger.debug("baudRate              = {0}", baudRate);
            logger.debug("hasCooling            = {0}", hasCooling);
            logger.debug("hasBattery            = {0}", hasBattery);
            logger.debug("hasLaser              = {0}", hasLaser);
            logger.debug("excitationNM          = {0}", excitationNM);
            logger.debug("slitSizeUM            = {0}", slitSizeUM);

            for (int i = 0; i < wavecalCoeffs.Length; i++)
                logger.debug("wavecalCoeffs[{0}]      = {1}", i, wavecalCoeffs[i]);
            for (int i = 0; i < degCToDACCoeffs.Length; i++)
                logger.debug("degCToDACCoeffs[{0}]    = {1}", i, degCToDACCoeffs[i]);
            logger.debug("detectorTempMin       = {0}", detectorTempMin);
            logger.debug("detectorTempMax       = {0}", detectorTempMax);
            for (int i = 0; i < adcToDegCCoeffs.Length; i++)
                logger.debug("adcToDegCCoeffs[{0}]    = {1}", i, adcToDegCCoeffs[i]);
            logger.debug("thermistorResistanceAt298K = {0}", thermistorResistanceAt298K);
            logger.debug("thermistorBeta        = {0}", thermistorBeta);
            logger.debug("calibrationDate       = {0}", calibrationDate);
            logger.debug("calibrationBy         = {0}", calibrationBy);
                                               
            logger.debug("detectorName          = {0}", detectorName);
            logger.debug("activePixelsHoriz     = {0}", activePixelsHoriz);
            logger.debug("activePixelsVert      = {0}", activePixelsVert);
            logger.debug("minIntegrationTimeMS  = {0}", minIntegrationTimeMS);
            logger.debug("maxIntegrationTimeMS  = {0}", maxIntegrationTimeMS);
            logger.debug("actualHoriz           = {0}", actualHoriz);
            logger.debug("ROIHorizStart         = {0}", ROIHorizStart);
            logger.debug("ROIHorizEnd           = {0}", ROIHorizEnd);
            for (int i = 0; i < ROIVertRegionStart.Length; i++)
                logger.debug("ROIVertRegionStart[{0}] = {1}", i, ROIVertRegionStart[i]);
            for (int i = 0; i < ROIVertRegionEnd.Length; i++)
                logger.debug("ROIVertRegionEnd[{0}]   = {1}", i, ROIVertRegionEnd[i]);
            for (int i = 0; i < linearityCoeffs.Length; i++)
                logger.debug("linearityCoeffs[{0}]    = {1}", i, linearityCoeffs[i]);

            logger.debug("userText              = {0}", userText);

            for (int i = 0; i < badPixels.Length; i++)
                logger.debug("badPixels[{0,2}]         = {1}", i, badPixels[i]);
        }
    }
}
