using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class LayerMaskPaintTests
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
			TestPaintMaskHidesLayer();
			TestUndoRestoresMask();
			TestPaintLayerEditsPixelsNotMask();
			TestEraserOnMaskHides();
			return s_failures;
		}

		private static ToolState BuildState(SKColor foreground)
		{
			ToolState state = new ToolState();
			state.SetBrushSize(20);
			state.SetBrushHardness(100);
			state.SetBrushOpacity(100);
			state.SetBrushFlow(100);
			state.SetForeground(foreground);
			return state;
		}

		private static int CompositeAlphaAt(Document document, int x, int y)
		{
			SKBitmap target = new SKBitmap(document.Width(), document.Height(), SKColorType.Rgba8888, SKAlphaType.Premul);
			document.CompositeInto(target);
			SKColor pixel = target.GetPixel(x, y);
			int alpha = pixel.Alpha;
			target.Dispose();
			return alpha;
		}

		private static void TestPaintMaskHidesLayer()
		{
			Document document = new Document("paintmask", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(200, 40, 40, 255));
			document.AddMaskToActiveLayer(true);
			SKColor colorBefore = layer.Bitmap().GetPixel(32, 32);
			document.SetPaintTarget(ePaintTarget.Mask);
			ToolState state = BuildState(new SKColor(0, 0, 0, 255));
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 32, 32, state);
			brush.OnReleased(document, 32, 32, state);
			document.EndStroke();
			int paintedAlpha = CompositeAlphaAt(document, 32, 32);
			int farAlpha = CompositeAlphaAt(document, 5, 5);
			Check(paintedAlpha < 16, "painting black into the mask hides the painted point");
			Check(farAlpha == 255, "far away point stays fully opaque");
			SKColor colorAfter = layer.Bitmap().GetPixel(32, 32);
			Check(colorAfter.Red == colorBefore.Red && colorAfter.Green == colorBefore.Green && colorAfter.Blue == colorBefore.Blue && colorAfter.Alpha == colorBefore.Alpha, "layer color bitmap at painted point is unchanged by a mask stroke");
		}

		private static void TestUndoRestoresMask()
		{
			Document document = new Document("undomask", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(200, 40, 40, 255));
			document.AddMaskToActiveLayer(true);
			document.SetPaintTarget(ePaintTarget.Mask);
			ToolState state = BuildState(new SKColor(0, 0, 0, 255));
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 32, 32, state);
			brush.OnReleased(document, 32, 32, state);
			document.EndStroke();
			int hiddenAlpha = CompositeAlphaAt(document, 32, 32);
			Check(hiddenAlpha < 16, "mask stroke hides the point before undo");
			document.Undo();
			int restoredAlpha = CompositeAlphaAt(document, 32, 32);
			Check(restoredAlpha == 255, "undo reverts the mask so the point is opaque again");
		}

		private static void TestPaintLayerEditsPixelsNotMask()
		{
			Document document = new Document("paintlayer", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(200, 40, 40, 255));
			document.AddMaskToActiveLayer(true);
			SKColor maskBefore = layer.MaskBitmap().GetPixel(32, 32);
			document.SetPaintTarget(ePaintTarget.Layer);
			ToolState state = BuildState(new SKColor(0, 220, 0, 255));
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 32, 32, state);
			brush.OnReleased(document, 32, 32, state);
			document.EndStroke();
			SKColor colorAfter = layer.Bitmap().GetPixel(32, 32);
			Check(colorAfter.Green > 150 && colorAfter.Red < 80, "painting the layer edits the layer color bitmap");
			SKColor maskAfter = layer.MaskBitmap().GetPixel(32, 32);
			Check(maskAfter.Red == maskBefore.Red && maskAfter.Green == maskBefore.Green && maskAfter.Blue == maskBefore.Blue, "layer stroke leaves the mask unchanged");
		}

		private static void TestEraserOnMaskHides()
		{
			Document document = new Document("erasemask", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(40, 90, 200, 255));
			document.AddMaskToActiveLayer(true);
			SKColor colorBefore = layer.Bitmap().GetPixel(32, 32);
			document.SetPaintTarget(ePaintTarget.Mask);
			ToolState state = BuildState(new SKColor(255, 255, 255, 255));
			EraserTool eraser = new EraserTool();
			document.BeginStroke();
			eraser.OnPressed(document, 32, 32, state);
			eraser.OnReleased(document, 32, 32, state);
			document.EndStroke();
			int paintedAlpha = CompositeAlphaAt(document, 32, 32);
			Check(paintedAlpha < 16, "eraser on the mask hides the painted point");
			SKColor colorAfter = layer.Bitmap().GetPixel(32, 32);
			Check(colorAfter.Red == colorBefore.Red && colorAfter.Green == colorBefore.Green && colorAfter.Blue == colorBefore.Blue && colorAfter.Alpha == colorBefore.Alpha, "eraser mask stroke leaves the layer color bitmap unchanged");
		}
	}
}
