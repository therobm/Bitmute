using SkiaSharp;

namespace Bitmute.Imaging
{
	public enum eBlendMode
	{
		Normal,
		Dissolve,
		Darken,
		Multiply,
		ColorBurn,
		LinearBurn,
		DarkerColor,
		Lighten,
		Screen,
		ColorDodge,
		Add,
		LighterColor,
		Overlay,
		SoftLight,
		HardLight,
		VividLight,
		LinearLight,
		PinLight,
		HardMix,
		Difference,
		Exclusion,
		Subtract,
		Divide,
		Hue,
		Saturation,
		Color,
		Luminosity
	}

	public class Layer
	{
		private string m_name;
		private bool m_visible;
		private byte m_opacity;
		private eBlendMode m_blendMode;
		private SKBitmap m_bitmap;
		private int m_offsetX;
		private int m_offsetY;
		private bool m_isBackground;
		private bool m_isText;
		private string m_text;
		private int m_textX;
		private int m_textY;
		private float m_textSize;
		private bool m_textBold;
		private bool m_textItalic;
		private string m_textFontFamily;
		private SKColor m_textColor;
		private int m_textAlign;
		private int m_textAntiAlias;
		private bool m_textLeadingAuto;
		private float m_textLeading;
		private int m_textTracking;
		private int m_textHorizontalScale;
		private int m_textVerticalScale;
		private int m_textBaselineShift;
		private bool m_textFauxBold;
		private bool m_textFauxItalic;
		private bool m_textKerningAuto;

		public static bool IsCustomBlend(eBlendMode blendMode)
		{
			if (blendMode == eBlendMode.Dissolve)
			{
				return true;
			}
			if (blendMode == eBlendMode.LinearBurn)
			{
				return true;
			}
			if (blendMode == eBlendMode.DarkerColor)
			{
				return true;
			}
			if (blendMode == eBlendMode.LighterColor)
			{
				return true;
			}
			if (blendMode == eBlendMode.VividLight)
			{
				return true;
			}
			if (blendMode == eBlendMode.LinearLight)
			{
				return true;
			}
			if (blendMode == eBlendMode.PinLight)
			{
				return true;
			}
			if (blendMode == eBlendMode.HardMix)
			{
				return true;
			}
			if (blendMode == eBlendMode.Subtract)
			{
				return true;
			}
			if (blendMode == eBlendMode.Divide)
			{
				return true;
			}
			return false;
		}

		public static SKBlendMode ToSkBlendMode(eBlendMode blendMode)
		{
			if (blendMode == eBlendMode.Darken)
			{
				return SKBlendMode.Darken;
			}
			if (blendMode == eBlendMode.Multiply)
			{
				return SKBlendMode.Multiply;
			}
			if (blendMode == eBlendMode.ColorBurn)
			{
				return SKBlendMode.ColorBurn;
			}
			if (blendMode == eBlendMode.Lighten)
			{
				return SKBlendMode.Lighten;
			}
			if (blendMode == eBlendMode.Screen)
			{
				return SKBlendMode.Screen;
			}
			if (blendMode == eBlendMode.ColorDodge)
			{
				return SKBlendMode.ColorDodge;
			}
			if (blendMode == eBlendMode.Add)
			{
				return SKBlendMode.Plus;
			}
			if (blendMode == eBlendMode.Overlay)
			{
				return SKBlendMode.Overlay;
			}
			if (blendMode == eBlendMode.SoftLight)
			{
				return SKBlendMode.SoftLight;
			}
			if (blendMode == eBlendMode.HardLight)
			{
				return SKBlendMode.HardLight;
			}
			if (blendMode == eBlendMode.Difference)
			{
				return SKBlendMode.Difference;
			}
			if (blendMode == eBlendMode.Exclusion)
			{
				return SKBlendMode.Exclusion;
			}
			if (blendMode == eBlendMode.Hue)
			{
				return SKBlendMode.Hue;
			}
			if (blendMode == eBlendMode.Saturation)
			{
				return SKBlendMode.Saturation;
			}
			if (blendMode == eBlendMode.Color)
			{
				return SKBlendMode.Color;
			}
			if (blendMode == eBlendMode.Luminosity)
			{
				return SKBlendMode.Luminosity;
			}
			return SKBlendMode.SrcOver;
		}

		public Layer(string name, int width, int height)
		{
			m_name = name;
			m_visible = true;
			m_opacity = 255;
			m_blendMode = eBlendMode.Normal;
			m_bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			m_bitmap.Erase(SKColors.Transparent);
			m_offsetX = 0;
			m_offsetY = 0;
			m_isBackground = false;
			m_isText = false;
			m_text = "";
			m_textX = 0;
			m_textY = 0;
			m_textSize = 32.0f;
			m_textBold = false;
			m_textItalic = false;
			m_textFontFamily = "Segoe UI";
			m_textColor = new SKColor(0, 0, 0, 255);
			m_textAlign = 0;
			m_textAntiAlias = 3;
			m_textLeadingAuto = true;
			m_textLeading = 0.0f;
			m_textTracking = 0;
			m_textHorizontalScale = 100;
			m_textVerticalScale = 100;
			m_textBaselineShift = 0;
			m_textFauxBold = false;
			m_textFauxItalic = false;
			m_textKerningAuto = true;
		}

		public bool TextLeadingAuto()
		{
			return m_textLeadingAuto;
		}

		public float TextLeading()
		{
			return m_textLeading;
		}

		public int TextTracking()
		{
			return m_textTracking;
		}

		public int TextHorizontalScale()
		{
			return m_textHorizontalScale;
		}

		public int TextVerticalScale()
		{
			return m_textVerticalScale;
		}

		public int TextBaselineShift()
		{
			return m_textBaselineShift;
		}

		public bool TextFauxBold()
		{
			return m_textFauxBold;
		}

		public bool TextFauxItalic()
		{
			return m_textFauxItalic;
		}

		public bool TextKerningAuto()
		{
			return m_textKerningAuto;
		}

		public void SetTextCharacter(bool leadingAuto, float leading, int tracking, int horizontalScale, int verticalScale, int baselineShift, bool fauxBold, bool fauxItalic, bool kerningAuto)
		{
			m_textLeadingAuto = leadingAuto;
			m_textLeading = leading;
			m_textTracking = tracking;
			m_textHorizontalScale = horizontalScale;
			m_textVerticalScale = verticalScale;
			m_textBaselineShift = baselineShift;
			m_textFauxBold = fauxBold;
			m_textFauxItalic = fauxItalic;
			m_textKerningAuto = kerningAuto;
		}

		public bool IsText()
		{
			return m_isText;
		}

		public string Text()
		{
			return m_text;
		}

		public int TextX()
		{
			return m_textX;
		}

		public int TextY()
		{
			return m_textY;
		}

		public float TextSize()
		{
			return m_textSize;
		}

		public bool TextBold()
		{
			return m_textBold;
		}

		public bool TextItalic()
		{
			return m_textItalic;
		}

		public string TextFontFamily()
		{
			return m_textFontFamily;
		}

		public SKColor TextColor()
		{
			return m_textColor;
		}

		public int TextAlign()
		{
			return m_textAlign;
		}

		public int TextAntiAlias()
		{
			return m_textAntiAlias;
		}

		public void SetTextPosition(int x, int y)
		{
			m_isText = true;
			m_textX = x;
			m_textY = y;
		}

		public void SetTextString(string text)
		{
			m_text = text;
		}

		public void SetTextStyle(float size, string fontFamily, bool bold, bool italic, SKColor color, int align, int antiAlias)
		{
			m_textSize = size;
			m_textFontFamily = fontFamily;
			m_textBold = bold;
			m_textItalic = italic;
			m_textColor = color;
			m_textAlign = align;
			m_textAntiAlias = antiAlias;
		}

		public void RasterizeText()
		{
			m_isText = false;
		}

		public void RenderText()
		{
			m_bitmap.Erase(SKColors.Transparent);
			if (!m_isText)
			{
				return;
			}
			TextRasterizer.Draw(this);
		}

		public bool IsBackground()
		{
			return m_isBackground;
		}

		public void SetIsBackground(bool isBackground)
		{
			m_isBackground = isBackground;
		}

		public SKBitmap Bitmap()
		{
			return m_bitmap;
		}

		public void SetBitmap(SKBitmap bitmap)
		{
			m_bitmap = bitmap;
		}

		public int OffsetX()
		{
			return m_offsetX;
		}

		public int OffsetY()
		{
			return m_offsetY;
		}

		public void SetOffset(int offsetX, int offsetY)
		{
			m_offsetX = offsetX;
			m_offsetY = offsetY;
		}

		public void ExpandToCover(int canvasWidth, int canvasHeight)
		{
			SKRectI contentLocal = PixelRegion.ComputeContentBounds(m_bitmap);
			int coverLeft;
			int coverTop;
			int coverRight;
			int coverBottom;
			if (contentLocal.Width <= 0 || contentLocal.Height <= 0)
			{
				coverLeft = 0;
				coverTop = 0;
				coverRight = canvasWidth;
				coverBottom = canvasHeight;
			}
			else
			{
				int contentLeft = m_offsetX + contentLocal.Left;
				int contentTop = m_offsetY + contentLocal.Top;
				int contentRight = m_offsetX + contentLocal.Right;
				int contentBottom = m_offsetY + contentLocal.Bottom;
				coverLeft = contentLeft;
				if (coverLeft > 0)
				{
					coverLeft = 0;
				}
				coverTop = contentTop;
				if (coverTop > 0)
				{
					coverTop = 0;
				}
				coverRight = contentRight;
				if (coverRight < canvasWidth)
				{
					coverRight = canvasWidth;
				}
				coverBottom = contentBottom;
				if (coverBottom < canvasHeight)
				{
					coverBottom = canvasHeight;
				}
			}
			int newWidth = coverRight - coverLeft;
			int newHeight = coverBottom - coverTop;
			if (newWidth == m_bitmap.Width && newHeight == m_bitmap.Height && coverLeft == m_offsetX && coverTop == m_offsetY)
			{
				return;
			}
			SKBitmap grown = new SKBitmap(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			grown.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(grown);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKImage image = SKImage.FromBitmap(m_bitmap);
			SKPaint paint = new SKPaint();
			canvas.DrawImage(image, m_offsetX - coverLeft, m_offsetY - coverTop, sampling, paint);
			paint.Dispose();
			image.Dispose();
			canvas.Dispose();
			m_bitmap = grown;
			m_offsetX = coverLeft;
			m_offsetY = coverTop;
		}

		public void SetPixelCanvas(int canvasX, int canvasY, SKColor color)
		{
			int bitmapX = canvasX - m_offsetX;
			int bitmapY = canvasY - m_offsetY;
			if (bitmapX < 0 || bitmapY < 0 || bitmapX >= m_bitmap.Width || bitmapY >= m_bitmap.Height)
			{
				return;
			}
			m_bitmap.SetPixel(bitmapX, bitmapY, color);
		}

		public SKColor GetPixelCanvas(int canvasX, int canvasY)
		{
			int bitmapX = canvasX - m_offsetX;
			int bitmapY = canvasY - m_offsetY;
			if (bitmapX < 0 || bitmapY < 0 || bitmapX >= m_bitmap.Width || bitmapY >= m_bitmap.Height)
			{
				return SKColors.Transparent;
			}
			return m_bitmap.GetPixel(bitmapX, bitmapY);
		}

		public string Name()
		{
			return m_name;
		}

		public void SetName(string name)
		{
			m_name = name;
		}

		public bool IsVisible()
		{
			return m_visible;
		}

		public void SetVisible(bool visible)
		{
			m_visible = visible;
		}

		public byte Opacity()
		{
			return m_opacity;
		}

		public void SetOpacity(byte opacity)
		{
			m_opacity = opacity;
		}

		public eBlendMode BlendMode()
		{
			return m_blendMode;
		}

		public void SetBlendMode(eBlendMode blendMode)
		{
			m_blendMode = blendMode;
		}

		public void FillWhite()
		{
			m_bitmap.Erase(SKColors.White);
		}

		public void SetPixelsFrom(SKBitmap source)
		{
			m_bitmap.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(m_bitmap);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKImage image = SKImage.FromBitmap(source);
			SKPaint paint = new SKPaint();
			canvas.DrawImage(image, 0.0f, 0.0f, sampling, paint);
			paint.Dispose();
			image.Dispose();
			canvas.Dispose();
		}
	}
}
