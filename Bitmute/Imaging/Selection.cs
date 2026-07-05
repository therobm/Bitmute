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
		private eSelectionMode m_operationMode;
		private int m_featherRadius;
		private byte[] m_regionScratch;
		private byte[] m_blurScratch;
		private bool m_active;
		private SKRectI m_bounds;
		private int m_generation;

		public Selection(int width, int height)
		{
			m_width = width;
			m_height = height;
			m_mask = new byte[width * height];
			m_baseMask = new byte[width * height];
			m_operationMode = eSelectionMode.Replace;
			m_featherRadius = 0;
			m_regionScratch = null;
			m_blurScratch = null;
			m_active = false;
			m_bounds = SKRectI.Empty;
			m_generation = 0;
		}

		private void RecomputeFromMask()
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

		private void CombineRegion(byte[] region)
		{
			if (m_operationMode == eSelectionMode.Intersect)
			{
				for (int index = 0; index < m_mask.Length; index++)
				{
					int baseValue = m_baseMask[index];
					int regionValue = region[index];
					int result = baseValue;
					if (regionValue < result)
					{
						result = regionValue;
					}
					m_mask[index] = (byte)result;
				}
				return;
			}
			if (m_operationMode == eSelectionMode.Subtract)
			{
				for (int index = 0; index < m_mask.Length; index++)
				{
					int result = m_baseMask[index] - region[index];
					if (result < 0)
					{
						result = 0;
					}
					m_mask[index] = (byte)result;
				}
				return;
			}
			for (int index = 0; index < m_mask.Length; index++)
			{
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

		private void BoxBlurHorizontal(byte[] source, byte[] destination, int left, int top, int right, int bottom, int radius)
		{
			int window = (radius * 2) + 1;
			int half = window / 2;
			for (int y = top; y < bottom; y++)
			{
				int rowStart = y * m_width;
				int sum = 0;
				int primeEnd = left + radius;
				if (primeEnd > right - 1)
				{
					primeEnd = right - 1;
				}
				for (int x = left; x <= primeEnd; x++)
				{
					sum = sum + source[rowStart + x];
				}
				for (int x = left; x < right; x++)
				{
					destination[rowStart + x] = (byte)((sum + half) / window);
					int addIndex = x + radius + 1;
					if (addIndex < right)
					{
						sum = sum + source[rowStart + addIndex];
					}
					int removeIndex = x - radius;
					if (removeIndex >= left)
					{
						sum = sum - source[rowStart + removeIndex];
					}
				}
			}
		}

		private void BoxBlurVertical(byte[] source, byte[] destination, int left, int top, int right, int bottom, int radius)
		{
			int window = (radius * 2) + 1;
			int half = window / 2;
			for (int x = left; x < right; x++)
			{
				int sum = 0;
				int primeEnd = top + radius;
				if (primeEnd > bottom - 1)
				{
					primeEnd = bottom - 1;
				}
				for (int y = top; y <= primeEnd; y++)
				{
					sum = sum + source[(y * m_width) + x];
				}
				for (int y = top; y < bottom; y++)
				{
					destination[(y * m_width) + x] = (byte)((sum + half) / window);
					int addIndex = y + radius + 1;
					if (addIndex < bottom)
					{
						sum = sum + source[(addIndex * m_width) + x];
					}
					int removeIndex = y - radius;
					if (removeIndex >= top)
					{
						sum = sum - source[(removeIndex * m_width) + x];
					}
				}
			}
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
				for (int index = 0; index < m_baseMask.Length; index++)
				{
					m_baseMask[index] = 0;
				}
			}
			else
			{
				for (int index = 0; index < m_baseMask.Length; index++)
				{
					m_baseMask[index] = m_mask[index];
				}
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
				System.Array.Clear(m_regionScratch, 0, m_regionScratch.Length);
				for (int y = top; y < bottom; y++)
				{
					int rowStart = y * m_width;
					for (int x = left; x < right; x++)
					{
						m_regionScratch[rowStart + x] = 255;
					}
				}
				FeatherRegion(m_regionScratch, new SKRectI(left, top, right, bottom));
				CombineRegion(m_regionScratch);
				RecomputeFromMask();
				return;
			}
			if (m_operationMode == eSelectionMode.Intersect)
			{
				for (int index = 0; index < m_mask.Length; index++)
				{
					m_mask[index] = 0;
				}
				for (int y = top; y < bottom; y++)
				{
					int rowStart = y * m_width;
					for (int x = left; x < right; x++)
					{
						m_mask[rowStart + x] = m_baseMask[rowStart + x];
					}
				}
				RecomputeFromMask();
				return;
			}
			byte value = 255;
			if (m_operationMode == eSelectionMode.Subtract)
			{
				value = 0;
			}
			for (int index = 0; index < m_mask.Length; index++)
			{
				m_mask[index] = m_baseMask[index];
			}
			for (int y = top; y < bottom; y++)
			{
				int rowStart = y * m_width;
				for (int x = left; x < right; x++)
				{
					m_mask[rowStart + x] = value;
				}
			}
			RecomputeFromMask();
		}

		public void ApplyMask(byte[] regionMask)
		{
			if (m_featherRadius > 0)
			{
				SKRectI regionBounds = NonzeroBounds(regionMask);
				if (regionBounds.Width > 0 && regionBounds.Height > 0)
				{
					FeatherRegion(regionMask, regionBounds);
				}
			}
			CombineRegion(regionMask);
			RecomputeFromMask();
		}

		public int Generation()
		{
			return m_generation;
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
			for (int index = 0; index < m_mask.Length; index++)
			{
				m_mask[index] = 0;
			}
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

			for (int index = 0; index < m_mask.Length; index++)
			{
				m_mask[index] = 0;
			}
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
			for (int index = 0; index < m_mask.Length; index++)
			{
				m_mask[index] = mask[index];
			}
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
			for (int index = 0; index < m_mask.Length; index++)
			{
				copy[index] = m_mask[index];
			}
			return copy;
		}

		public void SetShifted(byte[] sourceMask, SKRectI sourceBounds, int deltaX, int deltaY)
		{
			for (int index = 0; index < m_mask.Length; index++)
			{
				m_mask[index] = 0;
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
