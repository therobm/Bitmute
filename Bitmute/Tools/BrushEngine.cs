using System;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class BrushEngine
	{
		private byte[] m_coverage;
		private int m_width;
		private int m_height;
		private SKBitmap m_original;
		private bool m_ownsOriginal;
		private int m_radius;
		private double m_hardness;
		private double m_opacity;
		private double m_flow;
		private byte m_red;
		private byte m_green;
		private byte m_blue;
		private bool m_active;

		public void Begin(Layer layer, SKBitmap original, int radius, double hardness, double opacity, double flow, SKColor color)
		{
			End();
			SKBitmap bitmap = layer.Bitmap();
			m_width = bitmap.Width;
			m_height = bitmap.Height;
			m_coverage = new byte[m_width * m_height];
			if (original != null && original.Width == m_width && original.Height == m_height)
			{
				m_original = original;
				m_ownsOriginal = false;
			}
			else
			{
				m_original = bitmap.Copy();
				m_ownsOriginal = true;
			}
			m_radius = radius;
			m_hardness = hardness;
			m_opacity = opacity;
			m_flow = flow;
			m_red = color.Red;
			m_green = color.Green;
			m_blue = color.Blue;
			m_active = true;
		}

		public bool IsActive()
		{
			return m_active;
		}

		public void End()
		{
			if (m_ownsOriginal && m_original != null)
			{
				m_original.Dispose();
			}
			m_original = null;
			m_ownsOriginal = false;
			m_coverage = null;
			m_active = false;
		}

		private double TipCoverage(int offsetX, int offsetY)
		{
			if (m_radius <= 0)
			{
				if (offsetX == 0 && offsetY == 0)
				{
					return 1.0;
				}
				return 0.0;
			}
			int distanceSquared = (offsetX * offsetX) + (offsetY * offsetY);
			if (distanceSquared > m_radius * m_radius)
			{
				return 0.0;
			}
			double distance = System.Math.Sqrt(distanceSquared) / m_radius;
			if (distance <= m_hardness)
			{
				return 1.0;
			}
			double falloff = 1.0 - m_hardness;
			if (falloff <= 0.0001)
			{
				return 1.0;
			}
			return (1.0 - distance) / falloff;
		}

		public unsafe void StampDab(Layer layer, int centerX, int centerY, Selection selection)
		{
			if (!m_active)
			{
				return;
			}
			SKBitmap bitmap = layer.Bitmap();
			if (bitmap.Width != m_width || bitmap.Height != m_height)
			{
				return;
			}
			int rowBytes = bitmap.RowBytes;
			int originalRowBytes = m_original.RowBytes;
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			byte* pixels = (byte*)bitmap.GetPixels().ToPointer();
			byte* originalPixels = (byte*)m_original.GetPixels().ToPointer();
			bool clip = selection != null && selection.IsActive();
			int radius = m_radius;
			for (int offsetY = -radius; offsetY <= radius; offsetY++)
			{
				int canvasY = centerY + offsetY;
				int bitmapY = canvasY - layerOffsetY;
				if (bitmapY < 0 || bitmapY >= m_height)
				{
					continue;
				}
				for (int offsetX = -radius; offsetX <= radius; offsetX++)
				{
					double tip = TipCoverage(offsetX, offsetY);
					if (tip <= 0.0)
					{
						continue;
					}
					int canvasX = centerX + offsetX;
					if (clip && !selection.IsSelected(canvasX, canvasY))
					{
						continue;
					}
					int bitmapX = canvasX - layerOffsetX;
					if (bitmapX < 0 || bitmapX >= m_width)
					{
						continue;
					}
					int coverageIndex = (bitmapY * m_width) + bitmapX;
					double accumulated = m_coverage[coverageIndex] / 255.0;
					double deposit = m_flow * tip;
					double updated = accumulated + (deposit * (1.0 - accumulated));
					if (updated > 1.0)
					{
						updated = 1.0;
					}
					m_coverage[coverageIndex] = (byte)((updated * 255.0) + 0.5);
					double finalAlpha = updated;
					if (finalAlpha > m_opacity)
					{
						finalAlpha = m_opacity;
					}
					byte* originalPixel = originalPixels + (bitmapY * originalRowBytes) + (bitmapX * 4);
					double originalAlpha = originalPixel[3] / 255.0;
					double inverse = 1.0 - finalAlpha;
					double outAlpha = finalAlpha + (originalAlpha * inverse);
					byte* destinationPixel = pixels + (bitmapY * rowBytes) + (bitmapX * 4);
					if (outAlpha <= 0.0)
					{
						destinationPixel[0] = 0;
						destinationPixel[1] = 0;
						destinationPixel[2] = 0;
						destinationPixel[3] = 0;
						continue;
					}
					double weightedOriginal = originalAlpha * inverse;
					double outRed = ((m_red * finalAlpha) + (originalPixel[0] * weightedOriginal)) / outAlpha;
					double outGreen = ((m_green * finalAlpha) + (originalPixel[1] * weightedOriginal)) / outAlpha;
					double outBlue = ((m_blue * finalAlpha) + (originalPixel[2] * weightedOriginal)) / outAlpha;
					destinationPixel[0] = (byte)(outRed + 0.5);
					destinationPixel[1] = (byte)(outGreen + 0.5);
					destinationPixel[2] = (byte)(outBlue + 0.5);
					destinationPixel[3] = (byte)((outAlpha * 255.0) + 0.5);
				}
			}
		}

		public void StrokeTo(Layer layer, int fromX, int fromY, int toX, int toY, Selection selection)
		{
			int deltaX = toX - fromX;
			int deltaY = toY - fromY;
			int steps = System.Math.Abs(deltaX);
			int absDeltaY = System.Math.Abs(deltaY);
			if (absDeltaY > steps)
			{
				steps = absDeltaY;
			}
			if (steps <= 0)
			{
				StampDab(layer, toX, toY, selection);
				return;
			}
			for (int step = 1; step <= steps; step++)
			{
				double fraction = (double)step / (double)steps;
				int pointX = fromX + (int)System.Math.Round(deltaX * fraction);
				int pointY = fromY + (int)System.Math.Round(deltaY * fraction);
				StampDab(layer, pointX, pointY, selection);
			}
		}
	}
}
