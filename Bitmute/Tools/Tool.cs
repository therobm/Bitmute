using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public abstract class Tool
	{
		protected int m_lastX;
		protected int m_lastY;
		protected bool m_hasLast;

		protected unsafe void DrawDab(Layer layer, int centerX, int centerY, int radius, SKColor color, Selection selection)
		{
			SKBitmap bitmap = layer.Bitmap();
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			byte* pixels = (byte*)bitmap.GetPixels().ToPointer();
			byte red = color.Red;
			byte green = color.Green;
			byte blue = color.Blue;
			byte alpha = color.Alpha;
			bool clip = selection != null && selection.IsActive();
			int radiusSquared = radius * radius;
			for (int offsetY = -radius; offsetY <= radius; offsetY++)
			{
				int canvasY = centerY + offsetY;
				int bitmapY = canvasY - layerOffsetY;
				if (bitmapY < 0 || bitmapY >= height)
				{
					continue;
				}
				int offsetYSquared = offsetY * offsetY;
				byte* rowStart = pixels + (bitmapY * rowBytes);
				for (int offsetX = -radius; offsetX <= radius; offsetX++)
				{
					if ((offsetX * offsetX) + offsetYSquared > radiusSquared)
					{
						continue;
					}
					int canvasX = centerX + offsetX;
					if (clip && !selection.IsSelected(canvasX, canvasY))
					{
						continue;
					}
					int bitmapX = canvasX - layerOffsetX;
					if (bitmapX < 0 || bitmapX >= width)
					{
						continue;
					}
					byte* pixel = rowStart + (bitmapX * 4);
					pixel[0] = red;
					pixel[1] = green;
					pixel[2] = blue;
					pixel[3] = alpha;
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

		protected void MarkStrokeDirty(Document document, int startX, int startY, int endX, int endY, int radius)
		{
			int left = startX;
			if (endX < left)
			{
				left = endX;
			}
			int right = startX;
			if (endX > right)
			{
				right = endX;
			}
			int top = startY;
			if (endY < top)
			{
				top = endY;
			}
			int bottom = startY;
			if (endY > bottom)
			{
				bottom = endY;
			}
			int pad = radius + 1;
			SKRectI rect = new SKRectI(left - pad, top - pad, right + pad + 1, bottom + pad + 1);
			document.MarkComposeDirtyRegion(rect);
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
