using IDSImaging.Peak.API;
using IDSImaging.Peak.API.Core;
using IDSImaging.Peak.API.Core.Nodes;
using IDSImaging.Peak.API.Std;
using LibUsbDotNet.Main;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WasatchNET
{
    public class IDSHybridSpectrometer : Spectrometer
    {
        protected static bool isInit = false;
        private Spectrometer sidecar = null;
        private bool sidecarAvailable = false;
        private DeviceManager deviceManager = DeviceManager.Instance();
        private Device device = null;
        private NodeMap nodeMap = null;
        private string userSet = "";
        private string[] userSetOptions { get; } = new string[]{ "Default", "LongExposure" };

        internal IDSHybridSpectrometer(UsbRegistry usbReg) : base(usbReg, true) 
        {
            if (!isInit)
            {
                Library.Initialize();
                isInit = true;
            }

            sidecar = new Spectrometer(usbReg);
        }

        override internal bool open()
        {
            Task<bool> task = Task.Run(async () => await openAsync());
            return task.Result;
        }

        override internal async Task<bool> openAsync()
        {
            deviceManager.Update();
            if (!deviceManager.Devices().Any())
            {
                return false;
            }

            sidecarAvailable = await sidecar.openAsync();
            if (sidecarAvailable)
                eeprom = sidecar.eeprom;
            else
                eeprom = new EEPROM(this);

            var devices = deviceManager.Devices();
            device = devices[0].OpenDevice(DeviceAccessType.Control);
            nodeMap = device.RemoteDevice().NodeMaps()[0];

            eeprom.detectorSerialNumber = nodeMap.FindNode<StringNode>("DeviceSerialNumber").Value();
            eeprom.detectorName = nodeMap.FindNode<StringNode>("SensorName").Value();
            eeprom.activePixelsHoriz = eeprom.actualPixelsHoriz = (ushort)nodeMap.FindNode<IntegerNode>("WidthMax").Value();
            eeprom.activePixelsVert = (ushort)nodeMap.FindNode<IntegerNode>("Height").Value();
            integrationTimeMS = 15000;//(uint)(nodeMap.FindNode<FloatNode>("ExposureTime").Value() / 1000f);
            detectorStartLine = 0;
            detectorStopLine = (ushort)(eeprom.activePixelsVert - 1);
            setUserSet("Default");
            nodeMap.FindNode<FloatNode>("ExposureTime").SetValue(15000);
            return true;
        }

        void setUserSet(string value, bool force = false)
        {
            if (!userSetOptions.Contains(value))
                return;

            if (value != userSet || force)
            {
                userSet = value;

                lock (commsLock)
                {
                    nodeMap.FindNode<EnumerationNode>("UserSetSelector").SetCurrentEntry(value);
                    nodeMap.FindNode<CommandNode>("UserSetLoad").Execute();
                    nodeMap.FindNode<CommandNode>("UserSetLoad").WaitUntilDone();

                    var node = nodeMap.FindNode<FloatNode>("ExposureTime");
                    logger.debug($"set_user_set: applied UserSetSelector {value}, ExposureTime range now ({node.Minimum()}, {node.Maximum()})µs");
                    initSoftwareTriggering();
                }
            }
        }

        void initSoftwareTriggering()
        {
            nodeMap.FindNode<EnumerationNode>("TriggerSelector").SetCurrentEntry("ReadOutStart");
            nodeMap.FindNode<EnumerationNode>("TriggerMode").SetCurrentEntry("On");
            nodeMap.FindNode<EnumerationNode>("TriggerSource").SetCurrentEntry("Software");

        }

        ~IDSHybridSpectrometer()
        {
            logger.debug("entered IDS Hybrid finalizer");
            Library.Close();
        }

    }
}
