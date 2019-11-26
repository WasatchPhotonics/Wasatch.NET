# Overview

This document is a specification for how we use USB bridges to interface computers with
Spectrometer board configurations that are otherwise designed to "speak" SPI.

In short, our production software expects a very specific setup in order to communicate
with SPI spectrometers.

If different SPI setups are required, please contact us and we can coordinate making the connection
process more flexible.

#Requirements

To use our software with SPI spectrometers you will need

1) Adafruit FTD232H breakout comms board (https://www.adafruit.com/product/2264) (other boards with the same FTDI chip will likely work but are not tested)

2) FTD2XX_NET (https://www.ftdichip.com/Support/SoftwareExamples/CodeExamples/CSharp/FTD2XX_NET_v1.1.0.zip)
	and
   MPSEELight (https://github.com/zhelnio/MPSSELight)
   Libraries, both freely distributed and included with Wasatch.NET
   
#Board Wiring (Production)

Our software expects the FTD232H board to be wired for SPI as follows
C0: clock
C1: MOSI
C2: MISO
C3: enable

Our software also currently performs a software based external trigger with the following wire setup
D0: trigger
D1: data ready