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

        Logger logger = Logger.getInstance();
        Driver driver = Driver.getInstance();
        List<Spectrometer> spectrometers = new List<Spectrometer>();
        Spectrometer currentSpectrometer;
        Dictionary<string, Tuple<TreeNode, string>> treeNodes = new Dictionary<string, Tuple<TreeNode, string>>();
        Dictionary<Spectrometer, SpectrometerState> spectrometerStates = new Dictionary<Spectrometer, SpectrometerState>();
        List<Series> traces = new List<Series>();
        bool graphWavenumbers;
        bool shutdownPending;
       
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

            stubTreeView();

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
            state.worker.ProgressChanged += backgroundWorker_ProgressChanged;
            state.worker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;

            spectrometerStates.Add(s, state);

            chart1.Series.Add(state.series);

            // one or more of these seriously increases laser power
            s.linkLaserModToIntegrationTime(false);
            s.setLaserMod(false);
            s.setIntegrationTimeMS(100);
            s.setCCDTriggerSource(0);
            s.setLaserEnable(false);
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

            updateSettings();

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
                        continue;

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
        // Settings Tree View
        ////////////////////////////////////////////////////////////////////////

        #region settings
        void updateSettings()
        {
            if (currentSpectrometer == null)
                return;

            // methods 
            updateSetting("firmwareRev", currentSpectrometer.getFirmwareRev());
            updateSetting("fpgaRev", currentSpectrometer.getFPGARev());
            updateSetting("integrationTimeMS", currentSpectrometer.getIntegrationTimeMS());
            updateSetting("frame", currentSpectrometer.getActualFrames());

            // properties
            updateSetting("model", currentSpectrometer.model);
            updateSetting("serialNumber", currentSpectrometer.serialNumber);
            updateSetting("baudRate", currentSpectrometer.baudRate);
            updateSetting("hasCooling", currentSpectrometer.hasCooling);
            updateSetting("hasBattery", currentSpectrometer.hasBattery);
            updateSetting("hasLaser", currentSpectrometer.hasLaser);
            updateSetting("excitationNM", currentSpectrometer.excitationNM);
            updateSetting("slitSizeUM", currentSpectrometer.slitSizeUM);
            updateSetting("pixels", currentSpectrometer.pixels);

            for (int i = 0; i < currentSpectrometer.wavecalCoeffs.Length; i++)
                updateSetting("wavecalCoeff" + i, currentSpectrometer.wavecalCoeffs[i]);
            for (int i = 0; i < currentSpectrometer.detectorTempCoeffs.Length; i++)
                updateSetting("detectorTempCoeff" + i, currentSpectrometer.detectorTempCoeffs[i]);
            updateSetting("detectorTempMin", currentSpectrometer.detectorTempMin);
            updateSetting("detectorTempMax", currentSpectrometer.detectorTempMax);
            for (int i = 0; i < currentSpectrometer.adcCoeffs.Length; i++)
                updateSetting("adcCoeff" + i, currentSpectrometer.adcCoeffs[i]);
            updateSetting("thermistorResistanceAt298K", currentSpectrometer.thermistorResistanceAt298K);
            updateSetting("thermistorBeta", currentSpectrometer.thermistorResistanceAt298K);
            updateSetting("calibrationDate", currentSpectrometer.calibrationDate);
            updateSetting("calibrationBy", currentSpectrometer.calibrationBy);

            updateSetting("detectorName", currentSpectrometer.detectorName);
            updateSetting("activePixelsHoriz", currentSpectrometer.activePixelsHoriz);
            updateSetting("activePixelsVert", currentSpectrometer.activePixelsVert);
            updateSetting("minIntegrationTimeMS", currentSpectrometer.minIntegrationTimeMS);
            updateSetting("maxIntegrationTimeMS", currentSpectrometer.maxIntegrationTimeMS);
            updateSetting("actualHoriz", currentSpectrometer.actualHoriz);
            updateSetting("ROIHorizStart", currentSpectrometer.ROIHorizStart);
            updateSetting("ROIHorizEnd", currentSpectrometer.ROIHorizEnd);
            for (int i = 0; i < currentSpectrometer.ROIVertRegionStart.Length; i++)
                updateSetting(String.Format("ROIVertRegion{0}Start", i + 1), currentSpectrometer.ROIVertRegionStart[i]);
            for (int i = 0; i < currentSpectrometer.ROIVertRegionEnd.Length; i++)
                updateSetting(String.Format("ROIVertRegion{0}End", i + 1), currentSpectrometer.ROIVertRegionEnd[i]);

            updateSetting("userText", currentSpectrometer.userText);

            for (int i = 0; i < currentSpectrometer.badPixels.Length; i++)
                updateSetting("badPixels" + i, currentSpectrometer.badPixels[i] == -1 ? "" 
                                             : currentSpectrometer.badPixels[i].ToString());

            updateSetting("fpgaIntegrationTimeResolution", currentSpectrometer.fpgaIntegrationTimeResolution);
            updateSetting("fpgaDataHeader", currentSpectrometer.fpgaDataHeader);
            updateSetting("fpgaHasCFSelect", currentSpectrometer.fpgaHasCFSelect);
            updateSetting("fpgaLaserType", currentSpectrometer.fpgaLaserType);
            updateSetting("fpgaLaserControl", currentSpectrometer.fpgaLaserControl);
            updateSetting("fpgaHasAreaScan", currentSpectrometer.fpgaHasAreaScan);
            updateSetting("fpgaHasActualIntegTime", currentSpectrometer.fpgaHasActualIntegTime);
            updateSetting("fpgaHasHorizBinning", currentSpectrometer.fpgaHasHorizBinning);
        }

        void stubTreeView()
        {
            stubSetting("model",                    "Identity/Model");
            stubSetting("serialNumber",             "Identity/Serial Number");

            stubSetting("firmwareRev",              "Version/Firmware Rev");
            stubSetting("fpgaRev",                  "Version/FPGA Rev");

            stubSetting("baudRate",                 "Comms/Baud Rate");

            stubSetting("slitSizeUM",               "Optics/Slit Size (µm)");

            stubSetting("fpgaIntegrationTimeResolution", "FPGA/Integ Time Resolution (enum)");
            stubSetting("fpgaDataHeader",           "FPGA/Data Header");
            stubSetting("fpgaHasCFSelect",          "FPGA/Has CF Select");
            stubSetting("fpgaLaserType",            "FPGA/Laser Type");
            stubSetting("fpgaLaserControl",         "FPGA/Laser Control");
            stubSetting("fpgaHasAreaScan",          "FPGA/Has Area Scan");
            stubSetting("fpgaHasActualIntegTime",   "FPGA/Has Actual Integration Time");
            stubSetting("fpgaHasHorizBinning",      "FPGA/Has Horiz Binning");

            stubSetting("integrationTimeMS",        "Acquisition/Integration Time (ms)");
            stubSetting("actualIntegrationTimeMS",  "Acquisition/Integration/Actual (ms)");
            stubSetting("minIntegrationTimeMS",     "Acquisition/Integration/Min (ms)");
            stubSetting("maxIntegrationTimeMS",     "Acquisition/Integration/Max (ms)");
            stubSetting("frame",                    "Acquisition/Frame");

            stubSetting("wavecalCoeff0",            "Wavecal/Coeff0");
            stubSetting("wavecalCoeff1",            "Wavecal/Coeff1");
            stubSetting("wavecalCoeff2",            "Wavecal/Coeff2");
            stubSetting("wavecalCoeff3",            "Wavecal/Coeff3");

            stubSetting("detectorName",             "Detector/Name");
            stubSetting("detectorTempCoeff0",       "Detector/Temperature Calibration/Coeff0");
            stubSetting("detectorTempCoeff1",       "Detector/Temperature Calibration/Coeff1");
            stubSetting("detectorTempCoeff2",       "Detector/Temperature Calibration/Coeff2");
            stubSetting("detectorTempMin",          "Detector/Temperature Limits/Min");
            stubSetting("detectorTempMax",          "Detector/Temperature Limits/Max");

            stubSetting("hasCooling",               "Features/Has Cooling");
            stubSetting("hasLaser",                 "Features/Has Laser");
            stubSetting("hasBattery",               "Features/Has Battery");

            stubSetting("pixels",                   "Detector/Pixels/Spectrum Length");
            stubSetting("activePixelsHoriz",        "Detector/Pixels/Active/Horizontal");
            stubSetting("activePixelsVert",         "Detector/Pixels/Active/Vertical");
            stubSetting("actualHoriz",              "Detector/Pixels/Actual/Horizontal");
            stubSetting("ROIHorizStart",            "Detector/ROI/Horiz/Start)");
            stubSetting("ROIHorizEnd",              "Detector/ROI/Horiz/End");
            for (int i = 1; i < 4; i++)
            {
                stubSetting(String.Format("ROIVertRegion{0}Start", i), String.Format("Detector/ROI/Vert/Region{0}/Start", i));
                stubSetting(String.Format("ROIVertRegion{0}End",   i), String.Format("Detector/ROI/Vert/Region{0}/End", i));
            }
            stubSetting("ccdTECEnable",             "Detector/TEC/Enabled");
            stubSetting("ccdTempSetpoint",          "Detector/TEC/Setpoint");
            stubSetting("ccdTemp",                  "Detector/TEC/Temperature");
            stubSetting("thermistorResistanceAt298K", "Detector/TEC/Thermistor/Resistance at 298K");
            stubSetting("thermistorBeta",           "Detector/TEC/Thermistor/Beta value");
            stubSetting("ccdOffset",                "Detector/CCD/Offset");
            stubSetting("ccdGain",                  "Detector/CCD/Gain");
            stubSetting("horizBinning",             "Detector/Horizontal Binning");
            stubSetting("threshSensingMode",        "Detector/Sensitivity/Threshold Sensing Mode");
            stubSetting("sensingThresh",            "Detector/Sensitivity/Sensing Threshold");

            for (int i = 0; i < 15; i++)
                stubSetting("badPixels" + i, "Detector/Bad Pixels/Index " + i);

            stubSetting("laserEnabled",             "Laser/Enabled");
            stubSetting("excitationNM",             "Laser/Excitation (nm)");
            stubSetting("laserTemp",                "Laser/TEC/Temperature");
            stubSetting("laserTempSetpoint",        "Laser/TEC/Setpoint");
            stubSetting("laserMod",                 "Laser/Modulation/Value");
            stubSetting("laserModDur",              "Laser/Modulation/Duration");
            stubSetting("modPeriod",                "Laser/Modulation/Period");
            stubSetting("modPulseDelay",            "Laser/Modulation/Pulse Delay");
            stubSetting("laserModPulseWidth",       "Laser/Modulation/Pulse Width");
            stubSetting("linkLaserModToIntegrationTime", "Laser/Linked to Integration");
            stubSetting("laserRampingMode",         "Laser/Ramping Mode");
            stubSetting("selectedLaser",            "Laser/Selected");
            stubSetting("interlock",                "Laser/Interlock");
            for (int i = 0; i < 3; i++)
                stubSetting("adcCoeff" + i, "Laser/TEC/ADC/Coeff" + i);

            stubSetting("triggerDelay",             "Triggering/Delay (us)");
            stubSetting("triggerSource",            "Triggering/Source");
            stubSetting("externalTriggerOutput",    "Triggering/External Output");

            stubSetting("calibrationDate",          "Manufacture/Date");
            stubSetting("calibrationBy",            "Manufacture/Technician");

            stubSetting("userText",                 "User Data/Text");
        }

        void stubSetting(string key, string path)
        {
            string[] names = path.Split('/');

            // create or traverse intervening nodes
            TreeNodeCollection children = treeViewSettings.Nodes;
            for (int i = 0; i < names.Length - 1; i++)
            {
                string name = names[i];
                if (children.ContainsKey(name))
                {
                    children = children[name].Nodes;
                }
                else
                {
                    children = children.Add(name, name).Nodes;
                }
            }

            // now create the leaf node
            string prefix = names[names.Length - 1];
            if (!children.ContainsKey(key))
            {
                // do we even need to track TreeNode in the dict, since it has a unique key?
                TreeNode node = children.Add(key, prefix);
                treeNodes.Add(key, new Tuple<TreeNode, string>(node, prefix));
            }
            else
            {
                logger.error("stubSetting: path exists: {0}", path);
            }
        }

        void updateSetting(string key, object value)
        {
            if (!treeNodes.ContainsKey(key))
            {
                logger.error("updateSetting: unknown key {0}", key);
                return;
            }

            TreeNode node = treeNodes[key].Item1;
            string prefix = treeNodes[key].Item2;
            node.Text = prefix + ": " + value.ToString();
        }
        #endregion

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
            currentSpectrometer.setIntegrationTimeMS((uint)numericUpDownIntegTimeMS.Value);
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
            updateSettings();
        }

        ////////////////////////////////////////////////////////////////////////
        // BackgroundWorker: GUI Updates
        ////////////////////////////////////////////////////////////////////////

        private void backgroundWorkerGUIUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            while(true)
            {
                if (worker.CancellationPending || shutdownPending)
                    break;

                chart1.BeginInvoke(new MethodInvoker(delegate { updateGraph(); }));
                logger.flush();

                Thread.Sleep(100);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Background Worker: Acquisition Threads
        ////////////////////////////////////////////////////////////////////////

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Spectrometer spectrometer = (Spectrometer) e.Argument;
            SpectrometerState state = spectrometerStates[spectrometer];

            string prefix = String.Format("Worker.{0}.{1}", spectrometer.model, spectrometer.serialNumber);
            logger.debug("{0}: starting", prefix);
            state.running = true;

            BackgroundWorker worker = sender as BackgroundWorker;
            while (true)
            {
                double[] raw = spectrometer.getSpectrum();

                // process for graphing
                lock(spectrometers)
                    state.processSpectrum(raw);

                // report progress
                // worker.ReportProgress(scanCount, spectrometer);

                // end thread if we've been asked to cancel
                if (worker.CancellationPending)
                    break;

                // for debugging only
                Thread.Sleep(10);
            }
            logger.debug("{0}: stopped", prefix);
            state.running = false;
            e.Result = spectrometer;
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Spectrometer spectrometer = (Spectrometer) e.UserState;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Spectrometer spectrometer = e.Result as Spectrometer;
            if (spectrometer == currentSpectrometer)
                updateStartButton(false);
        }
    }
}
