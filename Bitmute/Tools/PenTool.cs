using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class PenTool : Tool
	{
		public const int ModeDraw = 0;
		public const int ModeClose = 1;
		public const int ModeAdd = 2;
		public const int ModeDelete = 3;

		private PathData m_currentPath;
		private bool m_active;
		private int m_dragStartX;
		private int m_dragStartY;
		private int m_pickRadius;

		public PenTool()
		{
			m_currentPath = null;
			m_active = false;
			m_pickRadius = 6;
		}

		private static float Distance(float ax, float ay, float bx, float by)
		{
			float dx = ax - bx;
			float dy = ay - by;
			return (float)Math.Sqrt((dx * dx) + (dy * dy));
		}

		private void ConvertAnchor(PathData path, int anchorIndex)
		{
			PathPoint point = path.m_points[anchorIndex];
			if (point.m_smooth)
			{
				point.m_smooth = false;
				point.m_hasControlIn = false;
				point.m_hasControlOut = false;
				return;
			}
			int count = path.m_points.Count;
			PathPoint prev;
			PathPoint next;
			if (path.m_isClosed)
			{
				prev = path.m_points[((anchorIndex - 1) + count) % count];
				next = path.m_points[(anchorIndex + 1) % count];
			}
			else
			{
				if (anchorIndex > 0)
				{
					prev = path.m_points[anchorIndex - 1];
				}
				else
				{
					prev = point;
				}
				if (anchorIndex < count - 1)
				{
					next = path.m_points[anchorIndex + 1];
				}
				else
				{
					next = point;
				}
			}
			float tangentX = next.m_x - prev.m_x;
			float tangentY = next.m_y - prev.m_y;
			float len = (float)Math.Sqrt((tangentX * tangentX) + (tangentY * tangentY));
			if (len < 0.0001f)
			{
				return;
			}
			float ux = tangentX / len;
			float uy = tangentY / len;
			float outDist = 0.33f * Distance(point.m_x, point.m_y, next.m_x, next.m_y);
			float inDist = 0.33f * Distance(point.m_x, point.m_y, prev.m_x, prev.m_y);
			point.m_controlOutX = point.m_x + (ux * outDist);
			point.m_controlOutY = point.m_y + (uy * outDist);
			point.m_controlInX = point.m_x - (ux * inDist);
			point.m_controlInY = point.m_y - (uy * inDist);
			point.m_hasControlIn = true;
			point.m_hasControlOut = true;
			point.m_smooth = true;
		}

		public void SetPickRadius(int radius)
		{
			if (radius < 1)
			{
				radius = 1;
			}
			m_pickRadius = radius;
		}

		public bool HasPreview()
		{
			return m_active && m_currentPath != null && m_currentPath.m_points.Count > 0;
		}

		public bool HasActivePath()
		{
			return m_active && m_currentPath != null && m_currentPath.m_points.Count >= 1;
		}

		public int HoverMode(Document document, int x, int y, int radius)
		{
			if (m_active && m_currentPath != null)
			{
				int activeCount = m_currentPath.m_points.Count;
				if (activeCount >= 2)
				{
					PathPoint first = m_currentPath.m_points[0];
					float distance = Distance(x, y, first.m_x, first.m_y);
					if (distance <= radius)
					{
						return ModeClose;
					}
				}
				return ModeDraw;
			}
			List<PathData> paths = document.Paths();
			int pathCount = paths.Count;
			for (int p = 0; p < pathCount; p++)
			{
				PathData path = paths[p];
				int anchorIndex = path.HitAnchor(x, y, radius);
				if (anchorIndex >= 0)
				{
					return ModeDelete;
				}
				int segmentIndex;
				float segT;
				bool onSeg = path.HitSegment(x, y, radius, out segmentIndex, out segT);
				if (onSeg)
				{
					return ModeAdd;
				}
			}
			return ModeDraw;
		}

		public PathData CurrentPath()
		{
			return m_currentPath;
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public void FinishPath(Document document)
		{
			if (HasActivePath() && m_currentPath.m_points.Count >= 2)
			{
				document.BeginPathEdit("Add Path");
				document.AddPath(m_currentPath);
				document.EndPathEdit();
				m_currentPath = null;
				m_active = false;
				return;
			}
			m_currentPath = null;
			m_active = false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			if (m_active && m_currentPath != null)
			{
				int count = m_currentPath.m_points.Count;
				if (count >= 2)
				{
					PathPoint first = m_currentPath.m_points[0];
					float distance = Distance(x, y, first.m_x, first.m_y);
					if (distance <= m_pickRadius)
					{
						m_currentPath.m_isClosed = true;
						document.BeginPathEdit("Add Path");
						document.AddPath(m_currentPath);
						document.EndPathEdit();
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

			List<PathData> paths = document.Paths();
			int pathCount = paths.Count;
			for (int p = 0; p < pathCount; p++)
			{
				PathData path = paths[p];
				int anchorIndex = path.HitAnchor(x, y, m_pickRadius);
				if (anchorIndex >= 0)
				{
					if (state.AltHeld())
					{
						document.BeginPathEdit("Convert Anchor");
						ConvertAnchor(path, anchorIndex);
						document.EndPathEdit();
						return true;
					}
					document.BeginPathEdit("Delete Anchor");
					path.RemoveAnchorAt(anchorIndex);
					if (path.m_points.Count < 2)
					{
						paths.RemoveAt(p);
					}
					document.EndPathEdit();
					return true;
				}
				int segmentIndex;
				float segT;
				bool onSeg = path.HitSegment(x, y, m_pickRadius, out segmentIndex, out segT);
				if (onSeg)
				{
					document.BeginPathEdit("Insert Anchor");
					path.InsertAnchorOnSegment(segmentIndex, segT);
					document.EndPathEdit();
					return true;
				}
			}

			m_currentPath = new PathData("Path");
			m_currentPath.m_strokeColor = state.Foreground();
			m_currentPath.m_points.Add(new PathPoint(x, y));
			m_active = true;
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

			if (Math.Abs(dx) < 2.0f && Math.Abs(dy) < 2.0f)
			{
				last.m_hasControlIn = false;
				last.m_hasControlOut = false;
				last.m_controlInX = last.m_x;
				last.m_controlInY = last.m_y;
				last.m_controlOutX = last.m_x;
				last.m_controlOutY = last.m_y;
				last.m_smooth = false;
				return true;
			}

			last.m_hasControlIn = true;
			last.m_hasControlOut = true;
			last.m_smooth = true;
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
