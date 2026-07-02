using System;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class BrushEngine
	{
		private const double ShoulderExponent = 1.7;

		private byte[] m_coverage;
		private int m_width;
		private int m_height;
		private SKBitmap m_original;
		private bool m_ownsOriginal;
		private int m_radius;
		private double m_hardness;
		private double m_opacity;
		private double m_flow;
		private bool m_square;
		private double m_spacingPx;
		private double m_penX;
		private double m_penY;
		private bool m_hasPen;
		private double m_distanceSinceStamp;
		private byte m_red;
		private byte m_green;
		private byte m_blue;
		private bool m_active;

		public void Begin(Layer layer, SKBitmap original, int radius, double hardness, double opacity, double flow, bool square, double spacingFraction, SKColor color)
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
			m_square = square;
			int diameter = radius * 2;
			if (diameter < 1)
			{
				diameter = 1;
			}
			m_spacingPx = spacingFraction * diameter;
			if (m_spacingPx < 1.0)
			{
				m_spacingPx = 1.0;
			}
			m_penX = 0.0;
			m_penY = 0.0;
			m_hasPen = false;
			m_distanceSinceStamp = 0.0;
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

		private double TipCoverage(double offsetX, double offsetY)
		{
			if (m_radius <= 0)
			{
				if (System.Math.Abs(offsetX) < 0.5 && System.Math.Abs(offsetY) < 0.5)
				{
					return 1.0;
				}
				return 0.0;
			}
			double outer = m_radius;
			double distance;
			if (m_square)
			{
				double absX = System.Math.Abs(offsetX);
				double absY = System.Math.Abs(offsetY);
				distance = absX;
				if (absY > absX)
				{
					distance = absY;
				}
			}
			else
			{
				distance = System.Math.Sqrt((offsetX * offsetX) + (offsetY * offsetY));
			}
			double inner = m_hardness * outer;
			double antialias = 1.0;
			if (outer - inner < antialias)
			{
				inner = outer - antialias;
			}
			if (inner < 0.0)
			{
				inner = 0.0;
			}
			if (distance <= inner)
			{
				return 1.0;
			}
			if (distance >= outer)
			{
				return 0.0;
			}
			double t = (distance - inner) / (outer - inner);
			t = System.Math.Pow(t, ShoulderExponent);
			double smooth = t * t * (3.0 - 2.0 * t);
			return 1.0 - smooth;
		}

		public unsafe void StampDab(Layer layer, double centerX, double centerY, Selection selection)
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
			int minCanvasX = (int)System.Math.Floor(centerX) - radius - 1;
			int maxCanvasX = (int)System.Math.Ceiling(centerX) + radius + 1;
			int minCanvasY = (int)System.Math.Floor(centerY) - radius - 1;
			int maxCanvasY = (int)System.Math.Ceiling(centerY) + radius + 1;
			for (int canvasY = minCanvasY; canvasY <= maxCanvasY; canvasY++)
			{
				int bitmapY = canvasY - layerOffsetY;
				if (bitmapY < 0 || bitmapY >= m_height)
				{
					continue;
				}
				double offsetY = canvasY - centerY;
				for (int canvasX = minCanvasX; canvasX <= maxCanvasX; canvasX++)
				{
					double tip = TipCoverage(canvasX - centerX, offsetY);
					if (tip <= 0.0)
					{
						continue;
					}
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

		public void StampFirst(Layer layer, double x, double y, Selection selection)
		{
			StampDab(layer, x, y, selection);
			m_penX = x;
			m_penY = y;
			m_hasPen = true;
			m_distanceSinceStamp = 0.0;
		}

		public void StrokeTo(Layer layer, double x, double y, Selection selection)
		{
			if (!m_hasPen)
			{
				StampFirst(layer, x, y, selection);
				return;
			}
			double deltaX = x - m_penX;
			double deltaY = y - m_penY;
			double segmentLength = System.Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
			if (segmentLength <= 0.0)
			{
				return;
			}
			double directionX = deltaX / segmentLength;
			double directionY = deltaY / segmentLength;
			double traveled = 0.0;
			for (;;)
			{
				double distanceToNext = m_spacingPx - m_distanceSinceStamp;
				if (traveled + distanceToNext > segmentLength)
				{
					break;
				}
				traveled = traveled + distanceToNext;
				double stampX = m_penX + (directionX * traveled);
				double stampY = m_penY + (directionY * traveled);
				StampDab(layer, stampX, stampY, selection);
				m_distanceSinceStamp = 0.0;
			}
			m_distanceSinceStamp = m_distanceSinceStamp + (segmentLength - traveled);
			m_penX = x;
			m_penY = y;
		}
	}
}
