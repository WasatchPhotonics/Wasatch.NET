using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace LibUsbDotNetTest
{
    public partial class Form1 : Form
    {
        const byte DEVICE_TO_HOST = 0xc0;
        const byte HOST_TO_DEVICE = 0x40;
        const byte SECOND_TIER_COMMAND = 0xff;
        const byte GET_MODEL_CONFIG = 0x01;
        const byte READ_COMPILATION_OPTIONS = 0x04;
        const byte PAGE_SIZE = 64;
        const int  TIMEOUT_MS = 1000;

        ////////////////////////////////////////////////////////////////////////
        // Attributes
        ////////////////////////////////////////////////////////////////////////

        bool initialized = false;
        List<UsbRegistry> wasatchRegistries = new List<UsbRegistry>();
        UsbDevice usbDevice;
        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public Form1()
        {
            InitializeComponent();
            logger.setTextBox(textBoxEventLog);
            logger.level = Logger.LogLevel.DEBUG;
        }

        ////////////////////////////////////////////////////////////////////////
        // Callbacks
        ////////////////////////////////////////////////////////////////////////

        private void buttonInit_Click(object sender, EventArgs e)
        {
            initialize();
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            test();
        }

        ////////////////////////////////////////////////////////////////////////
        // Methods
        ////////////////////////////////////////////////////////////////////////

        bool initialize()
        {
            if (initialized)
                return initialized;

            UsbDevice.UsbErrorEvent += OnUsbError;

            UsbRegDeviceList deviceRegistries = UsbDevice.AllDevices;
            foreach (UsbRegistry usbRegistry in deviceRegistries)
            {
                String desc = String.Format("Vid:0x{0:x4} Pid:0x{1:x4} (rev:{2}) - {3}",
                    usbRegistry.Vid,
                    usbRegistry.Pid,
                    (ushort) usbRegistry.Rev,
                    usbRegistry[SPDRP.DeviceDesc]);

                if (usbRegistry.Vid == 0x24aa)
                {
                    logger.info("found Wasatch device!");
                    logDevice(usbRegistry);
                    wasatchRegistries.Add(usbRegistry);
                }
                else
                {
                    logger.debug("ignored {0}", desc);
                }
            }

            initialized = true;
            buttonTest.Enabled = true;

            return true;
        }

        void test()
        {
            foreach (UsbRegistry usbRegistry in wasatchRegistries)
            {
                logger.info("testing {0:x4}:{1:x4}", usbRegistry.Vid, usbRegistry.Pid);

                if (!usbRegistry.Open(out usbDevice))
                {
                    logger.error("test: failed to open UsbRegistry");
                    continue;
                }

                IUsbDevice wholeUsbDevice = usbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    logger.debug("test: setting configuration");
                    wholeUsbDevice.SetConfiguration(1);
                    logger.debug("test: claiming interface");
                    wholeUsbDevice.ClaimInterface(0);
                }
                else
                {
                    logger.debug("Spectrometer.reconnect: WinUSB detected (not setting configuration or claiming interface)");
                }

                // spectralReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
                // statusReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep06);

                for (ushort pageID = 0; pageID < 5; pageID++)
                {
                    logger.info("reading EEPROM page {0}", pageID);
                    getCmd2(GET_MODEL_CONFIG, PAGE_SIZE, wIndex: pageID);
                }

                logger.info("reading FPGA Compilation Options");
                getCmd2(READ_COMPILATION_OPTIONS, 3);

                ushort setpoint = 0x0a45; // computed from UV-VIS degToADCCoeffs for 0 degC
                logger.info("setting detector TEC setpoint deg C");
                sendCmd(0xd8, setpoint);

                // MZ: I can't get this command to work on ARM -- skip it
                // logger.info("enabling detector TEC");
                // sendCmd(0xd6, 1);
            }

            logger.debug("test: done");
            return;
        }

        // byte[] getCmd(byte opcode, int len, ushort wIndex = 0, int fullLen = 0)
        // {
        //     int bytesToRead = fullLen == 0 ? len : fullLen;
        //     byte[] buf = new byte[bytesToRead];
        // 
        //     UsbSetupPacket setupPacket = new UsbSetupPacket(
        //         DEVICE_TO_HOST, // bRequestType
        //         opcode,         // bRequest
        //         0,              // wValue
        //         wIndex,         // wIndex
        //         bytesToRead);   // wLength
        // 
        //     logger.debug("getCmd: about to send request 0x{0:x2} with index 0x{1:x4}", opcode, wIndex);
        //     bool result = usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead);
        // 
        //     string prefix = String.Format("getCmd: 0x{0:x2} index 0x{1:x4} ->", opcode, wIndex);
        //     logger.hexdump(buf, prefix);
        // 
        //     if (fullLen == 0)
        //         return buf;
        // 
        //     // extract just the bytes we really needed
        //     byte[] tmp = new byte[len];
        //     Array.Copy(buf, tmp, len);
        //     return tmp;
        // }

        byte[] getCmd2(byte opcode, int len, ushort wIndex = 0)
        {
            byte[] buf = new byte[len];

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                DEVICE_TO_HOST, // bRequestType
                0xff,           // bRequest
                opcode,         // wValue
                wIndex,         // wIndex
                len);           // wLength

            bool result = usbDevice.ControlTransfer(ref setupPacket, buf, buf.Length, out int bytesRead);

            string prefix = String.Format("getCmd2: 0x{0:x2} index 0x{1:x4} -> ",
                opcode, wIndex);
            logger.hexdump(buf, prefix);

            return buf;
        }

        void sendCmd(byte opcode, ushort wValue = 0, ushort wIndex = 0, byte[] buf = null)
        {
            ushort wLength = (ushort)((buf == null) ? 0 : buf.Length);

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                HOST_TO_DEVICE, // bRequestType
                opcode,         // bRequest
                wValue,         // wValue
                wIndex,         // wIndex
                wLength);       // wLength

            if (buf != null)
                logger.hexdump(buf, String.Format("sendCmd(opcode 0x{0:x2}, value 0x{1:x4}, index 0x{2:x4}, len {3}): ", opcode, wValue, wIndex, wLength));
            bool result = usbDevice.ControlTransfer(ref setupPacket, buf, wLength, out int bytesWritten);

            logger.info("sendCmd: 0x{0:x2} wValue 0x{1:x4}, wIndex 0x{2:x4} -> {3}", opcode, wValue, wIndex, result);

            // no point dumping response because there can't be any -- only DEVICE_TO_HOST get a response
            // if (buf != null)
            //    logger.hexdump(buf, "sendCmd: response -> ");
            return;
        }

        void logDevice(UsbRegistry usbRegistry)
        {
            UsbDevice device;
            if (!usbRegistry.Open(out device))
                return;

            // split device.Info's string representation on linefeeds
            //   iterateDevice: Vid:0x2457 Pid:0x101E (rev:2) - Ocean Optics USB2000+ (WinUSB)
            //   iterateDevice: Vid:0x24AA Pid:0x1000 (rev:100) - Wasatch Photonics Spectrometer
            // Not all device info summaries will appear in device.Configs...not sure why
            string[] deviceInfoSummaries = device.Info.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in deviceInfoSummaries)
                logger.debug("Summary: {0}", line);

            foreach (UsbConfigInfo cfgInfo in device.Configs)
            {
                logger.debug("Config {0}", cfgInfo.Descriptor.ConfigID);

                // log UsbConfigInfo
                logNameValuePairs(cfgInfo.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

                // log UsbInterfaceInfo 
                foreach (UsbInterfaceInfo interfaceInfo in cfgInfo.InterfaceInfoList)
                {
                    logger.debug("Interface [InterfaceID {0}, AlternateID {1}]", interfaceInfo.Descriptor.InterfaceID, interfaceInfo.Descriptor.AlternateID);
                    logNameValuePairs(interfaceInfo.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

                    // log UsbEndpointInfo
                    foreach (UsbEndpointInfo endpointInfo in interfaceInfo.EndpointInfoList)
                    {
                        logger.debug("  Endpoint 0x" + (endpointInfo.Descriptor.EndpointID).ToString("x2"));
                        logNameValuePairs(endpointInfo.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), "    ");
                    }
                }

                // log SymbolicName 
                UsbSymbolicName symName = UsbSymbolicName.Parse(usbRegistry.SymbolicName);
                logger.debug("Symbolic Name:");
                logger.debug("  VID:          0x{0:x4}", symName.Vid);
                logger.debug("  PID:          0x{0:x4}", symName.Pid);
                logger.debug("  SerialNumber: {0}", symName.SerialNumber);
                logger.debug("  Class Guid:   {0}", symName.ClassGuid);

                logger.debug("Device Properties:");
                foreach (KeyValuePair<string, object> pair in usbRegistry.DeviceProperties)
                {
                    string key = pair.Key;
                    object value = pair.Value;

                    // handle array values
                    if (value is string[])
                    {
                        string[] values = value as string[];
                        logger.debug("  {0}: [ {1} ]", key, string.Join(", ", values));
                    }
                    else
                    {
                        logger.debug("  {0}: {1}", key, value);
                    }
                }

                logger.debug(" ");
            }
            device.Close();
        }

        void logNameValuePairs(string[] pairs, string prefix = "  ")
        {
            foreach (string pair in pairs)
            {
                string[] tok = pair.Split(':');
                if (tok.Length == 2)
                    logger.debug("{0}{1} = {2}", prefix, tok[0], tok[1]);
                else
                    logger.debug("{0}unparseable: {1}", prefix, pair);
            }
        }

        void OnUsbError(object sender, UsbError e)
        {
            string prefix = String.Format("Driver.OnUsbError: sender {0}", sender.GetType());
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

        //  if ((mSender is UsbEndpointBase)|| (mSender is UsbTransfer))
        //  {
        //      UsbEndpointBase ep;
        //      if (mSender is UsbTransfer)
        //          ep = ((UsbTransfer) mSender).EndpointBase;
        //      else
        //          ep = mSender as UsbEndpointBase;
        //
        //      if (ep.mEpNum != 0)
        //      {
        //          senderText = senderText+=string.Format(" Ep 0x{0:X2} ", ep.mEpNum);
        //      }
        //  }
        //  else if (mSender is Type)
        //  {
        //      Type t = mSender as Type;
        //      senderText = senderText += string.Format(" {0} ", t.Name);
        //  }
    }
}