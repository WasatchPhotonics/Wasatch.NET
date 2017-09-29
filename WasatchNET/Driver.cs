using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using LibUsbDotNet.LudnMonoLibUsb;

namespace WasatchNET
{
    public sealed class Driver
    {
        static readonly Driver instance = new Driver();
        List<Spectrometer> spectrometers = new List<Spectrometer>();

        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // static methods
        ////////////////////////////////////////////////////////////////////////

        public static Driver getInstance()
        {
            return instance;
        }

        ////////////////////////////////////////////////////////////////////////
        // public methods
        ////////////////////////////////////////////////////////////////////////

        public int openAllSpectrometers()
        {
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
                        spectrometers.Add(spectrometer);
                        logger.debug("openAllSpectrometers: index {0}: {1}", spectrometers.Count - 1, desc);
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

            // TODO: sort spectrometers for deterministic behavior

            return spectrometers.Count;
        }

        public int getNumberOfSpectrometers()
        {
            return spectrometers.Count;
        }

        public Spectrometer getSpectrometer(int index)
        {
            if (index < spectrometers.Count)
                return spectrometers[index];
            return null;
        }

        public void closeAllSpectrometers()
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

        ////////////////////////////////////////////////////////////////////////
        // private methods
        ////////////////////////////////////////////////////////////////////////

        private Driver()
        {
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

        private void OnUsbError(object sender, UsbError usbError)
        {
            logger.error(usbError.ToString());
        }
    }
}
