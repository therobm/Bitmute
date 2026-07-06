using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Storage
{
	public class PatternPalette
	{
		private string m_manifestPath;
		private string m_manifestDirectory;
		private string m_patternsDirectory;
		private List<Pattern> m_patterns;

		private void Load()
		{
			PaletteManifest manifest = PaletteFile.ReadManifest(m_manifestPath);
			if (manifest == null)
			{
				return;
			}
			for (int i = 0; i < manifest.entries.Count; i++)
			{
				PaletteEntry entry = manifest.entries[i];
				if (entry.name == null || entry.path == null)
				{
					continue;
				}
				string absolute = PaletteFile.ToAbsolute(m_manifestDirectory, entry.path);
				SKBitmap bitmap = PaletteFile.ReadPngUnpremul(absolute);
				if (bitmap == null)
				{
					continue;
				}
				Pattern pattern = new Pattern(entry.name, bitmap);
				pattern.m_relativePath = entry.path;
				m_patterns.Add(pattern);
			}
		}

		private void Save()
		{
			PaletteManifest manifest = new PaletteManifest();
			manifest.entries = new List<PaletteEntry>();
			for (int i = 0; i < m_patterns.Count; i++)
			{
				Pattern pattern = m_patterns[i];
				PaletteEntry entry = new PaletteEntry();
				entry.name = pattern.m_name;
				entry.path = pattern.m_relativePath;
				entry.kind = ePaletteEntryKind.Image;
				manifest.entries.Add(entry);
			}
			PaletteFile.WriteManifest(m_manifestPath, manifest);
		}

		public PatternPalette(string root)
		{
			m_manifestDirectory = Path.Combine(root, "Palettes");
			m_patternsDirectory = Path.Combine(root, "Patterns");
			PaletteFile.EnsureDirectory(m_manifestDirectory);
			PaletteFile.EnsureDirectory(m_patternsDirectory);
			m_manifestPath = Path.Combine(m_manifestDirectory, "patterns.plt");
			m_patterns = new List<Pattern>();
			if (File.Exists(m_manifestPath))
			{
				Load();
			}
			else
			{
				DefaultPalette.SeedPatterns(this);
			}
		}

		public List<Pattern> Patterns()
		{
			return m_patterns;
		}

		public Pattern AddCaptured(SKBitmap tile, string suggestedName)
		{
			string path = PaletteFile.UniqueResourcePath(m_patternsDirectory, suggestedName, ".png");
			PaletteFile.WritePng(tile, path);
			string relative = PaletteFile.ToRelative(m_manifestDirectory, path);
			Pattern pattern = new Pattern(suggestedName, tile);
			pattern.m_relativePath = relative;
			m_patterns.Add(pattern);
			Save();
			return pattern;
		}

		public void Remove(Pattern pattern)
		{
			m_patterns.Remove(pattern);
			Save();
		}
	}
}
