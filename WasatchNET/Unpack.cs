using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    /// <summary>
    /// A class to help unpack byte arrays returned from the spectrometer's 
    /// various getCmd() results into standard datatypes.
    /// </summary>
    /// <remarks>
    /// Differs from ParseData in that null-checks are required, and there are 
    /// no offsets.
    /// </remarks>
    class Unpack
    {
        public static bool toBool(byte[] buf)
        {
            return buf != null && buf[0] != 0;
        }

        public static byte toByte(byte[] buf)
        {
            return (byte) ((buf == null) ? 0 : buf[0]);
        }

        public static ushort toUshort(byte[] buf)
        {
            if (buf == null)
                return 0;

            if (buf.Length == 1)
                return buf[0];
            else
                return BitConverter.ToUInt16(buf, 0);
        }

        public static short toShort(byte[] buf)
        {
            if (buf == null)
                return -1;

            if (buf.Length == 1)
                return buf[0];
            else
                return BitConverter.ToInt16(buf, 0);
        }

        public static uint toUint(byte[] buf)
        {
            if (buf == null)
                return 0;

            byte[] tmp = new byte[4];
            Array.Copy(buf, tmp, Math.Min(buf.Length, tmp.Length));
            return BitConverter.ToUInt32(tmp, 0);
        }

        public static int toInt(byte[] buf)
        {
            if (buf == null)
                return -1;

            byte[] tmp = new byte[4];
            Array.Copy(buf, tmp, Math.Min(buf.Length, tmp.Length));
            return BitConverter.ToInt32(tmp, 0);
        }

        public static UInt64 toUint64(byte[] buf)
        {
            if (buf == null)
                return 0;

            byte[] tmp = new byte[8];
            Array.Copy(buf, tmp, Math.Min(buf.Length, tmp.Length));
            return BitConverter.ToUInt64(tmp, 0);
        }
    }
}
