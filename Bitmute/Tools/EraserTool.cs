using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class EraserTool : BrushFamilyTool
	{
		protected override void BeginStroke(Document document, Layer layer, ToolState state)
		{
			int radius = state.BrushSize() / 2;
			bool background = layer.IsBackground();
			eBrushOp op = eBrushOp.Erase;
			SKColor color = new SKColor(0, 0, 0, 0);
			if (background)
			{
				op = eBrushOp.Paint;
				SKColor fill = state.Background();
				color = new SKColor(fill.Red, fill.Green, fill.Blue, 255);
			}
			m_engine.Begin(layer, document.StrokeSnapshot(), radius, state.BrushHardness() / 100.0, state.BrushOpacity() / 100.0, state.BrushFlow() / 100.0, state.BrushSquareTip(), state.BrushSpacing() / 100.0, state.BrushSmoothing() / 100.0, op, eBlendMode.Normal, color);
		}
	}
}
