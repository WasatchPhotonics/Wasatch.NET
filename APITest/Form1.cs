using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace APITest
{
    public partial class Form1 : Form
    {
        ////////////////////////////////////////////////////////////////////////
        // Attributes
        ////////////////////////////////////////////////////////////////////////

        SortedDictionary<string, Command> commands;
        Command currentCommand;

        UsbDevice usbDevice;
        bool isInGaAs = false;

        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public Form1()
        {
            InitializeComponent();

            logger.setTextBox(textBoxEventLog);            
        }

        ////////////////////////////////////////////////////////////////////////
        // Callbacks
        ////////////////////////////////////////////////////////////////////////

        private void checkBoxVerbose_CheckedChanged(object sender, EventArgs e)
        {
            logger.level = checkBoxVerbose.Checked ? Logger.LogLevel.DEBUG : Logger.LogLevel.INFO;
        }

        private void buttonInitialize_Click(object sender, EventArgs e)
        {
            UsbDevice.UsbErrorEvent += OnUsbError;

            UsbRegDeviceList deviceRegistries = UsbDevice.AllDevices;
            UsbRegistry myUsbRegistry = null;
            foreach (UsbRegistry usbRegistry in deviceRegistries)
            {
                String desc = String.Format("Vid:0x{0:x4} Pid:0x{1:x4} (rev:{2}) - {3}",
                    usbRegistry.Vid,
                    usbRegistry.Pid,
                    (ushort)usbRegistry.Rev,
                    usbRegistry[SPDRP.DeviceDesc]);

                if (usbRegistry.Vid == 0x24aa)
                {
                    logger.info("found Wasatch device: {0}", desc);
                    if (myUsbRegistry == null)
                        myUsbRegistry = usbRegistry;
                }
                else
                {
                    logger.debug("ignored {0}", desc);
                }
            }

            logger.info("opening {0:x4}:{1:x4}", myUsbRegistry.Vid, myUsbRegistry.Pid);

            if (!myUsbRegistry.Open(out usbDevice))
            {
                logger.error("test: failed to open UsbRegistry");
                return;
            }

            IUsbDevice wholeUsbDevice = usbDevice as IUsbDevice;
            if (!ReferenceEquals(wholeUsbDevice, null))
            {
                logger.info("setting configuration");
                wholeUsbDevice.SetConfiguration(1);

                logger.info("claiming interface");
                wholeUsbDevice.ClaimInterface(0);
            }
            else
            {
                logger.info("WinUSB detected (not setting configuration or claiming interface)");
            }

            if (myUsbRegistry.Pid == 0x4000)
                checkBoxUseARM.Checked = true;
            if (myUsbRegistry.Pid == 0x2000)
                isInGaAs = true;

            buttonInitialize.Enabled = false;
            if (commands != null)
                enableAll();
        }

        private void buttonLoadAPI_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialogAPI.ShowDialog();
            if (result != DialogResult.OK)
                return;

            string pathname = openFileDialogAPI.FileName;
            commands = CommandFactory.loadCommands(pathname);

            foreach (KeyValuePair<string, Command> pair in commands)
                comboBoxCommand.Items.Add(pair.Key);
            comboBoxCommand.SelectedIndex = 0;

            buttonLoadAPI.Enabled = false;

            if (usbDevice != null)
                enableAll();
        }

        private void comboBoxCommand_SelectedIndexChanged(object sender, EventArgs e)
        {
            string name = comboBoxCommand.SelectedItem.ToString();
            if (!commands.ContainsKey(name))
                return;
            currentCommand = commands[name];
            resetCommandGUI();
        }

        private void textBoxParam1_TextChanged(object sender, EventArgs e)
        {
            if (currentCommand == null)
                return;

            string s = textBoxParam1.Text;

            if (currentCommand.name == "SET_INTEGRATION_TIME")
            {
                UInt32 ms = 0;
                if (s.Length > 0)
                    ms = UInt32.Parse(s);

                numericUpDownWValue.Value = (ushort)(ms % 65536);
                numericUpDownWIndex.Value = (ushort)(ms / 65536);
            }
            else
                logger.error("Param1 unimplemented for {0}", currentCommand.name);
        }

        private void buttonAddToScript_Click(object sender, EventArgs e)
        {
            if (currentCommand == null)
                return;

            UsbSetupPacket packet = createUsbSetupPacket();
            string line = stringify(packet);
            textBoxScript.AppendText(String.Format("{0}{1}", line, Environment.NewLine));
        }

        private void buttonRunNow_Click(object sender, EventArgs e)
        {
            int count = (int) numericUpDownCount.Value;
            int ms = (int) numericUpDownDelayMS.Value;
            for (int i = 0; i < count; i++)
            {
                runCmd();
                if (i + 1 < count)
                    Thread.Sleep(ms);
            }
        }

        private void buttonTestAll_Click(object sender, EventArgs e)
        {
            // start with accessors
            List<string> failures = new List<string>();
            foreach (KeyValuePair<string, Command> pair in commands)
            {
                string name = pair.Key;
                Command cmd = pair.Value;

                if (cmd.direction == Command.Direction.HOST_TO_DEVICE)
                    continue;

                if (!cmd.batchTest)
                {
                    logger.info("not BatchTest: {0}", name);
                    continue;
                }

                currentCommand = cmd;
                bool success = runCmd();

                if (!success)
                    failures.Add(name);
            }

            if (failures.Count == 0)
                logger.info("All tests passed");
            else
                logger.error("The following tests failed: {0}", String.Join(", ", failures));
        }

        ////////////////////////////////////////////////////////////////////////
        // Methods
        ////////////////////////////////////////////////////////////////////////

        void enableAll()
        {
            groupBoxCommand.Enabled = 
                groupBoxScript.Enabled =
                groupBoxParameters.Enabled =
                groupBoxProperties.Enabled = true;
        }

        bool runCmd()
        {
            if (currentCommand == null)
                return false;

            if (!isSupported())
            {
                logger.error("{0} is not supported on this spectrometer", currentCommand.name);
                return false;
            }

            UsbSetupPacket packet = createUsbSetupPacket();

            bool expectedResult = true;
            if (checkBoxUseARM.Checked && currentCommand.armInvertedReturn)
                expectedResult = false;

            string verb;

            // allocate buffer
            byte[] buf = null;
            if (currentCommand.direction == Command.Direction.DEVICE_TO_HOST)
            {
                verb = "read";
                if (packet.Length > 0)
                    buf = new byte[packet.Length];
            }
            else
            {
                verb = "wrote";
                if (currentCommand.fakeBufferLength > 0)
                    buf = new byte[currentCommand.fakeBufferLength];
                else if (currentCommand.makeFakeBufferFromValue)
                    buf = new byte[currentCommand.wValue];
                else if (packet.Length > 0)
                {
                    logger.error("run not implemented: {0}", currentCommand.name);
                    return false; // not yet setup to handle SET_MODEL_CONFIG_REAL
                }
            }
            int bufLen = buf == null ? 0 : buf.Length;

            // execute transfer
            logger.debug("running: {0} {1} with {2} bufLen", verb, stringify(packet), bufLen);
            bool ok = usbDevice.ControlTransfer(ref packet, buf, bufLen, out int bytesXfer);

            // log result
            logger.debug("{0} {1} bytes; result = {2} (expected {3})", verb, bytesXfer, ok, expectedResult);
            if (ok == expectedResult)
                logger.info("{0} passed", currentCommand.name);
            else
                logger.error("{0} failed", currentCommand.name);

            // dump received data
            if (currentCommand.direction == Command.Direction.DEVICE_TO_HOST)
            {
                // truncate received data to needed length
                if (currentCommand.length > 0 && bufLen > currentCommand.length)
                {
                    byte[] tmp = new byte[currentCommand.length];
                    Array.Copy(buf, tmp, tmp.Length);
                    buf = tmp;
                }

                // if we were returning the response back to the caller, we'd return buf
                logger.hexdump(buf, String.Format("{0} << ", currentCommand.name));
            }

            return ok == expectedResult;
        }

        UsbSetupPacket createUsbSetupPacket()
        {
            byte bRequestType = Util.requestType(currentCommand.direction);
            byte bRequest = currentCommand.opcode;
            int wValue = (int) numericUpDownWValue.Value;
            int wIndex = (int) numericUpDownWIndex.Value;

            int fakeBufferLength = currentCommand.fakeBufferLength;

            // on ARM, always send at least 8 bytes?
            // http://www.beyondlogic.org/usbnutshell/usb4.shtml
            if (checkBoxUseARM.Checked && fakeBufferLength == 0)
                fakeBufferLength = 8;

            int wLength = 0;
            if (currentCommand.makeFakeBufferFromValue)
                wLength = wValue;
            else if (fakeBufferLength > 0)
                wLength = fakeBufferLength;

            return new UsbSetupPacket(bRequestType, bRequest, wValue, wIndex, wLength);
        }

        string stringify(UsbSetupPacket packet)
        {
            return String.Format("{0}(bRequestType: 0x{1:x2}, bRequest: 0x{2:x4}, wValue: 0x{3:x4}, wIndex: 0x{4:x4}, wLength: 0x{5:x4})",
                currentCommand.name,
                packet.RequestType,
                packet.Request,
                packet.Value,
                packet.Index,
                packet.Length);
        }

        void clearCommandGUI()
        {
            textBoxParam1.Text = "";
            textBoxParam1.Enabled = false;
            comboBoxEnum.Items.Clear();
            comboBoxEnum.Enabled = false;
        }

        void resetCommandGUI()
        {
            clearCommandGUI();

            if (currentCommand == null)
                return;

            numericUpDownWValue.Value = currentCommand.wValue;
            numericUpDownWIndex.Value = currentCommand.wIndex;
            numericUpDownWValue.Enabled = !currentCommand.fixedWValue;
            numericUpDownWIndex.Enabled = !currentCommand.fixedWIndex;

            labelUnits.Text = currentCommand.units;
            labelLength.Text = currentCommand.length.ToString();
            labelOpcode.Text = String.Format("0x{0:x2}", currentCommand.opcode);
            labelReverse.Text = currentCommand.reverse.ToString();
            labelDataType.Text = currentCommand.dataType.ToString();
            labelDirection.Text = currentCommand.direction.ToString();
            labelFakeBufLen.Text = currentCommand.fakeBufferLength.ToString();
            labelMakeFakeBuf.Text = currentCommand.makeFakeBufferFromValue.ToString();
            labelReadEndpoint.Text = currentCommand.readEndpoint.ToString();

            // ARM-specific
            if (checkBoxUseARM.Checked)
            {
                labelReadback.Text = currentCommand.readBackARM.ToString();
                labelExpectedResult.Text = (!currentCommand.armInvertedReturn).ToString();
            }
            else
            {
                labelReadback.Text = currentCommand.readBack.ToString();
                labelExpectedResult.Text = true.ToString();
            }

            labelSupported.Text = isSupported().ToString();

            // enums
            if (currentCommand.enumValues != null)
            {
                foreach (string s in currentCommand.enumValues)
                    comboBoxEnum.Items.Add(s);
                comboBoxEnum.SelectedIndex = 0;
                comboBoxEnum.Enabled = true;
            }
            else if (currentCommand.dataType == Command.DataType.BOOL)
            {
                comboBoxEnum.Items.Add("false");
                comboBoxEnum.Items.Add("true");
                comboBoxEnum.SelectedIndex = 0;
                comboBoxEnum.Enabled = true;
            }

            // convenience params
            if (currentCommand.name == "SET_INTEGRATION_TIME")
            {
                textBoxParam1.Enabled = true;
            }

            // notes
            textBoxNotes.Text = currentCommand.notes;
        }

        bool isSupported()
        {
            if (currentCommand.supportsBoards == null || currentCommand.supportsBoards.Count == 0)
                return true; // command supported on all platforms

            if (checkBoxUseARM.Checked && currentCommand.supportsBoards.Contains("ARM"))
                return true; 

            if (!checkBoxUseARM.Checked && currentCommand.supportsBoards.Contains("FX2"))
                return true;

            if (isInGaAs && currentCommand.supportsBoards.Contains("InGaAs"))
                return true;

            if (!isInGaAs && currentCommand.supportsBoards.Contains("Silicon"))
                return true;

            return false;
        }

        void OnUsbError(object sender, UsbError e)
        {
            string prefix = String.Format("OnUsbError: sender {0}", sender.GetType());
            if (sender is UsbEndpointBase)
            {
                logger.error("{0} [UsbEndPointBase]: Win32ErrorNumber {1} ({2}): {3}",
                    prefix, e.Win32ErrorNumber, e.Win32ErrorString, e.Description);

                // Magic number 31 came from here: http://libusbdotnet.sourceforge.net/V2/html/718df290-f19a-9033-3a26-1e8917adf15d.htm
                if (e.Win32ErrorNumber == 31)
                {
                    UsbDevice usb = sender as UsbDevice;
                    if (usb.IsOpen)
                    {
                        UsbEndpointBase baseDevice = sender as UsbEndpointBase;
                        // UsbEndpointInfo uei = baseDevice.EndpointInfo;
                        // LibUsbDotNet.Descriptors.UsbEndpointDescriptor ued = uei.Descriptor;
                        logger.error("{0} [UsbEndPointBase]: usb device still open on endpoint {1}", prefix, baseDevice.EpNum);

                        if (baseDevice.Reset())
                        {
                            // docs say to set e.Handled = true here, but UsbError has no such field;
                            // was commented out here:
                            // https://github.com/GeorgeHahn/LibUsbDotNet/blob/master/LibWinUsb/UsbDevice.Error.cs#L49
                            return;
                        }
                    }
                }
            }
            else if (sender is UsbTransfer)
            {
                logger.error("{0} [UsbTransfer]: Win32ErrorNumber {1} ({2}): {3}",
                    prefix, e.Win32ErrorNumber, e.Win32ErrorString, e.Description);
                UsbEndpointBase ep = ((UsbTransfer)sender).EndpointBase;
                logger.error("{0} [UsbTransfer]: Endpoint = 0x{1:x2}", prefix, ep.EpNum);
            }
            else if (sender is Type)
            {
                Type t = sender as Type;
                logger.error("{0} [Type]: type = {1}", prefix, t.Name);
            }
            else
            {
                logger.error("{0} [other]: {1}", prefix, e.ToString());
                // is there anything we should DO here, other than logging it?
            }
        }
    }
}
