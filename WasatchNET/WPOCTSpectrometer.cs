using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WasatchNET.HOCTSpectrometer;

namespace WasatchNET
{
    public class WPOCTSpectrometer : Spectrometer
    {
        internal WPOCTSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            isOCT = true;
            //OctUsb.SetLinesPerFrame(500);
            integrationTimeMS_ = (uint)OctUsb.DefaultIntegrationTime();
        }

    }
}
