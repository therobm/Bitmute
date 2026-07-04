using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class SliceTool : Tool
	{
		private bool m_active;
		private int m_startX;
		private int m_startY;
		private int m_endX;
		private int m_endY;

		public override bool IsDestructive()
		{
			return false;
		}

		public bool HasPreview()
		{
			return m_active;
		}

		public int PreviewLeft()
		{
			if (m_startX < m_endX)
			{
				return m_startX;
			}
			return m_endX;
		}

		public int PreviewTop()
		{
			if (m_startY < m_endY)
			{
				return m_startY;
			}
			return m_endY;
		}

		public int PreviewRight()
		{
			if (m_startX > m_endX)
			{
				return m_startX;
			}
			return m_endX;
		}

		public int PreviewBottom()
		{
			if (m_startY > m_endY)
			{
				return m_startY;
			}
			return m_endY;
		}

		public void Reset()
		{
			m_active = false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			if (state.AltHeld())
			{
				int hit = document.Slices().HitTest(x, y);
				if (hit >= 0)
				{
					document.Slices().RemoveAt(hit);
				}
				return false;
			}
			m_active = true;
			m_startX = x;
			m_startY = y;
			m_endX = x;
			m_endY = y;
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				return false;
			}
			m_endX = x;
			m_endY = y;
			return false;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				return;
			}
			m_endX = x;
			m_endY = y;
			m_active = false;
			int left = PreviewLeft();
			int top = PreviewTop();
			int right = PreviewRight();
			int bottom = PreviewBottom();
			if (right - left < 2 || bottom - top < 2)
			{
				return;
			}
			Slices slices = document.Slices();
			string name = "Slice " + (slices.Count() + 1);
			slices.Add(name, new SKRectI(left, top, right, bottom));
		}
	}
}
