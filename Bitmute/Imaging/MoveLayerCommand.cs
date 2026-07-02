using System.Collections.Generic;

namespace Bitmute.Imaging
{
	public class MoveLayerCommand : EditCommand
	{
		private int m_layerIndex;
		private int m_oldOffsetX;
		private int m_oldOffsetY;
		private int m_newOffsetX;
		private int m_newOffsetY;

		public MoveLayerCommand(int layerIndex, int oldOffsetX, int oldOffsetY, int newOffsetX, int newOffsetY)
		{
			m_layerIndex = layerIndex;
			m_oldOffsetX = oldOffsetX;
			m_oldOffsetY = oldOffsetY;
			m_newOffsetX = newOffsetX;
			m_newOffsetY = newOffsetY;
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
			layer.SetOffset(m_oldOffsetX, m_oldOffsetY);
		}

		public override void ApplyAfter(Document document)
		{
			Layer layer = TargetLayer(document);
			if (layer == null)
			{
				return;
			}
			layer.SetOffset(m_newOffsetX, m_newOffsetY);
		}
	}
}
