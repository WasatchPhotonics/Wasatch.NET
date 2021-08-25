using System;
using WasatchNET;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace MacCMDDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            UsbRegDeviceList deviceRegistries = UsbDevice.AllDevices;

            Driver driver = Driver.getInstance();
            driver.openAllSpectrometers();
            if (driver.getNumberOfSpectrometers() > 0)
            {
                Spectrometer spectrometer = driver.getSpectrometer(0);
                double[] data = spectrometer.getSpectrum();
                foreach (double piece in data)
                {
                    Console.WriteLine(piece.ToString("f0"));
                }
            }

            //Console.WriteLine("Hello World!");
        }
    }
}
