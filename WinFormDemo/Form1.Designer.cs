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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItemTest = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemTestWriteEEPROM = new System.Windows.Forms.ToolStripMenuItem();
            this.setDFUModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemEEPROMJson = new System.Windows.Forms.ToolStripMenuItem();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.groupBoxSetup = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxXAxis = new System.Windows.Forms.ComboBox();
            this.checkBoxVerbose = new System.Windows.Forms.CheckBox();
            this.buttonInitialize = new System.Windows.Forms.Button();
            this.groupBoxSpectrometers = new System.Windows.Forms.GroupBox();
            this.comboBoxSpectrometer = new System.Windows.Forms.ComboBox();
            this.groupBoxControl = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageAcquisition = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.numericUpDownIntegTimeMS = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDownScanAveraging = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDownBoxcarHalfWidth = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBoxExternalTriggerSource = new System.Windows.Forms.CheckBox();
            this.checkBoxRamanCorrection = new System.Windows.Forms.CheckBox();
            this.tabPageLaser = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.numericUpDownLaserPowerPerc = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.numericUpDownLaserPowerMW = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.checkBoxLaserEnable = new System.Windows.Forms.CheckBox();
            this.tabPageTEC = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.numericUpDownDetectorSetpointDegC = new System.Windows.Forms.NumericUpDown();
            this.labelDetTempDegC = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.tabPageMisc = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.label7 = new System.Windows.Forms.Label();
            this.numericUpDownAcquisitionPeriodMS = new System.Windows.Forms.NumericUpDown();
            this.checkBoxContinuousAcquisition = new System.Windows.Forms.CheckBox();
            this.labelSpectrumCount = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.tabPageAccessories = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxAccessoriesEnabled = new System.Windows.Forms.CheckBox();
            this.checkBoxLampEnabled = new System.Windows.Forms.CheckBox();
            this.groupBoxMode = new System.Windows.Forms.GroupBox();
            this.radioButtonModeTransmission = new System.Windows.Forms.RadioButton();
            this.radioButtonModeAbsorbance = new System.Windows.Forms.RadioButton();
            this.radioButtonModeScope = new System.Windows.Forms.RadioButton();
            this.buttonStart = new System.Windows.Forms.Button();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxTakeDark = new System.Windows.Forms.CheckBox();
            this.checkBoxTakeReference = new System.Windows.Forms.CheckBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonAddTrace = new System.Windows.Forms.Button();
            this.buttonClearTraces = new System.Windows.Forms.Button();
            this.groupBoxSettings = new System.Windows.Forms.GroupBox();
            this.treeViewSettings = new System.Windows.Forms.TreeView();
            this.groupBoxEventLog = new System.Windows.Forms.GroupBox();
            this.textBoxEventLog = new System.Windows.Forms.TextBox();
            this.backgroundWorkerGUIUpdate = new System.ComponentModel.BackgroundWorker();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.backgroundWorkerSettings = new System.ComponentModel.BackgroundWorker();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.checkBoxLaserPowerInMW = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTopVsLog)).BeginInit();
            this.splitContainerTopVsLog.Panel1.SuspendLayout();
            this.splitContainerTopVsLog.Panel2.SuspendLayout();
            this.splitContainerTopVsLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerGraphVsControls)).BeginInit();
            this.splitContainerGraphVsControls.Panel1.SuspendLayout();
            this.splitContainerGraphVsControls.Panel2.SuspendLayout();
            this.splitContainerGraphVsControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.groupBoxSetup.SuspendLayout();
            this.groupBoxSpectrometers.SuspendLayout();
            this.groupBoxControl.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageAcquisition.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownIntegTimeMS)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownScanAveraging)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBoxcarHalfWidth)).BeginInit();
            this.tabPageLaser.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLaserPowerPerc)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLaserPowerMW)).BeginInit();
            this.tabPageTEC.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDetectorSetpointDegC)).BeginInit();
            this.tabPageMisc.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAcquisitionPeriodMS)).BeginInit();
            this.tabPageAccessories.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.groupBoxMode.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.groupBoxSettings.SuspendLayout();
            this.groupBoxEventLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerTopVsLog
            // 
            this.splitContainerTopVsLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerTopVsLog.Location = new System.Drawing.Point(0, 0);
            this.splitContainerTopVsLog.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
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
            this.splitContainerTopVsLog.Size = new System.Drawing.Size(1059, 807);
            this.splitContainerTopVsLog.SplitterDistance = 715;
            this.splitContainerTopVsLog.TabIndex = 0;
            // 
            // splitContainerGraphVsControls
            // 
            this.splitContainerGraphVsControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerGraphVsControls.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerGraphVsControls.Location = new System.Drawing.Point(0, 0);
            this.splitContainerGraphVsControls.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainerGraphVsControls.Name = "splitContainerGraphVsControls";
            // 
            // splitContainerGraphVsControls.Panel1
            // 
            this.splitContainerGraphVsControls.Panel1.Controls.Add(this.chart1);
            this.splitContainerGraphVsControls.Panel1.Controls.Add(this.menuStrip1);
            // 
            // splitContainerGraphVsControls.Panel2
            // 
            this.splitContainerGraphVsControls.Panel2.Controls.Add(this.flowLayoutPanel3);
            this.splitContainerGraphVsControls.Size = new System.Drawing.Size(1059, 715);
            this.splitContainerGraphVsControls.SplitterDistance = 830;
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
            this.chart1.Location = new System.Drawing.Point(0, 26);
            this.chart1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(830, 689);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemTest});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
            this.menuStrip1.Size = new System.Drawing.Size(830, 26);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItemTest
            // 
            this.toolStripMenuItemTest.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemTestWriteEEPROM,
            this.setDFUModeToolStripMenuItem,
            this.toolStripMenuItemEEPROMJson});
            this.toolStripMenuItemTest.Enabled = false;
            this.toolStripMenuItemTest.Name = "toolStripMenuItemTest";
            this.toolStripMenuItemTest.Size = new System.Drawing.Size(49, 24);
            this.toolStripMenuItemTest.Text = "Test";
            // 
            // toolStripMenuItemTestWriteEEPROM
            // 
            this.toolStripMenuItemTestWriteEEPROM.Name = "toolStripMenuItemTestWriteEEPROM";
            this.toolStripMenuItemTestWriteEEPROM.Size = new System.Drawing.Size(235, 26);
            this.toolStripMenuItemTestWriteEEPROM.Text = "Write EEPROM...";
            this.toolStripMenuItemTestWriteEEPROM.ToolTipText = "Demonstrate how to write to the EEPROM";
            this.toolStripMenuItemTestWriteEEPROM.Click += new System.EventHandler(this.toolStripMenuItemTestWriteEEPROM_Click);
            // 
            // setDFUModeToolStripMenuItem
            // 
            this.setDFUModeToolStripMenuItem.Name = "setDFUModeToolStripMenuItem";
            this.setDFUModeToolStripMenuItem.Size = new System.Drawing.Size(235, 26);
            this.setDFUModeToolStripMenuItem.Text = "Set DFU mode";
            this.setDFUModeToolStripMenuItem.ToolTipText = "WARNING: used for reflashing ARM firmware!";
            this.setDFUModeToolStripMenuItem.Click += new System.EventHandler(this.setDFUModeToolStripMenuItem_Click);
            // 
            // toolStripMenuItemEEPROMJson
            // 
            this.toolStripMenuItemEEPROMJson.Name = "toolStripMenuItemEEPROMJson";
            this.toolStripMenuItemEEPROMJson.Size = new System.Drawing.Size(235, 26);
            this.toolStripMenuItemEEPROMJson.Text = "Log EEPROM as JSON";
            this.toolStripMenuItemEEPROMJson.ToolTipText = "Log the EEPROM contents as JSON";
            this.toolStripMenuItemEEPROMJson.Click += new System.EventHandler(this.toolStripMenuItemEEPROMJson_Click);
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoScroll = true;
            this.flowLayoutPanel3.Controls.Add(this.groupBoxSetup);
            this.flowLayoutPanel3.Controls.Add(this.groupBoxSpectrometers);
            this.flowLayoutPanel3.Controls.Add(this.groupBoxSettings);
            this.flowLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel3.Margin = new System.Windows.Forms.Padding(4);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(303, 718);
            this.flowLayoutPanel3.TabIndex = 2;
            // 
            // groupBoxSetup
            // 
            this.groupBoxSetup.Controls.Add(this.label4);
            this.groupBoxSetup.Controls.Add(this.comboBoxXAxis);
            this.groupBoxSetup.Controls.Add(this.checkBoxVerbose);
            this.groupBoxSetup.Controls.Add(this.buttonInitialize);
            this.groupBoxSetup.Location = new System.Drawing.Point(3, 2);
            this.groupBoxSetup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxSetup.Name = "groupBoxSetup";
            this.groupBoxSetup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxSetup.Size = new System.Drawing.Size(280, 81);
            this.groupBoxSetup.TabIndex = 0;
            this.groupBoxSetup.TabStop = false;
            this.groupBoxSetup.Text = "Setup";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(133, 53);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(44, 16);
            this.label4.TabIndex = 3;
            this.label4.Text = "X-Axis";
            // 
            // comboBoxXAxis
            // 
            this.comboBoxXAxis.FormattingEnabled = true;
            this.comboBoxXAxis.Items.AddRange(new object[] {
            "Wavelength",
            "Wavenumber"});
            this.comboBoxXAxis.Location = new System.Drawing.Point(5, 48);
            this.comboBoxXAxis.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.comboBoxXAxis.Name = "comboBoxXAxis";
            this.comboBoxXAxis.Size = new System.Drawing.Size(121, 24);
            this.comboBoxXAxis.TabIndex = 2;
            this.comboBoxXAxis.SelectedIndexChanged += new System.EventHandler(this.comboBoxXAxis_SelectedIndexChanged);
            // 
            // checkBoxVerbose
            // 
            this.checkBoxVerbose.AutoSize = true;
            this.checkBoxVerbose.Location = new System.Drawing.Point(109, 22);
            this.checkBoxVerbose.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxVerbose.Name = "checkBoxVerbose";
            this.checkBoxVerbose.Size = new System.Drawing.Size(79, 20);
            this.checkBoxVerbose.TabIndex = 1;
            this.checkBoxVerbose.Text = "verbose";
            this.checkBoxVerbose.UseVisualStyleBackColor = true;
            this.checkBoxVerbose.CheckedChanged += new System.EventHandler(this.checkBoxVerbose_CheckedChanged);
            // 
            // buttonInitialize
            // 
            this.buttonInitialize.Location = new System.Drawing.Point(5, 21);
            this.buttonInitialize.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonInitialize.Name = "buttonInitialize";
            this.buttonInitialize.Size = new System.Drawing.Size(75, 25);
            this.buttonInitialize.TabIndex = 0;
            this.buttonInitialize.Text = "Initialize";
            this.buttonInitialize.UseVisualStyleBackColor = true;
            this.buttonInitialize.Click += new System.EventHandler(this.buttonInitialize_Click);
            // 
            // groupBoxSpectrometers
            // 
            this.groupBoxSpectrometers.Controls.Add(this.comboBoxSpectrometer);
            this.groupBoxSpectrometers.Controls.Add(this.groupBoxControl);
            this.groupBoxSpectrometers.Enabled = false;
            this.groupBoxSpectrometers.Location = new System.Drawing.Point(3, 87);
            this.groupBoxSpectrometers.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxSpectrometers.Name = "groupBoxSpectrometers";
            this.groupBoxSpectrometers.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxSpectrometers.Size = new System.Drawing.Size(280, 441);
            this.groupBoxSpectrometers.TabIndex = 1;
            this.groupBoxSpectrometers.TabStop = false;
            this.groupBoxSpectrometers.Text = "Spectrometers";
            // 
            // comboBoxSpectrometer
            // 
            this.comboBoxSpectrometer.FormattingEnabled = true;
            this.comboBoxSpectrometer.Location = new System.Drawing.Point(5, 21);
            this.comboBoxSpectrometer.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.comboBoxSpectrometer.Name = "comboBoxSpectrometer";
            this.comboBoxSpectrometer.Size = new System.Drawing.Size(267, 24);
            this.comboBoxSpectrometer.TabIndex = 0;
            this.comboBoxSpectrometer.SelectedIndexChanged += new System.EventHandler(this.comboBoxSpectrometer_SelectedIndexChanged);
            // 
            // groupBoxControl
            // 
            this.groupBoxControl.Controls.Add(this.flowLayoutPanel1);
            this.groupBoxControl.Location = new System.Drawing.Point(0, 50);
            this.groupBoxControl.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxControl.Name = "groupBoxControl";
            this.groupBoxControl.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxControl.Size = new System.Drawing.Size(273, 385);
            this.groupBoxControl.TabIndex = 1;
            this.groupBoxControl.TabStop = false;
            this.groupBoxControl.Text = "Control";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.tabControl1);
            this.flowLayoutPanel1.Controls.Add(this.groupBoxMode);
            this.flowLayoutPanel1.Controls.Add(this.buttonStart);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 17);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(267, 366);
            this.flowLayoutPanel1.TabIndex = 3;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageAcquisition);
            this.tabControl1.Controls.Add(this.tabPageLaser);
            this.tabControl1.Controls.Add(this.tabPageTEC);
            this.tabControl1.Controls.Add(this.tabPageMisc);
            this.tabControl1.Controls.Add(this.tabPageAccessories);
            this.tabControl1.Location = new System.Drawing.Point(3, 2);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(261, 208);
            this.tabControl1.TabIndex = 2;
            // 
            // tabPageAcquisition
            // 
            this.tabPageAcquisition.Controls.Add(this.tableLayoutPanel1);
            this.tabPageAcquisition.Location = new System.Drawing.Point(4, 25);
            this.tabPageAcquisition.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageAcquisition.Name = "tabPageAcquisition";
            this.tabPageAcquisition.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageAcquisition.Size = new System.Drawing.Size(253, 179);
            this.tabPageAcquisition.TabIndex = 0;
            this.tabPageAcquisition.Text = "Acquisition";
            this.tabPageAcquisition.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.numericUpDownIntegTimeMS, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.numericUpDownScanAveraging, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.numericUpDownBoxcarHalfWidth, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label3, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxExternalTriggerSource, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxRamanCorrection, 1, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 2);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(247, 175);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // numericUpDownIntegTimeMS
            // 
            this.numericUpDownIntegTimeMS.Location = new System.Drawing.Point(3, 2);
            this.numericUpDownIntegTimeMS.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numericUpDownIntegTimeMS.Maximum = new decimal(new int[] {
            300000,
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(88, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "integ (ms)";
            // 
            // numericUpDownScanAveraging
            // 
            this.numericUpDownScanAveraging.Location = new System.Drawing.Point(3, 28);
            this.numericUpDownScanAveraging.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
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
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(88, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "scan avg";
            // 
            // numericUpDownBoxcarHalfWidth
            // 
            this.numericUpDownBoxcarHalfWidth.Location = new System.Drawing.Point(3, 54);
            this.numericUpDownBoxcarHalfWidth.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numericUpDownBoxcarHalfWidth.Name = "numericUpDownBoxcarHalfWidth";
            this.numericUpDownBoxcarHalfWidth.Size = new System.Drawing.Size(79, 22);
            this.numericUpDownBoxcarHalfWidth.TabIndex = 4;
            this.numericUpDownBoxcarHalfWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownBoxcarHalfWidth.ValueChanged += new System.EventHandler(this.numericUpDownBoxcarHalfWidth_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(88, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 16);
            this.label3.TabIndex = 5;
            this.label3.Text = "boxcar";
            // 
            // checkBoxExternalTriggerSource
            // 
            this.checkBoxExternalTriggerSource.AutoSize = true;
            this.checkBoxExternalTriggerSource.Location = new System.Drawing.Point(89, 82);
            this.checkBoxExternalTriggerSource.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxExternalTriggerSource.Name = "checkBoxExternalTriggerSource";
            this.checkBoxExternalTriggerSource.Size = new System.Drawing.Size(87, 20);
            this.checkBoxExternalTriggerSource.TabIndex = 18;
            this.checkBoxExternalTriggerSource.Text = "ext trigger";
            this.checkBoxExternalTriggerSource.UseVisualStyleBackColor = true;
            this.checkBoxExternalTriggerSource.CheckedChanged += new System.EventHandler(this.checkBoxExternalTriggerSource_CheckedChanged);
            // 
            // checkBoxRamanCorrection
            // 
            this.checkBoxRamanCorrection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxRamanCorrection.Location = new System.Drawing.Point(89, 110);
            this.checkBoxRamanCorrection.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxRamanCorrection.Name = "checkBoxRamanCorrection";
            this.checkBoxRamanCorrection.Size = new System.Drawing.Size(155, 61);
            this.checkBoxRamanCorrection.TabIndex = 19;
            this.checkBoxRamanCorrection.Text = "raman y-axis correction";
            this.checkBoxRamanCorrection.UseVisualStyleBackColor = true;
            this.checkBoxRamanCorrection.CheckedChanged += new System.EventHandler(this.checkBoxRamanCorrection_CheckedChanged);
            // 
            // tabPageLaser
            // 
            this.tabPageLaser.Controls.Add(this.tableLayoutPanel2);
            this.tabPageLaser.Location = new System.Drawing.Point(4, 25);
            this.tabPageLaser.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageLaser.Name = "tabPageLaser";
            this.tabPageLaser.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageLaser.Size = new System.Drawing.Size(253, 179);
            this.tabPageLaser.TabIndex = 1;
            this.tabPageLaser.Text = "Laser";
            this.tabPageLaser.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.numericUpDownLaserPowerPerc, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label5, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.numericUpDownLaserPowerMW, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label8, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.checkBoxLaserEnable, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.checkBoxLaserPowerInMW, 1, 3);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 2);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(247, 175);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // numericUpDownLaserPowerPerc
            // 
            this.numericUpDownLaserPowerPerc.DecimalPlaces = 1;
            this.numericUpDownLaserPowerPerc.Enabled = false;
            this.numericUpDownLaserPowerPerc.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownLaserPowerPerc.Location = new System.Drawing.Point(4, 4);
            this.numericUpDownLaserPowerPerc.Margin = new System.Windows.Forms.Padding(4);
            this.numericUpDownLaserPowerPerc.Name = "numericUpDownLaserPowerPerc";
            this.numericUpDownLaserPowerPerc.Size = new System.Drawing.Size(64, 22);
            this.numericUpDownLaserPowerPerc.TabIndex = 14;
            this.numericUpDownLaserPowerPerc.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownLaserPowerPerc.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownLaserPowerPerc.ValueChanged += new System.EventHandler(this.numericUpDownLaserPowerPerc_ValueChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(76, 0);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(125, 16);
            this.label5.TabIndex = 15;
            this.label5.Text = "laser power percent";
            // 
            // numericUpDownLaserPowerMW
            // 
            this.numericUpDownLaserPowerMW.Enabled = false;
            this.numericUpDownLaserPowerMW.Location = new System.Drawing.Point(3, 32);
            this.numericUpDownLaserPowerMW.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numericUpDownLaserPowerMW.Name = "numericUpDownLaserPowerMW";
            this.numericUpDownLaserPowerMW.Size = new System.Drawing.Size(65, 22);
            this.numericUpDownLaserPowerMW.TabIndex = 16;
            this.numericUpDownLaserPowerMW.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownLaserPowerMW.ValueChanged += new System.EventHandler(this.numericUpDownLaserPowerMW_ValueChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(75, 30);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(104, 16);
            this.label8.TabIndex = 17;
            this.label8.Text = "laser power mW";
            // 
            // checkBoxLaserEnable
            // 
            this.checkBoxLaserEnable.AutoSize = true;
            this.checkBoxLaserEnable.Location = new System.Drawing.Point(75, 58);
            this.checkBoxLaserEnable.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxLaserEnable.Name = "checkBoxLaserEnable";
            this.checkBoxLaserEnable.Size = new System.Drawing.Size(104, 20);
            this.checkBoxLaserEnable.TabIndex = 6;
            this.checkBoxLaserEnable.Text = "laser enable";
            this.checkBoxLaserEnable.UseVisualStyleBackColor = true;
            this.checkBoxLaserEnable.CheckedChanged += new System.EventHandler(this.checkBoxLaserEnable_CheckedChanged);
            // 
            // tabPageTEC
            // 
            this.tabPageTEC.Controls.Add(this.tableLayoutPanel3);
            this.tabPageTEC.Location = new System.Drawing.Point(4, 25);
            this.tabPageTEC.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageTEC.Name = "tabPageTEC";
            this.tabPageTEC.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageTEC.Size = new System.Drawing.Size(253, 179);
            this.tabPageTEC.TabIndex = 2;
            this.tabPageTEC.Text = "TEC";
            this.tabPageTEC.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.Controls.Add(this.numericUpDownDetectorSetpointDegC, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.labelDetTempDegC, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.label6, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label9, 1, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 2);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.Size = new System.Drawing.Size(247, 175);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // numericUpDownDetectorSetpointDegC
            // 
            this.numericUpDownDetectorSetpointDegC.Location = new System.Drawing.Point(3, 2);
            this.numericUpDownDetectorSetpointDegC.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numericUpDownDetectorSetpointDegC.Name = "numericUpDownDetectorSetpointDegC";
            this.numericUpDownDetectorSetpointDegC.Size = new System.Drawing.Size(79, 22);
            this.numericUpDownDetectorSetpointDegC.TabIndex = 16;
            this.numericUpDownDetectorSetpointDegC.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownDetectorSetpointDegC.ValueChanged += new System.EventHandler(this.numericUpDownDetectorSetpointDegC_ValueChanged);
            // 
            // labelDetTempDegC
            // 
            this.labelDetTempDegC.AutoSize = true;
            this.labelDetTempDegC.Location = new System.Drawing.Point(4, 26);
            this.labelDetTempDegC.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelDetTempDegC.Name = "labelDetTempDegC";
            this.labelDetTempDegC.Size = new System.Drawing.Size(34, 16);
            this.labelDetTempDegC.TabIndex = 4;
            this.labelDetTempDegC.Text = "10°C";
            this.labelDetTempDegC.Visible = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(88, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(152, 16);
            this.label6.TabIndex = 17;
            this.label6.Text = "detector TEC setpoint °C";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(88, 26);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(131, 16);
            this.label9.TabIndex = 18;
            this.label9.Text = "detector temperature";
            // 
            // tabPageMisc
            // 
            this.tabPageMisc.Controls.Add(this.tableLayoutPanel4);
            this.tabPageMisc.Location = new System.Drawing.Point(4, 25);
            this.tabPageMisc.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageMisc.Name = "tabPageMisc";
            this.tabPageMisc.Size = new System.Drawing.Size(253, 179);
            this.tabPageMisc.TabIndex = 3;
            this.tabPageMisc.Text = "Misc";
            this.tabPageMisc.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.Controls.Add(this.label7, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.numericUpDownAcquisitionPeriodMS, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.checkBoxContinuousAcquisition, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.labelSpectrumCount, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.label10, 1, 2);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 3;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.Size = new System.Drawing.Size(253, 179);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(91, 0);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(105, 16);
            this.label7.TabIndex = 21;
            this.label7.Text = "acq period (sec)";
            // 
            // numericUpDownAcquisitionPeriodMS
            // 
            this.numericUpDownAcquisitionPeriodMS.Location = new System.Drawing.Point(4, 4);
            this.numericUpDownAcquisitionPeriodMS.Margin = new System.Windows.Forms.Padding(4);
            this.numericUpDownAcquisitionPeriodMS.Name = "numericUpDownAcquisitionPeriodMS";
            this.numericUpDownAcquisitionPeriodMS.Size = new System.Drawing.Size(79, 22);
            this.numericUpDownAcquisitionPeriodMS.TabIndex = 20;
            this.numericUpDownAcquisitionPeriodMS.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownAcquisitionPeriodMS.ValueChanged += new System.EventHandler(this.numericUpDownAcquisitionPeriodMS_ValueChanged);
            // 
            // checkBoxContinuousAcquisition
            // 
            this.checkBoxContinuousAcquisition.AutoSize = true;
            this.checkBoxContinuousAcquisition.Location = new System.Drawing.Point(91, 34);
            this.checkBoxContinuousAcquisition.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxContinuousAcquisition.Name = "checkBoxContinuousAcquisition";
            this.checkBoxContinuousAcquisition.Size = new System.Drawing.Size(93, 20);
            this.checkBoxContinuousAcquisition.TabIndex = 19;
            this.checkBoxContinuousAcquisition.Text = "continuous";
            this.checkBoxContinuousAcquisition.UseVisualStyleBackColor = true;
            this.checkBoxContinuousAcquisition.CheckedChanged += new System.EventHandler(this.checkBoxContinuousAcquisition_CheckedChanged);
            // 
            // labelSpectrumCount
            // 
            this.labelSpectrumCount.AutoSize = true;
            this.labelSpectrumCount.Location = new System.Drawing.Point(4, 58);
            this.labelSpectrumCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSpectrumCount.Name = "labelSpectrumCount";
            this.labelSpectrumCount.Size = new System.Drawing.Size(21, 16);
            this.labelSpectrumCount.TabIndex = 5;
            this.labelSpectrumCount.Text = "##";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(91, 58);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(52, 16);
            this.label10.TabIndex = 22;
            this.label10.Text = "spectra";
            // 
            // tabPageAccessories
            // 
            this.tabPageAccessories.Controls.Add(this.tableLayoutPanel5);
            this.tabPageAccessories.Location = new System.Drawing.Point(4, 25);
            this.tabPageAccessories.Margin = new System.Windows.Forms.Padding(4);
            this.tabPageAccessories.Name = "tabPageAccessories";
            this.tabPageAccessories.Padding = new System.Windows.Forms.Padding(4);
            this.tabPageAccessories.Size = new System.Drawing.Size(253, 179);
            this.tabPageAccessories.TabIndex = 4;
            this.tabPageAccessories.Text = "Accessories";
            this.tabPageAccessories.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Controls.Add(this.checkBoxAccessoriesEnabled, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.checkBoxLampEnabled, 0, 1);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(4, 4);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 2;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(245, 171);
            this.tableLayoutPanel5.TabIndex = 1;
            // 
            // checkBoxAccessoriesEnabled
            // 
            this.checkBoxAccessoriesEnabled.AutoSize = true;
            this.checkBoxAccessoriesEnabled.Location = new System.Drawing.Point(4, 4);
            this.checkBoxAccessoriesEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxAccessoriesEnabled.Name = "checkBoxAccessoriesEnabled";
            this.checkBoxAccessoriesEnabled.Size = new System.Drawing.Size(150, 20);
            this.checkBoxAccessoriesEnabled.TabIndex = 23;
            this.checkBoxAccessoriesEnabled.Text = "Enable Accessories";
            this.checkBoxAccessoriesEnabled.UseVisualStyleBackColor = true;
            this.checkBoxAccessoriesEnabled.CheckedChanged += new System.EventHandler(this.checkBoxAccessoriesEnabled_CheckedChanged);
            // 
            // checkBoxLampEnabled
            // 
            this.checkBoxLampEnabled.AutoSize = true;
            this.checkBoxLampEnabled.Enabled = false;
            this.checkBoxLampEnabled.Location = new System.Drawing.Point(4, 32);
            this.checkBoxLampEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxLampEnabled.Name = "checkBoxLampEnabled";
            this.checkBoxLampEnabled.Size = new System.Drawing.Size(117, 20);
            this.checkBoxLampEnabled.TabIndex = 24;
            this.checkBoxLampEnabled.Text = "Lamp Enabled";
            this.checkBoxLampEnabled.UseVisualStyleBackColor = true;
            this.checkBoxLampEnabled.CheckedChanged += new System.EventHandler(this.checkBoxLampEnabled_CheckedChanged);
            // 
            // groupBoxMode
            // 
            this.groupBoxMode.Controls.Add(this.radioButtonModeTransmission);
            this.groupBoxMode.Controls.Add(this.radioButtonModeAbsorbance);
            this.groupBoxMode.Controls.Add(this.radioButtonModeScope);
            this.groupBoxMode.Location = new System.Drawing.Point(3, 214);
            this.groupBoxMode.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxMode.Name = "groupBoxMode";
            this.groupBoxMode.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxMode.Size = new System.Drawing.Size(261, 49);
            this.groupBoxMode.TabIndex = 13;
            this.groupBoxMode.TabStop = false;
            this.groupBoxMode.Text = "Mode";
            // 
            // radioButtonModeTransmission
            // 
            this.radioButtonModeTransmission.AutoSize = true;
            this.radioButtonModeTransmission.Enabled = false;
            this.radioButtonModeTransmission.Location = new System.Drawing.Point(173, 21);
            this.radioButtonModeTransmission.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.radioButtonModeTransmission.Name = "radioButtonModeTransmission";
            this.radioButtonModeTransmission.Size = new System.Drawing.Size(79, 20);
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
            this.radioButtonModeAbsorbance.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.radioButtonModeAbsorbance.Name = "radioButtonModeAbsorbance";
            this.radioButtonModeAbsorbance.Size = new System.Drawing.Size(101, 20);
            this.radioButtonModeAbsorbance.TabIndex = 1;
            this.radioButtonModeAbsorbance.TabStop = true;
            this.radioButtonModeAbsorbance.Text = "absorbance";
            this.toolTip1.SetToolTip(this.radioButtonModeAbsorbance, "Beer\'s Law (AU)");
            this.radioButtonModeAbsorbance.UseVisualStyleBackColor = true;
            this.radioButtonModeAbsorbance.CheckedChanged += new System.EventHandler(this.radioButtonModeAbsorbance_CheckedChanged);
            // 
            // radioButtonModeScope
            // 
            this.radioButtonModeScope.AutoSize = true;
            this.radioButtonModeScope.Checked = true;
            this.radioButtonModeScope.Location = new System.Drawing.Point(5, 21);
            this.radioButtonModeScope.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.radioButtonModeScope.Name = "radioButtonModeScope";
            this.radioButtonModeScope.Size = new System.Drawing.Size(66, 20);
            this.radioButtonModeScope.TabIndex = 0;
            this.radioButtonModeScope.TabStop = true;
            this.radioButtonModeScope.Text = "scope";
            this.toolTip1.SetToolTip(this.radioButtonModeScope, "Graph raw spectra");
            this.radioButtonModeScope.UseVisualStyleBackColor = true;
            this.radioButtonModeScope.CheckedChanged += new System.EventHandler(this.radioButtonModeScope_CheckedChanged);
            // 
            // buttonStart
            // 
            this.buttonStart.BackColor = System.Drawing.Color.Green;
            this.buttonStart.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.ForeColor = System.Drawing.Color.White;
            this.buttonStart.Location = new System.Drawing.Point(3, 267);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(261, 38);
            this.buttonStart.TabIndex = 9;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = false;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.checkBoxTakeDark);
            this.flowLayoutPanel2.Controls.Add(this.checkBoxTakeReference);
            this.flowLayoutPanel2.Controls.Add(this.buttonSave);
            this.flowLayoutPanel2.Controls.Add(this.buttonAddTrace);
            this.flowLayoutPanel2.Controls.Add(this.buttonClearTraces);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 309);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(261, 50);
            this.flowLayoutPanel2.TabIndex = 4;
            // 
            // checkBoxTakeDark
            // 
            this.checkBoxTakeDark.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxTakeDark.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("checkBoxTakeDark.BackgroundImage")));
            this.checkBoxTakeDark.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.checkBoxTakeDark.Enabled = false;
            this.checkBoxTakeDark.Location = new System.Drawing.Point(3, 2);
            this.checkBoxTakeDark.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxTakeDark.Name = "checkBoxTakeDark";
            this.checkBoxTakeDark.Size = new System.Drawing.Size(45, 39);
            this.checkBoxTakeDark.TabIndex = 7;
            this.toolTip1.SetToolTip(this.checkBoxTakeDark, "Use current spectrum as dark");
            this.checkBoxTakeDark.UseVisualStyleBackColor = true;
            this.checkBoxTakeDark.CheckedChanged += new System.EventHandler(this.checkBoxTakeDark_CheckedChanged);
            // 
            // checkBoxTakeReference
            // 
            this.checkBoxTakeReference.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxTakeReference.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("checkBoxTakeReference.BackgroundImage")));
            this.checkBoxTakeReference.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.checkBoxTakeReference.Enabled = false;
            this.checkBoxTakeReference.Location = new System.Drawing.Point(54, 2);
            this.checkBoxTakeReference.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxTakeReference.Name = "checkBoxTakeReference";
            this.checkBoxTakeReference.Size = new System.Drawing.Size(45, 39);
            this.checkBoxTakeReference.TabIndex = 8;
            this.toolTip1.SetToolTip(this.checkBoxTakeReference, "Use current spectrum as reference");
            this.checkBoxTakeReference.UseVisualStyleBackColor = true;
            this.checkBoxTakeReference.CheckedChanged += new System.EventHandler(this.checkBoxTakeReference_CheckedChanged);
            // 
            // buttonSave
            // 
            this.buttonSave.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("buttonSave.BackgroundImage")));
            this.buttonSave.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonSave.Enabled = false;
            this.buttonSave.Location = new System.Drawing.Point(105, 2);
            this.buttonSave.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(37, 39);
            this.buttonSave.TabIndex = 10;
            this.toolTip1.SetToolTip(this.buttonSave, "Save spectra");
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonAddTrace
            // 
            this.buttonAddTrace.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("buttonAddTrace.BackgroundImage")));
            this.buttonAddTrace.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonAddTrace.Enabled = false;
            this.buttonAddTrace.Location = new System.Drawing.Point(148, 2);
            this.buttonAddTrace.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonAddTrace.Name = "buttonAddTrace";
            this.buttonAddTrace.Size = new System.Drawing.Size(44, 39);
            this.buttonAddTrace.TabIndex = 11;
            this.toolTip1.SetToolTip(this.buttonAddTrace, "Add spectrum trace");
            this.buttonAddTrace.UseVisualStyleBackColor = true;
            this.buttonAddTrace.Click += new System.EventHandler(this.buttonAddTrace_Click);
            // 
            // buttonClearTraces
            // 
            this.buttonClearTraces.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("buttonClearTraces.BackgroundImage")));
            this.buttonClearTraces.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonClearTraces.Enabled = false;
            this.buttonClearTraces.Location = new System.Drawing.Point(198, 2);
            this.buttonClearTraces.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonClearTraces.Name = "buttonClearTraces";
            this.buttonClearTraces.Size = new System.Drawing.Size(44, 39);
            this.buttonClearTraces.TabIndex = 12;
            this.toolTip1.SetToolTip(this.buttonClearTraces, "Clear all spectra traces");
            this.buttonClearTraces.UseVisualStyleBackColor = true;
            this.buttonClearTraces.Click += new System.EventHandler(this.buttonClearTraces_Click);
            // 
            // groupBoxSettings
            // 
            this.groupBoxSettings.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.groupBoxSettings.Controls.Add(this.treeViewSettings);
            this.groupBoxSettings.Location = new System.Drawing.Point(3, 532);
            this.groupBoxSettings.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxSettings.MinimumSize = new System.Drawing.Size(133, 123);
            this.groupBoxSettings.Name = "groupBoxSettings";
            this.groupBoxSettings.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxSettings.Size = new System.Drawing.Size(280, 181);
            this.groupBoxSettings.TabIndex = 2;
            this.groupBoxSettings.TabStop = false;
            this.groupBoxSettings.Text = "Settings";
            // 
            // treeViewSettings
            // 
            this.treeViewSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewSettings.Location = new System.Drawing.Point(3, 17);
            this.treeViewSettings.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.treeViewSettings.Name = "treeViewSettings";
            this.treeViewSettings.Size = new System.Drawing.Size(274, 162);
            this.treeViewSettings.TabIndex = 0;
            this.toolTip1.SetToolTip(this.treeViewSettings, "Double-click to update");
            this.treeViewSettings.DoubleClick += new System.EventHandler(this.treeViewSettings_DoubleClick);
            // 
            // groupBoxEventLog
            // 
            this.groupBoxEventLog.Controls.Add(this.textBoxEventLog);
            this.groupBoxEventLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxEventLog.Location = new System.Drawing.Point(0, 0);
            this.groupBoxEventLog.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxEventLog.Name = "groupBoxEventLog";
            this.groupBoxEventLog.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxEventLog.Size = new System.Drawing.Size(1059, 88);
            this.groupBoxEventLog.TabIndex = 0;
            this.groupBoxEventLog.TabStop = false;
            this.groupBoxEventLog.Text = "Event Log";
            // 
            // textBoxEventLog
            // 
            this.textBoxEventLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxEventLog.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxEventLog.Location = new System.Drawing.Point(3, 17);
            this.textBoxEventLog.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBoxEventLog.Multiline = true;
            this.textBoxEventLog.Name = "textBoxEventLog";
            this.textBoxEventLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxEventLog.Size = new System.Drawing.Size(1053, 69);
            this.textBoxEventLog.TabIndex = 0;
            // 
            // backgroundWorkerGUIUpdate
            // 
            this.backgroundWorkerGUIUpdate.WorkerSupportsCancellation = true;
            this.backgroundWorkerGUIUpdate.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerGUIUpdate_DoWork);
            // 
            // backgroundWorkerSettings
            // 
            this.backgroundWorkerSettings.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerSettings_DoWork);
            // 
            // checkBoxLaserPowerInMW
            // 
            this.checkBoxLaserPowerInMW.AutoSize = true;
            this.checkBoxLaserPowerInMW.Location = new System.Drawing.Point(75, 83);
            this.checkBoxLaserPowerInMW.Name = "checkBoxLaserPowerInMW";
            this.checkBoxLaserPowerInMW.Size = new System.Drawing.Size(78, 20);
            this.checkBoxLaserPowerInMW.TabIndex = 18;
            this.checkBoxLaserPowerInMW.Text = "use mW";
            this.checkBoxLaserPowerInMW.UseVisualStyleBackColor = true;
            this.checkBoxLaserPowerInMW.CheckedChanged += new System.EventHandler(this.checkBoxLaserPowerInMW_CheckedChanged);
            // 
            // Form1
            // 
            this.AcceptButton = this.buttonInitialize;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1059, 807);
            this.Controls.Add(this.splitContainerTopVsLog);
            this.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Wasatch.NET WinForm Demo";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainerTopVsLog.Panel1.ResumeLayout(false);
            this.splitContainerTopVsLog.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTopVsLog)).EndInit();
            this.splitContainerTopVsLog.ResumeLayout(false);
            this.splitContainerGraphVsControls.Panel1.ResumeLayout(false);
            this.splitContainerGraphVsControls.Panel1.PerformLayout();
            this.splitContainerGraphVsControls.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerGraphVsControls)).EndInit();
            this.splitContainerGraphVsControls.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.groupBoxSetup.ResumeLayout(false);
            this.groupBoxSetup.PerformLayout();
            this.groupBoxSpectrometers.ResumeLayout(false);
            this.groupBoxControl.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPageAcquisition.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownIntegTimeMS)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownScanAveraging)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBoxcarHalfWidth)).EndInit();
            this.tabPageLaser.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLaserPowerPerc)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLaserPowerMW)).EndInit();
            this.tabPageTEC.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDetectorSetpointDegC)).EndInit();
            this.tabPageMisc.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAcquisitionPeriodMS)).EndInit();
            this.tabPageAccessories.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.groupBoxMode.ResumeLayout(false);
            this.groupBoxMode.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.groupBoxSettings.ResumeLayout(false);
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
        private System.ComponentModel.BackgroundWorker backgroundWorkerSettings;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemTest;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemTestWriteEEPROM;
        private System.Windows.Forms.Label labelDetTempDegC;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolStripMenuItem setDFUModeToolStripMenuItem;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numericUpDownLaserPowerPerc;
        private System.Windows.Forms.NumericUpDown numericUpDownDetectorSetpointDegC;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox checkBoxExternalTriggerSource;
        private System.Windows.Forms.CheckBox checkBoxContinuousAcquisition;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numericUpDownAcquisitionPeriodMS;
        private System.Windows.Forms.Label labelSpectrumCount;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageAcquisition;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TabPage tabPageLaser;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.NumericUpDown numericUpDownLaserPowerMW;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TabPage tabPageTEC;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TabPage tabPageMisc;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox checkBoxRamanCorrection;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemEEPROMJson;
        private System.Windows.Forms.TabPage tabPageAccessories;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.CheckBox checkBoxAccessoriesEnabled;
        private System.Windows.Forms.CheckBox checkBoxLampEnabled;
        private System.Windows.Forms.CheckBox checkBoxLaserPowerInMW;
    }
}

