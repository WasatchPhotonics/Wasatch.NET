﻿using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace WasatchNET
{
    public class COMOCTSpectrometer : WPOCTSpectrometer
    {
        public string portName;
        SerialPort port;
        internal Dictionary<Opcodes, string> commands = OCTOpcodeHelper.getInstance().getDict();

        static string tryPort(SerialPort p)
        {
            try
            {
                byte[] buffer = new byte[100];
                int ok = p.Read(buffer, 0, 100);
                return System.Text.Encoding.UTF8.GetString(buffer);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal COMOCTSpectrometer(string portName, string ccfFile, IWPOCTCamera camera, string camID, UsbRegistry usbReg, int index = 0) : base(camera, camID, usbReg, index)
        {
            this.portName = portName;
        }

        internal override async Task<bool> openAsync()
        {
            string resp = "";

            try
            {
                port = new SerialPort(portName, 9600);
                port.DataBits = 8;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.NewLine = "\r\n";
                port.Open();

                if (port.IsOpen)
                {
                    logger.info("Port {0} open", portName);

                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                    port.DiscardOutBuffer();


                    if (!sendCOMCommand(Opcodes.GET_LINE_PERIOD, ref resp, null))
                    {
                        port.Close();
                        port.Dispose();
                        port = null;
                        return false;
                    }

                }
            }
            catch (Exception)
            {
                return false;
            }

            bool openBase = await base.openAsync();

            linePeriod = 50;
            integrationTimeUS = 45;
            eeprom.startupIntegrationTimeMS = 45;
            collectionMode = 16672;
            bool ok = sendCOMCommand(Opcodes.GET_MODEL_CONFIG, ref resp, null);
            if (ok)
            {
                eeprom.serialNumber = resp.Split('\r')[0];
                camSN = eeprom.serialNumber;
            }

            testPattern = 0;

            eeprom.featureMask.PropertyChanged += FeatureMask_PropertyChanged;
            eeprom.featureMask.invertXAxis = false;
            reverseSpectrum = false;

            return ok && openBase;
        }

        private void FeatureMask_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            reverseSpectrum = eeprom.featureMask.invertXAxis;
        }

        public async override Task closeAsync()
        {
            await base.closeAsync();
            port.Close();
            port.Dispose();
            port = null;
        }

        public override string firmwareRevision
        {
            get
            {
                string resp = "";
                bool ok = sendCOMCommand(Opcodes.GET_FIRMWARE_REVISION, ref resp, null);
                if (ok)
                    return resp.Split('\r')[0].Trim();
                else
                    return "";
            }
        }

        public bool reverseSpectrum
        {
            get
            {
                return _reverseSpectrum;
            }
            set
            {
                bool prevValue = _reverseSpectrum;
                int setValue = 0;
                if (value)
                    setValue = 1;

                string resp = "";
                bool ok = sendCOMCommand(Opcodes.SET_INVERT_X_AXIS, ref resp, new float[] { setValue }, new int[] { 0 });
                if (ok)
                {
                    ok = sendCOMCommand(Opcodes.GET_INVERT_X_AXIS, ref resp, null);

                    if (ok)
                    {
                        int rev = System.Convert.ToInt32(resp.Split('\r')[0]);
                        if (rev == 0)
                            _reverseSpectrum = false;
                        else
                            _reverseSpectrum = true;
                    }
                    else
                        _reverseSpectrum = prevValue;
                }
            }
        }
        bool _reverseSpectrum = false;

        public string camType
        {
            get
            {
                string resp = "";
                bool ok = sendCOMCommand("r mdnm", ref resp, null);
                if (ok)
                {
                    string fullCamID = resp.Split('_')[0];
                    if (fullCamID == "OCTOPUS1")
                        fullCamID = "Octoplus";

                    ok = sendCOMCommand("r idnb", ref resp, null);
                    if (ok)
                    {
                        fullCamID += " ";
                        fullCamID += resp.Split('\r')[0].Split('-').Last();
                        return fullCamID;
                    }
                }

                return "";
            }
        }

        public string camSN
        {
            get
            {
                return _camSN;
            }
            set
            {
                _camSN = value;
            }
        }
        string _camSN;

        public int collectionMode
        {
            get { return _collectionMode; }
            set
            {
                string resp = "";
                bool ok = sendCOMCommand(Opcodes.GET_COLLECTION_MODE, ref resp, null);
                if (ok)
                {
                    int mode = System.Convert.ToInt32(resp.Split('\r')[0]);
                    if (mode != value)
                    {
                        ok = sendCOMCommand(Opcodes.SET_COLLECTION_MODE, ref resp, new float[] { value }, new int[] { 0 });
                        if (ok)
                            _collectionMode = value;
                        else
                            _collectionMode = mode;
                    }

                }

                _collectionMode = value;
            }
        }
        int _collectionMode;

        public override int testPattern
        {
            get
            {
                return testPattern_;
            }
            set
            {
                int prevValue = testPattern_;
                string resp = "";
                bool ok = sendCOMCommand(Opcodes.SET_TEST_PATTERN, ref resp, new float[] { value }, new int[] { 0 });
                if (ok)
                {
                    ok = sendCOMCommand(Opcodes.GET_TEST_PATTERN, ref resp, null);

                    if (ok)
                        testPattern_ = System.Convert.ToInt32(resp.Split('\r')[0]);
                    else
                        testPattern_ = prevValue;
                }
            }
        }

        public override float linePeriod
        {
            get
            {
                return linePeriod_;
            }
            set
            {
                if (value != linePeriod_)
                {
                    float prevValue = linePeriod_;
                    if (value < integrationTimeUS)
                    {
                        linePeriod_ = value;
                        integrationTimeUS = value - 0.7f;
                    }

                    string resp = "";
                    bool ok = sendCOMCommand(Opcodes.SET_LINE_PERIOD, ref resp, new float[] { value * 100 }, new int[] { 0 });
                    if (ok)
                    {
                        ok = sendCOMCommand(Opcodes.GET_LINE_PERIOD, ref resp, null);

                        if (ok)
                            linePeriod_ = System.Convert.ToSingle(resp.Split('\r')[0]) / 100;
                        else
                            linePeriod_ = prevValue;
                    }
                }
            }
        }

        public override float integrationTimeUS
        {
            get
            {
                return integrationTimeUS_;
            }
            set
            {
                if (value != integrationTimeUS_)
                {
                    float prevValue = integrationTimeUS_;
                    if (value > linePeriod)
                    {
                        integrationTimeUS_ = value;
                        linePeriod = value + 0.7f;
                    }


                    string resp = "";
                    bool ok = sendCOMCommand(Opcodes.SET_INTEGRATION_TIME, ref resp, new float[] { value * 100 }, new int[] { 0 });
                    if (ok)
                    {
                        ok = sendCOMCommand(Opcodes.GET_INTEGRATION_TIME, ref resp, null);

                        if (ok)
                            integrationTimeUS_ = System.Convert.ToSingle(resp.Split('\r')[0]) / 100;
                        else
                            integrationTimeUS_ = prevValue;
                    }

                }

            }
        }

        public override uint integrationTimeMS
        {
            get
            {
                return (uint)integrationTimeUS_;
            }
            set
            {
                if (value != integrationTimeUS_)
                {
                    integrationTimeUS = value;
                }
            }
        }

        internal bool sendCOMCommand(Opcodes opcode, ref string response, float[] args, int[] precs = null)
        {
            if (commands.ContainsKey(opcode))
            {
                string command = commands[opcode];
                if (args != null)
                {
                    if (precs == null || precs.Length != args.Length)
                    {
                        foreach (float arg in args)
                            command += " " + arg.ToString("g");
                    }
                    else
                    {
                        for (int i = 0; i < args.Length; ++i)
                            command += " " + args[i].ToString("f" + precs[i].ToString());
                    }
                }
                command += "\r";

                port.Write(command);
                Thread.Sleep(33);
                string resp = "";
                Thread t = new Thread(() => resp = tryPort(port));
                t.Start();
                if (!t.Join(TimeSpan.FromMilliseconds(50)))
                {
                    return false;
                }
                else if (resp == null)
                {
                    return false;
                }
                else
                {
                    response = resp;
                    return resp.Contains("Ok");
                }

            }
            else
            {
                return false;
            }
        }
        internal bool sendCOMCommand(string command, ref string response, float[] args, int[] precs = null)
        {
            string commandLocal = command;
            if (args != null)
            {
                if (precs == null || precs.Length != args.Length)
                {
                    foreach (float arg in args)
                        commandLocal += " " + arg.ToString("g");
                }
                else
                {
                    for (int i = 0; i < args.Length; ++i)
                        commandLocal += " " + args[i].ToString("f" + precs[i].ToString());
                }
            }
            commandLocal += "\r";

            port.Write(commandLocal);
            Thread.Sleep(33);
            string resp = "";
            Thread t = new Thread(() => resp = tryPort(port));
            t.Start();
            if (!t.Join(TimeSpan.FromMilliseconds(50)))
            {
                return false;
            }
            else if (resp == null)
            {
                return false;
            }
            else
            {
                response = resp;
                return resp.Contains("Ok");
            }
        }
    }
}
