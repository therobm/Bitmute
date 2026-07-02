using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class MoveTool : Tool
	{
		private SKBitmap m_snapshot;
		private int m_startX;
		private int m_startY;

		private void RedrawShifted(Layer layer, int deltaX, int deltaY)
		{
			SKBitmap bitmap = layer.Bitmap();
			bitmap.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(bitmap);
			SKImage image = SKImage.FromBitmap(m_snapshot);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			canvas.DrawImage(image, deltaX, deltaY, sampling, paint);
			paint.Dispose();
			image.Dispose();
			canvas.Dispose();
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			if (m_snapshot != null)
			{
				m_snapshot.Dispose();
			}
			m_snapshot = layer.Bitmap().Copy();
			m_startX = x;
			m_startY = y;
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			if (m_snapshot == null)
			{
				return false;
			}
			RedrawShifted(layer, x - m_startX, y - m_startY);
			return true;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (m_snapshot != null)
			{
				m_snapshot.Dispose();
				m_snapshot = null;
			}
			m_hasLast = false;
		}
	}
}
