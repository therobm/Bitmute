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

		public void BeginOperation(eSelectionMode mode)
		{
			m_operationMode = mode;
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
						if (m_baseMask[rowStart + x] != 0)
						{
							m_mask[rowStart + x] = 255;
						}
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
			if (m_operationMode == eSelectionMode.Intersect)
			{
				for (int index = 0; index < m_mask.Length; index++)
				{
					if (m_baseMask[index] != 0 && regionMask[index] != 0)
					{
						m_mask[index] = 255;
					}
					else
					{
						m_mask[index] = 0;
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
				byte result = m_baseMask[index];
				if (regionMask[index] != 0)
				{
					result = value;
				}
				m_mask[index] = result;
			}
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
			return m_mask[(y * m_width) + x] != 0;
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
				if (m_mask[index] == 0)
				{
					m_mask[index] = 255;
				}
				else
				{
					m_mask[index] = 0;
				}
			}
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
					if (sourceMask[(y * m_width) + x] == 0)
					{
						continue;
					}
					int newX = x + deltaX;
					int newY = y + deltaY;
					if (newX < 0 || newY < 0 || newX >= m_width || newY >= m_height)
					{
						continue;
					}
					m_mask[(newY * m_width) + newX] = 255;
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
