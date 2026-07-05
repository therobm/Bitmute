using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
    public static class ChannelRender
    {
        private sealed unsafe class RenderWorker
        {
            public byte* m_sourceBase;
            public byte* m_targetBase;
            public int m_sourceRowBytes;
            public int m_targetRowBytes;
            public int m_width;
            public int m_selected;

            public void Band(int start, int end)
            {
                for (int row = start; row < end; row++)
                {
                    for (int x = 0; x < m_width; x++)
                    {
                        byte* p = m_sourceBase + (row * m_sourceRowBytes) + (x * 4);
                        byte* t = m_targetBase + (row * m_targetRowBytes) + (x * 4);
                        byte sr = p[0];
                        byte sg = p[1];
                        byte sb = p[2];
                        byte sa = p[3];
                        byte gray = 0;
                        if (m_selected == 3)
                        {
                            gray = sa;
                        }
                        if (m_selected == 0)
                        {
                            gray = Unpremultiply(sr, sa);
                        }
                        if (m_selected == 1)
                        {
                            gray = Unpremultiply(sg, sa);
                        }
                        if (m_selected == 2)
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

        private sealed unsafe class VisibilityMaskWorker
        {
            public byte* m_sourceBase;
            public byte* m_targetBase;
            public int m_sourceRowBytes;
            public int m_targetRowBytes;
            public int m_width;
            public bool m_showRed;
            public bool m_showGreen;
            public bool m_showBlue;
            public bool m_showAlpha;

            public void Band(int start, int end)
            {
                for (int row = start; row < end; row++)
                {
                    for (int x = 0; x < m_width; x++)
                    {
                        byte* p = m_sourceBase + (row * m_sourceRowBytes) + (x * 4);
                        byte* t = m_targetBase + (row * m_targetRowBytes) + (x * 4);
                        byte sa = p[3];
                        byte red = Unpremultiply(p[0], sa);
                        byte green = Unpremultiply(p[1], sa);
                        byte blue = Unpremultiply(p[2], sa);
                        if (!m_showRed)
                        {
                            red = 0;
                        }
                        if (!m_showGreen)
                        {
                            green = 0;
                        }
                        if (!m_showBlue)
                        {
                            blue = 0;
                        }
                        byte outAlpha = sa;
                        if (!m_showAlpha)
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

            RenderWorker worker = new RenderWorker();
            worker.m_sourceBase = sourceBase;
            worker.m_targetBase = targetBase;
            worker.m_sourceRowBytes = sourceRowBytes;
            worker.m_targetRowBytes = targetRowBytes;
            worker.m_width = width;
            worker.m_selected = selected;
            RowBands.Run(0, height, worker.Band);
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

            VisibilityMaskWorker worker = new VisibilityMaskWorker();
            worker.m_sourceBase = sourceBase;
            worker.m_targetBase = targetBase;
            worker.m_sourceRowBytes = sourceRowBytes;
            worker.m_targetRowBytes = targetRowBytes;
            worker.m_width = width;
            worker.m_showRed = showRed;
            worker.m_showGreen = showGreen;
            worker.m_showBlue = showBlue;
            worker.m_showAlpha = showAlpha;
            RowBands.Run(0, height, worker.Band);
        }
    }
}
