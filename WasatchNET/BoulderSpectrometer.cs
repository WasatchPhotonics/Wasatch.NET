using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace WasatchNET
{
    public class BoulderSpectrometer : Spectrometer
    {
        public const byte OP_TECENABLE = 0x70;
        public const byte OP_SETDETSETPOINT = 0x71;
        public const byte OP_STATUS = 0xfe;

        public const byte txEndpoint = 0x01;
        public const byte rxEndpoint = 0x81;

        //internal Wrapper wrapper;
        //internal SeaBreezeWrapper wrapper;
        internal int specIndex;
        BoulderStatusRegister status = new BoulderStatusRegister();
        BoulderStatusRegister lastStatus;

        /// <summary>
        /// Project Boulder is an OEM spectrometer with customer-supplied electronics 
        /// using Ocean Optics-derived firmware interface, hence SeaBreeze communications
        /// </summary>
        internal BoulderSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            excitationWavelengthNM = 0;
            triggerSource = TRIGGER_SOURCE.EXTERNAL;
            specIndex = index;
            int errorReader = 0;
            lock (acquisitionLock)
            {
                integrationTime_ = SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(specIndex, ref errorReader) / 1000;
            }
            if (errorReader != 0)
                integrationTime_ = 8;
        }

        override internal bool open()
        {
            eeprom = new BoulderEEPROM(this);

            int errorReader = 0;

            lock (acquisitionLock)
            {
                if (SeaBreezeWrapper.seabreeze_open_spectrometer(specIndex, ref errorReader) == 0)
                {
                    pixels = (uint)SeaBreezeWrapper.seabreeze_get_formatted_spectrum_length(specIndex, ref errorReader);

                    logger.info("found spectrometer with {0} pixels", pixels);

                    if (!eeprom.read())
                    {
                        logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                        //wrapper.shutdown();
                        close();
                        return false;
                    }
                    logger.debug("back from reading EEPROM");

                    regenerateWavelengths();
                    //detectorTECSetpointDegC = 15.0f;

                    logger.info("Opened Ocean Spectrometer with index {0}", specIndex);

                    return true;
                }

                else
                {
                    logger.debug("Unable to open Ocean spectrometer with index {0}", specIndex);
                    return false;
                }
            }

        }

        public override void close()
        {
            //wrapper.shutdown();
            int errorReader = 0;
            lock (acquisitionLock)
            {
                SeaBreezeWrapper.seabreeze_close_spectrometer(specIndex, ref errorReader);
            }
        }

        public bool updateStatus()
        {
            byte[] request = new byte[1];
            request[0] = OP_STATUS;
            
            byte[] response;
            bool ok = false;

            lock (acquisitionLock)
            {
                ok = sbWrite(request, false);
                if (!ok)
                {
                    //wrapper.setError("Error requesting status register update");
                    //mut.ReleaseMutex();
                    //statusMut.ReleaseMutex();
                    logger.info("updateStatus: failed");
                    return false;
                }

                response = sbRead(16, false);
            }

            if (response == null || response.Length == 0)
                if (!ok)
                {
                    logger.info("updateStatus: failed");
                    return false;
                }

            // logger.info("updateStatus: updating from response");
            status.update(response);

            // did anything significant change? this will include any button-presses, hence user behavior
            if (lastStatus == null || !lastStatus.same(status))
            {
                logger.info("CCA status: ({0})", DateTime.Now);
                logger.info("  detectorTemperature = 0x{0:x6} -> 0x{1:x4} -> {2:f2} deg C", status.detectorTemperature24, status.detectorTemperature16, status.detectorTemperatureDegC);
                logger.info("  laserTemperature    = 0x{0:x6} -> 0x{1:x4} -> {2:f2} deg C", status.laserTemperature24, status.laserTemperature16, status.laserTemperatureDegC);
                logger.info("  runningOnBatteries  = {0}", status.runningOnBatteries);
                logger.info("  laserTECEnabled     = {0}", status.laserTECEnabled);
                logger.info("  detectorTECEnabled  = {0}", status.detectorTECEnabled);
                logger.info("  batteryVoltage      = 0x{0:x4}", status.batteryVoltage);
                logger.info("  spectrumCount       = {0}", status.spectrumCount);
                
                lastStatus = new BoulderStatusRegister(status);

                // obviously something just happened, so brighten the display unless forced otherwise
                //userOperation();
            }
            
            return true;
        }

        private bool sbWrite(byte[] data, bool log = true)
        {
            if (log)
            {
                string debug = "";
                for (int i = 0; i < data.Length; i++)
                    debug += String.Format(" 0x{0:x2}", data[i]);
                logger.info(">> {0}", debug);
            }

            int errorCode = 0;
            SeaBreezeWrapper.seabreeze_write_usb(specIndex, ref errorCode, txEndpoint, ref data[0], data.Length);

            if (errorCode != 0)
            {
                return false;
            }
            return true;
        }

        private byte[] sbRead(int bytes, bool log = true)
        {
            byte[] data = new byte[bytes];

            int errorCode = 0;
            SeaBreezeWrapper.seabreeze_read_usb(specIndex, ref errorCode, rxEndpoint, ref data[0], data.Length);

            if (errorCode != 0)
            {
                return null;
            }

            if (log)
            {
                string debug = "";
                for (int i = 0; i < data.Length; i++)
                    debug += String.Format(" 0x{0:x2}", data[i]);
                logger.info("<< {0}", debug);
            }

            return data;
        }

        public void setDetectorTECSetpointDegreesC(double deg)
        {
            // TEC Setpoint (Op Code 0x71): (NEW)
            //      Detector TEC Set point = =(R/(10000+R))*2^12
            //      Where R = -0.1867*(T^3)+25.767*(T^2)-1380.1*T+31286
            //      Where T is the desired set point in degrees C.
            double R = -0.1867 * deg * deg * deg
                     + 25.767 * deg * deg
                     - 1380.1 * deg
                     + 31286;
            uint setpoint = (uint)(R / (10000 + R) * 4096);

            logger.info("[setDetectorTECSetpointDegreesC] {0:f2} degC => R {1:f4} => setpoint 0x{2:x4}", deg, R, setpoint);

            byte[] cmd = new byte[3];
            cmd[0] = OP_SETDETSETPOINT;
            cmd[1] = (byte)((setpoint >> 8) & 0xff);
            cmd[2] = (byte)(setpoint & 0xff);

            lock (acquisitionLock)
            {
                sbWrite(cmd);
            }
        }

        public bool getDetectorTECEnabled()
        {
            if (status == null)
                return false;
            return status.detectorTECEnabled;
        }

        public bool getLaserTECEnabled()
        {
            return status.laserTECEnabled;
        }

        public bool enableDetectorTEC(bool flag)
        {
            logger.info("enableDetectorTEC: asked to set Detector TEC {0}", flag ? "ON" : "off");
            return enableTEC(flag, getLaserTECEnabled());
        }

        // change just the laser TEC state
        public bool enableLaserTEC(bool flag)
        {
            logger.info("enableLaserTEC: asked to set Laser TEC {0}", flag ? "ON" : "off");
            return enableTEC(getDetectorTECEnabled(), flag);
        }

        // change both TEC states
        bool enableTEC(bool detectorFlag, bool laserFlag)
        {
            bool result = false;

            logger.info("enableTEC: setting detector TEC {0}, laser TEC {1}", detectorFlag ? "ON" : "off", laserFlag ? "ON" : "off");

            byte[] cmd = new byte[3];
            cmd[0] = OP_TECENABLE;
            cmd[1] = (byte)(detectorFlag ? 1 : 0);
            cmd[2] = (byte)(laserFlag ? 1 : 0);

            lock (acquisitionLock)
            {
                result = sbWrite(cmd);
            }
            
            if (!result)
            {
                return false;
            }

            return true;
        }

        public override double[] getSpectrum(bool forceNew = false)
        {
            lock (acquisitionLock)
            {
                double[] sum = getSpectrumRaw();
                if (sum == null)
                {
                    logger.error("getSpectrum: getSpectrumRaw returned null");
                    return null;
                }
                logger.debug("getSpectrum: received {0} pixels", sum.Length);

                if (scanAveraging_ > 1)
                {
                    // logger.debug("getSpectrum: getting additional spectra for averaging");
                    for (uint i = 1; i < scanAveraging_; i++)
                    {
                        double[] tmp = getSpectrumRaw();
                        if (tmp == null)
                            return null;

                        for (int px = 0; px < pixels; px++)
                            sum[px] += tmp[px];
                    }

                    for (int px = 0; px < pixels; px++)
                        sum[px] /= scanAveraging_;
                }

                if (dark != null && dark.Length == sum.Length)
                    for (int px = 0; px < pixels; px++)
                        sum[px] -= dark_[px];

                if (boxcarHalfWidth > 0)
                {
                    // logger.debug("getSpectrum: returning boxcar");
                    return Util.applyBoxcar(boxcarHalfWidth, sum);
                }
                else
                {
                    // logger.debug("getSpectrum: returning sum");
                    return sum;
                }
            }
        }

        protected override double[] getSpectrumRaw(bool skipTrigger=false)
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            double[] spec = new double[pixels]; // default to all zeros
            int errorReader = 0;

            SeaBreezeWrapper.seabreeze_get_formatted_spectrum(specIndex, ref errorReader, ref spec[0], (int)pixels);

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
            return spec;
        }

        public override bool laserEnabled
        {
            get { return false; }
            set { laserEnabled_ = value; }
        }

        public override uint integrationTimeMS
        {
            get
            {
                //return (uint)wrapper.getIntegrationTimeMillisec();
                return (uint)integrationTime_;
            }
            set
            {
                //if (value < wrapper.getMaxIntegrationTimeMillisec())
                //wrapper.setIntegrationTimeMillisec((long)value);
                lock (acquisitionLock)
                {
                    int errorReader = 0;
                    SeaBreezeWrapper.seabreeze_set_integration_time_microsec(specIndex, ref errorReader, (long)(value * 1000));
                    if (errorReader == 0)
                        integrationTime_ = value;
                    
                }
            }
        }
        long integrationTime_;

        public override bool hasLaser
        {
            get => false;
        }

        public override bool setLaserPowerPercentage(float perc)
        {
            return false;
        }

        public override LaserPowerResolution laserPowerResolution
        {
            get
            {
                return LaserPowerResolution.LASER_POWER_RESOLUTION_MANUAL;
            }
        }

        public override bool laserInterlockEnabled { get => false; }

        public override UInt64 laserModulationPeriod { get => 100; }

        public override ulong laserModulationPulseWidth { get => 0; set { } }

        public override float detectorGain { get => 0; }

        public override float detectorGainOdd { get => 0; }

        public override short detectorOffset { get => 0; }

        public override short detectorOffsetOdd { get => 0; }

        public override bool isARM => false;

        public override TRIGGER_SOURCE triggerSource
        {
            get => TRIGGER_SOURCE.EXTERNAL;
            set
            {

            }
        }

        public override float laserTemperatureDegC { get => 0; }

        public override ushort laserTemperatureRaw { get => 0; }

        public override byte laserTemperatureSetpointRaw { get => 0; }

        public override float batteryPercentage
        {
            get
            {
                return 0.0f; 
            }
        }

        public override bool batteryCharging { get => false; }

        public override bool detectorTECEnabled
        {
            get
            {
                return tecEnabled_;
                //return wrapper.getSpectrometerTECEnabled();
            }
            set
            {
                //wrapper.setSpectrometerTECEnabled(value);
                /*int errorReader = 0;
                if (value)
                    SeaBreezeWrapper.seabreeze_set_tec_enable(specIndex, ref errorReader, 1);
                else
                    SeaBreezeWrapper.seabreeze_set_tec_enable(specIndex, ref errorReader, 0);
                if (errorReader == 0)
                    tecEnabled_ = value;*/
                bool ok = enableDetectorTEC(value);
                if (ok)
                    tecEnabled_ = value;
            }
        }
        bool tecEnabled_ = false;

        public override float detectorTemperatureDegC
        {
            get
            {
                bool readSuccess = updateStatus();
                if (readSuccess)
                    return (float)status.detectorTemperatureDegC;
                else
                    return 0;
                //int errorReader = 0;
                //return 0;
                //return (float)SeaBreezeWrapper.seabreeze_read_tec_temperature(specIndex, ref errorReader);
            }
        }

        public override ushort detectorTECSetpointRaw
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        public override float detectorTECSetpointDegC
        {
            get => base.detectorTECSetpointDegC;
            set
            {
                if (value != detectorTECSetpointDegC_)
                    setDetectorTECSetpointDegreesC(value);
                detectorTECSetpointDegC_ = value;
                
            }
            
        }

        public override ushort secondaryADC
        {
            get
            {
                return 0;
            }
        }

        public override string firmwareRevision
        {
            get
            {
                string retval = null;

                lock (acquisitionLock)
                {
                    try
                    {
                        byte[] raw = new byte[32];
                        int error = 0;

                        SeaBreezeWrapper.seabreeze_get_usb_descriptor_string(specIndex, ref error, 1, ref raw[0], raw.Length);

                        if (error == 0)
                        {
                            int len = 0;
                            while (raw[len] != 0 && len + 1 < raw.Length)
                                len++;

                            byte[] cleanByte = Encoding.Convert(Encoding.GetEncoding("iso-8859-1"), Encoding.UTF8, raw);
                            string text = Encoding.UTF8.GetString(cleanByte, 0, len);
                            const string pattern = @"\b(\d+\.\d+\.\d+)\b";
                            Regex regEx = new Regex(pattern);
                            MatchCollection matches = regEx.Matches(text);
                            if (matches.Count > 0)
                                retval =  matches[0].Groups[0].Value;
                            else
                                retval = text;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.error("Error getting FX2 firmware version: {0}", e.Message);
                    }
                }
                
                return retval;
            }
        }

        public override string fpgaRevision
        {
            get
            {
                lock (acquisitionLock)
                {
                    byte[] cmd = new byte[2];
                    cmd[0] = 0x6b; // read FPGA register
                    cmd[1] = 0x04; // read FPGA version number

                    sbWrite(cmd);
                    byte[] response = sbRead(3);

                    UInt16 bytes = (UInt16)((response[2] << 8) | response[1]);

                    int major = (bytes >> 12) & 0x0f;
                    int minor = (bytes >> 4) & 0xff;
                    int build = (bytes) & 0x0f;

                    string formatted = String.Format("{0:x1}.{1:x2}.{2:x1}", major, minor, build);
                    logger.debug("converted raw FPGA version {0:x4} to {1}", bytes, formatted);
                    
                    return formatted;
                }
            }
        }

    public override float excitationWavelengthNM
        {
            get
            {
                return eeprom.laserExcitationWavelengthNMFloat;
            }
            set
            {

            }
        }

    }
}
