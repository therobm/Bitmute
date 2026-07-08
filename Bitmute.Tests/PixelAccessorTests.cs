using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class PixelAccessorTests
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
			TestEightBitRoundTrip();
			TestSixteenBitRoundTrip();
			TestThirtyTwoFloatRoundTrip();
			return s_failures;
		}

		private static void TestEightBitRoundTrip()
		{
			SKBitmap bitmap = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			byte[] values = new byte[] { 0, 1, 2, 63, 64, 127, 128, 191, 192, 254, 255 };
			bool exact = true;
			for (int index = 0; index < values.Length; index++)
			{
				byte original = values[index];
				float normalized = original / 255.0f;
				accessor.WriteNormalized(1, 1, normalized, normalized, normalized, normalized);
				for (int channel = 0; channel < 4; channel++)
				{
					if (RawByte(bitmap, 1, 1, channel) != original)
					{
						exact = false;
					}
				}
				float readRed;
				float readGreen;
				float readBlue;
				float readAlpha;
				accessor.ReadNormalized(1, 1, out readRed, out readGreen, out readBlue, out readAlpha);
				if (readRed != normalized || readGreen != normalized || readBlue != normalized || readAlpha != normalized)
				{
					exact = false;
				}
			}
			Check(exact, "8-bit write/read round-trips exact for all sampled bytes");
			bitmap.Dispose();
		}

		private static void TestSixteenBitRoundTrip()
		{
			SKBitmap bitmap = new SKBitmap(4, 4, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			ushort[] values = new ushort[] { 0x0000, 0x0100, 0x0101, 0x00FF, 0xFFFE, 0xFFFF };
			bool exact = true;
			for (int index = 0; index < values.Length; index++)
			{
				ushort original = values[index];
				float normalized = original / 65535.0f;
				accessor.WriteNormalized(1, 1, normalized, normalized, normalized, normalized);
				for (int channel = 0; channel < 4; channel++)
				{
					if (RawUshort(bitmap, 1, 1, channel) != original)
					{
						exact = false;
					}
				}
			}
			Check(exact, "16-bit write/read round-trips exact incl. sub-8-bit distinct values");
			bitmap.Dispose();
		}

		private static void TestThirtyTwoFloatRoundTrip()
		{
			SKBitmap bitmap = new SKBitmap(4, 4, SKColorType.RgbaF32, SKAlphaType.Unpremul);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			float[] values = new float[] { 0.0f, 0.25f, 0.5f, 1.0f, 4.0f };
			bool exact = true;
			for (int index = 0; index < values.Length; index++)
			{
				float original = values[index];
				accessor.WriteNormalized(1, 1, original, original, original, original);
				float readRed;
				float readGreen;
				float readBlue;
				float readAlpha;
				accessor.ReadNormalized(1, 1, out readRed, out readGreen, out readBlue, out readAlpha);
				if (readRed != original || readGreen != original || readBlue != original || readAlpha != original)
				{
					exact = false;
				}
			}
			Check(exact, "32-float write/read preserves values including HDR above 1.0");
			bitmap.Dispose();
		}
	}
}
