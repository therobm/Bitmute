using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
				Save();
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

		private void Load()
		{
			byte[] bytes = File.ReadAllBytes(m_manifestPath);
			JsonDocument document = JsonDocument.Parse(bytes);
			JsonElement root = document.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				document.Dispose();
				return;
			}
			JsonElement entries;
			if (!root.TryGetProperty("entries", out entries))
			{
				document.Dispose();
				return;
			}
			if (entries.ValueKind != JsonValueKind.Array)
			{
				document.Dispose();
				return;
			}
			foreach (JsonElement entry in entries.EnumerateArray())
			{
				if (entry.ValueKind != JsonValueKind.Object)
				{
					continue;
				}
				JsonElement nameElement;
				JsonElement pathElement;
				if (!entry.TryGetProperty("name", out nameElement))
				{
					continue;
				}
				if (!entry.TryGetProperty("path", out pathElement))
				{
					continue;
				}
				string name = nameElement.GetString();
				string relative = pathElement.GetString();
				if (name == null || relative == null)
				{
					continue;
				}
				string absolute = PaletteFile.ToAbsolute(m_manifestDirectory, relative);
				SKBitmap bitmap = PaletteFile.ReadPngUnpremul(absolute);
				if (bitmap == null)
				{
					continue;
				}
				Pattern pattern = new Pattern(name, bitmap);
				pattern.m_relativePath = relative;
				m_patterns.Add(pattern);
			}
			document.Dispose();
		}

		private void Save()
		{
			FileStream stream = File.Create(m_manifestPath);
			Utf8JsonWriter writer = new Utf8JsonWriter(stream);
			writer.WriteStartObject();
			writer.WriteString("type", "pattern");
			writer.WriteStartArray("entries");
			for (int i = 0; i < m_patterns.Count; i++)
			{
				Pattern pattern = m_patterns[i];
				writer.WriteStartObject();
				writer.WriteString("name", pattern.m_name);
				writer.WriteString("path", pattern.m_relativePath);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
			writer.Flush();
			writer.Dispose();
			stream.Dispose();
		}
	}
}
