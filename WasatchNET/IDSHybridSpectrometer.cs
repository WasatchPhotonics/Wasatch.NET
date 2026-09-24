using IDSImaging.Peak.API;
using IDSImaging.Peak.API.Core;
using IDSImaging.Peak.API.Core.Nodes;
using IDSImaging.Peak.API.Std;
using IDSImaging.Peak.Common;
using IDSImaging.Peak.IPL;

using LibUsbDotNet.Main;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WasatchNET
{
    public class IDSHybridSpectrometer : Spectrometer
    {
        protected static bool isInit = false; 
        protected ushort[] lastFrame = null;

        private Spectrometer sidecar = null;
        private bool sidecarAvailable = false;
        private DeviceManager deviceManager = DeviceManager.Instance();
        private Device device = null;
        private NodeMap nodeMap = null;
        private DataStream dataStream = null;
        private string userSet = "";
        private ImageConverter imc = null;

        private string[] userSetOptions { get; } = new string[] { "Default", "LongExposure" };

        internal IDSHybridSpectrometer(UsbRegistry usbReg) : base(usbReg, true)
        {
            if (!isInit)
            {
                IDSImaging.Peak.API.Library.Initialize();
                isInit = true;
            }

            sidecar = new Spectrometer(usbReg);
        }

        override internal bool open()
        {
            Task<bool> task = Task.Run(async () => await openAsync());
            return task.Result;
        }

        override internal async Task<bool> openAsync()
        {
            deviceManager.Update();
            if (!deviceManager.Devices().Any())
            {
                return false;
            }

            sidecarAvailable = await sidecar.openAsync();
            if (sidecarAvailable)
                eeprom = sidecar.eeprom;
            else
                eeprom = new EEPROM(this);

            var devices = deviceManager.Devices();
            device = devices[0].OpenDevice(DeviceAccessType.Control);
            nodeMap = device.RemoteDevice().NodeMaps()[0];

            eeprom.detectorSerialNumber = nodeMap.FindNode<StringNode>("DeviceSerialNumber").Value();
            eeprom.detectorName = nodeMap.FindNode<StringNode>("SensorName").Value();
            eeprom.activePixelsHoriz = eeprom.actualPixelsHoriz = (ushort)nodeMap.FindNode<IntegerNode>("WidthMax").Value();
            eeprom.activePixelsVert = (ushort)nodeMap.FindNode<IntegerNode>("Height").Value();
            integrationTimeMS = 15;//(uint)(nodeMap.FindNode<FloatNode>("ExposureTime").Value() / 1000f);
            lastIntegrationTimeMS = 15;
            detectorStartLine = 0;
            detectorStopLine = (ushort)(eeprom.activePixelsVert - 1);
            setUserSet("Default");
            nodeMap.FindNode<FloatNode>("ExposureTime").SetValue(15000);
            return true;
        }

        void setUserSet(string value, bool force = false)
        {
            if (!userSetOptions.Contains(value))
                return;

            if (value != userSet || force)
            {
                userSet = value;

                lock (commsLock)
                {
                    nodeMap.FindNode<EnumerationNode>("UserSetSelector").SetCurrentEntry(value);
                    nodeMap.FindNode<CommandNode>("UserSetLoad").Execute();
                    nodeMap.FindNode<CommandNode>("UserSetLoad").WaitUntilDone();

                    var node = nodeMap.FindNode<FloatNode>("ExposureTime");
                    logger.debug($"set_user_set: applied UserSetSelector {value}, ExposureTime range now ({node.Minimum()}, {node.Maximum()})µs");
                    initSoftwareTriggering();
                }
            }
        }

        void initSoftwareTriggering()
        {
            nodeMap.FindNode<EnumerationNode>("TriggerSelector").SetCurrentEntry("ReadOutStart");
            nodeMap.FindNode<EnumerationNode>("TriggerMode").SetCurrentEntry("On");
            nodeMap.FindNode<EnumerationNode>("TriggerSource").SetCurrentEntry("Software");

        }

        ~IDSHybridSpectrometer()
        {
            logger.debug("entered IDS Hybrid finalizer");
            IDSImaging.Peak.API.Library.Close();
        }


        public override void close()
        {
            Task task = Task.Run(async () => await closeAsync());
            task.Wait();
        }
        public async override Task closeAsync()
        {
            //wrapper.shutdown();
            //await Task.Run(() => andorDriver.SetCurrentCamera(cameraHandle));
            //await Task.Run(() => andorDriver.ShutDown());
        }

        void initImageConverte()
        {
            imc = new ImageConverter();

        }

        void sendTrigger()
        {
            nodeMap.FindNode<CommandNode>("TriggerSoftware").Execute();
            nodeMap.FindNode<CommandNode>("TriggerSoftware").WaitUntilDone();
        }

        void resetDataStream()
        {
        }

        bool started = false;

        void startCollection()
        {
            if (started)
                return;

            dataStream = null;
            dataStream = device.DataStreams()[0].OpenDataStream();

            nodeMap.FindNodeBoolean("TLParamsLocked").SetValue(true);

            /*
             * 
             * Skip input format stuff for now
             * 
             */

            dataStream.StartAcquisition();
            nodeMap.FindNode<CommandNode>("AcquisitionStart").Execute();
            nodeMap.FindNode<CommandNode>("AcquisitionStart").WaitUntilDone();
            started = true;
        }

        uint[] binImage(Image image)
        {

            return null;
        }


        public override double[] getSpectrum(bool forceNew = false)
        {
            lock (acquisitionLock)
            {
                Task<double[]> task = Task.Run(async () => await getSpectrumAsync(forceNew));
                return task.Result;
            }
        }


        public override async Task<double[]> getSpectrumAsync(bool forceNew = false)
        {
            return null;
        }

        protected override double[] getSpectrumRaw(bool skipTrigger = false)
        {
            Task<double[]> task = Task.Run(async () => await getSpectrumRawAsync(skipTrigger));
            return task.Result;
        }

        protected override async Task<double[]> getSpectrumRawAsync(bool skipTrigger = false)
        {
            Task<ushort[]> frameTask = Task.Run(() => getFrame());

            ushort[] RawPixelData = await frameTask;



            return null;
        }

        public override ushort[] getFrame(bool direct = true)
        {
            if (direct && lastFrame != null)
                return lastFrame;

            ulong timeoutMS = (ulong)(1000 + 2 * Math.Max(integrationTimeMS, lastIntegrationTimeMS));
            var buffer = dataStream.WaitForFinishedBuffer(timeoutMS);

            Image image = IPLExtension.ToIPLImage(buffer);
            

            lastIntegrationTimeMS = integrationTimeMS;
            return null;
        }

        public override bool resetFPGA() => true;

        public override bool areaScanEnabled
        {
            get
            {
                return areaScanEnabled_;
            }
            set
            {
                areaScanEnabled_ = value;
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

        const uint fullMaxMS = 120000;
        const uint fullMinMS = 15;

        // 1001 ms
        const uint longExposureMinUS = 1001000;

        // 2000 ms
        const uint defaultMaxUS = 2000000;

        uint lastIntegrationTimeMS { get; set; }
        public override uint integrationTimeMS
        {
            get
            {
                return integrationTimeMS_;
            }
            set
            {
                lock (acquisitionLock)
                {
                    integrationTimeMS_ = Math.Min(fullMaxMS, Math.Max(value, fullMinMS));
                    var intTimeUS = 1000 * integrationTimeMS_;

                    // if requested time is outside range of either mode, change to the other
                    // otherwise keep as is
                    if (intTimeUS <= longExposureMinUS)
                        setUserSet("Default");
                    else if (intTimeUS >= defaultMaxUS)
                        setUserSet("LongExposure");

                    nodeMap.FindNode<FloatNode>("ExposureTime").SetValue(intTimeUS);
                }
            }
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
        public override byte laserWarningDelaySec { get => 0; set { } }
        public override byte laserPowerAttenuation { get => 0; set { } }

        public override UInt64 laserModulationPeriod { get => 100; }

        public override ulong laserModulationPulseWidth { get => 0; set { } }

        public override float detectorGain
        {
            get
            {
                return 0.0f;
            }
            set
            {
                double local = value;
                
                //unclear as of 9/24//26 if this is actually a flot node -TS
                var node = nodeMap.FindNode("Gain");
                var typedNode = node as FloatNode;

                local = Math.Max(local, typedNode.Minimum());
                local = Math.Min(local, typedNode.Maximum());

            }
        }


        public override float detectorGainOdd
        {
            get
            {
                return 0.0f;
            }
            set
            {

            }
        }

        public override short detectorOffset
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }


        public override short detectorOffsetOdd
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        public override ushort detectorStartLine
        {
            get { return detectorStartLine_; }
            set { lock (acquisitionLock) detectorStartLine_ = value; }
        }

        public override ushort detectorStopLine
        {
            get { return detectorStopLine_; }
            set { lock (acquisitionLock) detectorStopLine_ = value; }
        }


        public override bool isARM => false;

        // This won't actually do anything until we add code to load the virtual EEPROM from AWS
        public override bool isInGaAs => eeprom.detectorName.Contains("DU490");

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

        public override UInt16 laserWatchdogSec
        {

            get
            {
                return 0;
            }
            set
            {

            }

        }

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
            }
            set
            {
                tecEnabled_ = value;
            }
        }
        bool tecEnabled_ = false;

        public override float detectorTemperatureDegC
        {
            get
            {
                // get a new value if possible, but if a spectrum is being collected just
                // return the cached value
                if (Monitor.TryEnter(acquisitionLock))
                {
                }

                return lastDetectorTemperatureDegC;
            }
        }

        public override short ambientTemperatureDegC
        {
            get { return 0; }
        }

        public override bool laserTECEnabled
        {
            get
            {
                return false;
            }
            set
            {

            }
        }

        public override ushort laserTECMode
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        public override IMAGE_SENSOR_STATUS imageSensorStatus
        {
            //we do NOT want to cache this one
            get
            {
                return IMAGE_SENSOR_STATUS.IMG_SNSR_STATE_NO_RESPONSE;
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
                //andorDriver.SetTemperature((int)value);
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

                return retval;
            }
        }

        public override string fpgaRevision
        {
            get
            {
                string retval = "";

                return retval;
            }
        }

        public override string bleRevision
        {
            get
            {
                string retval = "";

                return retval;
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
                eeprom.excitationNM = (ushort)value;
                eeprom.laserExcitationWavelengthNMFloat = value;
            }
        }

        public override bool continuousAcquisitionEnable { get => false; set { } }
        public override byte continuousFrames { get => 0; set { } }
        public override ushort detectorTemperatureRaw { get => 0; }
    }
}
