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

        // whatever was last returned by "Acquire All", "Take Darks" or "Take References"
        // (added so we'd have persisted data to write when user clicked "Save")
        List<ChannelSpectrum> lastSpectra;

        // Monte Carlo 
        BackgroundWorker workerBatch;
        string batchPathname;
        string talliesPathname;
        uint integrationTimeMSRandomMin = 100;
        uint integrationTimeMSRandomMax = 500;
        bool batchRunning;

        const int WORKER_PERIOD_MS = 2000;
        const int MAX_CONSECUTIVE_READ_FAILURES = 3;

        // place to store intensities by integration time
        // tallies[pos][integTimeMS].{total, count}
        SortedDictionary<int, SortedDictionary<uint, Tally>> tallies;

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public Form1()
        {
            InitializeComponent();

            Text = String.Format("MultiChannelDemo v{0}", Application.ProductVersion);

            logger.setTextBox(textBoxEventLog);

            // note widget names are 1-indexed
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

            if (!await wrapper.openAsync())
            {
                logger.error("failed to open MultiChannelWrapper");
                return;
            }

            // enable hardware triggering by default IFF we found a trigger spectrometer
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
            // take position from last character of control name
            CheckBox cb = sender as CheckBox;
            int pos = int.Parse(cb.Name.Substring(cb.Name.Length - 1, 1));

            var spec = wrapper.getSpectrometer(pos);
            if (spec != null)
                spec.multiChannelSelected = cb.Checked;
        }

        void updateGroupBoxTitles(int position=-1)
        {
            foreach (var pos in wrapper.positions)
            {
                if (position > -1 && position != pos)
                    continue;

                var spec = wrapper.getSpectrometer(pos);
                var gb = groupBoxes[pos - 1];
                gb.Text = string.Format("#{0} ({1}) {2}ms {3:f2}°C {4}",
                    pos, 
                    spec.serialNumber, 
                    spec.integrationTimeMS, 
                    spec.lastDetectorTemperatureDegC,
                    spec.firmwareRevision);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // System-Level Control (all spectrometers)
        ////////////////////////////////////////////////////////////////////////

        // user clicked the "enable external hardware triggering" system checkbox 
        // (this is now automatically done at initialization)
        void checkBoxTriggerEnableAll_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = checkBoxTriggerEnableAll.Checked;
            logger.header("Hardware Triggering {0}", enabled ? "enabled" : "disabled");
            wrapper.hardwareTriggeringEnabled = enabled;

            updateScanAveragingAvailability();
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

            // graph optimizers while they run -- this method won't return 
            // until all optimizers have closed
            await Task.Run(() => graphActiveOptimizers(intOpts));

            // report results
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

        bool processSpectra(List<ChannelSpectrum> spectra)
        {
            if (spectra is null)
                return false;

            foreach (var cs in spectra)
                processSpectrum(cs);

            return true;
        }

        // Do whatever we're gonna do with new spectra (save, dark-correct, etc).
        // Since we encapsulated dark subtraction in WasatchNET.Spectrometer, and
        // reflectance in MultiChannelWrapper, there's not a lot to do here...
        void processSpectrum(ChannelSpectrum cs)
        {
            if (cs is null || cs.intensities is null)
                return;

            updateChartAll(cs);
            updateChartSmall(cs);
            updateGroupBoxTitles(cs.pos);
        }

        // update the little graph
        void updateChartSmall(ChannelSpectrum cs) => bindSeries(charts[cs.pos - 1].Series[0], cs);

        // update the big graph
        void updateChartAll(ChannelSpectrum cs) => bindSeries(seriesCombined[cs.pos - 1], cs);

        void bindSeries(Series s, ChannelSpectrum cs)
        {
            if (s is null || cs is null)
                return;
            chartAll.BeginInvoke(new MethodInvoker(delegate { 
                s.Points.DataBindXY(cs.xAxis, cs.intensities); }));
        }

        /// <summary>
        /// Provide a visual monitor of one or more IntegrationOptimizers as
        /// they optimize integration time on various spectrometers.
        /// </summary>
        /// <remarks>
        /// It is assumed that this will be called as a background task via 
        /// 'await', hence uses delegates to graph the collected spectra.
        /// </remarks>
        ///
        /// <returns>
        /// Returns when optimization is finished on all spectrometers (successful
        /// or otherwise).
        /// </returns>
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

        // Currently, no wavelengths or wavenumbers are written, as basically
        // we'd have to write a separate x-axis for each spectrometer.  Instead,
        // just default to pixel.
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

        // User changed the minimum randomly-selectable Monte Carlo integration time
        private void numericUpDownIntegrationTimeMSMin_ValueChanged(object sender, EventArgs e)
        {
            integrationTimeMSRandomMin = (uint)numericUpDownIntegrationTimeMSMin.Value;
        }

        // User changed the maximum randomly-selectable Monte Carlo integration time
        private void numericUpDownIntegrationTimeMSMax_ValueChanged(object sender, EventArgs e)
        {
            integrationTimeMSRandomMax = (uint)numericUpDownIntegrationTimeMSMax.Value;
        }

        // User clicked "Start/Stop" Monte Carlo operation
        private void buttonBatchStart_Click(object sender, EventArgs e)
        {
            if (batchRunning)
                stopBatch();
            else
                startBatch();
        }

        void stopBatch()
        {
            logger.header("stopping batch collection");
            workerBatch.CancelAsync();
        }

        void startBatch()
        {
            logger.header("starting batch collection");

            // prompt user for filename to save data; cancel batch if no filename is provided
            DialogResult result = saveFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            enableControls(false);

            batchRunning = true;

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

            logger.debug("spawning worker");
            workerBatch.RunWorkerAsync();

            enableControls(true);
        }

        void backgroundWorkerBatch_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.CurrentThread.Name = "workerBatch";

            logger.info("Starting Monte Carlo Worker");

            var startTime = DateTime.Now;
            var endTime = startTime.AddMinutes((int)numericUpDownBatchMin.Value);
            logger.debug($"workerBatch will shutdown after {endTime}");

            Random r = new Random();

            using (StreamWriter sw = new StreamWriter(batchPathname))
            {
                ////////////////////////////////////////////////////////////////////
                // Write report header
                ////////////////////////////////////////////////////////////////////

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
                foreach (var pos in wrapper.positions)
                    if (wrapper.getSpectrometer(pos).multiChannelSelected)
                        headers.Add($"Pos{pos}_ShiftedMarkers");
                sw.WriteLine(string.Join<string>(", ", headers));

                Dictionary<int, int> acquisitionCounts = new Dictionary<int, int>();
                Dictionary<int, int> consecutiveReadFailures = new Dictionary<int, int>();
                foreach (var pos in wrapper.positions)
                    acquisitionCounts[pos] = 
                    consecutiveReadFailures[pos] = 0;

                ////////////////////////////////////////////////////////////////////
                // Loop over iterations until Monte Carlo test complete
                ////////////////////////////////////////////////////////////////////

                var triggerCount = 0;
                while (DateTime.Now < endTime)
                {
                    DateTime iterationStart = DateTime.Now;

                    if (workerBatch.CancellationPending)
                    {
                        logger.debug("workerBatch.DoWork cancelled");
                        e.Cancel = true;
                        break;
                    }

                    ////////////////////////////////////////////////////////////////
                    // randomize integration times
                    ////////////////////////////////////////////////////////////////

                    // Note that this can take as long as 4sec (8 x 500ms)
                    foreach (var pos in wrapper.positions)
                    {
                        var spec = wrapper.getSpectrometer(pos);
                        var ms = (uint)r.Next((int)integrationTimeMSRandomMin,
                                              (int)integrationTimeMSRandomMax + 1);

                        // will generate SW-triggered throwaway (all synchronous)
                        spec.integrationTimeMS = ms; 
                    }

                    ////////////////////////////////////////////////////////////
                    // generate trigger (HW or SW)
                    ////////////////////////////////////////////////////////////

                    logger.info($"generating trigger {triggerCount}");
                    _ = wrapper.startAcquisitionAsync().Result;

                    ////////////////////////////////////////////////////////////////
                    // collect spectra
                    ////////////////////////////////////////////////////////////////

                    var spectra = wrapper.getSpectraAsync(sendTrigger: false).Result;
                    foreach (var cs in spectra)
                    {
                        var pos = cs.pos;
                        var spec = wrapper.getSpectrometer(pos);

                        if (cs.intensities != null)
                        {
                            acquisitionCounts[pos]++;
                            consecutiveReadFailures[pos] = 0;
                        }
                        else
                        {
                            if (consecutiveReadFailures[pos]++ >= MAX_CONSECUTIVE_READ_FAILURES)
                                chartAll.BeginInvoke((MethodInvoker)delegate {
                                    checkBoxes[pos].Checked = false; });
                        }
                    }
                    
                    chartAll.BeginInvoke((MethodInvoker)delegate { processSpectra(spectra); });

                    ////////////////////////////////////////////////////////////////
                    // update elapsed time
                    ////////////////////////////////////////////////////////////////

                    var now = DateTime.Now;
                    var nowStr = now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    var elapsedMS = (uint)(now - startTime).TotalMilliseconds;
                    var timeRemaining = (endTime - now).ToString(@"hh\:mm\:ss");
                    labelBatchStatus.BeginInvoke((MethodInvoker)delegate { labelBatchStatus.Text = timeRemaining + " remaining"; });

                    ////////////////////////////////////////////////////////////////
                    // write iteration to file
                    ////////////////////////////////////////////////////////////////

                    List<string> specInteg   = new List<string>();
                    List<string> specAvg     = new List<string>();
                    List<string> specDegC    = new List<string>();
                    List<string> specSpectra = new List<string>();
                    List<string> specShifts  = new List<string>();
                    foreach (var pos in wrapper.positions)
                    {
                        var spec = wrapper.getSpectrometer(pos);
                        if (!spec.multiChannelSelected)
                            continue;

                        var avg = spec.lastSpectrum.Average();
                        var ms = spec.integrationTimeMS;

                        specInteg   .Add(string.Format("{0}", ms));
                        specAvg     .Add(string.Format("{0:f2}", avg));
                        specDegC    .Add(string.Format("{0:f2}", spec.lastDetectorTemperatureDegC));
                        specSpectra .Add(string.Format("{0}", acquisitionCounts[pos]));
                        specShifts  .Add(string.Format("{0}", spec.shiftedMarkerCount));

                        // update tallies
                        if (!tallies[pos].ContainsKey(ms))
                            tallies[pos].Add(ms, new Tally());
                        tallies[pos][ms].total += avg;
                        tallies[pos][ms].count++;
                    }

                    // output record
                    sw.Write($"{triggerCount}, {nowStr}, {timeRemaining}, {elapsedMS}, {wrapper.triggerPulseWidthMS}, ");
                    sw.Write(string.Join<string>(", ", specInteg  ) + ", ");
                    sw.Write(string.Join<string>(", ", specAvg    ) + ", ");
                    sw.Write(string.Join<string>(", ", specDegC   ) + ", ");
                    sw.Write(string.Join<string>(", ", specSpectra) + ", ");
                    sw.Write(string.Join<string>(", ", specShifts ));
                    sw.WriteLine();

                    triggerCount++;
                }
            }

            // shouldn't be any, but just in case
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

        private void checkBoxIntegThrowaways_CheckedChanged(object sender, EventArgs e)
        {
            wrapper.integrationThrowaways = checkBoxIntegThrowaways.Checked;
        }

        private void checkBoxContinuousAcquisition_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = checkBoxContinuousAcquisition.Checked;
            wrapper.useContinuousAcquisition = enabled;
            updateScanAveragingAvailability();
        }

        void updateScanAveragingAvailability()
        {
            if (wrapper.hardwareTriggeringEnabled)
                numericUpDownScansToAverage.Enabled = wrapper.useContinuousAcquisition;
            else
                numericUpDownScansToAverage.Enabled = true;
        }
    }
}
