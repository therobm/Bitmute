using System;
using SkiaSharp;

namespace Bitmute.UI
{
	public static class GridOverlay
	{
		private static void DrawLines(SKCanvas canvas, SKPaint paint, float offsetX, float offsetY, float zoom, int documentWidth, int documentHeight, int step, SKRect bounds, SKRect visible)
		{
			int firstX = (int)Math.Floor(((visible.Left - 1.0f) - offsetX) / zoom / step);
			if (firstX < 1)
			{
				firstX = 1;
			}
			int lastX = (int)Math.Ceiling(((visible.Right + 1.0f) - offsetX) / zoom / step);
			for (int index = firstX; index <= lastX; index++)
			{
				int x = index * step;
				if (x >= documentWidth)
				{
					break;
				}
				float screen = (float)Math.Floor(offsetX + (x * zoom)) + 0.5f;
				canvas.DrawLine(screen, bounds.Top, screen, bounds.Bottom, paint);
			}
			int firstY = (int)Math.Floor(((visible.Top - 1.0f) - offsetY) / zoom / step);
			if (firstY < 1)
			{
				firstY = 1;
			}
			int lastY = (int)Math.Ceiling(((visible.Bottom + 1.0f) - offsetY) / zoom / step);
			for (int index = firstY; index <= lastY; index++)
			{
				int y = index * step;
				if (y >= documentHeight)
				{
					break;
				}
				float screen = (float)Math.Floor(offsetY + (y * zoom)) + 0.5f;
				canvas.DrawLine(bounds.Left, screen, bounds.Right, screen, paint);
			}
		}

		public static void Draw(SKCanvas canvas, float offsetX, float offsetY, float zoom, int documentWidth, int documentHeight, int cellSize, bool pixelGrid)
		{
			if (zoom <= 0.0f)
			{
				return;
			}
			bool drawCells = cellSize > 0 && (cellSize * zoom) >= 4.0f;
			bool drawPixels = pixelGrid && zoom >= 8.0f;
			if (!drawCells && !drawPixels)
			{
				return;
			}
			SKRect bounds = new SKRect(offsetX, offsetY, offsetX + (documentWidth * zoom), offsetY + (documentHeight * zoom));
			canvas.Save();
			canvas.ClipRect(bounds);
			SKRect visible = canvas.LocalClipBounds;
			if (drawPixels)
			{
				SKPaint pixelPaint = new SKPaint();
				pixelPaint.Color = new SKColor(0x88, 0x88, 0x88, 0x33);
				pixelPaint.StrokeWidth = 1.0f;
				pixelPaint.IsAntialias = false;
				DrawLines(canvas, pixelPaint, offsetX, offsetY, zoom, documentWidth, documentHeight, 1, bounds, visible);
				pixelPaint.Dispose();
			}
			if (drawCells)
			{
				SKPaint cellPaint = new SKPaint();
				cellPaint.Color = new SKColor(0x88, 0x88, 0x88, 0x66);
				cellPaint.StrokeWidth = 1.0f;
				cellPaint.IsAntialias = false;
				DrawLines(canvas, cellPaint, offsetX, offsetY, zoom, documentWidth, documentHeight, cellSize, bounds, visible);
				cellPaint.Dispose();
			}
			canvas.Restore();
		}
	}
}
