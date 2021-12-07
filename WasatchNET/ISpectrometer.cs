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
        bool isSPI { get; }
        bool isInGaAs { get; }
        bool hasLaser { get; }

        /// <summary>how many pixels does the spectrometer have (spectrum length)</summary>
        uint pixels { get; }

        float excitationWavelengthNM { get; set; }

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

        uint batteryStateRaw { get; }
        float batteryPercentage { get; }
        bool batteryCharging { get; }

        /// <summary>After the first trigger is received, no further triggers are required; spectrometer will enter free-running mode.</summary>
        bool continuousAcquisitionEnable { get; set; }

        /// <summary>When not using "continous acquisitions" with external triggers, how many spectra to acquire per trigger event.</summary>
        byte continuousFrames { get; set; }

        /// <summary>Maps to an FPGA register inside the spectrometer used to scale pixels read from the ADC to optimize dynamic range.</summary>
        /// <remarks>Not normally changed by customer code.  Values are normally read from the EEPROM and written back to the spectrometer's FPGA
        ///          by the driver at initialization.
        ///
        ///          Altering this value may degrade spectrometer performance.
        /// </remarks>
        float detectorGain { get; set; }

        /// <summary>Maps to an FPGA register inside the spectrometer used to offset the pixels (dark baseline) read from the ADC to optimize dynamic range.</summary>
        /// <remarks>Not normally changed by customer code.  Values are normally read from the EEPROM and written back to the spectrometer's FPGA
        ///          by the driver at initialization.
        ///
        ///          Altering this value may degrade spectrometer performance.
        /// </remarks>
        short detectorOffset { get; set; }

        /// <summary>(InGaAs-only) Companion property to detectorGain, which on InGaAs detectors applies only to even-numbered pixels.</summary>
        float detectorGainOdd { get; set; }

        /// <summary>(InGaAs-only) Companion property to detectorOffset, which on InGaAs detectors applies only to even-numbered pixels.</summary>
        short detectorOffsetOdd { get; set; }

        ushort detectorSensingThreshold { get; set; }
        bool detectorSensingThresholdEnabled { get; set; }

        bool detectorTECEnabled { get; set; }
        float detectorTECSetpointDegC { get; set; }
        ushort detectorTECSetpointRaw { get; set; }

        float detectorTemperatureDegC { get; }
        ushort detectorTemperatureRaw { get; }
        double detectorTemperatureCacheTimeMS { get; set; }

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
        Spectrometer.LaserPowerResolution laserPowerResolution { get; set; }

        /// <summary>If you want the laser modulation to start part-way through an acquisition, this defines the delay in microseconds from the beginning of the
        ///          integration until laser modulation begins.</summary>
        /// <remarks>Warning: Laser modulation commands are normally internally set by the function setLaserPowerPercentage().
        ///
        ///          Value is in microseconds, range 40bit.
        ///
        ///          It is unclear from documentation whether MODULATION starts after the given delay, or the LASER is enabled after the delay.
        /// </remarks>
        UInt64 laserModulationPulseDelay { get; set; }

        /// <summary>When defining the laser modulation duty cycle, the length (period) of the duty cycle in microsec.</summary>
        /// <remarks>Warning: Laser modulation commands are normally internally set by the function setLaserPowerPercentage().
        ///
        ///          Value is in microseconds, range 40bit.</remarks>
        UInt64 laserModulationPeriod { get; set; }

        /// <summary>If you only want the laser to be modulated for a portion of each acquisition (rare), how long in microseconds should the laser be modulated during each integration.</summary>
        /// <remarks>Value is in microseconds, range 40bit.
        /// 
        ///          It is unclear from documentation if, at the end of this duration, the LASER turns off (zero power), or MODULATION turns off (i.e. reverts to full power).
        /// </remarks>
        UInt64 laserModulationDuration { get; set; }

        /// <summary>When defining the laser modulation duty cycle, the length (width) of the period during which the laser is enabled.</summary>
        /// <remarks>Warning: Laser modulation commands are normally internally set by the function setLaserPowerPercentage().
        ///
        ///          Value is in microseconds, range 40bit.
        ///
        ///          Example: if period was 100us, and pulseWidth was 20us, then the laser would fire 1/5 of the time and therefore operating
        ///          at 20% power.
        /// </remarks>
        UInt64 laserModulationPulseWidth { get; set; }

        /// <remarks>
        /// Not supported on all spectrometers.  Firmware status uncertain.
        ///
        /// It is unclear how this relates to FPGA_LASER_CONTROL.RAMPING.  
        /// </remarks>
        bool laserRampingEnabled { get; set; }

        /// <summary>Configure detector for 2D image mode</summary>
        /// <remarks>
        /// Not supported on all spectrometers.
        ///
        /// Wasatch spectrometers normally "vertically bins" pixel columns on the detector, outputting the spectrum as a one-dimensional array of intensities by pixel.
        ///
        /// For production alignment, an "area scan" imaging mode is provided to output each row on the detector as a separate line, so client software can reconstruct
        /// a 2D image of the light patterns spread across the detector.  In this mode, the intensity value of the first pixel of each line is overwritten by the row
        /// index, to ensure the 2D image is received and displayed correctly.
        /// </remarks>
        bool areaScanEnabled { get; set; }

        bool useRamanIntensityCorrection { get; set; }

        /// <summary>
        /// convert the raw laser temperature reading into degrees centigrade
        /// </summary>
        /// <returns>laser temperature in &deg;C</returns>
        /// <see cref="https://www.ipslasers.com/data-sheets/SM-TO-56-Data-Sheet-IPS.pdf"/>
        /// <remarks>
        /// Laser temperature conversion doesn't use EEPROM coeffs at all.
        /// Most Wasatch Raman systems use an IPS Wavelength-Stabilized TO-56
        /// laser, which internally uses a Betatherm 10K3CG3 thermistor.
        ///    
        /// The official conversion from thermistor resistance (in ohms) to degC is:
        ///    
        /// 1 / (   C1 
        ///       + C2 * ln(ohms) 
        ///       + C3 * pow(ln(ohms), 3)
        ///     ) 
        /// - 273.15
        ///    
        /// Where: C1 = 0.00113
        ///        C2 = 0.000234
        ///        C3 = 8.78e-8
        ///
        /// Early Dash / ENLIGHTEN implementations used a simpler curve-fit which yielded 
        /// nearly identical performance.
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

        /// <summary>Internal firmware name for the highGainModeEnabled feature available on InGaAs detectors</summary>
        bool optCFSelect { get; }

        /// <summary>Whether area scan mode is supported</summary>
        bool optAreaScan { get; }
        bool optActualIntegrationTime { get; }
        bool optHorizontalBinning { get; }
        FPGA_INTEG_TIME_RES optIntegrationTimeResolution { get; }
        FPGA_DATA_HEADER optDataHeaderTag { get; }
        FPGA_LASER_TYPE optLaserType { get; }
        FPGA_LASER_CONTROL optLaserControl { get; }

        ushort primaryADC { get; }

        /// <summary>
        /// This is provided for spectrometers with a secondary ADC connected to an external 
        /// laser, photodiode or what-have-you.  Attempts to read it on spectrometers where it
        /// has not been configured can result in indeterminate behavior; therefore, hasSecondaryADC
        /// is provided to allow callers to selectively enable this function if they believe they
        /// are using supported hardware.
        /// </summary>
        ushort secondaryADC { get; }

        /// <summary>
        /// This should be replaced with an FGPACompilationFlag or EEPROM field at some point.
        /// </summary>
        bool hasSecondaryADC { get; set; }

        /// <summary>A configurable delay from when an inbound trigger signal is
        /// received by the spectrometer, until the triggered acquisition actually starts.</summary>
        /// <remarks>
        /// Default value is 0us.
        ///
        /// Unit is in 0.5 microseconds (500ns), so value of 25 would represent 12.5us.
        ///
        /// Value is 24bit, so max value is 16777216 (8.388608 sec).
        ///
        /// As part of triggering, only currently supported on ARM.
        /// </remarks>
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
        double[] getSpectrum(bool forceNew);
    }
}
