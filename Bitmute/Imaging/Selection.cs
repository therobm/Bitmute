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
		private int m_width;
		private int m_height;
		private byte[] m_mask;
		private byte[] m_baseMask;
		private SKRectI m_baseBounds;
		private eSelectionMode m_operationMode;
		private int m_featherRadius;
		private byte[] m_regionScratch;
		private byte[] m_blurScratch;
		private bool m_active;
		private SKRectI m_bounds;
		private int m_generation;

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
			m_mask = new byte[width * height];
			m_baseMask = new byte[width * height];
			m_baseBounds = SKRectI.Empty;
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

		private SKRectI ClampToCanvas(SKRectI rect)
		{
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
			if (right <= left || bottom <= top)
			{
				return SKRectI.Empty;
			}
			return new SKRectI(left, top, right, bottom);
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
			RecomputeFromMask(new SKRectI(0, 0, m_width, m_height));
		}

		private void RecomputeFromMask(SKRectI searchBounds)
		{
			SKRectI search = ClampToCanvas(searchBounds);
			int minX = m_width;
			int minY = m_height;
			int maxX = -1;
			int maxY = -1;
			for (int y = search.Top; y < search.Bottom; y++)
			{
				int rowStart = y * m_width;
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
			if (maxX < 0)
			{
				m_active = false;
				m_bounds = SKRectI.Empty;
			}
			else
			{
				m_active = true;
				m_bounds = new SKRectI(minX, minY, maxX + 1, maxY + 1);
			}
			m_generation = m_generation + 1;
		}

		private SKRectI NonzeroBounds(byte[] mask)
		{
			int minX = m_width;
			int minY = m_height;
			int maxX = -1;
			int maxY = -1;
			for (int y = 0; y < m_height; y++)
			{
				int rowStart = y * m_width;
				for (int x = 0; x < m_width; x++)
				{
					if (mask[rowStart + x] == 0)
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
			if (maxX < 0)
			{
				return SKRectI.Empty;
			}
			return new SKRectI(minX, minY, maxX + 1, maxY + 1);
		}

		private void CombineRegion(byte[] region, SKRectI regionBounds)
		{
			SKRectI bounds = ClampToCanvas(regionBounds);
			if (m_operationMode == eSelectionMode.Intersect)
			{
				Array.Clear(m_mask, 0, m_mask.Length);
				for (int y = bounds.Top; y < bounds.Bottom; y++)
				{
					int rowStart = y * m_width;
					for (int x = bounds.Left; x < bounds.Right; x++)
					{
						int index = rowStart + x;
						int baseValue = m_baseMask[index];
						int regionValue = region[index];
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
			Buffer.BlockCopy(m_baseMask, 0, m_mask, 0, m_baseMask.Length);
			if (m_operationMode == eSelectionMode.Subtract)
			{
				for (int y = bounds.Top; y < bounds.Bottom; y++)
				{
					int rowStart = y * m_width;
					for (int x = bounds.Left; x < bounds.Right; x++)
					{
						int index = rowStart + x;
						int result = m_baseMask[index] - region[index];
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
				int rowStart = y * m_width;
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					int index = rowStart + x;
					int baseValue = m_baseMask[index];
					int regionValue = region[index];
					int result = baseValue;
					if (regionValue > result)
					{
						result = regionValue;
					}
					m_mask[index] = (byte)result;
				}
			}
		}

		private void BoxBlurHorizontal(byte[] source, byte[] destination, int left, int top, int right, int bottom, int radius)
		{
			HorizontalBlurWorker worker = new HorizontalBlurWorker();
			worker.m_source = source;
			worker.m_destination = destination;
			worker.m_width = m_width;
			worker.m_left = left;
			worker.m_top = top;
			worker.m_right = right;
			worker.m_bottom = bottom;
			worker.m_radius = radius;
			RowBands.Run(top, bottom, worker.Band);
		}

		private void BoxBlurVertical(byte[] source, byte[] destination, int left, int top, int right, int bottom, int radius)
		{
			VerticalBlurWorker worker = new VerticalBlurWorker();
			worker.m_source = source;
			worker.m_destination = destination;
			worker.m_width = m_width;
			worker.m_left = left;
			worker.m_top = top;
			worker.m_right = right;
			worker.m_bottom = bottom;
			worker.m_radius = radius;
			RowBands.Run(left, right, worker.Band);
		}

		private void FeatherRegion(byte[] mask, SKRectI bounds)
		{
			int radius = m_featherRadius;
			if (radius <= 0)
			{
				return;
			}
			int inflate = (radius * 3) + 1;
			int left = bounds.Left - inflate;
			int top = bounds.Top - inflate;
			int right = bounds.Right + inflate;
			int bottom = bounds.Bottom + inflate;
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
			if (right <= left || bottom <= top)
			{
				return;
			}
			if (m_blurScratch == null || m_blurScratch.Length != m_mask.Length)
			{
				m_blurScratch = new byte[m_mask.Length];
			}
			for (int pass = 0; pass < 3; pass++)
			{
				BoxBlurHorizontal(mask, m_blurScratch, left, top, right, bottom, radius);
				BoxBlurVertical(m_blurScratch, mask, left, top, right, bottom, radius);
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
		}

		public void ApplyRect(SKRectI rect)
		{
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
			bool hasArea = right > left && bottom > top;
			if (m_featherRadius > 0 && hasArea)
			{
				if (m_regionScratch == null || m_regionScratch.Length != m_mask.Length)
				{
					m_regionScratch = new byte[m_mask.Length];
				}
				int inflate = (m_featherRadius * 3) + 1;
				SKRectI inflated = ClampToCanvas(new SKRectI(left - inflate, top - inflate, right + inflate, bottom + inflate));
				for (int y = inflated.Top; y < inflated.Bottom; y++)
				{
					Array.Clear(m_regionScratch, (y * m_width) + inflated.Left, inflated.Width);
				}
				for (int y = top; y < bottom; y++)
				{
					int rowStart = y * m_width;
					for (int x = left; x < right; x++)
					{
						m_regionScratch[rowStart + x] = 255;
					}
				}
				FeatherRegion(m_regionScratch, new SKRectI(left, top, right, bottom));
				CombineRegion(m_regionScratch, inflated);
				RecomputeFromMask(CombinedResultBounds(inflated));
				return;
			}
			SKRectI rectBounds = SKRectI.Empty;
			if (hasArea)
			{
				rectBounds = new SKRectI(left, top, right, bottom);
			}
			if (m_operationMode == eSelectionMode.Intersect)
			{
				Array.Clear(m_mask, 0, m_mask.Length);
				for (int y = top; y < bottom; y++)
				{
					int rowStart = y * m_width;
					for (int x = left; x < right; x++)
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
			Buffer.BlockCopy(m_baseMask, 0, m_mask, 0, m_baseMask.Length);
			for (int y = top; y < bottom; y++)
			{
				int rowStart = y * m_width;
				for (int x = left; x < right; x++)
				{
					m_mask[rowStart + x] = value;
				}
			}
			RecomputeFromMask(CombinedResultBounds(rectBounds));
		}

		public void ApplyMask(byte[] regionMask)
		{
			SKRectI regionBounds = NonzeroBounds(regionMask);
			if (m_featherRadius > 0 && regionBounds.Width > 0 && regionBounds.Height > 0)
			{
				FeatherRegion(regionMask, regionBounds);
				int inflate = (m_featherRadius * 3) + 1;
				regionBounds = ClampToCanvas(new SKRectI(regionBounds.Left - inflate, regionBounds.Top - inflate, regionBounds.Right + inflate, regionBounds.Bottom + inflate));
			}
			CombineRegion(regionMask, regionBounds);
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
			if (x < 0 || y < 0 || x >= m_width || y >= m_height)
			{
				return false;
			}
			return m_mask[(y * m_width) + x] >= 128;
		}

		public int Coverage(int x, int y)
		{
			if (x < 0 || y < 0 || x >= m_width || y >= m_height)
			{
				return 0;
			}
			return m_mask[(y * m_width) + x];
		}

		public void Clear()
		{
			Array.Clear(m_mask, 0, m_mask.Length);
			m_active = false;
			m_bounds = SKRectI.Empty;
			m_generation = m_generation + 1;
		}

		public void Invert()
		{
			for (int index = 0; index < m_mask.Length; index++)
			{
				m_mask[index] = (byte)(255 - m_mask[index]);
			}
			RecomputeFromMask();
		}

		public void FeatherActive(int radius)
		{
			if (!m_active || radius <= 0)
			{
				return;
			}
			int savedRadius = m_featherRadius;
			m_featherRadius = radius;
			FeatherRegion(m_mask, m_bounds);
			m_featherRadius = savedRadius;
			RecomputeFromMask();
		}

		public void SelectRect(SKRectI rect)
		{
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
			if (right <= left || bottom <= top)
			{
				Clear();
				return;
			}

			Array.Clear(m_mask, 0, m_mask.Length);
			for (int y = top; y < bottom; y++)
			{
				int rowStart = y * m_width;
				for (int x = left; x < right; x++)
				{
					m_mask[rowStart + x] = 255;
				}
			}
			m_active = true;
			m_bounds = new SKRectI(left, top, right, bottom);
			m_generation = m_generation + 1;
		}

		public void SelectMask(byte[] mask, SKRectI bounds)
		{
			Buffer.BlockCopy(mask, 0, m_mask, 0, m_mask.Length);
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

		public void SetShifted(byte[] sourceMask, SKRectI sourceBounds, int deltaX, int deltaY)
		{
			if (m_active && m_bounds.Width > 0 && m_bounds.Height > 0)
			{
				for (int y = m_bounds.Top; y < m_bounds.Bottom; y++)
				{
					Array.Clear(m_mask, (y * m_width) + m_bounds.Left, m_bounds.Width);
				}
			}
			int minX = m_width;
			int minY = m_height;
			int maxX = -1;
			int maxY = -1;
			for (int y = sourceBounds.Top; y < sourceBounds.Bottom; y++)
			{
				for (int x = sourceBounds.Left; x < sourceBounds.Right; x++)
				{
					byte coverage = sourceMask[(y * m_width) + x];
					if (coverage == 0)
					{
						continue;
					}
					int newX = x + deltaX;
					int newY = y + deltaY;
					if (newX < 0 || newY < 0 || newX >= m_width || newY >= m_height)
					{
						continue;
					}
					m_mask[(newY * m_width) + newX] = coverage;
					if (newX < minX)
					{
						minX = newX;
					}
					if (newX > maxX)
					{
						maxX = newX;
					}
					if (newY < minY)
					{
						minY = newY;
					}
					if (newY > maxY)
					{
						maxY = newY;
					}
				}
			}
			if (maxX < 0)
			{
				m_active = false;
				m_bounds = SKRectI.Empty;
				m_generation = m_generation + 1;
				return;
			}
			m_active = true;
			m_bounds = new SKRectI(minX, minY, maxX + 1, maxY + 1);
			m_generation = m_generation + 1;
		}
	}
}
