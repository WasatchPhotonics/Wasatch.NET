using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WasatchNET;

namespace MultiChannelDemo
{
    public partial class Form1 : Form
    {
        MultiChannelWrapper wrapper = MultiChannelWrapper.getInstance();
        Logger logger = Logger.getInstance();

        bool initialized = false;
        int selectedPos = -1;

        // note these are zero-indexed
        Chart[] charts;
        GroupBox[] groupBoxes;
        RadioButton[] radioButtons;

        Dictionary<int, Series> seriesAll = new Dictionary<int, Series>();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public Form1()
        {
            InitializeComponent();

            logger.setTextBox(textBoxEventLog);
            // logger.level = LogLevel.DEBUG;

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
                groupBoxes[pos - 1].Enabled = true;

                // big graph
                var s = new Series($"Position {pos}");
                s.ChartType = SeriesChartType.Line;
                seriesAll[pos - 1] = s;
                chartAll.Series.Add(s);
            }

            initialized = true;
        }

        ////////////////////////////////////////////////////////////////////////
        // System-Level Control (all spectrometers)
        ////////////////////////////////////////////////////////////////////////

        // user clicked the "enable external hardware triggering" system checkbox 
        void checkBoxTriggerEnableAll_CheckedChanged(object sender, EventArgs e) => wrapper.triggeringEnabled = checkBoxTriggerEnableAll.Checked;

        // user clicked the "fan control" system checkbox
        void checkBoxFanEnable_CheckedChanged(object sender, EventArgs e) => wrapper.fanEnabled = checkBoxFanEnable.Checked;

        // user clicked the system-level "Acquire" button 
        void buttonAcquireAll_Click(object sender, EventArgs e) => processSpectra(wrapper.getSpectra());

        // user clicked the system-level "Take Dark" button 
        void buttonTakeDark_Click(object sender, EventArgs e) => processSpectra(wrapper.takeDark());

        async void buttonOptimizeAll_Click(object sender, EventArgs e)
        {
            List<IntegrationOptimizer> intOpts = new List<IntegrationOptimizer>();
            foreach (var pos in wrapper.positions)
            {
                var spec = wrapper.getSpectrometer(pos);
                IntegrationOptimizer intOpt = new IntegrationOptimizer(spec);
                intOpt.start();
                intOpts.Add(intOpt);
            }

            await Task.Run(() => graphIntegrationSpectra(intOpts));

            // all done
            logger.info("Optimization completed");
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
            if (spec != null)
                spec.integrationTimeMS = (uint)numericUpDownIntegrationTimeMSOne.Value;
        }

        void clearSelection()
        {
            groupBoxSelected.Enabled = false;
            labelSelectedPos.Text = labelSelectedNotes.Text = "";
        }

        /// <summary>
        /// The user has clicked one of the "Selected" radio buttons on the GUI,
        /// so select that spectrometer for subsequent "single-device" operations.
        /// Alternatively, they clicked the "Clear Selection" button, so do that.
        /// </summary>
        /// <param name="selected">the RadioButton just clicked</param>
        void updateSelection(RadioButton selected=null)
        {
            foreach (var rb in radioButtons)
            { 
                if ((selected is null || rb != selected) && rb.Checked)
                {
                    logger.debug($"unchecking {rb.Name}");
                    rb.Checked = false;
                }
            }

            selectedPos = -1;
            for (int i = 0; i < radioButtons.Length; i++)
                if (radioButtons[i].Checked)
                    selectedPos = i + 1;

            var spec = wrapper.getSpectrometer(selectedPos);

            if (selectedPos < 0 || spec is null)
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

            numericUpDownIntegrationTimeMSOne.Minimum = spec.eeprom.minIntegrationTimeMS;
            numericUpDownIntegrationTimeMSOne.Maximum = spec.eeprom.maxIntegrationTimeMS;
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

            var cs = await takeOneSpectrum(spec);
            processSpectrum(cs);
        }
        
        // User clicked the "Take Dark" button for an individual spectrometer
        async void buttonTakeDarkOne_Click(object sender, EventArgs e)
        {
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            var cs = await takeOneSpectrum(spec);
            spec.dark = cs.intensities;
            processSpectrum(cs);
        }

        async void buttonOptimizeOne_Click(object sender, EventArgs e)
        {
            Spectrometer spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            // kick-off optimization in a background thread
            var intOpt = new IntegrationOptimizer(spec);
            intOpt.start();

            // graph spectra during optimization
            await Task.Run(() => graphIntegrationSpectra(new IntegrationOptimizer[] { intOpt }.ToList()));

            // all done
            logger.info($"Optimization completed with status {intOpt.status}, integration time {spec.integrationTimeMS}ms");
        }

        ////////////////////////////////////////////////////////////////////////
        // Graphing
        ////////////////////////////////////////////////////////////////////////

        void processSpectra(List<ChannelSpectrum> spectra)
        {
            foreach (var cs in spectra)
                processSpectrum(cs);
        }

        // do whatever we're gonna do with new spectra (save, dark-correct, ...)
        void processSpectrum(ChannelSpectrum cs)
        {
            updateChartAll(cs);
            updateChartSmall(cs);
        }

        // update the little graph
        void updateChartSmall(ChannelSpectrum cs)
        {
            var chart = charts[cs.pos - 1];
            var s = chart.Series[0];
            s.Points.DataBindXY(cs.xAxis, cs.intensities);
        }

        // update the big graph
        void updateChartAll(ChannelSpectrum cs)
        {
            Series s = null;
            seriesAll.TryGetValue(cs.pos - 1, out s);
            if (s != null)
                s.Points.DataBindXY(cs.xAxis, cs.intensities);
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
        }

    }
}
