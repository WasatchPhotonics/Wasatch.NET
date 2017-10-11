using System;

namespace WasatchNET
{
    /// <summary>
    /// A class to access fields of various primitive types embedded at 
    /// arbitrary indexes within a byte array. Used by ModelConfig.
    /// </summary>
    /// <remarks>
    /// Not happy with class name; BufferHelper? Sounds like BurgerHelper.
    /// ArrayPacker? Don't get me started.
    ///
    /// Differs from Unpack in that these functions all take an offset,
    /// don't have to perform a null-check and supports different types.
    /// Also, supports write as well as read.
    /// </remarks>
    class ParseData
    {
        ////////////////////////////////////////////////////////////////////////
        // extract (read) datatypes from a buffer
        ////////////////////////////////////////////////////////////////////////

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
            return BitConverter.ToSingle(buf, index);
        }

        public static short toInt16(byte[] buf, int index)
        {
            return BitConverter.ToInt16(buf, index);
        }

        public static ushort toUInt16(byte[] buf, int index)
        {
            return BitConverter.ToUInt16(buf, index);
        }

        public static int toInt32(byte[] buf, int index)
        {
            return BitConverter.ToInt32(buf, index);
        }

        public static uint toUInt32(byte[] buf, int index)
        {
            return BitConverter.ToUInt32(buf, index);
        }

        ////////////////////////////////////////////////////////////////////////
        // write datatypes into a buffer
        ////////////////////////////////////////////////////////////////////////

        public static bool writeFloat(float value, byte[] buf, int index)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            if (tmp == null || tmp.Length != 4)
            {
                Logger.getInstance().error("ParseData.writeFloat: can't serialize {0}", value);
                return false;
            }
            Array.Copy(tmp, 0, buf, index, tmp.Length);
            return true;
        }

        public static bool writeString(string value, byte[] buf, int index, int maxLen)
        {
            for (int i = 0; i < maxLen; i++)
            {
                int pos = index + i;
                if (i < value.Length)
                    buf[pos] = (byte) value[i];
                else 
                    buf[pos] = 0;
            }
            return true;
        }

        public static bool writeInt16(Int16 value, byte[] buf, int index)
        {
            Logger logger = Logger.getInstance();
            byte[] tmp = BitConverter.GetBytes(value);
            if (tmp == null || tmp.Length != 2)
            {
                Logger.getInstance().error("ParseData.writeInt16: can't serialize {0}", value);
                return false;
            }
            Array.Copy(tmp, 0, buf, index, tmp.Length);
            logger.debug("writeInt16: wrote {0} as 0x{1:x2} 0x{2:x2} to index {3} of {4}-byte buf",
                value, tmp[0], tmp[1], index, buf.Length);
            return true;
        }
    }
}