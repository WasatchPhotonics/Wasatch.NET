# Overview

Older Wasatch Photonics spectrometers shipped with drivers from Cypress 
Semiconductor (manufacturer of the FX2 USB chip used at the time).  Sometimes
those old drivers are still on computers and need to be removed so you can use
our new open-source, libusb-win32-based drivers.

Fortunately, it's pretty easy to remove the old drivers.

# Process

You know you have this problem when you try to install our libusb-win32 drivers,
but get this error message:

![Best Driver Already Installed](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/cyusb3-01-already-installed.png)

To confirm it, you open the Device Manager and can see that the spectrometer is
attached, but appears under "USB Controllers" instead of "libusb-win32 devices":

![Best Driver Already Installed](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/cyusb3-02-device-manager.png)

In order to permanently delete the old driver, we need to confirm the name of
the INF file which was generated when that old driver was originally installed.
There is no way to predict that filename, and it may vary from computer to 
computer.  

It's easy to find out, though.  From the Device Manager, right-click the
spectrometer and select "Properties".  Select the "Details" tab, and use the
drop-down menu to select "INF Name".  In the example shown, that happened to be
"oem10":

![Best Driver Already Installed](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/cyusb3-02-device-manager.png)

Now open an Administrative Cmd shell.

- type "cmd" in the Start Menu search bar
- right-click on "Command Prompt" and choose "Run as Administrator"

Finally, we'll use Microsoft's standard [PnPUtil](https://docs.microsoft.com/en-us/windows-hardware/drivers/devtest/pnputil)
utility to delete the old driver.  Just type:

    C:\> pnputil /delete-driver oem10 /force

(Where instead of "oem10," you use the name of the INF file identified in the
previous step.)

Finally, right-click on the spectrometer in the Device Manager, and this time
choose "Uninstall", then go to the "Action" menu and select "Scan for Hardware
Changes."

Now your spectrometer should come up as "Unknown Device," and you can follow the 
standard Wasatch.NET driver installation instructions from the README :-)
