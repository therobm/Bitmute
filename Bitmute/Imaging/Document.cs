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
		private List<EditCommand> m_undoStack;
		private List<EditCommand> m_redoStack;
		private SKBitmap m_strokeSnapshot;
		private int m_strokeLayerIndex;
		private Selection m_selection;
		private SKRectI m_composeDirtyRect;
		private bool m_composeDirtyAny;
		private bool m_composeDirtyAll;
		private SKRectI m_strokeDirtyRect;
		private bool m_strokeDirtyValid;

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
			m_undoStack = new List<EditCommand>();
			m_redoStack = new List<EditCommand>();
			m_strokeSnapshot = null;
			m_strokeLayerIndex = 0;
			m_selection = new Selection(width, height);
			m_composeDirtyRect = SKRectI.Empty;
			m_composeDirtyAny = false;
			m_composeDirtyAll = false;
			m_strokeDirtyRect = SKRectI.Empty;
			m_strokeDirtyValid = false;
		}

		private static SKRectI UnionRects(SKRectI first, SKRectI second)
		{
			int left = first.Left;
			if (second.Left < left)
			{
				left = second.Left;
			}
			int top = first.Top;
			if (second.Top < top)
			{
				top = second.Top;
			}
			int right = first.Right;
			if (second.Right > right)
			{
				right = second.Right;
			}
			int bottom = first.Bottom;
			if (second.Bottom > bottom)
			{
				bottom = second.Bottom;
			}
			return new SKRectI(left, top, right, bottom);
		}

		public void MarkComposeDirtyAll()
		{
			m_composeDirtyAny = true;
			m_composeDirtyAll = true;
		}

		public void MarkComposeDirtyRegion(SKRectI canvasRect)
		{
			if (canvasRect.Width <= 0 || canvasRect.Height <= 0)
			{
				return;
			}
			if (!m_composeDirtyAny || m_composeDirtyRect.Width <= 0)
			{
				m_composeDirtyRect = canvasRect;
			}
			else
			{
				m_composeDirtyRect = UnionRects(m_composeDirtyRect, canvasRect);
			}
			m_composeDirtyAny = true;
			if (!m_strokeDirtyValid)
			{
				m_strokeDirtyRect = canvasRect;
				m_strokeDirtyValid = true;
			}
			else
			{
				m_strokeDirtyRect = UnionRects(m_strokeDirtyRect, canvasRect);
			}
		}

		public bool ComposeDirtyAny()
		{
			return m_composeDirtyAny;
		}

		public bool ComposeDirtyAll()
		{
			return m_composeDirtyAll;
		}

		public SKRectI ComposeDirtyRect()
		{
			return m_composeDirtyRect;
		}

		public void ClearComposeDirty()
		{
			m_composeDirtyRect = SKRectI.Empty;
			m_composeDirtyAny = false;
			m_composeDirtyAll = false;
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
			m_strokeDirtyRect = SKRectI.Empty;
			m_strokeDirtyValid = false;
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
			if (current.Width != m_strokeSnapshot.Width || current.Height != m_strokeSnapshot.Height)
			{
				m_strokeSnapshot.Dispose();
				m_strokeSnapshot = null;
				return;
			}
			SKRectI searchRect = new SKRectI(0, 0, current.Width, current.Height);
			if (m_strokeDirtyValid)
			{
				Layer strokeLayer = m_layers[m_strokeLayerIndex];
				int bitmapLeft = m_strokeDirtyRect.Left - strokeLayer.OffsetX();
				int bitmapTop = m_strokeDirtyRect.Top - strokeLayer.OffsetY();
				int bitmapRight = m_strokeDirtyRect.Right - strokeLayer.OffsetX();
				int bitmapBottom = m_strokeDirtyRect.Bottom - strokeLayer.OffsetY();
				if (bitmapLeft < 0)
				{
					bitmapLeft = 0;
				}
				if (bitmapTop < 0)
				{
					bitmapTop = 0;
				}
				if (bitmapRight > current.Width)
				{
					bitmapRight = current.Width;
				}
				if (bitmapBottom > current.Height)
				{
					bitmapBottom = current.Height;
				}
				if (bitmapRight <= bitmapLeft || bitmapBottom <= bitmapTop)
				{
					m_strokeSnapshot.Dispose();
					m_strokeSnapshot = null;
					return;
				}
				searchRect = new SKRectI(bitmapLeft, bitmapTop, bitmapRight, bitmapBottom);
			}
			SKRectI rect = PixelRegion.ComputeDirtyRect(m_strokeSnapshot, current, searchRect);
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

		public void PushCommand(EditCommand command)
		{
			m_undoStack.Add(command);
			m_redoStack.Clear();
			if (m_undoStack.Count > MaxUndoDepth)
			{
				m_undoStack.RemoveAt(0);
			}
		}

		public bool Undo()
		{
			if (m_undoStack.Count == 0)
			{
				return false;
			}
			int last = m_undoStack.Count - 1;
			EditCommand command = m_undoStack[last];
			m_undoStack.RemoveAt(last);
			command.ApplyBefore(this);
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
			EditCommand command = m_redoStack[last];
			m_redoStack.RemoveAt(last);
			command.ApplyAfter(this);
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

		private void DrawLayers(SKCanvas canvas)
		{
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
				canvas.DrawImage(image, layer.OffsetX(), layer.OffsetY(), sampling, paint);
				image.Dispose();
			}
			paint.Dispose();
		}

		public void CompositeInto(SKBitmap target)
		{
			SKCanvas canvas = new SKCanvas(target);
			canvas.Clear(SKColors.Transparent);
			DrawLayers(canvas);
			canvas.Dispose();
		}

		public void CompositeRegion(SKBitmap target, SKRectI region)
		{
			SKCanvas canvas = new SKCanvas(target);
			SKRect clipRect = new SKRect(region.Left, region.Top, region.Right, region.Bottom);
			canvas.Save();
			canvas.ClipRect(clipRect);
			SKPaint clearPaint = new SKPaint();
			clearPaint.Color = SKColors.Transparent;
			clearPaint.BlendMode = SKBlendMode.Src;
			canvas.DrawRect(clipRect, clearPaint);
			clearPaint.Dispose();
			DrawLayers(canvas);
			canvas.Restore();
			canvas.Dispose();
		}
	}
}
