# Overview

Wasatch.NET is an application-level wrapper over Wasatch Photonics' USB 
API. It is provided so that application developers don't need to worry about
opcodes and [de]marshalling octets of raw data; they can simply call high-level
properties and methods like integrationTimeMS and getSpectrum().

Wasatch.NET is expected to work from all .NET-compatible languages, including:

- C#
- F#
- Xamarin
- Visual Basic.NET
- Visual Basic for Applications (Excel)
- LabVIEW (tested with 2017)
- Embarcadero Delphi

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

The driver is designed to closely mimic the USB API defined in the following 
documents:

- http://wasatchdevices.com/wp-content/uploads/2017/02/OEM-WP-Raman-USB-Interface-Spec-Rev1\_4.pdf
- http://wasatchdevices.com/wp-content/uploads/2016/08/OEM-API-Specification.pdf

Therefore, most questions about parameters, modes and options can likely be
resolved by review of the underlying spectrometer USB interface.

# Dependencies

The driver was written and tested under Visual Studio 2017 Community on Win10 
64-bit. It uses the following external components:

## LibUsbDotNet

URL: https://sourceforge.net/projects/libusbdotnet/files/LibUsbDotNet/

The pre-compiled DLL provided in our lib/ directory was built from v2.2.8 using
Visual Studio 2017 Community against the .NET 4.0 Client Profile.

## Wasatch Enlighten

Wasatch.NET does not come with the INF or other setup files required to 
associate Wasatch Photonic's VID/PID tuples with libusb-win32.  The simplest
solution for now is to install Enlighten, which automatically configures the 
low-level USB driver bindings.

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
reference implementations to demonstrate how to command and control our 
spectrometers over USB from a variety of platforms and languages. As working
examples and "convenience wrappers," they are not guaranteed to include
convenience functions for every call and option within the hardware API,
nor are they necessarily the most efficient or optimal implementation in any
given language.

The formal and complete interface to our spectrometers is provided in our USB
API documentation, and standard USB drivers to access that direct interface 
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

- [ ] cover all API functions
- [ ] Doxygen rendered API documentation

## Complete

- [x] expand GET\_MODEL\_CONFIG to include new pages from OEM API
- [x] populate more config fields in WinFormDemo
- [x] post to GitHub
- [x] implement "Save" in demo

# Version History

- 2017-10-02 initial Doxygen
- 2017-09-29 initial GitHub release
- 2017-09-25 initial creation

