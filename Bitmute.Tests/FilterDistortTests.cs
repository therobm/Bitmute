using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterDistortTests
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

		private static SKBitmap BuildPatternBitmap(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte red = (byte)(((x * 7) + (y * 3)) % 256);
					byte green = (byte)(((x * 5) + (y * 11)) % 256);
					byte blue = (byte)(((x * 13) + (y * 2)) % 256);
					byte alpha = 255;
					if (((x + y) % 9) == 0)
					{
						alpha = 128;
					}
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, alpha));
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildSmoothGradientBitmap(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte red = (byte)(40 + x);
					byte green = (byte)(40 + y);
					byte blue = 128;
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, (byte)255));
				}
			}
			return bitmap;
		}

		private static unsafe bool BitmapBytesEqual(SKBitmap first, SKBitmap second)
		{
			if (first.Width != second.Width || first.Height != second.Height)
			{
				Console.WriteLine("  dimensions mismatch");
				return false;
			}
			byte* firstBase = (byte*)first.GetPixels().ToPointer();
			byte* secondBase = (byte*)second.GetPixels().ToPointer();
			int firstStride = first.RowBytes;
			int secondStride = second.RowBytes;
			int rowLength = first.Width * 4;
			for (int y = 0; y < first.Height; y++)
			{
				byte* firstRow = firstBase + ((long)y * firstStride);
				byte* secondRow = secondBase + ((long)y * secondStride);
				for (int index = 0; index < rowLength; index++)
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

		private static unsafe bool BitmapsDiffer(SKBitmap first, SKBitmap second)
		{
			if (first.Width != second.Width || first.Height != second.Height)
			{
				return true;
			}
			byte* firstBase = (byte*)first.GetPixels().ToPointer();
			byte* secondBase = (byte*)second.GetPixels().ToPointer();
			int firstStride = first.RowBytes;
			int secondStride = second.RowBytes;
			int rowLength = first.Width * 4;
			for (int y = 0; y < first.Height; y++)
			{
				byte* firstRow = firstBase + ((long)y * firstStride);
				byte* secondRow = secondBase + ((long)y * secondStride);
				for (int index = 0; index < rowLength; index++)
				{
					if (firstRow[index] != secondRow[index])
					{
						return true;
					}
				}
			}
			return false;
		}

		private static unsafe bool PixelBytesEqual(SKBitmap first, SKBitmap second, int x, int y)
		{
			byte* firstPixel = (byte*)first.GetPixels().ToPointer() + ((long)y * first.RowBytes) + (x * 4);
			byte* secondPixel = (byte*)second.GetPixels().ToPointer() + ((long)y * second.RowBytes) + (x * 4);
			for (int index = 0; index < 4; index++)
			{
				if (firstPixel[index] != secondPixel[index])
				{
					return false;
				}
			}
			return true;
		}

		private static void TestZeroParameterIdentity()
		{
			SKBitmap reference = BuildPatternBitmap(64, 48);

			SKBitmap pinched = BuildPatternBitmap(64, 48);
			FilterDistort.Pinch(pinched, 0);
			Check(BitmapBytesEqual(pinched, reference), "pinch amount 0 is bit-identical");
			pinched.Dispose();

			SKBitmap twirled = BuildPatternBitmap(64, 48);
			FilterDistort.Twirl(twirled, 0);
			Check(BitmapBytesEqual(twirled, reference), "twirl angle 0 is bit-identical");
			twirled.Dispose();

			SKBitmap rippled = BuildPatternBitmap(64, 48);
			FilterDistort.Ripple(rippled, 0, 1);
			Check(BitmapBytesEqual(rippled, reference), "ripple amount 0 is bit-identical");
			rippled.Dispose();

			SKBitmap sheared = BuildPatternBitmap(64, 48);
			FilterDistort.Shear(sheared, 0, 0);
			Check(BitmapBytesEqual(sheared, reference), "shear amount 0 is bit-identical");
			sheared.Dispose();

			SKBitmap pinchedStrong = BuildPatternBitmap(64, 48);
			FilterDistort.Pinch(pinchedStrong, 80);
			Check(PixelBytesEqual(pinchedStrong, reference, 0, 0), "pinch 80 leaves corner outside inscribed ellipse bit-identical");
			Check(BitmapsDiffer(pinchedStrong, reference), "pinch 80 changes interior content");
			pinchedStrong.Dispose();

			reference.Dispose();
		}

		private static void TestTwirlKeepsCenterPixel()
		{
			SKBitmap bitmap = BuildPatternBitmap(33, 33);
			SKBitmap reference = BuildPatternBitmap(33, 33);
			FilterDistort.Twirl(bitmap, 180);
			Check(PixelBytesEqual(bitmap, reference, 16, 16), "twirl 180 keeps the exact center pixel");
			Check(BitmapsDiffer(bitmap, reference), "twirl 180 changes off-center content");
			bitmap.Dispose();
			reference.Dispose();
		}

		private static void TestSpherizeKeepsCenterPixel()
		{
			SKBitmap bitmap = BuildPatternBitmap(65, 65);
			SKBitmap reference = BuildPatternBitmap(65, 65);
			FilterDistort.Spherize(bitmap, 100, 0);
			Check(PixelBytesEqual(bitmap, reference, 32, 32), "spherize 100 keeps the center pixel color");
			Check(BitmapsDiffer(bitmap, reference), "spherize 100 changes off-center content");
			bitmap.Dispose();
			reference.Dispose();
		}

		private static void TestPolarRoundTrip()
		{
			SKBitmap bitmap = BuildSmoothGradientBitmap(128, 128);
			SKBitmap reference = BuildSmoothGradientBitmap(128, 128);
			FilterDistort.PolarCoordinates(bitmap, 0);
			FilterDistort.PolarCoordinates(bitmap, 1);
			int[] checkX = new int[] { 32, 96, 64, 64, 50, 80 };
			int[] checkY = new int[] { 64, 64, 32, 96, 50, 90 };
			for (int index = 0; index < checkX.Length; index++)
			{
				int x = checkX[index];
				int y = checkY[index];
				SKColor actual = bitmap.GetPixel(x, y);
				SKColor expected = reference.GetPixel(x, y);
				string label = "polar round-trip at " + x + "," + y;
				CheckNear(actual.Red, expected.Red, 30, label + " red");
				CheckNear(actual.Green, expected.Green, 30, label + " green");
				CheckNear(actual.Blue, expected.Blue, 30, label + " blue");
				Check(actual.Alpha >= 250, label + " alpha stays opaque");
			}
			bitmap.Dispose();
			reference.Dispose();
		}

		private static void TestBandedMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			SKBitmap parallelPinch = BuildPatternBitmap(180, 300);
			SKBitmap parallelTwirl = BuildPatternBitmap(180, 300);
			SKBitmap parallelRipple = BuildPatternBitmap(180, 300);
			SKBitmap parallelWave = BuildPatternBitmap(180, 300);
			RowBands.SetMaxBands(savedMax);
			FilterDistort.Pinch(parallelPinch, 60);
			FilterDistort.Twirl(parallelTwirl, 240);
			FilterDistort.Ripple(parallelRipple, 300, 1);
			FilterDistort.Wave(parallelWave, 25, 7, 1);
			RowBands.SetMaxBands(1);
			SKBitmap singlePinch = BuildPatternBitmap(180, 300);
			SKBitmap singleTwirl = BuildPatternBitmap(180, 300);
			SKBitmap singleRipple = BuildPatternBitmap(180, 300);
			SKBitmap singleWave = BuildPatternBitmap(180, 300);
			FilterDistort.Pinch(singlePinch, 60);
			FilterDistort.Twirl(singleTwirl, 240);
			FilterDistort.Ripple(singleRipple, 300, 1);
			FilterDistort.Wave(singleWave, 25, 7, 1);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapBytesEqual(parallelPinch, singlePinch), "banded pinch matches single band");
			Check(BitmapBytesEqual(parallelTwirl, singleTwirl), "banded twirl matches single band");
			Check(BitmapBytesEqual(parallelRipple, singleRipple), "banded ripple matches single band");
			Check(BitmapBytesEqual(parallelWave, singleWave), "banded wave matches single band");
			Check(RowBands.MaxBands() == savedMax, "max bands restored after banded distort tests");
			parallelPinch.Dispose();
			parallelTwirl.Dispose();
			parallelRipple.Dispose();
			parallelWave.Dispose();
			singlePinch.Dispose();
			singleTwirl.Dispose();
			singleRipple.Dispose();
			singleWave.Dispose();
		}

		private static unsafe void TestSpherizeTransparentRedSquare()
		{
			SKBitmap bitmap = new SKBitmap(101, 101, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(new SKColor(0, 0, 0, 0));
			for (int y = 30; y <= 70; y++)
			{
				for (int x = 30; x <= 70; x++)
				{
					bitmap.SetPixel(x, y, new SKColor(255, 0, 0, 255));
				}
			}
			FilterDistort.Spherize(bitmap, 100, 0);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			int minRed = 255;
			int visibleCount = 0;
			for (int y = 0; y < 101; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < 101; x++)
				{
					byte* pixel = row + (x * 4);
					if (pixel[3] > 0)
					{
						visibleCount = visibleCount + 1;
						if (pixel[0] < minRed)
						{
							minRed = pixel[0];
						}
					}
				}
			}
			Check(visibleCount > 0, "spherize red square keeps visible pixels (" + visibleCount + ")");
			Check(minRed >= 253, "spherize keeps unpremultiplied red where alpha > 0 (min red " + minRed + ")");
			bitmap.Dispose();
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestZeroParameterIdentity();
			TestTwirlKeepsCenterPixel();
			TestSpherizeKeepsCenterPixel();
			TestPolarRoundTrip();
			TestBandedMatchesSingleBand();
			TestSpherizeTransparentRedSquare();
			return s_failures;
		}
	}
}
