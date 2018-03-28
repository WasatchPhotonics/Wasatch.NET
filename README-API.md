# Overview

api-fid.json encapsulates the current version of the FeatureIdentificationDevice 
(FID) Application Programming Interface (API) in a syntax that can easily be
read and processed by scripts and programs for schema validation and API
discovery.

# Command Attributes

## Required Attributes

- Opcode (byte)
    - the bRequest in decimal or hex, e.g. "0x64" or "100"
- Direction (enum)
    - the bRequestType ("HOST\_TO\_DEVICE" or "DEVICE\_TO\_HOST")
- Type (enum)
    - see below

## Optional Attributes

- wIndex (uint16)
    - a hard-coded constant expressed in decimal or hex
    - example: GET\_CCD\_TEMP\_SETPOINT vs GET\_DAC
- wValue (uint16)
    - a hard-coded constant expressed in decimal or hex
    - used in all "secondary commands", e.g. GET\_MODEL\_CONFIG
- Enum ([string, ...])
    - ignored unless Type == "Enum"
    - a list of zero-indexed enum labels, e.g. SELECT\_HORIZ\_BINNING
    - only relevant in host-to-device
- Length (int)
    - size of the buffer to send (pre-filled with nulls) or expect to receive
- ReadBack (int)
    - how many bytes the host should read back from the device following a 
      device-to-host transfer, if different from Length
    - e.g. GET\_INTEGRATION\_TIME
- ReadBackARM (int)
    - overrides ReadBack on ARM architectures, e.g. READ\_COMPILATION\_OPTIONS
- Supports ([string, ...])
    - list of board and sensor types, including any or all from ["ARM", "FX2", "InGaAs", "Silicon"])
- Uses ([string, ...])
    - a list of any or all of the following: ["wValue", "wIndex"] which the user 
      should be allowed to enter as parameters
    - default: ["wValue"] 
    - only relevant in host-to-device
    - e.g. GET\_MODEL\_CONFIG, SET\_INTEGRATION\_TIME
- ARMInvertedReturn (bool)
    - a boolean, true if expectded USB control transfer success value is 
      inverted on ARM architectures (; default false
- FakeBufferLength (int)
    - send the given buffer length to the device in the control transfer packet, 
      even though no actual data is being sent (e.g., SET_LASER_MOD_ENABLED)
- MakeFakeBufferFromValue (bool)
    - generate a buffer of nulls whose length matches the wValue argument
    - e.g., SET\_MOD\_PERIOD, SET\_LASER\_MOD\_PULSE\_WIDTH
- Reverse (bool)
    - reverse the ordering of the received bytes
    - only relevant in device-to-host
    - e.g. GET\_CCD\_TEMP, GET\_CODE\_REVISION
- Enabled (bool)
    - whether the API command is supported by current firmware
- Units (string)
    - unit of the relevant value
    - e.g. SET\_INTEGRATION\_TIME, GET\_TRIGGER\_DELAY etc
- Notes (string)
    - comments or description of the command, arguments and return value

These are only for ACQUIRE\_CCD, and as such may be removed from the API:

- ReadEndpoint
    - read bulk data from this endpoint
- ReadBlockSize 
    - recommended block size for reading bulk data

## Type

These are the supported datatypes:

- Bool
    - wValue 0 or 1
- Byte\[\] 
    - raw array of bytes
- Enum 
    - requires an "Enum" attribute with a list of zero-indexed enumeration labels
- Float16
    - a custom half-precision floating-point type in which the MSB represents 
      the integral value, and LSB holds the fractional value
    - e.g. SET\_CCD\_GAIN
- String
    - character data
- Uint8
    - a single byte, e.g. VR\_GET\_NUM\_FRAMES
- Uint12
    - 12-bit DAC or ADC value, e.g. SET\_CCD\_TEMP\_SETPOINT
- Uint16
- Uint16\[\]
    - pixel data
- Uint24 
    - e.g. SET\_TRIGGER\_DELAY
- Uint32
- Uint40
    - longer integers used for laser timing which span wValue, wIndex and 
      finally MSB in an extra payload byte
    - e.g. GET\_LASER\_MOD\_DURATION

# History

- 2018-03-28 0.1.0
	- initial draft
