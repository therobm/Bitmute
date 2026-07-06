using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class PressureTests
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
			TestSizePressure();
			TestOpacityPressure();
			TestMouseNeutralUnchanged();
			return s_failures;
		}

		private static int PaintedRadius(bool touchPressure, float pressure)
		{
			Document document = new Document("t", 128, 128);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			ToolState state = new ToolState();
			state.SetBrushSize(24);
			state.SetForeground(new SKColor(0, 200, 0, 255));
			if (touchPressure)
			{
				state.SetPenPressure(pressure);
			}
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 40, 40, state);
			brush.OnReleased(document, 40, 40, state);
			document.EndStroke();
			int farthest = -1;
			int offset = 0;
			for (offset = 0; offset < 64; offset++)
			{
				int sampleX = 40 + offset;
				if (sampleX >= 128)
				{
					break;
				}
				SKColor sample = layer.GetPixelCanvas(sampleX, 40);
				if (sample.Alpha > 0)
				{
					farthest = offset;
				}
			}
			return farthest;
		}

		private static int CenterAlpha(bool touchPressure, float pressure)
		{
			Document document = new Document("t", 128, 128);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			ToolState state = new ToolState();
			state.SetBrushSize(24);
			state.SetBrushOpacity(100);
			state.SetBrushHardness(100);
			state.SetForeground(new SKColor(0, 200, 0, 255));
			if (touchPressure)
			{
				state.SetPenPressure(pressure);
			}
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 64, 64, state);
			brush.OnReleased(document, 64, 64, state);
			document.EndStroke();
			SKColor center = layer.GetPixelCanvas(64, 64);
			return center.Alpha;
		}

		private static void TestSizePressure()
		{
			int fullRadius = PaintedRadius(true, 1.0f);
			int lowRadius = PaintedRadius(true, 0.2f);
			Check(fullRadius > 0, "size pressure: full pressure paints a measurable radius");
			Check(lowRadius > 0, "size pressure: low pressure still paints");
			Check(lowRadius < fullRadius, "size pressure: low pressure radius is strictly smaller than full pressure radius");
		}

		private static void TestOpacityPressure()
		{
			int fullAlpha = CenterAlpha(true, 1.0f);
			int lowAlpha = CenterAlpha(true, 0.2f);
			Check(fullAlpha > 0, "opacity pressure: full pressure center is painted");
			Check(lowAlpha > 0, "opacity pressure: low pressure center is painted");
			Check(lowAlpha < fullAlpha, "opacity pressure: low pressure center alpha is strictly lower than full pressure center alpha");
		}

		private static void TestMouseNeutralUnchanged()
		{
			int fullAlpha = CenterAlpha(true, 1.0f);
			int defaultAlpha = CenterAlpha(false, 1.0f);
			Check(fullAlpha == defaultAlpha, "mouse neutral: full pressure center alpha equals untouched default brush center alpha");
		}
	}
}
