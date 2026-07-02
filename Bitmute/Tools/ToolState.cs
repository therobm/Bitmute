using SkiaSharp;

namespace Bitmute.Tools
{
	public class ToolState
	{
		private eTool m_tool;
		private SKColor m_foreground;
		private SKColor m_background;
		private int m_brushSize;
		private int m_fillTolerance;
		private bool m_lineAntiAlias;
		private bool m_shiftHeld;

		public ToolState()
		{
			m_tool = eTool.Brush;
			m_foreground = new SKColor(0, 0, 0, 255);
			m_background = new SKColor(255, 255, 255, 255);
			m_brushSize = 6;
			m_fillTolerance = 0;
			m_lineAntiAlias = false;
			m_shiftHeld = false;
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
