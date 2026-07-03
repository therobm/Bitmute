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

		private static float LineHeight(float size)
		{
			return size * 1.25f;
		}

		private static SKFont BuildFont(string fontFamily, bool bold, bool italic, float size)
		{
			SKTypeface typeface = Resolve(fontFamily, bold, italic);
			SKFont font = new SKFont(typeface, size);
			return font;
		}

		private static List<string> SplitLines(string text)
		{
			List<string> lines = new List<string>();
			string current = "";
			for (int index = 0; index < text.Length; index++)
			{
				char character = text[index];
				if (character == '\n' || character == '\r')
				{
					lines.Add(current);
					current = "";
				}
				else
				{
					current = current + character;
				}
			}
			lines.Add(current);
			return lines;
		}

		private static void LocateCaret(List<string> lines, int caretIndex, out int lineIndex, out int column)
		{
			if (caretIndex < 0)
			{
				caretIndex = 0;
			}
			int position = 0;
			for (int index = 0; index < lines.Count; index++)
			{
				int lineLength = lines[index].Length;
				if (caretIndex <= position + lineLength)
				{
					lineIndex = index;
					column = caretIndex - position;
					return;
				}
				position = position + lineLength + 1;
			}
			lineIndex = lines.Count - 1;
			column = lines[lineIndex].Length;
		}

		private static int LineStartFlat(List<string> lines, int lineIndex)
		{
			int position = 0;
			for (int index = 0; index < lineIndex; index++)
			{
				position = position + lines[index].Length + 1;
			}
			return position;
		}

		private static float ColumnX(SKFont font, string line, int column, float x, int alignment)
		{
			if (column < 0)
			{
				column = 0;
			}
			if (column > line.Length)
			{
				column = line.Length;
			}
			float lineWidth = font.MeasureText(line);
			float prefixWidth = font.MeasureText(line.Substring(0, column));
			float left = x;
			if (alignment == 1)
			{
				left = x - (lineWidth / 2.0f);
			}
			else if (alignment == 2)
			{
				left = x - lineWidth;
			}
			return left + prefixWidth;
		}

		public static void Draw(SKBitmap bitmap, string text, int x, int y, SKColor color, float size, string fontFamily, bool bold, bool italic, int alignment, int antiAlias)
		{
			if (text == null || text.Length == 0)
			{
				return;
			}
			SKCanvas canvas = new SKCanvas(bitmap);
			SKFont font = BuildFont(fontFamily, bold, italic, size);
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
			float lineHeight = LineHeight(size);
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

		public static void MeasureCaret(string text, int caretIndex, int x, int y, float size, string fontFamily, bool bold, bool italic, int alignment, out float caretX, out float caretY, out float caretHeight)
		{
			string safeText = text;
			if (safeText == null)
			{
				safeText = "";
			}
			SKFont font = BuildFont(fontFamily, bold, italic, size);
			List<string> lines = SplitLines(safeText);
			int lineIndex;
			int column;
			LocateCaret(lines, caretIndex, out lineIndex, out column);
			caretX = ColumnX(font, lines[lineIndex], column, x, alignment);
			caretY = y + (lineIndex * LineHeight(size));
			caretHeight = size;
			font.Dispose();
		}

		public static int CaretIndexAtPoint(string text, float documentX, float documentY, int x, int y, float size, string fontFamily, bool bold, bool italic, int alignment)
		{
			string safeText = text;
			if (safeText == null)
			{
				safeText = "";
			}
			SKFont font = BuildFont(fontFamily, bold, italic, size);
			List<string> lines = SplitLines(safeText);
			float lineHeight = LineHeight(size);
			int lineIndex = 0;
			if (lineHeight > 0.0f)
			{
				lineIndex = (int)System.Math.Floor((documentY - y) / lineHeight);
			}
			if (lineIndex < 0)
			{
				lineIndex = 0;
			}
			if (lineIndex > lines.Count - 1)
			{
				lineIndex = lines.Count - 1;
			}
			string line = lines[lineIndex];
			int bestColumn = 0;
			float bestDistance = -1.0f;
			for (int column = 0; column <= line.Length; column++)
			{
				float columnX = ColumnX(font, line, column, x, alignment);
				float distance = columnX - documentX;
				if (distance < 0.0f)
				{
					distance = -distance;
				}
				if (bestDistance < 0.0f || distance < bestDistance)
				{
					bestDistance = distance;
					bestColumn = column;
				}
			}
			font.Dispose();
			return LineStartFlat(lines, lineIndex) + bestColumn;
		}

		public static List<SKRect> MeasureSelectionRuns(string text, int start, int end, int x, int y, float size, string fontFamily, bool bold, bool italic, int alignment)
		{
			List<SKRect> runs = new List<SKRect>();
			string safeText = text;
			if (safeText == null)
			{
				safeText = "";
			}
			int low = start;
			int high = end;
			if (high < low)
			{
				int temp = low;
				low = high;
				high = temp;
			}
			if (high <= low)
			{
				return runs;
			}
			SKFont font = BuildFont(fontFamily, bold, italic, size);
			List<string> lines = SplitLines(safeText);
			float lineHeight = LineHeight(size);
			for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
			{
				string line = lines[lineIndex];
				int lineStart = LineStartFlat(lines, lineIndex);
				int lineEnd = lineStart + line.Length;
				int runStart = low;
				if (runStart < lineStart)
				{
					runStart = lineStart;
				}
				int runEnd = high;
				if (runEnd > lineEnd)
				{
					runEnd = lineEnd;
				}
				if (runEnd <= runStart)
				{
					continue;
				}
				float leftX = ColumnX(font, line, runStart - lineStart, x, alignment);
				float rightX = ColumnX(font, line, runEnd - lineStart, x, alignment);
				float top = y + (lineIndex * lineHeight);
				runs.Add(new SKRect(leftX, top, rightX, top + size));
			}
			font.Dispose();
			return runs;
		}
	}
}
