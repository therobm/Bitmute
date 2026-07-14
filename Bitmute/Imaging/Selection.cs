using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public enum eSelectionMode
	{
		Replace,
		Add,
		Subtract,
		Intersect
	}

	public class Selection
	{
		private const int GrowthPadding = 128;

		private int m_width;
		private int m_height;
		private int m_originX;
		private int m_originY;
		private int m_maskWidth;
		private int m_maskHeight;
		private byte[] m_mask;
		private byte[] m_baseMask;
		private SKRectI m_baseBounds;
		private SKRectI m_lastCombinedBounds;
		private eSelectionMode m_operationMode;
		private int m_featherRadius;
		private byte[] m_regionScratch;
		private byte[] m_blurScratch;
		private bool m_active;
		private SKRectI m_bounds;
		private int m_generation;
		private bool m_lastOpShift;
		private bool m_lastShiftUnclipped;
		private bool m_shiftTranslatable;
		private int m_prevShiftDeltaX;
		private int m_prevShiftDeltaY;
		private int m_shiftStepX;
		private int m_shiftStepY;

		private sealed class HorizontalBlurWorker
		{
			public byte[] m_source;
			public byte[] m_destination;
			public int m_width;
			public int m_left;
			public int m_top;
			public int m_right;
			public int m_bottom;
			public int m_radius;

			public void Band(int start, int end)
			{
				int window = (m_radius * 2) + 1;
				int half = window / 2;
				for (int y = start; y < end; y++)
				{
					int rowStart = y * m_width;
					int sum = 0;
					int primeEnd = m_left + m_radius;
					if (primeEnd > m_right - 1)
					{
						primeEnd = m_right - 1;
					}
					for (int x = m_left; x <= primeEnd; x++)
					{
						sum = sum + m_source[rowStart + x];
					}
					for (int x = m_left; x < m_right; x++)
					{
						m_destination[rowStart + x] = (byte)((sum + half) / window);
						int addIndex = x + m_radius + 1;
						if (addIndex < m_right)
						{
							sum = sum + m_source[rowStart + addIndex];
						}
						int removeIndex = x - m_radius;
						if (removeIndex >= m_left)
						{
							sum = sum - m_source[rowStart + removeIndex];
						}
					}
				}
			}
		}

		private sealed class VerticalBlurWorker
		{
			public byte[] m_source;
			public byte[] m_destination;
			public int m_width;
			public int m_left;
			public int m_top;
			public int m_right;
			public int m_bottom;
			public int m_radius;

			public void Band(int start, int end)
			{
				int window = (m_radius * 2) + 1;
				int half = window / 2;
				for (int x = start; x < end; x++)
				{
					int sum = 0;
					int primeEnd = m_top + m_radius;
					if (primeEnd > m_bottom - 1)
					{
						primeEnd = m_bottom - 1;
					}
					for (int y = m_top; y <= primeEnd; y++)
					{
						sum = sum + m_source[(y * m_width) + x];
					}
					for (int y = m_top; y < m_bottom; y++)
					{
						m_destination[(y * m_width) + x] = (byte)((sum + half) / window);
						int addIndex = y + m_radius + 1;
						if (addIndex < m_bottom)
						{
							sum = sum + m_source[(addIndex * m_width) + x];
						}
						int removeIndex = y - m_radius;
						if (removeIndex >= m_top)
						{
							sum = sum - m_source[(removeIndex * m_width) + x];
						}
					}
				}
			}
		}

		public Selection(int width, int height)
		{
			m_width = width;
			m_height = height;
			m_originX = 0;
			m_originY = 0;
			m_maskWidth = width;
			m_maskHeight = height;
			m_mask = new byte[width * height];
			m_baseMask = new byte[width * height];
			m_baseBounds = SKRectI.Empty;
			m_lastCombinedBounds = SKRectI.Empty;
			m_operationMode = eSelectionMode.Replace;
			m_featherRadius = 0;
			m_regionScratch = null;
			m_blurScratch = null;
			m_active = false;
			m_bounds = SKRectI.Empty;
			m_generation = 0;
		}

		private static SKRectI UnionBounds(SKRectI first, SKRectI second)
		{
			bool firstEmpty = first.Width <= 0 || first.Height <= 0;
			bool secondEmpty = second.Width <= 0 || second.Height <= 0;
			if (firstEmpty)
			{
				return second;
			}
			if (secondEmpty)
			{
				return first;
			}
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

		private static SKRectI IntersectBounds(SKRectI first, SKRectI second)
		{
			int left = first.Left;
			if (second.Left > left)
			{
				left = second.Left;
			}
			int top = first.Top;
			if (second.Top > top)
			{
				top = second.Top;
			}
			int right = first.Right;
			if (second.Right < right)
			{
				right = second.Right;
			}
			int bottom = first.Bottom;
			if (second.Bottom < bottom)
			{
				bottom = second.Bottom;
			}
			if (right <= left || bottom <= top)
			{
				return SKRectI.Empty;
			}
			return new SKRectI(left, top, right, bottom);
		}

		private SKRectI MaskRect()
		{
			return new SKRectI(m_originX, m_originY, m_originX + m_maskWidth, m_originY + m_maskHeight);
		}

		private int MaskRow(int y)
		{
			return (y - m_originY) * m_maskWidth;
		}

		private SKRectI ClampToMask(SKRectI rect)
		{
			int left = rect.Left;
			int top = rect.Top;
			int right = rect.Right;
			int bottom = rect.Bottom;
			if (left < m_originX)
			{
				left = m_originX;
			}
			if (top < m_originY)
			{
				top = m_originY;
			}
			if (right > m_originX + m_maskWidth)
			{
				right = m_originX + m_maskWidth;
			}
			if (bottom > m_originY + m_maskHeight)
			{
				bottom = m_originY + m_maskHeight;
			}
			if (right <= left || bottom <= top)
			{
				return SKRectI.Empty;
			}
			return new SKRectI(left, top, right, bottom);
		}

		private void GrowToInclude(SKRectI rect)
		{
			if (rect.Width <= 0 || rect.Height <= 0)
			{
				return;
			}
			SKRectI current = MaskRect();
			if (rect.Left >= current.Left && rect.Top >= current.Top && rect.Right <= current.Right && rect.Bottom <= current.Bottom)
			{
				return;
			}
			int newLeft = current.Left;
			if (rect.Left < newLeft)
			{
				newLeft = rect.Left - GrowthPadding;
			}
			int newTop = current.Top;
			if (rect.Top < newTop)
			{
				newTop = rect.Top - GrowthPadding;
			}
			int newRight = current.Right;
			if (rect.Right > newRight)
			{
				newRight = rect.Right + GrowthPadding;
			}
			int newBottom = current.Bottom;
			if (rect.Bottom > newBottom)
			{
				newBottom = rect.Bottom + GrowthPadding;
			}
			int newWidth = newRight - newLeft;
			int newHeight = newBottom - newTop;
			byte[] newMask = new byte[newWidth * newHeight];
			byte[] newBase = new byte[newWidth * newHeight];
			int copyOffsetX = current.Left - newLeft;
			int copyOffsetY = current.Top - newTop;
			for (int y = 0; y < m_maskHeight; y++)
			{
				int sourceOffset = y * m_maskWidth;
				int destOffset = ((y + copyOffsetY) * newWidth) + copyOffsetX;
				Buffer.BlockCopy(m_mask, sourceOffset, newMask, destOffset, m_maskWidth);
				Buffer.BlockCopy(m_baseMask, sourceOffset, newBase, destOffset, m_maskWidth);
			}
			m_mask = newMask;
			m_baseMask = newBase;
			m_originX = newLeft;
			m_originY = newTop;
			m_maskWidth = newWidth;
			m_maskHeight = newHeight;
			m_regionScratch = null;
			m_blurScratch = null;
		}

		private void ResetGeometry()
		{
			if (m_originX == 0 && m_originY == 0 && m_maskWidth == m_width && m_maskHeight == m_height)
			{
				Array.Clear(m_mask, 0, m_mask.Length);
				return;
			}
			m_originX = 0;
			m_originY = 0;
			m_maskWidth = m_width;
			m_maskHeight = m_height;
			m_mask = new byte[m_width * m_height];
			m_baseMask = new byte[m_width * m_height];
			m_regionScratch = null;
			m_blurScratch = null;
		}

		private SKRectI CombinedResultBounds(SKRectI regionBounds)
		{
			if (m_operationMode == eSelectionMode.Subtract)
			{
				return m_baseBounds;
			}
			if (m_operationMode == eSelectionMode.Intersect)
			{
				return IntersectBounds(m_baseBounds, regionBounds);
			}
			return UnionBounds(m_baseBounds, regionBounds);
		}

		private void RecomputeFromMask()
		{
			RecomputeFromMask(MaskRect());
		}

		private void RecomputeFromMask(SKRectI searchBounds)
		{
			SKRectI search = ClampToMask(searchBounds);
			int minX = int.MaxValue;
			int minY = int.MaxValue;
			int maxX = int.MinValue;
			int maxY = int.MinValue;
			for (int y = search.Top; y < search.Bottom; y++)
			{
				int rowStart = MaskRow(y) - m_originX;
				for (int x = search.Left; x < search.Right; x++)
				{
					if (m_mask[rowStart + x] == 0)
					{
						continue;
					}
					if (x < minX)
					{
						minX = x;
					}
					if (x > maxX)
					{
						maxX = x;
					}
					if (y < minY)
					{
						minY = y;
					}
					if (y > maxY)
					{
						maxY = y;
					}
				}
			}
			if (maxX == int.MinValue)
			{
				m_active = false;
				m_bounds = SKRectI.Empty;
			}
			else
			{
				m_active = true;
				m_bounds = new SKRectI(minX, minY, maxX + 1, maxY + 1);
			}
			m_lastOpShift = false;
			m_generation = m_generation + 1;
		}

		private void CombineRegion(byte[] region, SKRectI regionRect, SKRectI regionBounds)
		{
			SKRectI bounds = ClampToMask(regionBounds);
			SKRectI restore = ClampToMask(UnionBounds(m_lastCombinedBounds, bounds));
			m_lastCombinedBounds = bounds;
			if (m_operationMode == eSelectionMode.Intersect)
			{
				for (int y = restore.Top; y < restore.Bottom; y++)
				{
					Array.Clear(m_mask, MaskRow(y) + (restore.Left - m_originX), restore.Width);
				}
				for (int y = bounds.Top; y < bounds.Bottom; y++)
				{
					int maskRow = MaskRow(y) - m_originX;
					int regionRow = ((y - regionRect.Top) * regionRect.Width) - regionRect.Left;
					for (int x = bounds.Left; x < bounds.Right; x++)
					{
						int index = maskRow + x;
						int baseValue = m_baseMask[index];
						int regionValue = region[regionRow + x];
						int result = baseValue;
						if (regionValue < result)
						{
							result = regionValue;
						}
						m_mask[index] = (byte)result;
					}
				}
				return;
			}
			for (int y = restore.Top; y < restore.Bottom; y++)
			{
				int rowOffset = MaskRow(y) + (restore.Left - m_originX);
				Buffer.BlockCopy(m_baseMask, rowOffset, m_mask, rowOffset, restore.Width);
			}
			if (m_operationMode == eSelectionMode.Subtract)
			{
				for (int y = bounds.Top; y < bounds.Bottom; y++)
				{
					int maskRow = MaskRow(y) - m_originX;
					int regionRow = ((y - regionRect.Top) * regionRect.Width) - regionRect.Left;
					for (int x = bounds.Left; x < bounds.Right; x++)
					{
						int index = maskRow + x;
						int result = m_baseMask[index] - region[regionRow + x];
						if (result < 0)
						{
							result = 0;
						}
						m_mask[index] = (byte)result;
					}
				}
				return;
			}
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				int maskRow = MaskRow(y) - m_originX;
				int regionRow = ((y - regionRect.Top) * regionRect.Width) - regionRect.Left;
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					int index = maskRow + x;
					int baseValue = m_baseMask[index];
					int regionValue = region[regionRow + x];
					int result = baseValue;
					if (regionValue > result)
					{
						result = regionValue;
					}
					m_mask[index] = (byte)result;
				}
			}
		}

		private void BoxBlurHorizontal(byte[] source, byte[] destination, int stride, int left, int top, int right, int bottom, int radius)
		{
			HorizontalBlurWorker worker = new HorizontalBlurWorker();
			worker.m_source = source;
			worker.m_destination = destination;
			worker.m_width = stride;
			worker.m_left = left;
			worker.m_top = top;
			worker.m_right = right;
			worker.m_bottom = bottom;
			worker.m_radius = radius;
			RowBands.Run(top, bottom, worker.Band);
		}

		private void BoxBlurVertical(byte[] source, byte[] destination, int stride, int left, int top, int right, int bottom, int radius)
		{
			VerticalBlurWorker worker = new VerticalBlurWorker();
			worker.m_source = source;
			worker.m_destination = destination;
			worker.m_width = stride;
			worker.m_left = left;
			worker.m_top = top;
			worker.m_right = right;
			worker.m_bottom = bottom;
			worker.m_radius = radius;
			RowBands.Run(left, right, worker.Band);
		}

		private void FeatherRegion(byte[] mask, int stride, int rows, SKRectI localBounds)
		{
			int radius = m_featherRadius;
			if (radius <= 0)
			{
				return;
			}
			int inflate = (radius * 3) + 1;
			int left = localBounds.Left - inflate;
			int top = localBounds.Top - inflate;
			int right = localBounds.Right + inflate;
			int bottom = localBounds.Bottom + inflate;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > stride)
			{
				right = stride;
			}
			if (bottom > rows)
			{
				bottom = rows;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}
			int needed = stride * rows;
			if (m_blurScratch == null || m_blurScratch.Length < needed)
			{
				m_blurScratch = new byte[needed];
			}
			for (int pass = 0; pass < 3; pass++)
			{
				BoxBlurHorizontal(mask, m_blurScratch, stride, left, top, right, bottom, radius);
				BoxBlurVertical(m_blurScratch, mask, stride, left, top, right, bottom, radius);
			}
		}

		public void BeginOperation(eSelectionMode mode)
		{
			BeginOperation(mode, 0);
		}

		public void BeginOperation(eSelectionMode mode, int featherRadius)
		{
			m_operationMode = mode;
			if (featherRadius < 0)
			{
				featherRadius = 0;
			}
			m_featherRadius = featherRadius;
			if (mode == eSelectionMode.Replace)
			{
				Array.Clear(m_baseMask, 0, m_baseMask.Length);
				m_baseBounds = SKRectI.Empty;
			}
			else
			{
				Buffer.BlockCopy(m_mask, 0, m_baseMask, 0, m_mask.Length);
				m_baseBounds = m_bounds;
			}
			m_lastCombinedBounds = m_bounds;
		}

		public void ApplyRect(SKRectI rect)
		{
			bool hasArea = rect.Right > rect.Left && rect.Bottom > rect.Top;
			if (m_featherRadius > 0 && hasArea)
			{
				int inflate = (m_featherRadius * 3) + 1;
				SKRectI inflated = new SKRectI(rect.Left - inflate, rect.Top - inflate, rect.Right + inflate, rect.Bottom + inflate);
				GrowToInclude(inflated);
				inflated = ClampToMask(inflated);
				if (m_regionScratch == null || m_regionScratch.Length < m_mask.Length)
				{
					m_regionScratch = new byte[m_mask.Length];
				}
				for (int y = inflated.Top; y < inflated.Bottom; y++)
				{
					Array.Clear(m_regionScratch, MaskRow(y) + (inflated.Left - m_originX), inflated.Width);
				}
				for (int y = rect.Top; y < rect.Bottom; y++)
				{
					int rowStart = MaskRow(y) - m_originX;
					for (int x = rect.Left; x < rect.Right; x++)
					{
						m_regionScratch[rowStart + x] = 255;
					}
				}
				SKRectI localBounds = new SKRectI(rect.Left - m_originX, rect.Top - m_originY, rect.Right - m_originX, rect.Bottom - m_originY);
				FeatherRegion(m_regionScratch, m_maskWidth, m_maskHeight, localBounds);
				CombineRegion(m_regionScratch, MaskRect(), inflated);
				RecomputeFromMask(CombinedResultBounds(inflated));
				return;
			}
			SKRectI rectBounds = SKRectI.Empty;
			if (hasArea)
			{
				GrowToInclude(rect);
				rectBounds = rect;
			}
			SKRectI restore = ClampToMask(UnionBounds(m_lastCombinedBounds, rectBounds));
			m_lastCombinedBounds = rectBounds;
			if (m_operationMode == eSelectionMode.Intersect)
			{
				for (int y = restore.Top; y < restore.Bottom; y++)
				{
					Array.Clear(m_mask, MaskRow(y) + (restore.Left - m_originX), restore.Width);
				}
				for (int y = rectBounds.Top; y < rectBounds.Bottom; y++)
				{
					int rowStart = MaskRow(y) - m_originX;
					for (int x = rectBounds.Left; x < rectBounds.Right; x++)
					{
						m_mask[rowStart + x] = m_baseMask[rowStart + x];
					}
				}
				RecomputeFromMask(CombinedResultBounds(rectBounds));
				return;
			}
			byte value = 255;
			if (m_operationMode == eSelectionMode.Subtract)
			{
				value = 0;
			}
			for (int y = restore.Top; y < restore.Bottom; y++)
			{
				int rowOffset = MaskRow(y) + (restore.Left - m_originX);
				Buffer.BlockCopy(m_baseMask, rowOffset, m_mask, rowOffset, restore.Width);
			}
			for (int y = rectBounds.Top; y < rectBounds.Bottom; y++)
			{
				int rowStart = MaskRow(y) - m_originX;
				for (int x = rectBounds.Left; x < rectBounds.Right; x++)
				{
					m_mask[rowStart + x] = value;
				}
			}
			if (m_operationMode == eSelectionMode.Subtract)
			{
				RecomputeFromMask(CombinedResultBounds(rectBounds));
				return;
			}
			SKRectI resultBounds = CombinedResultBounds(rectBounds);
			bool resultEmpty = resultBounds.Width <= 0 || resultBounds.Height <= 0;
			if (resultEmpty)
			{
				m_active = false;
				m_bounds = SKRectI.Empty;
			}
			else
			{
				m_active = true;
				m_bounds = resultBounds;
			}
			m_lastOpShift = false;
			m_generation = m_generation + 1;
		}

		public void ApplyMask(byte[] regionMask, SKRectI regionBounds)
		{
			if (regionBounds.Width <= 0 || regionBounds.Height <= 0)
			{
				ApplyRect(SKRectI.Empty);
				return;
			}
			if (m_featherRadius > 0)
			{
				int inflate = (m_featherRadius * 3) + 1;
				SKRectI inflated = new SKRectI(regionBounds.Left - inflate, regionBounds.Top - inflate, regionBounds.Right + inflate, regionBounds.Bottom + inflate);
				GrowToInclude(inflated);
				int needed = inflated.Width * inflated.Height;
				if (m_regionScratch == null || m_regionScratch.Length < needed)
				{
					m_regionScratch = new byte[needed];
				}
				Array.Clear(m_regionScratch, 0, needed);
				for (int y = 0; y < regionBounds.Height; y++)
				{
					Buffer.BlockCopy(regionMask, y * regionBounds.Width, m_regionScratch, (((y + inflate) * inflated.Width) + inflate), regionBounds.Width);
				}
				SKRectI localBounds = new SKRectI(inflate, inflate, inflate + regionBounds.Width, inflate + regionBounds.Height);
				FeatherRegion(m_regionScratch, inflated.Width, inflated.Height, localBounds);
				CombineRegion(m_regionScratch, inflated, inflated);
				RecomputeFromMask(CombinedResultBounds(inflated));
				return;
			}
			GrowToInclude(regionBounds);
			CombineRegion(regionMask, regionBounds, regionBounds);
			RecomputeFromMask(CombinedResultBounds(regionBounds));
		}

		public int Generation()
		{
			return m_generation;
		}

		public int Width()
		{
			return m_width;
		}

		public int Height()
		{
			return m_height;
		}

		public int MaskOriginX()
		{
			return m_originX;
		}

		public int MaskOriginY()
		{
			return m_originY;
		}

		public int MaskWidth()
		{
			return m_maskWidth;
		}

		public int MaskHeight()
		{
			return m_maskHeight;
		}

		public bool IsActive()
		{
			return m_active;
		}

		public SKRectI Bounds()
		{
			return m_bounds;
		}

		public bool IsSelected(int x, int y)
		{
			if (x < m_originX || y < m_originY || x >= m_originX + m_maskWidth || y >= m_originY + m_maskHeight)
			{
				return false;
			}
			return m_mask[MaskRow(y) + (x - m_originX)] >= 128;
		}

		public int Coverage(int x, int y)
		{
			if (x < m_originX || y < m_originY || x >= m_originX + m_maskWidth || y >= m_originY + m_maskHeight)
			{
				return 0;
			}
			return m_mask[MaskRow(y) + (x - m_originX)];
		}

		public void Clear()
		{
			ResetGeometry();
			m_lastCombinedBounds = MaskRect();
			m_lastOpShift = false;
			m_active = false;
			m_bounds = SKRectI.Empty;
			m_generation = m_generation + 1;
		}

		public void Invert()
		{
			// Invert across the whole document, not just the current mask region, so a small
			// selection inverts to "everything except it" rather than only within its own bounds.
			GrowToInclude(new SKRectI(0, 0, m_width, m_height));
			for (int y = m_originY; y < m_originY + m_maskHeight; y++)
			{
				int rowStart = MaskRow(y) - m_originX;
				bool insideCanvasRow = y >= 0 && y < m_height;
				for (int x = m_originX; x < m_originX + m_maskWidth; x++)
				{
					int index = rowStart + x;
					if (insideCanvasRow && x >= 0 && x < m_width)
					{
						m_mask[index] = (byte)(255 - m_mask[index]);
					}
					else
					{
						m_mask[index] = 0;
					}
				}
			}
			m_lastCombinedBounds = MaskRect();
			RecomputeFromMask();
		}

		public void FeatherActive(int radius)
		{
			if (!m_active || radius <= 0)
			{
				return;
			}
			SKRectI inflated = new SKRectI(m_bounds.Left - ((radius * 3) + 1), m_bounds.Top - ((radius * 3) + 1), m_bounds.Right + ((radius * 3) + 1), m_bounds.Bottom + ((radius * 3) + 1));
			GrowToInclude(inflated);
			int savedRadius = m_featherRadius;
			m_featherRadius = radius;
			SKRectI localBounds = new SKRectI(m_bounds.Left - m_originX, m_bounds.Top - m_originY, m_bounds.Right - m_originX, m_bounds.Bottom - m_originY);
			FeatherRegion(m_mask, m_maskWidth, m_maskHeight, localBounds);
			m_featherRadius = savedRadius;
			m_lastCombinedBounds = MaskRect();
			RecomputeFromMask();
		}

		public void ContractActive(int pixels)
		{
			if (!m_active || pixels <= 0)
			{
				return;
			}
			if (m_regionScratch == null || m_regionScratch.Length < m_mask.Length)
			{
				m_regionScratch = new byte[m_mask.Length];
			}
			SKRectI work = ClampToMask(m_bounds);
			for (int y = work.Top; y < work.Bottom; y++)
			{
				int rowStart = MaskRow(y) - m_originX;
				for (int x = work.Left; x < work.Right; x++)
				{
					int index = rowStart + x;
					if (m_mask[index] < 128)
					{
						m_regionScratch[index] = 0;
						continue;
					}
					bool eroded = false;
					int neighborTop = y - pixels;
					int neighborBottom = y + pixels;
					int neighborLeft = x - pixels;
					int neighborRight = x + pixels;
					for (int neighborY = neighborTop; neighborY <= neighborBottom; neighborY++)
					{
						if (neighborY < m_originY || neighborY >= m_originY + m_maskHeight)
						{
							eroded = true;
							break;
						}
						int neighborRowStart = MaskRow(neighborY) - m_originX;
						for (int neighborX = neighborLeft; neighborX <= neighborRight; neighborX++)
						{
							if (neighborX < m_originX || neighborX >= m_originX + m_maskWidth)
							{
								eroded = true;
								break;
							}
							if (m_mask[neighborRowStart + neighborX] < 128)
							{
								eroded = true;
								break;
							}
						}
						if (eroded)
						{
							break;
						}
					}
					if (eroded)
					{
						m_regionScratch[index] = 0;
					}
					else
					{
						m_regionScratch[index] = 255;
					}
				}
			}
			for (int y = work.Top; y < work.Bottom; y++)
			{
				int rowStart = MaskRow(y) - m_originX;
				for (int x = work.Left; x < work.Right; x++)
				{
					int index = rowStart + x;
					m_mask[index] = m_regionScratch[index];
				}
			}
			m_lastCombinedBounds = MaskRect();
			RecomputeFromMask();
			if (!m_active)
			{
				Clear();
			}
		}

		public void SmoothActive(int radius)
		{
			if (!m_active || radius <= 0)
			{
				return;
			}
			SKRectI inflated = new SKRectI(m_bounds.Left - radius, m_bounds.Top - radius, m_bounds.Right + radius, m_bounds.Bottom + radius);
			GrowToInclude(inflated);
			if (m_regionScratch == null || m_regionScratch.Length < m_mask.Length)
			{
				m_regionScratch = new byte[m_mask.Length];
			}
			SKRectI work = ClampToMask(inflated);
			int window = (radius * 2) + 1;
			int threshold = (window * window) / 2;
			for (int y = work.Top; y < work.Bottom; y++)
			{
				int rowStart = MaskRow(y) - m_originX;
				for (int x = work.Left; x < work.Right; x++)
				{
					int index = rowStart + x;
					int selectedCount = 0;
					int neighborTop = y - radius;
					int neighborBottom = y + radius;
					int neighborLeft = x - radius;
					int neighborRight = x + radius;
					for (int neighborY = neighborTop; neighborY <= neighborBottom; neighborY++)
					{
						if (neighborY < m_originY || neighborY >= m_originY + m_maskHeight)
						{
							continue;
						}
						int neighborRowStart = MaskRow(neighborY) - m_originX;
						for (int neighborX = neighborLeft; neighborX <= neighborRight; neighborX++)
						{
							if (neighborX < m_originX || neighborX >= m_originX + m_maskWidth)
							{
								continue;
							}
							if (m_mask[neighborRowStart + neighborX] >= 128)
							{
								selectedCount = selectedCount + 1;
							}
						}
					}
					if (selectedCount > threshold)
					{
						m_regionScratch[index] = 255;
					}
					else
					{
						m_regionScratch[index] = 0;
					}
				}
			}
			for (int y = work.Top; y < work.Bottom; y++)
			{
				int rowStart = MaskRow(y) - m_originX;
				for (int x = work.Left; x < work.Right; x++)
				{
					int index = rowStart + x;
					m_mask[index] = m_regionScratch[index];
				}
			}
			m_lastCombinedBounds = MaskRect();
			RecomputeFromMask();
			if (!m_active)
			{
				Clear();
			}
		}

		public void SelectRect(SKRectI rect)
		{
			if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
			{
				Clear();
				return;
			}
			GrowToInclude(rect);
			Array.Clear(m_mask, 0, m_mask.Length);
			for (int y = rect.Top; y < rect.Bottom; y++)
			{
				int rowStart = MaskRow(y) - m_originX;
				for (int x = rect.Left; x < rect.Right; x++)
				{
					m_mask[rowStart + x] = 255;
				}
			}
			m_lastCombinedBounds = MaskRect();
			m_lastOpShift = false;
			m_active = true;
			m_bounds = rect;
			m_generation = m_generation + 1;
		}

		public void SelectMask(byte[] mask, SKRectI bounds)
		{
			ResetGeometry();
			Buffer.BlockCopy(mask, 0, m_mask, 0, m_mask.Length);
			m_lastCombinedBounds = MaskRect();
			m_lastOpShift = false;
			m_active = true;
			m_bounds = bounds;
			m_generation = m_generation + 1;
		}

		public void SelectMaskPlaced(byte[] mask, SKRectI maskRect, SKRectI bounds)
		{
			m_originX = maskRect.Left;
			m_originY = maskRect.Top;
			m_maskWidth = maskRect.Width;
			m_maskHeight = maskRect.Height;
			int count = m_maskWidth * m_maskHeight;
			if (m_mask.Length != count)
			{
				m_mask = new byte[count];
				m_baseMask = new byte[count];
				m_regionScratch = null;
				m_blurScratch = null;
			}
			Buffer.BlockCopy(mask, 0, m_mask, 0, count);
			m_lastCombinedBounds = MaskRect();
			m_lastOpShift = false;
			m_active = true;
			m_bounds = bounds;
			m_generation = m_generation + 1;
		}

		public byte[] Mask()
		{
			return m_mask;
		}

		public byte[] MaskCopy()
		{
			byte[] copy = new byte[m_mask.Length];
			Buffer.BlockCopy(m_mask, 0, copy, 0, m_mask.Length);
			return copy;
		}

		public void SetShifted(byte[] sourceMask, SKRectI sourceRect, SKRectI sourceBounds, int deltaX, int deltaY)
		{
			if (m_active && m_bounds.Width > 0 && m_bounds.Height > 0)
			{
				SKRectI clear = ClampToMask(m_bounds);
				for (int y = clear.Top; y < clear.Bottom; y++)
				{
					Array.Clear(m_mask, MaskRow(y) + (clear.Left - m_originX), clear.Width);
				}
			}
			m_lastCombinedBounds = MaskRect();
			if (sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
			{
				m_lastOpShift = false;
				m_active = false;
				m_bounds = SKRectI.Empty;
				m_generation = m_generation + 1;
				return;
			}
			SKRectI shifted = new SKRectI(sourceBounds.Left + deltaX, sourceBounds.Top + deltaY, sourceBounds.Right + deltaX, sourceBounds.Bottom + deltaY);
			GrowToInclude(shifted);
			for (int y = shifted.Top; y < shifted.Bottom; y++)
			{
				int sourceOffset = (((y - deltaY) - sourceRect.Top) * sourceRect.Width) + (sourceBounds.Left - sourceRect.Left);
				int destOffset = MaskRow(y) + (shifted.Left - m_originX);
				Buffer.BlockCopy(sourceMask, sourceOffset, m_mask, destOffset, shifted.Width);
			}
			m_lastCombinedBounds = MaskRect();
			bool translatable = true;
			if (m_lastOpShift && !m_lastShiftUnclipped)
			{
				translatable = false;
			}
			int stepX = deltaX;
			int stepY = deltaY;
			if (m_lastOpShift)
			{
				stepX = deltaX - m_prevShiftDeltaX;
				stepY = deltaY - m_prevShiftDeltaY;
			}
			m_shiftStepX = stepX;
			m_shiftStepY = stepY;
			m_prevShiftDeltaX = deltaX;
			m_prevShiftDeltaY = deltaY;
			m_lastShiftUnclipped = true;
			m_shiftTranslatable = translatable;
			m_lastOpShift = true;
			m_active = true;
			m_bounds = shifted;
			m_generation = m_generation + 1;
		}

		public bool LastChangeWasTranslatableShift()
		{
			return m_lastOpShift && m_shiftTranslatable;
		}

		public int ShiftStepX()
		{
			return m_shiftStepX;
		}

		public int ShiftStepY()
		{
			return m_shiftStepY;
		}
	}
}
