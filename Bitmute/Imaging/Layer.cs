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
		}

		public SKBitmap Bitmap()
		{
			return m_bitmap;
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
