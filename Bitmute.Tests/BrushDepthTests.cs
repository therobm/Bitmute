using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class BrushDepthTests
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
			TestEightBitBrushStillPaints();
			TestSixteenBitBrushMatchesEightBit();
			TestSixteenBitEraserMatchesEightBit();
			return s_failures;
		}

		private static void ReadEightNormalized(Layer layer, int canvasX, int canvasY, out float red, out float green, out float blue, out float alpha)
		{
			SKColor sample = layer.GetPixelCanvas(canvasX, canvasY);
			red = sample.Red / 255.0f;
			green = sample.Green / 255.0f;
			blue = sample.Blue / 255.0f;
			alpha = sample.Alpha / 255.0f;
		}

		private static void ReadHighNormalized(Layer layer, int canvasX, int canvasY, out float red, out float green, out float blue, out float alpha)
		{
			SKBitmap bitmap = layer.Bitmap();
			int bitmapX = canvasX - layer.OffsetX();
			int bitmapY = canvasY - layer.OffsetY();
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			accessor.ReadNormalized(bitmapX, bitmapY, out red, out green, out blue, out alpha);
		}

		private static bool Near(float actual, float expected, float tolerance)
		{
			float delta = actual - expected;
			if (delta < 0.0f)
			{
				delta = -delta;
			}
			return delta <= tolerance;
		}

		private static void PaintBrushStroke(Document document, ToolState state)
		{
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 20, 32, state);
			int step = 20;
			for (step = 20; step <= 44; step++)
			{
				brush.OnDragged(document, step, 32, state);
			}
			brush.OnReleased(document, 44, 32, state);
			document.EndStroke();
		}

		private static void EraseBrushStroke(Document document, ToolState state)
		{
			EraserTool eraser = new EraserTool();
			document.BeginStroke();
			eraser.OnPressed(document, 20, 32, state);
			int step = 20;
			for (step = 20; step <= 44; step++)
			{
				eraser.OnDragged(document, step, 32, state);
			}
			eraser.OnReleased(document, 44, 32, state);
			document.EndStroke();
		}

		private static ToolState BuildBrushState()
		{
			ToolState state = new ToolState();
			state.SetBrushSize(16);
			state.SetBrushOpacity(80);
			state.SetBrushHardness(60);
			state.SetBrushFlow(100);
			state.SetForeground(new SKColor(40, 160, 220, 255));
			return state;
		}

		private static void TestEightBitBrushStillPaints()
		{
			Document document = new Document("t", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.SetIsBackground(false);
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			ToolState state = BuildBrushState();
			PaintBrushStroke(document, state);
			SKColor center = layer.GetPixelCanvas(32, 32);
			Check(center.Alpha > 180, "8-bit brush dab deposits opaque coverage at center");
			Check(center.Red > 20 && center.Red < 70, "8-bit brush dab deposits foreground red channel");
			Check(center.Green > 130 && center.Green < 190, "8-bit brush dab deposits foreground green channel");
			Check(center.Blue > 190, "8-bit brush dab deposits foreground blue channel");
			SKColor outside = layer.GetPixelCanvas(2, 2);
			Check(outside.Alpha == 0, "8-bit brush leaves area outside the stroke transparent");
		}

		private static void TestSixteenBitBrushMatchesEightBit()
		{
			Document eightDocument = new Document("eight", 64, 64);
			Layer eightLayer = eightDocument.ActiveLayer();
			eightLayer.SetIsBackground(false);
			eightLayer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			ToolState eightState = BuildBrushState();
			PaintBrushStroke(eightDocument, eightState);

			Document sixteenDocument = new Document("sixteen", 64, 64);
			Layer sixteenLayer = sixteenDocument.ActiveLayer();
			sixteenLayer.SetIsBackground(false);
			sixteenDocument.ConvertColorDepth(eColorDepth.Sixteen);
			sixteenDocument.ActiveLayer().Bitmap().Erase(new SKColor(0, 0, 0, 0));
			ToolState sixteenState = BuildBrushState();
			PaintBrushStroke(sixteenDocument, sixteenState);

			int[] samplesX = new int[] { 20, 26, 32, 38, 44 };
			int[] samplesY = new int[] { 28, 32, 36 };
			float tolerance = 0.02f;
			bool agree = true;
			for (int yi = 0; yi < samplesY.Length; yi++)
			{
				for (int xi = 0; xi < samplesX.Length; xi++)
				{
					int canvasX = samplesX[xi];
					int canvasY = samplesY[yi];
					float eightRed;
					float eightGreen;
					float eightBlue;
					float eightAlpha;
					ReadEightNormalized(eightDocument.ActiveLayer(), canvasX, canvasY, out eightRed, out eightGreen, out eightBlue, out eightAlpha);
					float sixteenRed;
					float sixteenGreen;
					float sixteenBlue;
					float sixteenAlpha;
					ReadHighNormalized(sixteenDocument.ActiveLayer(), canvasX, canvasY, out sixteenRed, out sixteenGreen, out sixteenBlue, out sixteenAlpha);
					bool sampleAgrees = Near(sixteenRed, eightRed, tolerance) && Near(sixteenGreen, eightGreen, tolerance) && Near(sixteenBlue, eightBlue, tolerance) && Near(sixteenAlpha, eightAlpha, tolerance);
					if (!sampleAgrees)
					{
						agree = false;
					}
				}
			}
			Check(agree, "16-bit brush paint reproduces 8-bit paint within tolerance across sampled locations");

			float centerSixteenAlpha;
			float centerSixteenRed;
			float centerSixteenGreen;
			float centerSixteenBlue;
			ReadHighNormalized(sixteenDocument.ActiveLayer(), 32, 32, out centerSixteenRed, out centerSixteenGreen, out centerSixteenBlue, out centerSixteenAlpha);
			Check(centerSixteenAlpha > 0.7f, "16-bit brush actually deposits coverage at center");
		}

		private static void TestSixteenBitEraserMatchesEightBit()
		{
			SKColor fill = new SKColor(200, 90, 60, 255);

			Document eightDocument = new Document("eight", 64, 64);
			Layer eightLayer = eightDocument.ActiveLayer();
			eightLayer.SetIsBackground(false);
			eightLayer.Bitmap().Erase(fill);
			ToolState eightState = BuildBrushState();
			EraseBrushStroke(eightDocument, eightState);

			Document sixteenDocument = new Document("sixteen", 64, 64);
			Layer sixteenLayer = sixteenDocument.ActiveLayer();
			sixteenLayer.SetIsBackground(false);
			sixteenDocument.ConvertColorDepth(eColorDepth.Sixteen);
			sixteenDocument.ActiveLayer().Bitmap().Erase(fill);
			ToolState sixteenState = BuildBrushState();
			EraseBrushStroke(sixteenDocument, sixteenState);

			float eightRed;
			float eightGreen;
			float eightBlue;
			float eightAlpha;
			ReadEightNormalized(eightDocument.ActiveLayer(), 32, 32, out eightRed, out eightGreen, out eightBlue, out eightAlpha);
			float sixteenRed;
			float sixteenGreen;
			float sixteenBlue;
			float sixteenAlpha;
			ReadHighNormalized(sixteenDocument.ActiveLayer(), 32, 32, out sixteenRed, out sixteenGreen, out sixteenBlue, out sixteenAlpha);

			Check(eightAlpha < 0.5f, "8-bit eraser reduces destination alpha at center");
			Check(Near(sixteenAlpha, eightAlpha, 0.02f), "16-bit eraser reduces alpha to match 8-bit within tolerance");

			float eightOutRed;
			float eightOutGreen;
			float eightOutBlue;
			float eightOutAlpha;
			ReadEightNormalized(eightDocument.ActiveLayer(), 2, 2, out eightOutRed, out eightOutGreen, out eightOutBlue, out eightOutAlpha);
			float sixteenOutRed;
			float sixteenOutGreen;
			float sixteenOutBlue;
			float sixteenOutAlpha;
			ReadHighNormalized(sixteenDocument.ActiveLayer(), 2, 2, out sixteenOutRed, out sixteenOutGreen, out sixteenOutBlue, out sixteenOutAlpha);
			Check(sixteenOutAlpha > 0.9f, "16-bit eraser leaves area outside the stroke opaque");
			Check(Near(sixteenOutAlpha, eightOutAlpha, 0.02f), "16-bit eraser untouched area matches 8-bit alpha");
		}
	}
}
