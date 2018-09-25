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
        ModelConfig modelConfig { get; }

        ////////////////////////////////////////////////////////////////////////
        // convenience accessors
        ////////////////////////////////////////////////////////////////////////

        bool isARM { get; }
        bool hasLaser { get; }

        /// <summary>how many pixels does the spectrometer have (spectrum length)</summary>
        uint pixels { get; }

        /// <summary>pre-populated array of wavelengths (nm) by pixel, generated from ModelConfig.wavecalCoeffs</summary>
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
        ushort laserTemperatureRaw { get; }
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

        byte selectedADC { get; set; }

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