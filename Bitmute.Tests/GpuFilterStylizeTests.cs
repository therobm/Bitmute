using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.UI;

namespace Bitmute.Tests
{
	public static class GpuFilterStylizeTests
	{
		private const int TestWidth = 96;
		private const int TestHeight = 64;
		private const int TestSeed = 731;
		private const int Tolerance = 3;
		private const int MaxPrintedMismatches = 5;

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

		private static int AbsDelta(int first, int second)
		{
			int delta = first - second;
			if (delta < 0)
			{
				delta = -delta;
			}
			return delta;
		}

		private static int PremultiplyChannel(int channel, int alpha)
		{
			return ((channel * alpha) + 127) / 255;
		}

		private static SKBitmap BuildAlphaVaryingBitmap(int width, int height, int seed)
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
					int phase = (x + y) % 4;
					byte alpha = 0;
					if (phase == 0)
					{
						alpha = 255;
					}
					else if (phase == 1)
					{
						alpha = (byte)(96 + random.Next(96));
					}
					else if (phase == 2)
					{
						alpha = (byte)(1 + random.Next(40));
					}
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, alpha));
				}
			}
			return bitmap;
		}

		private static void CompareBitmapsPremul(SKBitmap cpuResult, SKBitmap gpuResult, int tolerance, string label)
		{
			if (cpuResult.Width != gpuResult.Width || cpuResult.Height != gpuResult.Height)
			{
				Check(false, label + " dimensions mismatch " + cpuResult.Width + "x" + cpuResult.Height + " vs " + gpuResult.Width + "x" + gpuResult.Height);
				return;
			}
			int mismatchCount = 0;
			int printedCount = 0;
			for (int y = 0; y < cpuResult.Height; y++)
			{
				for (int x = 0; x < cpuResult.Width; x++)
				{
					SKColor cpuPixel = cpuResult.GetPixel(x, y);
					SKColor gpuPixel = gpuResult.GetPixel(x, y);
					int cpuRed = PremultiplyChannel(cpuPixel.Red, cpuPixel.Alpha);
					int cpuGreen = PremultiplyChannel(cpuPixel.Green, cpuPixel.Alpha);
					int cpuBlue = PremultiplyChannel(cpuPixel.Blue, cpuPixel.Alpha);
					int gpuRed = PremultiplyChannel(gpuPixel.Red, gpuPixel.Alpha);
					int gpuGreen = PremultiplyChannel(gpuPixel.Green, gpuPixel.Alpha);
					int gpuBlue = PremultiplyChannel(gpuPixel.Blue, gpuPixel.Alpha);
					bool bad = false;
					if (AbsDelta(cpuPixel.Alpha, gpuPixel.Alpha) > tolerance)
					{
						bad = true;
					}
					if (AbsDelta(cpuRed, gpuRed) > tolerance)
					{
						bad = true;
					}
					if (AbsDelta(cpuGreen, gpuGreen) > tolerance)
					{
						bad = true;
					}
					if (AbsDelta(cpuBlue, gpuBlue) > tolerance)
					{
						bad = true;
					}
					if (bad)
					{
						mismatchCount = mismatchCount + 1;
						if (printedCount < MaxPrintedMismatches)
						{
							printedCount = printedCount + 1;
							Console.WriteLine("  " + label + " mismatch at " + x + "," + y + " cpu premul " + cpuRed + "," + cpuGreen + "," + cpuBlue + " a " + cpuPixel.Alpha + " gpu premul " + gpuRed + "," + gpuGreen + "," + gpuBlue + " a " + gpuPixel.Alpha);
						}
					}
				}
			}
			Check(mismatchCount == 0, label + " (" + mismatchCount + " mismatched pixels, tolerance " + tolerance + ")");
		}

		private static bool RunGpuEffect(SKBitmap input, string skslSource, SKImage offsetMap, int[] values, SKBitmap destination)
		{
			SKImageInfo scratchInfo = new SKImageInfo(input.Width, input.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
			SKSurface scratchA = SKSurface.Create(scratchInfo);
			SKSurface scratchB = SKSurface.Create(scratchInfo);
			SKImage source = SKImage.FromPixels(input.PeekPixels());
			bool succeeded = GpuFilterPreview.RunEffect(scratchA, scratchB, source, skslSource, 1, false, true, offsetMap, values, destination);
			source.Dispose();
			scratchA.Dispose();
			scratchB.Dispose();
			return succeeded;
		}

		private static void RunDiffuseCase(int mode, int seed, string name)
		{
			SKBitmap cpuBitmap = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap gpuInput = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap destination = new SKBitmap(TestWidth, TestHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FilterStylize.Diffuse(cpuBitmap, mode, seed);
			SKBitmap offsetBitmap = new SKBitmap(TestWidth, TestHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			GpuFilterPreview.BuildDiffuseOffsets(offsetBitmap, seed);
			SKImage offsetMap = SKImage.FromPixels(offsetBitmap.PeekPixels());
			int[] values = new int[] { mode };
			bool succeeded = RunGpuEffect(gpuInput, GpuFilterPreview.DiffuseSource, offsetMap, values, destination);
			Check(succeeded, name + " RunEffect returned true");
			if (succeeded)
			{
				CompareBitmapsPremul(cpuBitmap, destination, Tolerance, name + " matches cpu reference");
			}
			offsetMap.Dispose();
			offsetBitmap.Dispose();
			cpuBitmap.Dispose();
			gpuInput.Dispose();
			destination.Dispose();
		}

		private static void RunEmbossCase(int angle, int height, int amount, string name)
		{
			SKBitmap cpuBitmap = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap gpuInput = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap destination = new SKBitmap(TestWidth, TestHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			FilterStylize.Emboss(cpuBitmap, angle, height, amount);
			int offsetX = (int)Math.Round(Math.Cos(angle * (Math.PI / 180.0)) * height);
			int offsetY = (int)Math.Round(-Math.Sin(angle * (Math.PI / 180.0)) * height);
			int[] values = new int[] { offsetX, offsetY, amount };
			bool succeeded = RunGpuEffect(gpuInput, GpuFilterPreview.EmbossSource, null, values, destination);
			Check(succeeded, name + " RunEffect returned true");
			if (succeeded)
			{
				CompareBitmapsPremul(cpuBitmap, destination, Tolerance, name + " matches cpu reference");
			}
			cpuBitmap.Dispose();
			gpuInput.Dispose();
			destination.Dispose();
		}

		public static int RunAll()
		{
			TestDiffuseModeNormal();
			TestDiffuseModeDarkenOnly();
			TestDiffuseModeLightenOnly();
			TestDiffuseHighBitSeed();
			TestEmbossAngle135Height3Amount100();
			TestEmbossAngleNegative90Height10Amount100();
			TestEmbossAngle45Height1Amount500();
			TestEmbossAngle0Height5Amount40();
			return s_failures;
		}

		private static void TestDiffuseModeNormal()
		{
			RunDiffuseCase(0, 123456789, "gpu diffuse mode 0 seed 123456789");
		}

		private static void TestDiffuseModeDarkenOnly()
		{
			RunDiffuseCase(1, 123456789, "gpu diffuse mode 1 seed 123456789");
		}

		private static void TestDiffuseModeLightenOnly()
		{
			RunDiffuseCase(2, 123456789, "gpu diffuse mode 2 seed 123456789");
		}

		private static void TestDiffuseHighBitSeed()
		{
			RunDiffuseCase(0, 0x7FFF4321, "gpu diffuse mode 0 seed 2147435297");
		}

		private static void TestEmbossAngle135Height3Amount100()
		{
			RunEmbossCase(135, 3, 100, "gpu emboss angle 135 height 3 amount 100");
		}

		private static void TestEmbossAngleNegative90Height10Amount100()
		{
			RunEmbossCase(-90, 10, 100, "gpu emboss angle -90 height 10 amount 100");
		}

		private static void TestEmbossAngle45Height1Amount500()
		{
			RunEmbossCase(45, 1, 500, "gpu emboss angle 45 height 1 amount 500");
		}

		private static void TestEmbossAngle0Height5Amount40()
		{
			RunEmbossCase(0, 5, 40, "gpu emboss angle 0 height 5 amount 40");
		}
	}
}
