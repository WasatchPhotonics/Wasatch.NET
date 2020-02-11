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

        const int MAX_CHANNELS = 8; // this is for the GUI, not the MultiChannelWrapper library

        // note these are zero-indexed
        Chart[] charts;
        GroupBox[] groupBoxes;
        RadioButton[] radioButtons;

        public Form1()
        {
            InitializeComponent();

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

            foreach (var pos in wrapper.positions)
                groupBoxes[pos - 1].Enabled = true;

            initialized = true;
        }

        void updateSelection(bool reset=false)
        {
            if (reset)
                foreach (var rb in radioButtons)
                    rb.Checked = false;

            selectedPos = -1;
            for (int i = 0; i < radioButtons.Length; i++)
                if (radioButtons[i].Checked)
                    selectedPos = i + 1;

            if (selectedPos < 0)
            {
                groupBoxSelected.Enabled = false;
                labelSelectedPos.Text = "Selected Pos: N/A";
                labelSelectedNotes.Text = "";
                return;
            }

            groupBoxSelected.Enabled = true;

            labelSelectedPos.Text = String.Format("Selected Pos: {0}", selectedPos);
            if (selectedPos == wrapper.getTriggerPos())
                labelSelectedNotes.Text = "Trigger Master";
            else if (selectedPos == wrapper.getFanPos())
                labelSelectedNotes.Text = "Fan Master";
            else
                labelSelectedNotes.Text = "";
        }

        private void radioButtonSelected_CheckedChanged(object sender, EventArgs e) => updateSelection();
        private void buttonClearSelection_Click(object sender, EventArgs e) => updateSelection(true);

        private void checkBoxTriggerEnableAll_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxFanEnable_CheckedChanged(object sender, EventArgs e) { wrapper.fanEnabled = checkBoxFanEnable.Checked; }
    }
}
