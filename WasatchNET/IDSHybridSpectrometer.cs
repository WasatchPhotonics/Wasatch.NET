using IDSImaging.Peak.API;

#if x64
using IDSImaging.Peak.API.Core;
using IDSImaging.Peak.API.Core.Nodes;
using IDSImaging.Peak.API.Std;
#endif

using IDSImaging.Peak.Common;
using IDSImaging.Peak.Common.Serialization;
using IDSImaging.Peak.IPL;

using LibUsbDotNet.Main;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WasatchNET
{
    public class IDSHybridSpectrometer : Spectrometer
    {
        public int availableCameras = 0;
        protected static bool isInit = false;
        protected ushort[] lastFrame = null;

        private Spectrometer sidecar = null;
        private bool sidecarAvailable = false;

#if x64
        private DeviceManager deviceManager = null; //DeviceManager.Instance();
        private Device device = null;
        private NodeMap nodeMap = null;
        private DataStream dataStream = null;
#endif
        private string userSet = "";
        private ImageConverter imc = null;
        private ImageTransformer imt = null;
        private string inputFormat = "Invalid";
        private string outputFormat = "Mono16";
        private int bufferAmount = 1;
#if x64
        private List<IDSImaging.Peak.API.Core.Buffer> buffers = new List<IDSImaging.Peak.API.Core.Buffer>();
#endif
        private List<IntPtr> pointers = new List<IntPtr>();

        readonly string[] SUPPORTED_CONVERSIONS = {
            "Mono16", "Mono12", "Mono10", "Mono8",
            "RGB12",  "RGB10",  "RGB8",
            "BGR12",  "BGR10",  "BGR8",
            "RGBa12", "RGBa10", "RGBa8",
            "BGRa12", "BGRa10", "BGRa8"
        };


        private string[] userSetOptions { get; } = new string[] { "Default", "LongExposure" };

        internal IDSHybridSpectrometer(UsbRegistry usbReg) : base(usbReg, true)
        {

#if x64
            if (!isInit)
            {
                IDSImaging.Peak.API.Library.Initialize();
                isInit = true;
            }

            deviceManager = DeviceManager.Instance();
            deviceManager.Update();
            availableCameras = deviceManager.Devices().Count;
#endif

            sidecar = new Spectrometer(usbReg);
        }

        override internal bool open()
        {
            Task<bool> task = Task.Run(async () => await openAsync());
            return task.Result;
        }

        override internal async Task<bool> openAsync()
        {
#if WIN32
            return false;
        }
    }
#elif x64

            try
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
                if (!sidecarAvailable)
                {
                    eeprom.serialNumber = "WP-SV-XXXXX";
                    eeprom.model = "UNKNOWN-SV";
                    eeprom.userData = new byte[63];
                    FloatNode fn = nodeMap.FindNode<FloatNode>("ExposureTime");
                    eeprom.minIntegrationTimeMS = (uint)(fn.Minimum() / 1000);
                    eeprom.maxIntegrationTimeMS = (uint)(fn.Maximum() / 1000);
                }

                integrationTimeMS = 15;//(uint)(nodeMap.FindNode<FloatNode>("ExposureTime").Value() / 1000f);
                lastIntegrationTimeMS = 15;
                detectorStartLine = 0;
                detectorStopLine = (ushort)(eeprom.activePixelsVert - 1);
                setUserSet("Default");
                nodeMap.FindNode<FloatNode>("ExposureTime").SetValue(15000);
                startCollection();
            }
            catch (Exception ex)
            {
                logger.error("IDS Spec open failed with error {0}", ex.Message);

                return false;
            }

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

        public void shutDownLibrary()
        {
            if (isInit)
            {
                logger.debug("shutting down IDS API");
                IDSImaging.Peak.API.Library.Close();
                isInit = false;
            }
        }

        ~IDSHybridSpectrometer()
        {
            logger.debug("entered IDS Hybrid finalizer");
            close();
        }


        public override void close()
        {
            Task task = Task.Run(async () => await closeAsync());
            task.Wait();
        }
        public async override Task closeAsync()
        {
            stopCollection();
            if (dataStream != null)
            {
                foreach (var buf in dataStream.AnnouncedBuffers())
                {
                    dataStream.RevokeBuffer(buf);
                }
            }

            //wrapper.shutdown();
            //await Task.Run(() => andorDriver.SetCurrentCamera(cameraHandle));
            //await Task.Run(() => andorDriver.ShutDown());
        }

        void initImageConverter(string newOutputFormat = null)
        {
            PixelFormatName inputName = PixelFormatName.Invalid;
            PixelFormatName outputName = PixelFormatName.Invalid;

            bool ok = Enum.TryParse<PixelFormatName>(inputFormat, out inputName);
            ok = ok && Enum.TryParse<PixelFormatName>(outputFormat, out outputName);

            if (!ok)
                return;

            // there has got to be a better way to do this...
            imc = new ImageConverter();
            imc.PreAllocateConversion(new PixelFormat(inputName), new PixelFormat(outputName), eeprom.activePixelsHoriz, eeprom.activePixelsVert);

        }

        void sendTrigger()
        {
            nodeMap.FindNode<CommandNode>("TriggerSoftware").Execute();
            nodeMap.FindNode<CommandNode>("TriggerSoftware").WaitUntilDone();
        }

        void resetDataStream()
        {
            if (dataStream != null)
            {
                foreach (var buffer in dataStream.AnnouncedBuffers())
                {
                    dataStream.RevokeBuffer(buffer);
                }

                buffers.Clear();
                pointers.Clear();
            }

            var payloadSize = nodeMap.FindNodeInteger("PayloadSize").Value();
            bufferAmount = (int)dataStream.NumBuffersAnnouncedMinRequired();
            for (int i = 0; i < bufferAmount; i++)
            {
                IntPtr newPtr = new IntPtr();
                pointers.Add(newPtr);
                var bufferLocal = dataStream.AllocAndAnnounceBuffer((uint)payloadSize, newPtr);
                buffers.Add(bufferLocal);
            }
        }

        bool started = false;

        void startCollection()
        {
            if (started)
                return;

            dataStream = null;
            dataStream = device.DataStreams()[0].OpenDataStream();
            resetDataStream();

            // unsure on this one
            nodeMap.FindNode<IntegerNode>("TLParamsLocked").SetValue(1);

            var ifNode = nodeMap.TryFindNodeEnumeration("PixelFormat");
            inputFormat = ifNode.CurrentEntry().SymbolicValue();

            if (imc == null)
                initImageConverter();

            if (imt == null)
                imt = new ImageTransformer();

            dataStream.StartAcquisition();
            nodeMap.FindNode<CommandNode>("AcquisitionStart").Execute();
            nodeMap.FindNode<CommandNode>("AcquisitionStart").WaitUntilDone();
            started = true;
        }

        void stopCollection()
        {
            if (!started)
            {
                logger.debug("trying to stop collection that never started");
                return;
            }
            nodeMap.FindNode<CommandNode>("AcquisitionStop").Execute();
            nodeMap.FindNode<CommandNode>("AcquisitionStop").WaitUntilDone();
            dataStream.StopAcquisition(AcquisitionStopMode.Default);
            nodeMap.FindNode<IntegerNode>("TLParamsLocked").SetValue(0);
            dataStream.Flush(DataStreamFlushMode.DiscardAll);
            started = false;
        }

        Image processImage(Image image, IDSImaging.Peak.API.Core.Buffer buffer = null)
        {
            PixelFormatName outputName = PixelFormatName.Invalid;
            bool ok = Enum.TryParse<PixelFormatName>(outputFormat, out outputName);
            if (!ok)
                return null;

            var converted = imc.Convert(image, new PixelFormat(outputName));

            if (buffer != null)
                dataStream.QueueBuffer(buffer);

            if (eeprom.featureMask.invertXAxis)
                imt.MirrorUpDownLeftRightInPlace(converted);

            return converted;
        }

        protected override double[] getAreaScanLightweight()
        {
            double[] data = lastFrame.Select(x => (double)x).ToArray();
            return data;
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
            double[] sum = getSpectrumRaw();
            if (scanAveraging_ > 1)
            {
                // logger.debug("getSpectrum: getting additional spectra for averaging");
                for (uint i = 1; i < scanAveraging_; i++)
                {
                    // don't send a new SW trigger if using continuous acquisition
                    double[] tmp;
                    while (true)
                    {
                        if (currentAcquisitionCancelled || shuttingDown)
                            return null;

                        if (areaScanEnabled && fastAreaScan)
                        {
                            tmp = getAreaScanLightweight();
                        }
                        else
                        {
                            tmp = getSpectrumRaw();
                        }

                        if (currentAcquisitionCancelled || shuttingDown)
                            return null;

                        if (tmp != null)
                            break;

                        return null;
                    }
                    if (tmp is null)
                        return null;

                    for (int px = 0; px < sum.Length; px++)
                        sum[px] += tmp[px];
                }

                for (int px = 0; px < sum.Length; px++)
                    sum[px] /= scanAveraging_;
            }

            //camera.StopAcquiring(true);
            return sum;
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
            double[] data = new double[pixels];
            if (RawPixelData != null)
            {
                for (int i = 0; i < pixels; ++i)
                {
                    double sum = 0;
                    for (int j = 0; j < linesPerFrame; ++j)
                    {
                        sum += RawPixelData[i + j * pixels];
                    }

                    data[i] = sum;
                }
            }

            return data;
        }

        public override ushort[] getFrame(bool direct = true)
        {
            if (direct && lastFrame != null)
                return lastFrame;

            sendTrigger();

            ulong timeoutMS = (ulong)(1000 + 2 * Math.Max(integrationTimeMS, lastIntegrationTimeMS));

            IDSImaging.Peak.API.Core.Buffer buffer = null;

            try
            {
                buffer = dataStream.WaitForFinishedBuffer(timeoutMS);
            }
            catch (Exception ex)
            {

                logger.error($"failed on datastream.WaitForFinishedBuffer(timeout {timeoutMS}ms) with error {ex.Message}");
                return null;
            }

            Image image = IPLExtension.ToIPLImage(buffer);
            image = processImage(image, buffer);

            IntPtr convertedPtr = image.Data();
            ushort[] pixels = getPixels(convertedPtr);


            //below is a "safe" pixel grab alternative worth exploring
            /*
            PixelRow pr = new PixelRow(image, 0);
            foreach (var pix in pr.Channels())
            {
                pix.Values[]
            }
            */

            lastIntegrationTimeMS = integrationTimeMS;
            return pixels;
        }

        //
        // This assumes Mono16. Borrowed from OCT
        // I *love* learning about pixel formats -TS
        //
        unsafe ushort[] getPixels(IntPtr buffer)
        {
            int width = eeprom.activePixelsHoriz;
            int height = eeprom.activePixelsVert;
            int bitWidth = 16;
            //ubitsperpixel == bitwidth, numbitsused == numbitsperpixel 

            //var boundsRect = new Rectangle(0, 0, width, height);
            ushort[] pixels = new ushort[width * height];

            unsafe
            {
                int intensity = 0;
                ushort* sPixels = (ushort*)buffer;

                // For each row...
                for (int i = 0; i < height; i++)
                {
                    int rowStart = i * width;
                    // For each col...
                    for (int j = 0; j < width; j++)
                    {
                        intensity = sPixels[i * width + j];
                        pixels[rowStart + j] = (ushort)intensity;
                    }
                }

            }

            return pixels;
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
#endif
}
