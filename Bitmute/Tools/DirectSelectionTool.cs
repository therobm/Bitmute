using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class DirectSelectionTool : Tool
	{
		private const int GrabNone = 0;
		private const int GrabAnchor = 1;
		private const int GrabHandleIn = 2;
		private const int GrabHandleOut = 3;

		private int m_pickRadius;
		private int m_selectedPath;
		private int m_selectedAnchor;
		private int m_grab;
		private bool m_active;
		private int m_prevX;
		private int m_prevY;

		public DirectSelectionTool()
		{
			m_pickRadius = 6;
			m_selectedPath = -1;
			m_selectedAnchor = -1;
			m_grab = GrabNone;
			m_active = false;
			m_prevX = 0;
			m_prevY = 0;
		}

		public void SetPickRadius(int radius)
		{
			if (radius < 1)
			{
				radius = 1;
			}
			m_pickRadius = radius;
		}

		public int SelectedPath()
		{
			return m_selectedPath;
		}

		public int SelectedAnchor()
		{
			return m_selectedAnchor;
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			List<PathData> paths = document.Paths();
			m_grab = GrabNone;

			if (m_selectedPath >= 0 && m_selectedPath < paths.Count && m_selectedAnchor >= 0 && m_selectedAnchor < paths[m_selectedPath].m_points.Count)
			{
				PathData selectedPath = paths[m_selectedPath];
				if (selectedPath.HitHandleOut(m_selectedAnchor, x, y, m_pickRadius))
				{
					m_grab = GrabHandleOut;
				}
				else if (selectedPath.HitHandleIn(m_selectedAnchor, x, y, m_pickRadius))
				{
					m_grab = GrabHandleIn;
				}
				if (m_grab != GrabNone)
				{
					document.BeginPathEdit("Edit Path");
					m_active = true;
					m_prevX = x;
					m_prevY = y;
					return true;
				}
			}

			int pathCount = paths.Count;
			for (int p = 0; p < pathCount; p++)
			{
				int anchorIndex = paths[p].HitAnchor(x, y, m_pickRadius);
				if (anchorIndex >= 0)
				{
					m_selectedPath = p;
					m_selectedAnchor = anchorIndex;
					m_grab = GrabAnchor;
					document.BeginPathEdit("Move Anchor");
					m_active = true;
					m_prevX = x;
					m_prevY = y;
					return true;
				}
			}

			m_selectedPath = -1;
			m_selectedAnchor = -1;
			return true;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				return false;
			}
			List<PathData> paths = document.Paths();
			if (m_selectedPath < 0 || m_selectedPath >= paths.Count)
			{
				return false;
			}
			PathData path = paths[m_selectedPath];
			if (m_grab == GrabAnchor)
			{
				int dx = x - m_prevX;
				int dy = y - m_prevY;
				path.MoveAnchor(m_selectedAnchor, dx, dy);
				m_prevX = x;
				m_prevY = y;
				return true;
			}
			if (m_grab == GrabHandleOut)
			{
				if (state.AltHeld())
				{
					path.m_points[m_selectedAnchor].m_smooth = false;
				}
				path.MoveHandleOut(m_selectedAnchor, x, y);
				return true;
			}
			if (m_grab == GrabHandleIn)
			{
				if (state.AltHeld())
				{
					path.m_points[m_selectedAnchor].m_smooth = false;
				}
				path.MoveHandleIn(m_selectedAnchor, x, y);
				return true;
			}
			return true;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (m_active)
			{
				document.EndPathEdit();
				m_active = false;
				m_grab = GrabNone;
			}
		}

		public bool DeleteSelected(Document document)
		{
			List<PathData> paths = document.Paths();
			if (m_selectedPath >= 0 && m_selectedPath < paths.Count && m_selectedAnchor >= 0 && m_selectedAnchor < paths[m_selectedPath].m_points.Count)
			{
				document.BeginPathEdit("Delete Anchor");
				PathData path = paths[m_selectedPath];
				path.RemoveAnchorAt(m_selectedAnchor);
				if (path.m_points.Count < 2)
				{
					paths.RemoveAt(m_selectedPath);
				}
				m_selectedPath = -1;
				m_selectedAnchor = -1;
				document.EndPathEdit();
				return true;
			}
			return false;
		}

		public override void Reset()
		{
			m_selectedPath = -1;
			m_selectedAnchor = -1;
			m_grab = GrabNone;
			m_active = false;
		}
	}
}
