using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class BrushTool : BrushFamilyTool
	{
		protected override void BeginStroke(Document document, Layer layer, ToolState state)
		{
			int radius = state.BrushSize() / 2;
			SKColor color = state.Foreground();
			if (document.PaintTarget() == ePaintTarget.Mask && layer.HasMask())
			{
				int gray = ((state.Foreground().Red * 77) + (state.Foreground().Green * 150) + (state.Foreground().Blue * 29)) / 256;
				color = new SKColor((byte)gray, (byte)gray, (byte)gray, 255);
			}
			m_engine.Begin(layer, document.StrokeSnapshot(), radius, state.BrushHardness() / 100.0, state.BrushOpacity() / 100.0, state.BrushFlow() / 100.0, state.BrushSquareTip(), state.BrushSpacing() / 100.0, state.BrushSmoothing() / 100.0, eBrushOp.Paint, state.BrushMode(), color);
		}
	}
}
