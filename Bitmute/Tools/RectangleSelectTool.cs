using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class RectangleSelectTool : Tool
	{
		private const double ClickTravelThreshold = 3.0;

		private int m_startX;
		private int m_startY;
		private int m_guideSnapTolerance = -1;
		private double m_pointerTravel;
		private bool m_pointerTravelMeasured;
		private bool m_constrainAllowed;
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
			m_constrainAllowed = !document.Selection().IsActive();
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
			bool square = m_constrainAllowed && state.ShiftHeld();
			bool fromCenter = m_constrainAllowed && state.CtrlHeld();
			int cornerAX;
			int cornerAY;
			int cornerBX;
			int cornerBY;
			ConstrainMarqueeCorners(m_startX, m_startY, x, y, square, fromCenter, out cornerAX, out cornerAY, out cornerBX, out cornerBY);
			int left = cornerAX;
			int right = cornerBX;
			if (right < left)
			{
				int swap = left;
				left = right;
				right = swap;
			}
			int top = cornerAY;
			int bottom = cornerBY;
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
			SKRectI rect = new SKRectI(left, top, rightBoundary, bottomBoundary);
			document.Selection().ApplyRect(rect);
			m_previewWidth = rightBoundary - left;
			m_previewHeight = bottomBoundary - top;
			return false;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_previewActive = false;
			base.OnReleased(document, x, y, state);
		}
	}
}
