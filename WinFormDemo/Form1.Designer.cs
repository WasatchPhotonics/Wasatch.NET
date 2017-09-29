namespace WinFormDemo
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.splitContainerTopVsLog = new System.Windows.Forms.SplitContainer();
            this.splitContainerGraphVsControls = new System.Windows.Forms.SplitContainer();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.groupBoxSettings = new System.Windows.Forms.GroupBox();
            this.treeViewSettings = new System.Windows.Forms.TreeView();
            this.groupBoxSpectrometers = new System.Windows.Forms.GroupBox();
            this.comboBoxSpectrometer = new System.Windows.Forms.ComboBox();
            this.groupBoxControl = new System.Windows.Forms.GroupBox();
            this.groupBoxMode = new System.Windows.Forms.GroupBox();
            this.radioButtonModeTransmission = new System.Windows.Forms.RadioButton();
            this.radioButtonModeAbsorbance = new System.Windows.Forms.RadioButton();
            this.radioButtonModeScope = new System.Windows.Forms.RadioButton();
            this.buttonClearTraces = new System.Windows.Forms.Button();
            this.buttonAddTrace = new System.Windows.Forms.Button();
            this.numericUpDownIntegTimeMS = new System.Windows.Forms.NumericUpDown();
            this.buttonSave = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.numericUpDownScanAveraging = new System.Windows.Forms.NumericUpDown();
            this.checkBoxTakeReference = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.checkBoxTakeDark = new System.Windows.Forms.CheckBox();
            this.numericUpDownBoxcarHalfWidth = new System.Windows.Forms.NumericUpDown();
            this.checkBoxLaserEnable = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBoxSetup = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxXAxis = new System.Windows.Forms.ComboBox();
            this.checkBoxVerbose = new System.Windows.Forms.CheckBox();
            this.buttonInitialize = new System.Windows.Forms.Button();
            this.groupBoxEventLog = new System.Windows.Forms.GroupBox();
            this.textBoxEventLog = new System.Windows.Forms.TextBox();
            this.backgroundWorkerGUIUpdate = new System.ComponentModel.BackgroundWorker();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTopVsLog)).BeginInit();
            this.splitContainerTopVsLog.Panel1.SuspendLayout();
            this.splitContainerTopVsLog.Panel2.SuspendLayout();
            this.splitContainerTopVsLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerGraphVsControls)).BeginInit();
            this.splitContainerGraphVsControls.Panel1.SuspendLayout();
            this.splitContainerGraphVsControls.Panel2.SuspendLayout();
            this.splitContainerGraphVsControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.groupBoxSettings.SuspendLayout();
            this.groupBoxSpectrometers.SuspendLayout();
            this.groupBoxControl.SuspendLayout();
            this.groupBoxMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownIntegTimeMS)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownScanAveraging)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBoxcarHalfWidth)).BeginInit();
            this.groupBoxSetup.SuspendLayout();
            this.groupBoxEventLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerTopVsLog
            // 
            this.splitContainerTopVsLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerTopVsLog.Location = new System.Drawing.Point(0, 0);
            this.splitContainerTopVsLog.Name = "splitContainerTopVsLog";
            this.splitContainerTopVsLog.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerTopVsLog.Panel1
            // 
            this.splitContainerTopVsLog.Panel1.Controls.Add(this.splitContainerGraphVsControls);
            // 
            // splitContainerTopVsLog.Panel2
            // 
            this.splitContainerTopVsLog.Panel2.Controls.Add(this.groupBoxEventLog);
            this.splitContainerTopVsLog.Size = new System.Drawing.Size(930, 726);
            this.splitContainerTopVsLog.SplitterDistance = 581;
            this.splitContainerTopVsLog.TabIndex = 0;
            // 
            // splitContainerGraphVsControls
            // 
            this.splitContainerGraphVsControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerGraphVsControls.Location = new System.Drawing.Point(0, 0);
            this.splitContainerGraphVsControls.Name = "splitContainerGraphVsControls";
            // 
            // splitContainerGraphVsControls.Panel1
            // 
            this.splitContainerGraphVsControls.Panel1.Controls.Add(this.chart1);
            // 
            // splitContainerGraphVsControls.Panel2
            // 
            this.splitContainerGraphVsControls.Panel2.Controls.Add(this.groupBoxSettings);
            this.splitContainerGraphVsControls.Panel2.Controls.Add(this.groupBoxSpectrometers);
            this.splitContainerGraphVsControls.Panel2.Controls.Add(this.groupBoxSetup);
            this.splitContainerGraphVsControls.Size = new System.Drawing.Size(930, 581);
            this.splitContainerGraphVsControls.SplitterDistance = 639;
            this.splitContainerGraphVsControls.TabIndex = 6;
            // 
            // chart1
            // 
            chartArea1.AxisX.LabelStyle.Format = "F2";
            chartArea1.CursorX.IsUserEnabled = true;
            chartArea1.CursorX.IsUserSelectionEnabled = true;
            chartArea1.CursorY.IsUserEnabled = true;
            chartArea1.CursorY.IsUserSelectionEnabled = true;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Alignment = System.Drawing.StringAlignment.Center;
            legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(639, 581);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            // 
            // groupBoxSettings
            // 
            this.groupBoxSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxSettings.Controls.Add(this.treeViewSettings);
            this.groupBoxSettings.Location = new System.Drawing.Point(2, 431);
            this.groupBoxSettings.Name = "groupBoxSettings";
            this.groupBoxSettings.Size = new System.Drawing.Size(281, 142);
            this.groupBoxSettings.TabIndex = 2;
            this.groupBoxSettings.TabStop = false;
            this.groupBoxSettings.Text = "Settings";
            // 
            // treeViewSettings
            // 
            this.treeViewSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewSettings.Location = new System.Drawing.Point(3, 18);
            this.treeViewSettings.Name = "treeViewSettings";
            this.treeViewSettings.Size = new System.Drawing.Size(275, 121);
            this.treeViewSettings.TabIndex = 0;
            // 
            // groupBoxSpectrometers
            // 
            this.groupBoxSpectrometers.Controls.Add(this.comboBoxSpectrometer);
            this.groupBoxSpectrometers.Controls.Add(this.groupBoxControl);
            this.groupBoxSpectrometers.Enabled = false;
            this.groupBoxSpectrometers.Location = new System.Drawing.Point(3, 90);
            this.groupBoxSpectrometers.Name = "groupBoxSpectrometers";
            this.groupBoxSpectrometers.Size = new System.Drawing.Size(280, 335);
            this.groupBoxSpectrometers.TabIndex = 1;
            this.groupBoxSpectrometers.TabStop = false;
            this.groupBoxSpectrometers.Text = "Spectrometers";
            // 
            // comboBoxSpectrometer
            // 
            this.comboBoxSpectrometer.FormattingEnabled = true;
            this.comboBoxSpectrometer.Location = new System.Drawing.Point(6, 21);
            this.comboBoxSpectrometer.Name = "comboBoxSpectrometer";
            this.comboBoxSpectrometer.Size = new System.Drawing.Size(267, 24);
            this.comboBoxSpectrometer.TabIndex = 0;
            this.comboBoxSpectrometer.SelectedIndexChanged += new System.EventHandler(this.comboBoxSpectrometer_SelectedIndexChanged);
            // 
            // groupBoxControl
            // 
            this.groupBoxControl.Controls.Add(this.groupBoxMode);
            this.groupBoxControl.Controls.Add(this.buttonClearTraces);
            this.groupBoxControl.Controls.Add(this.buttonAddTrace);
            this.groupBoxControl.Controls.Add(this.numericUpDownIntegTimeMS);
            this.groupBoxControl.Controls.Add(this.buttonSave);
            this.groupBoxControl.Controls.Add(this.label1);
            this.groupBoxControl.Controls.Add(this.buttonStart);
            this.groupBoxControl.Controls.Add(this.numericUpDownScanAveraging);
            this.groupBoxControl.Controls.Add(this.checkBoxTakeReference);
            this.groupBoxControl.Controls.Add(this.label2);
            this.groupBoxControl.Controls.Add(this.checkBoxTakeDark);
            this.groupBoxControl.Controls.Add(this.numericUpDownBoxcarHalfWidth);
            this.groupBoxControl.Controls.Add(this.checkBoxLaserEnable);
            this.groupBoxControl.Controls.Add(this.label3);
            this.groupBoxControl.Location = new System.Drawing.Point(0, 51);
            this.groupBoxControl.Name = "groupBoxControl";
            this.groupBoxControl.Size = new System.Drawing.Size(273, 279);
            this.groupBoxControl.TabIndex = 1;
            this.groupBoxControl.TabStop = false;
            this.groupBoxControl.Text = "Control";
            // 
            // groupBoxMode
            // 
            this.groupBoxMode.Controls.Add(this.radioButtonModeTransmission);
            this.groupBoxMode.Controls.Add(this.radioButtonModeAbsorbance);
            this.groupBoxMode.Controls.Add(this.radioButtonModeScope);
            this.groupBoxMode.Location = new System.Drawing.Point(6, 132);
            this.groupBoxMode.Name = "groupBoxMode";
            this.groupBoxMode.Size = new System.Drawing.Size(261, 49);
            this.groupBoxMode.TabIndex = 13;
            this.groupBoxMode.TabStop = false;
            this.groupBoxMode.Text = "Mode";
            // 
            // radioButtonModeTransmission
            // 
            this.radioButtonModeTransmission.AutoSize = true;
            this.radioButtonModeTransmission.Enabled = false;
            this.radioButtonModeTransmission.Location = new System.Drawing.Point(174, 21);
            this.radioButtonModeTransmission.Name = "radioButtonModeTransmission";
            this.radioButtonModeTransmission.Size = new System.Drawing.Size(85, 21);
            this.radioButtonModeTransmission.TabIndex = 2;
            this.radioButtonModeTransmission.TabStop = true;
            this.radioButtonModeTransmission.Text = "trans/refl";
            this.toolTip1.SetToolTip(this.radioButtonModeTransmission, "Transmission and Reflectance");
            this.radioButtonModeTransmission.UseVisualStyleBackColor = true;
            this.radioButtonModeTransmission.CheckedChanged += new System.EventHandler(this.radioButtonModeTransmission_CheckedChanged);
            // 
            // radioButtonModeAbsorbance
            // 
            this.radioButtonModeAbsorbance.AutoSize = true;
            this.radioButtonModeAbsorbance.Enabled = false;
            this.radioButtonModeAbsorbance.Location = new System.Drawing.Point(72, 21);
            this.radioButtonModeAbsorbance.Name = "radioButtonModeAbsorbance";
            this.radioButtonModeAbsorbance.Size = new System.Drawing.Size(104, 21);
            this.radioButtonModeAbsorbance.TabIndex = 1;
            this.radioButtonModeAbsorbance.TabStop = true;
            this.radioButtonModeAbsorbance.Text = "absorbance";
            this.radioButtonModeAbsorbance.UseVisualStyleBackColor = true;
            this.radioButtonModeAbsorbance.CheckedChanged += new System.EventHandler(this.radioButtonModeAbsorbance_CheckedChanged);
            // 
            // radioButtonModeScope
            // 
            this.radioButtonModeScope.AutoSize = true;
            this.radioButtonModeScope.Checked = true;
            this.radioButtonModeScope.Location = new System.Drawing.Point(6, 21);
            this.radioButtonModeScope.Name = "radioButtonModeScope";
            this.radioButtonModeScope.Size = new System.Drawing.Size(67, 21);
            this.radioButtonModeScope.TabIndex = 0;
            this.radioButtonModeScope.TabStop = true;
            this.radioButtonModeScope.Text = "scope";
            this.radioButtonModeScope.UseVisualStyleBackColor = true;
            this.radioButtonModeScope.CheckedChanged += new System.EventHandler(this.radioButtonModeScope_CheckedChanged);
            // 
            // buttonClearTraces
            // 
            this.buttonClearTraces.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("buttonClearTraces.BackgroundImage")));
            this.buttonClearTraces.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonClearTraces.Enabled = false;
            this.buttonClearTraces.Location = new System.Drawing.Point(160, 231);
            this.buttonClearTraces.Name = "buttonClearTraces";
            this.buttonClearTraces.Size = new System.Drawing.Size(44, 41);
            this.buttonClearTraces.TabIndex = 12;
            this.toolTip1.SetToolTip(this.buttonClearTraces, "Clear all spectra traces");
            this.buttonClearTraces.UseVisualStyleBackColor = true;
            this.buttonClearTraces.Click += new System.EventHandler(this.buttonClearTraces_Click);
            // 
            // buttonAddTrace
            // 
            this.buttonAddTrace.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("buttonAddTrace.BackgroundImage")));
            this.buttonAddTrace.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonAddTrace.Enabled = false;
            this.buttonAddTrace.Location = new System.Drawing.Point(110, 231);
            this.buttonAddTrace.Name = "buttonAddTrace";
            this.buttonAddTrace.Size = new System.Drawing.Size(44, 41);
            this.buttonAddTrace.TabIndex = 11;
            this.toolTip1.SetToolTip(this.buttonAddTrace, "Add spectrum trace");
            this.buttonAddTrace.UseVisualStyleBackColor = true;
            this.buttonAddTrace.Click += new System.EventHandler(this.buttonAddTrace_Click);
            // 
            // numericUpDownIntegTimeMS
            // 
            this.numericUpDownIntegTimeMS.Location = new System.Drawing.Point(6, 21);
            this.numericUpDownIntegTimeMS.Maximum = new decimal(new int[] {
            30000,
            0,
            0,
            0});
            this.numericUpDownIntegTimeMS.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownIntegTimeMS.Name = "numericUpDownIntegTimeMS";
            this.numericUpDownIntegTimeMS.Size = new System.Drawing.Size(79, 22);
            this.numericUpDownIntegTimeMS.TabIndex = 0;
            this.numericUpDownIntegTimeMS.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownIntegTimeMS.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownIntegTimeMS.ValueChanged += new System.EventHandler(this.numericUpDownIntegTimeMS_ValueChanged);
            // 
            // buttonSave
            // 
            this.buttonSave.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("buttonSave.BackgroundImage")));
            this.buttonSave.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonSave.Enabled = false;
            this.buttonSave.Location = new System.Drawing.Point(210, 231);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(38, 41);
            this.buttonSave.TabIndex = 10;
            this.toolTip1.SetToolTip(this.buttonSave, "Save spectra");
            this.buttonSave.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(91, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(137, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "integration time (ms)";
            // 
            // buttonStart
            // 
            this.buttonStart.BackColor = System.Drawing.Color.Green;
            this.buttonStart.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.ForeColor = System.Drawing.Color.White;
            this.buttonStart.Location = new System.Drawing.Point(6, 187);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(255, 38);
            this.buttonStart.TabIndex = 9;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = false;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // numericUpDownScanAveraging
            // 
            this.numericUpDownScanAveraging.Location = new System.Drawing.Point(6, 49);
            this.numericUpDownScanAveraging.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownScanAveraging.Name = "numericUpDownScanAveraging";
            this.numericUpDownScanAveraging.Size = new System.Drawing.Size(79, 22);
            this.numericUpDownScanAveraging.TabIndex = 2;
            this.numericUpDownScanAveraging.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownScanAveraging.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownScanAveraging.ValueChanged += new System.EventHandler(this.numericUpDownScanAveraging_ValueChanged);
            // 
            // checkBoxTakeReference
            // 
            this.checkBoxTakeReference.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxTakeReference.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("checkBoxTakeReference.BackgroundImage")));
            this.checkBoxTakeReference.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.checkBoxTakeReference.Enabled = false;
            this.checkBoxTakeReference.Location = new System.Drawing.Point(58, 231);
            this.checkBoxTakeReference.Name = "checkBoxTakeReference";
            this.checkBoxTakeReference.Size = new System.Drawing.Size(46, 41);
            this.checkBoxTakeReference.TabIndex = 8;
            this.toolTip1.SetToolTip(this.checkBoxTakeReference, "Use current spectrum as reference");
            this.checkBoxTakeReference.UseVisualStyleBackColor = true;
            this.checkBoxTakeReference.CheckedChanged += new System.EventHandler(this.checkBoxTakeReference_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(91, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "scan averaging";
            // 
            // checkBoxTakeDark
            // 
            this.checkBoxTakeDark.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxTakeDark.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("checkBoxTakeDark.BackgroundImage")));
            this.checkBoxTakeDark.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.checkBoxTakeDark.Enabled = false;
            this.checkBoxTakeDark.Location = new System.Drawing.Point(6, 231);
            this.checkBoxTakeDark.Name = "checkBoxTakeDark";
            this.checkBoxTakeDark.Size = new System.Drawing.Size(46, 41);
            this.checkBoxTakeDark.TabIndex = 7;
            this.toolTip1.SetToolTip(this.checkBoxTakeDark, "Use current spectrum as dark");
            this.checkBoxTakeDark.UseVisualStyleBackColor = true;
            this.checkBoxTakeDark.CheckedChanged += new System.EventHandler(this.checkBoxTakeDark_CheckedChanged);
            // 
            // numericUpDownBoxcarHalfWidth
            // 
            this.numericUpDownBoxcarHalfWidth.Location = new System.Drawing.Point(6, 77);
            this.numericUpDownBoxcarHalfWidth.Name = "numericUpDownBoxcarHalfWidth";
            this.numericUpDownBoxcarHalfWidth.Size = new System.Drawing.Size(79, 22);
            this.numericUpDownBoxcarHalfWidth.TabIndex = 4;
            this.numericUpDownBoxcarHalfWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownBoxcarHalfWidth.ValueChanged += new System.EventHandler(this.numericUpDownBoxcarHalfWidth_ValueChanged);
            // 
            // checkBoxLaserEnable
            // 
            this.checkBoxLaserEnable.AutoSize = true;
            this.checkBoxLaserEnable.Location = new System.Drawing.Point(69, 105);
            this.checkBoxLaserEnable.Name = "checkBoxLaserEnable";
            this.checkBoxLaserEnable.Size = new System.Drawing.Size(108, 21);
            this.checkBoxLaserEnable.TabIndex = 6;
            this.checkBoxLaserEnable.Text = "laser enable";
            this.checkBoxLaserEnable.UseVisualStyleBackColor = true;
            this.checkBoxLaserEnable.CheckedChanged += new System.EventHandler(this.checkBoxLaserEnable_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(91, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(114, 17);
            this.label3.TabIndex = 5;
            this.label3.Text = "boxcar half-width";
            // 
            // groupBoxSetup
            // 
            this.groupBoxSetup.Controls.Add(this.label4);
            this.groupBoxSetup.Controls.Add(this.comboBoxXAxis);
            this.groupBoxSetup.Controls.Add(this.checkBoxVerbose);
            this.groupBoxSetup.Controls.Add(this.buttonInitialize);
            this.groupBoxSetup.Location = new System.Drawing.Point(3, 3);
            this.groupBoxSetup.Name = "groupBoxSetup";
            this.groupBoxSetup.Size = new System.Drawing.Size(280, 81);
            this.groupBoxSetup.TabIndex = 0;
            this.groupBoxSetup.TabStop = false;
            this.groupBoxSetup.Text = "Setup";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(133, 51);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 17);
            this.label4.TabIndex = 3;
            this.label4.Text = "X-Axis";
            // 
            // comboBoxXAxis
            // 
            this.comboBoxXAxis.FormattingEnabled = true;
            this.comboBoxXAxis.Items.AddRange(new object[] {
            "Wavelength",
            "Wavenumber"});
            this.comboBoxXAxis.Location = new System.Drawing.Point(6, 48);
            this.comboBoxXAxis.Name = "comboBoxXAxis";
            this.comboBoxXAxis.Size = new System.Drawing.Size(121, 24);
            this.comboBoxXAxis.TabIndex = 2;
            this.comboBoxXAxis.SelectedIndexChanged += new System.EventHandler(this.comboBoxXAxis_SelectedIndexChanged);
            // 
            // checkBoxVerbose
            // 
            this.checkBoxVerbose.AutoSize = true;
            this.checkBoxVerbose.Location = new System.Drawing.Point(109, 21);
            this.checkBoxVerbose.Name = "checkBoxVerbose";
            this.checkBoxVerbose.Size = new System.Drawing.Size(81, 21);
            this.checkBoxVerbose.TabIndex = 1;
            this.checkBoxVerbose.Text = "verbose";
            this.checkBoxVerbose.UseVisualStyleBackColor = true;
            this.checkBoxVerbose.CheckedChanged += new System.EventHandler(this.checkBoxVerbose_CheckedChanged);
            // 
            // buttonInitialize
            // 
            this.buttonInitialize.Location = new System.Drawing.Point(6, 21);
            this.buttonInitialize.Name = "buttonInitialize";
            this.buttonInitialize.Size = new System.Drawing.Size(75, 23);
            this.buttonInitialize.TabIndex = 0;
            this.buttonInitialize.Text = "Initialize";
            this.buttonInitialize.UseVisualStyleBackColor = true;
            this.buttonInitialize.Click += new System.EventHandler(this.buttonInitialize_Click);
            // 
            // groupBoxEventLog
            // 
            this.groupBoxEventLog.Controls.Add(this.textBoxEventLog);
            this.groupBoxEventLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxEventLog.Location = new System.Drawing.Point(0, 0);
            this.groupBoxEventLog.Name = "groupBoxEventLog";
            this.groupBoxEventLog.Size = new System.Drawing.Size(930, 141);
            this.groupBoxEventLog.TabIndex = 0;
            this.groupBoxEventLog.TabStop = false;
            this.groupBoxEventLog.Text = "Event Log";
            // 
            // textBoxEventLog
            // 
            this.textBoxEventLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxEventLog.Location = new System.Drawing.Point(3, 18);
            this.textBoxEventLog.Multiline = true;
            this.textBoxEventLog.Name = "textBoxEventLog";
            this.textBoxEventLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxEventLog.Size = new System.Drawing.Size(924, 120);
            this.textBoxEventLog.TabIndex = 0;
            // 
            // backgroundWorkerGUIUpdate
            // 
            this.backgroundWorkerGUIUpdate.WorkerSupportsCancellation = true;
            this.backgroundWorkerGUIUpdate.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerGUIUpdate_DoWork);
            // 
            // Form1
            // 
            this.AcceptButton = this.buttonInitialize;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(930, 726);
            this.Controls.Add(this.splitContainerTopVsLog);
            this.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Wasatch.NET WinForm Demo";
            this.splitContainerTopVsLog.Panel1.ResumeLayout(false);
            this.splitContainerTopVsLog.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTopVsLog)).EndInit();
            this.splitContainerTopVsLog.ResumeLayout(false);
            this.splitContainerGraphVsControls.Panel1.ResumeLayout(false);
            this.splitContainerGraphVsControls.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerGraphVsControls)).EndInit();
            this.splitContainerGraphVsControls.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.groupBoxSettings.ResumeLayout(false);
            this.groupBoxSpectrometers.ResumeLayout(false);
            this.groupBoxControl.ResumeLayout(false);
            this.groupBoxControl.PerformLayout();
            this.groupBoxMode.ResumeLayout(false);
            this.groupBoxMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownIntegTimeMS)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownScanAveraging)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBoxcarHalfWidth)).EndInit();
            this.groupBoxSetup.ResumeLayout(false);
            this.groupBoxSetup.PerformLayout();
            this.groupBoxEventLog.ResumeLayout(false);
            this.groupBoxEventLog.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerTopVsLog;
        private System.Windows.Forms.TextBox textBoxEventLog;
        private System.ComponentModel.BackgroundWorker backgroundWorkerGUIUpdate;
        protected System.Windows.Forms.GroupBox groupBoxEventLog;
        private System.Windows.Forms.GroupBox groupBoxSettings;
        private System.Windows.Forms.TreeView treeViewSettings;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.GroupBox groupBoxSetup;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxXAxis;
        private System.Windows.Forms.CheckBox checkBoxVerbose;
        private System.Windows.Forms.Button buttonInitialize;
        private System.Windows.Forms.GroupBox groupBoxSpectrometers;
        private System.Windows.Forms.ComboBox comboBoxSpectrometer;
        private System.Windows.Forms.GroupBox groupBoxControl;
        private System.Windows.Forms.GroupBox groupBoxMode;
        private System.Windows.Forms.RadioButton radioButtonModeTransmission;
        private System.Windows.Forms.RadioButton radioButtonModeAbsorbance;
        private System.Windows.Forms.RadioButton radioButtonModeScope;
        private System.Windows.Forms.Button buttonClearTraces;
        private System.Windows.Forms.Button buttonAddTrace;
        private System.Windows.Forms.NumericUpDown numericUpDownIntegTimeMS;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.NumericUpDown numericUpDownScanAveraging;
        private System.Windows.Forms.CheckBox checkBoxTakeReference;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBoxTakeDark;
        private System.Windows.Forms.NumericUpDown numericUpDownBoxcarHalfWidth;
        private System.Windows.Forms.CheckBox checkBoxLaserEnable;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.SplitContainer splitContainerGraphVsControls;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}

