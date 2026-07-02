using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public abstract class Tool
	{
		protected int m_lastX;
		protected int m_lastY;
		protected bool m_hasLast;

		protected void SetPixelClamped(Layer layer, int x, int y, SKColor color, Selection selection)
		{
			if (selection != null && selection.IsActive() && !selection.IsSelected(x, y))
			{
				return;
			}
			layer.SetPixelCanvas(x, y, color);
		}

		protected void DrawDab(Layer layer, int centerX, int centerY, int radius, SKColor color, Selection selection)
		{
			if (radius <= 0)
			{
				SetPixelClamped(layer, centerX, centerY, color, selection);
				return;
			}
			int radiusSquared = radius * radius;
			for (int offsetY = -radius; offsetY <= radius; offsetY++)
			{
				for (int offsetX = -radius; offsetX <= radius; offsetX++)
				{
					if ((offsetX * offsetX) + (offsetY * offsetY) <= radiusSquared)
					{
						SetPixelClamped(layer, centerX + offsetX, centerY + offsetY, color, selection);
					}
				}
			}
		}

		protected void StrokeLine(Layer layer, int startX, int startY, int endX, int endY, int radius, SKColor color, Selection selection)
		{
			int deltaX = endX - startX;
			int deltaY = endY - startY;
			int steps = System.Math.Abs(deltaX);
			int absDeltaY = System.Math.Abs(deltaY);
			if (absDeltaY > steps)
			{
				steps = absDeltaY;
			}
			if (steps <= 0)
			{
				DrawDab(layer, startX, startY, radius, color, selection);
				return;
			}
			for (int step = 0; step <= steps; step++)
			{
				double fraction = (double)step / (double)steps;
				int pointX = startX + (int)System.Math.Round(deltaX * fraction);
				int pointY = startY + (int)System.Math.Round(deltaY * fraction);
				DrawDab(layer, pointX, pointY, radius, color, selection);
			}
		}

		public virtual bool IsDestructive()
		{
			return true;
		}

		public abstract bool OnPressed(Document document, int x, int y, ToolState state);

		public abstract bool OnDragged(Document document, int x, int y, ToolState state);

		public virtual void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_hasLast = false;
		}
	}
}
