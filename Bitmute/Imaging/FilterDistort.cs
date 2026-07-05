using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterDistort
	{
		private const int KindPinch = 0;
		private const int KindPolar = 1;
		private const int KindRipple = 2;
		private const int KindShear = 3;
		private const int KindSpherize = 4;
		private const int KindTwirl = 5;
		private const int KindWave = 6;

		private const int EdgeTransparent = 0;
		private const int EdgeClamp = 1;
		private const int EdgeWrapX = 2;

		private const double CenterEpsilon = 0.000001;
		private const double HalfPi = 1.5707963267948966;

		private sealed unsafe class DistortWorker
		{
			public byte* m_rawBase;
			public int m_rawStride;
			public byte* m_premulBase;
			public int m_premulStride;
			public byte* m_destinationBase;
			public int m_destinationStride;
			public int m_width;
			public int m_height;
			public int m_kind;
			public int m_edgeMode;
			public int m_mode;
			public double m_centerX;
			public double m_centerY;
			public double m_radiusX;
			public double m_radiusY;
			public double m_amount;
			public double m_amplitude;
			public double m_wavelengthX;
			public double m_wavelengthY;
			public double m_spanX;
			public double m_spanY;

			private void Sample(double srcX, double srcY, byte* destinationPixel)
			{
				if (srcX >= 0.0 && srcY >= 0.0 && srcX <= m_width - 1 && srcY <= m_height - 1)
				{
					double flooredX = Math.Floor(srcX);
					double flooredY = Math.Floor(srcY);
					if (srcX == flooredX && srcY == flooredY)
					{
						int exactX = (int)flooredX;
						int exactY = (int)flooredY;
						byte* rawPixel = m_rawBase + ((long)exactY * m_rawStride) + (exactX * 4);
						destinationPixel[0] = rawPixel[0];
						destinationPixel[1] = rawPixel[1];
						destinationPixel[2] = rawPixel[2];
						destinationPixel[3] = rawPixel[3];
						return;
					}
				}
				int baseX = (int)Math.Floor(srcX);
				int baseY = (int)Math.Floor(srcY);
				double fractionX = srcX - baseX;
				double fractionY = srcY - baseY;
				double sumRed = 0.0;
				double sumGreen = 0.0;
				double sumBlue = 0.0;
				double sumAlpha = 0.0;
				for (int tapY = 0; tapY < 2; tapY++)
				{
					double weightY = 1.0 - fractionY;
					if (tapY == 1)
					{
						weightY = fractionY;
					}
					if (weightY <= 0.0)
					{
						continue;
					}
					int sampleY = baseY + tapY;
					if (m_edgeMode == EdgeTransparent)
					{
						if (sampleY < 0 || sampleY >= m_height)
						{
							continue;
						}
					}
					else
					{
						if (sampleY < 0)
						{
							sampleY = 0;
						}
						if (sampleY > m_height - 1)
						{
							sampleY = m_height - 1;
						}
					}
					byte* premulRow = m_premulBase + ((long)sampleY * m_premulStride);
					for (int tapX = 0; tapX < 2; tapX++)
					{
						double weightX = 1.0 - fractionX;
						if (tapX == 1)
						{
							weightX = fractionX;
						}
						if (weightX <= 0.0)
						{
							continue;
						}
						int sampleX = baseX + tapX;
						if (m_edgeMode == EdgeWrapX)
						{
							sampleX = sampleX % m_width;
							if (sampleX < 0)
							{
								sampleX = sampleX + m_width;
							}
						}
						else if (m_edgeMode == EdgeClamp)
						{
							if (sampleX < 0)
							{
								sampleX = 0;
							}
							if (sampleX > m_width - 1)
							{
								sampleX = m_width - 1;
							}
						}
						else
						{
							if (sampleX < 0 || sampleX >= m_width)
							{
								continue;
							}
						}
						byte* premulPixel = premulRow + (sampleX * 4);
						double weight = weightX * weightY;
						sumRed += premulPixel[0] * weight;
						sumGreen += premulPixel[1] * weight;
						sumBlue += premulPixel[2] * weight;
						sumAlpha += premulPixel[3] * weight;
					}
				}
				if (sumAlpha <= 0.0)
				{
					destinationPixel[0] = 0;
					destinationPixel[1] = 0;
					destinationPixel[2] = 0;
					destinationPixel[3] = 0;
					return;
				}
				destinationPixel[0] = ClampByte((sumRed * 255.0) / sumAlpha);
				destinationPixel[1] = ClampByte((sumGreen * 255.0) / sumAlpha);
				destinationPixel[2] = ClampByte((sumBlue * 255.0) / sumAlpha);
				destinationPixel[3] = ClampByte(sumAlpha);
			}

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						byte* destinationPixel = destinationRow + (x * 4);
						double srcX = x;
						double srcY = y;
						if (m_kind == KindPinch)
						{
							double dx = x - m_centerX;
							double dy = y - m_centerY;
							double nx = dx / m_radiusX;
							double ny = dy / m_radiusY;
							double distance = Math.Sqrt((nx * nx) + (ny * ny));
							if (distance > CenterEpsilon && distance < 1.0)
							{
								double curve = Math.Sin(HalfPi * distance);
								double factor = Math.Pow(curve, -m_amount);
								srcX = m_centerX + (dx * factor);
								srcY = m_centerY + (dy * factor);
							}
						}
						else if (m_kind == KindPolar)
						{
							if (m_mode == 0)
							{
								double nx = (x - m_centerX) / m_radiusX;
								double ny = (y - m_centerY) / m_radiusY;
								double radius = Math.Sqrt((nx * nx) + (ny * ny));
								double angle = Math.Atan2(nx, -ny);
								srcX = ((angle + Math.PI) / (2.0 * Math.PI)) * m_spanX;
								srcY = radius * m_spanY;
							}
							else
							{
								double angle = -Math.PI + ((2.0 * Math.PI) * (x / m_spanX));
								double radius = y / m_spanY;
								srcX = m_centerX + (radius * m_radiusX * Math.Sin(angle));
								srcY = m_centerY - (radius * m_radiusY * Math.Cos(angle));
							}
						}
						else if (m_kind == KindRipple)
						{
							srcX = x + (m_amount * Math.Sin((2.0 * Math.PI) * (y / m_wavelengthY)));
							srcY = y + (m_amount * Math.Sin((2.0 * Math.PI) * (x / m_wavelengthX)));
						}
						else if (m_kind == KindShear)
						{
							srcX = x + (m_amplitude * Math.Sin(Math.PI * (y / m_spanY)));
						}
						else if (m_kind == KindSpherize)
						{
							if (m_mode == 1)
							{
								double dx = x - m_centerX;
								double distance = Math.Abs(dx) / m_radiusX;
								if (distance > CenterEpsilon && distance <= 1.0)
								{
									double factor = SpherizeFactor(distance, m_amount);
									srcX = m_centerX + (dx * factor);
								}
							}
							else if (m_mode == 2)
							{
								double dy = y - m_centerY;
								double distance = Math.Abs(dy) / m_radiusY;
								if (distance > CenterEpsilon && distance <= 1.0)
								{
									double factor = SpherizeFactor(distance, m_amount);
									srcY = m_centerY + (dy * factor);
								}
							}
							else
							{
								double dx = x - m_centerX;
								double dy = y - m_centerY;
								double nx = dx / m_radiusX;
								double ny = dy / m_radiusY;
								double distance = Math.Sqrt((nx * nx) + (ny * ny));
								if (distance > CenterEpsilon && distance < 1.0)
								{
									double factor = SpherizeFactor(distance, m_amount);
									srcX = m_centerX + (dx * factor);
									srcY = m_centerY + (dy * factor);
								}
							}
						}
						else if (m_kind == KindTwirl)
						{
							double dx = x - m_centerX;
							double dy = y - m_centerY;
							double nx = dx / m_radiusX;
							double ny = dy / m_radiusY;
							double distance = Math.Sqrt((nx * nx) + (ny * ny));
							if (distance < 1.0)
							{
								double falloff = 1.0 - distance;
								double rotation = m_amount * falloff * falloff;
								double cosine = Math.Cos(rotation);
								double sine = Math.Sin(rotation);
								srcX = m_centerX + ((dx * cosine) - (dy * sine));
								srcY = m_centerY + ((dx * sine) + (dy * cosine));
							}
						}
						else
						{
							srcX = x + (m_amplitude * WaveValue(m_mode, y / m_wavelengthY));
							srcY = y + (m_amplitude * WaveValue(m_mode, (x / m_wavelengthX) + 0.25));
						}
						Sample(srcX, srcY, destinationPixel);
					}
				}
			}
		}

		private static byte ClampByte(double value)
		{
			if (value < 0.0)
			{
				return 0;
			}
			if (value > 255.0)
			{
				return 255;
			}
			return (byte)Math.Round(value);
		}

		private static int ClampRange(int value, int minimum, int maximum)
		{
			if (value < minimum)
			{
				return minimum;
			}
			if (value > maximum)
			{
				return maximum;
			}
			return value;
		}

		private static double SpherizeFactor(double distance, double amount)
		{
			double mapped;
			if (amount >= 0.0)
			{
				double sphere = Math.Asin(distance) * (2.0 / Math.PI);
				mapped = distance + (amount * (sphere - distance));
			}
			else
			{
				double sphere = Math.Sin(distance * HalfPi);
				mapped = distance + ((-amount) * (sphere - distance));
			}
			return mapped / distance;
		}

		private static double WaveValue(int type, double t)
		{
			if (type == 1)
			{
				double fraction = t - Math.Floor(t);
				if (fraction < 0.25)
				{
					return 4.0 * fraction;
				}
				if (fraction < 0.75)
				{
					return 2.0 - (4.0 * fraction);
				}
				return (4.0 * fraction) - 4.0;
			}
			if (type == 2)
			{
				double fraction = t - Math.Floor(t);
				if (fraction < 0.5)
				{
					return 1.0;
				}
				return -1.0;
			}
			return Math.Sin((2.0 * Math.PI) * t);
		}

		private static void SetEllipse(DistortWorker worker, int width, int height)
		{
			worker.m_centerX = (width - 1) / 2.0;
			worker.m_centerY = (height - 1) / 2.0;
			double radiusX = (width - 1) / 2.0;
			double radiusY = (height - 1) / 2.0;
			if (radiusX < 0.5)
			{
				radiusX = 0.5;
			}
			if (radiusY < 0.5)
			{
				radiusY = 0.5;
			}
			worker.m_radiusX = radiusX;
			worker.m_radiusY = radiusY;
		}

		private static unsafe void Apply(SKBitmap bitmap, DistortWorker worker)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			if (width < 1 || height < 1)
			{
				return;
			}
			SKBitmap scratchRaw = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap scratchPremul = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			int destinationStride = bitmap.RowBytes;
			byte* rawBase = (byte*)scratchRaw.GetPixels().ToPointer();
			int rawStride = scratchRaw.RowBytes;
			byte* premulBase = (byte*)scratchPremul.GetPixels().ToPointer();
			int premulStride = scratchPremul.RowBytes;
			long rowLength = (long)width * 4;
			for (int y = 0; y < height; y++)
			{
				byte* sourceRow = destinationBase + ((long)y * destinationStride);
				byte* rawRow = rawBase + ((long)y * rawStride);
				byte* premulRow = premulBase + ((long)y * premulStride);
				Buffer.MemoryCopy(sourceRow, rawRow, rowLength, rowLength);
				for (int x = 0; x < width; x++)
				{
					int pixelOffset = x * 4;
					int alpha = sourceRow[pixelOffset + 3];
					premulRow[pixelOffset + 0] = (byte)(((sourceRow[pixelOffset + 0] * alpha) + 127) / 255);
					premulRow[pixelOffset + 1] = (byte)(((sourceRow[pixelOffset + 1] * alpha) + 127) / 255);
					premulRow[pixelOffset + 2] = (byte)(((sourceRow[pixelOffset + 2] * alpha) + 127) / 255);
					premulRow[pixelOffset + 3] = (byte)alpha;
				}
			}
			worker.m_rawBase = rawBase;
			worker.m_rawStride = rawStride;
			worker.m_premulBase = premulBase;
			worker.m_premulStride = premulStride;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationStride = destinationStride;
			worker.m_width = width;
			worker.m_height = height;
			RowBands.Run(0, height, worker.Band);
			scratchRaw.Dispose();
			scratchPremul.Dispose();
		}

		public static void Pinch(SKBitmap bitmap, int amount)
		{
			if (bitmap == null)
			{
				return;
			}
			amount = ClampRange(amount, -100, 100);
			if (amount == 0)
			{
				return;
			}
			DistortWorker worker = new DistortWorker();
			worker.m_kind = KindPinch;
			worker.m_edgeMode = EdgeTransparent;
			worker.m_amount = amount / 100.0;
			SetEllipse(worker, bitmap.Width, bitmap.Height);
			Apply(bitmap, worker);
		}

		public static void PolarCoordinates(SKBitmap bitmap, int direction)
		{
			if (bitmap == null)
			{
				return;
			}
			DistortWorker worker = new DistortWorker();
			worker.m_kind = KindPolar;
			worker.m_edgeMode = EdgeTransparent;
			worker.m_mode = 0;
			if (direction != 0)
			{
				worker.m_mode = 1;
			}
			SetEllipse(worker, bitmap.Width, bitmap.Height);
			double spanX = bitmap.Width - 1;
			double spanY = bitmap.Height - 1;
			if (spanX < 1.0)
			{
				spanX = 1.0;
			}
			if (spanY < 1.0)
			{
				spanY = 1.0;
			}
			worker.m_spanX = spanX;
			worker.m_spanY = spanY;
			Apply(bitmap, worker);
		}

		public static void Ripple(SKBitmap bitmap, int amount, int size)
		{
			if (bitmap == null)
			{
				return;
			}
			amount = ClampRange(amount, -999, 999);
			if (amount == 0)
			{
				return;
			}
			double wavelength = 32.0;
			if (size == 0)
			{
				wavelength = 12.0;
			}
			if (size == 2)
			{
				wavelength = 64.0;
			}
			DistortWorker worker = new DistortWorker();
			worker.m_kind = KindRipple;
			worker.m_edgeMode = EdgeClamp;
			worker.m_amount = amount / 100.0;
			worker.m_wavelengthX = wavelength;
			worker.m_wavelengthY = wavelength;
			Apply(bitmap, worker);
		}

		public static void Shear(SKBitmap bitmap, int amount, int undefinedAreas)
		{
			if (bitmap == null)
			{
				return;
			}
			amount = ClampRange(amount, -100, 100);
			if (amount == 0)
			{
				return;
			}
			DistortWorker worker = new DistortWorker();
			worker.m_kind = KindShear;
			worker.m_edgeMode = EdgeWrapX;
			if (undefinedAreas == 1)
			{
				worker.m_edgeMode = EdgeClamp;
			}
			worker.m_amplitude = (amount / 100.0) * (bitmap.Width / 4.0);
			double spanY = bitmap.Height;
			if (spanY < 1.0)
			{
				spanY = 1.0;
			}
			worker.m_spanY = spanY;
			Apply(bitmap, worker);
		}

		public static void Spherize(SKBitmap bitmap, int amount, int mode)
		{
			if (bitmap == null)
			{
				return;
			}
			amount = ClampRange(amount, -100, 100);
			if (amount == 0)
			{
				return;
			}
			DistortWorker worker = new DistortWorker();
			worker.m_kind = KindSpherize;
			worker.m_edgeMode = EdgeTransparent;
			worker.m_amount = amount / 100.0;
			worker.m_mode = mode;
			SetEllipse(worker, bitmap.Width, bitmap.Height);
			Apply(bitmap, worker);
		}

		public static void Twirl(SKBitmap bitmap, int angle)
		{
			if (bitmap == null)
			{
				return;
			}
			angle = ClampRange(angle, -999, 999);
			if (angle == 0)
			{
				return;
			}
			DistortWorker worker = new DistortWorker();
			worker.m_kind = KindTwirl;
			worker.m_edgeMode = EdgeTransparent;
			worker.m_amount = (angle * Math.PI) / 180.0;
			SetEllipse(worker, bitmap.Width, bitmap.Height);
			Apply(bitmap, worker);
		}

		public static void Wave(SKBitmap bitmap, int wavelength, int amplitude, int type)
		{
			if (bitmap == null)
			{
				return;
			}
			if (amplitude == 0)
			{
				return;
			}
			double length = wavelength;
			if (length < 1.0)
			{
				length = 1.0;
			}
			DistortWorker worker = new DistortWorker();
			worker.m_kind = KindWave;
			worker.m_edgeMode = EdgeClamp;
			worker.m_amplitude = amplitude;
			worker.m_wavelengthX = length;
			worker.m_wavelengthY = length;
			worker.m_mode = type;
			Apply(bitmap, worker);
		}
	}
}
