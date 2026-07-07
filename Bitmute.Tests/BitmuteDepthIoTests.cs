using System;
using System.IO;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Storage;

namespace Bitmute.Tests
{
	public static class BitmuteDepthIoTests
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
			TestSixteenBitRoundTrips();
			TestEightBitRoundTrips();
			return s_failures;
		}

		private static void TestSixteenBitRoundTrips()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_depth_io");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "depth16.bitmute");
			Document doc = new Document("depth16", 4, 3);
			Layer source = doc.ActiveLayer();
			source.Bitmap().SetPixel(2, 1, new SKColor(10, 100, 200, 128));
			doc.ConvertColorDepth(eColorDepth.Sixteen);
			SKBitmap beforeBitmap = doc.ActiveLayer().Bitmap();
			ushort redBefore = RawUshort(beforeBitmap, 2, 1, 0);
			ushort greenBefore = RawUshort(beforeBitmap, 2, 1, 1);
			ushort blueBefore = RawUshort(beforeBitmap, 2, 1, 2);
			ushort alphaBefore = RawUshort(beforeBitmap, 2, 1, 3);
			bool wrote = BitmuteFile.Write(path, doc);
			Check(wrote, "depth16 write");
			Document back = BitmuteFile.Read(path);
			if (back == null)
			{
				Check(false, "depth16 read returned null");
				File.Delete(path);
				return;
			}
			Check(back.ColorDepth() == eColorDepth.Sixteen, "depth16 document color depth is Sixteen");
			SKBitmap backBitmap = back.ActiveLayer().Bitmap();
			Check(backBitmap.ColorType == SKColorType.Rgba16161616, "depth16 active layer bitmap is Rgba16161616");
			ushort redAfter = RawUshort(backBitmap, 2, 1, 0);
			ushort greenAfter = RawUshort(backBitmap, 2, 1, 1);
			ushort blueAfter = RawUshort(backBitmap, 2, 1, 2);
			ushort alphaAfter = RawUshort(backBitmap, 2, 1, 3);
			bool exact = redAfter == redBefore && greenAfter == greenBefore && blueAfter == blueBefore && alphaAfter == alphaBefore;
			Check(exact, "depth16 pixel ushorts round-trip losslessly");
			File.Delete(path);
		}

		private static void TestEightBitRoundTrips()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_depth_io");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "depth8.bitmute");
			Document doc = new Document("depth8", 4, 3);
			Layer source = doc.ActiveLayer();
			source.Bitmap().SetPixel(2, 1, new SKColor(10, 100, 200, 128));
			bool wrote = BitmuteFile.Write(path, doc);
			Check(wrote, "depth8 write");
			Document back = BitmuteFile.Read(path);
			if (back == null)
			{
				Check(false, "depth8 read returned null");
				File.Delete(path);
				return;
			}
			Check(back.ColorDepth() == eColorDepth.Eight, "depth8 document color depth is Eight");
			SKBitmap backBitmap = back.ActiveLayer().Bitmap();
			Check(backBitmap.ColorType == SKColorType.Rgba8888, "depth8 active layer bitmap is Rgba8888");
			byte red = RawByte(backBitmap, 2, 1, 0);
			byte green = RawByte(backBitmap, 2, 1, 1);
			byte blue = RawByte(backBitmap, 2, 1, 2);
			byte alpha = RawByte(backBitmap, 2, 1, 3);
			bool exact = red == 10 && green == 100 && blue == 200 && alpha == 128;
			Check(exact, "depth8 pixel bytes round-trip intact");
			File.Delete(path);
		}
	}
}
