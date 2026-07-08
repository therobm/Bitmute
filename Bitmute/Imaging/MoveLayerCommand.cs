using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public class MoveLayerCommand : EditCommand
	{
		private int m_layerIndex;
		private SKBitmap m_oldBitmap;
		private SKBitmap m_oldMask;
		private int m_oldOffsetX;
		private int m_oldOffsetY;
		private SKBitmap m_newBitmap;
		private SKBitmap m_newMask;
		private int m_newOffsetX;
		private int m_newOffsetY;
		private bool m_masksCaptured;

		public MoveLayerCommand(int layerIndex, SKBitmap oldBitmap, int oldOffsetX, int oldOffsetY, SKBitmap newBitmap, int newOffsetX, int newOffsetY)
		{
			m_layerIndex = layerIndex;
			m_oldBitmap = oldBitmap;
			m_oldOffsetX = oldOffsetX;
			m_oldOffsetY = oldOffsetY;
			m_newBitmap = newBitmap;
			m_newOffsetX = newOffsetX;
			m_newOffsetY = newOffsetY;
			m_masksCaptured = false;
		}

		public MoveLayerCommand(int layerIndex, SKBitmap oldBitmap, SKBitmap oldMask, int oldOffsetX, int oldOffsetY, SKBitmap newBitmap, SKBitmap newMask, int newOffsetX, int newOffsetY)
		{
			m_layerIndex = layerIndex;
			m_oldBitmap = oldBitmap;
			m_oldMask = oldMask;
			m_oldOffsetX = oldOffsetX;
			m_oldOffsetY = oldOffsetY;
			m_newBitmap = newBitmap;
			m_newMask = newMask;
			m_newOffsetX = newOffsetX;
			m_newOffsetY = newOffsetY;
			m_masksCaptured = true;
		}

		public override string Label()
		{
			return "Move Layer";
		}

		private Layer TargetLayer(Document document)
		{
			List<Layer> layers = document.Layers();
			if (m_layerIndex < 0 || m_layerIndex >= layers.Count)
			{
				return null;
			}
			return layers[m_layerIndex];
		}

		public override void ApplyBefore(Document document)
		{
			Layer layer = TargetLayer(document);
			if (layer == null)
			{
				return;
			}
			if (m_masksCaptured)
			{
				layer.RestoreMoveState(m_oldBitmap, m_oldMask, m_oldOffsetX, m_oldOffsetY);
			}
			else
			{
				layer.SetBitmap(m_oldBitmap);
				layer.SetOffset(m_oldOffsetX, m_oldOffsetY);
			}
		}

		public override void ApplyAfter(Document document)
		{
			Layer layer = TargetLayer(document);
			if (layer == null)
			{
				return;
			}
			if (m_masksCaptured)
			{
				layer.RestoreMoveState(m_newBitmap, m_newMask, m_newOffsetX, m_newOffsetY);
			}
			else
			{
				layer.SetBitmap(m_newBitmap);
				layer.SetOffset(m_newOffsetX, m_newOffsetY);
			}
		}
	}
}
