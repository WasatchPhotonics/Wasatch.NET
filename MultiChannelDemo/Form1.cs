using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Text.RegularExpressions;

using WasatchNET;

namespace MultiChannelDemo
{
    public partial class Form1 : Form
    {
        MultiChannelWrapper wrapper = MultiChannelWrapper.getInstance();
        Logger logger = Logger.getInstance();

        bool initialized = false;

        // note these are zero-indexed
        Chart[] charts;
        GroupBox[] groupBoxes;
        CheckBox[] checkBoxes;

        Dictionary<int, Series> seriesCombined = new Dictionary<int, Series>();
        Dictionary<int, Series> seriesTime = new Dictionary<int, Series>();

        // whatever was last returned by "Acquire All", "Take Darks" or "Take References"
        // (added so we'd have persisted data to write when user clicked "Save")
        List<ChannelSpectrum> lastSpectra;

        // Monte Carlo 
        BackgroundWorker workerBatch;
        Dictionary<int, int> acquisitionCounts;
        Dictionary<int, int> secondTriggerCounts;
        Random r = new Random();
        string batchPathname;
        string talliesPathname;
        uint integrationTimeMSRandomMin = 100;
        uint integrationTimeMSRandomMax = 500;
        uint maxPulseWidthMS = 25; 
        bool batchRunning;
        bool batchShuttingDown;
        int triggerCount;
        int readFailureCount;
        bool doubleTriggerAttemptRead;
        int WORKER_PERIOD_MS = 2000;

        // place to store intensities by integration time
        // tallies[pos][integTimeMS].{total, count}
        SortedDictionary<int, SortedDictionary<uint, Tally>> tallies;

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public Form1()
        {
            InitializeComponent();

            Thread.CurrentThread.Name = "MainProcess";

            Text = String.Format("MultiChannelDemo v{0}", Application.ProductVersion);

            logger.setTextBox(textBoxEventLog);

            // note all are 1-indexed
            charts = new Chart[] { 
                chart1, chart2, chart3, chart4, 
                chart5, chart6, chart7, chart8 };
            groupBoxes = new GroupBox[] { 
                groupBoxPos1, groupBoxPos2, groupBoxPos3, groupBoxPos4, 
                groupBoxPos5, groupBoxPos6, groupBoxPos7, groupBoxPos8 };
            checkBoxes = new CheckBox[] {
                checkBoxPos1, checkBoxPos2, checkBoxPos3, checkBoxPos4, 
                checkBoxPos5, checkBoxPos6, checkBoxPos7, checkBoxPos8 };

            // initialize widgets
            for (int i = 0; i < 8; i++)
            {
                groupBoxes[i].Enabled = false;
                checkBoxes[i].Checked = false;
            }

            // initialize charts
            initChart(chartAll);
            foreach (var chart in charts)
                initChart(chart);

            workerBatch = new BackgroundWorker() { WorkerSupportsCancellation = true };
            workerBatch.DoWork             += backgroundWorkerBatch_DoWork;
            workerBatch.RunWorkerCompleted += backgroundWorkerBatch_RunWorkerCompleted;

            AcceptButton = buttonInit;

            groupBoxBatch.Enabled = false;

            maxPulseWidthMS = 25; // (uint)(0.25 * integrationTimeMSRandomMin);
        }

        void initChart(Chart chart, string xAxisLabelFormat = "{0:f2}")
        {
            var area = chart.ChartAreas[0];
            area.AxisY.IsStartedFromZero = false;
            area.AxisX.LabelStyle.Format = xAxisLabelFormat;
        }

        async void buttonInit_Click(object sender, EventArgs e)
        {
            if (initialized)
                return;

            logger.header("Initializing");

            if (! await wrapper.openAsync())
            {
                logger.error("failed to open MultiChannelWrapper");
                return;
            }

            // enable hardware triggering by default IF WE FOUND a trigger spectrometer
            if (wrapper.triggerPos > -1)
            {
                logger.debug("found a spectrometer with trigger control, so enabling hardware triggering");
                checkBoxTriggerEnableAll.Checked = true;
            }

            // per-channel initialization
            chartAll.Series.Clear();
            foreach (var pos in wrapper.positions)
            {
                var spec = wrapper.getSpectrometer(pos);
                var sn = spec.serialNumber;

                // spec.featureIdentification.usbDelayMS = 200;

                groupBoxes[pos-1].Enabled = true;
                checkBoxes[pos-1].Checked = true;

                // big graph
                var s = new Series($"Pos {pos} ({sn})");
                s.ChartType = SeriesChartType.Line;
                seriesCombined[pos - 1] = s;
                chartAll.Series.Add(s);

                logger.info("Pos {0} ({1}) wavecal: {2}",
                    pos,
                    spec.serialNumber,
                    string.Join<float>(", ", spec.eeprom.wavecalCoeffs));
            }
            updateGroupBoxTitles();

            // turn on the fan, if there is one
            if (wrapper.fanPos > -1)
            {
                logger.debug("found a spectrometer with fan control, so enabling that");
                checkBoxFanEnable.Checked = true;
            }
            else
                checkBoxFanEnable.Enabled = false;

            logger.header("Initialization Complete");

            initialized = true;
            buttonInit.Enabled = false;
            AcceptButton = buttonAcquireAll;

            groupBoxBatch.Enabled = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e) => logger.setTextBox(null);

        ////////////////////////////////////////////////////////////////////////
        // Utility methods
        ////////////////////////////////////////////////////////////////////////

        // user dis/enabled verbose logging
        private void checkBoxVerbose_CheckedChanged(object sender, EventArgs e) => logger.level = checkBoxVerbose.Checked ? LogLevel.DEBUG : LogLevel.INFO;

        void enableControls(bool flag)
        {
            groupBoxSystem.Enabled = 
            groupBoxBatch.Enabled = flag;
        }

        ////////////////////////////////////////////////////////////////////////
        // Spectrometer Selection
        ////////////////////////////////////////////////////////////////////////

        // user unselected all spectrometers
        private void buttonUnselectAll_Click(object sender, EventArgs e)
        {
            foreach (var cb in checkBoxes)
                cb.Checked = false;
        }

        private void checkBoxPosX_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = sender as CheckBox;

            // take position from last character of control name
            int pos = int.Parse(cb.Name.Substring(cb.Name.Length - 1, 1));
            var spec = wrapper.getSpectrometer(pos);
            if (spec != null)
                spec.multiChannelSelected = cb.Checked;
        }

        void updateGroupBoxTitles(List<ChannelSpectrum> spectra=null)
        {
            if (spectra is null)
            {
                // we weren't passed any spectra, so don't know temperature, so 
                // dynamically query them
                foreach (var pos in wrapper.positions)
                {
                    var spec = wrapper.getSpectrometer(pos);
                    var gb = groupBoxes[pos - 1];
                    gb.Text = string.Format("Position {0} ({1}) {2}ms {3:f2}°C",
                        pos, 
                        spec.serialNumber, 
                        spec.integrationTimeMS, 
                        spec.detectorTemperatureDegC);
                }
            }
            else
            {
                // we were passed ChannelSpectra, which already contains temperature,
                // so use that
                foreach (var cs in spectra)
                {
                    var pos = cs.pos;
                    var spec = wrapper.getSpectrometer(pos);
                    var gb = groupBoxes[pos - 1];
                    gb.Text = string.Format("Position {0} ({1}) {2}ms {3:f2}°C",
                        pos, 
                        spec.serialNumber, 
                        spec.integrationTimeMS, 
                        cs.detectorTemperatureDegC);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // System-Level Control (all spectrometers)
        ////////////////////////////////////////////////////////////////////////

        // user changed trigger pulse width
        private void numericUpDownTriggerWidthMS_ValueChanged(object sender, EventArgs e)
        {
            var ms = (int)numericUpDownTriggerWidthMS.Value;
            logger.info($"Trigger pulse width now {ms}ms");
            wrapper.triggerPulseWidthMS = ms;
        }

        // user clicked the "enable external hardware triggering" system checkbox 
        // (this is now automatically done at initialization)
        void checkBoxTriggerEnableAll_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = checkBoxTriggerEnableAll.Checked;
            logger.header("Hardware Triggering {0}", enabled ? "enabled" : "disabled");
            wrapper.hardwareTriggeringEnabled = enabled;
        }

        // user clicked the "fan control" system checkbox
        void checkBoxFanEnable_CheckedChanged(object sender, EventArgs e)
        {
            wrapper.fanEnabled = checkBoxFanEnable.Checked;
        }

        // user clicked the "enable reflectance" system checkbox
        private void checkBoxReflectanceEnabled_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = checkBoxReflectanceEnabled.Checked;
            wrapper.reflectanceEnabled =
                buttonTakeRefs.Enabled = 
                buttonClearRefs.Enabled = enabled;
        }

        // user clicked the system-level "Acquire" button, so take a spectrum
        // from every channel
        async void buttonAcquireAll_Click(object sender, EventArgs e)
        {
            logger.header("User clicked Acquire");

            enableControls(false);
            lastSpectra = await wrapper.getSpectraAsync();
            processSpectra(lastSpectra);
            enableControls(true);

            logger.header("Acquire complete");
        }

        // user clicked the system-level "Take Dark" button, so take a spectrum
        // from every channel, and save them as "dark"
        async void buttonTakeDark_Click(object sender, EventArgs e)
        {
            enableControls(false);
            lastSpectra = await wrapper.takeDarkAsync();
            processSpectra(lastSpectra);

            // only allow references after dark correction
            buttonTakeRefs.Enabled = true;

            enableControls(true);
        }

        // user clicked the "Clear Darks" button, so clear all darks
        private void buttonClearDark_Click(object sender, EventArgs e)
        {
            wrapper.clearDark();
            buttonTakeRefs.Enabled = false;
        }

        // user clicked the "Take References" button, so take a spectrum
        // from each channel and save as Reference
        async void buttonTakeRefs_Click(object sender, EventArgs e)
        {
            enableControls(false);
            lastSpectra = await wrapper.takeReferenceAsync();
            processSpectra(lastSpectra);

            // we have references, so can now compute reflectance
            checkBoxReflectanceEnabled.Enabled = true;

            enableControls(true);
        }

        // User clicked "Clear References" button, so clear all references
        private void buttonClearRefs_Click(object sender, EventArgs e)
        {
            wrapper.clearReference();
            checkBoxReflectanceEnabled.Enabled = false;
            checkBoxReflectanceEnabled.Checked = false;
        }

        // user set all spectrometers to one integration time
        void numericUpDownIntegrationTimeMS_ValueChanged(object sender, EventArgs e)
        {
            uint ms = (uint)numericUpDownIntegrationTimeMS.Value;

            enableControls(false);
            wrapper.setIntegrationTimeMS(ms);
            updateGroupBoxTitles();
            enableControls(true);
        }

        // user changed scan averaging, so set in each spectrometer
        private void numericUpDownScansToAverage_ValueChanged(object sender, EventArgs e)
        {
            uint value = (uint)numericUpDownScansToAverage.Value;
            foreach (var pos in wrapper.positions)
            {
                var spec = wrapper.getSpectrometer(pos);
                spec.scanAveraging = value;
            }
        }

        /// <summary>
        /// The user clicked the button to optimize integration time for all units.
        /// </summary>
        async void buttonOptimizeAll_Click(object sender, EventArgs e)
        {
            logger.header("User clicked Optimize");

            enableControls(false);

            // kick-off background optimizers
            List<IntegrationOptimizer> intOpts = new List<IntegrationOptimizer>();
            foreach (var pos in wrapper.positions)
            {
                if (!checkBoxes[pos - 1].Checked)
                    continue;

                var spec = wrapper.getSpectrometer(pos);
                IntegrationOptimizer intOpt = new IntegrationOptimizer(spec);
                intOpt.targetCounts = (int)numericUpDownOptimizeTarget.Value;
                intOpt.targetCountThreshold = (int)numericUpDownOptimizeThreshold.Value;
                intOpt.start();
                intOpts.Add(intOpt);
            }

            // graph optimizers while they run
            await Task.Run(() => graphActiveOptimizers(intOpts));

            // reports results
            SortedSet<int> failedChannels = new SortedSet<int>();
            foreach (var intOpt in intOpts)
                if (intOpt.status != IntegrationOptimizer.Status.SUCCESS)
                    failedChannels.Add(intOpt.spec.multiChannelPosition);

            if (failedChannels.Count == 0)
            {
                logger.info("Optimization successful");
                foreach (var intOpt in intOpts)
                    logger.info($"  pos {intOpt.spec.multiChannelPosition} {intOpt.spec.serialNumber} integration time {intOpt.spec.integrationTimeMS}ms");
            }
            else
                logger.error("Optimization FAILED on positions {0}", string.Join<int>(", ", failedChannels));

            enableControls(true);

            logger.header("Optimize complete");
        }

        ////////////////////////////////////////////////////////////////////////
        // Graphing
        ////////////////////////////////////////////////////////////////////////

        // Although this method isn't marked async, it does get called via a 
        // BeginInvoke delegate from BatchWorker, which means it _does_ get run
        // asynchronously, and IN PARALLEL with anything else going on in the
        // worker.  Therefore, avoid talking over USB from this or subordinate
        // methods (which is why we pass spectra to updateGroupBoxTitles).
        bool processSpectra(List<ChannelSpectrum> spectra)
        {
            if (spectra is null)
                return false;

            foreach (var cs in spectra)
                processSpectrum(cs);

            updateGroupBoxTitles(spectra);

            return true;
        }

        // Do whatever we're gonna do with new spectra (save, dark-correct, ...).
        // Since we encapsulated dark subtraction in WasatchNET.Spectrometer, and
        // reflectance in MultiChannelWrapper, there's not a lot to do here.
        void processSpectrum(ChannelSpectrum cs)
        {
            if (cs is null || cs.intensities is null)
                return;

            updateChartAll(cs);
            updateChartSmall(cs);
        }

        // update the little graph
        void updateChartSmall(ChannelSpectrum cs)
        {
            var chart = charts[cs.pos - 1];
            var s = chart.Series[0];
            chart.BeginInvoke(new MethodInvoker(delegate { s.Points.DataBindXY(cs.xAxis, cs.intensities); }));
        }

        // update the big graph
        void updateChartAll(ChannelSpectrum cs)
        {
            Series s = null;
            seriesCombined.TryGetValue(cs.pos - 1, out s);
            if (s != null)
                chartAll.BeginInvoke(new MethodInvoker(delegate { s.Points.DataBindXY(cs.xAxis, cs.intensities); }));
        }

        /// <summary>
        /// Provide a visual monitor of one or more IntegrationOptimizers as
        /// they optimize integration time on various spectrometers.
        /// </summary>
        /// <remarks>
        /// It is assumed that this will be called as a background task via 
        /// 'await', hence uses delegates to graph the collected spectra.
        ///
        /// Returns when optimization is finished on all spectrometers (successful
        /// or otherwise).
        /// </remarks>
        void graphActiveOptimizers(List<IntegrationOptimizer> intOpts)
        {
            if (intOpts is null)
                return;
            
            // loop until all optimizers are complete (no longer PENDING)
            while (true)
            {
                // these are the spectra we're going to graph
                var spectra = new List<ChannelSpectrum>();

                // fill the list with the latest spectra from each incomplete optimizer
                foreach (var intOpt in intOpts)
                    if (intOpt.status == IntegrationOptimizer.Status.PENDING)
                        spectra.Add(new ChannelSpectrum(intOpt.spec));

                // exit when all optimizers have finished
                if (spectra.Count == 0)
                    break;

                chartAll.BeginInvoke(new MethodInvoker(delegate { processSpectra(spectra); }));
                Thread.Sleep(100);
            }

            // just to make things pretty, one final update with the latest 
            // spectrum from every optimizer
            var final = new List<ChannelSpectrum>();
            foreach (var intOpt in intOpts)
                final.Add(new ChannelSpectrum(intOpt.spec));
            chartAll.BeginInvoke(new MethodInvoker(delegate { processSpectra(final); }));
        }

        ////////////////////////////////////////////////////////////////////////
        // Saving
        ////////////////////////////////////////////////////////////////////////

        // The user clicked the "enable interpolation" checkbox
        private void checkBoxInterpolate_CheckedChanged(object sender, EventArgs e)
        {
            groupBoxInterpolation.Enabled = checkBoxInterpolate.Checked;
        }

        /// <summary>
        /// Write spectra by pixel or interpolated x-axis.
        /// </summary>
        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (lastSpectra is null)
                return;

            DialogResult result = saveFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            var pathname = saveFileDialog.FileName;
            using (StreamWriter sw = new StreamWriter(pathname))
            {
                if (checkBoxInterpolate.Checked)
                    saveInterpolated(sw);
                else
                    saveByPixel(sw);
            }
            logger.info($"saved {pathname}");
        }

        // Note that interpolation uses whatever x-axis is in effect
        // (wavelengths or wavenumbers).  It is ASSUMED that all channels
        // will use the same x-axis.
        void saveInterpolated(StreamWriter sw)
        {
            ////////////////////////////////////////////////////////////////////
            // Generate interpolated x-axis
            ////////////////////////////////////////////////////////////////////

            double min = (double)numericUpDownInterpMin.Value;
            double max = (double)numericUpDownInterpMax.Value;
            double inc = (double)numericUpDownInterpStep.Value;

            int steps = (int)Math.Floor((max - min) / inc);
            double[] xAxis = new double[steps];
            for (int i = 0; i < steps; i++)
                xAxis[i] = min + i * inc;

            ////////////////////////////////////////////////////////////////////
            // Interpolate spectra
            ////////////////////////////////////////////////////////////////////

            Dictionary<int, double[]> interpolatedSpectra = new Dictionary<int, double[]>();
            foreach (var cs in lastSpectra)
            {
                var interpolatedSpectrum = WasatchMath.NumericalMethods.interpolateSpectrumLagrange(
                    cs.xAxis, cs.intensities, xAxis, order: 3, trim: true);
                interpolatedSpectra.Add(cs.pos, interpolatedSpectrum);
            }

            ////////////////////////////////////////////////////////////////////
            // Save file
            ////////////////////////////////////////////////////////////////////

            sw.Write("Wavelength");
            foreach (var cs in lastSpectra)
                sw.Write($", Pos {cs.pos}");
            sw.WriteLine();
            
            for (int i = 0; i < steps; i++)
            {
                double x = xAxis[i];
                sw.Write("{x:f2}");
                foreach (var cs in lastSpectra)
                {
                    double intensity = interpolatedSpectra[cs.pos][i];
                    sw.Write($", {intensity:f2}");
                }
                sw.WriteLine();
            }
        }

        void saveByPixel(StreamWriter sw)
        {
            sw.Write("Pixel");
            foreach (var cs in lastSpectra)
                sw.Write($", Pos {cs.pos}");
            sw.WriteLine();

            int pixels = lastSpectra[0].intensities.Length;
            for (int i = 0; i < pixels; i++)
            {
                sw.Write(i);
                foreach (var cs in lastSpectra)
                    sw.Write($", {0:f2}", cs.intensities[i]);
                sw.WriteLine();
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Monte-Carlo Batch Testing
        ////////////////////////////////////////////////////////////////////////

        // @todo move to a MonteCarlo class
        //
        // This ended up getting more complicated than expected due to scope
        // creep.  Originally we were just going to take HW-triggered spectra
        // for 5hr, essentially wrapping a loop around wrapper.getSpectraAsync().
        //
        // Then requirements evolved to include checking that double-triggers
        // weren't being read, and then to actively injecting double-triggers.
        // Things also complicated a bit when we determined the need for a
        // throwaway spectrum after changing integration time.
        //
        // The current implementation is probably more complicated than it needs
        // to be, by maintaining 8 distinct "free-running spectra" tasks.  A
        // simpler design would probably have no free-running loops, and just
        // execute the following events at a fixed schedule:
        //
        //      1. randomize integration time on all spectrometers
        //      2. perform one throwaway spectrum on each (SW or HW triggered)
        //      3. attempt to read a 2nd trigger, confirm fails
        //      4. generate official HW trigger
        //      5. collect one spectrum from each
        //      6. attempt to read a 2nd trigger, confirm fails
        //
        // BackgroundWorkerBatch drives the Monte Carlo test.  This worker runs 
        // from startTime to endTime, in deliberately-sedate 3sec increments
        // (2sec works for fewer spectrometers, went to 3sec for all 8).
        //
        // Every iteration, it generates a hardware trigger.  That trigger may or
        // may not be intentionally doubled (lo-hi-lo-hi-lo) if forceDoubleTrigger 
        // is set.
        //
        // At the end of each iteration it outputs a record of test state. 
        // Besides outputing records and generating triggers, the BackgroundWorker
        // doesn't do anything.  All acquisitions are in the per-position Tasks.
        //
        // The spectrometers themselves run in essentially free-running async 
        // methods.  All they do is loop over:
        //
        //      - randomize integration time
        //      - attempt acquire FIRST (real) trigger (should succeed)
        //      - attempt to SECOND (fake) trigger (should fail)

        // User changed the minimum randomly-selectable Monte Carlo integration time
        private void numericUpDownIntegrationTimeMSMin_ValueChanged(object sender, EventArgs e)
        {
            integrationTimeMSRandomMin = (uint)numericUpDownIntegrationTimeMSMin.Value;
            // maxPulseWidthMS = (uint)(0.25 * integrationTimeMSRandomMin);
        }

        // User changed the maximum randomly-selectable Monte Carlo integration time
        private void numericUpDownIntegrationTimeMSMax_ValueChanged(object sender, EventArgs e)
        {
            integrationTimeMSRandomMax = (uint)numericUpDownIntegrationTimeMSMax.Value;
        }

        // User indicated whether they wish to attempt reading double-trigger events.
        private void checkBoxDoubleTriggerAttempt_CheckedChanged(object sender, EventArgs e)
        {
            doubleTriggerAttemptRead = checkBoxDoubleTriggerAttempt.Checked;

            // give ourselves more time per iteration if checking for double triggers
            WORKER_PERIOD_MS = doubleTriggerAttemptRead ? 3000 : 2000;
        }

        // User indicated that want double-triggers randomly injected into the test
        private void checkBoxDoubleTriggerEnable_CheckedChanged(object sender, EventArgs e)
        {
            wrapper.forceDoubleTrigger = checkBoxDoubleTriggerEnable.Checked;
        }

        // User specified the likelihood of random double-trigger events
        private void numericUpDownDoubleTriggerProbability_ValueChanged(object sender, EventArgs e)
        {
            wrapper.doubleTriggerPercentage = ((double)numericUpDownDoubleTriggerProbability.Value) / 100.0;
        }

        // User clicked "Start/Stop" Monte Carlo operation
        private void buttonBatchStart_Click(object sender, EventArgs e)
        {
            if (batchRunning)
                stopBatch();
            else
                startBatchAsync();
        }

        void stopBatch()
        {
            logger.header("stopping batch collection");
            batchShuttingDown = true;
            workerBatch.CancelAsync();
        }

        async void startBatchAsync()
        {
            logger.header("starting batch collection");

            // prompt user for filename to save data; cancel batch if no filename is provided
            DialogResult result = saveFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            enableControls(false);

            batchRunning = true;
            batchShuttingDown = false;
            triggerCount = 0;
            readFailureCount = 0;
            wrapper.doubleTriggerCount = 0;

            // It's interesting that though this is an async method, it can 
            // change GUI widgets.  I suppose it is technically a TASK run ON THE
            // GUI THREAD.
            buttonBatchStart.Text = "Stop";

            groupBoxIntegrationTimeLimits.Enabled = 
                numericUpDownBatchMin.Enabled =
                groupBoxSystem.Enabled = false;

            batchPathname = saveFileDialog.FileName;
            if (batchPathname.EndsWith(".csv"))
                talliesPathname = Regex.Replace(batchPathname, @"\.csv$", "-tallies.csv");
            else
                talliesPathname = batchPathname + ".tallies";
            logger.debug($"will use talliesPathname {talliesPathname}");

            // init tallies
            tallies = new SortedDictionary<int, SortedDictionary<uint, Tally>>();
            foreach (var pos in wrapper.positions)
                if (wrapper.getSpectrometer(pos).multiChannelSelected)
                    tallies.Add(pos, new SortedDictionary<uint, Tally>());

            // init success/failure buckets
            acquisitionCounts = new Dictionary<int, int>();
            secondTriggerCounts = new Dictionary<int, int>();

            // init graph
            foreach (var s in seriesTime)
                s.Value.Points.Clear();

            logger.header("Creating freeRunningSpectrometers");
            List<Task> tasks = new List<Task>();
            foreach (var pos in wrapper.positions)
            {
                var spec = wrapper.getSpectrometer(pos);
                if (!spec.multiChannelSelected)
                    continue;

                logger.debug($"creating freeRunningSpectrometerAsync({pos})");
                tasks.Add(Task.Run(() => { var ok = freeRunningSpectrometerAsync(pos); }));
            }

            logger.debug("waiting 1sec for FRS to randomize integration time and take throwaways");
            await Task.Delay(1000);

            logger.debug("spawning worker");
            workerBatch.RunWorkerAsync();

            // This may take 5hr to complete.  Their termination will be
            // triggered by batchShuttingDown, itself raised at the end of
            // the BackgroundWorker's DoWork (NOT Completed, though it could
            // have been).
            logger.debug("Waiting for all FRS to exit");
            Task.WaitAll(tasks.ToArray());

            enableControls(true);
        }

        // Eight (8) Tasks run this function asynchronously on different
        // spectrometers...it's basically a poor man's BackgroundWorker.  
        // Compare to WasatchDeviceWrapper.continuous_poll.
        //
        // Its purpose is to loop over workerBatch triggers.  After each
        // trigger, it should successfully acquire ONE spectra, and then
        // FAIL to acquire a SECOND spectra (confirming no "double-triggers").
        //
        // Note that this function doesn't do any graphing...batchWorker
        // takes care of GUI updates.
        //
        // I made this a Task rather than a BackgroundWorker because it seemed 
        // lighter-weight.  However, I now realize that Tasks aren't actually on
        // their own threads, meaning you can't reliably "name" the thread 
        // persistently, making debugging across 8 of them a bit harder.  In 
        // retrospect, I think I'll change this back to a (re-usable) 
        // BackgroundWorker at some point (still taking position as a parameter),
        // just for the improved logging that allows.
        async Task<bool> freeRunningSpectrometerAsync(int pos)
        {
            var spec = wrapper.getSpectrometer(pos);
            if (spec is null)
                return false;

            string prefix = $"frs({pos})";
            logger.info($"freeRunningSpectrometer starting on pos {pos}");

            // initialize storage
            acquisitionCounts[pos] = 0;
            secondTriggerCounts[pos] = 0;

            // don't throw errors until we've received our first trigger
            spec.errorOnTimeout = false;

            // everyone starts at 100ms
            setIntegrationTimeMS(spec, 100, prefix);

            // enter acquisition loop
            bool firstTriggerReceived = false;
            while (true)
            {
                if (batchShuttingDown)
                    break;

                ////////////////////////////////////////////////////////////////
                // block on HW trigger 
                ////////////////////////////////////////////////////////////////

                // try to read the "first" (and ideally only) spectrum generated 
                // by the HW trigger

                var timeoutMS = (uint)WORKER_PERIOD_MS;
                logger.debug($"{prefix}: setting timeout to {timeoutMS}");
                spec.acquisitionTimeoutMS = timeoutMS;

                logger.debug($"{prefix}: trying to read FIRST spectrum (trigger {triggerCount})");
                var cs = await wrapper.getSpectrumAsync(pos);
                if (cs != null)
                {
                    logger.debug($"{prefix}: read FIRST spectrum (trigger {triggerCount})");
                    firstTriggerReceived = true;
                    acquisitionCounts[pos]++;

                    // henceforth, log errors on timeout, UNLESS we're deliberately courting
                    // timeouts
                    spec.errorOnTimeout = !doubleTriggerAttemptRead;

                    chartAll.BeginInvoke((MethodInvoker)delegate { processSpectrum(cs); });
                }
                else
                {
                    if (batchShuttingDown)
                        break;

                    if (firstTriggerReceived)
                    {
                        logger.error($"{prefix}: [BAD] failed to read FIRST spectrum (trigger {triggerCount}) ");
                        readFailureCount++;
                    }
                    else
                    {
                        // Special logic for reading the VERY FIRST trigger of the whole
                        // Monte Carlo operation: we don't know exactly how long it will
                        // take between when the FRS is spun-up, to when the BackgroundWorker
                        // will get going and emit the first trigger, so just wait patiently.
                        logger.debug($"{prefix}: failed to read FIRST spectrum (okay), trying again");
                    }

                    // for whatever reason, haven't read the FIRST trigger yet,
                    // so keep trying
                    continue;
                }

                if (batchShuttingDown)
                    break;

                // make sure the pulse which triggered the FIRST spectrum is
                // actually complete
                await Task.Delay((int)maxPulseWidthMS);

                ////////////////////////////////////////////////////////////////
                // try to read results of SECOND TRIGGER 
                ////////////////////////////////////////////////////////////////

                if (doubleTriggerAttemptRead)
                {
                    // This is to try to prove that no second triggers are being 
                    // generated or detected. Ideally none will occur, and this read
                    // will always fail.  Although the phrase "non-existent" is used
                    // in log messages, if we are deliberately injecting double-
                    // triggers via wrapper.forceDoubleTrigger, then sometimes those
                    // double-triggers will be genuine.  In that case, our goal is to
                    // count whether they generated new spectra or not.  
                    //
                    // Older firmware was known to allow double-triggers (even within 
                    // a period less than the configured integration time) to 
                    // generate multiple acquisitions (or worse, a single corrupted 
                    // acquisition).  New FW under test should silently ignore new
                    // triggers received while the spectrometer is already in the
                    // midst of an acquisition.

                    logger.header($"{prefix}: trying to read (non-existant) SECOND spectrum (trigger {triggerCount})");

                    // 100ms more than integration time should be plenty
                    timeoutMS = spec.integrationTimeMS + 100;
                    logger.debug($"{prefix}: setting timeout to {timeoutMS}");
                    spec.acquisitionTimeoutMS = timeoutMS;

                    cs = await wrapper.getSpectrumAsync(pos);
                    if (cs is null)
                    {
                        logger.debug($"{prefix}: correctly timed-out on non-existent second trigger (trigger {triggerCount})");
                    }
                    else
                    {
                        logger.error($"{prefix}: [BAD] actually read (non-existent) second trigger (trigger {triggerCount})");
                        acquisitionCounts[pos]++;
                        secondTriggerCounts[pos]++;

                        // make sure the pulse which triggered the SECOND spectrum is
                        // actually complete
                        await Task.Delay((int)maxPulseWidthMS);
                    }

                    if (batchShuttingDown)
                        break;
                }

                ////////////////////////////////////////////////////////////////
                // scramble integration time for next intended read 
                ////////////////////////////////////////////////////////////////

                var ms = (uint)r.Next((int)integrationTimeMSRandomMin,
                                      (int)integrationTimeMSRandomMax + 1);
                setIntegrationTimeMS(spec, ms, prefix);
            }

            logger.info($"freeRunningSpectrometer on pos {pos} done");
            return true;
        }

        // This function is provided to ensure the new timeout is set along with
        // (just before, actually) the new integration time, and to let 
        // freeRunningSpectrometer easily use it twice.
        void setIntegrationTimeMS(Spectrometer spec, uint ms, string prefix="setIntegrationTimeMS")
        {
            logger.debug($"{prefix}: setting randomized integration time to {ms}");

            var timeoutMS = ms + 100;
            logger.debug($"{prefix}: setting acquisitionTimeoutMS to {timeoutMS}");
            spec.acquisitionTimeoutMS = timeoutMS;

            logger.debug($"{prefix}: setting integrationTimeMS (will generate SW throwaway)");
            spec.integrationTimeMS = ms; // will generate SW-triggered throwaway
            logger.debug($"{prefix}: back from setting integration time");
        }

        void backgroundWorkerBatch_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.CurrentThread.Name = "workerBatch";

            logger.info("Starting Monte Carlo Worker");

            var startTime = DateTime.Now;
            var endTime = startTime.AddMinutes((int)numericUpDownBatchMin.Value);
            logger.debug($"workerBatch will shutdown after {endTime}");

            using (StreamWriter sw = new StreamWriter(batchPathname))
            {
                logger.info($"writing {batchPathname}");

                // header e.g. Count, Timestamp, Remaining, ElapsedMS, TriggerMS, Pos_1_MS, Pos_5_MS, Pos_1_DegC, Pos_5_DegC, Pos_1_Avg, Pos_5_Avg
                List<string> headers = new List<string>();
                headers.Add("Count");
                headers.Add("Timestamp");
                headers.Add("Remaining");
                headers.Add("ElapsedMS");
                headers.Add("TriggerMS");
                foreach (var pos in wrapper.positions)
                    if (wrapper.getSpectrometer(pos).multiChannelSelected)
                        headers.Add($"Pos{pos}_MS");
                foreach (var pos in wrapper.positions)
                    if (wrapper.getSpectrometer(pos).multiChannelSelected)
                        headers.Add($"Pos{pos}_Avg");
                foreach (var pos in wrapper.positions)
                    if (wrapper.getSpectrometer(pos).multiChannelSelected)
                        headers.Add($"Pos{pos}_DegC");
                foreach (var pos in wrapper.positions)
                    if (wrapper.getSpectrometer(pos).multiChannelSelected)
                        headers.Add($"Pos{pos}_Spectra");
                sw.WriteLine(string.Join<string>(", ", headers));

                while (DateTime.Now < endTime)
                {
                    DateTime iterationStart = DateTime.Now;

                    if (workerBatch.CancellationPending || batchShuttingDown)
                    {
                        logger.debug("workerBatch.DoWork cancelled");
                        e.Cancel = true;
                        break;
                    }

                    // update times
                    var now = DateTime.Now;
                    var nowStr = now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    var elapsedMS = (uint)(now - startTime).TotalMilliseconds;
                    var timeRemaining = (endTime - now).ToString(@"hh\:mm\:ss");
                    labelBatchStatus.BeginInvoke((MethodInvoker)delegate { labelBatchStatus.Text = timeRemaining + " remaining"; });

                    // randomize trigger pulse width (see MultiChannelWrapper.forceDoubleTrigger
                    // docs for logic on max trigger length)
                    //
                    // wrapper.triggerPulseWidthMS = r.Next(1, (int)maxPulseWidthMS);

                    ////////////////////////////////////////////////////////////////
                    // write to file
                    ////////////////////////////////////////////////////////////////

                    // we write to the file BEFORE sending the trigger to start the
                    // next collection event, to ensure we don't capture new data in 
                    // the midst of saving the old
                    if (triggerCount > 0)
                    {
                        List<string> specInteg = new List<string>();
                        List<string> specAvg = new List<string>();
                        List<string> specDegC = new List<string>();
                        List<string> specSpectra = new List<string>();
                        foreach (var pos in wrapper.positions)
                        {
                            var spec = wrapper.getSpectrometer(pos);
                            if (!spec.multiChannelSelected)
                                continue;

                            var avg = spec.lastSpectrum.Average();
                            var ms = spec.integrationTimeMS;

                            specInteg.Add(string.Format("{0}", ms));
                            specAvg.Add(string.Format("{0:f2}", avg));
                            specDegC.Add(string.Format("{0:f2}", spec.detectorTemperatureDegC));
                            specSpectra.Add(string.Format("{0}", acquisitionCounts[pos]));

                            // update tallies
                            if (!tallies[pos].ContainsKey(ms))
                                tallies[pos].Add(ms, new Tally());
                            tallies[pos][ms].total += avg;
                            tallies[pos][ms].count++;
                        }

                        // output record
                        sw.Write($"{triggerCount}, {nowStr}, {timeRemaining}, {elapsedMS}, {wrapper.triggerPulseWidthMS}, ");
                        sw.Write(string.Join<string>(", ", specInteg) + ", ");
                        sw.Write(string.Join<string>(", ", specAvg) + ", ");
                        sw.Write(string.Join<string>(", ", specDegC) + ", ");
                        sw.Write(string.Join<string>(", ", specSpectra));
                        sw.WriteLine();
                    }

                    ////////////////////////////////////////////////////////////
                    // generate trigger
                    ////////////////////////////////////////////////////////////

                    logger.info($"generating trigger {triggerCount} ({wrapper.triggerPulseWidthMS} ms)");

                    // Although startAcquisitionAsync is declared async, we're 
                    // calling it synchronously, so it runs within the worker's 
                    // thread. Also note we are not configuring timeouts here, as
                    // presumably the FRS are ALREADY in a blocking acquire
                    var ok = wrapper.startAcquisitionAsync(false).Result;

                    // run batch at evenly-stepped increments
                    int iterationElapsedMS = (int)(DateTime.Now - iterationStart).TotalMilliseconds;
                    int sleepMS = WORKER_PERIOD_MS - iterationElapsedMS;

                    logger.debug($"workerBatch: sleeping {sleepMS}ms");
                    Thread.Sleep(sleepMS);
                    triggerCount++;
                }
            }

            logger.info("closing all freeRunningSpectrometers");
            batchShuttingDown = true;
            foreach (var pos in wrapper.positions)
                wrapper.getSpectrometer(pos).cancelCurrentAcquisition();

            logger.debug("Monte Carlo Worker done");
        }

        private void backgroundWorkerBatch_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            logger.header("Monte Carlo Complete");

            // We create a separate tallies file so all of the intensities at
            // different integration times can be easily graphed and visualized
            // for all the spectrometers.  
            //
            // Note that we're taking an average of an average here, so anomalies 
            // will be suppressed in the tally report.  If you wanted to explicitly
            // check for anomalies, it would be worth reporting stdev or min/max.
            // Some statistics would require holding more data in memory than the
            // current Tally class however.  
            // 
            // Of course, since all raw data is logged in the big .csv, end-users
            // can always generate such statistics themselves with a bit of work
            // in Excel.
            using (StreamWriter sw = new StreamWriter(talliesPathname))
            {
                logger.info($"writing {talliesPathname}");

                // metadata
                sw.WriteLine($"Failures to read first triggered spectrum, {readFailureCount}");
                sw.WriteLine($"Second triggers sent, {wrapper.doubleTriggerCount}");
                sw.WriteLine();

                sw.WriteLine("[Second Triggers Read]");
                foreach (var pair in tallies)
                    sw.Write($", Pos{pair.Key}_2ndTrig");
                sw.WriteLine();
                foreach (var pair in tallies)
                    sw.Write($", {secondTriggerCounts[pair.Key]}");
                sw.WriteLine();
                sw.WriteLine();

                // header e.g. IntegrationTimeMS, Pos_1, Pos_5
                sw.Write("IntegrationTimeMS");
                foreach (var pair in tallies)
                    sw.Write($", Pos{pair.Key}_Mean");
                sw.WriteLine();

                // iterate over full range of supported random integration times
                for (uint ms = integrationTimeMSRandomMin; ms <= integrationTimeMSRandomMax; ms++)
                {
                    sw.Write(ms);

                    // write the average intensity for that integration time for each position
                    foreach (var pair in tallies)
                    {
                        var pos = pair.Key;
                        var integPair = pair.Value;
                        if (integPair.ContainsKey(ms))
                        {
                            var tally = integPair[ms];
                            var mean = tally.total / tally.count;
                            sw.Write($", {mean:f2}");
                        }
                        else
                        {
                            sw.Write(", ");
                        }
                    }
                    sw.WriteLine();
                }
            }

            batchRunning = false;
            buttonBatchStart.Text = "Start";
            groupBoxIntegrationTimeLimits.Enabled = 
                numericUpDownBatchMin.Enabled =
                groupBoxSystem.Enabled = true;
            labelBatchStatus.Text = "click to start";
        }
    }
}
