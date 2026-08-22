using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class ChannelPlaneTests
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
			TestExtractChannelToGray();
			TestExtractClipsToBitmap();
			TestWriteRedChannelIntoAlpha();
			TestWriteClipsAtEdges();
			TestSixteenBitChannelRoundTrip();
			TestChannelStrokeTouchesOnlySelectedChannel();
			TestChannelStrokeUndoRestoresLayer();
			TestFillLayerTargetsChannel();
			return s_failures;
		}

		private static SKBitmap BuildBitmap(int width, int height, SKColor color)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(color);
			return bitmap;
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

		private static void TestExtractChannelToGray()
		{
			SKBitmap source = BuildBitmap(8, 8, new SKColor(200, 100, 50, 180));
			SKBitmap red = ChannelPlane.Extract(source, new SKRectI(0, 0, 8, 8), 0);
			SKBitmap alpha = ChannelPlane.Extract(source, new SKRectI(0, 0, 8, 8), 3);
			SKColor redPixel = red.GetPixel(4, 4);
			SKColor alphaPixel = alpha.GetPixel(4, 4);
			Check(redPixel.Red == 200 && redPixel.Green == 200 && redPixel.Blue == 200, "red channel extracts to gray 200");
			Check(redPixel.Alpha == 255, "extracted channel plane is opaque");
			Check(alphaPixel.Red == 180, "alpha channel extracts to gray 180");
			red.Dispose();
			alpha.Dispose();
			source.Dispose();
		}

		private static void TestExtractClipsToBitmap()
		{
			SKBitmap source = BuildBitmap(8, 8, new SKColor(120, 0, 0, 255));
			SKBitmap plane = ChannelPlane.Extract(source, new SKRectI(4, 4, 20, 20), 0);
			SKBitmap outside = ChannelPlane.Extract(source, new SKRectI(20, 20, 30, 30), 0);
			Check(plane != null && plane.Width == 4 && plane.Height == 4, "extract clips the region to the bitmap");
			Check(outside == null, "a region outside the bitmap extracts nothing");
			if (plane != null)
			{
				plane.Dispose();
			}
			source.Dispose();
		}

		private static void TestWriteRedChannelIntoAlpha()
		{
			SKBitmap source = BuildBitmap(8, 8, new SKColor(200, 100, 50, 255));
			SKBitmap target = BuildBitmap(8, 8, new SKColor(10, 20, 30, 40));
			SKBitmap red = ChannelPlane.Extract(source, new SKRectI(0, 0, 8, 8), 0);
			ChannelPlane.Write(target, red, 3, 0, 0);
			SKColor pixel = target.GetPixel(4, 4);
			Check(pixel.Alpha == 200, "the red channel lands in the alpha channel");
			Check(pixel.Red == 10 && pixel.Green == 20 && pixel.Blue == 30, "writing alpha leaves the color channels alone");
			red.Dispose();
			target.Dispose();
			source.Dispose();
		}

		private static void TestWriteClipsAtEdges()
		{
			SKBitmap target = BuildBitmap(8, 8, new SKColor(10, 20, 30, 40));
			SKBitmap plane = BuildBitmap(4, 4, new SKColor(255, 255, 255, 255));
			ChannelPlane.Write(target, plane, 0, 6, 6);
			ChannelPlane.Write(target, plane, 0, -2, -2);
			SKColor corner = target.GetPixel(7, 7);
			SKColor origin = target.GetPixel(0, 0);
			SKColor middle = target.GetPixel(4, 4);
			Check(corner.Red == 255, "a write past the right edge fills the overlapping pixels");
			Check(origin.Red == 255, "a write past the top left edge fills the overlapping pixels");
			Check(middle.Red == 10, "pixels outside the written region are untouched");
			plane.Dispose();
			target.Dispose();
		}

		private static void TestSixteenBitChannelRoundTrip()
		{
			SKBitmap source = new SKBitmap(8, 8, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			source.Erase(new SKColor(200, 100, 50, 255));
			SKBitmap target = new SKBitmap(8, 8, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			target.Erase(new SKColor(10, 20, 30, 255));
			SKBitmap red = ChannelPlane.Extract(source, new SKRectI(0, 0, 8, 8), 0);
			Check(red != null && red.ColorType == SKColorType.Rgba16161616, "a 16 bit channel extracts at 16 bit");
			ChannelPlane.Write(target, red, 3, 0, 0);
			SKColor pixel = target.GetPixel(4, 4);
			int alphaDelta = pixel.Alpha - 200;
			if (alphaDelta < 0)
			{
				alphaDelta = -alphaDelta;
			}
			Check(alphaDelta <= 1, "a 16 bit red channel lands in the 16 bit alpha channel (actual " + pixel.Alpha + ")");
			Check(pixel.Red == 10, "the 16 bit color channels survive an alpha write");
			red.Dispose();
			target.Dispose();
			source.Dispose();
		}

		private static void TestChannelStrokeTouchesOnlySelectedChannel()
		{
			Document document = new Document("channelstroke", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(200, 100, 50, 255));
			document.SetPaintChannel(3);
			Check(document.PaintTarget() == ePaintTarget.Channel, "selecting a channel sets the channel paint target");
			ToolState state = BuildState(new SKColor(0, 0, 0, 255));
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 32, 32, state);
			brush.OnReleased(document, 32, 32, state);
			document.EndStroke();
			SKColor painted = layer.Bitmap().GetPixel(32, 32);
			SKColor untouched = layer.Bitmap().GetPixel(5, 5);
			Check(painted.Alpha < 16, "painting black on the alpha channel clears alpha at the painted point");
			Check(painted.Red == 200 && painted.Green == 100 && painted.Blue == 50, "the color channels are unchanged by an alpha channel stroke");
			Check(untouched.Alpha == 255, "a point outside the dab keeps its alpha");
		}

		private static void TestChannelStrokeUndoRestoresLayer()
		{
			Document document = new Document("channelundo", 64, 64);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(200, 100, 50, 255));
			document.SetPaintChannel(0);
			ToolState state = BuildState(new SKColor(0, 0, 0, 255));
			BrushTool brush = new BrushTool();
			document.BeginStroke();
			brush.OnPressed(document, 32, 32, state);
			brush.OnReleased(document, 32, 32, state);
			document.EndStroke();
			SKColor painted = layer.Bitmap().GetPixel(32, 32);
			Check(painted.Red < 16, "painting black on the red channel clears red at the painted point");
			bool undone = document.Undo();
			SKColor restored = document.ActiveLayer().Bitmap().GetPixel(32, 32);
			Check(undone, "a channel stroke pushes one undo entry");
			Check(restored.Red == 200 && restored.Green == 100 && restored.Blue == 50 && restored.Alpha == 255, "undo restores the channel");
		}

		private static void TestFillLayerTargetsChannel()
		{
			Document document = new Document("channelfill", 32, 32);
			Layer layer = document.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(200, 100, 50, 255));
			document.SetPaintChannel(1);
			document.FillLayer(new SKColor(0, 0, 0, 255));
			SKColor filled = layer.Bitmap().GetPixel(16, 16);
			Check(filled.Green == 0, "filling with black clears the selected channel");
			Check(filled.Red == 200 && filled.Blue == 50 && filled.Alpha == 255, "filling a channel leaves the other channels alone");
		}
	}
}
