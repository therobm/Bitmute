using System;
using SkiaSharp;
using Bitmute.Storage;

namespace Bitmute.Tests
{
	public static class PngFileTests
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

		private static unsafe void WriteUshort(SKBitmap bitmap, int x, int y, ushort red, ushort green, ushort blue, ushort alpha)
		{
			ushort* basePointer = (ushort*)((byte*)bitmap.GetPixels().ToPointer() + ((long)y * bitmap.RowBytes));
			int index = x * 4;
			basePointer[index] = red;
			basePointer[index + 1] = green;
			basePointer[index + 2] = blue;
			basePointer[index + 3] = alpha;
		}

		private static unsafe void WriteByte(SKBitmap bitmap, int x, int y, byte red, byte green, byte blue, byte alpha)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer() + ((long)y * bitmap.RowBytes);
			int index = x * 4;
			basePointer[index] = red;
			basePointer[index + 1] = green;
			basePointer[index + 2] = blue;
			basePointer[index + 3] = alpha;
		}

		private static byte[] EncodePng(SKBitmap bitmap)
		{
			SKImage image = SKImage.FromBitmap(bitmap);
			SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
			byte[] bytes = data.ToArray();
			data.Dispose();
			image.Dispose();
			return bytes;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestSixteenBitRgbaRoundTrips();
			TestEightBitRgbaRoundTrips();
			TestNullOnUnsupported();
			return s_failures;
		}

		private static void TestSixteenBitRgbaRoundTrips()
		{
			SKBitmap source = new SKBitmap(3, 2, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			source.Erase(new SKColor(0, 0, 0, 255));
			WriteUshort(source, 0, 0, 0x0101, 0x0100, 0x8000, 0xFFFF);
			WriteUshort(source, 2, 1, 0x1234, 0x00FF, 0xABCD, 0x0001);
			byte[] encoded = EncodePng(source);
			SKBitmap decoded = PngFile.Decode(encoded);
			if (decoded == null)
			{
				Check(false, "png 16-bit decode returned null");
				source.Dispose();
				return;
			}
			Check(decoded.ColorType == SKColorType.Rgba16161616, "png 16-bit color type is Rgba16161616");
			Check(decoded.Width == 3 && decoded.Height == 2, "png 16-bit dimensions match");
			bool pixelA = RawUshort(decoded, 0, 0, 0) == 0x0101 && RawUshort(decoded, 0, 0, 1) == 0x0100 && RawUshort(decoded, 0, 0, 2) == 0x8000 && RawUshort(decoded, 0, 0, 3) == 0xFFFF;
			Check(pixelA, "png 16-bit pixel 0,0 ushorts round-trip losslessly");
			bool pixelB = RawUshort(decoded, 2, 1, 0) == 0x1234 && RawUshort(decoded, 2, 1, 1) == 0x00FF && RawUshort(decoded, 2, 1, 2) == 0xABCD && RawUshort(decoded, 2, 1, 3) == 0x0001;
			Check(pixelB, "png 16-bit pixel 2,1 ushorts round-trip losslessly");
			bool subEight = RawUshort(decoded, 0, 0, 0) != RawUshort(decoded, 0, 0, 1);
			Check(subEight, "png 16-bit sub-8-bit distinction survives");
			decoded.Dispose();
			source.Dispose();
		}

		private static void TestEightBitRgbaRoundTrips()
		{
			SKBitmap source = new SKBitmap(3, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			source.Erase(new SKColor(0, 0, 0, 255));
			WriteByte(source, 0, 0, 10, 100, 200, 128);
			WriteByte(source, 2, 1, 255, 1, 64, 255);
			byte[] encoded = EncodePng(source);
			SKBitmap decoded = PngFile.Decode(encoded);
			if (decoded == null)
			{
				Check(false, "png 8-bit decode returned null");
				source.Dispose();
				return;
			}
			Check(decoded.ColorType == SKColorType.Rgba8888, "png 8-bit color type is Rgba8888");
			Check(decoded.Width == 3 && decoded.Height == 2, "png 8-bit dimensions match");
			bool pixelA = RawByte(decoded, 0, 0, 0) == 10 && RawByte(decoded, 0, 0, 1) == 100 && RawByte(decoded, 0, 0, 2) == 200 && RawByte(decoded, 0, 0, 3) == 128;
			Check(pixelA, "png 8-bit pixel 0,0 bytes round-trip intact");
			bool pixelB = RawByte(decoded, 2, 1, 0) == 255 && RawByte(decoded, 2, 1, 1) == 1 && RawByte(decoded, 2, 1, 2) == 64 && RawByte(decoded, 2, 1, 3) == 255;
			Check(pixelB, "png 8-bit pixel 2,1 bytes round-trip intact");
			decoded.Dispose();
			source.Dispose();
		}

		private static void TestNullOnUnsupported()
		{
			byte[] notPng = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
			SKBitmap decoded = PngFile.Decode(notPng);
			Check(decoded == null, "png decode returns null for non-PNG data");
		}
	}
}
