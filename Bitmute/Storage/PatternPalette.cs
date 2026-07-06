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

		public void Rename(Pattern pattern, string newName)
		{
			if (!m_patterns.Contains(pattern))
			{
				return;
			}
			pattern.m_name = newName;
			Save();
		}

		public void Move(Pattern pattern, int targetIndex)
		{
			if (!m_patterns.Contains(pattern))
			{
				return;
			}
			m_patterns.Remove(pattern);
			int clamped = targetIndex;
			if (clamped < 0)
			{
				clamped = 0;
			}
			if (clamped > m_patterns.Count)
			{
				clamped = m_patterns.Count;
			}
			m_patterns.Insert(clamped, pattern);
			Save();
		}

		public int ImportFrom(string pltPath)
		{
			PaletteManifest manifest = PaletteFile.ReadManifest(pltPath);
			if (manifest == null)
			{
				return 0;
			}
			string sourceDirectory = Path.GetDirectoryName(pltPath);
			int added = 0;
			for (int i = 0; i < manifest.entries.Count; i++)
			{
				PaletteEntry entry = manifest.entries[i];
				if (entry.name == null || entry.path == null)
				{
					continue;
				}
				string sourceFile = PaletteFile.ToAbsolute(sourceDirectory, entry.path);
				if (!File.Exists(sourceFile))
				{
					continue;
				}
				string baseName = Path.GetFileNameWithoutExtension(sourceFile);
				string copy = PaletteFile.UniqueResourcePath(m_patternsDirectory, baseName, ".png");
				File.Copy(sourceFile, copy, false);
				SKBitmap bitmap = PaletteFile.ReadPngUnpremul(copy);
				if (bitmap == null)
				{
					File.Delete(copy);
					continue;
				}
				string relative = PaletteFile.ToRelative(m_manifestDirectory, copy);
				Pattern pattern = new Pattern(entry.name, bitmap);
				pattern.m_relativePath = relative;
				m_patterns.Add(pattern);
				added = added + 1;
			}
			Save();
			return added;
		}

		public void ExportTo(string pltPath)
		{
			string targetDirectory = Path.GetDirectoryName(pltPath);
			PaletteFile.EnsureDirectory(targetDirectory);
			string resourceDirectory = Path.Combine(targetDirectory, "Patterns");
			PaletteFile.EnsureDirectory(resourceDirectory);
			PaletteManifest manifest = new PaletteManifest();
			manifest.entries = new List<PaletteEntry>();
			for (int i = 0; i < m_patterns.Count; i++)
			{
				Pattern pattern = m_patterns[i];
				string sourceFile = PaletteFile.ToAbsolute(m_manifestDirectory, pattern.m_relativePath);
				if (!File.Exists(sourceFile))
				{
					continue;
				}
				string baseName = Path.GetFileNameWithoutExtension(sourceFile);
				string copy = PaletteFile.UniqueResourcePath(resourceDirectory, baseName, ".png");
				File.Copy(sourceFile, copy, false);
				PaletteEntry entry = new PaletteEntry();
				entry.name = pattern.m_name;
				entry.path = PaletteFile.ToRelative(targetDirectory, copy);
				entry.kind = ePaletteEntryKind.Image;
				manifest.entries.Add(entry);
			}
			PaletteFile.WriteManifest(pltPath, manifest);
		}
	}
}
