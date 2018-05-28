using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>
    /// This interface is provided for COM clients (Delphi etc) who seem to find it useful.
    /// I don't know that .NET users would find much benefit in it.
    /// </summary>
    [ComVisible(true)]
    [Guid("D6BC706B-4B50-4CFD-AFFC-6A01F56B92B4")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IModelConfig
    {
        List<byte[]> pages { get; }

        /// <summary>spectrometer model</summary>
        string model { get; }

        /// <summary>spectrometer serialNumber</summary>
        string serialNumber { get; }

        /// <summary>baud rate (bits/sec) for serial communications</summary>
        int baudRate { get; }

        /// <summary>whether the spectrometer has an on-board TEC for cooling the detector</summary>
        bool hasCooling { get; }

        /// <summary>whether the spectrometer has an on-board battery</summary>
        bool hasBattery { get; }

        /// <summary>whether the spectrometer has an integrated laser</summary>
        bool hasLaser { get; }

        /// <summary>the integral center wavelength of the laser in nanometers, if present</summary>
        /// <remarks>user-writable</remarks>
        /// <see cref="Util.wavelengthsToWavenumbers(double, double[])"/>
        short excitationNM { get; }

        /// <summary>the slit width in µm</summary>
        short slitSizeUM { get; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 1
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>coefficients of a 3rd-order polynomial representing the configured wavelength calibration</summary>
        /// <remarks>
        /// These are automatically expanded into an accessible array in 
        /// Spectrometer.wavelengths.  Also see Util.generateWavelengths() for 
        /// the process of expanding the polynomial.
        ///
        /// user-writable
        /// </remarks>
        /// <see cref="Spectrometer.wavelengths"/>
        /// <see cref="Util.generateWavelengths(uint, float[])"/>
        float[] wavecalCoeffs { get; set; }

        /// <summary>
        /// These are used to convert the user's desired setpoint in degrees Celsius to raw 12-bit DAC inputs.
        /// </summary>
        /// <remarks>
        /// These correspond to the fields "Temp to TEC Cal" in Wasatch Model Configuration GUI.
        ///
        /// Use these when setting the TEC setpoint.
        /// </remarks>
        float[] degCToDACCoeffs { get; }
        float detectorTempMin { get; }
        float detectorTempMax { get; }

        /// <summary>
        /// These are used to convert 12-bit raw ADC temperature readings into degrees Celsius.
        /// </summary>
        /// <remarks>
        /// These correspond to the fields "Therm to Temp Cal" in Wasatch Model Configuration GUI.
        /// 
        /// Use these when reading the detector temperature.
        /// </remarks>
        float[] adcToDegCCoeffs { get; }
        short thermistorResistanceAt298K { get; }
        short thermistorBeta { get; }

        /// <summary>when the unit was last calibrated (unstructured 12-char field)</summary>
        /// <remarks>user-writable</remarks>
        string calibrationDate { get; set; }

        /// <summary>whom the unit was last calibrated by (unstructured 3-char field)</summary>
        /// <remarks>user-writable</remarks>
        string calibrationBy { get; set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 2
        /////////////////////////////////////////////////////////////////////////       

        string detectorName { get; }
        short activePixelsHoriz { get; }
        short activePixelsVert { get; }
        ushort minIntegrationTimeMS { get; }
        ushort maxIntegrationTimeMS { get; }
        short actualHoriz { get; }

        // writable
        short ROIHorizStart { get; set; }
        short ROIHorizEnd { get; set; }
        short[] ROIVertRegionStart { get; }
        short[] ROIVertRegionEnd { get; }

        /// <summary>
        /// These are reserved for a non-linearity calibration,
        /// but may be harnessed by users for other purposes.
        /// </summary>
        /// <remarks>user-writable</remarks>
        float[] linearityCoeffs { get; set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 3
        /////////////////////////////////////////////////////////////////////////       

        // public int deviceLifetimeOperationMinutes { get; private set; }
        // public int laserLifetimeOperationMinutes { get; private set; }
        // public short laserTemperatureMax { get; private set; }
        // public short laserTemperatureMin { get; private set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 4
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// 63 bytes of unstructured space which the user is free to use however
        /// they see fit.
        /// </summary>
        /// <remarks>
        /// For convenience, the same raw storage space is also accessible as a 
        /// null-terminated string via userText. 
        ///
        /// Unfortunately, the 64th byte (you knew there had to be one) is used
        /// internally to represent EEPROM page format version.
        /// </remarks>
        byte[] userData { get; }

        /// <summary>
        /// a stringified version of the 63-byte raw data block provided by userData
        /// </summary>
        /// <remarks>accessible as a null-terminated string via userText</remarks>
        string userText { get; set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 5
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// array of up to 15 "bad" (hot or dead) pixels which software may wish
        /// to skip or "average over" during spectral post-processing.
        /// </summary>
        /// <remarks>bad pixels are identified by pixel number; empty slots are indicated by -1</remarks>
        short[] badPixels { get; }

        /////////////////////////////////////////////////////////////////////////       
        // Pages 6-7 unallocated
        /////////////////////////////////////////////////////////////////////////       

        /////////////////////////////////////////////////////////////////////////       
        //
        // public methods
        //
        /////////////////////////////////////////////////////////////////////////       

        /// <summary>
        /// Save updated EEPROM fields to the device.
        /// </summary>
        /// <remarks>
        /// Only a handful of fields are recommended to be changed by users: 
        ///
        /// - excitationNM
        /// - wavecalCoeffs
        /// - calibrationDate
        /// - calibrationBy
        /// - ROI
        /// - linearityCoeffs (not currently used)
        /// - userData
        /// - badPixels
        /// 
        /// Note that the EEPROM isn't an SSD...it's not terribly fast, and there
        /// are a finite number of lifetime writes, so use sparingly.
        ///
        /// Due to the high risk of bricking a unit through a failed / bad EEPROM
        /// write, all internal calls bail at the first error in hopes of salvaging
        /// the unit if at all possible.
        ///
        /// That said, if you do frag your EEPROM, Wasatch has a "Model Configuration"
        /// utility to let you manually write EEPROM fields; contact your sales rep
        /// for a copy.
        /// </remarks>
        /// <returns>true on success, false on failure</returns>
        bool write();
    }
}
