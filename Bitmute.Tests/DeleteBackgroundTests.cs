using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class DeleteBackgroundTests
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
			TestPreserveKeepsEmptyPixelClear();
			TestPreserveFillsExistingContent();
			TestWithoutPreserveFillsEverything();
			return s_failures;
		}

		private static Document BuildTransparentBackgroundWithPatch()
		{
			Document document = new Document("d", 100, 100);
			Layer layer = document.ActiveLayer();
			layer.SetIsBackground(true);
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			layer.SetPixelCanvas(10, 10, new SKColor(255, 0, 0, 255));
			ToolState state = new ToolState();
			RectangleSelectTool tool = new RectangleSelectTool();
			tool.SetPointerTravel(40.0);
			tool.SetGuideSnap(-1);
			tool.OnPressed(document, 5, 5, state);
			tool.OnDragged(document, 20, 20, state);
			return document;
		}

		private static void TestPreserveKeepsEmptyPixelClear()
		{
			Document document = BuildTransparentBackgroundWithPatch();
			document.FillSelection(new SKColor(0, 0, 255, 255), true);
			SKColor empty = document.ActiveLayer().GetPixelCanvas(7, 7);
			Check(empty.Alpha == 0, "background delete leaves an empty pixel transparent when preserving (" + empty.Alpha + ")");
		}

		private static void TestPreserveFillsExistingContent()
		{
			Document document = BuildTransparentBackgroundWithPatch();
			document.FillSelection(new SKColor(0, 0, 255, 255), true);
			SKColor patch = document.ActiveLayer().GetPixelCanvas(10, 10);
			Check(patch.Alpha == 255 && patch.Blue == 255 && patch.Red == 0, "background delete fills the existing opaque pixel with the background color");
		}

		private static void TestWithoutPreserveFillsEverything()
		{
			Document document = BuildTransparentBackgroundWithPatch();
			document.FillSelection(new SKColor(0, 0, 255, 255), false);
			SKColor empty = document.ActiveLayer().GetPixelCanvas(7, 7);
			Check(empty.Alpha == 255, "without preserve, the previously empty pixel is filled opaque (control)");
		}
	}
}
