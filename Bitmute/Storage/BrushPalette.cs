using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SkiaSharp;
using Bitmute.Tools;

namespace Bitmute.Storage
{
	public class BrushPalette
	{
		private string m_manifestPath;
		private string m_manifestDirectory;
		private string m_brushesDirectory;
		private List<CustomBrush> m_brushes;

		public BrushPalette(string root)
		{
			m_manifestDirectory = Path.Combine(root, "Palettes");
			m_brushesDirectory = Path.Combine(root, "Brushes");
			PaletteFile.EnsureDirectory(m_manifestDirectory);
			PaletteFile.EnsureDirectory(m_brushesDirectory);
			m_manifestPath = Path.Combine(m_manifestDirectory, "brushes.plt");
			m_brushes = new List<CustomBrush>();
			if (File.Exists(m_manifestPath))
			{
				Load();
			}
			else
			{
				Save();
			}
		}

		public List<CustomBrush> CustomBrushes()
		{
			return m_brushes;
		}

		public CustomBrush AddCapturedTip(SKBitmap tip, string suggestedName)
		{
			string path = PaletteFile.UniqueResourcePath(m_brushesDirectory, suggestedName, ".png");
			PaletteFile.WritePng(tip, path);
			string relative = PaletteFile.ToRelative(m_manifestDirectory, path);
			CustomBrush brush = new CustomBrush(suggestedName, tip);
			brush.m_isProcedural = false;
			brush.m_shape = null;
			brush.m_relativePath = relative;
			m_brushes.Add(brush);
			Save();
			return brush;
		}

		public CustomBrush AddProcedural(ProceduralBrushShape shape, string suggestedName)
		{
			string path = PaletteFile.UniqueResourcePath(m_brushesDirectory, suggestedName, ".brush");
			WriteShape(shape, path);
			string relative = PaletteFile.ToRelative(m_manifestDirectory, path);
			CustomBrush brush = new CustomBrush(suggestedName, null);
			brush.m_isProcedural = true;
			brush.m_shape = shape;
			brush.m_relativePath = relative;
			m_brushes.Add(brush);
			Save();
			return brush;
		}

		public void Remove(CustomBrush brush)
		{
			m_brushes.Remove(brush);
			Save();
		}

		private static void WriteShape(ProceduralBrushShape shape, string path)
		{
			FileStream stream = File.Create(path);
			Utf8JsonWriter writer = new Utf8JsonWriter(stream);
			writer.WriteStartObject();
			writer.WriteNumber("size", shape.m_size);
			writer.WriteNumber("hardness", shape.m_hardness);
			writer.WriteNumber("spacing", shape.m_spacing);
			writer.WriteNumber("fade", shape.m_fade);
			writer.WriteBoolean("square", shape.m_square);
			writer.WriteNumber("roundness", shape.m_roundness);
			writer.WriteNumber("angle", shape.m_angle);
			writer.WriteNumber("smoothing", shape.m_smoothing);
			writer.WriteEndObject();
			writer.Flush();
			writer.Dispose();
			stream.Dispose();
		}

		private static int ReadShapeInt(JsonElement parent, string name)
		{
			JsonElement element;
			if (!parent.TryGetProperty(name, out element))
			{
				return 0;
			}
			if (element.ValueKind != JsonValueKind.Number)
			{
				return 0;
			}
			int value;
			if (!element.TryGetInt32(out value))
			{
				return 0;
			}
			return value;
		}

		private static bool ReadShapeBool(JsonElement parent, string name)
		{
			JsonElement element;
			if (!parent.TryGetProperty(name, out element))
			{
				return false;
			}
			if (element.ValueKind == JsonValueKind.True)
			{
				return true;
			}
			return false;
		}

		private static ProceduralBrushShape ReadShape(string path)
		{
			if (!File.Exists(path))
			{
				return null;
			}
			byte[] bytes = File.ReadAllBytes(path);
			JsonDocument document = JsonDocument.Parse(bytes);
			JsonElement root = document.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				document.Dispose();
				return null;
			}
			ProceduralBrushShape shape = new ProceduralBrushShape();
			shape.m_size = ReadShapeInt(root, "size");
			shape.m_hardness = ReadShapeInt(root, "hardness");
			shape.m_spacing = ReadShapeInt(root, "spacing");
			shape.m_fade = ReadShapeInt(root, "fade");
			shape.m_square = ReadShapeBool(root, "square");
			shape.m_roundness = ReadShapeInt(root, "roundness");
			shape.m_angle = ReadShapeInt(root, "angle");
			shape.m_smoothing = ReadShapeInt(root, "smoothing");
			document.Dispose();
			return shape;
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
				JsonElement kindElement;
				if (!entry.TryGetProperty("name", out nameElement))
				{
					continue;
				}
				if (!entry.TryGetProperty("path", out pathElement))
				{
					continue;
				}
				if (!entry.TryGetProperty("kind", out kindElement))
				{
					continue;
				}
				string name = nameElement.GetString();
				string relative = pathElement.GetString();
				string kind = kindElement.GetString();
				if (name == null || relative == null || kind == null)
				{
					continue;
				}
				string absolute = PaletteFile.ToAbsolute(m_manifestDirectory, relative);
				if (kind == "image")
				{
					SKBitmap tip = PaletteFile.ReadPngUnpremul(absolute);
					if (tip == null)
					{
						continue;
					}
					CustomBrush brush = new CustomBrush(name, tip);
					brush.m_isProcedural = false;
					brush.m_shape = null;
					brush.m_relativePath = relative;
					m_brushes.Add(brush);
					continue;
				}
				if (kind == "procedural")
				{
					ProceduralBrushShape shape = ReadShape(absolute);
					if (shape == null)
					{
						continue;
					}
					CustomBrush brush = new CustomBrush(name, null);
					brush.m_isProcedural = true;
					brush.m_shape = shape;
					brush.m_relativePath = relative;
					m_brushes.Add(brush);
					continue;
				}
			}
			document.Dispose();
		}

		private void Save()
		{
			FileStream stream = File.Create(m_manifestPath);
			Utf8JsonWriter writer = new Utf8JsonWriter(stream);
			writer.WriteStartObject();
			writer.WriteString("type", "brush");
			writer.WriteStartArray("entries");
			for (int i = 0; i < m_brushes.Count; i++)
			{
				CustomBrush brush = m_brushes[i];
				writer.WriteStartObject();
				writer.WriteString("name", brush.m_name);
				writer.WriteString("path", brush.m_relativePath);
				if (brush.m_isProcedural)
				{
					writer.WriteString("kind", "procedural");
				}
				else
				{
					writer.WriteString("kind", "image");
				}
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
