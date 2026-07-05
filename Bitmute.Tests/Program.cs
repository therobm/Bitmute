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
			TestFloatingSelection();
			TestFloatingSelectionMultiGrab();
			TestFloatCommitBeyondSmallLayerBitmap();
			TestMoveTextLayerUndo();
			TestCustomBlends();
			TestCustomBlendOpacity();
			TestCustomBlendTransparentBase();
			TestMixedComposite();
			TestCompositeRangeInto();
			TestGuideStickyCenter();
			TestMoveSnapToGuides();
			TestMoveSnapTargets();
			TestFloatMoveSnapToGuides();
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
			TestStructuralLayerUndo();
			TestGuidesModel();
			TestSelectionCoverageBrush();
			TestSelectionCombineCoverage();
			TestSelectionFeather();
			TestEllipseAntiAlias();
			TestIsSelectedThreshold();
			TestInnerGlowInsideOnly();
			TestSpreadDilatesBeforeBlur();
			TestBevelOppositeSides();
			TestFeatherActive();
			TestBrightnessContrastMatchesReference();
			TestHueSaturationLightnessMatchesReference();
			TestDesaturateMatchesReference();
			TestPosterizeMatchesReference();
			TestThresholdMatchesReference();
			TestAddNoiseMatchesReference();
			TestPixelateMatchesReference();
			TestUnsharpMaskMatchesReference();
			TestGaussianBlurMatchesReference();
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
			state.SetWandAntiAlias(false);
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

		private static void TestStructuralLayerUndo()
		{
			Document doc = new Document("t", 16, 16);

			int beforeAdd = doc.Layers().Count;
			doc.BeginCanvasEdit("Add Layer");
			doc.AddLayer("X");
			doc.EndCanvasEdit();
			Check(doc.Layers().Count == beforeAdd + 1, "add layer increases count");
			doc.Undo();
			Check(doc.Layers().Count == beforeAdd, "undo removes added layer");

			int beforeDuplicate = doc.Layers().Count;
			doc.BeginCanvasEdit("Duplicate Layer");
			doc.DuplicateLayer(doc.ActiveLayerIndex());
			doc.EndCanvasEdit();
			Check(doc.Layers().Count == beforeDuplicate + 1, "duplicate layer increases count");
			doc.Undo();
			Check(doc.Layers().Count == beforeDuplicate, "undo removes duplicated layer");

			Layer removable = doc.AddLayer("Y");
			int beforeDelete = doc.Layers().Count;
			doc.BeginCanvasEdit("Delete Layer");
			doc.DeleteLayer(doc.ActiveLayerIndex());
			doc.EndCanvasEdit();
			Check(doc.Layers().Count == beforeDelete - 1, "delete layer decreases count");
			doc.Undo();
			Check(doc.Layers().Count == beforeDelete, "undo restores deleted layer");

			int topIndex = doc.Layers().Count - 1;
			doc.Layers()[topIndex].Bitmap().SetPixel(0, 0, new SKColor(7, 8, 9, 255));
			doc.BeginCanvasEdit("Reorder Layer");
			doc.MoveLayer(topIndex, 0);
			doc.EndCanvasEdit();
			SKColor movedMark = doc.Layers()[0].Bitmap().GetPixel(0, 0);
			Check(movedMark.Red == 7 && movedMark.Green == 8 && movedMark.Blue == 9, "reorder moves marked layer to bottom");
			doc.Undo();
			SKColor restoredMark = doc.Layers()[topIndex].Bitmap().GetPixel(0, 0);
			Check(restoredMark.Red == 7 && restoredMark.Green == 8 && restoredMark.Blue == 9, "undo restores layer order");
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

		private static void TestFloatMoveSnapToGuides()
		{
			SKColor red = new SKColor(255, 0, 0, 255);
			Document doc = new Document("t", 64, 48);
			doc.Guides().AddVertical(50);
			Layer content = doc.AddLayer("c");
			for (int y = 10; y < 20; y++)
			{
				for (int x = 10; x < 20; x++)
				{
					content.Bitmap().SetPixel(x, y, red);
				}
			}
			doc.Selection().SelectRect(new SKRectI(10, 10, 20, 20));
			ToolState state = new ToolState();
			state.SetSnapToGuides(true);
			state.SetSnapTolerance(6);
			MoveTool move = new MoveTool();
			move.OnPressed(doc, 15, 15, state);
			move.OnDragged(doc, 53, 15, state);
			move.OnReleased(doc, 53, 15, state);
			Check(doc.HasFloatingSelection(), "float snap: move floats the selection");
			Check(doc.FloatDeltaX() == 40, "float snap: delta snaps left edge to guide (delta 40)");
			Check(doc.FloatDeltaY() == 0, "float snap: no vertical snap without horizontal guide");
			Check(doc.Selection().Bounds().Left == 50, "float snap: selection left edge lands on guide (50)");
			doc.CancelFloatingSelection();
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
			Check(doc.HasFloatingSelection(), "selection move floats the selection");
			SKColor vacated = content.GetPixelCanvas(20, 20);
			Check(vacated.Alpha == 0, "selection move on a layer vacates the origin (20,20)");
			SKColor unselected = content.GetPixelCanvas(12, 20);
			Check(unselected.Red == 255 && unselected.Alpha == 255, "selection move on a layer leaves unselected pixels (12,20)");
			Check(doc.Selection().IsSelected(40, 20), "selection mask follows the move (40,20 selected)");
			Check(!doc.Selection().IsSelected(20, 20), "selection mask leaves the origin (20,20 unselected)");
			doc.CommitFloatingSelection();
			SKColor moved = content.GetPixelCanvas(40, 20);
			Check(moved.Red == 255 && moved.Alpha == 255, "commit bakes moved pixels (40,20)");
			SKColor movedEdge = content.GetPixelCanvas(44, 20);
			Check(movedEdge.Red == 255 && movedEdge.Alpha == 255, "commit bakes moved pixels at far edge (44,20)");
		}

		private static void TestFloatingSelection()
		{
			Document doc = new Document("t", 80, 48);
			Layer content = doc.AddLayer("c");
			content.SetOffset(20, 0);
			SKColor red = new SKColor(255, 0, 0, 255);
			for (int y = 10; y < 30; y++)
			{
				for (int x = 30; x < 50; x++)
				{
					content.SetPixelCanvas(x, y, red);
				}
			}
			doc.Selection().SelectRect(new SKRectI(34, 14, 44, 24));

			doc.LiftFloatingSelection();
			Check(doc.HasFloatingSelection(), "lift makes float active");
			SKColor hole = content.GetPixelCanvas(36, 16);
			Check(hole.Alpha == 0, "lift leaves a transparent hole at the selection (36,16)");
			SKColor keptUnselected = content.GetPixelCanvas(30, 16);
			Check(keptUnselected.Red == 255 && keptUnselected.Alpha == 255, "lift keeps unselected layer pixels (30,16)");
			SKBitmap floatBitmap = doc.FloatBitmap();
			SKColor floatPixel = floatBitmap.GetPixel(36 - content.OffsetX(), 16 - content.OffsetY());
			Check(floatPixel.Red == 255 && floatPixel.Alpha == 255, "float bitmap holds the lifted block pixels");

			doc.SetFloatingSelectionDelta(5, 3);
			doc.SetFloatingSelectionDelta(10, 6);
			SKColor frozen = floatBitmap.GetPixel(36 - content.OffsetX(), 16 - content.OffsetY());
			Check(frozen.Red == 255 && frozen.Alpha == 255, "float bitmap stays frozen after moves");
			SKRectI shifted = doc.Selection().Bounds();
			Check(shifted.Left == 44 && shifted.Top == 20, "selection bounds shift by the total delta");
			Check(doc.Selection().IsSelected(44, 20), "marquee follows the float (44,20 selected)");
			Check(!doc.Selection().IsSelected(34, 14), "marquee vacates the origin (34,14 unselected)");

			doc.CommitFloatingSelection();
			Check(!doc.HasFloatingSelection(), "commit clears the float");
			SKColor movedTo = content.GetPixelCanvas(52, 22);
			Check(movedTo.Red == 255 && movedTo.Alpha == 255, "commit places moved pixels at the shifted position (52,22)");
			SKColor origin = content.GetPixelCanvas(36, 16);
			Check(origin.Alpha == 0, "commit keeps the origin cleared (36,16)");

			bool undone = doc.Undo();
			Check(undone, "commit is undoable");
			SKColor home = content.GetPixelCanvas(36, 16);
			Check(home.Red == 255 && home.Alpha == 255, "undo restores pixels home (36,16)");
			SKColor movedGone = content.GetPixelCanvas(52, 22);
			Check(movedGone.Alpha == 0, "undo clears the moved position (52,22)");
			bool redone = doc.Redo();
			Check(redone, "commit is redoable");
			SKColor redoMoved = content.GetPixelCanvas(52, 22);
			Check(redoMoved.Red == 255 && redoMoved.Alpha == 255, "redo re-applies the move (52,22)");

			Document cancelDoc = new Document("t", 80, 48);
			Layer cancelContent = cancelDoc.AddLayer("c");
			cancelContent.SetOffset(20, 0);
			for (int y = 10; y < 30; y++)
			{
				for (int x = 30; x < 50; x++)
				{
					cancelContent.SetPixelCanvas(x, y, red);
				}
			}
			cancelDoc.Selection().SelectRect(new SKRectI(34, 14, 44, 24));
			cancelDoc.LiftFloatingSelection();
			cancelDoc.SetFloatingSelectionDelta(10, 6);
			cancelDoc.CancelFloatingSelection();
			Check(!cancelDoc.HasFloatingSelection(), "cancel clears the float");
			SKColor cancelHome = cancelContent.GetPixelCanvas(36, 16);
			Check(cancelHome.Red == 255 && cancelHome.Alpha == 255, "cancel restores the layer pixels home (36,16)");
			SKRectI cancelBounds = cancelDoc.Selection().Bounds();
			Check(cancelBounds.Left == 34 && cancelBounds.Top == 14, "cancel restores the original selection bounds");

			Document undoCancelDoc = new Document("t", 80, 48);
			Layer undoContent = undoCancelDoc.AddLayer("c");
			undoContent.SetOffset(20, 0);
			for (int y = 10; y < 30; y++)
			{
				for (int x = 30; x < 50; x++)
				{
					undoContent.SetPixelCanvas(x, y, red);
				}
			}
			undoCancelDoc.Selection().SelectRect(new SKRectI(34, 14, 44, 24));
			undoCancelDoc.LiftFloatingSelection();
			undoCancelDoc.SetFloatingSelectionDelta(10, 6);
			bool undoCancelled = undoCancelDoc.Undo();
			Check(undoCancelled, "undo returns true while floating");
			Check(!undoCancelDoc.HasFloatingSelection(), "undo cancels the float");
			SKColor undoHome = undoContent.GetPixelCanvas(36, 16);
			Check(undoHome.Red == 255 && undoHome.Alpha == 255, "undo-cancel restores pixels home (36,16)");
			SKColor undoMovedGone = undoContent.GetPixelCanvas(52, 22);
			Check(undoMovedGone.Alpha == 0, "undo-cancel drops the moved position (52,22)");
		}

		private static void MoveGrab(MoveTool move, Document doc, int fromX, int fromY, int toX, int toY, ToolState state)
		{
			bool destructive = move.IsDestructive();
			if (destructive)
			{
				doc.BeginStroke();
			}
			move.OnPressed(doc, fromX, fromY, state);
			move.OnDragged(doc, toX, toY, state);
			move.OnReleased(doc, toX, toY, state);
			if (destructive)
			{
				doc.EndStroke();
			}
		}

		private static void TestFloatingSelectionMultiGrab()
		{
			Document doc = new Document("t", 80, 48);
			Layer content = doc.AddLayer("c");
			content.SetOffset(20, 0);
			SKColor red = new SKColor(255, 0, 0, 255);
			for (int y = 10; y < 30; y++)
			{
				for (int x = 30; x < 50; x++)
				{
					content.SetPixelCanvas(x, y, red);
				}
			}
			doc.Selection().SelectRect(new SKRectI(34, 14, 44, 24));

			ToolState state = new ToolState();
			MoveTool move = new MoveTool();

			MoveGrab(move, doc, 38, 18, 44, 24, state);
			Check(doc.HasFloatingSelection(), "multi-grab: float active after grab 1");
			Check(doc.FloatDeltaX() == 6 && doc.FloatDeltaY() == 6, "multi-grab: grab 1 moves float by (6,6)");

			MoveGrab(move, doc, 44, 24, 48, 27, state);
			Check(doc.HasFloatingSelection(), "multi-grab: float active after grab 2");
			Check(doc.FloatDeltaX() == 10 && doc.FloatDeltaY() == 9, "multi-grab: grab 2 accumulates float to (10,9)");

			doc.ResetSelection();
			Check(!doc.HasFloatingSelection(), "multi-grab: deselect commits and clears the float");

			SKColor movedTo = content.GetPixelCanvas(46, 25);
			Check(movedTo.Red == 255 && movedTo.Alpha == 255, "multi-grab: commit places moved pixels at final position (46,25)");
			SKColor movedEdge = content.GetPixelCanvas(53, 32);
			Check(movedEdge.Red == 255 && movedEdge.Alpha == 255, "multi-grab: commit keeps far corner of moved block (53,32)");
			SKColor origin = content.GetPixelCanvas(36, 16);
			Check(origin.Alpha == 0, "multi-grab: commit keeps the origin cleared (36,16)");
			SKColor initialMove = content.GetPixelCanvas(42, 22);
			Check(initialMove.Alpha == 0, "multi-grab: commit does not leave content at the first-grab position (42,22)");

			bool undone = doc.Undo();
			Check(undone, "multi-grab: commit is undoable");
			SKColor home = content.GetPixelCanvas(36, 16);
			Check(home.Red == 255 && home.Alpha == 255, "multi-grab: one undo restores pixels home (36,16)");
			SKColor undoMovedGone = content.GetPixelCanvas(52, 31);
			Check(undoMovedGone.Alpha == 0, "multi-grab: one undo clears the moved position (52,31)");
			bool secondUndo = doc.Undo();
			Check(!secondUndo, "multi-grab: commit pushes exactly one undo entry (no stray stroke command)");
		}

		private static void TestFloatCommitBeyondSmallLayerBitmap()
		{
			Document doc = new Document("t", 64, 64);
			Layer pasted = doc.AddLayer("p");
			SKBitmap small = new SKBitmap(12, 12, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKColor green = new SKColor(0, 200, 0, 255);
			small.Erase(green);
			pasted.SetBitmap(small);
			pasted.SetOffset(10, 10);
			doc.Selection().SelectRect(new SKRectI(10, 10, 22, 22));
			doc.LiftFloatingSelection();
			doc.SetFloatingSelectionDelta(30, 25);
			doc.CommitFloatingSelection();
			Check(!doc.HasFloatingSelection(), "small-layer float: commit clears the float");
			SKColor topLeft = pasted.GetPixelCanvas(40, 35);
			Check(topLeft.Green == 200 && topLeft.Alpha == 255, "small-layer float: commit keeps top-left at the new position (40,35)");
			SKColor topRight = pasted.GetPixelCanvas(51, 35);
			Check(topRight.Green == 200 && topRight.Alpha == 255, "small-layer float: commit keeps top-right at the new position (51,35)");
			SKColor center = pasted.GetPixelCanvas(46, 41);
			Check(center.Green == 200 && center.Alpha == 255, "small-layer float: commit keeps the center at the new position (46,41)");
			SKColor bottomLeft = pasted.GetPixelCanvas(40, 46);
			Check(bottomLeft.Green == 200 && bottomLeft.Alpha == 255, "small-layer float: commit keeps bottom-left at the new position (40,46)");
			SKColor bottomRight = pasted.GetPixelCanvas(51, 46);
			Check(bottomRight.Green == 200 && bottomRight.Alpha == 255, "small-layer float: commit keeps bottom-right at the new position (51,46)");
			SKColor vacatedTopLeft = pasted.GetPixelCanvas(10, 10);
			Check(vacatedTopLeft.Alpha == 0, "small-layer float: commit vacates the original position (10,10)");
			SKColor vacatedBottomRight = pasted.GetPixelCanvas(21, 21);
			Check(vacatedBottomRight.Alpha == 0, "small-layer float: commit vacates the original position (21,21)");
			bool undone = doc.Undo();
			Check(undone, "small-layer float: commit is undoable");
			Check(pasted.Bitmap().Width == 12 && pasted.Bitmap().Height == 12, "small-layer float: undo restores the original 12x12 bitmap");
			Check(pasted.OffsetX() == 10 && pasted.OffsetY() == 10, "small-layer float: undo restores the original offset");
			SKColor undoHome = pasted.GetPixelCanvas(10, 10);
			Check(undoHome.Green == 200 && undoHome.Alpha == 255, "small-layer float: undo restores pixels home (10,10)");
			SKColor undoMovedGone = pasted.GetPixelCanvas(46, 41);
			Check(undoMovedGone.Alpha == 0, "small-layer float: undo clears the moved position (46,41)");
			bool redone = doc.Redo();
			Check(redone, "small-layer float: commit is redoable");
			SKColor redoMoved = pasted.GetPixelCanvas(46, 41);
			Check(redoMoved.Green == 200 && redoMoved.Alpha == 255, "small-layer float: redo re-applies the move (46,41)");

			Document cancelDoc = new Document("t", 64, 64);
			Layer cancelPasted = cancelDoc.AddLayer("p");
			SKBitmap cancelSmall = new SKBitmap(12, 12, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			cancelSmall.Erase(green);
			cancelPasted.SetBitmap(cancelSmall);
			cancelPasted.SetOffset(10, 10);
			cancelDoc.Selection().SelectRect(new SKRectI(10, 10, 22, 22));
			cancelDoc.LiftFloatingSelection();
			cancelDoc.SetFloatingSelectionDelta(30, 25);
			cancelDoc.CancelFloatingSelection();
			Check(!cancelDoc.HasFloatingSelection(), "small-layer float: cancel clears the float");
			Check(cancelPasted.Bitmap().Width == 12 && cancelPasted.Bitmap().Height == 12, "small-layer float: cancel restores the original 12x12 bitmap");
			Check(cancelPasted.OffsetX() == 10 && cancelPasted.OffsetY() == 10, "small-layer float: cancel restores the original offset");
			SKColor cancelHome = cancelPasted.GetPixelCanvas(15, 15);
			Check(cancelHome.Green == 200 && cancelHome.Alpha == 255, "small-layer float: cancel restores pixels home (15,15)");
			SKRectI cancelBounds = cancelDoc.Selection().Bounds();
			Check(cancelBounds.Left == 10 && cancelBounds.Top == 10, "small-layer float: cancel restores the original selection bounds");
		}

		private static int CountOpaquePixels(SKBitmap bitmap)
		{
			int count = 0;
			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					if (bitmap.GetPixel(x, y).Alpha != 0)
					{
						count = count + 1;
					}
				}
			}
			return count;
		}

		private static bool BitmapsIdentical(SKBitmap first, SKBitmap second)
		{
			if (first.Width != second.Width || first.Height != second.Height)
			{
				return false;
			}
			for (int y = 0; y < first.Height; y++)
			{
				for (int x = 0; x < first.Width; x++)
				{
					if (first.GetPixel(x, y) != second.GetPixel(x, y))
					{
						return false;
					}
				}
			}
			return true;
		}

		private static void TestMoveTextLayerUndo()
		{
			Document doc = new Document("t", 64, 32);
			Layer text = doc.AddLayer("Words");
			text.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			text.SetTextPosition(8, 6);
			text.SetTextString("Hi");
			text.SetTextStyle(18.0f, "Arial", true, false, new SKColor(0, 0, 0, 255), 0, 0);
			text.RenderText();
			Check(text.IsText(), "text move: layer is a text layer");
			int originalInk = CountOpaquePixels(text.Bitmap());
			Check(originalInk > 0, "text move: text renders visible ink before the move");
			SKBitmap original = text.Bitmap().Copy();

			int undoBefore = doc.HistoryIndex();
			ToolState state = new ToolState();
			MoveTool move = new MoveTool();
			Check(!move.IsDestructive(), "text move: move tool is non-destructive (helper will not auto-wrap the stroke)");
			MoveGrab(move, doc, 10, 10, 24, 18, state);

			Check(text.TextX() == 22 && text.TextY() == 14, "text move: text position shifts by (14,8)");
			Check(doc.HistoryIndex() == undoBefore + 1, "text move: a single move pushes exactly one undo entry");
			Check(!BitmapsIdentical(original, text.Bitmap()), "text move: moved render differs from the original render");

			bool undone = doc.Undo();
			Check(undone, "text move: move is undoable");
			Check(BitmapsIdentical(original, text.Bitmap()), "text move: undo restores the text pixels to the original position");
			Check(doc.HistoryIndex() == undoBefore, "text move: undo consumes the single move command");
			original.Dispose();
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

		private static void TestSelectionCoverageBrush()
		{
			Document doc = new Document("t", 32, 32);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			byte[] mask = new byte[32 * 32];
			for (int y = 2; y < 30; y++)
			{
				for (int x = 2; x < 14; x++)
				{
					mask[(y * 32) + x] = 128;
				}
				for (int x = 18; x < 30; x++)
				{
					mask[(y * 32) + x] = 255;
				}
			}
			doc.Selection().SelectMask(mask, new SKRectI(2, 2, 30, 30));
			BrushEngine engine = new BrushEngine();
			engine.Begin(layer, null, 4, 1.0, 1.0, 1.0, false, 0.25, 0.0, eBrushOp.Paint, eBlendMode.Normal, new SKColor(255, 0, 0, 255));
			engine.StampFirst(doc, layer, 8, 16, doc.Selection());
			engine.StampFirst(doc, layer, 24, 16, doc.Selection());
			engine.End();
			SKColor half = layer.GetPixelCanvas(8, 16);
			CheckNear(half.Alpha, 128, 3, "half coverage paint yields half alpha");
			Check(half.Red == 255 && half.Green == 0, "half coverage paint keeps color");
			SKColor full = layer.GetPixelCanvas(24, 16);
			Check(full.Alpha == 255, "full coverage paint yields full alpha");
			Check(full.Red == 255 && full.Green == 0, "full coverage paint keeps color");
		}

		private static void TestSelectionCombineCoverage()
		{
			Selection sel = new Selection(4, 4);
			byte[] baseAdd = new byte[16];
			baseAdd[5] = 200;
			sel.SelectMask(baseAdd, new SKRectI(1, 1, 2, 2));
			sel.BeginOperation(eSelectionMode.Add);
			byte[] regionAdd = new byte[16];
			regionAdd[5] = 120;
			regionAdd[6] = 90;
			sel.ApplyMask(regionAdd);
			Check(sel.Coverage(1, 1) == 200, "union keeps max of base and new");
			Check(sel.Coverage(2, 1) == 90, "union takes new where base empty");

			byte[] baseSubtract = new byte[16];
			baseSubtract[5] = 200;
			baseSubtract[6] = 40;
			sel.SelectMask(baseSubtract, new SKRectI(1, 1, 3, 2));
			sel.BeginOperation(eSelectionMode.Subtract);
			byte[] regionSubtract = new byte[16];
			regionSubtract[5] = 120;
			regionSubtract[6] = 90;
			sel.ApplyMask(regionSubtract);
			Check(sel.Coverage(1, 1) == 80, "subtract is base minus new");
			Check(sel.Coverage(2, 1) == 0, "subtract clamps at zero");

			byte[] baseIntersect = new byte[16];
			baseIntersect[5] = 200;
			sel.SelectMask(baseIntersect, new SKRectI(1, 1, 2, 2));
			sel.BeginOperation(eSelectionMode.Intersect);
			byte[] regionIntersect = new byte[16];
			regionIntersect[5] = 120;
			regionIntersect[6] = 90;
			sel.ApplyMask(regionIntersect);
			Check(sel.Coverage(1, 1) == 120, "intersect takes min");
			Check(sel.Coverage(2, 1) == 0, "intersect zero where base empty");
		}

		private static void TestSelectionFeather()
		{
			Selection sel = new Selection(64, 64);
			sel.BeginOperation(eSelectionMode.Replace, 4);
			sel.ApplyRect(new SKRectI(20, 20, 44, 44));
			Check(sel.IsActive(), "feathered selection stays active");
			Check(sel.Coverage(32, 32) > 200, "feather keeps strong center");
			int edge = sel.Coverage(20, 32);
			Check(edge > 0 && edge < 255, "feather intermediate at rect edge");
			int skirt = sel.Coverage(17, 32);
			Check(skirt > 0 && skirt < 128, "feather skirt outside original rect");
			Check(sel.Coverage(2, 32) == 0, "feather zero far outside");
			Check(sel.IsSelected(32, 32), "feathered center still selected");
		}

		private static void TestEllipseAntiAlias()
		{
			Document doc = new Document("t", 64, 64);
			ToolState state = new ToolState();
			state.SetSelectionAntiAlias(true);
			EllipseSelectTool tool = new EllipseSelectTool();
			tool.OnPressed(doc, 12, 12, state);
			tool.OnDragged(doc, 50, 46, state);
			tool.OnReleased(doc, 50, 46, state);
			byte[] mask = doc.Selection().Mask();
			int intermediate = 0;
			for (int index = 0; index < mask.Length; index++)
			{
				if (mask[index] > 0 && mask[index] < 255)
				{
					intermediate = intermediate + 1;
				}
			}
			Check(intermediate > 0, "ellipse AA edge has partial coverage");
			Check(doc.Selection().Coverage(31, 29) == 255, "ellipse AA center full coverage");

			Document docHard = new Document("t", 64, 64);
			state.SetSelectionAntiAlias(false);
			EllipseSelectTool toolHard = new EllipseSelectTool();
			toolHard.OnPressed(docHard, 12, 12, state);
			toolHard.OnDragged(docHard, 50, 46, state);
			toolHard.OnReleased(docHard, 50, 46, state);
			byte[] maskHard = docHard.Selection().Mask();
			int intermediateHard = 0;
			for (int index = 0; index < maskHard.Length; index++)
			{
				if (maskHard[index] > 0 && maskHard[index] < 255)
				{
					intermediateHard = intermediateHard + 1;
				}
			}
			Check(intermediateHard == 0, "ellipse AA off has no partial coverage");
			Check(docHard.Selection().IsActive(), "ellipse AA off still selects");
		}

		private static void TestIsSelectedThreshold()
		{
			Selection sel = new Selection(4, 4);
			byte[] mask = new byte[16];
			mask[0] = 127;
			mask[1] = 128;
			mask[2] = 255;
			sel.SelectMask(mask, new SKRectI(0, 0, 3, 1));
			Check(!sel.IsSelected(0, 0), "coverage 127 below threshold");
			Check(sel.IsSelected(1, 0), "coverage 128 at threshold selected");
			Check(sel.IsSelected(2, 0), "coverage 255 selected");
			Check(sel.Coverage(0, 0) == 127, "coverage accessor exact value");
			Check(sel.Coverage(-1, 0) == 0, "coverage out of bounds x is zero");
			Check(sel.Coverage(0, 9) == 0, "coverage out of bounds y is zero");
		}

		private static void TestFeatherActive()
		{
			Document doc = new Document("t", 32, 32);
			Selection sel = doc.Selection();
			sel.SelectRect(new SKRectI(4, 4, 28, 28));
			int generationBefore = sel.Generation();
			sel.FeatherActive(2);
			Check(sel.IsActive(), "feather active keeps selection active");
			Check(sel.Generation() > generationBefore, "feather active bumps the generation");
			bool hasPartial = false;
			for (int y = 0; y < 32; y++)
			{
				for (int x = 0; x < 32; x++)
				{
					int cov = sel.Coverage(x, y);
					if (cov > 0 && cov < 255)
					{
						hasPartial = true;
					}
				}
			}
			Check(hasPartial, "feather active produces a partial coverage band");
			Check(sel.Coverage(16, 16) == 255, "feather active keeps the selection center solid");
		}

		private static void TestInnerGlowInsideOnly()
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
			int placeX;
			int placeY;
			SKColor green = new SKColor(0, 255, 0, 255);
			SKBitmap innerGlow = LayerStyles.RenderInnerGlow(source, green, 3, 0, 255, out placeX, out placeY);
			Check(placeX == 0 && placeY == 0, "inner glow placement is the source origin");
			Check(innerGlow.Width == 16 && innerGlow.Height == 16, "inner glow result matches source size");
			bool outsideClear = true;
			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					bool insideBody = x >= 4 && x < 12 && y >= 4 && y < 12;
					if (insideBody)
					{
						continue;
					}
					if (innerGlow.GetPixel(x, y).Alpha != 0)
					{
						outsideClear = false;
					}
				}
			}
			Check(outsideClear, "inner glow writes no pixels outside the body alpha");
			SKColor edgePixel = innerGlow.GetPixel(4, 8);
			Check(edgePixel.Alpha > 0 && edgePixel.Green == 255, "inner glow halo present just inside the edge");
			SKColor centerPixel = innerGlow.GetPixel(8, 8);
			Check(centerPixel.Alpha < edgePixel.Alpha, "inner glow fades toward the body center");
			innerGlow.Dispose();
			source.Dispose();
		}

		private static void TestSpreadDilatesBeforeBlur()
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
			int hardPlaceX;
			int hardPlaceY;
			SKColor yellow = new SKColor(255, 255, 0, 255);
			SKBitmap hard = LayerStyles.RenderOuterGlow(source, yellow, 4, 100, 255, out hardPlaceX, out hardPlaceY);
			SKColor dilatedBand = hard.GetPixel(4, 12);
			Check(dilatedBand.Alpha == 255, "spread 100 dilates the silhouette to a hard opaque band");
			SKColor beyondBand = hard.GetPixel(3, 12);
			Check(beyondBand.Alpha == 0, "spread 100 has a hard edge with no blur beyond the dilation");
			hard.Dispose();
			int softPlaceX;
			int softPlaceY;
			SKBitmap soft = LayerStyles.RenderOuterGlow(source, yellow, 4, 0, 255, out softPlaceX, out softPlaceY);
			SKColor softBand = soft.GetPixel(4, 12);
			Check(softBand.Alpha > 0 && softBand.Alpha < 255, "spread 0 keeps the soft blurred falloff");
			int legacyPlaceX;
			int legacyPlaceY;
			SKBitmap legacy = LayerStyles.RenderOuterGlow(source, yellow, 4, 255, out legacyPlaceX, out legacyPlaceY);
			Check(legacy.GetPixel(4, 12).Alpha == soft.GetPixel(4, 12).Alpha, "spread 0 output matches the legacy glow output");
			legacy.Dispose();
			soft.Dispose();
			source.Dispose();
		}

		private static void TestBevelOppositeSides()
		{
			SKBitmap source = new SKBitmap(20, 20, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			source.Erase(SKColors.Transparent);
			SKColor opaque = new SKColor(40, 40, 200, 255);
			for (int y = 4; y < 16; y++)
			{
				for (int x = 4; x < 16; x++)
				{
					source.SetPixel(x, y, opaque);
				}
			}
			int placeX;
			int placeY;
			SKColor white = new SKColor(255, 255, 255, 255);
			SKColor black = new SKColor(0, 0, 0, 255);
			SKBitmap bevel = LayerStyles.RenderBevel(source, 100, 3, 0, white, 255, black, 255, out placeX, out placeY);
			Check(placeX == 0 && placeY == 0, "bevel placement is the source origin");
			SKColor leftBand = bevel.GetPixel(5, 10);
			Check(leftBand.Alpha > 0 && leftBand.Red == 255 && leftBand.Green == 255 && leftBand.Blue == 255, "bevel angle 0 lights the left edge with the highlight color");
			SKColor rightBand = bevel.GetPixel(14, 10);
			Check(rightBand.Alpha > 0 && rightBand.Red == 0 && rightBand.Green == 0 && rightBand.Blue == 0, "bevel angle 0 shades the right edge with the shadow color");
			SKColor outside = bevel.GetPixel(2, 10);
			Check(outside.Alpha == 0, "bevel writes nothing outside the body alpha");
			SKColor center = bevel.GetPixel(10, 10);
			Check(center.Alpha == 0, "bevel leaves the flat body center untouched");
			bevel.Dispose();
			source.Dispose();
		}

		private static SKBitmap BuildAdjustmentTestBitmap()
		{
			SKBitmap bitmap = new SKBitmap(41, 29, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Random random = new Random(777);
			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					byte red = (byte)random.Next(256);
					byte green = (byte)random.Next(256);
					byte blue = (byte)random.Next(256);
					byte alpha = (byte)random.Next(256);
					bitmap.SetPixel(x, y, new SKColor(red, green, blue, alpha));
				}
			}
			bitmap.SetPixel(0, 0, new SKColor(0, 0, 0, 0));
			bitmap.SetPixel(1, 0, new SKColor(255, 255, 255, 255));
			bitmap.SetPixel(2, 0, new SKColor(0, 0, 0, 255));
			bitmap.SetPixel(3, 0, new SKColor(255, 0, 0, 128));
			bitmap.SetPixel(4, 0, new SKColor(17, 200, 90, 1));
			return bitmap;
		}

		private static bool AdjustmentBitmapsEqual(SKBitmap first, SKBitmap second)
		{
			for (int y = 0; y < first.Height; y++)
			{
				for (int x = 0; x < first.Width; x++)
				{
					SKColor a = first.GetPixel(x, y);
					SKColor b = second.GetPixel(x, y);
					if (a.Red != b.Red || a.Green != b.Green || a.Blue != b.Blue || a.Alpha != b.Alpha)
					{
						Console.WriteLine("  mismatch at " + x + "," + y + " got " + a + " expected " + b);
						return false;
					}
				}
			}
			return true;
		}

		private static byte ReferenceClampByte(double value)
		{
			if (value < 0.0)
			{
				return 0;
			}
			if (value > 255.0)
			{
				return 255;
			}
			return (byte)Math.Round(value);
		}

		private static void TestBrightnessContrastMatchesReference()
		{
			int[] brightnessValues = new int[] { 0, 40, -60, 100 };
			int[] contrastValues = new int[] { 0, 55, -35, -100 };
			for (int i = 0; i < brightnessValues.Length; i++)
			{
				SKBitmap actual = BuildAdjustmentTestBitmap();
				SKBitmap expected = BuildAdjustmentTestBitmap();
				Adjustments.BrightnessContrast(actual, brightnessValues[i], contrastValues[i]);
				double brightnessOffset = brightnessValues[i] * 2.55;
				double contrastMapped = contrastValues[i] * 2.55;
				double factor = (259.0 * (contrastMapped + 255.0)) / (255.0 * (259.0 - contrastMapped));
				for (int y = 0; y < expected.Height; y++)
				{
					for (int x = 0; x < expected.Width; x++)
					{
						SKColor color = expected.GetPixel(x, y);
						double red = factor * ((color.Red + brightnessOffset) - 128.0) + 128.0;
						double green = factor * ((color.Green + brightnessOffset) - 128.0) + 128.0;
						double blue = factor * ((color.Blue + brightnessOffset) - 128.0) + 128.0;
						expected.SetPixel(x, y, new SKColor(ReferenceClampByte(red), ReferenceClampByte(green), ReferenceClampByte(blue), color.Alpha));
					}
				}
				Check(AdjustmentBitmapsEqual(actual, expected), "brightness/contrast " + brightnessValues[i] + "/" + contrastValues[i] + " matches reference");
				actual.Dispose();
				expected.Dispose();
			}
		}

		private static void TestHueSaturationLightnessMatchesReference()
		{
			int[] hueValues = new int[] { 0, 120, -200, 359 };
			int[] saturationValues = new int[] { 0, 80, -100, 50 };
			int[] lightnessValues = new int[] { 0, 30, -45, 100 };
			for (int i = 0; i < hueValues.Length; i++)
			{
				SKBitmap actual = BuildAdjustmentTestBitmap();
				SKBitmap expected = BuildAdjustmentTestBitmap();
				Adjustments.HueSaturationLightness(actual, hueValues[i], saturationValues[i], lightnessValues[i]);
				for (int y = 0; y < expected.Height; y++)
				{
					for (int x = 0; x < expected.Width; x++)
					{
						SKColor color = expected.GetPixel(x, y);
						float h;
						float s;
						float l;
						color.ToHsl(out h, out s, out l);
						h = h + hueValues[i];
						for (;;)
						{
							if (h >= 0.0f)
							{
								break;
							}
							h = h + 360.0f;
						}
						for (;;)
						{
							if (h < 360.0f)
							{
								break;
							}
							h = h - 360.0f;
						}
						s = s * (1.0f + (saturationValues[i] / 100.0f));
						if (s < 0.0f)
						{
							s = 0.0f;
						}
						if (s > 100.0f)
						{
							s = 100.0f;
						}
						l = l + lightnessValues[i];
						if (l < 0.0f)
						{
							l = 0.0f;
						}
						if (l > 100.0f)
						{
							l = 100.0f;
						}
						expected.SetPixel(x, y, SKColor.FromHsl(h, s, l, color.Alpha));
					}
				}
				Check(AdjustmentBitmapsEqual(actual, expected), "hue/sat/light " + hueValues[i] + "/" + saturationValues[i] + "/" + lightnessValues[i] + " matches reference");
				actual.Dispose();
				expected.Dispose();
			}
		}

		private static void TestDesaturateMatchesReference()
		{
			SKBitmap actual = BuildAdjustmentTestBitmap();
			SKBitmap expected = BuildAdjustmentTestBitmap();
			Adjustments.Desaturate(actual);
			for (int y = 0; y < expected.Height; y++)
			{
				for (int x = 0; x < expected.Width; x++)
				{
					SKColor color = expected.GetPixel(x, y);
					byte gray = ReferenceClampByte(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);
					expected.SetPixel(x, y, new SKColor(gray, gray, gray, color.Alpha));
				}
			}
			Check(AdjustmentBitmapsEqual(actual, expected), "desaturate matches reference");
			actual.Dispose();
			expected.Dispose();
		}

		private static void TestPosterizeMatchesReference()
		{
			int[] levelValues = new int[] { 2, 4, 7, 255 };
			for (int i = 0; i < levelValues.Length; i++)
			{
				SKBitmap actual = BuildAdjustmentTestBitmap();
				SKBitmap expected = BuildAdjustmentTestBitmap();
				Adjustments.Posterize(actual, levelValues[i]);
				for (int y = 0; y < expected.Height; y++)
				{
					for (int x = 0; x < expected.Width; x++)
					{
						SKColor color = expected.GetPixel(x, y);
						byte red = ReferencePosterizeChannel(color.Red, levelValues[i]);
						byte green = ReferencePosterizeChannel(color.Green, levelValues[i]);
						byte blue = ReferencePosterizeChannel(color.Blue, levelValues[i]);
						expected.SetPixel(x, y, new SKColor(red, green, blue, color.Alpha));
					}
				}
				Check(AdjustmentBitmapsEqual(actual, expected), "posterize " + levelValues[i] + " matches reference");
				actual.Dispose();
				expected.Dispose();
			}
		}

		private static byte ReferencePosterizeChannel(byte channel, int levels)
		{
			double normalized = channel / 255.0;
			double stepped = Math.Round(normalized * (levels - 1));
			double result = Math.Round(stepped / (levels - 1) * 255.0);
			return ReferenceClampByte(result);
		}

		private static void TestThresholdMatchesReference()
		{
			int[] cutoffValues = new int[] { 0, 90, 128, 255 };
			for (int i = 0; i < cutoffValues.Length; i++)
			{
				SKBitmap actual = BuildAdjustmentTestBitmap();
				SKBitmap expected = BuildAdjustmentTestBitmap();
				Adjustments.Threshold(actual, cutoffValues[i]);
				for (int y = 0; y < expected.Height; y++)
				{
					for (int x = 0; x < expected.Width; x++)
					{
						SKColor color = expected.GetPixel(x, y);
						double luminance = 0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue;
						if (luminance >= cutoffValues[i])
						{
							expected.SetPixel(x, y, new SKColor(255, 255, 255, color.Alpha));
						}
						else
						{
							expected.SetPixel(x, y, new SKColor(0, 0, 0, color.Alpha));
						}
					}
				}
				Check(AdjustmentBitmapsEqual(actual, expected), "threshold " + cutoffValues[i] + " matches reference");
				actual.Dispose();
				expected.Dispose();
			}
		}

		private static void TestAddNoiseMatchesReference()
		{
			bool[] monochromeValues = new bool[] { true, false };
			for (int i = 0; i < monochromeValues.Length; i++)
			{
				SKBitmap actual = BuildAdjustmentTestBitmap();
				SKBitmap expected = BuildAdjustmentTestBitmap();
				Adjustments.AddNoise(actual, 40, monochromeValues[i]);
				Random random = new Random(12345);
				for (int y = 0; y < expected.Height; y++)
				{
					for (int x = 0; x < expected.Width; x++)
					{
						SKColor color = expected.GetPixel(x, y);
						byte red;
						byte green;
						byte blue;
						if (monochromeValues[i])
						{
							double n = (random.NextDouble() * 2.0 - 1.0) * 40 * 1.28;
							red = ReferenceClampByte(color.Red + n);
							green = ReferenceClampByte(color.Green + n);
							blue = ReferenceClampByte(color.Blue + n);
						}
						else
						{
							double nRed = (random.NextDouble() * 2.0 - 1.0) * 40 * 1.28;
							double nGreen = (random.NextDouble() * 2.0 - 1.0) * 40 * 1.28;
							double nBlue = (random.NextDouble() * 2.0 - 1.0) * 40 * 1.28;
							red = ReferenceClampByte(color.Red + nRed);
							green = ReferenceClampByte(color.Green + nGreen);
							blue = ReferenceClampByte(color.Blue + nBlue);
						}
						expected.SetPixel(x, y, new SKColor(red, green, blue, color.Alpha));
					}
				}
				Check(AdjustmentBitmapsEqual(actual, expected), "add noise monochrome=" + monochromeValues[i] + " matches reference");
				actual.Dispose();
				expected.Dispose();
			}
		}

		private static void TestPixelateMatchesReference()
		{
			int[] cellValues = new int[] { 1, 5, 16 };
			for (int i = 0; i < cellValues.Length; i++)
			{
				SKBitmap actual = BuildAdjustmentTestBitmap();
				SKBitmap expected = BuildAdjustmentTestBitmap();
				Adjustments.Pixelate(actual, cellValues[i]);
				int cellSize = cellValues[i];
				for (int blockY = 0; blockY < expected.Height; blockY += cellSize)
				{
					for (int blockX = 0; blockX < expected.Width; blockX += cellSize)
					{
						int endY = blockY + cellSize;
						if (endY > expected.Height)
						{
							endY = expected.Height;
						}
						int endX = blockX + cellSize;
						if (endX > expected.Width)
						{
							endX = expected.Width;
						}
						long sumRed = 0;
						long sumGreen = 0;
						long sumBlue = 0;
						long sumAlpha = 0;
						int count = 0;
						for (int y = blockY; y < endY; y++)
						{
							for (int x = blockX; x < endX; x++)
							{
								SKColor color = expected.GetPixel(x, y);
								sumRed += color.Red;
								sumGreen += color.Green;
								sumBlue += color.Blue;
								sumAlpha += color.Alpha;
								count++;
							}
						}
						SKColor average = new SKColor((byte)(sumRed / count), (byte)(sumGreen / count), (byte)(sumBlue / count), (byte)(sumAlpha / count));
						for (int y = blockY; y < endY; y++)
						{
							for (int x = blockX; x < endX; x++)
							{
								expected.SetPixel(x, y, average);
							}
						}
					}
				}
				Check(AdjustmentBitmapsEqual(actual, expected), "pixelate " + cellValues[i] + " matches reference");
				actual.Dispose();
				expected.Dispose();
			}
		}

		private static void ReferenceBoxBlur(SKBitmap source, SKBitmap destination, int radius)
		{
			int width = source.Width;
			int height = source.Height;
			if (radius <= 0)
			{
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						destination.SetPixel(x, y, source.GetPixel(x, y));
					}
				}
				return;
			}
			int windowLength = 2 * radius + 1;
			SKBitmap horizontal = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					long sumRed = 0;
					long sumGreen = 0;
					long sumBlue = 0;
					long sumAlpha = 0;
					for (int offset = -radius; offset <= radius; offset++)
					{
						int sampleX = x + offset;
						if (sampleX < 0)
						{
							sampleX = 0;
						}
						if (sampleX > width - 1)
						{
							sampleX = width - 1;
						}
						SKColor sample = source.GetPixel(sampleX, y);
						sumRed += sample.Red;
						sumGreen += sample.Green;
						sumBlue += sample.Blue;
						sumAlpha += sample.Alpha;
					}
					horizontal.SetPixel(x, y, new SKColor((byte)(sumRed / windowLength), (byte)(sumGreen / windowLength), (byte)(sumBlue / windowLength), (byte)(sumAlpha / windowLength)));
				}
			}
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					long sumRed = 0;
					long sumGreen = 0;
					long sumBlue = 0;
					long sumAlpha = 0;
					for (int offset = -radius; offset <= radius; offset++)
					{
						int sampleY = y + offset;
						if (sampleY < 0)
						{
							sampleY = 0;
						}
						if (sampleY > height - 1)
						{
							sampleY = height - 1;
						}
						SKColor sample = horizontal.GetPixel(x, sampleY);
						sumRed += sample.Red;
						sumGreen += sample.Green;
						sumBlue += sample.Blue;
						sumAlpha += sample.Alpha;
					}
					destination.SetPixel(x, y, new SKColor((byte)(sumRed / windowLength), (byte)(sumGreen / windowLength), (byte)(sumBlue / windowLength), (byte)(sumAlpha / windowLength)));
				}
			}
			horizontal.Dispose();
		}

		private static void TestUnsharpMaskMatchesReference()
		{
			SKBitmap actual = BuildAdjustmentTestBitmap();
			SKBitmap expected = BuildAdjustmentTestBitmap();
			Adjustments.UnsharpMask(actual, 120, 3);
			SKBitmap blurred = new SKBitmap(expected.Width, expected.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			ReferenceBoxBlur(expected, blurred, 3);
			double strength = 120 / 100.0;
			for (int y = 0; y < expected.Height; y++)
			{
				for (int x = 0; x < expected.Width; x++)
				{
					SKColor color = expected.GetPixel(x, y);
					SKColor blur = blurred.GetPixel(x, y);
					double red = color.Red + strength * (color.Red - blur.Red);
					double green = color.Green + strength * (color.Green - blur.Green);
					double blue = color.Blue + strength * (color.Blue - blur.Blue);
					expected.SetPixel(x, y, new SKColor(ReferenceClampByte(red), ReferenceClampByte(green), ReferenceClampByte(blue), color.Alpha));
				}
			}
			Check(AdjustmentBitmapsEqual(actual, expected), "unsharp mask matches reference");
			actual.Dispose();
			expected.Dispose();
			blurred.Dispose();
		}

		private static void TestGaussianBlurMatchesReference()
		{
			int[] radiusValues = new int[] { 0, 2, 6 };
			for (int i = 0; i < radiusValues.Length; i++)
			{
				SKBitmap actual = BuildAdjustmentTestBitmap();
				SKBitmap expected = BuildAdjustmentTestBitmap();
				Adjustments.GaussianBlur(actual, radiusValues[i]);
				int width = expected.Width;
				int height = expected.Height;
				SKBitmap premultiplied = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						SKColor color = expected.GetPixel(x, y);
						byte red = (byte)(((color.Red * color.Alpha) + 127) / 255);
						byte green = (byte)(((color.Green * color.Alpha) + 127) / 255);
						byte blue = (byte)(((color.Blue * color.Alpha) + 127) / 255);
						premultiplied.SetPixel(x, y, new SKColor(red, green, blue, color.Alpha));
					}
				}
				SKBitmap blurA = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				SKBitmap blurB = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				ReferenceBoxBlur(premultiplied, blurA, radiusValues[i]);
				ReferenceBoxBlur(blurA, blurB, radiusValues[i]);
				ReferenceBoxBlur(blurB, blurA, radiusValues[i]);
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						SKColor color = blurA.GetPixel(x, y);
						if (color.Alpha == 0)
						{
							expected.SetPixel(x, y, new SKColor(0, 0, 0, 0));
							continue;
						}
						int red = ((color.Red * 255) + (color.Alpha / 2)) / color.Alpha;
						int green = ((color.Green * 255) + (color.Alpha / 2)) / color.Alpha;
						int blue = ((color.Blue * 255) + (color.Alpha / 2)) / color.Alpha;
						if (red > 255)
						{
							red = 255;
						}
						if (green > 255)
						{
							green = 255;
						}
						if (blue > 255)
						{
							blue = 255;
						}
						expected.SetPixel(x, y, new SKColor((byte)red, (byte)green, (byte)blue, color.Alpha));
					}
				}
				Check(AdjustmentBitmapsEqual(actual, expected), "gaussian blur radius " + radiusValues[i] + " matches reference");
				actual.Dispose();
				expected.Dispose();
				premultiplied.Dispose();
				blurA.Dispose();
				blurB.Dispose();
			}
		}
	}
}
