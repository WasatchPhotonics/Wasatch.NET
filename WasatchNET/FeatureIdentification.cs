using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>the fundamental electronic board configurations supported by our spectrometers</summary>
    public enum BOARD_TYPES { RAMAN_FX2, INGAAS_FX2, DRAGSTER_FX3, ARM, ERROR };

    /// <summary>
    /// TODO: move to IFeatureIdentification file
    /// </summary>
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
        bool hasDefaultTECSetpointDegC { get; }
        int defaultTECSetpointDegC { get; }
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

        public int vid { get; private set; }
        public int pid { get; private set; }
        public string firmwarePartNum { get; private set; }
        public string firmwareDesc { get; private set; }
        public bool isSupported { get; private set; } = true;
        public uint defaultPixels { get; private set; } = 1024;
        public uint spectraBlockSize { get; private set; }  = 1024 * 2;
        public uint usbDelayMS { get; private set; } = 0;
        public bool hasDefaultTECSetpointDegC { get; private set; } = false;
        public int defaultTECSetpointDegC { get; private set; } = 0;

        internal FeatureIdentification(int vid, int pid)
        {
            this.vid = vid;
            this.pid = pid;

            if (pid == 0x1000)
            {
                boardType = BOARD_TYPES.RAMAN_FX2;
                firmwarePartNum = "170003";
                firmwareDesc = "FX2 USB Board";
            }
            else if (pid == 0x2000)
            {
                boardType = BOARD_TYPES.INGAAS_FX2;
                firmwarePartNum = "170037";
                firmwareDesc = "FX2 InGaAs USB Board";
                defaultPixels = 512;
                spectraBlockSize = defaultPixels * 2;
                hasDefaultTECSetpointDegC = true;
                defaultTECSetpointDegC = -15;
            }
            else if (pid == 0x4000)
            {
                boardType = BOARD_TYPES.ARM;
                firmwarePartNum = "170019";
                firmwareDesc = "ARM USB Board";
            }
            else if (pid == 0x1018)
            {

            }
            else
            {
                logger.error("Unrecognized PID {0:x4}", pid);
                isSupported = false;
            }
        }
    }
}