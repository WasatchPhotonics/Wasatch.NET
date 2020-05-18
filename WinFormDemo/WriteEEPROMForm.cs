using System;
using System.Windows.Forms;
using WasatchNET;

namespace WinFormDemo
{
    /// <summary>
    /// This form is not actively maintained, and is provided as a quick example.
    /// For production use, recommend using the EEPROMEditor in WPSpecCal or ENLIGHTEN.
    /// </summary>
    public partial class WriteEEPROMForm : Form
    {
        EEPROM eeprom;
        Logger logger = Logger.getInstance();

        public WriteEEPROMForm(EEPROM eeprom)
        {
            InitializeComponent();
            this.eeprom = eeprom;
            load();
        }

        void load()
        {
            numericUpDownExcitationNM.Value = eeprom.excitationNM;

            textBoxCalibrationDate.Text = eeprom.calibrationDate;
            textBoxCalibratedBy.Text = eeprom.calibrationBy;

            textBoxUserText.Text = eeprom.userText;

            textBoxWavecalCoeff0.Text = eeprom.wavecalCoeffs[0].ToString();
            textBoxWavecalCoeff1.Text = eeprom.wavecalCoeffs[1].ToString();
            textBoxWavecalCoeff2.Text = eeprom.wavecalCoeffs[2].ToString();
            textBoxWavecalCoeff3.Text = eeprom.wavecalCoeffs[3].ToString();

            textBoxLinearityCoeff0.Text = eeprom.linearityCoeffs[0].ToString();
            textBoxLinearityCoeff1.Text = eeprom.linearityCoeffs[1].ToString();
            textBoxLinearityCoeff2.Text = eeprom.linearityCoeffs[2].ToString();
            textBoxLinearityCoeff3.Text = eeprom.linearityCoeffs[3].ToString();
            textBoxLinearityCoeff4.Text = eeprom.linearityCoeffs[4].ToString();

            textBoxGainEven.Text = eeprom.detectorGain.ToString();
            textBoxGainOdd.Text = eeprom.detectorGainOdd.ToString();
            textBoxOffsetEven.Text = eeprom.detectorOffset.ToString();
            textBoxOffsetOdd.Text = eeprom.detectorOffsetOdd.ToString();

            numericUpDownROIHorizStart.Value = eeprom.ROIHorizStart;
            numericUpDownROIHorizEnd.Value   = eeprom.ROIHorizEnd;

            numericUpDownROIVertStart0.Value = eeprom.ROIVertRegionStart[0];
            numericUpDownROIVertEnd0.Value   = eeprom.ROIVertRegionEnd[0];
            numericUpDownROIVertStart1.Value = eeprom.ROIVertRegionStart[1];
            numericUpDownROIVertEnd1.Value   = eeprom.ROIVertRegionEnd[1];
            numericUpDownROIVertStart2.Value = eeprom.ROIVertRegionStart[2];
            numericUpDownROIVertEnd2.Value   = eeprom.ROIVertRegionEnd[2];

            numericUpDownBadPixel0.Value  = validPixel(eeprom.badPixels[0 ]);
            numericUpDownBadPixel1.Value  = validPixel(eeprom.badPixels[1 ]);
            numericUpDownBadPixel2.Value  = validPixel(eeprom.badPixels[2 ]);
            numericUpDownBadPixel3.Value  = validPixel(eeprom.badPixels[3 ]);
            numericUpDownBadPixel4.Value  = validPixel(eeprom.badPixels[4 ]);
            numericUpDownBadPixel5.Value  = validPixel(eeprom.badPixels[5 ]);
            numericUpDownBadPixel6.Value  = validPixel(eeprom.badPixels[6 ]);
            numericUpDownBadPixel7.Value  = validPixel(eeprom.badPixels[7 ]);
            numericUpDownBadPixel8.Value  = validPixel(eeprom.badPixels[8 ]);
            numericUpDownBadPixel9.Value  = validPixel(eeprom.badPixels[9 ]);
            numericUpDownBadPixel10.Value = validPixel(eeprom.badPixels[10]);
            numericUpDownBadPixel11.Value = validPixel(eeprom.badPixels[11]);
            numericUpDownBadPixel12.Value = validPixel(eeprom.badPixels[12]);
            numericUpDownBadPixel13.Value = validPixel(eeprom.badPixels[13]);
            numericUpDownBadPixel14.Value = validPixel(eeprom.badPixels[14]);
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

            var ok = eeprom.write();

            Close();
        }

        void updateFloat(TextBox tb, ref float value)
        {
            value = parseFloat(tb);
        }

        float parseFloat(TextBox tb)
        {
            try
            {
                float f = float.Parse(tb.Text);
                return f;
            }
            catch (Exception ex)
            {
                logger.error("Invalid float: {0} ({1})", tb.Text, ex.Message);
                return 0;
            }
        }

        private void numericUpDownExcitationNM_ValueChanged(object sender, EventArgs e) { eeprom.excitationNM = (ushort) numericUpDownExcitationNM.Value; }
        private void textBoxCalibrationDate_TextChanged(object sender, EventArgs e) { eeprom.calibrationDate = textBoxCalibrationDate.Text; } 
        private void textBoxCalibratedBy_TextChanged(object sender, EventArgs e) { eeprom.calibrationBy = textBoxCalibratedBy.Text; } 
        private void textBoxUserText_TextChanged(object sender, EventArgs e) { eeprom.userText = textBoxUserText.Text; }

        private void textBoxWavecalCoeff0_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref eeprom.wavecalCoeffs[0]); }
        private void textBoxWavecalCoeff1_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref eeprom.wavecalCoeffs[1]); }
        private void textBoxWavecalCoeff2_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref eeprom.wavecalCoeffs[2]); }
        private void textBoxWavecalCoeff3_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref eeprom.wavecalCoeffs[3]); }

        private void textBoxLinearityCoeff0_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref eeprom.linearityCoeffs[0]); }
        private void textBoxLinearityCoeff1_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref eeprom.linearityCoeffs[1]); }
        private void textBoxLinearityCoeff2_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref eeprom.linearityCoeffs[2]); }
        private void textBoxLinearityCoeff3_TextChanged(object sender, EventArgs e) { updateFloat(sender as TextBox, ref eeprom.linearityCoeffs[3]); }
        private void textBoxLinearityCoeff4_TextChanged(object sender, EventArgs e) => updateFloat(sender as TextBox, ref eeprom.linearityCoeffs[4]);

        private void textBoxGainEven_TextChanged(object sender, EventArgs e) => eeprom.detectorGain = parseFloat(sender as TextBox);
        private void textBoxGainOdd_TextChanged(object sender, EventArgs e) => eeprom.detectorGainOdd = parseFloat(sender as TextBox);
        private void textBoxOffsetEven_TextChanged(object sender, EventArgs e) => eeprom.detectorOffset = (short)parseFloat(sender as TextBox);
        private void textBoxOffsetOdd_TextChanged(object sender, EventArgs e) => eeprom.detectorOffsetOdd = (short)parseFloat(sender as TextBox);

        private void numericUpDownROIHorizStart_ValueChanged(object sender, EventArgs e) { eeprom.ROIHorizStart = (ushort) numericUpDownROIHorizStart.Value; }
        private void numericUpDownROIHorizEnd_ValueChanged(object sender, EventArgs e) { eeprom.ROIHorizEnd = (ushort) numericUpDownROIHorizEnd.Value; }

        private void numericUpDownROIVertStart0_ValueChanged(object sender, EventArgs e) { eeprom.ROIVertRegionStart[0] = (ushort)numericUpDownROIVertStart0.Value; }
        private void numericUpDownROIVertStart1_ValueChanged(object sender, EventArgs e) { eeprom.ROIVertRegionStart[1] = (ushort)numericUpDownROIVertStart1.Value; }
        private void numericUpDownROIVertStart2_ValueChanged(object sender, EventArgs e) { eeprom.ROIVertRegionStart[2] = (ushort)numericUpDownROIVertStart2.Value; }
        private void numericUpDownROIVertEnd0_ValueChanged(object sender, EventArgs e) { eeprom.ROIVertRegionEnd[0] = (ushort)numericUpDownROIVertEnd0.Value; }
        private void numericUpDownROIVertEnd1_ValueChanged(object sender, EventArgs e) { eeprom.ROIVertRegionEnd[1] = (ushort)numericUpDownROIVertEnd1.Value; }
        private void numericUpDownROIVertEnd2_ValueChanged(object sender, EventArgs e) { eeprom.ROIVertRegionEnd[2] = (ushort)numericUpDownROIVertEnd2.Value; }

        private void numericUpDownBadPixel0_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 0] = (short)numericUpDownBadPixel0.Value; } 
        private void numericUpDownBadPixel1_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 1] = (short)numericUpDownBadPixel1.Value; } 
        private void numericUpDownBadPixel2_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 2] = (short)numericUpDownBadPixel2.Value; } 
        private void numericUpDownBadPixel3_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 3] = (short)numericUpDownBadPixel3.Value; } 
        private void numericUpDownBadPixel4_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 4] = (short)numericUpDownBadPixel4.Value; } 
        private void numericUpDownBadPixel5_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 5] = (short)numericUpDownBadPixel5.Value; } 
        private void numericUpDownBadPixel6_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 6] = (short)numericUpDownBadPixel6.Value; } 
        private void numericUpDownBadPixel7_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 7] = (short)numericUpDownBadPixel7.Value; } 
        private void numericUpDownBadPixel8_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 8] = (short)numericUpDownBadPixel8.Value; } 
        private void numericUpDownBadPixel9_ValueChanged(object sender, EventArgs e)  { eeprom.badPixels[ 9] = (short)numericUpDownBadPixel9.Value; } 
        private void numericUpDownBadPixel10_ValueChanged(object sender, EventArgs e) { eeprom.badPixels[10] = (short)numericUpDownBadPixel10.Value; } 
        private void numericUpDownBadPixel11_ValueChanged(object sender, EventArgs e) { eeprom.badPixels[11] = (short)numericUpDownBadPixel11.Value; } 
        private void numericUpDownBadPixel12_ValueChanged(object sender, EventArgs e) { eeprom.badPixels[12] = (short)numericUpDownBadPixel12.Value; } 
        private void numericUpDownBadPixel13_ValueChanged(object sender, EventArgs e) { eeprom.badPixels[13] = (short)numericUpDownBadPixel13.Value; } 
        private void numericUpDownBadPixel14_ValueChanged(object sender, EventArgs e) { eeprom.badPixels[14] = (short)numericUpDownBadPixel14.Value; }

    }
}