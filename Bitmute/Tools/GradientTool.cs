using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class GradientTool : Tool
	{
		private bool m_active;
		private int m_startX;
		private int m_startY;
		private int m_endX;
		private int m_endY;

		public override bool IsDestructive()
		{
			return true;
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

		private void RenderGradient(Document document, Layer layer, ToolState state)
		{
			int documentWidth = document.Width();
			int documentHeight = document.Height();
			if (documentWidth <= 0 || documentHeight <= 0)
			{
				return;
			}

			SKBitmap gradientBitmap = new SKBitmap(documentWidth, documentHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKCanvas gradientCanvas = new SKCanvas(gradientBitmap);
			SKColor[] colors = new SKColor[] { state.Foreground(), state.Background() };
			SKPoint startPoint = new SKPoint(m_startX, m_startY);
			SKPoint endPoint = new SKPoint(m_endX, m_endY);
			SKShader shader = SKShader.CreateLinearGradient(startPoint, endPoint, colors, null, SKShaderTileMode.Clamp);
			SKPaint gradientPaint = new SKPaint();
			gradientPaint.Shader = shader;
			gradientCanvas.DrawRect(new SKRect(0.0f, 0.0f, documentWidth, documentHeight), gradientPaint);
			gradientPaint.Dispose();
			shader.Dispose();
			gradientCanvas.Dispose();

			BlitCanvasBitmap(document, layer, gradientBitmap, 0, 0);
			gradientBitmap.Dispose();
			MarkStrokeDirty(document, 0, 0, documentWidth, documentHeight, 0);
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
				return;
			}
			SetConstrainedEnd(x, y, state.ShiftHeld());
			Layer layer = document.ActiveLayer();
			if (layer != null)
			{
				RenderGradient(document, layer, state);
			}
			m_active = false;
		}
	}
}
