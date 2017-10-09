# Overview

Wasatch.NET is an application-level wrapper over Wasatch Photonics' USB 
API. It is provided so that application developers don't need to worry about
opcodes and [de]marshalling octets of raw data; they can simply call high-level
properties and methods like integrationTimeMS and getSpectrum().

Wasatch.NET is expected to work from all .NET-compatible languages, including:

- C#
- LabVIEW (tested with 2017)
- MATLAB/Simulink
- Embarcadero Delphi
- Xamarin
- Visual Basic.NET
- VBA (Excel)
- F#

If there are others you would like to see listed, please let us know and we'll 
test them!

# Quick Start

API docs: http://www.wasatchphotonics.com/api/Wasatch.NET/

\code{.cs}

    WasatchNET.Driver driver = WasatchNET.Driver.getInstance();
    if (driver.openAllSpectrometers() > 0)
    {
        WasatchNET.Spectrometer spectrometer = driver.getSpectrometer(0);
        spectrometer.integrationTimeMS = 100;
        double[] spectrum = spectrometer.getSpectrum();
    }

\endcode

Key classes:

- WasatchNET.Driver 
- WasatchNET.Spectrometer
- WasatchNET.Util (post-acquisition spectral processing)

The driver is designed to closely mimic the USB APIs defined in the following 
documents:

- http://wasatchphotonics.com/eng-0001/ (USB API)
- http://wasatchphotonics.com/eng-0034/ (FID API)
- http://wasatchphotonics.com/oem-api-specification/ (SPI API)

Therefore, most questions about parameters, modes and options can likely be
resolved by review of the underlying spectrometer USB interface.

# Dependencies

The driver was written and tested under Visual Studio 2017 Community on Win10 
64-bit. It uses the following external components:

## LibUsbDotNet

URL: https://sourceforge.net/projects/libusbdotnet/files/LibUsbDotNet/

This is a .NET wrapper over the standard libusb-win32 which is used by many
USB device vendors.

The pre-compiled DLL provided in our lib/ directory was built from v2.2.8 using
Visual Studio 2017 Community against the .NET 4.0 Client Profile.

## .INF file installation

Wasatch.NET is a high-level "application driver," which communicates with our
spectrometers using the "mid-level" driver LibUsbDotNet, which in-turn 
communicates using the "low-level" USB driver libusb-win32.  However, Windows 
doesn't *know* that our spectrometers are meant to use libusb-win32 until we tell
it!  

So the first thing we need to do is install the .INF files which associate our 
USB devices (via VID/PID) with libusb.  This is the process to do so:

1. Plug in a Wasatch Photonics spectrometer. (Go and buy one from our website
   if you haven't already done so!)
2. Windows should prompt you to "locate drivers for this device."
3. Browse to C:\Program Files (x86)\Wasatch Photonics\Wasatch.NET (or wherever
   you've installed our driver), then select "libusb\_drivers".
4. Ensure that "include subfolders" is checked, then click "Ok."
<a href="https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-01-open-device-manager.png"><img src="https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-01-open-device-manager.png" width="20%" height="20%" align="right" style="clear:both"/></a>
5. After the driver has installed, go to Device Manager (just type "Device Manager"
   into the Win10 Cortana search field on the Start Menu), and check that your
   spectrometer appears under "libusb-win32 devices".
<a href="https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-02-device-manager.png"><img src="https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-02-device-manager.png" width="20%" height="20%" align="right" style="clear:both"/></a>

### Alternative: install Enlighten

Another way to associate Wasatch Photonic's VID/PID tuples with libusb-win32 is 
simply to install Enlighten, which automatically configures the low-level USB 
driver bindings so that your spectrometer will be properly enumerated by 
LibUsbDotNet and therefore visible in Wasatch.NET.

# Build Configuration

Our standard DLL is built against .NET 4.0 Client Profile, with debugging 
enabled, "For Any CPU".  The WinFormDemo is built against .NET 4.6.1, because 
why not.  The Setup installer is configured for x86, which works fine on x64 as 
well.

Users are welcome to rebuild the library in other configurations; please let us 
know if you encounter any issues which we may help resolve.

# Installation

Wasatch.NET is distributed in a Microsoft Installer (.msi) file, which installs
both WasatchNET.dll and accompanying WinFormDemo under 
\\Program Files (x86)\\Wasatch Photonics.  The DLL, along with the dependency
LibUsbDotNet.dll, are both in WinFormDemo\\lib, giving you both the library and
a means to verify its successful operation in a vendor-supplied test harness.

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

# Backlog

- [ ] confirm algorithm to convert raw ADC to degrees C
- [ ] add EEPROM write to WinFormDemo
- [ ] test with multiple parallel spectrometers
- [ ] test on more hardware (NIR)
- [ ] refactor USB calls into Bus abstraction for Bluetooth/Ethernet

## Complete

- [x] cover all API functions
- [x] perform a sweep for boardType incompatibilities in API calls
- [x] Doxygen rendered API documentation
- [x] support writing EEPROM
- [x] implement "Save" in demo
- [x] expand GET\_MODEL\_CONFIG to include new pages from OEM API
- [x] post to GitHub

# Version History

- 2017-09-29 initial GitHub release (alpha)
- 2017-09-25 initial creation
