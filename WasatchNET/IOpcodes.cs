namespace WasatchNET
{
    public enum TRIGGER_SOURCE { INTERNAL, EXTERNAL, ERROR };
    public enum EXTERNAL_TRIGGER_OUTPUT { LASER_MODULATION, INTEGRATION_ACTIVE_PULSE, ERROR };
    public enum HORIZONTAL_BINNING { NONE, TWO_PIXEL, FOUR_PIXEL, ERROR };

    /// <summary>
    /// Convenience enum for mapping USB API commands to stringifiable English labels. 
    /// </summary>
    /// <remarks>
    /// </remarks>
    public enum Opcodes
    {
        ACQUIRE_SPECTRUM,
        GET_ACTUAL_FRAMES,
        GET_ACTUAL_INTEGRATION_TIME,
        GET_ADC_RAW,
        GET_AREA_SCAN_ENABLE,
        GET_BATTERY_STATE,
        GET_CF_SELECT,
        GET_COMPILATION_OPTIONS,
        GET_CONTINUOUS_ACQUISITION,
        GET_CONTINUOUS_FRAMES,
        GET_DETECTOR_GAIN,
        GET_DETECTOR_GAIN_ODD,
        GET_DETECTOR_OFFSET,
        GET_DETECTOR_OFFSET_ODD,
        GET_DETECTOR_SENSING_THRESHOLD,
        GET_DETECTOR_SENSING_THRESHOLD_ENABLE,
        GET_DETECTOR_TEC_ENABLE,
        GET_DETECTOR_TEC_SETPOINT,
        GET_DETECTOR_TEMPERATURE,
        GET_FIRMWARE_REVISION,
        GET_FPGA_REVISION,
        GET_HORIZONTAL_BINNING,
        GET_INTEGRATION_TIME,
        GET_LASER_ENABLE,
        GET_LASER_INTERLOCK,
        GET_LASER_MOD_DURATION,
        GET_LASER_MOD_ENABLE,
        GET_LASER_MOD_PERIOD,
        GET_LASER_MOD_PULSE_DELAY,
        GET_LASER_MOD_PULSE_WIDTH,
        GET_LASER_RAMPING_MODE, // not implemented
        GET_LASER_TEC_SETPOINT,
        GET_LINE_LENGTH,
        GET_LINK_LASER_MOD_TO_INTEGRATION_TIME,
        GET_MODEL_CONFIG,
        GET_OPT_ACTUAL_INTEGRATION_TIME,
        GET_OPT_AREA_SCAN,
        GET_OPT_CF_SELECT,
        GET_OPT_DATA_HEADER_TAG,
        GET_OPT_HORIZONTAL_BINNING,
        GET_OPT_INTEGRATION_TIME_RESOLUTION,
        GET_OPT_LASER_CONTROL,
        GET_OPT_LASER_TYPE,
        GET_SELECTED_ADC,
        GET_TRIGGER_DELAY,
        GET_TRIGGER_OUTPUT,
        GET_TRIGGER_SOURCE,
        POLL_DATA,
        SECOND_TIER_COMMAND,
        SET_AREA_SCAN_ENABLE,
        SET_CF_SELECT,
        SET_CONTINUOUS_ACQUISITION,
        SET_CONTINUOUS_FRAMES,
        SET_DETECTOR_GAIN,
        SET_DETECTOR_GAIN_ODD,
        SET_DETECTOR_OFFSET,
        SET_DETECTOR_OFFSET_ODD,
        SET_DETECTOR_SENSING_THRESHOLD,
        SET_DETECTOR_SENSING_THRESHOLD_ENABLE,
        SET_DETECTOR_TEC_ENABLE,
        SET_DETECTOR_TEC_SETPOINT,
        SET_DFU_MODE,
        SET_HORIZONTAL_BINNING,
        SET_INTEGRATION_TIME,
        SET_LASER_ENABLE,
        SET_LASER_MOD_DURATION,
        SET_LASER_MOD_ENABLE,
        SET_LASER_MOD_PERIOD,
        SET_LASER_MOD_PULSE_DELAY,
        SET_LASER_MOD_PULSE_WIDTH,
        SET_LASER_RAMPING_MODE, // not implemented
        SET_LASER_TEC_SETPOINT,
        SET_LINK_LASER_MOD_TO_INTEGRATION_TIME,
        SET_MODEL_CONFIG_DO_NOT_USE,
        SET_MODEL_CONFIG_REAL,
        SET_SELECTED_ADC,
        SET_TRIGGER_DELAY,
        SET_TRIGGER_OUTPUT,
        SET_TRIGGER_SOURCE
    }
}
