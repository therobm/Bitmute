using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class EraserTool : Tool
	{
		private static readonly SKColor s_clear = new SKColor(0, 0, 0, 0);

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			int radius = state.BrushSize() / 2;
			DrawDab(layer.Bitmap(), x, y, radius, s_clear, document.Selection());
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
				StrokeLine(layer.Bitmap(), m_lastX, m_lastY, x, y, radius, s_clear, document.Selection());
			}
			else
			{
				DrawDab(layer.Bitmap(), x, y, radius, s_clear, document.Selection());
			}
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}
	}
}
