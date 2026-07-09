using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class LassoCloseTests
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
			TestZoomedInDoesNotCloseEarly();
			TestCloseWithinRadius();
			return s_failures;
		}

		private static void TestZoomedInDoesNotCloseEarly()
		{
			Document document = new Document("l", 200, 200);
			ToolState state = new ToolState();
			LassoTool tool = new LassoTool();
			tool.SetCloseRadius(3);
			tool.OnPressed(document, 20, 20, state);
			tool.OnPressed(document, 80, 20, state);
			tool.OnPressed(document, 80, 80, state);
			tool.OnPressed(document, 26, 20, state);
			Check(!document.Selection().IsActive(), "zoomed-in lasso does not close when the click is 6 doc px from the start but the screen radius is 3");
		}

		private static void TestCloseWithinRadius()
		{
			Document document = new Document("l", 200, 200);
			ToolState state = new ToolState();
			LassoTool tool = new LassoTool();
			tool.SetCloseRadius(6);
			tool.OnPressed(document, 20, 20, state);
			tool.OnPressed(document, 80, 20, state);
			tool.OnPressed(document, 80, 80, state);
			tool.OnPressed(document, 26, 20, state);
			Check(document.Selection().IsActive(), "lasso closes when the click is within the close radius");
		}
	}
}
