using Bitmute.Imaging;

namespace Bitmute.Tools
{
	public abstract class BrushFamilyTool : Tool
	{
		protected BrushEngine m_engine;

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
			m_engine.SetPressure(state.PenPressure(), state.PressureSizeEnabled(), state.PressureOpacityEnabled());
			m_engine.SetTipShape(state.BrushRoundness(), state.BrushAngle());
			m_engine.StampFirst(document, layer, x, y, document.Selection());
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
			m_engine.SetPressure(state.PenPressure(), state.PressureSizeEnabled(), state.PressureOpacityEnabled());
			if (m_hasLast)
			{
				m_engine.StrokeTo(document, layer, x, y, document.Selection());
			}
			else
			{
				m_engine.StampFirst(document, layer, x, y, document.Selection());
			}
			m_lastX = x;
			m_lastY = y;
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
			base.OnReleased(document, x, y, state);
		}
	}
}
