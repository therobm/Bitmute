using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class CloneTool : BrushFamilyTool
	{
		private int m_sourceX;
		private int m_sourceY;
		private bool m_hasSource;

		protected override void BeginStroke(Document document, Layer layer, ToolState state)
		{
			int radius = state.BrushSize() / 2;
			SKColor unused = new SKColor(0, 0, 0, 255);
			m_engine.Begin(layer, document.StrokeSnapshot(), radius, state.BrushHardness() / 100.0, state.BrushOpacity() / 100.0, state.BrushFlow() / 100.0, state.BrushSquareTip(), state.BrushSpacing() / 100.0, state.BrushSmoothing() / 100.0, eBrushOp.Clone, eBlendMode.Normal, unused);
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			if (state.AltHeld())
			{
				m_sourceX = x;
				m_sourceY = y;
				m_hasSource = true;
				return false;
			}
			if (!m_hasSource)
			{
				return false;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			BeginStroke(document, layer, state);
			m_engine.SetCloneOffset(x - m_sourceX, y - m_sourceY);
			m_engine.StampFirst(document, layer, x, y, document.Selection());
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}
	}
}
