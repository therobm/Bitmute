using System;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class CropTool : Tool
	{
		private const int DragNone = 0;
		private const int DragCreate = 1;
		private const int DragResize = 2;
		private const int DragMove = 3;
		private const int CornerTolerance = 8;
		private const int MinimumSize = 3;
		private const double DoubleClickMilliseconds = 350.0;
		private const int DoubleClickDistance = 4;

		private bool m_hasPending;
		private int m_left;
		private int m_top;
		private int m_right;
		private int m_bottom;
		private int m_dragMode;
		private int m_anchorX;
		private int m_anchorY;
		private int m_grabOffsetX;
		private int m_grabOffsetY;
		private int m_moveWidth;
		private int m_moveHeight;
		private DateTime m_lastPressTime;
		private int m_lastPressX;
		private int m_lastPressY;
		private bool m_hasLastPress;

		public CropTool()
		{
			m_hasPending = false;
			m_dragMode = DragNone;
			m_hasLastPress = false;
		}

		private static int ClampCoordinate(int value, int maximum)
		{
			if (value < 0)
			{
				return 0;
			}
			if (value > maximum)
			{
				return maximum;
			}
			return value;
		}

		private void ClearAll()
		{
			m_hasPending = false;
			m_dragMode = DragNone;
			m_hasLastPress = false;
		}

		private bool IsDoubleClick(int x, int y)
		{
			if (!m_hasLastPress)
			{
				return false;
			}
			double elapsed = (DateTime.Now - m_lastPressTime).TotalMilliseconds;
			if (elapsed > DoubleClickMilliseconds)
			{
				return false;
			}
			int deltaX = x - m_lastPressX;
			int deltaY = y - m_lastPressY;
			return ((deltaX * deltaX) + (deltaY * deltaY)) <= (DoubleClickDistance * DoubleClickDistance);
		}

		private void RecordPress(int x, int y)
		{
			m_lastPressTime = DateTime.Now;
			m_lastPressX = x;
			m_lastPressY = y;
			m_hasLastPress = true;
		}

		private static bool CornerHit(int x, int y, int cornerX, int cornerY)
		{
			int deltaX = x - cornerX;
			if (deltaX < 0)
			{
				deltaX = -deltaX;
			}
			int deltaY = y - cornerY;
			if (deltaY < 0)
			{
				deltaY = -deltaY;
			}
			return deltaX <= CornerTolerance && deltaY <= CornerTolerance;
		}

		private bool IsInsideRect(int x, int y)
		{
			return x >= m_left && x <= m_right && y >= m_top && y <= m_bottom;
		}

		private void SetRectFromAnchor(Document document, int x, int y, bool shift)
		{
			int clampedX = ClampCoordinate(x, document.Width());
			int clampedY = ClampCoordinate(y, document.Height());
			if (shift)
			{
				int deltaX = clampedX - m_anchorX;
				int deltaY = clampedY - m_anchorY;
				int absoluteX = deltaX;
				if (absoluteX < 0)
				{
					absoluteX = -absoluteX;
				}
				int absoluteY = deltaY;
				if (absoluteY < 0)
				{
					absoluteY = -absoluteY;
				}
				int side = absoluteX;
				if (absoluteY < side)
				{
					side = absoluteY;
				}
				int signX = 1;
				if (deltaX < 0)
				{
					signX = -1;
				}
				int signY = 1;
				if (deltaY < 0)
				{
					signY = -1;
				}
				clampedX = m_anchorX + (signX * side);
				clampedY = m_anchorY + (signY * side);
			}
			if (clampedX < m_anchorX)
			{
				m_left = clampedX;
				m_right = m_anchorX;
			}
			else
			{
				m_left = m_anchorX;
				m_right = clampedX;
			}
			if (clampedY < m_anchorY)
			{
				m_top = clampedY;
				m_bottom = m_anchorY;
			}
			else
			{
				m_top = m_anchorY;
				m_bottom = clampedY;
			}
		}

		private void BeginResize(int anchorX, int anchorY)
		{
			m_anchorX = anchorX;
			m_anchorY = anchorY;
			m_dragMode = DragResize;
		}

		private void BeginCreate(Document document, int x, int y, bool shift)
		{
			m_hasPending = false;
			m_anchorX = ClampCoordinate(x, document.Width());
			m_anchorY = ClampCoordinate(y, document.Height());
			m_dragMode = DragCreate;
			SetRectFromAnchor(document, x, y, shift);
		}

		private void Commit(Document document)
		{
			SKRectI rect = new SKRectI(m_left, m_top, m_right, m_bottom);
			document.CropToRect(rect);
			ClearAll();
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public void Reset()
		{
			ClearAll();
		}

		public bool HasPreview()
		{
			return m_hasPending || m_dragMode != DragNone;
		}

		public int RectLeft()
		{
			return m_left;
		}

		public int RectTop()
		{
			return m_top;
		}

		public int RectRight()
		{
			return m_right;
		}

		public int RectBottom()
		{
			return m_bottom;
		}

		public void CommitPending(Document document)
		{
			if (!m_hasPending)
			{
				return;
			}
			Commit(document);
		}

		public void CancelPending()
		{
			ClearAll();
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			bool doubleClick = IsDoubleClick(x, y);
			RecordPress(x, y);
			if (m_hasPending)
			{
				if (doubleClick && IsInsideRect(x, y))
				{
					Commit(document);
					return false;
				}
				if (CornerHit(x, y, m_left, m_top))
				{
					BeginResize(m_right, m_bottom);
					return false;
				}
				if (CornerHit(x, y, m_right, m_top))
				{
					BeginResize(m_left, m_bottom);
					return false;
				}
				if (CornerHit(x, y, m_left, m_bottom))
				{
					BeginResize(m_right, m_top);
					return false;
				}
				if (CornerHit(x, y, m_right, m_bottom))
				{
					BeginResize(m_left, m_top);
					return false;
				}
				if (IsInsideRect(x, y))
				{
					m_dragMode = DragMove;
					m_grabOffsetX = x - m_left;
					m_grabOffsetY = y - m_top;
					m_moveWidth = m_right - m_left;
					m_moveHeight = m_bottom - m_top;
					return false;
				}
			}
			BeginCreate(document, x, y, state.ShiftHeld());
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (m_dragMode == DragCreate || m_dragMode == DragResize)
			{
				SetRectFromAnchor(document, x, y, state.ShiftHeld());
				return false;
			}
			if (m_dragMode == DragMove)
			{
				int newLeft = ClampCoordinate(x - m_grabOffsetX, document.Width() - m_moveWidth);
				int newTop = ClampCoordinate(y - m_grabOffsetY, document.Height() - m_moveHeight);
				m_left = newLeft;
				m_top = newTop;
				m_right = newLeft + m_moveWidth;
				m_bottom = newTop + m_moveHeight;
				return false;
			}
			return false;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (m_dragMode == DragCreate || m_dragMode == DragResize)
			{
				SetRectFromAnchor(document, x, y, state.ShiftHeld());
				if ((m_right - m_left) >= MinimumSize && (m_bottom - m_top) >= MinimumSize)
				{
					m_hasPending = true;
				}
				else
				{
					m_hasPending = false;
				}
			}
			m_dragMode = DragNone;
		}
	}
}
