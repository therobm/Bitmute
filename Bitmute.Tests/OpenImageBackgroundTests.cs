using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class OpenImageBackgroundTests
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

		public static int RunAll()
		{
			s_failures = 0;
			TestTransparentOpenIsNotBackground();
			TestOpaqueOpenIsBackground();
			return s_failures;
		}

		private static void TestTransparentOpenIsNotBackground()
		{
			SKBitmap source = new SKBitmap(32, 32, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			source.Erase(new SKColor(0, 0, 0, 0));
			source.SetPixel(10, 10, new SKColor(255, 0, 0, 255));
			Document document = Document.OpenImage("t", source);
			Check(!document.ActiveLayer().IsBackground(), "opening an image with transparency yields a non-background layer");
		}

		private static void TestOpaqueOpenIsBackground()
		{
			SKBitmap source = new SKBitmap(32, 32, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			source.Erase(new SKColor(255, 255, 255, 255));
			Document document = Document.OpenImage("t", source);
			Check(document.ActiveLayer().IsBackground(), "opening a fully opaque image yields a background layer");
		}
	}
}
