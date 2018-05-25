using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WasatchNET
{
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
    [ComVisible(true)]
    [Guid("1CA5AF48-05E6-4A91-96D2-0DF78CB9DCF7")]
    [ProgId("WasatchNET.Opcodes")]
    [ClassInterface(ClassInterfaceType.None)]
    public class OpcodeHelper : IOpcodeHelper
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
            cmd[Opcodes.GET_CF_SELECT                          ] = 0xec; // per Jason 4-Dec-2017
            cmd[Opcodes.GET_CODE_REVISION                      ] = 0xc0;
            cmd[Opcodes.GET_EXTERNAL_TRIGGER_OUTPUT            ] = 0xe1;
            cmd[Opcodes.GET_FPGA_REV                           ] = 0xb4;
            cmd[Opcodes.GET_HORIZ_BINNING                      ] = 0xbc;
            cmd[Opcodes.GET_INTEGRATION_TIME                   ] = 0xbf;
            cmd[Opcodes.GET_INTERLOCK                          ] = 0xef;
            cmd[Opcodes.GET_LASER_ENABLED                      ] = 0xe2;
            cmd[Opcodes.GET_LASER_MOD_DURATION                 ] = 0xc3;
            cmd[Opcodes.GET_LASER_MOD_ENABLED                  ] = 0xe3;
            cmd[Opcodes.GET_LASER_MOD_PULSE_WIDTH              ] = 0xdc;
            cmd[Opcodes.GET_LASER_RAMPING_MODE                 ] = 0xea;
            cmd[Opcodes.GET_LASER_TEMP                         ] = 0xd5;
            cmd[Opcodes.GET_LASER_TEMP_SETPOINT                ] = 0xe8;
            cmd[Opcodes.GET_LINK_LASER_MOD_TO_INTEGRATION_TIME ] = 0xde;
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
            cmd[Opcodes.SET_CCD_TEC_ENABLE                     ] = 0xd6;
            cmd[Opcodes.SET_CCD_TEMP_SETPOINT                  ] = 0xd8;
            cmd[Opcodes.SET_CCD_THRESHOLD_SENSING_MODE         ] = 0xce;
            cmd[Opcodes.SET_CCD_TRIGGER_SOURCE                 ] = 0xd2;
            cmd[Opcodes.SET_CF_SELECT                          ] = 0xeb; // per Jason 4-Dec-2017
            cmd[Opcodes.SET_DAC                                ] = 0xd8;
            cmd[Opcodes.SET_DFU_MODE                           ] = 0xfe; // (0x40, 0xFE, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0], 1000)
            cmd[Opcodes.SET_EXTERNAL_TRIGGER_OUTPUT            ] = 0xe0;
            cmd[Opcodes.SET_INTEGRATION_TIME                   ] = 0xb2;
            cmd[Opcodes.SET_LASER_ENABLED                      ] = 0xbe;
            cmd[Opcodes.SET_LASER_MOD_DURATION                 ] = 0xb9;
            cmd[Opcodes.SET_LASER_MOD_ENABLED                  ] = 0xbd;
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

            armInvertedRetvals.Add(Opcodes.ACQUIRE_CCD);
            armInvertedRetvals.Add(Opcodes.GET_CCD_OFFSET);
            armInvertedRetvals.Add(Opcodes.LINK_LASER_MOD_TO_INTEGRATION_TIME);
            armInvertedRetvals.Add(Opcodes.SELECT_LASER);
            armInvertedRetvals.Add(Opcodes.SET_CCD_GAIN);
            armInvertedRetvals.Add(Opcodes.SET_CCD_OFFSET);
            armInvertedRetvals.Add(Opcodes.SET_CCD_SENSING_THRESHOLD);
            armInvertedRetvals.Add(Opcodes.SET_CCD_TEC_ENABLE);
            armInvertedRetvals.Add(Opcodes.SET_CCD_TEMP_SETPOINT);
            armInvertedRetvals.Add(Opcodes.SET_CCD_THRESHOLD_SENSING_MODE);
            armInvertedRetvals.Add(Opcodes.SET_CCD_TRIGGER_SOURCE);
            armInvertedRetvals.Add(Opcodes.SET_DAC);
            armInvertedRetvals.Add(Opcodes.SET_EXTERNAL_TRIGGER_OUTPUT);
            armInvertedRetvals.Add(Opcodes.SET_INTEGRATION_TIME);
            armInvertedRetvals.Add(Opcodes.SET_LASER_ENABLED);
            armInvertedRetvals.Add(Opcodes.SET_LASER_MOD_ENABLED);
            armInvertedRetvals.Add(Opcodes.SET_LASER_MOD_DURATION);
            armInvertedRetvals.Add(Opcodes.SET_LASER_MOD_PULSE_WIDTH);
            armInvertedRetvals.Add(Opcodes.SET_LASER_TEMP_SETPOINT);
            armInvertedRetvals.Add(Opcodes.SET_MOD_PERIOD);
            armInvertedRetvals.Add(Opcodes.SET_MOD_PULSE_DELAY);
            armInvertedRetvals.Add(Opcodes.VR_SET_CONTINUOUS_CCD);
            armInvertedRetvals.Add(Opcodes.VR_SET_NUM_FRAMES);
        }
    }
}