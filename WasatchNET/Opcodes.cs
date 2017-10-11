using System.Collections.Generic;

namespace WasatchNET
{
    ////////////////////////////////////////////////////////////////////////////
    // Enumerations
    ////////////////////////////////////////////////////////////////////////////

    // move to FPGAOptions?
    public enum FPGA_INTEG_TIME_RES { ONE_MS, TEN_MS, SWITCHABLE, ERROR };
    public enum FPGA_DATA_HEADER { NONE, OCEAN_OPTICS, WASATCH, ERROR };
    public enum FPGA_LASER_TYPE { NONE, INTERNAL, EXTERNAL, ERROR };
    public enum FPGA_LASER_CONTROL { MODULATION, TRANSITION_POINTS, RAMPING, ERROR };

    public enum CCD_TRIGGER_SOURCE { USB, EXTERNAL, ERROR };
    public enum EXTERNAL_TRIGGER_OUTPUT { LASER_MODULATION, INTEGRATION_ACTIVE_PULSE, ERROR };
    public enum HORIZ_BINNING { NONE, TWO_PIXEL, FOUR_PIXEL, ERROR };

    /// <summary>
    /// Convenience enum for mapping USB API commands to stringifiable English 
    /// labels. 
    /// </summary>
    /// <remarks>
    /// At this time, I am keeping these aligned with the USB API documentation 
    /// (mostly) for easy cross-referencing with manuals (e.g. GET_LASER instead 
    /// of GET_LASER_ENABLED).
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
    /// Utility class for automating Opcode operations and processing.
    /// </summary>
    /// <remarks>
    /// It would be slightly more efficient if we made these constants,
    /// rather than Dictionary lookups. However, accessing them through
    /// an Enum key provides significant advantages in troubleshooting
    /// and error reporting, as the Enum can always be stringified. As
    /// this driver is oriented toward rapid-prototyping rather than
    /// maximum performance, I'm sticking with the map for now.
    /// </remarks>
    public class OpcodeHelper
    {
        static readonly OpcodeHelper instance = new OpcodeHelper();

        public static OpcodeHelper getInstance()
        {
            return instance;
        }

        Dictionary<Opcodes, byte> cmd = new Dictionary<Opcodes, byte>();

        public Dictionary<Opcodes, byte> getDict()
        {
            return cmd;
        }
        
        OpcodeHelper()
        {
            // bRequest commands
            cmd[Opcodes.ACQUIRE_CCD                            ] = 0xad;
            cmd[Opcodes.GET_ACTUAL_FRAMES                      ] = 0xe4;
            cmd[Opcodes.GET_ACTUAL_INTEGRATION_TIME            ] = 0xdf;
            cmd[Opcodes.GET_CCD_GAIN                           ] = 0xc5;
            cmd[Opcodes.GET_CCD_OFFSET                         ] = 0xc4;
            cmd[Opcodes.GET_CCD_SENSING_THRESHOLD              ] = 0xd1;
            cmd[Opcodes.GET_CCD_TEMP                           ] = 0xd7;
            cmd[Opcodes.GET_CCD_TEMP_ENABLE                    ] = 0xda;
            cmd[Opcodes.GET_CCD_TEMP_SETPOINT                  ] = 0xd9;
            cmd[Opcodes.GET_DAC                                ] = 0xd9;
            cmd[Opcodes.GET_CCD_THRESHOLD_SENSING_MODE         ] = 0xcf;
            cmd[Opcodes.GET_CCD_TRIGGER_SOURCE                 ] = 0xd3;
            cmd[Opcodes.GET_CODE_REVISION                      ] = 0xc0;
            cmd[Opcodes.GET_EXTERNAL_TRIGGER_OUTPUT            ] = 0xe1;
            cmd[Opcodes.GET_FPGA_REV                           ] = 0xb4;
            cmd[Opcodes.GET_HORIZ_BINNING                      ] = 0xbc;
            cmd[Opcodes.GET_INTEGRATION_TIME                   ] = 0xbf;
            cmd[Opcodes.GET_INTERLOCK                          ] = 0xef;
            cmd[Opcodes.GET_LASER                              ] = 0xe2;
            cmd[Opcodes.GET_LASER_MOD                          ] = 0xe3;
            cmd[Opcodes.GET_LASER_MOD_PULSE_WIDTH              ] = 0xdc;
            cmd[Opcodes.GET_LASER_RAMPING_MODE                 ] = 0xea;
            cmd[Opcodes.GET_LASER_TEMP                         ] = 0xd5;
            cmd[Opcodes.GET_LASER_TEMP_SETPOINT                ] = 0xe8;
            cmd[Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME ] = 0xde;
            cmd[Opcodes.GET_MOD_DURATION                       ] = 0xc3;
            cmd[Opcodes.GET_MOD_PERIOD                         ] = 0xcb;
            cmd[Opcodes.GET_MOD_PULSE_DELAY                    ] = 0xca;
            cmd[Opcodes.GET_SELECTED_LASER                     ] = 0xee;
            cmd[Opcodes.GET_TRIGGER_DELAY                      ] = 0xab;
            cmd[Opcodes.LINK_LASER_MOD_TO_INTEGRATION_TIME     ] = 0xdd;
            cmd[Opcodes.POLL_DATA                              ] = 0xd4;
            cmd[Opcodes.SECOND_TIER_COMMAND                    ] = 0xff;
            cmd[Opcodes.SELECT_HORIZ_BINNING                   ] = 0xb8;
            cmd[Opcodes.SELECT_LASER                           ] = 0xed;
            cmd[Opcodes.SET_CCD_GAIN                           ] = 0xb7;
            cmd[Opcodes.SET_CCD_OFFSET                         ] = 0xb6;
            cmd[Opcodes.SET_CCD_SENSING_THRESHOLD              ] = 0xd0;
            cmd[Opcodes.SET_CCD_TEMP_ENABLE                    ] = 0xd6;
            cmd[Opcodes.SET_CCD_TEMP_SETPOINT                  ] = 0xd8;
            cmd[Opcodes.SET_CCD_THRESHOLD_SENSING_MODE         ] = 0xce;
            cmd[Opcodes.SET_CCD_TRIGGER_SOURCE                 ] = 0xd2;
            cmd[Opcodes.SET_DAC                                ] = 0xd8;
            cmd[Opcodes.SET_EXTERNAL_TRIGGER_OUTPUT            ] = 0xe0;
            cmd[Opcodes.SET_INTEGRATION_TIME                   ] = 0xb2;
            cmd[Opcodes.SET_LASER                              ] = 0xbe;
            cmd[Opcodes.SET_LASER_MOD                          ] = 0xbd;
            cmd[Opcodes.SET_LASER_MOD_DUR                      ] = 0xb9;
            cmd[Opcodes.SET_LASER_MOD_PULSE_WIDTH              ] = 0xdb;
            cmd[Opcodes.SET_LASER_RAMPING_MODE                 ] = 0xe9;
            cmd[Opcodes.SET_LASER_TEMP_SETPOINT                ] = 0xe7;
            cmd[Opcodes.SET_MOD_PERIOD                         ] = 0xc7;
            cmd[Opcodes.SET_MOD_PULSE_DELAY                    ] = 0xc6;
            cmd[Opcodes.SET_MODEL_CONFIG_REAL                  ] = 0xa2; // TODO: add to USB API docs
            cmd[Opcodes.SET_TRIGGER_DELAY                      ] = 0xaa;
            cmd[Opcodes.VR_GET_CONTINUOUS_CCD                  ] = 0xcc; // VR = Vendor Request, old Cypress EzUSB nomenclature
            cmd[Opcodes.VR_GET_NUM_FRAMES                      ] = 0xcd;
            cmd[Opcodes.VR_SET_CONTINUOUS_CCD                  ] = 0xc8;
            cmd[Opcodes.VR_SET_NUM_FRAMES                      ] = 0xc9;

            // these are duplicate VR_ labels from our USB API docs, but the 
            // opcodes are implemented elsewhere above
         // cmd[Opcodes.VR_ENABLE_CCD_TEMP_CONTROL             ] = 0xd6;
         // cmd[Opcodes.VR_GET_CCD_TEMP_CONTROL                ] = 0xda;
         // cmd[Opcodes.VR_GET_LASER_TEMP                      ] = 0xd5;
         // cmd[Opcodes.VR_READ_CCD_TEMPERATURE                ] = 0xd7;

            // wValue for SECOND_TIER_COMMAND
            cmd[Opcodes.GET_MODEL_CONFIG                       ] = 0x01;
            cmd[Opcodes.SET_MODEL_CONFIG_DO_NOT_USE            ] = 0x02; // DO NOT USE
            cmd[Opcodes.GET_LINE_LENGTH                        ] = 0x03;
            cmd[Opcodes.READ_COMPILATION_OPTIONS               ] = 0x04;
            cmd[Opcodes.OPT_INT_TIME_RES                       ] = 0x05;
            cmd[Opcodes.OPT_DATA_HDR_TAB                       ] = 0x06;
            cmd[Opcodes.OPT_CF_SELECT                          ] = 0x07;
            cmd[Opcodes.OPT_LASER                              ] = 0x08;
            cmd[Opcodes.OPT_LASER_CONTROL                      ] = 0x09;
            cmd[Opcodes.OPT_AREA_SCAN                          ] = 0x0a;
            cmd[Opcodes.OPT_ACT_INT_TIME                       ] = 0x0b;
            cmd[Opcodes.OPT_HORIZONTAL_BINNING                 ] = 0x0c;
        }
    }
}