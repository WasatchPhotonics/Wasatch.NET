# Overview

This is a simple C# GUI application written in Visual Studio (2017 Community) 
demonstrating how to command and query Wasatch Photonics spectrometers using our
open-source Wasatch.NET application driver.

# Command-Line Arguments

The following optional command-line arguments are supported:

--autoSave          automatically save every spectrum to the specified location
--autoStart         automatically initialize and open the first Wasatch spectrometer
--help              this page
--integrationTimeMS integration time in milliseconds
--saveDir           path to save spectra (must exist and be writable)
--scanCount         how many spectra to acquire and optionally save before exiting
--scanIntervalSec   how long to wait between acquisitions
