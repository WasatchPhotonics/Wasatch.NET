using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    public enum CCD_TRIGGER_SOURCE { USB, EXTERNAL, ERROR };
    public enum EXTERNAL_TRIGGER_OUTPUT { LASER_MODULATION, INTEGRATION_ACTIVE_PULSE, ERROR };
    public enum HORIZ_BINNING { NONE, TWO_PIXEL, FOUR_PIXEL, ERROR };

    /// <summary>
    /// Convenience enum for mapping USB API commands to stringifiable English labels. 
    /// </summary>
    /// <remarks>
    /// </remarks>
    public enum Opcodes
    {
        ACQUIRE_CCD,
        GET_ACTUAL_FRAMES,
        GET_ACTUAL_INTEGRATION_TIME,
        GET_CCD_GAIN,
        GET_CCD_OFFSET,
        GET_CCD_SENSING_THRESHOLD,
        GET_CCD_TEMP,
        GET_CCD_TEMP_ENABLE,
        GET_CCD_TEMP_SETPOINT,
        GET_CCD_THRESHOLD_SENSING_MODE,
        GET_CCD_TRIGGER_SOURCE,
        GET_CF_SELECT,
        GET_CODE_REVISION,
        GET_DAC,
        GET_EXTERNAL_TRIGGER_OUTPUT,
        GET_FPGA_REV,
        GET_HORIZ_BINNING,
        GET_INTEGRATION_TIME,
        GET_INTERLOCK,
        GET_LASER_ENABLED,
        GET_LASER_MOD_DURATION,
        GET_LASER_MOD_ENABLED,
        GET_LASER_MOD_PULSE_WIDTH,
        GET_LASER_RAMPING_MODE,
        GET_LASER_TEMP,
        GET_LASER_TEMP_SETPOINT,
        GET_LINE_LENGTH,
        GET_LINK_LASER_MOD_TO_INTEGRATION_TIME,
        GET_MODEL_CONFIG,
        GET_MOD_PERIOD,
        GET_MOD_PULSE_DELAY,
        GET_SELECTED_LASER,
        GET_TRIGGER_DELAY,
        LINK_LASER_MOD_TO_INTEGRATION_TIME,
        OPT_ACT_INT_TIME,
        OPT_AREA_SCAN,
        OPT_CF_SELECT,
        OPT_DATA_HDR_TAB,
        OPT_HORIZONTAL_BINNING,
        OPT_INT_TIME_RES,
        OPT_LASER,
        OPT_LASER_CONTROL,
        POLL_DATA,
        READ_COMPILATION_OPTIONS,
        SECOND_TIER_COMMAND,
        SELECT_HORIZ_BINNING,
        SELECT_LASER,
        SET_CCD_GAIN,
        SET_CCD_OFFSET,
        SET_CCD_SENSING_THRESHOLD,
        SET_CCD_TEC_ENABLE,
        SET_CCD_TEMP_SETPOINT,
        SET_CCD_THRESHOLD_SENSING_MODE,
        SET_CCD_TRIGGER_SOURCE,
        SET_CF_SELECT,
        SET_DAC,
        SET_DFU_MODE,
        SET_EXTERNAL_TRIGGER_OUTPUT,
        SET_INTEGRATION_TIME,
        SET_LASER_ENABLED,
        SET_LASER_MOD_DURATION,
        SET_LASER_MOD_ENABLED,
        SET_LASER_MOD_PULSE_WIDTH,
        SET_LASER_RAMPING_MODE,
        SET_LASER_TEMP_SETPOINT,
        SET_MODEL_CONFIG_DO_NOT_USE,
        SET_MODEL_CONFIG_REAL,
        SET_MOD_PERIOD,
        SET_MOD_PULSE_DELAY,
        SET_TRIGGER_DELAY,
     // VR_ENABLE_CCD_TEMP_CONTROL,
     // VR_GET_CCD_TEMP_CONTROL,
        VR_GET_CONTINUOUS_CCD,
     // VR_GET_LASER_TEMP,
        VR_GET_NUM_FRAMES,
     // VR_READ_CCD_TEMPERATURE,
        VR_SET_CONTINUOUS_CCD,
        VR_SET_NUM_FRAMES
    }

    /// <summary>
    /// This interface is provided for COM clients (Delphi etc) who seem to find it useful.
    /// I don't know that .NET users would find much benefit in it.
    /// </summary>
    [ComVisible(true)]
    [Guid("DC076FE7-2203-4B6F-867C-287E982D007A")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IOpcodeHelper
    {
        Dictionary<Opcodes, byte> getDict();
        HashSet<Opcodes> getArmInvertedRetvals();
    }
}
