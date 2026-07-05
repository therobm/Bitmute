using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class Adjustments
	{
		public static unsafe void InvertColors(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					pixel[0] = (byte)(255 - pixel[0]);
					pixel[1] = (byte)(255 - pixel[1]);
					pixel[2] = (byte)(255 - pixel[2]);
				}
			}
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
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					pixel[0] = table[pixel[0]];
					pixel[1] = table[pixel[1]];
					pixel[2] = table[pixel[2]];
				}
			}
		}

		public static unsafe void HueSaturationLightness(SKBitmap bitmap, int hue, int saturation, int lightness)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					SKColor color = new SKColor(pixel[0], pixel[1], pixel[2], pixel[3]);
					float h;
					float s;
					float l;
					color.ToHsl(out h, out s, out l);
					h = h + hue;
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
					s = s * (1.0f + (saturation / 100.0f));
					if (s < 0.0f)
					{
						s = 0.0f;
					}
					if (s > 100.0f)
					{
						s = 100.0f;
					}
					l = l + lightness;
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

		public static unsafe void Desaturate(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
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
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					pixel[0] = table[pixel[0]];
					pixel[1] = table[pixel[1]];
					pixel[2] = table[pixel[2]];
				}
			}
		}

		public static unsafe void Threshold(SKBitmap bitmap, int cutoff)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					double luminance = 0.299 * pixel[0] + 0.587 * pixel[1] + 0.114 * pixel[2];
					if (luminance >= cutoff)
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
			for (int blockY = 0; blockY < height; blockY += cellSize)
			{
				for (int blockX = 0; blockX < width; blockX += cellSize)
				{
					int endY = blockY + cellSize;
					if (endY > height)
					{
						endY = height;
					}
					int endX = blockX + cellSize;
					if (endX > width)
					{
						endX = width;
					}
					long sumRed = 0;
					long sumGreen = 0;
					long sumBlue = 0;
					long sumAlpha = 0;
					int count = 0;
					for (int y = blockY; y < endY; y++)
					{
						byte* row = basePointer + ((long)y * rowBytes);
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
						byte* row = basePointer + ((long)y * rowBytes);
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
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				byte* blurredRow = blurredBase + ((long)y * blurredStride);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					byte* blurPixel = blurredRow + (x * 4);
					double red = pixel[0] + strength * (pixel[0] - blurPixel[0]);
					double green = pixel[1] + strength * (pixel[1] - blurPixel[1]);
					double blue = pixel[2] + strength * (pixel[2] - blurPixel[2]);
					pixel[0] = ClampByte(red);
					pixel[1] = ClampByte(green);
					pixel[2] = ClampByte(blue);
				}
			}
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
			for (int y = 0; y < height; y++)
			{
				byte* sourceRow = sourceBase + ((long)y * sourceStride);
				byte* destinationRow = destinationBase + ((long)y * destinationStride);
				Buffer.MemoryCopy(sourceRow, destinationRow, rowLength, rowLength);
			}
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
			for (int y = 0; y < height; y++)
			{
				byte* sourceRow = sourceBase + (long)y * sourceStride;
				byte* destinationRow = destinationBase + (long)y * destinationStride;
				long sumRed = 0;
				long sumGreen = 0;
				long sumBlue = 0;
				long sumAlpha = 0;
				for (int offset = -radius; offset <= radius; offset++)
				{
					int sampleX = offset;
					if (sampleX < 0)
					{
						sampleX = 0;
					}
					if (sampleX > width - 1)
					{
						sampleX = width - 1;
					}
					int sampleOffset = sampleX * 4;
					sumRed += sourceRow[sampleOffset + 0];
					sumGreen += sourceRow[sampleOffset + 1];
					sumBlue += sourceRow[sampleOffset + 2];
					sumAlpha += sourceRow[sampleOffset + 3];
				}
				for (int x = 0; x < width; x++)
				{
					int pixelOffset = x * 4;
					destinationRow[pixelOffset + 0] = (byte)(sumRed / windowLength);
					destinationRow[pixelOffset + 1] = (byte)(sumGreen / windowLength);
					destinationRow[pixelOffset + 2] = (byte)(sumBlue / windowLength);
					destinationRow[pixelOffset + 3] = (byte)(sumAlpha / windowLength);
					int leavingX = x - radius;
					if (leavingX < 0)
					{
						leavingX = 0;
					}
					if (leavingX > width - 1)
					{
						leavingX = width - 1;
					}
					int enteringX = x + radius + 1;
					if (enteringX < 0)
					{
						enteringX = 0;
					}
					if (enteringX > width - 1)
					{
						enteringX = width - 1;
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
			for (int x = 0; x < width; x++)
			{
				int pixelOffset = x * 4;
				long sumRed = 0;
				long sumGreen = 0;
				long sumBlue = 0;
				long sumAlpha = 0;
				for (int offset = -radius; offset <= radius; offset++)
				{
					int sampleY = offset;
					if (sampleY < 0)
					{
						sampleY = 0;
					}
					if (sampleY > height - 1)
					{
						sampleY = height - 1;
					}
					byte* sampleRow = sourceBase + (long)sampleY * sourceStride;
					sumRed += sampleRow[pixelOffset + 0];
					sumGreen += sampleRow[pixelOffset + 1];
					sumBlue += sampleRow[pixelOffset + 2];
					sumAlpha += sampleRow[pixelOffset + 3];
				}
				for (int y = 0; y < height; y++)
				{
					byte* destinationRow = destinationBase + (long)y * destinationStride;
					destinationRow[pixelOffset + 0] = (byte)(sumRed / windowLength);
					destinationRow[pixelOffset + 1] = (byte)(sumGreen / windowLength);
					destinationRow[pixelOffset + 2] = (byte)(sumBlue / windowLength);
					destinationRow[pixelOffset + 3] = (byte)(sumAlpha / windowLength);
					int leavingY = y - radius;
					if (leavingY < 0)
					{
						leavingY = 0;
					}
					if (leavingY > height - 1)
					{
						leavingY = height - 1;
					}
					int enteringY = y + radius + 1;
					if (enteringY < 0)
					{
						enteringY = 0;
					}
					if (enteringY > height - 1)
					{
						enteringY = height - 1;
					}
					byte* leavingRow = sourceBase + (long)leavingY * sourceStride;
					byte* enteringRow = sourceBase + (long)enteringY * sourceStride;
					sumRed += enteringRow[pixelOffset + 0] - leavingRow[pixelOffset + 0];
					sumGreen += enteringRow[pixelOffset + 1] - leavingRow[pixelOffset + 1];
					sumBlue += enteringRow[pixelOffset + 2] - leavingRow[pixelOffset + 2];
					sumAlpha += enteringRow[pixelOffset + 3] - leavingRow[pixelOffset + 3];
				}
			}
		}

		private static unsafe void Premultiply(SKBitmap source, SKBitmap destination)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* sourceRow = sourceBase + ((long)y * sourceStride);
				byte* destinationRow = destinationBase + ((long)y * destinationStride);
				for (int x = 0; x < width; x++)
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

		private static unsafe void Unpremultiply(SKBitmap source, SKBitmap destination)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* sourceRow = sourceBase + ((long)y * sourceStride);
				byte* destinationRow = destinationBase + ((long)y * destinationStride);
				for (int x = 0; x < width; x++)
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
