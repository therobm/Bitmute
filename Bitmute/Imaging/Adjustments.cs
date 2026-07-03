using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class Adjustments
	{
		public static void InvertColors(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
					byte red = (byte)(255 - color.Red);
					byte green = (byte)(255 - color.Green);
					byte blue = (byte)(255 - color.Blue);
					SKColor inverted = new SKColor(red, green, blue, color.Alpha);
					bitmap.SetPixel(x, y, inverted);
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
			BoxBlurCopy(bitmap, bufferA, radius);
			BoxBlurCopy(bufferA, bufferB, radius);
			BoxBlurCopy(bufferB, bufferA, radius);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bitmap.SetPixel(x, y, bufferA.GetPixel(x, y));
				}
			}
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

		private static void BoxBlurCopy(SKBitmap source, SKBitmap destination, int radius)
		{
			int width = source.Width;
			int height = source.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					long sumRed = 0;
					long sumGreen = 0;
					long sumBlue = 0;
					int count = 0;
					for (int offsetY = -radius; offsetY <= radius; offsetY++)
					{
						for (int offsetX = -radius; offsetX <= radius; offsetX++)
						{
							int sampleX = x + offsetX;
							int sampleY = y + offsetY;
							if (sampleX < 0)
							{
								sampleX = 0;
							}
							if (sampleX > width - 1)
							{
								sampleX = width - 1;
							}
							if (sampleY < 0)
							{
								sampleY = 0;
							}
							if (sampleY > height - 1)
							{
								sampleY = height - 1;
							}
							SKColor sample = source.GetPixel(sampleX, sampleY);
							sumRed += sample.Red;
							sumGreen += sample.Green;
							sumBlue += sample.Blue;
							count++;
						}
					}
					SKColor original = source.GetPixel(x, y);
					byte avgRed = ClampByte((int)(sumRed / count));
					byte avgGreen = ClampByte((int)(sumGreen / count));
					byte avgBlue = ClampByte((int)(sumBlue / count));
					SKColor blurred = new SKColor(avgRed, avgGreen, avgBlue, original.Alpha);
					destination.SetPixel(x, y, blurred);
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
