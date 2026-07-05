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
		private sealed unsafe class FillWorker
		{
			public byte* m_basePixels;
			public int m_rowBytes;
			public int m_width;
			public eGradientType m_type;
			public float m_startX;
			public float m_startY;
			public bool m_reverse;
			public double m_length;
			public double m_ux;
			public double m_uy;
			public double m_px;
			public double m_py;
			public double m_startR;
			public double m_startG;
			public double m_startB;
			public double m_startA;
			public double m_endR;
			public double m_endG;
			public double m_endB;
			public double m_endA;
			public double m_axisAngle;
			public double m_twoPi;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_basePixels + (y * m_rowBytes);
					double ry = (y + 0.5) - m_startY;
					for (int x = 0; x < m_width; x++)
					{
						double rx = (x + 0.5) - m_startX;
						double u = (rx * m_ux) + (ry * m_uy);
						double v = (rx * m_px) + (ry * m_py);
						double t = 0.0;
						if (m_type == eGradientType.Linear)
						{
							t = u / m_length;
						}
						else if (m_type == eGradientType.Radial)
						{
							t = Math.Sqrt((rx * rx) + (ry * ry)) / m_length;
						}
						else if (m_type == eGradientType.Angle)
						{
							double angle = Math.Atan2(ry, rx) - m_axisAngle;
							for (;;)
							{
								if (angle < 0.0)
								{
									angle = angle + m_twoPi;
								}
								else
								{
									break;
								}
							}
							for (;;)
							{
								if (angle >= m_twoPi)
								{
									angle = angle - m_twoPi;
								}
								else
								{
									break;
								}
							}
							t = angle / m_twoPi;
						}
						else if (m_type == eGradientType.Reflected)
						{
							t = Math.Abs(u) / m_length;
						}
						else
						{
							t = (Math.Abs(u) + Math.Abs(v)) / m_length;
						}
						if (t < 0.0)
						{
							t = 0.0;
						}
						if (t > 1.0)
						{
							t = 1.0;
						}
						if (m_reverse)
						{
							t = 1.0 - t;
						}
						byte* pixel = row + (x * 4);
						pixel[0] = ClampToByte(m_startR + ((m_endR - m_startR) * t));
						pixel[1] = ClampToByte(m_startG + ((m_endG - m_startG) * t));
						pixel[2] = ClampToByte(m_startB + ((m_endB - m_startB) * t));
						pixel[3] = ClampToByte(m_startA + ((m_endA - m_startA) * t));
					}
				}
			}
		}

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

		public static unsafe void Fill(SKBitmap target, eGradientType type, float startX, float startY, float endX, float endY, SKColor startColor, SKColor endColor, bool reverse)
		{
			int width = target.Width;
			int height = target.Height;
			if (width <= 0 || height <= 0)
			{
				return;
			}
			int rowBytes = target.RowBytes;
			byte* basePixels = (byte*)target.GetPixels().ToPointer();
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
				for (int y = 0; y < height; y++)
				{
					byte* row = basePixels + (y * rowBytes);
					for (int x = 0; x < width; x++)
					{
						byte* pixel = row + (x * 4);
						pixel[0] = solid.Red;
						pixel[1] = solid.Green;
						pixel[2] = solid.Blue;
						pixel[3] = solid.Alpha;
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
			double axisAngle = Math.Atan2(ay, ax);
			double twoPi = 2.0 * Math.PI;
			FillWorker worker = new FillWorker();
			worker.m_basePixels = basePixels;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_type = type;
			worker.m_startX = startX;
			worker.m_startY = startY;
			worker.m_reverse = reverse;
			worker.m_length = length;
			worker.m_ux = ux;
			worker.m_uy = uy;
			worker.m_px = px;
			worker.m_py = py;
			worker.m_startR = startR;
			worker.m_startG = startG;
			worker.m_startB = startB;
			worker.m_startA = startA;
			worker.m_endR = endR;
			worker.m_endG = endG;
			worker.m_endB = endB;
			worker.m_endA = endA;
			worker.m_axisAngle = axisAngle;
			worker.m_twoPi = twoPi;
			RowBands.Run(0, height, worker.Band);
		}
	}
}
