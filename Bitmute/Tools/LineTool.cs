using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class LineTool : Tool
	{
		private bool m_active;
		private int m_startX;
		private int m_startY;
		private int m_endX;
		private int m_endY;

		private static SKColor SourceOver(SKColor source, SKColor destination)
		{
			int sourceAlpha = source.Alpha;
			if (sourceAlpha == 255)
			{
				return source;
			}
			int inverse = 255 - sourceAlpha;
			int destinationContribution = ((destination.Alpha * inverse) + 127) / 255;
			int outAlpha = sourceAlpha + destinationContribution;
			if (outAlpha == 0)
			{
				return new SKColor(0, 0, 0, 0);
			}
			int red = ((source.Red * sourceAlpha) + (destination.Red * destinationContribution) + (outAlpha / 2)) / outAlpha;
			int green = ((source.Green * sourceAlpha) + (destination.Green * destinationContribution) + (outAlpha / 2)) / outAlpha;
			int blue = ((source.Blue * sourceAlpha) + (destination.Blue * destinationContribution) + (outAlpha / 2)) / outAlpha;
			return new SKColor((byte)red, (byte)green, (byte)blue, (byte)outAlpha);
		}

		public bool HasPreview()
		{
			return m_active;
		}

		public int PreviewStartX()
		{
			return m_startX;
		}

		public int PreviewStartY()
		{
			return m_startY;
		}

		public int PreviewEndX()
		{
			return m_endX;
		}

		public int PreviewEndY()
		{
			return m_endY;
		}

		private void SetConstrainedEnd(int rawEndX, int rawEndY, bool shift)
		{
			if (!shift)
			{
				m_endX = rawEndX;
				m_endY = rawEndY;
				return;
			}
			int deltaX = rawEndX - m_startX;
			int deltaY = rawEndY - m_startY;
			int absoluteX = deltaX;
			if (absoluteX < 0)
			{
				absoluteX = -absoluteX;
			}
			int absoluteY = deltaY;
			if (absoluteY < 0)
			{
				absoluteY = -absoluteY;
			}
			if (absoluteX > (2 * absoluteY))
			{
				m_endX = rawEndX;
				m_endY = m_startY;
				return;
			}
			if (absoluteY > (2 * absoluteX))
			{
				m_endX = m_startX;
				m_endY = rawEndY;
				return;
			}
			int diagonal = (absoluteX + absoluteY) / 2;
			int signX = 1;
			if (deltaX < 0)
			{
				signX = -1;
			}
			int signY = 1;
			if (deltaY < 0)
			{
				signY = -1;
			}
			m_endX = m_startX + (signX * diagonal);
			m_endY = m_startY + (signY * diagonal);
		}

		private void RenderLine(Document document, Layer layer, ToolState state)
		{
			int lineWidth = state.BrushSize();
			if (lineWidth < 1)
			{
				lineWidth = 1;
			}
			int pad = (lineWidth / 2) + 2;
			int left = m_startX;
			if (m_endX < left)
			{
				left = m_endX;
			}
			left = left - pad;
			int top = m_startY;
			if (m_endY < top)
			{
				top = m_endY;
			}
			top = top - pad;
			int right = m_startX;
			if (m_endX > right)
			{
				right = m_endX;
			}
			right = right + pad + 1;
			int bottom = m_startY;
			if (m_endY > bottom)
			{
				bottom = m_endY;
			}
			bottom = bottom + pad + 1;
			int boundsWidth = right - left;
			int boundsHeight = bottom - top;
			if (boundsWidth <= 0 || boundsHeight <= 0)
			{
				return;
			}

			SKBitmap temp = new SKBitmap(boundsWidth, boundsHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			temp.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(temp);
			SKPaint paint = new SKPaint();
			paint.Color = state.Foreground();
			paint.StrokeWidth = lineWidth;
			paint.StrokeCap = SKStrokeCap.Round;
			paint.IsAntialias = state.LineAntiAlias();
			paint.Style = SKPaintStyle.Stroke;
			canvas.DrawLine(m_startX - left, m_startY - top, m_endX - left, m_endY - top, paint);
			paint.Dispose();
			canvas.Dispose();

			Selection selection = document.Selection();
			bool clip = selection.IsActive();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			SKBitmap layerBitmap = layer.Bitmap();
			int layerWidth = layerBitmap.Width;
			int layerHeight = layerBitmap.Height;
			for (int tempY = 0; tempY < boundsHeight; tempY++)
			{
				int canvasY = top + tempY;
				for (int tempX = 0; tempX < boundsWidth; tempX++)
				{
					SKColor source = temp.GetPixel(tempX, tempY);
					if (source.Alpha == 0)
					{
						continue;
					}
					int canvasX = left + tempX;
					if (clip && !selection.IsSelected(canvasX, canvasY))
					{
						continue;
					}
					int bitmapX = canvasX - offsetX;
					int bitmapY = canvasY - offsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= layerWidth || bitmapY >= layerHeight)
					{
						continue;
					}
					SKColor destination = layerBitmap.GetPixel(bitmapX, bitmapY);
					SKColor blended = SourceOver(source, destination);
					layerBitmap.SetPixel(bitmapX, bitmapY, blended);
				}
			}
			temp.Dispose();
			MarkStrokeDirty(document, m_startX, m_startY, m_endX, m_endY, pad);
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			m_active = true;
			m_startX = x;
			m_startY = y;
			m_endX = x;
			m_endY = y;
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				return false;
			}
			SetConstrainedEnd(x, y, state.ShiftHeld());
			return false;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				m_hasLast = false;
				return;
			}
			SetConstrainedEnd(x, y, state.ShiftHeld());
			Layer layer = document.ActiveLayer();
			if (layer != null)
			{
				RenderLine(document, layer, state);
			}
			m_active = false;
			m_hasLast = false;
		}
	}
}
