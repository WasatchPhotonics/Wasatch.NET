using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WasatchNET;

namespace WinFormDemo
{
    public partial class WriteEEPROMForm : Form
    {
        EEPROM config;
        Logger logger = Logger.getInstance();

        public WriteEEPROMForm(EEPROM mc)
        {
            InitializeComponent();
            config = mc;
            load();
        }

        void load()
        {
            numericUpDownExcitationNM.Value = config.excitationNM;

            textBoxCalibrationDate.Text = config.calibrationDate;
            textBoxCalibratedBy.Text = config.calibrationBy;

            textBoxUserText.Text = config.userText;

            textBoxWavecalCoeff0.Text = config.wavecalCoeffs[0].ToString();
            textBoxWavecalCoeff1.Text = config.wavecalCoeffs[1].ToString();
            textBoxWavecalCoeff2.Text = config.wavecalCoeffs[2].ToString();
            textBoxWavecalCoeff3.Text = config.wavecalCoeffs[3].ToString();

            textBoxLinearityCoeff0.Text = config.linearityCoeffs[0].ToString();
            textBoxLinearityCoeff1.Text = config.linearityCoeffs[1].ToString();
            textBoxLinearityCoeff2.Text = config.linearityCoeffs[2].ToString();
            textBoxLinearityCoeff3.Text = config.linearityCoeffs[3].ToString();
            textBoxLinearityCoeff4.Text = config.linearityCoeffs[4].ToString();

            numericUpDownROIHorizStart.Value = config.ROIHorizStart;
            numericUpDownROIHorizEnd.Value   = config.ROIHorizEnd;

            numericUpDownROIVertStart0.Value = config.ROIVertRegionStart[0];
            numericUpDownROIVertEnd0.Value   = config.ROIVertRegionEnd[0];
            numericUpDownROIVertStart1.Value = config.ROIVertRegionStart[1];
            numericUpDownROIVertEnd1.Value   = config.ROIVertRegionEnd[1];
            numericUpDownROIVertStart2.Value = config.ROIVertRegionStart[2];
            numericUpDownROIVertEnd2.Value   = config.ROIVertRegionEnd[2];

            numericUpDownBadPixel0.Value  = validPixel(config.badPixels[0 ]);
            numericUpDownBadPixel1.Value  = validPixel(config.badPixels[1 ]);
            numericUpDownBadPixel2.Value  = validPixel(config.badPixels[2 ]);
            numericUpDownBadPixel3.Value  = validPixel(config.badPixels[3 ]);
            numericUpDownBadPixel4.Value  = validPixel(config.badPixels[4 ]);
            numericUpDownBadPixel5.Value  = validPixel(config.badPixels[5 ]);
            numericUpDownBadPixel6.Value  = validPixel(config.badPixels[6 ]);
            numericUpDownBadPixel7.Value  = validPixel(config.badPixels[7 ]);
            numericUpDownBadPixel8.Value  = validPixel(config.badPixels[8 ]);
            numericUpDownBadPixel9.Value  = validPixel(config.badPixels[9 ]);
            numericUpDownBadPixel10.Value = validPixel(config.badPixels[10]);
            numericUpDownBadPixel11.Value = validPixel(config.badPixels[11]);
            numericUpDownBadPixel12.Value = validPixel(config.badPixels[12]);
            numericUpDownBadPixel13.Value = validPixel(config.badPixels[13]);
            numericUpDownBadPixel14.Value = validPixel(config.badPixels[14]);
        }

        int validPixel(int n)
        {
            if (n < -1)
                return -1;
            else if (n > 1023)
                return 1023;
            else return n;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure? Writing bad values to the EEPROM could invalidate its factory calibration and lead to degraded measurements.", 
                "Extreme Caution Alert", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            config.write();

            Close();
        }

        void updateFloat(TextBox tb, ref float value)
        {
            try
            {
                float f = float.Parse(tb.Text);
                value = f;
            }
            catch (Exception ex)
            {
                logger.error("Invalid float: {0} ({1})", tb.Text, ex.Message);
            }
        }

        private void numericUpDownExcitationNM_ValueChanged(object sender, EventArgs e) { config.excitationNM = (ushort) numericUpDownExcitationNM.Value; }
        private void textBoxCalibrationDate_TextChanged(object sender, EventArgs e) { config.calibrationDate = textBoxCalibrationDate.Text; } 
        private void textBoxCalibratedBy_TextChanged(object sender, EventArgs e) { config.calibrationBy = textBoxCalibratedBy.Text; } 
        private void textBoxUserText_TextChanged(object sender, EventArgs e) { config.userText = textBoxUserText.Text; }

        private void textBoxWavecalCoeff0_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.wavecalCoeffs[0]); }
        private void textBoxWavecalCoeff1_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.wavecalCoeffs[1]); }
        private void textBoxWavecalCoeff2_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.wavecalCoeffs[2]); }
        private void textBoxWavecalCoeff3_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.wavecalCoeffs[3]); }

        private void textBoxLinearityCoeff0_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.linearityCoeffs[0]); }
        private void textBoxLinearityCoeff1_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.linearityCoeffs[1]); }
        private void textBoxLinearityCoeff2_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.linearityCoeffs[2]); }
        private void textBoxLinearityCoeff3_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.linearityCoeffs[3]); }
        private void textBoxLinearityCoeff4_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref config.linearityCoeffs[4]); }

        private void numericUpDownROIHorizStart_ValueChanged(object sender, EventArgs e) { config.ROIHorizStart = (ushort) numericUpDownROIHorizStart.Value; }
        private void numericUpDownROIHorizEnd_ValueChanged(object sender, EventArgs e) { config.ROIHorizEnd = (ushort) numericUpDownROIHorizEnd.Value; }

        private void numericUpDownROIVertStart0_ValueChanged(object sender, EventArgs e) { config.ROIVertRegionStart[0] = (ushort)numericUpDownROIVertStart0.Value; }
        private void numericUpDownROIVertStart1_ValueChanged(object sender, EventArgs e) { config.ROIVertRegionStart[1] = (ushort)numericUpDownROIVertStart1.Value; }
        private void numericUpDownROIVertStart2_ValueChanged(object sender, EventArgs e) { config.ROIVertRegionStart[2] = (ushort)numericUpDownROIVertStart2.Value; }
        private void numericUpDownROIVertEnd0_ValueChanged(object sender, EventArgs e) { config.ROIVertRegionEnd[0] = (ushort)numericUpDownROIVertEnd0.Value; }
        private void numericUpDownROIVertEnd1_ValueChanged(object sender, EventArgs e) { config.ROIVertRegionEnd[1] = (ushort)numericUpDownROIVertEnd1.Value; }
        private void numericUpDownROIVertEnd2_ValueChanged(object sender, EventArgs e) { config.ROIVertRegionEnd[2] = (ushort)numericUpDownROIVertEnd2.Value; }

        private void numericUpDownBadPixel0_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 0] = (short)numericUpDownBadPixel0.Value; } 
        private void numericUpDownBadPixel1_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 1] = (short)numericUpDownBadPixel1.Value; } 
        private void numericUpDownBadPixel2_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 2] = (short)numericUpDownBadPixel2.Value; } 
        private void numericUpDownBadPixel3_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 3] = (short)numericUpDownBadPixel3.Value; } 
        private void numericUpDownBadPixel4_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 4] = (short)numericUpDownBadPixel4.Value; } 
        private void numericUpDownBadPixel5_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 5] = (short)numericUpDownBadPixel5.Value; } 
        private void numericUpDownBadPixel6_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 6] = (short)numericUpDownBadPixel6.Value; } 
        private void numericUpDownBadPixel7_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 7] = (short)numericUpDownBadPixel7.Value; } 
        private void numericUpDownBadPixel8_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 8] = (short)numericUpDownBadPixel8.Value; } 
        private void numericUpDownBadPixel9_ValueChanged(object sender, EventArgs e)  { config.badPixels[ 9] = (short)numericUpDownBadPixel9.Value; } 
        private void numericUpDownBadPixel10_ValueChanged(object sender, EventArgs e) { config.badPixels[10] = (short)numericUpDownBadPixel10.Value; } 
        private void numericUpDownBadPixel11_ValueChanged(object sender, EventArgs e) { config.badPixels[11] = (short)numericUpDownBadPixel11.Value; } 
        private void numericUpDownBadPixel12_ValueChanged(object sender, EventArgs e) { config.badPixels[12] = (short)numericUpDownBadPixel12.Value; } 
        private void numericUpDownBadPixel13_ValueChanged(object sender, EventArgs e) { config.badPixels[13] = (short)numericUpDownBadPixel13.Value; } 
        private void numericUpDownBadPixel14_ValueChanged(object sender, EventArgs e) { config.badPixels[14] = (short)numericUpDownBadPixel14.Value; } 
    }
}