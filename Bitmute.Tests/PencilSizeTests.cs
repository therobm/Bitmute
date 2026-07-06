using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class PencilSizeTests
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
			TestPencilSizeHonored();
			TestPencilSizeOnePixel();
			return s_failures;
		}

		private static void TestPencilSizeHonored()
		{
			Document document = new Document("t", 64, 64);
			Layer layer = document.ActiveLayer();
			ToolState state = new ToolState();
			state.SetForeground(new SKColor(255, 0, 0, 255));
			state.SetBrushSize(9);
			PencilTool tool = new PencilTool();
			tool.OnPressed(document, 32, 32, state);
			SKColor center = layer.GetPixelCanvas(32, 32);
			Check(center.Red == 255 && center.Green == 0, "pencil size honored: center painted");
			SKColor inside = layer.GetPixelCanvas(36, 32);
			Check(inside.Red == 255 && inside.Green == 0, "pencil size honored: pixel inside radius painted");
			SKColor outside = layer.GetPixelCanvas(40, 32);
			Check(outside.Green == 255, "pencil size honored: pixel outside radius untouched");
		}

		private static void TestPencilSizeOnePixel()
		{
			Document document = new Document("t", 64, 64);
			Layer layer = document.ActiveLayer();
			ToolState state = new ToolState();
			state.SetForeground(new SKColor(255, 0, 0, 255));
			state.SetBrushSize(1);
			PencilTool tool = new PencilTool();
			tool.OnPressed(document, 10, 10, state);
			SKColor center = layer.GetPixelCanvas(10, 10);
			Check(center.Red == 255 && center.Green == 0, "pencil size 1: center painted");
			SKColor neighbor = layer.GetPixelCanvas(11, 10);
			Check(neighbor.Green == 255, "pencil size 1: neighbor untouched");
		}
	}
}
