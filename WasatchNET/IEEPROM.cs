using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>
    /// COM wrapper for the EEPROM class.
    /// </summary>
    [ComVisible(true)]
    [Guid("D6BC706B-4B50-4CFD-AFFC-6A01F56B92B4")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IEEPROM
    {
        byte format { get; set; }

        /// <summary>spectrometer model</summary>
        string model { get; set;  }

        /// <summary>spectrometer serialNumber</summary>
        string serialNumber { get; set; }

        /// <summary>baud rate (bits/sec) for serial communications</summary>
        uint baudRate { get; set;  }

        /// <summary>whether the spectrometer has an on-board TEC for cooling the detector</summary>
        bool hasCooling { get; set; }

        /// <summary>whether the spectrometer has an on-board battery</summary>
        bool hasBattery { get; set; }

        /// <summary>whether the spectrometer has an integrated laser</summary>
        bool hasLaser { get; set; }

        /// <summary>the integral center wavelength of the laser in nanometers, if present</summary>
        /// <remarks>user-writable</remarks>
        /// <see cref="Util.wavelengthsToWavenumbers(double, double[])"/>
        ushort excitationNM { get; set; }

        /// <summary>the slit width in µm</summary>
        ushort slitSizeUM { get; set;  }

        // These will come with ENG-0034 Rev 4
        ushort startupIntegrationTimeMS { get; set; }
        short TECSetpoint { get; set; }
        byte startupTriggeringMode { get; set; }
        float detectorGain { get; set; }
        short detectorOffset { get; set; }
        float detectorGainOdd { get; set; }
        short detectorOffsetOdd { get; set; }

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
        float[] degCToDACCoeffs { get; set; }
        short detectorTempMin { get; set; }
        short detectorTempMax { get; set; }

        /// <summary>
        /// These are used to convert 12-bit raw ADC temperature readings into degrees Celsius.
        /// </summary>
        /// <remarks>
        /// These correspond to the fields "Therm to Temp Cal" in Wasatch Model Configuration GUI.
        /// 
        /// Use these when reading the detector temperature.
        /// </remarks>
        float[] adcToDegCCoeffs { get; set; }
        short thermistorResistanceAt298K { get; set; }
        short thermistorBeta { get; set; }

        /// <summary>when the unit was last calibrated (unstructured 12-char field)</summary>
        /// <remarks>user-writable</remarks>
        string calibrationDate { get; set; }

        /// <summary>whom the unit was last calibrated by (unstructured 3-char field)</summary>
        /// <remarks>user-writable</remarks>
        string calibrationBy { get; set; }

        /////////////////////////////////////////////////////////////////////////       
        // Page 2
        /////////////////////////////////////////////////////////////////////////       

        string detectorName { get; set; }
        ushort activePixelsHoriz { get; set; }
        ushort activePixelsVert { get; set; }
        uint minIntegrationTimeMS { get; set; }
        uint maxIntegrationTimeMS { get; set; }
        ushort actualPixelsHoriz { get; set; }

        // writable

        /// <summary>The first valid, usable pixel on the detector</summary>
        ushort ROIHorizStart { get; set; }
        /// <summary>The last valid, usable pixel on the detector (NOT the first INVALID pixel)</summary>
        ushort ROIHorizEnd { get; set; }
        ushort[] ROIVertRegionStart { get; set; }
        ushort[] ROIVertRegionEnd { get; set; }

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
        float maxLaserPowerMW { get; set; }
        float minLaserPowerMW { get; set; }
        float laserExcitationWavelengthNMFloat { get; set; }
        float[] laserPowerCoeffs { get; set; }

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
        byte[] userData { get; set; }

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
        short[] badPixels { get; set; }

        /// <summary>additional information to tailor the specs inferred from "model"</summary>
        string productConfiguration { get; set;  }


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
        bool write(bool allPages=false);

        /// <summary>Called automatically when Spectrometer opened</summary>
        /// <remarks>Can be re-called to overwrite local changes to field contents with spectrometer data</remarks>
        /// <returns>true on success, false on failure</returns>
        bool read();
    }
}
