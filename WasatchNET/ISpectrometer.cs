using System;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>
    /// This interface is provided for COM clients (Delphi etc) who seem to find it useful.
    /// I don't know that .NET users would find much benefit in it.
    /// </summary>
    [ComVisible(true)]
    [Guid("7F04C891-E0AC-4F47-812E-C757CF2918B7")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface ISpectrometer
    {
        ////////////////////////////////////////////////////////////////////////
        // structures
        ////////////////////////////////////////////////////////////////////////

        /// <summary>metadata inferred from the spectrometer's USB PID</summary>
        FeatureIdentification featureIdentification { get; }

        /// <summary>set of compilation options used to compile the FPGA firmware in this spectrometer</summary>
        FPGAOptions fpgaOptions { get; }

        /// <summary>configuration settings stored in the spectrometer's EEPROM</summary>
        EEPROM eeprom { get; }

        ////////////////////////////////////////////////////////////////////////
        // convenience accessors
        ////////////////////////////////////////////////////////////////////////

        bool isARM { get; }
        bool isSiG { get; }
        bool hasLaser { get; }

        /// <summary>how many pixels does the spectrometer have (spectrum length)</summary>
        uint pixels { get; }

        float excitationWavelengthNM();

        /// <summary>pre-populated array of wavelengths (nm) by pixel, generated from eeprom.wavecalCoeffs</summary>
        /// <remarks>see Util.generateWavelengths</remarks>
        double[] wavelengths { get; }

        /// <summary>pre-populated array of Raman shifts in wavenumber (1/cm) by pixel, generated from wavelengths[] and excitationNM</summary>
        /// <remarks>see Util.wavelengthsToWavenumbers</remarks>
        double[] wavenumbers { get; }

        /// <summary>spectrometer model</summary>
        string model { get; }

        /// <summary>spectrometer serial number</summary>
        string serialNumber { get; }

        ////////////////////////////////////////////////////////////////////////
        // Driver state
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// How many acquisitions to average together (zero for no averaging)
        /// </summary>
        uint scanAveraging { get; set; }

        /// <summary>
        /// Perform post-acquisition high-frequency smoothing by averaging
        /// together "n" pixels to either side of each acquired pixel; zero
        /// to disable (default).
        /// </summary>
        uint boxcarHalfWidth { get; set; }

        /// <summary>
        /// Perform automatic dark subtraction by setting this property to
        /// an acquired dark spectrum; leave "null" to disable.
        /// </summary>
        double[] dark { get; set; }

        ////////////////////////////////////////////////////////////////////////
        // Properties
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// How many frames have been read since last power cycle (has overflow)
        /// </summary>
        ushort actualFrames { get; }

        /// <summary>
        /// Return integration time + clock-out time (and laser pulse time if externally triggered).
        /// </summary>
        /// <returns>actual integration time in microseconds (zero on error)</returns>
        uint actualIntegrationTimeUS { get; }

        /// <summary>
        /// Reads the currently selected 12-bit ADC.
        /// <see cref="selectedADC"/>
        /// <see cref="primaryADC"/>
        /// <see cref="secondaryADC"/>
        /// </summary>
        ushort adcRaw { get; }

        uint batteryStateRaw { get; }
        float batteryPercentage { get; }
        bool batteryCharging { get; }

        /// <summary>After the first trigger is received, no further triggers are required; spectrometer will enter free-running mode.</summary>
        bool continuousAcquisitionEnable { get; set; }

        /// <summary>When not using "continous acquisitions" with external triggers, how many spectra to acquire per trigger event.</summary>
        byte continuousFrames { get; set; }

        float detectorGain { get; set; }
        ushort detectorOffset { get; set; }

        ushort detectorSensingThreshold { get; set; }
        bool detectorSensingThresholdEnabled { get; set; }

        bool detectorTECEnabled { get; set; }
        float detectorTECSetpointDegC { get; set; }
        ushort detectorTECSetpointRaw { get; set; }

        float detectorTemperatureDegC { get; }
        ushort detectorTemperatureRaw { get; }

        string firmwareRevision { get; }
        string fpgaRevision { get; }

        bool highGainModeEnabled { get; set; }

        /// <summary>
        /// Current integration time in milliseconds.
        /// </summary>
        uint integrationTimeMS { get; set; }

        bool laserEnabled { get; set; }
        bool laserInterlockEnabled { get; }
        bool laserModulationEnabled { get; set; }
        bool laserModulationLinkedToIntegrationTime { get; set; }
        UInt64 laserModulationPulseDelay { get; set; }
        UInt64 laserModulationPeriod { get; set; }
        UInt64 laserModulationDuration { get; set; }
        UInt64 laserModulationPulseWidth { get; set; }
        bool laserRampingEnabled { get; set; }

        /// <summary>
        /// convert the raw laser temperature reading into degrees centigrade
        /// </summary>
        /// <returns>laser temperature in &deg;C</returns>
        /// <remarks>
        /// Note that the adcToDegCCoeffs are NOT used in this method; those
        /// coefficients ONLY apply to the detector.  At this time, all laser
        /// temperature math is hardcoded (confirmed with Jason 22-Nov-2017).
        /// </remarks>
        float laserTemperatureDegC { get; }

        /// <summary>
        /// Synonym for primaryADC
        /// </summary>
        /// <see cref="selectedADC"/>
        /// <see cref="primaryADC"/>
        ushort laserTemperatureRaw { get; }

        /// <summary>
        /// FACTORY ONLY
        /// </summary>
        ///
        /// <remarks>
        /// WARNING: raising this value above 63 will "increase temperature AND volatility",
        /// while lowering this value below 63 will "decrease temperature WHILE increasing 
        /// volatility".
        ///
        /// During factory turning of the potentiometer on the laser driver 
        /// controller board, this function is used to temporarily offset
        /// the TEC DAC setpoint from its default power-up value of 63.
        /// 
        /// End-users should not adjust this value at runtime, as the potentiometer
        /// has already been locked-down at a point optimized for laser power 
        /// stability over temperature; any changes to this value from the default
        /// value of 63 will only increase the chances of power instability,
        /// including mode-hopping and hysteresis effects.
        /// 
        /// See RamanSpecCal's LaserTemperatureTest documentation for additional
        /// information.
        ///
        /// Note that input values will automatically be capped at 7-bit (0, 127),
        /// the operative range of the laser TEC DAC.
        ///
        /// Also note that although this property includes the word "temperature"
        /// because the setpoint relates to and will change the laser's central
        /// temperature, you cannot set a specific temperature setpoint in degC,
        /// as the final temperature is generated in hardware as a combination of
        /// the DAC setpoint value and the physical potentiometer.
        /// </remarks>
        byte laserTemperatureSetpointRaw { get; set; }

        uint lineLength { get; }

        bool optCFSelect { get; }
        bool optAreaScan { get; }
        bool optActualIntegrationTime { get; }
        bool optHorizontalBinning { get; }
        FPGA_INTEG_TIME_RES optIntegrationTimeResolution { get; }
        FPGA_DATA_HEADER optDataHeaderTag { get; }
        FPGA_LASER_TYPE optLaserType { get; }
        FPGA_LASER_CONTROL optLaserControl { get; }

        ushort primaryADC { get; }

        /// <summary>
        /// Used to toggle between the primary ADC (index 0, used to read laser temperature)
        /// and secondary ADC (index 1, used for optional OEM accessories like a photodiode).
        /// </summary>
        /// <remarks>
        /// Not all units will have a secondary ADC.
        ///
        /// Due to the way the ADC internally collects multiple analog readings over time, 
        /// between digital readouts, and the fact that "clearing" the ADCs internal 
        /// register is essentially done by reading it, it is advised to perform a 
        /// "throwaway read" after switching the selected ADC, to ensure that the next
        /// intentional read will only contain signal from the new ADC, and not the previous
        /// one.  
        /// 
        /// Therefore, an option has been provided to automatically perform a throwaway read
        /// when the ADC selection is changed.
        /// </remarks>
        byte selectedADC { get; set; }

        ushort secondaryADC { get; }

        uint triggerDelay { get; set; }
        EXTERNAL_TRIGGER_OUTPUT triggerOutput { get; set; }

        /// <summary>
        /// Whether acquisitions are triggered "internally" (via the ACQUIRE opcode
        /// sent by software) or "externally" (via an electrical signal wired to the
        /// accessory connector).
        /// </summary>
        TRIGGER_SOURCE triggerSource { get; set; }

        ////////////////////////////////////////////////////////////////////////
        // Methods
        ////////////////////////////////////////////////////////////////////////

        void close();
        bool reconnect();

        // if eeprom.wavecalCoeffs has changed, regenerate wavelengths
        // (would be nice to automate)
        void regenerateWavelengths();

        /// <summary>
        /// Put the ARM microcontroller into DFU mode for firmware update.
        /// </summary>
        /// <remarks>
        /// Will not work on FX2.  Cannot be "undone" from software (power-cycle 
        /// the spectrometer to reset out of DFU mode).  Not recommended for the
        /// faint of heart.
        /// </remarks>
        bool setDFUMode();

        /// <summary>
        /// Set laser power to the specified percentage.
        /// </summary>
        /// <param name="perc">value from 0 to 1.0</param>
        /// <returns>true on success</returns>
        /// <remarks>
        /// The "fake" buffers being send with the commands relate to a legacy
        /// bug in which some firmware mistakenly checked the "payload length"
        /// rather than "wValue" for their key parameter.  It would be good to
        /// document which firmware versions exhibit this behavior, so we do not
        /// propogate an inefficient and unnecessary patch to newer models which
        /// do not require it.
        /// </remarks>
        bool setLaserPowerPercentage(float perc);

        /// <summary>
        /// Take a single complete spectrum, including any configured scan 
        /// averaging, boxcar and dark subtraction.
        /// </summary>
        /// <returns>The acquired spectrum as an array of doubles</returns>
        double[] getSpectrum();
    }
}