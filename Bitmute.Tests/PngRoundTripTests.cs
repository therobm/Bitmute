using System;
using System.IO;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Storage;

namespace Bitmute.Tests
{
	public static class PngRoundTripTests
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

		private static bool NearUshort(ushort actual, int expected, int tolerance)
		{
			int delta = actual - expected;
			if (delta < 0)
			{
				delta = -delta;
			}
			return delta <= tolerance;
		}

		private static bool NearByte(byte actual, int expected, int tolerance)
		{
			int delta = actual - expected;
			if (delta < 0)
			{
				delta = -delta;
			}
			return delta <= tolerance;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestSixteenBitPngRoundTrips();
			TestEightBitPngRoundTrips();
			return s_failures;
		}

		private static void TestSixteenBitPngRoundTrips()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_png_roundtrip");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "roundtrip16.png");
			Document document = new Document("t", 4, 3);
			document.ActiveLayer().Bitmap().SetPixel(2, 1, new SKColor(10, 100, 200, 255));
			document.ConvertColorDepth(eColorDepth.Sixteen);
			bool exported = ImageFile.Export(document, path, "png", 100, false, true);
			Check(exported, "png 16-bit export succeeds");
			SKBitmap decoded = ImageFile.DecodeFile(path);
			if (decoded == null)
			{
				Check(false, "png 16-bit decode returned null");
				File.Delete(path);
				return;
			}
			Check(decoded.ColorType == SKColorType.Rgba16161616, "png 16-bit stays Rgba16161616 through export and import");
			Document opened = Document.OpenImage("t", decoded);
			Check(opened.ColorDepth() == eColorDepth.Sixteen, "png 16-bit opened document color depth is Sixteen");
			SKBitmap layerBitmap = opened.ActiveLayer().Bitmap();
			ushort red = RawUshort(layerBitmap, 2, 1, 0);
			ushort green = RawUshort(layerBitmap, 2, 1, 1);
			ushort blue = RawUshort(layerBitmap, 2, 1, 2);
			ushort alpha = RawUshort(layerBitmap, 2, 1, 3);
			bool nearPixel = NearUshort(red, 10 * 257, 2) && NearUshort(green, 100 * 257, 2) && NearUshort(blue, 200 * 257, 2) && NearUshort(alpha, 65535, 2);
			Check(nearPixel, "png 16-bit opened layer pixel ushorts near expected");
			decoded.Dispose();
			File.Delete(path);
		}

		private static void TestEightBitPngRoundTrips()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_png_roundtrip");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "roundtrip8.png");
			Document document = new Document("t", 4, 3);
			document.ActiveLayer().Bitmap().SetPixel(2, 1, new SKColor(10, 100, 200, 255));
			bool exported = ImageFile.Export(document, path, "png", 100, false, true);
			Check(exported, "png 8-bit export succeeds");
			SKBitmap decoded = ImageFile.DecodeFile(path);
			if (decoded == null)
			{
				Check(false, "png 8-bit decode returned null");
				File.Delete(path);
				return;
			}
			Check(decoded.ColorType == SKColorType.Rgba8888, "png 8-bit stays Rgba8888 through export and import");
			Document opened = Document.OpenImage("t", decoded);
			Check(opened.ColorDepth() == eColorDepth.Eight, "png 8-bit opened document color depth is Eight");
			SKBitmap layerBitmap = opened.ActiveLayer().Bitmap();
			byte red = RawByte(layerBitmap, 2, 1, 0);
			byte green = RawByte(layerBitmap, 2, 1, 1);
			byte blue = RawByte(layerBitmap, 2, 1, 2);
			byte alpha = RawByte(layerBitmap, 2, 1, 3);
			bool nearPixel = NearByte(red, 10, 1) && NearByte(green, 100, 1) && NearByte(blue, 200, 1) && NearByte(alpha, 255, 1);
			Check(nearPixel, "png 8-bit opened layer pixel bytes near expected");
			decoded.Dispose();
			File.Delete(path);
		}
	}
}
