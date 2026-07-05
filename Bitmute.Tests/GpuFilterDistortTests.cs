using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.UI;

namespace Bitmute.Tests
{
	public static class GpuFilterDistortTests
	{
		private const int FilterKindPinch = 0;
		private const int FilterKindPolar = 1;
		private const int FilterKindRipple = 2;
		private const int FilterKindShear = 3;
		private const int FilterKindSpherize = 4;
		private const int FilterKindTwirl = 5;
		private const int FilterKindWave = 6;
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

		private static void BuildEffectArgs(int filterKind, int valueA, int valueB, int valueC, out string skslSource, out int[] values)
		{
			if (filterKind == FilterKindPinch)
			{
				skslSource = GpuFilterPreview.PinchSource;
				values = new int[] { valueA };
			}
			else if (filterKind == FilterKindPolar)
			{
				skslSource = GpuFilterPreview.PolarCoordinatesSource;
				values = new int[] { valueA };
			}
			else if (filterKind == FilterKindRipple)
			{
				skslSource = GpuFilterPreview.RippleSource;
				values = new int[] { valueA, valueB };
			}
			else if (filterKind == FilterKindShear)
			{
				skslSource = GpuFilterPreview.ShearSource;
				values = new int[] { valueA, valueB };
			}
			else if (filterKind == FilterKindSpherize)
			{
				skslSource = GpuFilterPreview.SpherizeSource;
				values = new int[] { valueA, valueB };
			}
			else if (filterKind == FilterKindTwirl)
			{
				skslSource = GpuFilterPreview.TwirlSource;
				values = new int[] { valueA };
			}
			else
			{
				skslSource = GpuFilterPreview.WaveSource;
				values = new int[] { valueA, valueB, valueC };
			}
		}

		private static void ApplyCpuReference(int filterKind, SKBitmap bitmap, int valueA, int valueB, int valueC)
		{
			if (filterKind == FilterKindPinch)
			{
				FilterDistort.Pinch(bitmap, valueA);
			}
			else if (filterKind == FilterKindPolar)
			{
				FilterDistort.PolarCoordinates(bitmap, valueA);
			}
			else if (filterKind == FilterKindRipple)
			{
				FilterDistort.Ripple(bitmap, valueA, valueB);
			}
			else if (filterKind == FilterKindShear)
			{
				FilterDistort.Shear(bitmap, valueA, valueB);
			}
			else if (filterKind == FilterKindSpherize)
			{
				FilterDistort.Spherize(bitmap, valueA, valueB);
			}
			else if (filterKind == FilterKindTwirl)
			{
				FilterDistort.Twirl(bitmap, valueA);
			}
			else
			{
				FilterDistort.Wave(bitmap, valueA, valueB, valueC);
			}
		}

		private static bool RunGpuEffect(SKBitmap input, int filterKind, int valueA, int valueB, int valueC, SKBitmap destination)
		{
			string skslSource;
			int[] values;
			BuildEffectArgs(filterKind, valueA, valueB, valueC, out skslSource, out values);
			SKImageInfo scratchInfo = new SKImageInfo(input.Width, input.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
			SKSurface scratchA = SKSurface.Create(scratchInfo);
			SKSurface scratchB = SKSurface.Create(scratchInfo);
			SKImage source = SKImage.FromPixels(input.PeekPixels());
			bool succeeded = GpuFilterPreview.RunEffect(scratchA, scratchB, source, skslSource, 1, false, false, null, values, destination);
			source.Dispose();
			scratchA.Dispose();
			scratchB.Dispose();
			return succeeded;
		}

		private static void RunRandomCase(int filterKind, int valueA, int valueB, int valueC, int tolerance, string name)
		{
			SKBitmap cpuBitmap = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap gpuInput = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap destination = new SKBitmap(TestWidth, TestHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			ApplyCpuReference(filterKind, cpuBitmap, valueA, valueB, valueC);
			bool succeeded = RunGpuEffect(gpuInput, filterKind, valueA, valueB, valueC, destination);
			Check(succeeded, name + " RunEffect returned true");
			if (succeeded)
			{
				CompareBitmapsPremul(cpuBitmap, destination, tolerance, name + " matches cpu reference");
			}
			cpuBitmap.Dispose();
			gpuInput.Dispose();
			destination.Dispose();
		}

		private static void RunIdentityCase(int filterKind, int valueA, int valueB, int valueC, string name)
		{
			SKBitmap expected = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap gpuInput = BuildAlphaVaryingBitmap(TestWidth, TestHeight, TestSeed);
			SKBitmap destination = new SKBitmap(TestWidth, TestHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bool succeeded = RunGpuEffect(gpuInput, filterKind, valueA, valueB, valueC, destination);
			Check(succeeded, name + " RunEffect returned true");
			if (succeeded)
			{
				CompareBitmapsPremul(expected, destination, 1, name + " is an identity");
			}
			expected.Dispose();
			gpuInput.Dispose();
			destination.Dispose();
		}

		public static int RunAll()
		{
			TestPinchAmountFifty();
			TestPinchAmountNegativeHundred();
			TestPinchAmountHundred();
			TestPolarRectangularToPolar();
			TestPolarPolarToRectangular();
			TestRippleAmountHundredSizeSmall();
			TestRippleAmountHundredSizeMedium();
			TestRippleAmountHundredSizeLarge();
			TestRippleAmountNegativeFourHundredSizeMedium();
			TestShearAmountTwentyFiveWrap();
			TestShearAmountTwentyFiveClamp();
			TestShearAmountNegativeEightyWrap();
			TestSpherizeAmountFiftyNormal();
			TestSpherizeAmountNegativeHundredNormal();
			TestSpherizeAmountSixtyHorizontalOnly();
			TestSpherizeAmountSixtyVerticalOnly();
			TestTwirlAngleFifty();
			TestTwirlAngleNineHundredNinetyNine();
			TestTwirlAngleNegativeThreeHundred();
			TestWaveSineWavelengthFortyAmplitudeTen();
			TestWaveTriangleWavelengthFortyAmplitudeTen();
			TestWaveSquareWavelengthFortyAmplitudeTen();
			TestWaveSineWavelengthTwelveAmplitudeTwentyFive();
			TestIdentityNoOps();
			return s_failures;
		}

		private static void TestPinchAmountFifty()
		{
			RunRandomCase(FilterKindPinch, 50, 0, 0, 3, "gpu pinch amount 50");
		}

		private static void TestPinchAmountNegativeHundred()
		{
			RunRandomCase(FilterKindPinch, -100, 0, 0, 3, "gpu pinch amount -100");
		}

		private static void TestPinchAmountHundred()
		{
			RunRandomCase(FilterKindPinch, 100, 0, 0, 3, "gpu pinch amount 100");
		}

		private static void TestPolarRectangularToPolar()
		{
			RunRandomCase(FilterKindPolar, 0, 0, 0, 3, "gpu polar coordinates rectangular to polar");
		}

		private static void TestPolarPolarToRectangular()
		{
			RunRandomCase(FilterKindPolar, 1, 0, 0, 3, "gpu polar coordinates polar to rectangular");
		}

		private static void TestRippleAmountHundredSizeSmall()
		{
			RunRandomCase(FilterKindRipple, 100, 0, 0, 3, "gpu ripple amount 100 size small");
		}

		private static void TestRippleAmountHundredSizeMedium()
		{
			RunRandomCase(FilterKindRipple, 100, 1, 0, 3, "gpu ripple amount 100 size medium");
		}

		private static void TestRippleAmountHundredSizeLarge()
		{
			RunRandomCase(FilterKindRipple, 100, 2, 0, 3, "gpu ripple amount 100 size large");
		}

		private static void TestRippleAmountNegativeFourHundredSizeMedium()
		{
			RunRandomCase(FilterKindRipple, -400, 1, 0, 3, "gpu ripple amount -400 size medium");
		}

		private static void TestShearAmountTwentyFiveWrap()
		{
			RunRandomCase(FilterKindShear, 25, 0, 0, 3, "gpu shear amount 25 wrap around");
		}

		private static void TestShearAmountTwentyFiveClamp()
		{
			RunRandomCase(FilterKindShear, 25, 1, 0, 3, "gpu shear amount 25 repeat edge pixels");
		}

		private static void TestShearAmountNegativeEightyWrap()
		{
			RunRandomCase(FilterKindShear, -80, 0, 0, 3, "gpu shear amount -80 wrap around");
		}

		private static void TestSpherizeAmountFiftyNormal()
		{
			RunRandomCase(FilterKindSpherize, 50, 0, 0, 3, "gpu spherize amount 50 normal");
		}

		private static void TestSpherizeAmountNegativeHundredNormal()
		{
			RunRandomCase(FilterKindSpherize, -100, 0, 0, 3, "gpu spherize amount -100 normal");
		}

		private static void TestSpherizeAmountSixtyHorizontalOnly()
		{
			RunRandomCase(FilterKindSpherize, 60, 1, 0, 3, "gpu spherize amount 60 horizontal only");
		}

		private static void TestSpherizeAmountSixtyVerticalOnly()
		{
			RunRandomCase(FilterKindSpherize, 60, 2, 0, 3, "gpu spherize amount 60 vertical only");
		}

		private static void TestTwirlAngleFifty()
		{
			RunRandomCase(FilterKindTwirl, 50, 0, 0, 3, "gpu twirl angle 50");
		}

		private static void TestTwirlAngleNineHundredNinetyNine()
		{
			RunRandomCase(FilterKindTwirl, 999, 0, 0, 3, "gpu twirl angle 999");
		}

		private static void TestTwirlAngleNegativeThreeHundred()
		{
			RunRandomCase(FilterKindTwirl, -300, 0, 0, 3, "gpu twirl angle -300");
		}

		private static void TestWaveSineWavelengthFortyAmplitudeTen()
		{
			RunRandomCase(FilterKindWave, 40, 10, 0, 3, "gpu wave wavelength 40 amplitude 10 sine");
		}

		private static void TestWaveTriangleWavelengthFortyAmplitudeTen()
		{
			RunRandomCase(FilterKindWave, 40, 10, 1, 3, "gpu wave wavelength 40 amplitude 10 triangle");
		}

		private static void TestWaveSquareWavelengthFortyAmplitudeTen()
		{
			RunRandomCase(FilterKindWave, 40, 10, 2, 3, "gpu wave wavelength 40 amplitude 10 square");
		}

		private static void TestWaveSineWavelengthTwelveAmplitudeTwentyFive()
		{
			RunRandomCase(FilterKindWave, 12, 25, 0, 3, "gpu wave wavelength 12 amplitude 25 sine");
		}

		private static void TestIdentityNoOps()
		{
			RunIdentityCase(FilterKindPinch, 0, 0, 0, "gpu pinch amount 0");
			RunIdentityCase(FilterKindRipple, 0, 1, 0, "gpu ripple amount 0 size medium");
			RunIdentityCase(FilterKindShear, 0, 0, 0, "gpu shear amount 0 wrap around");
			RunIdentityCase(FilterKindShear, 0, 1, 0, "gpu shear amount 0 repeat edge pixels");
			RunIdentityCase(FilterKindSpherize, 0, 0, 0, "gpu spherize amount 0 normal");
			RunIdentityCase(FilterKindTwirl, 0, 0, 0, "gpu twirl angle 0");
			RunIdentityCase(FilterKindWave, 40, 0, 0, "gpu wave wavelength 40 amplitude 0 sine");
		}
	}
}
