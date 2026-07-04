using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
    public enum eGradientType
    {
        Linear,
        Radial,
        Angle,
        Reflected,
        Diamond
    }

    public static class GradientFill
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

        public static void Fill(SKBitmap target, eGradientType type, float startX, float startY, float endX, float endY, SKColor startColor, SKColor endColor, bool reverse)
        {
            double ax = endX - startX;
            double ay = endY - startY;
            double length = Math.Sqrt((ax * ax) + (ay * ay));

            if (length < 0.000001)
            {
                SKColor solid = startColor;

                if (reverse)
                {
                    solid = endColor;
                }

                for (int y = 0; y < target.Height; y++)
                {
                    for (int x = 0; x < target.Width; x++)
                    {
                        target.SetPixel(x, y, solid);
                    }
                }

                return;
            }

            double ux = ax / length;
            double uy = ay / length;
            double px = -uy;
            double py = ux;

            double startR = startColor.Red;
            double startG = startColor.Green;
            double startB = startColor.Blue;
            double startA = startColor.Alpha;

            double endR = endColor.Red;
            double endG = endColor.Green;
            double endB = endColor.Blue;
            double endA = endColor.Alpha;

            for (int y = 0; y < target.Height; y++)
            {
                for (int x = 0; x < target.Width; x++)
                {
                    double rx = (x + 0.5) - startX;
                    double ry = (y + 0.5) - startY;
                    double u = (rx * ux) + (ry * uy);
                    double v = (rx * px) + (ry * py);

                    double t = 0.0;

                    if (type == eGradientType.Linear)
                    {
                        t = u / length;
                    }

                    if (type == eGradientType.Radial)
                    {
                        t = Math.Sqrt((rx * rx) + (ry * ry)) / length;
                    }

                    if (type == eGradientType.Angle)
                    {
                        double angle = Math.Atan2(ry, rx) - Math.Atan2(ay, ax);

                        for (;;)
                        {
                            if (angle < 0.0)
                            {
                                angle = angle + (2.0 * Math.PI);
                            }
                            else
                            {
                                break;
                            }
                        }

                        for (;;)
                        {
                            if (angle >= (2.0 * Math.PI))
                            {
                                angle = angle - (2.0 * Math.PI);
                            }
                            else
                            {
                                break;
                            }
                        }

                        t = angle / (2.0 * Math.PI);
                    }

                    if (type == eGradientType.Reflected)
                    {
                        t = Math.Abs(u) / length;
                    }

                    if (type == eGradientType.Diamond)
                    {
                        t = (Math.Abs(u) + Math.Abs(v)) / length;
                    }

                    if (t < 0.0)
                    {
                        t = 0.0;
                    }

                    if (t > 1.0)
                    {
                        t = 1.0;
                    }

                    if (reverse)
                    {
                        t = 1.0 - t;
                    }

                    byte r = ClampToByte(startR + ((endR - startR) * t));
                    byte g = ClampToByte(startG + ((endG - startG) * t));
                    byte b = ClampToByte(startB + ((endB - startB) * t));
                    byte a = ClampToByte(startA + ((endA - startA) * t));

                    SKColor color = new SKColor(r, g, b, a);
                    target.SetPixel(x, y, color);
                }
            }
        }
    }
}
