using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.UI;

namespace Bitmute.Tests
{
	public static class GpuFilterPreviewTests
	{
		private const int FilterKindBox = 0;
		private const int FilterKindMotion = 1;
		private const int FilterKindRadial = 2;
		private const int FilterKindGaussian = 3;
		private const int TestWidth = 96;
		private const int TestHeight = 64;
		private const int TestSeed = 731;
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
			CompareBitmapsPremulInset(cpuResult, gpuResult, tolerance, 0, label);
		}

		private static void CompareBitmapsPremulInset(SKBitmap cpuResult, SKBitmap gpuResult, int tolerance, int inset, string label)
		{
			if (cpuResult.Width != gpuResult.Width || cpuResult.Height != gpuResult.Height)
			{
				Check(false, label + " dimensions mismatch " + cpuResult.Width + "x" + cpuResult.Height + " vs " + gpuResult.Width + "x" + gpuResult.Height);
				return;
			}
			int mismatchCount = 0;
			int printedCount = 0;
			for (int y = inset; y < cpuResult.Height - inset; y++)
			{
				for (int x = inset; x < cpuResult.Width - inset; x++)
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

		private static void BuildEffectArgs(int filterKind, int valueA, int valueB, out string skslSource, out int passes, out bool builtinBlur, out int[] values)
		{
			if (filterKind == FilterKindBox)
			{
				skslSource = GpuFilterPreview.BoxBlurSource;
				passes = 2;
				builtinBlur = false;
				values = new int[] { valueA };
			}
			else if (filterKind == FilterKindMotion)
			{
				skslSource = GpuFilterPreview.MotionBlurSource;
				passes = 1;
				builtinBlur = false;
				values = new int[] { valueA, valueB };
			}
			else if (filterKind == FilterKindRadial)
			{
				skslSource = GpuFilterPreview.RadialBlurSource;
				passes = 1;
				builtinBlur = false;
				values = new int[] { valueA, valueB };
			}
			else
			{
				skslSource = null;
				passes = 1;
				builtinBlur = true;
				values = new int[] { valueA };
			}
		}

		private static void ApplyCpuReference(int filterKind, SKBitmap bitmap, int valueA, int valueB)
		{
			if (filterKind == FilterKindBox)
			{
				FilterBlur.BoxBlur(bitmap, valueA);
			}
			else if (filterKind == FilterKindMotion)
			{
				FilterBlur.MotionBlur(bitmap, valueA, valueB);
			}
			else if (filterKind == FilterKindRadial)
			{
				FilterBlur.RadialBlur(bitmap, valueA, valueB);
			}
			else
			{
				Adjustments.GaussianBlur(bitmap, valueA);
			}
		}

		private static bool RunGpuEffect(SKBitmap input, int filterKind, int valueA, int valueB, SKBitmap destination)
		{
			string skslSource;
			int passes;
			bool builtinBlur;
			int[] values;
			BuildEffectArgs(filterKind, valueA, valueB, out skslSource, out passes, out builtinBlur, out values);
			SKImageInfo scratchInfo = new SKImageInfo(input.Width, input.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
			SKSurface scratchA = SKSurface.Create(scratchInfo);
			SKSurface scratchB = SKSurface.Create(scratchInfo);
			SKImage source = SKImage.FromPixels(input.PeekPixels());
			bool succeeded = GpuFilterPreview.RunEffect(scratchA, scratchB, source, skslSource, passes, builtinBlur, values, destination);
			source.Dispose();
			scratchA.Dispose();
			scratchB.Dispose();
			return succeeded;
		}

		private static void RunRandomCase(int filterKind, int valueA, int valueB, int tolerance, string name)
		{
			SKBitmap cpuBitmap = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap gpuInput = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap destination = new SKBitmap(TestWidth, TestHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			ApplyCpuReference(filterKind, cpuBitmap, valueA, valueB);
			bool succeeded = RunGpuEffect(gpuInput, filterKind, valueA, valueB, destination);
			Check(succeeded, name + " RunEffect returned true");
			if (succeeded)
			{
				CompareBitmapsPremul(cpuBitmap, destination, tolerance, name + " matches cpu reference");
			}
			cpuBitmap.Dispose();
			gpuInput.Dispose();
			destination.Dispose();
		}

		private static void RunUniformCase(int filterKind, int valueA, int valueB, string name)
		{
			SKColor color = new SKColor(90, 160, 40, 200);
			SKBitmap input = BuildUniformBitmap(TestWidth, TestHeight, color);
			SKBitmap expected = BuildUniformBitmap(TestWidth, TestHeight, color);
			SKBitmap destination = new SKBitmap(TestWidth, TestHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bool succeeded = RunGpuEffect(input, filterKind, valueA, valueB, destination);
			Check(succeeded, name + " RunEffect returned true");
			if (succeeded)
			{
				CompareBitmapsPremul(expected, destination, 1, name + " uniform bitmap is a no-op");
			}
			input.Dispose();
			expected.Dispose();
			destination.Dispose();
		}

		public static int RunAll()
		{
			TestBoxBlurRadiusThree();
			TestBoxBlurRadiusTwentyFive();
			TestMotionBlurAngleZeroDistanceTen();
			TestMotionBlurAngleFortyFiveDistanceSixty();
			TestMotionBlurAngleNegativeNinetyDistanceFive();
			TestRadialSpinAmountThirty();
			TestRadialSpinAmountHundred();
			TestRadialZoomAmountFifty();
			TestGaussianBuiltinRadiusTwo();
			TestGaussianBuiltinRadiusTen();
			TestUniformNoOps();
			TestCanRunShaderSources();
			return s_failures;
		}

		private static void TestBoxBlurRadiusThree()
		{
			RunRandomCase(FilterKindBox, 3, 0, 3, "gpu box blur radius 3");
		}

		private static void TestBoxBlurRadiusTwentyFive()
		{
			RunRandomCase(FilterKindBox, 25, 0, 3, "gpu box blur radius 25");
		}

		private static void TestMotionBlurAngleZeroDistanceTen()
		{
			RunRandomCase(FilterKindMotion, 0, 10, 3, "gpu motion blur angle 0 distance 10");
		}

		private static void TestMotionBlurAngleFortyFiveDistanceSixty()
		{
			RunRandomCase(FilterKindMotion, 45, 60, 3, "gpu motion blur angle 45 distance 60");
		}

		private static void TestMotionBlurAngleNegativeNinetyDistanceFive()
		{
			RunRandomCase(FilterKindMotion, -90, 5, 3, "gpu motion blur angle -90 distance 5");
		}

		private static void TestRadialSpinAmountThirty()
		{
			RunRandomCase(FilterKindRadial, 30, 0, 3, "gpu radial spin amount 30");
		}

		private static void TestRadialSpinAmountHundred()
		{
			RunRandomCase(FilterKindRadial, 100, 0, 3, "gpu radial spin amount 100");
		}

		private static void TestRadialZoomAmountFifty()
		{
			RunRandomCase(FilterKindRadial, 50, 1, 3, "gpu radial zoom amount 50");
		}

		private static void RunGaussianCase(int radius, string name)
		{
			int width = 192;
			int height = 128;
			SKBitmap cpuBitmap = BuildAlphaVaryingBitmap(width, height, TestSeed);
			SKBitmap gpuInput = BuildAlphaVaryingBitmap(width, height, TestSeed);
			SKBitmap destination = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			ApplyCpuReference(FilterKindGaussian, cpuBitmap, radius, 0);
			bool succeeded = RunGpuEffect(gpuInput, FilterKindGaussian, radius, 0, destination);
			Check(succeeded, name + " RunEffect returned true");
			if (succeeded)
			{
				double sigma = Math.Sqrt((double)radius * (radius + 1));
				int inset = 3 * (int)Math.Ceiling(sigma);
				CompareBitmapsPremulInset(cpuBitmap, destination, 6, inset, name + " matches cpu reference in the interior");
			}
			cpuBitmap.Dispose();
			gpuInput.Dispose();
			destination.Dispose();
		}

		private static void TestGaussianBuiltinRadiusTwo()
		{
			RunGaussianCase(2, "gpu builtin gaussian radius 2");
		}

		private static void TestGaussianBuiltinRadiusTen()
		{
			RunGaussianCase(10, "gpu builtin gaussian radius 10");
		}

		private static void TestUniformNoOps()
		{
			RunUniformCase(FilterKindBox, 5, 0, "gpu box blur radius 5");
			RunUniformCase(FilterKindMotion, 0, 10, "gpu motion blur angle 0 distance 10");
			RunUniformCase(FilterKindRadial, 50, 0, "gpu radial spin amount 50");
			RunUniformCase(FilterKindRadial, 50, 1, "gpu radial zoom amount 50");
			RunUniformCase(FilterKindGaussian, 5, 0, "gpu builtin gaussian radius 5");
		}

		private static void TestCanRunShaderSources()
		{
			Check(GpuFilterPreview.CanRun(GpuFilterPreview.BoxBlurSource), "CanRun compiles box blur shader source");
			Check(GpuFilterPreview.CanRun(GpuFilterPreview.MotionBlurSource), "CanRun compiles motion blur shader source");
			Check(GpuFilterPreview.CanRun(GpuFilterPreview.RadialBlurSource), "CanRun compiles radial blur shader source");
		}
	}
}
