using System;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace WasatchNET
{
    /// <summary>
    /// Developmental class to support the acquisition status we plan to send on endpoint 6 following an integration.
    /// </summary>
    /// <remarks>not currently used</remarks>
    public class AcquisitionStatus
    {
        /// <summary>mathematical sum of the spectral values for all pixels, with 16-bit unsigned rollover</summary>
        public ushort checksum { get; private set; }

        /// <summary>absolute acquisition count from last spectrometer power-on</summary>
        public ushort frame { get; private set; }

        // TODO: is this MS or US?
        public uint integTimeMS { get; private set; }

        /// <summary>
        /// read and parse AcquisitionStatus from the passed endpoint
        /// </summary>
        /// <param name="reader">an instantiated USB endpoint reader (presumably EP6)</param>
        public AcquisitionStatus(UsbEndpointReader reader)
        {
            const int timeoutMS = 2;
            byte[] buf = new byte[8];

            int bytesRead = 0;
            ErrorCode err = reader.Read(buf, timeoutMS, out bytesRead);
            if (bytesRead >= 7 && err == ErrorCode.Ok)
            {
                checksum = (ushort)(buf[0] + (buf[1] << 8));
                frame = (ushort)(buf[2] + (buf[3] << 8));
                integTimeMS = (uint)(buf[4] + (buf[5] << 8) + (buf[6] << 16));
            }
            else
            {
                throw new Exception(String.Format("Could not read acquisition status; error = {0}", err));
            }
        }
    }
}