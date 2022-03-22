namespace WasatchNET
{
    /// <summary>
    /// This class encapsulates a 16-bit set of boolean flags which indicate
    /// whether a given spectrometer has a particular feature or not, without
    /// expending quite as much storage as, for instance, legacy hasCooling,
    /// hasLaser or hasBattery bytes.
    /// </summary>
    public class FeatureMask
    {
        enum Flags 
        { 
            INVERT_X_AXIS                = 0x0001, // 2^0 
            BIN_2X2                      = 0x0002, // 2^1
            GEN15                        = 0x0004, // 2^2
            CUTOFF_INSTALLED             = 0x0008, // 2^3
            HARDWARE_EVEN_ODD_CORRECTION = 0x0010, // 2^4
            SIG_LASER_TEC                = 0x0020, // 2^5
            HAS_INTERLOCK_FEEDBACK       = 0x0040  // 2^6
        }

        public FeatureMask(ushort value = 0)
        {
            invertXAxis              = 0 != (value & (ushort)Flags.INVERT_X_AXIS);
            bin2x2                   = 0 != (value & (ushort)Flags.BIN_2X2);
            gen15                    = 0 != (value & (ushort)Flags.GEN15);
            cutoffInstalled          = 0 != (value & (ushort)Flags.CUTOFF_INSTALLED);
            evenOddHardwareCorrected = 0 != (value & (ushort)Flags.HARDWARE_EVEN_ODD_CORRECTION);
            sigLaserTEC              = 0 != (value & (ushort)Flags.SIG_LASER_TEC);
            hasInterlockFeedback     = 0 != (value & (ushort)Flags.HAS_INTERLOCK_FEEDBACK);
        }

        public ushort toUInt16()
        {
            ushort value = 0;
            if (invertXAxis)                value |= (ushort)Flags.INVERT_X_AXIS;
            if (bin2x2)                     value |= (ushort)Flags.BIN_2X2;
            if (gen15)                      value |= (ushort)Flags.GEN15;
            if (cutoffInstalled)            value |= (ushort)Flags.CUTOFF_INSTALLED;
            if (evenOddHardwareCorrected)   value |= (ushort)Flags.HARDWARE_EVEN_ODD_CORRECTION;
            if (sigLaserTEC)                value |= (ushort)Flags.SIG_LASER_TEC;
            if (hasInterlockFeedback)       value |= (ushort)Flags.HAS_INTERLOCK_FEEDBACK;
            return value;
        }

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

        /// <summary>
        /// Starting in 2021, some of our spectrometers have what is called a 
        /// "Gen 1.5 Connector" for interfacing with various external functionality.
        /// 
        /// More information to come later.
        /// </summary>
        public bool gen15 { get; set; }

        /// <summary>
        /// In some downstream applications, especially related to "range-finding,"
        /// whether or not there's a cutoff filter can greatly affect our in-house 
        /// algorithms.
        /// </summary>
        public bool cutoffInstalled { get; set; }

        /// <summary>
        /// Our InGaAs detector spectrometers have a sawtooth pattern between their
        /// even/odd pixels. We have used differentiated gain and offsets to correct this
        /// difference. We did not have hardware level correction for this pattern
        /// until briefly in 2021, before moving such corrections permanently to software.
        /// This field is used to indicate whether software level
        /// even/odd correction need be applied, or if the correction is already
        /// taken care of at a hardware level.
        /// </summary>
        public bool evenOddHardwareCorrected { get; set; }

        /// <summary>
        /// By default, all Wasatch lasers (either SML or MML) include a built-in
        /// TEC, EXCEPT on the SiG (Series XS, "microRaman") units.  Laser TEC is
        /// a rare / experimental option on that platform, so this this flag is
        /// introduced to configure whether a laser TEC is present.
        /// </summary>
        public bool sigLaserTEC { get; set; }

        /// <summary>
        /// Originally the laser interlock board did not provide feedback to
        /// software as to whether the laser interlock was open (laser cannot
        /// fire) or closed (laser can fire).  In addition, it did not provide
        /// feedback to software as to whether the laser was ACTUALLY FIRING,
        /// irrespective of whether laserEnable was set.  Newer firmware and
        /// board wiring supports this feedback, and this flag indicates whether
        /// the installed firmware and wiring support this feature.
        /// </summary>
        public bool hasInterlockFeedback { get; set; }
    }
}
