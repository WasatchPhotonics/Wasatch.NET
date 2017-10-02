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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend3 = new System.Windows.Forms.DataVisualization.Charting.Legend();
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
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
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
            this.splitContainerTopVsLog.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
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
            this.splitContainerTopVsLog.Size = new System.Drawing.Size(698, 590);
            this.splitContainerTopVsLog.SplitterDistance = 472;
            this.splitContainerTopVsLog.SplitterWidth = 3;
            this.splitContainerTopVsLog.TabIndex = 0;
            // 
            // splitContainerGraphVsControls
            // 
            this.splitContainerGraphVsControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerGraphVsControls.Location = new System.Drawing.Point(0, 0);
            this.splitContainerGraphVsControls.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
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
            this.splitContainerGraphVsControls.Size = new System.Drawing.Size(698, 472);
            this.splitContainerGraphVsControls.SplitterDistance = 479;
            this.splitContainerGraphVsControls.SplitterWidth = 3;
            this.splitContainerGraphVsControls.TabIndex = 6;
            // 
            // chart1
            // 
            chartArea3.AxisX.LabelStyle.Format = "F2";
            chartArea3.CursorX.IsUserEnabled = true;
            chartArea3.CursorX.IsUserSelectionEnabled = true;
            chartArea3.CursorY.IsUserEnabled = true;
            chartArea3.CursorY.IsUserSelectionEnabled = true;
            chartArea3.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea3);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            legend3.Alignment = System.Drawing.StringAlignment.Center;
            legend3.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            legend3.Name = "Legend1";
            this.chart1.Legends.Add(legend3);
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(479, 472);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            // 
            // groupBoxSettings
            // 
            this.groupBoxSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxSettings.Controls.Add(this.treeViewSettings);
            this.groupBoxSettings.Location = new System.Drawing.Point(2, 350);
            this.groupBoxSettings.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxSettings.Name = "groupBoxSettings";
            this.groupBoxSettings.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxSettings.Size = new System.Drawing.Size(212, 115);
            this.groupBoxSettings.TabIndex = 2;
            this.groupBoxSettings.TabStop = false;
            this.groupBoxSettings.Text = "Settings";
            // 
            // treeViewSettings
            // 
            this.treeViewSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewSettings.Location = new System.Drawing.Point(2, 15);
            this.treeViewSettings.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.treeViewSettings.Name = "treeViewSettings";
            this.treeViewSettings.Size = new System.Drawing.Size(208, 98);
            this.treeViewSettings.TabIndex = 0;
            this.treeViewSettings.DoubleClick += new System.EventHandler(this.treeViewSettings_DoubleClick);
            // 
            // groupBoxSpectrometers
            // 
            this.groupBoxSpectrometers.Controls.Add(this.comboBoxSpectrometer);
            this.groupBoxSpectrometers.Controls.Add(this.groupBoxControl);
            this.groupBoxSpectrometers.Enabled = false;
            this.groupBoxSpectrometers.Location = new System.Drawing.Point(2, 73);
            this.groupBoxSpectrometers.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxSpectrometers.Name = "groupBoxSpectrometers";
            this.groupBoxSpectrometers.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxSpectrometers.Size = new System.Drawing.Size(210, 272);
            this.groupBoxSpectrometers.TabIndex = 1;
            this.groupBoxSpectrometers.TabStop = false;
            this.groupBoxSpectrometers.Text = "Spectrometers";
            // 
            // comboBoxSpectrometer
            // 
            this.comboBoxSpectrometer.FormattingEnabled = true;
            this.comboBoxSpectrometer.Location = new System.Drawing.Point(4, 17);
            this.comboBoxSpectrometer.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.comboBoxSpectrometer.Name = "comboBoxSpectrometer";
            this.comboBoxSpectrometer.Size = new System.Drawing.Size(201, 21);
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
            this.groupBoxControl.Location = new System.Drawing.Point(0, 41);
            this.groupBoxControl.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxControl.Name = "groupBoxControl";
            this.groupBoxControl.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxControl.Size = new System.Drawing.Size(205, 227);
            this.groupBoxControl.TabIndex = 1;
            this.groupBoxControl.TabStop = false;
            this.groupBoxControl.Text = "Control";
            // 
            // groupBoxMode
            // 
            this.groupBoxMode.Controls.Add(this.radioButtonModeTransmission);
            this.groupBoxMode.Controls.Add(this.radioButtonModeAbsorbance);
            this.groupBoxMode.Controls.Add(this.radioButtonModeScope);
            this.groupBoxMode.Location = new System.Drawing.Point(4, 107);
            this.groupBoxMode.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxMode.Name = "groupBoxMode";
            this.groupBoxMode.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxMode.Size = new System.Drawing.Size(196, 40);
            this.groupBoxMode.TabIndex = 13;
            this.groupBoxMode.TabStop = false;
            this.groupBoxMode.Text = "Mode";
            // 
            // radioButtonModeTransmission
            // 
            this.radioButtonModeTransmission.AutoSize = true;
            this.radioButtonModeTransmission.Enabled = false;
            this.radioButtonModeTransmission.Location = new System.Drawing.Point(130, 17);
            this.radioButtonModeTransmission.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.radioButtonModeTransmission.Name = "radioButtonModeTransmission";
            this.radioButtonModeTransmission.Size = new System.Drawing.Size(67, 17);
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
            this.radioButtonModeAbsorbance.Location = new System.Drawing.Point(54, 17);
            this.radioButtonModeAbsorbance.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.radioButtonModeAbsorbance.Name = "radioButtonModeAbsorbance";
            this.radioButtonModeAbsorbance.Size = new System.Drawing.Size(81, 17);
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
            this.radioButtonModeScope.Location = new System.Drawing.Point(4, 17);
            this.radioButtonModeScope.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.radioButtonModeScope.Name = "radioButtonModeScope";
            this.radioButtonModeScope.Size = new System.Drawing.Size(54, 17);
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
            this.buttonClearTraces.Location = new System.Drawing.Point(120, 188);
            this.buttonClearTraces.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonClearTraces.Name = "buttonClearTraces";
            this.buttonClearTraces.Size = new System.Drawing.Size(33, 33);
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
            this.buttonAddTrace.Location = new System.Drawing.Point(82, 188);
            this.buttonAddTrace.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonAddTrace.Name = "buttonAddTrace";
            this.buttonAddTrace.Size = new System.Drawing.Size(33, 33);
            this.buttonAddTrace.TabIndex = 11;
            this.toolTip1.SetToolTip(this.buttonAddTrace, "Add spectrum trace");
            this.buttonAddTrace.UseVisualStyleBackColor = true;
            this.buttonAddTrace.Click += new System.EventHandler(this.buttonAddTrace_Click);
            // 
            // numericUpDownIntegTimeMS
            // 
            this.numericUpDownIntegTimeMS.Location = new System.Drawing.Point(4, 17);
            this.numericUpDownIntegTimeMS.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
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
            this.numericUpDownIntegTimeMS.Size = new System.Drawing.Size(59, 20);
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
            this.buttonSave.Location = new System.Drawing.Point(158, 188);
            this.buttonSave.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(28, 33);
            this.buttonSave.TabIndex = 10;
            this.toolTip1.SetToolTip(this.buttonSave, "Save spectra");
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(68, 19);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "integration time (ms)";
            // 
            // buttonStart
            // 
            this.buttonStart.BackColor = System.Drawing.Color.Green;
            this.buttonStart.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.ForeColor = System.Drawing.Color.White;
            this.buttonStart.Location = new System.Drawing.Point(4, 152);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(191, 31);
            this.buttonStart.TabIndex = 9;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = false;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // numericUpDownScanAveraging
            // 
            this.numericUpDownScanAveraging.Location = new System.Drawing.Point(4, 40);
            this.numericUpDownScanAveraging.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.numericUpDownScanAveraging.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownScanAveraging.Name = "numericUpDownScanAveraging";
            this.numericUpDownScanAveraging.Size = new System.Drawing.Size(59, 20);
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
            this.checkBoxTakeReference.Location = new System.Drawing.Point(44, 188);
            this.checkBoxTakeReference.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.checkBoxTakeReference.Name = "checkBoxTakeReference";
            this.checkBoxTakeReference.Size = new System.Drawing.Size(34, 33);
            this.checkBoxTakeReference.TabIndex = 8;
            this.toolTip1.SetToolTip(this.checkBoxTakeReference, "Use current spectrum as reference");
            this.checkBoxTakeReference.UseVisualStyleBackColor = true;
            this.checkBoxTakeReference.CheckedChanged += new System.EventHandler(this.checkBoxTakeReference_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(68, 41);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "scan averaging";
            // 
            // checkBoxTakeDark
            // 
            this.checkBoxTakeDark.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxTakeDark.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("checkBoxTakeDark.BackgroundImage")));
            this.checkBoxTakeDark.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.checkBoxTakeDark.Enabled = false;
            this.checkBoxTakeDark.Location = new System.Drawing.Point(4, 188);
            this.checkBoxTakeDark.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.checkBoxTakeDark.Name = "checkBoxTakeDark";
            this.checkBoxTakeDark.Size = new System.Drawing.Size(34, 33);
            this.checkBoxTakeDark.TabIndex = 7;
            this.toolTip1.SetToolTip(this.checkBoxTakeDark, "Use current spectrum as dark");
            this.checkBoxTakeDark.UseVisualStyleBackColor = true;
            this.checkBoxTakeDark.CheckedChanged += new System.EventHandler(this.checkBoxTakeDark_CheckedChanged);
            // 
            // numericUpDownBoxcarHalfWidth
            // 
            this.numericUpDownBoxcarHalfWidth.Location = new System.Drawing.Point(4, 63);
            this.numericUpDownBoxcarHalfWidth.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.numericUpDownBoxcarHalfWidth.Name = "numericUpDownBoxcarHalfWidth";
            this.numericUpDownBoxcarHalfWidth.Size = new System.Drawing.Size(59, 20);
            this.numericUpDownBoxcarHalfWidth.TabIndex = 4;
            this.numericUpDownBoxcarHalfWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownBoxcarHalfWidth.ValueChanged += new System.EventHandler(this.numericUpDownBoxcarHalfWidth_ValueChanged);
            // 
            // checkBoxLaserEnable
            // 
            this.checkBoxLaserEnable.AutoSize = true;
            this.checkBoxLaserEnable.Location = new System.Drawing.Point(52, 85);
            this.checkBoxLaserEnable.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.checkBoxLaserEnable.Name = "checkBoxLaserEnable";
            this.checkBoxLaserEnable.Size = new System.Drawing.Size(83, 17);
            this.checkBoxLaserEnable.TabIndex = 6;
            this.checkBoxLaserEnable.Text = "laser enable";
            this.checkBoxLaserEnable.UseVisualStyleBackColor = true;
            this.checkBoxLaserEnable.CheckedChanged += new System.EventHandler(this.checkBoxLaserEnable_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(68, 64);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "boxcar half-width";
            // 
            // groupBoxSetup
            // 
            this.groupBoxSetup.Controls.Add(this.label4);
            this.groupBoxSetup.Controls.Add(this.comboBoxXAxis);
            this.groupBoxSetup.Controls.Add(this.checkBoxVerbose);
            this.groupBoxSetup.Controls.Add(this.buttonInitialize);
            this.groupBoxSetup.Location = new System.Drawing.Point(2, 2);
            this.groupBoxSetup.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxSetup.Name = "groupBoxSetup";
            this.groupBoxSetup.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxSetup.Size = new System.Drawing.Size(210, 66);
            this.groupBoxSetup.TabIndex = 0;
            this.groupBoxSetup.TabStop = false;
            this.groupBoxSetup.Text = "Setup";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(100, 41);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(36, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "X-Axis";
            // 
            // comboBoxXAxis
            // 
            this.comboBoxXAxis.FormattingEnabled = true;
            this.comboBoxXAxis.Items.AddRange(new object[] {
            "Wavelength",
            "Wavenumber"});
            this.comboBoxXAxis.Location = new System.Drawing.Point(4, 39);
            this.comboBoxXAxis.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.comboBoxXAxis.Name = "comboBoxXAxis";
            this.comboBoxXAxis.Size = new System.Drawing.Size(92, 21);
            this.comboBoxXAxis.TabIndex = 2;
            this.comboBoxXAxis.SelectedIndexChanged += new System.EventHandler(this.comboBoxXAxis_SelectedIndexChanged);
            // 
            // checkBoxVerbose
            // 
            this.checkBoxVerbose.AutoSize = true;
            this.checkBoxVerbose.Location = new System.Drawing.Point(82, 17);
            this.checkBoxVerbose.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.checkBoxVerbose.Name = "checkBoxVerbose";
            this.checkBoxVerbose.Size = new System.Drawing.Size(64, 17);
            this.checkBoxVerbose.TabIndex = 1;
            this.checkBoxVerbose.Text = "verbose";
            this.checkBoxVerbose.UseVisualStyleBackColor = true;
            this.checkBoxVerbose.CheckedChanged += new System.EventHandler(this.checkBoxVerbose_CheckedChanged);
            // 
            // buttonInitialize
            // 
            this.buttonInitialize.Location = new System.Drawing.Point(4, 17);
            this.buttonInitialize.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonInitialize.Name = "buttonInitialize";
            this.buttonInitialize.Size = new System.Drawing.Size(56, 19);
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
            this.groupBoxEventLog.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxEventLog.Name = "groupBoxEventLog";
            this.groupBoxEventLog.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBoxEventLog.Size = new System.Drawing.Size(698, 115);
            this.groupBoxEventLog.TabIndex = 0;
            this.groupBoxEventLog.TabStop = false;
            this.groupBoxEventLog.Text = "Event Log";
            // 
            // textBoxEventLog
            // 
            this.textBoxEventLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxEventLog.Location = new System.Drawing.Point(2, 15);
            this.textBoxEventLog.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBoxEventLog.Multiline = true;
            this.textBoxEventLog.Name = "textBoxEventLog";
            this.textBoxEventLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxEventLog.Size = new System.Drawing.Size(694, 98);
            this.textBoxEventLog.TabIndex = 0;
            // 
            // backgroundWorkerGUIUpdate
            // 
            this.backgroundWorkerGUIUpdate.WorkerSupportsCancellation = true;
            this.backgroundWorkerGUIUpdate.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerGUIUpdate_DoWork);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "csv";
            this.saveFileDialog1.Title = "Save spectra";
            // 
            // Form1
            // 
            this.AcceptButton = this.buttonInitialize;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(698, 590);
            this.Controls.Add(this.splitContainerTopVsLog);
            this.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
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
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}

