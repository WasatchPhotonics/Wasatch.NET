using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WasatchNET;

namespace WinFormDemo
{
    public partial class Form1 : Form
    {

        ////////////////////////////////////////////////////////////////////////
        // Attributes
        ////////////////////////////////////////////////////////////////////////

        Logger logger = Logger.getInstance();
        Driver driver = Driver.getInstance();
        List<Spectrometer> spectrometers = new List<Spectrometer>();
        Spectrometer currentSpectrometer;

        Dictionary<Spectrometer, SpectrometerState> spectrometerStates = new Dictionary<Spectrometer, SpectrometerState>();
        List<Series> traces = new List<Series>();
        bool graphWavenumbers;
        bool shutdownPending;

        Settings settings;
        Options opts;

        private Timer timer = new System.Windows.Forms.Timer();

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
            logger.close();
            driver.closeAllSpectrometers();
        }   

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
            logger.debug($"initializing spectrometer {s.eeprom.serialNumber}");

            logger.debug("adding to spectrometerStates and series");
            spectrometerStates.Add(s, state);

            chart1.Series.Add(state.series);

            if (!s.isARM)
            {
                logger.debug("not ARM, so disabling external trigger (in case it had been set)");
                s.triggerSource = TRIGGER_SOURCE.INTERNAL;
            }

            logger.debug("applying integration time limits");
            numericUpDownIntegTimeMS.Minimum = s.eeprom.minIntegrationTimeMS;
            numericUpDownIntegTimeMS.Value = s.integrationTimeMS;
            // numericUpDownIntegTimeMS.Maximum = s.eeprom.maxIntegrationTimeMS; // disabled to allow long integration times

            if (s.pixels > 0)
                logger.info("Found {0} {1} with {2} pixels from {3:f2} to {4:f2}nm",
                    s.model, s.serialNumber, s.pixels, s.wavelengths[0], s.wavelengths[s.wavelengths.Length - 1]);
            else
                logger.error("Found [model: {0}] [serial: {1}] with {2} pixels", s.model, s.serialNumber, s.pixels);

            logger.debug("defaulting to high-resolution laser power");
            s.laserPowerResolution = Spectrometer.LaserPowerResolution.LASER_POWER_RESOLUTION_1000;

            logger.debug("done initializing spectrometer");
        }

        void updateCurrentSpectrometer()
        {
            logger.debug("updateCurrentSpectrometer: start");
            if (currentSpectrometer is null)
            {
                return;
            }

            logger.debug($"updateCurrentSpectrometer: {currentSpectrometer.serialNumber}");
            var state = currentSpectrometerState();
            if (state is null)
                return;

            logger.debug("updating settings");
            treeViewSettings_DoubleClick(null, null); 

            logger.debug("update basic controls");
            DemoUtil.expandNUD(numericUpDownIntegTimeMS, (int)currentSpectrometer.integrationTimeMS);
            numericUpDownBoxcarHalfWidth.Value = currentSpectrometer.boxcarHalfWidth;
            numericUpDownScanAveraging.Value = currentSpectrometer.scanAveraging;

            logger.debug("update high-gain mode");
            if (currentSpectrometer.isInGaAs)
            {
                checkBoxHighGainMode.Enabled = true;
                checkBoxHighGainMode.Checked = currentSpectrometer.highGainModeEnabled;
            }
            else
            {
                checkBoxHighGainMode.Enabled = false;
            }

            logger.debug("update TEC controls");
            numericUpDownDetectorSetpointDegC.Minimum = (int)currentSpectrometer.eeprom.detectorTempMin;
            numericUpDownDetectorSetpointDegC.Maximum = (int)currentSpectrometer.eeprom.detectorTempMax;
            numericUpDownDetectorSetpointDegC.Enabled = currentSpectrometer.eeprom.hasCooling;

            logger.debug("update laser controls");
            if (currentSpectrometer.hasLaser)
            {
                logger.debug("has laser, so enabling controls");
                checkBoxLaserEnable.Enabled = true;
                checkBoxLaserEnable.Checked = currentSpectrometer.laserEnabled;

                if (currentSpectrometer.eeprom.hasLaserPowerCalibration())
                {
                    logger.debug("configuring laser power limits");
                    checkBoxLaserPowerInMW.Checked = true;
                    checkBoxLaserPowerInMW.Enabled = true;
                    numericUpDownLaserPowerMW.Maximum = (decimal)currentSpectrometer.eeprom.maxLaserPowerMW;
                    numericUpDownLaserPowerMW.Minimum = (decimal)currentSpectrometer.eeprom.minLaserPowerMW;
                    logger.debug("done configuring laser power limits");
                }
                else
                {
                    logger.debug("has no laser power calibration");
                    checkBoxLaserPowerInMW.Checked = false;
                    checkBoxLaserPowerInMW.Enabled = false;
                }
            }
            else
            {
                logger.debug("no laser, so disabling controls");
                numericUpDownLaserPowerPerc.Enabled =
                numericUpDownLaserPowerMW.Enabled =
                checkBoxLaserEnable.Enabled =
                checkBoxLaserPowerInMW.Enabled = 
                checkBoxLaserEnable.Checked = false;
            }

            //checkBoxAccessoriesEnabled.Enabled = 

            logger.debug("configuring dark/ref");
            checkBoxTakeDark.Enabled = buttonSave.Enabled = state.spectrum != null;
            checkBoxTakeReference.Enabled = state.spectrum != null && currentSpectrometer.dark != null;

            logger.debug("configuring technique");
            if (state.processingMode == SpectrometerState.ProcessingModes.SCOPE)
                radioButtonModeScope.Checked = true;
            else if (state.processingMode == SpectrometerState.ProcessingModes.TRANSMISSION)
                radioButtonModeTransmission.Checked = true;
            else
                radioButtonModeAbsorbance.Checked = true;

            logger.debug("enabling options");
            groupBoxSettings.Enabled = 
            groupBoxControl.Enabled = 
            toolStripMenuItemTest.Enabled = 
            labelDetTempDegC.Visible = true;

            logger.debug("done updating current spectrometer");
        }

        void updateGraph()
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

            comboBoxSpectrometer.Items.Clear();
            spectrometers.Clear();
            spectrometerStates.Clear();
            chart1.Series.Clear();

            if (driver.openAllSpectrometers() > 0)
            {
                logger.debug("successfully openedAllSpectrometers");
                for (int i = 0; i < driver.getNumberOfSpectrometers(); i++)
                {
                    logger.debug($"preparing spectrometer {i}");
                    Spectrometer s = driver.getSpectrometer(i);
                    currentSpectrometer = s;
                    spectrometers.Add(s);
                    comboBoxSpectrometer.Items.Add(String.Format("{0} ({1})", s.model, s.serialNumber));
                    initializeSpectrometer(s);
                    logger.debug("back from initializing spectrometer");

                    if (opts.integrationTimeMS > 0)
                    {
                        logger.debug("overriding integration time");
                        s.integrationTimeMS = opts.integrationTimeMS;
                    }
                }

                logger.debug("selecting first spectrometer");
                comboBoxSpectrometer.SelectedIndex = 0;
            }
            else
            {
                logger.info("No Wasatch Photonics spectrometers were found.");
            }

            groupBoxSpectrometers.Enabled = true;
        }

        // This only affects the currently-running spectrometer
        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text == "Start")
            {

                // start getting spectra
                timer.Interval = 2500; // 1000 / 10;
                int tickCount = 0;
                timer.Tick += delegate (object senderT, EventArgs args)
                {
                    acquireAllSpectrometers();
                    updateGraph();

                    tickCount++;

                    if (tickCount >= 4)
                    {
                        timer.Interval = 1000 / 10;
                    }
                };
                timer.Start();
                

                // set button to say "Stop"
                buttonStart.Text = "Stop";
                buttonStart.BackColor = Color.DarkRed;
            }
            else
            {
                // stop getting spectra
                timer.Stop();       

                // set button to say "Start
                buttonStart.Text = "Start";
                buttonStart.BackColor = Color.DarkGreen;
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
            logger.debug($"selecting spectrometer {index}");
            if (index >= 0 && index < spectrometers.Count)
            { 
                currentSpectrometer = spectrometers[index];
                updateCurrentSpectrometer();
            }
            logger.debug($"done selecting spectrometer {index}");
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

        private void checkBoxRamanCorrection_CheckedChanged(object sender, EventArgs e)
        {
            logger.debug($"changing currentSpectrometer {currentSpectrometer.serialNumber} raman correction to {checkBoxRamanCorrection.Enabled}");
            currentSpectrometer.ramanIntensityCorrectionEnabled = checkBoxRamanCorrection.Checked;
            checkBoxTakeDark.Enabled = !checkBoxRamanCorrection.Checked;

        }

        private void checkBoxHighGainMode_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            logger.debug($"changing currentSpectrometer {currentSpectrometer.serialNumber} High-Gain Mode to {cb.Checked}");
            currentSpectrometer.highGainModeEnabled = cb.Checked;
        }

        private void checkBoxAccessoriesEnabled_CheckedChanged(object sender, EventArgs e)
        {
            logger.debug($"changing currentSpectrometer {currentSpectrometer.serialNumber} accessory enabled to {checkBoxAccessoriesEnabled.Enabled}");
            currentSpectrometer.accessoryEnabled = checkBoxAccessoriesEnabled.Checked;
            checkBoxLampEnabled.Enabled = checkBoxAccessoriesEnabled.Checked;
        }

        private void checkBoxLampEnabled_CheckedChanged(object sender, EventArgs e)
        {
            logger.debug($"changing currentSpectrometer {currentSpectrometer.serialNumber} lamp enabled to {checkBoxLampEnabled.Enabled}");
            currentSpectrometer.lampEnabled = checkBoxLampEnabled.Checked;
            logger.debug($"currentSpectrometer {currentSpectrometer.serialNumber} lamp enabled after set attempt is: {currentSpectrometer.lampEnabled}");
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
                    // a dark can and should only be taken when raman intensity correction is disabled
                    if (state.spectrum != null && !state.spectrometer.ramanIntensityCorrectionEnabled)
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

        private void toolStripMenuItemEEPROMJson_Click(object sender, EventArgs e)
        {
            if (currentSpectrometer is null)
                return;

            var eepromJSON = currentSpectrometer.eeprom.toJSON();
            logger.info("EEPROM as JSON: {0}", eepromJSON);
        }

        private void toolStripMenuItemLoadFromJSON_Click(object sender, EventArgs e)
        {
            if (currentSpectrometer is null)
                return;

            openFileDialog1.DefaultExt = "json";
            var result = openFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return;

            string pathname = openFileDialog1.FileName;
            if (!currentSpectrometer.loadFromJSON(pathname))
            {
                logger.error($"Failed to load EEPROM from {pathname}");
                return;
            }

            logger.info($"Successfully loaded EEPROM from {pathname}");
            settings.update(currentSpectrometer);
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
            if (currentSpectrometer == null)
                return;
            var perc = (float)(sender as NumericUpDown).Value / 100.0f;
            logger.debug($"setting laser power to {perc}");
            currentSpectrometer.setLaserPowerPercentage(perc);
        }

        private void numericUpDownLaserPowerMW_ValueChanged(object sender, EventArgs e)
        {
            if (currentSpectrometer == null)
                return;
            var mW = (float)(sender as NumericUpDown).Value;
            logger.debug($"setting laser power to {mW}mW");
            currentSpectrometer.laserPowerSetpointMW = mW;
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
            return;

            logger.debug("GUIUpdate thread starting");
            BackgroundWorker worker = sender as BackgroundWorker;
            ushort lowFreqOperations = 0;
            while (true)
            {
                if (worker.CancellationPending || shutdownPending)
                {
                    e.Cancel = false;
                    break;
                }

                chart1.BeginInvoke(new MethodInvoker(delegate { updateGraph(); }));

                // once a second, update temperatures
                if (lowFreqOperations++ > 5)
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
        private void acquireAllSpectrometers()
        {
            foreach (Spectrometer spectrometer in spectrometers)
            {
                if (!spectrometerStates.ContainsKey(spectrometer))
                {
                    logger.error("Unexpected: missing state for {spectrometer.serialNumber}");
                    return;
                }
                var state = spectrometerStates[spectrometer];
                bool ok = doAcquireIteration(state);
                // if (!ok) // do something   
            }
        }

        bool doAcquireIteration(SpectrometerState state)
        {
            DateTime startTime = DateTime.Now;

            logger.debug("doAcquireIteration: getting spectrum");
            double[] raw = state.spectrometer.getSpectrum();
            if (raw is null)
            {
                return false;
            }

            // process for graphing
            logger.debug("doAcquireIteration: processing spectrum");
            state.processSpectrum(raw);
            logger.debug("doAcquireIteration: done processing spectrum");

            return true;
        }

        // can be called from either BackgroundWorker or Task
        void doComplete(Spectrometer spectrometer)
        {
            logger.debug($"doComplete: {spectrometer.serialNumber}");

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

        private void checkBoxLaserPowerInMW_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            numericUpDownLaserPowerMW.Enabled = cb.Checked;
            numericUpDownLaserPowerPerc.Enabled = !cb.Checked;
        }
    }
}
