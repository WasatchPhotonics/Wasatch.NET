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
        /// <summary>
        /// The orientations of the grating and detector in this spectrometer are 
        /// rotated such that spectra are read-out "red-to-blue" rather than the
        /// normal "blue-to-red".  Therefore, automatically reverse the spectrum
        /// array.  This EEPROM field ensures that, regardless of hardware 
        /// orientation or firmware, spectra is always reported to the user FROM
        /// WASATCH.NET in a consistent "blue-to-red" order (increasing 
        /// wavelengths).  This is the order intended and assumed by the factory-
        /// configured wavelength calibration.  
        /// 
        /// The user should not have to change behavior or process spectra 
        /// differently due to this value; its purpose is to communicate state 
        /// between the spectrometer and driver and ensure correct internal 
        /// processing within the driver.
        /// </summary>
        public bool invertXAxis { get; set; }

        /// <summary>
        /// Some 2D detectors use a Bayer filter in which pixel columns alternate
        /// between red and blue sensitivity (green is uniform throughout).  By 
        /// binning square blocks of 2x2 pixels (for any given detector position,
        /// this would include 1 blue, 1 red and 2 green), an even sensitivity is
        /// achieved across spectral range.
        /// 
        /// As "vertical binning" is normally performed within firmware, the only
        /// portion of this 2x2 binning which is performed within the software
        /// driver is the horizontal binning.  This is currently performed within
        /// Spectrometer.getSpectrumRaw.
        /// </summary>
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
