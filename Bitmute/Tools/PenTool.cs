using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class PenTool : Tool
	{
		private PathData m_currentPath;
		private bool m_active;
		private int m_dragStartX;
		private int m_dragStartY;
		private const float CloseDistance = 12.0f;

		public PenTool()
		{
			m_currentPath = null;
			m_active = false;
		}

		public bool HasPreview()
		{
			return m_active && m_currentPath != null && m_currentPath.m_points.Count > 0;
		}

		public PathData CurrentPath()
		{
			return m_currentPath;
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				m_currentPath = new PathData("Path");
				m_currentPath.m_strokeColor = state.Foreground();
				m_currentPath.m_points.Add(new PathPoint(x, y));
				m_active = true;
				m_dragStartX = x;
				m_dragStartY = y;
				return true;
			}

			int count = m_currentPath.m_points.Count;
			if (count > 0)
			{
				PathPoint first = m_currentPath.m_points[0];
				float dx = x - first.m_x;
				float dy = y - first.m_y;
				float dist = (float)System.Math.Sqrt((dx * dx) + (dy * dy));
				if (dist <= CloseDistance && count >= 2)
				{
					m_currentPath.m_isClosed = true;
					document.AddPath(m_currentPath);
					m_currentPath = null;
					m_active = false;
					return true;
				}
			}

			m_currentPath.m_points.Add(new PathPoint(x, y));
			m_dragStartX = x;
			m_dragStartY = y;
			return true;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (!m_active || m_currentPath == null)
			{
				return false;
			}

			int count = m_currentPath.m_points.Count;
			if (count == 0)
			{
				return false;
			}

			PathPoint last = m_currentPath.m_points[count - 1];

			float dx = x - m_dragStartX;
			float dy = y - m_dragStartY;

			if (System.Math.Abs(dx) < 2.0f && System.Math.Abs(dy) < 2.0f)
			{
				last.m_hasControlIn = false;
				last.m_hasControlOut = false;
				last.m_controlInX = last.m_x;
				last.m_controlInY = last.m_y;
				last.m_controlOutX = last.m_x;
				last.m_controlOutY = last.m_y;
				return true;
			}

			last.m_hasControlIn = true;
			last.m_hasControlOut = true;
			last.m_controlInX = last.m_x - dx;
			last.m_controlInY = last.m_y - dy;
			last.m_controlOutX = last.m_x + dx;
			last.m_controlOutY = last.m_y + dy;

			return true;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
		}

		public override void Reset()
		{
			m_currentPath = null;
			m_active = false;
		}

		public void CancelPath()
		{
			m_currentPath = null;
			m_active = false;
		}
	}
}