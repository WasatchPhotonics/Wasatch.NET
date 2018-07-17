# Firmware Updates

## FX2 Firmware

The process to update FX2 firmware with an .iic file and Cypress's CyConsole 
application is detailed in Wasatch Photonics 
[PRD-0105](https://drive.google.com/file/d/0B1eC7P4MQiFiTjdGR3VyclBndHc/view).  
However, that document skims-over the process of unassociating the spectrometer
from libusb-win32 drivers and reverting back to the desired Cypress EZ-USB 
drivers.  Here's the short-form on how to do that.

Download the Cypress EZ-USB drivers from here: https://community.cypress.com/docs/DOC-12366

Use the Device Manager to "Uninstall" your libusb-win32 spectrometer.  Make sure
you tick the box to "Uninstall" the old driver.  This will leave the device as a
raw "Stroker FX2".

Use Device Manager to "Update" your driver.
![Update Drivers](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-01-update.png)

Choose to "Browse" your PC.
![Browse](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-02-browse.png)

Instead of browsing your filesystem, "Let me pick" from the list of available drivers.
![Pick](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-03-pick.png)

Tell Windows to show you "All" common hardware types.
![All](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-04-all.png)

Even if Cypress is listed as a manufacturer, go ahead and choose "Have Disk".
![Disk](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-05-disk.png)

Browse wherever you unpacked the Cypress drivers downloaded from cypress.com, 
then select the appropriate .inf (e.g. Drivers/Win10/x64/cyusb3.inf).
![INF](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-06-inf.png)

Pick the approrpriate Cypress FX2 model (e.g. FX2LP Development board).
![FX2](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-07-fx2.png)

Ignore the "Update Driver Warning" (click Yes).
![Warning](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-08-warning.png)

There you go!  
![Success](https://github.com/WasatchPhotonics/Wasatch.NET/raw/master/screenshots/ezusb-09-success.png)

You should now be able to run CyConsole and upload your .iic file using the 
EZ-USB interface and the "lg EEPROM" button.

(Remember that a full power-cycle (12V, not just USB re-enumeration) is required
following a firmware update.)
