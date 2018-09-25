using System.Collections.Generic;

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
            cmd[Opcodes.GET_ACTUAL_FRAMES                      ] = 0xe4;
            cmd[Opcodes.GET_ACTUAL_INTEGRATION_TIME            ] = 0xdf;
            cmd[Opcodes.GET_DETECTOR_GAIN                      ] = 0xc5;
            cmd[Opcodes.GET_DETECTOR_OFFSET                    ] = 0xc4;
            cmd[Opcodes.GET_DETECTOR_SENSING_THRESHOLD         ] = 0xd1;
            cmd[Opcodes.GET_DETECTOR_SENSING_THRESHOLD_ENABLE  ] = 0xcf;
            cmd[Opcodes.GET_DETECTOR_TEMPERATURE               ] = 0xd7;
            cmd[Opcodes.GET_DETECTOR_TEC_ENABLE                ] = 0xda;
            cmd[Opcodes.GET_DETECTOR_TEC_SETPOINT              ] = 0xd9;
            cmd[Opcodes.GET_TRIGGER_SOURCE                     ] = 0xd3;
            cmd[Opcodes.GET_CF_SELECT                          ] = 0xec;
            cmd[Opcodes.GET_FIRMWARE_REVISION                  ] = 0xc0;
            cmd[Opcodes.GET_TRIGGER_OUTPUT                     ] = 0xe1;
            cmd[Opcodes.GET_FPGA_REVISION                      ] = 0xb4;
            cmd[Opcodes.GET_HORIZONTAL_BINNING                 ] = 0xbc;
            cmd[Opcodes.GET_INTEGRATION_TIME                   ] = 0xbf;
            cmd[Opcodes.GET_LASER_INTERLOCK                    ] = 0xef;
            cmd[Opcodes.GET_LASER_ENABLE                       ] = 0xe2;
            cmd[Opcodes.GET_LASER_MOD_DURATION                 ] = 0xc3;
            cmd[Opcodes.GET_LASER_MOD_ENABLE                   ] = 0xe3;
            cmd[Opcodes.GET_LASER_MOD_PULSE_WIDTH              ] = 0xdc;
            cmd[Opcodes.GET_LASER_RAMPING_MODE                 ] = 0xea;
            cmd[Opcodes.GET_LASER_TEMPERATURE                  ] = 0xd5;
            cmd[Opcodes.GET_LASER_TEC_SETPOINT                 ] = 0xe8;
            cmd[Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME ] = 0xde;
            cmd[Opcodes.GET_LASER_MOD_PERIOD                   ] = 0xcb;
            cmd[Opcodes.GET_LASER_MOD_PULSE_DELAY              ] = 0xca;
            cmd[Opcodes.GET_SELECTED_ADC                       ] = 0xee;
            cmd[Opcodes.GET_TRIGGER_DELAY                      ] = 0xab;
            cmd[Opcodes.SET_LINK_LASER_MOD_TO_INTEGRATION_TIME ] = 0xdd;
            cmd[Opcodes.SET_LINK_LASER_MOD_TO_INTEGRATION_TIME ] = 0xde;
            cmd[Opcodes.POLL_DATA                              ] = 0xd4;
            cmd[Opcodes.SECOND_TIER_COMMAND                    ] = 0xff;
            cmd[Opcodes.SET_HORIZONTAL_BINNING                 ] = 0xb8;
            cmd[Opcodes.SET_SELECTED_ADC                       ] = 0xed;
            cmd[Opcodes.SET_DETECTOR_GAIN                      ] = 0xb7;
            cmd[Opcodes.SET_DETECTOR_OFFSET                    ] = 0xb6;
            cmd[Opcodes.SET_DETECTOR_SENSING_THRESHOLD         ] = 0xd0;
            cmd[Opcodes.SET_DETECTOR_SENSING_THRESHOLD_ENABLE  ] = 0xce;
            cmd[Opcodes.SET_DETECTOR_TEC_ENABLE                ] = 0xd6;
            cmd[Opcodes.SET_DETECTOR_TEC_SETPOINT              ] = 0xd8;
            cmd[Opcodes.SET_TRIGGER_SOURCE                     ] = 0xd2;
            cmd[Opcodes.SET_CF_SELECT                          ] = 0xeb;
            cmd[Opcodes.SET_DFU_MODE                           ] = 0xfe;
            cmd[Opcodes.SET_TRIGGER_OUTPUT                     ] = 0xe0;
            cmd[Opcodes.SET_INTEGRATION_TIME                   ] = 0xb2;
            cmd[Opcodes.SET_LASER_ENABLE                       ] = 0xbe;
            cmd[Opcodes.SET_LASER_MOD_DURATION                 ] = 0xb9;
            cmd[Opcodes.SET_LASER_MOD_ENABLE                   ] = 0xbd;
            cmd[Opcodes.SET_LASER_MOD_PULSE_WIDTH              ] = 0xdb;
            cmd[Opcodes.SET_LASER_RAMPING_MODE                 ] = 0xe9;
            cmd[Opcodes.SET_LASER_TEC_SETPOINT                 ] = 0xe7;
            cmd[Opcodes.SET_LASER_MOD_PERIOD                   ] = 0xc7;
            cmd[Opcodes.SET_LASER_MOD_PULSE_DELAY              ] = 0xc6;
            cmd[Opcodes.SET_MODEL_CONFIG_REAL                  ] = 0xa2; 
            cmd[Opcodes.SET_TRIGGER_DELAY                      ] = 0xaa;
            cmd[Opcodes.GET_CONTINUOUS_ACQUISITION             ] = 0xcc;
            cmd[Opcodes.GET_CONTINUOUS_FRAMES                  ] = 0xcd;
            cmd[Opcodes.SET_CONTINUOUS_ACQUISITION             ] = 0xc8;
            cmd[Opcodes.SET_CONTINUOUS_FRAMES                  ] = 0xc9;

            // wValue for SECOND_TIER_COMMAND
            cmd[Opcodes.GET_MODEL_CONFIG                       ] = 0x01;
            cmd[Opcodes.SET_MODEL_CONFIG_DO_NOT_USE            ] = 0x02; // DO NOT USE (rdickerson reported works)
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

            // this list has not been double-checked; ENLIGHTEN ignores return values :-(
            armInvertedRetvals.Add(Opcodes.ACQUIRE_SPECTRUM);
            armInvertedRetvals.Add(Opcodes.GET_DETECTOR_OFFSET);
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
        }
    }
}