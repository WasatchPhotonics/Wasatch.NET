using LibUsbDotNet.Main;
using MPSSELight;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WasatchNET
{
    public enum OCT_PROCESS_METHOD { INVALID, LINE_SAMPLE, SIMPLE_MEAN };

    public class WPOCTSpectrometer : Spectrometer
    {
        protected IWPOCTCamera camera { get; set; } = null;
        public string camID = null;
        int numBitsPerPixel = 12;

        public List<ushort[]> firstFrames = new List<ushort[]>();
        public OCT_PROCESS_METHOD processMethod = OCT_PROCESS_METHOD.LINE_SAMPLE;
        protected ushort[] lastFrame = null;

        public List<ushort[,]> firstFrames2D
        {
            get
            {
                List<ushort[,]> frames = new List<ushort[,]>();

                foreach (ushort[] frame in firstFrames) 
                {
                    ushort[,] frame2d = new ushort[linesPerFrame, pixels];

                    for (int i = 0; i < frame.Length; i++) 
                    {
                        int line = i / (int)pixels;
                        int pixel = i % (int)pixels;

                        frame2d[line, pixel] = frame[i];

                    }
                    
                    frames.Add(frame2d);
                }

                return frames;
            }
        }


        public int sampleLine
        {
            get
            {
                return sampleLine_;
            }
            set
            {
                if (value > linesPerFrame)
                    sampleLine_ = linesPerFrame;
                else if (value < 0)
                    sampleLine_ = 0;
                else
                    sampleLine_ = value;
            }

        }

        //public int linesPerFrame = 500;

        int sampleLine_ = 100;


        internal WPOCTSpectrometer(IWPOCTCamera camera, string camID, UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            prioritizeVirtualEEPROM = true;
            isOCT = true;
            this.camera = camera;
            this.camID = camID;
            featureIdentification = new FeatureIdentification(0, 0);
        }

        internal override bool open()
        {
            Task<bool> task = Task.Run(async () => await openAsync());
            return task.Result;
        }

        internal override async Task<bool> openAsync()
        {
            bool openOk = camera.Open(camID);

            if (openOk)
            {
                if (!camID.ToLower().Contains("mx4") &&  !camID.ToLower().Contains("dual"))
                {
                    camera.SetResourceIndex(0);
                }

                pixels = (uint)camera.GetScanWidth();
                eeprom = new WPOCTEEPROM(this, camera);
                if (!(await eeprom.readAsync()))
                {
                    logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                    return false;
                }
                linesPerFrame = eeprom.activePixelsVert;

                camera.StartAcquiring();
                regenerateWavelengths();

                return true;
            }

            else
                return false;

        }

        public override void close()
        {
            Task task = Task.Run(async () => await closeAsync());
            task.Wait();
        }
        public async override Task closeAsync()
        {
            bool stopped = camera.StopAcquiring(true);
            camera.Close();
        }

        public float explicitPeriod()
        {
            return camera.GetLinePeriod();
        }

        public override double[] getSpectrum(bool forceNew = false)
        {
            //camera.StartAcquiring();
            if (forceNew)
            {
                int minWait = 20;

                int wait = (int)((integrationTimeMS / 11.25) / 5);
                if (wait < minWait)
                    wait = minWait;

                //give time for loop wait and usb read back
                wait += 150;

                Thread.Sleep(wait);
            }

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
            ushort[] sum = getFrame(false);

            if (firstFrames.Count < 100)
                firstFrames.Add(sum);

            double[] data = new double[pixels];

            if (sum != null)
            {
                if (processMethod == OCT_PROCESS_METHOD.LINE_SAMPLE)
                {
                    for (int i = 0; i < pixels; ++i)
                        data[i] = sum[i + (sampleLine_ * pixels)];
                }
                else if (processMethod == OCT_PROCESS_METHOD.SIMPLE_MEAN)
                {
                    for (int i = 0; i < pixels; ++i)
                    {
                        double pixelSum = 0;
                        for (int j = 0; j < linesPerFrame; ++j)
                        {
                            pixelSum += sum[i + j * pixels];
                        }

                        data[i] = pixelSum / (double)linesPerFrame;
                    }
                }
            }

            return data;
        }


        public override async Task<double[]> getSpectrumAsync(bool forceNew = false)
        {
            if (forceNew)
            {
                int minWait = 20;

                int wait = (int)((integrationTimeMS / 11.25) / 5);
                if (wait < minWait)
                    wait = minWait;

                //give time for loop wait and usb read back
                wait += 150;

                Thread.Sleep(wait);
            }

            Task<ushort[]> frameTask = Task.Run(() => getFrame());

            ushort[] RawPixelData = await frameTask;
            double[] data = new double[pixels];

            if (RawPixelData != null)
            {
                if (processMethod == OCT_PROCESS_METHOD.LINE_SAMPLE)
                {
                    for (int i = 0; i < pixels; ++i)
                        data[i] = RawPixelData[i + (sampleLine_ * pixels)];
                }
                else if (processMethod == OCT_PROCESS_METHOD.SIMPLE_MEAN)
                {
                    for (int i = 0; i < pixels; ++i)
                    {
                        double sum = 0;
                        for (int j = 0; j < linesPerFrame; ++j) 
                        {
                            sum += RawPixelData[i + j * pixels];   
                        }

                        data[i] = sum / (double)linesPerFrame;
                    }
                }
            }

            return data;
        }

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

        public override ushort[] getFrame(bool direct = true)
        {
            if (direct && lastFrame != null)
                return lastFrame;

            ushort[] bufferLocal = new ushort[pixels * linesPerFrame];
            camera.FlushBuffers();
            bool ok = camera.GetBufferCopy(bufferLocal);

            if (ok)
            {
                GCHandle pinnedArray = GCHandle.Alloc(bufferLocal, GCHandleType.Pinned);
                IntPtr cameraBuffer = pinnedArray.AddrOfPinnedObject();
                ushort[] pixelData = getPixels(cameraBuffer);
                lastFrame = pixelData;
                pinnedArray.Free();
                camera.RequeueBuffer();
                return pixelData;
            }

            return null;
        }

        public override string serialNumber
        {
            get { return eeprom.serialNumber; }
        }

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

        public override float detectorGain
        {
            get
            {
                return 0.0f;
            }
            set
            {

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

        public override bool highGainModeEnabled
        {
            get { return false; }
            set { return; }
        }

        public override bool detectorTECEnabled
        {
            get
            {
                return false;
            }
            set
            {

            }
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
        public override float detectorTemperatureDegC
        {
            get
            {
                return 0.0f;
            }
        }

        public override short ambientTemperatureDegC
        {
            get { return 0; }
        }

        public override string firmwareRevision
        {
            get
            {
                return "";
            }
        }

        public override string fpgaRevision
        {
            get
            {
                return "";
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

        public virtual int testPattern
        {
            get
            {
                return testPattern_;
            }
            set
            {
                testPattern_ = value;
            }
        }
        protected int testPattern_ = 0;

        public virtual float linePeriod
        {
            get
            {
                return linePeriod_;
            }
            set
            {
                if (value != linePeriod_)
                {
                    float prevValue = linePeriod_;
                    if (value < integrationTimeUS)
                    {
                        linePeriod_ = value;
                        integrationTimeUS = value - 0.7f;
                    }

                    bool ok = camera.SetLinePeriod(value);
                    if (ok)
                        linePeriod_ = value;
                    else
                        linePeriod_ = prevValue;
                }
            }
        }
        protected float linePeriod_;

        public virtual float integrationTimeUS
        {
            get
            {
                return integrationTimeUS_;
            }
            set
            {
                if (value != integrationTimeUS_)
                {
                    float prevValue = integrationTimeUS_;
                    if (value > linePeriod)
                    {
                        integrationTimeUS_ = value;
                        linePeriod = value + 0.7f;
                    }

                    bool ok = camera.SetExposureTime(value);
                    if (ok)
                        integrationTimeUS_ = value;
                    else
                        integrationTimeUS_ = prevValue;
                }

            }
        }
        protected float integrationTimeUS_;

        public override uint integrationTimeMS
        {
            get
            {
                return integrationTimeMS_;
            }
            set
            {
                if (value != integrationTimeMS_)
                {
                    camera.SetLinePeriod(value); 
                    bool ok = camera.SetExposureTime(value / 2); //OctUsb.SetIntegrationTime((int)value);
                    if (ok)
                        integrationTimeMS_ = (uint)value;
                }
            }
        }

        public override bool isARM => false;
        public override bool isInGaAs => false;
        public override float excitationWavelengthNM
        {
            get => 840f;
            set { }
        }

        public override bool laserEnabled // dangerous one to cache...
        {
            get
            {
                return false;
            }
            set
            {

            }
        }

        public override ulong laserModulationPulseWidth 
        {
            get
            {
                return 1;
            }
            set
            {

            }
        }

        public override ulong laserModulationPeriod
        {
            get
            {
                return 1;
            }
            set
            {

            }
        }

        public override ushort laserTemperatureRaw
        {
            get
            {
                return 0;
            }

        }

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
                return 1;
            }
        }


        public override TRIGGER_SOURCE triggerSource
        {
            get
            {
                return TRIGGER_SOURCE.INTERNAL;
            }
            set
            {

            }
        }



    }
}
