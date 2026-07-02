using Bitmute.Imaging;

namespace Bitmute.Tools
{
	public class BrushTool : Tool
	{
		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			int radius = state.BrushSize() / 2;
			DrawDab(layer.Bitmap(), x, y, radius, state.Foreground());
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
				StrokeLine(layer.Bitmap(), m_lastX, m_lastY, x, y, radius, state.Foreground());
			}
			else
			{
				DrawDab(layer.Bitmap(), x, y, radius, state.Foreground());
			}
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}
	}
}
