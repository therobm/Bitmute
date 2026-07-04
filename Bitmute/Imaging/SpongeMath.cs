using System;

namespace Bitmute.Imaging
{
    public static class SpongeMath
    {
        private static byte ClampToByte(double value)
        {
            double rounded = Math.Round(value);

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

        public static void Apply(byte inR, byte inG, byte inB, bool saturate, double strength, out byte outR, out byte outG, out byte outB)
        {
            double luma = (0.299 * inR) + (0.587 * inG) + (0.114 * inB);

            double resultR;
            double resultG;
            double resultB;

            if (saturate)
            {
                resultR = inR + ((inR - luma) * strength);
                resultG = inG + ((inG - luma) * strength);
                resultB = inB + ((inB - luma) * strength);
            }
            else
            {
                resultR = inR + ((luma - inR) * strength);
                resultG = inG + ((luma - inG) * strength);
                resultB = inB + ((luma - inB) * strength);
            }

            outR = ClampToByte(resultR);
            outG = ClampToByte(resultG);
            outB = ClampToByte(resultB);
        }
    }
}
