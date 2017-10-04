using System.Collections.Generic;

namespace WasatchNET
{
    ////////////////////////////////////////////////////////////////////////////
    // Enumerations
    ////////////////////////////////////////////////////////////////////////////

    // funny that C# lets you define enums in a namespace, but not constants :-/
    public enum FPGA_INTEG_TIME_RES { ONE_MS, TEN_MS, SWITCHABLE, ERROR };
    public enum FPGA_DATA_HEADER { NONE, OCEAN_OPTICS, WASATCH, ERROR };
    public enum FPGA_LASER_TYPE { NONE, INTERNAL, EXTERNAL, ERROR };
    public enum FPGA_LASER_CONTROL { MODULATION, TRANSITION_POINTS, RAMPING, ERROR };
    public enum CCD_TRIGGER_SOURCE { USB, EXTERNAL, ERROR };
    public enum EXTERNAL_TRIGGER_OUTPUT {  LASER_MODULATION, INTEGRATION_ACTIVE_PULSE, ERROR };
    public enum HORIZ_BINNING { NONE, TWO_PIXEL, FOUR_PIXEL, ERROR };

    /// <summary>
    /// Convenience enum for mapping USB API commands to stringifiable English 
    /// labels. At this time, I am keeping these aligned with the USB API 
    /// documentation for easy cross-referencing with manuals (e.g. GET_LASER
    /// instead of GET_LASER_ENABLED).
    /// </summary>
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
        GET_CODE_REVISION,
        GET_DAC,
        GET_EXTERNAL_TRIGGER_OUTPUT,
        GET_FPGA_REV,
        GET_HORIZ_BINNING,
        GET_INTEGRATION_TIME,
        GET_INTERLOCK,
        GET_LASER,
        GET_LASER_MOD,
        GET_LASER_MOD_PULSE_WIDTH,
        GET_LASER_RAMPING_MODE,
        GET_LASER_TEMP,
        GET_LASER_TEMP_SETPOINT,
        GET_LINE_LENGTH,
        GET_LINK_LASER_MOD_TO_INTEGRATION_TIME,
        GET_MODEL_CONFIG,
        GET_MOD_DURATION,
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
        SET_CCD_TEMP_ENABLE,
        SET_CCD_TEMP_SETPOINT,
        SET_CCD_THRESHOLD_SENSING_MODE,
        SET_CCD_TRIGGER_SOURCE,
        SET_DAC,
        SET_EXTERNAL_TRIGGER_OUTPUT,
        SET_INTEGRATION_TIME,
        SET_LASER,
        SET_LASER_MOD,
        SET_LASER_MOD_DUR,
        SET_LASER_MOD_PULSE_WIDTH,
        SET_LASER_RAMPING_MODE,
        SET_LASER_TEMP_SETPOINT,
        SET_MODEL_CONFIG,
        SET_MOD_PERIOD,
        SET_MOD_PULSE_DELAY,
        SET_TRIGGER_DELAY,
        VR_ENABLE_CCD_TEMP_CONTROL,
        VR_GET_CCD_TEMP_CONTROL,
        VR_GET_CONTINUOUS_CCD,
        VR_GET_LASER_TEMP,
        VR_GET_NUM_FRAMES,
        VR_READ_CCD_TEMPERATURE,
        VR_SET_CONTINUOUS_CCD,
        VR_SET_NUM_FRAMES
    }

    /// <summary>
    /// Utility class for automating Opcode operations and processing.
    /// </summary>
    public class OpcodeUtil
    {
        /// <summary>
        /// It would be slightly more efficient if we made these constants,
        /// rather than Dictionary lookups. However, accessing them through
        /// an Enum key provides significant advantages in troubleshooting
        /// and error reporting, as the Enum can always be stringified. As
        /// this driver is oriented toward rapid-prototyping rather than
        /// maximum performance, I'm sticking with the map for now.
        /// </summary>
        /// <returns>A dictionary of the USB API byte value for each Opcode.</returns>
        public static Dictionary<Opcodes, byte> getDictionary()
        {
            Dictionary<Opcodes, byte> cmd = new Dictionary<Opcodes, byte>();

            // bRequest commands
            cmd[Opcodes.ACQUIRE_CCD                            ] = 0xad; // impl
            cmd[Opcodes.GET_ACTUAL_FRAMES                      ] = 0xe4; // impl
            cmd[Opcodes.GET_ACTUAL_INTEGRATION_TIME            ] = 0xdf; // impl
            cmd[Opcodes.GET_CCD_GAIN                           ] = 0xc5; // impl
            cmd[Opcodes.GET_CCD_OFFSET                         ] = 0xc4; // impl
            cmd[Opcodes.GET_CCD_SENSING_THRESHOLD              ] = 0xd1; // impl
            cmd[Opcodes.GET_CCD_TEMP                           ] = 0xd7; // impl
            cmd[Opcodes.GET_CCD_TEMP_ENABLE                    ] = 0xda; // impl
            cmd[Opcodes.GET_CCD_TEMP_SETPOINT                  ] = 0xd9; // impl
            cmd[Opcodes.GET_DAC                                ] = 0xd9; // impl
            cmd[Opcodes.GET_CCD_THRESHOLD_SENSING_MODE         ] = 0xcf; // impl
            cmd[Opcodes.GET_CCD_TRIGGER_SOURCE                 ] = 0xd3; // impl
            cmd[Opcodes.GET_CODE_REVISION                      ] = 0xc0; // impl
            cmd[Opcodes.GET_EXTERNAL_TRIGGER_OUTPUT            ] = 0xe1; // impl
            cmd[Opcodes.GET_FPGA_REV                           ] = 0xb4; // impl
            cmd[Opcodes.GET_HORIZ_BINNING                      ] = 0xbc; // impl
            cmd[Opcodes.GET_INTEGRATION_TIME                   ] = 0xbf; // impl
            cmd[Opcodes.GET_INTERLOCK                          ] = 0xef; // impl
            cmd[Opcodes.GET_LASER                              ] = 0xe2; // impl
            cmd[Opcodes.GET_LASER_MOD                          ] = 0xe3; // impl
            cmd[Opcodes.GET_LASER_MOD_PULSE_WIDTH              ] = 0xdc; // impl
            cmd[Opcodes.GET_LASER_RAMPING_MODE                 ] = 0xea; // impl
            cmd[Opcodes.GET_LASER_TEMP                         ] = 0xd5; // impl
            cmd[Opcodes.GET_LASER_TEMP_SETPOINT                ] = 0xe8; // impl
            cmd[Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME ] = 0xde; // impl
            cmd[Opcodes.GET_MOD_DURATION                       ] = 0xc3; // impl
            cmd[Opcodes.GET_MOD_PERIOD                         ] = 0xcb; // impl
            cmd[Opcodes.GET_MOD_PULSE_DELAY                    ] = 0xca; // impl
            cmd[Opcodes.GET_SELECTED_LASER                     ] = 0xee; // impl
            cmd[Opcodes.GET_TRIGGER_DELAY                      ] = 0xab; // impl
            cmd[Opcodes.LINK_LASER_MOD_TO_INTEGRATION_TIME     ] = 0xdd; // impl
            cmd[Opcodes.POLL_DATA                              ] = 0xd4; // impl
            cmd[Opcodes.SECOND_TIER_COMMAND                    ] = 0xff; // impl
            cmd[Opcodes.SELECT_HORIZ_BINNING                   ] = 0xb8; // impl
            cmd[Opcodes.SELECT_LASER                           ] = 0xed; // impl
            cmd[Opcodes.SET_CCD_GAIN                           ] = 0xb7; // impl
            cmd[Opcodes.SET_CCD_OFFSET                         ] = 0xb6; // impl
            cmd[Opcodes.SET_CCD_SENSING_THRESHOLD              ] = 0xd0; // impl
            cmd[Opcodes.SET_CCD_TEMP_ENABLE                    ] = 0xd6; // impl
            cmd[Opcodes.SET_CCD_TEMP_SETPOINT                  ] = 0xd8; // impl
            cmd[Opcodes.SET_CCD_THRESHOLD_SENSING_MODE         ] = 0xce; // impl
            cmd[Opcodes.SET_CCD_TRIGGER_SOURCE                 ] = 0xd2; // impl
            cmd[Opcodes.SET_DAC                                ] = 0xd8; // impl
            cmd[Opcodes.SET_EXTERNAL_TRIGGER_OUTPUT            ] = 0xe0; // impl
            cmd[Opcodes.SET_INTEGRATION_TIME                   ] = 0xb2; // impl
            cmd[Opcodes.SET_LASER                              ] = 0xbe; // impl
            cmd[Opcodes.SET_LASER_MOD                          ] = 0xbd; // impl
            cmd[Opcodes.SET_LASER_MOD_DUR                      ] = 0xb9; // impl
            cmd[Opcodes.SET_LASER_MOD_PULSE_WIDTH              ] = 0xdb; // impl
            cmd[Opcodes.SET_LASER_RAMPING_MODE                 ] = 0xe9; // impl
            cmd[Opcodes.SET_LASER_TEMP_SETPOINT                ] = 0xe7; // impl
            cmd[Opcodes.SET_MOD_PERIOD                         ] = 0xc7;
            cmd[Opcodes.SET_MOD_PULSE_DELAY                    ] = 0xc6;
            cmd[Opcodes.SET_TRIGGER_DELAY                      ] = 0xaa;
            cmd[Opcodes.VR_ENABLE_CCD_TEMP_CONTROL             ] = 0xd6;
            cmd[Opcodes.VR_GET_CCD_TEMP_CONTROL                ] = 0xda;
            cmd[Opcodes.VR_GET_CONTINUOUS_CCD                  ] = 0xcc;
            cmd[Opcodes.VR_GET_LASER_TEMP                      ] = 0xd5;
            cmd[Opcodes.VR_GET_NUM_FRAMES                      ] = 0xcd;
            cmd[Opcodes.VR_READ_CCD_TEMPERATURE                ] = 0xd7;
            cmd[Opcodes.VR_SET_CONTINUOUS_CCD                  ] = 0xc8;
            cmd[Opcodes.VR_SET_NUM_FRAMES                      ] = 0xc9;

            // wValue for SECOND_TIER_COMMAND
            cmd[Opcodes.GET_MODEL_CONFIG                       ] = 0x01; // impl
            cmd[Opcodes.SET_MODEL_CONFIG                       ] = 0x02;
            cmd[Opcodes.GET_LINE_LENGTH                        ] = 0x03; // impl
            cmd[Opcodes.READ_COMPILATION_OPTIONS               ] = 0x04; // impl
            cmd[Opcodes.OPT_INT_TIME_RES                       ] = 0x05; // impl
            cmd[Opcodes.OPT_DATA_HDR_TAB                       ] = 0x06; // impl
            cmd[Opcodes.OPT_CF_SELECT                          ] = 0x07; // impl
            cmd[Opcodes.OPT_LASER                              ] = 0x08; // impl
            cmd[Opcodes.OPT_LASER_CONTROL                      ] = 0x09; // impl
            cmd[Opcodes.OPT_AREA_SCAN                          ] = 0x0a; // impl
            cmd[Opcodes.OPT_ACT_INT_TIME                       ] = 0x0b; // impl
            cmd[Opcodes.OPT_HORIZONTAL_BINNING                 ] = 0x0c; // impl

            return cmd;
        }
    }
}
