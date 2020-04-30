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
using System.Security.Cryptography;

namespace MultiChannelDemo
{
    public partial class Form1 : Form
    {
        MultiChannelWrapper wrapper = MultiChannelWrapper.getInstance();
        Logger logger = Logger.getInstance();

        bool initialized = false;

        // Monte Carlo limits
        Random r = new Random();
        uint integrationTimeMSRandomMin = 100;
        uint integrationTimeMSRandomMax = 500;
        uint maxPulseWidthMS = 25; 
        Dictionary<int, int> acquisitionCounts;

        // note these are zero-indexed
        Chart[] charts;
        GroupBox[] groupBoxes;
        CheckBox[] checkBoxes;

        Dictionary<int, Series> seriesCombined = new Dictionary<int, Series>();
        Dictionary<int, Series> seriesTime = new Dictionary<int, Series>();

        // whatever was last returned by "Acquire All", "Take Darks" or "Take References"
        // (added so we'd have something to save)
        List<ChannelSpectrum> lastSpectra;

        // Batch test
        bool batchRunning;
        bool batchShuttingDown;
        string batchPathname;
        string talliesPathname;
        const int WORKER_PERIOD_MS = 2000;
        BackgroundWorker workerBatch;
        int triggerCount;

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

            initChart(chartAll);
            foreach (var chart in charts)
                initChart(chart);

            Text = String.Format("MultiChannelDemo v{0}", Application.ProductVersion);

            workerBatch = new BackgroundWorker() { WorkerSupportsCancellation = true };
            workerBatch.DoWork             += backgroundWorkerBatch_DoWork;
            workerBatch.RunWorkerCompleted += backgroundWorkerBatch_RunWorkerCompleted;

            AcceptButton = buttonInit;

            groupBoxBatch.Enabled = false;
            
            maxPulseWidthMS = (uint)(0.25 * integrationTimeMSRandomMin);
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
                foreach (var pos in wrapper.positions)
                {
                    var spec = wrapper.getSpectrometer(pos);
                    var gb = groupBoxes[pos - 1];
                    gb.Text = string.Format("Position {0} ({1}) {2}ms {3:f2}°C",
                        pos, spec.serialNumber, spec.integrationTimeMS, spec.detectorTemperatureDegC);
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
                        pos, spec.serialNumber, spec.integrationTimeMS, cs.detectorTemperatureDegC);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // System-Level Control (all spectrometers)
        ////////////////////////////////////////////////////////////////////////

        // user changed trigger pulse width
        private void numericUpDownTriggerWidthMS_ValueChanged(object sender, EventArgs e) => wrapper.triggerPulseWidthMS = (int)numericUpDownTriggerWidthMS.Value;

        // user clicked the "enable external hardware triggering" system checkbox 
        void checkBoxTriggerEnableAll_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = checkBoxTriggerEnableAll.Checked;
            logger.header("Hardware Triggering {0}", enabled ? "enabled" : "disabled");
            wrapper.hardwareTriggeringEnabled = enabled;
        }

        // user clicked the "fan control" system checkbox
        void checkBoxFanEnable_CheckedChanged(object sender, EventArgs e) => wrapper.fanEnabled = checkBoxFanEnable.Checked;

        // user clicked the "enable reflectance" system checkbox
        private void checkBoxReflectanceEnabled_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = checkBoxReflectanceEnabled.Checked;
            wrapper.reflectanceEnabled =
                buttonTakeRefs.Enabled = 
                buttonClearRefs.Enabled = enabled;
        }

        // user clicked the system-level "Acquire" button 
        async void buttonAcquireAll_Click(object sender, EventArgs e)
        {
            logger.header("User clicked Acquire");

            enableControls(false);
            lastSpectra = await wrapper.getSpectraAsync();
            processSpectra(lastSpectra);
            enableControls(true);

            logger.header("Acquire complete");
        }

        // user clicked the system-level "Take Dark" button 
        async void buttonTakeDark_Click(object sender, EventArgs e)
        {
            enableControls(false);
            lastSpectra = await wrapper.takeDarkAsync();
            processSpectra(lastSpectra);

            // only allow references after dark correction
            buttonTakeRefs.Enabled = true;

            enableControls(true);
        }

        private void buttonClearDark_Click(object sender, EventArgs e)
        {
            wrapper.clearDark();
            buttonTakeRefs.Enabled = false;
        }

        async void buttonTakeRefs_Click(object sender, EventArgs e)
        {
            enableControls(false);
            lastSpectra = await wrapper.takeReferenceAsync();
            processSpectra(lastSpectra);

            // we have references, so can now compute reflectance
            checkBoxReflectanceEnabled.Enabled = true;

            enableControls(true);
        }

        private void buttonClearRefs_Click(object sender, EventArgs e)
        {
            wrapper.clearReference();
            checkBoxReflectanceEnabled.Enabled = false;
            checkBoxReflectanceEnabled.Checked = false;
        }

        // user set all spectrometers to one integration time
        void numericUpDownIntegrationTimeMS_ValueChanged(object sender, EventArgs e)
        {
            enableControls(false);
            uint value = (uint)numericUpDownIntegrationTimeMS.Value;
            wrapper.setIntegrationTimeMS(value);

            updateGroupBoxTitles();
            enableControls(true);
        }

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

            updateIntegrationTimeControl(); 

            enableControls(true);

            logger.header("Optimize complete");
        }

        // The GUI exposes a control to set the integration time for any
        // currently-selected spectrometer.  Ergo, the min/max of that control
        // should be the INTERSECTION of all selected spectrometers.
        void updateIntegrationTimeControl()
        {
            int min = -1;
            int max = 0x10000;
            int value = -1;

            foreach (int pos in wrapper.positions)
            {
                var spec = wrapper.getSpectrometer(pos);
                if (!spec.multiChannelSelected)
                    continue;

                if (spec is null)
                    return;
                min = (int)Math.Max(min, spec.eeprom.minIntegrationTimeMS);
                max = (int)Math.Min(max, spec.eeprom.maxIntegrationTimeMS);
                value = (int)Math.Max(value, spec.integrationTimeMS);
            }

            numericUpDownIntegrationTimeMS.ValueChanged -= numericUpDownIntegrationTimeMS_ValueChanged;
            numericUpDownIntegrationTimeMS.Minimum = max;
            numericUpDownIntegrationTimeMS.Maximum = min;
            numericUpDownIntegrationTimeMS.Value = value;
            numericUpDownIntegrationTimeMS.ValueChanged += numericUpDownIntegrationTimeMS_ValueChanged;
        }

        ////////////////////////////////////////////////////////////////////////
        // Graphing
        ////////////////////////////////////////////////////////////////////////

        // Although this method isn't marked async, it does get called via a 
        // BeginInvoke delegate from BatchWorker, which means it _does_ get run
        // asynchronously, and IN PARALLEL with anything else going on in the
        // worker.  Therefore, avoid talking over USB from this or subordinate
        // methods.
        bool processSpectra(List<ChannelSpectrum> spectra)
        {
            if (spectra is null)
            {
                logger.error("Can't process missing spectra");
                return false;
            }

            foreach (var cs in spectra)
                processSpectrum(cs);

            updateGroupBoxTitles(spectra);

            return true;
        }

        // Do whatever we're gonna do with new spectra (save, dark-correct, ...).
        // Since we encapsulated dark subtraction in WasatchNET.Spectrometer, and
        // reflectance in MultiChannelWrapper, there's (deliberately) not a lot 
        // to do here.
        void processSpectrum(ChannelSpectrum cs)
        {
            if (cs.intensities is null)
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

        private void checkBoxInterpolate_CheckedChanged(object sender, EventArgs e)
        {
            groupBoxInterpolation.Enabled = checkBoxInterpolate.Checked;
        }

        /// <summary>
        /// Just write spectra in pixel space for now.  (Kludged-in as somehow I
        /// didn't originally think this would be required.)
        /// </summary>
        /// <todo>
        /// optionally interpolate on wavelength
        /// </todo>
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
        // Okay, this is the proposed structure of this process, given our desire
        // to "allow but count" double-triggered acquisitions if they happen to 
        // occur.
        //
        // BackgroundWorkerBatch drives the Monte Carlo test.  This worker runs 
        // from startTime to endTime, in deliberately-sedate 2sec increments.
        // Every 2sec, it generates a hardware trigger.  That trigger may or may
        // not be intentionally doubled (lo-hi-lo-hi-lo) if forceDoubleTrigger
        // is set.  At the BEGINNING of each iteration (before the new trigger is
        // sent), it outputs a record containing the LAST positional integration 
        // times, spectral counts and means.  Besides outputing records and
        // generating triggers, it doesn't do anything.  All acquisitions are
        // in the per-position threads.
        //
        // The spectrometers themselves run in essentially free-running async 
        // methods.  All they do is loop over:
        //
        //      - exit if shuttingDown
        //      - acquire FIRST (real) trigger
        //      - acquire SECOND (fake) trigger
        //      - increment spectralCount unless error
        //
        // This way, each spectrometer has plenty of time to fully execute and
        // acquire one expected integration (up to 500ms), with a whole 1.5sec
        // to acquire any misfires as well.  

        private void numericUpDownIntegrationTimeMSMin_ValueChanged(object sender, EventArgs e)
        {
            integrationTimeMSRandomMin = (uint)numericUpDownIntegrationTimeMSMin.Value;
            maxPulseWidthMS = (uint)(0.25 * integrationTimeMSRandomMin);
        }

        private void numericUpDownIntegrationTimeMSMax_ValueChanged(object sender, EventArgs e)
        {
            integrationTimeMSRandomMax = (uint)numericUpDownIntegrationTimeMSMax.Value;
        }

        private void checkBoxDoubleTriggerEnable_CheckedChanged(object sender, EventArgs e)
        {
            wrapper.forceDoubleTrigger = checkBoxDoubleTriggerEnable.Checked;
        }

        private void numericUpDownDoubleTriggerProbability_ValueChanged(object sender, EventArgs e)
        {
            wrapper.doubleTriggerPercentage = ((double)numericUpDownDoubleTriggerProbability.Value) / 100.0;
        }

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

            DialogResult result = saveFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            enableControls(false);

            batchRunning = true;
            batchShuttingDown = false;
            triggerCount = 0;

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
            acquisitionCounts = new Dictionary<int, int>();

            // init graph
            foreach (var s in seriesTime)
                s.Value.Points.Clear();

            foreach (var pos in wrapper.positions)
                if (wrapper.getSpectrometer(pos).multiChannelSelected)
                    tallies.Add(pos, new SortedDictionary<uint, Tally>());

            logger.header("Creating freeRunningSpectrometers");
            List<Task> tasks = new List<Task>();
            foreach (var pos in wrapper.positions)
            {
                var spec = wrapper.getSpectrometer(pos);
                if (!spec.multiChannelSelected)
                    continue;

                logger.debug($"creating freeRunningSpectrometer({pos})");
                tasks.Add(Task.Run(() => { var ok = freeRunningSpectrometer(pos); }));
            }

            logger.debug("waiting 1sec for FRS to randomize integration time and take throwaways");
            await Task.Delay(1000);

            logger.debug("spawning worker");
            workerBatch.RunWorkerAsync();

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
        async Task<bool> freeRunningSpectrometer(int pos)
        {
            var spec = wrapper.getSpectrometer(pos);
            if (spec is null)
                return false;

            string prefix = $"frs({pos})";
            Thread.CurrentThread.Name = prefix;

            logger.header($"{prefix} starting");

            if (!acquisitionCounts.ContainsKey(pos))
                acquisitionCounts[pos] = 0;
            bool firstTriggerReceived = false;

            // everyone starts at 100ms
            setIntegrationTimeMS(spec, 100, prefix);

            while (true)
            {
                if (batchShuttingDown)
                {
                    logger.debug($"{prefix}: shutting down");
                    break;
                }

                ////////////////////////////////////////////////////////////////
                // block on HW trigger 
                ////////////////////////////////////////////////////////////////

                // try to read the "first" (and ideally only) spectrum generated 
                // by the HW trigger

                var timeoutMS = (uint)(WORKER_PERIOD_MS - spec.integrationTimeMS);
                logger.debug($"{prefix}: setting timeout to {timeoutMS}");
                spec.acquisitionTimeoutMS = timeoutMS;

                logger.debug($"{prefix}: trying to read FIRST spectrum (trigger {triggerCount})");
                var cs = await wrapper.getSpectrumAsync(pos);
                if (cs != null)
                {
                    logger.debug($"{prefix}: read FIRST spectrum (trigger {triggerCount})");
                    firstTriggerReceived = true;
                    acquisitionCounts[pos]++;
                    chartAll.BeginInvoke((MethodInvoker)delegate { processSpectrum(cs); });
                }
                else
                {
                    if (batchShuttingDown)
                    {
                        logger.debug($"{prefix}: shutting down");
                        break;
                    }

                    if (firstTriggerReceived)
                        logger.error($"{prefix}: [BAD] failed to read FIRST spectrum (trigger {triggerCount}) ");
                    else
                        logger.debug($"{prefix}: failed to read FIRST spectrum (okay), trying again");

                    continue;
                }

                if (batchShuttingDown)
                {
                    logger.debug($"{prefix}: shutting down");
                    break;
                }

                ////////////////////////////////////////////////////////////////
                // try to read results of SECOND TRIGGER 
                ////////////////////////////////////////////////////////////////

                // This is to try to prove that no second triggers are being 
                // generated or detected. Ideally none will occur, and this read
                // will always fail.


                // make sure the pulse which triggered the FIRST spectrum is
                // actually complete
                await Task.Delay((int)maxPulseWidthMS);

                logger.header($"{prefix}: trying to read (non-existant) SECOND spectrum (trigger {triggerCount})");

                // 100ms more than integration time should be plenty
                timeoutMS = spec.integrationTimeMS + 100;
                logger.debug($"{prefix}: setting timeout to {timeoutMS}");
                spec.acquisitionTimeoutMS = timeoutMS;

                cs = await wrapper.getSpectrumAsync(pos);
                if (cs is null)
                    logger.debug($"{prefix}: correctly timed-out on non-existent second trigger (trigger {triggerCount})");
                else
                {
                    logger.error($"{prefix}: [BAD] actually read (non-existent) second trigger (trigger {triggerCount})");
                    acquisitionCounts[pos]++;
                }

                if (batchShuttingDown)
                {
                    logger.debug($"{prefix}: shutting down");
                    break;
                }

                ////////////////////////////////////////////////////////////////
                // scramble integration time for next intended read 
                ////////////////////////////////////////////////////////////////

                var ms = (uint)r.Next((int)integrationTimeMSRandomMin,
                                      (int)integrationTimeMSRandomMax + 1);
                setIntegrationTimeMS(spec, ms, prefix);
            }

            logger.header($"{prefix}: done");
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

            logger.header("Starting Monte-Carlo Worker");

            Random r = new Random();
            var startTime = DateTime.Now;
            var endTime = startTime.AddMinutes((int)numericUpDownBatchMin.Value);
            logger.debug($"workerBatch will end at {endTime}");

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

                    var now = DateTime.Now;
                    var nowStr = now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    var elapsedMS = (uint)(now - startTime).TotalMilliseconds;
                    var timeRemaining = (endTime - now).ToString(@"hh\:mm\:ss");

                    // update on-screen countdown
                    labelBatchStatus.BeginInvoke((MethodInvoker)delegate { labelBatchStatus.Text = timeRemaining + " remaining"; });

                    // randomize trigger pulse width (see MultiChannelWrapper.forceDoubleTrigger
                    // docs for logic on max trigger length)
                    wrapper.triggerPulseWidthMS = r.Next(1, (int)maxPulseWidthMS);

                    ////////////////////////////////////////////////////////////////
                    // write to file
                    ////////////////////////////////////////////////////////////////

                    List<string> specMS = new List<string>();
                    List<string> specAvg = new List<string>();
                    List<string> specDegC = new List<string>();
                    foreach (var pos in wrapper.positions)
                    {
                        var spec = wrapper.getSpectrometer(pos);
                        if (!spec.multiChannelSelected)
                            continue;

                        var avg = spec.lastSpectrum.Average();
                        var ms = spec.integrationTimeMS;

                        specMS.Add(string.Format("{0}", ms));
                        specAvg.Add(string.Format("{0:f2}", avg));
                        specDegC.Add(string.Format("{0:f2}", spec.detectorTemperatureDegC));

                        // update tallies
                        if (!tallies[pos].ContainsKey(ms))
                            tallies[pos].Add(ms, new Tally());
                        tallies[pos][ms].total += avg;
                        tallies[pos][ms].count++;
                    }

                    // output record
                    sw.Write($"{triggerCount}, {nowStr}, {timeRemaining}, {elapsedMS}, {wrapper.triggerPulseWidthMS}");
                    sw.Write(string.Join<string>(", ", specMS) + ", ");
                    sw.Write(string.Join<string>(", ", specAvg) + ", ");
                    sw.WriteLine(string.Join<string>(", ", specDegC));

                    // generate trigger
                    logger.header($"workerBatch: generating trigger {triggerCount}");

                    // Although startAcquisitionAsync is declared async, we're 
                    // calling it synchronously, so it runs within the worker's 
                    // thread. Also note we are not configuring timeouts here, as
                    // presumably the FRS are ALREADY in a blocking acquire
                    var ok = wrapper.startAcquisitionAsync(false).Result;

                    // for now, just run batch at 2sec increments
                    int iterationElapsedMS = (int)(DateTime.Now - iterationStart).TotalMilliseconds;
                    int sleepMS = WORKER_PERIOD_MS - iterationElapsedMS;
                    logger.debug($"workerBatch: sleeping {sleepMS}ms");
                    Thread.Sleep(sleepMS);

                    triggerCount++;
                }
            }

            logger.header("closing all freeRunningSpectrometers");
            batchShuttingDown = true;
            foreach (var pos in wrapper.positions)
                wrapper.getSpectrometer(pos).cancelCurrentAcquisition();

            logger.debug("workerBatch.DoWork done");
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

                // header e.g. IntegrationTimeMS, Pos_1, Pos_5
                sw.Write("IntegrationTimeMS");
                foreach (var pair in tallies)
                {
                    var pos = pair.Key;
                    sw.Write($", Pos_{pos}");
                }
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
