using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class StrokeSnapshotDepthTests
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

		private static unsafe ushort RawUshort(SKBitmap bitmap, int x, int y, int channel)
		{
			ushort* basePointer = (ushort*)((byte*)bitmap.GetPixels().ToPointer() + ((long)y * bitmap.RowBytes));
			return basePointer[(x * 4) + channel];
		}

		private static unsafe void WriteRawUshort(SKBitmap bitmap, int x, int y, ushort red, ushort green, ushort blue, ushort alpha)
		{
			ushort* basePointer = (ushort*)((byte*)bitmap.GetPixels().ToPointer() + ((long)y * bitmap.RowBytes));
			basePointer[(x * 4) + 0] = red;
			basePointer[(x * 4) + 1] = green;
			basePointer[(x * 4) + 2] = blue;
			basePointer[(x * 4) + 3] = alpha;
		}

		private static unsafe byte RawByte(SKBitmap bitmap, int x, int y, int channel)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			return basePointer[((long)y * bitmap.RowBytes) + (x * 4) + channel];
		}

		private static unsafe void WriteRawByte(SKBitmap bitmap, int x, int y, byte red, byte green, byte blue, byte alpha)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer() + ((long)y * bitmap.RowBytes) + (x * 4);
			basePointer[0] = red;
			basePointer[1] = green;
			basePointer[2] = blue;
			basePointer[3] = alpha;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestSixteenBitSnapshotRestoreExact();
			TestEightBitSnapshotRestoreExact();
			TestSixteenBitDirtyRectSinglePixel();
			TestSixteenBitDirtyRectHighByteOnly();
			return s_failures;
		}

		private static void TestSixteenBitSnapshotRestoreExact()
		{
			Document document = new Document("t", 32, 24);
			Layer layer = document.ActiveLayer();
			int pixelX = 16;
			int pixelY = 12;
			ushort originalRed = 0x1234;
			ushort originalGreen = 0x5678;
			ushort originalBlue = 0x9ABC;
			ushort originalAlpha = 0xDEF0;
			document.ConvertColorDepth(eColorDepth.Sixteen);
			WriteRawUshort(document.ActiveLayer().Bitmap(), pixelX, pixelY, originalRed, originalGreen, originalBlue, originalAlpha);
			document.BeginStroke();
			WriteRawUshort(document.ActiveLayer().Bitmap(), pixelX, pixelY, 0x0101, 0x0202, 0x0303, 0x0404);
			document.RestoreStrokeSnapshot();
			SKBitmap restored = document.ActiveLayer().Bitmap();
			ushort red = RawUshort(restored, pixelX, pixelY, 0);
			ushort green = RawUshort(restored, pixelX, pixelY, 1);
			ushort blue = RawUshort(restored, pixelX, pixelY, 2);
			ushort alpha = RawUshort(restored, pixelX, pixelY, 3);
			bool exact = red == originalRed && green == originalGreen && blue == originalBlue && alpha == originalAlpha;
			Check(exact, "16-bit stroke snapshot restore puts original ushorts back exactly");
		}

		private static void TestEightBitSnapshotRestoreExact()
		{
			Document document = new Document("t", 32, 24);
			Layer layer = document.ActiveLayer();
			int pixelX = 16;
			int pixelY = 12;
			byte originalRed = 10;
			byte originalGreen = 60;
			byte originalBlue = 200;
			byte originalAlpha = 255;
			WriteRawByte(document.ActiveLayer().Bitmap(), pixelX, pixelY, originalRed, originalGreen, originalBlue, originalAlpha);
			document.BeginStroke();
			WriteRawByte(document.ActiveLayer().Bitmap(), pixelX, pixelY, 1, 2, 3, 4);
			document.RestoreStrokeSnapshot();
			SKBitmap restored = document.ActiveLayer().Bitmap();
			byte red = RawByte(restored, pixelX, pixelY, 0);
			byte green = RawByte(restored, pixelX, pixelY, 1);
			byte blue = RawByte(restored, pixelX, pixelY, 2);
			byte alpha = RawByte(restored, pixelX, pixelY, 3);
			bool exact = red == originalRed && green == originalGreen && blue == originalBlue && alpha == originalAlpha;
			Check(exact, "8-bit stroke snapshot restore puts original bytes back exactly");
		}

		private static SKBitmap BuildSixteenSolid(int width, int height, ushort red, ushort green, ushort blue, ushort alpha)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					WriteRawUshort(bitmap, x, y, red, green, blue, alpha);
				}
			}
			return bitmap;
		}

		private static void TestSixteenBitDirtyRectSinglePixel()
		{
			int width = 16;
			int height = 12;
			SKBitmap before = BuildSixteenSolid(width, height, 0x2000, 0x4000, 0x6000, 0xFFFF);
			SKBitmap after = BuildSixteenSolid(width, height, 0x2000, 0x4000, 0x6000, 0xFFFF);
			int changedX = 7;
			int changedY = 5;
			WriteRawUshort(after, changedX, changedY, 0x2001, 0x4000, 0x6000, 0xFFFF);
			SKRectI rect = PixelRegion.ComputeDirtyRect(before, after);
			bool tight = rect.Left == changedX && rect.Top == changedY && rect.Right == changedX + 1 && rect.Bottom == changedY + 1;
			Check(tight, "16-bit dirty rect tightly bounds a single changed interior pixel");
			before.Dispose();
			after.Dispose();
		}

		private static void TestSixteenBitDirtyRectHighByteOnly()
		{
			int width = 16;
			int height = 12;
			SKBitmap before = BuildSixteenSolid(width, height, 0x2000, 0x4000, 0x6000, 0xFFFF);
			SKBitmap after = BuildSixteenSolid(width, height, 0x2000, 0x4000, 0x6000, 0xFFFF);
			int changedX = 9;
			int changedY = 6;
			WriteRawUshort(after, changedX, changedY, 0x2100, 0x4000, 0x6000, 0xFFFF);
			SKRectI rect = PixelRegion.ComputeDirtyRect(before, after);
			bool tight = rect.Left == changedX && rect.Top == changedY && rect.Right == changedX + 1 && rect.Bottom == changedY + 1;
			Check(tight, "16-bit dirty rect detects a high-byte-only channel change");
			before.Dispose();
			after.Dispose();
		}
	}
}
