using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterVideoTests
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

		private static int ReferencePremultiply(int channel, int alpha)
		{
			return ((channel * alpha) + 127) / 255;
		}

		private static int ReferenceUnpremultiply(int premultiplied, int alpha)
		{
			if (alpha == 0)
			{
				return 0;
			}
			int value = ((premultiplied * 255) + (alpha / 2)) / alpha;
			if (value > 255)
			{
				value = 255;
			}
			return value;
		}

		private static SKColor ReferenceAverage(SKColor above, SKColor below)
		{
			int alpha = (above.Alpha + below.Alpha + 1) / 2;
			int red = (ReferencePremultiply(above.Red, above.Alpha) + ReferencePremultiply(below.Red, below.Alpha) + 1) / 2;
			int green = (ReferencePremultiply(above.Green, above.Alpha) + ReferencePremultiply(below.Green, below.Alpha) + 1) / 2;
			int blue = (ReferencePremultiply(above.Blue, above.Alpha) + ReferencePremultiply(below.Blue, below.Alpha) + 1) / 2;
			return new SKColor((byte)ReferenceUnpremultiply(red, alpha), (byte)ReferenceUnpremultiply(green, alpha), (byte)ReferenceUnpremultiply(blue, alpha), (byte)alpha);
		}

		private static SKColor[] SixRowColors()
		{
			SKColor[] colors = new SKColor[6];
			colors[0] = new SKColor(250, 10, 40, 255);
			colors[1] = new SKColor(20, 240, 60, 200);
			colors[2] = new SKColor(60, 90, 230, 255);
			colors[3] = new SKColor(180, 140, 30, 128);
			colors[4] = new SKColor(90, 40, 160, 255);
			colors[5] = new SKColor(10, 200, 220, 64);
			return colors;
		}

		private static unsafe SKBitmap BuildRowBitmap(SKColor[] rows, int width)
		{
			SKBitmap bitmap = new SKBitmap(width, rows.Length, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			for (int y = 0; y < rows.Length; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					pixel[0] = rows[y].Red;
					pixel[1] = rows[y].Green;
					pixel[2] = rows[y].Blue;
					pixel[3] = rows[y].Alpha;
				}
			}
			return bitmap;
		}

		private static unsafe void CheckRows(SKBitmap bitmap, SKColor[] expectedRows, string name)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			for (int y = 0; y < bitmap.Height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < bitmap.Width; x++)
				{
					byte* pixel = row + (x * 4);
					if (pixel[0] != expectedRows[y].Red || pixel[1] != expectedRows[y].Green || pixel[2] != expectedRows[y].Blue || pixel[3] != expectedRows[y].Alpha)
					{
						Check(false, name + " row " + y + " column " + x + " got " + pixel[0] + "," + pixel[1] + "," + pixel[2] + "," + pixel[3] + " expected " + expectedRows[y].Red + "," + expectedRows[y].Green + "," + expectedRows[y].Blue + "," + expectedRows[y].Alpha);
						return;
					}
				}
			}
			Check(true, name);
		}

		private static unsafe SKBitmap BuildLargeBitmap(int seed)
		{
			SKBitmap bitmap = new SKBitmap(64, 300, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Random random = new Random(seed);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			for (int y = 0; y < bitmap.Height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = 0; x < bitmap.Width; x++)
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

		private static unsafe bool BitmapsEqual(SKBitmap first, SKBitmap second)
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
			int byteWidth = first.Width * 4;
			for (int y = 0; y < first.Height; y++)
			{
				byte* firstRow = firstBase + ((long)y * firstStride);
				byte* secondRow = secondBase + ((long)y * secondStride);
				for (int offset = 0; offset < byteWidth; offset++)
				{
					if (firstRow[offset] != secondRow[offset])
					{
						Console.WriteLine("  mismatch at row " + y + " byte " + offset + " got " + firstRow[offset] + " expected " + secondRow[offset]);
						return false;
					}
				}
			}
			return true;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestDeInterlaceDuplicateOddRows();
			TestDeInterlaceDuplicateEvenRows();
			TestDeInterlaceInterpolateOddRows();
			TestDeInterlaceInterpolateEvenRows();
			TestDeInterlaceAlphaInterpolation();
			TestDeInterlaceBandedMatchesSingleBand();
			return s_failures;
		}

		private static void TestDeInterlaceDuplicateOddRows()
		{
			SKColor[] colors = SixRowColors();
			SKBitmap bitmap = BuildRowBitmap(colors, 5);
			FilterVideo.DeInterlace(bitmap, 0, 0);
			SKColor[] expected = new SKColor[6];
			expected[0] = colors[0];
			expected[1] = colors[0];
			expected[2] = colors[2];
			expected[3] = colors[2];
			expected[4] = colors[4];
			expected[5] = colors[4];
			CheckRows(bitmap, expected, "deinterlace duplicate odd rows from kept row above");
			bitmap.Dispose();
		}

		private static void TestDeInterlaceDuplicateEvenRows()
		{
			SKColor[] colors = SixRowColors();
			SKBitmap bitmap = BuildRowBitmap(colors, 5);
			FilterVideo.DeInterlace(bitmap, 1, 0);
			SKColor[] expected = new SKColor[6];
			expected[0] = colors[1];
			expected[1] = colors[1];
			expected[2] = colors[1];
			expected[3] = colors[3];
			expected[4] = colors[3];
			expected[5] = colors[5];
			CheckRows(bitmap, expected, "deinterlace duplicate even rows, row 0 copies kept row below");
			bitmap.Dispose();
		}

		private static void TestDeInterlaceInterpolateOddRows()
		{
			SKColor[] colors = SixRowColors();
			SKBitmap bitmap = BuildRowBitmap(colors, 5);
			FilterVideo.DeInterlace(bitmap, 0, 1);
			SKColor[] expected = new SKColor[6];
			expected[0] = colors[0];
			expected[1] = ReferenceAverage(colors[0], colors[2]);
			expected[2] = colors[2];
			expected[3] = ReferenceAverage(colors[2], colors[4]);
			expected[4] = colors[4];
			expected[5] = colors[4];
			CheckRows(bitmap, expected, "deinterlace interpolate odd rows, last row duplicates kept row above");
			bitmap.Dispose();
		}

		private static void TestDeInterlaceInterpolateEvenRows()
		{
			SKColor[] colors = SixRowColors();
			SKBitmap bitmap = BuildRowBitmap(colors, 5);
			FilterVideo.DeInterlace(bitmap, 1, 1);
			SKColor[] expected = new SKColor[6];
			expected[0] = colors[1];
			expected[1] = colors[1];
			expected[2] = ReferenceAverage(colors[1], colors[3]);
			expected[3] = colors[3];
			expected[4] = ReferenceAverage(colors[3], colors[5]);
			expected[5] = colors[5];
			CheckRows(bitmap, expected, "deinterlace interpolate even rows, row 0 duplicates kept row below");
			bitmap.Dispose();
		}

		private static unsafe void TestDeInterlaceAlphaInterpolation()
		{
			SKColor[] colors = new SKColor[3];
			colors[0] = new SKColor(200, 60, 20, 255);
			colors[1] = new SKColor(1, 2, 3, 4);
			colors[2] = new SKColor(10, 220, 90, 0);
			SKBitmap bitmap = BuildRowBitmap(colors, 4);
			FilterVideo.DeInterlace(bitmap, 0, 1);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			byte* middleRow = basePointer + ((long)1 * bitmap.RowBytes);
			CheckNear(middleRow[3], 128, 1, "alpha interpolation lands near half opacity");
			CheckNear(middleRow[0], 200, 2, "alpha interpolation keeps opaque side red");
			CheckNear(middleRow[1], 60, 2, "alpha interpolation keeps opaque side green");
			CheckNear(middleRow[2], 20, 2, "alpha interpolation keeps opaque side blue");
			bitmap.Dispose();
		}

		private static void TestDeInterlaceBandedMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			SKBitmap parallelOdd = BuildLargeBitmap(41);
			SKBitmap parallelEven = BuildLargeBitmap(42);
			FilterVideo.DeInterlace(parallelOdd, 0, 1);
			FilterVideo.DeInterlace(parallelEven, 1, 1);
			RowBands.SetMaxBands(1);
			SKBitmap singleOdd = BuildLargeBitmap(41);
			SKBitmap singleEven = BuildLargeBitmap(42);
			FilterVideo.DeInterlace(singleOdd, 0, 1);
			FilterVideo.DeInterlace(singleEven, 1, 1);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapsEqual(parallelOdd, singleOdd), "banded interpolate odd rows matches single band");
			Check(BitmapsEqual(parallelEven, singleEven), "banded interpolate even rows matches single band");
			parallelOdd.Dispose();
			parallelEven.Dispose();
			singleOdd.Dispose();
			singleEven.Dispose();
		}
	}
}
