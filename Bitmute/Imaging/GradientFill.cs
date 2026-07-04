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
			for (int y = 0; y < height; y++)
			{
				byte* row = basePixels + (y * rowBytes);
				double ry = (y + 0.5) - startY;
				for (int x = 0; x < width; x++)
				{
					double rx = (x + 0.5) - startX;
					double u = (rx * ux) + (ry * uy);
					double v = (rx * px) + (ry * py);
					double t = 0.0;
					if (type == eGradientType.Linear)
					{
						t = u / length;
					}
					else if (type == eGradientType.Radial)
					{
						t = Math.Sqrt((rx * rx) + (ry * ry)) / length;
					}
					else if (type == eGradientType.Angle)
					{
						double angle = Math.Atan2(ry, rx) - axisAngle;
						for (;;)
						{
							if (angle < 0.0)
							{
								angle = angle + twoPi;
							}
							else
							{
								break;
							}
						}
						for (;;)
						{
							if (angle >= twoPi)
							{
								angle = angle - twoPi;
							}
							else
							{
								break;
							}
						}
						t = angle / twoPi;
					}
					else if (type == eGradientType.Reflected)
					{
						t = Math.Abs(u) / length;
					}
					else
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
					byte* pixel = row + (x * 4);
					pixel[0] = ClampToByte(startR + ((endR - startR) * t));
					pixel[1] = ClampToByte(startG + ((endG - startG) * t));
					pixel[2] = ClampToByte(startB + ((endB - startB) * t));
					pixel[3] = ClampToByte(startA + ((endA - startA) * t));
				}
			}
		}
	}
}
