using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class EllipseSelectTool : Tool
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
			document.Selection().BeginOperation(mode);
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
			right = right + 1;
			bottom = bottom + 1;
			int spanX = right - left;
			int spanY = bottom - top;
			if (spanX < MinimumSpan || spanY < MinimumSpan)
			{
				document.Selection().ApplyRect(SKRectI.Empty);
				return false;
			}

			int documentWidth = document.Width();
			int documentHeight = document.Height();
			int clampedLeft = left;
			if (clampedLeft < 0)
			{
				clampedLeft = 0;
			}
			int clampedTop = top;
			if (clampedTop < 0)
			{
				clampedTop = 0;
			}
			int clampedRight = right;
			if (clampedRight > documentWidth)
			{
				clampedRight = documentWidth;
			}
			int clampedBottom = bottom;
			if (clampedBottom > documentHeight)
			{
				clampedBottom = documentHeight;
			}
			if (clampedRight <= clampedLeft || clampedBottom <= clampedTop)
			{
				document.Selection().Clear();
				return false;
			}

			double centerX = (left + right) / 2.0;
			double centerY = (top + bottom) / 2.0;
			double radiusX = (right - left) / 2.0;
			double radiusY = (bottom - top) / 2.0;

			byte[] mask = new byte[documentWidth * documentHeight];
			for (int pixelY = clampedTop; pixelY < clampedBottom; pixelY++)
			{
				double normalizedY = ((pixelY + 0.5) - centerY) / radiusY;
				double normalizedYSquared = normalizedY * normalizedY;
				int rowStart = pixelY * documentWidth;
				for (int pixelX = clampedLeft; pixelX < clampedRight; pixelX++)
				{
					double normalizedX = ((pixelX + 0.5) - centerX) / radiusX;
					if ((normalizedX * normalizedX) + normalizedYSquared > 1.0)
					{
						continue;
					}
					mask[rowStart + pixelX] = 255;
				}
			}

			document.Selection().ApplyMask(mask);
			return false;
		}
	}
}
