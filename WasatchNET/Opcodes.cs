namespace WasatchNET
{
    public class Opcodes
    {
        // communication direction
        public const byte HOST_TO_DEVICE = 0x40;
        public const byte DEVICE_TO_HOST = 0xc0;

        // bRequest commands
        public const byte ACQUIRE_CCD                              = 0xad; // impl
        public const byte GET_ACTUAL_FRAMES                        = 0xe4; // impl
        public const byte GET_ACTUAL_INTEGRATION_TIME              = 0xdf;
        public const byte GET_CCD_GAIN                             = 0xc5;
        public const byte GET_CCD_OFFSET                           = 0xc4;
        public const byte GET_CCD_SENSING_THRESHOLD                = 0xd1;
        public const byte GET_CCD_TEMP                             = 0xd7; // impl
        public const byte GET_CCD_TEMP_ENABLE                      = 0xda;
        public const byte GET_CCD_TEMP_SETPOINT                    = 0xd9; // alias of GET_DAC
        public const byte GET_CCD_THRESHOLD_SENSING_MODE           = 0xcf;
        public const byte GET_CCD_TRIGGER_SOURCE                   = 0xd3;
        public const byte GET_CODE_REVISION                        = 0xc0; // impl
        public const byte GET_DAC                                  = 0xd9; // alias of GET_CCD_TEMP_SETPOINT
        public const byte GET_EXTERNAL_TRIGGER_OUTPUT              = 0xe1;
        public const byte GET_FPGA_REV                             = 0xb4; // impl
        public const byte GET_HORIZ_BINNING                        = 0xbc;
        public const byte GET_INTEGRATION_TIME                     = 0xbf; // impl
        public const byte GET_INTERLOCK                            = 0xef;
        public const byte GET_LASER                                = 0xe2;
        public const byte GET_LASER_MOD                            = 0xe3;
        public const byte GET_LASER_MOD_PULSE_WIDTH                = 0xdc;
        public const byte GET_LASER_RAMPING_MODE                   = 0xea;
        public const byte GET_LASER_TEMP                           = 0xd5; // impl
        public const byte GET_LASER_TEMP_SETPOINT                  = 0xe8;
        public const byte GET_LINK_LASER_MOD_TO_INTEGRATION_TIME   = 0xde;
        public const byte GET_MOD_DURATION                         = 0xc3;
        public const byte GET_MOD_PERIOD                           = 0xcb;
        public const byte GET_MOD_PULSE_DELAY                      = 0xca;
        public const byte GET_SELECTED_LASER                       = 0xee;
        public const byte GET_TRIGGER_DELAY                        = 0xab;
        public const byte LINK_LASER_MOD_TO_INTEGRATION_TIME       = 0xdd;
        public const byte POLL_DATA                                = 0xd4; // impl
        public const byte SECOND_TIER_COMMAND                      = 0xff; // impl
        public const byte SELECT_HORIZ_BINNING                     = 0xb8;
        public const byte SELECT_LASER                             = 0xed;
        public const byte SET_CCD_GAIN                             = 0xb7;
        public const byte SET_CCD_OFFSET                           = 0xb6;
        public const byte SET_CCD_SENSING_THRESHOLD                = 0xd0;
        public const byte SET_CCD_TEMP_ENABLE                      = 0xd6; // impl
        public const byte SET_CCD_TEMP_SETPOINT                    = 0xd8; // alias of SET_DAC
        public const byte SET_CCD_THRESHOLD_SENSING_MODE           = 0xce;
        public const byte SET_CCD_TRIGGER_SOURCE                   = 0xd2; // impl
        public const byte SET_DAC                                  = 0xd8; // alias of SET_CCD_TEMP_SETPOINT
        public const byte SET_EXTERNAL_TRIGGER_OUTPUT              = 0xe0;
        public const byte SET_INTEGRATION_TIME                     = 0xb2; // impl
        public const byte SET_LASER                                = 0xbe; // impl
        public const byte SET_LASER_MOD                            = 0xbd; // impl
        public const byte SET_LASER_MOD_DUR                        = 0xb9;
        public const byte SET_LASER_MOD_PULSE_WIDTH                = 0xdb;
        public const byte SET_LASER_RAMPING_MODE                   = 0xe9;
        public const byte SET_LASER_TEMP_SETPOINT                  = 0xe7;
        public const byte SET_MOD_PERIOD                           = 0xc7;
        public const byte SET_MOD_PULSE_DELAY                      = 0xc6;
        public const byte SET_TRIGGER_DELAY                        = 0xaa;
        public const byte VR_ENABLE_CCD_TEMP_CONTROL               = 0xd6;
        public const byte VR_GET_CCD_TEMP_CONTROL                  = 0xda;
        public const byte VR_GET_CONTINUOUS_CCD                    = 0xcc;
        public const byte VR_GET_LASER_TEMP                        = 0xd5;
        public const byte VR_GET_NUM_FRAMES                        = 0xcd;
        public const byte VR_READ_CCD_TEMPERATURE                  = 0xd7;
        public const byte VR_SET_CONTINUOUS_CCD                    = 0xc8;
        public const byte VR_SET_NUM_FRAMES                        = 0xc9;

        // wValue for SECOND_TIER_COMMAND
        public const byte GET_MODEL_CONFIG                         = 0x01; // impl
        public const byte SET_MODEL_CONFIG                         = 0x02;
        public const byte GET_LINE_LENGTH                          = 0x03; // impl
        public const byte READ_COMPILATION_OPTIONS                 = 0x04; // impl
        public const byte OPT_INT_TIME_RES                         = 0x05; // impl
        public const byte OPT_DATA_HDR_TAB                         = 0x06; // impl
        public const byte OPT_CF_SELECT                            = 0x07; // impl
        public const byte OPT_LASER                                = 0x08; // impl
        public const byte OPT_LASER_CONTROL                        = 0x09; // impl
        public const byte OPT_AREA_SCAN                            = 0x0a; // impl
        public const byte OPT_ACT_INT_TIME                         = 0x0b; // impl
        public const byte OPT_HORIZONTAL_BINNING                   = 0x0c; // ipml
    }
}
