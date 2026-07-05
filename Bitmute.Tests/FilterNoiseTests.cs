using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterNoiseTests
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

		private static void CheckNear(int actual, int expected, int tolerance, string name)
		{
			int delta = actual - expected;
			if (delta < 0)
			{
				delta = -delta;
			}
			Check(delta <= tolerance, name + " (actual " + actual + " expected " + expected + ")");
		}

		private static unsafe void SetRawPixel(SKBitmap bitmap, int x, int y, SKColor color)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			byte* pixel = basePointer + ((long)y * bitmap.RowBytes) + (x * 4);
			pixel[0] = color.Red;
			pixel[1] = color.Green;
			pixel[2] = color.Blue;
			pixel[3] = color.Alpha;
		}

		private static unsafe int GetRawChannel(SKBitmap bitmap, int x, int y, int channel)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			byte* pixel = basePointer + ((long)y * bitmap.RowBytes) + (x * 4);
			return pixel[channel];
		}

		private static unsafe void FillRaw(SKBitmap bitmap, int left, int top, int right, int bottom, SKColor color)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			for (int y = top; y < bottom; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = left; x < right; x++)
				{
					byte* pixel = row + (x * 4);
					pixel[0] = color.Red;
					pixel[1] = color.Green;
					pixel[2] = color.Blue;
					pixel[3] = color.Alpha;
				}
			}
		}

		private static unsafe SKBitmap BuildRandomBitmap(int seed, int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Random random = new Random(seed);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					pixel[0] = (byte)random.Next(256);
					pixel[1] = (byte)random.Next(256);
					pixel[2] = (byte)random.Next(256);
					pixel[3] = (byte)random.Next(256);
				}
			}
			return bitmap;
		}

		private static unsafe bool BitmapBytesEqual(SKBitmap first, SKBitmap second)
		{
			if (first.Width != second.Width || first.Height != second.Height)
			{
				return false;
			}
			byte* firstBase = (byte*)first.GetPixels().ToPointer();
			byte* secondBase = (byte*)second.GetPixels().ToPointer();
			int firstStride = first.RowBytes;
			int secondStride = second.RowBytes;
			int width = first.Width;
			for (int y = 0; y < first.Height; y++)
			{
				byte* firstRow = firstBase + ((long)y * firstStride);
				byte* secondRow = secondBase + ((long)y * secondStride);
				for (int index = 0; index < width * 4; index++)
				{
					if (firstRow[index] != secondRow[index])
					{
						Console.WriteLine("  byte mismatch at row " + y + " offset " + index + " got " + firstRow[index] + " expected " + secondRow[index]);
						return false;
					}
				}
			}
			return true;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestDespeckleRemovesSpeck();
			TestDespeckleKeepsHardEdge();
			TestMedianOutlier();
			TestMedianUniform();
			TestMedianAlphaBoundary();
			TestParallelMatchesSingleBand();
			return s_failures;
		}

		private static void TestDespeckleRemovesSpeck()
		{
			SKColor field = new SKColor(180, 190, 200, 255);
			SKColor speck = new SKColor(10, 10, 10, 255);
			SKBitmap bitmap = new SKBitmap(16, 16, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FillRaw(bitmap, 0, 0, 16, 16, field);
			SetRawPixel(bitmap, 8, 8, speck);
			FilterNoise.Despeckle(bitmap);
			CheckNear(GetRawChannel(bitmap, 8, 8, 0), 180, 2, "despeckle speck red becomes field");
			CheckNear(GetRawChannel(bitmap, 8, 8, 1), 190, 2, "despeckle speck green becomes field");
			CheckNear(GetRawChannel(bitmap, 8, 8, 2), 200, 2, "despeckle speck blue becomes field");
			Check(GetRawChannel(bitmap, 8, 8, 3) == 255, "despeckle speck alpha stays opaque");
			Check(GetRawChannel(bitmap, 2, 2, 0) == 180 && GetRawChannel(bitmap, 2, 2, 1) == 190 && GetRawChannel(bitmap, 2, 2, 2) == 200, "despeckle flat field far from speck unchanged");
			bitmap.Dispose();
		}

		private static void TestDespeckleKeepsHardEdge()
		{
			SKColor black = new SKColor(0, 0, 0, 255);
			SKColor white = new SKColor(255, 255, 255, 255);
			SKBitmap bitmap = new SKBitmap(16, 16, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FillRaw(bitmap, 0, 0, 8, 16, black);
			FillRaw(bitmap, 8, 0, 16, 16, white);
			SKBitmap expected = new SKBitmap(16, 16, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FillRaw(expected, 0, 0, 8, 16, black);
			FillRaw(expected, 8, 0, 16, 16, white);
			FilterNoise.Despeckle(bitmap);
			Check(BitmapBytesEqual(bitmap, expected), "despeckle hard edge byte-identical on both sides");
			bitmap.Dispose();
			expected.Dispose();
		}

		private static void TestMedianOutlier()
		{
			SKColor field = new SKColor(100, 150, 200, 255);
			SKColor outlier = new SKColor(250, 10, 90, 255);
			SKBitmap bitmap = new SKBitmap(5, 5, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FillRaw(bitmap, 0, 0, 5, 5, field);
			SetRawPixel(bitmap, 2, 2, outlier);
			FilterNoise.Median(bitmap, 1);
			Check(GetRawChannel(bitmap, 2, 2, 0) == 100, "median outlier red is hand-computed median 100");
			Check(GetRawChannel(bitmap, 2, 2, 1) == 150, "median outlier green is hand-computed median 150");
			Check(GetRawChannel(bitmap, 2, 2, 2) == 200, "median outlier blue is hand-computed median 200");
			Check(GetRawChannel(bitmap, 2, 2, 3) == 255, "median outlier alpha is hand-computed median 255");
			Check(GetRawChannel(bitmap, 0, 0, 0) == 100 && GetRawChannel(bitmap, 0, 0, 1) == 150 && GetRawChannel(bitmap, 0, 0, 2) == 200, "median corner window stays field color");
			bitmap.Dispose();
		}

		private static void TestMedianUniform()
		{
			SKColor color = new SKColor(37, 99, 181, 255);
			SKBitmap bitmap = new SKBitmap(32, 32, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FillRaw(bitmap, 0, 0, 32, 32, color);
			SKBitmap expected = new SKBitmap(32, 32, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FillRaw(expected, 0, 0, 32, 32, color);
			FilterNoise.Median(bitmap, 3);
			Check(BitmapBytesEqual(bitmap, expected), "median preserves uniform bitmap exactly");
			bitmap.Dispose();
			expected.Dispose();
		}

		private static void TestMedianAlphaBoundary()
		{
			SKColor opaqueRed = new SKColor(255, 0, 0, 255);
			SKColor transparent = new SKColor(0, 0, 0, 0);
			SKBitmap bitmap = new SKBitmap(16, 16, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FillRaw(bitmap, 0, 0, 8, 16, opaqueRed);
			FillRaw(bitmap, 8, 0, 16, 16, transparent);
			FilterNoise.Median(bitmap, 1);
			bool noDarkening = true;
			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					int alpha = GetRawChannel(bitmap, x, y, 3);
					if (alpha == 0)
					{
						continue;
					}
					int red = GetRawChannel(bitmap, x, y, 0);
					if (red < 253)
					{
						Console.WriteLine("  darkened pixel at " + x + "," + y + " red " + red + " alpha " + alpha);
						noDarkening = false;
					}
				}
			}
			Check(noDarkening, "median opaque region next to transparency does not darken");
			bitmap.Dispose();
		}

		private static void TestParallelMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			SKBitmap parallelDespeckle = BuildRandomBitmap(101, 64, 300);
			SKBitmap parallelMedian = BuildRandomBitmap(202, 64, 300);
			RowBands.SetMaxBands(savedMax);
			FilterNoise.Despeckle(parallelDespeckle);
			FilterNoise.Median(parallelMedian, 2);
			RowBands.SetMaxBands(1);
			SKBitmap singleDespeckle = BuildRandomBitmap(101, 64, 300);
			SKBitmap singleMedian = BuildRandomBitmap(202, 64, 300);
			FilterNoise.Despeckle(singleDespeckle);
			FilterNoise.Median(singleMedian, 2);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapBytesEqual(parallelDespeckle, singleDespeckle), "parallel despeckle matches single band");
			Check(BitmapBytesEqual(parallelMedian, singleMedian), "parallel median matches single band");
			parallelDespeckle.Dispose();
			parallelMedian.Dispose();
			singleDespeckle.Dispose();
			singleMedian.Dispose();
		}
	}
}
