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

        /// <summary>metadata inferred from the spectrometer's USB PID</summary>
        FeatureIdentification featureIdentification { get; }

        /// <summary>set of compilation options used to compile the FPGA firmware in this spectrometer</summary>
        FPGAOptions fpgaOptions { get; }

        /// <summary>configuration settings stored in the spectrometer's EEPROM</summary>
        ModelConfig modelConfig { get; }

        bool laserRampingEnabled { get; set; }

        /// <summary>
        /// Current integration time in milliseconds. Reading this property
        /// returns a CACHED value for performance reasons; use getIntegrationTimeMS
        /// to read from spectrometer.
        /// </summary>
        /// <see cref="getIntegrationTimeMS"/>
        uint integrationTimeMS { get; set; }

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

        void close();

        bool isARM();
        bool hasLaser();
        bool reconnect();

        bool setCCDTriggerSource(ushort source);
        bool setExternalTriggerOutput(EXTERNAL_TRIGGER_OUTPUT value);
        bool setTriggerDelay(uint value);
        bool setLaserRampingEnable(bool flag);
        bool setHorizontalBinning(HORIZ_BINNING mode);

        /// <summary>
        /// Set the detector's thermoelectric cooler (TEC) to the desired setpoint in degrees Celsius.
        /// </summary>
        /// <param name="degC">Desired temperature in Celsius.</param>
        /// <returns>true on success</returns>
        bool setCCDTemperatureSetpointDegC(float degC);
        bool setDFUMode(bool flag);
        bool setHighGainModeEnabled(bool flag);
        bool setCCDGain(float gain);
        bool setCCDTemperatureEnable(bool flag);
        bool setDAC(ushort word);
        bool setCCDOffset(ushort value);
        bool setCCDSensingThreshold(ushort value);
        bool setCCDThresholdSensingEnable(bool flag);

        /// <summary>
        /// Actually reads integration time from the spectrometer.
        /// </summary>
        /// <returns>integration time in milliseconds</returns>
        uint getIntegrationTimeMS();
        string getFPGARev();
        string getFirmwareRev();
        CCD_TRIGGER_SOURCE getCCDTriggerSource();
        EXTERNAL_TRIGGER_OUTPUT getExternalTriggerOutput();
        HORIZ_BINNING getHorizBinning();

        /// <summary>
        /// Return integration time + clock-out time (and laser pulse time if externally triggered).
        /// </summary>
        /// <remarks>buggy? still testing</remarks>
        /// <returns>actual integration time in microseconds (zero on error)</returns>
        uint getActualIntegrationTimeUS();
        bool getLaserRampingEnabled();
        bool getHighGainModeEnabled();
        uint getCCDTriggerDelay();
        ushort getCCDTemperatureRaw();

        float getCCDTemperatureDegC();
        byte getLaserTemperatureSetpoint();

        ushort getActualFrames();
        float getCCDGain();
        ushort getCCDOffset();
        ushort getCCDSensingThreshold();
        bool getCCDThresholdSensingEnabled();
        bool getCCDTempEnabled();
        ushort getDAC();
        bool getInterlockEnabled();
        bool getLaserEnabled();
        bool getLaserModulationEnabled();
        UInt64 getLaserModulationDuration();
        UInt64 getLaserModulationPeriod();
        UInt64 getLaserModulationPulseDelay();
        UInt64 getLaserModulationPulseWidth();
        uint getLineLength();
        byte getSelectedLaser();
        bool getLaserModulationLinkedToIntegrationTime();
        bool getOptCFSelect();
        bool getOptAreaScan();
        bool getOptActIntTime();
        bool getOptHorizontalBinning();
        FPGA_INTEG_TIME_RES getOptIntTimeRes();
        FPGA_DATA_HEADER getOptDataHdrTab();
        FPGA_LASER_TYPE getOptLaserType();
        FPGA_LASER_CONTROL getOptLaserControl();

        ushort getLaserTemperatureRaw();

        /// <summary>
        /// convert the raw laser temperature reading into degrees centigrade
        /// </summary>
        /// <returns>laser temperature in &deg;C</returns>
        /// <remarks>
        /// Note that the adcToDegCCoeffs are NOT used in this method; those
        /// coefficients ONLY apply to the detector.  At this time, all laser
        /// temperature math is hardcoded (confirmed with Jason 22-Nov-2017).
        /// </remarks>
        float getLaserTemperatureDegC();

        // TODO: something's buggy with this?
        // MZ: not sure we can return this in deg C (our adctoDegC coeffs are for the thermistor, not the TEC...I THINK)
        ushort getDetectorSetpointRaw();

        /// <summary>
        /// When using external triggering, perform multiple acquisitions on a single inbound trigger event.
        /// </summary>
        /// <param name="flag">whether to acquire multiple spectra per trigger</param>
        void setContinuousCCDEnable(bool flag);

        /// <summary>
        /// Determine whether continuous acquisition is enabled
        /// </summary>
        /// <returns>whether continuous acquisition is enabled</returns>
        bool getContinuousCCDEnable();

        /// <summary>
        /// When using "continous CCD" acquisitions with external triggers, how many spectra to acquire per trigger event.
        /// </summary>
        /// <param name="n">how many spectra to acquire</param>
        void setContinuousCCDFrames(byte n);

        /// <summary>
        /// When using "continuous CCD" acquisitions with external triggering, how many spectra are being acquired per trigger event.
        /// </summary>
        /// <returns>number of spectra</returns>
        byte getContinuousCCDFrames();

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
        /// Sets the laser modulation duration to the given 40-bit value (microseconds).
        /// </summary>
        /// <param name="us">duration in microseconds</param>
        /// <returns>true on success</returns>
        bool setLaserModulationDurationMicrosec(UInt64 us);

        /// <summary>
        /// Sets the laser modulation duration to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit duration</param>
        /// <returns>true on success</returns>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        bool setLaserModulationPulseWidth(UInt64 value);

        /// <summary>
        /// Sets the laser modulation period to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit period</param>
        /// <returns>true on success</returns>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        bool setLaserModulationPeriod(UInt64 value);

        /// <summary>
        /// Sets the laser modulation pulse delay to the given 40-bit value.
        /// </summary>
        /// <param name="value">40-bit period</param>
        /// <returns>true on success</returns>
        /// <see cref="setLaserModulationDuration(ulong)"/>
        bool setLaserModulationPulseDelay(UInt64 value);

        /// <summary>
        /// This is probably inadvisable.  Unclear when the user would want to do this.
        /// </summary>
        /// <param name="value"></param>
        bool setLaserTemperatureSetpoint(byte value);
        bool setLaserModulationEnable(bool flag);
        bool setSelectedLaser(byte id);
        bool linkLaserModToIntegrationTime(bool flag);
        bool setLaserEnable(bool flag);
    }
}
