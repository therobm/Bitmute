using System;

namespace Bitmute.Imaging
{
    public static class ColorReplaceMath
    {
        private static double Lum(double r, double g, double b)
        {
            return (0.3 * r) + (0.59 * g) + (0.11 * b);
        }

        private static double Sat(double r, double g, double b)
        {
            double maxV = Math.Max(r, Math.Max(g, b));
            double minV = Math.Min(r, Math.Min(g, b));
            return maxV - minV;
        }

        private static void ClipColor(ref double r, ref double g, ref double b)
        {
            double l = Lum(r, g, b);
            double n = Math.Min(r, Math.Min(g, b));
            double x = Math.Max(r, Math.Max(g, b));

            if (n < 0.0)
            {
                double denom = l - n;
                if (Math.Abs(denom) > 1e-12)
                {
                    r = l + (((r - l) * l) / denom);
                    g = l + (((g - l) * l) / denom);
                    b = l + (((b - l) * l) / denom);
                }
            }

            if (x > 1.0)
            {
                double denom = x - l;
                if (Math.Abs(denom) > 1e-12)
                {
                    r = l + (((r - l) * (1.0 - l)) / denom);
                    g = l + (((g - l) * (1.0 - l)) / denom);
                    b = l + (((b - l) * (1.0 - l)) / denom);
                }
            }
        }

        private static void SetLum(double r, double g, double b, double targetLum, out double outR, out double outG, out double outB)
        {
            double d = targetLum - Lum(r, g, b);
            double rr = r + d;
            double gg = g + d;
            double bb = b + d;
            ClipColor(ref rr, ref gg, ref bb);
            outR = rr;
            outG = gg;
            outB = bb;
        }

        private static void SetSat(double r, double g, double b, double targetSat, out double outR, out double outG, out double outB)
        {
            double rr = r;
            double gg = g;
            double bb = b;

            double minV = Math.Min(rr, Math.Min(gg, bb));
            double maxV = Math.Max(rr, Math.Max(gg, bb));

            if (maxV > minV)
            {
                double range = maxV - minV;

                if (rr == maxV)
                {
                    if (gg >= bb)
                    {
                        double mid = ((gg - minV) * targetSat) / range;
                        outR = targetSat;
                        outG = mid;
                        outB = 0.0;
                    }
                    else
                    {
                        double mid = ((bb - minV) * targetSat) / range;
                        outR = targetSat;
                        outG = 0.0;
                        outB = mid;
                    }
                }
                else
                {
                    if (gg == maxV)
                    {
                        if (rr >= bb)
                        {
                            double mid = ((rr - minV) * targetSat) / range;
                            outG = targetSat;
                            outR = mid;
                            outB = 0.0;
                        }
                        else
                        {
                            double mid = ((bb - minV) * targetSat) / range;
                            outG = targetSat;
                            outR = 0.0;
                            outB = mid;
                        }
                    }
                    else
                    {
                        if (rr >= gg)
                        {
                            double mid = ((rr - minV) * targetSat) / range;
                            outB = targetSat;
                            outR = mid;
                            outG = 0.0;
                        }
                        else
                        {
                            double mid = ((gg - minV) * targetSat) / range;
                            outB = targetSat;
                            outR = 0.0;
                            outG = mid;
                        }
                    }
                }
            }
            else
            {
                outR = 0.0;
                outG = 0.0;
                outB = 0.0;
            }
        }

        private static byte ClampToByte(double normalized)
        {
            double scaled = Math.Round(normalized * 255.0);

            if (scaled < 0.0)
            {
                scaled = 0.0;
            }

            if (scaled > 255.0)
            {
                scaled = 255.0;
            }

            return (byte)scaled;
        }

        public static void Apply(byte baseR, byte baseG, byte baseB, byte colorR, byte colorG, byte colorB, int mode, double strength, out byte outR, out byte outG, out byte outB)
        {
            double br = baseR / 255.0;
            double bg = baseG / 255.0;
            double bb = baseB / 255.0;

            double cr = colorR / 255.0;
            double cg = colorG / 255.0;
            double cb = colorB / 255.0;

            double blendedR;
            double blendedG;
            double blendedB;

            if (mode == 1)
            {
                double s1;
                double s2;
                double s3;
                SetSat(cr, cg, cb, Sat(br, bg, bb), out s1, out s2, out s3);
                SetLum(s1, s2, s3, Lum(br, bg, bb), out blendedR, out blendedG, out blendedB);
            }
            else
            {
                if (mode == 2)
                {
                    double s1;
                    double s2;
                    double s3;
                    SetSat(br, bg, bb, Sat(cr, cg, cb), out s1, out s2, out s3);
                    SetLum(s1, s2, s3, Lum(br, bg, bb), out blendedR, out blendedG, out blendedB);
                }
                else
                {
                    if (mode == 3)
                    {
                        SetLum(br, bg, bb, Lum(cr, cg, cb), out blendedR, out blendedG, out blendedB);
                    }
                    else
                    {
                        SetLum(cr, cg, cb, Lum(br, bg, bb), out blendedR, out blendedG, out blendedB);
                    }
                }
            }

            double finalR = br + ((blendedR - br) * strength);
            double finalG = bg + ((blendedG - bg) * strength);
            double finalB = bb + ((blendedB - bb) * strength);

            outR = ClampToByte(finalR);
            outG = ClampToByte(finalG);
            outB = ClampToByte(finalB);
        }
    }
}
