﻿using LibUsbDotNet.Main;
using MPSSELight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WasatchNET
{
    public class TCPSpectrometer : Spectrometer
    {
        IPAddress address;
        string ip;
        int port;
        IPEndPoint ep;
        TcpClient client;
        NetworkStream stream;

        internal TCPSpectrometer(UsbRegistry usbReg, string address, int port, int index = 0) : base(usbReg)
        {
            this.address = IPAddress.Parse(address);
            this.ip = address;
            this.port = port;
            ep = new IPEndPoint(this.address, port);
            featureIdentification = new FeatureIdentification(0, 0);
        }

        override internal bool open()
        {
            eeprom = new TCPEEPROM(this);

            if (!eeprom.read())
            {
                logger.info("Spectrometer: failed to GET_MODEL_CONFIG");
                close();
                return false;
            }

            regenerateWavelengths();

            logger.debug("Successfully connected to TCP Spectrometer");

            return true;

        }

        override internal async Task<bool> openAsync()
        {
            try
            {
                client = new TcpClient(ip, port);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                logger.info("TCP/IP socket connect failed with issue {0}", ex.Message);
                return false;
            }

            if (!checkSocket())
                return false;

            setBinaryMode();

            eeprom = new TCPEEPROM(this);
            if (!eeprom.read())
            {
                logger.info("Spectrometer: failed to GET_MODEL_CONFIG");
                close();
                return false;
            }

            //rawWriteSeq();
            //rawWriteRan();

            pixels = (uint)getPixels();
            regenerateWavelengths();

            logger.debug("Successfully connected to TCP Spectrometer");

            return true;
        }

        bool checkSocket()
        {
            /*
            bool part1 = client.Poll(10000, SelectMode.SelectRead);
            bool part2 = (client.Available == 0);

            if (part1 && part2)
                return false;
            else
                return true;
            */
            return true;
        }

        bool setBinaryMode()
        {
            byte[] data = readData(3);
            if (data == null || (char)data[0] != 'O' || (char)data[1] != 'K' || (char)data[2] != '\n')
            {
                return false;
            }
            sendString("BIN\n");
            if (readData(1)[0] == 0)
                return true;
            else
                return false;
        }

        byte[] rawWriteSeq()
        {
            byte[] serialized = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
            stream.Write(serialized, 0, serialized.Length);
            return null;
        }
        
        byte[] rawWriteRan()
        {
            byte[] serialized = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            stream.Write(serialized, 0, serialized.Length);
            return null;
        }

        byte[] getCommand(byte bRequest, int len, int wValue = 0, int wIndex = 0, int fullLen = 0)
        {
            int bytesToRead = Math.Max(len, fullLen);
            byte[] buf = new byte[bytesToRead];
            TCPMessagePacket packet = new TCPMessagePacket(bRequest, (ushort)wValue, (ushort)wIndex, null);
            byte[] serialized = packet.serialize();

            stream.Write(serialized, 0, serialized.Length);
            byte[] data = readData(bytesToRead);
            return data;
        }

        byte[] sendCommand(byte bRequest, int wValue = 0, int wIndex = 0, byte[] payload = null, int? readBack = null)
        {
            TCPMessagePacket packet = new TCPMessagePacket(bRequest, (ushort)wValue, (ushort)wIndex, payload);
            byte[] serialized = packet.serialize();

            stream.Write(serialized, 0, serialized.Length);

            if (readBack != null)
            {
                byte[] data = readData(readBack.Value);
                return data;
            }
            else
            {
                byte[] data = readData(1);
                return data;
            }
        }

        byte[] readData(int length)
        {
            if (length == 0)
                return null;

            byte[] response = new byte[length];
            int read = stream.Read(response, 0, length); //client.Receive(response, length, SocketFlags.None);

            if (read == length)
                return response;
            else
                return null;
        }

        void sendString(string message)
        {
            int length = message.Length;
            List<byte> data = new List<byte>();
            foreach (char b in message)
            {
                data.Add((byte)b);
            }
            //data.Add(0);

            stream.Write(data.ToArray(), 0, data.Count);

        }

        public override double[] getSpectrum(bool forceNew = false)
        {
            double[] sum = getSpectrumRaw();
            if (sum == null)
            {
                logger.error("getSpectrum: getSpectrumRaw returned null");
                return null;
            }
            logger.debug("getSpectrum: received {0} pixels", sum.Length);

            if (scanAveraging_ > 1)
            {
                // logger.debug("getSpectrum: getting additional spectra for averaging");
                for (uint i = 1; i < scanAveraging_; i++)
                {
                    double[] tmp = getSpectrumRaw();
                    if (tmp == null)
                        return null;

                    for (int px = 0; px < pixels; px++)
                        sum[px] += tmp[px];
                }

                for (int px = 0; px < pixels; px++)
                    sum[px] /= scanAveraging_;
            }

            if (dark != null && dark.Length == sum.Length)
                for (int px = 0; px < pixels; px++)
                    sum[px] -= dark_[px];

            if (boxcarHalfWidth > 0)
            {
                // logger.debug("getSpectrum: returning boxcar");
                return Util.applyBoxcar(boxcarHalfWidth, sum);
            }
            else
            {
                // logger.debug("getSpectrum: returning sum");
                return sum;
            }
        }
        public override async Task<double[]> getSpectrumAsync(bool forceNew = false)
        {
            double[] sum = await getSpectrumRawAsync();
            if (sum == null)
            {
                logger.error("getSpectrum: getSpectrumRaw returned null");
                return null;
            }
            logger.debug("getSpectrum: received {0} pixels", sum.Length);

            if (scanAveraging_ > 1)
            {
                // logger.debug("getSpectrum: getting additional spectra for averaging");
                for (uint i = 1; i < scanAveraging_; i++)
                {
                    double[] tmp = await getSpectrumRawAsync();
                    if (tmp == null)
                        return null;

                    for (int px = 0; px < pixels; px++)
                        sum[px] += tmp[px];
                }

                for (int px = 0; px < pixels; px++)
                    sum[px] /= scanAveraging_;
            }

            if (dark != null && dark.Length == sum.Length)
                for (int px = 0; px < pixels; px++)
                    sum[px] -= dark_[px];

            if (boxcarHalfWidth > 0)
            {
                // logger.debug("getSpectrum: returning boxcar");
                return Util.applyBoxcar(boxcarHalfWidth, sum);
            }
            else
            {
                // logger.debug("getSpectrum: returning sum");
                return sum;
            }
        }

        protected override double[] getSpectrumRaw(bool skipTrigger = false)
        {
            double[] spec = new double[pixels];

            sendCommand(0xad);

            byte[] data = readData((int)pixels * 2);
            for (int px = 0; px < pixels; px++)
            {
                int intensity = data[px * 2] | data[px * 2 + 1] << 8;
                spec[px] = intensity;
            }

            if (eeprom.featureMask.invertXAxis)
                Array.Reverse(spec);

            return spec;

        }
        protected override async Task<double[]> getSpectrumRawAsync(bool skipTrigger = false)
        {
            logger.debug("requesting spectrum");
            double[] spec = new double[pixels];

            sendCommand(0xad);

            byte[] data = readData((int)pixels * 2);
            for (int px = 0; px < pixels; px++)
            {
                int intensity = data[px * 2] | data[px * 2 + 1] << 8;
                spec[px] = intensity;
            }

            if (eeprom.featureMask.invertXAxis)
                Array.Reverse(spec);

            return spec;

        }

        public int getPixels()
        {
            byte[] resp = getCommand(0x03, 2);
            ushort pix = Unpack.toUshort(resp);
            logger.info("TCP spec has {0} pixels", pix);
            return pix;
        }

        public int getHeight()
        {
            byte[] resp = getCommand(0xff, 2, 0xaa10);
            ushort pix = Unpack.toUshort(resp);
            logger.info("TCP spec has {0} vertical pixels", pix);
            return pix;
        }

        public string getSerialNumber()
        {
            byte[] resp = getCommand(0xff, 16, 0xaa09);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in resp)
            {
                if (b == 0)
                    break;
                else 
                    sb.Append((char)b);
            }

            logger.info("TCP spec S/N: {0}", sb.ToString());
            return sb.ToString();
        }

        public string getModelName()
        {
            byte[] resp = getCommand(0xff, 32, 0xaa0b);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in resp)
            {
                if (b == 0)
                    break;
                else 
                    sb.Append((char)b);
            }

            logger.info("TCP spec model: {0}", sb.ToString());
            return sb.ToString();
        }

        public float getWavecalCoeff(int exponent)
        {
            byte[] resp = getCommand(0xff, 4, 0xaa0d, exponent);
            float coeff = System.BitConverter.ToSingle(resp, 0);
            logger.info("TCP spec wavecal[{0}]: {1:f6}", exponent, coeff);
            return coeff;
        }
        public float getExcitation()
        {
            byte[] resp = getCommand(0xff, 4, 0xaa12);
            float excitation = System.BitConverter.ToSingle(resp, 0);
            logger.info("TCP spec excitation {0:f4}", excitation);
            return excitation;
        }

        public override bool highGainModeEnabled
        {
            get { return false; }
            set { return; }
        }

        public override bool laserEnabled
        {
            get { return false; }
            set { laserEnabled_ = value; }
        }

        public override string serialNumber
        {
            get => eeprom.serialNumber;
        }

        public override uint boxcarHalfWidth
        {
            get { return boxcarHalfWidth_; }
            set { lock (acquisitionLock) boxcarHalfWidth_ = value; }
        }

        public override uint integrationTimeMS
        {
            get
            {
                const Opcodes op = Opcodes.GET_INTEGRATION_TIME;
                if (haveCache(op))
                    return integrationTimeMS_;

                byte[] buf = getCommand(0xbf, 3);
                if (buf is null)
                    return 0;
                readOnce.Add(op);
                return integrationTimeMS_ = Unpack.toUint(buf);
            }
            set
            {
                const Opcodes op = Opcodes.GET_INTEGRATION_TIME;
                if (haveCache(op) && value == integrationTimeMS_)
                    return;

                uint integTimeMS = (uint)value;
                UInt16 lsw = (ushort)(value & 0xffff);
                byte msb = (byte)((value >> 16) & 0xff);

                sendCommand(0xb2, lsw, msb);
                readOnce.Add(op);

                integrationTimeMS_ = integTimeMS;
            }
        }

        public override bool hasLaser
        {
            get => false;
        }

        public override bool setLaserPowerPercentage(float perc)
        {
            return false;
        }

        public override LaserPowerResolution laserPowerResolution
        {
            get
            {
                return LaserPowerResolution.LASER_POWER_RESOLUTION_MANUAL;
            }
        }

        public override bool laserInterlockEnabled { get => false; }

        public override UInt64 laserModulationPeriod { get => 0; }
        public override UInt64 laserModulationPulseWidth { get => 100; }

        public override float detectorGain
        {
            get 
            {
                const Opcodes op = Opcodes.GET_DETECTOR_GAIN;
                if (haveCache(op))
                    return detectorGain_;

                byte[] buf = getCommand(0xc5, 2);
                if (buf is null)
                    return 0;
                readOnce.Add(op);
                return detectorGain_ = FunkyFloat.toFloat(Unpack.toUshort(buf));
            }
            set
            {
                const Opcodes op = Opcodes.GET_DETECTOR_GAIN;
                if (haveCache(op) && value == detectorGain_)
                    return;

                ushort word = FunkyFloat.fromFloat(detectorGain_ = value);
                sendCommand(0xb7, word);
                readOnce.Add(op);
            }
        }

        public override float detectorGainOdd { get => 0; set { } }

        public override short detectorOffset
        {
            get
            {
                return 0;

            }
            set
            {

            }
        }

        public override short detectorOffsetOdd { get => 0; set { } }

        public override UInt16 detectorStartLine
        {
            get
            {
                const Opcodes op = Opcodes.GET_DETECTOR_START_LINE;
                if (haveCache(op))
                    return detectorStartLine_;

                byte[] buf = getCommand(0xff, 2, 0x22);
                detectorStartLine_ = Unpack.toUshort(buf);

                readOnce.Add(op);
                return detectorStartLine_;
            }
            set
            {
                const Opcodes op = Opcodes.GET_DETECTOR_START_LINE;
                if (haveCache(op) && value == detectorStartLine_)
                    return;

                sendCommand(0xff, 0x21, detectorStartLine_ = value);
                readOnce.Add(op);
            }

        }

        public override UInt16 detectorStopLine
        {
            get
            {
                const Opcodes op = Opcodes.GET_DETECTOR_STOP_LINE;
                if (haveCache(op))
                    return detectorStopLine_;

                byte[] buf = getCommand(0xff, 2, 0x24);
                detectorStopLine_ = Unpack.toUshort(buf);

                readOnce.Add(op);
                return detectorStopLine_;
            }
            set
            {
                const Opcodes op = Opcodes.GET_DETECTOR_STOP_LINE;
                if (haveCache(op) && value == detectorStopLine_)
                    return;

                sendCommand(0xff, 0x23, detectorStopLine_ = value);
                readOnce.Add(op);
            }
        }

        public override bool isARM => false;
        public override bool isInGaAs => eeprom.detectorName.StartsWith("g", StringComparison.CurrentCultureIgnoreCase);
        public override TRIGGER_SOURCE triggerSource
        {
            get => TRIGGER_SOURCE.EXTERNAL;
            set
            {
                if (value != TRIGGER_SOURCE.EXTERNAL)
                    logger.error("TRIGGER_SOURCE {0} not supported in SPISpectrometer", value.ToString());
            }
        }

        public override float laserTemperatureDegC { get => 0; }

        public override ushort laserTemperatureRaw { get => 0; }

        public override bool laserTECEnabled
        {
            get
            {
                return false;
            }
            set
            {

            }
        }

        public override ushort laserTECMode
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        public override ushort laserTemperatureSetpointRaw { get => 0; }

        public override UInt16 laserWatchdogSec
        {

            get
            {
                return 0;
            }
            set
            {

            }

        }

        public override float batteryPercentage
        {
            get
            {
                return 50.0f; // TS - this is really stupid and needs to be fixed someday
            }
        }

        public override bool batteryCharging { get => false; }

        public override bool detectorTECEnabled
        {
            get
            {
                return detectorTECEnabled_;
            }
            set
            {
                detectorTECEnabled_ = value;
            }
        }

        public override float detectorTemperatureDegC
        {
            get => 0;
        }

        public override short ambientTemperatureDegC
        {
            get { return 0; }
        }

        public override ushort detectorTECSetpointRaw
        {
            get => 0;
            set
            {
                logger.error("detectorTECSetpoint not supported via SPI");
            }
        }

        public override float detectorTECSetpointDegC
        {
            get => base.detectorTECSetpointDegC;
            set
            {
                detectorTECSetpointDegC_ = value;
            }

        }

        public override ushort secondaryADC
        {
            get => 0;
        }

        public override string firmwareRevision
        {
            get
            {
                const Opcodes op = Opcodes.GET_FIRMWARE_REVISION;
                if (haveCache(op))
                    return firmwareRevision_;

                List<string> sb = new List<string>();
                byte[] resp = getCommand(0xc0, 4);

                foreach (byte b in resp)
                {
                    if (b == 0)
                        break;
                    else
                    {
                        sb.Add(((char)b).ToString());
                    }
                }

                readOnce.Add(op);
                return firmwareRevision_ = String.Join(".", sb.ToArray());
            }
        }

        public override string fpgaRevision
        {
            get
            {
                return "TCP";
            }
        }

        public override string bleRevision
        {
            get
            {
                string retval = "";

                return retval;
            }
        }

        public override float excitationWavelengthNM
        {
            get => eeprom.laserExcitationWavelengthNMFloat;
        }

    }


    public class TCPMessagePacket
    {
        byte bRequest;
        UInt16 wValue;
        UInt16 wIndex;
        byte bLength;
        byte[] payload;

        public TCPMessagePacket(byte bRequest,UInt16 wValue,UInt16 wIndex,byte[] payload, byte[] serialized = null) 
        {
            if (serialized != null)
            {
                deserialize(serialized);
            }
            else
            {
                this.bRequest = bRequest;
                this.wValue = wValue;
                this.wIndex = wIndex;
                if (payload == null)
                    bLength = 0;
                else
                {
                    this.payload = payload;
                    bLength = (byte)payload.Length;
                }
            }
        }

        public byte[] serialize()
        {
            List<byte> data = new List<byte>();
            data.Add(bRequest);
            data.Add((byte)((wValue >> 8) & 0xFF));
            data.Add((byte)((wValue) & 0xFF));
            data.Add((byte)((wIndex >> 8) & 0xFF));
            data.Add((byte)((wIndex) & 0xFF));
            data.Add(bLength);
            if (payload != null) 
                data.AddRange(payload);

            return data.ToArray();
        }

        public void deserialize(byte[] serialized)
        {
            if (serialized.Length < 6)
                return;

            bRequest = serialized[0];
            wValue = (ushort)(serialized[1] << 8 | serialized[2]);
            wIndex = (ushort)(serialized[3] << 8 | serialized[4]);
            bLength = serialized[5];

            int payloadLen = serialized.Length - 6;
            if (payloadLen == bLength)
            {
                payload = serialized.Skip(6).ToArray();
            }
        }
    }

}
