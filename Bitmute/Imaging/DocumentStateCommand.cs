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
		private eColorDepth m_beforeColorDepth;
		private List<Layer> m_beforeLayers;
		private byte[] m_beforeMask;
		private SKRectI m_beforeMaskRect;
		private SKRectI m_beforeBounds;
		private bool m_beforeSelActive;
		private int m_afterWidth;
		private int m_afterHeight;
		private int m_afterActive;
		private eColorDepth m_afterColorDepth;
		private List<Layer> m_afterLayers;
		private byte[] m_afterMask;
		private SKRectI m_afterMaskRect;
		private SKRectI m_afterBounds;
		private bool m_afterSelActive;

		private static SKRectI SelectionMaskRect(Selection selection)
		{
			return new SKRectI(selection.MaskOriginX(), selection.MaskOriginY(), selection.MaskOriginX() + selection.MaskWidth(), selection.MaskOriginY() + selection.MaskHeight());
		}

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
			m_beforeColorDepth = document.ColorDepth();
			Selection selection = document.Selection();
			m_beforeSelActive = selection.IsActive();
			m_beforeBounds = selection.Bounds();
			m_beforeMaskRect = SelectionMaskRect(selection);
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
			m_afterColorDepth = document.ColorDepth();
			Selection selection = document.Selection();
			m_afterSelActive = selection.IsActive();
			m_afterBounds = selection.Bounds();
			m_afterMaskRect = SelectionMaskRect(selection);
			if (m_afterSelActive)
			{
				m_afterMask = selection.MaskCopy();
			}
			else
			{
				m_afterMask = null;
			}
		}

		private static bool ByteArraysEqual(byte[] first, byte[] second)
		{
			if (first == null || second == null)
			{
				return first == second;
			}
			if (first.Length != second.Length)
			{
				return false;
			}
			for (int index = 0; index < first.Length; index++)
			{
				if (first[index] != second[index])
				{
					return false;
				}
			}
			return true;
		}

		public bool HasChange()
		{
			if (m_beforeWidth != m_afterWidth || m_beforeHeight != m_afterHeight)
			{
				return true;
			}
			if (m_beforeActive != m_afterActive || m_beforeColorDepth != m_afterColorDepth)
			{
				return true;
			}
			if (m_beforeSelActive != m_afterSelActive)
			{
				return true;
			}
			if (m_beforeSelActive)
			{
				if (!m_beforeBounds.Equals(m_afterBounds))
				{
					return true;
				}
				if (!m_beforeMaskRect.Equals(m_afterMaskRect))
				{
					return true;
				}
				if (!ByteArraysEqual(m_beforeMask, m_afterMask))
				{
					return true;
				}
			}
			if (m_beforeLayers.Count != m_afterLayers.Count)
			{
				return true;
			}
			for (int index = 0; index < m_beforeLayers.Count; index++)
			{
				if (!m_beforeLayers[index].ContentEquals(m_afterLayers[index]))
				{
					return true;
				}
			}
			return false;
		}

		public override void ApplyBefore(Document document)
		{
			document.SetColorDepth(m_beforeColorDepth);
			document.ReplaceLayers(m_beforeLayers, m_beforeWidth, m_beforeHeight, m_beforeActive);
			document.RestoreSelection(m_beforeMask, m_beforeMaskRect, m_beforeBounds, m_beforeSelActive);
		}

		public override void ApplyAfter(Document document)
		{
			document.SetColorDepth(m_afterColorDepth);
			document.ReplaceLayers(m_afterLayers, m_afterWidth, m_afterHeight, m_afterActive);
			document.RestoreSelection(m_afterMask, m_afterMaskRect, m_afterBounds, m_afterSelActive);
		}
	}
}
