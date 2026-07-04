using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class MoveTool : Tool
	{
		private bool m_offsetMode;
		private bool m_textMode;
		private SKBitmap m_moving;
		private SKBitmap m_static;
		private byte[] m_selectionMask;
		private SKRectI m_selectionBounds;
		private int m_startX;
		private int m_startY;
		private int m_oldOffsetX;
		private int m_oldOffsetY;
		private int m_oldTextX;
		private int m_oldTextY;
		private SKRectI m_moveContentBounds;
		private bool m_moveContentValid;
		private int m_prevDeltaX;
		private int m_prevDeltaY;
		private List<int> m_snapLayerXs;
		private List<int> m_snapLayerYs;

		private static SKRectI OffsetRectI(SKRectI rect, int deltaX, int deltaY)
		{
			return new SKRectI(rect.Left + deltaX, rect.Top + deltaY, rect.Right + deltaX, rect.Bottom + deltaY);
		}

		private static SKRectI UnionRectI(SKRectI first, SKRectI second)
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

		private void MarkMoveDirty(Document document, int curDeltaX, int curDeltaY)
		{
			Layer activeStyledLayer = document.ActiveLayer();
			if (activeStyledLayer != null && activeStyledLayer.LayerStyle().HasAnyEffect())
			{
				document.MarkComposeDirtyAll();
				m_prevDeltaX = curDeltaX;
				m_prevDeltaY = curDeltaY;
				return;
			}
			if (!m_moveContentValid)
			{
				document.MarkComposeDirtyAll();
				return;
			}
			SKRectI previous = OffsetRectI(m_moveContentBounds, m_prevDeltaX, m_prevDeltaY);
			SKRectI current = OffsetRectI(m_moveContentBounds, curDeltaX, curDeltaY);
			SKRectI region = UnionRectI(previous, current);
			int left = region.Left - 1;
			int top = region.Top - 1;
			int right = region.Right + 1;
			int bottom = region.Bottom + 1;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > document.Width())
			{
				right = document.Width();
			}
			if (bottom > document.Height())
			{
				bottom = document.Height();
			}
			if (right <= left || bottom <= top)
			{
				m_prevDeltaX = curDeltaX;
				m_prevDeltaY = curDeltaY;
				return;
			}
			document.MarkComposeDirtyRegion(new SKRectI(left, top, right, bottom));
			m_prevDeltaX = curDeltaX;
			m_prevDeltaY = curDeltaY;
		}

		private void CacheMoveContentBounds(Layer layer, SKRectI canvasBounds)
		{
			m_moveContentBounds = canvasBounds;
			m_moveContentValid = canvasBounds.Width > 0 && canvasBounds.Height > 0;
			m_prevDeltaX = 0;
			m_prevDeltaY = 0;
		}

		private SKRectI CanvasContentBounds(Layer layer)
		{
			SKRectI local = PixelRegion.ComputeContentBounds(layer.Bitmap());
			if (local.Width <= 0 || local.Height <= 0)
			{
				return SKRectI.Empty;
			}
			return OffsetRectI(local, layer.OffsetX(), layer.OffsetY());
		}

		private static int NearestMultiple(int value, int gridSize)
		{
			int half = gridSize / 2;
			int shifted;
			if (value >= 0)
			{
				shifted = value + half;
			}
			else
			{
				shifted = value - half;
			}
			return (shifted / gridSize) * gridSize;
		}

		private static int BestAxisDelta(int edgeLow, int edgeMid, int edgeHigh, List<int> candidates, int tolerance, int gridSize)
		{
			int bestDelta = 0;
			int bestDistance = tolerance + 1;
			int[] edges = new int[] { edgeLow, edgeMid, edgeHigh };
			for (int index = 0; index < 3; index++)
			{
				for (int candidate = 0; candidate < candidates.Count; candidate++)
				{
					int delta = candidates[candidate] - edges[index];
					int magnitude = delta;
					if (magnitude < 0)
					{
						magnitude = -magnitude;
					}
					if (magnitude > tolerance)
					{
						continue;
					}
					if (magnitude < bestDistance)
					{
						bestDistance = magnitude;
						bestDelta = delta;
					}
				}
				if (gridSize > 0)
				{
					int nearest = NearestMultiple(edges[index], gridSize);
					int delta = nearest - edges[index];
					int magnitude = delta;
					if (magnitude < 0)
					{
						magnitude = -magnitude;
					}
					if (magnitude <= tolerance && magnitude < bestDistance)
					{
						bestDistance = magnitude;
						bestDelta = delta;
					}
				}
			}
			return bestDelta;
		}

		private void AddGuideCandidates(Guides guides, List<int> xs, List<int> ys)
		{
			List<int> verticals = guides.VerticalGuides();
			for (int index = 0; index < verticals.Count; index++)
			{
				xs.Add(verticals[index]);
			}
			List<int> horizontals = guides.HorizontalGuides();
			for (int index = 0; index < horizontals.Count; index++)
			{
				ys.Add(horizontals[index]);
			}
		}

		private void CacheLayerBoundsCandidates(Document document)
		{
			m_snapLayerXs = new List<int>();
			m_snapLayerYs = new List<int>();
			int activeIndex = document.ActiveLayerIndex();
			List<Layer> layers = document.Layers();
			for (int index = 0; index < layers.Count; index++)
			{
				if (index == activeIndex)
				{
					continue;
				}
				Layer other = layers[index];
				if (!other.IsVisible())
				{
					continue;
				}
				SKRectI box = CanvasContentBounds(other);
				if (box.Width <= 0 || box.Height <= 0)
				{
					continue;
				}
				m_snapLayerXs.Add(box.Left);
				m_snapLayerXs.Add(box.Right);
				m_snapLayerYs.Add(box.Top);
				m_snapLayerYs.Add(box.Bottom);
			}
		}

		private void SnapDelta(Document document, ToolState state, ref int deltaX, ref int deltaY)
		{
			bool anyTarget = state.SnapToGuides() || state.SnapGrid() || state.SnapEdges() || state.SnapLayerBounds();
			if (!anyTarget)
			{
				return;
			}
			if (!m_moveContentValid)
			{
				return;
			}
			int tolerance = state.SnapTolerance();
			List<int> xs = new List<int>();
			List<int> ys = new List<int>();
			if (state.SnapToGuides())
			{
				AddGuideCandidates(document.Guides(), xs, ys);
			}
			if (state.SnapEdges())
			{
				xs.Add(0);
				xs.Add(document.Width());
				ys.Add(0);
				ys.Add(document.Height());
			}
			if (state.SnapLayerBounds() && m_snapLayerXs != null)
			{
				for (int index = 0; index < m_snapLayerXs.Count; index++)
				{
					xs.Add(m_snapLayerXs[index]);
				}
				for (int index = 0; index < m_snapLayerYs.Count; index++)
				{
					ys.Add(m_snapLayerYs[index]);
				}
			}
			int left = m_moveContentBounds.Left + deltaX;
			int right = m_moveContentBounds.Right + deltaX;
			int centerX = ((m_moveContentBounds.Left + m_moveContentBounds.Right) / 2) + deltaX;
			int top = m_moveContentBounds.Top + deltaY;
			int bottom = m_moveContentBounds.Bottom + deltaY;
			int centerY = ((m_moveContentBounds.Top + m_moveContentBounds.Bottom) / 2) + deltaY;
			int gridSize = 0;
			if (state.SnapGrid())
			{
				gridSize = state.SnapGridSize();
			}
			deltaX = deltaX + BestAxisDelta(left, centerX, right, xs, tolerance, gridSize);
			deltaY = deltaY + BestAxisDelta(top, centerY, bottom, ys, tolerance, gridSize);
		}

		private SKBitmap ExtractSelected(Layer layer, Selection selection)
		{
			SKBitmap source = layer.Bitmap();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int width = source.Width;
			int height = source.Height;
			SKBitmap moving = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			moving.Erase(SKColors.Transparent);
			SKRectI bounds = selection.Bounds();
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					if (!selection.IsSelected(x, y))
					{
						continue;
					}
					int bitmapX = x - offsetX;
					int bitmapY = y - offsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= width || bitmapY >= height)
					{
						continue;
					}
					moving.SetPixel(bitmapX, bitmapY, source.GetPixel(bitmapX, bitmapY));
				}
			}
			return moving;
		}

		private SKBitmap CloneWithSelectionCleared(Layer layer, Selection selection)
		{
			SKBitmap remainder = layer.Bitmap().Copy();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int width = remainder.Width;
			int height = remainder.Height;
			SKRectI bounds = selection.Bounds();
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					if (!selection.IsSelected(x, y))
					{
						continue;
					}
					int bitmapX = x - offsetX;
					int bitmapY = y - offsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= width || bitmapY >= height)
					{
						continue;
					}
					remainder.SetPixel(bitmapX, bitmapY, SKColors.Transparent);
				}
			}
			return remainder;
		}

		private void Rebuild(Layer layer, int deltaX, int deltaY)
		{
			SKBitmap bitmap = layer.Bitmap();
			bitmap.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(bitmap);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			if (m_static != null)
			{
				SKImage staticImage = SKImage.FromBitmap(m_static);
				canvas.DrawImage(staticImage, 0.0f, 0.0f, sampling, paint);
				staticImage.Dispose();
			}
			SKImage movingImage = SKImage.FromBitmap(m_moving);
			canvas.DrawImage(movingImage, deltaX, deltaY, sampling, paint);
			movingImage.Dispose();
			paint.Dispose();
			canvas.Dispose();
		}

		private void ReleaseBuffers()
		{
			if (m_moving != null)
			{
				m_moving.Dispose();
				m_moving = null;
			}
			if (m_static != null)
			{
				m_static.Dispose();
				m_static = null;
			}
			m_selectionMask = null;
			m_moveContentValid = false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			ReleaseBuffers();
			m_startX = x;
			m_startY = y;
			Selection selection = document.Selection();
			if (selection.IsActive())
			{
				m_offsetMode = false;
				m_textMode = false;
				m_moving = ExtractSelected(layer, selection);
				m_static = CloneWithSelectionCleared(layer, selection);
				m_selectionMask = selection.MaskCopy();
				m_selectionBounds = selection.Bounds();
				CacheMoveContentBounds(layer, m_selectionBounds);
			}
			else if (layer.IsText())
			{
				m_textMode = true;
				m_offsetMode = false;
				m_oldTextX = layer.TextX();
				m_oldTextY = layer.TextY();
				CacheMoveContentBounds(layer, CanvasContentBounds(layer));
			}
			else
			{
				m_textMode = false;
				m_offsetMode = true;
				m_oldOffsetX = layer.OffsetX();
				m_oldOffsetY = layer.OffsetY();
				CacheMoveContentBounds(layer, CanvasContentBounds(layer));
			}
			m_snapLayerXs = null;
			m_snapLayerYs = null;
			if (state.SnapLayerBounds())
			{
				CacheLayerBoundsCandidates(document);
			}
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			int deltaX = x - m_startX;
			int deltaY = y - m_startY;
			SnapDelta(document, state, ref deltaX, ref deltaY);
			if (m_textMode)
			{
				layer.SetTextPosition(m_oldTextX + deltaX, m_oldTextY + deltaY);
				layer.RenderText();
				MarkMoveDirty(document, deltaX, deltaY);
				return true;
			}
			if (m_offsetMode)
			{
				layer.SetOffset(m_oldOffsetX + deltaX, m_oldOffsetY + deltaY);
				MarkMoveDirty(document, deltaX, deltaY);
				return true;
			}
			if (m_moving == null)
			{
				return false;
			}
			Rebuild(layer, deltaX, deltaY);
			if (m_selectionMask != null)
			{
				document.Selection().SetShifted(m_selectionMask, m_selectionBounds, deltaX, deltaY);
			}
			MarkMoveDirty(document, deltaX, deltaY);
			return true;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (m_textMode)
			{
				m_textMode = false;
				ReleaseBuffers();
				m_hasLast = false;
				return;
			}
			if (m_offsetMode)
			{
				Layer layer = document.ActiveLayer();
				if (layer != null)
				{
					if (layer.OffsetX() != m_oldOffsetX || layer.OffsetY() != m_oldOffsetY)
					{
						SKBitmap oldBitmap = layer.Bitmap();
						layer.ExpandToCover(document.Width(), document.Height());
						SKBitmap newBitmap = layer.Bitmap();
						document.PushCommand(new MoveLayerCommand(document.ActiveLayerIndex(), oldBitmap, m_oldOffsetX, m_oldOffsetY, newBitmap, layer.OffsetX(), layer.OffsetY()));
					}
				}
			}
			ReleaseBuffers();
			m_hasLast = false;
		}
	}
}
