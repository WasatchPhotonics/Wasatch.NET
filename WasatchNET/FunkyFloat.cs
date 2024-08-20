using System;

namespace WasatchNET
{
    /// <summary>
    /// Converts to and from the weird unsigned 16-bit float format used by the 
    /// CCD gain commands.
    /// </summary>
    /// <remarks>
    /// Every company has a legacy API decision that nobody can currently
    /// explain or defend, yet for support reasons cannot easily deprecate.
    /// For Wasatch Photonics, this is that class. Yes, CCD gain probably
    /// should have been a standard 32-bit float. Bear with us, and get your 
    /// funk on.
    ///
    /// This is basically an unsigned 16-bit float, with exactly eight bits of
    /// precision for the integral component (left of the decimal, stored in the
    /// MSB), and eight bits of precision for the fractional component (right of 
    /// the decimal, in the LSB). 
    ///
    /// That is to say, the MSB (left of the decimal, in float representation) 
    /// represents an integral value from 0-255 as does any byte, while the LSB 
    /// represents a fractional value between 0/256 and 255/256. This allows the
    /// resulting fraction to take on any unsigned fractional value between 0.0
    /// and 255 + 255/256, in 1/256 (0.004) increments.
    ///
    /// Example: 0x01e6 is approximately equal to 1.9
    ///
    /// <pre>
    ///    MSB       LSB
    /// --------- ---------
    /// 0000 0001 1110 0110
    ///         | |||| |||+ 0 * 1/256 = 0
    ///         | |||| ||+- 1 * 1/128 = 0.0078125
    ///         | |||| |+-- 1 * 1/64  = 0.015625
    ///         | |||| +--- 0 * 1/32  = 0
    ///         | |||+----- 0 * 1/16  = 0
    ///         | ||+------ 1 * 1/8   = 0.125
    ///         | |+------- 1 * 1/4   = 0.25
    ///         | +-------- 1 * 1/2   = 0.5
    ///         +---------- 1 * 1     = 1
    ///                               ===========
    ///                                 1.8984375
    /// </pre>
    /// </remarks>
    public class FunkyFloat
    {
        /// <summary>
        /// convert a standard IEEE float into the MSB-LSB UInt16 used within the spectrometer for gain control
        /// </summary>
        /// <param name="f">single-precision IEEE float</param>
        /// <returns>a UInt16 in which the MSB is a standard 8-bit byte and the LSB represents 8 bits of decreasing fractional precision</returns>
        public static ushort fromFloat(float f)
        {
            if (f < 0 || f >= 256)
            {
                WPFLogger.getInstance().error("FunkyFloat: input float out-of-range: {0}", f);
                return 0;
            }

            byte msb = (byte) Math.Floor(f);
            double frac = f - Math.Floor(f);
            byte lsb = 0;

            // iterate RIGHTWARDS from the decimal point, in DECREASING significance
            // (traverse the LSB in order --> 0123 4567)
            for (int bit = 0; bit < 8; bit++)
            {
                double placeValue = Math.Pow(2, -1 - bit);
                if (frac >= placeValue) 
                { 
                    byte mask = (byte) (1 << (7 - bit));
                    lsb |= mask;
                    frac -= placeValue;
                }                    
            }

            return (ushort)((msb << 8) | lsb);
        }

        /// <summary>
        /// convert the MSB-LSB UInt16 used within the spectrometer for gain control into a standard single-precision IEEE float
        /// </summary>
        /// <param name="n">UInt16 in which the MSB is a standard 8-bit byte and the LSB represents 8 bits of decreasing fractional precision</param>
        /// <returns>single-precision IEEE float</returns>
        public static float toFloat(ushort n)
        {
            byte msb = (byte) ((n >> 8) & 0xff);
            byte lsb = (byte) (n & 0xff);
            double frac = 0;

            // iterate RIGHTWARDS from the decimal point, in DECREASING significance
            // (traverse the LSB in order --> 0123 4567)
            for (int bit = 0; bit < 8; bit++)
            {
                byte mask = (byte)(1 << (7 - bit));
                if ((lsb & mask) != 0)
                {
                    double placeValue = Math.Pow(2, -1 - bit);
                    frac += placeValue;
                }
            }
            return (float) (msb + frac);
        }
    }
}
