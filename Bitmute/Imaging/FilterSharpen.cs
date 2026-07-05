using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterSharpen
	{
		private const double SharpenStrength = 0.5;
		private const double SharpenMoreStrength = 1.5;
		private const double EdgeRangeThreshold = 30.0;

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

		private sealed unsafe class SharpenWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_height;
			public double m_strength;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						int sumRed;
						int sumGreen;
						int sumBlue;
						int sumAlpha;
						SumWindow(m_sourceBase, m_sourceStride, m_width, m_height, x, y, out sumRed, out sumGreen, out sumBlue, out sumAlpha);
						byte* center = sourceRow + (x * 4);
						byte red = SharpenChannel(center[0], sumRed, m_strength);
						byte green = SharpenChannel(center[1], sumGreen, m_strength);
						byte blue = SharpenChannel(center[2], sumBlue, m_strength);
						byte alpha = SharpenChannel(center[3], sumAlpha, m_strength);
						WriteUnpremultiplied(destinationRow + (x * 4), red, green, blue, alpha);
					}
				}
			}
		}

		private sealed unsafe class SharpenEdgesWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_height;
			public double m_strength;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						double range = LumaRange(m_sourceBase, m_sourceStride, m_width, m_height, x, y);
						if (range <= EdgeRangeThreshold)
						{
							continue;
						}
						int sumRed;
						int sumGreen;
						int sumBlue;
						int sumAlpha;
						SumWindow(m_sourceBase, m_sourceStride, m_width, m_height, x, y, out sumRed, out sumGreen, out sumBlue, out sumAlpha);
						byte* center = sourceRow + (x * 4);
						byte red = SharpenChannel(center[0], sumRed, m_strength);
						byte green = SharpenChannel(center[1], sumGreen, m_strength);
						byte blue = SharpenChannel(center[2], sumBlue, m_strength);
						byte alpha = SharpenChannel(center[3], sumAlpha, m_strength);
						WriteUnpremultiplied(destinationRow + (x * 4), red, green, blue, alpha);
					}
				}
			}
		}

		private static unsafe void SumWindow(byte* sourceBase, int sourceStride, int width, int height, int x, int y, out int sumRed, out int sumGreen, out int sumBlue, out int sumAlpha)
		{
			sumRed = 0;
			sumGreen = 0;
			sumBlue = 0;
			sumAlpha = 0;
			for (int offsetY = -1; offsetY <= 1; offsetY++)
			{
				int sampleY = y + offsetY;
				if (sampleY < 0)
				{
					sampleY = 0;
				}
				if (sampleY > height - 1)
				{
					sampleY = height - 1;
				}
				byte* sampleRow = sourceBase + ((long)sampleY * sourceStride);
				for (int offsetX = -1; offsetX <= 1; offsetX++)
				{
					int sampleX = x + offsetX;
					if (sampleX < 0)
					{
						sampleX = 0;
					}
					if (sampleX > width - 1)
					{
						sampleX = width - 1;
					}
					byte* sample = sampleRow + (sampleX * 4);
					sumRed += sample[0];
					sumGreen += sample[1];
					sumBlue += sample[2];
					sumAlpha += sample[3];
				}
			}
		}

		private static unsafe double LumaRange(byte* sourceBase, int sourceStride, int width, int height, int x, int y)
		{
			double minLuma = 256.0;
			double maxLuma = -1.0;
			for (int offsetY = -1; offsetY <= 1; offsetY++)
			{
				int sampleY = y + offsetY;
				if (sampleY < 0)
				{
					sampleY = 0;
				}
				if (sampleY > height - 1)
				{
					sampleY = height - 1;
				}
				byte* sampleRow = sourceBase + ((long)sampleY * sourceStride);
				for (int offsetX = -1; offsetX <= 1; offsetX++)
				{
					int sampleX = x + offsetX;
					if (sampleX < 0)
					{
						sampleX = 0;
					}
					if (sampleX > width - 1)
					{
						sampleX = width - 1;
					}
					byte* sample = sampleRow + (sampleX * 4);
					double luma = 0.299 * sample[0] + 0.587 * sample[1] + 0.114 * sample[2];
					if (luma < minLuma)
					{
						minLuma = luma;
					}
					if (luma > maxLuma)
					{
						maxLuma = luma;
					}
				}
			}
			return maxLuma - minLuma;
		}

		private static byte SharpenChannel(byte source, int windowSum, double strength)
		{
			double average = windowSum / 9.0;
			double value = source + strength * (source - average);
			return ClampByte(value);
		}

		private static unsafe void WriteUnpremultiplied(byte* pixel, byte red, byte green, byte blue, byte alpha)
		{
			int alphaValue = alpha;
			if (alphaValue == 0)
			{
				pixel[0] = 0;
				pixel[1] = 0;
				pixel[2] = 0;
				pixel[3] = 0;
				return;
			}
			int redValue = ((red * 255) + (alphaValue / 2)) / alphaValue;
			int greenValue = ((green * 255) + (alphaValue / 2)) / alphaValue;
			int blueValue = ((blue * 255) + (alphaValue / 2)) / alphaValue;
			if (redValue > 255)
			{
				redValue = 255;
			}
			if (greenValue > 255)
			{
				greenValue = 255;
			}
			if (blueValue > 255)
			{
				blueValue = 255;
			}
			pixel[0] = (byte)redValue;
			pixel[1] = (byte)greenValue;
			pixel[2] = (byte)blueValue;
			pixel[3] = (byte)alphaValue;
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

		private sealed unsafe class HighPassWorker
		{
			public byte* m_sourceBase;
			public byte* m_blurredBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_blurredStride;
			public int m_destinationStride;
			public int m_width;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceStride);
					byte* blurredRow = m_blurredBase + ((long)y * m_blurredStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						int pixelOffset = x * 4;
						destinationRow[pixelOffset + 0] = ClampByte(128.0 + (sourceRow[pixelOffset + 0] - blurredRow[pixelOffset + 0]));
						destinationRow[pixelOffset + 1] = ClampByte(128.0 + (sourceRow[pixelOffset + 1] - blurredRow[pixelOffset + 1]));
						destinationRow[pixelOffset + 2] = ClampByte(128.0 + (sourceRow[pixelOffset + 2] - blurredRow[pixelOffset + 2]));
						destinationRow[pixelOffset + 3] = sourceRow[pixelOffset + 3];
					}
				}
			}
		}

		private static unsafe void SharpenWithStrength(SKBitmap bitmap, double strength)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap scratch = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, scratch);
			byte* sourceBase = (byte*)scratch.GetPixels().ToPointer();
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			SharpenWorker worker = new SharpenWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = scratch.RowBytes;
			worker.m_destinationStride = bitmap.RowBytes;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_strength = strength;
			RowBands.Run(0, height, worker.Band);
			scratch.Dispose();
		}

		public static void Sharpen(SKBitmap bitmap)
		{
			SharpenWithStrength(bitmap, SharpenStrength);
		}

		public static void SharpenMore(SKBitmap bitmap)
		{
			SharpenWithStrength(bitmap, SharpenMoreStrength);
		}

		public static unsafe void HighPass(SKBitmap bitmap, int radius)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap source = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap blurred = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			byte* bitmapBase = (byte*)bitmap.GetPixels().ToPointer();
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* blurredBase = (byte*)blurred.GetPixels().ToPointer();
			long rowLength = (long)width * 4;
			for (int y = 0; y < height; y++)
			{
				byte* bitmapRow = bitmapBase + ((long)y * bitmap.RowBytes);
				Buffer.MemoryCopy(bitmapRow, sourceBase + ((long)y * source.RowBytes), rowLength, rowLength);
				Buffer.MemoryCopy(bitmapRow, blurredBase + ((long)y * blurred.RowBytes), rowLength, rowLength);
			}
			Adjustments.GaussianBlur(blurred, radius);
			HighPassWorker worker = new HighPassWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_blurredBase = blurredBase;
			worker.m_destinationBase = bitmapBase;
			worker.m_sourceStride = source.RowBytes;
			worker.m_blurredStride = blurred.RowBytes;
			worker.m_destinationStride = bitmap.RowBytes;
			worker.m_width = width;
			RowBands.Run(0, height, worker.Band);
			source.Dispose();
			blurred.Dispose();
		}

		public static unsafe void SharpenEdges(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap scratch = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, scratch);
			byte* sourceBase = (byte*)scratch.GetPixels().ToPointer();
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			SharpenEdgesWorker worker = new SharpenEdgesWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = scratch.RowBytes;
			worker.m_destinationStride = bitmap.RowBytes;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_strength = SharpenStrength;
			RowBands.Run(0, height, worker.Band);
			scratch.Dispose();
		}
	}
}
