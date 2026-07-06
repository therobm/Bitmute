using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterGenerateTests
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

		private static SKBitmap BuildHorizontalRampBitmap(int width, int height, int baseValue, int step)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int value = baseValue + (step * x);
					if (value < 0)
					{
						value = 0;
					}
					if (value > 255)
					{
						value = 255;
					}
					byte gray = (byte)value;
					bitmap.SetPixel(x, y, new SKColor(gray, gray, gray, 255));
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildBumpyBitmap(int width, int height, int seed)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Random random = new Random(seed);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte red = (byte)random.Next(256);
					byte green = (byte)random.Next(256);
					byte blue = (byte)random.Next(256);
					byte alpha = (byte)(100 + random.Next(156));
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, alpha));
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildTilingSineBitmap(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					double phase = (2.0 * Math.PI * x) / width;
					double value = 127.5 + (100.0 * Math.Sin(phase));
					int rounded = (int)Math.Round(value);
					if (rounded < 0)
					{
						rounded = 0;
					}
					if (rounded > 255)
					{
						rounded = 255;
					}
					byte gray = (byte)rounded;
					bitmap.SetPixel(x, y, new SKColor(gray, gray, gray, 255));
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

		private static double DecodedLength(byte red, byte green, byte blue)
		{
			double x = ((red / 255.0) * 2.0) - 1.0;
			double y = ((green / 255.0) * 2.0) - 1.0;
			double z = ((blue / 255.0) * 2.0) - 1.0;
			return Math.Sqrt((x * x) + (y * y) + (z * z));
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestFlatInputIsNeutral();
			TestDecodeUnitLength();
			TestBlueNeverNegative();
			TestWrapMatchesClampInInterior();
			TestWrapDiffersFromClampAtBorder();
			TestKernelSizeInvariance();
			TestDeterminism();
			TestParallelMatchesSingleBand();
			return s_failures;
		}

		private static void TestFlatInputIsNeutral()
		{
			FilterGenerate.eNormalMapKernel[] kernels = new FilterGenerate.eNormalMapKernel[] { FilterGenerate.eNormalMapKernel.Sobel3, FilterGenerate.eNormalMapKernel.Prewitt3, FilterGenerate.eNormalMapKernel.Sobel5, FilterGenerate.eNormalMapKernel.Sobel9 };
			float[] strengths = new float[] { 0.5f, 1.0f, 4.0f };
			FilterGenerate.eNormalMapEdge[] edges = new FilterGenerate.eNormalMapEdge[] { FilterGenerate.eNormalMapEdge.Wrap, FilterGenerate.eNormalMapEdge.Clamp };
			byte[] grays = new byte[] { 0, 40, 128, 200, 255 };
			for (int kernelIndex = 0; kernelIndex < kernels.Length; kernelIndex++)
			{
				for (int strengthIndex = 0; strengthIndex < strengths.Length; strengthIndex++)
				{
					for (int edgeIndex = 0; edgeIndex < edges.Length; edgeIndex++)
					{
						for (int grayIndex = 0; grayIndex < grays.Length; grayIndex++)
						{
							SKBitmap bitmap = BuildUniformBitmap(20, 20, grays[grayIndex], grays[grayIndex], grays[grayIndex], 210);
							FilterGenerate.NormalMap(bitmap, strengths[strengthIndex], kernels[kernelIndex], false, false, edges[edgeIndex]);
							bool allNeutral = true;
							bool alphaKept = true;
							for (int y = 0; y < 20; y++)
							{
								for (int x = 0; x < 20; x++)
								{
									if (PixelByte(bitmap, x, y, 0) != 128 || PixelByte(bitmap, x, y, 1) != 128 || PixelByte(bitmap, x, y, 2) != 255)
									{
										allNeutral = false;
									}
									if (PixelByte(bitmap, x, y, 3) != 210)
									{
										alphaKept = false;
									}
								}
							}
							Check(allNeutral, "flat input neutral kernel " + kernels[kernelIndex] + " strength " + strengths[strengthIndex] + " edge " + edges[edgeIndex] + " gray " + grays[grayIndex]);
							Check(alphaKept, "flat input keeps alpha kernel " + kernels[kernelIndex] + " edge " + edges[edgeIndex] + " gray " + grays[grayIndex]);
							bitmap.Dispose();
						}
					}
				}
			}
		}

		private static void TestDecodeUnitLength()
		{
			FilterGenerate.eNormalMapKernel[] kernels = new FilterGenerate.eNormalMapKernel[] { FilterGenerate.eNormalMapKernel.Sobel3, FilterGenerate.eNormalMapKernel.Prewitt3, FilterGenerate.eNormalMapKernel.Sobel5, FilterGenerate.eNormalMapKernel.Sobel9 };
			double tolerance = (2.0 / 255.0) * Math.Sqrt(3.0) + 0.0001;
			for (int kernelIndex = 0; kernelIndex < kernels.Length; kernelIndex++)
			{
				SKBitmap bitmap = BuildBumpyBitmap(40, 40, 4242 + kernelIndex);
				FilterGenerate.NormalMap(bitmap, 2.5f, kernels[kernelIndex], false, false, FilterGenerate.eNormalMapEdge.Clamp);
				bool allUnit = true;
				for (int y = 0; y < 40; y++)
				{
					for (int x = 0; x < 40; x++)
					{
						double length = DecodedLength(PixelByte(bitmap, x, y, 0), PixelByte(bitmap, x, y, 1), PixelByte(bitmap, x, y, 2));
						double delta = length - 1.0;
						if (delta < 0.0)
						{
							delta = -delta;
						}
						if (delta > tolerance)
						{
							allUnit = false;
						}
					}
				}
				Check(allUnit, "decoded vectors are unit length kernel " + kernels[kernelIndex]);
				bitmap.Dispose();
			}
		}

		private static void TestBlueNeverNegative()
		{
			FilterGenerate.eNormalMapKernel[] kernels = new FilterGenerate.eNormalMapKernel[] { FilterGenerate.eNormalMapKernel.Sobel3, FilterGenerate.eNormalMapKernel.Prewitt3, FilterGenerate.eNormalMapKernel.Sobel5, FilterGenerate.eNormalMapKernel.Sobel9 };
			for (int kernelIndex = 0; kernelIndex < kernels.Length; kernelIndex++)
			{
				SKBitmap bitmap = BuildBumpyBitmap(48, 48, 909 + kernelIndex);
				FilterGenerate.NormalMap(bitmap, 6.0f, kernels[kernelIndex], true, true, FilterGenerate.eNormalMapEdge.Wrap);
				bool allNonNegative = true;
				for (int y = 0; y < 48; y++)
				{
					for (int x = 0; x < 48; x++)
					{
						if (PixelByte(bitmap, x, y, 2) < 128)
						{
							allNonNegative = false;
						}
					}
				}
				Check(allNonNegative, "blue channel stays at or above 128 kernel " + kernels[kernelIndex]);
				bitmap.Dispose();
			}
		}

		private static void TestWrapMatchesClampInInterior()
		{
			SKBitmap wrap = BuildTilingSineBitmap(64, 16);
			SKBitmap clamp = BuildTilingSineBitmap(64, 16);
			FilterGenerate.NormalMap(wrap, 3.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, false, FilterGenerate.eNormalMapEdge.Wrap);
			FilterGenerate.NormalMap(clamp, 3.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, false, FilterGenerate.eNormalMapEdge.Clamp);
			bool interiorMatches = true;
			for (int y = 0; y < 16; y++)
			{
				for (int x = 4; x < 60; x++)
				{
					if (PixelByte(wrap, x, y, 0) != PixelByte(clamp, x, y, 0) || PixelByte(wrap, x, y, 1) != PixelByte(clamp, x, y, 1) || PixelByte(wrap, x, y, 2) != PixelByte(clamp, x, y, 2))
					{
						interiorMatches = false;
					}
				}
			}
			Check(interiorMatches, "wrap and clamp agree away from the horizontal border");
			wrap.Dispose();
			clamp.Dispose();
		}

		private static void TestWrapDiffersFromClampAtBorder()
		{
			SKBitmap wrap = BuildTilingSineBitmap(64, 16);
			SKBitmap clamp = BuildTilingSineBitmap(64, 16);
			FilterGenerate.NormalMap(wrap, 3.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, false, FilterGenerate.eNormalMapEdge.Wrap);
			FilterGenerate.NormalMap(clamp, 3.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, false, FilterGenerate.eNormalMapEdge.Clamp);
			int wrapLeftRed = PixelByte(wrap, 0, 8, 0);
			int clampLeftRed = PixelByte(clamp, 0, 8, 0);
			int expectedWrapLeftRed = ExpectedSobel3RampRed(118, 137, 3.0f);
			int expectedClampLeftRed = ExpectedSobel3RampRed(128, 137, 3.0f);
			bool wrapMatchesWrappedNeighbor = NearInt(wrapLeftRed, expectedWrapLeftRed, 1);
			bool clampMatchesClampedNeighbor = NearInt(clampLeftRed, expectedClampLeftRed, 1);
			bool borderDiffers = wrapLeftRed != clampLeftRed;
			Check(wrapMatchesWrappedNeighbor, "wrap left border uses the wrapped right-edge neighbor (expected " + expectedWrapLeftRed + " got " + wrapLeftRed + ")");
			Check(clampMatchesClampedNeighbor, "clamp left border uses the clamped self neighbor (expected " + expectedClampLeftRed + " got " + clampLeftRed + ")");
			Check(borderDiffers, "wrap and clamp differ at the left border (" + wrapLeftRed + " vs " + clampLeftRed + ")");
			Check(wrapLeftRed < 128, "wrap left border tilts like a rising ramp");
			wrap.Dispose();
			clamp.Dispose();
		}

		private static bool NearInt(int actual, int expected, int tolerance)
		{
			int delta = actual - expected;
			if (delta < 0)
			{
				delta = -delta;
			}
			return delta <= tolerance;
		}

		private static int ExpectedSobel3RampRed(int leftGray, int rightGray, float strength)
		{
			double dx = 0.5 * (((double)rightGray - (double)leftGray) / 255.0);
			double vectorX = -dx * strength;
			double vectorZ = 1.0;
			double length = Math.Sqrt((vectorX * vectorX) + (vectorZ * vectorZ));
			double normalizedX = vectorX / length;
			double mapped = ((normalizedX * 0.5) + 0.5) * 255.0;
			int rounded = (int)Math.Round(mapped);
			if (rounded < 0)
			{
				rounded = 0;
			}
			if (rounded > 255)
			{
				rounded = 255;
			}
			return rounded;
		}

		private static void TestKernelSizeInvariance()
		{
			FilterGenerate.eNormalMapKernel[] kernels = new FilterGenerate.eNormalMapKernel[] { FilterGenerate.eNormalMapKernel.Sobel3, FilterGenerate.eNormalMapKernel.Prewitt3, FilterGenerate.eNormalMapKernel.Sobel5, FilterGenerate.eNormalMapKernel.Sobel9 };
			int sampleX = 32;
			int sampleY = 8;
			int referenceRed = -1;
			bool allClose = true;
			for (int kernelIndex = 0; kernelIndex < kernels.Length; kernelIndex++)
			{
				SKBitmap bitmap = BuildHorizontalRampBitmap(64, 16, 40, 2);
				FilterGenerate.NormalMap(bitmap, 1.0f, kernels[kernelIndex], false, false, FilterGenerate.eNormalMapEdge.Clamp);
				int red = PixelByte(bitmap, sampleX, sampleY, 0);
				if (referenceRed < 0)
				{
					referenceRed = red;
				}
				else
				{
					int delta = red - referenceRed;
					if (delta < 0)
					{
						delta = -delta;
					}
					if (delta > 1)
					{
						allClose = false;
					}
				}
				bitmap.Dispose();
			}
			Check(allClose, "linear ramp red channel matches across kernel sizes within tolerance");
			Check(referenceRed != 128, "linear ramp produces a non-neutral tilt");
		}

		private static void TestDeterminism()
		{
			SKBitmap first = BuildBumpyBitmap(50, 50, 31337);
			SKBitmap second = BuildBumpyBitmap(50, 50, 31337);
			FilterGenerate.NormalMap(first, 2.0f, FilterGenerate.eNormalMapKernel.Sobel5, true, false, FilterGenerate.eNormalMapEdge.Wrap);
			FilterGenerate.NormalMap(second, 2.0f, FilterGenerate.eNormalMapKernel.Sobel5, true, false, FilterGenerate.eNormalMapEdge.Wrap);
			Check(BitmapBytesEqual(first, second), "same input and settings yield byte-identical output");
			first.Dispose();
			second.Dispose();
		}

		private static void TestParallelMatchesSingleBand()
		{
			int savedMax = RowBands.MaxBands();
			RowBands.SetMaxBands(savedMax);
			SKBitmap parallelSobel3 = BuildBumpyBitmap(96, 300, 71);
			SKBitmap parallelSobel9 = BuildBumpyBitmap(96, 300, 72);
			FilterGenerate.NormalMap(parallelSobel3, 3.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, true, FilterGenerate.eNormalMapEdge.Clamp);
			FilterGenerate.NormalMap(parallelSobel9, 3.0f, FilterGenerate.eNormalMapKernel.Sobel9, false, true, FilterGenerate.eNormalMapEdge.Wrap);
			RowBands.SetMaxBands(1);
			SKBitmap singleSobel3 = BuildBumpyBitmap(96, 300, 71);
			SKBitmap singleSobel9 = BuildBumpyBitmap(96, 300, 72);
			FilterGenerate.NormalMap(singleSobel3, 3.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, true, FilterGenerate.eNormalMapEdge.Clamp);
			FilterGenerate.NormalMap(singleSobel9, 3.0f, FilterGenerate.eNormalMapKernel.Sobel9, false, true, FilterGenerate.eNormalMapEdge.Wrap);
			RowBands.SetMaxBands(savedMax);
			Check(BitmapBytesEqual(parallelSobel3, singleSobel3), "parallel sobel3 matches single band");
			Check(BitmapBytesEqual(parallelSobel9, singleSobel9), "parallel sobel9 matches single band");
			parallelSobel3.Dispose();
			parallelSobel9.Dispose();
			singleSobel3.Dispose();
			singleSobel9.Dispose();
		}
	}
}
