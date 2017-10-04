namespace WasatchNET
{
    public class FeatureIdentification
    {
        public enum BOARD_TYPES {  RAMAN_FX2, INGAAS_FX2, DRAGSTER_FX3, STROKER_ARM, ERROR };

        Logger logger = Logger.getInstance();

        public BOARD_TYPES boardType;
        public string firmwarePartNum;
        public string firmwareDesc;

        public FeatureIdentification(int pid)
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
            }
            else if (pid == 0x3000)
            {
                boardType = BOARD_TYPES.DRAGSTER_FX3;
                firmwarePartNum = "170001";
                firmwareDesc = "Dragster USB Board FX3 Code";
            }
            else if (pid == 0x4000)
            {
                boardType = BOARD_TYPES.STROKER_ARM;
                firmwarePartNum = "170019";
                firmwareDesc = "Stroker ARM USB Board";
            }
            else
            {
                logger.error("Unrecognized PID {0:x4}", pid);
            }
        }
    }
}