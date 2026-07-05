using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterRenderTests
	{
		private static int s_failures;

		private static void Check(bool condition, string name)
		{
			if (condition)
			{
				Console.WriteLine("PASS " + name);
			}
			else
			{
				s_failures = s_failures + 1;
				Console.WriteLine("FAIL " + name);
			}
		}

		private static SKBitmap CreateBitmap(int width, int height)
		{
			return new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
		}

		private static unsafe void FillSolid(SKBitmap bitmap, byte red, byte green, byte blue, byte alpha)
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
					pixel[0] = red;
					pixel[1] = green;
					pixel[2] = blue;
					pixel[3] = alpha;
				}
			}
		}

		private static unsafe void FillPattern(SKBitmap bitmap)
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
					pixel[0] = (byte)(((x * 7) + (y * 13)) & 255);
					pixel[1] = (byte)(((x * 31) + (y * 5)) & 255);
					pixel[2] = (byte)(((x * 3) + (y * 47)) & 255);
					pixel[3] = (byte)(((x * 11) + (y * 17)) & 255);
				}
			}
		}

		private static unsafe bool BitmapsEqual(SKBitmap left, SKBitmap right)
		{
			if (left.Width != right.Width || left.Height != right.Height)
			{
				return false;
			}
			int width = left.Width;
			int height = left.Height;
			int leftStride = left.RowBytes;
			int rightStride = right.RowBytes;
			byte* leftBase = (byte*)left.GetPixels().ToPointer();
			byte* rightBase = (byte*)right.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* leftRow = leftBase + ((long)y * leftStride);
				byte* rightRow = rightBase + ((long)y * rightStride);
				for (int index = 0; index < width * 4; index++)
				{
					if (leftRow[index] != rightRow[index])
					{
						return false;
					}
				}
			}
			return true;
		}

		private static byte MinByte(byte first, byte second)
		{
			if (first < second)
			{
				return first;
			}
			return second;
		}

		private static byte MaxByte(byte first, byte second)
		{
			if (first > second)
			{
				return first;
			}
			return second;
		}

		private static void TestCloudsDeterminism()
		{
			SKColor foreground = new SKColor(210, 40, 130, 255);
			SKColor background = new SKColor(30, 160, 70, 255);
			SKBitmap first = CreateBitmap(64, 64);
			SKBitmap second = CreateBitmap(64, 64);
			SKBitmap third = CreateBitmap(64, 64);
			FillSolid(first, 1, 2, 3, 4);
			FillPattern(second);
			FillPattern(third);
			FilterRender.Clouds(first, foreground, background, 12345);
			FilterRender.Clouds(second, foreground, background, 12345);
			FilterRender.Clouds(third, foreground, background, 54321);
			Check(BitmapsEqual(first, second), "clouds same seed byte-identical regardless of prior content");
			Check(!BitmapsEqual(first, third), "clouds different seed differs somewhere");
			SKBitmap diffFirst = CreateBitmap(64, 64);
			SKBitmap diffSecond = CreateBitmap(64, 64);
			FillPattern(diffFirst);
			FillPattern(diffSecond);
			FilterRender.DifferenceClouds(diffFirst, foreground, background, 777);
			FilterRender.DifferenceClouds(diffSecond, foreground, background, 777);
			Check(BitmapsEqual(diffFirst, diffSecond), "difference clouds same seed byte-identical");
			first.Dispose();
			second.Dispose();
			third.Dispose();
			diffFirst.Dispose();
			diffSecond.Dispose();
		}

		private static unsafe void TestCloudsRange()
		{
			SKColor foreground = new SKColor(200, 40, 120, 255);
			SKColor background = new SKColor(60, 180, 90, 10);
			SKBitmap bitmap = CreateBitmap(128, 96);
			FillPattern(bitmap);
			FilterRender.Clouds(bitmap, foreground, background, 99);
			byte minRed = MinByte(foreground.Red, background.Red);
			byte maxRed = MaxByte(foreground.Red, background.Red);
			byte minGreen = MinByte(foreground.Green, background.Green);
			byte maxGreen = MaxByte(foreground.Green, background.Green);
			byte minBlue = MinByte(foreground.Blue, background.Blue);
			byte maxBlue = MaxByte(foreground.Blue, background.Blue);
			bool inRange = true;
			bool alphaOpaque = true;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = 0; y < bitmap.Height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < bitmap.Width; x++)
				{
					byte* pixel = row + (x * 4);
					if (pixel[0] < minRed || pixel[0] > maxRed)
					{
						inRange = false;
					}
					if (pixel[1] < minGreen || pixel[1] > maxGreen)
					{
						inRange = false;
					}
					if (pixel[2] < minBlue || pixel[2] > maxBlue)
					{
						inRange = false;
					}
					if (pixel[3] != 255)
					{
						alphaOpaque = false;
					}
				}
			}
			Check(inRange, "clouds every channel within [min(fg,bg), max(fg,bg)]");
			Check(alphaOpaque, "clouds alpha is 255 everywhere");
			bitmap.Dispose();
		}

		private static unsafe void TestDifferenceCloudsOverBlack()
		{
			SKColor foreground = new SKColor(240, 130, 20, 255);
			SKColor background = new SKColor(10, 60, 200, 255);
			SKBitmap difference = CreateBitmap(96, 80);
			SKBitmap clouds = CreateBitmap(96, 80);
			FillSolid(difference, 0, 0, 0, 255);
			FillPattern(clouds);
			FilterRender.DifferenceClouds(difference, foreground, background, 4242);
			FilterRender.Clouds(clouds, foreground, background, 4242);
			bool rgbMatch = true;
			bool alphaKept = true;
			int differenceStride = difference.RowBytes;
			int cloudsStride = clouds.RowBytes;
			byte* differenceBase = (byte*)difference.GetPixels().ToPointer();
			byte* cloudsBase = (byte*)clouds.GetPixels().ToPointer();
			for (int y = 0; y < difference.Height; y++)
			{
				byte* differenceRow = differenceBase + ((long)y * differenceStride);
				byte* cloudsRow = cloudsBase + ((long)y * cloudsStride);
				for (int x = 0; x < difference.Width; x++)
				{
					byte* differencePixel = differenceRow + (x * 4);
					byte* cloudsPixel = cloudsRow + (x * 4);
					if (differencePixel[0] != cloudsPixel[0] || differencePixel[1] != cloudsPixel[1] || differencePixel[2] != cloudsPixel[2])
					{
						rgbMatch = false;
					}
					if (differencePixel[3] != 255)
					{
						alphaKept = false;
					}
				}
			}
			Check(rgbMatch, "difference clouds over opaque black equals plain clouds rgb");
			Check(alphaKept, "difference clouds over opaque black keeps alpha 255");
			SKBitmap translucent = CreateBitmap(96, 80);
			FillSolid(translucent, 0, 0, 0, 137);
			FilterRender.DifferenceClouds(translucent, foreground, background, 4242);
			bool translucentAlphaKept = true;
			int translucentStride = translucent.RowBytes;
			byte* translucentBase = (byte*)translucent.GetPixels().ToPointer();
			for (int y = 0; y < translucent.Height; y++)
			{
				byte* row = translucentBase + ((long)y * translucentStride);
				for (int x = 0; x < translucent.Width; x++)
				{
					byte* pixel = row + (x * 4);
					if (pixel[3] != 137)
					{
						translucentAlphaKept = false;
					}
				}
			}
			Check(translucentAlphaKept, "difference clouds keeps existing non-opaque alpha");
			difference.Dispose();
			clouds.Dispose();
			translucent.Dispose();
		}

		private static unsafe void TestCloudsSmoothness()
		{
			SKColor foreground = new SKColor(255, 255, 255, 255);
			SKColor background = new SKColor(0, 0, 0, 255);
			SKBitmap bitmap = CreateBitmap(256, 256);
			FillSolid(bitmap, 0, 0, 0, 255);
			FilterRender.Clouds(bitmap, foreground, background, 2026);
			int maxStep = 0;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = 0; y < bitmap.Height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < bitmap.Width - 1; x++)
				{
					byte* pixel = row + (x * 4);
					byte* next = row + ((x + 1) * 4);
					for (int channel = 0; channel < 3; channel++)
					{
						int step = next[channel] - pixel[channel];
						if (step < 0)
						{
							step = -step;
						}
						if (step > maxStep)
						{
							maxStep = step;
						}
					}
				}
			}
			Check(maxStep < 48, "clouds max horizontal adjacent step below 48 (max " + maxStep + ")");
			bitmap.Dispose();
		}

		private static void TestBandedMatchesSingleBand()
		{
			SKColor foreground = new SKColor(220, 90, 30, 255);
			SKColor background = new SKColor(40, 20, 190, 255);
			int savedMax = RowBands.MaxBands();
			SKBitmap cloudsParallel = CreateBitmap(256, 300);
			SKBitmap differenceParallel = CreateBitmap(256, 300);
			FillSolid(cloudsParallel, 0, 0, 0, 255);
			FillPattern(differenceParallel);
			RowBands.SetMaxBands(savedMax);
			FilterRender.Clouds(cloudsParallel, foreground, background, 606);
			FilterRender.DifferenceClouds(differenceParallel, foreground, background, 707);
			RowBands.SetMaxBands(1);
			SKBitmap cloudsSingle = CreateBitmap(256, 300);
			SKBitmap differenceSingle = CreateBitmap(256, 300);
			FillSolid(cloudsSingle, 0, 0, 0, 255);
			FillPattern(differenceSingle);
			FilterRender.Clouds(cloudsSingle, foreground, background, 606);
			FilterRender.DifferenceClouds(differenceSingle, foreground, background, 707);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapsEqual(cloudsParallel, cloudsSingle), "parallel clouds matches single band");
			Check(BitmapsEqual(differenceParallel, differenceSingle), "parallel difference clouds matches single band");
			Check(RowBands.MaxBands() == savedMax, "max bands restored after clouds tests");
			cloudsParallel.Dispose();
			differenceParallel.Dispose();
			cloudsSingle.Dispose();
			differenceSingle.Dispose();
		}

		public static int RunAll()
		{
			TestCloudsDeterminism();
			TestCloudsRange();
			TestDifferenceCloudsOverBlack();
			TestCloudsSmoothness();
			TestBandedMatchesSingleBand();
			return s_failures;
		}
	}
}
