using SkiaSharp;

namespace Bitmute.Imaging
{
	public class Selection
	{
		private int m_width;
		private int m_height;
		private byte[] m_mask;
		private bool m_active;
		private SKRectI m_bounds;

		public Selection(int width, int height)
		{
			m_width = width;
			m_height = height;
			m_mask = new byte[width * height];
			m_active = false;
			m_bounds = SKRectI.Empty;
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
		}

		public void SelectMask(byte[] mask, SKRectI bounds)
		{
			for (int index = 0; index < m_mask.Length; index++)
			{
				m_mask[index] = mask[index];
			}
			m_active = true;
			m_bounds = bounds;
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
				return;
			}
			m_active = true;
			m_bounds = new SKRectI(minX, minY, maxX + 1, maxY + 1);
		}
	}
}
