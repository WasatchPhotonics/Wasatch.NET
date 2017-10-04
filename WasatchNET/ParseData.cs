using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    /// <summary>
    /// A class to extract fields of various primitive types embedded at 
    /// arbitrary indexes within a byte array. Used by ModelConfig.
    /// </summary>
    /// <remarks>
    /// Differs from Unpack in that these functions all take an offset,
    /// don't have to perform a null-check and supports different types.
    /// </remarks>
    class ParseData
    {
        public static String toString(byte[] buf, int index = 0, int len = 0) 
        {
            if (len == 0)
                len = buf.Length;
            String s = "";
            for (int i = 0; i < len; i++)
            {
                int pos = index + i;
                if (buf[pos] == 0)
                    break;
                s += (char)buf[pos];
            }
            return s.TrimEnd();
        }

        public static bool toBool(byte[] buf, int index)
        {
            return buf[index] != 0;
        }

        public static float toFloat(byte[] buf, int index)
        {
            return System.BitConverter.ToSingle(buf, index);
        }

        public static short toInt16(byte[] buf, int index)
        {
            return System.BitConverter.ToInt16(buf, index);
        }

        public static ushort toUInt16(byte[] buf, int index)
        {
            return System.BitConverter.ToUInt16(buf, index);
        }

        public static int toInt32(byte[] buf, int index)
        {
            return System.BitConverter.ToInt32(buf, index);
        }

        public static uint toUInt32(byte[] buf, int index)
        {
            return System.BitConverter.ToUInt32(buf, index);
        }
    }
}