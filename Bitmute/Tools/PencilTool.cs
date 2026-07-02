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
			DrawDab(layer, x, y, 0, state.Foreground(), document.Selection());
			MarkStrokeDirty(document, x, y, x, y, 0);
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
			if (m_hasLast)
			{
				StrokeLine(layer, m_lastX, m_lastY, x, y, 0, state.Foreground(), document.Selection());
				MarkStrokeDirty(document, m_lastX, m_lastY, x, y, 0);
			}
			else
			{
				DrawDab(layer, x, y, 0, state.Foreground(), document.Selection());
				MarkStrokeDirty(document, x, y, x, y, 0);
			}
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}
	}
}
