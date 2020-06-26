using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace WasatchNET
{
    public class MockSpectrometer : Spectrometer
    {
        uint darkBaseline = 0;
        double sensitivity = 1.0;
        string currentSource = "";

        public enum SAMPLE_METHOD { EXACT, LINEAR_INTERPOLATION, NOISY_LINEAR_INTERPOLATION };

        Dictionary<string, SortedDictionary<int, double[]>> interpolationSamples = new Dictionary<string, SortedDictionary<int, double[]>>();
        public SAMPLE_METHOD sampleMethod = SAMPLE_METHOD.NOISY_LINEAR_INTERPOLATION;

        internal MockSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {

        }

        public override float batteryPercentage
        {
            get
            {
                return 0;
            }
        }

        public override bool batteryCharging
        {
            get
            {
                return false;
            }
        }

        public override float detectorGain
        {
            get
            {
                return 0f;
            }
            set
            {

            }
        }

        public override float detectorGainOdd
        {
            get
            {
                return 0f;
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

        public override bool detectorTECEnabled
        {
            get
            {
                return detectorTECEnabled_;
            }
            set
            {
                detectorTECEnabled_ = value;
            }
        }

        public override ushort detectorTECSetpointRaw
        {
            get
            {
                return detectorTECSetpointRaw_;
            }
            set
            {
                if (eeprom.hasCooling)
                {
                    detectorTECSetpointRaw_ = value;
                }
            }
        }

        public override float detectorTemperatureDegC
        {
            get
            {
                return (float)addNoise(detectorTECSetpointDegC, .1, .03);
            }
        }

        public override string firmwareRevision
        {
            get
            {
                return "MOCK";
            }
        }

        public override string fpgaRevision
        {
            get
            {
                return "MOCK";
            }
        }

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
                    //should logic for setting from stored data be here??
                    integrationTimeMS_ = value;
                }
            }
        }

        public override bool laserEnabled // dangerous one to cache...
        {
            get
            {
                return laserEnabled_;
            }
            set
            {
                laserEnabled_ = value;
            }
        }

        public override bool laserInterlockEnabled
        {
            get
            {
                return true;
            }
        }

        public override UInt64 laserModulationPeriod
        {
            get
            {
                return laserModulationPeriod_;
            }
            set
            {
                laserModulationPeriod_ = value;
            }
        }

        public override UInt64 laserModulationPulseWidth
        {
            get
            {
                return laserModulationPulseWidth_;
            }
            set
            {
                laserModulationPulseWidth_ = value;
            }
        }

        public override float laserTemperatureDegC
        {
            get
            {
                return 0;
            }
        }

        public override ushort laserTemperatureRaw => 0;

        public override byte laserTemperatureSetpointRaw
        {
            get
            {
                return laserTemperatureSetpointRaw_;
            }
            set
            {
                laserTemperatureSetpointRaw_ = Math.Min((byte)127, value);
            }
        }

        public override ushort secondaryADC
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
                return triggerSource_;
            }
            set
            {
                triggerSource_ = value;
            }
        }

        //////////////////////////////////////////////////////
        //              NEEDS IMPLEMENT
        //////////////////////////////////////////////////////
        override internal bool open()
        {
            darkBaseline = 800;

            eeprom = new EEPROM(this);

            if (!eeprom.read())
            {
                logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                //wrapper.shutdown();
                close();
                return false;
            }

            regenerateWavelengths();

            return true;
        }

        public bool open(uint pixels)
        {
            this.pixels = pixels;
            return open();
        }

        //////////////////////////////////////////////////////
        //              NEEDS IMPLEMENT
        //////////////////////////////////////////////////////
        public override void close()
        {

        }

        public override bool isARM => false;

        public override bool reconnect()
        {
            return true;
        }

        //////////////////////////////////////////////////////
        //              NEEDS IMPLEMENT
        //////////////////////////////////////////////////////
        public override double[] getSpectrum(bool forceNew = false)
        {
            lock (acquisitionLock)
            {
                double[] sum = getSpectrumRaw();
                if (sum is null)
                {
                    if (!currentAcquisitionCancelled && errorOnTimeout)
                        logger.error($"getSpectrum: getSpectrumRaw returned null ({id})");
                    return null;
                }
                logger.debug("getSpectrum: received {0} pixels", sum.Length);

                if (scanAveraging_ > 1)
                {
                    // logger.debug("getSpectrum: getting additional spectra for averaging");
                    for (uint i = 1; i < scanAveraging_; i++)
                    {
                        // don't send a new SW trigger if using continuous acquisition
                        double[] tmp = getSpectrumRaw(skipTrigger: scanAveragingIsContinuous);
                        if (tmp is null)
                            return null;

                        for (int px = 0; px < pixels; px++)
                            sum[px] += tmp[px];
                    }

                    for (int px = 0; px < pixels; px++)
                        sum[px] /= scanAveraging_;
                }

                correctBadPixels(ref sum);

                if (dark != null && dark.Length == sum.Length)
                    for (int px = 0; px < pixels; px++)
                        sum[px] -= dark_[px];

                // this should be enough to update the cached value
                if (readTemperatureAfterSpectrum && eeprom.hasCooling)
                    _ = detectorTemperatureDegC;

                if (boxcarHalfWidth_ > 0)
                {
                    // logger.debug("getSpectrum: returning boxcar");
                    return Util.applyBoxcar(boxcarHalfWidth_, sum);
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
            logger.debug($"getSpectrumRaw: requesting spectrum {id}");
            byte[] buf = null;

            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            double[] spec = new double[pixels]; // default to all zeros

            SortedDictionary<int, double[]> spectra;
            if (sampleMethod == SAMPLE_METHOD.LINEAR_INTERPOLATION || sampleMethod == SAMPLE_METHOD.NOISY_LINEAR_INTERPOLATION)
            {
                if (interpolationSamples.TryGetValue(currentSource, out spectra))
                {
                    spec = interpolateSamples();
                }
                else
                    spec = addNoise(spec, darkBaseline, darkBaseline / 20);
            }

            if (eeprom.featureMask.invertXAxis)
                Array.Reverse(spec);

            if (eeprom.featureMask.bin2x2)
            {
                var smoothed = new double[spec.Length];
                for (int i = 0; i < spec.Length - 1; i++)
                    smoothed[i] = (spec[i] + spec[i + 1]) / 2.0;
                smoothed[spec.Length - 1] = spec[spec.Length - 1];
                spec = smoothed;
            }

            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);

            // logger.debug("getSpectrumRaw({0}): {1}", id, string.Join<double>(", ", spec));

            Thread.Sleep((int)integrationTimeMS_);

            lastSpectrum = spec;
            return spec;
        }

        public void setSource(string src)
        {
            currentSource = src;
        }

        public void addData(string src, int integrationTime, double[] spectrum)
        {
            SortedDictionary<int, double[]> tempSpectra = new SortedDictionary<int, double[]>();
            if(!interpolationSamples.TryGetValue(src, out tempSpectra))
            {
                tempSpectra = new SortedDictionary<int, double[]>();
                interpolationSamples.Add(src, tempSpectra);
            }

            double[] tempSpectrum;
            if(!tempSpectra.TryGetValue(integrationTime, out tempSpectrum))
            {
                tempSpectra.Add(integrationTime, spectrum);
            }

            tempSpectra[integrationTime] = spectrum;
            interpolationSamples[src] = tempSpectra;
        }

        public void clearData()
        {
            interpolationSamples.Clear();
        }

        double[] interpolateSamples()
        {
            double[] final = new double[pixels];

            SortedDictionary<int, double[]> spectra;
            if (interpolationSamples.TryGetValue(currentSource, out spectra))
            {
                if (spectra.Count == 1)
                    return spectra.First().Value;

                int index = -1;
                foreach (int integration in spectra.Keys)
                {
                    if (integration > integrationTimeMS)
                        break;
                    index++;
                }

                if (index != spectra.Count - 1 && index != -1)
                {
                    double[] minSpectrum;
                    double[] maxSpectrum;
                    int minIntegration;
                    int maxIntegration;

                    var enumerator = spectra.GetEnumerator();
                    enumerator.MoveNext();
                    for (int i = 0; i < index; ++i)
                        enumerator.MoveNext();
                    minSpectrum = enumerator.Current.Value;
                    minIntegration = enumerator.Current.Key;
                    enumerator.MoveNext();

                    maxSpectrum = enumerator.Current.Value;
                    maxIntegration = enumerator.Current.Key;

                    double span = maxIntegration - minIntegration;
                    double pctMax = ((double)integrationTimeMS - (double)minIntegration) / span;

                    for (int i = 0; i < pixels; ++i)
                        final[i] = (1 - pctMax) * minSpectrum[i] + pctMax * maxSpectrum[i];

                }
                else
                {
                    double[] minSpectrum;
                    double[] maxSpectrum;
                    int minIntegration;
                    int maxIntegration;

                    if (index == -1)
                    {
                        var enumerator = spectra.GetEnumerator();
                        enumerator.MoveNext();
                        minSpectrum = enumerator.Current.Value;
                        minIntegration = enumerator.Current.Key;
                        enumerator.MoveNext();

                        maxSpectrum = enumerator.Current.Value;
                        maxIntegration = enumerator.Current.Key;
                    }
                    else
                    {
                        var enumerator = spectra.GetEnumerator();
                        for (int i = 0; i < spectra.Count - 1; ++i)
                            enumerator.MoveNext();
                        minSpectrum = enumerator.Current.Value;
                        minIntegration = enumerator.Current.Key;
                        enumerator.MoveNext();

                        maxSpectrum = enumerator.Current.Value;
                        maxIntegration = enumerator.Current.Key;
                    }

                    for (int i = 0; i < pixels; ++i)
                    {
                        double slope = (maxSpectrum[i] - minSpectrum[i]) / (maxIntegration - minIntegration);
                        double delta = slope * (integrationTimeMS - maxIntegration);
                        final[i] = maxSpectrum[i] + delta;
                    }


                }
            }

            if (sampleMethod == SAMPLE_METHOD.NOISY_LINEAR_INTERPOLATION)
                final = addNoise(final, 20, 1);

            return final;
        }

        double addNoise(double data, double mean, double sd)
        {
            Random rand = new Random(); //reuse this if you are generating many
            double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal = mean + sd * randStdNormal;

            double noised = (double)data + randNormal;

            return noised;
        }

        double[] addNoise(double[] data, double mean, double sd)
        {
            Random rand = new Random(); //reuse this if you are generating many

            double[] noised = new double[data.Length];

            int index = 0;
            foreach (double d in data)
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal = mean + sd * randStdNormal;

                double nd = (double)d + randNormal;
                noised[index] = nd;
                ++index;
            }

            return noised;
        }


    }
}
