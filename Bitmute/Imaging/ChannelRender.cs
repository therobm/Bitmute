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
    }
}
