using System;
using System.IO;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;
using Bitmute.Storage;

namespace Bitmute.Tests
{
	public static class PalettePersistenceTests
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

		private static string FreshRoot()
		{
			return Path.Combine(Path.GetTempPath(), "bitmute_plt_" + Guid.NewGuid().ToString("N"));
		}

		private static SKBitmap BuildMarkedTile()
		{
			SKBitmap bitmap = new SKBitmap(6, 6, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			for (int y = 0; y < 6; y++)
			{
				for (int x = 0; x < 6; x++)
				{
					bitmap.SetPixel(x, y, new SKColor(10, 20, 30, 255));
				}
			}
			bitmap.SetPixel(3, 4, new SKColor(200, 90, 40, 128));
			return bitmap;
		}

		private static void CopyTree(string source, string destination)
		{
			Directory.CreateDirectory(destination);
			string[] files = Directory.GetFiles(source);
			for (int i = 0; i < files.Length; i++)
			{
				string file = files[i];
				string name = Path.GetFileName(file);
				File.Copy(file, Path.Combine(destination, name), true);
			}
			string[] directories = Directory.GetDirectories(source);
			for (int i = 0; i < directories.Length; i++)
			{
				string directory = directories[i];
				string name = Path.GetFileName(directory);
				CopyTree(directory, Path.Combine(destination, name));
			}
		}

		private static void TestPatternRoundTrip()
		{
			string root = FreshRoot();
			PatternPalette palette = new PatternPalette(root);
			SKBitmap tile = BuildMarkedTile();
			Pattern pattern = palette.AddCaptured(tile, "MarkerPattern");
			string absolute = Path.GetFullPath(Path.Combine(root, "Palettes", pattern.m_relativePath));
			Check(File.Exists(absolute), "pattern png written to disk");

			PatternPalette reloaded = new PatternPalette(root);
			Check(reloaded.Patterns().Count == 1, "pattern reload has one entry");
			if (reloaded.Patterns().Count != 1)
			{
				return;
			}
			Pattern back = reloaded.Patterns()[0];
			Check(back.m_name == "MarkerPattern", "pattern reload name matches");
			SKColor marker = back.m_bitmap.GetPixel(3, 4);
			Check(marker.Red == 200 && marker.Green == 90 && marker.Blue == 40 && marker.Alpha == 128, "pattern reload marker pixel survives without premul drift");
		}

		private static void TestBrushTipRoundTrip()
		{
			string root = FreshRoot();
			BrushPalette palette = new BrushPalette(root);
			SKBitmap tip = BuildMarkedTile();
			CustomBrush brush = palette.AddCapturedTip(tip, "MarkerTip");
			string absolute = Path.GetFullPath(Path.Combine(root, "Palettes", brush.m_relativePath));
			Check(File.Exists(absolute), "brush tip png written to disk");

			BrushPalette reloaded = new BrushPalette(root);
			Check(reloaded.CustomBrushes().Count == 1, "brush tip reload has one entry");
			if (reloaded.CustomBrushes().Count != 1)
			{
				return;
			}
			CustomBrush back = reloaded.CustomBrushes()[0];
			Check(back.m_name == "MarkerTip", "brush tip reload name matches");
			Check(back.m_isProcedural == false, "brush tip reload is not procedural");
			Check(back.m_tip != null, "brush tip reload has a tip bitmap");
			if (back.m_tip == null)
			{
				return;
			}
			SKColor marker = back.m_tip.GetPixel(3, 4);
			Check(marker.Red == 200 && marker.Green == 90 && marker.Blue == 40 && marker.Alpha == 128, "brush tip reload marker pixel survives without premul drift");
		}

		private static void TestProceduralRoundTrip()
		{
			string root = FreshRoot();
			BrushPalette palette = new BrushPalette(root);
			ProceduralBrushShape shape = new ProceduralBrushShape();
			shape.m_size = 37;
			shape.m_hardness = 62;
			shape.m_spacing = 44;
			shape.m_fade = 19;
			shape.m_square = true;
			shape.m_roundness = 71;
			shape.m_angle = 133;
			shape.m_smoothing = 28;
			palette.AddProcedural(shape, "MarkerPreset");

			BrushPalette reloaded = new BrushPalette(root);
			Check(reloaded.CustomBrushes().Count == 1, "procedural reload has one entry");
			if (reloaded.CustomBrushes().Count != 1)
			{
				return;
			}
			CustomBrush back = reloaded.CustomBrushes()[0];
			Check(back.m_isProcedural == true, "procedural reload is procedural");
			Check(back.m_tip == null, "procedural reload has no tip");
			if (back.m_shape == null)
			{
				Check(false, "procedural reload has a shape");
				return;
			}
			ProceduralBrushShape backShape = back.m_shape;
			bool allSurvive = backShape.m_size == 37 && backShape.m_hardness == 62 && backShape.m_spacing == 44 && backShape.m_fade == 19 && backShape.m_square == true && backShape.m_roundness == 71 && backShape.m_angle == 133 && backShape.m_smoothing == 28;
			Check(allSurvive, "procedural reload preserves all eight params");
		}

		private static void TestPortableRelativePaths()
		{
			string rootA = FreshRoot();
			PatternPalette palette = new PatternPalette(rootA);
			SKBitmap tile = BuildMarkedTile();
			palette.AddCaptured(tile, "PortablePattern");
			string rootB = FreshRoot();
			CopyTree(rootA, rootB);

			PatternPalette moved = new PatternPalette(rootB);
			Check(moved.Patterns().Count == 1, "copied palette resolves one entry");
			if (moved.Patterns().Count != 1)
			{
				return;
			}
			Pattern back = moved.Patterns()[0];
			Check(back.m_bitmap != null, "copied palette tile decodes");
			if (back.m_bitmap == null)
			{
				return;
			}
			SKColor marker = back.m_bitmap.GetPixel(3, 4);
			Check(marker.Red == 200 && marker.Green == 90 && marker.Blue == 40 && marker.Alpha == 128, "copied palette tile marker pixel intact");
		}

		private static void TestRemoveKeepsFile()
		{
			string root = FreshRoot();
			PatternPalette palette = new PatternPalette(root);
			SKBitmap tile = BuildMarkedTile();
			Pattern pattern = palette.AddCaptured(tile, "RemovablePattern");
			string absolute = Path.GetFullPath(Path.Combine(root, "Palettes", pattern.m_relativePath));
			palette.Remove(pattern);
			Check(palette.Patterns().Count == 0, "remove empties live list");
			Check(File.Exists(absolute), "remove keeps the png on disk");

			PatternPalette reloaded = new PatternPalette(root);
			Check(reloaded.Patterns().Count == 0, "reload after remove is empty");
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestPatternRoundTrip();
			TestBrushTipRoundTrip();
			TestProceduralRoundTrip();
			TestPortableRelativePaths();
			TestRemoveKeepsFile();
			return s_failures;
		}
	}
}
