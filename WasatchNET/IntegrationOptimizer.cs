using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// This class is similar to the PeakIntegrationOptimizer (PIO) in WPSpecCal,
    /// but simpler, as it only optimizes "full spectra" (uses the maximum height
    /// of the entire spectrum), rather than allowing individual peaks to be optimized
    /// to target levels.
    /// </remarks>
    public class IntegrationOptimizer
    {
        public enum Status { PENDING, SUCCESS, ERROR }

        public Status status = Status.PENDING;
        public int targetCounts = 40000;
        public int targetCountThreshold = 2500;
        public uint maxIterations = 20;
        public uint maxSaneIntegrationTimeMS = 10000;
        public uint startMS = 10;

        public Spectrometer spec;
        Logger logger = Logger.getInstance();

        public IntegrationOptimizer(Spectrometer spec)
        {
            this.spec = spec;
        }

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

        void run()
        { 
            double peakCounts = 0;
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

                peakCounts = spectrum.Max();
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
