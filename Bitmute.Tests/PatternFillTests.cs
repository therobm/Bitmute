using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class PatternFillTests
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

		private static void FillLayer(Layer layer, SKColor color)
		{
			SKBitmap bitmap = layer.Bitmap();
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bitmap.SetPixel(x, y, color);
				}
			}
		}

		private static SKBitmap BuildPattern()
		{
			SKBitmap pattern = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int py = 0; py < 4; py++)
			{
				for (int px = 0; px < 4; px++)
				{
					pattern.SetPixel(px, py, new SKColor((byte)(px * 60), (byte)(py * 60), 200, 255));
				}
			}
			return pattern;
		}

		private static void TestPatternTiling()
		{
			Document document = new Document("t", 16, 16);
			Layer layer = document.ActiveLayer();
			FillLayer(layer, new SKColor(10, 20, 30, 255));
			SKBitmap patternBitmap = BuildPattern();
			Pattern pattern = new Pattern("p", patternBitmap);
			ToolState state = new ToolState();
			state.SetFillTolerance(0);
			state.SetActivePattern(pattern);
			state.SetFillContent(eFillContent.Pattern);
			FillTool tool = new FillTool();
			bool changed = tool.OnPressed(document, 8, 8, state);
			Check(changed, "pattern fill reports change");
			SKColor at00 = layer.GetPixelCanvas(0, 0);
			Check(at00 == patternBitmap.GetPixel(0, 0), "pattern sample at (0,0) matches tile");
			SKColor at23 = layer.GetPixelCanvas(2, 3);
			Check(at23 == patternBitmap.GetPixel(2, 3), "pattern sample at (2,3) matches tile");
			SKColor at57 = layer.GetPixelCanvas(5, 7);
			Check(at57 == patternBitmap.GetPixel(5 % 4, 7 % 4), "pattern sample at (5,7) matches wrapped tile");
			SKColor at40 = layer.GetPixelCanvas(4, 0);
			Check(at40 == at00, "one pattern-width apart equal (tiling)");
			SKColor at04 = layer.GetPixelCanvas(0, 4);
			Check(at04 == at00, "one pattern-height apart equal (tiling)");
		}

		private static void TestPatternOffsetLayer()
		{
			Document document = new Document("t", 16, 16);
			Layer layer = document.ActiveLayer();
			layer.SetOffset(3, 0);
			FillLayer(layer, new SKColor(10, 20, 30, 255));
			SKBitmap patternBitmap = BuildPattern();
			Pattern pattern = new Pattern("p", patternBitmap);
			ToolState state = new ToolState();
			state.SetFillTolerance(0);
			state.SetActivePattern(pattern);
			state.SetFillContent(eFillContent.Pattern);
			FillTool tool = new FillTool();
			bool changed = tool.OnPressed(document, 8, 8, state);
			Check(changed, "pattern fill on offset layer reports change");
			SKColor atCanvas4 = layer.GetPixelCanvas(4, 0);
			Check(atCanvas4 == patternBitmap.GetPixel(4 % 4, 0), "offset layer sample uses canvas coords at (4,0)");
			SKColor atCanvas5 = layer.GetPixelCanvas(5, 6);
			Check(atCanvas5 == patternBitmap.GetPixel(5 % 4, 6 % 4), "offset layer sample uses canvas coords at (5,6)");
			SKColor atCanvas8 = layer.GetPixelCanvas(8, 0);
			Check(atCanvas8 == patternBitmap.GetPixel(4 % 4, 0), "offset layer tile aligned to canvas 0,0");
		}

		private static void TestForegroundUnaffected()
		{
			Document document = new Document("t", 16, 16);
			Layer layer = document.ActiveLayer();
			FillLayer(layer, new SKColor(10, 20, 30, 255));
			SKBitmap patternBitmap = BuildPattern();
			Pattern pattern = new Pattern("p", patternBitmap);
			ToolState state = new ToolState();
			state.SetFillTolerance(0);
			state.SetActivePattern(pattern);
			state.SetFillContent(eFillContent.Foreground);
			state.SetForeground(new SKColor(120, 40, 220, 255));
			FillTool tool = new FillTool();
			bool changed = tool.OnPressed(document, 8, 8, state);
			Check(changed, "foreground fill reports change");
			SKColor filled = layer.GetPixelCanvas(8, 8);
			Check(filled == new SKColor(120, 40, 220, 255), "foreground mode uses foreground color");
		}

		private static void TestPatternModeNoActivePatternNoOp()
		{
			Document document = new Document("t", 16, 16);
			Layer layer = document.ActiveLayer();
			FillLayer(layer, new SKColor(10, 20, 30, 255));
			ToolState state = new ToolState();
			state.SetFillTolerance(0);
			state.SetActivePattern(null);
			state.SetFillContent(eFillContent.Pattern);
			FillTool tool = new FillTool();
			bool changed = tool.OnPressed(document, 8, 8, state);
			Check(!changed, "pattern mode with no active pattern returns false");
			SKColor untouched = layer.GetPixelCanvas(8, 8);
			Check(untouched == new SKColor(10, 20, 30, 255), "pattern mode with no active pattern paints nothing");
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestPatternTiling();
			TestPatternOffsetLayer();
			TestForegroundUnaffected();
			TestPatternModeNoActivePatternNoOp();
			return s_failures;
		}
	}
}
