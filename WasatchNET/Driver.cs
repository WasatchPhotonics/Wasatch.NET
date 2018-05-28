using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace WasatchNET
{
    [ComVisible(true)]
    [Guid("E71326C8-C2BC-404F-81E4-394D95541A24")]
    [ProgId("WasatchNET.Driver")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Driver : IDriver
    {
        static Driver instance = new Driver();
        List<Spectrometer> spectrometers = new List<Spectrometer>();

        bool suppressErrors = false;

        public Logger logger { get; } = Logger.getInstance();
        public string version { get; }

        ////////////////////////////////////////////////////////////////////////
        // static methods
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get a handle to the Driver Singleton. Idempotent (can be called 
        /// repeatedly with no side-effects).
        /// </summary>
        /// <returns>reference to the Driver Singleton</returns>
        public static Driver getInstance()
        {
            return instance;
        }

        ////////////////////////////////////////////////////////////////////////
        // public methods
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Iterate over all discoverable Wasatch Photonics USB spectrometers,
        /// and return the number found. Individual spectrometers can then be
        /// accessed via the getSpectrometer(index) call.
        /// </summary>
        /// <returns>number of Wasatch Photonics USB spectrometers found</returns>
        public int openAllSpectrometers()
        {
            SortedDictionary<string, List<Spectrometer>> sorted = new SortedDictionary<string, List<Spectrometer>>();

            // This requires libusb-1.0.dll in the path, and generates a slew of errors like:
            // MonoUsbProfileHandle : ReleaseHandle #9
            // LibUsbDotNet.LudnMonoLibUsb.MonoUsbDevice[non - UsbEndpointBase]: MonoApiError: GetDescriptor Failed
            // LibUsbDotNet.LudnMonoLibUsb.MonoUsbDevice[non - UsbEndpointBase]: Win32Error: GetLangIDs
            //
            // UsbDevice.ForceLibUsbWinBack = true;  

            // This seems to be mainly for Linux
            UsbDevice.ForceLegacyLibUsb = false;

            UsbDevice.UsbErrorEvent += OnUsbError;

            UsbRegDeviceList deviceRegistries = UsbDevice.AllDevices;
            foreach (UsbRegistry usbRegistry in deviceRegistries)
            {
                String desc = String.Format("Vid:0x{0:x4} Pid:0x{1:x4} (rev:{2}) - {3}",
                    usbRegistry.Vid,
                    usbRegistry.Pid,
                    (ushort) usbRegistry.Rev,
                    usbRegistry[SPDRP.DeviceDesc]);

                if (logger.debugEnabled())
                {
                    logger.debug("USB Registry for: {0}", desc);
                    logDevice(usbRegistry);
                }

                if (usbRegistry.Vid == 0x24aa)
                {
                    Spectrometer spectrometer = new Spectrometer(usbRegistry);
                    if (spectrometer.open())
                    {
                        // sort them by model, serial (allow duplicates for unconfigured)
                        // TODO: is there any way to deterministically sort between units
                        //       without a configured unique serial number?
                        string key = String.Format("{0}-{1}", spectrometer.modelConfig.model, spectrometer.modelConfig.serialNumber);
                        if (!sorted.ContainsKey(key))
                            sorted.Add(key, new List<Spectrometer>());
                        sorted[key].Add(spectrometer);
                        logger.debug("openAllSpectrometers: found key {0} ({1})", key, desc);

                        // doesn't help
                        if (false && spectrometer.isARM())
                        {
                            logger.debug("suppressing errors on ARM");
                            suppressErrors = true;
                        }
                    }
                    else
                    {
                        logger.error("openAllSpectrometers: failed to open {0}", desc);
                    }
                }
                else
                {
                    logger.debug("openAllSpectrometers: ignored {0}", desc);
                }
            }

            // add to final list in sorted order
            spectrometers.Clear();
            foreach (KeyValuePair<string, List<Spectrometer>> pair in sorted)
            {
                foreach (Spectrometer s in pair.Value)
                {
                    spectrometers.Add(s);
                    logger.debug("openAllSpectrometers: index {0}: {1} {2}", spectrometers.Count - 1, s.model, s.serialNumber);
                }
            }

            return spectrometers.Count;
        }

        /// <summary>
        /// How many Wasatch USB spectrometers were found.
        /// </summary>
        /// <returns>number of enumerated Wasatch spectrometers</returns>
        public int getNumberOfSpectrometers()
        {
            return spectrometers.Count;
        }

        /// <summary>
        /// Obtains a reference to the specified spectrometer, performing any 
        /// prelimary "open" / instantiation steps required to leave the spectrometer
        /// fully ready for use.
        /// </summary>
        /// <param name="index">zero-indexed (should be less than the value returned by openAllSpectrometers() / getNumberOfSpectrometers())</param>
        /// <remarks>Spectrometers are deterministically ordered by (model, serialNumber) for repeatability.</remarks>
        /// <returns>a reference to the requested Spectrometer object, or null on error</returns>
        public Spectrometer getSpectrometer(int index)
        {
            if (index < spectrometers.Count)
                return spectrometers[index];
            return null;
        }

        /// <summary>
        /// Automatically called as part of application shutdown (can be called manually).
        /// </summary>
        public void closeAllSpectrometers()
        {
            lock(this)
            {
                if (spectrometers.Count > 0)
                {
                    foreach (Spectrometer spectrometer in spectrometers)
                    {
                        spectrometer.close();
                    }
                    spectrometers.Clear();
                    UsbDevice.Exit();
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // private methods
        ////////////////////////////////////////////////////////////////////////

        private Driver()
        {
            version = String.Format("Wasatch.NET v{0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        ~Driver()
        {
            closeAllSpectrometers();
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
            if (suppressErrors)
                return;

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
