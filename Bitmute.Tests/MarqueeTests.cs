using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class MarqueeTests
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
			TestFarEdgeGuideSnap();
			TestNearEdgeGuideSnap();
			TestClickVersusOnePixel();
			return s_failures;
		}

		private static void TestFarEdgeGuideSnap()
		{
			Document document = new Document("m", 200, 200);
			document.Guides().AddHorizontal(100);
			ToolState state = new ToolState();
			RectangleSelectTool tool = new RectangleSelectTool();
			tool.SetPointerTravel(50.0);
			tool.SetGuideSnap(5);
			tool.OnPressed(document, 20, 20, state);
			tool.OnDragged(document, 60, 98, state);
			Check(document.Selection().Bounds().Bottom == 100, "marquee far-edge guide snap lands bottom boundary on guide (" + document.Selection().Bounds().Bottom + ")");
		}

		private static void TestNearEdgeGuideSnap()
		{
			Document document = new Document("m", 200, 200);
			document.Guides().AddHorizontal(100);
			ToolState state = new ToolState();
			RectangleSelectTool tool = new RectangleSelectTool();
			tool.SetPointerTravel(50.0);
			tool.SetGuideSnap(5);
			tool.OnPressed(document, 20, 98, state);
			tool.OnDragged(document, 60, 160, state);
			Check(document.Selection().Bounds().Top == 100, "marquee near-edge guide snap lands top boundary on guide (" + document.Selection().Bounds().Top + ")");
		}

		private static void TestClickVersusOnePixel()
		{
			Document clickDocument = new Document("m", 200, 200);
			ToolState clickState = new ToolState();
			RectangleSelectTool clickTool = new RectangleSelectTool();
			clickTool.SetPointerTravel(0.5);
			clickTool.SetGuideSnap(-1);
			clickTool.OnPressed(clickDocument, 30, 30, clickState);
			clickTool.OnDragged(clickDocument, 30, 30, clickState);
			Check(!clickDocument.Selection().IsActive(), "marquee click below travel threshold makes no selection");

			Document pixelDocument = new Document("m", 200, 200);
			ToolState pixelState = new ToolState();
			RectangleSelectTool pixelTool = new RectangleSelectTool();
			pixelTool.SetPointerTravel(40.0);
			pixelTool.SetGuideSnap(-1);
			pixelTool.OnPressed(pixelDocument, 30, 30, pixelState);
			pixelTool.OnDragged(pixelDocument, 30, 30, pixelState);
			SKRectI bounds = pixelDocument.Selection().Bounds();
			Check(bounds.Width == 1 && bounds.Height == 1, "marquee drag at/above travel threshold honors a 1x1 selection (" + bounds.Width + "x" + bounds.Height + ")");
		}
	}
}
