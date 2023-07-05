﻿using LibUsbDotNet.Main;
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
    public class WPOCTSpectrometer : Spectrometer
    {
        protected IWPOCTCamera camera { get; set; } = null;
        public string camID = null;
        int numBitsPerPixel = 12;

        public List<ushort[]> firstFrames = new List<ushort[]>();
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
                pixels = (uint)camera.GetScanWidth();
                eeprom = new WPOCTEEPROM(this, camera);
                if (!(await eeprom.readAsync()))
                {
                    logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                    return false;
                }
                linesPerFrame = eeprom.activePixelsVert;

                camera.StartAcquiring();

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

        public override double[] getSpectrum(bool forceNew = false)
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

            ushort[] RawPixelData = getFrame(false);

            if (firstFrames.Count < 100)
                firstFrames.Add(RawPixelData);

            double[] data = new double[pixels];

            if (RawPixelData != null)
            {
                for (int i = 0; i < pixels; ++i)
                    data[i] = RawPixelData[i + (sampleLine_ * pixels)];
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
                for (int i = 0; i < pixels; ++i)
                    data[i] = RawPixelData[i + (sampleLine_ * pixels)];
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
                return true;
            }
            set
            {

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

        public override ushort laserTemperatureRaw
        {
            get
            {
                return 0;
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
