using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class EraserTool : Tool
	{
		private BrushEngine m_engine;

		public EraserTool()
		{
			m_engine = new BrushEngine();
		}

		private void BeginStroke(Document document, Layer layer, ToolState state)
		{
			int radius = state.BrushSize() / 2;
			bool background = layer.IsBackground();
			bool erase = !background;
			SKColor color = new SKColor(0, 0, 0, 0);
			if (background)
			{
				SKColor fill = state.Background();
				color = new SKColor(fill.Red, fill.Green, fill.Blue, 255);
			}
			m_engine.Begin(layer, document.StrokeSnapshot(), radius, state.BrushHardness() / 100.0, state.BrushOpacity() / 100.0, state.BrushFlow() / 100.0, state.BrushSquareTip(), state.BrushSpacing() / 100.0, state.BrushSmoothing() / 100.0, erase, eBlendMode.Normal, color);
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			BeginStroke(document, layer, state);
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

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_engine.End();
			base.OnReleased(document, x, y, state);
		}
	}
}
