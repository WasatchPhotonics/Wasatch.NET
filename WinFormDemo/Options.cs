using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WasatchNET;

namespace WinFormDemo
{
    public class Options
    {
        public string saveDir = "";
        public uint scanCount;
        public uint scanIntervalSec;
        public bool autoSave;
        public bool autoStart;
        public uint integrationTimeMS;
        public bool shutdown;

        Logger logger = Logger.getInstance();

        public Options(String[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToUpper();
                if (arg == "--HELP")
                    usage();
                else if (arg == "--SAVEDIR")
                    saveDir = args[++i];
                else if (arg == "--SCANCOUNT")
                    scanCount = Convert.ToUInt32(args[++i]);
                else if (arg == "--SCANINTERVALSEC")
                    scanIntervalSec = Convert.ToUInt32(args[++i]);
                else if (arg == "--AUTOSAVE")
                    autoSave = true;
                else if (arg == "--AUTOSTART")
                    autoStart = true;
                else if (arg == "--INTEGRATIONTIMEMS")
                    integrationTimeMS = Convert.ToUInt32(args[++i]);
                else
                {
                    logger.error("Unsupported argument: {0}", arg);
                    usage();
                }
            }
        }

        public void usage()
        {
            Console.WriteLine("WinFormDemo, an open-source C# spectroscopy demo distributed with Wasatch.NET");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("--autoStart         automatically initialize and open the first Wasatch spectrometer");
            Console.WriteLine("--autoSave          automatically save every spectrum to the specified location");
            Console.WriteLine("--saveDir           path to save spectra (must exist and be writable)");
            Console.WriteLine("--scanCount         how many spectra to acquire and optionally save before exiting");
            Console.WriteLine("--scanIntervalSec   how long to wait between acquisitions");
            Console.WriteLine("--integrationTimeMS integration time in milliseconds");

            shutdown = true;
        }
    }
}
