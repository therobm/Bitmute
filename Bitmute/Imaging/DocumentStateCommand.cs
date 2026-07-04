using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public class DocumentStateCommand : EditCommand
	{
		private string m_label;
		private int m_beforeWidth;
		private int m_beforeHeight;
		private int m_beforeActive;
		private List<Layer> m_beforeLayers;
		private byte[] m_beforeMask;
		private SKRectI m_beforeBounds;
		private bool m_beforeSelActive;
		private int m_afterWidth;
		private int m_afterHeight;
		private int m_afterActive;
		private List<Layer> m_afterLayers;
		private byte[] m_afterMask;
		private SKRectI m_afterBounds;
		private bool m_afterSelActive;

		public DocumentStateCommand(string label)
		{
			m_label = label;
		}

		public override string Label()
		{
			return m_label;
		}

		public void CaptureBefore(Document document)
		{
			m_beforeLayers = document.CloneLayers();
			m_beforeWidth = document.Width();
			m_beforeHeight = document.Height();
			m_beforeActive = document.ActiveLayerIndex();
			Selection selection = document.Selection();
			m_beforeSelActive = selection.IsActive();
			m_beforeBounds = selection.Bounds();
			if (m_beforeSelActive)
			{
				m_beforeMask = selection.MaskCopy();
			}
			else
			{
				m_beforeMask = null;
			}
		}

		public void CaptureAfter(Document document)
		{
			m_afterLayers = document.CloneLayers();
			m_afterWidth = document.Width();
			m_afterHeight = document.Height();
			m_afterActive = document.ActiveLayerIndex();
			Selection selection = document.Selection();
			m_afterSelActive = selection.IsActive();
			m_afterBounds = selection.Bounds();
			if (m_afterSelActive)
			{
				m_afterMask = selection.MaskCopy();
			}
			else
			{
				m_afterMask = null;
			}
		}

		public override void ApplyBefore(Document document)
		{
			document.ReplaceLayers(m_beforeLayers, m_beforeWidth, m_beforeHeight, m_beforeActive);
			document.RestoreSelection(m_beforeMask, m_beforeBounds, m_beforeSelActive);
		}

		public override void ApplyAfter(Document document)
		{
			document.ReplaceLayers(m_afterLayers, m_afterWidth, m_afterHeight, m_afterActive);
			document.RestoreSelection(m_afterMask, m_afterBounds, m_afterSelActive);
		}
	}
}
