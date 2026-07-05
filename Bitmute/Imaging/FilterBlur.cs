using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterBlur
	{
		private const int RadialSampleCount = 16;

		private sealed unsafe class PremultiplyWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						int pixelOffset = x * 4;
						int alpha = sourceRow[pixelOffset + 3];
						destinationRow[pixelOffset + 0] = (byte)(((sourceRow[pixelOffset + 0] * alpha) + 127) / 255);
						destinationRow[pixelOffset + 1] = (byte)(((sourceRow[pixelOffset + 1] * alpha) + 127) / 255);
						destinationRow[pixelOffset + 2] = (byte)(((sourceRow[pixelOffset + 2] * alpha) + 127) / 255);
						destinationRow[pixelOffset + 3] = (byte)alpha;
					}
				}
			}
		}

		private sealed unsafe class UnpremultiplyWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						int pixelOffset = x * 4;
						int alpha = sourceRow[pixelOffset + 3];
						if (alpha == 0)
						{
							destinationRow[pixelOffset + 0] = 0;
							destinationRow[pixelOffset + 1] = 0;
							destinationRow[pixelOffset + 2] = 0;
							destinationRow[pixelOffset + 3] = 0;
							continue;
						}
						int red = ((sourceRow[pixelOffset + 0] * 255) + (alpha / 2)) / alpha;
						int green = ((sourceRow[pixelOffset + 1] * 255) + (alpha / 2)) / alpha;
						int blue = ((sourceRow[pixelOffset + 2] * 255) + (alpha / 2)) / alpha;
						if (red > 255)
						{
							red = 255;
						}
						if (green > 255)
						{
							green = 255;
						}
						if (blue > 255)
						{
							blue = 255;
						}
						destinationRow[pixelOffset + 0] = (byte)red;
						destinationRow[pixelOffset + 1] = (byte)green;
						destinationRow[pixelOffset + 2] = (byte)blue;
						destinationRow[pixelOffset + 3] = (byte)alpha;
					}
				}
			}
		}

		private sealed unsafe class FillRgbWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public byte m_red;
			public byte m_green;
			public byte m_blue;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						pixel[0] = m_red;
						pixel[1] = m_green;
						pixel[2] = m_blue;
					}
				}
			}
		}

		private sealed unsafe class Kernel3x3Worker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_height;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					int yUp = y - 1;
					if (yUp < 0)
					{
						yUp = 0;
					}
					int yDown = y + 1;
					if (yDown > m_height - 1)
					{
						yDown = m_height - 1;
					}
					byte* rowUp = m_sourceBase + ((long)yUp * m_sourceStride);
					byte* rowMid = m_sourceBase + ((long)y * m_sourceStride);
					byte* rowDown = m_sourceBase + ((long)yDown * m_sourceStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						int xLeft = x - 1;
						if (xLeft < 0)
						{
							xLeft = 0;
						}
						int xRight = x + 1;
						if (xRight > m_width - 1)
						{
							xRight = m_width - 1;
						}
						int leftOffset = xLeft * 4;
						int midOffset = x * 4;
						int rightOffset = xRight * 4;
						byte* destinationPixel = destinationRow + midOffset;
						for (int channel = 0; channel < 4; channel++)
						{
							int sum = rowUp[leftOffset + channel] + (2 * rowUp[midOffset + channel]) + rowUp[rightOffset + channel] + (2 * rowMid[leftOffset + channel]) + (4 * rowMid[midOffset + channel]) + (2 * rowMid[rightOffset + channel]) + rowDown[leftOffset + channel] + (2 * rowDown[midOffset + channel]) + rowDown[rightOffset + channel];
							destinationPixel[channel] = (byte)((sum + 8) / 16);
						}
					}
				}
			}
		}

		private sealed unsafe class BoxBlurHorizontalWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_radius;
			public int m_windowLength;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					long sumRed = 0;
					long sumGreen = 0;
					long sumBlue = 0;
					long sumAlpha = 0;
					for (int offset = -m_radius; offset <= m_radius; offset++)
					{
						int sampleX = offset;
						if (sampleX < 0)
						{
							sampleX = 0;
						}
						if (sampleX > m_width - 1)
						{
							sampleX = m_width - 1;
						}
						int sampleOffset = sampleX * 4;
						sumRed += sourceRow[sampleOffset + 0];
						sumGreen += sourceRow[sampleOffset + 1];
						sumBlue += sourceRow[sampleOffset + 2];
						sumAlpha += sourceRow[sampleOffset + 3];
					}
					for (int x = 0; x < m_width; x++)
					{
						int pixelOffset = x * 4;
						destinationRow[pixelOffset + 0] = (byte)(sumRed / m_windowLength);
						destinationRow[pixelOffset + 1] = (byte)(sumGreen / m_windowLength);
						destinationRow[pixelOffset + 2] = (byte)(sumBlue / m_windowLength);
						destinationRow[pixelOffset + 3] = (byte)(sumAlpha / m_windowLength);
						int leavingX = x - m_radius;
						if (leavingX < 0)
						{
							leavingX = 0;
						}
						if (leavingX > m_width - 1)
						{
							leavingX = m_width - 1;
						}
						int enteringX = x + m_radius + 1;
						if (enteringX < 0)
						{
							enteringX = 0;
						}
						if (enteringX > m_width - 1)
						{
							enteringX = m_width - 1;
						}
						int leavingOffset = leavingX * 4;
						int enteringOffset = enteringX * 4;
						sumRed += sourceRow[enteringOffset + 0] - sourceRow[leavingOffset + 0];
						sumGreen += sourceRow[enteringOffset + 1] - sourceRow[leavingOffset + 1];
						sumBlue += sourceRow[enteringOffset + 2] - sourceRow[leavingOffset + 2];
						sumAlpha += sourceRow[enteringOffset + 3] - sourceRow[leavingOffset + 3];
					}
				}
			}
		}

		private sealed unsafe class BoxBlurVerticalWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_height;
			public int m_radius;
			public int m_windowLength;

			public void Band(int start, int end)
			{
				for (int x = start; x < end; x++)
				{
					int pixelOffset = x * 4;
					long sumRed = 0;
					long sumGreen = 0;
					long sumBlue = 0;
					long sumAlpha = 0;
					for (int offset = -m_radius; offset <= m_radius; offset++)
					{
						int sampleY = offset;
						if (sampleY < 0)
						{
							sampleY = 0;
						}
						if (sampleY > m_height - 1)
						{
							sampleY = m_height - 1;
						}
						byte* sampleRow = m_sourceBase + ((long)sampleY * m_sourceStride);
						sumRed += sampleRow[pixelOffset + 0];
						sumGreen += sampleRow[pixelOffset + 1];
						sumBlue += sampleRow[pixelOffset + 2];
						sumAlpha += sampleRow[pixelOffset + 3];
					}
					for (int y = 0; y < m_height; y++)
					{
						byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
						destinationRow[pixelOffset + 0] = (byte)(sumRed / m_windowLength);
						destinationRow[pixelOffset + 1] = (byte)(sumGreen / m_windowLength);
						destinationRow[pixelOffset + 2] = (byte)(sumBlue / m_windowLength);
						destinationRow[pixelOffset + 3] = (byte)(sumAlpha / m_windowLength);
						int leavingY = y - m_radius;
						if (leavingY < 0)
						{
							leavingY = 0;
						}
						if (leavingY > m_height - 1)
						{
							leavingY = m_height - 1;
						}
						int enteringY = y + m_radius + 1;
						if (enteringY < 0)
						{
							enteringY = 0;
						}
						if (enteringY > m_height - 1)
						{
							enteringY = m_height - 1;
						}
						byte* leavingRow = m_sourceBase + ((long)leavingY * m_sourceStride);
						byte* enteringRow = m_sourceBase + ((long)enteringY * m_sourceStride);
						sumRed += enteringRow[pixelOffset + 0] - leavingRow[pixelOffset + 0];
						sumGreen += enteringRow[pixelOffset + 1] - leavingRow[pixelOffset + 1];
						sumBlue += enteringRow[pixelOffset + 2] - leavingRow[pixelOffset + 2];
						sumAlpha += enteringRow[pixelOffset + 3] - leavingRow[pixelOffset + 3];
					}
				}
			}
		}

		private sealed unsafe class MotionBlurWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_height;
			public double[] m_offsetX;
			public double[] m_offsetY;
			public int m_count;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						double sumRed = 0.0;
						double sumGreen = 0.0;
						double sumBlue = 0.0;
						double sumAlpha = 0.0;
						for (int index = 0; index < m_count; index++)
						{
							double red;
							double green;
							double blue;
							double alpha;
							SampleBilinear(m_sourceBase, m_sourceStride, m_width, m_height, x + m_offsetX[index], y + m_offsetY[index], out red, out green, out blue, out alpha);
							sumRed += red;
							sumGreen += green;
							sumBlue += blue;
							sumAlpha += alpha;
						}
						byte* destinationPixel = destinationRow + (x * 4);
						destinationPixel[0] = ClampByte(sumRed / m_count);
						destinationPixel[1] = ClampByte(sumGreen / m_count);
						destinationPixel[2] = ClampByte(sumBlue / m_count);
						destinationPixel[3] = ClampByte(sumAlpha / m_count);
					}
				}
			}
		}

		private sealed unsafe class RadialSpinWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_height;
			public double m_centerX;
			public double m_centerY;
			public double[] m_deltas;
			public int m_count;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					double deltaY = y - m_centerY;
					for (int x = 0; x < m_width; x++)
					{
						double deltaX = x - m_centerX;
						double radius = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
						double baseAngle = Math.Atan2(deltaY, deltaX);
						double sumRed = 0.0;
						double sumGreen = 0.0;
						double sumBlue = 0.0;
						double sumAlpha = 0.0;
						for (int index = 0; index < m_count; index++)
						{
							double angle = baseAngle + m_deltas[index];
							double sampleX = m_centerX + (radius * Math.Cos(angle));
							double sampleY = m_centerY + (radius * Math.Sin(angle));
							double red;
							double green;
							double blue;
							double alpha;
							SampleBilinear(m_sourceBase, m_sourceStride, m_width, m_height, sampleX, sampleY, out red, out green, out blue, out alpha);
							sumRed += red;
							sumGreen += green;
							sumBlue += blue;
							sumAlpha += alpha;
						}
						byte* destinationPixel = destinationRow + (x * 4);
						destinationPixel[0] = ClampByte(sumRed / m_count);
						destinationPixel[1] = ClampByte(sumGreen / m_count);
						destinationPixel[2] = ClampByte(sumBlue / m_count);
						destinationPixel[3] = ClampByte(sumAlpha / m_count);
					}
				}
			}
		}

		private sealed unsafe class RadialZoomWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_height;
			public double m_centerX;
			public double m_centerY;
			public double[] m_scales;
			public int m_count;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					double deltaY = y - m_centerY;
					for (int x = 0; x < m_width; x++)
					{
						double deltaX = x - m_centerX;
						double sumRed = 0.0;
						double sumGreen = 0.0;
						double sumBlue = 0.0;
						double sumAlpha = 0.0;
						for (int index = 0; index < m_count; index++)
						{
							double scale = m_scales[index];
							double sampleX = m_centerX + (deltaX * scale);
							double sampleY = m_centerY + (deltaY * scale);
							double red;
							double green;
							double blue;
							double alpha;
							SampleBilinear(m_sourceBase, m_sourceStride, m_width, m_height, sampleX, sampleY, out red, out green, out blue, out alpha);
							sumRed += red;
							sumGreen += green;
							sumBlue += blue;
							sumAlpha += alpha;
						}
						byte* destinationPixel = destinationRow + (x * 4);
						destinationPixel[0] = ClampByte(sumRed / m_count);
						destinationPixel[1] = ClampByte(sumGreen / m_count);
						destinationPixel[2] = ClampByte(sumBlue / m_count);
						destinationPixel[3] = ClampByte(sumAlpha / m_count);
					}
				}
			}
		}

		private static unsafe void SampleBilinear(byte* sourceBase, int sourceStride, int width, int height, double sampleX, double sampleY, out double red, out double green, out double blue, out double alpha)
		{
			double maxX = width - 1;
			double maxY = height - 1;
			if (sampleX < 0.0)
			{
				sampleX = 0.0;
			}
			if (sampleX > maxX)
			{
				sampleX = maxX;
			}
			if (sampleY < 0.0)
			{
				sampleY = 0.0;
			}
			if (sampleY > maxY)
			{
				sampleY = maxY;
			}
			int x0 = (int)sampleX;
			int y0 = (int)sampleY;
			double fx = sampleX - x0;
			double fy = sampleY - y0;
			int x1 = x0 + 1;
			if (x1 > width - 1)
			{
				x1 = width - 1;
			}
			int y1 = y0 + 1;
			if (y1 > height - 1)
			{
				y1 = height - 1;
			}
			double w00 = (1.0 - fx) * (1.0 - fy);
			double w10 = fx * (1.0 - fy);
			double w01 = (1.0 - fx) * fy;
			double w11 = fx * fy;
			byte* row0 = sourceBase + ((long)y0 * sourceStride);
			byte* row1 = sourceBase + ((long)y1 * sourceStride);
			byte* p00 = row0 + (x0 * 4);
			byte* p10 = row0 + (x1 * 4);
			byte* p01 = row1 + (x0 * 4);
			byte* p11 = row1 + (x1 * 4);
			red = (w00 * p00[0]) + (w10 * p10[0]) + (w01 * p01[0]) + (w11 * p11[0]);
			green = (w00 * p00[1]) + (w10 * p10[1]) + (w01 * p01[1]) + (w11 * p11[1]);
			blue = (w00 * p00[2]) + (w10 * p10[2]) + (w01 * p01[2]) + (w11 * p11[2]);
			alpha = (w00 * p00[3]) + (w10 * p10[3]) + (w01 * p01[3]) + (w11 * p11[3]);
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

		private static unsafe void Premultiply(SKBitmap source, SKBitmap destination)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			PremultiplyWorker worker = new PremultiplyWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = sourceStride;
			worker.m_destinationStride = destinationStride;
			worker.m_width = width;
			RowBands.Run(0, height, worker.Band);
		}

		private static unsafe void Unpremultiply(SKBitmap source, SKBitmap destination)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			UnpremultiplyWorker worker = new UnpremultiplyWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = sourceStride;
			worker.m_destinationStride = destinationStride;
			worker.m_width = width;
			RowBands.Run(0, height, worker.Band);
		}

		private static unsafe void Kernel3x3(SKBitmap source, SKBitmap destination)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			Kernel3x3Worker worker = new Kernel3x3Worker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = sourceStride;
			worker.m_destinationStride = destinationStride;
			worker.m_width = width;
			worker.m_height = height;
			RowBands.Run(0, height, worker.Band);
		}

		private static unsafe void BoxBlurHorizontal(SKBitmap source, SKBitmap destination, int radius)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			int windowLength = (2 * radius) + 1;
			BoxBlurHorizontalWorker worker = new BoxBlurHorizontalWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = sourceStride;
			worker.m_destinationStride = destinationStride;
			worker.m_width = width;
			worker.m_radius = radius;
			worker.m_windowLength = windowLength;
			RowBands.Run(0, height, worker.Band);
		}

		private static unsafe void BoxBlurVertical(SKBitmap source, SKBitmap destination, int radius)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			int windowLength = (2 * radius) + 1;
			BoxBlurVerticalWorker worker = new BoxBlurVerticalWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = sourceStride;
			worker.m_destinationStride = destinationStride;
			worker.m_height = height;
			worker.m_radius = radius;
			worker.m_windowLength = windowLength;
			RowBands.Run(0, width, worker.Band);
		}

		public static unsafe void Average(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			long sumRed = 0;
			long sumGreen = 0;
			long sumBlue = 0;
			long sumAlpha = 0;
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					int alpha = pixel[3];
					sumRed += pixel[0] * alpha;
					sumGreen += pixel[1] * alpha;
					sumBlue += pixel[2] * alpha;
					sumAlpha += alpha;
				}
			}
			if (sumAlpha == 0)
			{
				return;
			}
			byte meanRed = ClampByte((double)sumRed / sumAlpha);
			byte meanGreen = ClampByte((double)sumGreen / sumAlpha);
			byte meanBlue = ClampByte((double)sumBlue / sumAlpha);
			FillRgbWorker worker = new FillRgbWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_red = meanRed;
			worker.m_green = meanGreen;
			worker.m_blue = meanBlue;
			RowBands.Run(0, height, worker.Band);
		}

		public static void Blur(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap bufferA = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap bufferB = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, bufferA);
			Kernel3x3(bufferA, bufferB);
			Unpremultiply(bufferB, bitmap);
			bufferA.Dispose();
			bufferB.Dispose();
		}

		public static void BlurMore(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap bufferA = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap bufferB = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, bufferA);
			Kernel3x3(bufferA, bufferB);
			Kernel3x3(bufferB, bufferA);
			Kernel3x3(bufferA, bufferB);
			Unpremultiply(bufferB, bitmap);
			bufferA.Dispose();
			bufferB.Dispose();
		}

		public static void BoxBlur(SKBitmap bitmap, int radius)
		{
			if (radius < 1)
			{
				radius = 1;
			}
			if (radius > 100)
			{
				radius = 100;
			}
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap bufferA = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap bufferB = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, bufferA);
			BoxBlurHorizontal(bufferA, bufferB, radius);
			BoxBlurVertical(bufferB, bufferA, radius);
			Unpremultiply(bufferA, bitmap);
			bufferA.Dispose();
			bufferB.Dispose();
		}

		public static unsafe void MotionBlur(SKBitmap bitmap, int angle, int distance)
		{
			if (angle < -90)
			{
				angle = -90;
			}
			if (angle > 90)
			{
				angle = 90;
			}
			if (distance < 1)
			{
				distance = 1;
			}
			if (distance > 200)
			{
				distance = 200;
			}
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap bufferA = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap bufferB = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, bufferA);
			double radians = angle * Math.PI / 180.0;
			double directionX = Math.Cos(radians);
			double directionY = Math.Sin(radians);
			int sampleCount = distance;
			if (sampleCount > 32)
			{
				sampleCount = 32;
			}
			double[] offsetX = new double[sampleCount];
			double[] offsetY = new double[sampleCount];
			for (int index = 0; index < sampleCount; index++)
			{
				double t = 0.0;
				if (sampleCount > 1)
				{
					t = (index * (distance - 1.0) / (sampleCount - 1.0)) - ((distance - 1) / 2.0);
				}
				offsetX[index] = t * directionX;
				offsetY[index] = t * directionY;
			}
			MotionBlurWorker worker = new MotionBlurWorker();
			worker.m_sourceBase = (byte*)bufferA.GetPixels().ToPointer();
			worker.m_destinationBase = (byte*)bufferB.GetPixels().ToPointer();
			worker.m_sourceStride = bufferA.RowBytes;
			worker.m_destinationStride = bufferB.RowBytes;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_offsetX = offsetX;
			worker.m_offsetY = offsetY;
			worker.m_count = sampleCount;
			RowBands.Run(0, height, worker.Band);
			Unpremultiply(bufferB, bitmap);
			bufferA.Dispose();
			bufferB.Dispose();
		}

		public static unsafe void RadialBlur(SKBitmap bitmap, int amount, int method)
		{
			if (amount < 1)
			{
				amount = 1;
			}
			if (amount > 100)
			{
				amount = 100;
			}
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap bufferA = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap bufferB = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, bufferA);
			double centerX = (width - 1) / 2.0;
			double centerY = (height - 1) / 2.0;
			if (method == 1)
			{
				double strength = (amount / 100.0) * 0.2;
				double[] scales = new double[RadialSampleCount];
				for (int index = 0; index < RadialSampleCount; index++)
				{
					scales[index] = 1.0 + ((((index + 0.5) / RadialSampleCount) - 0.5) * strength);
				}
				RadialZoomWorker worker = new RadialZoomWorker();
				worker.m_sourceBase = (byte*)bufferA.GetPixels().ToPointer();
				worker.m_destinationBase = (byte*)bufferB.GetPixels().ToPointer();
				worker.m_sourceStride = bufferA.RowBytes;
				worker.m_destinationStride = bufferB.RowBytes;
				worker.m_width = width;
				worker.m_height = height;
				worker.m_centerX = centerX;
				worker.m_centerY = centerY;
				worker.m_scales = scales;
				worker.m_count = RadialSampleCount;
				RowBands.Run(0, height, worker.Band);
			}
			else
			{
				double arcRadians = (amount / 100.0) * 25.0 * Math.PI / 180.0;
				double[] deltas = new double[RadialSampleCount];
				for (int index = 0; index < RadialSampleCount; index++)
				{
					deltas[index] = (((index + 0.5) / RadialSampleCount) - 0.5) * arcRadians;
				}
				RadialSpinWorker worker = new RadialSpinWorker();
				worker.m_sourceBase = (byte*)bufferA.GetPixels().ToPointer();
				worker.m_destinationBase = (byte*)bufferB.GetPixels().ToPointer();
				worker.m_sourceStride = bufferA.RowBytes;
				worker.m_destinationStride = bufferB.RowBytes;
				worker.m_width = width;
				worker.m_height = height;
				worker.m_centerX = centerX;
				worker.m_centerY = centerY;
				worker.m_deltas = deltas;
				worker.m_count = RadialSampleCount;
				RowBands.Run(0, height, worker.Band);
			}
			Unpremultiply(bufferB, bitmap);
			bufferA.Dispose();
			bufferB.Dispose();
		}
	}
}
