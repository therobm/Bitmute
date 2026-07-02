using Bitmute.Imaging;

namespace Bitmute.Tools
{
	public class BrushTool : BrushFamilyTool
	{
		protected override void BeginStroke(Document document, Layer layer, ToolState state)
		{
			int radius = state.BrushSize() / 2;
			m_engine.Begin(layer, document.StrokeSnapshot(), radius, state.BrushHardness() / 100.0, state.BrushOpacity() / 100.0, state.BrushFlow() / 100.0, state.BrushSquareTip(), state.BrushSpacing() / 100.0, state.BrushSmoothing() / 100.0, eBrushOp.Paint, state.BrushMode(), state.Foreground());
		}
	}
}
