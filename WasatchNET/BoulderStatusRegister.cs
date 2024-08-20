using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace WasatchNET
{
    ////////////////////////////////////////////////////////////////////////
    // Latest (16-Apr-2015)
    ////////////////////////////////////////////////////////////////////////
    //
    // Corresponding firmware code to populate the BoulderStatusRegister:
    //
    // *buf++= DetTemperatureMSB;
    // *buf++= DetTemperatureLSB; 
    // *buf++= DetTemperatureLLSB; 
    // *buf++= LDTemperatureMSB;  
    // *buf++= LDTemperatureLSB;   
    // *buf++= LDTemperatureLLSB;   
    // *buf++= ((LASER_ENA << 3) | (DETTEC_ENA << 2) | (LDTEC_ENA << 1) | (PWR_STATE)); 
    // *buf++= BatteryVoltageMSB;   
    // *buf++= BatteryVoltageLSB;  
    // *buf++= Trigger;           
    // *buf++= Keypad;           
    // *buf++= 0x00; (reserved)
    // *buf++= TECSetpointMSB;  
    // *buf++= TECSetpointLSB; 
    // *buf++= LaserPD_MSB;   
    // *buf++= LaserPD_LSB;  
    //  
    // Photodiode read back info: 
    //      ADC voltage = ((raw 16-bit hex value * 1.25)/ 2^16 ) = 4.75k * Pi (photodiode current)
    //
    // TEC Setpoint (Op Code 0x71): 
    //      Detector TEC Set point = =(R/(10000+R))*2^12
    //      Where R = -0.1867*(T^3)+25.767*(T^2)-1380.1*T+31286
    //      Where T is the desired set point in degrees C.
    //
    class BoulderStatusRegister
    {
        // 24-bit raw values from the register
        public UInt32 detectorTemperature24;    
        public UInt32 laserTemperature24;        

        // demarshalled 16-bit values
        public UInt16 detectorTemperature16;      
        public UInt16 laserTemperature16;          
        public UInt16 batteryVoltage;              
        public UInt16 detectorTECSetpoint16;
        public UInt16 laserPhotodiode16;

        // post-processed computed values
        public double detectorTemperatureDegC;
        public double laserTemperatureDegC;
        public double laserPhotodiodeCurrentAmps;

        // flags
        public bool remoteTrigger;
        public bool detectorTECEnabled;
        public bool laserTECEnabled;
        public bool laserIsFiring;
        public bool runningOnBatteries;

        // bitmasks
        byte powerState; // private to avoid confusion and force getters
        public byte keypadStatus;                 
        public byte spectrumCount;

        // internal
        static Mutex  mut = new Mutex();
        WPFLogger logger = WPFLogger.getInstance();

        public BoulderStatusRegister()
        {
        }

        public void update(byte[] response)
        {
            // 4/22/2015 10:27:34 AM LOG: [StatusBuffer] raw = 0x80 0x65 0xc0 0x80 0x69 0xc0 0x00 0x02 0x7c 0x00 0x00 0x00 0x8c 0x00 0x0d 0x7d 
            // 4/22/2015 10:27:35 AM LOG: [StatusBuffer] raw = 0x80 0x65 0xc0 0x9a 0x6a 0x40 0x02 0x02 0x7c 0x00 0x00 0x00 0x8c 0x00 0x0d 0x87 
            //                                                    0    1    2    3    4    5    6    7    8    9   10   11   12   13   14   15
            // string debug = "";
            // for (int i = 0; i < response.Length; i++)
            //     debug += String.Format("0x{0:x2} ", response[i]);
            // logger.log("[StatusBuffer] raw = {0}", debug);

            detectorTemperature24   = (UInt32)((response[0] << 16) | (response[1] << 8) | response[2]);
            laserTemperature24      = (UInt32)((response[3] << 16) | (response[4] << 8) | response[5]);
            powerState              = response[6];
            batteryVoltage          = (UInt16)((response[7] << 8) | response[8]);
            remoteTrigger           = response[9] != 0x00;
            keypadStatus            = response[10];
            spectrumCount           = response[11];
            detectorTECSetpoint16   = (UInt16)((response[12] << 8) | response[13]);
            laserPhotodiode16       = (UInt16)((response[14] << 8) | response[15]);

            // post-process temperature
            detectorTemperature16 = extractTemperature24to16bit(detectorTemperature24);
            laserTemperature16 = extractTemperature24to16bit(laserTemperature24);
            detectorTemperatureDegC = convertTemperature16ToDegreesC(detectorTemperature16);
            laserTemperatureDegC = convertTemperature16ToDegreesC(laserTemperature16);

            // post-process trigger as 8th button (physical keypad only has 7)
            if (remoteTrigger)
                keypadStatus |= 0x80;

            runningOnBatteries = ((powerState & 0x01) != 0);
            laserTECEnabled    = ((powerState & 0x02) != 0);
            detectorTECEnabled = ((powerState & 0x04) != 0);
            laserIsFiring      = ((powerState & 0x08) != 0);

            // post-process photodiode
            laserPhotodiodeCurrentAmps = ((laserPhotodiode16 * 1.25) / 65536) / 4750;

            // post-process detector TEC
            //      Detector TEC Set point = =(R/(10000+R))*2^12
            //      Where R = -0.1867*(T^3)+25.767*(T^2)-1380.1*T+31286
            //      Where T is the desired set point in degrees C.
        }

        public bool isDepressed(int key)
        {
            /*
            if (key >= Settings.NUMBER_OF_KEYPAD_BUTTONS)
                return false;
                */
            byte mask = 1;
            for (int i = 0; i < key; i++)
                mask <<= 1;

            bool depressed = (keypadStatus & mask) != 0;

            return depressed;
        }

        // explicit copy-constructor to be sure
        public BoulderStatusRegister(BoulderStatusRegister old)
        {
              detectorTemperature24 = old.detectorTemperature24;
                 laserTemperature24 = old.laserTemperature24;
              detectorTemperature16 = old.detectorTemperature16;
                 laserTemperature16 = old.laserTemperature16;
            detectorTemperatureDegC = old.detectorTemperatureDegC;
               laserTemperatureDegC = old.laserTemperatureDegC;
                         powerState = old.powerState;
                      remoteTrigger = old.remoteTrigger;
                     batteryVoltage = old.batteryVoltage;
                       keypadStatus = old.keypadStatus;
        }

        // The three-byte temperature readings are defined as follows:
        //  
        // D23 = Sign bit (This should always be 1)
        // D22 = Overflow (A 1 indicates that the ADC is maxed out – this shouldn’t ever happen)
        // D21…D6 = 16-bit ADC Value
        // D5…D0 = Ignore
        // +--- Sign
        // |+-- Overflow
        // ||+- msb    +- lsb    +- Ignore
        // SOMM MMMM MMLL LLLL LLII IIII
        // ---- ----+---- ----+---- ----
        // 7654 3210|7654 3210|7654 3210
        // \__MSB__/ \__LSB__/ \_LLSB__/
        UInt16 extractTemperature24to16bit(UInt32 raw)
        {
            byte msb = (byte)((raw >> 16) & 0xff);
            byte lsb = (byte)((raw >>  8) & 0xff);
            byte llsb = (byte)((raw) & 0xff);

            byte sign_bit = (byte)(msb & 0x80);
            byte overflow = (byte)(msb & 0x40);

            // could be simpler against 'raw' but whatever...
            byte newMSB = (byte)(((byte)(msb & 0x3f) << 2) | ((byte)( lsb & 0xc0) >> 6));
            byte newLSB = (byte)(((byte)(lsb & 0x3f) << 2) | ((byte)(llsb & 0xc0) >> 6));

            UInt16 retval = (UInt16)((newMSB << 8) | newLSB);
            return retval;
        }

        // this determines whether the screen should re-brighten from a dimmed state
        public bool same(object obj)
        {
            BoulderStatusRegister rhs = obj as BoulderStatusRegister;
            return powerState == rhs.powerState
                && keypadStatus == rhs.keypadStatus
                && remoteTrigger == rhs.remoteTrigger;
        }

        static public double convertTemperature16ToDegreesC(uint adc)
        {
            double ohms = ((adc * 1.0 / 65536) * 33000.0 / 2.0) / (2.5 - (adc / 65536.0) * 3.3 / 2.0);
            double lnOhms = Math.Log(ohms);
            double degC = 1.0 / (1.13e-3 + 2.34e-4 * lnOhms + 8.78e-8 * (lnOhms * lnOhms * lnOhms)) - 273.15;
            return degC;
        }
    }
}
