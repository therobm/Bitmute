using System.Collections.Generic;
using System.IO;
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

		private static ProceduralBrushData ShapeToData(ProceduralBrushShape shape)
		{
			ProceduralBrushData data = new ProceduralBrushData();
			data.size = shape.m_size;
			data.hardness = shape.m_hardness;
			data.spacing = shape.m_spacing;
			data.fade = shape.m_fade;
			data.square = shape.m_square;
			data.roundness = shape.m_roundness;
			data.angle = shape.m_angle;
			data.smoothing = shape.m_smoothing;
			return data;
		}

		private static ProceduralBrushShape DataToShape(ProceduralBrushData data)
		{
			ProceduralBrushShape shape = new ProceduralBrushShape();
			shape.m_size = data.size;
			shape.m_hardness = data.hardness;
			shape.m_spacing = data.spacing;
			shape.m_fade = data.fade;
			shape.m_square = data.square;
			shape.m_roundness = data.roundness;
			shape.m_angle = data.angle;
			shape.m_smoothing = data.smoothing;
			return shape;
		}

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
				if (entry.kind == ePaletteEntryKind.Image)
				{
					SKBitmap tip = PaletteFile.ReadPngUnpremul(absolute);
					if (tip == null)
					{
						continue;
					}
					CustomBrush brush = new CustomBrush(entry.name, tip);
					brush.m_isProcedural = false;
					brush.m_shape = null;
					brush.m_relativePath = entry.path;
					m_brushes.Add(brush);
					continue;
				}
				if (entry.kind == ePaletteEntryKind.Procedural)
				{
					ProceduralBrushData data = PaletteFile.ReadShapeData(absolute);
					if (data == null)
					{
						continue;
					}
					CustomBrush brush = new CustomBrush(entry.name, null);
					brush.m_isProcedural = true;
					brush.m_shape = DataToShape(data);
					brush.m_relativePath = entry.path;
					m_brushes.Add(brush);
					continue;
				}
			}
		}

		private void Save()
		{
			PaletteManifest manifest = new PaletteManifest();
			manifest.entries = new List<PaletteEntry>();
			for (int i = 0; i < m_brushes.Count; i++)
			{
				CustomBrush brush = m_brushes[i];
				PaletteEntry entry = new PaletteEntry();
				entry.name = brush.m_name;
				entry.path = brush.m_relativePath;
				if (brush.m_isProcedural)
				{
					entry.kind = ePaletteEntryKind.Procedural;
				}
				else
				{
					entry.kind = ePaletteEntryKind.Image;
				}
				manifest.entries.Add(entry);
			}
			PaletteFile.WriteManifest(m_manifestPath, manifest);
		}

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
			ProceduralBrushData data = ShapeToData(shape);
			PaletteFile.WriteShapeData(path, data);
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
	}
}
