using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public class Document
	{
		private static int s_maxUndoDepth = 100;

		public static int MaxUndoDepth()
		{
			return s_maxUndoDepth;
		}

		public static void SetMaxUndoDepth(int depth)
		{
			if (depth < 10)
			{
				depth = 10;
			}
			if (depth > 500)
			{
				depth = 500;
			}
			s_maxUndoDepth = depth;
		}

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
		private string m_sourcePath;
		private Guides m_guides;
		private DocumentStateCommand m_pendingDocEdit;

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
			m_sourcePath = null;
			m_guides = new Guides();
			m_pendingDocEdit = null;
		}

		public string SourcePath()
		{
			return m_sourcePath;
		}

		public void SetSourcePath(string path)
		{
			m_sourcePath = path;
		}

		public bool FlatCompatible()
		{
			if (m_layers.Count != 1)
			{
				return false;
			}
			if (m_layers[0].IsText())
			{
				return false;
			}
			return true;
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

		public void ResetSelection()
		{
			m_selection = new Selection(m_width, m_height);
		}

		public unsafe void FillSelection(SKColor fill)
		{
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return;
			}
			if (!m_selection.IsActive())
			{
				return;
			}
			SKBitmap bitmap = layer.Bitmap();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			SKRectI bounds = m_selection.Bounds();
			int left = bounds.Left;
			int top = bounds.Top;
			int right = bounds.Right;
			int bottom = bounds.Bottom;
			if (left < offsetX)
			{
				left = offsetX;
			}
			if (top < offsetY)
			{
				top = offsetY;
			}
			if (right > offsetX + bitmap.Width)
			{
				right = offsetX + bitmap.Width;
			}
			if (bottom > offsetY + bitmap.Height)
			{
				bottom = offsetY + bitmap.Height;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}
			byte[] mask = m_selection.Mask();
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			byte fillRed = fill.Red;
			byte fillGreen = fill.Green;
			byte fillBlue = fill.Blue;
			byte fillAlpha = fill.Alpha;
			for (int canvasY = top; canvasY < bottom; canvasY++)
			{
				int maskRow = canvasY * m_width;
				byte* row = basePointer + ((canvasY - offsetY) * rowBytes);
				for (int canvasX = left; canvasX < right; canvasX++)
				{
					if (mask[maskRow + canvasX] == 0)
					{
						continue;
					}
					byte* pixel = row + ((canvasX - offsetX) * 4);
					pixel[0] = fillRed;
					pixel[1] = fillGreen;
					pixel[2] = fillBlue;
					pixel[3] = fillAlpha;
				}
			}
			MarkComposeDirtyRegion(new SKRectI(left, top, right, bottom));
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
			if (m_undoStack.Count > s_maxUndoDepth)
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
			if (m_undoStack.Count > s_maxUndoDepth)
			{
				m_undoStack.RemoveAt(0);
			}
			m_dirty = true;
		}

		public Guides Guides()
		{
			return m_guides;
		}

		public List<Layer> CloneLayers()
		{
			List<Layer> copy = new List<Layer>();
			for (int index = 0; index < m_layers.Count; index++)
			{
				copy.Add(m_layers[index].Clone());
			}
			return copy;
		}

		public void ReplaceLayers(List<Layer> layers, int width, int height, int activeIndex)
		{
			m_layers = new List<Layer>();
			for (int index = 0; index < layers.Count; index++)
			{
				m_layers.Add(layers[index].Clone());
			}
			m_width = width;
			m_height = height;
			if (activeIndex < 0)
			{
				activeIndex = 0;
			}
			if (activeIndex >= m_layers.Count)
			{
				activeIndex = m_layers.Count - 1;
			}
			m_activeLayerIndex = activeIndex;
			MarkComposeDirtyAll();
		}

		public void RestoreSelection(byte[] mask, SKRectI bounds, bool active)
		{
			m_selection = new Selection(m_width, m_height);
			if (active && mask != null && mask.Length == m_width * m_height)
			{
				m_selection.SelectMask(mask, bounds);
			}
		}

		public void BeginCanvasEdit(string label)
		{
			if (m_pendingDocEdit != null)
			{
				return;
			}
			m_pendingDocEdit = new DocumentStateCommand(label);
			m_pendingDocEdit.CaptureBefore(this);
		}

		public void EndCanvasEdit()
		{
			if (m_pendingDocEdit == null)
			{
				return;
			}
			m_pendingDocEdit.CaptureAfter(this);
			PushCommand(m_pendingDocEdit);
			m_pendingDocEdit = null;
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

		public List<string> HistoryLabels()
		{
			List<string> labels = new List<string>();
			for (int index = 0; index < m_undoStack.Count; index++)
			{
				labels.Add(m_undoStack[index].Label());
			}
			for (int index = m_redoStack.Count - 1; index >= 0; index--)
			{
				labels.Add(m_redoStack[index].Label());
			}
			return labels;
		}

		public int HistoryIndex()
		{
			return m_undoStack.Count;
		}

		public void JumpToHistory(int appliedCount)
		{
			int current = m_undoStack.Count;
			if (appliedCount < current)
			{
				int steps = current - appliedCount;
				for (int index = 0; index < steps; index++)
				{
					Undo();
				}
				return;
			}
			if (appliedCount > current)
			{
				int steps = appliedCount - current;
				for (int index = 0; index < steps; index++)
				{
					Redo();
				}
			}
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
				SKPixmap pixmap = layer.Bitmap().PeekPixels();
				SKImage image = SKImage.FromPixels(pixmap);
				canvas.DrawImage(image, layer.OffsetX(), layer.OffsetY(), sampling, paint);
				image.Dispose();
				pixmap.Dispose();
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

		private bool AnyVisibleCustomBlend()
		{
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				if (Layer.IsCustomBlend(layer.BlendMode()))
				{
					return true;
				}
			}
			return false;
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
			if (AnyVisibleCustomBlend())
			{
				CompositeRegionSoftware(target, region);
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

		private void CompositeRegionSoftware(SKBitmap target, SKRectI region)
		{
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
			if (right > target.Width)
			{
				right = target.Width;
			}
			if (bottom > target.Height)
			{
				bottom = target.Height;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}
			SKRect clipRect = new SKRect(left, top, right, bottom);

			SKCanvas clearCanvas = new SKCanvas(target);
			clearCanvas.Save();
			clearCanvas.ClipRect(clipRect);
			SKPaint clearPaint = new SKPaint();
			clearPaint.Color = SKColors.Transparent;
			clearPaint.BlendMode = SKBlendMode.Src;
			clearCanvas.DrawRect(clipRect, clearPaint);
			clearPaint.Dispose();
			clearCanvas.Restore();
			clearCanvas.Dispose();

			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				if (Layer.IsCustomBlend(layer.BlendMode()))
				{
					BlendCustomLayer(target, left, top, right, bottom, layer);
				}
				else
				{
					SKCanvas canvas = new SKCanvas(target);
					canvas.Save();
					canvas.ClipRect(clipRect);
					SKPaint paint = new SKPaint();
					paint.Color = SKColors.White.WithAlpha(layer.Opacity());
					paint.BlendMode = Layer.ToSkBlendMode(layer.BlendMode());
					SKPixmap pixmap = layer.Bitmap().PeekPixels();
					SKImage image = SKImage.FromPixels(pixmap);
					canvas.DrawImage(image, layer.OffsetX(), layer.OffsetY(), sampling, paint);
					image.Dispose();
					pixmap.Dispose();
					paint.Dispose();
					canvas.Restore();
					canvas.Dispose();
				}
			}
		}

		private unsafe void BlendCustomLayer(SKBitmap target, int left, int top, int right, int bottom, Layer layer)
		{
			SKBitmap source = layer.Bitmap();
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int opacity = layer.Opacity();
			eBlendMode mode = layer.BlendMode();
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			int sourceRowBytes = source.RowBytes;
			byte* targetBase = (byte*)target.GetPixels().ToPointer();
			int targetRowBytes = target.RowBytes;
			for (int canvasY = top; canvasY < bottom; canvasY++)
			{
				int bitmapY = canvasY - offsetY;
				if (bitmapY < 0 || bitmapY >= sourceHeight)
				{
					continue;
				}
				byte* sourceRow = sourceBase + (bitmapY * sourceRowBytes);
				byte* targetRow = targetBase + (canvasY * targetRowBytes);
				for (int canvasX = left; canvasX < right; canvasX++)
				{
					int bitmapX = canvasX - offsetX;
					if (bitmapX < 0 || bitmapX >= sourceWidth)
					{
						continue;
					}
					byte* sourcePixel = sourceRow + (bitmapX * 4);
					int sourceAlpha = sourcePixel[3];
					if (sourceAlpha == 0)
					{
						continue;
					}
					int effectiveAlpha = ((sourceAlpha * opacity) + 127) / 255;
					if (effectiveAlpha == 0)
					{
						continue;
					}
					byte* targetPixel = targetRow + (canvasX * 4);
					if (mode == eBlendMode.Dissolve)
					{
						int threshold = ((canvasX * 73) + (canvasY * 151)) & 255;
						if (effectiveAlpha > threshold)
						{
							targetPixel[0] = sourcePixel[0];
							targetPixel[1] = sourcePixel[1];
							targetPixel[2] = sourcePixel[2];
							targetPixel[3] = 255;
						}
						continue;
					}
					int baseAlpha = targetPixel[3];
					byte blendedRed;
					byte blendedGreen;
					byte blendedBlue;
					if (baseAlpha == 0)
					{
						blendedRed = sourcePixel[0];
						blendedGreen = sourcePixel[1];
						blendedBlue = sourcePixel[2];
					}
					else
					{
						int baseRed = ((targetPixel[0] * 255) + (baseAlpha / 2)) / baseAlpha;
						int baseGreen = ((targetPixel[1] * 255) + (baseAlpha / 2)) / baseAlpha;
						int baseBlue = ((targetPixel[2] * 255) + (baseAlpha / 2)) / baseAlpha;
						if (baseRed > 255)
						{
							baseRed = 255;
						}
						if (baseGreen > 255)
						{
							baseGreen = 255;
						}
						if (baseBlue > 255)
						{
							baseBlue = 255;
						}
						BlendModes.Blend(mode, (byte)baseRed, (byte)baseGreen, (byte)baseBlue, sourcePixel[0], sourcePixel[1], sourcePixel[2], out blendedRed, out blendedGreen, out blendedBlue);
					}
					int inverseAlpha = 255 - effectiveAlpha;
					int resultRed = (((blendedRed * effectiveAlpha) + 127) / 255) + (((targetPixel[0] * inverseAlpha) + 127) / 255);
					int resultGreen = (((blendedGreen * effectiveAlpha) + 127) / 255) + (((targetPixel[1] * inverseAlpha) + 127) / 255);
					int resultBlue = (((blendedBlue * effectiveAlpha) + 127) / 255) + (((targetPixel[2] * inverseAlpha) + 127) / 255);
					int resultAlpha = effectiveAlpha + (((baseAlpha * inverseAlpha) + 127) / 255);
					if (resultRed > 255)
					{
						resultRed = 255;
					}
					if (resultGreen > 255)
					{
						resultGreen = 255;
					}
					if (resultBlue > 255)
					{
						resultBlue = 255;
					}
					if (resultAlpha > 255)
					{
						resultAlpha = 255;
					}
					targetPixel[0] = (byte)resultRed;
					targetPixel[1] = (byte)resultGreen;
					targetPixel[2] = (byte)resultBlue;
					targetPixel[3] = (byte)resultAlpha;
				}
			}
		}

		private SKBitmap BakeLayerToCanvas(Layer layer)
		{
			SKBitmap baked = new SKBitmap(m_width, m_height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			baked.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(baked);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			paint.BlendMode = SKBlendMode.Src;
			SKImage image = SKImage.FromBitmap(layer.Bitmap());
			canvas.DrawImage(image, layer.OffsetX(), layer.OffsetY(), sampling, paint);
			image.Dispose();
			paint.Dispose();
			canvas.Dispose();
			return baked;
		}

		private static unsafe SKBitmap RotateFlipBitmap(SKBitmap source, int kind)
		{
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			int destinationWidth = sourceWidth;
			int destinationHeight = sourceHeight;
			if (kind == 3 || kind == 4)
			{
				destinationWidth = sourceHeight;
				destinationHeight = sourceWidth;
			}
			SKBitmap destination = new SKBitmap(destinationWidth, destinationHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			int sourceRowBytes = source.RowBytes;
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			int destinationRowBytes = destination.RowBytes;
			for (int y = 0; y < destinationHeight; y++)
			{
				byte* destinationRow = destinationBase + (y * destinationRowBytes);
				for (int x = 0; x < destinationWidth; x++)
				{
					int sourceX;
					int sourceY;
					if (kind == 0)
					{
						sourceX = sourceWidth - 1 - x;
						sourceY = y;
					}
					else if (kind == 1)
					{
						sourceX = x;
						sourceY = sourceHeight - 1 - y;
					}
					else if (kind == 2)
					{
						sourceX = sourceWidth - 1 - x;
						sourceY = sourceHeight - 1 - y;
					}
					else if (kind == 3)
					{
						sourceX = y;
						sourceY = sourceHeight - 1 - x;
					}
					else
					{
						sourceX = sourceWidth - 1 - y;
						sourceY = x;
					}
					byte* sourcePixel = sourceBase + (sourceY * sourceRowBytes) + (sourceX * 4);
					byte* destinationPixel = destinationRow + (x * 4);
					destinationPixel[0] = sourcePixel[0];
					destinationPixel[1] = sourcePixel[1];
					destinationPixel[2] = sourcePixel[2];
					destinationPixel[3] = sourcePixel[3];
				}
			}
			return destination;
		}

		private void ApplyRotateFlip(int kind, bool swapDimensions)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = RotateFlipBitmap(baked, kind);
				baked.Dispose();
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			if (swapDimensions)
			{
				int temp = m_width;
				m_width = m_height;
				m_height = temp;
			}
			m_dirty = true;
		}

		public void FlipHorizontal()
		{
			ApplyRotateFlip(0, false);
		}

		public void FlipVertical()
		{
			ApplyRotateFlip(1, false);
		}

		public void Rotate180()
		{
			ApplyRotateFlip(2, false);
		}

		public void Rotate90()
		{
			ApplyRotateFlip(3, true);
		}

		public void Rotate270()
		{
			ApplyRotateFlip(4, true);
		}

		public void CropToSelection()
		{
			if (!m_selection.IsActive())
			{
				return;
			}
			CropToRect(m_selection.Bounds());
		}

		public void CropToRect(SKRectI rect)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			int left = rect.Left;
			int top = rect.Top;
			int right = rect.Right;
			int bottom = rect.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > m_width)
			{
				right = m_width;
			}
			if (bottom > m_height)
			{
				bottom = m_height;
			}
			int cropWidth = right - left;
			int cropHeight = bottom - top;
			if (cropWidth <= 0 || cropHeight <= 0)
			{
				return;
			}
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = new SKBitmap(cropWidth, cropHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				destination.Erase(SKColors.Transparent);
				SKCanvas canvas = new SKCanvas(destination);
				SKPaint paint = new SKPaint();
				paint.BlendMode = SKBlendMode.Src;
				SKPixmap pixmap = baked.PeekPixels();
				SKImage image = SKImage.FromPixels(pixmap);
				canvas.DrawImage(image, -left, -top, sampling, paint);
				image.Dispose();
				pixmap.Dispose();
				paint.Dispose();
				canvas.Dispose();
				baked.Dispose();
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			m_width = cropWidth;
			m_height = cropHeight;
			m_selection.Clear();
			m_dirty = true;
		}

		public void RotateArbitrary(double angleDegrees, int interpolation)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			int newWidth = m_width;
			int newHeight = m_height;
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap rotated = RotateTransform.Rotate(baked, angleDegrees, interpolation);
				baked.Dispose();
				layer.SetBitmap(rotated);
				layer.SetOffset(0, 0);
				newWidth = rotated.Width;
				newHeight = rotated.Height;
			}
			m_width = newWidth;
			m_height = newHeight;
			m_dirty = true;
		}

		public void Trim()
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			int unionLeft = m_width;
			int unionTop = m_height;
			int unionRight = -1;
			int unionBottom = -1;
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKRectI contentBounds = PixelRegion.ComputeContentBounds(layer.Bitmap());
				if (contentBounds.Width <= 0)
				{
					continue;
				}
				int canvasLeft = contentBounds.Left + layer.OffsetX();
				int canvasTop = contentBounds.Top + layer.OffsetY();
				int canvasRight = contentBounds.Right + layer.OffsetX();
				int canvasBottom = contentBounds.Bottom + layer.OffsetY();
				if (canvasLeft < unionLeft)
				{
					unionLeft = canvasLeft;
				}
				if (canvasTop < unionTop)
				{
					unionTop = canvasTop;
				}
				if (canvasRight > unionRight)
				{
					unionRight = canvasRight;
				}
				if (canvasBottom > unionBottom)
				{
					unionBottom = canvasBottom;
				}
			}
			if (unionRight < 0)
			{
				return;
			}
			if (unionLeft < 0)
			{
				unionLeft = 0;
			}
			if (unionTop < 0)
			{
				unionTop = 0;
			}
			if (unionRight > m_width)
			{
				unionRight = m_width;
			}
			if (unionBottom > m_height)
			{
				unionBottom = m_height;
			}
			int trimWidth = unionRight - unionLeft;
			int trimHeight = unionBottom - unionTop;
			if (trimWidth <= 0 || trimHeight <= 0)
			{
				return;
			}
			if (unionLeft == 0 && unionTop == 0 && trimWidth == m_width && trimHeight == m_height)
			{
				return;
			}
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = new SKBitmap(trimWidth, trimHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				for (int y = 0; y < trimHeight; y++)
				{
					for (int x = 0; x < trimWidth; x++)
					{
						destination.SetPixel(x, y, baked.GetPixel(unionLeft + x, unionTop + y));
					}
				}
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			m_width = trimWidth;
			m_height = trimHeight;
			m_dirty = true;
		}

		public void ResizeCanvas(int newWidth, int newHeight, int anchorX, int anchorY)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			if (newWidth <= 0 || newHeight <= 0)
			{
				return;
			}
			int dx = 0;
			if (anchorX == 1)
			{
				dx = newWidth - m_width;
			}
			else if (anchorX == 0)
			{
				dx = (newWidth - m_width) / 2;
			}
			int dy = 0;
			if (anchorY == 1)
			{
				dy = newHeight - m_height;
			}
			else if (anchorY == 0)
			{
				dy = (newHeight - m_height) / 2;
			}
			int sourceWidth = m_width;
			int sourceHeight = m_height;
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = new SKBitmap(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				destination.Erase(SKColors.Transparent);
				for (int y = 0; y < sourceHeight; y++)
				{
					int destinationY = y + dy;
					if (destinationY < 0 || destinationY >= newHeight)
					{
						continue;
					}
					for (int x = 0; x < sourceWidth; x++)
					{
						int destinationX = x + dx;
						if (destinationX < 0 || destinationX >= newWidth)
						{
							continue;
						}
						destination.SetPixel(destinationX, destinationY, baked.GetPixel(x, y));
					}
				}
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			m_width = newWidth;
			m_height = newHeight;
			m_dirty = true;
		}

		public void ScaleImage(int newWidth, int newHeight, int interpolation)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			if (newWidth <= 0 || newHeight <= 0)
			{
				return;
			}
			SKFilterMode filterMode = SKFilterMode.Linear;
			if (interpolation == 0)
			{
				filterMode = SKFilterMode.Nearest;
			}
			SKSamplingOptions sampling = new SKSamplingOptions(filterMode, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = new SKBitmap(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				destination.Erase(SKColors.Transparent);
				SKCanvas canvas = new SKCanvas(destination);
				SKImage image = SKImage.FromBitmap(baked);
				SKRect destinationRect = new SKRect(0.0f, 0.0f, newWidth, newHeight);
				SKPaint paint = new SKPaint();
				paint.BlendMode = SKBlendMode.Src;
				canvas.DrawImage(image, destinationRect, sampling, paint);
				paint.Dispose();
				image.Dispose();
				canvas.Dispose();
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			m_width = newWidth;
			m_height = newHeight;
			m_dirty = true;
		}
	}
}
