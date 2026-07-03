using SkiaSharp;

namespace Bitmute.Imaging
{
	public enum eBlendMode
	{
		Normal,
		Multiply,
		Screen,
		Overlay,
		Add
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

		public static SKBlendMode ToSkBlendMode(eBlendMode blendMode)
		{
			if (blendMode == eBlendMode.Multiply)
			{
				return SKBlendMode.Multiply;
			}
			if (blendMode == eBlendMode.Screen)
			{
				return SKBlendMode.Screen;
			}
			if (blendMode == eBlendMode.Overlay)
			{
				return SKBlendMode.Overlay;
			}
			if (blendMode == eBlendMode.Add)
			{
				return SKBlendMode.Plus;
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
			TextRasterizer.Draw(m_bitmap, m_text, m_textX - m_offsetX, m_textY - m_offsetY, m_textColor, m_textSize, m_textFontFamily, m_textBold, m_textItalic, m_textAlign, m_textAntiAlias);
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
