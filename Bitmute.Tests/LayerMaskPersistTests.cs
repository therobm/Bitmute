using System;
using System.IO;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Storage;

namespace Bitmute.Tests
{
	public static class LayerMaskPersistTests
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
			TestMaskRoundTrips();
			TestDisabledFlagRoundTrips();
			TestNoMaskDocumentUnaffected();
			return s_failures;
		}

		private static void TestMaskRoundTrips()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_mask_persist");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "mask_roundtrip.bitmute");
			Document doc = new Document("mask", 32, 24);
			doc.AddLayer("Paint");
			doc.SetActiveLayerIndex(0);
			Layer first = doc.Layers()[0];
			first.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			doc.AddMaskToActiveLayer(true);
			SKBitmap mask = first.MaskBitmap();
			mask.SetPixel(10, 10, new SKColor(0, 0, 0, 255));
			mask.SetPixel(20, 20, new SKColor(128, 128, 128, 255));
			bool wrote = BitmuteFile.Write(path, doc);
			Check(wrote, "mask persist write");
			Document back = BitmuteFile.Read(path);
			if (back == null)
			{
				Check(false, "mask persist read returned null");
				File.Delete(path);
				return;
			}
			Layer backFirst = back.Layers()[0];
			Check(backFirst.HasMask(), "mask persist layer 0 has mask");
			Check(backFirst.MaskEnabled(), "mask persist layer 0 mask enabled");
			SKColor blackMark = backFirst.MaskBitmap().GetPixel(10, 10);
			Check(blackMark.Red == 0, "mask persist black coverage survives");
			SKColor grayMark = backFirst.MaskBitmap().GetPixel(20, 20);
			CheckNear((int)grayMark.Red, 128, 2, "mask persist gray coverage survives");
			SKColor whiteMark = backFirst.MaskBitmap().GetPixel(5, 5);
			Check(whiteMark.Red == 255, "mask persist white coverage survives");
			Layer backSecond = back.Layers()[1];
			Check(backSecond.HasMask() == false, "mask persist layer 1 has no mask");
			File.Delete(path);
		}

		private static void TestDisabledFlagRoundTrips()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_mask_persist");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "mask_disabled.bitmute");
			Document doc = new Document("mask", 32, 24);
			doc.SetActiveLayerIndex(0);
			Layer first = doc.Layers()[0];
			first.Bitmap().Erase(new SKColor(255, 255, 255, 255));
			doc.AddMaskToActiveLayer(true);
			doc.SetActiveMaskEnabled(false);
			bool wrote = BitmuteFile.Write(path, doc);
			Check(wrote, "mask disabled write");
			Document back = BitmuteFile.Read(path);
			if (back == null)
			{
				Check(false, "mask disabled read returned null");
				File.Delete(path);
				return;
			}
			Layer backFirst = back.Layers()[0];
			Check(backFirst.HasMask(), "mask disabled still has mask");
			Check(backFirst.MaskEnabled() == false, "mask disabled flag round-trips");
			File.Delete(path);
		}

		private static void TestNoMaskDocumentUnaffected()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_mask_persist");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "no_mask.bitmute");
			Document doc = new Document("plain", 32, 24);
			doc.AddLayer("Paint");
			bool wrote = BitmuteFile.Write(path, doc);
			Check(wrote, "no mask write");
			Document back = BitmuteFile.Read(path);
			if (back == null)
			{
				Check(false, "no mask read returned null");
				File.Delete(path);
				return;
			}
			bool anyMask = false;
			for (int index = 0; index < back.Layers().Count; index++)
			{
				if (back.Layers()[index].HasMask())
				{
					anyMask = true;
				}
			}
			Check(anyMask == false, "no mask document has no masks");
			File.Delete(path);
		}
	}
}
