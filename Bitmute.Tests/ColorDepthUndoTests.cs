using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class ColorDepthUndoTests
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
			TestModeChangeUndoRestoresDepthAndPixels();
			TestModeChangeRedoReappliesDepth();
			return s_failures;
		}

		private static void TestModeChangeUndoRestoresDepthAndPixels()
		{
			Document document = new Document("t", 2, 2);
			document.ActiveLayer().Bitmap().SetPixel(1, 1, new SKColor(10, 100, 200, 128));
			Check(document.ColorDepth() == eColorDepth.Eight, "document starts at 8-bit");

			DocumentStateCommand command = new DocumentStateCommand("Mode");
			command.CaptureBefore(document);
			document.ConvertColorDepth(eColorDepth.Sixteen);
			command.CaptureAfter(document);

			Check(document.ColorDepth() == eColorDepth.Sixteen, "convert moves document to 16-bit");
			Check(document.ActiveLayer().Bitmap().ColorType == SKColorType.Rgba16161616, "converted layer bitmap is 16-bit");

			command.ApplyBefore(document);
			Check(document.ColorDepth() == eColorDepth.Eight, "undo restores document color depth to 8-bit");
			SKBitmap restored = document.ActiveLayer().Bitmap();
			Check(restored.ColorType == SKColorType.Rgba8888, "undo restores active layer bitmap to Rgba8888");
			SKColor pixel = restored.GetPixel(1, 1);
			Check(pixel.Red == 10 && pixel.Green == 100 && pixel.Blue == 200 && pixel.Alpha == 128, "undo restores original pixel value");
			document.ReleaseComposite();
		}

		private static void TestModeChangeRedoReappliesDepth()
		{
			Document document = new Document("t", 2, 2);
			document.ActiveLayer().Bitmap().SetPixel(1, 1, new SKColor(10, 100, 200, 128));

			DocumentStateCommand command = new DocumentStateCommand("Mode");
			command.CaptureBefore(document);
			document.ConvertColorDepth(eColorDepth.Sixteen);
			command.CaptureAfter(document);

			command.ApplyBefore(document);
			Check(document.ColorDepth() == eColorDepth.Eight, "redo test undo lands on 8-bit");

			command.ApplyAfter(document);
			Check(document.ColorDepth() == eColorDepth.Sixteen, "redo re-applies 16-bit depth");
			Check(document.ActiveLayer().Bitmap().ColorType == SKColorType.Rgba16161616, "redo re-applies 16-bit layer bitmap");
			document.ReleaseComposite();
		}
	}
}
