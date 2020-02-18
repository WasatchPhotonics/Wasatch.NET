# MultiChannelWrapper

WasatchNET has always provided support to control multiple spectrometers in 
parallel.  However, such control is inherently manual, with all operations and
timing left to the user.  Other than basic synchronization of the USB bus, no
automated timing or coordination is provided across multiple devices.

MultiChannelWrapper simplifies operation for developers creating multi-channel
spectroscopic applications in which multiple spectrometers are expected to start
acquisitions at the same time, typically via hardware triggering.  In particular,
for customers using languages like LabVIEW in which complex multi-threaded coding
can be painful, the Wrapper encapsulates and automates some steps that would
otherwise be difficult in some environments.

Not all features will be applicable to all users, but each feature was requested
by a particular user for a particular system, and may be found of use to others.

For API, see:

- \ref WasatchNET.MultiChannelWrapper

# Triggering Configuration

In the future, Wasatch Photonics spectrometers will have an explicit OEM Accessory
Connector providing clearly-allocated pins for various common triggering and
signaling operations, as well as various GPIO pins which can be used for various
ad-hoc and general purposes.

However, many WP OEM spectrometers do not currently provide software-controllable
"output" pins with which to generate, for instance, a hardware trigger signal.
In this case, if the spectrometers in question do not have embedded lasers, then
the standard "laserEnable" output pin can be used to raise and lower a generic
ouput signal, such as can be used to generate a hardware trigger, or turn system
fans on and off.

In the EEPROM configuration, when a spectrometer is configured to control a 
"feature" such as the "trigger" or the "fan," what is meant is that the identified
spectrometer's laserEnable pin has been wired to the named device or peripheral, 
and that system component can be controlled (on or off) by raising or lowering
the laserEnable output pin (i.e., by "firing" or "turning off" the non-installed 
laser).

# EEPROM Configuration

Multi-channel systems are assumed to use the "userText" portion of EEPROM page 4
to define various settings appropriate to multi-channel operations.

In particular, each spectrometer in a multi-channel system is expected to have
one or more "name=value" pairs defined in the userText field, delimited by
semi-colons.

Supported fields currently include:

- pos: the integral channel ID or "position" within the physical system (i.e. 1-8)
- feature: a unique feature assigned to that spectrometer's output signal, allowing
    it to control one hardware feature within the system

In a representative test system, the eight included spectrometers had their 
individual userText fields configured as follows:

	pos=1; feature=trigger
	pos=2; feature=fan
	pos=3
	pos=4
	pos=5
	pos=6
	pos=7
	pos=8

# MultiChannelDemo

This executable program is provided as an example of how to use MultiChannelWrapper.
Note that it is hard-coded to support an 8-channel system, but will work with fewer
spectrometers, and can readily be extended to support more if needed.

