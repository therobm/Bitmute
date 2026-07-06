using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class LayerMaskApplyTests
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

		private static void CheckNear(int actual, int expected, int tolerance, string name)
		{
			int delta = actual - expected;
			if (delta < 0)
			{
				delta = -delta;
			}
			Check(delta <= tolerance, name + " (actual " + actual + " expected " + expected + ")");
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestApplyBakesMaskIntoAlpha();
			return s_failures;
		}

		private static void TestApplyBakesMaskIntoAlpha()
		{
			Document document = new Document("maskapply", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(200, 100, 50, 255));
			document.AddMaskToActiveLayer(true);
			SKBitmap mask = layer.MaskBitmap();
			mask.SetPixel(10, 10, new SKColor(0, 0, 0, 255));
			mask.SetPixel(20, 20, new SKColor(128, 128, 128, 255));
			mask.SetPixel(30, 30, new SKColor(255, 255, 255, 255));
			SKBitmap before = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
			document.CompositeInto(before);
			int beforeAlphaBlack = before.GetPixel(10, 10).Alpha;
			int beforeAlphaGray = before.GetPixel(20, 20).Alpha;
			int beforeAlphaWhite = before.GetPixel(30, 30).Alpha;
			Check(beforeAlphaBlack == 0, "before apply black mask hides pixel");
			CheckNear(beforeAlphaGray, 128, 2, "before apply gray mask yields half alpha");
			Check(beforeAlphaWhite == 255, "before apply white mask keeps pixel opaque");
			before.Dispose();
			document.ApplyActiveMask();
			Check(!layer.HasMask(), "mask removed after apply");
			int bakedAlphaBlack = layer.Bitmap().GetPixel(10, 10).Alpha;
			int bakedAlphaGray = layer.Bitmap().GetPixel(20, 20).Alpha;
			int bakedAlphaWhite = layer.Bitmap().GetPixel(30, 30).Alpha;
			Check(bakedAlphaBlack == 0, "baked layer alpha zero at black mask point");
			CheckNear(bakedAlphaGray, 128, 2, "baked layer alpha half at gray mask point");
			Check(bakedAlphaWhite == 255, "baked layer alpha opaque at white mask point");
			SKColor bakedColor = layer.Bitmap().GetPixel(30, 30);
			Check(bakedColor.Red == 200 && bakedColor.Green == 100 && bakedColor.Blue == 50, "baked layer rgb unchanged");
			SKBitmap after = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
			document.CompositeInto(after);
			int afterAlphaBlack = after.GetPixel(10, 10).Alpha;
			int afterAlphaGray = after.GetPixel(20, 20).Alpha;
			int afterAlphaWhite = after.GetPixel(30, 30).Alpha;
			Check(afterAlphaBlack == beforeAlphaBlack, "recomposite alpha matches pre-apply at black point");
			CheckNear(afterAlphaGray, beforeAlphaGray, 2, "recomposite alpha matches pre-apply at gray point");
			Check(afterAlphaWhite == beforeAlphaWhite, "recomposite alpha matches pre-apply at white point");
			after.Dispose();
		}
	}
}
