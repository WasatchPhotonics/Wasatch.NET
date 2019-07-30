﻿using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace WasatchNET
{
    /// <summary>
    /// Encapsulates access to the spectrometer's writable but non-volatile EEPROM.
    /// </summary>
    /// <remarks>
    /// In retrospect, should have been named "EEPROM."
    /// 
    /// While users are freely encouraged to read and parse EEPROM contents, they
    /// are STRONGLY ADVISED to exercise GREAT CAUTION in changing or writing 
    /// EEPROM values. It is entirely possible that an erroneous or corrupted 
    /// write operation could "brick" your spectrometer, requiring RMA to the 
    /// manufacturer. It is MORE likely that inappropriate values in some fields
    /// could lead to subtly malformed or biased spectral readings, which could
    /// taint or invalidate your measurement results.
    /// </remarks>
    [ComVisible(true)]
    [Guid("A224D5A7-A0E0-4AAC-8489-4BB0CED3171B")]
    [ProgId("WasatchNET.ModelConfig")]
    [ClassInterface(ClassInterfaceType.None)]
    public class EEPROM : IEEPROM
    {
        /////////////////////////////////////////////////////////////////////////       
        // private attributes
        /////////////////////////////////////////////////////////////////////////       

        const int MAX_PAGES = 6; // really 8, but last 2 are unallocated
        const byte FORMAT = 5;

        Spectrometer spectrometer;
        Logger logger = Logger.getInstance();

        public List<byte[]> pages { get; private set; }
        
        /////////////////////////////////////////////////////////////////////////       
        //
        // public attributes
        //
        /////////////////////////////////////////////////////////////////////////       

        /////////////////////////////////////////////////////////////////////////       
        // Page 0 
        /////////////////////////////////////////////////////////////////////////       

        public byte format { get; set; }

        /// <summary>spectrometer model</summary>
        public string model { get; set; }

        /// <summary>spectrometer serialNumber</summary>
        public string serialNumber { get; set; }

        /// <summary>baud rate (bits/sec) for serial communications</summary>
        public uint baudRate { get; set; }

        /// <summary>whether the spectrometer has an on-board TEC for cooling the detector</summary>
        public bool hasCooling { get; set; }

        /// <summary>whether the spectrometer has an on-board battery</summary>
        public bool hasBattery { get; set; }

        /// <summary>whether the spectrometer has an integrated laser</summary>
        public bool hasLaser { get; set; }

        /// <summary>the integral center wavelength of the laser in nanometers, if present</summary>
        /// <remarks>user-writable</remarks>
        /// <see cref="Util.wavelengthsToWavenumbers(double, double[])"/>
        public ushort excitationNM { get; set; }

        /// <summary>the slit width in µm</summary>
        public ushort slitSizeUM { get; set; }

        // these will come with ENG-0034 Rev 4
        public ushort startupIntegrationTimeMS { get; set; }
        public short  startupDetectorTemperatureDegC { get; set; }
        public byte   startupTriggeringMode { get; set; }
        public float  detectorGain { get; set; }
        public short  detectorOffset { get; set; }
        public float  detectorGainOdd { get; set; }
        public short  detectorOffsetOdd { get; set; }

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
        /// <remarks>
        /// These correspond to the fields "Temp to TEC Cal" in Wasatch Model Configuration GUI.
        ///
        /// Use these when setting the TEC setpoint.
        /// </remarks>
        public float[] degCToDACCoeffs { get; set; }
        public short detectorTempMin { get; set; }
        public short detectorTempMax { get; set; }

        /// <summary>
        /// These are used to convert 12-bit raw ADC temperature readings into degrees Celsius.
        /// </summary>
        /// <remarks>
        /// These correspond to the fields "Therm to Temp Cal" in Wasatch Model Configuration GUI.
        /// 
        /// Use these when reading the detector temperature.
        /// </remarks>
        public float[] adcToDegCCoeffs { get; set; }
        public short thermistorResistanceAt298K { get; set; }
        public short thermistorBeta { get; set; }

        /// <summary>when the unit was last calibrated (unstructured 12-char field)</summary>
        /// <remarks>user-writable</remarks>
        public string calibrationDate { get; set; }

        /// <summary>whom the unit was last calibrated by (unstructured 3-char field)</summary>
        /// <remarks>user-writable</remarks>
        public string calibrationBy { get; set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 2
        /////////////////////////////////////////////////////////////////////////       

        public string detectorName { get; set; }
        public ushort activePixelsHoriz { get; set; }
        public ushort activePixelsVert { get; set; }
        public uint minIntegrationTimeMS { get; set; }
        public uint maxIntegrationTimeMS { get; set; }
        public ushort actualPixelsHoriz { get; set; }

        // writable
        public ushort ROIHorizStart { get; set; }
        public ushort ROIHorizEnd { get; set; }
        public ushort[] ROIVertRegionStart { get; set; }
        public ushort[] ROIVertRegionEnd { get; set; }

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
        public float maxLaserPowerMW { get; set; }
        public float minLaserPowerMW { get; set; }
        public float laserExcitationWavelengthNMFloat { get; set; }
        public float[] laserPowerCoeffs { get; set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 4
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// 64 bytes of unstructured space which the user is free to use however
        /// they see fit.
        /// </summary>
        /// <remarks>
        /// For convenience, the same raw storage space is also accessible as a 
        /// null-terminated string via userText. 
        ///
        /// EEPROM versions prior to 4 only had 63 bytes of user data.
        /// </remarks>
        public byte[] userData { get; set; } 

        /// <summary>
        /// a stringified version of the 64-byte raw data block provided by userData
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
        public short[] badPixels { get; set; }

        // read-only containers for expedited processing
        public List<short> badPixelList { get; private set; }
        public SortedSet<short> badPixelSet { get; private set; }

        public string productConfiguration { get; set; }

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

            if (!ParseData.writeString(model,                pages[0],  0, 16)) return false;
            if (!ParseData.writeString(serialNumber,         pages[0], 16, 16)) return false;
            if (!ParseData.writeUInt32(baudRate,             pages[0], 32)) return false;
            if (!ParseData.writeBool  (hasCooling,           pages[0], 36)) return false;
            if (!ParseData.writeBool  (hasBattery,           pages[0], 37)) return false;
            if (!ParseData.writeBool  (hasLaser,             pages[0], 38)) return false;
            if (!ParseData.writeUInt16(excitationNM,         pages[0], 39)) return false;
            if (!ParseData.writeUInt16(slitSizeUM,           pages[0], 41)) return false;
            if (!ParseData.writeUInt16(startupIntegrationTimeMS, pages[0], 43)) return false;
            if (!ParseData.writeInt16 (startupDetectorTemperatureDegC, pages[0], 45)) return false;
            if (!ParseData.writeByte  (startupTriggeringMode,pages[0], 47)) return false;
            if (!ParseData.writeFloat (detectorGain,         pages[0], 48)) return false;
            if (!ParseData.writeInt16 (detectorOffset,       pages[0], 52)) return false;
            if (!ParseData.writeFloat (detectorGainOdd,      pages[0], 54)) return false;
            if (!ParseData.writeInt16 (detectorOffsetOdd,    pages[0], 58)) return false;

            if (!ParseData.writeFloat(wavecalCoeffs[0],      pages[1],  0)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[1],      pages[1],  4)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[2],      pages[1],  8)) return false;
            if (!ParseData.writeFloat(wavecalCoeffs[3],      pages[1], 12)) return false;
            if (!ParseData.writeFloat(degCToDACCoeffs[0],    pages[1], 16)) return false;
            if (!ParseData.writeFloat(degCToDACCoeffs[1],    pages[1], 20)) return false;
            if (!ParseData.writeFloat(degCToDACCoeffs[2],    pages[1], 24)) return false;
            if (!ParseData.writeInt16(detectorTempMax,       pages[1], 28)) return false;
            if (!ParseData.writeInt16(detectorTempMin,       pages[1], 30)) return false;
            if (!ParseData.writeFloat(adcToDegCCoeffs[0],    pages[1], 32)) return false;
            if (!ParseData.writeFloat(adcToDegCCoeffs[1],    pages[1], 36)) return false;
            if (!ParseData.writeFloat(adcToDegCCoeffs[2],    pages[1], 40)) return false;
            if (!ParseData.writeInt16(thermistorResistanceAt298K, pages[1], 44)) return false;
            if (!ParseData.writeInt16(thermistorBeta,        pages[1], 46)) return false;
            if (!ParseData.writeString(calibrationDate,      pages[1], 48, 12)) return false;
            if (!ParseData.writeString(calibrationBy,        pages[1], 60,  3)) return false;

            if (!ParseData.writeString(detectorName,         pages[2], 0, 16)) return false;
            if (!ParseData.writeUInt16(activePixelsHoriz,    pages[2], 16)) return false;
            // skip 18
            if (!ParseData.writeUInt16(activePixelsVert,     pages[2], 19)) return false;
            if (!ParseData.writeUInt16((ushort)minIntegrationTimeMS, pages[2], 21)) return false; // for now
            if (!ParseData.writeUInt16((ushort)maxIntegrationTimeMS, pages[2], 23)) return false; // for now
            if (!ParseData.writeUInt16(actualPixelsHoriz,    pages[2], 25)) return false;
            if (!ParseData.writeUInt16(ROIHorizStart,        pages[2], 27)) return false;
            if (!ParseData.writeUInt16(ROIHorizEnd,          pages[2], 29)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionStart[0],pages[2], 31)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionEnd  [0],pages[2], 33)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionStart[1],pages[2], 35)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionEnd  [1],pages[2], 37)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionStart[2],pages[2], 39)) return false;
            if (!ParseData.writeUInt16(ROIVertRegionEnd  [2],pages[2], 41)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[0],    pages[2], 43)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[1],    pages[2], 47)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[2],    pages[2], 51)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[3],    pages[2], 55)) return false;
            if (!ParseData.writeFloat(linearityCoeffs[4],    pages[2], 59)) return false;

            if (!ParseData.writeFloat(laserPowerCoeffs[0], pages[3], 12)) return false;
            if (!ParseData.writeFloat(laserPowerCoeffs[1], pages[3], 16)) return false;
            if (!ParseData.writeFloat(laserPowerCoeffs[2], pages[3], 20)) return false;
            if (!ParseData.writeFloat(laserPowerCoeffs[3], pages[3], 24)) return false;
            if (!ParseData.writeFloat(maxLaserPowerMW, pages[3], 28)) return false;
            if (!ParseData.writeFloat(minLaserPowerMW, pages[3], 32)) return false;
            if (!ParseData.writeFloat(laserExcitationWavelengthNMFloat, pages[3], 36)) return false;
            if (!ParseData.writeUInt32(minIntegrationTimeMS, pages[3], 40)) return false;
            if (!ParseData.writeUInt32(maxIntegrationTimeMS, pages[3], 44)) return false;

            Array.Copy(userData, pages[4], userData.Length);

            // note that we write the positional, error-prone array (which is 
            // user -writable), not the List or SortedSet caches
            for (int i = 0; i < badPixels.Length; i++)
                if (!ParseData.writeInt16(badPixels[i], pages[5], i * 2))
                    return false;

            if (!ParseData.writeString(productConfiguration, pages[5], 30, 16)) return false;

            // regardless of what the "read" format was (this.format), we always WRITE the latest format version.
            pages[0][63] = FORMAT;

            for (short page = 0; page < pages.Count; page++)
            {
                bool ok = false;
                if (spectrometer.isARM)
                {
                    logger.hexdump(pages[page], String.Format("writing page {0} [ARM]: ", page));
                    ok = spectrometer.sendCmd(
                        opcode: Opcodes.SECOND_TIER_COMMAND,
                        wValue: (ushort)Opcodes.SET_MODEL_CONFIG_ARM,
                        wIndex: (ushort)page,
                        buf: pages[page]);
                }
                else
                {
                    const uint DATA_START = 0x3c00; // from Wasatch Stroker Console's EnhancedStroker.SetModelInformation()
                    ushort pageOffset = (ushort) (DATA_START + page * 64);
                    logger.hexdump(pages[page], String.Format("writing page {0} to offset {1} [FX2]: ", page, pageOffset));
                    ok = spectrometer.sendCmd(
                        opcode: Opcodes.SET_MODEL_CONFIG_FX2, 
                        wValue: pageOffset,
                        wIndex: 0,
                        buf: pages[page]);
                }
                if (!ok)
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

        internal EEPROM(Spectrometer spec)
        {
            spectrometer = spec;

            wavecalCoeffs = new float[4];
            degCToDACCoeffs = new float[3];
            adcToDegCCoeffs = new float[3];
            ROIVertRegionStart = new ushort[3];
            ROIVertRegionEnd = new ushort[3];
            badPixels = new short[15];
            linearityCoeffs = new float[5];
            laserPowerCoeffs = new float[4];

            badPixelList = new List<short>();
            badPixelSet = new SortedSet<short>();
        }

        internal bool read()
        {
            // read all pages into cache
            pages = new List<byte[]>();
            for (ushort page = 0; page < MAX_PAGES; page++)
            {
                byte[] buf = spectrometer.getCmd2(Opcodes.GET_MODEL_CONFIG, 64, wIndex: page, fakeBufferLengthARM: 8);
                if (buf == null)
                    return false;
                pages.Add(buf);
                logger.hexdump(buf, String.Format("read page {0}: ", page));
            }

            format = pages[0][63];

            try 
            {
                model                   = ParseData.toString(pages[0],  0, 16);
                serialNumber            = ParseData.toString(pages[0], 16, 16);
                baudRate                = ParseData.toUInt32(pages[0], 32); 
                hasCooling              = ParseData.toBool  (pages[0], 36);
                hasBattery              = ParseData.toBool  (pages[0], 37);
                hasLaser                = ParseData.toBool  (pages[0], 38);
                excitationNM            = ParseData.toUInt16(pages[0], 39);
                slitSizeUM              = ParseData.toUInt16(pages[0], 41);

               startupIntegrationTimeMS = ParseData.toUInt16(pages[0], 43);
         startupDetectorTemperatureDegC = ParseData.toInt16 (pages[0], 45);
                startupTriggeringMode   = ParseData.toUInt8 (pages[0], 47);
                detectorGain            = ParseData.toFloat (pages[0], 48); // "even pixels" for InGaAs
                detectorOffset          = ParseData.toInt16 (pages[0], 52); // "even pixels" for InGaAs
                detectorGainOdd         = ParseData.toFloat (pages[0], 54); // InGaAs-only
                detectorOffsetOdd       = ParseData.toInt16 (pages[0], 58); // InGaAs-only

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
                activePixelsHoriz       = ParseData.toUInt16(pages[2], 16); // note: byte 18 unused
                activePixelsVert        = ParseData.toUInt16(pages[2], 19);
                minIntegrationTimeMS    = ParseData.toUInt16(pages[2], 21); // will overwrite if 
                maxIntegrationTimeMS    = ParseData.toUInt16(pages[2], 23); //   format >= 5
                actualPixelsHoriz       = ParseData.toUInt16(pages[2], 25);
                ROIHorizStart           = ParseData.toUInt16(pages[2], 27);
                ROIHorizEnd             = ParseData.toUInt16(pages[2], 29);
                ROIVertRegionStart[0]   = ParseData.toUInt16(pages[2], 31);
                ROIVertRegionEnd[0]     = ParseData.toUInt16(pages[2], 33);
                ROIVertRegionStart[1]   = ParseData.toUInt16(pages[2], 35);
                ROIVertRegionEnd[1]     = ParseData.toUInt16(pages[2], 37);
                ROIVertRegionStart[2]   = ParseData.toUInt16(pages[2], 39);
                ROIVertRegionEnd[2]     = ParseData.toUInt16(pages[2], 41);
                linearityCoeffs[0]      = ParseData.toFloat (pages[2], 43);
                linearityCoeffs[1]      = ParseData.toFloat (pages[2], 47);
                linearityCoeffs[2]      = ParseData.toFloat (pages[2], 51);
                linearityCoeffs[3]      = ParseData.toFloat (pages[2], 55);
                linearityCoeffs[4]      = ParseData.toFloat (pages[2], 59);

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
            }
            catch (Exception ex)
            {
                logger.error("ModelConfig: caught exception: {0}", ex.Message);
                return false;
            }

            if (logger.debugEnabled())
                dump();

            enforceReasonableDefaults();

            return true;
        }

        public bool hasLaserPowerCalibration()
        {
            if (maxLaserPowerMW <= 0)
                return false;

            if (laserPowerCoeffs == null || laserPowerCoeffs.Length < 4)
                return false;

            foreach (double d in laserPowerCoeffs)
                if (Double.IsNaN(d))
                    return false;

            return true;
        }

        void enforceReasonableDefaults()
        {
            bool defaultWavecal = false;
            for (int i = 0; i < 4; i++)
                if (Double.IsNaN(wavecalCoeffs[i]))
                    defaultWavecal = true;
            if (defaultWavecal)
            {
                logger.error("No wavecal found (pixel space)");
                wavecalCoeffs[0] = 0;
                wavecalCoeffs[1] = 1;
                wavecalCoeffs[2] = 0;
                wavecalCoeffs[3] = 0;
            }

            if (minIntegrationTimeMS < 1)
            {
                logger.error("invalid minIntegrationTimeMS found ({0}), defaulting to 1", minIntegrationTimeMS);
                minIntegrationTimeMS = 1;
            }
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

            logger.debug("startupIntegrationTimeMS = {0}", startupIntegrationTimeMS);
            logger.debug("startupDetectorTempDegC = {0}", startupDetectorTemperatureDegC);
            logger.debug("startupTriggeringMode = {0}", startupTriggeringMode);
            logger.debug("detectorGain          = {0:f2}", detectorGain);
            logger.debug("detectorOffset        = {0}", detectorOffset);
            logger.debug("detectorGainOdd       = {0:f2}", detectorGainOdd);
            logger.debug("detectorOffsetOdd     = {0}", detectorOffsetOdd);

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
            logger.debug("actualPixelsHoriz     = {0}", actualPixelsHoriz);
            logger.debug("ROIHorizStart         = {0}", ROIHorizStart);
            logger.debug("ROIHorizEnd           = {0}", ROIHorizEnd);
            for (int i = 0; i < ROIVertRegionStart.Length; i++)
                logger.debug("ROIVertRegionStart[{0}] = {1}", i, ROIVertRegionStart[i]);
            for (int i = 0; i < ROIVertRegionEnd.Length; i++)
                logger.debug("ROIVertRegionEnd[{0}]   = {1}", i, ROIVertRegionEnd[i]);
            for (int i = 0; i < linearityCoeffs.Length; i++)
                logger.debug("linearityCoeffs[{0}]    = {1}", i, linearityCoeffs[i]);

            for (int i = 0; i < laserPowerCoeffs.Length; i++)
                logger.debug("laserPowerCoeffs[{0}]   = {1}", i, laserPowerCoeffs[i]);
            logger.debug("maxLaserPowerMW       = {0}", maxLaserPowerMW);
            logger.debug("minLaserPowerMW       = {0}", minLaserPowerMW);
            logger.debug("laserExcitationNMFloat= {0}", laserExcitationWavelengthNMFloat);

            logger.debug("userText              = {0}", userText);

            for (int i = 0; i < badPixels.Length; i++)
                logger.debug("badPixels[{0,2}]         = {1}", i, badPixels[i]);

            logger.debug("productConfiguration  = {0}", productConfiguration);
        }
    }
}