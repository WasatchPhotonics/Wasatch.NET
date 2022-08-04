using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Tracing;

namespace WasatchNET
{
    public enum X_AXIS_TYPE { PIXEL, WAVELENGTH, WAVENUMBER };

    /// <summary>
    /// Return type used by MultiChannelWrapper acquisitions, provided to bundle
    /// x-axis and position number.  Also snapshots integration time and temperature
    /// at time of construction.
    /// </summary>
    public class ChannelSpectrum
    {
        public int pos = -1;
        public uint integrationTimeMS;
        public float detectorTemperatureDegC;
        public double[] intensities;
        public X_AXIS_TYPE xAxisType;
        Spectrometer spec;

        public ChannelSpectrum(Spectrometer spec)
        {
            this.spec = spec;
            pos = spec.multiChannelPosition;
            xAxisType = spec.wavenumbers is null ? X_AXIS_TYPE.WAVELENGTH : X_AXIS_TYPE.WAVENUMBER;
            integrationTimeMS = spec.integrationTimeMS;
            detectorTemperatureDegC = spec.lastDetectorTemperatureDegC;
        }

        public double[] xAxis
        {
            get
            {
                switch (xAxisType)
                {
                    case X_AXIS_TYPE.WAVELENGTH: return spec.wavelengths;
                    case X_AXIS_TYPE.WAVENUMBER: return spec.wavenumbers;
                    default:  return spec.pixelAxis;
                }
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
        /// Whether spectrometers in the system should automatically take 
        /// "throwaway" acquisitions after changing integration time (default).
        /// </summary>
        public bool integrationThrowaways
        {
            get => _integrationThrowaways;
            set
            {
                foreach (var pair in specByPos)
                    pair.Value.throwawayAfterIntegrationTime = value;
                _integrationThrowaways = value;
            }
        }
        bool _integrationThrowaways = true;

        /// <summary>
        /// A convenience accessor to iterate over populated positions (channels).
        /// </summary>
        public SortedSet<int> positions { get; private set; } = new SortedSet<int>();

        /// <summary>
        /// If enabled, and if reference measurements have been recorded, calls
        /// to getSpectraAsync() and getSpectrumAsync() will populate 
        /// ChannelSpectrum intensities with computed reflectance rather than raw 
        /// counts.
        /// </summary>
        public bool reflectanceEnabled { get; set; }

        /// <summary>
        /// How long the triggerSpec's laserEnable signal is raised high to
        /// generate an acquisition trigger to all spectrometers.
        /// </summary>
        ///
        /// <remarks>
        /// Per Nicolas Baron, minimum width is probably 1ms.  Note that
        /// CSharp timing is not particularly precise, so the value entered
        /// here is really more of a floor (even values of zero may work).
        /// </remarks>
        public int triggerPulseWidthMS { get; set; } = 5;

        /// <summary>
        /// Configure how the whole system will report spectra, so it doesn't 
        /// vary per-channel.
        /// </summary>
        public X_AXIS_TYPE xAxisType = X_AXIS_TYPE.WAVELENGTH;

        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        static MultiChannelWrapper instance = null;

        public Driver driver = Driver.getInstance();
        SortedDictionary<int, Spectrometer> specByPos;

        // which spectrometer generates the external trigger
        Spectrometer specTrigger = null;

        // which spectrometer controls the fan
        Spectrometer specFan = null;

        // when the last trigger sent
        DateTime? lastTriggerSent = null;

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
            logger.debug("MultiChannelWrapper.ctor: start");
            reset();
            logger.debug("MultiChannelWrapper.ctor: done");
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
        ///
        /// <todo>
        /// could provide some "filter" options to let the caller determine which
        /// connected spectrometers should be considered valid channels, e.g. 
        /// regex patterns for model, serial and/or userData.
        /// </todo>
        ///
        /// <returns>true on success</returns>
        public async Task<bool> openAsync()
        {
            reset();

            var count = await driver.openAllSpectrometers();
            logger.header($"openAsync: initializing {count} spectrometers");
            for (var i = 0; i < count; i++)
            {
                var spec = driver.getSpectrometer(i);
                var sn = spec.serialNumber;

                logger.info("initializing {0} {1} with {2} pixels ({3:f2}, {4:f2})",
                    spec.model, sn, spec.pixels, spec.wavelengths[0], spec.wavelengths[spec.pixels - 1]);

                // select by default
                spec.multiChannelSelected = true;

                // to minimize USB comm collisions, automatically capture the 
                // temperature right after acquisition, so it can be later read
                // from cache if desired
                spec.readTemperatureAfterSpectrum = true;

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
                logger.debug($"  storing {sn} as position {pos}");
                specByPos[pos] = spec;
                positions.Add(pos);

                // let the Spectrometer know its own position as a 
                // convenient back-reference
                spec.multiChannelPosition = pos;
            }

            // ARM units (used for triggering) seem to benefit from a 
            // throwaway after changing integration time.  (Honestly, 
            // probably all models do.)  
            integrationThrowaways = true;

            // Default to system-wide software triggering, regardless of
            // whether we found a specTrigger or not; let the app decide to
            // override that (not all MultiChannel apps will use hardware
            // triggering).
            logger.header("openAsync: defaulting to software triggering");
            hardwareTriggeringEnabled = false;

            if (spectrometerCount <= 0)
                return logger.error("no spectrometers found");

            // take a throwaway stability spectrum 
            // we do this WITHOUT sending a "system-level" trigger (either SW or HW)
            // because at this point in initialization, the spectrometer is still in
            // the defaul software-triggered mode.  It will automatically generate a
            // SW trigger when Spectrometer.getSpectrum() is called.
            logger.header("taking initial throwaway stability spectra (SW triggered)");
            _ = await getSpectraAsync(sendTrigger: false);

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

        /// <summary>
        /// Whether external hardware triggering is currently enabled.
        /// </summary>
        ///
        /// <remarks>
        /// Note that with ARM spectrometers, whether hardware triggering is 
        /// enabled or not may seem somewhat philosophical; after all, ARM 
        /// spectrometers ALWAYS respond to both HW and SW triggers, so this 
        /// value doesn't actually change anything inside the spectrometer.
        /// 
        /// What it does is change behavior within WasatchNET, as normally a 
        /// spectrometer with TriggerSource.INTERNAL (and autoTrigger) will 
        /// automatically generate a SW /trigger whenever Spectrometer.getSpectrum() 
        /// is called.  Likewise, timeout behavior changes depending on this 
        /// setting, as a HW-triggered acquisition should generally continue to 
        /// block until a HW trigger is detected.
        ///
        /// Of course, things are actually more complicated than that, as 
        /// Spectrometer.autoTrigger also feeds into whether the spectrometer 
        /// will internally generate a SW trigger, and there are various explicit
        /// timeouts which can be applied even to HW-triggered spectrometers.
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
        /// At the moment, we have no validated way to encapsulate scan averaging
        /// within WasatchNET when using HW drivers.  Therefore, we're doing the
        /// looping and averaging in getSpectraAsync, controlled by this field.
        /// </summary>
        public uint scanAveraging 
        {
            get => _scanAveraging;
            set => _scanAveraging = Math.Max(1, value);
        }
        uint _scanAveraging = 1;

        #if false
        /// <summary>
        /// Whether scan averaging should be implemented via "continuous acquisition"
        /// within the spectrometer (realistically, the only way to support this feature
        /// while using hardware triggers).
        /// </summary>
        ///
        /// <remarks>
        /// EXPERIMENTAL -- NOT FOR USE IN PRODUCTION SOFTWARE.
        /// </remarks>
        public bool useContinuousAcquisition
        {
            get => _useContinuousAcquisition;
            set
            {
                foreach (var pair in specByPos)
                {
                    var spec = pair.Value;
                    if (!spec.multiChannelSelected)
                        continue;

                    logger.debug($"pos {pair.Key}: scanAveragingIsContinuous -> {value}");
                    spec.scanAveragingIsContinuous = value;
                }
                _useContinuousAcquisition = value;

            }
        }
        bool _useContinuousAcquisition;
        #endif

        ////////////////////////////////////////////////////////////////////////
        // Acquisition Parameters
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A convenience method to set all spectrometers in the system to a common
        /// integration time.
        /// </summary>
        ///
        /// <remarks>
        /// Would run faster if parallized with Tasks and WaitAll, but reducing
        /// opportunities for bus collisions.
        /// </remarks>
        ///
        /// <param name="ms">desired integration time</param>
        ///
        /// <returns>true on success</returns>
        public bool setIntegrationTimeMS(uint ms)
        {
            logger.header($"setting all spectrometers to {ms}ms integration time");
            foreach (var pair in specByPos)
            {
                var spec = pair.Value;
                if (!spec.multiChannelSelected)
                    continue;

                spec.acquisitionTimeoutMS = null;
                spec.integrationTimeMS = ms;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // Spectra
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Use the configured spectrometer's laserEnable output to raise a brief 
        /// HW trigger pulse.
        /// </summary>
        public async Task<bool> startAcquisitionAsync()
        {
            if (hardwareTriggeringEnabled)
            {
                if (specTrigger is null)
                {
                    logger.error("no spectrometer configured with trigger control");
                    return false;
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
                    await spec.sendSWTrigger();
                }
            }
            lastTriggerSent = DateTime.Now;
            return true;
        }

        /// <summary>
        /// Sends a hardware trigger using the configured spectrometer.
        /// </summary>
        /// <returns>true on success</returns>
        public async Task<bool> sendHWTrigger()
        {
            if (specTrigger is null)
                return false;

            logger.debug($"sending {triggerPulseWidthMS}ms HW trigger via {specTrigger.id}");
            specTrigger.laserEnabled = true;
            await Task.Delay(millisecondsDelay: triggerPulseWidthMS);
            specTrigger.laserEnabled = false;
            logger.debug("sendHWTrigger done");

            return true;
        }

        /// <summary>
        /// Provide a list of spectrometer positions, sorted in ascending order 
        /// by integration time.
        /// </summary>
        ///
        /// <remarks>
        /// This is provided so that operations which sequentially iterate over each
        /// spectrometer as they complete an acquisition can complete in the shortest
        /// possible time, without actually being parallel.
        /// </remarks>
        public List<int> positionsByIntegTime()
        {
            var sortedPos = from pair in specByPos
                            orderby pair.Value.integrationTimeMS ascending
                            select pair.Key;
            return sortedPos.ToList();
        }

        /// <summary>
        /// Get one spectrum from each populated position.
        /// </summary>
        ///
        /// <remarks>
        /// This reads one spectrum from each selected device.  The reads are 
        /// performed in serial for two reasons.  First, the USB bus is itself 
        /// physically serial, so truely parallel communication isn't technically
        /// possible.  Secondly, the attempt to interleave spectral packets over
        /// bulk endpoints, and the randomized inter-packet timing and buffer 
        /// sizing thus introduced, has been observed to confuse some hardware USB 
        /// controllers. 
        /// 
        /// Sorting by integration time should ensure total read-out time is close 
        /// to minimal.
        ///
        /// At present, all scan averaging (SW and HW triggered) is provided by 
        /// this method.
        /// </remarks>
        ///
        /// <param name="sendTrigger">whether to automatically send trigger(s) to
        ///     start the acquisitions</param>
        ///
        /// <returns>a list of ChannelSpectra in position order</returns>
        public async Task<List<ChannelSpectrum>> getSpectraAsync(bool sendTrigger=true)
        {
            uint count = 0;

            SortedDictionary<int, ChannelSpectrum> spectraByPos = new SortedDictionary<int, ChannelSpectrum>();
            do
            {
                if (sendTrigger)
                {
                    if (!await startAcquisitionAsync())
                    {
                        logger.error("Unable to start acquisition");
                        return null;
                    }
                }

                // for a slight speedup, read spectra in ascending order by
                // integration time
                foreach (var pos in positionsByIntegTime())
                {
                    var spec = specByPos[pos];
                    if (!spec.multiChannelSelected)
                        continue;

                    logger.debug($"getSpectraAsync: calling getSpectrumAsync({pos})");
                    var cs = await getSpectrumAsync(pos);
                    logger.debug($"getSpectraAsync: back from getSpectrumAsync({pos})");
                    if (cs is null)
                        continue;

                    if (spectraByPos.ContainsKey(pos))
                    {
                        // add existing intensities into the newest ChannelSpectrum
                        var old = spectraByPos[pos].intensities;
                        for (int i = 0; i < old.Length; i++)
                            cs.intensities[i] += old[i];
                        spectraByPos[pos] = cs; // keep newest (for temperature etc)
                    }
                    else
                    {
                        spectraByPos.Add(pos, cs);
                    }
                }
                count++;
                logger.debug($"getSpectraAsync: count = {count}");
            } while (count < scanAveraging);

            // re-sort results by position, for caller convenience, and divide out
            List <ChannelSpectrum> result = new List<ChannelSpectrum>();
            foreach (var pair in spectraByPos)
            {
                var cs = pair.Value;
                if (scanAveraging > 1)
                    for (int i = 0; i < cs.intensities.Length; i++)
                        cs.intensities[i] /= scanAveraging;
                result.Add(cs);
            }

            return result;
        }

        /// <summary>
        /// Get a spectrum from one spectrometer.  Does not call startAcquisition,
        /// and assumes that EITHER a hardware trigger has been initiated through
        /// other means, or that the system is in software triggered mode.
        /// </summary>
        ///
        /// <remarks>
        /// The intended use-case for "computeTimeout" is to ensure that, when 
        /// the higher-level getSpectraAsync() is called and generates a 
        /// startAcquisition trigger, we don't completely throw the notion of 
        /// "reasonable timeouts" out the window.  That is, even though 
        /// getSpectraAsync is reading spectra in serial, and therefore spectrometers
        /// further down the line will actually have had more time to complete 
        /// their acquisitions, we don't unfairly "penalize" earlier spectrometers 
        /// by holding them to strict timeouts, to which later spectrometers avoided
        /// by happenstance.  Partially this is so we can continue to use timeouts 
        /// as for their intended purpose, to correctly alert the user and system
        /// developers when a component is failing to meet specified tolerances and
        /// expectations of performance.  Timeouts are there to tell us when a 
        /// component is failing to perform as intended, and we don't want to lose
        /// that important feedback even when measurements are artificially slowed
        /// by having to read back spectra from numerous devices.
        /// </remarks>
        ///
        /// <param name="pos">spectrometer position to acquire</param>
        /// <param name="computeTimeout">compute a dynamic timeout from lastTriggerSent</param>
        public async Task<ChannelSpectrum> getSpectrumAsync(int pos, bool computeTimeout=true)
        {
            if (!specByPos.ContainsKey(pos))
                return null;
            Spectrometer spec = specByPos[pos];

            if (computeTimeout && lastTriggerSent != null)
            {
                // make some effort to enforce reasonable timeouts, ensuring all 
                // spectrometers are held to a fair standard (even those late on
                // the bus)
                logger.debug($"computing timeout for pos {pos}");
                var now = DateTime.Now;
                var elapsedMS = (now - lastTriggerSent.Value).TotalMilliseconds;
                var multiDeviceOverheadMS = 200 * driver.getNumberOfSpectrometers();
                var defaultTimeoutMS = spec.integrationTimeMS * 2 + multiDeviceOverheadMS;
                var remainingMS = defaultTimeoutMS - elapsedMS;
                var timeoutMS = Math.Max(remainingMS, multiDeviceOverheadMS);
                logger.debug($"applying timeout of {timeoutMS}");
                spec.acquisitionTimeoutMS = (uint)timeoutMS;
            }

            logger.debug($"getSpectrumAsync({pos}): calling getSpectrum");
            var intensities = await Task.Run(() => spec.getSpectrum());
            logger.debug($"getSpectrumAsync({pos}): back from getSpectrum");
            if (intensities is null)
                return null;

            ChannelSpectrum cs = new ChannelSpectrum(spec) { intensities = intensities, xAxisType = xAxisType };

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
