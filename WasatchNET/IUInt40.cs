using System;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>
    /// 40-bit unsigned value, used for many of the laser functions.
    /// </summary>
    [ComVisible(true)]  
    [Guid("5BC91277-C373-44F3-8C84-77964365F627")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IUInt40
    {
        ushort LSW { get; }
        ushort MidW { get; }
        byte MSB { get; }
        byte[] buf { get; }
    }
}
