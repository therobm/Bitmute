using Bitmute.Imaging;

namespace Bitmute.Tools
{
	public class PencilTool : Tool
	{
		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			int radius = state.BrushSize() / 2;
			DrawDab(layer, x, y, radius, state.Foreground(), document.Selection());
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
			int radius = state.BrushSize() / 2;
			if (m_hasLast)
			{
				StrokeLine(layer, m_lastX, m_lastY, x, y, radius, state.Foreground(), document.Selection());
				MarkStrokeDirty(document, m_lastX, m_lastY, x, y, radius);
			}
			else
			{
				DrawDab(layer, x, y, radius, state.Foreground(), document.Selection());
				MarkStrokeDirty(document, x, y, x, y, radius);
			}
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}
	}
}
