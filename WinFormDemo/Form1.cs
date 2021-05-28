using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WasatchNET;

namespace WinFormDemo
{
    public partial class Form1 : Form
    {
        const bool useTasks = false; // whether you want to use BackgroundWorkers or async
        const int minThreadSleepMS = 100;
        const int minTaskDelayMS = 250;  // Task.Delay is less accurate than Thread.Sleep

        ////////////////////////////////////////////////////////////////////////
        // Attributes
        ////////////////////////////////////////////////////////////////////////

        Logger logger = Logger.getInstance();
        Driver driver = Driver.getInstance();
        List<Spectrometer> spectrometers = new List<Spectrometer>();
        Spectrometer currentSpectrometer;
        Mutex mut = new Mutex();

        Dictionary<Spectrometer, SpectrometerState> spectrometerStates = new Dictionary<Spectrometer, SpectrometerState>();
        List<Series> traces = new List<Series>();
        bool graphWavenumbers;
        bool shutdownPending;

        Settings settings;
        Options opts;
       
        ////////////////////////////////////////////////////////////////////////
        // Methods
        ////////////////////////////////////////////////////////////////////////

        public Form1(Options options)
        {
            InitializeComponent();

            // Don't know why I have to do this...never needed to under Win7/VS2010 :-(
            splitContainerGraphVsControls.SplitterDistance = splitContainerGraphVsControls.Width - (groupBoxControl.Width + 12);

            // attach the Driver's logger to our textbox
            logger.setTextBox(textBoxEventLog);

            Text = String.Format("Wasatch.NET WinForm Demo (v{0})", driver.version);

            // select the first X-Axis option
            comboBoxXAxis.SelectedIndex = 0;

            settings = new Settings(treeViewSettings);

            // kick this off to flush log messages from the GUI thread
            backgroundWorkerGUIUpdate.RunWorkerAsync();

            opts = options;
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            if (opts.autoStart)
                buttonInitialize_Click(null, null);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            logger.debug("OnFormClosing: start");
            if (!shutdownPending)
            {
                backgroundWorkerGUIUpdate.CancelAsync();
                Thread.Sleep(minThreadSleepMS);

                bool readyToShutdown = true;
                lock (spectrometers)
                {
                    foreach (var pair in spectrometerStates)
                    {
                        var state = pair.Value;
                        if (state.running)
                            readyToShutdown = false;
                    }
                }

                if (!readyToShutdown)
                {
                    logger.debug("OnFormClosing: raised shutdownPending");
                    shutdownPending = true;
                    e.Cancel = true;
                    return;
                }
            }

            logger.debug("OnFormClosing: completing");
            logger.close();
            // driver.closeAllSpectrometers();
        }   

        ////////////////////////////////////////////////////////////////////////
        // Business Logic
        ////////////////////////////////////////////////////////////////////////

        SpectrometerState currentSpectrometerState(string label="???")
        {
            if (currentSpectrometer is null)
            {
                logger.error($"Unexpected[{label}]: no current spectrometer");
                return null;
            }

            if (!spectrometerStates.ContainsKey(currentSpectrometer))
            {
                logger.error($"Unexpected[{label}]: missing state for {currentSpectrometer.serialNumber}");
                return null;
            }

            return spectrometerStates[currentSpectrometer];
        }

        void initializeSpectrometer(Spectrometer s)
        {
            SpectrometerState state = new SpectrometerState(s, opts);

            // TODO: move into SpectrometerState ctor
            if (!useTasks)
            {
                state.worker.DoWork += backgroundWorker_DoWork;
                state.worker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            }

            spectrometerStates.Add(s, state);

            chart1.Series.Add(state.series);

            if (!s.isARM)
                s.triggerSource = TRIGGER_SOURCE.INTERNAL;

            numericUpDownIntegTimeMS.Minimum = s.eeprom.minIntegrationTimeMS;
            numericUpDownIntegTimeMS.Value = s.integrationTimeMS;
            // numericUpDownIntegTimeMS.Maximum = s.eeprom.maxIntegrationTimeMS; // disabled to allow long integration times

            if (s.pixels > 0)
                logger.info("Found {0} {1} with {2} pixels from {3:f2} to {4:f2}nm",
                    s.model, s.serialNumber, s.pixels, s.wavelengths[0], s.wavelengths[s.wavelengths.Length - 1]);
            else
                logger.error("Found [model: {0}] [serial: {1}] with {2} pixels", s.model, s.serialNumber, s.pixels);

            // default to high-resolution laser power
            s.laserPowerResolution = Spectrometer.LaserPowerResolution.LASER_POWER_RESOLUTION_1000;
        }

        void updateCurrentSpectrometer()
        {
            if (currentSpectrometer is null)
            {
                logger.debug("updateCurrentSpectrometer: null");
                groupBoxSettings.Enabled =
                    groupBoxControl.Enabled =
                    toolStripMenuItemTest.Enabled = false;
                updateStartButton(false);
                return;
            }

            logger.debug($"updateCurrentSpectrometer: {currentSpectrometer.serialNumber}");
            var state = currentSpectrometerState();
            if (state is null)
                return;

            // update tree view
            treeViewSettings_DoubleClick(null, null);

            // update start button
            updateStartButton(state.running);

            // update basic controls
            DemoUtil.expandNUD(numericUpDownIntegTimeMS, (int)currentSpectrometer.integrationTimeMS);
            numericUpDownBoxcarHalfWidth.Value = currentSpectrometer.boxcarHalfWidth;
            numericUpDownScanAveraging.Value = currentSpectrometer.scanAveraging;

            // update TEC controls
            numericUpDownDetectorSetpointDegC.Minimum = (int)currentSpectrometer.eeprom.detectorTempMin;
            numericUpDownDetectorSetpointDegC.Maximum = (int)currentSpectrometer.eeprom.detectorTempMax;
            numericUpDownDetectorSetpointDegC.Enabled = currentSpectrometer.eeprom.hasCooling;

            // update laser controls
            if (currentSpectrometer.hasLaser)
            {
                numericUpDownLaserPowerPerc.Enabled =
                checkBoxLaserEnable.Enabled = true;
                checkBoxLaserEnable.Checked = currentSpectrometer.laserEnabled;

                if (currentSpectrometer.eeprom.hasLaserPowerCalibration())
                {
                    numericUpDownLaserPowerMW.Enabled = true;
                    numericUpDownLaserPowerMW.Maximum = (decimal)currentSpectrometer.eeprom.maxLaserPowerMW;
                    numericUpDownLaserPowerMW.Minimum = (decimal)currentSpectrometer.eeprom.minLaserPowerMW;
                }
                else
                {
                    numericUpDownLaserPowerMW.Enabled = false;
                }
            }
            else
            {
                numericUpDownLaserPowerPerc.Enabled =
                numericUpDownLaserPowerMW.Enabled =
                checkBoxLaserEnable.Enabled =
                checkBoxLaserEnable.Checked = false;
            }

            checkBoxTakeDark.Enabled = buttonSave.Enabled = state.spectrum != null;
            checkBoxTakeReference.Enabled = state.spectrum != null && currentSpectrometer.dark != null;

            if (state.processingMode == SpectrometerState.ProcessingModes.SCOPE)
                radioButtonModeScope.Checked = true;
            else if (state.processingMode == SpectrometerState.ProcessingModes.TRANSMISSION)
                radioButtonModeTransmission.Checked = true;
            else
                radioButtonModeAbsorbance.Checked = true;

            groupBoxSettings.Enabled = 
            groupBoxControl.Enabled = 
            toolStripMenuItemTest.Enabled = 
            labelDetTempDegC.Visible = true;
        }

        void updateStartButton(bool isRunning)
        {
            if (isRunning)
            {
                buttonStart.Text = "Stop";
                buttonStart.BackColor = Color.DarkRed;
            }
            else
            {
                buttonStart.Text = "Start";
                buttonStart.BackColor = Color.DarkGreen;
            }
        }

        void updateGraph()
        {
            lock (spectrometers)
            {
                foreach (var pair in spectrometerStates)
                {
                    var spectrometer = pair.Key;
                    var state = pair.Value;

                    if (state.spectrum is null)
                        continue;

                    Series series = state.series;
                    series.Points.Clear();

                    if (graphWavenumbers && spectrometer.wavenumbers != null)
                        for (uint i = 0; i < spectrometer.pixels; i++)
                            series.Points.AddXY(spectrometer.wavenumbers[i], state.spectrum[i]);
                    else
                        for (uint i = 0; i < spectrometer.pixels; i++)
                            series.Points.AddXY(spectrometer.wavelengths[i], state.spectrum[i]);

                    // extra handling for current spectrometer
                    if (spectrometer == currentSpectrometer)
                    {
                        /// has spectra, so allow darks and traces
                        checkBoxTakeDark.Enabled =
                            buttonAddTrace.Enabled =
                            buttonSave.Enabled = true;

                        labelDetTempDegC.Text = String.Format("{0:f1}°C", state.detTempDegC);
                    }

                }

                chart1.ChartAreas[0].AxisY.IsStartedFromZero = false;
                chart1.ChartAreas[0].RecalculateAxesScale();
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // GUI Callbacks
        ////////////////////////////////////////////////////////////////////////

        private void checkBoxVerbose_CheckedChanged(object sender, EventArgs e)
        {
            logger.level = checkBoxVerbose.Checked ? LogLevel.DEBUG : LogLevel.INFO;
        }

        private async void buttonInitialize_Click(object sender, EventArgs e)
        {
            logger.debug("buttonInitialize clicked");
            mut.WaitOne();

            groupBoxSpectrometers.Enabled = false;

            if (spectrometerStates.Count > 0)
            {
                // this is a re-initialization
                logger.debug("starting reinitialize");

                // not really sure what behavior would be considered "ideal" in this case,
                // but I'm stopping all "running" background threads
                const int delayMS = 1000;
                foreach (var pair in spectrometerStates)
                {
                    var spec = pair.Key;
                    var state = pair.Value;

                    if (useTasks)
                    {
                        state.stopping = true;
                        await Task.Delay(delayMS);
                    }
                    else
                    {
                        state.worker.CancelAsync();
                        Thread.Sleep(delayMS);
                    }

                    while (state.running)
                    {
                        logger.debug("waiting to stop {0}", spec.serialNumber);
                        if (useTasks)
                            await Task.Delay(delayMS);
                        else
                            Thread.Sleep(delayMS);
                    }
                }

                // note that we never actually call closeAllSpectrometers;
                // that is in fact the point of this test
                driver.closeAllSpectrometers();
            }

            logger.debug("initialization proceeding");

            // at this point, no background acquisitions should be running, and we
            // can completely restart state
            comboBoxSpectrometer.Items.Clear();
            spectrometers.Clear();
            spectrometerStates.Clear();
            chart1.Series.Clear();

            if (driver.openAllSpectrometers() > 0)
            {
                for (int i = 0; i < driver.getNumberOfSpectrometers(); i++)
                {
                    Spectrometer s = driver.getSpectrometer(i);
                    currentSpectrometer = s;
                    spectrometers.Add(s);
                    comboBoxSpectrometer.Items.Add(String.Format("{0} ({1})", s.model, s.serialNumber));
                    initializeSpectrometer(s);

                    if (opts.integrationTimeMS > 0)
                        s.integrationTimeMS = opts.integrationTimeMS;

                    comboBoxSpectrometer.SelectedIndex = comboBoxSpectrometer.Items.Count - 1;
                    await Task.Delay(minThreadSleepMS);

                    logger.debug($"clicking Start button for {s.serialNumber}");
                    buttonStart_Click(null, null);
                    await Task.Delay(minThreadSleepMS);
                }

                // allow re-initialization (repeat calls to openAllSpectrometers), as some
                // customers do this when power-cycling "one of many" connected units
                // buttonInitialize.Enabled = false;

                groupBoxSpectrometers.Enabled = true;

                comboBoxSpectrometer.SelectedIndex = 0;

                // AcceptButton = buttonStart;
            }
            else
            {
                logger.info("No Wasatch Photonics spectrometers were found.");
            }

            mut.ReleaseMutex();
        }

        // This only affects the currently-running spectrometer
        private void buttonStart_Click(object sender, EventArgs e)
        {
            var state = currentSpectrometerState();
            if (state is null)
                return;

            if (!state.running)
            {
                logger.info($"Starting {currentSpectrometer.serialNumber}");
                updateStartButton(true);
                if (useTasks)
                    _ = Task_DoWork(currentSpectrometer);
                else
                    state.worker.RunWorkerAsync(currentSpectrometer);
            }
            else
            {
                logger.info($"Stopping {currentSpectrometer.serialNumber}");
                state.stopping = true;
                if (!useTasks)
                    state.worker.CancelAsync();
            }
        }

        private void comboBoxXAxis_SelectedIndexChanged(object sender, EventArgs e)
        {
            graphWavenumbers = "WAVENUMBER" == comboBoxXAxis.SelectedItem.ToString().ToUpper();
            chart1.ChartAreas[0].AxisX.Title = graphWavenumbers ? "Wavenumber (cm⁻¹)" : "Wavelength (nm)";
            updateGraph();
        }

        private void comboBoxSpectrometer_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = comboBoxSpectrometer.SelectedIndex;
            if (index >= 0 && index < spectrometers.Count)
            { 
                currentSpectrometer = spectrometers[index];
                updateCurrentSpectrometer();
            }
        }

        private void numericUpDownIntegTimeMS_ValueChanged(object sender, EventArgs e)
        {
            var ms = (uint)numericUpDownIntegTimeMS.Value;
            logger.debug($"changing currentSpectrometer {currentSpectrometer.serialNumber} integration time to {ms}");
            currentSpectrometer.integrationTimeMS = ms;
        }

        private void numericUpDownScanAveraging_ValueChanged(object sender, EventArgs e)
        {
            currentSpectrometer.scanAveraging = (uint)numericUpDownScanAveraging.Value;
        }

        private void numericUpDownBoxcarHalfWidth_ValueChanged(object sender, EventArgs e)
        {
            currentSpectrometer.boxcarHalfWidth = (uint)numericUpDownBoxcarHalfWidth.Value;
        }

        private void checkBoxLaserEnable_CheckedChanged(object sender, EventArgs e)
        {
            currentSpectrometer.laserEnabled = checkBoxLaserEnable.Checked;
        }

        private void checkBoxTakeDark_CheckedChanged(object sender, EventArgs e)
        {
            var state = currentSpectrometerState();
            if (state is null)
                return;

            lock (spectrometers)
            {
                if (checkBoxTakeDark.Checked)
                {
                    if (state.spectrum != null)
                    {
                        currentSpectrometer.dark = state.spectrum;
                        state.spectrum = new double[currentSpectrometer.pixels];

                        checkBoxTakeReference.Enabled = true;
                    }
                }
                else
                {
                    currentSpectrometer.dark = null;

                    // if we have no dark, then we can have no reference
                    state.reference = null;
                    checkBoxTakeReference.Checked =
                        checkBoxTakeReference.Enabled = false;

                    // if we have no dark, we can do no processing
                    radioButtonModeScope.Checked = true;
                }
            }
        }

        private void checkBoxTakeReference_CheckedChanged(object sender, EventArgs e)
        {
            var state = currentSpectrometerState();
            if (state is null)
                return;

            lock (spectrometers)
            {
                bool success = false;
                if (checkBoxTakeReference.Checked)
                {
                    if (state.spectrum != null)
                    {
                        // business logic should ensure this, but just in case
                        if (currentSpectrometer.dark != null)
                        {
                            state.reference = new double[state.spectrum.Length];
                            Array.Copy(state.spectrum, state.reference, state.spectrum.Length);
                            success = true;

                            radioButtonModeTransmission.Enabled = true;
                            radioButtonModeAbsorbance.Enabled = true;
                        }
                        else
                        {
                            logger.error("Can't take reference without dark");
                        }
                    }
                }

                if (!success)
                {
                    state.reference = null;
                    radioButtonModeTransmission.Enabled = false;
                    radioButtonModeAbsorbance.Enabled = false;
                }
            }
        }

        private void radioButtonModeScope_CheckedChanged(object sender, EventArgs e)
        {
            var state = currentSpectrometerState();
            if (state != null)
                state.processingMode = SpectrometerState.ProcessingModes.SCOPE;
        }

        private void radioButtonModeAbsorbance_CheckedChanged(object sender, EventArgs e)
        {
            var state = currentSpectrometerState();
            if (state != null)
                state.processingMode = SpectrometerState.ProcessingModes.ABSORBANCE;
        }

        private void radioButtonModeTransmission_CheckedChanged(object sender, EventArgs e)
        {
            var state = currentSpectrometerState();
            if (state != null)
                state.processingMode = SpectrometerState.ProcessingModes.TRANSMISSION;
        }

        private void buttonAddTrace_Click(object sender, EventArgs e)
        {
            var state = currentSpectrometerState();
            if (state is null)
                return;

            Series trace = new Series();
            trace.IsVisibleInLegend = false;
            trace.ChartType = SeriesChartType.Line;

            foreach (DataPoint p in state.series.Points)
                trace.Points.Add(p);

            traces.Add(trace);
            chart1.Series.Add(trace);

            buttonClearTraces.Enabled = true;
        }

        private void buttonClearTraces_Click(object sender, EventArgs e)
        {
            foreach (Series trace in traces)
            {
                chart1.Series.Remove(trace);
            }
            traces.Clear();
            buttonClearTraces.Enabled = false;
        }

        public bool initSaveDir()
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return false;

            opts.saveDir = folderBrowserDialog1.SelectedPath;
            return true;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (opts.saveDir.Length == 0)
                if (!initSaveDir())
                    return;

            // More complex implementations could save all spectra from all spectrometers;
            // or include snapped traces; or export directly to multi-tab Excel spreadsheets.

            lock (spectrometers)
            {
                var state = currentSpectrometerState();
                if (state is null || state.spectrum is null)
                    return;

                state.save();
            }
        }

        private void treeViewSettings_DoubleClick(object sender, EventArgs e)
        {
            if (!backgroundWorkerSettings.IsBusy)
                backgroundWorkerSettings.RunWorkerAsync();
        }

        private void toolStripMenuItemTestWriteEEPROM_Click(object sender, EventArgs e)
        {
            if (currentSpectrometer is null)
                return;

            WriteEEPROMForm eepromForm = new WriteEEPROMForm(currentSpectrometer.eeprom);
            eepromForm.ShowDialog();
        }

        private void setDFUModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentSpectrometer is null)
                return;

            DialogResult result = MessageBox.Show("Are you sure? This mode is used for reflashing ARM firmware, and could brick your spectrometer.",
                "Extreme Caution Alert", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            currentSpectrometer.setDFUMode();
        }

        private void numericUpDownLaserPowerPerc_ValueChanged(object sender, EventArgs e)
        {
            if (currentSpectrometer != null)
                currentSpectrometer.setLaserPowerPercentage(((float)numericUpDownLaserPowerPerc.Value) / 100.0f);
        }

        private void numericUpDownLaserPowerMW_ValueChanged(object sender, EventArgs e)
        {
            if (currentSpectrometer != null)
                currentSpectrometer.laserPowerSetpointMW = (float)numericUpDownLaserPowerMW.Value;
        }

        private void numericUpDownDetectorSetpointDegC_ValueChanged(object sender, EventArgs e)
        {
            if (currentSpectrometer != null)
                currentSpectrometer.detectorTECSetpointDegC = (float)numericUpDownDetectorSetpointDegC.Value;
        }

        private void checkBoxExternalTriggerSource_CheckedChanged(object sender, EventArgs e)
        {
            if (currentSpectrometer is null)
                return;

            currentSpectrometer.triggerSource = checkBoxExternalTriggerSource.Checked ? TRIGGER_SOURCE.EXTERNAL : TRIGGER_SOURCE.INTERNAL;
            logger.debug("GUI: trigger source now {0}", currentSpectrometer.triggerSource);
        }

        private void checkBoxContinuousAcquisition_CheckedChanged(object sender, EventArgs e)
        {
            if (currentSpectrometer is null)
                return;

            currentSpectrometer.scanAveragingIsContinuous = checkBoxContinuousAcquisition.Checked;
        }

        private void numericUpDownAcquisitionPeriodMS_ValueChanged(object sender, EventArgs e)
        {
            opts.scanIntervalSec = (uint)(sender as NumericUpDown).Value;
        }
        ////////////////////////////////////////////////////////////////////////
        // BackgroundWorker: GUI Updates
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Update the graph at 10Hz regardless of integration time(s) 
        /// (prevents CPU overruns).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorkerGUIUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            logger.debug("GUIUpdate thread starting");
            BackgroundWorker worker = sender as BackgroundWorker;
            ushort lowFreqOperations = 0;
            while (true)
            {
                Thread.Sleep(minThreadSleepMS);
                if (worker.CancellationPending || shutdownPending)
                {
                    e.Cancel = false;
                    break;
                }

                chart1.BeginInvoke(new MethodInvoker(delegate { updateGraph(); }));

                // once a second, update temperatures
                if (lowFreqOperations++ > 5)
                {
                    if (mut.WaitOne(10))
                    {
                        int count = 0;
                        foreach (var pair in spectrometerStates)
                        {
                            var spectrometer = pair.Key;
                            count += spectrometer.spectrumCount;
                            if (!spectrometer.isARM)
                            {
                                var state = pair.Value;
                                state.detTempDegC = spectrometer.detectorTemperatureDegC;
                            }
                        }
                        labelSpectrumCount.BeginInvoke(new MethodInvoker(delegate { labelSpectrumCount.Text = $"{count}"; }));
                        lowFreqOperations = 0;
                        mut.ReleaseMutex();
                    }
                }
            }
            logger.debug("GUIUpdate thread exiting");
        }

        ////////////////////////////////////////////////////////////////////////
        // BackgroundWorker: Settings Update
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Update TreeView settings in a background thread.
        /// </summary>
        /// <remarks>
        /// Because...
        /// 1. there are a lot of them
        /// 2. if USB errors occur, timeouts can cause this to take awhile
        /// 3. it forces us to test some nice concurrency corner-cases
        /// </remarks>
        private void backgroundWorkerSettings_DoWork(object sender, DoWorkEventArgs e)
        {
            logger.debug("Settings thread starting");
            settings.updateAll(currentSpectrometer);
            logger.debug("Settings thread exiting");
        }

        ////////////////////////////////////////////////////////////////////////
        // Background Worker: Acquisition Threads
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Perform all acquisitions in background threads so the GUI stays responsive.
        /// </summary>
        /// <remarks>
        /// Note that this method is used by potentially several different 
        /// BackgroundWorkers in parallel (one per attached spectrometer).
        ///
        /// TODO: rename backgroundWorkerAcquisition
        /// </remarks>
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Spectrometer spectrometer = (Spectrometer) e.Argument;
            if (!spectrometerStates.ContainsKey(spectrometer))
            {
                logger.error("Unexpected: missing state for {spectrometer.serialNumber}");
                return;
            }
            var state = spectrometerStates[spectrometer];

            state.running = true;
            state.stopping = false;

            BackgroundWorker worker = sender as BackgroundWorker;
            Thread.CurrentThread.Name = $"worker-{spectrometer.serialNumber}";

            while (true)
            {
                // end thread if we've been asked to cancel
                if (worker.CancellationPending || state.stopping || shutdownPending)
                {
                    logger.debug("worker closing because cancellationPending or stopping or shutdownPending");
                    e.Cancel = false;
                    break;
                }

                bool ok = doAcquireIteration(state).Result;
                if (!ok)
                    break;
            }

            logger.debug("worker closing");
            state.running = false; // shouldn't need this, but RunWorkerCompleted isn't always firing?
            e.Result = spectrometer; // pass spectrometer handle to _Completed callback
        }

        async Task<bool> Task_DoWork(Spectrometer spectrometer)
        {
            if (!spectrometerStates.ContainsKey(spectrometer))
            {
                logger.error("Unexpected: missing state for {spectrometer.serialNumber}");
                return false;
            }
            var state = spectrometerStates[spectrometer];

            state.running = true;
            state.stopping = false;

            while (true)
            {
                // end Task if we've been asked to cancel
                if (shutdownPending || state.stopping)
                    break;

                logger.debug("Task_DoWork: not stopping");
                if (!await doAcquireIteration(state))
                    break;
            }

            logger.debug("Task_DoWork closing");
            doComplete(spectrometer);
            return true;
        }

        async Task<bool> doAcquireIteration(SpectrometerState state)
        {
            DateTime startTime = DateTime.Now;

            logger.debug("doAcquireIteration: getting spectrum");
            double[] raw = state.spectrometer.getSpectrum();
            if (raw is null)
            {
                if (useTasks)
                    await Task.Delay(minTaskDelayMS);
                else
                    Thread.Sleep(minTaskDelayMS);
                return false;
            }

            // process for graphing
            logger.debug("doAcquireIteration: processing spectrum");
            lock (spectrometers)
            {
                state.processSpectrum(raw);
            }
            logger.debug("doAcquireIteration: done processing spectrum");

            // end thread if we've completed our allocated acquisitions
            if (opts.scanCount > 0 && state.scanCount >= opts.scanCount)
                return false;

            // figure out how long to wait before the next iteration
            int delayMS = 0;
            if (opts.scanIntervalSec != 0)
            {
                DateTime endTime = DateTime.Now;
                var elapsedMS = (endTime - startTime).TotalMilliseconds;
                delayMS = (int)(opts.scanIntervalSec * 1000.0 - elapsedMS);
            }

            delayMS = Math.Max(delayMS, minTaskDelayMS);
            if (delayMS > 0)
                if (useTasks)
                    await Task.Delay(delayMS);
                else
                    Thread.Sleep(delayMS);

            logger.debug("doAcquireIteration: done");
            return true;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var spectrometer = e.Result as Spectrometer;
            logger.debug($"Worker complete: {spectrometer.serialNumber}");
            doComplete(spectrometer);
        }

        // can be called from either BackgroundWorker or Task
        void doComplete(Spectrometer spectrometer)
        {
            logger.debug($"doComplete: {spectrometer.serialNumber}");
            if (spectrometer == currentSpectrometer)
                updateStartButton(false);

            if (!spectrometerStates.ContainsKey(spectrometer))
            {
                // this can happen during deliberate disconnect events
                logger.error($"Unexpected: spectrometer {spectrometer.serialNumber} has no tracked state");
                return;
            }

            var state = spectrometerStates[spectrometer];
            state.running = false;

            // should we auto-exit?
            if (shutdownPending || (opts.autoStart && opts.scanCount > 0))
            {
                List<string> waitList = new List<string>();
                lock (spectrometers)
                {
                    foreach (Spectrometer s in spectrometers)
                    {
                        if (!spectrometerStates.ContainsKey(s))
                        {
                            logger.error($"Unexpected: missing spectrometerState {s.serialNumber}");
                            continue;
                        }

                        SpectrometerState ss = spectrometerStates[s];
                        if (ss.scanCount < opts.scanCount)
                            waitList.Add(s.serialNumber);
                    }
                }

                if (waitList.Count == 0)
                    Close(); // will re-trigger OnFormClosing
                else
                    logger.debug("shutdown still pending %s", string.Join(", ", waitList));
            }
        }

    }
}
