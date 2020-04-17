using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using WasatchNET;

namespace MultiChannelDemo
{
    public partial class Form1 : Form
    {
        MultiChannelWrapper wrapper = MultiChannelWrapper.getInstance();
        Logger logger = Logger.getInstance();

        bool initialized = false;
        int selectedPos = 0; // multi-channel systems assumed to be 1-indexed

        // note these are zero-indexed
        Chart[] charts;
        GroupBox[] groupBoxes;
        RadioButton[] radioButtons;

        Dictionary<int, Series> seriesAll = new Dictionary<int, Series>();

        // whatever was last returned by "Acquire All", "Take Darks" or "Take References"
        // (added so we'd have something to save)
        List<ChannelSpectrum> lastSpectra;

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public Form1()
        {
            InitializeComponent();

            logger.setTextBox(textBoxEventLog);

            // note all are 1-indexed
            charts = new Chart[] { chart1, chart2, chart3, chart4, chart5, chart6, chart7, chart8 };
            groupBoxes = new GroupBox[] { groupBoxPos1, groupBoxPos2, groupBoxPos3, groupBoxPos4, groupBoxPos5, groupBoxPos6, groupBoxPos7, groupBoxPos8 };
            radioButtons = new RadioButton[] { radioButtonSelectedPos1, radioButtonSelectedPos2, radioButtonSelectedPos3, radioButtonSelectedPos4,
                                               radioButtonSelectedPos5, radioButtonSelectedPos6, radioButtonSelectedPos7, radioButtonSelectedPos8 };

            // initialize widgets
            foreach (var gb in groupBoxes)
                gb.Enabled = false;

            initChart(chartAll);
            foreach (var chart in charts)
                initChart(chart);

            clearSelection();

            Text = String.Format("MultiChannelDemo v{0}", Application.ProductVersion);
        }

        void initChart(Chart chart)
        {
            var area = chart.ChartAreas[0];
            area.AxisY.IsStartedFromZero = false;
            area.AxisX.LabelStyle.Format = "{0:f2}";
        }

        void buttonInit_Click(object sender, EventArgs e)
        {
            if (initialized)
                return;

            if (!wrapper.open())
            {
                logger.error("failed to open MultiChannelWrapper");
                return;
            }

            // per-channel initialization
            chartAll.Series.Clear();
            foreach (var pos in wrapper.positions)
            {
                var spec = wrapper.getSpectrometer(pos);
                var sn = spec.serialNumber;

                var gb = groupBoxes[pos - 1];
                gb.Enabled = true;
                gb.Text = $"Position {pos} ({sn})";

                // big graph
                var s = new Series($"Pos {pos} ({sn})");
                s.ChartType = SeriesChartType.Line;
                seriesAll[pos - 1] = s;
                chartAll.Series.Add(s);
            }

            initialized = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e) => logger.setTextBox(null);

        private void checkBoxVerbose_CheckedChanged(object sender, EventArgs e) => logger.level = checkBoxVerbose.Checked ? LogLevel.DEBUG : LogLevel.INFO;

        ////////////////////////////////////////////////////////////////////////
        // System-Level Control (all spectrometers)
        ////////////////////////////////////////////////////////////////////////

        // user clicked the "enable external hardware triggering" system checkbox 
        void checkBoxTriggerEnableAll_CheckedChanged(object sender, EventArgs e) => wrapper.triggeringEnabled = checkBoxTriggerEnableAll.Checked;

        // user clicked the "fan control" system checkbox
        void checkBoxFanEnable_CheckedChanged(object sender, EventArgs e) => wrapper.fanEnabled = checkBoxFanEnable.Checked;

        // user clicked the "compute reflectance" system checkbox
        private void checkBoxReflectanceEnabled_CheckedChanged(object sender, EventArgs e) => wrapper.reflectanceEnabled = checkBoxReflectanceEnabled.Checked;

        // user clicked the system-level "Acquire" button 
        async void buttonAcquireAll_Click(object sender, EventArgs e)
        {
            enableControls(false);
            lastSpectra = await wrapper.getSpectra();
            processSpectra(lastSpectra);
            enableControls(true);
        }

        // user clicked the system-level "Take Dark" button 
        async void buttonTakeDark_Click(object sender, EventArgs e)
        {
            enableControls(false);
            lastSpectra = await wrapper.takeDark();
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
            lastSpectra = await wrapper.takeReference();
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

        /// <summary>
        /// The user clicked the button to optimize integration time for all units.
        /// </summary>
        /// <remarks>
        /// Even though this is an async method, it is triggered from the GUI 
        /// thread, so can manipulate widgets.
        /// </remarks>
        async void buttonOptimizeAll_Click(object sender, EventArgs e)
        {
            enableControls(false);

            // kick-off background optimizers
            List<IntegrationOptimizer> intOpts = new List<IntegrationOptimizer>();
            foreach (var pos in wrapper.positions)
            {
                var spec = wrapper.getSpectrometer(pos);
                IntegrationOptimizer intOpt = new IntegrationOptimizer(spec);
                intOpt.start();
                intOpts.Add(intOpt);
            }

            // graph optimizers while they run
            await Task.Run(() => graphIntegrationSpectra(intOpts));

            // reports results
            bool allPassed = true;
            foreach (var intOpt in intOpts)
                allPassed &= intOpt.status == IntegrationOptimizer.Status.SUCCESS;

            if (allPassed)
            {
                logger.info("Optimization successful");
                foreach (var intOpt in intOpts)
                    logger.info($"  pos {intOpt.spec.multiChannelPosition} {intOpt.spec.serialNumber} integration time {intOpt.spec.integrationTimeMS}ms");
            }
            else
                logger.error("Optimization FAILED!");

            updateIntegrationTimeControl(); 

            enableControls(true);
        }

        /// <todo>
        /// enable/disable RadioButtons
        /// </todo>
        void enableControls(bool flag)
        {
            if (flag)
            {
                groupBoxSystem.Enabled = true;
                groupBoxSelected.Enabled = getSelectedPos() > 0;
            }
            else
            {
                groupBoxSystem.Enabled = false;
                groupBoxSelected.Enabled = false;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Individual Spectrometer Control (for testing/troubleshooting)
        ////////////////////////////////////////////////////////////////////////

        // user selected a spectrometer
        void radioButtonSelected_CheckedChanged(object sender, EventArgs e) => updateSelection(sender as RadioButton);

        // user selected no spectrometer
        void buttonClearSelection_Click(object sender, EventArgs e) => updateSelection();

        // user manually toggled triggering for a single spectrometer
        void checkBoxTriggerEnableOne_CheckedChanged(object sender, EventArgs e)
        {
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec != null)
                spec.triggerSource = checkBoxTriggerEnableOne.Checked ? TRIGGER_SOURCE.EXTERNAL : TRIGGER_SOURCE.INTERNAL;
        }

        // user manually set integration time for a single spectrometer
        void numericUpDownIntegrationTimeMSOne_ValueChanged(object sender, EventArgs e) 
        {
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            var old = spec.integrationTimeMS;
            var ms = (uint) numericUpDownIntegrationTimeMSOne.Value;
            logger.info($"{spec.serialNumber}: changing integration time from {old} to {ms}");
            spec.integrationTimeMS = ms;
        }

        void clearSelection()
        {
            groupBoxSelected.Enabled = false;
            labelSelectedPos.Text = labelSelectedNotes.Text = "";
        }

        int getSelectedPos()
        {
            for (int i = 0; i < radioButtons.Length; i++)
                if (radioButtons[i].Checked)
                    return i + 1; // position is 1-indexed
            return 0;
        }

        /// <summary>
        /// The user has clicked one of the "Selected" radio buttons on the GUI,
        /// so select that spectrometer for subsequent "single-device" operations.
        /// Alternatively, they clicked the "Clear Selection" button, so do that.
        /// </summary>
        /// <param name="selected">the RadioButton just clicked</param>
        void updateSelection(RadioButton selected = null)
        {
            // ensure all other RadioButtons are unchecked (not in a single GroupBox)
            foreach (var rb in radioButtons)
                if (selected is null || (selected.Checked && rb != selected))
                    rb.Checked = false;

            // determine our channel position
            selectedPos = getSelectedPos();
            var spec = wrapper.getSpectrometer(selectedPos);
            if (selectedPos < 1 || spec is null)
            {
                clearSelection();
                return;
            }

            ////////////////////////////////////////////////////////////////////
            // We have a valid selection
            ////////////////////////////////////////////////////////////////////

            groupBoxSelected.Enabled = true;

            labelSelectedPos.Text = $"Selected Pos: {selectedPos}";
            if (selectedPos == wrapper.triggerPos)
                labelSelectedNotes.Text = "Trigger Master";
            else if (selectedPos == wrapper.fanPos)
                labelSelectedNotes.Text = "Fan Master";
            else
                labelSelectedNotes.Text = "";

            updateIntegrationTimeControl();
        }

        void updateIntegrationTimeControl()
        { 
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            numericUpDownIntegrationTimeMSOne.ValueChanged -= numericUpDownIntegrationTimeMSOne_ValueChanged;
            numericUpDownIntegrationTimeMSOne.Minimum = spec.eeprom.minIntegrationTimeMS;
            numericUpDownIntegrationTimeMSOne.Maximum = spec.eeprom.maxIntegrationTimeMS;
            numericUpDownIntegrationTimeMSOne.Value = spec.integrationTimeMS;
            numericUpDownIntegrationTimeMSOne.ValueChanged += numericUpDownIntegrationTimeMSOne_ValueChanged;
        }

        async Task<ChannelSpectrum> takeOneSpectrum(Spectrometer spec)
        {
            bool restoreExternal = false;
            if (spec.triggerSource != TRIGGER_SOURCE.INTERNAL)
            {
                logger.debug($"disabling triggering on {selectedPos}");
                spec.triggerSource = TRIGGER_SOURCE.INTERNAL;
                restoreExternal = true;
            }

            logger.info($"getting spectrum from {selectedPos}");
            ChannelSpectrum spectrum = await Task.Run(() => wrapper.getSpectrum(selectedPos));
            logger.debug($"recieved spectrum from {selectedPos}");

            if (restoreExternal)
            {
                logger.debug($"restoring triggering on {selectedPos}");
                spec.triggerSource = TRIGGER_SOURCE.EXTERNAL;
            }

            return spectrum;
        }

        // User clicked the "Acquire one spectrum from selected spectrometer",
        // so do that.  Note we take the spectrum immediately, w/o triggering
        // (if they want to test triggering, that is done with the standard
        // system-level "Acquire" button).
        async void buttonAcquireOne_Click(object sender, EventArgs e)
        {
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            enableControls(false);
            var cs = await takeOneSpectrum(spec);
            processSpectrum(cs);
            enableControls(true);
        }
        
        // User clicked the "Take Dark" button for an individual spectrometer
        async void buttonTakeDarkOne_Click(object sender, EventArgs e)
        {
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            enableControls(false);
            var cs = await takeOneSpectrum(spec);
            spec.dark = cs.intensities;
            processSpectrum(cs);
            enableControls(true);
        }

        private void buttonClearDarkOne_Click(object sender, EventArgs e)
        {
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            spec.dark = null;
        }

        async void buttonOptimizeOne_Click(object sender, EventArgs e)
        {
            Spectrometer spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            enableControls(false);

            // kick-off optimization in a background thread
            var intOpt = new IntegrationOptimizer(spec);
            intOpt.start();

            // graph spectra during optimization
            await Task.Run(() => graphIntegrationSpectra(new IntegrationOptimizer[] { intOpt }.ToList()));

            // all done
            logger.info($"Optimization completed with status {intOpt.status}, integration time {spec.integrationTimeMS}ms");

            updateIntegrationTimeControl();
            enableControls(true);
        }

        ////////////////////////////////////////////////////////////////////////
        // Graphing
        ////////////////////////////////////////////////////////////////////////

        void processSpectra(List<ChannelSpectrum> spectra)
        {
            foreach (var cs in spectra)
                processSpectrum(cs);
        }

        // Do whatever we're gonna do with new spectra (save, dark-correct, ...).
        // Since we encapsulated dark subtraction in WasatchNET.Spectrometer, and
        // reflectance in MultiChannelWrapper, there's (deliberately) not a lot 
        // to do here.
        void processSpectrum(ChannelSpectrum cs)
        {
            if (cs.intensities is null)
            {
                logger.error("can't graph null spectrum");
                return;
            }
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
            seriesAll.TryGetValue(cs.pos - 1, out s);
            if (s != null)
                chartAll.BeginInvoke(new MethodInvoker(delegate { s.Points.DataBindXY(cs.xAxis, cs.intensities); }));
        }

        /// <summary>
        /// Provide a visual monitor of one or more IntegrationOptimizers as
        /// they optimize integration time on various spectrometers.
        /// </summary>
        /// <remarks>
        /// It is assumed that this will be called as a background task via 
        /// 'await', hence uses a delegate to graph the collected spectra.
        ///
        /// Returns when optimization is finished on all spectrometers (successful
        /// or otherwise).
        /// </remarks>
        void graphIntegrationSpectra(List<IntegrationOptimizer> intOpts)
        {
            if (intOpts is null)
                return;
            
            while (true)
            {
                var spectra = new List<ChannelSpectrum>();
                foreach (var intOpt in intOpts)
                    if (intOpt.status == IntegrationOptimizer.Status.PENDING)
                        spectra.Add(new ChannelSpectrum(intOpt.spec));

                // exit if all optimizers have finished
                if (spectra.Count == 0)
                    break;

                chartAll.BeginInvoke(new MethodInvoker(delegate { processSpectra(spectra); }));
                Thread.Sleep(200);
            }

            var final = new List<ChannelSpectrum>();
            foreach (var intOpt in intOpts)
                final.Add(new ChannelSpectrum(intOpt.spec));
            chartAll.BeginInvoke(new MethodInvoker(delegate { processSpectra(final); }));
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

            logger.info($"saved {pathname}");
        }
    }
}
