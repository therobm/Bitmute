using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class DodgeBurnTool : BrushFamilyTool
	{
		protected override void BeginStroke(Document document, Layer layer, ToolState state)
		{
			int radius = state.BrushSize() / 2;
			eBrushOp op = eBrushOp.Dodge;
			if (state.AltHeld())
			{
				op = eBrushOp.Burn;
			}
			SKColor unused = new SKColor(0, 0, 0, 255);
			m_engine.Begin(layer, document.StrokeSnapshot(), radius, state.BrushHardness() / 100.0, state.BrushOpacity() / 100.0, state.BrushFlow() / 100.0, state.BrushSquareTip(), state.BrushSpacing() / 100.0, state.BrushSmoothing() / 100.0, op, eBlendMode.Normal, unused);
		}
	}
}
