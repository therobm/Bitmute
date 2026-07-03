using System;
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
		private bool m_dirty;

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
			background.SetIsBackground(true);
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
			m_dirty = false;
		}

		public bool IsDirty()
		{
			return m_dirty;
		}

		public void MarkClean()
		{
			m_dirty = false;
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

		public SKBitmap StrokeSnapshot()
		{
			return m_strokeSnapshot;
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
			m_dirty = true;
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
			m_dirty = true;
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
			m_dirty = true;
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
			m_dirty = true;
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

		public void MoveLayer(int fromIndex, int toIndex)
		{
			if (fromIndex < 0 || fromIndex >= m_layers.Count)
			{
				return;
			}
			int target = toIndex;
			if (target < 0)
			{
				target = 0;
			}
			if (target > m_layers.Count - 1)
			{
				target = m_layers.Count - 1;
			}
			if (target == fromIndex)
			{
				return;
			}
			Layer layer = m_layers[fromIndex];
			m_layers.RemoveAt(fromIndex);
			m_layers.Insert(target, layer);
			if (m_activeLayerIndex == fromIndex)
			{
				m_activeLayerIndex = target;
			}
			else if (fromIndex < m_activeLayerIndex && m_activeLayerIndex <= target)
			{
				m_activeLayerIndex = m_activeLayerIndex - 1;
			}
			else if (target <= m_activeLayerIndex && m_activeLayerIndex < fromIndex)
			{
				m_activeLayerIndex = m_activeLayerIndex + 1;
			}
			m_dirty = true;
		}

		public void MoveLayerUp(int index)
		{
			MoveLayer(index, index + 1);
		}

		public void MoveLayerDown(int index)
		{
			MoveLayer(index, index - 1);
		}

		public void DuplicateLayer(int index)
		{
			if (index < 0 || index >= m_layers.Count)
			{
				return;
			}
			Layer source = m_layers[index];
			SKBitmap sourceBitmap = source.Bitmap();
			Layer copy = new Layer(source.Name() + " copy", sourceBitmap.Width, sourceBitmap.Height);
			copy.SetBitmap(sourceBitmap.Copy());
			copy.SetOffset(source.OffsetX(), source.OffsetY());
			copy.SetOpacity(source.Opacity());
			copy.SetBlendMode(source.BlendMode());
			copy.SetVisible(source.IsVisible());
			m_layers.Insert(index + 1, copy);
			m_activeLayerIndex = index + 1;
			m_dirty = true;
		}

		public void MergeDown(int index)
		{
			if (index <= 0 || index >= m_layers.Count)
			{
				return;
			}
			Layer upper = m_layers[index];
			Layer lower = m_layers[index - 1];
			SKBitmap upperBitmap = upper.Bitmap();
			SKBitmap lowerBitmap = lower.Bitmap();
			int upperLeft = upper.OffsetX();
			int upperTop = upper.OffsetY();
			int lowerLeft = lower.OffsetX();
			int lowerTop = lower.OffsetY();
			int left = upperLeft;
			if (lowerLeft < left)
			{
				left = lowerLeft;
			}
			int top = upperTop;
			if (lowerTop < top)
			{
				top = lowerTop;
			}
			int right = upperLeft + upperBitmap.Width;
			int lowerRight = lowerLeft + lowerBitmap.Width;
			if (lowerRight > right)
			{
				right = lowerRight;
			}
			int bottom = upperTop + upperBitmap.Height;
			int lowerBottom = lowerTop + lowerBitmap.Height;
			if (lowerBottom > bottom)
			{
				bottom = lowerBottom;
			}
			int mergedWidth = right - left;
			int mergedHeight = bottom - top;
			SKBitmap merged = new SKBitmap(mergedWidth, mergedHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			merged.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(merged);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			paint.BlendMode = SKBlendMode.SrcOver;
			paint.Color = SKColors.White.WithAlpha(lower.Opacity());
			SKImage lowerImage = SKImage.FromBitmap(lowerBitmap);
			canvas.DrawImage(lowerImage, lowerLeft - left, lowerTop - top, sampling, paint);
			lowerImage.Dispose();
			paint.BlendMode = Layer.ToSkBlendMode(upper.BlendMode());
			paint.Color = SKColors.White.WithAlpha(upper.Opacity());
			SKImage upperImage = SKImage.FromBitmap(upperBitmap);
			canvas.DrawImage(upperImage, upperLeft - left, upperTop - top, sampling, paint);
			upperImage.Dispose();
			paint.Dispose();
			canvas.Dispose();
			lower.SetBitmap(merged);
			lower.SetOffset(left, top);
			lower.SetOpacity(255);
			lower.SetBlendMode(eBlendMode.Normal);
			m_layers.RemoveAt(index);
			if (m_activeLayerIndex >= m_layers.Count)
			{
				m_activeLayerIndex = m_layers.Count - 1;
			}
			else
			{
				m_activeLayerIndex = index - 1;
			}
			m_dirty = true;
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

		private bool AllVisibleLayersNormal()
		{
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				if (layer.BlendMode() != eBlendMode.Normal)
				{
					return false;
				}
			}
			return true;
		}

		public void CompositeInto(SKBitmap target)
		{
			CompositeRegion(target, new SKRectI(0, 0, m_width, m_height));
		}

		public void CompositeRegion(SKBitmap target, SKRectI region)
		{
			if (AllVisibleLayersNormal())
			{
				CompositeRegionRaw(target, region);
				return;
			}
			CompositeRegionSkia(target, region);
		}

		private unsafe void CompositeRegionRaw(SKBitmap target, SKRectI region)
		{
			int canvasWidth = target.Width;
			int canvasHeight = target.Height;
			int left = region.Left;
			int top = region.Top;
			int right = region.Right;
			int bottom = region.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > canvasWidth)
			{
				right = canvasWidth;
			}
			if (bottom > canvasHeight)
			{
				bottom = canvasHeight;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}

			int layerCount = m_layers.Count;
			IntPtr* sourceBases = stackalloc IntPtr[layerCount];
			int* sourceRowBytes = stackalloc int[layerCount];
			int* sourceWidths = stackalloc int[layerCount];
			int* sourceHeights = stackalloc int[layerCount];
			int* sourceOffsetsX = stackalloc int[layerCount];
			int* sourceOffsetsY = stackalloc int[layerCount];
			int* sourceOpacities = stackalloc int[layerCount];
			int visibleCount = 0;
			for (int index = 0; index < layerCount; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				SKBitmap bitmap = layer.Bitmap();
				sourceBases[visibleCount] = bitmap.GetPixels();
				sourceRowBytes[visibleCount] = bitmap.RowBytes;
				sourceWidths[visibleCount] = bitmap.Width;
				sourceHeights[visibleCount] = bitmap.Height;
				sourceOffsetsX[visibleCount] = layer.OffsetX();
				sourceOffsetsY[visibleCount] = layer.OffsetY();
				sourceOpacities[visibleCount] = layer.Opacity();
				visibleCount++;
			}

			int targetRowBytes = target.RowBytes;
			byte* targetBase = (byte*)target.GetPixels().ToPointer();

			for (int canvasY = top; canvasY < bottom; canvasY++)
			{
				byte* targetRow = targetBase + (canvasY * targetRowBytes);
				for (int canvasX = left; canvasX < right; canvasX++)
				{
					int accumulatedRed = 0;
					int accumulatedGreen = 0;
					int accumulatedBlue = 0;
					int accumulatedAlpha = 0;
					for (int layerIndex = 0; layerIndex < visibleCount; layerIndex++)
					{
						int bitmapX = canvasX - sourceOffsetsX[layerIndex];
						int bitmapY = canvasY - sourceOffsetsY[layerIndex];
						if (bitmapX < 0 || bitmapY < 0 || bitmapX >= sourceWidths[layerIndex] || bitmapY >= sourceHeights[layerIndex])
						{
							continue;
						}
						byte* sourcePixel = (byte*)sourceBases[layerIndex].ToPointer() + (bitmapY * sourceRowBytes[layerIndex]) + (bitmapX * 4);
						int sourceAlpha = sourcePixel[3];
						if (sourceAlpha == 0)
						{
							continue;
						}
						int effectiveAlpha = ((sourceAlpha * sourceOpacities[layerIndex]) + 127) / 255;
						if (effectiveAlpha == 0)
						{
							continue;
						}
						int premultipliedRed = ((sourcePixel[0] * effectiveAlpha) + 127) / 255;
						int premultipliedGreen = ((sourcePixel[1] * effectiveAlpha) + 127) / 255;
						int premultipliedBlue = ((sourcePixel[2] * effectiveAlpha) + 127) / 255;
						int inverseAlpha = 255 - effectiveAlpha;
						accumulatedRed = premultipliedRed + (((accumulatedRed * inverseAlpha) + 127) / 255);
						accumulatedGreen = premultipliedGreen + (((accumulatedGreen * inverseAlpha) + 127) / 255);
						accumulatedBlue = premultipliedBlue + (((accumulatedBlue * inverseAlpha) + 127) / 255);
						accumulatedAlpha = effectiveAlpha + (((accumulatedAlpha * inverseAlpha) + 127) / 255);
					}
					byte* targetPixel = targetRow + (canvasX * 4);
					targetPixel[0] = (byte)accumulatedRed;
					targetPixel[1] = (byte)accumulatedGreen;
					targetPixel[2] = (byte)accumulatedBlue;
					targetPixel[3] = (byte)accumulatedAlpha;
				}
			}
		}

		private void CompositeRegionSkia(SKBitmap target, SKRectI region)
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
