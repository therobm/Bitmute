using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class MoveTool : Tool
	{
		private bool m_offsetMode;
		private bool m_textMode;
		private SKBitmap m_moving;
		private SKBitmap m_static;
		private byte[] m_selectionMask;
		private SKRectI m_selectionBounds;
		private int m_startX;
		private int m_startY;
		private int m_oldOffsetX;
		private int m_oldOffsetY;
		private int m_oldTextX;
		private int m_oldTextY;

		private SKBitmap ExtractSelected(Layer layer, Selection selection)
		{
			SKBitmap source = layer.Bitmap();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int width = source.Width;
			int height = source.Height;
			SKBitmap moving = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			moving.Erase(SKColors.Transparent);
			SKRectI bounds = selection.Bounds();
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					if (!selection.IsSelected(x, y))
					{
						continue;
					}
					int bitmapX = x - offsetX;
					int bitmapY = y - offsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= width || bitmapY >= height)
					{
						continue;
					}
					moving.SetPixel(bitmapX, bitmapY, source.GetPixel(bitmapX, bitmapY));
				}
			}
			return moving;
		}

		private SKBitmap CloneWithSelectionCleared(Layer layer, Selection selection)
		{
			SKBitmap remainder = layer.Bitmap().Copy();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int width = remainder.Width;
			int height = remainder.Height;
			SKRectI bounds = selection.Bounds();
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					if (!selection.IsSelected(x, y))
					{
						continue;
					}
					int bitmapX = x - offsetX;
					int bitmapY = y - offsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= width || bitmapY >= height)
					{
						continue;
					}
					remainder.SetPixel(bitmapX, bitmapY, SKColors.Transparent);
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
			m_selectionMask = null;
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
				m_textMode = false;
				m_moving = ExtractSelected(layer, selection);
				m_static = CloneWithSelectionCleared(layer, selection);
				m_selectionMask = selection.MaskCopy();
				m_selectionBounds = selection.Bounds();
			}
			else if (layer.IsText())
			{
				m_textMode = true;
				m_offsetMode = false;
				m_oldTextX = layer.TextX();
				m_oldTextY = layer.TextY();
			}
			else
			{
				m_textMode = false;
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
			if (m_textMode)
			{
				layer.SetTextPosition(m_oldTextX + (x - m_startX), m_oldTextY + (y - m_startY));
				layer.RenderText();
				return true;
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
			int deltaX = x - m_startX;
			int deltaY = y - m_startY;
			Rebuild(layer, deltaX, deltaY);
			if (m_selectionMask != null)
			{
				document.Selection().SetShifted(m_selectionMask, m_selectionBounds, deltaX, deltaY);
			}
			return true;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (m_textMode)
			{
				m_textMode = false;
				ReleaseBuffers();
				m_hasLast = false;
				return;
			}
			if (m_offsetMode)
			{
				Layer layer = document.ActiveLayer();
				if (layer != null)
				{
					if (layer.OffsetX() != m_oldOffsetX || layer.OffsetY() != m_oldOffsetY)
					{
						SKBitmap oldBitmap = layer.Bitmap();
						layer.ExpandToCover(document.Width(), document.Height());
						SKBitmap newBitmap = layer.Bitmap();
						document.PushCommand(new MoveLayerCommand(document.ActiveLayerIndex(), oldBitmap, m_oldOffsetX, m_oldOffsetY, newBitmap, layer.OffsetX(), layer.OffsetY()));
					}
				}
			}
			ReleaseBuffers();
			m_hasLast = false;
		}
	}
}
