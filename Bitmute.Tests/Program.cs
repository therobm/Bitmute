using System;
using System.IO;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;
using Bitmute.Storage;

namespace Bitmute.Tests
{
	public static class Program
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

		private static void CheckNear(int actual, int expected, int tolerance, string name)
		{
			int delta = actual - expected;
			if (delta < 0)
			{
				delta = -delta;
			}
			Check(delta <= tolerance, name + " (actual " + actual + " expected " + expected + ")");
		}

		public static int Main(string[] args)
		{
			TestFillSelection();
			TestCustomBlends();
			TestCustomBlendOpacity();
			TestCustomBlendTransparentBase();
			TestMixedComposite();
			TestCompositeRangeInto();
			TestWandContiguous();
			TestWandNonContiguous();
			TestWandSampleAll();
			TestTgaRoundTrip();
			TestBitmuteRoundTrip();
			TestCropToRect();
			TestRotateTransform();
			TestSelectionStroke();
			TestWarpAffineScale();
			TestFlipTransformCommitUndo();
			TestTransformScaleCommit();
			TestTransformSelectionLiftCancel();
			TestTransformBackgroundStaysCanvas();
			TestCanvasOpUndo();
			TestGuidesModel();
			if (s_failures == 0)
			{
				Console.WriteLine("ALL PASS");
				return 0;
			}
			Console.WriteLine(s_failures + " FAILURES");
			return 1;
		}

		private static void TestFillSelection()
		{
			Document doc = new Document("t", 64, 64);
			Layer layer = doc.ActiveLayer();
			layer.SetOffset(20, 0);
			doc.Selection().SelectRect(new SKRectI(10, 10, 40, 30));
			doc.BeginStroke();
			doc.FillSelection(new SKColor(255, 0, 0, 255));
			doc.EndStroke();
			SKColor inside = layer.GetPixelCanvas(25, 15);
			Check(inside.Red == 255 && inside.Green == 0, "fill inside selection is red");
			SKColor edge = layer.GetPixelCanvas(39, 15);
			Check(edge.Red == 255 && edge.Green == 0, "fill at selection right edge is red");
			SKColor above = layer.GetPixelCanvas(25, 5);
			Check(above.Green == 255, "outside selection (above) untouched white");
			SKColor rightOut = layer.GetPixelCanvas(41, 15);
			Check(rightOut.Green == 255, "outside selection (right) untouched white");
			bool undone = doc.Undo();
			Check(undone, "fill undo available");
			SKColor reverted = layer.GetPixelCanvas(25, 15);
			Check(reverted.Green == 255, "fill undo reverts to white");
		}

		private static SKBitmap CompositeDoc(Document doc)
		{
			SKBitmap target = new SKBitmap(doc.Width(), doc.Height(), SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(target);
			return target;
		}

		private static void TestCustomBlends()
		{
			eBlendMode[] modes = new eBlendMode[] { eBlendMode.LinearBurn, eBlendMode.DarkerColor, eBlendMode.LighterColor, eBlendMode.VividLight, eBlendMode.LinearLight, eBlendMode.PinLight, eBlendMode.HardMix, eBlendMode.Subtract, eBlendMode.Divide };
			for (int index = 0; index < modes.Length; index++)
			{
				eBlendMode mode = modes[index];
				Document doc = new Document("t", 4, 4);
				doc.ActiveLayer().Bitmap().Erase(new SKColor(200, 100, 50, 255));
				Layer top = doc.AddLayer("top");
				top.Bitmap().Erase(new SKColor(80, 60, 40, 255));
				top.SetBlendMode(mode);
				byte expectedR;
				byte expectedG;
				byte expectedB;
				BlendModes.Blend(mode, 200, 100, 50, 80, 60, 40, out expectedR, out expectedG, out expectedB);
				SKBitmap target = CompositeDoc(doc);
				SKColor actual = target.GetPixel(1, 1);
				CheckNear(actual.Red, expectedR, 1, mode + " red");
				CheckNear(actual.Green, expectedG, 1, mode + " green");
				CheckNear(actual.Blue, expectedB, 1, mode + " blue");
				Check(actual.Alpha == 255, mode + " alpha opaque");
				target.Dispose();
			}
		}

		private static void TestCustomBlendOpacity()
		{
			Document doc = new Document("t", 4, 4);
			doc.ActiveLayer().Bitmap().Erase(new SKColor(200, 100, 50, 255));
			Layer top = doc.AddLayer("top");
			top.Bitmap().Erase(new SKColor(80, 60, 40, 255));
			top.SetBlendMode(eBlendMode.Subtract);
			top.SetOpacity(128);
			byte blendedR;
			byte blendedG;
			byte blendedB;
			BlendModes.Blend(eBlendMode.Subtract, 200, 100, 50, 80, 60, 40, out blendedR, out blendedG, out blendedB);
			int effective = ((255 * 128) + 127) / 255;
			int expectedR = ((blendedR * effective) + ((200 * (255 - effective)))) / 255;
			int expectedG = ((blendedG * effective) + ((100 * (255 - effective)))) / 255;
			int expectedB = ((blendedB * effective) + ((50 * (255 - effective)))) / 255;
			SKBitmap target = CompositeDoc(doc);
			SKColor actual = target.GetPixel(2, 2);
			CheckNear(actual.Red, expectedR, 2, "subtract 50% red");
			CheckNear(actual.Green, expectedG, 2, "subtract 50% green");
			CheckNear(actual.Blue, expectedB, 2, "subtract 50% blue");
			Check(actual.Alpha == 255, "subtract 50% alpha opaque");
			target.Dispose();
		}

		private static void TestCustomBlendTransparentBase()
		{
			Document doc = new Document("t", 4, 4);
			doc.ActiveLayer().Bitmap().Erase(new SKColor(0, 0, 0, 0));
			Layer top = doc.AddLayer("top");
			top.Bitmap().Erase(new SKColor(80, 60, 40, 255));
			top.SetBlendMode(eBlendMode.Divide);
			SKBitmap target = CompositeDoc(doc);
			SKColor actual = target.GetPixel(1, 1);
			CheckNear(actual.Red, 80, 2, "divide over transparent red");
			CheckNear(actual.Green, 60, 2, "divide over transparent green");
			CheckNear(actual.Blue, 40, 2, "divide over transparent blue");
			Check(actual.Alpha == 255, "divide over transparent alpha");
			target.Dispose();
		}

		private static void PaintSquare(Layer layer, int left, int top, int size, SKColor color)
		{
			for (int y = top; y < top + size; y++)
			{
				for (int x = left; x < left + size; x++)
				{
					layer.SetPixelCanvas(x, y, color);
				}
			}
		}

		private static void TestWandContiguous()
		{
			Document doc = new Document("t", 32, 32);
			Layer layer = doc.ActiveLayer();
			PaintSquare(layer, 2, 2, 4, new SKColor(255, 0, 0, 255));
			PaintSquare(layer, 20, 20, 4, new SKColor(255, 0, 0, 255));
			ToolState state = new ToolState();
			state.SetFillTolerance(0);
			MagicWandTool wand = new MagicWandTool();
			wand.OnPressed(doc, 3, 3, state);
			Check(doc.Selection().IsSelected(3, 3), "wand contiguous selects seed square");
			Check(!doc.Selection().IsSelected(21, 21), "wand contiguous skips disjoint square");
			Check(!doc.Selection().IsSelected(10, 10), "wand contiguous skips white");
		}

		private static void TestWandNonContiguous()
		{
			Document doc = new Document("t", 32, 32);
			Layer layer = doc.ActiveLayer();
			PaintSquare(layer, 2, 2, 4, new SKColor(255, 0, 0, 255));
			PaintSquare(layer, 20, 20, 4, new SKColor(255, 0, 0, 255));
			ToolState state = new ToolState();
			state.SetFillTolerance(0);
			state.SetWandContiguous(false);
			MagicWandTool wand = new MagicWandTool();
			wand.OnPressed(doc, 3, 3, state);
			Check(doc.Selection().IsSelected(3, 3), "wand global selects seed square");
			Check(doc.Selection().IsSelected(21, 21), "wand global selects disjoint square");
			Check(!doc.Selection().IsSelected(10, 10), "wand global skips white");
		}

		private static void TestWandSampleAll()
		{
			Document doc = new Document("t", 32, 32);
			Layer top = doc.AddLayer("top");
			top.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			PaintSquare(top, 10, 10, 5, new SKColor(0, 0, 255, 255));
			doc.SetActiveLayerIndex(0);
			ToolState state = new ToolState();
			state.SetFillTolerance(0);
			state.SetWandSampleAll(true);
			MagicWandTool wand = new MagicWandTool();
			wand.OnPressed(doc, 11, 11, state);
			Check(doc.Selection().IsSelected(11, 11), "wand sample-all selects composite blue");
			Check(doc.Selection().IsSelected(14, 14), "wand sample-all selects blue extent");
			Check(!doc.Selection().IsSelected(3, 3), "wand sample-all skips white");
			state.SetWandSampleAll(false);
			wand.OnPressed(doc, 11, 11, state);
			Check(doc.Selection().IsSelected(3, 3), "wand active-layer seed is white under blue");
		}

		private static SKBitmap BuildTestBitmap()
		{
			SKBitmap bitmap = new SKBitmap(16, 9, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < 9; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					byte alpha = 255;
					if (x > 10)
					{
						alpha = 128;
					}
					bitmap.SetPixel(x, y, new SKColor((byte)(x * 15), (byte)(y * 28), (byte)((x + y) * 9), alpha));
				}
			}
			for (int x = 0; x < 8; x++)
			{
				bitmap.SetPixel(x, 4, new SKColor(200, 50, 25, 255));
			}
			return bitmap;
		}

		private static void CompareBitmaps(SKBitmap expected, SKBitmap actual, string name)
		{
			if (actual == null)
			{
				Check(false, name + " decoded null");
				return;
			}
			if (expected.Width != actual.Width || expected.Height != actual.Height)
			{
				Check(false, name + " dimensions mismatch");
				return;
			}
			for (int y = 0; y < expected.Height; y++)
			{
				for (int x = 0; x < expected.Width; x++)
				{
					SKColor left = expected.GetPixel(x, y);
					SKColor right = actual.GetPixel(x, y);
					if (left != right)
					{
						Check(false, name + " pixel mismatch at " + x + "," + y + " " + left + " vs " + right);
						return;
					}
				}
			}
			Check(true, name + " pixels identical");
		}

		private static void TestTgaRoundTrip()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_verify");
			Directory.CreateDirectory(directory);
			SKBitmap source = BuildTestBitmap();
			string rawPath = Path.Combine(directory, "raw.tga");
			bool wroteRaw = TgaFile.Write(rawPath, source, false);
			Check(wroteRaw, "tga uncompressed write");
			SKBitmap rawBack = TgaFile.Read(rawPath);
			CompareBitmaps(source, rawBack, "tga uncompressed round-trip");
			string rlePath = Path.Combine(directory, "rle.tga");
			bool wroteRle = TgaFile.Write(rlePath, source, true);
			Check(wroteRle, "tga rle write");
			SKBitmap rleBack = TgaFile.Read(rlePath);
			CompareBitmaps(source, rleBack, "tga rle round-trip");
			long rawSize = new FileInfo(rawPath).Length;
			long rleSize = new FileInfo(rlePath).Length;
			Check(rleSize < rawSize, "tga rle smaller than raw (" + rleSize + " vs " + rawSize + ")");
		}

		private static void TestBitmuteRoundTrip()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_verify");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "project.bitmute");
			Document doc = new Document("roundtrip", 32, 24);
			Layer second = doc.AddLayer("Paint");
			second.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			second.SetOffset(5, 3);
			second.SetPixelCanvas(10, 10, new SKColor(255, 0, 0, 255));
			second.SetOpacity(128);
			second.SetBlendMode(eBlendMode.Multiply);
			Layer textLayer = doc.AddLayer("Words");
			textLayer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			textLayer.SetTextPosition(4, 6);
			textLayer.SetTextString("Hello");
			textLayer.SetTextStyle(24.0f, "Arial", true, false, new SKColor(10, 20, 30, 255), 1, 2);
			textLayer.SetTextCharacter(false, 30.0f, 5, 110, 90, 2, true, false, false);
			doc.SetActiveLayerIndex(1);
			doc.Selection().SelectRect(new SKRectI(2, 2, 12, 8));
			doc.Guides().AddVertical(7);
			doc.Guides().AddHorizontal(15);
			bool wrote = BitmuteFile.Write(path, doc);
			Check(wrote, "bitmute write");
			Document back = BitmuteFile.Read(path);
			if (back == null)
			{
				Check(false, "bitmute read returned null");
				return;
			}
			Check(back.Width() == 32 && back.Height() == 24, "bitmute dimensions");
			Check(back.Layers().Count == 3, "bitmute layer count");
			Layer backSecond = back.Layers()[1];
			Check(backSecond.Name() == "Paint", "bitmute layer name");
			Check(backSecond.OffsetX() == 5 && backSecond.OffsetY() == 3, "bitmute layer offset");
			Check(backSecond.Opacity() == 128, "bitmute layer opacity");
			Check(backSecond.BlendMode() == eBlendMode.Multiply, "bitmute layer blend mode");
			SKColor pixel = backSecond.GetPixelCanvas(10, 10);
			Check(pixel.Red == 255 && pixel.Alpha == 255, "bitmute layer pixel");
			Layer backText = back.Layers()[2];
			Check(backText.IsText(), "bitmute text flag");
			Check(backText.Text() == "Hello", "bitmute text string");
			Check(backText.TextBold(), "bitmute text bold");
			Check(backText.TextFontFamily() == "Arial", "bitmute text font");
			Check(back.ActiveLayer() == backSecond, "bitmute active layer");
			Check(back.Selection().IsActive(), "bitmute selection active");
			Check(back.Selection().IsSelected(5, 5), "bitmute selection inside");
			Check(!back.Selection().IsSelected(20, 20), "bitmute selection outside");
			Check(back.Guides().VerticalGuides().Count == 1 && back.Guides().VerticalGuides()[0] == 7, "bitmute guide vertical round-trip");
			Check(back.Guides().HorizontalGuides().Count == 1 && back.Guides().HorizontalGuides()[0] == 15, "bitmute guide horizontal round-trip");
		}

		private static void TestCropToRect()
		{
			Document doc = new Document("t", 32, 24);
			Layer layer = doc.ActiveLayer();
			layer.SetPixelCanvas(10, 9, new SKColor(255, 0, 0, 255));
			doc.CropToRect(new SKRectI(4, 4, 20, 16));
			Check(doc.Width() == 16 && doc.Height() == 12, "crop dimensions");
			SKColor moved = doc.ActiveLayer().GetPixelCanvas(6, 5);
			Check(moved.Red == 255 && moved.Green == 0, "crop pixel relocated");
			SKColor white = doc.ActiveLayer().GetPixelCanvas(0, 0);
			Check(white.Green == 255, "crop background intact");
		}

		private static void TestRotateTransform()
		{
			SKBitmap source = new SKBitmap(10, 6, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			source.Erase(new SKColor(0, 0, 255, 255));
			source.SetPixel(2, 1, new SKColor(255, 0, 0, 255));
			SKBitmap rotated90 = RotateTransform.Rotate(source, 90.0, 0);
			Check(rotated90.Width == 6 && rotated90.Height == 10, "rotate 90 dimensions");
			SKColor at = rotated90.GetPixel(4, 2);
			Check(at.Red == 255 && at.Green == 0 && at.Blue == 0, "rotate 90 pixel mapping");
			rotated90.Dispose();
			SKBitmap rotated45 = RotateTransform.Rotate(source, 45.0, 1);
			Check(rotated45.Width > 10 && rotated45.Height > 6, "rotate 45 expands");
			SKColor center = rotated45.GetPixel(rotated45.Width / 2, rotated45.Height / 2);
			Check(center.Blue > 200 && center.Alpha > 200, "rotate 45 center preserved");
			SKColor corner = rotated45.GetPixel(0, 0);
			Check(corner.Alpha == 0, "rotate 45 corner transparent");
			rotated45.Dispose();
			source.Dispose();
		}

		private static void TestSelectionStroke()
		{
			Document doc = new Document("t", 32, 32);
			doc.Selection().SelectRect(new SKRectI(8, 8, 24, 24));
			SelectionStroke.Apply(doc, new SKColor(255, 0, 0, 255), 2, 0);
			Layer layer = doc.ActiveLayer();
			Check(layer.GetPixelCanvas(8, 8).Red == 255 && layer.GetPixelCanvas(8, 8).Green == 0, "stroke inside boundary pixel");
			Check(layer.GetPixelCanvas(9, 12).Green == 0, "stroke inside second ring");
			Check(layer.GetPixelCanvas(12, 12).Green == 255, "stroke interior untouched");
			Check(layer.GetPixelCanvas(7, 7).Green == 255, "stroke outside untouched for inside mode");

			Document docCenter = new Document("t", 32, 32);
			docCenter.Selection().SelectRect(new SKRectI(8, 8, 24, 24));
			SelectionStroke.Apply(docCenter, new SKColor(0, 200, 0, 255), 2, 1);
			Layer centerLayer = docCenter.ActiveLayer();
			Check(centerLayer.GetPixelCanvas(8, 12).Red == 0, "stroke center on-boundary painted");
			Check(centerLayer.GetPixelCanvas(7, 12).Red == 0, "stroke center outside ring painted");
			Check(centerLayer.GetPixelCanvas(5, 12).Red == 255, "stroke center beyond band untouched");

			Document docOut = new Document("t", 32, 32);
			docOut.Selection().SelectRect(new SKRectI(8, 8, 24, 24));
			SelectionStroke.Apply(docOut, new SKColor(0, 0, 200, 255), 3, 2);
			Layer outLayer = docOut.ActiveLayer();
			Check(outLayer.GetPixelCanvas(7, 12).Blue == 200, "stroke outside ring painted");
			Check(outLayer.GetPixelCanvas(8, 12).Blue != 200, "stroke outside leaves selected pixels");
			Check(outLayer.GetPixelCanvas(3, 12).Blue != 200, "stroke outside beyond band untouched");
		}

		private static void TestWarpAffineScale()
		{
			SKBitmap source = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			source.Erase(new SKColor(0, 0, 255, 255));
			source.SetPixel(1, 1, new SKColor(255, 0, 0, 255));
			int outX;
			int outY;
			SKBitmap scaled = TransformMath.WarpAffine(source, 2.0f, 0.0f, 0.0f, 2.0f, 0.0f, 0.0f, 0, out outX, out outY);
			Check(scaled != null, "warp affine returns bitmap");
			Check(scaled.Width == 8 && scaled.Height == 8, "warp affine doubles size");
			Check(outX == 0 && outY == 0, "warp affine origin");
			SKColor center = scaled.GetPixel(3, 3);
			Check(center.Red == 255 && center.Blue == 0, "warp affine red block scaled");
			SKColor interior = scaled.GetPixel(6, 1);
			Check(interior.Blue == 255 && interior.Red == 0, "warp affine blue preserved");
			scaled.Dispose();
			source.Dispose();
		}

		private static void TestFlipTransformCommitUndo()
		{
			Document doc = new Document("t", 16, 8);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			layer.SetPixelCanvas(1, 4, new SKColor(255, 0, 0, 255));
			FreeTransformTool transform = new FreeTransformTool();
			bool armed = transform.Begin(doc, 6, new SKColor(255, 255, 255, 255));
			Check(armed, "flip transform arms and commits");
			SKColor mirrored = doc.ActiveLayer().GetPixelCanvas(14, 4);
			Check(mirrored.Red == 255 && mirrored.Alpha == 255, "flip transform mirrored pixel");
			SKColor original = doc.ActiveLayer().GetPixelCanvas(1, 4);
			Check(original.Alpha == 0, "flip transform cleared original");
			bool undone = doc.Undo();
			Check(undone, "flip transform undoable");
			SKColor restored = doc.ActiveLayer().GetPixelCanvas(1, 4);
			Check(restored.Red == 255 && restored.Alpha == 255, "flip transform undo restores");
		}

		private static void TestTransformScaleCommit()
		{
			Document doc = new Document("t", 8, 8);
			Layer layer = doc.ActiveLayer();
			layer.SetIsBackground(false);
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			for (int y = 2; y < 6; y++)
			{
				for (int x = 2; x < 6; x++)
				{
					layer.SetPixelCanvas(x, y, new SKColor(255, 0, 0, 255));
				}
			}
			FreeTransformTool transform = new FreeTransformTool();
			transform.SetPickRadius(2);
			bool armed = transform.Begin(doc, 1, new SKColor(255, 255, 255, 255));
			Check(armed, "transform scale arms");
			ToolState state = new ToolState();
			transform.OnPressed(doc, 8, 8, state);
			transform.OnDragged(doc, 16, 16, state);
			transform.OnReleased(doc, 16, 16, state);
			transform.Commit(doc);
			SKBitmap result = doc.ActiveLayer().Bitmap();
			Check(result.Width == 16 && result.Height == 16, "transform scale doubles layer bitmap");
			SKColor scaled = doc.ActiveLayer().GetPixelCanvas(8, 8);
			Check(scaled.Red > 150 && scaled.Green < 80, "transform scale keeps painted content");
			bool undone = doc.Undo();
			Check(undone, "transform scale undoable");
			Check(doc.ActiveLayer().Bitmap().Width == 8, "transform scale undo restores bitmap");
		}

		private static void TestTransformSelectionLiftCancel()
		{
			Document doc = new Document("t", 16, 16);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(255, 0, 0, 255));
			layer.SetIsBackground(false);
			doc.Selection().SelectRect(new SKRectI(4, 4, 8, 8));
			FreeTransformTool transform = new FreeTransformTool();
			bool armed = transform.Begin(doc, 1, new SKColor(255, 255, 255, 255));
			Check(armed, "transform selection arms");
			Check(doc.ActiveLayer().GetPixelCanvas(5, 5).Alpha == 0, "transform lifts selected pixels");
			Check(doc.ActiveLayer().GetPixelCanvas(0, 0).Red == 255, "transform leaves unselected pixels");
			Check(!doc.Selection().IsActive(), "transform clears live selection");
			transform.Cancel();
			Check(doc.ActiveLayer().GetPixelCanvas(5, 5).Red == 255, "transform cancel restores lifted pixels");
			Check(doc.Selection().IsActive(), "transform cancel restores selection");
		}

		private static void TestTransformBackgroundStaysCanvas()
		{
			Document doc = new Document("t", 8, 8);
			Layer layer = doc.ActiveLayer();
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					layer.SetPixelCanvas(x, y, new SKColor(20, 40, 60, 255));
				}
			}
			FreeTransformTool transform = new FreeTransformTool();
			transform.SetPickRadius(2);
			transform.Begin(doc, 1, new SKColor(200, 200, 200, 255));
			ToolState state = new ToolState();
			transform.OnPressed(doc, 8, 8, state);
			transform.OnDragged(doc, 4, 4, state);
			transform.OnReleased(doc, 4, 4, state);
			transform.Commit(doc);
			SKBitmap result = doc.ActiveLayer().Bitmap();
			Check(result.Width == 8 && result.Height == 8, "background transform keeps canvas size");
			Check(doc.ActiveLayer().IsBackground(), "background stays a background layer");
			SKColor exposed = doc.ActiveLayer().GetPixelCanvas(7, 7);
			Check(exposed.Alpha == 255, "background exposed area stays opaque");
		}

		private static void TestCanvasOpUndo()
		{
			Document doc = new Document("t", 12, 8);
			Layer layer = doc.ActiveLayer();
			layer.SetPixelCanvas(0, 0, new SKColor(255, 0, 0, 255));
			doc.BeginCanvasEdit("Rotate 90 CW");
			doc.Rotate90();
			doc.EndCanvasEdit();
			Check(doc.Width() == 8 && doc.Height() == 12, "rotate90 swaps dimensions");
			bool undone = doc.Undo();
			Check(undone, "rotate90 undoable");
			Check(doc.Width() == 12 && doc.Height() == 8, "rotate90 undo restores dimensions");
			SKColor restored = doc.ActiveLayer().GetPixelCanvas(0, 0);
			Check(restored.Red == 255, "rotate90 undo restores pixel");
			bool redone = doc.Redo();
			Check(redone, "rotate90 redoable");
			Check(doc.Width() == 8 && doc.Height() == 12, "rotate90 redo re-applies");
		}

		private static void TestGuidesModel()
		{
			Guides guides = new Guides();
			guides.AddVertical(10);
			guides.AddVertical(10);
			guides.AddHorizontal(20);
			Check(guides.VerticalGuides().Count == 1, "guides dedupe vertical");
			Check(guides.HorizontalGuides().Count == 1, "guides add horizontal");
			Check(guides.HitVertical(12, 3) == 0, "guides hit vertical in tolerance");
			Check(guides.HitVertical(30, 3) == -1, "guides hit vertical miss");
			Check(guides.SnapX(11, 3) == 10, "guides snap x");
			Check(guides.SnapX(30, 3) == 30, "guides snap x passthrough");
			guides.MoveVertical(0, 40);
			Check(guides.VerticalGuides()[0] == 40, "guides move vertical");
			guides.SetLocked(true);
			guides.AddVertical(99);
			Check(guides.VerticalGuides().Count == 1, "guides locked blocks add");
			guides.MoveVertical(0, 5);
			Check(guides.VerticalGuides()[0] == 40, "guides locked blocks move");
			guides.SetLocked(false);
			guides.RemoveVertical(0);
			Check(guides.VerticalGuides().Count == 0, "guides remove vertical");
		}

		private static void TestCompositeRangeInto()
		{
			Document doc = new Document("t", 4, 4);
			doc.ActiveLayer().Bitmap().Erase(new SKColor(255, 0, 0, 255));
			Layer mid = doc.AddLayer("mid");
			mid.Bitmap().Erase(new SKColor(0, 255, 0, 255));
			Layer top = doc.AddLayer("top");
			top.Bitmap().Erase(new SKColor(0, 0, 255, 0));
			SKBitmap target = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Premul);
			target.Erase(SKColors.Transparent);
			doc.CompositeRangeInto(target, new SKRectI(0, 0, 4, 4), 1, 3);
			SKColor px = target.GetPixel(1, 1);
			Check(px.Green == 255 && px.Red == 0 && px.Blue == 0, "range composite excludes below-range layer");
			Check(px.Alpha == 255, "range composite alpha from in-range layer");
			SKBitmap targetTop = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Premul);
			targetTop.Erase(SKColors.Transparent);
			doc.CompositeRangeInto(targetTop, new SKRectI(0, 0, 4, 4), 2, 3);
			SKColor pxTop = targetTop.GetPixel(1, 1);
			Check(pxTop.Alpha == 0, "range composite of transparent-only range is empty");
			target.Dispose();
			targetTop.Dispose();
		}

		private static void TestMixedComposite()
		{
			Document doc = new Document("t", 4, 4);
			Layer mid = doc.AddLayer("mid");
			mid.Bitmap().Erase(new SKColor(100, 100, 100, 255));
			mid.SetBlendMode(eBlendMode.Multiply);
			Layer top = doc.AddLayer("top");
			top.Bitmap().Erase(new SKColor(200, 200, 200, 255));
			top.SetBlendMode(eBlendMode.Divide);
			byte expectedR;
			byte expectedG;
			byte expectedB;
			BlendModes.Blend(eBlendMode.Divide, 100, 100, 100, 200, 200, 200, out expectedR, out expectedG, out expectedB);
			SKBitmap target = CompositeDoc(doc);
			SKColor actual = target.GetPixel(1, 1);
			CheckNear(actual.Red, expectedR, 2, "mixed native+custom red");
			CheckNear(actual.Green, expectedG, 2, "mixed native+custom green");
			CheckNear(actual.Blue, expectedB, 2, "mixed native+custom blue");
			Check(actual.Alpha == 255, "mixed native+custom alpha");
			target.Dispose();
		}
	}
}
