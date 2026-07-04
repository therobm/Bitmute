using System;

namespace Bitmute.Imaging
{
    public static class HealMath
    {
        private static byte ClampToByte(double value)
        {
            double rounded = Math.Round(value, MidpointRounding.AwayFromZero);

            if (rounded < 0.0)
            {
                rounded = 0.0;
            }

            if (rounded > 255.0)
            {
                rounded = 255.0;
            }

            return (byte)rounded;
        }

        public static void Apply(byte sourceR, byte sourceG, byte sourceB, double sourceAvgR, double sourceAvgG, double sourceAvgB, double destAvgR, double destAvgG, double destAvgB, double strength, byte destR, byte destG, byte destB, out byte outR, out byte outG, out byte outB)
        {
            double healedR = destAvgR + ((double)sourceR - sourceAvgR);
            double healedG = destAvgG + ((double)sourceG - sourceAvgG);
            double healedB = destAvgB + ((double)sourceB - sourceAvgB);

            byte clampedHealedR = ClampToByte(healedR);
            byte clampedHealedG = ClampToByte(healedG);
            byte clampedHealedB = ClampToByte(healedB);

            double resultR = (double)destR + (((double)clampedHealedR - (double)destR) * strength);
            double resultG = (double)destG + (((double)clampedHealedG - (double)destG) * strength);
            double resultB = (double)destB + (((double)clampedHealedB - (double)destB) * strength);

            outR = ClampToByte(resultR);
            outG = ClampToByte(resultG);
            outB = ClampToByte(resultB);
        }
    }
}
