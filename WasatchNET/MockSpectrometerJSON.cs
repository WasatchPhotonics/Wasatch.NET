using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    class MockSpectrometerJSON
    {
        public Dictionary<string, SortedDictionary<int, double[]>> measurements;
        public EEPROMJSON EEPROM;
    }

}
