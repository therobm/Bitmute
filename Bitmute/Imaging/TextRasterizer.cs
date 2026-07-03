using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class TextRasterizer
	{
		public static void Draw(SKBitmap bitmap, string text, int x, int y, SKColor color, float size, string fontFamily, bool bold, bool italic)
		{
			SKFontStyleWeight weight = SKFontStyleWeight.Normal;
			if (bold)
			{
				weight = SKFontStyleWeight.Bold;
			}
			SKFontStyleSlant slant = SKFontStyleSlant.Upright;
			if (italic)
			{
				slant = SKFontStyleSlant.Italic;
			}
			SKFontStyle fontStyle = new SKFontStyle(weight, SKFontStyleWidth.Normal, slant);
			SKTypeface typeface = SKFontManager.Default.MatchFamily(fontFamily, fontStyle);
			if (typeface == null)
			{
				typeface = SKTypeface.Default;
			}
			SKCanvas canvas = new SKCanvas(bitmap);
			SKFont font = new SKFont(typeface, size);
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
