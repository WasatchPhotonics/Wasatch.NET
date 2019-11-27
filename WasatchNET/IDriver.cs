using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>
    /// Singleton providing access to individual Spectrometer instances,
    /// while providing high-level support infrastructure like a master
    /// version string, reusable logger etc.
    ///
    /// In defiance of Microsoft convention, there are no Hungarian 
    /// prefixes, and camelCase is used throughout. Sorry.
    /// </summary>
    [ComVisible(true)]  
    [Guid("860AEAC3-6016-47B0-ABB9-88F0194601EB")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IDriver
    {
        Logger logger { get; }
        string version { get; }

        /// <summary>
        /// Iterate over all discoverable Wasatch Photonics USB spectrometers,
        /// and return the number found. Individual spectrometers can then be
        /// accessed via the getSpectrometer(index) call.
        /// </summary>
        /// <returns>number of Wasatch Photonics USB spectrometers found</returns>
        int openAllSpectrometers();

        /// <summary>
        /// How many Wasatch USB spectrometers were found.
        /// </summary>
        /// <returns>number of enumerated Wasatch spectrometers</returns>
        int getNumberOfSpectrometers();

        /// <summary>
        /// Obtains a reference to the specified spectrometer, performing any 
        /// prelimary "open" / instantiation steps required to leave the spectrometer
        /// fully ready for use.
        /// </summary>
        /// <param name="index">zero-indexed (should be less than the value returned by openAllSpectrometers() / getNumberOfSpectrometers())</param>
        /// <remarks>Spectrometers are deterministically ordered by (model, serialNumber) for repeatability.</remarks>
        /// <returns>a reference to the requested Spectrometer object, or null on error</returns>
        Spectrometer getSpectrometer(int index);

        /// <summary>
        /// Automatically called as part of application shutdown (can be called manually).
        /// </summary>
        void closeAllSpectrometers();
    }
}
