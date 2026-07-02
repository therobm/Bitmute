using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class EraserTool : Tool
	{
		private static readonly SKColor s_clear = new SKColor(0, 0, 0, 0);

		private static SKColor EraseColor(Layer layer, ToolState state)
		{
			if (layer.IsBackground())
			{
				SKColor background = state.Background();
				return new SKColor(background.Red, background.Green, background.Blue, 255);
			}
			return s_clear;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			int radius = state.BrushSize() / 2;
			DrawDab(layer, x, y, radius, EraseColor(layer, state), document.Selection());
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
			SKColor color = EraseColor(layer, state);
			if (m_hasLast)
			{
				StrokeLine(layer, m_lastX, m_lastY, x, y, radius, color, document.Selection());
				MarkStrokeDirty(document, m_lastX, m_lastY, x, y, radius);
			}
			else
			{
				DrawDab(layer, x, y, radius, color, document.Selection());
				MarkStrokeDirty(document, x, y, x, y, radius);
			}
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}
	}
}
