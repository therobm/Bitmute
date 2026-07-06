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
				DefaultPalette.SeedBrushes(this);
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

		public void Rename(CustomBrush brush, string newName)
		{
			if (!m_brushes.Contains(brush))
			{
				return;
			}
			brush.m_name = newName;
			Save();
		}

		public void Move(CustomBrush brush, int targetIndex)
		{
			if (!m_brushes.Contains(brush))
			{
				return;
			}
			m_brushes.Remove(brush);
			int clamped = targetIndex;
			if (clamped < 0)
			{
				clamped = 0;
			}
			if (clamped > m_brushes.Count)
			{
				clamped = m_brushes.Count;
			}
			m_brushes.Insert(clamped, brush);
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
				if (entry.kind == ePaletteEntryKind.Procedural)
				{
					string copy = PaletteFile.UniqueResourcePath(m_brushesDirectory, baseName, ".brush");
					File.Copy(sourceFile, copy, false);
					ProceduralBrushData data = PaletteFile.ReadShapeData(copy);
					if (data == null)
					{
						File.Delete(copy);
						continue;
					}
					string relative = PaletteFile.ToRelative(m_manifestDirectory, copy);
					CustomBrush brush = new CustomBrush(entry.name, null);
					brush.m_isProcedural = true;
					brush.m_shape = DataToShape(data);
					brush.m_relativePath = relative;
					m_brushes.Add(brush);
					added = added + 1;
					continue;
				}
				string tipCopy = PaletteFile.UniqueResourcePath(m_brushesDirectory, baseName, ".png");
				File.Copy(sourceFile, tipCopy, false);
				SKBitmap tip = PaletteFile.ReadPngUnpremul(tipCopy);
				if (tip == null)
				{
					File.Delete(tipCopy);
					continue;
				}
				string tipRelative = PaletteFile.ToRelative(m_manifestDirectory, tipCopy);
				CustomBrush tipBrush = new CustomBrush(entry.name, tip);
				tipBrush.m_isProcedural = false;
				tipBrush.m_shape = null;
				tipBrush.m_relativePath = tipRelative;
				m_brushes.Add(tipBrush);
				added = added + 1;
			}
			Save();
			return added;
		}

		public void ExportTo(string pltPath)
		{
			string targetDirectory = Path.GetDirectoryName(pltPath);
			PaletteFile.EnsureDirectory(targetDirectory);
			string resourceDirectory = Path.Combine(targetDirectory, "Brushes");
			PaletteFile.EnsureDirectory(resourceDirectory);
			PaletteManifest manifest = new PaletteManifest();
			manifest.entries = new List<PaletteEntry>();
			for (int i = 0; i < m_brushes.Count; i++)
			{
				CustomBrush brush = m_brushes[i];
				string sourceFile = PaletteFile.ToAbsolute(m_manifestDirectory, brush.m_relativePath);
				if (!File.Exists(sourceFile))
				{
					continue;
				}
				string baseName = Path.GetFileNameWithoutExtension(sourceFile);
				string extension = ".png";
				if (brush.m_isProcedural)
				{
					extension = ".brush";
				}
				string copy = PaletteFile.UniqueResourcePath(resourceDirectory, baseName, extension);
				File.Copy(sourceFile, copy, false);
				PaletteEntry entry = new PaletteEntry();
				entry.name = brush.m_name;
				entry.path = PaletteFile.ToRelative(targetDirectory, copy);
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
			PaletteFile.WriteManifest(pltPath, manifest);
		}
	}
}
