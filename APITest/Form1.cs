using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            buttonRunNow.Enabled = 
            buttonRunScript.Enabled = true;
            buttonInitialize.Enabled = false;
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

        UsbSetupPacket createUsbSetupPacket()
        {
            byte bRequestType = Util.requestType(currentCommand.direction);
            byte bRequest = currentCommand.opcode;
            int wValue = (int) numericUpDownWValue.Value;
            int wIndex = (int) numericUpDownWIndex.Value;

            int wLength = 0;
            if (currentCommand.fakeBufferLength > 0)
                wLength = currentCommand.fakeBufferLength;
            else if (currentCommand.makeFakeBufferFromValue)
                wLength = wValue;

            return new UsbSetupPacket(bRequestType, bRequest, wValue, wIndex, wLength);
        }

        private void buttonAddToScript_Click(object sender, EventArgs e)
        {
            if (currentCommand == null)
                return;

            UsbSetupPacket packet = createUsbSetupPacket();

            string line = String.Format("{0}(bRequestType: 0x{1}, bRequest: 0x{2:x2}, wValue: {3}, wIndex: {4}, wLength: 0x{5:x4})",
                    currentCommand.name,
                    packet.RequestType,
                    packet.Request,
                    packet.Value,
                    packet.Index,
                    packet.Length);

            textBoxScript.AppendText(String.Format("{0}{1}", line, Environment.NewLine));
        }

        ////////////////////////////////////////////////////////////////////////
        // Methods
        ////////////////////////////////////////////////////////////////////////

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

            // This doesn't handle all possible permutations
            bool supported = true;
            if (currentCommand.supportsBoards != null)
            {
                supported = false;

                // qualify ARM-vs-FX2
                if (checkBoxUseARM.Checked)
                {
                    // qualify if ARM and ARM is supported
                    if (currentCommand.supportsBoards.Contains("ARM"))
                        supported = true;
                }
                else
                {
                    // qualify if not ARM and FX2 is supported
                    if (currentCommand.supportsBoards.Contains("FX2"))
                        supported = true;
                }

                // qualify NIR-vs-silicon
                if (currentCommand.supportsBoards.Contains("InGaAs"))
                    supported = isInGaAs;
            }
            labelSupported.Text = supported.ToString();

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
