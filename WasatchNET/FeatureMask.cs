using MPSSELight;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class FeatureMask
    {
        public bool invertXAxis { get; set; }
        public bool bin2x2 { get; set; }

        public FeatureMask(ushort value = 0)
        {
            invertXAxis = 0 != (value & 0x0001);
            bin2x2      = 0 != (value & 0x0002);
        }

        public ushort toUInt16()
        {
            ushort value = 0;
            if (invertXAxis) value |= 0x0001;
            if (bin2x2)      value |= 0x0002;
            return value;
        }
    }
}
