using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Markup;

namespace WasatchNET
{
    public enum X_AXIS_TYPE { PIXEL, WAVELENGTH, WAVENUMBER };

    /// <summary>
    /// Return type used by MultiChannelWrapper acquisitions, primarily provided
    /// to bundle x-axis and position number, as different channels will presumably
    /// have different settings.  
    /// </summary>
    /// <remarks>
    /// Automatically populates detector temperature if/when a valid intensities
    /// is set.  That way we don't hammer the spectrometer with temperature reads
    /// while looping over null spectra because we're waiting on a trigger.
    /// </remarks>
    public class ChannelSpectrum
    {
        public int pos = -1;
        public X_AXIS_TYPE xAxisType;
        public double[] xAxis;

        public float detectorTemperatureDegC;
        public uint integrationTimeMS;

        Spectrometer spec;

        public ChannelSpectrum(Spectrometer spec = null)
        {
            this.spec = spec;
            if (spec != null)
            {
                pos = spec.multiChannelPosition;
                xAxis = spec.wavenumbers is null ? spec.wavelengths : spec.wavenumbers;
                xAxisType = spec.wavenumbers is null ? X_AXIS_TYPE.WAVELENGTH : X_AXIS_TYPE.WAVENUMBER;
                integrationTimeMS = spec.integrationTimeMS;
            }
        }

        public double[] intensities
        {
            get => _intensities;
            set
            {
                _intensities = value;
                if (spec != null)
                    detectorTemperatureDegC = spec.detectorTemperatureDegC;
            }
        }
        double[] _intensities;
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
    ///
    /// <remarks>
    /// - "Position" and "Channel" are used interchangeably.
    /// - all testing was done with 8-channel system of WP-OEM (ARM) spectrometers,
    ///   but should apply to other configurations
    /// - Position is 1-indexed, given typical customer tendencies and user 
    ///   labeling requirements.
    /// 
    /// As WP-OEM spectrometers do not currently expose an accessory connector 
    /// with an obvious GPIO output useable as a trigger source, and as the 
    /// immediate customer is using non-laser models excited via external light 
    /// sources, the laserEnable output is being used as a quasi-GPIO, both for 
    /// raising trigger signals and to control connected fans.
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

        /// <summary>
        /// If enabled, and if reference measurements have been recorded, will
        /// populate ChannelSpectrum objects with computed reflectance rather 
        /// than raw intensity.
        /// </summary>
        public bool reflectanceEnabled { get; set; }

        /// <summary>
        /// How long the triggerSpec's laserEnable signal is raised high to
        /// generate an acquisition trigger to all spectrometers.
        /// </summary>
        /// <remarks>
        /// Per Nicolas Baron, minimum width is probably 1ms.  Note that
        /// CSharp timing is not particularly precise, so the value entered
        /// here is really more of a floor (even values of zero may work).
        /// </remarks>
        public int triggerPulseWidthMS { get; set; } = 5;

        /// <summary>
        /// force double triggers to occur (QC testing only)
        /// </summary> 
        /// <remarks>
        /// triggerPulseWidthMS is also used as the intra-trigger delay. If used, 
        /// recommend that triggerPulseWidthMS be set no higher than 25% of the 
        /// LOWEST integration time in the system's channels.  That way, assuming 
        /// integration starts on the leading edge, there is room for the 
        /// following sequence during even a minimum-length acquisition (giving
        /// room for a double-trigger event to occur inside a measurement).
        /// 
        /// ----------+----------+----------+----------+
        /// [trigger 1]
        /// [integration starts and runs until it stops]
        ///           [trig delay]
        ///                      [trigger 2]
        /// </remarks>
        public bool forceDoubleTrigger { get; set; }

        /// <summary>
        /// How many extra random triggers were generated via forceDoubleTrigger.
        /// </summary>
        public int doubleTriggersSent { get; set; }

        /// <summary>
        /// if forced double triggering is enabled, it will occur this often 
        /// (1.0 = 100% of the time)
        /// </summary> 
        public double doubleTriggerPercentage { get; set; } = 0.2;
        
        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        static MultiChannelWrapper instance = null;

        Driver driver = Driver.getInstance();
        SortedDictionary<int, Spectrometer> specByPos;

        // which spectrometer generates the external trigger
        Spectrometer specTrigger = null;

        // which spectrometer controls the fan
        Spectrometer specFan = null;

        Random r = new Random();

        static object mut = new object();

        Logger logger = Logger.getInstance();

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
            logger.header($"openAsync: initializing {count} spectrometers");
            for (var i = 0; i < count; i++)
            {
                var spec = driver.getSpectrometer(i);
                var sn = spec.serialNumber;

                logger.info("initializing {0} {1} with {2} pixels ({3:f2}, {4:f2})",
                    spec.model, sn, spec.pixels, spec.wavelengths[0], spec.wavelengths[spec.pixels - 1]);

                // select by default
                spec.multiChannelSelected = true;

                // don't auto-generate SW triggers on calls to Spectrometer.getSpectrum
                // (most of our acquisitions will use hardware triggers)
                spec.autoTrigger = false;

                // ARM units (used for triggering) seem to benefit from a 
                // throwaway after changing integration time.  (Honestly, 
                // probably all models do.)  
                spec.throwawayAfterIntegrationTime = true;

                // ARM units seem susceptible to hanging if commanded too rapidly
                spec.featureIdentification.usbDelayMS = 10;

                ////////////////////////////////////////////////////////////////
                // Parse EEPROM.userText
                ////////////////////////////////////////////////////////////////

                var pos = spectrometerCount + 1;

                // by convention, multi-channel spectrometers will use the
                // EEPROM's userData text field to configure semicolon-delimited
                // name=value pairs, such as the following 3 lines (each a 
                // different nominal spectrometer):
                //
                //   pos=1; feature=trigger
                //   pos=2; feature=fan
                //   pos=3

                // iterate over semi-colon delimited attributes
                var attributes = spec.eeprom.userText.Split(';');
                foreach (var attr in attributes)
                {
                    // assume each attribute is a name=value pair
                    var pair = attr.Split('=');
                    if (pair.Length == 2)
                    {
                        var name = pair[0].ToLower().Trim();
                        var value = pair[1].ToLower().Trim();

                        // currently the only supported attributes
                        // are "pos" and "feature"
                        if (name.Contains("pos"))
                        {
                            if (Int32.TryParse(value, out pos))
                                logger.info($"  {sn} has position {pos}");
                            else
                                logger.error($"{sn}: failed to parse {attr}");
                        }
                        else if (name == "feature")
                        {
                            // currently the only supported features
                            // are "trigger" and "fan"
                            if (value.Contains("trigger"))
                            {
                                logger.info($"  {sn} has trigger feature");
                                specTrigger = spec;
                            }
                            else if (value.Contains("fan"))
                            {
                                logger.info($"  {sn} has fan feature");
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

                // if duplicate positions are defined in the EEPROMs,
                // ignore them and generate unique values
                while (specByPos.ContainsKey(pos))
                    pos++;
                logger.info($"  storing {sn} as position {pos}");
                specByPos[pos] = spec;
                positions.Add(pos);

                // let the Spectrometer know its own position as a 
                // convenient back-reference
                spec.multiChannelPosition = pos;
            }

            // Default to system-wide software triggering, regardless of
            // whether we found a specTrigger or not; let the app decide to
            // override that (not all MultiChannel apps will use hardware
            // triggering).
            logger.header("openAsync: defaulting to software triggering");
            hardwareTriggeringEnabled = false;

            if (spectrometerCount <= 0)
                return logger.error("no spectrometers found");

            // take a throwaway stability spectrum 
            logger.header("taking initial throwaway stability spectra (SW triggered)");
            _ = await getSpectraAsync();

            logger.header("initialization completed successfully");
            return true;
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
        IEnumerable<Spectrometer> specByIntegrationTime_NOT_USED()
        {
            return from pair in specByPos
                   orderby pair.Value.integrationTimeMS ascending
                   select pair.Value;
        }

        /// <summary>
        /// Get a handle to the spectrometer at a given position.
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
        /// The position of the spectrometer configured to generate the external
        /// hardware trigger.
        /// </summary>
        /// <returns>-1 if none found</returns>
        public int triggerPos => specTrigger != null ? specTrigger.multiChannelPosition : -1;

        /// <summary>
        /// Whether external hardware triggering is currently enabled.
        /// </summary>
        /// <remarks>
        /// Note that with ARM spectrometers, whether hardware triggering is enabled
        /// or not may seem somewhat philosophical; after all, ARM spectrometers 
        /// ALWAYS respond to both HW and SW triggers, so this value doesn't actually
        /// change anything inside the spectrometer.
        /// 
        /// What it does do is change behavior within WasatchNET, as normally a 
        /// spectrometer in SW-triggered mode will automatically generate a SW 
        /// trigger when Spectrometer.getSpectrum() is called.  Likewise, timeout
        /// behavior changes depending on this setting, as a HW-triggered acquisition
        /// should generally continue to block until a HW trigger is detected.
        ///
        /// Of course, things are actually more complicated than that, as 
        /// Spectrometer.autoTrigger also feeds into whether the spectrometer 
        /// will internally generate a SW trigger, and there are various explicit
        /// timeouts which can be applied even to HW-triggered spectrometers 
        /// (such as used by MultiChannelDemo's Monte Carlo mode to check for 
        /// spurious double-triggers).
        /// </remarks>
        public bool hardwareTriggeringEnabled
        {
            get => _hardwareTriggeringEnabled;
            set
            {
                foreach (var pair in specByPos)
                {
                    var spec = pair.Value;
                    if (!spec.multiChannelSelected)
                        continue;

                    logger.debug($"pos {pair.Key}: triggerEnabled -> {value}");
                    spec.triggerSource = value ? TRIGGER_SOURCE.EXTERNAL : TRIGGER_SOURCE.INTERNAL;
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
        public bool setIntegrationTimeMS(uint ms)
        {
            logger.header($"setting all spectrometer integration times to {ms}");

            List<Task> settors = new List<Task>();

            foreach (var pair in specByPos)
            {
                var spec = pair.Value;
                if (spec.multiChannelSelected)
                    settors.Add(Task.Run(() => { spec.integrationTimeMS = ms; }));
            }

            Task.WaitAll(settors.ToArray());

            return true;
        }

        /// <summary>
        /// This was considered as a faster way to set all integration times quickly,
        /// performing a single throwaway acquisition on all devices in parallel, but
        /// is not currently being tested.
        /// </summary>
        /// <param name="times"></param>
        /// <returns></returns>
        public bool setIntegrationTimesMS(Dictionary<int, uint> times)
        {
            List<Task> settors = new List<Task>();

            logger.header("setting all integration times");

            foreach (var pair in times)
            {
                var spec = getSpectrometer(pair.Key);
                settors.Add(Task.Run(() => { spec.integrationTimeMS = pair.Value; }));
            }

            logger.debug("waiting for all tasks to complete");
            Task.WaitAll(settors.ToArray());

            logger.header("setIntegrationTimesMS complete");

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // Spectra
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Use the configured spectrometer's laserEnable output to raise a brief 
        /// HW trigger pulse.
        /// </summary>
        /// <remarks>This is async so the GUI is responsive while the trigger is high</remarks>
        public async Task<bool> startAcquisitionAsync(bool configureTimeouts=true)
        {
            if (hardwareTriggeringEnabled)
            {
                if (specTrigger is null)
                {
                    logger.error("no spectrometer configured with trigger control");
                    return false;
                }

                // configure each spectrometer's "even when externally triggered" timeout
                if (configureTimeouts)
                {
                    foreach (var pair in specByPos)
                    {
                        var spec = pair.Value;
                        if (!spec.multiChannelSelected)
                            continue;

                        // this seems sufficiently generous
                        var ms = (uint)(500
                                        + triggerPulseWidthMS
                                        + spec.integrationTimeMS * 2);
                        logger.debug($"startAcquisitionAsync: setting {spec.id} timeout to {ms}");
                        spec.acquisitionTimeoutMS = ms;
                    }
                }

                await sendHWTrigger();
            }
            else
            {
                // send a software trigger down each channel
                foreach (var pair in specByPos)
                {
                    var pos = pair.Key;
                    var spec = pair.Value;
                    if (!spec.multiChannelSelected)
                        continue;

                    logger.debug($"sending SW trigger to pos {pos}");
                    spec.sendSWTrigger();
                }
            }
            logger.debug("startAcquisitionAsync DONE");
            return true;
        }

        public async Task<bool> sendHWTrigger()
        {
            if (specTrigger is null)
                return false;

            int triggersToSend = 1;
            if (forceDoubleTrigger && r.NextDouble() < doubleTriggerPercentage)
                triggersToSend = 2;

            logger.debug($"sendHWTrigger: going to send {triggersToSend} triggers");
            for (int i = 0; i < triggersToSend; i++)
            { 
                logger.debug($"sending {triggerPulseWidthMS}ms HW trigger via {specTrigger.id}");
                specTrigger.laserEnabled = true;
                await Task.Delay(millisecondsDelay: triggerPulseWidthMS);
                specTrigger.laserEnabled = false;

                if (i > 0)
                    doubleTriggersSent++;

                // on multiple triggers, leave a gap
                if (i + 1 < triggersToSend)
                    await Task.Delay(triggerPulseWidthMS);
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
            foreach (var pair in specByPos)
            {
                var spec = pair.Value;
                if (!spec.multiChannelSelected)
                    continue;
            }

            if (sendTrigger)
            {
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

                if (!spec.multiChannelSelected)
                    continue;

                logger.debug($"getting spectrum from pos {pos} {spec.serialNumber} ({spec.integrationTimeMS} ms)");
                double[] intensities = await Task.Run(() => spec.getSpectrum());
                ChannelSpectrum cs = new ChannelSpectrum(spec) { intensities = intensities };
                if (reflectanceEnabled)
                    computeReflectance(spec, cs);

                spectra.Add(cs);
            }

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

            Spectrometer spec = specByPos[pos];

            var intensities = await Task.Run(() => spec.getSpectrum());
            if (intensities is null)
                return null;

            ChannelSpectrum cs = new ChannelSpectrum(spec) { intensities = intensities };

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
            {
                var spec = pair.Value;
                if (spec.multiChannelSelected)
                    spec.dark = null;
            }
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
            {
                var spec = pair.Value;
                if (spec.multiChannelSelected)
                    spec.reference = null;
            }
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
