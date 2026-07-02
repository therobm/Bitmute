using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public class LayerEditCommand : EditCommand
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

		private SKBitmap LayerBitmap(Document document)
		{
			List<Layer> layers = document.Layers();
			if (m_layerIndex < 0 || m_layerIndex >= layers.Count)
			{
				return null;
			}
			return layers[m_layerIndex].Bitmap();
		}

		public override void ApplyBefore(Document document)
		{
			SKBitmap bitmap = LayerBitmap(document);
			if (bitmap == null)
			{
				return;
			}
			PixelRegion.ApplyRegion(bitmap, m_before, m_rect.Left, m_rect.Top);
		}

		public override void ApplyAfter(Document document)
		{
			SKBitmap bitmap = LayerBitmap(document);
			if (bitmap == null)
			{
				return;
			}
			PixelRegion.ApplyRegion(bitmap, m_after, m_rect.Left, m_rect.Top);
		}
	}
}
