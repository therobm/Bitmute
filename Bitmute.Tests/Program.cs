using System;
using System.Collections.Generic;
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
			TestFillLayer();
			TestSelectionMoveLayer();
			TestCustomBlends();
			TestCustomBlendOpacity();
			TestCustomBlendTransparentBase();
			TestMixedComposite();
			TestCompositeRangeInto();
			TestGuideStickyCenter();
			TestMoveSnapToGuides();
			TestMoveSnapTargets();
			TestMovePerfRegion();
			TestSpongeMath();
			TestColorReplaceMath();
			TestGradientFill();
			TestHealMath();
			TestCloneAligned();
			TestBlurStrength();
			TestBrushHardnessSmall();
			TestGaussianBlur();
			TestGaussianBlurAlpha();
			TestLayerMerging();
			TestChannelVisibilityMask();
			TestDodgeBurnRange();
			TestSlices();
			TestChannelRender();
			TestWandContiguous();
			TestWandNonContiguous();
			TestWandSampleAll();
			TestTgaRoundTrip();
			TestBitmuteRoundTrip();
			TestLayerStyles();
			TestStyledComposite();
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
			second.SetLockAlpha(true);
			second.SetLockPosition(true);
			LayerStyle secondStyle = new LayerStyle();
			secondStyle.m_hasStroke = true;
			secondStyle.m_strokeSize = 7;
			secondStyle.m_strokePosition = 0;
			secondStyle.m_strokeColor = new SKColor(10, 20, 30, 255);
			secondStyle.m_hasDropShadow = true;
			secondStyle.m_shadowOpacity = 66;
			secondStyle.m_shadowAngle = 120;
			secondStyle.m_shadowDistance = 9;
			secondStyle.m_shadowSize = 4;
			secondStyle.m_hasOuterGlow = true;
			secondStyle.m_glowSize = 8;
			second.SetLayerStyle(secondStyle);
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
			doc.Slices().Add("Slice 1", new SKRectI(3, 4, 12, 10));
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
			Check(backSecond.LockAlpha() && backSecond.LockPosition(), "bitmute layer lock flags round-trip");
			Check(backSecond.PaintLocked() == false && backSecond.MoveLocked(), "bitmute layer lock semantics");
			LayerStyle backStyle = backSecond.LayerStyle();
			Check(backStyle.m_hasStroke && backStyle.m_strokeSize == 7 && backStyle.m_strokePosition == 0, "bitmute layer style stroke round-trip");
			Check(backStyle.m_strokeColor.Red == 10 && backStyle.m_strokeColor.Green == 20 && backStyle.m_strokeColor.Blue == 30, "bitmute layer style stroke color round-trip");
			Check(backStyle.m_hasDropShadow && backStyle.m_shadowOpacity == 66 && backStyle.m_shadowAngle == 120 && backStyle.m_shadowDistance == 9 && backStyle.m_shadowSize == 4, "bitmute layer style shadow round-trip");
			Check(backStyle.m_hasOuterGlow && backStyle.m_glowSize == 8, "bitmute layer style glow round-trip");
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
			Check(back.Slices().Count() == 1 && back.Slices().NameAt(0) == "Slice 1", "bitmute slice name round-trip");
			SKRectI backSlice = back.Slices().RectAt(0);
			Check(backSlice.Left == 3 && backSlice.Top == 4 && backSlice.Right == 12 && backSlice.Bottom == 10, "bitmute slice rect round-trip");
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

		private static void TestSpongeMath()
		{
			byte r;
			byte g;
			byte b;
			SpongeMath.Apply(255, 0, 0, false, 1.0, out r, out g, out b);
			Check(r == g && g == b, "sponge full desaturate to gray");
			byte r0;
			byte g0;
			byte b0;
			SpongeMath.Apply(200, 50, 40, false, 0.0, out r0, out g0, out b0);
			Check(r0 == 200 && g0 == 50 && b0 == 40, "sponge zero strength unchanged");
			byte rs;
			byte gs;
			byte bs;
			SpongeMath.Apply(128, 100, 100, true, 1.0, out rs, out gs, out bs);
			Check(rs > 128 && gs < 100, "sponge saturate pushes channels apart");
		}

		private static void TestColorReplaceMath()
		{
			byte r0;
			byte g0;
			byte b0;
			ColorReplaceMath.Apply(128, 128, 128, 255, 0, 0, 0, 0.0, out r0, out g0, out b0);
			Check(r0 == 128 && g0 == 128 && b0 == 128, "color replace zero strength unchanged");
			byte rc;
			byte gc;
			byte bc;
			ColorReplaceMath.Apply(128, 128, 128, 255, 0, 0, 0, 1.0, out rc, out gc, out bc);
			Check(rc > gc && rc > bc, "color replace color mode is reddish");
			byte rl;
			byte gl;
			byte bl;
			ColorReplaceMath.Apply(200, 200, 200, 255, 0, 0, 3, 1.0, out rl, out gl, out bl);
			Check(rl == gl && gl == bl, "color replace luminosity stays gray");
			Check(rl < 200, "color replace luminosity uses fg luma");
		}

		private static void TestHealMath()
		{
			byte r;
			byte g;
			byte b;
			HealMath.Apply(200, 200, 200, 180.0, 180.0, 180.0, 100.0, 100.0, 100.0, 1.0, 100, 100, 100, out r, out g, out b);
			Check(r == 120 && g == 120 && b == 120, "heal transfers source detail onto dest color");
			byte r0;
			byte g0;
			byte b0;
			HealMath.Apply(200, 200, 200, 180.0, 180.0, 180.0, 100.0, 100.0, 100.0, 0.0, 100, 100, 100, out r0, out g0, out b0);
			Check(r0 == 100 && g0 == 100 && b0 == 100, "heal zero strength keeps destination");
		}

		private static void TestSlices()
		{
			Slices slices = new Slices();
			slices.Add("A", new SKRectI(2, 2, 10, 10));
			slices.Add("B", new SKRectI(20, 20, 30, 30));
			slices.Add("bad", new SKRectI(5, 5, 5, 5));
			Check(slices.Count() == 2, "slices ignores zero-area rect");
			Check(slices.NameAt(1) == "B", "slices name at index");
			Check(slices.HitTest(25, 25) == 1, "slices hit test topmost");
			Check(slices.HitTest(50, 50) == -1, "slices hit test miss");
			int generationBefore = slices.Generation();
			slices.RemoveAt(0);
			Check(slices.Count() == 1 && slices.NameAt(0) == "B", "slices remove");
			Check(slices.Generation() > generationBefore, "slices generation bumps");
		}

		private static void TestGradientFill()
		{
			SKBitmap bmp = new SKBitmap(10, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKColor red = new SKColor(255, 0, 0, 255);
			SKColor blue = new SKColor(0, 0, 255, 255);
			GradientFill.Fill(bmp, eGradientType.Linear, 0.0f, 0.0f, 9.0f, 0.0f, red, blue, false);
			SKColor left = bmp.GetPixel(0, 0);
			SKColor right = bmp.GetPixel(9, 0);
			Check(left.Red > right.Red, "gradient linear left redder");
			Check(right.Blue > left.Blue, "gradient linear right bluer");
			GradientFill.Fill(bmp, eGradientType.Linear, 0.0f, 0.0f, 9.0f, 0.0f, red, blue, true);
			SKColor leftReversed = bmp.GetPixel(0, 0);
			Check(leftReversed.Blue > leftReversed.Red, "gradient reverse flips ends");
			bmp.Dispose();
		}

		private static void TestChannelRender()
		{
			SKBitmap source = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
			source.SetPixel(0, 0, new SKColor(255, 0, 0, 255));
			source.SetPixel(1, 0, new SKColor(0, 0, 0, 0));
			SKBitmap target = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			ChannelRender.Render(source, target, 0);
			SKColor redChannel = target.GetPixel(0, 0);
			Check(redChannel.Red == 255 && redChannel.Green == 255 && redChannel.Blue == 255, "channel red of opaque red is white");
			ChannelRender.Render(source, target, 1);
			SKColor greenChannel = target.GetPixel(0, 0);
			Check(greenChannel.Red == 0, "channel green of red pixel is black");
			ChannelRender.Render(source, target, 3);
			SKColor alphaOpaque = target.GetPixel(0, 0);
			SKColor alphaTransparent = target.GetPixel(1, 0);
			Check(alphaOpaque.Red == 255, "channel alpha of opaque is white");
			Check(alphaTransparent.Red == 0, "channel alpha of transparent is black");
			source.Dispose();
			target.Dispose();
		}

		private static void TestMovePerfRegion()
		{
			Document doc = new Document("t", 64, 48);
			Layer content = doc.AddLayer("c");
			SKColor mark = new SKColor(255, 0, 0, 255);
			for (int y = 5; y < 15; y++)
			{
				for (int x = 5; x < 15; x++)
				{
					content.Bitmap().SetPixel(x, y, mark);
				}
			}
			SKBitmap before = new SKBitmap(64, 48, SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(before);
			MoveTool move = new MoveTool();
			ToolState state = new ToolState();
			move.OnPressed(doc, 20, 20, state);
			move.OnDragged(doc, 28, 25, state);
			SKRectI dirty = doc.ComposeDirtyRect();
			Check(dirty.Width > 0 && dirty.Height > 0, "move marks a bounded dirty region");
			Check(dirty.Width < 64 || dirty.Height < 48, "move dirty region is smaller than full canvas");
			SKBitmap full = new SKBitmap(64, 48, SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(full);
			SKBitmap region = before.Copy();
			doc.CompositeRegion(region, dirty);
			bool match = true;
			for (int y = 0; y < 48; y++)
			{
				for (int x = 0; x < 64; x++)
				{
					if (full.GetPixel(x, y) != region.GetPixel(x, y))
					{
						match = false;
					}
				}
			}
			Check(match, "move region composite matches full recomposite");
			before.Dispose();
			full.Dispose();
			region.Dispose();
		}

		private static void TestMoveSnapToGuides()
		{
			SKColor mark = new SKColor(255, 0, 0, 255);
			Document doc = new Document("t", 64, 48);
			doc.Guides().AddVertical(20);
			Layer content = doc.AddLayer("c");
			for (int y = 4; y < 14; y++)
			{
				for (int x = 5; x < 15; x++)
				{
					content.Bitmap().SetPixel(x, y, mark);
				}
			}
			ToolState state = new ToolState();
			state.SetSnapToGuides(true);
			state.SetSnapTolerance(6);
			MoveTool move = new MoveTool();
			move.OnPressed(doc, 30, 20, state);
			move.OnDragged(doc, 33, 20, state);
			Check(content.OffsetX() == 5, "move snaps content right edge to guide (offset 5)");
			Check(content.OffsetY() == 0, "move no vertical snap without horizontal guide");
			Document docOff = new Document("t", 64, 48);
			docOff.Guides().AddVertical(20);
			Layer contentOff = docOff.AddLayer("c");
			for (int y = 4; y < 14; y++)
			{
				for (int x = 5; x < 15; x++)
				{
					contentOff.Bitmap().SetPixel(x, y, mark);
				}
			}
			ToolState stateOff = new ToolState();
			stateOff.SetSnapToGuides(false);
			MoveTool moveOff = new MoveTool();
			moveOff.OnPressed(docOff, 30, 20, stateOff);
			moveOff.OnDragged(docOff, 33, 20, stateOff);
			Check(contentOff.OffsetX() == 3, "no snap keeps raw delta (offset 3)");
		}

		private static Layer AddMarkedContent(Document doc)
		{
			SKColor mark = new SKColor(255, 0, 0, 255);
			Layer content = doc.AddLayer("c");
			for (int y = 4; y < 14; y++)
			{
				for (int x = 5; x < 15; x++)
				{
					content.Bitmap().SetPixel(x, y, mark);
				}
			}
			return content;
		}

		private static void TestMoveSnapTargets()
		{
			Document gridDoc = new Document("t", 64, 48);
			Layer gridContent = AddMarkedContent(gridDoc);
			ToolState gridState = new ToolState();
			gridState.SetSnapGrid(true);
			gridState.SetSnapGridSize(16);
			gridState.SetSnapTolerance(6);
			MoveTool gridMove = new MoveTool();
			gridMove.OnPressed(gridDoc, 30, 20, gridState);
			gridMove.OnDragged(gridDoc, 33, 20, gridState);
			Check(gridContent.OffsetX() == 1, "move snaps right edge to grid line (offset 1)");

			Document edgeDoc = new Document("t", 64, 48);
			Layer edgeContent = AddMarkedContent(edgeDoc);
			ToolState edgeState = new ToolState();
			edgeState.SetSnapEdges(true);
			edgeState.SetSnapTolerance(6);
			MoveTool edgeMove = new MoveTool();
			edgeMove.OnPressed(edgeDoc, 30, 20, edgeState);
			edgeMove.OnDragged(edgeDoc, 23, 20, edgeState);
			Check(edgeContent.OffsetX() == -5, "move snaps left edge to canvas edge (offset -5)");
		}

		private static void FillPositionGradient(Layer layer)
		{
			SKBitmap bitmap = layer.Bitmap();
			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					bitmap.SetPixel(x, y, new SKColor((byte)x, 0, 0, 255));
				}
			}
		}

		private static void CloneStroke(CloneTool clone, Document doc, int x, int y, ToolState state)
		{
			doc.BeginStroke();
			clone.OnPressed(doc, x, y, state);
			clone.OnReleased(doc, x, y, state);
			doc.EndStroke();
		}

		private static void TestCloneAligned()
		{
			Document doc = new Document("t", 64, 64);
			FillPositionGradient(doc.ActiveLayer());
			ToolState state = new ToolState();
			state.SetBrushSize(1);
			state.SetCloneAligned(true);
			CloneTool clone = new CloneTool();
			state.SetAltHeld(true);
			clone.OnPressed(doc, 10, 10, state);
			state.SetAltHeld(false);
			CloneStroke(clone, doc, 30, 10, state);
			CloneStroke(clone, doc, 40, 10, state);
			SKColor aligned = doc.ActiveLayer().GetPixelCanvas(40, 10);
			CheckNear(aligned.Red, 20, 1, "clone aligned stroke 2 samples shifted source");

			Document doc2 = new Document("t", 64, 64);
			FillPositionGradient(doc2.ActiveLayer());
			ToolState state2 = new ToolState();
			state2.SetBrushSize(1);
			state2.SetCloneAligned(false);
			CloneTool clone2 = new CloneTool();
			state2.SetAltHeld(true);
			clone2.OnPressed(doc2, 10, 10, state2);
			state2.SetAltHeld(false);
			CloneStroke(clone2, doc2, 30, 10, state2);
			CloneStroke(clone2, doc2, 40, 10, state2);
			SKColor unaligned = doc2.ActiveLayer().GetPixelCanvas(40, 10);
			CheckNear(unaligned.Red, 10, 1, "clone non-aligned stroke 2 re-anchors to source");
		}

		private static int BlurCenterRed(int strength)
		{
			Document doc = new Document("t", 32, 32);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			layer.Bitmap().SetPixel(16, 16, new SKColor(0, 0, 0, 255));
			ToolState state = new ToolState();
			state.SetBrushSize(12);
			state.SetBrushStrength(strength);
			BlurTool blur = new BlurTool();
			doc.BeginStroke();
			blur.OnPressed(doc, 16, 16, state);
			blur.OnReleased(doc, 16, 16, state);
			doc.EndStroke();
			return layer.GetPixelCanvas(16, 16).Red;
		}

		private static void TestBlurStrength()
		{
			int full = BlurCenterRed(100);
			int half = BlurCenterRed(50);
			CheckNear(full, 245, 4, "blur strength 100 lerps center fully to neighborhood average");
			CheckNear(half, 122, 4, "blur strength 50 lerps center halfway to neighborhood average");
		}

		private static int BrushEdgeRed(int hardness)
		{
			Document doc = new Document("t", 32, 32);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			ToolState state = new ToolState();
			state.SetBrushSize(4);
			state.SetBrushHardness(hardness);
			state.SetForeground(new SKColor(0, 0, 0, 255));
			BrushTool brush = new BrushTool();
			doc.BeginStroke();
			brush.OnPressed(doc, 16, 16, state);
			brush.OnReleased(doc, 16, 16, state);
			doc.EndStroke();
			return layer.GetPixelCanvas(17, 16).Red;
		}

		private static void TestBrushHardnessSmall()
		{
			int hard = BrushEdgeRed(100);
			int soft = BrushEdgeRed(0);
			Check(hard < 40, "small hard brush edge pixel is nearly opaque (was inert before)");
			Check(soft - hard > 25, "small brush hardness meaningfully changes edge coverage");
		}

		private static void TestGaussianBlur()
		{
			SKBitmap bmp = new SKBitmap(32, 32, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < 32; y++)
			{
				for (int x = 0; x < 32; x++)
				{
					if (x < 16)
					{
						bmp.SetPixel(x, y, new SKColor(0, 0, 0, 255));
					}
					else
					{
						bmp.SetPixel(x, y, new SKColor(255, 255, 255, 255));
					}
				}
			}
			Adjustments.GaussianBlur(bmp, 5);
			SKColor leftEdge = bmp.GetPixel(15, 16);
			SKColor rightEdge = bmp.GetPixel(16, 16);
			SKColor farLeft = bmp.GetPixel(0, 16);
			SKColor farRight = bmp.GetPixel(31, 16);
			Check(leftEdge.Red > 15, "gaussian blur lightens the dark side of an edge");
			Check(rightEdge.Red < 240, "gaussian blur darkens the light side of an edge");
			Check(farLeft.Red < 60, "gaussian blur leaves far dark region mostly dark");
			Check(farRight.Red > 195, "gaussian blur leaves far light region mostly light");
			Check(rightEdge.Alpha == 255, "gaussian blur preserves alpha");
			bmp.Dispose();

			SKBitmap uniform = new SKBitmap(16, 16, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			uniform.Erase(new SKColor(100, 150, 200, 255));
			Adjustments.GaussianBlur(uniform, 4);
			SKColor flat = uniform.GetPixel(8, 8);
			CheckNear(flat.Red, 100, 2, "gaussian blur leaves a flat field unchanged (red)");
			CheckNear(flat.Green, 150, 2, "gaussian blur leaves a flat field unchanged (green)");
			CheckNear(flat.Blue, 200, 2, "gaussian blur leaves a flat field unchanged (blue)");
			Check(flat.Alpha == 255, "gaussian blur flat field keeps alpha");
			uniform.Dispose();
		}

		private static void TestLayerMerging()
		{
			Document mergeVisibleDoc = new Document("t", 8, 8);
			mergeVisibleDoc.ActiveLayer().Bitmap().Erase(new SKColor(200, 0, 0, 255));
			Layer visibleMid = mergeVisibleDoc.AddLayer("mid");
			visibleMid.Bitmap().Erase(SKColors.Transparent);
			visibleMid.Bitmap().SetPixel(2, 2, new SKColor(0, 200, 0, 255));
			Layer visibleTop = mergeVisibleDoc.AddLayer("top");
			visibleTop.Bitmap().Erase(SKColors.Transparent);
			visibleTop.Bitmap().SetPixel(4, 4, new SKColor(0, 0, 200, 255));
			mergeVisibleDoc.MergeVisible();
			Check(mergeVisibleDoc.Layers().Count == 1, "merge visible collapses to one layer");
			Layer flattened = mergeVisibleDoc.ActiveLayer();
			Check(flattened.GetPixelCanvas(0, 0).Red > 180, "merge visible keeps background where nothing overlays");
			Check(flattened.GetPixelCanvas(2, 2).Green > 180, "merge visible keeps green from the middle layer");
			Check(flattened.GetPixelCanvas(4, 4).Blue > 180, "merge visible keeps blue from the top layer");

			Document mergeSelectedDoc = new Document("t", 8, 8);
			mergeSelectedDoc.ActiveLayer().Bitmap().Erase(new SKColor(200, 0, 0, 255));
			Layer selectedA = mergeSelectedDoc.AddLayer("a");
			selectedA.Bitmap().Erase(SKColors.Transparent);
			selectedA.Bitmap().SetPixel(1, 1, new SKColor(0, 200, 0, 255));
			Layer selectedB = mergeSelectedDoc.AddLayer("b");
			selectedB.Bitmap().Erase(SKColors.Transparent);
			selectedB.Bitmap().SetPixel(2, 2, new SKColor(0, 0, 200, 255));
			List<int> selection = new List<int>();
			selection.Add(1);
			selection.Add(2);
			mergeSelectedDoc.MergeLayers(selection);
			Check(mergeSelectedDoc.Layers().Count == 2, "merge selected combines two layers, keeps the background");
			Check(mergeSelectedDoc.Layers()[0].GetPixelCanvas(0, 0).Red > 180, "merge selected leaves the background layer untouched");
			Layer selectedMerged = mergeSelectedDoc.Layers()[1];
			Check(selectedMerged.GetPixelCanvas(1, 1).Green > 180, "merge selected holds green from layer a");
			Check(selectedMerged.GetPixelCanvas(2, 2).Blue > 180, "merge selected holds blue from layer b");
			Check(selectedMerged.GetPixelCanvas(0, 0).Alpha == 0, "merge selected stays transparent where neither layer had pixels");
		}

		private static void TestChannelVisibilityMask()
		{
			SKBitmap source = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
			source.SetPixel(0, 0, new SKColor(100, 150, 200, 255));
			source.SetPixel(1, 0, new SKColor(200, 100, 50, 128));

			SKBitmap hideGreen = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			ChannelRender.ApplyVisibilityMask(source, hideGreen, true, false, true, true);
			SKColor opaque = hideGreen.GetPixel(0, 0);
			Check(opaque.Red == 100, "channel mask keeps red when shown");
			Check(opaque.Green == 0, "channel mask zeroes green when hidden");
			Check(opaque.Blue == 200, "channel mask keeps blue when shown");
			Check(opaque.Alpha == 255, "channel mask keeps opaque alpha");

			SKBitmap hideAlpha = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			ChannelRender.ApplyVisibilityMask(source, hideAlpha, true, true, true, false);
			SKColor semi = hideAlpha.GetPixel(1, 0);
			Check(semi.Alpha == 255, "channel mask forces opaque when alpha hidden");
			CheckNear(semi.Red, 200, 3, "channel mask un-premultiplies red of a semi-transparent pixel");
		}

		private static void TestGaussianBlurAlpha()
		{
			SKBitmap bmp = new SKBitmap(16, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int x = 0; x < 16; x++)
			{
				if (x < 8)
				{
					bmp.SetPixel(x, 0, new SKColor(255, 0, 0, 255));
				}
				else
				{
					bmp.SetPixel(x, 0, new SKColor(0, 0, 0, 0));
				}
			}
			Adjustments.GaussianBlur(bmp, 3);
			SKColor edge = bmp.GetPixel(8, 0);
			SKColor farClear = bmp.GetPixel(15, 0);
			Check(edge.Alpha > 0 && edge.Alpha < 255, "gaussian blur feathers a hard alpha edge into partial transparency");
			Check(farClear.Alpha < 40, "gaussian blur leaves the far transparent region transparent");
			Check(edge.Red > 180, "gaussian blur keeps edge color saturated (premultiplied, no dark fringe)");
			bmp.Dispose();
		}

		private static int BurnCenterRed(int startValue, int range, int exposure)
		{
			Document doc = new Document("t", 32, 32);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor((byte)startValue, (byte)startValue, (byte)startValue, 255));
			ToolState state = new ToolState();
			state.SetBrushSize(6);
			state.SetAltHeld(true);
			state.SetDodgeBurnRange(range);
			state.SetDodgeBurnExposure(exposure);
			DodgeBurnTool tool = new DodgeBurnTool();
			doc.BeginStroke();
			tool.OnPressed(doc, 16, 16, state);
			tool.OnReleased(doc, 16, 16, state);
			doc.EndStroke();
			return layer.GetPixelCanvas(16, 16).Red;
		}

		private static void TestDodgeBurnRange()
		{
			int shadowsDark = BurnCenterRed(40, 0, 50);
			int highlightsDark = BurnCenterRed(40, 2, 50);
			Check(shadowsDark < 32, "burn shadows range darkens a dark pixel");
			Check(highlightsDark > 38, "burn highlights range barely touches a dark pixel");
			int exposureHalf = BurnCenterRed(40, 0, 50);
			int exposureFull = BurnCenterRed(40, 0, 100);
			Check(exposureFull < exposureHalf, "burn exposure 100 darkens more than exposure 50");
		}

		private static void TestLayerStyles()
		{
			SKBitmap source = new SKBitmap(16, 16, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			source.Erase(SKColors.Transparent);
			SKColor opaque = new SKColor(200, 40, 40, 255);
			for (int y = 4; y < 12; y++)
			{
				for (int x = 4; x < 12; x++)
				{
					source.SetPixel(x, y, opaque);
				}
			}

			int strokePlaceX;
			int strokePlaceY;
			SKColor blue = new SKColor(0, 0, 255, 255);
			SKBitmap stroke = LayerStyles.RenderStroke(source, 2, 2, blue, out strokePlaceX, out strokePlaceY);
			Check(strokePlaceX == -2 && strokePlaceY == -2, "stroke placement offset");
			SKColor strokeBand = stroke.GetPixel(4, 9);
			Check(strokeBand.Blue == 255 && strokeBand.Alpha == 255, "outside stroke band is opaque color");
			SKColor strokeInside = stroke.GetPixel(9, 9);
			Check(strokeInside.Alpha == 0, "outside stroke leaves interior clear");
			SKColor strokeFar = stroke.GetPixel(0, 0);
			Check(strokeFar.Alpha == 0, "outside stroke far corner is clear");
			stroke.Dispose();

			int shadowPlaceX;
			int shadowPlaceY;
			SKColor black = new SKColor(0, 0, 0, 255);
			SKBitmap shadow = LayerStyles.RenderDropShadow(source, black, 4, 4, 0, 255, out shadowPlaceX, out shadowPlaceY);
			Check(shadowPlaceX == 0 && shadowPlaceY == 0, "shadow placement offset");
			SKColor shadowPixel = shadow.GetPixel(10, 10);
			Check(shadowPixel.Alpha == 255 && shadowPixel.Red == 0 && shadowPixel.Green == 0 && shadowPixel.Blue == 0, "drop shadow is offset opaque black");
			SKColor shadowClear = shadow.GetPixel(2, 2);
			Check(shadowClear.Alpha == 0, "drop shadow origin area is clear");
			shadow.Dispose();

			int glowPlaceX;
			int glowPlaceY;
			SKColor yellow = new SKColor(255, 255, 0, 255);
			SKBitmap glow = LayerStyles.RenderOuterGlow(source, yellow, 3, 255, out glowPlaceX, out glowPlaceY);
			Check(glowPlaceX == -3 && glowPlaceY == -3, "glow placement offset");
			SKColor glowHalo = glow.GetPixel(6, 10);
			Check(glowHalo.Alpha > 0 && glowHalo.Red == 255 && glowHalo.Green == 255 && glowHalo.Blue == 0, "outer glow halo present in glow color");
			glow.Dispose();
			source.Dispose();
		}

		private static void TestSelectionMoveLayer()
		{
			Document doc = new Document("t", 64, 48);
			Layer content = doc.AddLayer("c");
			SKColor red = new SKColor(255, 0, 0, 255);
			for (int y = 10; y < 30; y++)
			{
				for (int x = 10; x < 30; x++)
				{
					content.Bitmap().SetPixel(x, y, red);
				}
			}
			doc.Selection().SelectRect(new SKRectI(15, 15, 25, 25));
			ToolState state = new ToolState();
			MoveTool move = new MoveTool();
			move.OnPressed(doc, 20, 20, state);
			move.OnDragged(doc, 40, 20, state);
			move.OnReleased(doc, 40, 20, state);
			SKColor moved = content.GetPixelCanvas(40, 20);
			Check(moved.Red == 255 && moved.Alpha == 255, "selection move on a layer keeps moved pixels (40,20)");
			SKColor movedEdge = content.GetPixelCanvas(44, 20);
			Check(movedEdge.Red == 255 && movedEdge.Alpha == 255, "selection move on a layer keeps moved pixels at far edge (44,20)");
			SKColor vacated = content.GetPixelCanvas(20, 20);
			Check(vacated.Alpha == 0, "selection move on a layer vacates the origin (20,20)");
			SKColor unselected = content.GetPixelCanvas(12, 20);
			Check(unselected.Red == 255 && unselected.Alpha == 255, "selection move on a layer leaves unselected pixels (12,20)");
			Check(doc.Selection().IsSelected(40, 20), "selection mask follows the move (40,20 selected)");
			Check(!doc.Selection().IsSelected(20, 20), "selection mask leaves the origin (20,20 unselected)");
		}

		private static void TestFillLayer()
		{
			Document doc = new Document("t", 16, 16);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			doc.BeginStroke();
			doc.FillLayer(new SKColor(255, 0, 0, 255));
			doc.EndStroke();
			SKColor corner = layer.GetPixelCanvas(0, 0);
			SKColor center = layer.GetPixelCanvas(8, 8);
			Check(corner.Red == 255 && corner.Alpha == 255, "fill layer covers corner");
			Check(center.Red == 255 && center.Alpha == 255, "fill layer covers center");
		}

		private static void TestStyledComposite()
		{
			Document doc = new Document("t", 32, 32);
			Layer layer = doc.AddLayer("c");
			SKColor red = new SKColor(255, 0, 0, 255);
			for (int y = 12; y < 20; y++)
			{
				for (int x = 12; x < 20; x++)
				{
					layer.Bitmap().SetPixel(x, y, red);
				}
			}
			LayerStyle style = new LayerStyle();
			style.m_hasStroke = true;
			style.m_strokeSize = 2;
			style.m_strokePosition = 2;
			style.m_strokeColor = new SKColor(0, 0, 255, 255);
			layer.SetLayerStyle(style);
			SKBitmap composite = new SKBitmap(32, 32, SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(composite);
			SKColor strokePixel = composite.GetPixel(11, 15);
			Check(strokePixel.Blue == 255 && strokePixel.Alpha == 255, "styled composite draws outside stroke around layer");
			SKColor interior = composite.GetPixel(15, 15);
			Check(interior.Red == 255 && interior.Blue == 0, "styled composite keeps layer body");
			composite.Dispose();
		}

		private static void TestGuideStickyCenter()
		{
			Document doc = new Document("t", 40, 20);
			SKRectI backgroundBox;
			bool backgroundIsBg;
			bool backgroundValid = doc.ActiveLayerContentBox(out backgroundBox, out backgroundIsBg);
			Check(backgroundValid, "sticky box valid on background");
			Check(backgroundIsBg, "background flagged as background");
			Check(backgroundBox.Left == 0 && backgroundBox.Top == 0 && backgroundBox.Right == 40 && backgroundBox.Bottom == 20, "background box is full canvas");
			Layer content = doc.AddLayer("content");
			SKColor mark = new SKColor(255, 0, 0, 255);
			for (int y = 4; y < 12; y++)
			{
				for (int x = 10; x < 20; x++)
				{
					content.Bitmap().SetPixel(x, y, mark);
				}
			}
			SKRectI contentBox;
			bool contentIsBg;
			bool contentValid = doc.ActiveLayerContentBox(out contentBox, out contentIsBg);
			Check(contentValid, "sticky box valid on written content");
			Check(!contentIsBg, "content layer not flagged background");
			Check(contentBox.Left == 10 && contentBox.Top == 4 && contentBox.Right == 20 && contentBox.Bottom == 12, "content box is written-pixel AABB");
			Layer empty = doc.AddLayer("empty");
			SKRectI emptyBox;
			bool emptyIsBg;
			bool emptyValid = doc.ActiveLayerContentBox(out emptyBox, out emptyIsBg);
			Check(!emptyValid, "sticky box absent on empty layer");
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
