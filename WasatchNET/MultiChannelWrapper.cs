using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WasatchNET
{
    public enum X_AXIS_TYPE { PIXEL, WAVELENGTH, WAVENUMBER };

    public class ChannelSpectrum
    {
        public X_AXIS_TYPE xAxisType;
        public double[] xAxis;
        public double[] intensities;
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
        /// Whether spectra should be returned with wavenumber axis or wavelengths.
        /// </summary>
        public bool useWavenumbers = false;

        /// <summary>
        /// A convenience accessor to iterate over valid positions (channels).
        /// </summary>
        public SortedSet<int> positions { get; private set; } = new SortedSet<int>();

        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        static MultiChannelWrapper instance = null;

        Driver driver = Driver.getInstance();
        Dictionary<int, Spectrometer> specByPos;
        Logger logger = Logger.getInstance();

        Spectrometer specTrigger = null;
        Spectrometer specFan = null;


        static object mut = new object();

        const int TRIGGER_PULSE_WIDTH_MS = 50;

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

        public int spectrometerCount => specByPos.Count;

        public bool open()
        {
            reset();

            var count = driver.openAllSpectrometers();
            logger.info($"found {count} spectrometers");
            for (var i = 0; i < count; i++)
            {
                var spec = driver.getSpectrometer(i);
                var sn = spec.serialNumber;

                logger.info($"found {spec.model} {sn} with {spec.pixels} pixels ({0:f2}, {1:f2})", 
                    spec.wavelengths[0], spec.wavelengths[spec.pixels-1]);

                ////////////////////////////////////////////////////////////////
                // Parse EEPROM.userText
                ////////////////////////////////////////////////////////////////

                var pos = spectrometerCount;

                var featureList = spec.eeprom.userText;
                var features = featureList.Split(';');

                foreach (var feature in features)
                {
                    var pair = feature.Split('=');
                    var key = pair[0].ToLower().Trim();
                    var value = pair[1].ToLower().Trim();

                    if (key.Contains("pos"))
                    {
                        if (Int32.TryParse(value, out pos))
                            logger.info($"{sn} has position {pos}");
                        else
                            logger.error($"{sn}: failed to parse {feature}");
                    }
                    else if (key == "feature")
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
                            logger.error($"unsupported value: {feature}");
                        }
                    }
                    else
                    {
                        logger.error($"unsupported feature: {feature}");
                    }
                }

                // make sure we have no duplicates
                while (specByPos.ContainsKey(pos))
                    pos++;

                logger.info($"storing {sn} as position {pos}");
                specByPos[pos] = spec;
                spec.multiChannelPosition = pos;
            }

            // default to internal triggering
            triggeringEnabled = false;

            return spectrometerCount > 0;
        }

        public void close()
        {
            driver.closeAllSpectrometers();
            reset();
        }

        void reset()
        {
            specByPos = new Dictionary<int, Spectrometer>();
            positions = new SortedSet<int>();
            specTrigger = null;
            specFan = null;
        }

        public int getFanPos() { return specFan != null ? specFan.multiChannelPosition : -1; }
        public int getTriggerPos() { return specTrigger != null ? specTrigger.multiChannelPosition : -1; }

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

        public bool triggeringEnabled
        {
            get => _triggeringEnabled; 
            set
            {
                foreach (var pair in specByPos)
                {
                    logger.debug($"pos {pair.Key}: triggerEnabled -> {value}");
                    pair.Value.triggerSource = value ? TRIGGER_SOURCE.EXTERNAL : TRIGGER_SOURCE.INTERNAL;
                }
                _triggeringEnabled = value;
            }
        }
        bool _triggeringEnabled;

        /// <summary>
        /// Lets the caller directly control the spectrometer at a given position.
        /// </summary>
        /// <param name="pos">Which channel</param>
        public Spectrometer getSpectrometer(int pos)
        {
            return specByPos.ContainsKey(pos) ? specByPos[pos] : null;
        }

        /// <summary>
        /// Use the configured spectrometer's laserEnable output to raise a brief 
        /// HW trigger pulse.
        /// </summary>
        public async void startAcquisition()
        {
            if (specTrigger is null)
            {
                logger.error("startAcquisition: no spectrometer configured with trigger control");
                return;
            }

            logger.debug("triggerPulse: high");
            specTrigger.laserEnabled = true;

            logger.debug("triggerPulse: wait");
            await Task.Delay(TRIGGER_PULSE_WIDTH_MS);

            logger.debug("triggerPulse: low");
            specTrigger.laserEnabled = false;
        }

        /// <summary>
        /// Get spectra from each populated position.
        /// </summary>
        /// <returns>
        /// A map of spectra for each position.  Each spectrum is itself a tuple of x- and y-axes.  
        /// Choice of x-axis is determined by useWavenumbers.
        /// </returns>
        /// <remarks>
        /// This is not executed in parallel because it doesn't need to be.
        /// The USB bus is serial anyway, and communication latencies are minimal
        /// compared to integration time.  Sorting by integration time should ensure
        /// total acquisition time is close to minimal.
        /// </remarks>
        public Dictionary<int, ChannelSpectrum> getSpectra()
        {
            Dictionary<int, ChannelSpectrum> results = new Dictionary<int, ChannelSpectrum>();

            // sort by integration time
            var sortedSpectrometers = from pair in specByPos
                                      orderby pair.Value.integrationTimeMS ascending
                                      select pair;

            foreach (KeyValuePair<int, Spectrometer> pair in sortedSpectrometers)
            {
                int pos = pair.Key;
                Spectrometer spec = pair.Value;

                logger.debug($"getting spectrum from pos {pos} {spec.serialNumber}");

                ChannelSpectrum cs = new ChannelSpectrum();
                cs.xAxisType = useWavenumbers ? X_AXIS_TYPE.WAVENUMBER : X_AXIS_TYPE.WAVELENGTH;
                cs.xAxis = useWavenumbers ? spec.wavenumbers : spec.wavelengths;
                cs.intensities = spec.getSpectrum();

                results.Add(pos, cs);
            }
            logger.info($"returning {results.Count} spectra");
            return results;
        }

        public ChannelSpectrum getSpectrum(int pos)
        {
            if (!specByPos.ContainsKey(pos))
                return null;

            Spectrometer spec = specByPos[pos];

            ChannelSpectrum cs = new ChannelSpectrum();
            cs.xAxisType = useWavenumbers ? X_AXIS_TYPE.WAVENUMBER : X_AXIS_TYPE.WAVELENGTH;
            cs.xAxis = useWavenumbers ? spec.wavenumbers : spec.wavelengths;
            cs.intensities = spec.getSpectrum();
            return cs;
        }
    }
}
