using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class MoveTool : Tool
	{
		private bool m_offsetMode;
		private SKBitmap m_moving;
		private SKBitmap m_static;
		private int m_startX;
		private int m_startY;
		private int m_oldOffsetX;
		private int m_oldOffsetY;

		private SKBitmap ExtractSelected(SKBitmap source, Selection selection)
		{
			int width = source.Width;
			int height = source.Height;
			SKBitmap moving = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			moving.Erase(SKColors.Transparent);
			SKRectI bounds = selection.Bounds();
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					if (selection.IsSelected(x, y))
					{
						moving.SetPixel(x, y, source.GetPixel(x, y));
					}
				}
			}
			return moving;
		}

		private SKBitmap CloneWithSelectionCleared(SKBitmap source, Selection selection)
		{
			SKBitmap remainder = source.Copy();
			SKRectI bounds = selection.Bounds();
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					if (selection.IsSelected(x, y))
					{
						remainder.SetPixel(x, y, SKColors.Transparent);
					}
				}
			}
			return remainder;
		}

		private void Rebuild(Layer layer, int deltaX, int deltaY)
		{
			SKBitmap bitmap = layer.Bitmap();
			bitmap.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(bitmap);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			if (m_static != null)
			{
				SKImage staticImage = SKImage.FromBitmap(m_static);
				canvas.DrawImage(staticImage, 0.0f, 0.0f, sampling, paint);
				staticImage.Dispose();
			}
			SKImage movingImage = SKImage.FromBitmap(m_moving);
			canvas.DrawImage(movingImage, deltaX, deltaY, sampling, paint);
			movingImage.Dispose();
			paint.Dispose();
			canvas.Dispose();
		}

		private void ReleaseBuffers()
		{
			if (m_moving != null)
			{
				m_moving.Dispose();
				m_moving = null;
			}
			if (m_static != null)
			{
				m_static.Dispose();
				m_static = null;
			}
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			ReleaseBuffers();
			m_startX = x;
			m_startY = y;
			Selection selection = document.Selection();
			if (selection.IsActive())
			{
				m_offsetMode = false;
				m_moving = ExtractSelected(layer.Bitmap(), selection);
				m_static = CloneWithSelectionCleared(layer.Bitmap(), selection);
			}
			else
			{
				m_offsetMode = true;
				m_oldOffsetX = layer.OffsetX();
				m_oldOffsetY = layer.OffsetY();
			}
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			if (m_offsetMode)
			{
				layer.SetOffset(m_oldOffsetX + (x - m_startX), m_oldOffsetY + (y - m_startY));
				return true;
			}
			if (m_moving == null)
			{
				return false;
			}
			Rebuild(layer, x - m_startX, y - m_startY);
			return true;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (m_offsetMode)
			{
				Layer layer = document.ActiveLayer();
				if (layer != null)
				{
					if (layer.OffsetX() != m_oldOffsetX || layer.OffsetY() != m_oldOffsetY)
					{
						document.PushCommand(new MoveLayerCommand(document.ActiveLayerIndex(), m_oldOffsetX, m_oldOffsetY, layer.OffsetX(), layer.OffsetY()));
					}
				}
			}
			ReleaseBuffers();
			m_hasLast = false;
		}
	}
}
