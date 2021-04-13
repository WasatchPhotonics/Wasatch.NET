using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    /// <summary>
    /// Controls the automatic (background) optimization of a spectrometer's integration time
    /// to within 'targetCountThreshold' counts of 'targetCounts' intensity goal.
    /// </summary>
    /// <remarks>
    /// This class is similar to the PeakIntegrationOptimizer (PIO) in WPSpecCal,
    /// but simpler, as it only optimizes "full spectra" (uses the maximum height
    /// of the entire spectrum), rather than allowing individual peaks to be optimized
    /// to target levels.
    ///
    /// The typical use-case would be to instantiate the IntegrationOptimizer
    /// with a reference to the spectrometer you want to optimize, then tweak
    /// the values of any public parameters (targetCounts, targetCountThreshold, 
    /// maxIterations, maxSaneItegrationTimeMS, startMS etc) you wish.  Then
    /// call start(), and internally poll the object until status no longer reads
    /// PENDING (switches to either SUCCESS or ERROR).  At that time, you can read
    /// the Spectrometer's integration time to obtain the converged value.
    ///
    /// While polling, you can graph the intermediate spectra if you wish, using
    /// Spectrometer.lastSpectrum.  See MultiChannelDemo.Form1.graphIntegrationSpectra 
    /// for examples.
    /// </remarks>
    public class IntegrationOptimizer
    {
        public enum Status { PENDING, SUCCESS, ERROR }

        /// <summary>
        /// Current (or final) status of the IntegrationOptimizer.  Defaults to 
        /// PENDING at construction, and switches to SUCCESS or ERROR only at 
        /// completion.
        /// </summary>
        public Status status = Status.PENDING;

        /// <summary>the intensity you want to reach</summary>
        public int targetCounts = 40000;

        /// <summary>error margin around the targetCounts intensity</summary>
        public int targetCountThreshold = 2500;

        /// <summary>how many acquisitions the algorithm can attempt</summary>
        public uint maxIterations = 20;

        /// <summary>longest integration time you're willing to consider</summary>
        public uint maxSaneIntegrationTimeMS = 10000;

        /// <summary>initial integration time for first iteration</summary>
        public uint startMS = 10;

        /// <summary>the Spectrometer being optimized</summary>
        public Spectrometer spec;
        Logger logger = Logger.getInstance();

        /// <summary>
        /// Instantiate a new IntegrationOptimizer on a particular Spectrometer.
        /// </summary>
        /// <param name="spec">the Spectrometer whose integration time should be optimized</param>
        public IntegrationOptimizer(Spectrometer spec)
        {
            this.spec = spec;
        }

        /// <summary>
        /// Start the background integration.  Note that this will generate a series of
        /// integrations in a background thread.  Spectrometer communications during an
        /// ongoing optimization may be compromised and parallel activity could yield
        /// unpredictable results.
        /// </summary>
        /// <todo>
        /// consider an additional lock, beyond acquisitionLock, to preclude user
        /// commands during internal activities
        /// </todo>
        public async void start()
        {
            spec.dark = null;
            var saveAvg = spec.scanAveraging;
            if (saveAvg > 1)
                spec.scanAveraging = 1;

            var sn = spec.serialNumber;

            logger.debug($"{sn}: starting optimization");
            await Task.Run(() => run());
            logger.debug($"{sn}: optimization done (status = {status}, integrationTimeMS = {spec.integrationTimeMS}");

            if (saveAvg > 1)
                spec.scanAveraging = saveAvg;
        }

        /// <summary>
        /// If we are "continuously optimizing" integration time to hold at a 
        /// configured peak intensity, use the latest spectrum to adjust 
        /// integration time.
        /// </summary>
        /// <returns>true if integration time optimal, false if otherwise</returns>
        public bool process()
        { 
            status = Status.PENDING;
            var sn = spec.serialNumber;
            var spectrum = spec.lastSpectrum;

            if (spec.shuttingDown || spectrum is null)
            {
                status = Status.ERROR;
                return false;
            }

            double peakCounts = spectrum.Max();

            ////////////////////////////////////////////////////////////////
            // are we optimal?
            ////////////////////////////////////////////////////////////////

            if (Math.Abs(peakCounts - targetCounts) <= targetCountThreshold)
            {
                logger.debug($"{sn}:   integration time at {spec.integrationTimeMS}ms is optimal (max {peakCounts}, target {targetCounts}, threshold {targetCountThreshold})");
                status = Status.SUCCESS;
                return true;
            }

            ////////////////////////////////////////////////////////////////
            // not yet optimal, so adjust
            ////////////////////////////////////////////////////////////////

            var ms = spec.integrationTimeMS;
            if (peakCounts > targetCounts)
                ms /= 2;
            else
            {
                var tmp = (uint) Math.Round(ms * targetCounts / peakCounts);
                if (tmp == ms)
                    tmp++;
                ms = tmp;
            }
            logger.debug($"{sn}:   scaling integration time to {ms}");

            // clamp
            if (ms > maxSaneIntegrationTimeMS)
            {
                logger.debug($"{sn}:   rounding {ms} down to {maxSaneIntegrationTimeMS}");
                ms = maxSaneIntegrationTimeMS;
            }
            else if (ms < spec.eeprom.minIntegrationTimeMS)
            {
                logger.debug($"{sn}:   rounding {ms} up to {spec.eeprom.minIntegrationTimeMS}");
                ms = spec.eeprom.minIntegrationTimeMS;
            }

            spec.integrationTimeMS = ms;
            return false;
        }

        void run()
        { 
            int iterations = 0;
            int consecutiveRoundUps = 0;
            int consecutiveRoundDowns = 0;
            int maxConsecutiveRounding = 2;

            status = Status.PENDING;
            spec.integrationTimeMS = startMS;
            var sn = spec.serialNumber;

            while (iterations < maxIterations)
            {
                logger.debug($"{sn}: iteration = {iterations}, ms = {spec.integrationTimeMS})");

                var spectrum = spec.getSpectrum();

                if (spec.shuttingDown || spectrum is null)
                {
                    status = Status.ERROR;
                    return;
                }

                double peakCounts = spectrum.Max();
                logger.debug($"{sn}:   max = {peakCounts}");

                ////////////////////////////////////////////////////////////////
                // are we optimal?
                ////////////////////////////////////////////////////////////////

                if (Math.Abs(peakCounts - targetCounts) <= targetCountThreshold)
                {
                    logger.debug($"{sn}:   optimized integration time at {spec.integrationTimeMS}ms (max {peakCounts}, target {targetCounts}, threshold {targetCountThreshold})");
                    status = Status.SUCCESS;
                    return;
                }

                ////////////////////////////////////////////////////////////////
                // not yet optimal, so adjust
                ////////////////////////////////////////////////////////////////

                var ms = spec.integrationTimeMS;
                if (peakCounts > targetCounts)
                    ms /= 2;
                else
                {
                    var tmp = (uint) Math.Round(ms * targetCounts / peakCounts);
                    if (tmp == ms)
                        tmp++;
                    ms = tmp;
                }
                logger.debug($"{sn}:   scaling integration time to {ms}");

                // clamp
                if (ms > maxSaneIntegrationTimeMS)
                {
                    logger.debug($"{sn}:   rounding {ms} down to {maxSaneIntegrationTimeMS}");
                    ms = maxSaneIntegrationTimeMS;
                    consecutiveRoundDowns++;
                    consecutiveRoundUps = 0;
                }
                else if (ms < spec.eeprom.minIntegrationTimeMS)
                {
                    logger.debug($"{sn}:   rounding {ms} up to {spec.eeprom.minIntegrationTimeMS}");
                    ms = spec.eeprom.minIntegrationTimeMS;
                    consecutiveRoundUps++;
                    consecutiveRoundDowns = 0;
                }
                else
                {
                    consecutiveRoundDowns = 0;
                    consecutiveRoundUps = 0;
                }

                // detect head-banging
                if (Math.Max(consecutiveRoundUps, consecutiveRoundDowns) > maxConsecutiveRounding)
                {
                    logger.error($"{sn}:    failing to converge...giving up after {maxConsecutiveRounding} consecutive roundings");
                    status = Status.ERROR;
                    return;
                }

                spec.integrationTimeMS = ms;
                iterations++;
            }

            logger.error($"{sn}: gave up after {iterations} iterations");
            status = Status.ERROR;
        }
    }
}
