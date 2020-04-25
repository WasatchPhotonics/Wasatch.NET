using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WasatchNET
{
    public enum X_AXIS_TYPE { PIXEL, WAVELENGTH, WAVENUMBER };

    /// <summary>
    /// Return type used by MultiChannelWrapper acquisitions, primarily provided
    /// to bundle x-axis and position number, as different channels will presumably
    /// have different settings.
    /// </summary>
    /// <remarks>
    /// Populates "intensities" with last spectrum, but many callers will promptly
    /// overwrite with getSpectrum().
    /// </remarks>
    public class ChannelSpectrum
    {
        public int pos = -1;
        public X_AXIS_TYPE xAxisType;
        public double[] xAxis;
        public double[] intensities;

        // these are mainly for QC testing, but retained for convenience
        public float detectorTemperatureDegC;
        public uint integrationTimeMS;

        public ChannelSpectrum(Spectrometer spec = null)
        {
            if (spec != null)
            {
                Logger.getInstance().debug($"instantiating ChannelSpectrum for {spec.id}");
                pos = spec.multiChannelPosition;
                xAxis = spec.wavenumbers is null ? spec.wavelengths : spec.wavenumbers;
                xAxisType = spec.wavenumbers is null ? X_AXIS_TYPE.WAVELENGTH : X_AXIS_TYPE.WAVENUMBER;
                intensities = spec.lastSpectrum;
                integrationTimeMS = spec.integrationTimeMS;
                detectorTemperatureDegC = spec.detectorTemperatureDegC;
            }
        }
    }

    /// <summary>
    /// WasatchNET has always provided support to control multiple spectrometers in 
    /// parallel. However, such control is inherently manual, with all operations and
    /// timing left to the user. Other than basic synchronization of the USB bus, no
    /// automated timing or coordination is provided across multiple devices.
    /// 
    /// MultiChannelWrapper simplifies operation for customers creating multi-channel
    /// spectroscopic applications in which multiple spectrometers are expected to start
    /// acquisitions at the same time (simultaneous measurements of a single event), 
    /// typically via hardware triggering.
    /// </summary>
    /// <remarks>
    /// - "Position" and "Channel" are used interchangeably.
    /// - all testing was done with 8-channel system of WP-OEM (ARM) spectrometers,
    ///   but should apply to other configurations
    /// - Position is generally assumed to be 1-indexed, given typical customer 
    ///   tendencies and user labeling requirements.  However, negatives are
    ///   definitely considered invalid.
    /// 
    /// As WP-OEM spectrometers do not currently expose an accessory connector 
    /// with an obvious GPIO output useable as a trigger source, and as the 
    /// immediate customer is using non-laser models with external light sources,
    /// the laserEnable output is being used as a quasi-GPIO, both for raising
    /// trigger signals and to control connected fans (if found).
    /// </remarks>
    public class MultiChannelWrapper
    {
        ////////////////////////////////////////////////////////////////////////
        // Public attributes
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A convenience accessor to iterate over valid positions (channels).
        /// </summary>
        public SortedSet<int> positions { get; private set; } = new SortedSet<int>();

        public bool reflectanceEnabled { get; set; }

        public int triggerPulseWidthMS { get; set; } = 50;
        
        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        static MultiChannelWrapper instance = null;

        Driver driver = Driver.getInstance();
        SortedDictionary<int, Spectrometer> specByPos;
        Logger logger = Logger.getInstance();

        Spectrometer specTrigger = null;
        Spectrometer specFan = null;

        static object mut = new object();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get a handle to the MultiChannelWrapper singleton.
        /// </summary>
        public static MultiChannelWrapper getInstance()
        {
            lock (mut)
            {
                if (instance is null)
                    instance = new MultiChannelWrapper();
                return instance;
            }
        }

        MultiChannelWrapper()
        {
            reset();
        }

        void reset()
        {
            specByPos = new SortedDictionary<int, Spectrometer>();
            positions = new SortedSet<int>();
            specTrigger = null;
            specFan = null;
        }

        /// <summary>
        /// Initialize all connected spectrometers, determine positions in the multi-channel
        /// system, find out which are configured to provide the external hardware trigger
        /// and fan control, etc.
        /// </summary>
        /// <returns>true on success</returns>
        public async Task<bool> openAsync()
        {
            reset();


            var count = driver.openAllSpectrometers();
            logger.info($"found {count} spectrometers");
            for (var i = 0; i < count; i++)
            {
                var spec = driver.getSpectrometer(i);
                var sn = spec.serialNumber;

                logger.info("found {0} {1} with {2} pixels ({3:f2}, {4:f2})",
                    spec.model, sn, spec.pixels, spec.wavelengths[0], spec.wavelengths[spec.pixels - 1]);

                // don't auto-generate SW triggers (we'll do it in the wrapper)
                spec.autoTrigger = false;

                // ARM units (used for triggering) seem to benefit from a 
                // throwaway after changing integration time.  Honestly, probably
                // all models do...
                //
                // YOU ARE HERE -- testing going back without this
                //
                // spec.throwawayAfterIntegrationTime = true;

                ////////////////////////////////////////////////////////////////
                // Parse EEPROM.userText
                ////////////////////////////////////////////////////////////////

                var pos = spectrometerCount + 1;

                var attributes = spec.eeprom.userText.Split(';');
                foreach (var attr in attributes)
                {
                    var pair = attr.Split('=');
                    if (pair.Length == 2)
                    {
                        var name = pair[0].ToLower().Trim();
                        var value = pair[1].ToLower().Trim();

                        if (name.Contains("pos"))
                        {
                            if (Int32.TryParse(value, out pos))
                                logger.info($"{sn} has position {pos}");
                            else
                                logger.error($"{sn}: failed to parse {attr}");
                        }
                        else if (name == "feature")
                        {
                            if (value.Contains("trigger"))
                            {
                                logger.info($"{sn} has trigger feature");
                                specTrigger = spec;
                            }
                            else if (value.Contains("fan"))
                            {
                                logger.info($"{sn} has fan feature");
                                specFan = spec;
                            }
                            else
                            {
                                logger.error($"unsupported feature: {attr}");
                            }
                        }
                        else
                        {
                            logger.error($"unsupported attribute: {attr}");
                        }
                    }
                    else
                    {
                        logger.error($"{sn}: not name-value pair: {attr}");
                    }
                }

                // make sure we have no duplicates
                while (specByPos.ContainsKey(pos))
                    pos++;
                logger.info($"storing {sn} as position {pos}");
                specByPos[pos] = spec;
                spec.multiChannelPosition = pos;
                positions.Add(pos);
            }

            // default to software triggering
            hardwareTriggeringEnabled = false;

            // take a throwaway spectrum from everything
            _ = await getSpectraAsync();

            return spectrometerCount > 0;
        }

        /// <summary>
        /// Release all resources at end of application session.
        /// </summary>
        public void close()
        {
            driver.closeAllSpectrometers();
            reset();
        }

        ////////////////////////////////////////////////////////////////////////
        // Device Control
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// How many spectrometers (channels) were found.
        /// </summary>
        public int spectrometerCount => specByPos.Count;

        // private utility method to sort spectrometers by integration time
        // in increasing order, so we can process the "soonest-done" first,
        // grab the longest at the end and save time.
        IEnumerable<Spectrometer> specByIntegrationTime()
        {
            return from pair in specByPos
                   orderby pair.Value.integrationTimeMS ascending
                   select pair.Value;
        }

        /// <summary>
        /// Lets the caller directly control the spectrometer at a given position.
        /// </summary>
        /// <param name="pos">Which channel</param>
        public Spectrometer getSpectrometer(int pos)
        {
            if (specByPos.ContainsKey(pos))
                return specByPos[pos];
            logger.error($"Attempt to access spectrometer at unpopulated position {pos}");
            return null;
        }

        /// <summary>
        /// The position of the spectrometer configured to generate the external hardware trigger.
        /// </summary>
        /// <returns>-1 if none found</returns>
        public int triggerPos => specTrigger != null ? specTrigger.multiChannelPosition : -1;

        /// <summary>
        /// Whether external hardware triggering is currently enabled.
        /// </summary>
        public bool hardwareTriggeringEnabled
        {
            get => _hardwareTriggeringEnabled;
            set
            {
                foreach (var pair in specByPos)
                {
                    logger.debug($"pos {pair.Key}: triggerEnabled -> {value}");
                    pair.Value.triggerSource = value ? TRIGGER_SOURCE.EXTERNAL : TRIGGER_SOURCE.INTERNAL;
                }
                _hardwareTriggeringEnabled = value;
            }
        }
        bool _hardwareTriggeringEnabled;

        /// <summary>
        /// The position of the spectrometer configured to control the fans.
        /// </summary>
        /// <returns>-1 if none found</returns>
        public int fanPos => specFan != null ? specFan.multiChannelPosition : -1;

        /// <summary>
        /// Use the configured spectrometer's laserEnable output to turn the system fans on or off.
        /// </summary>
        /// <remarks>Obviously dangerous on spectrometers with physical lasers.</remarks>
        /// <todo>move to GPIO</todo>
        public bool fanEnabled
        {
            get
            {
                if (specFan is null)
                {
                    logger.error("no spectrometer configured with fan control");
                    return false;
                }
                return specFan.laserEnabled;
            }

            set
            {
                if (specFan is null)
                {
                    logger.error("no spectrometer configured with fan control");
                    return;
                }
                specFan.laserEnabled = value;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Acquisition Parameters
        ////////////////////////////////////////////////////////////////////////

        // could refactor this to do each spectrometer in its own Task,
        // leaving automatic throwaways in place
        async public Task<bool> setIntegrationTimeMSAsync(uint ms)
        {
            logger.header($"setting all spectrometer integration times to {ms}");

            if (false)
            {
                // this is fast and works, but circumvents the robustness I'm trying to test

                // temporarily disable throwaways
                foreach (var pair in specByPos)
                {
                    var spec = pair.Value;
                    spec.throwawayAfterIntegrationTime = false;
                    spec.integrationTimeMS = ms;
                }

                // perform manual throwaway in parallel
                _ = await getSpectraAsync();

                // re-enable throwaways
                foreach (var pair in specByPos)
                    pair.Value.throwawayAfterIntegrationTime = true;
            }
            else
            {
                foreach (var pair in specByPos)
                {
                    var spec = pair.Value;
                    spec.integrationTimeMS = ms;
                }
            }

            logger.debug("done setting integration times");

            return true;
        }

        /*
        async public Task<bool> setIntegrationTimesAsync(Dictionary<int, int> times)
        {
            logger.header("setting all integration times");

            // temporarily disable throwaways
            foreach (var pair in times)
            {
                var pos = pair.Key;
                var ms = pair.Value;

                var spec = getSpectrometer(pos);
                spec.throwawayAfterIntegrationTime = false;
                spec.integrationTimeMS = (uint)ms;
            }

            // perform manual throwaway in parallel
            logger.debug("now taking throwaways (will involve sending 1 HW or 8 SW triggers)");
            _ = await getSpectraAsync();

            // re-enable throwaways
            logger.debug("done with throwaways");
            foreach (var pair in times)
            {
                var pos = pair.Key;

                var spec = getSpectrometer(pos);
                spec.throwawayAfterIntegrationTime = true;
            }

            return true;
        }
        */

        ////////////////////////////////////////////////////////////////////////
        // Spectra
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Use the configured spectrometer's laserEnable output to raise a brief 
        /// HW trigger pulse.
        /// </summary>
        /// <todo>test w/ARM spectrometers</todo>
        /// <remarks>This is async so the GUI is responsive while the trigger is high</remarks>
        public async Task<bool> startAcquisitionAsync()
        {
            if (hardwareTriggeringEnabled)
            {
                if (specTrigger is null)
                {
                    logger.error("no spectrometer configured with trigger control");
                    return false;
                }

                logger.header("sending HW trigger");

                logger.debug("trigger -> high");
                specTrigger.laserEnabled = true;

                logger.debug("trigger (wait)");
                await Task.Delay(millisecondsDelay: triggerPulseWidthMS);

                logger.debug("trigger -> low");
                specTrigger.laserEnabled = false;
            }
            else
            {
                // send a software trigger down each channel
                foreach (var pair in specByPos)
                {
                    var pos = pair.Key;
                    var spec = pair.Value;

                    logger.debug($"sending SW trigger to pos {pos}");
                    spec.sendTrigger();
                }
            }
            return true;
        }

        /// <summary>
        /// Get spectra from each populated position.
        /// </summary>
        /// <returns>
        /// A map of spectra for each position.  Each spectrum is itself a tuple of x- and y-axes.  
        /// Choice of x-axis is determined by useWavenumbers.
        /// </returns>
        /// <remarks>
        /// This is not executed in parallel because it doesn't really need to be.
        /// The USB bus is serial anyway, and communication latencies are minimal
        /// compared to integration time.  Sorting by integration time should ensure
        /// total acquisition time is close to minimal.
        /// </remarks>
        /// <param name="sendTrigger">whether to automatically raise the HW trigger (default true)</param>
        public async Task<List<ChannelSpectrum>> getSpectraAsync(bool sendTrigger = true)
        {
            if (sendTrigger)
            {
                logger.debug("getSpectraAsync: starting acquisition");
                if (!await startAcquisitionAsync())
                {
                    logger.error("Unable to start acquisition");
                    return null;
                }
            }

            List<ChannelSpectrum> spectra = new List<ChannelSpectrum>();

            foreach (var pair in specByPos)
            {
                var pos = pair.Key;
                var spec = pair.Value;

                logger.header($"MultiChannelWrapper.getSpectraAsync: position {pos}");

                logger.debug($"getting spectrum from pos {pos} {spec.serialNumber} ({spec.integrationTimeMS} ms)");
                ChannelSpectrum cs = new ChannelSpectrum(spec);
                cs.intensities = await Task.Run(() => spec.getSpectrum());
                if (reflectanceEnabled)
                    computeReflectance(spec, cs);

                spectra.Add(cs);
            }

            logger.debug($"getSpectraAsync: returning {spectra.Count} spectra");
            return spectra;
        }

        /// <summary>
        /// Get a spectrum from one spectrometer.  Assumes HW triggering is disabled, or caller is providing.
        /// </summary>
        /// <param name="pos">spectrometer position to acquire</param>
        /// <returns>measured spectrum</returns>
        public async Task<ChannelSpectrum> getSpectrumAsync(int pos)
        {
            if (!specByPos.ContainsKey(pos))
                return null;

            logger.header($"MultiChannelWrapper.getSpectrumAsync: position {pos}");

            Spectrometer spec = specByPos[pos];

            ChannelSpectrum cs = new ChannelSpectrum(spec);
            cs.intensities = await Task.Run(() => spec.getSpectrum());

            if (reflectanceEnabled)
                computeReflectance(spec, cs);

            return cs;
        }

        ////////////////////////////////////////////////////////////////////////
        // Dark
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Takes a spectrum from each spectrometer, and stores it internally as a new dark reference.
        /// </summary>
        /// <remarks>
        /// Subsequent calls to getSpectra will be automatically dark-corrected,
        /// until clearDark() is called.
        /// </remarks>
        /// <returns>The darks collected (also stored in Spectrometer)</returns>
        public async Task<List<ChannelSpectrum>> takeDarkAsync(bool sendTrigger = true)
        {
            clearDark();
            List<ChannelSpectrum> results = await getSpectraAsync(sendTrigger);
            foreach (var cs in results)
                specByPos[cs.pos].dark = cs.intensities;
            return results;
        }

        /// <summary>
        /// Clears the stored dark from all spectrometers.
        /// </summary>
        public void clearDark()
        {
            foreach (var pair in specByPos)
                pair.Value.dark = null;
        }

        ////////////////////////////////////////////////////////////////////////
        // Reference
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Takes a spectrum from each spectrometer, and stores it internally as 
        /// a new reference.
        /// </summary>
        /// <returns>The collected references (also stored in Spectrometer)</returns>
        public async Task<List<ChannelSpectrum>> takeReferenceAsync(bool sendTrigger = true)
        {
            clearReference();
            List<ChannelSpectrum> results = await getSpectraAsync(sendTrigger);
            foreach (var cs in results)
                specByPos[cs.pos].reference = cs.intensities;
            return results;
        }

        /// <summary>
        /// Clears the stored reference from all spectrometers.
        /// </summary>
        public void clearReference()
        {
            foreach (var pair in specByPos)
                pair.Value.reference = null;
        }

        ////////////////////////////////////////////////////////////////////////
        // Post-Processing
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// It is assumed that either the sample and the reference were both
        /// dark-corrected, or that neither were.
        /// </summary>
        void computeReflectance(Spectrometer spec, ChannelSpectrum cs)
        {
            if (!reflectanceEnabled || spec.reference is null)
                return;

            for (int i = 0; i < spec.pixels; i++)
                if (spec.reference[i] != 0.0)
                    cs.intensities[i] /= spec.reference[i];
                else
                    cs.intensities[i] = 0.0;
        }
    }
}
