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

		public static void BrightnessContrast(SKBitmap bitmap, int brightness, int contrast)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			double brightnessOffset = brightness * 2.55;
			double contrastMapped = contrast * 2.55;
			double factor = (259.0 * (contrastMapped + 255.0)) / (255.0 * (259.0 - contrastMapped));
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
					double red = color.Red + brightnessOffset;
					double green = color.Green + brightnessOffset;
					double blue = color.Blue + brightnessOffset;
					red = factor * (red - 128.0) + 128.0;
					green = factor * (green - 128.0) + 128.0;
					blue = factor * (blue - 128.0) + 128.0;
					SKColor adjusted = new SKColor(ClampByte(red), ClampByte(green), ClampByte(blue), color.Alpha);
					bitmap.SetPixel(x, y, adjusted);
				}
			}
		}

		public static void HueSaturationLightness(SKBitmap bitmap, int hue, int saturation, int lightness)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
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
					SKColor adjusted = SKColor.FromHsl(h, s, l, color.Alpha);
					bitmap.SetPixel(x, y, adjusted);
				}
			}
		}

		public static void Desaturate(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
					double luminance = 0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue;
					byte gray = ClampByte(luminance);
					SKColor adjusted = new SKColor(gray, gray, gray, color.Alpha);
					bitmap.SetPixel(x, y, adjusted);
				}
			}
		}

		public static void Posterize(SKBitmap bitmap, int levels)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
					byte red = PosterizeChannel(color.Red, levels);
					byte green = PosterizeChannel(color.Green, levels);
					byte blue = PosterizeChannel(color.Blue, levels);
					SKColor adjusted = new SKColor(red, green, blue, color.Alpha);
					bitmap.SetPixel(x, y, adjusted);
				}
			}
		}

		public static void Threshold(SKBitmap bitmap, int cutoff)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
					double luminance = 0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue;
					if (luminance >= cutoff)
					{
						SKColor white = new SKColor(255, 255, 255, color.Alpha);
						bitmap.SetPixel(x, y, white);
					}
					else
					{
						SKColor black = new SKColor(0, 0, 0, color.Alpha);
						bitmap.SetPixel(x, y, black);
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
			BoxBlurCopy(bufferA, bufferB, radius);
			BoxBlurCopy(bufferB, bufferA, radius);
			BoxBlurCopy(bufferA, bufferB, radius);
			Unpremultiply(bufferB, bitmap);
			bufferA.Dispose();
			bufferB.Dispose();
		}

		public static void AddNoise(SKBitmap bitmap, int amount, bool monochrome)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			Random random = new Random(12345);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
					byte red;
					byte green;
					byte blue;
					if (monochrome)
					{
						double n = (random.NextDouble() * 2.0 - 1.0) * amount * 1.28;
						red = ClampByte(color.Red + n);
						green = ClampByte(color.Green + n);
						blue = ClampByte(color.Blue + n);
					}
					else
					{
						double nRed = (random.NextDouble() * 2.0 - 1.0) * amount * 1.28;
						double nGreen = (random.NextDouble() * 2.0 - 1.0) * amount * 1.28;
						double nBlue = (random.NextDouble() * 2.0 - 1.0) * amount * 1.28;
						red = ClampByte(color.Red + nRed);
						green = ClampByte(color.Green + nGreen);
						blue = ClampByte(color.Blue + nBlue);
					}
					SKColor adjusted = new SKColor(red, green, blue, color.Alpha);
					bitmap.SetPixel(x, y, adjusted);
				}
			}
		}

		public static void Pixelate(SKBitmap bitmap, int cellSize)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
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
						for (int x = blockX; x < endX; x++)
						{
							SKColor color = bitmap.GetPixel(x, y);
							sumRed += color.Red;
							sumGreen += color.Green;
							sumBlue += color.Blue;
							sumAlpha += color.Alpha;
							count++;
						}
					}
					byte avgRed = ClampByte((int)(sumRed / count));
					byte avgGreen = ClampByte((int)(sumGreen / count));
					byte avgBlue = ClampByte((int)(sumBlue / count));
					byte avgAlpha = ClampByte((int)(sumAlpha / count));
					SKColor average = new SKColor(avgRed, avgGreen, avgBlue, avgAlpha);
					for (int y = blockY; y < endY; y++)
					{
						for (int x = blockX; x < endX; x++)
						{
							bitmap.SetPixel(x, y, average);
						}
					}
				}
			}
		}

		public static void UnsharpMask(SKBitmap bitmap, int amount, int radius)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap blurred = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			BoxBlurCopy(bitmap, blurred, radius);
			double strength = amount / 100.0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
					SKColor blur = blurred.GetPixel(x, y);
					double red = color.Red + strength * (color.Red - blur.Red);
					double green = color.Green + strength * (color.Green - blur.Green);
					double blue = color.Blue + strength * (color.Blue - blur.Blue);
					SKColor adjusted = new SKColor(ClampByte(red), ClampByte(green), ClampByte(blue), color.Alpha);
					bitmap.SetPixel(x, y, adjusted);
				}
			}
			blurred.Dispose();
		}

		private static unsafe void BoxBlurCopy(SKBitmap source, SKBitmap destination, int radius)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			if (radius <= 0)
			{
				for (int y = 0; y < height; y++)
				{
					byte* sourceRow = sourceBase + (long)y * sourceStride;
					byte* destinationRow = destinationBase + (long)y * destinationStride;
					for (int x = 0; x < width; x++)
					{
						int pixelOffset = x * 4;
						destinationRow[pixelOffset + 0] = sourceRow[pixelOffset + 0];
						destinationRow[pixelOffset + 1] = sourceRow[pixelOffset + 1];
						destinationRow[pixelOffset + 2] = sourceRow[pixelOffset + 2];
						destinationRow[pixelOffset + 3] = sourceRow[pixelOffset + 3];
					}
				}
				return;
			}
			int windowLength = 2 * radius + 1;
			SKBitmap temporary = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			int temporaryStride = temporary.RowBytes;
			byte* temporaryBase = (byte*)temporary.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* sourceRow = sourceBase + (long)y * sourceStride;
				byte* temporaryRow = temporaryBase + (long)y * temporaryStride;
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
					temporaryRow[pixelOffset + 0] = (byte)(sumRed / windowLength);
					temporaryRow[pixelOffset + 1] = (byte)(sumGreen / windowLength);
					temporaryRow[pixelOffset + 2] = (byte)(sumBlue / windowLength);
					temporaryRow[pixelOffset + 3] = (byte)(sumAlpha / windowLength);
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
					byte* sampleRow = temporaryBase + (long)sampleY * temporaryStride;
					sumRed += sampleRow[pixelOffset + 0];
					sumGreen += sampleRow[pixelOffset + 1];
					sumBlue += sampleRow[pixelOffset + 2];
					sumAlpha += sampleRow[pixelOffset + 3];
				}
				for (int y = 0; y < height; y++)
				{
					byte* temporaryRow = temporaryBase + (long)y * temporaryStride;
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
					byte* leavingRow = temporaryBase + (long)leavingY * temporaryStride;
					byte* enteringRow = temporaryBase + (long)enteringY * temporaryStride;
					sumRed += enteringRow[pixelOffset + 0] - leavingRow[pixelOffset + 0];
					sumGreen += enteringRow[pixelOffset + 1] - leavingRow[pixelOffset + 1];
					sumBlue += enteringRow[pixelOffset + 2] - leavingRow[pixelOffset + 2];
					sumAlpha += enteringRow[pixelOffset + 3] - leavingRow[pixelOffset + 3];
				}
			}
			temporary.Dispose();
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
