using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>the fundamental electronic board configurations supported by our spectrometers</summary>
    public enum BOARD_TYPES { RAMAN_FX2, INGAAS_FX2, DRAGSTER_FX3, STROKER_ARM, ERROR };

    [ComVisible(true)]  
    [Guid("4CCF3543-73EF-49D5-8234-FC2FDF647127")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IFeatureIdentification
    {
        /// <summary>board configuration</summary>
        BOARD_TYPES boardType { get; }

        string firmwarePartNum { get; }
        string firmwareDesc { get; }
        bool isSupported { get; }
        uint defaultPixels { get; }
        uint spectraBlockSize { get; }
        uint usbDelayMS { get; }
        int? defaultTECSetpointDegC { get; }
    }

    /// <summary>
    /// Encapsulates metadata inferred by the spectrometer PID
    /// </summary>
    /// <remarks>
    /// see ENG-0034, "WP Raman and Dragster Feature Identification Specification"
    ///
    /// And yes, this probably implies a need for inheritance in our class model.
    /// </remarks>
    [ComVisible(true)]
    [Guid("64D0A563-D2E6-4267-A496-A82C0F2D75DB")]
    [ProgId("WasatchNET.FeatureIdentification")]
    [ClassInterface(ClassInterfaceType.None)]
    public class FeatureIdentification : IFeatureIdentification
    {
        Logger logger = Logger.getInstance();

        /// <summary>board configuration</summary>
        public BOARD_TYPES boardType { get; }

        public string firmwarePartNum { get; private set; }
        public string firmwareDesc { get; private set; }
        public bool isSupported { get; private set; } = true;
        public uint defaultPixels { get; private set; } = 1024;
        public uint spectraBlockSize { get; private set; }  = 1024 * 2;
        public uint usbDelayMS { get; private set; } = 0;
        public int? defaultTECSetpointDegC { get; private set; } = null;

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
                defaultTECSetpointDegC = -15;
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
                // usbDelayMS = 50;
            }
            else
            {
                logger.error("Unrecognized PID {0:x4}", pid);
                isSupported = false;
            }
        }
    }
}