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

		private static float EffectiveSize(Layer layer)
		{
			int verticalScale = layer.TextVerticalScale();
			if (verticalScale < 1)
			{
				verticalScale = 1;
			}
			float size = layer.TextSize() * (verticalScale / 100.0f);
			if (size < 1.0f)
			{
				size = 1.0f;
			}
			return size;
		}

		private static SKFont BuildStyledFont(Layer layer)
		{
			int verticalScale = layer.TextVerticalScale();
			if (verticalScale < 1)
			{
				verticalScale = 1;
			}
			int horizontalScale = layer.TextHorizontalScale();
			if (horizontalScale < 1)
			{
				horizontalScale = 1;
			}
			SKTypeface typeface = Resolve(layer.TextFontFamily(), layer.TextBold(), layer.TextItalic());
			SKFont font = new SKFont(typeface, EffectiveSize(layer));
			font.ScaleX = (float)horizontalScale / (float)verticalScale;
			if (layer.TextFauxBold())
			{
				font.Embolden = true;
			}
			if (layer.TextFauxItalic())
			{
				font.SkewX = -0.25f;
			}
			return font;
		}

		private static float LineHeightFor(Layer layer)
		{
			if (!layer.TextLeadingAuto())
			{
				float leading = layer.TextLeading();
				if (leading < 1.0f)
				{
					leading = 1.0f;
				}
				return leading;
			}
			return layer.TextSize() * 1.25f;
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

		private static float PrefixAdvance(SKFont font, string line, int column, int tracking, bool kerningAuto)
		{
			if (column < 0)
			{
				column = 0;
			}
			if (column > line.Length)
			{
				column = line.Length;
			}
			float advance;
			if (kerningAuto)
			{
				advance = font.MeasureText(line.Substring(0, column));
			}
			else
			{
				advance = 0.0f;
				for (int index = 0; index < column; index++)
				{
					advance = advance + font.MeasureText(line.Substring(index, 1));
				}
			}
			return advance + (tracking * column);
		}

		private static float LineLeft(float x, float lineAdvance, int alignment)
		{
			if (alignment == 1)
			{
				return x - (lineAdvance / 2.0f);
			}
			if (alignment == 2)
			{
				return x - lineAdvance;
			}
			return x;
		}

		private static float ColumnX(SKFont font, string line, int column, float x, int alignment, int tracking, bool kerningAuto)
		{
			float lineAdvance = PrefixAdvance(font, line, line.Length, tracking, kerningAuto);
			float left = LineLeft(x, lineAdvance, alignment);
			return left + PrefixAdvance(font, line, column, tracking, kerningAuto);
		}

		public static void Draw(Layer layer)
		{
			string text = layer.Text();
			if (text == null || text.Length == 0)
			{
				return;
			}
			SKFont font = BuildStyledFont(layer);
			font.Edging = Edging(layer.TextAntiAlias());
			SKCanvas canvas = new SKCanvas(layer.Bitmap());
			SKPaint paint = new SKPaint();
			paint.Color = layer.TextColor();
			paint.IsAntialias = layer.TextAntiAlias() != 0;
			int tracking = layer.TextTracking();
			bool kerningAuto = layer.TextKerningAuto();
			int alignment = layer.TextAlign();
			float x = layer.TextX() - layer.OffsetX();
			float y = layer.TextY() - layer.OffsetY();
			float lineHeight = LineHeightFor(layer);
			float baseline = y + layer.TextSize() - layer.TextBaselineShift();
			List<string> lines = SplitLines(text);
			for (int index = 0; index < lines.Count; index++)
			{
				string line = lines[index];
				float lineAdvance = PrefixAdvance(font, line, line.Length, tracking, kerningAuto);
				float left = LineLeft(x, lineAdvance, alignment);
				if (tracking == 0 && kerningAuto)
				{
					canvas.DrawText(line, left, baseline, SKTextAlign.Left, font, paint);
				}
				else
				{
					for (int column = 0; column < line.Length; column++)
					{
						float charX = left + PrefixAdvance(font, line, column, tracking, kerningAuto);
						canvas.DrawText(line.Substring(column, 1), charX, baseline, SKTextAlign.Left, font, paint);
					}
				}
				baseline = baseline + lineHeight;
			}
			paint.Dispose();
			font.Dispose();
			canvas.Dispose();
		}

		public static void MeasureCaret(Layer layer, int caretIndex, out float caretX, out float caretY, out float caretHeight)
		{
			string text = layer.Text();
			if (text == null)
			{
				text = "";
			}
			SKFont font = BuildStyledFont(layer);
			List<string> lines = SplitLines(text);
			int lineIndex;
			int column;
			LocateCaret(lines, caretIndex, out lineIndex, out column);
			caretX = ColumnX(font, lines[lineIndex], column, layer.TextX(), layer.TextAlign(), layer.TextTracking(), layer.TextKerningAuto());
			caretY = layer.TextY() + (lineIndex * LineHeightFor(layer)) - layer.TextBaselineShift();
			caretHeight = EffectiveSize(layer);
			font.Dispose();
		}

		public static int CaretIndexAtPoint(Layer layer, float documentX, float documentY)
		{
			string text = layer.Text();
			if (text == null)
			{
				text = "";
			}
			SKFont font = BuildStyledFont(layer);
			List<string> lines = SplitLines(text);
			float lineHeight = LineHeightFor(layer);
			int lineIndex = 0;
			if (lineHeight > 0.0f)
			{
				lineIndex = (int)System.Math.Floor((documentY - layer.TextY()) / lineHeight);
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
			int tracking = layer.TextTracking();
			bool kerningAuto = layer.TextKerningAuto();
			int alignment = layer.TextAlign();
			int bestColumn = 0;
			float bestDistance = -1.0f;
			for (int column = 0; column <= line.Length; column++)
			{
				float columnX = ColumnX(font, line, column, layer.TextX(), alignment, tracking, kerningAuto);
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

		public static List<SKRect> MeasureSelectionRuns(Layer layer, int start, int end)
		{
			List<SKRect> runs = new List<SKRect>();
			string text = layer.Text();
			if (text == null)
			{
				text = "";
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
			SKFont font = BuildStyledFont(layer);
			List<string> lines = SplitLines(text);
			float lineHeight = LineHeightFor(layer);
			int tracking = layer.TextTracking();
			bool kerningAuto = layer.TextKerningAuto();
			int alignment = layer.TextAlign();
			float glyphHeight = EffectiveSize(layer);
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
				float leftX = ColumnX(font, line, runStart - lineStart, layer.TextX(), alignment, tracking, kerningAuto);
				float rightX = ColumnX(font, line, runEnd - lineStart, layer.TextX(), alignment, tracking, kerningAuto);
				float top = layer.TextY() + (lineIndex * lineHeight) - layer.TextBaselineShift();
				runs.Add(new SKRect(leftX, top, rightX, top + glyphHeight));
			}
			font.Dispose();
			return runs;
		}
	}
}
