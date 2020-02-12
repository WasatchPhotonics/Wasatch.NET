using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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

            // note all are 1-indexed
            charts = new Chart[] { chart1, chart2, chart3, chart4, chart5, chart6, chart7, chart8 };
            groupBoxes = new GroupBox[] { groupBoxPos1, groupBoxPos2, groupBoxPos3, groupBoxPos4, groupBoxPos5, groupBoxPos6, groupBoxPos7, groupBoxPos8 };
            radioButtons = new RadioButton[] { radioButtonSelectedPos1, radioButtonSelectedPos2, radioButtonSelectedPos3, radioButtonSelectedPos4,
                                               radioButtonSelectedPos5, radioButtonSelectedPos6, radioButtonSelectedPos7, radioButtonSelectedPos8 };
            foreach (var gb in groupBoxes)
                gb.Enabled = false;
        }

        private void buttonInit_Click(object sender, EventArgs e)
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
                seriesAll[pos] = s;
            }

            initialized = true;
        }

        ////////////////////////////////////////////////////////////////////////
        // System-Level Control (all spectrometers)
        ////////////////////////////////////////////////////////////////////////

        // user clicked the "enable external hardware triggering" system checkbox 
        private void checkBoxTriggerEnableAll_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = checkBoxTriggerEnableAll.Checked;
            logger.info($"HW triggering -> {enabled}");
            wrapper.triggeringEnabled = enabled;
        }

        // user clicked the "fan control" checkbox
        private void checkBoxFanEnable_CheckedChanged(object sender, EventArgs e)
        {
            wrapper.fanEnabled = checkBoxFanEnable.Checked;
        }

        // user clicked the "Acquire from all spectrometers" button
        private void buttonAcquireAll_Click(object sender, EventArgs e)
        {
            logger.info("starting acquisition");
            wrapper.startAcquisition();

            logger.info("getting spectra");
            var spectraByPos = wrapper.getSpectra();

            foreach (var pair in spectraByPos)
            {
                var pos = pair.Key;
                var spectrum = pair.Value;
                processSpectrum(pos, spectrum);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Individual Spectrometer Control (for testing/troubleshooting)
        ////////////////////////////////////////////////////////////////////////

        private void radioButtonSelected_CheckedChanged(object sender, EventArgs e) => updateSelection();
        private void buttonClearSelection_Click(object sender, EventArgs e) => updateSelection(reset: true);

        // user manually turned triggering on or off for the selected spectrometer
        private void checkBoxTriggerEnableOne_CheckedChanged(object sender, EventArgs e)
        {
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec != null)
                spec.triggerSource = checkBoxTriggerEnableOne.Checked ? TRIGGER_SOURCE.EXTERNAL : TRIGGER_SOURCE.INTERNAL;
        }

        // user manually overrode integration time for the selected spectrometer
        private void numericUpDownIntegrationTimeMSOne_ValueChanged(object sender, EventArgs e) 
        {
            var spec = wrapper.getSpectrometer(selectedPos);
            if (spec != null)
                spec.integrationTimeMS = (uint)numericUpDownIntegrationTimeMSOne.Value;
        }

        /// <summary>
        /// The user has clicked one of the "Selected" radio buttons on the GUI,
        /// so select that spectrometer for subsequent "single-device" operations.
        /// Alternatively, they clicked the "Clear Selection" button, so do that.
        /// </summary>
        /// <param name="reset">whether the selection should be cleared</param>
        void updateSelection(bool reset=false)
        {
            if (reset)
                foreach (var rb in radioButtons)
                    rb.Checked = false;

            selectedPos = -1;
            for (int i = 0; i < radioButtons.Length; i++)
                if (radioButtons[i].Checked)
                    selectedPos = i + 1;

            var spec = wrapper.getSpectrometer(selectedPos);

            if (selectedPos < 0 || spec is null)
            {
                groupBoxSelected.Enabled = false;
                labelSelectedPos.Text = "Selected Pos: N/A";
                labelSelectedNotes.Text = "";
                return;
            }

            ////////////////////////////////////////////////////////////////////
            // We have a valid selection
            ////////////////////////////////////////////////////////////////////

            groupBoxSelected.Enabled = true;

            labelSelectedPos.Text = $"Selected Pos: {selectedPos}";
            if (selectedPos == wrapper.getTriggerPos())
                labelSelectedNotes.Text = "Trigger Master";
            else if (selectedPos == wrapper.getFanPos())
                labelSelectedNotes.Text = "Fan Master";
            else
                labelSelectedNotes.Text = "";

            numericUpDownIntegrationTimeMSOne.Minimum = spec.eeprom.minIntegrationTimeMS;
            numericUpDownIntegrationTimeMSOne.Maximum = spec.eeprom.maxIntegrationTimeMS;
        }

        // User clicked the "Acquire one spectrum from selected spectrometer",
        // so do that.  Note we take the spectrum immediately, w/o triggering
        // (if they want to test triggering, that is done with the standard
        // system-level "Acquire" button).
        private async void buttonAcquireOne_Click(object sender, EventArgs e)
        {
            Spectrometer spec = wrapper.getSpectrometer(selectedPos);
            if (spec is null)
                return;

            var prev = spec.triggerSource;

            logger.info($"disabling triggering on {selectedPos}");
            spec.triggerSource = TRIGGER_SOURCE.INTERNAL;

            logger.info($"getting spectrum from {selectedPos}");
            ChannelSpectrum spectrum = await Task.Run(() => wrapper.getSpectrum(selectedPos));
            logger.debug($"recieved spectrum from {selectedPos}");

            logger.info($"restoring triggering on {selectedPos}");
            spec.triggerSource = prev;

            processSpectrum(selectedPos, spectrum);
        }

        ////////////////////////////////////////////////////////////////////////
        // Graphing
        ////////////////////////////////////////////////////////////////////////

        // do whatever we're gonna do with new spectra (save, dark-correct, ...)
        void processSpectrum(int pos, ChannelSpectrum spectrum)
        {
            updateChartAll(pos, spectrum);
            updateChartSmall(pos, spectrum);
        }

        // update the little graph
        void updateChartSmall(int pos, ChannelSpectrum spectrum)
        {
            Chart chart = charts[pos - 1];
            Series s = chart.Series[0];
            s.Points.DataBindXY(spectrum.xAxis, spectrum.intensities);
        }

        // update the big graph
        void updateChartAll(int pos, ChannelSpectrum spectrum)
        {
            Series s = null;
            seriesAll.TryGetValue(pos, out s);
            if (s != null)
                s.Points.DataBindXY(spectrum.xAxis, spectrum.intensities);
        }
    }
}
