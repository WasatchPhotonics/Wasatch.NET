﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Threading.Tasks;
#if WIN32
using ATMCD32CS;
#else
using ATMCD64CS;
#endif

namespace WasatchNET
{
    public class AndorSpectrometer : Spectrometer
    {
#if WIN32
        internal AndorSDK andorDriver = new ATMCD32CS.AndorSDK();
#else
        internal AndorSDK andorDriver = new ATMCD64CS.AndorSDK();
#endif
        internal int specIndex;
        int cameraHandle = 0;
        int yPixels;
        const int DRV_SUCCESS = 20002;
        const int SHUTTER_SPEED = 35;

        internal AndorSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            int minTemp = 0;
            int maxTemp = 0;
            int xPixels = 0;
            uint errorValue = 0;
            AndorSDK.AndorCapabilities capabilities = new AndorSDK.AndorCapabilities();

            //excitationWavelengthNM = 0;
            //triggerSource = TRIGGER_SOURCE.INTERNAL;
            specIndex = index;
            andorDriver.GetCameraHandle(specIndex, ref cameraHandle);
            andorDriver.SetCurrentCamera(cameraHandle);
            andorDriver.Initialize("");

            andorDriver.GetCapabilities(ref capabilities);
            andorDriver.GetTemperatureRange(ref minTemp, ref maxTemp);
            andorDriver.SetTemperature((minTemp + maxTemp) / 2);
            andorDriver.GetDetector(ref xPixels, ref yPixels);
            andorDriver.CoolerON();
            andorDriver.SetAcquisitionMode(1);
            andorDriver.SetTriggerMode(0);
            // Set read mode to required setting specified in xxxxWndw.c
            errorValue = andorDriver.SetReadMode(0);

            int VSnumber = 0;
            float speed = 0;
            // Set Vertical speed to recommended
            andorDriver.GetFastestRecommendedVSSpeed(ref VSnumber, ref speed);
            errorValue = andorDriver.SetVSSpeed(VSnumber);

            // Set Horizontal Speed to max
            float STemp = 0;
            int HSnumber = 0;
            int ADnumber = 0;
            int nAD = 0;
            int sIndex = 0;
            errorValue = andorDriver.GetNumberADChannels(ref nAD);
            if (errorValue != DRV_SUCCESS)
            {

            }
            else
            {
                for (int iAD = 0; iAD < nAD; iAD++)
                {
                    andorDriver.GetNumberHSSpeeds(iAD, 0, ref sIndex);
                    for (int iSpeed = 0; iSpeed < sIndex; iSpeed++)
                    {
                        andorDriver.GetHSSpeed(iAD, 0, iSpeed, ref speed);
                        if (speed > STemp)
                        {
                            STemp = speed;
                            HSnumber = iSpeed;
                            ADnumber = iAD;
                        }
                    }
                }
            }

            errorValue = andorDriver.SetADChannel(ADnumber);
            errorValue = andorDriver.SetHSSpeed(0, HSnumber);
            andorDriver.SetShutterEx(1, 1, SHUTTER_SPEED, SHUTTER_SPEED, 0);
            andorDriver.SetExposureTime(0.001f);
            pixels = (uint)xPixels;
            eeprom = new AndorEEPROM(this);

        }

        override internal bool open()
        {
            eeprom = new AndorEEPROM(this);

            lock (acquisitionLock)
            {
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

                logger.info("Opened Andor Spectrometer with index {0}", specIndex);

                return true;
            }
        }


        public override void close()
        {
            //wrapper.shutdown();
            andorDriver.SetCurrentCamera(cameraHandle);
            andorDriver.ShutDown();
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

        protected override double[] getSpectrumRaw(bool skipTrigger = false)
        {
            logger.debug("requesting spectrum");
            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////
            int[] spec = new int[pixels];
            //andorDriver.sh

            for (int size = 1; size <= yPixels; ++size)
            {
                spec = new int[size * pixels]; // default to all zeros
                andorDriver.StartAcquisition();
                andorDriver.WaitForAcquisition();
                uint success = andorDriver.GetAcquiredData(spec, (uint)(size * pixels));

                if (success == 20002)
                    break;
                if (success == 20067)
                    continue;
            }

            double[] convertedSpec = Array.ConvertAll(spec, item => (double)item);

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);
            return convertedSpec;
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
                lock (acquisitionLock)
                {
                    float exposure = 0;
                    float accumulate = 0;
                    float kinetic = 0;
                    andorDriver.SetExposureTime((float)value / 1000);
                    andorDriver.GetAcquisitionTimings(ref exposure, ref accumulate, ref kinetic);

                    integrationTime_ = (long)Math.Round(exposure * 1000);

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
            }
            set
            {
                if (value)
                    andorDriver.CoolerON();
                else
                    andorDriver.CoolerOFF();
                tecEnabled_ = value;
            }
        }
        bool tecEnabled_ = false;

        public override float detectorTemperatureDegC
        {
            get
            {
                int temp = 0;
                andorDriver.GetTemperature(ref temp);
                return temp;
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
                andorDriver.SetTemperature((int)value);
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

    }
}
