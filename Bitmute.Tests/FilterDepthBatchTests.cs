using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterDepthBatchTests
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

		private static bool Near(float actual, float expected, float tolerance)
		{
			float delta = actual - expected;
			if (delta < 0.0f)
			{
				delta = -delta;
			}
			if (delta <= tolerance)
			{
				return true;
			}
			return false;
		}

		private static unsafe byte RawByte(SKBitmap bitmap, int x, int y, int channel)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			return basePointer[((long)y * bitmap.RowBytes) + (x * 4) + channel];
		}

		private static int RampValue(int baseValue, int step, int position)
		{
			int value = baseValue + (step * position);
			if (value < 0)
			{
				value = 0;
			}
			if (value > 255)
			{
				value = 255;
			}
			return value;
		}

		private static SKBitmap BuildRampEight(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte red = (byte)RampValue(20, 3, x);
					byte green = (byte)RampValue(60, 2, x + y);
					byte blue = (byte)RampValue(180, -2, x);
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, 255));
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildRampSixteen(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float red = RampValue(20, 3, x) / 255.0f;
					float green = RampValue(60, 2, x + y) / 255.0f;
					float blue = RampValue(180, -2, x) / 255.0f;
					accessor.WriteNormalized(x, y, red, green, blue, 1.0f);
				}
			}
			return bitmap;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestDesaturateEightAndSixteenAgree();
			TestHueSaturationLightnessEightAndSixteenAgree();
			TestGaussianBlurEightAndSixteenAgree();
			TestBoxBlurEightAndSixteenAgree();
			TestBlurEightAndSixteenAgree();
			TestBlurMoreEightAndSixteenAgree();
			TestAverageEightAndSixteenAgree();
			TestSharpenEightAndSixteenAgree();
			TestUnsharpMaskEightAndSixteenAgree();
			return s_failures;
		}

		private static void CompareInterior(SKBitmap eight, SKBitmap sixteen, int[] sampleXs, int[] sampleYs, float tolerance, string name)
		{
			PixelAccessor sixteenAccessor = new PixelAccessor(sixteen.GetPixels(), sixteen.RowBytes, sixteen.ColorType);
			bool allAgree = true;
			for (int xi = 0; xi < sampleXs.Length; xi++)
			{
				for (int yi = 0; yi < sampleYs.Length; yi++)
				{
					int x = sampleXs[xi];
					int y = sampleYs[yi];
					float eightRed = RawByte(eight, x, y, 0) / 255.0f;
					float eightGreen = RawByte(eight, x, y, 1) / 255.0f;
					float eightBlue = RawByte(eight, x, y, 2) / 255.0f;
					float sixteenRed;
					float sixteenGreen;
					float sixteenBlue;
					float sixteenAlpha;
					sixteenAccessor.ReadNormalized(x, y, out sixteenRed, out sixteenGreen, out sixteenBlue, out sixteenAlpha);
					if (!Near(sixteenRed, eightRed, tolerance))
					{
						allAgree = false;
					}
					if (!Near(sixteenGreen, eightGreen, tolerance))
					{
						allAgree = false;
					}
					if (!Near(sixteenBlue, eightBlue, tolerance))
					{
						allAgree = false;
					}
				}
			}
			Check(allAgree, name);
		}

		private static void TestDesaturateEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			Adjustments.Desaturate(eight);
			Adjustments.Desaturate(sixteen);
			int[] sampleXs = new int[] { 8, 20, 32, 40 };
			int[] sampleYs = new int[] { 6, 12, 18 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.02f, "desaturate 8-bit and 16-bit agree within tolerance");
			PixelAccessor sixteenAccessor = new PixelAccessor(sixteen.GetPixels(), sixteen.RowBytes, sixteen.ColorType);
			bool allGray = true;
			for (int xi = 0; xi < sampleXs.Length; xi++)
			{
				for (int yi = 0; yi < sampleYs.Length; yi++)
				{
					float red;
					float green;
					float blue;
					float alpha;
					sixteenAccessor.ReadNormalized(sampleXs[xi], sampleYs[yi], out red, out green, out blue, out alpha);
					if (!Near(red, green, 0.0005f))
					{
						allGray = false;
					}
					if (!Near(green, blue, 0.0005f))
					{
						allGray = false;
					}
				}
			}
			Check(allGray, "desaturate 16-bit output has R == G == B");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestHueSaturationLightnessEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			Adjustments.HueSaturationLightness(eight, 40, 30, 15);
			Adjustments.HueSaturationLightness(sixteen, 40, 30, 15);
			int[] sampleXs = new int[] { 8, 20, 32, 40 };
			int[] sampleYs = new int[] { 6, 12, 18 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.02f, "hue/saturation/lightness 8-bit and 16-bit agree within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestGaussianBlurEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			Adjustments.GaussianBlur(eight, 4);
			Adjustments.GaussianBlur(sixteen, 4);
			int[] sampleXs = new int[] { 12, 20, 28, 36 };
			int[] sampleYs = new int[] { 8, 12, 16 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.03f, "gaussian blur 8-bit and 16-bit agree within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestBoxBlurEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			FilterBlur.BoxBlur(eight, 5);
			FilterBlur.BoxBlur(sixteen, 5);
			int[] sampleXs = new int[] { 12, 20, 28, 36 };
			int[] sampleYs = new int[] { 8, 12, 16 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.03f, "box blur 8-bit and 16-bit agree within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestBlurEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			FilterBlur.Blur(eight);
			FilterBlur.Blur(sixteen);
			int[] sampleXs = new int[] { 12, 20, 28, 36 };
			int[] sampleYs = new int[] { 8, 12, 16 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.03f, "blur 8-bit and 16-bit agree within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestBlurMoreEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			FilterBlur.BlurMore(eight);
			FilterBlur.BlurMore(sixteen);
			int[] sampleXs = new int[] { 12, 20, 28, 36 };
			int[] sampleYs = new int[] { 8, 12, 16 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.03f, "blur more 8-bit and 16-bit agree within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestAverageEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			FilterBlur.Average(eight);
			FilterBlur.Average(sixteen);
			int[] sampleXs = new int[] { 12, 20, 28, 36 };
			int[] sampleYs = new int[] { 8, 12, 16 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.03f, "average 8-bit and 16-bit agree within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestSharpenEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			FilterSharpen.Sharpen(eight);
			FilterSharpen.Sharpen(sixteen);
			int[] sampleXs = new int[] { 12, 20, 28, 36 };
			int[] sampleYs = new int[] { 8, 12, 16 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.03f, "sharpen 8-bit and 16-bit agree within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestUnsharpMaskEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(48, 24);
			SKBitmap sixteen = BuildRampSixteen(48, 24);
			Adjustments.UnsharpMask(eight, 120, 4);
			Adjustments.UnsharpMask(sixteen, 120, 4);
			int[] sampleXs = new int[] { 12, 20, 28, 36 };
			int[] sampleYs = new int[] { 8, 12, 16 };
			CompareInterior(eight, sixteen, sampleXs, sampleYs, 0.03f, "unsharp mask 8-bit and 16-bit agree within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}
	}
}
