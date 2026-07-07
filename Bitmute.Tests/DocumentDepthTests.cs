using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class DocumentDepthTests
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
			TestEightToSixteenWidensLossless();
			TestEightToThirtyTwoNormalizes();
			TestSixteenToEightRoundTrip();
			return s_failures;
		}

		private static void TestEightToSixteenWidensLossless()
		{
			Document document = new Document("t", 2, 2);
			SKBitmap sourceBitmap = document.ActiveLayer().Bitmap();
			sourceBitmap.SetPixel(1, 1, new SKColor(10, 100, 200, 128));
			document.ConvertColorDepth(eColorDepth.Sixteen);
			SKBitmap converted = document.ActiveLayer().Bitmap();
			Check(converted.ColorType == SKColorType.Rgba16161616, "8 to 16 produces Rgba16161616");
			ushort red = RawUshort(converted, 1, 1, 0);
			ushort green = RawUshort(converted, 1, 1, 1);
			ushort blue = RawUshort(converted, 1, 1, 2);
			ushort alpha = RawUshort(converted, 1, 1, 3);
			bool exact = red == (10 * 257) && green == (100 * 257) && blue == (200 * 257) && alpha == (128 * 257);
			Check(exact, "8 to 16 widens losslessly by factor 257");
			document.ReleaseComposite();
		}

		private static void TestEightToThirtyTwoNormalizes()
		{
			Document document = new Document("t", 2, 2);
			SKBitmap sourceBitmap = document.ActiveLayer().Bitmap();
			sourceBitmap.SetPixel(1, 1, new SKColor(10, 100, 200, 128));
			document.ConvertColorDepth(eColorDepth.ThirtyTwoFloat);
			SKBitmap converted = document.ActiveLayer().Bitmap();
			Check(converted.ColorType == SKColorType.RgbaF32, "8 to 32 produces RgbaF32");
			PixelAccessor accessor = new PixelAccessor(converted.GetPixels(), converted.RowBytes, converted.ColorType);
			float red;
			float green;
			float blue;
			float alpha;
			accessor.ReadNormalized(1, 1, out red, out green, out blue, out alpha);
			bool redNear = Near(red, 10.0f / 255.0f, 0.0005f);
			bool greenNear = Near(green, 100.0f / 255.0f, 0.0005f);
			bool blueNear = Near(blue, 200.0f / 255.0f, 0.0005f);
			bool alphaNear = Near(alpha, 128.0f / 255.0f, 0.0005f);
			Check(redNear && greenNear && blueNear && alphaNear, "8 to 32 normalizes channels to 0..1");
			document.ReleaseComposite();
		}

		private static void TestSixteenToEightRoundTrip()
		{
			Document document = new Document("t", 2, 2);
			SKBitmap sourceBitmap = document.ActiveLayer().Bitmap();
			sourceBitmap.SetPixel(1, 1, new SKColor(10, 100, 200, 128));
			document.ConvertColorDepth(eColorDepth.Sixteen);
			document.ConvertColorDepth(eColorDepth.Eight);
			SKBitmap converted = document.ActiveLayer().Bitmap();
			Check(converted.ColorType == SKColorType.Rgba8888, "16 to 8 produces Rgba8888");
			byte red = RawByte(converted, 1, 1, 0);
			byte green = RawByte(converted, 1, 1, 1);
			byte blue = RawByte(converted, 1, 1, 2);
			byte alpha = RawByte(converted, 1, 1, 3);
			bool redNear = Near(red, 10.0f, 1.0f);
			bool greenNear = Near(green, 100.0f, 1.0f);
			bool blueNear = Near(blue, 200.0f, 1.0f);
			bool alphaNear = Near(alpha, 128.0f, 1.0f);
			Check(redNear && greenNear && blueNear && alphaNear, "16 to 8 round-trips within 1 of originals");
			document.ReleaseComposite();
		}
	}
}
