using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    [ComVisible(true)]
    [ProgId("WasatchNET.UInt12")]
    [ClassInterface(ClassInterfaceType.None)]

    class UInt12 : IUInt12
    {
        public ushort val { get; private set; }

        public UInt12(ushort value)
        {
            val = (value > 0xFFF) ? (ushort)0xFFF : value;
        }
    }
}
