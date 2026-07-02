using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public abstract class Tool
	{
		protected int m_lastX;
		protected int m_lastY;
		protected bool m_hasLast;

		protected void SetPixelClamped(SKBitmap bitmap, int x, int y, SKColor color)
		{
			if (x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height)
			{
				return;
			}
			bitmap.SetPixel(x, y, color);
		}

		protected void DrawDab(SKBitmap bitmap, int centerX, int centerY, int radius, SKColor color)
		{
			if (radius <= 0)
			{
				SetPixelClamped(bitmap, centerX, centerY, color);
				return;
			}
			int radiusSquared = radius * radius;
			for (int offsetY = -radius; offsetY <= radius; offsetY++)
			{
				for (int offsetX = -radius; offsetX <= radius; offsetX++)
				{
					if ((offsetX * offsetX) + (offsetY * offsetY) <= radiusSquared)
					{
						SetPixelClamped(bitmap, centerX + offsetX, centerY + offsetY, color);
					}
				}
			}
		}

		protected void StrokeLine(SKBitmap bitmap, int startX, int startY, int endX, int endY, int radius, SKColor color)
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
				DrawDab(bitmap, startX, startY, radius, color);
				return;
			}
			for (int step = 0; step <= steps; step++)
			{
				double fraction = (double)step / (double)steps;
				int pointX = startX + (int)System.Math.Round(deltaX * fraction);
				int pointY = startY + (int)System.Math.Round(deltaY * fraction);
				DrawDab(bitmap, pointX, pointY, radius, color);
			}
		}

		public abstract bool OnPressed(Document document, int x, int y, ToolState state);

		public abstract bool OnDragged(Document document, int x, int y, ToolState state);

		public virtual void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_hasLast = false;
		}
	}
}
