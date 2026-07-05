using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterBlurTests
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

		private static bool BitmapsEqual(SKBitmap first, SKBitmap second)
		{
			if (first.Width != second.Width || first.Height != second.Height)
			{
				Console.WriteLine("  dimensions mismatch " + first.Width + "x" + first.Height + " vs " + second.Width + "x" + second.Height);
				return false;
			}
			for (int y = 0; y < first.Height; y++)
			{
				for (int x = 0; x < first.Width; x++)
				{
					SKColor a = first.GetPixel(x, y);
					SKColor b = second.GetPixel(x, y);
					if (a.Red != b.Red || a.Green != b.Green || a.Blue != b.Blue || a.Alpha != b.Alpha)
					{
						Console.WriteLine("  mismatch at " + x + "," + y + " got " + a + " expected " + b);
						return false;
					}
				}
			}
			return true;
		}

		private static SKBitmap BuildUniformBitmap(int width, int height, SKColor color)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bitmap.SetPixel(x, y, color);
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildRandomBitmap(int width, int height, int seed)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Random random = new Random(seed);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bitmap.SetPixel(x, y, new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
				}
			}
			return bitmap;
		}

		public static int RunAll()
		{
			TestBlurUniformNoOp();
			TestBlurMoreUniformNoOp();
			TestBoxBlurUniformNoOp();
			TestMotionBlurUniformNoOp();
			TestRadialSpinUniformNoOp();
			TestRadialZoomUniformNoOp();
			TestAverageMeanColor();
			TestAverageAlphaPreserved();
			TestBoxBlurImpulseSymmetry();
			TestMotionBlurAngleZeroHorizontalOnly();
			TestBoxBlurBandedMatchesSingleBand();
			TestMotionBlurBandedMatchesSingleBand();
			TestRadialBlurBandedMatchesSingleBand();
			TestBoxBlurAlphaFringe();
			return s_failures;
		}

		private static void TestBlurUniformNoOp()
		{
			SKColor color = new SKColor(73, 141, 209, 255);
			SKBitmap actual = BuildUniformBitmap(40, 40, color);
			SKBitmap expected = BuildUniformBitmap(40, 40, color);
			FilterBlur.Blur(actual);
			Check(BitmapsEqual(actual, expected), "blur uniform opaque bitmap is a no-op");
			actual.Dispose();
			expected.Dispose();
		}

		private static void TestBlurMoreUniformNoOp()
		{
			SKColor color = new SKColor(73, 141, 209, 255);
			SKBitmap actual = BuildUniformBitmap(40, 40, color);
			SKBitmap expected = BuildUniformBitmap(40, 40, color);
			FilterBlur.BlurMore(actual);
			Check(BitmapsEqual(actual, expected), "blur more uniform opaque bitmap is a no-op");
			actual.Dispose();
			expected.Dispose();
		}

		private static void TestBoxBlurUniformNoOp()
		{
			SKColor color = new SKColor(73, 141, 209, 255);
			SKBitmap actual = BuildUniformBitmap(40, 40, color);
			SKBitmap expected = BuildUniformBitmap(40, 40, color);
			FilterBlur.BoxBlur(actual, 4);
			Check(BitmapsEqual(actual, expected), "box blur uniform opaque bitmap is a no-op");
			actual.Dispose();
			expected.Dispose();
		}

		private static void TestMotionBlurUniformNoOp()
		{
			SKColor color = new SKColor(73, 141, 209, 255);
			SKBitmap actual = BuildUniformBitmap(40, 40, color);
			SKBitmap expected = BuildUniformBitmap(40, 40, color);
			FilterBlur.MotionBlur(actual, 30, 7);
			Check(BitmapsEqual(actual, expected), "motion blur uniform opaque bitmap is a no-op");
			actual.Dispose();
			expected.Dispose();
		}

		private static void TestRadialSpinUniformNoOp()
		{
			SKColor color = new SKColor(73, 141, 209, 255);
			SKBitmap actual = BuildUniformBitmap(40, 40, color);
			SKBitmap expected = BuildUniformBitmap(40, 40, color);
			FilterBlur.RadialBlur(actual, 60, 0);
			Check(BitmapsEqual(actual, expected), "radial spin blur uniform opaque bitmap is a no-op");
			actual.Dispose();
			expected.Dispose();
		}

		private static void TestRadialZoomUniformNoOp()
		{
			SKColor color = new SKColor(73, 141, 209, 255);
			SKBitmap actual = BuildUniformBitmap(40, 40, color);
			SKBitmap expected = BuildUniformBitmap(40, 40, color);
			FilterBlur.RadialBlur(actual, 60, 1);
			Check(BitmapsEqual(actual, expected), "radial zoom blur uniform opaque bitmap is a no-op");
			actual.Dispose();
			expected.Dispose();
		}

		private static void TestAverageMeanColor()
		{
			SKBitmap bitmap = new SKBitmap(8, 8, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					if (x < 4)
					{
						bitmap.SetPixel(x, y, new SKColor(255, 0, 0, 255));
					}
					else
					{
						bitmap.SetPixel(x, y, new SKColor(0, 0, 255, 255));
					}
				}
			}
			FilterBlur.Average(bitmap);
			bool uniform = true;
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					SKColor pixel = bitmap.GetPixel(x, y);
					if (pixel.Red != 128 || pixel.Green != 0 || pixel.Blue != 128 || pixel.Alpha != 255)
					{
						Console.WriteLine("  average mismatch at " + x + "," + y + " got " + pixel);
						uniform = false;
					}
				}
			}
			Check(uniform, "average of half red half blue is 128,0,128 everywhere");
			bitmap.Dispose();
		}

		private static void TestAverageAlphaPreserved()
		{
			SKBitmap bitmap = new SKBitmap(8, 8, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					byte alpha = (byte)((y * 32) + (x * 4) + 3);
					bitmap.SetPixel(x, y, new SKColor(200, 40, 90, alpha));
				}
			}
			FilterBlur.Average(bitmap);
			bool alphaKept = true;
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					byte expectedAlpha = (byte)((y * 32) + (x * 4) + 3);
					SKColor pixel = bitmap.GetPixel(x, y);
					if (pixel.Alpha != expectedAlpha)
					{
						Console.WriteLine("  alpha mismatch at " + x + "," + y + " got " + pixel.Alpha + " expected " + expectedAlpha);
						alphaKept = false;
					}
				}
			}
			Check(alphaKept, "average preserves per-pixel alpha");
			SKColor firstColor = bitmap.GetPixel(0, 1);
			SKColor lastColor = bitmap.GetPixel(7, 7);
			Check(firstColor.Red == lastColor.Red && firstColor.Green == lastColor.Green && firstColor.Blue == lastColor.Blue, "average writes the same rgb into every pixel");
			bitmap.Dispose();
		}

		private static void TestBoxBlurImpulseSymmetry()
		{
			SKBitmap bitmap = BuildUniformBitmap(25, 25, new SKColor(0, 0, 0, 255));
			bitmap.SetPixel(12, 12, new SKColor(255, 255, 255, 255));
			FilterBlur.BoxBlur(bitmap, 3);
			bool symmetric = true;
			for (int offset = 1; offset <= 8; offset++)
			{
				SKColor right = bitmap.GetPixel(12 + offset, 12);
				SKColor left = bitmap.GetPixel(12 - offset, 12);
				SKColor down = bitmap.GetPixel(12, 12 + offset);
				SKColor up = bitmap.GetPixel(12, 12 - offset);
				if (right.Green != left.Green || down.Green != up.Green || right.Green != down.Green)
				{
					Console.WriteLine("  impulse asymmetry at offset " + offset + " right " + right.Green + " left " + left.Green + " down " + down.Green + " up " + up.Green);
					symmetric = false;
				}
			}
			Check(symmetric, "box blur of a single-pixel impulse is symmetric");
			SKColor center = bitmap.GetPixel(12, 12);
			SKColor near = bitmap.GetPixel(13, 12);
			Check(center.Green >= near.Green, "box blur impulse center is at least as bright as neighbors");
			bitmap.Dispose();
		}

		private static void TestMotionBlurAngleZeroHorizontalOnly()
		{
			SKBitmap bitmap = BuildUniformBitmap(48, 48, new SKColor(255, 255, 255, 255));
			for (int x = 0; x < 48; x++)
			{
				bitmap.SetPixel(x, 24, new SKColor(255, 0, 0, 255));
			}
			FilterBlur.MotionBlur(bitmap, 0, 9);
			SKColor above = bitmap.GetPixel(24, 23);
			SKColor below = bitmap.GetPixel(24, 25);
			SKColor onLine = bitmap.GetPixel(24, 24);
			Check(above.Green == 255, "motion blur angle 0 leaves pixel above horizontal line white");
			Check(below.Green == 255, "motion blur angle 0 leaves pixel below horizontal line white");
			Check(onLine.Green == 0 && onLine.Red == 255, "motion blur angle 0 keeps horizontal line red");
			bitmap.Dispose();
		}

		private static void TestBoxBlurBandedMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			SKBitmap parallel = BuildRandomBitmap(256, 256, 41);
			RowBands.SetMaxBands(savedMax);
			FilterBlur.BoxBlur(parallel, 4);
			RowBands.SetMaxBands(1);
			SKBitmap single = BuildRandomBitmap(256, 256, 41);
			FilterBlur.BoxBlur(single, 4);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapsEqual(parallel, single), "parallel box blur matches single band");
			Check(RowBands.MaxBands() == savedMax, "box blur test restores max bands");
			parallel.Dispose();
			single.Dispose();
		}

		private static void TestMotionBlurBandedMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			SKBitmap parallel = BuildRandomBitmap(256, 256, 42);
			RowBands.SetMaxBands(savedMax);
			FilterBlur.MotionBlur(parallel, 35, 15);
			RowBands.SetMaxBands(1);
			SKBitmap single = BuildRandomBitmap(256, 256, 42);
			FilterBlur.MotionBlur(single, 35, 15);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapsEqual(parallel, single), "parallel motion blur matches single band");
			Check(RowBands.MaxBands() == savedMax, "motion blur test restores max bands");
			parallel.Dispose();
			single.Dispose();
		}

		private static void TestRadialBlurBandedMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			SKBitmap parallelSpin = BuildRandomBitmap(256, 256, 43);
			SKBitmap parallelZoom = BuildRandomBitmap(256, 256, 44);
			RowBands.SetMaxBands(savedMax);
			FilterBlur.RadialBlur(parallelSpin, 80, 0);
			FilterBlur.RadialBlur(parallelZoom, 80, 1);
			RowBands.SetMaxBands(1);
			SKBitmap singleSpin = BuildRandomBitmap(256, 256, 43);
			SKBitmap singleZoom = BuildRandomBitmap(256, 256, 44);
			FilterBlur.RadialBlur(singleSpin, 80, 0);
			FilterBlur.RadialBlur(singleZoom, 80, 1);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapsEqual(parallelSpin, singleSpin), "parallel radial spin blur matches single band");
			Check(BitmapsEqual(parallelZoom, singleZoom), "parallel radial zoom blur matches single band");
			Check(RowBands.MaxBands() == savedMax, "radial blur test restores max bands");
			parallelSpin.Dispose();
			parallelZoom.Dispose();
			singleSpin.Dispose();
			singleZoom.Dispose();
		}

		private static void TestBoxBlurAlphaFringe()
		{
			SKBitmap bitmap = BuildUniformBitmap(64, 64, new SKColor(0, 0, 0, 0));
			for (int y = 20; y < 44; y++)
			{
				for (int x = 20; x < 44; x++)
				{
					bitmap.SetPixel(x, y, new SKColor(255, 0, 0, 255));
				}
			}
			FilterBlur.BoxBlur(bitmap, 5);
			bool noFringe = true;
			int partialCount = 0;
			for (int y = 0; y < 64; y++)
			{
				for (int x = 0; x < 64; x++)
				{
					SKColor pixel = bitmap.GetPixel(x, y);
					if (pixel.Alpha > 0)
					{
						if (pixel.Alpha < 255)
						{
							partialCount = partialCount + 1;
						}
						if (pixel.Red < 253)
						{
							Console.WriteLine("  fringe at " + x + "," + y + " alpha " + pixel.Alpha + " red " + pixel.Red);
							noFringe = false;
						}
					}
				}
			}
			Check(partialCount > 0, "box blur of opaque square on transparent creates partial alpha edges");
			Check(noFringe, "box blur has no dark fringe: every alpha>0 pixel reads red >= 253");
			bitmap.Dispose();
		}
	}
}
