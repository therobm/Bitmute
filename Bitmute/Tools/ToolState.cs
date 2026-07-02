using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class ToolState
	{
		private eTool m_tool;
		private SKColor m_foreground;
		private SKColor m_background;
		private int m_brushSize;
		private int m_brushHardness;
		private int m_brushOpacity;
		private int m_brushFlow;
		private int m_brushSpacing;
		private bool m_brushSquareTip;
		private int m_brushSmoothing;
		private eBlendMode m_brushMode;
		private int m_fillTolerance;
		private bool m_lineAntiAlias;
		private bool m_shiftHeld;
		private bool m_altHeld;

		public ToolState()
		{
			m_tool = eTool.Brush;
			m_foreground = new SKColor(0, 0, 0, 255);
			m_background = new SKColor(255, 255, 255, 255);
			m_brushSize = 6;
			m_brushHardness = 100;
			m_brushOpacity = 100;
			m_brushFlow = 100;
			m_brushSpacing = 25;
			m_brushSquareTip = false;
			m_brushSmoothing = 0;
			m_brushMode = eBlendMode.Normal;
			m_fillTolerance = 0;
			m_lineAntiAlias = false;
			m_shiftHeld = false;
			m_altHeld = false;
		}

		public bool LineAntiAlias()
		{
			return m_lineAntiAlias;
		}

		public void SetLineAntiAlias(bool antiAlias)
		{
			m_lineAntiAlias = antiAlias;
		}

		public bool ShiftHeld()
		{
			return m_shiftHeld;
		}

		public void SetShiftHeld(bool shiftHeld)
		{
			m_shiftHeld = shiftHeld;
		}

		public bool AltHeld()
		{
			return m_altHeld;
		}

		public void SetAltHeld(bool altHeld)
		{
			m_altHeld = altHeld;
		}

		public eTool Tool()
		{
			return m_tool;
		}

		public void SetTool(eTool tool)
		{
			m_tool = tool;
		}

		public SKColor Foreground()
		{
			return m_foreground;
		}

		public void SetForeground(SKColor color)
		{
			m_foreground = color;
		}

		public SKColor Background()
		{
			return m_background;
		}

		public void SetBackground(SKColor color)
		{
			m_background = color;
		}

		public int BrushSize()
		{
			return m_brushSize;
		}

		public void SetBrushSize(int size)
		{
			if (size < 1)
			{
				size = 1;
			}
			m_brushSize = size;
		}

		public int BrushHardness()
		{
			return m_brushHardness;
		}

		public void SetBrushHardness(int hardness)
		{
			if (hardness < 0)
			{
				hardness = 0;
			}
			if (hardness > 100)
			{
				hardness = 100;
			}
			m_brushHardness = hardness;
		}

		public int BrushOpacity()
		{
			return m_brushOpacity;
		}

		public void SetBrushOpacity(int opacity)
		{
			if (opacity < 1)
			{
				opacity = 1;
			}
			if (opacity > 100)
			{
				opacity = 100;
			}
			m_brushOpacity = opacity;
		}

		public int BrushFlow()
		{
			return m_brushFlow;
		}

		public void SetBrushFlow(int flow)
		{
			if (flow < 1)
			{
				flow = 1;
			}
			if (flow > 100)
			{
				flow = 100;
			}
			m_brushFlow = flow;
		}

		public int BrushSpacing()
		{
			return m_brushSpacing;
		}

		public void SetBrushSpacing(int spacing)
		{
			if (spacing < 1)
			{
				spacing = 1;
			}
			if (spacing > 100)
			{
				spacing = 100;
			}
			m_brushSpacing = spacing;
		}

		public bool BrushSquareTip()
		{
			return m_brushSquareTip;
		}

		public void SetBrushSquareTip(bool square)
		{
			m_brushSquareTip = square;
		}

		public int BrushSmoothing()
		{
			return m_brushSmoothing;
		}

		public void SetBrushSmoothing(int smoothing)
		{
			if (smoothing < 0)
			{
				smoothing = 0;
			}
			if (smoothing > 100)
			{
				smoothing = 100;
			}
			m_brushSmoothing = smoothing;
		}

		public eBlendMode BrushMode()
		{
			return m_brushMode;
		}

		public void SetBrushMode(eBlendMode mode)
		{
			m_brushMode = mode;
		}

		public int FillTolerance()
		{
			return m_fillTolerance;
		}

		public void SetFillTolerance(int tolerance)
		{
			m_fillTolerance = tolerance;
		}

		public void SwapColors()
		{
			SKColor temp = m_foreground;
			m_foreground = m_background;
			m_background = temp;
		}

		public void ResetColors()
		{
			m_foreground = new SKColor(0, 0, 0, 255);
			m_background = new SKColor(255, 255, 255, 255);
		}
	}
}
