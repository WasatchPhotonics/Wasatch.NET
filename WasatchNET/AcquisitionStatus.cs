using System;
using LibUsbDotNet.Main;

namespace WasatchNET
{
    public class AcquisitionStatus
    {
        public ushort checksum;
        public ushort frame;
        public uint integTimeMS;

        // spectrum status is currently unimplemented in firmware
        public AcquisitionStatus(Spectrometer spec)
        {
            const int timeoutMS = 2;
            byte[] buf = new byte[8];

            int bytesRead = 0;
            ErrorCode err = spec.statusReader.Read(buf, timeoutMS, out bytesRead);
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