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
	}
}
