using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class Adjustments
	{
		private sealed unsafe class LutWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public byte[] m_table;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						pixel[0] = m_table[pixel[0]];
						pixel[1] = m_table[pixel[1]];
						pixel[2] = m_table[pixel[2]];
					}
				}
			}
		}

		private sealed unsafe class HueSaturationLightnessWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_hue;
			public int m_saturation;
			public int m_lightness;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						SKColor color = new SKColor(pixel[0], pixel[1], pixel[2], pixel[3]);
						float h;
						float s;
						float l;
						color.ToHsl(out h, out s, out l);
						h = h + m_hue;
						for (;;)
						{
							if (h >= 0.0f)
							{
								break;
							}
							h = h + 360.0f;
						}
						for (;;)
						{
							if (h < 360.0f)
							{
								break;
							}
							h = h - 360.0f;
						}
						s = s * (1.0f + (m_saturation / 100.0f));
						if (s < 0.0f)
						{
							s = 0.0f;
						}
						if (s > 100.0f)
						{
							s = 100.0f;
						}
						l = l + m_lightness;
						if (l < 0.0f)
						{
							l = 0.0f;
						}
						if (l > 100.0f)
						{
							l = 100.0f;
						}
						SKColor adjusted = SKColor.FromHsl(h, s, l, pixel[3]);
						pixel[0] = adjusted.Red;
						pixel[1] = adjusted.Green;
						pixel[2] = adjusted.Blue;
					}
				}
			}
		}

		private sealed unsafe class DesaturateWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						double luminance = 0.299 * pixel[0] + 0.587 * pixel[1] + 0.114 * pixel[2];
						byte gray = ClampByte(luminance);
						pixel[0] = gray;
						pixel[1] = gray;
						pixel[2] = gray;
					}
				}
			}
		}

		private sealed unsafe class ThresholdWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_cutoff;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						double luminance = 0.299 * pixel[0] + 0.587 * pixel[1] + 0.114 * pixel[2];
						if (luminance >= m_cutoff)
						{
							pixel[0] = 255;
							pixel[1] = 255;
							pixel[2] = 255;
						}
						else
						{
							pixel[0] = 0;
							pixel[1] = 0;
							pixel[2] = 0;
						}
					}
				}
			}
		}

		private sealed unsafe class PixelateWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_height;
			public int m_cellSize;

			public void Band(int start, int end)
			{
				for (int index = start; index < end; index++)
				{
					int blockY = index * m_cellSize;
					for (int blockX = 0; blockX < m_width; blockX += m_cellSize)
					{
						int endY = blockY + m_cellSize;
						if (endY > m_height)
						{
							endY = m_height;
						}
						int endX = blockX + m_cellSize;
						if (endX > m_width)
						{
							endX = m_width;
						}
						long sumRed = 0;
						long sumGreen = 0;
						long sumBlue = 0;
						long sumAlpha = 0;
						int count = 0;
						for (int y = blockY; y < endY; y++)
						{
							byte* row = m_base + ((long)y * m_rowBytes);
							for (int x = blockX; x < endX; x++)
							{
								byte* pixel = row + (x * 4);
								sumRed += pixel[0];
								sumGreen += pixel[1];
								sumBlue += pixel[2];
								sumAlpha += pixel[3];
								count++;
							}
						}
						byte avgRed = ClampByte((int)(sumRed / count));
						byte avgGreen = ClampByte((int)(sumGreen / count));
						byte avgBlue = ClampByte((int)(sumBlue / count));
						byte avgAlpha = ClampByte((int)(sumAlpha / count));
						for (int y = blockY; y < endY; y++)
						{
							byte* row = m_base + ((long)y * m_rowBytes);
							for (int x = blockX; x < endX; x++)
							{
								byte* pixel = row + (x * 4);
								pixel[0] = avgRed;
								pixel[1] = avgGreen;
								pixel[2] = avgBlue;
								pixel[3] = avgAlpha;
							}
						}
					}
				}
			}
		}

		private sealed unsafe class UnsharpMaskWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public byte* m_blurredBase;
			public int m_blurredStride;
			public int m_width;
			public double m_strength;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					byte* blurredRow = m_blurredBase + ((long)y * m_blurredStride);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						byte* blurPixel = blurredRow + (x * 4);
						double red = pixel[0] + m_strength * (pixel[0] - blurPixel[0]);
						double green = pixel[1] + m_strength * (pixel[1] - blurPixel[1]);
						double blue = pixel[2] + m_strength * (pixel[2] - blurPixel[2]);
						pixel[0] = ClampByte(red);
						pixel[1] = ClampByte(green);
						pixel[2] = ClampByte(blue);
					}
				}
			}
		}

		private sealed unsafe class CopyRowsWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public long m_rowLength;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					Buffer.MemoryCopy(sourceRow, destinationRow, m_rowLength, m_rowLength);
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
					byte* sourceRow = m_sourceBase + (long)y * m_sourceStride;
					byte* destinationRow = m_destinationBase + (long)y * m_destinationStride;
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
						byte* sampleRow = m_sourceBase + (long)sampleY * m_sourceStride;
						sumRed += sampleRow[pixelOffset + 0];
						sumGreen += sampleRow[pixelOffset + 1];
						sumBlue += sampleRow[pixelOffset + 2];
						sumAlpha += sampleRow[pixelOffset + 3];
					}
					for (int y = 0; y < m_height; y++)
					{
						byte* destinationRow = m_destinationBase + (long)y * m_destinationStride;
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
						byte* leavingRow = m_sourceBase + (long)leavingY * m_sourceStride;
						byte* enteringRow = m_sourceBase + (long)enteringY * m_sourceStride;
						sumRed += enteringRow[pixelOffset + 0] - leavingRow[pixelOffset + 0];
						sumGreen += enteringRow[pixelOffset + 1] - leavingRow[pixelOffset + 1];
						sumBlue += enteringRow[pixelOffset + 2] - leavingRow[pixelOffset + 2];
						sumAlpha += enteringRow[pixelOffset + 3] - leavingRow[pixelOffset + 3];
					}
				}
			}
		}

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

		public static unsafe void InvertColors(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte[] table = new byte[256];
			for (int value = 0; value < 256; value++)
			{
				table[value] = (byte)(255 - value);
			}
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			LutWorker worker = new LutWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_table = table;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void BrightnessContrast(SKBitmap bitmap, int brightness, int contrast)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			double brightnessOffset = brightness * 2.55;
			double contrastMapped = contrast * 2.55;
			double factor = (259.0 * (contrastMapped + 255.0)) / (255.0 * (259.0 - contrastMapped));
			byte[] table = new byte[256];
			for (int value = 0; value < 256; value++)
			{
				double mapped = factor * ((value + brightnessOffset) - 128.0) + 128.0;
				table[value] = ClampByte(mapped);
			}
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			LutWorker worker = new LutWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_table = table;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void HueSaturationLightness(SKBitmap bitmap, int hue, int saturation, int lightness)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			HueSaturationLightnessWorker worker = new HueSaturationLightnessWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_hue = hue;
			worker.m_saturation = saturation;
			worker.m_lightness = lightness;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void Desaturate(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			DesaturateWorker worker = new DesaturateWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void Posterize(SKBitmap bitmap, int levels)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte[] table = new byte[256];
			for (int value = 0; value < 256; value++)
			{
				table[value] = PosterizeChannel((byte)value, levels);
			}
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			LutWorker worker = new LutWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_table = table;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void Threshold(SKBitmap bitmap, int cutoff)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			ThresholdWorker worker = new ThresholdWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_cutoff = cutoff;
			RowBands.Run(0, height, worker.Band);
		}

		public static void GaussianBlur(SKBitmap bitmap, int radius)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap bufferA = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap bufferB = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, bufferA);
			BoxBlurHorizontal(bufferA, bufferB, radius);
			BoxBlurVertical(bufferB, bufferA, radius);
			BoxBlurHorizontal(bufferA, bufferB, radius);
			BoxBlurVertical(bufferB, bufferA, radius);
			BoxBlurHorizontal(bufferA, bufferB, radius);
			BoxBlurVertical(bufferB, bufferA, radius);
			Unpremultiply(bufferA, bitmap);
			bufferA.Dispose();
			bufferB.Dispose();
		}

		public static unsafe void AddNoise(SKBitmap bitmap, int amount, bool monochrome)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			Random random = new Random(12345);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					if (monochrome)
					{
						double n = (random.NextDouble() * 2.0 - 1.0) * amount * 1.28;
						pixel[0] = ClampByte(pixel[0] + n);
						pixel[1] = ClampByte(pixel[1] + n);
						pixel[2] = ClampByte(pixel[2] + n);
					}
					else
					{
						double nRed = (random.NextDouble() * 2.0 - 1.0) * amount * 1.28;
						double nGreen = (random.NextDouble() * 2.0 - 1.0) * amount * 1.28;
						double nBlue = (random.NextDouble() * 2.0 - 1.0) * amount * 1.28;
						pixel[0] = ClampByte(pixel[0] + nRed);
						pixel[1] = ClampByte(pixel[1] + nGreen);
						pixel[2] = ClampByte(pixel[2] + nBlue);
					}
				}
			}
		}

		public static unsafe void Pixelate(SKBitmap bitmap, int cellSize)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int blockRowCount = (height + cellSize - 1) / cellSize;
			PixelateWorker worker = new PixelateWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_cellSize = cellSize;
			RowBands.Run(0, blockRowCount, worker.Band);
		}

		public static unsafe void UnsharpMask(SKBitmap bitmap, int amount, int radius)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			SKBitmap scratch = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap blurred = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			BoxBlurHorizontal(bitmap, scratch, radius);
			BoxBlurVertical(scratch, blurred, radius);
			int blurredStride = blurred.RowBytes;
			double strength = amount / 100.0;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			byte* blurredBase = (byte*)blurred.GetPixels().ToPointer();
			UnsharpMaskWorker worker = new UnsharpMaskWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_blurredBase = blurredBase;
			worker.m_blurredStride = blurredStride;
			worker.m_width = width;
			worker.m_strength = strength;
			RowBands.Run(0, height, worker.Band);
			scratch.Dispose();
			blurred.Dispose();
		}

		private static unsafe void CopyRows(SKBitmap source, SKBitmap destination)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			long rowLength = (long)width * 4;
			CopyRowsWorker worker = new CopyRowsWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = sourceStride;
			worker.m_destinationStride = destinationStride;
			worker.m_rowLength = rowLength;
			RowBands.Run(0, height, worker.Band);
		}

		private static unsafe void BoxBlurHorizontal(SKBitmap source, SKBitmap destination, int radius)
		{
			if (radius <= 0)
			{
				CopyRows(source, destination);
				return;
			}
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			int windowLength = 2 * radius + 1;
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
			if (radius <= 0)
			{
				CopyRows(source, destination);
				return;
			}
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			int windowLength = 2 * radius + 1;
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

		private static byte PosterizeChannel(byte channel, int levels)
		{
			double normalized = channel / 255.0;
			double stepped = Math.Round(normalized * (levels - 1));
			double result = Math.Round(stepped / (levels - 1) * 255.0);
			return ClampByte(result);
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

		private static byte ClampByte(int value)
		{
			if (value < 0)
			{
				return 0;
			}
			if (value > 255)
			{
				return 255;
			}
			return (byte)value;
		}
	}
}
