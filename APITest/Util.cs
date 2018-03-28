using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APITest
{
    public class Util
    {
        public static ushort fromHex(string s)
        {
            if (s != null && s.StartsWith("0x"))
                return ushort.Parse(s.Substring(2), System.Globalization.NumberStyles.HexNumber);
            return 0;
        }

        public static byte requestType(Command.Direction d)
        {
            return (byte) (d == Command.Direction.DEVICE_TO_HOST ? 0xC0 : 0x40);
        }
    }
}
