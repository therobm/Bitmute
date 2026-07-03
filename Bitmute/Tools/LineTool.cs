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

			BlitCanvasBitmap(document, layer, temp, left, top);
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
