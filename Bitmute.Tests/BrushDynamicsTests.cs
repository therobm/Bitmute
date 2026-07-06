using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class BrushDynamicsTests
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
			TestFadeRampsToZero();
			TestFadeOffPaintsFarEnd();
			TestCustomTipSampled();
			return s_failures;
		}

		private static int FadeAlphaAt(Layer layer, int x)
		{
			int best = 0;
			for (int y = 0; y < 8; y++)
			{
				SKColor sample = layer.GetPixelCanvas(x, y);
				if (sample.Alpha > best)
				{
					best = sample.Alpha;
				}
			}
			return best;
		}

		private static Layer PaintFadeStroke(int fadeLength)
		{
			Document document = new Document("t", 220, 8);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			ToolState state = new ToolState();
			state.SetBrushSize(6);
			state.SetBrushOpacity(100);
			state.SetBrushHardness(100);
			state.SetForeground(new SKColor(0, 0, 0, 255));
			state.SetBrushFadeLength(fadeLength);
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 8, 4, state);
			int step = 8;
			for (step = 8; step <= 208; step++)
			{
				brush.OnDragged(document, step, 4, state);
			}
			brush.OnReleased(document, 208, 4, state);
			document.EndStroke();
			return layer;
		}

		private static void TestFadeRampsToZero()
		{
			Layer layer = PaintFadeStroke(100);
			int startAlpha = FadeAlphaAt(layer, 10);
			int alpha50 = FadeAlphaAt(layer, 58);
			int alpha90 = FadeAlphaAt(layer, 98);
			int alphaAfter = FadeAlphaAt(layer, 140);
			Check(startAlpha > 200, "fade: alpha near stroke start is high");
			Check(alphaAfter < 20, "fade: alpha past the fade length is nearly zero");
			Check(alpha50 > alpha90, "fade: alpha at 50px is greater than alpha at 90px");
		}

		private static void TestFadeOffPaintsFarEnd()
		{
			Layer layer = PaintFadeStroke(0);
			int farAlpha = FadeAlphaAt(layer, 200);
			Check(farAlpha > 200, "fade off: far end of stroke is still fully painted");
		}

		private static SKBitmap BuildHalfBlackHalfWhiteTip()
		{
			int width = 40;
			int height = 40;
			SKBitmap tip = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (x < width / 2)
					{
						tip.SetPixel(x, y, new SKColor(0, 0, 0, 255));
					}
					else
					{
						tip.SetPixel(x, y, new SKColor(255, 255, 255, 255));
					}
				}
			}
			return tip;
		}

		private static void TestCustomTipSampled()
		{
			Document document = new Document("t", 128, 128);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			ToolState state = new ToolState();
			state.SetBrushSize(40);
			state.SetBrushOpacity(100);
			state.SetBrushHardness(100);
			state.SetForeground(new SKColor(0, 0, 0, 255));
			SKBitmap tip = BuildHalfBlackHalfWhiteTip();
			state.SetActiveCustomTip(tip);
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 64, 64, state);
			brush.OnReleased(document, 64, 64, state);
			document.EndStroke();
			SKColor leftSample = layer.GetPixelCanvas(54, 64);
			SKColor rightSample = layer.GetPixelCanvas(74, 64);
			Check(leftSample.Alpha > 200, "custom tip: dark (left) half of tip paints coverage");
			Check(rightSample.Alpha < 20, "custom tip: white (right) half of tip paints nothing");
		}
	}
}
