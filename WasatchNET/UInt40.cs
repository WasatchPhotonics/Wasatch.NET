using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    /// <summary>
    /// 40-bit unsigned value, used for many of the laser functions.
    /// </summary>
    class UInt40
    {
        public ushort LSW { get; private set; }
        public ushort MidW { get; private set; }
        public byte MSB { get; private set; }
        public byte[] buf { get; private set; }

        public UInt40(UInt64 value)
        {
            // if we have to do this twice, make a UInt40 class
            const UInt64 max = (((UInt64)1) << 40) - 1;
            if (value > max)
                throw new ArgumentOutOfRangeException();

            LSW  = (ushort)(value & 0xffff);         // least-significant word
            MidW = (ushort)((value >> 16) & 0xffff); // next-least significant word
            MSB  = (byte)(value >> 32);              // most-significant byte

            buf = new byte[1];
            buf[0] = MSB;
        }
    }
}
