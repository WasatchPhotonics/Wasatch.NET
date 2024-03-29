# Overview

Wasatch.NET is an application-level wrapper over Wasatch Photonics' USB and [SPI](README-SPI.md) 
APIs. It is provided so that application developers don't need to worry about
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
- http://wasatchphotonics.com/eng-0034/ (USB EEPROM structure)
- http://wasatchphotonics.com/eng-0072/ (SPI API)

Therefore, most questions about parameters, modes and options can likely be
resolved by review of the underlying spectrometer communication interface.

# Driver Completeness

Wasatch Photonics application drivers are provided as *reference implementations* 
to demonstrate how to command and control our spectrometers over USB from a 
variety of platforms and languages. As working examples and "convenience wrappers" 
over our USB API, they are *not* guaranteed to include convenience functions for 
every call and option within the hardware API, *nor* are they necessarily the 
most efficient or optimal implementation in any given language.

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

# Installation

Wasatch.NET is distributed in a Microsoft Installer (.msi) file, which installs
the WasatchNET.dll and LibUsbDotNet.dll under C:\\Windows (so they'll always be
in the system path).  FTDI drivers are also installed to support SPI communications.

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

## Andor Driver Pack

To use XL-Series spectrometers with the Andor camera system, Wasatch.NET requires
the Andor Driver Pack 2 be installed.  This is currently available from Andor at
the following link:

- https://andor.oxinst.com/downloads/view/andor-driver-pack-2.104.30065.0-(ccd,iccd-emccd)

## FTDI

See [README-SPI.md](README-SPI.md).

## Build Configuration

Our standard DLL is built against .NET 4.8 Client Profile against the Debug 
target, so the DLL will have the maximum amount of debugging symbols and metadata
for user troubleshooting. The WinFormDemo is built against .NET 4.8.

The standard and recommended build configuration is x64, but we also distribute
installers for x86 (Win32), as for instance many users are have 32-bit versions 
of LabVIEW even on 64-bit operating systems.

By user request we include an "AnyCPU" installer as well, but this configuration
is missing some functionality (e.g. drivers for Andor / XL spectrometers).

Users are welcome to build the library themselves against any target 
configuration or architecture; please let us know if you encounter any issues 
which we can help resolve.

### .NET Framework 4.0

If you still need to build against .NET Framework 4.0, checkout the "framework40"
branch and build using Visual Studio 2019 Community Edition.

# Testing

The simplest way to test whether your installation is successful is to run the
provided WinFormDemo, which should be available on your Start Menu under
Wasatch Photonics -> Wasatch.NET -> WinFormDemo.

If you have a Wasatch Photonics spectrometer plugged-in and correctly showing
under "libusb-win32 devices" in the Device Manager, you should be able to run
the demo, then click "Initialize" to connect to the spectrometer.  

# Logging

Although the library lets applications configure logging programmatically via
Driver.Logger.level and .setPathname(), not all applications do so.  End-users
of compiled applications can still configure logging manually by setting these
environment variables before running a program using Wasatch.NET:

    C:> set WASATCHNET_LOGGER_PATHNAME=C:\temp\wasatchnet.log  (assumes directory exists)
    C:> set WASATCHNET_LOGGER_LEVEL=DEBUG  (can be DEBUG, INFO, ERROR or NEVER)

These environment "defaults" can still be overridden by application code which
explicitly calls the above methods and properties, however.

# Digital Signatures

At least one client language (LabVIEW NXG) only supports .NET assemblies loaded
in the GAC (General Assembly Cache).  In order to be loaded into the GAC, an
assembly has to be "strongly named" (digitally signed)...along with its direct
dependencies (3rd-party DLLs like LibUsbDotNet).

For information on digitally signing Wasatch.NET assemblies for GAC support,
Wasatch maintainers should reference "Admin/Keys/Wasatch.NET".

# Multi-Channel Operation

- see [MultiChannel](README_MULTICHANNEL.md)

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

- [ ] change installer projects to WiX so they can auto-populate the GAC?
- [ ] refactor USB calls into Bus abstraction for Bluetooth/Ethernet

# Version History

- see [Changelog](README_CHANGELOG.md)

# Support

For questions about the driver or API, please contact:

    support@wasatchphotonics.com

