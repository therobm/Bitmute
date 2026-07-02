using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class TextRasterizer
	{
		public static void Draw(SKBitmap bitmap, string text, int x, int y, SKColor color, float size)
		{
			SKCanvas canvas = new SKCanvas(bitmap);
			SKFont font = new SKFont(SKTypeface.Default, size);
			SKPaint paint = new SKPaint();
			paint.Color = color;
			paint.IsAntialias = true;
			canvas.DrawText(text, x, y + size, SKTextAlign.Left, font, paint);
			paint.Dispose();
			font.Dispose();
			canvas.Dispose();
		}
	}
}
