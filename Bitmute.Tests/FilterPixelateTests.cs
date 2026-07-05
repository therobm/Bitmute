using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterPixelateTests
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

		private static SKBitmap MakeBitmap(int width, int height)
		{
			return new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
		}

		private static unsafe void FillSolid(SKBitmap bitmap, SKColor color)
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
					pixel[0] = color.Red;
					pixel[1] = color.Green;
					pixel[2] = color.Blue;
					pixel[3] = color.Alpha;
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
					pixel[0] = (byte)(((x * 7) + (y * 3)) & 255);
					pixel[1] = (byte)(((x * 5) + (y * 11)) & 255);
					pixel[2] = (byte)((x + (y * 2)) & 255);
					pixel[3] = (byte)(255 - (((x * 2) + y) % 100));
				}
			}
		}

		private static unsafe void FillTwoTone(SKBitmap bitmap, SKColor leftColor, SKColor rightColor, int splitX)
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
					if (x < splitX)
					{
						pixel[0] = leftColor.Red;
						pixel[1] = leftColor.Green;
						pixel[2] = leftColor.Blue;
						pixel[3] = leftColor.Alpha;
					}
					else
					{
						pixel[0] = rightColor.Red;
						pixel[1] = rightColor.Green;
						pixel[2] = rightColor.Blue;
						pixel[3] = rightColor.Alpha;
					}
				}
			}
		}

		private static unsafe bool BitmapsEqual(SKBitmap first, SKBitmap second)
		{
			if (first.Width != second.Width || first.Height != second.Height)
			{
				return false;
			}
			int width = first.Width;
			int height = first.Height;
			int firstStride = first.RowBytes;
			int secondStride = second.RowBytes;
			byte* firstBase = (byte*)first.GetPixels().ToPointer();
			byte* secondBase = (byte*)second.GetPixels().ToPointer();
			int rowLength = width * 4;
			for (int y = 0; y < height; y++)
			{
				byte* firstRow = firstBase + ((long)y * firstStride);
				byte* secondRow = secondBase + ((long)y * secondStride);
				for (int index = 0; index < rowLength; index++)
				{
					if (firstRow[index] != secondRow[index])
					{
						return false;
					}
				}
			}
			return true;
		}

		private static unsafe bool RegionIsColor(SKBitmap bitmap, int left, int top, int right, int bottom, SKColor color)
		{
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = top; y < bottom; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				for (int x = left; x < right; x++)
				{
					byte* pixel = row + (x * 4);
					if (pixel[0] != color.Red || pixel[1] != color.Green || pixel[2] != color.Blue || pixel[3] != color.Alpha)
					{
						return false;
					}
				}
			}
			return true;
		}

		private static unsafe bool OnlyContainsColors(SKBitmap bitmap, SKColor[] colors)
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
					bool matched = false;
					for (int index = 0; index < colors.Length; index++)
					{
						SKColor color = colors[index];
						if (pixel[0] == color.Red && pixel[1] == color.Green && pixel[2] == color.Blue && pixel[3] == color.Alpha)
						{
							matched = true;
							break;
						}
					}
					if (!matched)
					{
						return false;
					}
				}
			}
			return true;
		}

		private static unsafe bool ContainsColor(SKBitmap bitmap, SKColor color)
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
					if (pixel[0] == color.Red && pixel[1] == color.Green && pixel[2] == color.Blue && pixel[3] == color.Alpha)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestFacetUniform();
			TestFragmentUniform();
			TestCrystallizeDeterminism();
			TestCrystallizeTwoColor();
			TestPointillizeSourceColors();
			TestPointillizeDeterminism();
			TestBandedMatchesSingleBand();
			return s_failures;
		}

		private static void TestFacetUniform()
		{
			SKColor color = new SKColor(80, 140, 220, 255);
			SKBitmap bitmap = MakeBitmap(64, 48);
			FillSolid(bitmap, color);
			FilterPixelate.Facet(bitmap);
			Check(RegionIsColor(bitmap, 0, 0, 64, 48, color), "facet uniform bitmap byte-identical");
			bitmap.Dispose();
		}

		private static void TestFragmentUniform()
		{
			SKColor color = new SKColor(35, 190, 110, 255);
			SKBitmap bitmap = MakeBitmap(64, 48);
			FillSolid(bitmap, color);
			FilterPixelate.Fragment(bitmap);
			Check(RegionIsColor(bitmap, 4, 4, 60, 44, color), "fragment uniform bitmap interior byte-identical");
			bitmap.Dispose();
		}

		private static void TestCrystallizeDeterminism()
		{
			SKBitmap first = MakeBitmap(80, 60);
			SKBitmap second = MakeBitmap(80, 60);
			SKBitmap third = MakeBitmap(80, 60);
			FillPattern(first);
			FillPattern(second);
			FillPattern(third);
			FilterPixelate.Crystallize(first, 7, 11);
			FilterPixelate.Crystallize(second, 7, 11);
			FilterPixelate.Crystallize(third, 7, 12);
			Check(BitmapsEqual(first, second), "crystallize same seed byte-identical");
			Check(!BitmapsEqual(first, third), "crystallize different seed differs");
			first.Dispose();
			second.Dispose();
			third.Dispose();
		}

		private static void TestCrystallizeTwoColor()
		{
			SKColor colorA = new SKColor(100, 50, 25, 255);
			SKColor colorB = new SKColor(101, 50, 25, 255);
			SKBitmap bitmap = MakeBitmap(96, 64);
			FillTwoTone(bitmap, colorA, colorB, 48);
			FilterPixelate.Crystallize(bitmap, 8, 5);
			SKColor[] allowed = new SKColor[] { colorA, colorB };
			Check(OnlyContainsColors(bitmap, allowed), "crystallize two-color outputs only the two input colors");
			Check(ContainsColor(bitmap, colorA), "crystallize two-color keeps first color");
			Check(ContainsColor(bitmap, colorB), "crystallize two-color keeps second color");
			bitmap.Dispose();
		}

		private static void TestPointillizeSourceColors()
		{
			SKColor leftColor = new SKColor(200, 30, 30, 255);
			SKColor rightColor = new SKColor(30, 30, 200, 255);
			SKColor background = new SKColor(10, 240, 10, 255);
			SKBitmap bitmap = MakeBitmap(90, 70);
			FillTwoTone(bitmap, leftColor, rightColor, 45);
			FilterPixelate.Pointillize(bitmap, 10, 3, background);
			SKColor[] allowed = new SKColor[] { background, leftColor, rightColor };
			Check(OnlyContainsColors(bitmap, allowed), "pointillize outputs only background and source colors");
			bool sawDot = ContainsColor(bitmap, leftColor) || ContainsColor(bitmap, rightColor);
			Check(sawDot, "pointillize paints at least one dot");
			bitmap.Dispose();
		}

		private static void TestPointillizeDeterminism()
		{
			SKColor background = new SKColor(255, 0, 255, 255);
			SKBitmap first = MakeBitmap(90, 70);
			SKBitmap second = MakeBitmap(90, 70);
			SKBitmap third = MakeBitmap(90, 70);
			FillPattern(first);
			FillPattern(second);
			FillPattern(third);
			FilterPixelate.Pointillize(first, 9, 21, background);
			FilterPixelate.Pointillize(second, 9, 21, background);
			FilterPixelate.Pointillize(third, 9, 22, background);
			Check(BitmapsEqual(first, second), "pointillize same seed byte-identical");
			Check(!BitmapsEqual(first, third), "pointillize different seed differs");
			first.Dispose();
			second.Dispose();
			third.Dispose();
		}

		private static void TestBandedMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			SKColor background = new SKColor(20, 20, 20, 255);
			SKBitmap parallelCrystallize = MakeBitmap(192, 256);
			SKBitmap parallelFacet = MakeBitmap(192, 256);
			SKBitmap parallelFragment = MakeBitmap(192, 256);
			SKBitmap parallelPointillize = MakeBitmap(192, 256);
			FillPattern(parallelCrystallize);
			FillPattern(parallelFacet);
			FillPattern(parallelFragment);
			FillPattern(parallelPointillize);
			RowBands.SetMaxBands(savedMax);
			FilterPixelate.Crystallize(parallelCrystallize, 9, 41);
			FilterPixelate.Facet(parallelFacet);
			FilterPixelate.Fragment(parallelFragment);
			FilterPixelate.Pointillize(parallelPointillize, 11, 42, background);
			RowBands.SetMaxBands(1);
			SKBitmap singleCrystallize = MakeBitmap(192, 256);
			SKBitmap singleFacet = MakeBitmap(192, 256);
			SKBitmap singleFragment = MakeBitmap(192, 256);
			SKBitmap singlePointillize = MakeBitmap(192, 256);
			FillPattern(singleCrystallize);
			FillPattern(singleFacet);
			FillPattern(singleFragment);
			FillPattern(singlePointillize);
			FilterPixelate.Crystallize(singleCrystallize, 9, 41);
			FilterPixelate.Facet(singleFacet);
			FilterPixelate.Fragment(singleFragment);
			FilterPixelate.Pointillize(singlePointillize, 11, 42, background);
			RowBands.SetMaxBands(savedMax);
			Check(RowBands.MaxBands() == savedMax, "max bands restored");
			Check(BitmapsEqual(parallelCrystallize, singleCrystallize), "parallel crystallize matches single band");
			Check(BitmapsEqual(parallelFacet, singleFacet), "parallel facet matches single band");
			Check(BitmapsEqual(parallelFragment, singleFragment), "parallel fragment matches single band");
			Check(BitmapsEqual(parallelPointillize, singlePointillize), "parallel pointillize matches single band");
			parallelCrystallize.Dispose();
			parallelFacet.Dispose();
			parallelFragment.Dispose();
			parallelPointillize.Dispose();
			singleCrystallize.Dispose();
			singleFacet.Dispose();
			singleFragment.Dispose();
			singlePointillize.Dispose();
		}
	}
}
