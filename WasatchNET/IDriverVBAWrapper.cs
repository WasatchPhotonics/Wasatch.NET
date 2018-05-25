using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    [ComVisible(true)]
    [Guid("B32497AD-4D20-4027-9E90-5A34E7367B3B")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)] 
    public interface IDriverVBAWrapper
    {
        IDriver instance { get; }
        IDriver getSingleton();
    }
}
