using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class AdjustmentsDepthTests
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

		public static int RunAll()
		{
			s_failures = 0;
			TestEightBitInvertExact();
			TestSixteenBitInvertExact();
			TestThirtyTwoFloatInvertPrecision();
			return s_failures;
		}

		private static void TestEightBitInvertExact()
		{
			SKBitmap bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.SetPixel(1, 1, new SKColor(10, 100, 200, 128));
			Adjustments.InvertColors(bitmap);
			byte red = RawByte(bitmap, 1, 1, 0);
			byte green = RawByte(bitmap, 1, 1, 1);
			byte blue = RawByte(bitmap, 1, 1, 2);
			byte alpha = RawByte(bitmap, 1, 1, 3);
			bool exact = red == 245 && green == 155 && blue == 55 && alpha == 128;
			Check(exact, "8-bit invert is exact and leaves alpha unchanged");
			bitmap.Dispose();
		}

		private static void TestSixteenBitInvertExact()
		{
			SKBitmap bitmap = new SKBitmap(2, 2, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			ushort originalRed = 0x2000;
			ushort originalGreen = 0x4000;
			ushort originalBlue = 0x8000;
			ushort originalAlpha = 0x6000;
			accessor.WriteNormalized(1, 1, originalRed / 65535.0f, originalGreen / 65535.0f, originalBlue / 65535.0f, originalAlpha / 65535.0f);
			Adjustments.InvertColors(bitmap);
			ushort red = RawUshort(bitmap, 1, 1, 0);
			ushort green = RawUshort(bitmap, 1, 1, 1);
			ushort blue = RawUshort(bitmap, 1, 1, 2);
			ushort alpha = RawUshort(bitmap, 1, 1, 3);
			bool exact = red == (0xFFFF - 0x2000) && green == (0xFFFF - 0x4000) && blue == (0xFFFF - 0x8000) && alpha == 0x6000;
			Check(exact, "16-bit invert is exact and leaves alpha unchanged");
			bitmap.Dispose();
		}

		private static void TestThirtyTwoFloatInvertPrecision()
		{
			SKBitmap bitmap = new SKBitmap(2, 2, SKColorType.RgbaF32, SKAlphaType.Unpremul);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			accessor.WriteNormalized(0, 0, 0.2f, 0.4f, 0.6f, 0.5f);
			Adjustments.InvertColors(bitmap);
			float red;
			float green;
			float blue;
			float alpha;
			accessor.ReadNormalized(0, 0, out red, out green, out blue, out alpha);
			bool redNear = Near(red, 0.8f, 0.0005f);
			bool greenNear = Near(green, 0.6f, 0.0005f);
			bool blueNear = Near(blue, 0.4f, 0.0005f);
			bool alphaExact = alpha == 0.5f;
			Check(redNear && greenNear && blueNear && alphaExact, "32-float invert preserves precision and leaves alpha unchanged");
			bitmap.Dispose();
		}
	}
}
