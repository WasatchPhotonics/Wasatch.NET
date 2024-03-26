using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class BoulderSpectrometer : Spectrometer
    {
        public const byte OP_TECENABLE = 0x70;
        public const byte OP_SETDETSETPOINT = 0x71;
        public const byte OP_STATUS = 0xfe;

        public const byte txEndpoint = 0x01;
        public const byte rxEndpoint = 0x81;

        public bool commError = false;
        public int nonSpectrumTimeoutMS = 20000;
        public bool correctPixelsMarkedBad = false;

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
            prioritizeVirtualEEPROM = true;
            specIndex = index;

            if (!commError)
            {
                lock(acquisitionLock)
                {
                    var task = Task.Run(async() => integrationTime_ = await getIntegrationTimeAsync());
                    task.Wait();
                }
            }

        }

        protected async Task<long> getIntegrationTimeAsync()
        {
            var task = launchSBGetIntegrationAsync();
            int timeout = nonSpectrumTimeoutMS;
            long integrationTime = 8;

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                integrationTime = task.Result;
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
            }

            return integrationTime;
        }

        protected async Task<long> launchSBGetIntegrationAsync()
        {
            int errorReader = 0;
            long integrationTime = 8;

            integrationTime = await Task.Run(() => SeaBreezeWrapper.seabreeze_get_min_integration_time_microsec(specIndex, ref errorReader)) / 1000;
            
            if (errorReader != 0)
                integrationTime = 8;

            return integrationTime;

        }

        override internal bool open()
        {
            Task<bool> task = Task.Run(async () => await openAsync());
            return task.Result;
        }

        override internal async Task<bool> openAsync()
        {
            eeprom = new BoulderEEPROM(this);

            if (!commError)
            {
                bool openSucceeded = false;
                var task = Task.Run(async () => openSucceeded = await openSpectrometerAsync());
                task.Wait();
                    
                if (openSucceeded)
                {
                    if (!commError)
                    {
                        var pixelTask = Task.Run(async () => pixels = (uint) await getPixelAsync());
                        pixelTask.Wait();
                    }
                    logger.info("found spectrometer with {0} pixels", pixels);

                    if (!(await eeprom.readAsync()))
                    {
                        logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                        //wrapper.shutdown();
                        close();
                        return false;
                    }
                    logger.debug("back from reading EEPROM");

                    regenerateWavelengths();
                    //detectorTECSetpointDegC = 15.0f;

                    logger.info("Opened SeaBreeze Spectrometer with index {0}", specIndex);

                    return true;
                }

                else
                {
                    logger.debug("Unable to open SeaBreeze spectrometer with index {0}", specIndex);
                    return false;
                }
            }

            else
            {
                logger.debug("Unable to open SeaBreeze spectrometer with index {0}", specIndex);
                return false;
            }
            

        }

        protected async Task<bool> openSpectrometerAsync()
        {
            var task = launchSBOpenSpecAsync();
            int timeout = nonSpectrumTimeoutMS * 3;
            bool result = false;

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                result = task.Result;
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
                result = false;
            }

            return result;
        }

        protected async Task<bool> launchSBOpenSpecAsync()
        {
            int errorReader = 0;
            bool retVal = await Task.Run(() => SeaBreezeWrapper.seabreeze_open_spectrometer(specIndex, ref errorReader) == 0);
            return retVal;
        }

        protected async Task<int> getPixelAsync()
        {
            var task = launchSBGetPixelAsync();
            int timeout = nonSpectrumTimeoutMS;
            int pixels = 0;

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                pixels = task.Result;
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
            }

            return pixels;
        }

        protected async Task<int> launchSBGetPixelAsync()
        {
            int errorReader = 0;
            int pixels = await Task.Run(() => SeaBreezeWrapper.seabreeze_get_formatted_spectrum_length(specIndex, ref errorReader));

            return pixels;
        }


        public override void close()
        {
            Task task = Task.Run(async () => await closeAsync());
            task.Wait();
        }

        public async override Task closeAsync()
        {
            //wrapper.shutdown();
            if (!commError)
            {
                await closeSpectrometerAsync();
            }
        }

        protected async Task<bool> closeSpectrometerAsync()
        {
            var task = launchSBCloseSpecAsync();
            int timeout = nonSpectrumTimeoutMS * 3;

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                return true;
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
                return false;
            }

        }

        protected async Task launchSBCloseSpecAsync()
        {
            int errorReader = 0;
            await Task.Run(() => SeaBreezeWrapper.seabreeze_close_spectrometer(specIndex, ref errorReader));
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

            
            if (!commError)
            {
                bool result = false;
                var task = Task.Run(async () => result = await sbWriteAsync(data));
                task.Wait();

                return result;
            }
            else
                return false;

        }

        protected async Task<bool> sbWriteAsync(byte[] data)
        {
            var task = launchSBWriteAsync(data);
            int timeout = nonSpectrumTimeoutMS;
            bool result = false;

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                result = task.Result;
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
                result = false;
            }

            return result;
        }

        protected async Task<bool> launchSBWriteAsync(byte[] data)
        {
            int errorCode = 0;
            await Task.Run(() => SeaBreezeWrapper.seabreeze_write_usb(specIndex, ref errorCode, txEndpoint, ref data[0], data.Length));

            if (errorCode != 0)
            {
                return false;
            }
            return true;
        }


        private byte[] sbRead(int bytes, bool log = true)
        {
            byte[] data = null;

            if (!commError)
            {
                var task = Task.Run(async () => data = await sbReadAsync(bytes));
                task.Wait();
            }
            if (log && data != null)
            {
                string debug = "";
                for (int i = 0; i < data.Length; i++)
                    debug += String.Format(" 0x{0:x2}", data[i]);
                logger.info("<< {0}", debug);
            }

            return data;
        }

        protected async Task<byte[]> sbReadAsync(int size)
        {
            var task = launchSBReadAsync(size);
            int timeout = nonSpectrumTimeoutMS;
            byte[] bytes = null;

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                bytes = task.Result;
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
                bytes = null;
            }

            return bytes;
        }

        protected async Task<byte[]> launchSBReadAsync(int bytes)
        {
            byte[] data = new byte[bytes];

            int errorCode = 0;
            await Task.Run(() => SeaBreezeWrapper.seabreeze_read_usb(specIndex, ref errorCode, rxEndpoint, ref data[0], data.Length));

            if (errorCode != 0)
            {
                return null;
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
            if (!commError)
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

                    if (correctPixelsMarkedBad)
                        correctBadPixels(ref sum);

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
            else
                return new double[pixels];

        }

        protected override double[] getSpectrumRaw(bool skipTrigger = false)
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            double[] spec = new double[pixels]; // default to all zeros

            var task = Task.Run(async () => spec = await getSpectrumAsync());
            task.Wait();

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
            return spec;
        }


        public override async Task<double[]> getSpectrumAsync(bool forceNew = false)
        {
            if (!commError)
            {
                double[] sum = await getSpectrumRawAsync();
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
                        double[] tmp = await getSpectrumRawAsync();
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

                if (correctPixelsMarkedBad)
                    correctBadPixels(ref sum);

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
            else
                return new double[pixels];

        }


        protected override async Task<double[]> getSpectrumRawAsync(bool skipTrigger=false)
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            double[] spec = new double[pixels]; // default to all zeros
            spec = await getSpectrumAsync();

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
            return spec;
        }


        protected async Task<double[]> getSpectrumAsync()
        {
            var task = launchSBSpectrumAsync();
            int timeout = Math.Max( (int)integrationTimeMS * 3, nonSpectrumTimeoutMS);
            double[] spec = new double[pixels];

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                spec = task.Result;
                logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
            }

            return spec;
        }

        protected async Task<double[]> launchSBSpectrumAsync()
        {
            double[] spec = new double[pixels];
            int errorReader = 0;

            var task = Task.Run(() => SeaBreezeWrapper.seabreeze_get_formatted_spectrum(specIndex, ref errorReader, ref spec[0], (int)pixels));

            await task;

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
            return spec;
        }

        public override bool areaScanEnabled
        {
            get
            {
                return areaScanEnabled_;
            }
            set
            {

            }
        }

        public override bool highGainModeEnabled
        {
            get { return false; }
            set { return; }
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
                if (!commError)
                {
                    lock (acquisitionLock)
                    {
                        int errorReader = 0;
                        var task = Task.Run(async () => errorReader = await setIntegrationAsync(value));
                        task.Wait();

                        if (errorReader == 0)
                            integrationTime_ = value;

                    }
                }
            }
        }
        long integrationTime_;

        protected async Task<int> setIntegrationAsync(uint value)
        {
            var task = launchSBSetIntegrationAsync(value);
            int timeout = nonSpectrumTimeoutMS;
            int errorCode = 0;
            
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                errorCode = task.Result;
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
                errorCode = -1;
            }

            return errorCode;
        }

        protected async Task<int> launchSBSetIntegrationAsync(uint value)
        {
            int errorReader = 0;
            await Task.Run(() => SeaBreezeWrapper.seabreeze_set_integration_time_microsec(specIndex, ref errorReader, (long)(value * 1000)));

            return errorReader;
        }



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
        public override bool isInGaAs => false;
        public override TRIGGER_SOURCE triggerSource
        {
            get => TRIGGER_SOURCE.EXTERNAL;
            set
            {

            }
        }

        public override float laserTemperatureDegC { get => 0; }

        public override ushort laserTemperatureRaw { get => 0; }

        public override ushort laserTemperatureSetpointRaw { get => 0; }

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
                string retval = "";

                if (!commError)
                {
                    lock (acquisitionLock)
                    {
                        var task = Task.Run(async () => retval = await getFirmwareRevAsync());
                        task.Wait();
                    }
                }
                return retval;
            }
        }

        protected async Task<string> getFirmwareRevAsync()
        {
            var task = launchSBGetFirmwareAsync();
            int timeout = nonSpectrumTimeoutMS;
            string firmware;

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                // task completed within timeout
                firmware = task.Result;
            }
            else
            {
                // timeout logic
                logger.error("SeaBreeze failing to return in expected time, communication error likely");
                commError = true;
                firmware = "";
            }

            return firmware;
        }

        protected async Task<string> launchSBGetFirmwareAsync()
        {
            string retval = null;

            try
            {
                byte[] raw = new byte[32];
                int error = 0;

                await Task.Run(() => SeaBreezeWrapper.seabreeze_get_usb_descriptor_string(specIndex, ref error, 1, ref raw[0], raw.Length));

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
                        retval = matches[0].Groups[0].Value;
                    else
                        retval = text;
                }
            }
            catch (Exception e)
            {
                logger.error("Error getting FX2 firmware version: {0}", e.Message);
            }
            

            return retval;
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

                    string formatted = "";

                    if (response != null)
                    {
                        UInt16 bytes = (UInt16)((response[2] << 8) | response[1]);

                        int major = (bytes >> 12) & 0x0f;
                        int minor = (bytes >> 4) & 0xff;
                        int build = (bytes) & 0x0f;

                        formatted = String.Format("{0:x1}.{1:x2}.{2:x1}", major, minor, build);
                        logger.debug("converted raw FPGA version {0:x4} to {1}", bytes, formatted);
                    }

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
