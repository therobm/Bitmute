using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class RectangleSelectTool : Tool
	{
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
			document.Selection().Clear();
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
			SKRectI rect = new SKRectI(left, top, right + 1, bottom + 1);
			document.Selection().SelectRect(rect);
			return false;
		}
	}
}
