using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class CloneTool : BrushFamilyTool
	{
		private int m_sourceX;
		private int m_sourceY;
		private bool m_hasSource;
		private int m_offsetX;
		private int m_offsetY;
		private bool m_hasOffset;

		public bool HasSource()
		{
			return m_hasSource;
		}

		public int SourceX()
		{
			return m_sourceX;
		}

		public int SourceY()
		{
			return m_sourceY;
		}

		public bool HasOffset()
		{
			return m_hasOffset;
		}

		public int SourceOffsetX()
		{
			return m_offsetX;
		}

		public int SourceOffsetY()
		{
			return m_offsetY;
		}

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
				m_hasOffset = false;
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
			int offsetX;
			int offsetY;
			if (state.CloneAligned() && m_hasOffset)
			{
				offsetX = m_offsetX;
				offsetY = m_offsetY;
			}
			else
			{
				offsetX = x - m_sourceX;
				offsetY = y - m_sourceY;
				m_offsetX = offsetX;
				m_offsetY = offsetY;
				m_hasOffset = true;
			}
			BeginStroke(document, layer, state);
			m_engine.SetTipShape(state.BrushRoundness(), state.BrushAngle());
			m_engine.SetCloneOffset(offsetX, offsetY);
			m_engine.StampFirst(document, layer, x, y, document.Selection());
			m_lastX = x;
			m_lastY = y;
			m_hasLast = true;
			return true;
		}
	}
}
