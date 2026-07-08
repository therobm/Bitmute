using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class RegionDepthTests
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
			TestSixteenBitExtractRegionExact();
			TestSixteenBitApplyRegionExact();
			TestEightBitExtractApplyRoundTrip();
			return s_failures;
		}

		private static ushort PatternRed(int x, int y)
		{
			return (ushort)(0x0100 + ((x + y) & 1) * 0x0100);
		}

		private static ushort PatternGreen(int x, int y)
		{
			return (ushort)(0x1000 + (x * 0x0011));
		}

		private static ushort PatternBlue(int x, int y)
		{
			return (ushort)(0x2000 + (y * 0x0101));
		}

		private static ushort PatternAlpha(int x, int y)
		{
			return (ushort)(0xF000 + (x * 0x0010) + y);
		}

		private static SKBitmap BuildSixteenPattern(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					WriteRawUshort(bitmap, x, y, PatternRed(x, y), PatternGreen(x, y), PatternBlue(x, y), PatternAlpha(x, y));
				}
			}
			return bitmap;
		}

		private static void TestSixteenBitExtractRegionExact()
		{
			int width = 32;
			int height = 24;
			SKBitmap source = BuildSixteenPattern(width, height);
			SKRectI rect = new SKRectI(6, 3, 27, 19);
			SKBitmap region = PixelRegion.ExtractRegion(source, rect);
			Check(region.ColorType == SKColorType.Rgba16161616, "16-bit extract region allocates an Rgba16161616 bitmap");
			bool extractMatches = true;
			for (int y = rect.Top; y < rect.Bottom; y++)
			{
				for (int x = rect.Left; x < rect.Right; x++)
				{
					int regionX = x - rect.Left;
					int regionY = y - rect.Top;
					ushort red = RawUshort(region, regionX, regionY, 0);
					ushort green = RawUshort(region, regionX, regionY, 1);
					ushort blue = RawUshort(region, regionX, regionY, 2);
					ushort alpha = RawUshort(region, regionX, regionY, 3);
					bool pixelMatches = red == RawUshort(source, x, y, 0) && green == RawUshort(source, x, y, 1) && blue == RawUshort(source, x, y, 2) && alpha == RawUshort(source, x, y, 3);
					if (!pixelMatches)
					{
						extractMatches = false;
					}
				}
			}
			Check(extractMatches, "16-bit extract region copies exact source ushorts including high bytes");
			region.Dispose();
			source.Dispose();
		}

		private static void TestSixteenBitApplyRegionExact()
		{
			int width = 32;
			int height = 24;
			SKBitmap source = BuildSixteenPattern(width, height);
			SKRectI rect = new SKRectI(6, 3, 27, 19);
			SKBitmap region = PixelRegion.ExtractRegion(source, rect);
			int offsetX = 4;
			int offsetY = 2;
			SKBitmap target = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					WriteRawUshort(target, x, y, 0x0000, 0x0000, 0x0000, 0x0000);
				}
			}
			PixelRegion.ApplyRegion(target, region, offsetX, offsetY);
			bool applyMatches = true;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bool inside = x >= offsetX && x < offsetX + region.Width && y >= offsetY && y < offsetY + region.Height;
					ushort expectedRed;
					ushort expectedGreen;
					ushort expectedBlue;
					ushort expectedAlpha;
					if (inside)
					{
						int regionX = x - offsetX;
						int regionY = y - offsetY;
						expectedRed = RawUshort(region, regionX, regionY, 0);
						expectedGreen = RawUshort(region, regionX, regionY, 1);
						expectedBlue = RawUshort(region, regionX, regionY, 2);
						expectedAlpha = RawUshort(region, regionX, regionY, 3);
					}
					else
					{
						expectedRed = 0x0000;
						expectedGreen = 0x0000;
						expectedBlue = 0x0000;
						expectedAlpha = 0x0000;
					}
					bool pixelMatches = RawUshort(target, x, y, 0) == expectedRed && RawUshort(target, x, y, 1) == expectedGreen && RawUshort(target, x, y, 2) == expectedBlue && RawUshort(target, x, y, 3) == expectedAlpha;
					if (!pixelMatches)
					{
						applyMatches = false;
					}
				}
			}
			Check(applyMatches, "16-bit apply region writes exact ushorts and leaves surroundings cleared");
			target.Dispose();
			region.Dispose();
			source.Dispose();
		}

		private static void TestEightBitExtractApplyRoundTrip()
		{
			int width = 32;
			int height = 24;
			SKBitmap source = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					WriteRawByte(source, x, y, (byte)(x * 7), (byte)(y * 9), (byte)((x + y) * 3), 255);
				}
			}
			SKRectI rect = new SKRectI(5, 4, 25, 18);
			SKBitmap region = PixelRegion.ExtractRegion(source, rect);
			Check(region.ColorType == SKColorType.Rgba8888, "8-bit extract region allocates an Rgba8888 bitmap");
			int offsetX = 3;
			int offsetY = 6;
			SKBitmap target = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			target.Erase(new SKColor(1, 1, 1, 1));
			PixelRegion.ApplyRegion(target, region, offsetX, offsetY);
			bool roundTrips = true;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bool inside = x >= offsetX && x < offsetX + region.Width && y >= offsetY && y < offsetY + region.Height;
					byte expectedRed;
					byte expectedGreen;
					byte expectedBlue;
					byte expectedAlpha;
					if (inside)
					{
						int sourceX = (x - offsetX) + rect.Left;
						int sourceY = (y - offsetY) + rect.Top;
						expectedRed = RawByte(source, sourceX, sourceY, 0);
						expectedGreen = RawByte(source, sourceX, sourceY, 1);
						expectedBlue = RawByte(source, sourceX, sourceY, 2);
						expectedAlpha = RawByte(source, sourceX, sourceY, 3);
					}
					else
					{
						expectedRed = 1;
						expectedGreen = 1;
						expectedBlue = 1;
						expectedAlpha = 1;
					}
					bool pixelMatches = RawByte(target, x, y, 0) == expectedRed && RawByte(target, x, y, 1) == expectedGreen && RawByte(target, x, y, 2) == expectedBlue && RawByte(target, x, y, 3) == expectedAlpha;
					if (!pixelMatches)
					{
						roundTrips = false;
					}
				}
			}
			Check(roundTrips, "8-bit extract then apply round-trips exact bytes and leaves surroundings untouched");
			target.Dispose();
			region.Dispose();
			source.Dispose();
		}
	}
}
