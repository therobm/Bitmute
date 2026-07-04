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
		private bool m_lockAll;
		private bool m_lockPixels;
		private bool m_lockPosition;
		private bool m_lockAlpha;
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
		private LayerStyle m_layerStyle;
		private SKBitmap m_cacheStroke;
		private SKBitmap m_cacheShadow;
		private SKBitmap m_cacheGlow;
		private SKBitmap m_cacheInnerGlow;
		private SKBitmap m_cacheBevel;
		private int m_cacheStrokeX;
		private int m_cacheStrokeY;
		private int m_cacheShadowX;
		private int m_cacheShadowY;
		private int m_cacheGlowX;
		private int m_cacheGlowY;
		private int m_cacheInnerGlowX;
		private int m_cacheInnerGlowY;
		private int m_cacheBevelX;
		private int m_cacheBevelY;
		private bool m_styleCacheDirty;

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
			m_lockAll = false;
			m_lockPixels = false;
			m_lockPosition = false;
			m_lockAlpha = false;
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
			m_layerStyle = new LayerStyle();
			m_styleCacheDirty = true;
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

		public bool LockAll()
		{
			return m_lockAll;
		}

		public void SetLockAll(bool locked)
		{
			m_lockAll = locked;
		}

		public bool LockPixels()
		{
			return m_lockPixels;
		}

		public void SetLockPixels(bool locked)
		{
			m_lockPixels = locked;
		}

		public bool LockPosition()
		{
			return m_lockPosition;
		}

		public void SetLockPosition(bool locked)
		{
			m_lockPosition = locked;
		}

		public bool LockAlpha()
		{
			return m_lockAlpha;
		}

		public void SetLockAlpha(bool locked)
		{
			m_lockAlpha = locked;
		}

		public bool PaintLocked()
		{
			return m_lockAll || m_lockPixels;
		}

		public bool MoveLocked()
		{
			return m_lockAll || m_lockPosition;
		}

		public SKBitmap Bitmap()
		{
			return m_bitmap;
		}

		public void SetBitmap(SKBitmap bitmap)
		{
			m_bitmap = bitmap;
			m_styleCacheDirty = true;
		}

		public LayerStyle LayerStyle()
		{
			return m_layerStyle;
		}

		public void SetLayerStyle(LayerStyle style)
		{
			m_layerStyle = style;
			m_styleCacheDirty = true;
		}

		public void InvalidateStyleCache()
		{
			m_styleCacheDirty = true;
		}

		private void DisposeStyleCache()
		{
			if (m_cacheStroke != null)
			{
				m_cacheStroke.Dispose();
				m_cacheStroke = null;
			}
			if (m_cacheShadow != null)
			{
				m_cacheShadow.Dispose();
				m_cacheShadow = null;
			}
			if (m_cacheGlow != null)
			{
				m_cacheGlow.Dispose();
				m_cacheGlow = null;
			}
			if (m_cacheInnerGlow != null)
			{
				m_cacheInnerGlow.Dispose();
				m_cacheInnerGlow = null;
			}
			if (m_cacheBevel != null)
			{
				m_cacheBevel.Dispose();
				m_cacheBevel = null;
			}
		}

		private void EnsureStyleCache()
		{
			if (!m_styleCacheDirty)
			{
				return;
			}
			DisposeStyleCache();
			if (m_layerStyle.m_hasDropShadow)
			{
				double radians = m_layerStyle.m_shadowAngle * System.Math.PI / 180.0;
				int shadowOffsetX = (int)System.Math.Round(System.Math.Cos(radians) * m_layerStyle.m_shadowDistance);
				int shadowOffsetY = (int)System.Math.Round(System.Math.Sin(radians) * m_layerStyle.m_shadowDistance);
				byte shadowOpacity = (byte)((m_layerStyle.m_shadowOpacity * 255) / 100);
				m_cacheShadow = LayerStyles.RenderDropShadow(m_bitmap, m_layerStyle.m_shadowColor, shadowOffsetX, shadowOffsetY, m_layerStyle.m_shadowSize, m_layerStyle.m_shadowSpread, shadowOpacity, out m_cacheShadowX, out m_cacheShadowY);
			}
			if (m_layerStyle.m_hasOuterGlow)
			{
				byte glowOpacity = (byte)((m_layerStyle.m_glowOpacity * 255) / 100);
				m_cacheGlow = LayerStyles.RenderOuterGlow(m_bitmap, m_layerStyle.m_glowColor, m_layerStyle.m_glowSize, m_layerStyle.m_glowSpread, glowOpacity, out m_cacheGlowX, out m_cacheGlowY);
			}
			if (m_layerStyle.m_hasInnerGlow)
			{
				byte innerGlowOpacity = (byte)((m_layerStyle.m_innerGlowOpacity * 255) / 100);
				m_cacheInnerGlow = LayerStyles.RenderInnerGlow(m_bitmap, m_layerStyle.m_innerGlowColor, m_layerStyle.m_innerGlowSize, m_layerStyle.m_innerGlowSpread, innerGlowOpacity, out m_cacheInnerGlowX, out m_cacheInnerGlowY);
			}
			if (m_layerStyle.m_hasBevel)
			{
				byte bevelHighlightOpacity = (byte)((m_layerStyle.m_bevelHighlightOpacity * 255) / 100);
				byte bevelShadowOpacity = (byte)((m_layerStyle.m_bevelShadowOpacity * 255) / 100);
				m_cacheBevel = LayerStyles.RenderBevel(m_bitmap, m_layerStyle.m_bevelDepth, m_layerStyle.m_bevelSize, m_layerStyle.m_bevelAngle, m_layerStyle.m_bevelHighlightColor, bevelHighlightOpacity, m_layerStyle.m_bevelShadowColor, bevelShadowOpacity, out m_cacheBevelX, out m_cacheBevelY);
			}
			if (m_layerStyle.m_hasStroke)
			{
				byte strokeOpacity = (byte)((m_layerStyle.m_strokeOpacity * 255) / 100);
				m_cacheStroke = LayerStyles.RenderStroke(m_bitmap, m_layerStyle.m_strokeSize, m_layerStyle.m_strokePosition, m_layerStyle.m_strokeColor, strokeOpacity, out m_cacheStrokeX, out m_cacheStrokeY);
			}
			m_styleCacheDirty = false;
		}

		private void DrawCachedEffect(SKCanvas canvas, SKBitmap effect, int placeX, int placeY, SKSamplingOptions sampling, byte layerOpacity, eBlendMode blendMode)
		{
			if (effect == null)
			{
				return;
			}
			SKPaint paint = new SKPaint();
			paint.Color = SKColors.White.WithAlpha(layerOpacity);
			paint.BlendMode = ToSkBlendMode(blendMode);
			SKPixmap pixmap = effect.PeekPixels();
			SKImage image = SKImage.FromPixels(pixmap);
			canvas.DrawImage(image, m_offsetX + placeX, m_offsetY + placeY, sampling, paint);
			image.Dispose();
			pixmap.Dispose();
			paint.Dispose();
		}

		public void DrawStyleUnder(SKCanvas canvas, SKSamplingOptions sampling)
		{
			if (!m_layerStyle.HasAnyEffect())
			{
				return;
			}
			EnsureStyleCache();
			DrawCachedEffect(canvas, m_cacheShadow, m_cacheShadowX, m_cacheShadowY, sampling, m_opacity, m_layerStyle.m_shadowBlendMode);
			DrawCachedEffect(canvas, m_cacheGlow, m_cacheGlowX, m_cacheGlowY, sampling, m_opacity, m_layerStyle.m_glowBlendMode);
		}

		public void DrawStyleOver(SKCanvas canvas, SKSamplingOptions sampling)
		{
			if (!m_layerStyle.HasAnyEffect())
			{
				return;
			}
			EnsureStyleCache();
			DrawCachedEffect(canvas, m_cacheInnerGlow, m_cacheInnerGlowX, m_cacheInnerGlowY, sampling, m_opacity, m_layerStyle.m_innerGlowBlendMode);
			DrawCachedEffect(canvas, m_cacheBevel, m_cacheBevelX, m_cacheBevelY, sampling, m_opacity, m_layerStyle.m_bevelBlendMode);
			DrawCachedEffect(canvas, m_cacheStroke, m_cacheStrokeX, m_cacheStrokeY, sampling, m_opacity, m_layerStyle.m_strokeBlendMode);
		}

		public Layer Clone()
		{
			Layer copy = new Layer(m_name, m_bitmap.Width, m_bitmap.Height);
			copy.m_bitmap = m_bitmap.Copy();
			copy.m_offsetX = m_offsetX;
			copy.m_offsetY = m_offsetY;
			copy.m_visible = m_visible;
			copy.m_opacity = m_opacity;
			copy.m_blendMode = m_blendMode;
			copy.m_isBackground = m_isBackground;
			copy.m_isText = m_isText;
			copy.m_lockAll = m_lockAll;
			copy.m_lockPixels = m_lockPixels;
			copy.m_lockPosition = m_lockPosition;
			copy.m_lockAlpha = m_lockAlpha;
			copy.m_text = m_text;
			copy.m_textX = m_textX;
			copy.m_textY = m_textY;
			copy.m_textSize = m_textSize;
			copy.m_textBold = m_textBold;
			copy.m_textItalic = m_textItalic;
			copy.m_textFontFamily = m_textFontFamily;
			copy.m_textColor = m_textColor;
			copy.m_textAlign = m_textAlign;
			copy.m_textAntiAlias = m_textAntiAlias;
			copy.m_textLeadingAuto = m_textLeadingAuto;
			copy.m_textLeading = m_textLeading;
			copy.m_textTracking = m_textTracking;
			copy.m_textHorizontalScale = m_textHorizontalScale;
			copy.m_textVerticalScale = m_textVerticalScale;
			copy.m_textBaselineShift = m_textBaselineShift;
			copy.m_textFauxBold = m_textFauxBold;
			copy.m_textFauxItalic = m_textFauxItalic;
			copy.m_textKerningAuto = m_textKerningAuto;
			copy.m_layerStyle = m_layerStyle.Clone();
			copy.m_styleCacheDirty = true;
			return copy;
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
