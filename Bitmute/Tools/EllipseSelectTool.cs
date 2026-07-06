using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class EllipseSelectTool : Tool
	{
		private const double ClickTravelThreshold = 3.0;

		private int m_startX;
		private int m_startY;
		private byte[] m_scratchMask;
		private int m_guideSnapTolerance = -1;
		private double m_pointerTravel;
		private bool m_pointerTravelMeasured;
		private bool m_previewActive;
		private int m_previewWidth;
		private int m_previewHeight;

		public override bool IsDestructive()
		{
			return false;
		}

		public void SetGuideSnap(int tolerance)
		{
			m_guideSnapTolerance = tolerance;
		}

		public void SetPointerTravel(double travel)
		{
			m_pointerTravelMeasured = true;
			m_pointerTravel = travel;
		}

		public bool HasSizePreview()
		{
			return m_previewActive;
		}

		public int PreviewWidth()
		{
			return m_previewWidth;
		}

		public int PreviewHeight()
		{
			return m_previewHeight;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			m_startX = x;
			m_startY = y;
			m_previewActive = true;
			eSelectionMode mode = SelectionModeFromState(state);
			if (mode == eSelectionMode.Replace)
			{
				document.Selection().Clear();
			}
			document.Selection().BeginOperation(mode, state.SelectionFeather());
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			int left = m_startX;
			int right = x;
			if (right < left)
			{
				int swap = left;
				left = right;
				right = swap;
			}
			int top = m_startY;
			int bottom = y;
			if (bottom < top)
			{
				int swap = top;
				top = bottom;
				bottom = swap;
			}
			if (m_pointerTravelMeasured && m_pointerTravel < ClickTravelThreshold)
			{
				document.Selection().ApplyRect(SKRectI.Empty);
				m_previewWidth = 0;
				m_previewHeight = 0;
				return false;
			}
			int leftBoundary = left;
			int topBoundary = top;
			int rightBoundary = right + 1;
			int bottomBoundary = bottom + 1;
			if (m_guideSnapTolerance >= 0)
			{
				Bitmute.Imaging.Guides guides = document.Guides();
				leftBoundary = guides.SnapX(leftBoundary, m_guideSnapTolerance);
				rightBoundary = guides.SnapX(rightBoundary, m_guideSnapTolerance);
				topBoundary = guides.SnapY(topBoundary, m_guideSnapTolerance);
				bottomBoundary = guides.SnapY(bottomBoundary, m_guideSnapTolerance);
				if (rightBoundary <= leftBoundary)
				{
					rightBoundary = leftBoundary + 1;
				}
				if (bottomBoundary <= topBoundary)
				{
					bottomBoundary = topBoundary + 1;
				}
			}
			left = leftBoundary;
			top = topBoundary;
			right = rightBoundary;
			bottom = bottomBoundary;
			m_previewWidth = right - left;
			m_previewHeight = bottom - top;

			int regionLeft = left - 1;
			int regionTop = top - 1;
			int regionRight = right + 1;
			int regionBottom = bottom + 1;
			int regionWidth = regionRight - regionLeft;
			int regionHeight = regionBottom - regionTop;

			double centerX = (left + right) / 2.0;
			double centerY = (top + bottom) / 2.0;
			double radiusX = (right - left) / 2.0;
			double radiusY = (bottom - top) / 2.0;
			bool antiAlias = state.SelectionAntiAlias();

			int regionCount = regionWidth * regionHeight;
			if (m_scratchMask == null || m_scratchMask.Length < regionCount)
			{
				m_scratchMask = new byte[regionCount];
			}
			byte[] mask = m_scratchMask;
			System.Array.Clear(mask, 0, regionCount);
			for (int pixelY = regionTop; pixelY < regionBottom; pixelY++)
			{
				double normalizedY = ((pixelY + 0.5) - centerY) / radiusY;
				double normalizedYSquared = normalizedY * normalizedY;
				int rowStart = ((pixelY - regionTop) * regionWidth) - regionLeft;
				for (int pixelX = regionLeft; pixelX < regionRight; pixelX++)
				{
					double normalizedX = ((pixelX + 0.5) - centerX) / radiusX;
					double radialSquared = (normalizedX * normalizedX) + normalizedYSquared;
					if (!antiAlias)
					{
						if (radialSquared > 1.0)
						{
							continue;
						}
						mask[rowStart + pixelX] = 255;
						continue;
					}
					double radial = System.Math.Sqrt(radialSquared);
					if (radial <= 0.0)
					{
						mask[rowStart + pixelX] = 255;
						continue;
					}
					double gradientX = normalizedX / radiusX;
					double gradientY = normalizedY / radiusY;
					double gradient = System.Math.Sqrt((gradientX * gradientX) + (gradientY * gradientY)) / radial;
					if (gradient <= 0.0)
					{
						mask[rowStart + pixelX] = 255;
						continue;
					}
					double signedDistance = (radial - 1.0) / gradient;
					double alpha = 0.5 - signedDistance;
					if (alpha <= 0.0)
					{
						continue;
					}
					if (alpha > 1.0)
					{
						alpha = 1.0;
					}
					mask[rowStart + pixelX] = (byte)((alpha * 255.0) + 0.5);
				}
			}

			document.Selection().ApplyMask(mask, new SKRectI(regionLeft, regionTop, regionRight, regionBottom));
			return false;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_previewActive = false;
			base.OnReleased(document, x, y, state);
		}
	}
}
