# Overview

Wasatch.NET is an application-level wrapper over Wasatch Photonics' USB 
API. It is provided so that application developers don't need to worry about
opcodes and [de]marshalling octets of raw data; they can simply call high-level
properties and methods like integrationTimeMS and getSpectrum().

Wasatch.NET is expected to work from all .NET-compatible languages, including:

| Platform              | Test status
| --------------------- | ----------------------------------------
| C#                    | tested with Visual Studio 2017 Community 
| LabVIEW               | tested with 2017 32-bit (see [Wasatch.LV](https://github.com/WasatchPhotonics/Wasatch.LV/))
| MATLAB/Simulink       | tested with 2017b 64-bit (see [Wasatch.MATLAB](https://github.com/WasatchPhotonics/Wasatch.MATLAB/))
| Embarcadero Delphi    | tested with Delphi Community Edition 10.2 over COM (see [Wasatch.Delphi](https://github.com/WasatchPhotonics/Wasatch.Delphi/))
| VBA (Excel)           | tested with Office 2010 64-bit (see [Wasatch.Excel](https://github.com/WasatchPhotonics/Wasatch.Excel/))
| R                     | not started (planned via [rClr](https://rclr.codeplex.com/))
| Xamarin               | not started
| Visual Basic.NET      | not started
| F#                    | not started
| Wolfram Mathematica   | not started

If there are others you would like to see listed, please let us know and we'll 
test them!

# Downloads

Pre-compiled installers are available for 32-bit and 64-bit Windows:

- http://wasatchphotonics.com/binaries/drivers/Wasatch.NET/

# Quick Start

\code{.cs}

    WasatchNET.Driver driver = WasatchNET.Driver.getInstance();
    if (driver.openAllSpectrometers() > 0)
    {
        WasatchNET.Spectrometer spectrometer = driver.getSpectrometer(0);
        spectrometer.integrationTimeMS = 100;
        double[] spectrum = spectrometer.getSpectrum();
    }

\endcode

For sample calling code, see the included C# [WinFormDemo](./doc/README-WinFormDemo.md).

# Documentation

API documentation is available here:

- http://wasatchphotonics.com/api/Wasatch.NET/

The driver is designed to closely mimic the USB APIs defined in the following 
documents:

- http://wasatchphotonics.com/eng-0001/ (USB API)
- http://wasatchphotonics.com/eng-0034/ (FID API)
- http://wasatchphotonics.com/oem-api-specification/ (SPI API)

Therefore, most questions about parameters, modes and options can likely be
resolved by review of the underlying spectrometer USB interface.

# Installation

Wasatch.NET is distributed in a Microsoft Installer (.msi) file, which installs
the WasatchNET.dll and LibUsbDotNet.dll under C:\\Windows (so they'll always be
in the system path).  

It also installs a simple C# spectroscopy GUI app, WinFormDemo, under 
\\Program Files\\Wasatch Photonics (or Program Files (x86) on 32-bit systems), 
giving you a means to control the spectrometer and quickly verify the driver is 
installed and working correctly.

Besides double-clicking the .msi installer, there are one or two additional steps
required for a complete installation:

## Post-Install Step #1: libusb drivers

Wasatch.NET is a high-level "application driver," which communicates with our
spectrometers using the "mid-level" driver LibUsbDotNet, which in-turn 
communicates using the "low-level" USB driver libusb-win32.  However, Windows 
doesn't *know* that our spectrometers are meant to use libusb-win32 until we tell
it!  

So the first thing we need to do is install the .INF files which associate our 
USB devices (via VID/PID) with libusb.  This is the process to do so:

- Plug in a USB Wasatch Photonics spectrometer.

- Windows may prompt you to "locate drivers for this device".  If not, go to the
  Device Manager (just type "Device Manager" into the Win10 search field on the
  Start Bar).

- Your spectrometer(s) should appear as "Stroker FX2" or "Stroker InGaS Camera" under "Other devices".

![Update Drivers](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-02-update-drivers.png)

- Right-click on the Stroker entry and select "Update Driver".

- Select "Manually browse for drivers" .

![Browse](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-03-browse.png)

- Ensure "[x] include subfolders" is checked

- Browse to "C:\Program Files\Wasatch Photonics\Wasatch.NET\libusb\_drivers" or 
  "C:\Program Files (x86)\Wasatch Photonics\Wasatch.NET\libusb\_drivers" as appropriate.

![Select](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-04-select.png)

- When prompted to confirm whether you wish to install the libusb drivers, click "Install."

![Install](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-05-install.png)

- Confirm that your spectrometer now appears under "libusb-win32 devices".

![Done](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-06-done.png)

## Post-Install Step #2: COM registration (optional)

This step is believed ONLY required for developers using Visual Basic 6 (VB6)
or Visual Basic for Applications (VBA, part of Microsoft Excel).

Because our .msi installer does not register the .tlb file needed by VB6/VBA, 
you need to perform one additional manual step:

- Navigate to \\Program Files\\Wasatch Photonics\\Wasatch.NET (or Program Files
  (x86) on 32-bit systems)

- Right-click the batch file "RegisterDLL.bat", and select "Run as Administrator"

![Run as Administrator](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/register-01-administrator.png)

- Confirm no errors appear in the result

![Done](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/register-02-done.png)

# Build 

So you'd like to build and compile Wasatch.NET yourself from source?  Good,
that's how we like to do it too...you learn so much more that way :-)

## Dependencies

The driver was written and tested under 
[Visual Studio 2017 Community](https://www.visualstudio.com/vs/community/) on 
Win10 64-bit. It is itself dependent on the following libraries:

## LibUsbDotNet

URL: https://sourceforge.net/projects/libusbdotnet/files/LibUsbDotNet/

This is a .NET wrapper over the standard libusb-win32 which is used by many
USB device vendors.  The pre-compiled DLL provided in our lib/ directory was 
built from v2.2.8 using Visual Studio 2017 Community against the .NET 4.0 Client
Profile.

## Build Configuration

Our standard DLL is built against .NET 4.0 Client Profile with debugging enabled,
and the WinFormDemo is built against .NET 4.6.1. 

Although the DLL and demo will build and run "For Any CPU", we went ahead and made
build configurations for x64 and x86 because some client platforms prefer binding
to specific architectures.

Users are welcome to build the library in other configurations; please let us 
know if you encounter any issues which we may help resolve.

# Testing

The simplest way to test whether your installation is successful is to run the
provided WinFormDemo, which should be available on your Start Menu under
Wasatch Photonics -> Wasatch.NET -> WinFormDemo.

If you have a Wasatch Photonics spectrometer plugged-in and correctly showing
under "libusb-win32 devices" in the Device Manager, you should be able to run
the demo, then click "Initialize" to connect to the spectrometer.  

# Support

For questions about the driver or API, please contact:

    support@wasatchphotonics.com

# Wrapper Completeness

The Wasatch.Driver series of wrappers over our USB API are provided as
*reference implementations* to demonstrate how to command and control our 
spectrometers over USB from a variety of platforms and languages. As working
examples and "convenience wrappers," they are *not* guaranteed to include
convenience functions for every call and option within the hardware API,
*nor* are they necessarily the most efficient or optimal implementation in any
given language.

The formal and complete interface to our spectrometers is provided in our USB
API documentation. Standard USB drivers to access that direct interface 
are plentiful on all standard operating systems: libusb, WinUSB etc. No 
additional wrappers or libraries are required to make full use of our 
spectrometers from the platform of your choice.

If there is a spectrometer or spectroscopy function that you do not find
provided in our open-source wrapper collection, please contact us and request
its addition; or if you wish to "get your hands dirty," feel free to create
your own implementation and optionally share it with us for merge into the
base distribution. Wasatch Photonics is proud to help support our online 
community, but not too proud to decline patches when they improve the product!

That said, some known areas for improvement can be found in our Backlog 
(below).

# Troubleshooting

## "The best driver is already installed."

If you have trouble installing our libusb-win32 drivers, see: 

- [README-CyUsb3.md](./doc/README-CyUsb3.md)

## No spectrometer found

This can happen when Wasatch Dash or another older Wasatch driver product
has been installed on the same computer as ENLIGHTEN or one of our newer
drivers (like Wasatch.NET).

You can tell that this is the problem if you look in the Windows Device
Manager, and you can see "Wasatch Photonics Device FX2" listed under
"Universal Serial Bus controllers".

To resolve, first uninstall the older driver, by right-clicking
"Wasatch Photonics Device FX2" and selecting "Uninstall device":

![Uninstall Device](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/uninstall-01.png)

Make sure you click "Delete the driver software for this device":

![Delete the driver software](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/uninstall-02.png)

From the "Action" menu, select "Scan for hardware changes" to re-enumerate the 
device under the correct device driver:

![Scan for hardware changes](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/uninstall-03.png)

You should now see your spectrometer listed under the expected libusb-win32
driver:

![Scan for hardware changes](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/uninstall-04.png)

You should now be able to re-run your Wasatch.NET, Wasatch.PY or ENLIGHTEN
software and connect to your spectrometer.

# Backlog

- [ ] refactor USB calls into Bus abstraction for Bluetooth/Ethernet

# Version History

- TODO: add cancelAcquisition()?

- 2019-07-29 2.0.20
    - remove fakeBuf from laser pulse width/period on FX2
- 2019-07-29 2.0.19
    - added laserPowerResolution
- 2019-07-16 2.0.18
    - automatically apply gain and offset from EEPROM if configured
    - ignore "odd" gain/offset for non-InGaAs
- 2019-06-14 2.0.17
    - changed SET\_MODEL\_CONFIG to 2nd-tier command
- 2019-05-29 2.0.16
    - added productConfiguration to EEPROM
    - moved min/maxIntegrationTimeMS from ushort to uint
- 2019-05-10 2.0.15
    - changed laserWavelengthNM() to writable property
    - changed hasLaser to only rely on EEPROM
    - changed WinFormDemo's numericUpDownIntegTimeMS.Maximum to 5min
- 2019-04-26 2.0.14
    - added (get/set)Detector(Gain/Offset)Odd
    - initial test support for areaScanEnable
    - disabled laserRampEnable pending redesign
    - documentation updates
- 2019-04-01 2.0.13
    - added new ENG-0034 Rev 4 fields to EEPROM.read() and .write().
    - TODO: add new -odd opcode implementations
- 2019-03-28 2.0.12
    - removed enforcement of EEPROM maxIntegrationTimeMS to allow longer at-risk integrations
    - added correctBadPixels
- 2019-02-01 2.0.11 (internal)
    - updated laser temperature degC formula from datasheet
- 2019-01-22 2.0.10
    - fixes for reading/setting laserTemperatureSetpoint
    - exposed VID/PID through API
    - improved support for floating-point excitation wavelength
- 2019-01-22 2.0.9
    - precompute endpoints / bulk sizes
    - triggering fixes for LabVIEW
- 2019-01-22 2.0.8
    - interlock fix on ARM
    - detector offset fix on ARM
- 2019-01-04 2.0.7
    - added isSiG
    - disable laser and detector temperature readouts on SiG
    - disable TEC enable and TEC setpoint unless eeprom.hasCooling
    - fixed setIntegrationTime on ARM
- 2019-01-04 2.0.6
    - added batteryPercentage and batteryCharging
    - secondaryADC cleanup
    - added "make cloc"
- 2018-12-19 2.0.5
    - updated to latest draft Rev 4 of ENG-0034
    - renamed ModelConfig to EEPROM
    - extended full EEPROM write privileges
- 2018-12-14 2.0.4
    - added min/maxLaserPowerMW, laserPowerCoeffs
- 2018-12-10 2.0.3
    - improved logging
- 2018-12-04 2.0.2
    - adding support for 2048-pixel detectors
- 2018-09-25 2.0.0
    - refactored to better use COM / .NET Properties
    - tested ARM triggering
- 2018-08-03 1.0.28
    - auto-set integration time limits
- 2018-07-20 1.0.27
    - removed Generics from COM API for Delphi
- 2018-05-29 1.0.26
    - changed ModelConfig.detectorTempMin/Max from float to short
- 2018-05-28 1.0.25
    - added IDriver.logger getter
- 2018-05-28 1.0.25
    - added IDriver.logger getter
- 2018-05-27 1.0.24
    - added ISpectrometer.getSpectrum()
- 2018-05-25 1.0.23
    - added IDriverVBAWrapper.getSingleton()
- 2018-05-25 1.0.22
    - More COM Interface classes
- 2018-05-24 1.0.21
    - COM annotations
- 2018-05-23 1.0.20
    - split RegisterDLL.bat into RegisterDLL32 and RegisterDLL64
- 2018-04-24 1.0.19
    - changed setCCDTriggerSource to Int40
- 2018-03-30 1.0.18
    - added APITest
    - improved ARM support for READ\_COMPILATION\_OPTIONS, ACQUIRE\_CCD, others
- 2018-03-22 1.0.17 
    - snapshot before testing with MonoLibUsb
- 2018-03-13 1.0.16 
    - ARM testing
- 2018-03-06 1.0.15 
    - internal test build
- 2018-01-10 1.0.14 
    - added detector TEC setpoint control to WinFormDemo
    - fixed dependency path in Setup64
- 2017-11-28 1.0.13 set initial detector TEC setpoint; 
    - added setHighGainModeEnabled
- 2017-11-28 1.0.12 
    - updated laser temperature readout
- 2017-10-26 1.0.11 
    - added setLaserPowerPercentage()
- 2017-10-25 1.0.10 
    - added setDFUMode() for ARM reflash
- 2017-10-25 1.0.9  
    - fixed spectrum-save issue
- 2017-10-24 1.0.8  
    - corrected detector temperature computation
    - resolved some multi-spectrometer syncronization issues
- 2017-10-23 1.0.7  
    - tweaks after multi-spectrometer testing with 1064-L
- 2017-10-22 1.0.6  
    - added cmd-line args to WinFormDemo
- 2017-10-11 1.0.5  
    - fixed temperature read-out in degrees
- 2017-10-11 1.0.4  
    - fixed and restored ModelConfig.write 
- 2017-10-11 1.0.3  
    - disabled ModelConfig.write due to corruption
- 2017-10-10 1.0.2  
    - separate 32/64-bit installers
- 2017-09-29 1.0.1  
    - initial GitHub release (alpha)
