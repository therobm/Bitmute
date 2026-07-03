using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class TextRasterizer
	{
		private static SKTypeface Resolve(string fontFamily, bool bold, bool italic)
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
			return typeface;
		}

		private static SKFontEdging Edging(int antiAlias)
		{
			if (antiAlias == 0)
			{
				return SKFontEdging.Alias;
			}
			if (antiAlias == 1 || antiAlias == 2)
			{
				return SKFontEdging.Antialias;
			}
			return SKFontEdging.SubpixelAntialias;
		}

		private static List<string> SplitLines(string text)
		{
			List<string> lines = new List<string>();
			string current = "";
			for (int index = 0; index < text.Length; index++)
			{
				char character = text[index];
				if (character == '\n')
				{
					lines.Add(current);
					current = "";
				}
				else if (character != '\r')
				{
					current = current + character;
				}
			}
			lines.Add(current);
			return lines;
		}

		public static void Draw(SKBitmap bitmap, string text, int x, int y, SKColor color, float size, string fontFamily, bool bold, bool italic, int alignment, int antiAlias)
		{
			if (text == null || text.Length == 0)
			{
				return;
			}
			SKTypeface typeface = Resolve(fontFamily, bold, italic);
			SKCanvas canvas = new SKCanvas(bitmap);
			SKFont font = new SKFont(typeface, size);
			font.Edging = Edging(antiAlias);
			SKPaint paint = new SKPaint();
			paint.Color = color;
			paint.IsAntialias = antiAlias != 0;
			SKTextAlign align = SKTextAlign.Left;
			if (alignment == 1)
			{
				align = SKTextAlign.Center;
			}
			else if (alignment == 2)
			{
				align = SKTextAlign.Right;
			}
			float lineHeight = size * 1.25f;
			List<string> lines = SplitLines(text);
			float baseline = y + size;
			for (int index = 0; index < lines.Count; index++)
			{
				canvas.DrawText(lines[index], x, baseline, align, font, paint);
				baseline = baseline + lineHeight;
			}
			paint.Dispose();
			font.Dispose();
			canvas.Dispose();
		}
	}
}
