using System;

namespace WasatchNET
{
    /// <summary>
    /// A class to access fields of various primitive types embedded at 
    /// arbitrary indexes within a byte array. Used by ModelConfig.
    /// </summary>
    /// <remarks>
    /// Differs from Unpack in that these functions all take an offset,
    /// don't have to perform a null-check and supports different types.
    /// Also, supports write as well as read.
    /// </remarks>
    public class ParseData
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

        // don't add a lot of value, do they?
        public static byte   toUInt8 (byte[] buf, int index) { return buf[index]; } 
        public static bool   toBool  (byte[] buf, int index) { return buf[index] != 0; } 
        public static float  toFloat (byte[] buf, int index) { return BitConverter.ToSingle(buf, index); } 
        public static short  toInt16 (byte[] buf, int index) { return BitConverter.ToInt16 (buf, index); } 
        public static ushort toUInt16(byte[] buf, int index) { return BitConverter.ToUInt16(buf, index); } 
        public static int    toInt32 (byte[] buf, int index) { return BitConverter.ToInt32 (buf, index); } 
        public static uint   toUInt32(byte[] buf, int index) { return BitConverter.ToUInt32(buf, index); } 

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

            Logger logger = Logger.getInstance();
            logger.debug("writeFloat: wrote {0} as 0x{1:x2}{2:x2}{3:x2}{4:x2} to index {5} of {6}-byte buf",
                value, tmp[0], tmp[1], tmp[2], tmp[3], index, buf.Length);

            return true;
        }

        public static bool writeString(string value, byte[] buf, int index, int maxLen)
        {
            if (value == null)
            {
                buf[0] = 0;
                return false;
            }
            

            for (int i = 0; i < maxLen; i++)
            {
                int pos = index + i;
                if (i < value.Length)
                    buf[pos] = (byte) value[i];
                else 
                    buf[pos] = 0;
            }

            Logger logger = Logger.getInstance();
            logger.debug("writeString: wrote up to {0} chars of {1} to index {2} of {3}-byte buf",
                maxLen, value, index, buf.Length);

            return true;
        }

        public static bool writeUInt16(UInt16 value, byte[] buf, int index)
        {
            Logger logger = Logger.getInstance();
            byte[] tmp = BitConverter.GetBytes(value);
            if (tmp == null || tmp.Length != 2)
            {
                Logger.getInstance().error("ParseData.writeUInt16: can't serialize {0}", value);
                return false;
            }
            Array.Copy(tmp, 0, buf, index, tmp.Length);
            logger.debug("writeUInt16: wrote {0} as 0x{1:x2} 0x{2:x2} to index {3} of {4}-byte buf",
                value, tmp[0], tmp[1], index, buf.Length);
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

        public static bool writeUInt32(UInt32 value, byte[] buf, int index)
        {
            Logger logger = Logger.getInstance();
            byte[] tmp = BitConverter.GetBytes(value);
            if (tmp == null || tmp.Length != 4)
            {
                Logger.getInstance().error("ParseData.writeUInt32: can't serialize {0}", value);
                return false;
            }
            Array.Copy(tmp, 0, buf, index, tmp.Length);
            logger.debug("writeUInt32: wrote {0} as 0x{1:x2} 0x{2:x2} 0x{3:x2} 0x{4:x2} to index {5} of {6}-byte buf",
                value, tmp[0], tmp[1], tmp[2], tmp[3], index, buf.Length);
            return true;
        }

        public static bool writeBool(bool value, byte[] buf, int index)
        {
            buf[index] = (byte) (value ? 1 : 0);

            Logger logger = Logger.getInstance();
            logger.debug("writeBool: wrote {0} as 0x{1:x2} to index {2} of {3}-byte buf",
                value, buf[index], index, buf.Length);

            return true;
        }

        public static bool writeByte(byte value, byte[] buf, int index)
        {
            buf[index] = value;

            Logger logger = Logger.getInstance();
            logger.debug("writeByte: wrote {0} as 0x{1:x2} to index {2} of {3}-byte buf",
                value, buf[index], index, buf.Length);

            return true;
        }

        /// <summary>
        /// Return the UInt16 with the same internal bit-structure as a given Int16.
        /// </summary>
        /// <remarks>
        /// Spectrometer.sendCmd() takes a ushort argument, because most spectrometer commands
        /// pass ushort values.  However, setting detector offset is a signed short.  I think 
        /// the easiest solution (rather than overloading sendCmd) is to just pass the ushort
        /// which would be deserialized to the desired short.  We don't want to cast directly
        /// from short to ushort or it'll probably truncate negative values.  These things are 
        /// easier in C.
        /// </remarks>
        public static ushort shortAsUshort(short n)
        {
            byte[] tmp = BitConverter.GetBytes(n);
            return BitConverter.ToUInt16(tmp, 0);
        }
    }
}