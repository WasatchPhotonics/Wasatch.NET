﻿using System.Collections.Generic;

namespace WasatchNET
{
    /// <summary>
    /// Utility class for automating Opcode operations and processing.
    /// </summary>
    /// <remarks>
    /// It would be slightly more efficient if we made these constants,
    /// rather than Dictionary lookups. However, accessing them through
    /// an enum key provides significant advantages in troubleshooting
    /// and error reporting, as the enum can always be stringified. As
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
        HashSet<Opcodes> armInvertedRetvals = new HashSet<Opcodes>();

        public Dictionary<Opcodes, byte> getDict()
        {
            return cmd;
        }

        public HashSet<Opcodes> getArmInvertedRetvals()
        {
            return armInvertedRetvals;
        }
        
        OpcodeHelper()
        {
            // bRequest commands
            cmd[Opcodes.ACQUIRE_SPECTRUM                       ] = 0xad;
            cmd[Opcodes.FPGA_RESET                             ] = 0xb5;
            //TO-DO: add GET_ACCESSORY_ENABLED once implemented
            cmd[Opcodes.GET_ACTUAL_FRAMES                      ] = 0xe4;
            cmd[Opcodes.GET_ACTUAL_INTEGRATION_TIME            ] = 0xdf;
            cmd[Opcodes.GET_ADC_RAW                            ] = 0xd5;
            cmd[Opcodes.GET_AREA_SCAN_ENABLE                   ] = 0xea; // was: GET_LASER_RAMPING_ENABLE
            cmd[Opcodes.GET_CF_SELECT                          ] = 0xec;
            cmd[Opcodes.GET_CONTINUOUS_ACQUISITION             ] = 0xcc;
            cmd[Opcodes.GET_CONTINUOUS_FRAMES                  ] = 0xcd;
            cmd[Opcodes.GET_DETECTOR_GAIN                      ] = 0xc5;
            cmd[Opcodes.GET_DETECTOR_GAIN_ODD                  ] = 0x9f;
            cmd[Opcodes.GET_DETECTOR_OFFSET                    ] = 0xc4;
            cmd[Opcodes.GET_DETECTOR_OFFSET_ODD                ] = 0x9e;
            cmd[Opcodes.GET_DETECTOR_SENSING_THRESHOLD         ] = 0xd1;
            cmd[Opcodes.GET_DETECTOR_SENSING_THRESHOLD_ENABLE  ] = 0xcf;
            cmd[Opcodes.GET_DETECTOR_TEC_ENABLE                ] = 0xda;
            cmd[Opcodes.GET_DETECTOR_TEC_SETPOINT              ] = 0xd9;
            cmd[Opcodes.GET_DETECTOR_TEMPERATURE               ] = 0xd7;
            cmd[Opcodes.GET_FIRMWARE_REVISION                  ] = 0xc0;
            cmd[Opcodes.GET_FPGA_REVISION                      ] = 0xb4;
            cmd[Opcodes.GET_HORIZONTAL_BINNING                 ] = 0xbc;
            cmd[Opcodes.GET_INTEGRATION_TIME                   ] = 0xbf;
            cmd[Opcodes.GET_LAMP_ENABLE                        ] = 0x33;
            cmd[Opcodes.GET_LASER_ENABLE                       ] = 0xe2;
            cmd[Opcodes.GET_LASER_INTERLOCK                    ] = 0xef; // aka, CAN_LASER_FIRE
            cmd[Opcodes.GET_LASER_MOD_DURATION                 ] = 0xc3;
            cmd[Opcodes.GET_LASER_MOD_ENABLE                   ] = 0xe3;
            cmd[Opcodes.GET_LASER_MOD_PERIOD                   ] = 0xcb;
            cmd[Opcodes.GET_LASER_MOD_PULSE_DELAY              ] = 0xca;
            cmd[Opcodes.GET_LASER_MOD_PULSE_WIDTH              ] = 0xdc;
            cmd[Opcodes.GET_LASER_TEC_SETPOINT                 ] = 0xe8;
            cmd[Opcodes.GET_LASER_TEC_MODE                     ] = 0x85;
            cmd[Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME ] = 0xde;
            cmd[Opcodes.GET_SELECTED_ADC                       ] = 0xee;
            cmd[Opcodes.GET_TRIGGER_DELAY                      ] = 0xab;
            cmd[Opcodes.GET_TRIGGER_OUTPUT                     ] = 0xe1;
            cmd[Opcodes.GET_TRIGGER_SOURCE                     ] = 0xd3;
            cmd[Opcodes.POLL_DATA                              ] = 0xd4; // aka, GET_UNTETHERED_CAPTURE_STATUS
            cmd[Opcodes.SECOND_TIER_COMMAND                    ] = 0xff;
            //cmd[Opcodes.SET_AREA_SCAN_ENABLE                   ] = 0xe9; // was: SET_LASER_RAMPING_ENABLE
            cmd[Opcodes.SET_ACCESSORY_ENABLE                   ] = 0x22;
            cmd[Opcodes.SET_AREA_SCAN_ENABLE                   ] = 0xeb; // legacy opcode...is the actual opcode now
            cmd[Opcodes.SET_CF_SELECT                          ] = 0xeb;
            cmd[Opcodes.SET_CONTINUOUS_ACQUISITION             ] = 0xc8;
            cmd[Opcodes.SET_CONTINUOUS_FRAMES                  ] = 0xc9;
            cmd[Opcodes.SET_DETECTOR_GAIN                      ] = 0xb7;
            cmd[Opcodes.SET_DETECTOR_GAIN_ODD                  ] = 0x9d;
            cmd[Opcodes.SET_DETECTOR_OFFSET                    ] = 0xb6;
            cmd[Opcodes.SET_DETECTOR_OFFSET_ODD                ] = 0x9c;
            cmd[Opcodes.SET_DETECTOR_SENSING_THRESHOLD         ] = 0xd0;
            cmd[Opcodes.SET_DETECTOR_SENSING_THRESHOLD_ENABLE  ] = 0xce;
            cmd[Opcodes.SET_DETECTOR_TEC_ENABLE                ] = 0xd6;
            cmd[Opcodes.SET_DETECTOR_TEC_SETPOINT              ] = 0xd8;
            cmd[Opcodes.SET_DFU_MODE                           ] = 0xfe;
            cmd[Opcodes.SET_HORIZONTAL_BINNING                 ] = 0xb8;
            cmd[Opcodes.SET_INTEGRATION_TIME                   ] = 0xb2;
            cmd[Opcodes.SET_LAMP_ENABLE                        ] = 0x32;
            cmd[Opcodes.SET_LASER_ENABLE                       ] = 0xbe;
            cmd[Opcodes.SET_LASER_MOD_DURATION                 ] = 0xb9;
            cmd[Opcodes.SET_LASER_MOD_ENABLE                   ] = 0xbd;
            cmd[Opcodes.SET_LASER_MOD_PERIOD                   ] = 0xc7;
            cmd[Opcodes.SET_LASER_MOD_PULSE_DELAY              ] = 0xc6;
            cmd[Opcodes.SET_LASER_MOD_PULSE_WIDTH              ] = 0xdb;
            cmd[Opcodes.SET_LASER_TEC_SETPOINT                 ] = 0xe7;
            cmd[Opcodes.SET_LASER_TEC_MODE                     ] = 0x84;
            cmd[Opcodes.SET_LINK_LASER_MOD_TO_INTEGRATION_TIME ] = 0xdd;
            cmd[Opcodes.SET_MODEL_CONFIG_FX2                   ] = 0xa2; // legacy, used for FX2
            cmd[Opcodes.SET_SELECTED_ADC                       ] = 0xed;
            cmd[Opcodes.SET_TRIGGER_DELAY                      ] = 0xaa;
            cmd[Opcodes.SET_TRIGGER_OUTPUT                     ] = 0xe0;
            cmd[Opcodes.SET_TRIGGER_SOURCE                     ] = 0xd2;

            // wValue for SECOND_TIER_COMMAND
            cmd[Opcodes.GET_MODEL_CONFIG                       ] = 0x01;
            cmd[Opcodes.SET_MODEL_CONFIG_ARM                   ] = 0x02; // rdickerson reported works, using for ARM onwards
            cmd[Opcodes.GET_LINE_LENGTH                        ] = 0x03;
            cmd[Opcodes.GET_COMPILATION_OPTIONS                ] = 0x04;
            cmd[Opcodes.GET_OPT_INTEGRATION_TIME_RESOLUTION    ] = 0x05;
            cmd[Opcodes.GET_OPT_DATA_HEADER_TAG                ] = 0x06;
            cmd[Opcodes.GET_OPT_CF_SELECT                      ] = 0x07;
            cmd[Opcodes.GET_OPT_LASER_TYPE                     ] = 0x08;
            cmd[Opcodes.GET_OPT_LASER_CONTROL                  ] = 0x09;
            cmd[Opcodes.GET_OPT_AREA_SCAN                      ] = 0x0a;
            cmd[Opcodes.GET_OPT_ACTUAL_INTEGRATION_TIME        ] = 0x0b;
            cmd[Opcodes.GET_OPT_HORIZONTAL_BINNING             ] = 0x0c;
            cmd[Opcodes.GET_LASER_FIRING                       ] = 0x0d; // AKA, IS_LASER_FIRING
            cmd[Opcodes.GET_BATTERY_STATE                      ] = 0x13;
            cmd[Opcodes.GET_LASER_WATCHDOG_SEC                 ] = 0x17;
            cmd[Opcodes.SET_LASER_WATCHDOG_SEC                 ] = 0x18;
            cmd[Opcodes.SET_DETECTOR_START_LINE                ] = 0x21;
            cmd[Opcodes.GET_DETECTOR_START_LINE                ] = 0x22;
            cmd[Opcodes.SET_DETECTOR_STOP_LINE                 ] = 0x23;
            cmd[Opcodes.GET_DETECTOR_STOP_LINE                 ] = 0x24;
            cmd[Opcodes.GET_STORAGE                            ] = 0x25;
            cmd[Opcodes.ERASE_STORAGE                          ] = 0x26;
            cmd[Opcodes.SET_FEEDBACK                           ] = 0x27;
            cmd[Opcodes.WRITE_LIBRARY                          ] = 0x28;
            cmd[Opcodes.PROCESS_LIBRARY                        ] = 0x29;
            cmd[Opcodes.GET_AMBIENT_TEMPERATURE_ARM            ] = 0x2a;
            cmd[Opcodes.GET_BLE_FW_VER_INFO                    ] = 0x2d;

            // TODO: implement, test and document these 2nd-tier commands (see vend_ax.h)
            //
            // 0x0d = clear error codes
            // 0x0e = retrieve error code list
            // 0x0f = set external laser power
            // 0x10 = get external laser power
            // 0x11 = I2C sensor write (IMX)
            // 0x12 = I2C sensor read  (IMX)

            // abandoned / former opcodes
            // 0xb3 = FPGA config register (16bit)
            // 0xbb = correlated double-sampling

            // this list has not been double-checked; ENLIGHTEN ignores return values :-(
            armInvertedRetvals.Add(Opcodes.ACQUIRE_SPECTRUM);
            armInvertedRetvals.Add(Opcodes.SET_LINK_LASER_MOD_TO_INTEGRATION_TIME);
            armInvertedRetvals.Add(Opcodes.SET_SELECTED_ADC);
            armInvertedRetvals.Add(Opcodes.SET_DETECTOR_GAIN);
            armInvertedRetvals.Add(Opcodes.SET_DETECTOR_OFFSET);
            armInvertedRetvals.Add(Opcodes.SET_DETECTOR_SENSING_THRESHOLD);
            armInvertedRetvals.Add(Opcodes.SET_DETECTOR_SENSING_THRESHOLD_ENABLE);
            armInvertedRetvals.Add(Opcodes.SET_DETECTOR_TEC_ENABLE);
            armInvertedRetvals.Add(Opcodes.SET_DETECTOR_TEC_SETPOINT);
            armInvertedRetvals.Add(Opcodes.SET_TRIGGER_SOURCE);
            armInvertedRetvals.Add(Opcodes.SET_TRIGGER_OUTPUT);
            armInvertedRetvals.Add(Opcodes.SET_INTEGRATION_TIME);
            armInvertedRetvals.Add(Opcodes.SET_LASER_ENABLE);
            armInvertedRetvals.Add(Opcodes.SET_LASER_MOD_ENABLE);
            armInvertedRetvals.Add(Opcodes.SET_LASER_MOD_DURATION);
            armInvertedRetvals.Add(Opcodes.SET_LASER_MOD_PULSE_WIDTH);
            armInvertedRetvals.Add(Opcodes.SET_LASER_TEC_SETPOINT);
            armInvertedRetvals.Add(Opcodes.SET_LASER_MOD_PERIOD);
            armInvertedRetvals.Add(Opcodes.SET_LASER_MOD_PULSE_DELAY);
            armInvertedRetvals.Add(Opcodes.SET_CONTINUOUS_ACQUISITION);
            armInvertedRetvals.Add(Opcodes.SET_CONTINUOUS_FRAMES);
            armInvertedRetvals.Add(Opcodes.SET_MODEL_CONFIG_ARM);
            armInvertedRetvals.Add(Opcodes.WRITE_LIBRARY);
            armInvertedRetvals.Add(Opcodes.PROCESS_LIBRARY);
        }
    }
    public class OCTOpcodeHelper 
    {
        static readonly OCTOpcodeHelper instance = new OCTOpcodeHelper();

        public static OCTOpcodeHelper getInstance()
        {
            return instance;
        }

        Dictionary<Opcodes, string> cmd = new Dictionary<Opcodes, string>();

        public Dictionary<Opcodes, string> getDict()
        {
            return cmd;
        }

        OCTOpcodeHelper()
        {
            cmd[Opcodes.GET_DETECTOR_GAIN                      ] = "tbd";
            cmd[Opcodes.GET_DETECTOR_OFFSET                    ] = "tbd";
            cmd[Opcodes.GET_FIRMWARE_REVISION                  ] = "tbd";
            cmd[Opcodes.GET_FPGA_REVISION                      ] = "tbd";
            cmd[Opcodes.GET_INTEGRATION_TIME                   ] = "r tint";
            cmd[Opcodes.GET_LINE_PERIOD                        ] = "r tper";
            cmd[Opcodes.GET_TEST_PATTERN                       ] = "r srce";
            cmd[Opcodes.GET_MODEL_CONFIG                       ] = "r deid";
            cmd[Opcodes.GET_FIRMWARE_REVISION                  ] = "r dfwv";
            cmd[Opcodes.GET_OCT_ANALOG_GAIN                    ] = "r pamp";
            cmd[Opcodes.GET_DETECTOR_GAIN                      ] = "r gain";
            cmd[Opcodes.GET_DETECTOR_OFFSET                    ] = "r offs";
            cmd[Opcodes.GET_COLLECTION_MODE                    ] = "r mode";
            cmd[Opcodes.GET_INVERT_X_AXIS                      ] = "r revr";

            cmd[Opcodes.SET_DETECTOR_GAIN                      ] = "tbd";
            cmd[Opcodes.SET_DETECTOR_OFFSET                    ] = "tbd";
            cmd[Opcodes.SET_INTEGRATION_TIME                   ] = "w tint";
            cmd[Opcodes.SET_LINE_PERIOD                        ] = "w tper";
            cmd[Opcodes.SET_TEST_PATTERN                       ] = "w srce";
            cmd[Opcodes.SET_OCT_ANALOG_GAIN                    ] = "w pamp";
            cmd[Opcodes.SET_DETECTOR_GAIN                      ] = "w gain";
            cmd[Opcodes.SET_DETECTOR_OFFSET                    ] = "w offs";
            cmd[Opcodes.SET_COLLECTION_MODE                    ] = "w mode";
            cmd[Opcodes.SET_INVERT_X_AXIS                      ] = "w revr";
        }
    }
}
