using Bitmute.Imaging;

namespace Bitmute.Tools
{
	public abstract class BrushFamilyTool : Tool
	{
		protected BrushEngine m_engine;
		private int m_strokeStartX;
		private int m_strokeStartY;
		private bool m_shiftAxisLocked;
		private bool m_shiftAxisHorizontal;
		private int m_prevEndX;
		private int m_prevEndY;
		private bool m_hasPrevEnd;

		public BrushFamilyTool()
		{
			m_engine = new BrushEngine();
		}

		protected abstract void BeginStroke(Document document, Layer layer, ToolState state);

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			BeginStroke(document, layer, state);
			m_engine.SetPressure(state.PenPressure(), state.PressureSizeEnabled(), state.PressureOpacityEnabled(), state.PressureMinimumSize(), state.PressureMinimumOpacity());
			m_engine.SetTipShape(state.BrushRoundness(), state.BrushAngle());
			m_shiftAxisLocked = false;
			if (state.ShiftHeld() && m_hasPrevEnd)
			{
				// Shift+click paints a straight line from where the last stroke ended.
				m_engine.StampFirst(document, layer, m_prevEndX, m_prevEndY, document.Selection());
				m_engine.StrokeTo(document, layer, x, y, document.Selection());
			}
			else
			{
				m_engine.StampFirst(document, layer, x, y, document.Selection());
			}
			m_strokeStartX = x;
			m_strokeStartY = y;
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			if (!m_engine.IsActive())
			{
				return false;
			}
			m_engine.SetPressure(state.PenPressure(), state.PressureSizeEnabled(), state.PressureOpacityEnabled(), state.PressureMinimumSize(), state.PressureMinimumOpacity());
			int pointX = x;
			int pointY = y;
			if (state.ShiftHeld())
			{
				ConstrainToAxis(ref pointX, ref pointY);
			}
			else
			{
				m_shiftAxisLocked = false;
			}
			if (m_hasLast)
			{
				m_engine.StrokeTo(document, layer, pointX, pointY, document.Selection());
			}
			else
			{
				m_engine.StampFirst(document, layer, pointX, pointY, document.Selection());
			}
			m_lastX = pointX;
			m_lastY = pointY;
			m_hasLast = true;
			return true;
		}

		public void AirbrushStamp(Document document, int x, int y, ToolState state)
		{
			if (!m_engine.IsActive())
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			m_engine.AirbrushStamp(document, layer, x, y, document.Selection());
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_engine.End();
			m_prevEndX = m_lastX;
			m_prevEndY = m_lastY;
			m_hasPrevEnd = m_hasLast;
			m_shiftAxisLocked = false;
			base.OnReleased(document, x, y, state);
		}

		// Locks the stroke to the axis (horizontal or vertical) it first moved along,
		// measured from the stroke's start point — Shift = straight up/down/left/right.
		private void ConstrainToAxis(ref int x, ref int y)
		{
			if (!m_shiftAxisLocked)
			{
				int deltaX = x - m_strokeStartX;
				int deltaY = y - m_strokeStartY;
				int absoluteX = (deltaX < 0) ? -deltaX : deltaX;
				int absoluteY = (deltaY < 0) ? -deltaY : deltaY;
				if (absoluteX == 0 && absoluteY == 0)
				{
					return;
				}
				m_shiftAxisHorizontal = absoluteX >= absoluteY;
				m_shiftAxisLocked = true;
			}
			if (m_shiftAxisHorizontal)
			{
				y = m_strokeStartY;
			}
			else
			{
				x = m_strokeStartX;
			}
		}

		public override void Reset()
		{
			base.Reset();
			m_shiftAxisLocked = false;
			m_hasPrevEnd = false;
		}
	}
}
