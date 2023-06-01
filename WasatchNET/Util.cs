using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
            if (coeffs == null || coeffs.Length < 4 || coeffs.Length > 5)
            {
                logger.error("generateWavelengths: invalid wavecal, only 4th- or 5th-order fits are valid");
                return null;
            }

            
            if (pixels > 2048)
            {
                logger.info("generateWavelengths: unlikely pixel count {0}", pixels);
            }
              
            
            double[] wavelengths = new double[pixels];
            for (uint pixel = 0; pixel < pixels; pixel++)
            {
                wavelengths[pixel] = coeffs[0];
                for (int i = 1; i < coeffs.Length; ++i)
                    wavelengths[pixel] += (coeffs[i] * Math.Pow(pixel, i));
            }
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
        public static double[] cleanNan(double[] a, double replacement = 0)
        {
            double[] clean = new double[a.Length];
            for (uint i = 0; i < a.Length; i++)
                clean[i] = (double.IsNaN(a[i]) || double.IsInfinity(a[i])) ? replacement : a[i];
            return clean;
        }

        // you'd think you could do this with generics :-(
        // @see https://stackoverflow.com/q/32664/11615696
        public static float[] cleanNan(float[] a, float replacement = 0)
        {
            float[] clean = new float[a.Length];
            for (uint i = 0; i < a.Length; i++)
                clean[i] = (float.IsNaN(a[i]) || float.IsInfinity(a[i])) ? replacement : a[i];
            return clean;
        }

        // not usable for Properties :-(
        public static void fixOrder<T>(ref T lo, ref T hi) where T : IComparable
        {
            if (hi.CompareTo(lo) < 0)
            {
                T tmp = lo;
                lo = hi;
                hi = tmp;
            }    
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

        public static float coeffConvertForward(float value, float[] coeffs)
        {
            float total = coeffs[0];

            for (int i = 1; i < coeffs.Length; ++i)
                total += (float)(coeffs[i] * Math.Pow(value, i));

            return total;
        }

        public static float coeffConvertBackwardsNewton(float targetY, float[] coeffs, float minY, float maxY, float minX, float maxX)
        {
            double thresh = 0.01;

            float increments = (maxX - minX) / 1000;


            double pctY = (targetY - minY) / (maxY - minY);

            float guessX = minX + (float)pctY * (maxX - minX);

            if (guessX <= minX)
                return minX;
            if (guessX >= maxX)
                return maxX;

            float result = coeffConvertForward(guessX, coeffs);
            double delta = targetY - result;
            double absDelta = Math.Abs(targetY - result);

            while (absDelta > thresh) 
            {
                double momentarySlope = (coeffConvertForward(guessX + increments, coeffs) - coeffConvertForward(guessX, coeffs)) / increments;

                guessX = guessX + ((float)delta * (1 / (float)momentarySlope)); 
                result = coeffConvertForward(guessX, coeffs);
                delta = targetY - result;
                absDelta = Math.Abs(targetY - result);

                if (guessX <= minX)
                    return minX;
                if (guessX >= maxX)
                    return maxX;
            }

            return guessX;
        }


        /// <summary>
        /// Performs SRM correction on the given spectrum using the proivided ROI and coefficients.
        /// Non-ROI pixels are not corrected. 
        /// </summary>
        ///
        /// <returns>The given spectrum, with ROI srm-corrected, as an array of doubles</returns>
        /// 
        public static double[] applyRamanCorrection(double[] spectrum, float[] correctionCoeffs, int roiStart, int roiEnd)
        {
            if (roiStart >= roiEnd)
                return spectrum;

            double[] temp = new double[spectrum.Length];
            spectrum.CopyTo(temp, 0);

            for (int i = roiStart; i <= roiEnd; ++i)
            {
                double logTen = 0.0;
                for (int j = 0; j < correctionCoeffs.Length; j++)
                {
                    double x_to_i = Math.Pow(i, j);
                    double scaled = correctionCoeffs[j] * x_to_i;
                    logTen += scaled;
                }

                double expanded = Math.Pow(10, logTen);
                temp[i] *= expanded;
            }

            return temp;
        }

        // copy-pasted directly from WPSC for consistency
        public static bool validTECCal(Spectrometer spec)
        {
            if (!spec.eeprom.hasCooling)
                return true;

            if (spec is HOCTSpectrometer || spec is BoulderSpectrometer || spec is SPISpectrometer || spec is AndorSpectrometer)
                return true;

            if (spec.eeprom.degCToDACCoeffs.Length != 3)
                return false;
            if (spec.eeprom.degCToDACCoeffs[0] == 2700)
                return false;
            if (spec.eeprom.degCToDACCoeffs[1] == 0)
                return false;
            if (spec.eeprom.degCToDACCoeffs[2] == 0)
                return false;

            return true;
        }

        public static double[] reverseRamanCorrection(double[] spectrum, float[] correctionCoeffs, int roiStart, int roiEnd)
        {
            if (roiStart >= roiEnd)
                return spectrum;

            double[] temp = new double[spectrum.Length];
            spectrum.CopyTo(temp, 0);

            for (int i = roiStart; i <= roiEnd; ++i)
            {
                double logTen = 0.0;
                for (int j = 0; j < correctionCoeffs.Length; j++)
                {
                    double x_to_i = Math.Pow(i, j);
                    double scaled = correctionCoeffs[j] * x_to_i;
                    logTen += scaled;
                }

                double expanded = Math.Pow(10, logTen);
                temp[i] /= expanded;
            }

            return temp;
        }

    }
}
