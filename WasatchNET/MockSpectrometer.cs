using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Newtonsoft.Json;
using System.IO;

namespace WasatchNET
{
    public class MockSpectrometer : Spectrometer
    {
        uint darkBaseline = 0;
        double sensitivity = 1.0;
        string currentSource = "";
        Random noiseMaker = new Random();

        public enum SAMPLE_METHOD { EXACT, LINEAR_INTERPOLATION, NOISY_LINEAR_INTERPOLATION };

        /// <summary>
        /// Collection of spectra for simulating various samples.
        /// In practice, accessing individual spectra (as double arrays) will look like => 
        /// interpolationSamples["Xe"][120] 
        /// </summary>
        Dictionary<string, SortedDictionary<int, double[]>> interpolationSamples = new Dictionary<string, SortedDictionary<int, double[]>>();
        Dictionary<string, SortedDictionary<int, ushort[]>> interpolationFrames = new Dictionary<string, SortedDictionary<int, ushort[]>>();

        public SAMPLE_METHOD sampleMethod = SAMPLE_METHOD.NOISY_LINEAR_INTERPOLATION;

        /// <summary>
        /// Simple, relatively flexible mock spectrometer
        /// General workflow is addData(source, time, spectrum) -> setSource(source) for as many different sources as desired
        /// Once these calls are made, the spectrometer will produce data, optionally with noise, for the set source
        /// With current implementation, need at least two integration times per source to interpolate and create "new" spectra
        /// Temperatures will be randomly produced based on the given setpoint
        /// </summary>
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

        public override bool highGainModeEnabled
        {
            get { return false; }
            set { return; }
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

        public override bool setLaserPowerPercentage(float perc)
        {
            return true;
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

            eeprom = new MockEEPROM(this);

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

        public override double[] getSpectrum(bool forceNew = false)
        {
            var driver = Driver.getInstance();
            lock (acquisitionLock)
            {
                currentAcquisitionCancelled = false;

                int retries = 0;
                double[] sum = null;
                while (true)
                {
                    if (currentAcquisitionCancelled || shuttingDown)
                        return null;

                    sum = getSpectrumRaw();

                    if (currentAcquisitionCancelled || shuttingDown)
                        return null;

                    if (sum != null)
                        break;

                    if (retries++ < acquisitionMaxRetries)
                    {
                        // retry the whole thing (including ACQUIRE)
                        logger.error($"getSpectrum: received null from getSpectrumRaw, attempting retry {retries}");
                        continue;
                    }
                    else if (errorOnTimeout)
                    {
                        // display error if configured
                        logger.error($"getSpectrum: getSpectrumRaw returned null ({id})");
                    }
                    return null;
                }
                logger.debug("getSpectrum: received {0} pixels", sum.Length);

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

                            tmp = getSpectrumRaw(skipTrigger: scanAveragingIsContinuous);

                            if (currentAcquisitionCancelled || shuttingDown)
                                return null;

                            if (tmp != null)
                                break;

                            if (retries++ < acquisitionMaxRetries)
                            {
                                // retry the whole thing (including ACQUIRE)
                                logger.error($"getSpectrum: received null from getSpectrumRaw, attempting retry {retries}");
                                continue;
                            }
                            else if (errorOnTimeout)
                            {
                                // display error if configured
                                logger.error($"getSpectrum: getSpectrumRaw returned null ({id})");
                            }
                            return null;
                        }
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

                spectrumCount++;

                if (boxcarHalfWidth_ > 0)
                    return Util.applyBoxcar(boxcarHalfWidth_, sum);
                else
                    return sum;
            }
        }


        protected override double[] getSpectrumRaw(bool skipTrigger = false)
        {
            logger.debug($"getSpectrumRaw: requesting spectrum {id}");

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
                {
                    logger.debug("Unable to generate spectrum for {0} in getSpectrum, returning noise", currentSource);
                    spec = addNoise(spec, darkBaseline, darkBaseline / 20);
                }
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

            if (isOCT)
                Thread.Sleep(70);
            else
                Thread.Sleep((int)integrationTimeMS_);

            lastSpectrum = spec;
            return spec;
        }

        public override ushort[] getFrame()
        {
            logger.debug($"getSpectrumRaw: requesting spectrum {id}");

            ////////////////////////////////////////////////////////////////////
            // read spectrum
            ////////////////////////////////////////////////////////////////////

            ushort[] spec = new ushort[pixels * linesPerFrame]; // default to all zeros

            SortedDictionary<int, ushort[]> spectra;
            if (sampleMethod == SAMPLE_METHOD.LINEAR_INTERPOLATION || sampleMethod == SAMPLE_METHOD.NOISY_LINEAR_INTERPOLATION)
            {
                if (interpolationFrames.TryGetValue(currentSource, out spectra))
                {
                    spec = interpolateFrames();
                }
                else
                {
                    logger.debug("Unable to generate spectrum for {0} in getSpectrum, returning noise", currentSource);
                }
            }

            if (eeprom.featureMask.invertXAxis)
                Array.Reverse(spec);


            logger.debug("getSpectrumRaw: returning {0} pixels", spec.Length);

            // logger.debug("getSpectrumRaw({0}): {1}", id, string.Join<double>(", ", spec));

            Thread.Sleep((int)integrationTimeMS_);

            return spec;
        }


        public void setSource(string src)
        {
            currentSource = src;
        }

        public string[] getSources()
        {
            return interpolationSamples.Keys.ToArray();
        }

        public bool initSpectrometer(string jsonFile)
        {
            MockSpectrometerJSON json = null;
            string text = File.ReadAllText(jsonFile);

            //string crypText = AesOperation.EncryptString(AesOperation.jsonLock, text);

            //text = AesOperation.DecryptString(AesOperation.jsonLock, crypText);

            try
            {
                json = JsonConvert.DeserializeObject<MockSpectrometerJSON>(text);
                logger.debug("successfully deserialized QCConfig");
            }
            catch (JsonReaderException)
            {
                return false;
            }

            if (json.EEPROM != null)
            {
                eeprom.setFromJSON(json.EEPROM);

                regenerateWavelengths();
            }

            if (json.frames != null && json.frames.Count > 0)
            {
                isOCT = true;
                linesPerFrame = 500;

                foreach (string src in json.frames.Keys)
                {
                    if (json.frames[src].Count > 0)
                    {
                        foreach (int time in json.frames[src].Keys)
                        {
                            addFrame(src, time, json.frames[src][time]);
                        }
                    }
                }
            }

            if (json.measurements != null && json.measurements.Count > 0)
            {
                foreach (string src in json.measurements.Keys)
                {
                    if (json.measurements[src].Count > 0)
                    {
                        foreach (int time in json.measurements[src].Keys)
                        {
                            addData(src, time, json.measurements[src][time]);
                        }
                    }
                }

                return true;

            }

            else
                return false;

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

        public void addFrame(string src, int integrationTime, ushort[] spectrum)
        {
            SortedDictionary<int, ushort[]> tempSpectra = new SortedDictionary<int, ushort[]>();
            if (!interpolationFrames.TryGetValue(src, out tempSpectra))
            {
                tempSpectra = new SortedDictionary<int, ushort[]>();
                interpolationFrames.Add(src, tempSpectra);
            }

            ushort[] tempSpectrum;
            if (!tempSpectra.TryGetValue(integrationTime, out tempSpectrum))
            {
                tempSpectra.Add(integrationTime, spectrum);
            }

            tempSpectra[integrationTime] = spectrum;
            interpolationFrames[src] = tempSpectra;
        }

        public void clearData()
        {
            interpolationSamples.Clear();
            interpolationFrames.Clear();
        }


        /// <summary>
        /// Creates an interpolated spectrum based on the integration time and the collected samples
        /// The call to this function assumes the current source is in the data dictionary, if not it will 
        /// return an array of 0s
        /// </summary>
        ///
        /// <param name="forceNew">not used in base class (provided for specialized subclasses)</param>
        ///
        /// <returns>An interpolated spectrum based on the stored data and current source</returns>
        double[] interpolateSamples()
        {
            double[] final = new double[pixels];

            SortedDictionary<int, double[]> spectra;

            // look for the current source in our collection
            if (interpolationSamples.TryGetValue(currentSource, out spectra))
            {
                //TS: If there is a single spectrum for the source simply return it. We could make some assumptions
                //about sensitivity here and still do an interpolation, but I'm personally not that comfortable with that
                //and prefer we require at least two spectra per source
                if (spectra.Count == 1)
                    return spectra.First().Value;

                //Traverse spectra (ordered by integration time) until index == spectra with integration time just below
                //the set integration time
                //
                //For example, if we have 10ms, 20ms, and 30ms, and want to build a spectrum at 25ms, index will be set to 1
                int index = -1;
                foreach (int integration in spectra.Keys)
                {
                    if (integration > integrationTimeMS)
                        break;
                    index++;
                }

                //Block executed if the requested integration time is between two existing samples in our library
                if (index != spectra.Count - 1 && index != -1)
                {
                    double[] minSpectrum;
                    double[] maxSpectrum;
                    int minIntegration;
                    int maxIntegration;

                    //grab the two spectra with integration times around the time we're aiming for
                    var enumerator = spectra.GetEnumerator();
                    enumerator.MoveNext();
                    for (int i = 0; i < index; ++i)
                        enumerator.MoveNext();
                    minSpectrum = enumerator.Current.Value;
                    minIntegration = enumerator.Current.Key;
                    enumerator.MoveNext();

                    maxSpectrum = enumerator.Current.Value;
                    maxIntegration = enumerator.Current.Key;

                    //do a per pixel weighted average (weighted by how close the desired integration time is) between the 
                    //two spectra to create the interpolated spectrum
                    double span = maxIntegration - minIntegration;
                    double pctMax = ((double)integrationTimeMS - (double)minIntegration) / span;

                    for (int i = 0; i < pixels; ++i)
                        final[i] = (1 - pctMax) * minSpectrum[i] + pctMax * maxSpectrum[i];

                }

                //Block executed if the requested integration time is less than the minmimum sample in our library,
                //or more than the maximum sample
                else
                {
                    double[] minSpectrum;
                    double[] maxSpectrum;
                    int minIntegration;
                    int maxIntegration;

                    //grab the spectra with the two lowest integration times
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
                    //grab the spectra with the two highest integration times
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

                    //extrapolate to "out of bounds" integration times, assuming perfect linearity on every pixel
                    for (int i = 0; i < pixels; ++i)
                    {
                        double slope = (maxSpectrum[i] - minSpectrum[i]) / (maxIntegration - minIntegration);
                        double delta = slope * (integrationTimeMS - maxIntegration);

                        //clamp to 0 - 2^16, should not be needed for interpolating, just this extrapolating
                        final[i] = Math.Max(0,Math.Min(maxSpectrum[i] + delta, Math.Pow(2,16)));
                    }


                }
            }

            //TS: apply noise if desired. Probably should have higher SD and be more data driven
            if (sampleMethod == SAMPLE_METHOD.NOISY_LINEAR_INTERPOLATION)
                final = addNoise(final, 20, 1);

            return final;
        }

        ushort[] interpolateFrames()
        {
            ushort[] final = new ushort[pixels * linesPerFrame];

            SortedDictionary<int, ushort[]> spectra;

            // look for the current source in our collection
            if (interpolationFrames.TryGetValue(currentSource, out spectra))
            {
                //TS: If there is a single spectrum for the source simply return it. We could make some assumptions
                //about sensitivity here and still do an interpolation, but I'm personally not that comfortable with that
                //and prefer we require at least two spectra per source
                if (spectra.Count == 1)
                    return spectra.First().Value;

                //Traverse spectra (ordered by integration time) until index == spectra with integration time just below
                //the set integration time
                //
                //For example, if we have 10ms, 20ms, and 30ms, and want to build a spectrum at 25ms, index will be set to 1
                int index = -1;
                foreach (int integration in spectra.Keys)
                {
                    if (integration > integrationTimeMS)
                        break;
                    index++;
                }

                //Block executed if the requested integration time is between two existing samples in our library
                if (index != spectra.Count - 1 && index != -1)
                {
                    ushort[] minSpectrum;
                    ushort[] maxSpectrum;
                    int minIntegration;
                    int maxIntegration;

                    //grab the two spectra with integration times around the time we're aiming for
                    var enumerator = spectra.GetEnumerator();
                    enumerator.MoveNext();
                    for (int i = 0; i < index; ++i)
                        enumerator.MoveNext();
                    minSpectrum = enumerator.Current.Value;
                    minIntegration = enumerator.Current.Key;
                    enumerator.MoveNext();

                    maxSpectrum = enumerator.Current.Value;
                    maxIntegration = enumerator.Current.Key;

                    //do a per pixel weighted average (weighted by how close the desired integration time is) between the 
                    //two spectra to create the interpolated spectrum
                    double span = maxIntegration - minIntegration;
                    double pctMax = ((double)integrationTimeMS - (double)minIntegration) / span;

                    for (int i = 0; i < pixels * linesPerFrame; ++i)
                        final[i] = (ushort)((1 - pctMax) * minSpectrum[i] + pctMax * maxSpectrum[i]);

                }

                //Block executed if the requested integration time is less than the minmimum sample in our library,
                //or more than the maximum sample
                else
                {
                    ushort[] minSpectrum;
                    ushort[] maxSpectrum;
                    int minIntegration;
                    int maxIntegration;

                    //grab the spectra with the two lowest integration times
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
                    //grab the spectra with the two highest integration times
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

                    //extrapolate to "out of bounds" integration times, assuming perfect linearity on every pixel
                    for (int i = 0; i < pixels * linesPerFrame; ++i)
                    {
                        double slope = (maxSpectrum[i] - minSpectrum[i]) / (maxIntegration - minIntegration);
                        double delta = slope * (integrationTimeMS - maxIntegration);

                        //clamp to 0 - 2^16, should not be needed for interpolating, just this extrapolating
                        final[i] = (ushort)Math.Max(0, Math.Min(maxSpectrum[i] + delta, Math.Pow(2, 16)));
                    }


                }
            }

            return final;
        }


        public void seedNoise(int seed)
        {
            noiseMaker = new Random(seed);
        }

        double addNoise(double data, double mean, double sd)
        {
            double u1 = 1.0 - noiseMaker.NextDouble(); //uniform(0,1] noiseMakerom doubles
            double u2 = 1.0 - noiseMaker.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal = mean + sd * randStdNormal;

            double noised = (double)data + randNormal;

            return noised;
        }

        double[] addNoise(double[] data, double mean, double sd)
        {
            double[] noised = new double[data.Length];

            int index = 0;
            foreach (double d in data)
            {
                double u1 = 1.0 - noiseMaker.NextDouble(); //uniform(0,1] noiseMakerom doubles
                double u2 = 1.0 - noiseMaker.NextDouble();
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
