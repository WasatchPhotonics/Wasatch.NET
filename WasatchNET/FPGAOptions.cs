using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    /// <summary>
    /// The set of options and settings enabled when the FPGA firmware was compiled for this spectrometer.
    /// </summary>
    public class FPGAOptions
    {
        // public attributes
        public FPGA_INTEG_TIME_RES integrationTimeResolution { get; private set; } = FPGA_INTEG_TIME_RES.ERROR;
        public FPGA_DATA_HEADER dataHeader { get; private set; } = FPGA_DATA_HEADER.ERROR;
        public bool hasCFSelect { get; private set; }
        public FPGA_LASER_TYPE laserType { get; private set; } = FPGA_LASER_TYPE.NONE;
        public FPGA_LASER_CONTROL laserControl { get; private set; } = FPGA_LASER_CONTROL.ERROR;
        public bool hasAreaScan { get; private set; }
        public bool hasActualIntegTime { get; private set; }
        public bool hasHorizBinning { get; private set; }

        // private attributes
        Spectrometer spectrometer;
        Logger logger = Logger.getInstance();

        public FPGAOptions(Spectrometer s)
        {
            spectrometer = s;
            // if (s.featureIdentification.boardType == FeatureIdentification.BOARD_TYPES.STROKER_ARM)
            // {
            //     logger.error("FPGAOptions not supported on {0}", s.featureIdentification.boardType);
            //     return;
            // }
            load();
        }

        /// <summary>
        /// Read FPGA compiler options; for values, see ENG-0034.
        /// </summary>
        void load()
        {
            byte[] buf = spectrometer.getCmd2(Opcodes.READ_COMPILATION_OPTIONS, 2);
            if (buf == null)
                return;

            ushort word = (ushort) (buf[0] | (buf[1] << 8));
            logger.debug("FPGA compiler options: 0x{0:x4}", word);

            // bits 0-2: 0000 0000 0000 0111 IntegrationTimeResolution
            // bit  3-5: 0000 0000 0011 1000 DataHeader
            // bit    6: 0000 0000 0100 0000 HasCFSelect
            // bit  7-8: 0000 0001 1000 0000 LaserType
            // bit 9-11: 0000 1110 0000 0000 LaserControl
            // bit   12: 0001 0000 0000 0000 HasAreaScan
            // bit   13: 0010 0000 0000 0000 HasActualIntegTime
            // bit   14: 0100 0000 0000 0000 HasHorizBinning

            try
            {
                integrationTimeResolution = parseResolution(word & 0x07);
                dataHeader = parseDataHeader((word & 0x0038) >> 3);
                hasCFSelect = (word & 0x0040) != 0;
                laserType = parseLaserType((word & 0x0180) >> 7);
                laserControl = parseLaserControl((word & 0x0e00) >> 9);
                hasAreaScan = (word & 0x1000) != 0;
                hasActualIntegTime = (word & 0x2000) != 0;
                hasHorizBinning = (word & 0x4000) != 0;
            }
            catch (Exception ex)
            {
                logger.error("failed to parse FPGA compilation options: {0}", ex.Message);
            }

            if (logger.debugEnabled())
            {
                logger.debug("  IntegrationTimeResolution = {0}", integrationTimeResolution);
                logger.debug("  DataHeader                = {0}", dataHeader);
                logger.debug("  HasCFSelect               = {0}", hasCFSelect);
                logger.debug("  LaserType                 = {0}", laserType);
                logger.debug("  LaserControl              = {0}", laserControl);
                logger.debug("  HasAreaScan               = {0}", hasAreaScan);
                logger.debug("  HasActualIntegTime        = {0}", hasActualIntegTime);
                logger.debug("  HasHorizBinning           = {0}", hasHorizBinning);
            }
        }

        public FPGA_INTEG_TIME_RES parseResolution(int value)
        {
            switch (value)
            {
                case 0: return FPGA_INTEG_TIME_RES.ONE_MS;    
                case 1: return FPGA_INTEG_TIME_RES.TEN_MS;    
                case 2: return FPGA_INTEG_TIME_RES.SWITCHABLE;
            }
            return FPGA_INTEG_TIME_RES.ERROR;
        }

        public FPGA_DATA_HEADER parseDataHeader(int value)
        {
            switch (value)
            {
                case 0: return FPGA_DATA_HEADER.NONE;
                case 1: return FPGA_DATA_HEADER.OCEAN_OPTICS;
                case 2: return FPGA_DATA_HEADER.WASATCH;
            }
            return FPGA_DATA_HEADER.ERROR;
        }

        public FPGA_LASER_TYPE parseLaserType(int value)
        {
            switch (value)
            {
                case 0: return FPGA_LASER_TYPE.NONE;
                case 1: return FPGA_LASER_TYPE.INTERNAL;
                case 2: return FPGA_LASER_TYPE.EXTERNAL;
            }
            return FPGA_LASER_TYPE.ERROR;
        }

        public FPGA_LASER_CONTROL parseLaserControl(int value)
        {
            switch (value)
            {
                case 0: return FPGA_LASER_CONTROL.MODULATION;
                case 1: return FPGA_LASER_CONTROL.TRANSITION_POINTS;
                case 2: return FPGA_LASER_CONTROL.RAMPING;
            }
            return FPGA_LASER_CONTROL.ERROR;
        }
    }
}
