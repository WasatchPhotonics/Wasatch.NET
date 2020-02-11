# MultiChannelWrapper

WasatchNET has always provided support to control multiple spectrometers in 
parallel.  However, such control is inherently manual, with all operations and
timing left to the user.  Other than basic synchronization of the USB bus, no
automated timing or coordination is provided across multiple devices.

MultiChannelWrapper simplifies operation for developers creating multi-channel
spectroscopic applications in which multiple spectrometers are expected to start
acquisitions at the same time, typically via hardware triggering.

Not all features will be applicable to all users, but each feature was requested
by a particular user for a particular system, and may be found of use to others.

# Proposal 

We could set a "channel position" in the EEPROM, using EEPROM page 4 (allocated 
for "customer data"), such that each spectrometer will “know” that it’s in position 
1, 2, 8 etc (allowing user to easily reference them by channel ID in their software).

- bool fanEnabled: turns fans on or off (via spectrometer with feature=fan)
- setTriggerEnable(true) — sets all spectrometers to block on a hardware trigger 
- startAcquisition() — sends a hardware trigger from the master (feature=trigger) 
	to all slaves (presumably including itself)
- getSpectra() — reads back full set of 8 spectra in a `Dictionary<channel, Tuple<double[], double[]>>`, 
	each containing (wavelengths, intensities) for typical application measurement 
	(blocks if triggerMeasurement has not yet been called)
- getSpectrumChannel(int channel) — gets one `<wavelengths, intensities>` spectrum 
	from the specified channel for debugging purposes (likewise blocks until 
	triggerMeasurement)

# EEPROM

Use EEPROM page 4 to contain ASCII text like this (each line represents a different spectrometer, 63 char max):

	pos=1; feature=trigger
	pos=2; feature=fan
	pos=3
	pos=4
	pos=5
	pos=6
	pos=7
	pos=8
