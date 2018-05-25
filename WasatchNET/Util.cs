using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WasatchNET
{
    /// <summary>
    /// A collection of static functions provided to automate common algorithms 
    /// and post-processing steps in spectroscopy applications.
    /// </summary>
    public class Util
    {
        static Logger logger = Logger.getInstance();

        /// <summary>
        /// Given a 4th-order wavelength calibration, generates the array of wavelengths.
        /// </summary>
        /// <param name="pixels">Number of wavelengths to generate</param>
        /// <param name="coeffs">4th-order polynomial coefficients (offset, x^1, x^2 and x^3)</param>
        /// <returns>array of 'pixel' wavalengths</returns>
        public static double[] generateWavelengths(uint pixels, float[] coeffs)
        {
            if (coeffs == null || coeffs.Length != 4)
            {
                logger.error("generateWavelengths: invalid 4th-order wavecal");
                return null;
            }

            if (pixels > 2048)
            {
                logger.error("generateWavelengths: unlikely pixel count {0}", pixels);
                return null;
            }
                
            double[] wavelengths = new double[pixels];
            for (uint pixel = 0; pixel < pixels; pixel++)
                wavelengths[pixel] = coeffs[0]
                                   + coeffs[1] * pixel
                                   + coeffs[2] * pixel * pixel
                                   + coeffs[3] * pixel * pixel * pixel;
            return wavelengths;
        }

        /// <summary>
        /// Given a dark spectrum, a reference spectrum and a sample spectrum, 
        /// computes the transmission or reflectance spectrum. All arguments must 
        /// be non-null and of equal length.
        /// </summary>
        /// <param name="dark">collected with the light source disabled or blocked by shutter</param>
        /// <param name="reference">a direct spectrum of the light source (transmission) or of the illuminated reference medium (reflectance)</param>
        /// <param name="sample">collected with the light source passing through the sample (transmission) or reflected directly from the sample (reflectance)</param>
        /// <returns>transmission or reflectance spectrum</returns>
        public static double[] computeTransmission(double[] dark, double[] reference, double[] sample)
        {
            if (dark == null || reference == null || sample == null || dark.Length != reference.Length || reference.Length != sample.Length)
            {
                logger.error("computeTransmission: invalid argument");
                return null;
            }

            double[] t = new double[dark.Length];
            for (int i = 0; i < t.Length; i++)
                t[i] = 100.0 * (sample[i] - dark[i]) / (reference[i] - dark[i]);
            return t;
        }

        /// <summary>
        /// Same as computeTransmission(dark, reference, sample), but assumes that both reference and sample
        /// spectra have already been dark-corrected.
        /// </summary>
        /// <param name="darkCorrectedReference">dark-corrected reference spectrum</param>
        /// <param name="darkCorrectedSample">dark-corrected sample spectrum</param>
        /// <returns>transmission or reflectance spectrum</returns>
        public static double[] computeTransmission(double[] darkCorrectedReference, double[] darkCorrectedSample)
        {
            if (darkCorrectedReference == null || darkCorrectedSample == null || darkCorrectedReference.Length != darkCorrectedSample.Length)
            {
                logger.error("computeTransmission: invalid argument");
                return null;
            }

            double[] t = new double[darkCorrectedReference.Length];
            for (int i = 0; i < t.Length; i++)
                t[i] = 100.0 * darkCorrectedSample[i] / darkCorrectedReference[i];
            return t;
        }

        /// <summary>
        /// Given dark, reference and sample spectra, compute the absorbance in AU (Absorbance Units) per Beer's Law.
        /// </summary>
        /// <param name="dark">collected with the light source disabled or blocked by shutter</param>
        /// <param name="reference">collected with the light source shining through the reference medium (e.g. cuvette filled with air or water)</param>
        /// <param name="sample">collected with the light source shining through the sample</param>
        /// <returns>absorbance spectrum in AU</returns>
        /// <see cref="https://en.wikipedia.org/wiki/Beer–Lambert_law"/>
        public static double[] computeAbsorbance(double[] dark, double[] reference, double[] sample)
        {
            if (dark == null || reference == null || sample == null || dark.Length != reference.Length || reference.Length != sample.Length)
            {
                logger.error("computeAbsorbance: invalid argument");
                return null;
            }

            double[] a = new double[dark.Length];
            for (int i = 0; i < a.Length; i++)
                a[i] = 0 - Math.Log10((sample[i] - dark[i]) / (reference[i] - dark[i]));
            return a;
        }

        /// <summary>
        /// Same as computeAbsorbance(dark, reference, sample), but assumes that both reference and sample have been previously dark-corrected.
        /// </summary>
        /// <param name="darkCorrectedReference">dark-corrected reference spectrum of the light source shining through the reference medium (e.g. cuvette filled with air or water)</param>
        /// <param name="darkCorrectedSample">dark-corrected sample spectrum light source shining through the sample</param>
        /// <returns>absorbance spectrum in AU</returns>
        /// <see cref="https://en.wikipedia.org/wiki/Beer–Lambert_law"/>
        public static double[] computeAbsorbance(double[] darkCorrectedReference, double[] darkCorrectedSample)
        {
            if (darkCorrectedReference == null || darkCorrectedSample == null || darkCorrectedReference.Length != darkCorrectedSample.Length)
            {
                logger.error("computeAbsorbance: invalid argument");
                return null;
            }

            double[] a = new double[darkCorrectedReference.Length];
            for (int i = 0; i < a.Length; i++)
                a[i] = 0 - Math.Log10(darkCorrectedSample[i] / darkCorrectedReference[i]);
            return a;
        }

        /// <summary>
        /// Given the center wavelength of a narrowband excitation source such as a laser, converts the
        /// passed x-axis of wavelengths (nm) into Raman shifts expressed in wavenumbers (1/cm).
        /// </summary>
        /// <param name="laserWavelengthNM">center wavelength of the excitation laser (nm)</param>
        /// <param name="wavelengths">array of calibrated wavelength values for each pixel</param>
        /// <returns>an array of the same length as the input wavelengths, with each element 
        ///          representing the corresponding Raman shift in wavenumbers from the excitation laser</returns>
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

        /// <summary>
        /// Remove high-frequency noise by averaging "n" (halfWidth) pixels to either
        /// side of each measured sample pixel. Note: this implementation does not average
        /// any pixels for which the full boxcar can not be computed (leaving noise on both
        /// ends of the spectrum).
        /// </summary>
        /// <param name="halfWidth">How many pixels to average to either side of each sample pixel; 
        ///     higher values yield increased smoothing with commensurate loss of detail (zero disables)</param>
        /// <param name="spectrum">Input spectrum (unmodified)</param>
        /// <returns>de-noised, smoothed spectrum</returns>
        public static double[] applyBoxcar(uint halfWidth, double[] spectrum)
        {
            uint pixels = (uint) spectrum.Length;
            double[] proc = new double[pixels];

            uint limit = pixels - halfWidth - 1;
            uint range = 2 * halfWidth + 1;

            for (uint i = 0; i < pixels; i++)
            {
                if (i < halfWidth || i > limit)
                    proc[i] = spectrum[i];
                else
                {
                    double sum = spectrum[i];
                    for (int j = 1; j <= halfWidth; j++)
                        sum += spectrum[i - j] + spectrum[i + j]; 
                    proc[i] = sum / range;
                }
            }

            return proc;
        }

        /// <summary>
        /// Remove any "not a number" (sqrt(-1)) or "infinity" (1/0) values from
        /// an array, replacing them with the specified alternative.
        /// </summary>
        /// <param name="spectrum">input array</param>
        /// <param name="replacement">value to replace any NaN/INF entries (default zero)</param>
        /// <returns>cleansed array with no invalid values</returns>
        public static double[] cleanNan(double[] spectrum, double replacement = 0)
        {
            double[] clean = new double[spectrum.Length];
            for (uint i = 0; i < spectrum.Length; i++)
                clean[i] = (Double.IsNaN(spectrum[i]) || Double.IsInfinity(spectrum[i]))
                         ? replacement : spectrum[i];
            return clean;
        }

        public static byte[] truncateArray(byte[] src, int len)
        {
            if (src == null)
                return null;

            if (src.Length <= len)
                return src;

            byte[] tmp = new byte[len];
            Array.Copy(src, tmp, len);
            return tmp;
        }
    }
}