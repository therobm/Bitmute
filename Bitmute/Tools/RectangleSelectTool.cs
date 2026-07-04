using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class RectangleSelectTool : Tool
	{
		private const int MinimumSpan = 3;

		private int m_startX;
		private int m_startY;

		public override bool IsDestructive()
		{
			return false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			m_startX = x;
			m_startY = y;
			eSelectionMode mode = SelectionModeFromState(state);
			if (mode == eSelectionMode.Replace)
			{
				document.Selection().Clear();
			}
			document.Selection().BeginOperation(mode, state.SelectionFeather());
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			int left = m_startX;
			int right = x;
			if (right < left)
			{
				int swap = left;
				left = right;
				right = swap;
			}
			int top = m_startY;
			int bottom = y;
			if (bottom < top)
			{
				int swap = top;
				top = bottom;
				bottom = swap;
			}
			int spanX = (right + 1) - left;
			int spanY = (bottom + 1) - top;
			if (spanX < MinimumSpan || spanY < MinimumSpan)
			{
				document.Selection().ApplyRect(SKRectI.Empty);
				return false;
			}
			SKRectI rect = new SKRectI(left, top, right + 1, bottom + 1);
			document.Selection().ApplyRect(rect);
			return false;
		}
	}
}
