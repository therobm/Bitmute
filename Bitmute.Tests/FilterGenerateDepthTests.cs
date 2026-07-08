using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class FilterGenerateDepthTests
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

		private static SKBitmap BuildUniformSixteen(int width, int height, float height01, float alpha)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					accessor.WriteNormalized(x, y, height01, height01, height01, alpha);
				}
			}
			return bitmap;
		}

		private static SKBitmap BuildRampEight(int width, int height, int baseValue, int step)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
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
					byte gray = (byte)value;
					bitmap.SetPixel(x, y, new SKColor(gray, gray, gray, 255));
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
					int value = baseValue + (step * x);
					if (value < 0)
					{
						value = 0;
					}
					if (value > 255)
					{
						value = 255;
					}
					float gray = value / 255.0f;
					accessor.WriteNormalized(x, y, gray, gray, gray, 1.0f);
				}
			}
			return bitmap;
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestSixteenBitFlatHeightIsNeutral();
			TestEightAndSixteenAgree();
			return s_failures;
		}

		private static void TestSixteenBitFlatHeightIsNeutral()
		{
			SKBitmap bitmap = BuildUniformSixteen(24, 24, 0.5f, 0.82f);
			FilterGenerate.NormalMap(bitmap, 2.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, false, FilterGenerate.eNormalMapEdge.Clamp);
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			bool allNeutral = true;
			for (int y = 4; y < 20; y++)
			{
				for (int x = 4; x < 20; x++)
				{
					float red;
					float green;
					float blue;
					float alpha;
					accessor.ReadNormalized(x, y, out red, out green, out blue, out alpha);
					if (!Near(red, 0.5f, 0.01f))
					{
						allNeutral = false;
					}
					if (!Near(green, 0.5f, 0.01f))
					{
						allNeutral = false;
					}
					if (!Near(blue, 1.0f, 0.01f))
					{
						allNeutral = false;
					}
				}
			}
			Check(allNeutral, "16-bit flat height yields neutral up-facing normal in the interior");
			bitmap.Dispose();
		}

		private static void TestEightAndSixteenAgree()
		{
			SKBitmap eight = BuildRampEight(64, 16, 40, 2);
			SKBitmap sixteen = BuildRampSixteen(64, 16, 40, 2);
			FilterGenerate.NormalMap(eight, 3.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, false, FilterGenerate.eNormalMapEdge.Clamp);
			FilterGenerate.NormalMap(sixteen, 3.0f, FilterGenerate.eNormalMapKernel.Sobel3, false, false, FilterGenerate.eNormalMapEdge.Clamp);
			PixelAccessor sixteenAccessor = new PixelAccessor(sixteen.GetPixels(), sixteen.RowBytes, sixteen.ColorType);
			int[] sampleXs = new int[] { 8, 20, 32, 44, 56 };
			int sampleY = 8;
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
				if (!Near(eightRed, sixteenRed, 0.02f))
				{
					allAgree = false;
				}
				if (!Near(eightGreen, sixteenGreen, 0.02f))
				{
					allAgree = false;
				}
				if (!Near(eightBlue, sixteenBlue, 0.02f))
				{
					allAgree = false;
				}
			}
			Check(allAgree, "8-bit and 16-bit normal maps agree on the same ramp within tolerance");
			eight.Dispose();
			sixteen.Dispose();
		}
	}
}
