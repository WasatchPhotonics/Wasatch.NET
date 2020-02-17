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
        public uint targetCounts = 40000;
        public uint targetCountThreshold = 2500;
        public uint maxIterations = 20;
        public uint maxSaneIntegrationTimeMS = 10000;

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

            logger.info("starting optimization");
            await Task.Run(() => run());
            logger.info($"optimization done (status = {status}, integrationTImeMS = {spec.integrationTimeMS}");

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

            while (iterations < maxIterations)
            {
                logger.debug($"determining optimal integration time for {spec.serialNumber} (iteration {iterations}, {spec.integrationTimeMS}ms)");

                var spectrum = spec.getSpectrum();

                if (spec.shuttingDown || spectrum is null)
                {
                    status = Status.ERROR;
                    return;
                }

                peakCounts = spectrum.Max();
                logger.debug($"max = {peakCounts}");

                ////////////////////////////////////////////////////////////////
                // are we optimal?
                ////////////////////////////////////////////////////////////////

                logger.debug("PIO: checking if optimal");
                if (Math.Abs(peakCounts - targetCounts) <= targetCountThreshold)
                {
                    logger.debug($"optimized integration time for {spec.serialNumber} at {spec.integrationTimeMS}ms");
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
                logger.debug($"adjusting integration time to {ms}ms");

                // clamp
                if (ms > maxSaneIntegrationTimeMS)
                {
                    ms = maxSaneIntegrationTimeMS;
                    logger.info($"    (rounding down to {ms}ms)");
                    consecutiveRoundDowns++;
                    consecutiveRoundUps = 0;
                }
                else if (ms < spec.eeprom.minIntegrationTimeMS)
                {
                    ms = spec.eeprom.minIntegrationTimeMS;
                    logger.info($"    (rounding up to {ms}ms)");
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
                    logger.error($"failing to converge...giving up after {maxConsecutiveRounding} consecutive roundings");
                    status = Status.ERROR;
                    return;
                }
                iterations++;
            }

            logger.error($"giving up after {iterations} iterations");
            status = Status.ERROR;
        }
    }
}
