using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public struct PixelAccessor
	{
		private IntPtr m_pixels;
		private int m_rowBytes;
		private eColorDepth m_depth;

		private static float Clamp01(float value)
		{
			if (value < 0.0f)
			{
				return 0.0f;
			}
			if (value > 1.0f)
			{
				return 1.0f;
			}
			return value;
		}

		public PixelAccessor(IntPtr pixels, int rowBytes, SKColorType colorType)
		{
			m_pixels = pixels;
			m_rowBytes = rowBytes;
			m_depth = colorType.ToColorDepth();
		}

		public int BytesPerPixel()
		{
			if (m_depth == eColorDepth.Eight)
			{
				return 4;
			}
			if (m_depth == eColorDepth.Sixteen)
			{
				return 8;
			}
			if (m_depth == eColorDepth.ThirtyTwoFloat)
			{
				return 16;
			}
			return 4;
		}

		public unsafe float AlphaAt(int x, int y)
		{
			if (m_depth == eColorDepth.Eight)
			{
				byte* p = (byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes) + (x * 4);
				return p[3] / 255.0f;
			}
			if (m_depth == eColorDepth.Sixteen)
			{
				ushort* p = (ushort*)((byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes)) + (x * 4);
				return p[3] / 65535.0f;
			}
			if (m_depth == eColorDepth.ThirtyTwoFloat)
			{
				float* p = (float*)((byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes)) + (x * 4);
				return p[3];
			}
			return 0.0f;
		}

		public unsafe void ReadNormalized(int x, int y, out float red, out float green, out float blue, out float alpha)
		{
			if (m_depth == eColorDepth.Eight)
			{
				byte* p = (byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes) + (x * 4);
				red = p[0] / 255.0f;
				green = p[1] / 255.0f;
				blue = p[2] / 255.0f;
				alpha = p[3] / 255.0f;
				return;
			}
			if (m_depth == eColorDepth.Sixteen)
			{
				ushort* p = (ushort*)((byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes)) + (x * 4);
				red = p[0] / 65535.0f;
				green = p[1] / 65535.0f;
				blue = p[2] / 65535.0f;
				alpha = p[3] / 65535.0f;
				return;
			}
			if (m_depth == eColorDepth.ThirtyTwoFloat)
			{
				float* p = (float*)((byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes)) + (x * 4);
				red = p[0];
				green = p[1];
				blue = p[2];
				alpha = p[3];
				return;
			}
			red = 0.0f;
			green = 0.0f;
			blue = 0.0f;
			alpha = 0.0f;
		}

		public unsafe void WriteNormalized(int x, int y, float red, float green, float blue, float alpha)
		{
			if (m_depth == eColorDepth.Eight)
			{
				byte* p = (byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes) + (x * 4);
				p[0] = (byte)(Clamp01(red) * 255.0f + 0.5f);
				p[1] = (byte)(Clamp01(green) * 255.0f + 0.5f);
				p[2] = (byte)(Clamp01(blue) * 255.0f + 0.5f);
				p[3] = (byte)(Clamp01(alpha) * 255.0f + 0.5f);
				return;
			}
			if (m_depth == eColorDepth.Sixteen)
			{
				ushort* p = (ushort*)((byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes)) + (x * 4);
				p[0] = (ushort)(Clamp01(red) * 65535.0f + 0.5f);
				p[1] = (ushort)(Clamp01(green) * 65535.0f + 0.5f);
				p[2] = (ushort)(Clamp01(blue) * 65535.0f + 0.5f);
				p[3] = (ushort)(Clamp01(alpha) * 65535.0f + 0.5f);
				return;
			}
			if (m_depth == eColorDepth.ThirtyTwoFloat)
			{
				float* p = (float*)((byte*)m_pixels.ToPointer() + ((long)y * m_rowBytes)) + (x * 4);
				p[0] = red;
				p[1] = green;
				p[2] = blue;
				p[3] = alpha;
				return;
			}
		}
	}
}
