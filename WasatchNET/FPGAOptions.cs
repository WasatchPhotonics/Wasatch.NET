using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    public class FPGAOptions
    {
        // public attributes
        public FPGA_INTEG_TIME_RES integrationTimeResolution { get; private set; }
        public FPGA_DATA_HEADER dataHeader { get; private set; }
        public bool hasCFSelect { get; private set; }
        public FPGA_LASER_TYPE laserType { get; private set; }
        public FPGA_LASER_CONTROL laserControl { get; private set; }
        public bool hasAreaScan { get; private set; }
        public bool hasActualIntegTime { get; private set; }
        public bool hasHorizBinning { get; private set; }

        // private attributes
        Spectrometer spectrometer;
        Logger logger = Logger.getInstance();

        public FPGAOptions(Spectrometer s)
        {
            spectrometer = s;
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

            // Question: how would the ENUMs handle misconfigured spectrometers?
            try
            {
                integrationTimeResolution = (FPGA_INTEG_TIME_RES) (word & 0x07);
                dataHeader = (FPGA_DATA_HEADER) ((word & 0x0038) >> 3);
                hasCFSelect = (word & 0x0040) != 0;
                laserType = (FPGA_LASER_TYPE)((word & 0x0180) >> 7);
                laserControl = (FPGA_LASER_CONTROL)((word & 0x0e00) >> 9);
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
    }
}
