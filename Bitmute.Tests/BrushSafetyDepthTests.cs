using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class BrushSafetyDepthTests
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
			TestSixteenBitSmudgeLeavesPixelsUnchanged();
			TestSixteenBitDodgeLeavesPixelsUnchanged();
			TestEightBitSmudgeStillChangesPixels();
			TestEightBitDodgeStillChangesPixels();
			return s_failures;
		}

		private static void FillHighDepthGradient(Layer layer)
		{
			SKBitmap bitmap = layer.Bitmap();
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float red = x / (float)(width - 1);
					float green = y / (float)(height - 1);
					float blue = ((x + y) % 64) / 63.0f;
					accessor.WriteNormalized(x, y, red, green, blue, 1.0f);
				}
			}
		}

		private static void FillEightBitGradient(Layer layer)
		{
			SKBitmap bitmap = layer.Bitmap();
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte red = (byte)((x * 255) / (width - 1));
					byte green = (byte)((y * 255) / (height - 1));
					byte blue = (byte)(((x + y) % 64) * 4);
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, 255));
				}
			}
		}

		private static byte[] SnapshotBytes(SKBitmap bitmap)
		{
			int length = bitmap.RowBytes * bitmap.Height;
			byte[] copy = new byte[length];
			Marshal.Copy(bitmap.GetPixels(), copy, 0, length);
			return copy;
		}

		private static bool BytesEqual(byte[] before, byte[] after)
		{
			if (before.Length != after.Length)
			{
				return false;
			}
			for (int index = 0; index < before.Length; index++)
			{
				if (before[index] != after[index])
				{
					return false;
				}
			}
			return true;
		}

		private static ToolState BuildSecondaryState()
		{
			ToolState state = new ToolState();
			state.SetBrushSize(20);
			state.SetBrushOpacity(100);
			state.SetBrushHardness(100);
			state.SetBrushFlow(100);
			state.SetBrushStrength(100);
			state.SetDodgeBurnRange(1);
			state.SetDodgeBurnExposure(100);
			return state;
		}

		private static void DriveStroke(BrushFamilyTool tool, Document document, ToolState state)
		{
			document.BeginStroke();
			tool.OnPressed(document, 18, 32, state);
			int step = 18;
			for (step = 18; step <= 46; step++)
			{
				tool.OnDragged(document, step, 32, state);
			}
			tool.OnReleased(document, 46, 32, state);
			document.EndStroke();
		}

		private static void TestSixteenBitSmudgeLeavesPixelsUnchanged()
		{
			Document document = new Document("sixteen", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.SetIsBackground(false);
			document.ConvertColorDepth(eColorDepth.Sixteen);
			FillHighDepthGradient(document.ActiveLayer());
			byte[] before = SnapshotBytes(document.ActiveLayer().Bitmap());
			ToolState state = BuildSecondaryState();
			SmudgeTool smudge = new SmudgeTool();
			DriveStroke(smudge, document, state);
			byte[] after = SnapshotBytes(document.ActiveLayer().Bitmap());
			Check(BytesEqual(before, after), "16-bit smudge safely no-ops and leaves layer pixels unchanged");
		}

		private static void TestSixteenBitDodgeLeavesPixelsUnchanged()
		{
			Document document = new Document("sixteen", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.SetIsBackground(false);
			document.ConvertColorDepth(eColorDepth.Sixteen);
			FillHighDepthGradient(document.ActiveLayer());
			byte[] before = SnapshotBytes(document.ActiveLayer().Bitmap());
			ToolState state = BuildSecondaryState();
			DodgeBurnTool dodge = new DodgeBurnTool();
			DriveStroke(dodge, document, state);
			byte[] after = SnapshotBytes(document.ActiveLayer().Bitmap());
			Check(BytesEqual(before, after), "16-bit dodge safely no-ops and leaves layer pixels unchanged");
		}

		private static void TestEightBitSmudgeStillChangesPixels()
		{
			Document document = new Document("eight", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.SetIsBackground(false);
			FillEightBitGradient(layer);
			byte[] before = SnapshotBytes(layer.Bitmap());
			ToolState state = BuildSecondaryState();
			SmudgeTool smudge = new SmudgeTool();
			DriveStroke(smudge, document, state);
			byte[] after = SnapshotBytes(layer.Bitmap());
			Check(!BytesEqual(before, after), "8-bit smudge still modifies layer pixels");
		}

		private static void TestEightBitDodgeStillChangesPixels()
		{
			Document document = new Document("eight", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.SetIsBackground(false);
			FillEightBitGradient(layer);
			byte[] before = SnapshotBytes(layer.Bitmap());
			ToolState state = BuildSecondaryState();
			DodgeBurnTool dodge = new DodgeBurnTool();
			DriveStroke(dodge, document, state);
			byte[] after = SnapshotBytes(layer.Bitmap());
			Check(!BytesEqual(before, after), "8-bit dodge still modifies layer pixels");
		}
	}
}
