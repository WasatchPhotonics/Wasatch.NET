using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    public enum FPGA_INTEG_TIME_RES { ONE_MS, TEN_MS, SWITCHABLE, ERROR };
    public enum FPGA_DATA_HEADER { NONE, OCEAN_OPTICS, WASATCH, ERROR };
    public enum FPGA_LASER_TYPE { NONE, INTERNAL, EXTERNAL, ERROR };
    public enum FPGA_LASER_CONTROL { MODULATION, TRANSITION_POINTS, RAMPING, ERROR };

    [ComVisible(true)]  
    [Guid("CCF6BA13-ECC7-456C-AAFC-ACB8D5E58BDE")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IFPGAOptions
    {
        FPGA_INTEG_TIME_RES integrationTimeResolution { get; } 
        FPGA_DATA_HEADER dataHeader { get; } 
        bool hasCFSelect { get; }
        FPGA_LASER_TYPE laserType { get; } 
        FPGA_LASER_CONTROL laserControl { get; }
        bool hasAreaScan { get; }
        bool hasActualIntegTime { get; }
        bool hasHorizBinning { get; }

        FPGA_INTEG_TIME_RES parseResolution(int value);
        FPGA_DATA_HEADER parseDataHeader(int value);
        FPGA_LASER_TYPE parseLaserType(int value);
        FPGA_LASER_CONTROL parseLaserControl(int value);
    }
}