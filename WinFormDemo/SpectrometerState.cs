using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.ComponentModel;
using WasatchNET;

namespace WinFormDemo
{
    class SpectrometerState
    {
        public enum ProcessingModes { SCOPE, ABSORBANCE, TRANSMISSION };

        public Spectrometer spectrometer;
        public BackgroundWorker worker = new BackgroundWorker() { WorkerSupportsCancellation = true };
        public Series series = new Series();
        public Options opts;

        public double[] raw;
        public double[] spectrum;
        public double[] reference;
        public double detTempDegC;

        public bool running;
        public bool stopping;
        public ProcessingModes processingMode = ProcessingModes.SCOPE;
        public DateTime lastUpdate;
        public uint scanCount;

        Logger logger = Logger.getInstance();

        public SpectrometerState(Spectrometer s, Options options)
        {
            spectrometer = s;
            opts = options;
            series.Name = s.serialNumber;
            series.ChartType = SeriesChartType.Line;
        }

        public void processSpectrum(double[] latest)
        {
            raw = latest;
            lastUpdate = DateTime.Now;

            // note: we are using the dark-corrected versions of these functions
            if (processingMode == ProcessingModes.TRANSMISSION && reference != null && spectrometer.dark != null)
                spectrum = Util.cleanNan(Util.computeTransmission(reference, raw));
            else if (processingMode == ProcessingModes.ABSORBANCE && reference != null && spectrometer.dark != null)
                spectrum = Util.cleanNan(Util.computeAbsorbance(reference, raw));
            else
                spectrum = raw;

            if (opts.autoSave)
                save();

            scanCount++;
        }

        public void save()
        {
            if (opts.saveDir.Length == 0)
                return;

            // More complex implementations could save all spectra from all spectrometers;
            // or include snapped traces; or export directly to multi-tab Excel spreadsheets.

            if (spectrum == null)
                return;

            // assemble the columns we're going to save
            List<Tuple<string, double[]>> cols = new List<Tuple<string, double[]>>();

            cols.Add(new Tuple<string, double[]>("wavelength", spectrometer.wavelengths));

            if (spectrometer.wavenumbers != null)
                cols.Add(new Tuple<string, double[]>("wavenumber", spectrometer.wavenumbers));

            string label = "spectrum";
            if (processingMode == SpectrometerState.ProcessingModes.TRANSMISSION)
                label = "trans/refl";
            else if (processingMode == SpectrometerState.ProcessingModes.ABSORBANCE)
                label = "abs";
            cols.Add(new Tuple<string, double[]>(label, spectrum));

            if (raw != spectrum)
                cols.Add(new Tuple<string, double[]>("raw", raw));

            if (reference != null)
                cols.Add(new Tuple<string, double[]>("reference", reference));

            if (spectrometer.dark != null)
                cols.Add(new Tuple<string, double[]>("dark", spectrometer.dark));

            string filename = String.Format("{0}/{1}-{2}.csv", 
                opts.saveDir, spectrometer.serialNumber, DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(filename))
            {
                // metadata
                outfile.WriteLine("model,{0}", spectrometer.model);
                outfile.WriteLine("serialNumber,{0}", spectrometer.serialNumber);
                outfile.WriteLine("timestamp,{0}", lastUpdate);
                outfile.WriteLine("integration time (ms),{0}", spectrometer.integrationTimeMS);
                outfile.WriteLine("scan averaging,{0}", spectrometer.scanAveraging);
                outfile.WriteLine("boxcar,{0}", spectrometer.boxcarHalfWidth);
                outfile.WriteLine();

                // header row
                outfile.Write("pixel");
                for (int i = 0; i < cols.Count; i++)
                    outfile.Write(",{0}", cols[i].Item1);
                outfile.WriteLine();

                // data
                for (uint pixel = 0; pixel < spectrometer.pixels; pixel++)
                {
                    outfile.Write(String.Format("{0}", pixel));
                    foreach (Tuple<string, double[]> col in cols)
                        outfile.Write(String.Format(",{0}", col.Item2[pixel]));
                    outfile.WriteLine();
                }                        
            }
        }
    }
}