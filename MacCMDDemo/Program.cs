using System;
using WasatchNET;

namespace MacCMDDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Driver driver = Driver.getInstance();
            driver.openAllSpectrometers();
            Console.WriteLine("Hello World!");
        }
    }
}
