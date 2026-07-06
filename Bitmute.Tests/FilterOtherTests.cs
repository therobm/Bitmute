using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterOtherTests
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

		private static unsafe byte PixelByte(SKBitmap bitmap, int x, int y, int channel)
		{
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			return basePointer[((long)y * bitmap.RowBytes) + (x * 4) + channel];
		}

		private static SKBitmap BuildPatternBitmap(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bitmap.SetPixel(x, y, new SKColor((byte)x, (byte)y, 128, 255));
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildHalvesBitmap(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (x < width / 2)
					{
						bitmap.SetPixel(x, y, new SKColor(255, 0, 0, 255));
					}
					else
					{
						bitmap.SetPixel(x, y, new SKColor(0, 0, 255, 255));
					}
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildSolidBitmap(int width, int height, byte red, byte green, byte blue)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, 255));
				}
			}
			return bitmap;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestIdentity();
			TestWrapSeam();
			TestWrapNegative();
			TestRepeatEdge();
			TestTransparent();
			return s_failures;
		}

		private static void TestIdentity()
		{
			SKBitmap bitmap = BuildPatternBitmap(40, 24);
			SKBitmap reference = BuildPatternBitmap(40, 24);
			FilterOther.Offset(bitmap, 0, 0, eOffsetEdge.Wrap);
			bool identical = true;
			for (int y = 0; y < 24; y++)
			{
				for (int x = 0; x < 40; x++)
				{
					for (int channel = 0; channel < 4; channel++)
					{
						if (PixelByte(bitmap, x, y, channel) != PixelByte(reference, x, y, channel))
						{
							identical = false;
						}
					}
				}
			}
			Check(identical, "offset 0,0 wrap is exact identity");
			bitmap.Dispose();
			reference.Dispose();
		}

		private static void TestWrapSeam()
		{
			SKBitmap bitmap = BuildHalvesBitmap(64, 32);
			FilterOther.Offset(bitmap, 32, 0, eOffsetEdge.Wrap);
			byte leftRed = PixelByte(bitmap, 0, 16, 0);
			byte leftBlue = PixelByte(bitmap, 0, 16, 2);
			byte rightRed = PixelByte(bitmap, 63, 16, 0);
			byte rightBlue = PixelByte(bitmap, 63, 16, 2);
			Check(leftBlue == 255 && leftRed == 0, "wrap shift right by 32 makes left edge blue");
			Check(rightRed == 255 && rightBlue == 0, "wrap shift right by 32 makes right edge red");
			bitmap.Dispose();
		}

		private static void TestWrapNegative()
		{
			SKBitmap bitmap = BuildPatternBitmap(32, 16);
			SKBitmap reference = BuildPatternBitmap(32, 16);
			FilterOther.Offset(bitmap, -8, 0, eOffsetEdge.Wrap);
			bool matched = true;
			for (int y = 0; y < 16; y++)
			{
				for (int channel = 0; channel < 4; channel++)
				{
					if (PixelByte(bitmap, 0, y, channel) != PixelByte(reference, 8, y, channel))
					{
						matched = false;
					}
				}
			}
			Check(matched, "wrap offset -8 places source column 8 at column 0");
			bitmap.Dispose();
			reference.Dispose();
		}

		private static void TestRepeatEdge()
		{
			SKBitmap bitmap = BuildSolidBitmap(32, 32, 40, 120, 200);
			FilterOther.Offset(bitmap, 8, 8, eOffsetEdge.RepeatEdge);
			byte red = PixelByte(bitmap, 0, 0, 0);
			byte green = PixelByte(bitmap, 0, 0, 1);
			byte blue = PixelByte(bitmap, 0, 0, 2);
			byte alpha = PixelByte(bitmap, 0, 0, 3);
			Check(red == 40 && green == 120 && blue == 200, "repeat edge fills top-left corner with clamped solid color");
			Check(alpha == 255, "repeat edge keeps top-left corner opaque");
			bitmap.Dispose();
		}

		private static void TestTransparent()
		{
			SKBitmap bitmap = BuildSolidBitmap(32, 32, 40, 120, 200);
			FilterOther.Offset(bitmap, 8, 8, eOffsetEdge.Transparent);
			byte cornerAlpha = PixelByte(bitmap, 0, 0, 3);
			byte interiorAlpha = PixelByte(bitmap, 20, 20, 3);
			byte interiorRed = PixelByte(bitmap, 20, 20, 0);
			Check(cornerAlpha == 0, "transparent fills top-left corner with alpha 0");
			Check(interiorAlpha == 255 && interiorRed == 40, "transparent keeps interior pixel opaque and colored");
			bitmap.Dispose();
		}
	}
}
