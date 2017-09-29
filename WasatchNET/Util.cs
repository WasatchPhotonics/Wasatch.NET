using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    public class Util
    {
        static Logger logger = Logger.getInstance();

        public static double[] generateWavelengths(uint pixels, float[] coeffs)
        {
            double[] wavelengths = new double[pixels];
            for (uint pixel = 0; pixel < pixels; pixel++)
                wavelengths[pixel] = coeffs[0]
                                   + coeffs[1] * pixel
                                   + coeffs[2] * pixel * pixel
                                   + coeffs[3] * pixel * pixel * pixel;
            return wavelengths;
        }

        public static double[] computeTransmission(double[] dark, double[] reference, double[] spectrum)
        {
            if (dark == null || reference == null || spectrum == null || dark.Length != reference.Length || reference.Length != spectrum.Length)
            {
                logger.error("computeTransmission: invalid argument");
                return null;
            }

            double[] t = new double[dark.Length];
            for (int i = 0; i < t.Length; i++)
                t[i] = 100.0 * (spectrum[i] - dark[i]) / (reference[i] - dark[i]);
            return t;
        }

        public static double[] computeTransmission(double[] darkCorrectedReference, double[] darkCorrectedSpectrum)
        {
            if (darkCorrectedReference == null || darkCorrectedSpectrum == null || darkCorrectedReference.Length != darkCorrectedSpectrum.Length)
            {
                logger.error("computeTransmission: invalid argument");
                return null;
            }

            double[] t = new double[darkCorrectedReference.Length];
            for (int i = 0; i < t.Length; i++)
                t[i] = 100.0 * darkCorrectedSpectrum[i] / darkCorrectedReference[i];
            return t;
        }

        public static double[] computeAbsorbance(double[] dark, double[] reference, double[] spectrum)
        {
            if (dark == null || reference == null || spectrum == null || dark.Length != reference.Length || reference.Length != spectrum.Length)
            {
                logger.error("computeAbsorbance: invalid argument");
                return null;
            }

            double[] a = new double[dark.Length];
            for (int i = 0; i < a.Length; i++)
                a[i] = 0 - Math.Log10((spectrum[i] - dark[i]) / (reference[i] - dark[i]));
            return a;
        }

        public static double[] computeAbsorbance(double[] darkCorrectedReference, double[] darkCorrectedSpectrum)
        {
            if (darkCorrectedReference == null || darkCorrectedSpectrum == null || darkCorrectedReference.Length != darkCorrectedSpectrum.Length)
            {
                logger.error("computeAbsorbance: invalid argument");
                return null;
            }

            double[] a = new double[darkCorrectedReference.Length];
            for (int i = 0; i < a.Length; i++)
                a[i] = 0 - Math.Log10(darkCorrectedSpectrum[i] / darkCorrectedReference[i]);
            return a;
        }

        public static double[] wavelengthsToWavenumbers(double laserWavelengthNM, double[] wavelengths)
        {
            const double NM_TO_CM = 1.0 / 10000000.0;
            double LASER_WAVENUMBER = 1.0 / (laserWavelengthNM * NM_TO_CM);

            if (wavelengths == null)
            {
                logger.error("wavelengthsToWavenumbers: invalid wavelengths");
                return null;
            }

            double[] wavenumbers = new double[wavelengths.Length];
            for (int i = 0; i < wavelengths.Length; i++)
            {
                double wavenumber = LASER_WAVENUMBER - (1.0 / (wavelengths[i] * NM_TO_CM));
                if (Double.IsInfinity(wavenumber) || Double.IsNaN(wavenumber))
                    wavenumbers[i] = 0;
                else
                    wavenumbers[i] = wavenumber;
            }
            return wavenumbers;
        }

        public static double[] applyBoxcar(uint halfWidth, double[] spectrum)
        {
            uint pixels = (uint) spectrum.Length;
            double[] proc = new double[pixels];

            uint limit = pixels - halfWidth - 1;
            uint range = 2 * halfWidth + 1;

            for (uint i = halfWidth; i <= limit; i++)
            {   
                double sum = spectrum[i];
                for (int j = 1; j <= halfWidth; j++)
                    sum += spectrum[i - j] + spectrum[i + j]; 
                proc[i] = sum / range;
            }

            return proc;
        }

        public static double[] cleanNan(double[] spectrum, double replacement = 0)
        {
            double[] clean = new double[spectrum.Length];
            for (uint i = 0; i < spectrum.Length; i++)
                clean[i] = (Double.IsNaN(spectrum[i]) || Double.IsInfinity(spectrum[i]))
                         ? replacement : spectrum[i];
            return clean;
        }
    }
}
