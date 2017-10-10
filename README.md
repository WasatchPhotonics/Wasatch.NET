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

# Build Configuration

Our standard DLL is built against .NET 4.0 Client Profile with debugging enabled,
and the WinFormDemo is built against .NET 4.6.1. 

Although the DLL and demo will build and run "For Any CPU", we went ahead and made
build configurations for x64 and x86 because some client platforms prefer binding
to specific architectures.

Users are welcome to build the library in other configurations; please let us 
know if you encounter any issues which we may help resolve.

# Installation

Wasatch.NET is distributed in a Microsoft Installer (.msi) file, which installs
the WasatchNET.dll and LibUsbDotNet.dll under C:\\Windows (so they'll always be
in the system path).  It also installs a simple C# spectroscopy GUI app, 
WinFormDemo, under \\Program Files\\Wasatch Photonics (or Program Files (x86) on
32-bit systems), giving you a means to control the spectrometer and quickly verify
the driver is installed and working.

## Post-Install Step #1: libusb drivers

Wasatch.NET is a high-level "application driver," which communicates with our
spectrometers using the "mid-level" driver LibUsbDotNet, which in-turn 
communicates using the "low-level" USB driver libusb-win32.  However, Windows 
doesn't *know* that our spectrometers are meant to use libusb-win32 until we tell
it!  

So the first thing we need to do is install the .INF files which associate our 
USB devices (via VID/PID) with libusb.  This is the process to do so:

1. Plug in a Wasatch Photonics spectrometer.

2. Windows may prompt you to "locate drivers for this device".  If not, go to the
   Device Manager (just type "Device Manager" into the Win10 search field on the
   Start Bar).
![Open Device Manager](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-01-open-device-manager.png)

3. Your spectrometers should appear as "Stroker FX2" under "Other devices".
![Update Drivers](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-02-update-drivers.png)

4. Right-click on the Stroker FX2 and select "Update Driver".
![Browse](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-03-browse.png)

5. Select "Manually browse for drivers" .

6. Ensure "[x] include subfolders" is checked

7. Browse to "C:\Program Files\Wasatch Photonics\Wasatch.NET\libusb\_drivers" or 
             "C:\Program Files (x86)\Wasatch Photonics\Wasatch.NET\libusb\_drivers" as appropriate.
![Select](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-04-select.png)

8. When prompted to confirm whether you wish to install the libusb drivers, click "Install."
![Install](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-05-install.png)

9. Confirm that your spectrometer now appears under "libusb-win32 devices".
![Done](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/drivers-06-done.png)

## Post-Install Step #2: COM registration (optional)

This step is believed ONLY required for developers using Visual Basic 6 (VB6)
or Visual Basic for Applications (VBA, part of Microsoft Excel).

Because our .msi installer does not register the .tlb file needed by VB6/VBA, 
you need to perform one additional manual step:

1. Navigate to \\Program Files\\Wasatch Photonics\\Wasatch.NET (or Program Files
   (x86) on 32-bit systems)

2. Right-click the batch file "RegisterDLL.bat", and select "Run as Administrator"
![Run as Administrator](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/register-01-administrator.png)

3. Confirm no errors appear in the result
![Success](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/register-02-done.png)

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

# Backlog

- [ ] add EEPROM write to WinFormDemo
- [ ] test with multiple parallel spectrometers
- [ ] test on more hardware (NIR)
- [ ] consider turning all accessors into Properties
- [ ] refactor into LaserController, TECController etc
- [ ] refactor USB calls into Bus abstraction for Bluetooth/Ethernet

# Version History

- 2017-10-10 1.0.2 separate 32/64-bit installers
- 2017-09-29 1.0.1 initial GitHub release (alpha)
- 2017-09-25 1.0.0 initial creation
