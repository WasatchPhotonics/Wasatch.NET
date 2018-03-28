using Usb = MonoLibUsb.MonoUsbApi;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using LibUsbDotNet.Main;
using MonoLibUsb;
using MonoLibUsb.Profile;
using MonoLibUsb.Transfer;
using MonoLibUsb.Descriptors;

using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Linq;

namespace MonoTest
{
    public partial class Form1 : Form
    {
        ////////////////////////////////////////////////////////////////////////
        // Attributes
        ////////////////////////////////////////////////////////////////////////

        MonoUsbSessionHandle monoUsbSession = null;
        List<MonoUsbProfile> profiles = null;
        List<MonoUsbDeviceHandle> devices = null;
        MonoUsbTransferDelegate controlTransferDelegate;

        bool initialized = false;

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

        private void buttonInit_Click(object sender, EventArgs e)
        {
            if (initialize())
            {
                buttonTest.Enabled = true;
            }
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            testComms();
        }

        ////////////////////////////////////////////////////////////////////////
        // Methods
        ////////////////////////////////////////////////////////////////////////

        bool initialize()
        {
            if (initialized)
                return initialized;

            monoUsbSession = new MonoUsbSessionHandle();
            if (monoUsbSession.IsInvalid)
            {
                logger.error("init: Failed to initialize context");
                return false;
            }
            MonoUsbApi.SetDebug(monoUsbSession, 0);

            controlTransferDelegate = ControlTransferCallback;

            findWasatchProfiles();

            initialized = true;
            return initialized;
        }

        // Predicate functions for finding only devices with the specified VendorID & ProductID.
        private static bool predicateWasatchVid(MonoUsbProfile profile)
        {
            if (profile.DeviceDescriptor.VendorID == 0x24aa) // && profile.DeviceDescriptor.ProductID == 0x0053
                return true;
            return false;
        }

        void findWasatchProfiles()
        {
            if (monoUsbSession == null || monoUsbSession.IsInvalid)
                return;

            MonoUsbProfileList profileList = new MonoUsbProfileList();

            int deviceCount = profileList.Refresh(monoUsbSession);
            if (deviceCount < 0)
            {
                logger.error("init: Failed to retrieve device list");
                return;
            }
            logger.info("{0} device(s) found.", deviceCount);

            profiles = profileList.GetList().FindAll(predicateWasatchVid);
            foreach (MonoUsbProfile profile in profiles)
            {
                logger.info("[Device] Vid:{0:X4} Pid:{1:X4}", profile.DeviceDescriptor.VendorID, profile.DeviceDescriptor.ProductID);
                logger.info("  Descriptors: {0}", profile.DeviceDescriptor);

                for (byte i = 0; i < profile.DeviceDescriptor.ConfigurationCount; i++)
                {
                    MonoUsbConfigHandle configHandle;

                    if (MonoUsbApi.GetConfigDescriptor(profile.ProfileHandle, i, out configHandle) < 0)
                        continue;

                    if (configHandle.IsInvalid)
                        continue;

                    MonoUsbConfigDescriptor configDescriptor = new MonoUsbConfigDescriptor(configHandle);
                    logger.info("  [Config] bConfigurationValue: {0}", configDescriptor.bConfigurationValue);

                    foreach (MonoUsbInterface usbInterface in configDescriptor.InterfaceList)
                    {
                        foreach (MonoUsbAltInterfaceDescriptor usbAltInterface in usbInterface.AltInterfaceList)
                        {
                            logger.info("    [Interface] bInterfaceNumber: {0}, bAlternateSetting: {1}", 
                                usbAltInterface.bInterfaceNumber, usbAltInterface.bAlternateSetting);

                            foreach (MonoUsbEndpointDescriptor endpoint in usbAltInterface.EndpointList)
                            {
                                // Write the bEndpointAddress, EndpointType, and wMaxPacketSize to console output.
                                logger.info("      [Endpoint] bEndpointAddress: 0x{0:X2}, EndpointType: {1}, wMaxPacketSize: {2}",
                                    endpoint.bEndpointAddress, (EndpointType)(endpoint.bmAttributes & 0x3), endpoint.wMaxPacketSize);
                            }
                        }
                    }
                    configHandle.Close();
                }
                // profile.Close();
            }
        }

        // Predicate functions for finding only devices with the specified VendorID & ProductID.
        static bool MyVidPidPredicate(MonoUsbProfile profile)
        {
            if (profile.DeviceDescriptor.VendorID == 0x04d8 && profile.DeviceDescriptor.ProductID == 0x0053)
                return true;
            return false;
        }

        void testComms()
        {
            if (profiles.Count < 1)
            {
                logger.info("testComms: no profiles");
                return;
            }

            foreach (MonoUsbProfile profile in profiles)
            {
                MonoUsbDeviceHandle device = null;
                try
                {
                    logger.info("Opening {0:x4}:{1:x4}", profile.DeviceDescriptor.VendorID, profile.DeviceDescriptor.ProductID);
                    device = profile.OpenDeviceHandle();
                    if (device.IsInvalid)
                    {
                        logger.error("Failed opening device handle: {0} ({1})", MonoUsbDeviceHandle.LastErrorCode, MonoUsbDeviceHandle.LastErrorString);
                        continue;
                    }

                    // re-confirm configurations for this profile
                    for (byte i = 0; i < profile.DeviceDescriptor.ConfigurationCount; i++)
                    {
                        MonoUsbConfigHandle configHandle;
                        if (MonoUsbApi.GetConfigDescriptor(profile.ProfileHandle, i, out configHandle) < 0)
                            continue;
                        if (configHandle.IsInvalid)
                            continue;
                        MonoUsbConfigDescriptor configDescriptor = new MonoUsbConfigDescriptor(configHandle);
                        logger.info("  configuration #{0}: {1} ({2})", i, configDescriptor.bConfigurationValue, configDescriptor);
                        logger.info("      type       = {0}", configDescriptor.bDescriptorType);
                        logger.info("      length     = {0}", configDescriptor.bLength);
                        logger.info("      attributes = {0:x2}", configDescriptor.bmAttributes);
                        logger.info("      interfaces = {0}", configDescriptor.bNumInterfaces);
                        logger.info("      extLength  = {0}", configDescriptor.ExtraLength);
                        logger.info("      iConfig    = {0}", configDescriptor.iConfiguration);
                        logger.info("      maxPower   = {0}", configDescriptor.MaxPower);
                        logger.info("      totalLength= {0}", configDescriptor.wTotalLength);
                    }

                    int result = 0;

                    // Set Configuration
                    // Note: http://libusb.sourceforge.net/api-1.0/caveats.html#configsel
                    MonoUsbError error = (MonoUsbError)(result = MonoUsbApi.SetConfiguration(device, 1));
                    if (false && result < 0)
                    {
                        logger.error("Failed SetConfiguration: {0} ({1}) (result = {2})", error, MonoUsbApi.StrError(error), result);
                        continue;
                    }

                    // Claim Interface
                    error = (MonoUsbError)(result = MonoUsbApi.ClaimInterface(device, 0));
                    if (result < 0)
                    {
                        logger.error("Failed ClaimInterface: {0} ({1})", error, MonoUsbApi.StrError(error));
                        continue;
                    }

                    // retain device handles
                    devices.Add(device);

                    byte DEVICE_TO_HOST = 0xc0;
                    byte SECOND_TIER_COMMAND = 0xff;
                    byte GET_MODEL_CONFIG = 0x01;
                    byte PAGE_SIZE = 64;
                    byte PAGE_ID = 0;
                    int TIMEOUT_MS = 1000;

                    // Create a vendor specific control setup, allocate 1 byte for return control data.
                    // byte requestType = (byte)(UsbCtrlFlags.Direction_In | UsbCtrlFlags.Recipient_Device | UsbCtrlFlags.RequestType_Vendor);
                    // byte request = 0x0F;
                    // MonoUsbControlSetupHandle controlSetupHandle = new MonoUsbControlSetupHandle(requestType, request, 0, 0, 1);

                    MonoUsbControlSetupHandle controlSetupHandle = new MonoUsbControlSetupHandle(DEVICE_TO_HOST, SECOND_TIER_COMMAND, GET_MODEL_CONFIG, PAGE_ID, PAGE_SIZE);

                    // Transfer the control setup packet
                    int len = libusb_control_transfer(device, controlSetupHandle, TIMEOUT_MS);
                    if (len > 0)
                    {
                        logger.info("Success");
                        byte[] ctrlDataBytes = controlSetupHandle.ControlSetup.GetData(len);
                        string ctrlDataString = Helper.HexString(ctrlDataBytes, String.Empty, "h ");
                        logger.info("Return Length: {0}", len);
                        logger.info("DATA (hex)   : [ {0} ]", ctrlDataString.Trim());
                    }
                    MonoUsbApi.ReleaseInterface(device, 0);
                }
                finally
                {
                    if (device != null)
                    {
                        logger.info("closing device");
                        device.Close();
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Utilities
        ////////////////////////////////////////////////////////////////////////

        static void ControlTransferCallback(MonoUsbTransfer transfer)
        {
            Logger logger = Logger.getInstance();
            logger.info("ControlTransferCallback: completing");

            ManualResetEvent completeEvent = GCHandle.FromIntPtr(transfer.PtrUserData).Target as ManualResetEvent;
            completeEvent.Set();
        }

        int libusb_control_transfer(MonoUsbDeviceHandle deviceHandle, MonoUsbControlSetupHandle controlSetupHandle, int timeout)
        {
            MonoUsbTransfer transfer = MonoUsbTransfer.Alloc(0);
            ManualResetEvent completeEvent = new ManualResetEvent(false);
            GCHandle gcCompleteEvent = GCHandle.Alloc(completeEvent);

            transfer.FillControl(deviceHandle, controlSetupHandle, controlTransferDelegate, GCHandle.ToIntPtr(gcCompleteEvent), timeout);

            int r = (int)transfer.Submit();
            if (r < 0)
            {
                transfer.Free();
                gcCompleteEvent.Free();
                return r;
            }

            while (!completeEvent.WaitOne(0, false))
            {
                r = MonoUsbApi.HandleEvents(monoUsbSession);
                if (r < 0)
                {
                    if (r == (int)MonoUsbError.ErrorInterrupted)
                        continue;
                    transfer.Cancel();
                    while (!completeEvent.WaitOne(0, false))
                        if (MonoUsbApi.HandleEvents(monoUsbSession) < 0)
                            break;
                    transfer.Free();
                    gcCompleteEvent.Free();
                    return r;
                }
            }

            if (transfer.Status == MonoUsbTansferStatus.TransferCompleted)
                r = transfer.ActualLength;
            else
                r = (int)MonoUsbApi.MonoLibUsbErrorFromTransferStatus(transfer.Status);

            transfer.Free();
            gcCompleteEvent.Free();
            return r;
        }
    }
}