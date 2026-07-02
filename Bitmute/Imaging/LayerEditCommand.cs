using SkiaSharp;

namespace Bitmute.Imaging
{
	public class LayerEditCommand
	{
		private int m_layerIndex;
		private SKRectI m_rect;
		private SKBitmap m_before;
		private SKBitmap m_after;

		public LayerEditCommand(int layerIndex, SKRectI rect, SKBitmap before, SKBitmap after)
		{
			m_layerIndex = layerIndex;
			m_rect = rect;
			m_before = before;
			m_after = after;
		}

		public int LayerIndex()
		{
			return m_layerIndex;
		}

		public void ApplyBefore(SKBitmap layerBitmap)
		{
			PixelRegion.ApplyRegion(layerBitmap, m_before, m_rect.Left, m_rect.Top);
		}

		public void ApplyAfter(SKBitmap layerBitmap)
		{
			PixelRegion.ApplyRegion(layerBitmap, m_after, m_rect.Left, m_rect.Top);
		}
	}
}
