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

		private static PatternPalette EmptyPatternPalette(string root)
		{
			Directory.CreateDirectory(Path.Combine(root, "Palettes"));
			File.WriteAllText(Path.Combine(root, "Palettes", "patterns.plt"), "{\"version\":1,\"entries\":[]}");
			return new PatternPalette(root);
		}

		private static BrushPalette EmptyBrushPalette(string root)
		{
			Directory.CreateDirectory(Path.Combine(root, "Palettes"));
			File.WriteAllText(Path.Combine(root, "Palettes", "brushes.plt"), "{\"version\":1,\"entries\":[]}");
			return new BrushPalette(root);
		}

		private static void TestPatternRoundTrip()
		{
			string root = FreshRoot();
			PatternPalette palette = EmptyPatternPalette(root);
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
			BrushPalette palette = EmptyBrushPalette(root);
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
			BrushPalette palette = EmptyBrushPalette(root);
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
			PatternPalette palette = EmptyPatternPalette(rootA);
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
			PatternPalette palette = EmptyPatternPalette(root);
			SKBitmap tile = BuildMarkedTile();
			Pattern pattern = palette.AddCaptured(tile, "RemovablePattern");
			string absolute = Path.GetFullPath(Path.Combine(root, "Palettes", pattern.m_relativePath));
			palette.Remove(pattern);
			Check(palette.Patterns().Count == 0, "remove empties live list");
			Check(File.Exists(absolute), "remove keeps the png on disk");

			PatternPalette reloaded = new PatternPalette(root);
			Check(reloaded.Patterns().Count == 0, "reload after remove is empty");
		}

		private static void TestNewerVersionRefused()
		{
			string root = FreshRoot();
			string paletteDirectory = Path.Combine(root, "Palettes");
			Directory.CreateDirectory(paletteDirectory);
			string manifestPath = Path.Combine(paletteDirectory, "patterns.plt");
			File.WriteAllText(manifestPath, "{\"version\":999,\"entries\":[]}");
			PatternPalette palette = new PatternPalette(root);
			Check(palette.Patterns().Count == 0, "newer manifest version is refused, not misparsed");
		}

		private static CustomBrush FindBrush(BrushPalette palette, string name)
		{
			for (int i = 0; i < palette.CustomBrushes().Count; i++)
			{
				CustomBrush brush = palette.CustomBrushes()[i];
				if (brush.m_name == name)
				{
					return brush;
				}
			}
			return null;
		}

		private static void TestDefaultPatternsSeeded()
		{
			string root = FreshRoot();
			PatternPalette palette = new PatternPalette(root);
			Check(palette.Patterns().Count == 3, "default patterns seeded (3)");
			PatternPalette reloaded = new PatternPalette(root);
			Check(reloaded.Patterns().Count == 3, "default patterns persist and are not re-seeded");
		}

		private static void TestDefaultBrushesSeeded()
		{
			string root = FreshRoot();
			BrushPalette palette = new BrushPalette(root);
			Check(palette.CustomBrushes().Count == 2, "default brushes seeded (2)");
			if (palette.CustomBrushes().Count != 2)
			{
				return;
			}
			bool allImageTips = true;
			for (int i = 0; i < palette.CustomBrushes().Count; i++)
			{
				CustomBrush brush = palette.CustomBrushes()[i];
				if (brush.m_isProcedural || brush.m_tip == null)
				{
					allImageTips = false;
				}
			}
			Check(allImageTips, "default brushes are image tips");
			BrushPalette reloaded = new BrushPalette(root);
			Check(reloaded.CustomBrushes().Count == 2, "default brushes persist and are not re-seeded");
		}

		private static void TestDefaultTipEncoding()
		{
			string root = FreshRoot();
			BrushPalette palette = new BrushPalette(root);
			CustomBrush hard = FindBrush(palette, "Hard Round");
			CustomBrush soft = FindBrush(palette, "Soft Round");
			if (hard == null || soft == null)
			{
				Check(false, "default tips present by name");
				return;
			}
			SKColor hardCenter = hard.m_tip.GetPixel(hard.m_tip.Width / 2, hard.m_tip.Height / 2);
			Check(hardCenter.Alpha == 255 && hardCenter.Red == 0 && hardCenter.Green == 0 && hardCenter.Blue == 0, "hard tip center is opaque black (full coverage)");
			SKColor hardCorner = hard.m_tip.GetPixel(0, 0);
			Check(hardCorner.Alpha == 0, "hard tip corner is transparent (no coverage)");
			SKColor softCenter = soft.m_tip.GetPixel(soft.m_tip.Width / 2, soft.m_tip.Height / 2);
			SKColor softEdge = soft.m_tip.GetPixel(soft.m_tip.Width / 2, soft.m_tip.Height - 3);
			Check(softCenter.Alpha > 200, "soft tip center is near-opaque (high coverage)");
			Check(softEdge.Alpha < softCenter.Alpha, "soft tip falls off toward the edge");
		}

		private static void TestRenamePersists()
		{
			string root = FreshRoot();
			PatternPalette palette = EmptyPatternPalette(root);
			SKBitmap tile = BuildMarkedTile();
			Pattern pattern = palette.AddCaptured(tile, "OriginalName");
			palette.Rename(pattern, "RenamedName");
			Check(pattern.m_name == "RenamedName", "rename updates live name");

			PatternPalette reloaded = new PatternPalette(root);
			Check(reloaded.Patterns().Count == 1, "rename reload has one entry");
			if (reloaded.Patterns().Count != 1)
			{
				return;
			}
			Check(reloaded.Patterns()[0].m_name == "RenamedName", "rename persists across reload");
		}

		private static void TestMoveReorders()
		{
			string root = FreshRoot();
			PatternPalette palette = EmptyPatternPalette(root);
			palette.AddCaptured(BuildMarkedTile(), "First");
			palette.AddCaptured(BuildMarkedTile(), "Second");
			palette.AddCaptured(BuildMarkedTile(), "Third");
			Pattern third = palette.Patterns()[2];
			palette.Move(third, 0);
			Check(palette.Patterns()[0].m_name == "Third", "move places item at target index");

			PatternPalette reloaded = new PatternPalette(root);
			Check(reloaded.Patterns().Count == 3, "move reload has three entries");
			if (reloaded.Patterns().Count != 3)
			{
				return;
			}
			bool orderMatches = reloaded.Patterns()[0].m_name == "Third" && reloaded.Patterns()[1].m_name == "First" && reloaded.Patterns()[2].m_name == "Second";
			Check(orderMatches, "move order persists across reload");
		}

		private static void TestImportMerges()
		{
			string rootX = FreshRoot();
			PatternPalette source = EmptyPatternPalette(rootX);
			source.AddCaptured(BuildMarkedTile(), "AlphaEntry");
			source.AddCaptured(BuildMarkedTile(), "BetaEntry");
			string sourceManifest = Path.Combine(rootX, "Palettes", "patterns.plt");

			string rootY = FreshRoot();
			PatternPalette target = EmptyPatternPalette(rootY);
			int added = target.ImportFrom(sourceManifest);
			Check(added == 2, "import returns two added");
			Check(target.Patterns().Count == 2, "import merges two entries");
			if (target.Patterns().Count != 2)
			{
				return;
			}
			Pattern imported = target.Patterns()[0];
			string copied = Path.GetFullPath(Path.Combine(rootY, "Palettes", imported.m_relativePath));
			Check(File.Exists(copied), "import copies file into target resource dir");
			Check(copied.Contains(rootY), "import copy lives under target root");
			if (imported.m_bitmap == null)
			{
				Check(false, "import decodes tile");
				return;
			}
			SKColor marker = imported.m_bitmap.GetPixel(3, 4);
			Check(marker.Red == 200 && marker.Green == 90 && marker.Blue == 40 && marker.Alpha == 128, "import marker pixel survives");
		}

		private static void TestExportRoundTrip()
		{
			string root = FreshRoot();
			BrushPalette source = EmptyBrushPalette(root);
			source.AddCapturedTip(BuildMarkedTile(), "ExportedTip");
			ProceduralBrushShape shape = new ProceduralBrushShape();
			shape.m_size = 21;
			shape.m_hardness = 55;
			shape.m_spacing = 33;
			shape.m_fade = 7;
			shape.m_square = false;
			shape.m_roundness = 80;
			shape.m_angle = 45;
			shape.m_smoothing = 12;
			source.AddProcedural(shape, "ExportedPreset");

			string exportDirectory = Path.Combine(FreshRoot(), "exported");
			Directory.CreateDirectory(exportDirectory);
			string exportManifest = Path.Combine(exportDirectory, "set.plt");
			source.ExportTo(exportManifest);
			Check(File.Exists(exportManifest), "export writes manifest");

			string freshRoot = FreshRoot();
			BrushPalette destination = EmptyBrushPalette(freshRoot);
			int added = destination.ImportFrom(exportManifest);
			Check(added == 2, "export round-trip imports two");
			if (destination.CustomBrushes().Count != 2)
			{
				Check(false, "export round-trip has two entries");
				return;
			}
			CustomBrush tipBrush = FindBrush(destination, "ExportedTip");
			CustomBrush presetBrush = FindBrush(destination, "ExportedPreset");
			if (tipBrush == null || presetBrush == null)
			{
				Check(false, "export round-trip preserves both by name");
				return;
			}
			if (tipBrush.m_tip == null)
			{
				Check(false, "export round-trip tip decodes");
				return;
			}
			SKColor marker = tipBrush.m_tip.GetPixel(3, 4);
			Check(marker.Red == 200 && marker.Green == 90 && marker.Blue == 40 && marker.Alpha == 128, "export round-trip tip pixel survives");
			if (presetBrush.m_shape == null)
			{
				Check(false, "export round-trip preset has shape");
				return;
			}
			bool presetSurvives = presetBrush.m_shape.m_size == 21 && presetBrush.m_shape.m_roundness == 80 && presetBrush.m_shape.m_angle == 45 && presetBrush.m_shape.m_smoothing == 12;
			Check(presetSurvives, "export round-trip preset params survive");
		}

		public static int RunAll()
		{
			s_failures = 0;
			TestPatternRoundTrip();
			TestBrushTipRoundTrip();
			TestProceduralRoundTrip();
			TestPortableRelativePaths();
			TestRemoveKeepsFile();
			TestNewerVersionRefused();
			TestDefaultPatternsSeeded();
			TestDefaultBrushesSeeded();
			TestDefaultTipEncoding();
			TestRenamePersists();
			TestMoveReorders();
			TestImportMerges();
			TestExportRoundTrip();
			return s_failures;
		}
	}
}
