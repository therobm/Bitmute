using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public class Document
	{
		private const int MaxUndoDepth = 100;

		private int m_width;
		private int m_height;
		private string m_title;
		private List<Layer> m_layers;
		private int m_activeLayerIndex;
		private List<LayerEditCommand> m_undoStack;
		private List<LayerEditCommand> m_redoStack;
		private SKBitmap m_strokeSnapshot;
		private int m_strokeLayerIndex;
		private Selection m_selection;

		public static Document OpenImage(string title, SKBitmap source)
		{
			Document document = new Document(title, source.Width, source.Height);
			document.ActiveLayer().SetPixelsFrom(source);
			return document;
		}

		public Document(string title, int width, int height)
		{
			m_title = title;
			m_width = width;
			m_height = height;
			m_layers = new List<Layer>();
			Layer background = new Layer("Background", width, height);
			background.FillWhite();
			m_layers.Add(background);
			m_activeLayerIndex = 0;
			m_undoStack = new List<LayerEditCommand>();
			m_redoStack = new List<LayerEditCommand>();
			m_strokeSnapshot = null;
			m_strokeLayerIndex = 0;
			m_selection = new Selection(width, height);
		}

		public Selection Selection()
		{
			return m_selection;
		}

		public void BeginStroke()
		{
			if (m_strokeSnapshot != null)
			{
				m_strokeSnapshot.Dispose();
				m_strokeSnapshot = null;
			}
			Layer active = ActiveLayer();
			if (active == null)
			{
				return;
			}
			m_strokeLayerIndex = m_activeLayerIndex;
			m_strokeSnapshot = active.Bitmap().Copy();
		}

		public void EndStroke()
		{
			if (m_strokeSnapshot == null)
			{
				return;
			}
			if (m_strokeLayerIndex < 0 || m_strokeLayerIndex >= m_layers.Count)
			{
				m_strokeSnapshot.Dispose();
				m_strokeSnapshot = null;
				return;
			}
			SKBitmap current = m_layers[m_strokeLayerIndex].Bitmap();
			SKRectI rect = PixelRegion.ComputeDirtyRect(m_strokeSnapshot, current);
			if (rect.Width <= 0 || rect.Height <= 0)
			{
				m_strokeSnapshot.Dispose();
				m_strokeSnapshot = null;
				return;
			}
			SKBitmap before = PixelRegion.ExtractRegion(m_strokeSnapshot, rect);
			SKBitmap after = PixelRegion.ExtractRegion(current, rect);
			LayerEditCommand command = new LayerEditCommand(m_strokeLayerIndex, rect, before, after);
			m_undoStack.Add(command);
			m_redoStack.Clear();
			if (m_undoStack.Count > MaxUndoDepth)
			{
				m_undoStack.RemoveAt(0);
			}
			m_strokeSnapshot.Dispose();
			m_strokeSnapshot = null;
		}

		public bool Undo()
		{
			if (m_undoStack.Count == 0)
			{
				return false;
			}
			int last = m_undoStack.Count - 1;
			LayerEditCommand command = m_undoStack[last];
			m_undoStack.RemoveAt(last);
			int index = command.LayerIndex();
			if (index >= 0 && index < m_layers.Count)
			{
				command.ApplyBefore(m_layers[index].Bitmap());
			}
			m_redoStack.Add(command);
			return true;
		}

		public bool Redo()
		{
			if (m_redoStack.Count == 0)
			{
				return false;
			}
			int last = m_redoStack.Count - 1;
			LayerEditCommand command = m_redoStack[last];
			m_redoStack.RemoveAt(last);
			int index = command.LayerIndex();
			if (index >= 0 && index < m_layers.Count)
			{
				command.ApplyAfter(m_layers[index].Bitmap());
			}
			m_undoStack.Add(command);
			return true;
		}

		public int Width()
		{
			return m_width;
		}

		public int Height()
		{
			return m_height;
		}

		public string Title()
		{
			return m_title;
		}

		public List<Layer> Layers()
		{
			return m_layers;
		}

		public int ActiveLayerIndex()
		{
			return m_activeLayerIndex;
		}

		public void SetActiveLayerIndex(int index)
		{
			if (index < 0)
			{
				return;
			}
			if (index >= m_layers.Count)
			{
				return;
			}
			m_activeLayerIndex = index;
		}

		public Layer ActiveLayer()
		{
			if (m_activeLayerIndex < 0)
			{
				return null;
			}
			if (m_activeLayerIndex >= m_layers.Count)
			{
				return null;
			}
			return m_layers[m_activeLayerIndex];
		}

		public Layer AddLayer(string name)
		{
			Layer layer = new Layer(name, m_width, m_height);
			m_layers.Add(layer);
			m_activeLayerIndex = m_layers.Count - 1;
			return layer;
		}

		public void DeleteLayer(int index)
		{
			if (m_layers.Count <= 1)
			{
				return;
			}
			if (index < 0 || index >= m_layers.Count)
			{
				return;
			}
			m_layers.RemoveAt(index);
			if (m_activeLayerIndex >= m_layers.Count)
			{
				m_activeLayerIndex = m_layers.Count - 1;
			}
		}

		public void CompositeInto(SKBitmap target)
		{
			SKCanvas canvas = new SKCanvas(target);
			canvas.Clear(SKColors.Transparent);
			SKPaint paint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				paint.Color = SKColors.White.WithAlpha(layer.Opacity());
				paint.BlendMode = Layer.ToSkBlendMode(layer.BlendMode());
				SKImage image = SKImage.FromBitmap(layer.Bitmap());
				canvas.DrawImage(image, 0.0f, 0.0f, sampling, paint);
				image.Dispose();
			}
			paint.Dispose();
			canvas.Dispose();
		}
	}
}
