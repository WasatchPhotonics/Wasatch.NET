using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using static IWPOCTCamera;
#if WIN32
#warning Building 32-bit Andor
using ATMCD32CS;
#elif x64
#warning Building 64-bit Andor
using ATMCD64CS;
#else
#warning Andor not supported
#endif

namespace WasatchNET
{
    [ComVisible(true)]
    [Guid("E71326C8-C2BC-404F-81E4-394D95541A24")]
    [ProgId("WasatchNET.Driver")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Driver : IDriver
    {
        const int MAX_RESETS = 3;

        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        static bool CheckLibrary(string fileName)
        {
            return LoadLibrary(fileName) != IntPtr.Zero;
        }

        static Driver instance = new Driver();
        List<Spectrometer> spectrometers = new List<Spectrometer>();

        bool opened = false;
        bool suppressErrors = false;
        int resetCount = 0;

        public Logger logger { get; } = Logger.getInstance();
        public string version { get; }

        // This is a Driver-level (singleton) object which tracks whether any
        // given spectrometer is believed to be in an "error state" (requiring
        // re-initialization) or not.  It is passed into each Spectrometer so 
        // they can update it with their state over time, but there is only one 
        // instance in the process.
        SpectrometerUptime uptime = new SpectrometerUptime();

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
        /// <remarks>
        /// If called multiple times during an application session WITHOUT calling
        /// closeAllSpectrometers() in between, will only add/open "new" 
        /// spectrometers which have been "hotplugged" since the previous call.
        /// 
        /// It is unclear how reliable this method will be on subsequent calls
        /// if a previously-opened spectrometer has been power-cycled or otherwise
        /// reset, but not formally "closed" via Spectrometer.close() or 
        /// Driver.closeAllSpectrometers().
        /// </remarks>
        /// <returns>number of Wasatch Photonics USB spectrometers found</returns>
        /// 
        public int openAllSpectrometers()
        {
            Task<int> task = Task.Run(async () => await openAllSpectrometersAsync());
            return task.Result;
        }
        public async Task<int> openAllSpectrometersAsync()
        {
            logger.header("openAllSpectrometers: start");

            logger.info("Wasatch.NET v{0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

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

            ////////////////////////////////////////////////////////////////////
            // Add Wasatch Photonics USB spectrometers
            ////////////////////////////////////////////////////////////////////

            // @todo move away from UsbRegistry to prep for LibUsbDotNet 3.x

            UsbRegDeviceList deviceRegistries = UsbDevice.AllDevices;

            foreach (UsbRegistry usbRegistry in deviceRegistries)
            {
                String desc = String.Format("Vid:0x{0:x4} Pid:0x{1:x4} (rev:{2}) - {3}",
                    usbRegistry.Vid,
                    usbRegistry.Pid,
                    (ushort) usbRegistry.Rev,
                    usbRegistry[SPDRP.DeviceDesc]);

                // Generate a "unique key" for the spectrometer based on its 
                // "physical" USB properties (no EEPROM fields involved).  This 
                // is being explored as a way to better recognize individual
                // spectrometers within a set, where one spectrometer has been
                // power-cycled or re-enumerated, but the others have not (and
                // the caller doesn't necessarily know which, so a repeat call
                // to "openAllSpectrometers" is intended to "get the new one(s)
                // running, without disrupting the old one(s)".
                logger.debug("USB Registry for: {0}", desc);
                logDevice(usbRegistry);

                if (usbRegistry.Vid == 0x24aa && usbRegistry.Pid == 0x5000)
                {
                    HOCTSpectrometer spectrometer = new HOCTSpectrometer(usbRegistry);
                    if (await spectrometer.openAsync())
                    {
                        string key = String.Format("{0}-{1}", "HOCT", "0000");
                        if (!sorted.ContainsKey(key))
                            sorted.Add(key, new List<Spectrometer>());
                        sorted[key].Add(spectrometer);
                    }
                }
                else if (usbRegistry.Vid == 0x24aa)
                {
                    Spectrometer spectrometer = new Spectrometer(usbRegistry) { uptime = uptime };
                    if (await spectrometer.openAsync())
                    {
                        // sort them by model, serial (allow duplicates for unconfigured)
                        string key = String.Format("{0}-{1}", spectrometer.eeprom.model, spectrometer.eeprom.serialNumber);
                        if (!sorted.ContainsKey(key))
                            sorted.Add(key, new List<Spectrometer>());
                        sorted[key].Add(spectrometer);
                        logger.debug("openAllSpectrometers: found key {0} ({1})", key, desc);
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
            logger.debug("openAllSpectrometers: sorting indices for deterministic behavior");
            foreach (KeyValuePair<string, List<Spectrometer>> pair in sorted)
            {
                foreach (Spectrometer s in pair.Value)
                {
                    spectrometers.Add(s);
                    logger.debug("  index {0}: {1} {2}", spectrometers.Count - 1, s.model, s.serialNumber);
                }
            }

            ////////////////////////////////////////////////////////////////////
            // Add Wasatch Photonics SPI spectrometers
            ////////////////////////////////////////////////////////////////////

            if (Environment.GetEnvironmentVariable("WASATCHNET_USE_SPI") != null)
            {
                logger.debug("Checking for SPI spectrometers");

                string currentDir = Directory.GetCurrentDirectory(); // pushdir
                logger.debug("caching directory {0}", currentDir);
                try
                {
                    // to load FTD2XX.dll, we apparently need to be in its directory
                    string dllDir = Path.Combine(new string[] {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),  // "Program Files" or "(ibid) x86" as appropriate
                    "Wasatch Photonics",
                    "Wasatch.NET" });
                    logger.debug("changing directory to {0}", dllDir);
                    Directory.SetCurrentDirectory(dllDir);
                    logger.debug("directory now {0}", Directory.GetCurrentDirectory());

                    SPISpectrometer spiSpec = new SPISpectrometer(null);
                    logger.debug("attempting to open spi spectrometer");
                    bool opened = await spiSpec.openAsync();
                    if (opened)
                    {
                        logger.debug("found SPISpectrometer");
                        spectrometers.Add(spiSpec);
                    }
                    else
                    {
                        logger.debug("no SPISpectrometer found");
                    }
                }
                catch
                {
                    logger.debug("Unable to check for SPISpectrometer");
                }
                finally
                {
                    logger.debug("restoring directory {0}", currentDir);
                    Directory.SetCurrentDirectory(currentDir); // popdir
                }
                
            }

            ////////////////////////////////////////////////////////////////////
            // Add 3rd-party USB spectrometers (e.g. Ocean Optics, etc)
            ////////////////////////////////////////////////////////////////////

            if (Environment.GetEnvironmentVariable("WASATCHNET_USE_SEABREEZE") != null)
            {
                logger.debug("Checking for SeaBreeze and Ocean Optics spectrometers");

                // Add 3rd-party USB spectrometers (e.g. Ocean Optics, etc)
                bool sbPresent = CheckLibrary("SeaBreeze");
                logger.debug("SeaBreeze {0} installed", sbPresent ? "appears" : "not");

                if (deviceRegistries.Count > 0 && Environment.Is64BitProcess && sbPresent)
                {
                    UsbRegistry usbRegistry2 = deviceRegistries[0];
                    String desc2 = String.Format("Vid:0x{0:x4} Pid:0x{1:x4} (rev:{2}) - {3}",
                        usbRegistry2.Vid,
                        usbRegistry2.Pid,
                        (ushort)usbRegistry2.Rev,
                        usbRegistry2[SPDRP.DeviceDesc]);

                    int boulderIndex = 0;
                    try
                    {
                        BoulderSpectrometer boulderSpectrometer = new BoulderSpectrometer(usbRegistry2, boulderIndex);

                        while (await boulderSpectrometer.openAsync())
                        {
                            boulderSpectrometer.detectorTECSetpointDegC = 15.0f;
                            spectrometers.Add(boulderSpectrometer);
                            ++boulderIndex;
                            boulderSpectrometer = new BoulderSpectrometer(usbRegistry2, boulderIndex);
                        }

                        if (boulderIndex == 0)
                        {
                            logger.debug("openAllSpectrometers: failed to open {0}", desc2);
                        }
                    }
                    catch (DllNotFoundException)
                    {
                        logger.debug("SeaBreeze does not appear to be installed, not trying to open relevant Spectrometers");
                    }
                }
            }

#if WIN32
            AndorSDK andorDriver = new ATMCD32CS.AndorSDK();
#elif x64
            AndorSDK andorDriver = new ATMCD64CS.AndorSDK();
#endif
#if WIN32 || x64
            int numAndorAvailable = 0;
            andorDriver.GetAvailableCameras(ref numAndorAvailable);
            logger.info("Found {0} Andor cameras", numAndorAvailable);
            if (numAndorAvailable > 0)
            {
                for (int i = 0; i < numAndorAvailable; ++i)
                {
                    logger.info("Attempting to open Andor camera {0}", i);
                    AndorSpectrometer spec = new AndorSpectrometer(null, i);
                    if (await spec.openAsync())
                        spectrometers.Add(spec);
                }
            }
#endif

            if (Environment.GetEnvironmentVariable("WASATCHNET_USE_WPOCT") != null)
            {
#if x64
                try
                {
                    IWPOCTCamera.CameraType cameraType = IWPOCTCamera.CameraType.USB3;
                    IWPOCTCamera camera = IWPOCTCamera.GetOCTCamera(cameraType);
                    camera.InitializeLibrary();
                    if (camera.IsInitialized())
                    {
                        int numCameras = camera.GetNumCameras();
                        if (numCameras > 0)
                        {
                            for (int i = 0; i < numCameras; i++)
                            {
                                string camID = camera.GetCameraID(i);
                                bool ok = camera.Open(camID);
                                if (ok)
                                {
                                    WPOCTSpectrometer spec = new WPOCTSpectrometer(camera, camID, null, 0);
                                    if (await spec.openAsync())
                                        spectrometers.Add(spec);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    logger.info("WPOCT Drivers are missing so OCT systems will not be found");
                }

                try
                {
                    string[] ports = SerialPort.GetPortNames();
                    foreach (string portName in ports)
                    {
                        IWPOCTCamera.CameraType cameraType = IWPOCTCamera.CameraType.CameraLink;
                        IWPOCTCamera camera = IWPOCTCamera.GetOCTCamera(cameraType);
                        camera.InitializeLibrary();
                        if (camera.IsInitialized())
                        {
                            camera.SetCameraFileName("./W_Cobra-S_2Tap_IntTrigger_MX4_1.ccf");
                            int numCameras = camera.GetNumCameras();
                            if (numCameras > 0)
                            {
                                for (int i = 0; i < numCameras; i++)
                                {
                                    string camID = camera.GetCameraID(i);
                                    bool ok = camera.Open(camID);
                                    if (ok)
                                    {
                                        COMOCTSpectrometer spec = new COMOCTSpectrometer(portName, null, camera, camID, null);
                                        if (await spec.openAsync())
                                            spectrometers.Add(spec);
                                    }
                                }
                            }
                        }
                    }
                    
                }
                catch (Exception)
                {
                    logger.info("WPOCT Drivers are missing so OCT systems will not be found");
                }
#endif
            }


            logger.debug($"openAllSpectrometers: returning {spectrometers.Count}");

            opened = true;
            return spectrometers.Count;
        }

        public int openMockSpectrometer(uint pixels)
        {
            MockSpectrometer mockSpectrometer = new MockSpectrometer(null);
            if (mockSpectrometer.open(pixels))
                spectrometers.Add(mockSpectrometer);

            return spectrometers.Count;
        }

        public async Task<int> openMockSpectrometerAsync(uint pixels)
        {
            MockSpectrometer mockSpectrometer = new MockSpectrometer(null);
            if(await mockSpectrometer.openAsync(pixels))
                spectrometers.Add(mockSpectrometer);

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
            Task task = Task.Run(async () => await closeAllSpectrometersAsync());
            task.Wait();
        }
        public async Task closeAllSpectrometersAsync()
        {
            if (!opened)
            {
                logger.debug("closeAllSpectrometers: not opened");
                return;
            }

            logger.debug("closeAllSpectrometers: start");

            if (spectrometers.Count > 0)
            {
                foreach (Spectrometer spectrometer in spectrometers)
                {
                    logger.debug("closeAllSpectrometers: closing spectrometer");
                    await spectrometer.closeAsync();
                }
                spectrometers.Clear();

            }
            else
                logger.debug("closeAllSpectrometers: no spectrometers to close");

            logger.debug("closeAllSpectrometers: unregistering error handler");
            UsbDevice.UsbErrorEvent -= OnUsbError;

            logger.debug("closeAllSpectrometers: exiting UsbDevice");
            UsbDevice.Exit();
            

            opened = false;
            logger.debug("closeAllSpectrometers: done");
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
            logger.close();
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

            // SiG-VIS doesn't seem to like the advanced logging?
            // if (usbRegistry.Pid == 0x4000)
            // {
            //     device.Close();
            //     return;
            // }

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
            }
            device.Close();

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

                if (value is string[])
                    logger.debug("  {0}: {1}", key, string.Format("[ {0} ]", string.Join(", ", value as string[])));
                else if (value is string)
                    logger.debug("  {0}: {1}", key, value as string);
            }
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

        void attemptReset(object sender)
        {
            UsbDevice usb = sender as UsbDevice;
            if (usb != null && usb.IsOpen)
            {
                if (resetCount > MAX_RESETS)
                {
                    logger.error("exceeded max resets, closing device");
                    usb.Close();
                    return;
                }

                UsbEndpointBase baseDevice = sender as UsbEndpointBase;
                if (baseDevice != null)
                {
                    // UsbEndpointInfo uei = baseDevice.EndpointInfo;
                    // LibUsbDotNet.Descriptors.UsbEndpointDescriptor ued = uei.Descriptor;
                    logger.error($"[UsbEndPointBase]: usb device still open on endpoint 0x{baseDevice.EpNum}, so resetting endpoint");
                    baseDevice.Reset();
                    resetCount++;
                }
            }
            else
            {
                // probably need to reconnect, per https://stackoverflow.com/questions/20822869/libusbdotnet-strange-errors-after-working-with-usb-device-for-awhile
                // except the reconnect recommended by the above SO link is basically
                // what we already have in Spectrometer.reconnect(), so maybe we want
                // to continue to "silently ignore" here and handle this from
                // Spectrometer.getSpectrum()?
                // Perhaps this would be a good place to throw a custom exception
                // that could be caught in getSpectrum, which could then trigger reconnect()?
                logger.error("YOU ARE HERE -- add disconnect / reconnect logic?");

                UsbEndpointReader reader = sender as UsbEndpointReader;
                if (reader != null)
                {
                    logger.error($"[UsbEndpointReader]: hit error at endpoint 0x{reader.EpNum:x2}");
                    //reader.Reset();
                    //reader.Flush();
                    return;
                }
            }
        }

        void OnUsbError(object sender, UsbError e)
        {
            if (suppressErrors)
                return;

            string prefix = String.Format("WasatchNET.Driver.OnUsbError (sender {0}):", sender.GetType());
            if (sender is UsbEndpointBase)
            {
                logger.error("{0} [UsbEndPointBase]: Win32ErrorNumber {1} ({2}): {3}",
                    prefix, e.Win32ErrorNumber, e.Win32ErrorString, e.Description);

                if (e.Win32ErrorNumber == 31)
                {
                    // Magic number 31 came from here: http://libusbdotnet.sourceforge.net/V2/html/718df290-f19a-9033-3a26-1e8917adf15d.htm
                    // (actually arises with ARM devices in testing)
                    // per https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
                    // 31 == ERROR_GEN_FAILURE (0x1f) "A device attached to the system is not functioning."
                    logger.error("[Driver] A device attached to the system is not functioning");
                    attemptReset(sender);
                }
                else if (e.Win32ErrorNumber == 22)
                {
                    logger.error("[Driver] The device does not recognize the command");
                    attemptReset(sender);
                }
                else
                {
                    // silently ignore errors other than Win32Error 31
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

                // reduce log level on these, as they seem to flood during 
                // recovery from errors logged elsewhere:
                if (sender.GetType().ToString() == "System.RuntimeType" && t.Name == "LibUsbDriverIO")
                    logger.debug("{0} [Type]: type = {1}", prefix, t.Name);
                else
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

        public MultiChannelWrapper getMultiChannelWrapper()
        {
            return MultiChannelWrapper.getInstance();
        }
    }
}
