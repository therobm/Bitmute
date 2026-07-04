using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
    public static class ChannelRender
    {
        private static byte Unpremultiply(byte premulValue, byte alpha)
        {
            if (alpha == 0)
            {
                return 0;
            }
            int result = ((premulValue * 255) + (alpha / 2)) / alpha;
            if (result > 255)
            {
                result = 255;
            }
            return (byte)result;
        }

        public static unsafe void Render(SKBitmap source, SKBitmap target, int channel)
        {
            if (source.Width != target.Width)
            {
                return;
            }
            if (source.Height != target.Height)
            {
                return;
            }

            int width = source.Width;
            int height = source.Height;
            int selected = channel;
            if (selected < 0)
            {
                selected = 0;
            }
            if (selected > 3)
            {
                selected = 0;
            }

            byte* sourceBase = (byte*)source.GetPixels();
            byte* targetBase = (byte*)target.GetPixels();
            int sourceRowBytes = source.RowBytes;
            int targetRowBytes = target.RowBytes;

            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte* p = sourceBase + (row * sourceRowBytes) + (x * 4);
                    byte* t = targetBase + (row * targetRowBytes) + (x * 4);
                    byte sr = p[0];
                    byte sg = p[1];
                    byte sb = p[2];
                    byte sa = p[3];
                    byte gray = 0;
                    if (selected == 3)
                    {
                        gray = sa;
                    }
                    if (selected == 0)
                    {
                        gray = Unpremultiply(sr, sa);
                    }
                    if (selected == 1)
                    {
                        gray = Unpremultiply(sg, sa);
                    }
                    if (selected == 2)
                    {
                        gray = Unpremultiply(sb, sa);
                    }
                    t[0] = gray;
                    t[1] = gray;
                    t[2] = gray;
                    t[3] = 255;
                }
            }
        }

        public static unsafe void ApplyVisibilityMask(SKBitmap source, SKBitmap target, bool showRed, bool showGreen, bool showBlue, bool showAlpha)
        {
            if (source.Width != target.Width)
            {
                return;
            }
            if (source.Height != target.Height)
            {
                return;
            }

            int width = source.Width;
            int height = source.Height;
            byte* sourceBase = (byte*)source.GetPixels();
            byte* targetBase = (byte*)target.GetPixels();
            int sourceRowBytes = source.RowBytes;
            int targetRowBytes = target.RowBytes;

            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte* p = sourceBase + (row * sourceRowBytes) + (x * 4);
                    byte* t = targetBase + (row * targetRowBytes) + (x * 4);
                    byte sa = p[3];
                    byte red = Unpremultiply(p[0], sa);
                    byte green = Unpremultiply(p[1], sa);
                    byte blue = Unpremultiply(p[2], sa);
                    if (!showRed)
                    {
                        red = 0;
                    }
                    if (!showGreen)
                    {
                        green = 0;
                    }
                    if (!showBlue)
                    {
                        blue = 0;
                    }
                    byte outAlpha = sa;
                    if (!showAlpha)
                    {
                        outAlpha = 255;
                    }
                    t[0] = red;
                    t[1] = green;
                    t[2] = blue;
                    t[3] = outAlpha;
                }
            }
        }
    }
}
