using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterStylizeTests
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

		private static unsafe byte PixelByte(SKBitmap bitmap, int x, int y, int channel)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			return basePointer[((long)y * bitmap.RowBytes) + (x * 4) + channel];
		}

		private static int WeightedLuma(byte red, byte green, byte blue)
		{
			return (299 * red) + (587 * green) + (114 * blue);
		}

		private static SKBitmap BuildNoisyBitmap(int width, int height, int seed)
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

		private static SKBitmap BuildUniformBitmap(int width, int height, byte red, byte green, byte blue, byte alpha)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, alpha));
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
			int width = first.Width;
			int height = first.Height;
			int firstRowBytes = first.RowBytes;
			int secondRowBytes = second.RowBytes;
			byte* firstBase = (byte*)first.GetPixels().ToPointer();
			byte* secondBase = (byte*)second.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* firstRow = firstBase + ((long)y * firstRowBytes);
				byte* secondRow = secondBase + ((long)y * secondRowBytes);
				for (int index = 0; index < width * 4; index++)
				{
					if (firstRow[index] != secondRow[index])
					{
						return false;
					}
				}
			}
			return true;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestSolarizeValues();
			TestEmbossUniform();
			TestFindEdgesUniform();
			TestDiffuseDeterminism();
			TestDiffuseDarkenOnly();
			TestDiffuseLightenOnly();
			TestDiffuseNeighborhood();
			TestParallelMatchesSingleBand();
			return s_failures;
		}

		private static void TestSolarizeValues()
		{
			byte[] inputs = new byte[] { 0, 100, 127, 128, 200, 255 };
			byte[] expected = new byte[] { 0, 100, 127, 127, 55, 0 };
			SKBitmap bitmap = new SKBitmap(6, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int index = 0; index < 6; index++)
			{
				bitmap.SetPixel(index, 0, new SKColor(inputs[index], inputs[index], inputs[index], 200));
			}
			FilterStylize.Solarize(bitmap);
			for (int index = 0; index < 6; index++)
			{
				byte red = PixelByte(bitmap, index, 0, 0);
				byte green = PixelByte(bitmap, index, 0, 1);
				byte blue = PixelByte(bitmap, index, 0, 2);
				byte alpha = PixelByte(bitmap, index, 0, 3);
				Check(red == expected[index] && green == expected[index] && blue == expected[index], "solarize " + inputs[index] + " maps to " + expected[index]);
				Check(alpha == 200, "solarize " + inputs[index] + " keeps alpha");
			}
			bitmap.Dispose();
		}

		private static void TestEmbossUniform()
		{
			SKBitmap bitmap = BuildUniformBitmap(24, 24, 90, 140, 190, 170);
			FilterStylize.Emboss(bitmap, 45, 3, 150);
			bool allGray = true;
			bool alphaKept = true;
			for (int y = 0; y < 24; y++)
			{
				for (int x = 0; x < 24; x++)
				{
					if (PixelByte(bitmap, x, y, 0) != 128 || PixelByte(bitmap, x, y, 1) != 128 || PixelByte(bitmap, x, y, 2) != 128)
					{
						allGray = false;
					}
					if (PixelByte(bitmap, x, y, 3) != 170)
					{
						alphaKept = false;
					}
				}
			}
			Check(allGray, "emboss uniform bitmap is 128 gray everywhere");
			Check(alphaKept, "emboss uniform bitmap preserves alpha");
			bitmap.Dispose();
		}

		private static void TestFindEdgesUniform()
		{
			SKBitmap bitmap = BuildUniformBitmap(24, 24, 90, 140, 190, 170);
			FilterStylize.FindEdges(bitmap);
			bool allWhite = true;
			bool alphaKept = true;
			for (int y = 0; y < 24; y++)
			{
				for (int x = 0; x < 24; x++)
				{
					if (PixelByte(bitmap, x, y, 0) != 255 || PixelByte(bitmap, x, y, 1) != 255 || PixelByte(bitmap, x, y, 2) != 255)
					{
						allWhite = false;
					}
					if (PixelByte(bitmap, x, y, 3) != 170)
					{
						alphaKept = false;
					}
				}
			}
			Check(allWhite, "find edges uniform bitmap is pure white");
			Check(alphaKept, "find edges uniform bitmap preserves alpha");
			bitmap.Dispose();
		}

		private static void TestDiffuseDeterminism()
		{
			SKBitmap first = BuildNoisyBitmap(64, 64, 77);
			SKBitmap second = BuildNoisyBitmap(64, 64, 77);
			SKBitmap third = BuildNoisyBitmap(64, 64, 77);
			FilterStylize.Diffuse(first, 0, 5);
			FilterStylize.Diffuse(second, 0, 5);
			FilterStylize.Diffuse(third, 0, 6);
			Check(BitmapBytesEqual(first, second), "diffuse same seed is byte-identical");
			Check(!BitmapBytesEqual(first, third), "diffuse different seed differs");
			first.Dispose();
			second.Dispose();
			third.Dispose();
		}

		private static void TestDiffuseDarkenOnly()
		{
			SKBitmap darkened = BuildNoisyBitmap(64, 64, 88);
			SKBitmap original = BuildNoisyBitmap(64, 64, 88);
			FilterStylize.Diffuse(darkened, 1, 9);
			bool neverBrighter = true;
			for (int y = 0; y < 64; y++)
			{
				for (int x = 0; x < 64; x++)
				{
					int afterLuma = WeightedLuma(PixelByte(darkened, x, y, 0), PixelByte(darkened, x, y, 1), PixelByte(darkened, x, y, 2));
					int beforeLuma = WeightedLuma(PixelByte(original, x, y, 0), PixelByte(original, x, y, 1), PixelByte(original, x, y, 2));
					if (afterLuma > beforeLuma)
					{
						neverBrighter = false;
					}
				}
			}
			Check(neverBrighter, "diffuse darken only never increases luminance");
			darkened.Dispose();
			original.Dispose();
		}

		private static void TestDiffuseLightenOnly()
		{
			SKBitmap lightened = BuildNoisyBitmap(64, 64, 88);
			SKBitmap original = BuildNoisyBitmap(64, 64, 88);
			FilterStylize.Diffuse(lightened, 2, 9);
			bool neverDarker = true;
			for (int y = 0; y < 64; y++)
			{
				for (int x = 0; x < 64; x++)
				{
					int afterLuma = WeightedLuma(PixelByte(lightened, x, y, 0), PixelByte(lightened, x, y, 1), PixelByte(lightened, x, y, 2));
					int beforeLuma = WeightedLuma(PixelByte(original, x, y, 0), PixelByte(original, x, y, 1), PixelByte(original, x, y, 2));
					if (afterLuma < beforeLuma)
					{
						neverDarker = false;
					}
				}
			}
			Check(neverDarker, "diffuse lighten only never decreases luminance");
			lightened.Dispose();
			original.Dispose();
		}

		private static void TestDiffuseNeighborhood()
		{
			SKBitmap diffused = BuildNoisyBitmap(48, 48, 99);
			SKBitmap original = BuildNoisyBitmap(48, 48, 99);
			FilterStylize.Diffuse(diffused, 0, 11);
			bool allFound = true;
			for (int y = 0; y < 48; y++)
			{
				for (int x = 0; x < 48; x++)
				{
					byte red = PixelByte(diffused, x, y, 0);
					byte green = PixelByte(diffused, x, y, 1);
					byte blue = PixelByte(diffused, x, y, 2);
					byte alpha = PixelByte(diffused, x, y, 3);
					bool found = false;
					for (int dy = -2; dy <= 2; dy++)
					{
						for (int dx = -2; dx <= 2; dx++)
						{
							int sampleX = x + dx;
							int sampleY = y + dy;
							if (sampleX < 0)
							{
								sampleX = 0;
							}
							if (sampleX > 47)
							{
								sampleX = 47;
							}
							if (sampleY < 0)
							{
								sampleY = 0;
							}
							if (sampleY > 47)
							{
								sampleY = 47;
							}
							if (PixelByte(original, sampleX, sampleY, 0) == red && PixelByte(original, sampleX, sampleY, 1) == green && PixelByte(original, sampleX, sampleY, 2) == blue && PixelByte(original, sampleX, sampleY, 3) == alpha)
							{
								found = true;
								break;
							}
						}
						if (found)
						{
							break;
						}
					}
					if (!found)
					{
						allFound = false;
					}
				}
			}
			Check(allFound, "diffuse output pixels come from 5x5 source neighborhood");
			diffused.Dispose();
			original.Dispose();
		}

		private static void TestParallelMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			SKBitmap parallelDiffuse = BuildNoisyBitmap(96, 300, 55);
			SKBitmap parallelEdges = BuildNoisyBitmap(96, 300, 56);
			RowBands.SetMaxBands(savedMax);
			FilterStylize.Diffuse(parallelDiffuse, 0, 21);
			FilterStylize.FindEdges(parallelEdges);
			RowBands.SetMaxBands(1);
			SKBitmap singleDiffuse = BuildNoisyBitmap(96, 300, 55);
			SKBitmap singleEdges = BuildNoisyBitmap(96, 300, 56);
			FilterStylize.Diffuse(singleDiffuse, 0, 21);
			FilterStylize.FindEdges(singleEdges);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapBytesEqual(parallelDiffuse, singleDiffuse), "parallel diffuse matches single band");
			Check(BitmapBytesEqual(parallelEdges, singleEdges), "parallel find edges matches single band");
			parallelDiffuse.Dispose();
			parallelEdges.Dispose();
			singleDiffuse.Dispose();
			singleEdges.Dispose();
		}
	}
}
