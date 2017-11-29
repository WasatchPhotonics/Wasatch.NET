﻿namespace WasatchNET
{
    /// <summary>
    /// Encapsulates metadata inferred by the spectrometer PID
    /// </summary>
    /// <remarks>
    /// see ENG-0034, "WP Raman and Dragster Feature Identification Specification"
    ///
    /// And yes, this probably implies a need for inheritance in our class model.
    /// </remarks>
    public class FeatureIdentification
    {
        /// <summary>the fundamental electronic board configurations supported by our spectrometers</summary>
        public enum BOARD_TYPES { RAMAN_FX2, INGAAS_FX2, DRAGSTER_FX3, STROKER_ARM, ERROR };

        Logger logger = Logger.getInstance();

        /// <summary>board configuration</summary>
        public BOARD_TYPES boardType;

        public string firmwarePartNum;
        public string firmwareDesc;
        public bool isSupported = true;
        public uint defaultPixels = 1024;
        public uint spectraBlockSize = 1024 * 2;

        internal FeatureIdentification(int pid)
        {
            if (pid == 0x1000)
            {
                boardType = BOARD_TYPES.RAMAN_FX2;
                firmwarePartNum = "170003";
                firmwareDesc = "Stroker USB Board FX2 Code";
            }
            else if (pid == 0x2000)
            {
                boardType = BOARD_TYPES.INGAAS_FX2;
                firmwarePartNum = "170037";
                firmwareDesc = "Hamamatsu InGaAs USB Board FX2 Code";
                defaultPixels = 512;
                spectraBlockSize = defaultPixels * 2;
            }
            else if (pid == 0x3000)
            {
                // older OCT product
                boardType = BOARD_TYPES.DRAGSTER_FX3;
                firmwarePartNum = "170001";
                firmwareDesc = "Dragster USB Board FX3 Code";
            }
            else if (pid == 0x4000)
            {
                // watch this space!
                boardType = BOARD_TYPES.STROKER_ARM;
                firmwarePartNum = "170019";
                firmwareDesc = "Stroker ARM USB Board";
                spectraBlockSize = 512;
            }
            else
            {
                logger.error("Unrecognized PID {0:x4}", pid);
                isSupported = false;
            }
        }
    }
}