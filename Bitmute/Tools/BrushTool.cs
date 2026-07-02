using Bitmute.Imaging;

namespace Bitmute.Tools
{
	public class BrushTool : Tool
	{
		private BrushEngine m_engine;

		public BrushTool()
		{
			m_engine = new BrushEngine();
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			int radius = state.BrushSize() / 2;
			m_engine.Begin(layer, document.StrokeSnapshot(), radius, state.BrushHardness() / 100.0, state.BrushOpacity() / 100.0, state.BrushFlow() / 100.0, state.BrushSquareTip(), state.BrushSpacing() / 100.0, state.Foreground());
			m_engine.StampFirst(layer, x, y, document.Selection());
			MarkStrokeDirty(document, x, y, x, y, radius);
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
			int radius = state.BrushSize() / 2;
			if (m_hasLast)
			{
				m_engine.StrokeTo(layer, x, y, document.Selection());
				MarkStrokeDirty(document, m_lastX, m_lastY, x, y, radius);
			}
			else
			{
				m_engine.StampFirst(layer, x, y, document.Selection());
				MarkStrokeDirty(document, x, y, x, y, radius);
			}
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_engine.End();
			base.OnReleased(document, x, y, state);
		}
	}
}
