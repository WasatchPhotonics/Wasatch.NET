using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    public class MultiChannelWrapper
    {
        static MultiChannelWrapper instance = null;

        Driver driver = Driver.getInstance();
        Dictionary<int, Spectrometer> specByPos;
        Logger logger = Logger.getInstance();

        Spectrometer specTrigger = null;
        Spectrometer specFan = null;

        public bool useWavenumbers = false;
        public SortedSet<int> positions { get; private set; } = new SortedSet<int>();

        static object mut = new object();


        public static MultiChannelWrapper getInstance()
        {
            lock (mut)
            {
                if (instance == null)
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

            int count = driver.openAllSpectrometers();
            logger.info("found {0} spectrometers", count);
            for (int i = 0; i < count; i++)
            {
                Spectrometer spec = driver.getSpectrometer(i);

                logger.info("found {0} {1} with {2} pixels ({3:f2}, {4:f2})", 
                    spec.model, spec.serialNumber, spec.pixels, spec.wavelengths[0], spec.wavelengths[spec.pixels-1]);

                ////////////////////////////////////////////////////////////////
                // Parse EEPROM.userText
                ////////////////////////////////////////////////////////////////

                int pos = spectrometerCount;

                String featureList = spec.eeprom.userText;
                String[] features = featureList.Split(';');

                foreach (String feature in features)
                {
                    String[] pair = feature.Split('=');
                    String key = pair[0].ToLower().Trim();
                    String value = pair[1].ToLower().Trim();

                    if (key.Contains("pos"))
                    {
                        if (Int32.TryParse(value, out pos))
                            logger.info("{0} has position {1}", spec.serialNumber, pos);
                        else
                            logger.error("failed to parse {0}", feature);
                    }
                    else if (key == "feature")
                    {
                        if (value.Contains("trigger"))
                        {
                            logger.info("{0} has trigger feature", spec.serialNumber);
                            specTrigger = spec;
                        }
                        else if (value.Contains("fan"))
                        {
                            logger.info("{0} has fan feature", spec.serialNumber);
                            specFan = spec;
                        }
                        else
                        {
                            logger.error("unsupported value: {0}", feature);
                        }
                    }
                    else
                    {
                        logger.error("unsupported feature: {0}", feature);
                    }
                }

                // make sure we have no duplicates
                while (specByPos.ContainsKey(pos))
                    pos++;

                logger.info("storing {0} as position {1}", spec.serialNumber, pos);
                specByPos[pos] = spec;
                spec.multiChannelPosition = pos;
            }

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
        /// <remarks>Obviously dangerous on spectrometers with physical fans -- should move to GPIO in future.</remarks>
        public bool fanEnabled
        {
            get
            {
                if (specFan == null)
                {
                    logger.error("no spectrometer configured with fan control");
                    return false;
                }
                return specFan.laserEnabled;
            }

            set
            {
                if (specFan == null)
                {
                    logger.error("no spectrometer configured with fan control");
                    return;
                }
                specFan.laserEnabled = value;
            }
        }

        /// <summary>
        /// Use the configured spectrometer's laserEnable output to raise a trigger event.
        /// </summary>
        /// <param name="flag">new fan setting</param>
        /// <remarks>Obviously dangerous on spectrometers with physical fans -- should move to GPIO in future.</remarks>
        void startAcquisition()
        {
            if (specTrigger == null)
            {
                logger.error("no spectrometer configured with trigger control");
                return;
            }
            specTrigger.laserEnabled = true;

            // YOU ARE HERE -- disable laserEnabled after NNN millisec
        }

        Dictionary<int, Tuple<double[], double[]>> getSpectra()
        {
            Dictionary<int, Tuple<double[], double[]>> results = new Dictionary<int, Tuple<double[], double[]>>();
            foreach (KeyValuePair<int, Spectrometer> pair in specByPos)
            {
                int pos = pair.Key;
                Spectrometer spec = pair.Value;

                logger.debug("getting spectrum from pos {0} {1}", pos, spec.serialNumber);

                double[] xAxis = useWavenumbers ? spec.wavenumbers : spec.wavelengths;
                double[] spectrum = spec.getSpectrum();

                results.Add(pos, new Tuple<double[], double[]>(xAxis, spectrum));
            }
            logger.info("returning {0} spectra", results.Count);
            return results;
        }
    }
}
