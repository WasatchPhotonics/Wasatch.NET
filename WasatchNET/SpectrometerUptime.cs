using System.Collections.Generic;

namespace WasatchNET
{
    /// <summary>
    /// Tracks whether a given spectrometer's "last known state" was successful 
    /// (working) or error.  
    /// </summary>
    ///
    /// <remarks>
    /// Given that common spectrometer error states only involve an unresponsive
    /// FPGA (can't get spectra) while the microcontroller keeps working (can 
    /// read detector temperature etc), the enum more literally means "last call
    /// to getSpectrum returned non-null".
    /// </remarks>
    ///
    /// <todo>
    /// - consider storing some level of spectrometerStatus (integration time, laser
    ///   state etc) such that a re-initialized spectrometer with a known (non-default)
    ///   state can be quickly and automatically restored to non-default values
    /// </todo>
    public class SpectrometerUptime
    {
        enum LastKnownState { SUCCESS, ERROR };

        Dictionary<string, LastKnownState> lastKnownStates = new Dictionary<string, LastKnownState>();

        public void setUnknown(string key) => lastKnownStates.Remove(key);
        public void setSuccess(string key) => lastKnownStates[key] = LastKnownState.SUCCESS;
        public void setError  (string key) => lastKnownStates[key] = LastKnownState.ERROR;

        // This basically assumes that if Spectrometer.open() is being called on
        // a spectrometer, and the last thing we know about the spectrometer is that
        // it was failing to take spectra, then hopefully someone power-cycled it and
        // it should be treated like a new unit.
        public bool needsInitialization(string key)
        {
            if (key is null || !lastKnownStates.ContainsKey(key))
                return true;

            return lastKnownStates[key] == LastKnownState.ERROR;
        }
    }
}
