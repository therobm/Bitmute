using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class AdjustmentsCommonDepthTests
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

		private static bool Near(float a, float b, float epsilon)
		{
			float delta = a - b;
			if (delta < 0.0f)
			{
				delta = -delta;
			}
			if (delta <= epsilon)
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

		private static unsafe ushort RawUshort(SKBitmap bitmap, int x, int y, int channel)
		{
			ushort* basePointer = (ushort*)((byte*)bitmap.GetPixels().ToPointer() + ((long)y * bitmap.RowBytes));
			return basePointer[(x * 4) + channel];
		}

		private static int RampValue(int baseValue, int step, int x)
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
			return value;
		}

		private static SKBitmap BuildRampEight(int width, int height, int baseValue, int step)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int value = RampValue(baseValue, step, x);
					byte red = (byte)value;
					byte green = (byte)RampValue(baseValue, step, x + 3);
					byte blue = (byte)RampValue(baseValue, step, x + 7);
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, 255));
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildRampSixteen(int width, int height, int baseValue, int step)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float red = RampValue(baseValue, step, x) / 255.0f;
					float green = RampValue(baseValue, step, x + 3) / 255.0f;
					float blue = RampValue(baseValue, step, x + 7) / 255.0f;
					accessor.WriteNormalized(x, y, red, green, blue, 1.0f);
				}
			}
			return bitmap;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestBrightnessContrastEightAndSixteenAgree();
			TestPosterizeEightAndSixteenAgree();
			TestThresholdEightAndSixteenAgree();
			TestOffsetEightAndSixteenAgree();
			TestOffsetSixteenKnownPixel();
			return s_failures;
		}

		private static void CompareEightAndSixteen(SKBitmap eight, SKBitmap sixteen, int[] sampleXs, int sampleY, string name)
		{
			PixelAccessor sixteenAccessor = new PixelAccessor(sixteen.GetPixels(), sixteen.RowBytes, sixteen.ColorType);
			bool allAgree = true;
			for (int index = 0; index < sampleXs.Length; index++)
			{
				int x = sampleXs[index];
				float eightRed = RawByte(eight, x, sampleY, 0) / 255.0f;
				float eightGreen = RawByte(eight, x, sampleY, 1) / 255.0f;
				float eightBlue = RawByte(eight, x, sampleY, 2) / 255.0f;
				float sixteenRed;
				float sixteenGreen;
				float sixteenBlue;
				float sixteenAlpha;
				sixteenAccessor.ReadNormalized(x, sampleY, out sixteenRed, out sixteenGreen, out sixteenBlue, out sixteenAlpha);
				if (!Near(eightRed, sixteenRed, 0.01f))
				{
					allAgree = false;
				}
				if (!Near(eightGreen, sixteenGreen, 0.01f))
				{
					allAgree = false;
				}
				if (!Near(eightBlue, sixteenBlue, 0.01f))
				{
					allAgree = false;
				}
			}
			Check(allAgree, name);
		}

		private static void TestBrightnessContrastEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(64, 16, 20, 3);
			SKBitmap sixteen = BuildRampSixteen(64, 16, 20, 3);
			Adjustments.BrightnessContrast(eight, 30, 45);
			Adjustments.BrightnessContrast(sixteen, 30, 45);
			int[] sampleXs = new int[] { 8, 20, 32, 44, 56 };
			CompareEightAndSixteen(eight, sixteen, sampleXs, 8, "brightness/contrast 8-bit and 16-bit agree on the same ramp within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestPosterizeEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(64, 16, 10, 3);
			SKBitmap sixteen = BuildRampSixteen(64, 16, 10, 3);
			Adjustments.Posterize(eight, 4);
			Adjustments.Posterize(sixteen, 4);
			int[] sampleXs = new int[] { 8, 20, 32, 44, 56 };
			CompareEightAndSixteen(eight, sixteen, sampleXs, 8, "posterize 8-bit and 16-bit agree on the same ramp within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestThresholdEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(64, 16, 0, 4);
			SKBitmap sixteen = BuildRampSixteen(64, 16, 0, 4);
			Adjustments.Threshold(eight, 128);
			Adjustments.Threshold(sixteen, 128);
			int[] sampleXs = new int[] { 8, 20, 32, 44, 56 };
			CompareEightAndSixteen(eight, sixteen, sampleXs, 8, "threshold 8-bit and 16-bit agree on the same ramp within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}

		private static void TestOffsetEightAndSixteenAgree()
		{
			int width = 32;
			int height = 16;
			SKBitmap sixteen = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			PixelAccessor writeAccessor = new PixelAccessor(sixteen.GetPixels(), sixteen.RowBytes, sixteen.ColorType);
			ushort[] sourceReds = new ushort[width * height];
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float red = RampValue(4, 7, x) / 255.0f;
					float green = RampValue(8, 5, y) / 255.0f;
					float blue = RampValue(2, 9, x + y) / 255.0f;
					writeAccessor.WriteNormalized(x, y, red, green, blue, 1.0f);
					sourceReds[(y * width) + x] = RawUshort(sixteen, x, y, 0);
				}
			}
			int offsetX = 5;
			int offsetY = 3;
			FilterOther.Offset(sixteen, offsetX, offsetY, eOffsetEdge.Wrap);
			bool allExact = true;
			int[] sampleXs = new int[] { 6, 12, 20, 28 };
			int[] sampleYs = new int[] { 4, 8, 12 };
			for (int xi = 0; xi < sampleXs.Length; xi++)
			{
				for (int yi = 0; yi < sampleYs.Length; yi++)
				{
					int x = sampleXs[xi];
					int y = sampleYs[yi];
					int srcX = ((((x - offsetX) % width) + width) % width);
					int srcY = ((((y - offsetY) % height) + height) % height);
					ushort actualRed = RawUshort(sixteen, x, y, 0);
					ushort expectedRed = sourceReds[(srcY * width) + srcX];
					if (actualRed != expectedRed)
					{
						allExact = false;
					}
				}
			}
			Check(allExact, "offset 16-bit wrap copies source ushorts exactly to the shifted location");
			sixteen.Dispose();
		}

		private static void TestOffsetSixteenKnownPixel()
		{
			int width = 8;
			int height = 8;
			SKBitmap sixteen = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			PixelAccessor writeAccessor = new PixelAccessor(sixteen.GetPixels(), sixteen.RowBytes, sixteen.ColorType);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					writeAccessor.WriteNormalized(x, y, 0.0f, 0.0f, 0.0f, 1.0f);
				}
			}
			ushort markRed = 0x1234;
			ushort markGreen = 0x5678;
			ushort markBlue = 0x9ABC;
			ushort markAlpha = 0xDEF0;
			int sourceX = 2;
			int sourceY = 3;
			WriteRawUshort(sixteen, sourceX, sourceY, markRed, markGreen, markBlue, markAlpha);
			int offsetX = 3;
			int offsetY = 2;
			FilterOther.Offset(sixteen, offsetX, offsetY, eOffsetEdge.Wrap);
			int destX = sourceX + offsetX;
			int destY = sourceY + offsetY;
			ushort red = RawUshort(sixteen, destX, destY, 0);
			ushort green = RawUshort(sixteen, destX, destY, 1);
			ushort blue = RawUshort(sixteen, destX, destY, 2);
			ushort alpha = RawUshort(sixteen, destX, destY, 3);
			bool intact = red == markRed && green == markGreen && blue == markBlue && alpha == markAlpha;
			Check(intact, "offset 16-bit wrap lands the known pixel at the shifted location with ushorts intact");
			sixteen.Dispose();
		}

		private static unsafe void WriteRawUshort(SKBitmap bitmap, int x, int y, ushort red, ushort green, ushort blue, ushort alpha)
		{
			ushort* basePointer = (ushort*)((byte*)bitmap.GetPixels().ToPointer() + ((long)y * bitmap.RowBytes));
			basePointer[(x * 4) + 0] = red;
			basePointer[(x * 4) + 1] = green;
			basePointer[(x * 4) + 2] = blue;
			basePointer[(x * 4) + 3] = alpha;
		}
	}
}
