using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WasatchNET;

namespace WinFormDemo
{
    public partial class Form1 : Form
    {
        ////////////////////////////////////////////////////////////////////////
        // Inner types
        ////////////////////////////////////////////////////////////////////////

        enum ProcessingMode { SCOPE, ABSORBANCE, TRANSMISSION };

        class SpectrometerState
        {
            public Spectrometer spectrometer;
            public BackgroundWorker worker = new BackgroundWorker();
            public Series series = new Series();
            public bool running;
            public double[] raw;
            public double[] spectrum;
            public double[] reference;
            public ProcessingMode processingMode = ProcessingMode.SCOPE;
            public DateTime lastUpdate;
            Logger logger = Logger.getInstance();

            public SpectrometerState(Spectrometer s)
            {
                spectrometer = s;
                series.Name = s.serialNumber;
                series.ChartType = SeriesChartType.Line;
                worker.WorkerReportsProgress = false;
                worker.WorkerSupportsCancellation = true;
            }

            public void processSpectrum(double[] latest)
            {
                raw = latest;
                lastUpdate = DateTime.Now;

                // note: we are using the dark-corrected versions of these functions
                if (processingMode == ProcessingMode.TRANSMISSION && reference != null && spectrometer.dark != null)
                    spectrum = Util.cleanNan(Util.computeTransmission(reference, raw));
                else if (processingMode == ProcessingMode.ABSORBANCE && reference != null && spectrometer.dark != null)
                    spectrum = Util.cleanNan(Util.computeAbsorbance(reference, raw));
                else
                    spectrum = raw;
            }
        }

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
       
        ////////////////////////////////////////////////////////////////////////
        // Methods
        ////////////////////////////////////////////////////////////////////////

        public Form1()
        {
            InitializeComponent();

            // Don't know why I have to do this...never needed to under Win7/VS2010 :-(
            splitContainerGraphVsControls.SplitterDistance = splitContainerGraphVsControls.Width - (groupBoxControl.Width + 12);

            // attach the Driver's logger to our textbox
            logger.setTextBox(textBoxEventLog);

            Text = String.Format("Wasatch.NET WinForm Demo (v{0})", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            // select the first X-Axis option
            comboBoxXAxis.SelectedIndex = 0;

            settings = new Settings(treeViewSettings);

            // kick this off to flush log messages from the GUI thread
            backgroundWorkerGUIUpdate.RunWorkerAsync();
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            shutdownPending = true;
        }   

        ////////////////////////////////////////////////////////////////////////
        // Business Logic
        ////////////////////////////////////////////////////////////////////////

        void initializeSpectrometer(Spectrometer s)
        {
            SpectrometerState state = new SpectrometerState(s);

            // TODO: move into SpectrometerState ctor
            state.worker.DoWork += backgroundWorker_DoWork;
            state.worker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;

            spectrometerStates.Add(s, state);

            chart1.Series.Add(state.series);

            // one or more of these seriously increases laser power
            s.linkLaserModToIntegrationTime(false);
            s.setLaserModulationEnable(false);
            s.setCCDTriggerSource(0);
            s.setLaserEnable(false);

            s.integrationTimeMS = 100;

            logger.info("Found {0} {1} with {2} pixels from {3:f2} to {4:f2}nm",
                s.model, s.serialNumber, s.pixels, s.wavelengths[0], s.wavelengths[s.wavelengths.Length-1]);
        }

        void updateCurrentSpectrometer()
        {
            if (currentSpectrometer == null)
            {
                groupBoxSettings.Enabled = false;
                groupBoxControl.Enabled = false;
                return;
            }

            SpectrometerState state = spectrometerStates[currentSpectrometer];

            // not sure which of these we should do
            // settings.update(currentSpectrometer);
            treeViewSettings_DoubleClick(null, null);

            updateStartButton(spectrometerStates[currentSpectrometer].running);

            checkBoxTakeDark.Enabled = 
                buttonSave.Enabled = state.spectrum != null;
            checkBoxTakeReference.Enabled = state.spectrum != null && currentSpectrometer.dark != null;

            if (state.processingMode == ProcessingMode.SCOPE)
                radioButtonModeScope.Checked = true;
            else if (state.processingMode == ProcessingMode.TRANSMISSION)
                radioButtonModeTransmission.Checked = true;
            else
                radioButtonModeAbsorbance.Checked = true;

            groupBoxSettings.Enabled = true;
            groupBoxControl.Enabled = true;
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
                foreach (Spectrometer spectrometer in spectrometers)
                {
                    SpectrometerState state = spectrometerStates[spectrometer];

                    if (state.spectrum == null)
                    {
                        // logger.debug("not graphing because spectrum null");
                        continue;
                    }

                    Series series = state.series;
                    series.Points.Clear();

                    if (graphWavenumbers && spectrometer.wavenumbers != null)
                    {
                        for (uint i = 0; i < spectrometer.pixels; i++)
                            series.Points.AddXY(spectrometer.wavenumbers[i], state.spectrum[i]);
                    }
                    else
                    {
                        for (uint i = 0; i < spectrometer.pixels; i++)
                            series.Points.AddXY(spectrometer.wavelengths[i], state.spectrum[i]);
                    }

                    // current spectrometer (now) has spectra, so allow darks and traces
                    if (spectrometer == currentSpectrometer)
                        checkBoxTakeDark.Enabled =
                            buttonAddTrace.Enabled = 
                            buttonSave.Enabled = true;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // GUI Callbacks
        ////////////////////////////////////////////////////////////////////////

        private void checkBoxVerbose_CheckedChanged(object sender, EventArgs e)
        {
            logger.level = checkBoxVerbose.Checked ? Logger.LogLevel.DEBUG : Logger.LogLevel.INFO;
        }

        private void buttonInitialize_Click(object sender, EventArgs e)
        {
            comboBoxSpectrometer.Items.Clear();
            groupBoxSpectrometers.Enabled = false;

            if (driver.openAllSpectrometers() > 0)
            {
                spectrometers.Clear();
                for (int i = 0; i < driver.getNumberOfSpectrometers(); i++)
                {
                    Spectrometer s = driver.getSpectrometer(i);
                    spectrometers.Add(s);
                    comboBoxSpectrometer.Items.Add(String.Format("{0} ({1})", s.model, s.serialNumber));
                    initializeSpectrometer(s);
                }

                buttonInitialize.Enabled = false;
                groupBoxSpectrometers.Enabled = true;

                comboBoxSpectrometer.SelectedIndex = 0;

                AcceptButton = buttonStart;

                // go ahead and click it...you know you want to
                Thread.Sleep(200); // helps?
                buttonStart_Click(null, null);
            }
            else
            {
                logger.info("No Wasatch Photonics spectrometers were found.");
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            SpectrometerState state = spectrometerStates[currentSpectrometer];
            if (!state.running)
            {
                updateStartButton(true);
                state.worker.RunWorkerAsync(currentSpectrometer);
            }
            else
            {
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
            currentSpectrometer.integrationTimeMS = (uint)numericUpDownIntegTimeMS.Value;
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
            currentSpectrometer.setLaserEnable(checkBoxLaserEnable.Checked);
        }

        private void checkBoxTakeDark_CheckedChanged(object sender, EventArgs e)
        {
            SpectrometerState state = spectrometerStates[currentSpectrometer];
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
            SpectrometerState state = spectrometerStates[currentSpectrometer];
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
            spectrometerStates[currentSpectrometer].processingMode = ProcessingMode.SCOPE;
        }

        private void radioButtonModeAbsorbance_CheckedChanged(object sender, EventArgs e)
        {
            spectrometerStates[currentSpectrometer].processingMode = ProcessingMode.ABSORBANCE;
        }

        private void radioButtonModeTransmission_CheckedChanged(object sender, EventArgs e)
        {
            spectrometerStates[currentSpectrometer].processingMode = ProcessingMode.TRANSMISSION;
        }

        private void buttonAddTrace_Click(object sender, EventArgs e)
        {
            SpectrometerState state = spectrometerStates[currentSpectrometer];

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

        private void buttonSave_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = String.Format("{0}-{1}", currentSpectrometer.serialNumber, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return;

            // More complex implementations could save all spectra from all spectrometers;
            // or include snapped traces; or export directly to multi-tab Excel spreadsheets.

            lock (spectrometers)
            {
                SpectrometerState state = spectrometerStates[currentSpectrometer];
                if (state.spectrum == null)
                    return;

                // assemble the columns we're going to save
                List<Tuple<string, double[]>> cols = new List<Tuple<string, double[]>>();

                cols.Add(new Tuple<string, double[]>("wavelength", currentSpectrometer.wavelengths));
                if (graphWavenumbers)
                    cols.Add(new Tuple<string, double[]>("wavenumber", currentSpectrometer.wavenumbers));

                string label = "spectrum";
                if (state.processingMode == ProcessingMode.TRANSMISSION)
                    label = "trans/refl";
                else if (state.processingMode == ProcessingMode.ABSORBANCE)
                    label = "abs";
                cols.Add(new Tuple<string, double[]>(label, state.spectrum));

                if (state.raw != state.spectrum)
                    cols.Add(new Tuple<string, double[]>("raw", state.raw));

                if (state.reference != null)
                    cols.Add(new Tuple<string, double[]>("reference", state.reference));

                if (currentSpectrometer.dark != null)
                    cols.Add(new Tuple<string, double[]>("dark", currentSpectrometer.dark));

                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(saveFileDialog1.FileName))
                {
                    // metadata
                    outfile.WriteLine("model,{0}", currentSpectrometer.model);
                    outfile.WriteLine("serialNumber,{0}", currentSpectrometer.serialNumber);
                    outfile.WriteLine("timestamp,{0}", state.lastUpdate);
                    outfile.WriteLine("integration time (ms),{0}", currentSpectrometer.integrationTimeMS);
                    outfile.WriteLine("scan averaging,{0}", currentSpectrometer.scanAveraging);
                    outfile.WriteLine("boxcar,{0}", currentSpectrometer.boxcarHalfWidth);
                    outfile.WriteLine();

                    // header row
                    outfile.Write("pixel");
                    for (int i = 0; i < cols.Count; i++)
                        outfile.Write(",{0}", cols[i].Item1);
                    outfile.WriteLine();

                    // data
                    for (uint pixel = 0; pixel < currentSpectrometer.pixels; pixel++)
                    {
                        outfile.Write(String.Format("{0}", pixel));
                        foreach (Tuple<string, double[]> col in cols)
                            outfile.Write(String.Format(",{0}", col.Item2[pixel]));
                        outfile.WriteLine();
                    }                        
                }
            }
        }

        private void treeViewSettings_DoubleClick(object sender, EventArgs e)
        {
            if (!backgroundWorkerSettings.IsBusy)
                backgroundWorkerSettings.RunWorkerAsync();
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
            while(true)
            {
                if (worker.CancellationPending || shutdownPending)
                    break;
                Thread.Sleep(100);
                chart1.BeginInvoke(new MethodInvoker(delegate { updateGraph(); }));
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
            SpectrometerState state = spectrometerStates[spectrometer];

            string prefix = String.Format("Worker.{0}.{1}", spectrometer.model, spectrometer.serialNumber);
            state.running = true;

            BackgroundWorker worker = sender as BackgroundWorker;
            while (true)
            {
                // logger.debug("workerAcquisition: getting spectrum");
                double[] raw = spectrometer.getSpectrum();

                // process for graphing
                lock(spectrometers)
                    state.processSpectrum(raw);

                // end thread if we've been asked to cancel
                if (worker.CancellationPending)
                    break;

                // for debugging only
                Thread.Sleep(10);
            }

            state.running = false;
            e.Result = spectrometer; // pass spectrometer handle to _Completed callback
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Spectrometer spectrometer = e.Result as Spectrometer;
            if (spectrometer == currentSpectrometer)
                updateStartButton(false);
        }

        private void toolStripMenuItemTestWriteEEPROM_Click(object sender, EventArgs e)
        {

        }
    }
}