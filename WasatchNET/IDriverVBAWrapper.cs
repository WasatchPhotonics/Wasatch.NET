using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    [ComVisible(true)]  
    [Guid("8EBC7FE1-9850-4267-A80C-D1BEF9A96B4A")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)] 
    public interface IDriverVBAWrapper
    {
        IDriver instance { get; }
    }
}
