# Overview

This document is to describe what an HOCT spectrometer is and why it is different from
a standard spectrometer.

The HOCT is the first spectrometer from WP's OCT division to be supported by WasatchNET,
which uses a different electronic setup than most of our OCT units. This support allows
our production team to perform automated tests in our calibration software.

Due to some unknown firmware quirks (ask an EE) HOCT units tend to only run properly when 
spectra are continuously collected, so unline our standard software driver, this one
continuously asynchronously collects "frames," 2D matrices of spectral rows, or "lines."
Then when a program calls getSpectrum or getFrame, the most recent frame is sampled and
sent out.

The unit's detector has 2048 pixels per row, but only 1024 receive light. As of this writing
we statically set 200 lines per frame, set 2048 pixels for hardware output, throw out the second
half of pixels for getSpectrum() and give full frames for getFrame().

The integration time for this unit is also not in milliseconds, or microseconds, see code notes for 
more details.

# Requirements

We require this spectrometer to have the "OCT is HS Mode" libusb drivers installed. Please
contact Wasatch Photonics if you need these and do not have them.