using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class FRAM
    {
        internal const int FRAM_LIB_START = 7512;
        internal const int FRAM_LIB_END = 8000;
        internal const ushort LIB_PAGE_SIZE = 61;
        public event EventHandler FRAMChanged;
        protected Logger logger = Logger.getInstance();
        protected Spectrometer spectrometer;
        public byte librarySpectraNum
        {
            get { return _librarySpectraNum; }
            set
            {
                _librarySpectraNum = value;
                FRAMChanged?.Invoke(this, new EventArgs());
            }
        }
        byte _librarySpectraNum = 1;

        public List<UInt16> librarySpectrum
        {
            get { return _librarySpectrum; }
            set
            {
                _librarySpectrum = value;
                FRAMChanged?.Invoke(this, new EventArgs());
            }
        }
        List<UInt16> _librarySpectrum;

        public List<byte[]> pages { get; protected set; }

        public FRAM(Spectrometer spec)
        {
            spectrometer = spec;
            pages = new List<byte[]>();
        }

        public bool write()
        {
            writeParse();
            int lib_num;
            int page_num;
            UInt16 combined_val;
            byte[] send_buf = { 1 };
            bool ok;

            for (int page = 0; page < pages.Count; page++)
            {
                lib_num = page / LIB_PAGE_SIZE;
                page_num = page % LIB_PAGE_SIZE;
                combined_val = Convert.ToUInt16(lib_num << 8 | page_num);
                ok = spectrometer.sendCmd(
                        opcode: Opcodes.SECOND_TIER_COMMAND,
                        wValue: spectrometer.cmd[Opcodes.WRITE_LIBRARY],
                        wIndex: combined_val,
                        buf: pages[page]);
            }
            ok = spectrometer.sendCmd(
                opcode: Opcodes.SECOND_TIER_COMMAND,
                wValue: spectrometer.cmd[Opcodes.PROCESS_LIBRARY],
                wIndex: 0,
                buf: send_buf);
            return true;
        }

        public bool writeParse()
        {
            int pixel = 0;
            while (pixel < librarySpectrum.Count)
            {
                int page = pixel / 32;
                int offset = 2 * (pixel % 32);

                // fill-in any pages that weren't loaded/created at start
                // (e.g. if a different subformat had been in effect)
                while (page >= pages.Count)
                {
                    logger.debug("appending new page {0}", pages.Count);
                    pages.Add(new byte[64]);
                }

                // logger.debug("writeLibrary: writing pixel {0,4} to page {1,4} offset {2,4} value {3,6}", 
                //     pixel, page, offset, librarySpectrum[pixel]);
                if (!ParseData.writeUInt16(librarySpectrum[pixel], pages[page], offset))
                {
                    logger.error("failed to write librarySpectrum pixel {0} page {1} offset {2}",
                        pixel, page, offset);
                    return false;
                }
                pixel++;
            }
            return true;
        }

        public bool read()
        {
            librarySpectrum = new List<ushort>();
            for (ushort page = FRAM_LIB_START; page < FRAM_LIB_END; page++)
            {
                byte[] buf = spectrometer.getStorage(page);
                if (buf is null)
                {
                    return true;
                }
                pages.Add(buf);
                logger.hexdump(buf, String.Format("read page {0}: ", page));
            }
            for (int page = 0; page < pages.Count; page++)
            {
                for (int pagePixel = 0; pagePixel < 32; pagePixel++)
                {
                    UInt16 lsb = pages[page][pagePixel * 2];
                    UInt16 msb = pages[page][pagePixel * 2 + 1];
                    UInt16 intensity = (UInt16)((msb << 8) | lsb);
                    librarySpectrum.Add(intensity);
                }
            }
            return true;
        }
    }
}
