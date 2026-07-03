using Bitmute.Imaging;
using Bitmute.UI;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class RulerTool : Tool
	{
		private bool m_active;
		private bool m_hasMeasurement;
		private int m_startX;
		private int m_startY;
		private int m_endX;
		private int m_endY;

		private void ReportMeasurement()
		{
			int deltaX = m_endX - m_startX;
			int deltaY = m_endY - m_startY;
			double length = System.Math.Sqrt((double)((deltaX * deltaX) + (deltaY * deltaY)));
			double angleRadians = System.Math.Atan2((double)(-deltaY), (double)deltaX);
			double angleDegrees = angleRadians * (180.0 / System.Math.PI);
			string message = "L: " + length.ToString("F1") + "  A: " + angleDegrees.ToString("F1") + "°  dX: " + deltaX.ToString() + "  dY: " + deltaY.ToString();
			MainView.Self.SetStatusMessage(message);
		}

		public bool HasPreview()
		{
			return m_hasMeasurement;
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

		public void Reset()
		{
			m_active = false;
			m_hasMeasurement = false;
			m_startX = 0;
			m_startY = 0;
			m_endX = 0;
			m_endY = 0;
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			m_active = true;
			m_hasMeasurement = true;
			m_startX = x;
			m_startY = y;
			m_endX = x;
			m_endY = y;
			ReportMeasurement();
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				return false;
			}
			m_endX = x;
			m_endY = y;
			m_hasMeasurement = true;
			ReportMeasurement();
			return false;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				m_hasLast = false;
				return;
			}
			m_endX = x;
			m_endY = y;
			m_hasMeasurement = true;
			ReportMeasurement();
			m_active = false;
			m_hasLast = false;
		}
	}
}
