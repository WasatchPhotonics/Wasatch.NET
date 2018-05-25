using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>
    /// A way to get to the static Singleton from languages that
    /// don't support static class methods like Driver.getInstance().
    /// </summary>
    /// <remarks>
    /// Research indicates that at least some versions of Visual Basic (pre-.NET),
    /// as well as Visual Basic for Applications (VBA) limit .NET classes to
    /// object creation, instance methods and instance properties. Unfortunately,
    /// that means they can't call pure static methods like 
    /// WasatchNET.Driver.getInstance().
    ///
    /// This class is provided as something that any caller can easily create
    /// (instantiate), and then access the Driver Singleton via the single
    /// exposed "instance" property.
    /// </remarks>
    [ComVisible(true)]
    [Guid("BB20CDE1-E5A3-4649-8161-7B6E8DAD66FE")]
    [ProgId("WasatchNET.DriverVBAWrapper")]
    public class DriverVBAWrapper : IDriverVBAWrapper
    {
        public IDriver instance { get; }
        public DriverVBAWrapper()
        {
            instance = Driver.getInstance();
        }
    }
}
