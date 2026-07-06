using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class LayerMaskTests
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

		public static int RunAll()
		{
			s_failures = 0;
			TestMaskHidesRevealsGray();
			TestMaskHidesRevealsGraySoftwarePath();
			TestDisabledMaskIgnored();
			TestMaskSurvivesClone();
			TestMaskTracksOffset();
			return s_failures;
		}

		private static void TestMaskHidesRevealsGray()
		{
			Document doc = new Document("mask", 64, 64);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			doc.AddMaskToActiveLayer(true);
			SKBitmap mask = layer.MaskBitmap();
			mask.SetPixel(20, 20, SKColors.Black);
			mask.SetPixel(40, 40, new SKColor(128, 128, 128, 255));
			SKBitmap target = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(target);
			SKColor revealPixel = target.GetPixel(5, 5);
			Check(revealPixel.Alpha == 255, "white mask area reveals layer opaque");
			SKColor hidePixel = target.GetPixel(20, 20);
			Check(hidePixel.Alpha == 0, "black mask area hides layer fully");
			SKColor grayPixel = target.GetPixel(40, 40);
			CheckNear(grayPixel.Alpha, 128, 2, "gray mask value yields half alpha");
			target.Dispose();
		}

		private static void TestMaskHidesRevealsGraySoftwarePath()
		{
			Document doc = new Document("masksoftware", 64, 64);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			doc.AddMaskToActiveLayer(true);
			SKBitmap mask = layer.MaskBitmap();
			mask.SetPixel(20, 20, SKColors.Black);
			mask.SetPixel(40, 40, new SKColor(128, 128, 128, 255));
			Layer forceSoftware = doc.AddLayer("subtract");
			forceSoftware.Bitmap().Erase(new SKColor(0, 0, 0, 0));
			forceSoftware.SetBlendMode(eBlendMode.Subtract);
			SKBitmap target = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(target);
			SKColor revealPixel = target.GetPixel(5, 5);
			Check(revealPixel.Alpha == 255, "software path white mask area reveals opaque");
			SKColor hidePixel = target.GetPixel(20, 20);
			Check(hidePixel.Alpha == 0, "software path black mask area hides fully");
			SKColor grayPixel = target.GetPixel(40, 40);
			CheckNear(grayPixel.Alpha, 128, 2, "software path gray mask yields half alpha");
			target.Dispose();
		}

		private static void TestDisabledMaskIgnored()
		{
			Document doc = new Document("maskdisabled", 64, 64);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			doc.AddMaskToActiveLayer(true);
			SKBitmap mask = layer.MaskBitmap();
			mask.SetPixel(20, 20, SKColors.Black);
			SKBitmap hidden = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(hidden);
			SKColor hiddenPixel = hidden.GetPixel(20, 20);
			Check(hiddenPixel.Alpha == 0, "enabled mask hides pixel before disable");
			hidden.Dispose();
			doc.SetActiveMaskEnabled(false);
			SKBitmap shown = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(shown);
			SKColor shownPixel = shown.GetPixel(20, 20);
			Check(shownPixel.Alpha == 255, "disabled mask restores hidden pixel to opaque");
			shown.Dispose();
		}

		private static void TestMaskSurvivesClone()
		{
			Document doc = new Document("maskclone", 64, 64);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			doc.AddMaskToActiveLayer(true);
			Layer clone = layer.Clone();
			Check(clone.HasMask(), "clone carries the mask");
			Check(clone.MaskEnabled(), "clone carries the mask enabled flag");
			Check(clone.MaskBitmap().Width == layer.Bitmap().Width && clone.MaskBitmap().Height == layer.Bitmap().Height, "clone mask dimensions match layer bitmap");
			clone.MaskBitmap().SetPixel(30, 30, SKColors.Black);
			SKColor sourceUnaffected = layer.MaskBitmap().GetPixel(30, 30);
			Check(sourceUnaffected.Red == 255, "clone mask is an independent copy");
		}

		private static void TestMaskTracksOffset()
		{
			Document doc = new Document("maskoffset", 64, 64);
			Layer layer = doc.ActiveLayer();
			layer.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			doc.AddMaskToActiveLayer(true);
			layer.SetOffset(10, 0);
			int maskX = 15;
			int maskY = 25;
			layer.MaskBitmap().SetPixel(maskX, maskY, SKColors.Black);
			SKBitmap target = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
			doc.CompositeInto(target);
			SKColor hiddenPixel = target.GetPixel(maskX + 10, maskY);
			Check(hiddenPixel.Alpha == 0, "mask hole tracks layer offset in canvas space");
			SKColor shownPixel = target.GetPixel(maskX, maskY);
			Check(shownPixel.Alpha == 255, "unmasked offset pixel stays opaque");
			target.Dispose();
		}
	}
}
