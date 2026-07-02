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
			DrawDab(layer.Bitmap(), x, y, 0, state.Foreground(), document.Selection());
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
				StrokeLine(layer.Bitmap(), m_lastX, m_lastY, x, y, 0, state.Foreground(), document.Selection());
			}
			else
			{
				DrawDab(layer.Bitmap(), x, y, 0, state.Foreground(), document.Selection());
			}
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}
	}
}
