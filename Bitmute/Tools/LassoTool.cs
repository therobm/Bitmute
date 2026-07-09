using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class LassoTool : Tool
	{
		private const int CloseThreshold = 6;
		private const int MinimumVertices = 3;
		private const double DoubleClickMilliseconds = 350.0;
		private const int DoubleClickDistance = 6;

		private List<int> m_verticesX;
		private List<int> m_verticesY;
		private bool m_active;
		private DateTime m_lastClickTime;
		private int m_lastClickX;
		private int m_lastClickY;
		private bool m_hasLastClick;
		private int m_closeRadius;

		public LassoTool()
		{
			m_verticesX = new List<int>();
			m_verticesY = new List<int>();
			m_active = false;
			m_hasLastClick = false;
			m_closeRadius = CloseThreshold;
		}

		public void SetCloseRadius(int radius)
		{
			if (radius < 3)
			{
				radius = 3;
			}
			m_closeRadius = radius;
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public override void Reset()
		{
			m_verticesX.Clear();
			m_verticesY.Clear();
			m_active = false;
			m_hasLastClick = false;
		}

		public bool HasPreview()
		{
			return m_active && m_verticesX.Count > 0;
		}

		public void FinalizePending(Document document, ToolState state)
		{
			if (!m_active || m_verticesX.Count < MinimumVertices)
			{
				return;
			}
			Finalize(document, state);
		}

		public int VertexCount()
		{
			return m_verticesX.Count;
		}

		public int VertexX(int index)
		{
			return m_verticesX[index];
		}

		public int VertexY(int index)
		{
			return m_verticesY[index];
		}

		private bool IsDoubleClick(int x, int y)
		{
			if (!m_hasLastClick)
			{
				return false;
			}
			double elapsed = (DateTime.Now - m_lastClickTime).TotalMilliseconds;
			if (elapsed > DoubleClickMilliseconds)
			{
				return false;
			}
			int deltaX = x - m_lastClickX;
			int deltaY = y - m_lastClickY;
			return ((deltaX * deltaX) + (deltaY * deltaY)) <= (DoubleClickDistance * DoubleClickDistance);
		}

		private void RecordClick(int x, int y)
		{
			m_lastClickTime = DateTime.Now;
			m_lastClickX = x;
			m_lastClickY = y;
			m_hasLastClick = true;
		}

		private void Finalize(Document document, ToolState state)
		{
			CommitSelection(document, state);
			m_active = false;
			m_verticesX.Clear();
			m_verticesY.Clear();
		}

		private static void SortAscending(double[] values, int count)
		{
			for (int i = 1; i < count; i++)
			{
				double current = values[i];
				int j = i - 1;
				for (;;)
				{
					if (j < 0)
					{
						break;
					}
					if (values[j] <= current)
					{
						break;
					}
					values[j + 1] = values[j];
					j = j - 1;
				}
				values[j + 1] = current;
			}
		}

		private void CommitSelection(Document document, ToolState state)
		{
			int count = m_verticesX.Count;
			if (count < MinimumVertices)
			{
				document.Selection().ApplyRect(SKRectI.Empty);
				return;
			}
			int minPolyX = m_verticesX[0];
			int maxPolyX = m_verticesX[0];
			int minPolyY = m_verticesY[0];
			int maxPolyY = m_verticesY[0];
			for (int i = 1; i < count; i++)
			{
				int vertexX = m_verticesX[i];
				if (vertexX < minPolyX)
				{
					minPolyX = vertexX;
				}
				if (vertexX > maxPolyX)
				{
					maxPolyX = vertexX;
				}
				int vertexY = m_verticesY[i];
				if (vertexY < minPolyY)
				{
					minPolyY = vertexY;
				}
				if (vertexY > maxPolyY)
				{
					maxPolyY = vertexY;
				}
			}
			int regionLeft = minPolyX - 1;
			int regionTop = minPolyY - 1;
			int regionRight = maxPolyX + 2;
			int regionBottom = maxPolyY + 2;
			int regionWidth = regionRight - regionLeft;
			int regionHeight = regionBottom - regionTop;
			byte[] mask = new byte[regionWidth * regionHeight];

			double[] crossings = new double[count];
			for (int pixelY = minPolyY; pixelY <= maxPolyY; pixelY++)
			{
				double scanLine = pixelY + 0.5;
				int crossCount = 0;
				for (int i = 0; i < count; i++)
				{
					int j = i + 1;
					if (j == count)
					{
						j = 0;
					}
					double ax = m_verticesX[i];
					double ay = m_verticesY[i];
					double bx = m_verticesX[j];
					double by = m_verticesY[j];
					bool crosses = (ay <= scanLine && by > scanLine) || (by <= scanLine && ay > scanLine);
					if (!crosses)
					{
						continue;
					}
					double t = (scanLine - ay) / (by - ay);
					crossings[crossCount] = ax + (t * (bx - ax));
					crossCount = crossCount + 1;
				}
				SortAscending(crossings, crossCount);
				for (int k = 0; k + 1 < crossCount; k = k + 2)
				{
					int spanLeft = (int)Math.Ceiling(crossings[k] - 0.5);
					int spanRight = (int)Math.Floor(crossings[k + 1] - 0.5);
					if (spanLeft < regionLeft)
					{
						spanLeft = regionLeft;
					}
					if (spanRight > regionRight - 1)
					{
						spanRight = regionRight - 1;
					}
					int rowStart = ((pixelY - regionTop) * regionWidth) - regionLeft;
					for (int pixelX = spanLeft; pixelX <= spanRight; pixelX++)
					{
						mask[rowStart + pixelX] = 255;
					}
				}
			}

			if (state.SelectionAntiAlias())
			{
				SmoothMaskBoundary(mask, regionWidth, regionHeight);
			}
			document.Selection().ApplyMask(mask, new SKRectI(regionLeft, regionTop, regionRight, regionBottom));
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			bool doubleClick = IsDoubleClick(x, y);
			RecordClick(x, y);
			if (!m_active)
			{
				m_active = true;
				m_verticesX.Clear();
				m_verticesY.Clear();
				eSelectionMode mode = SelectionModeFromState(state);
				if (mode == eSelectionMode.Replace)
				{
					document.Selection().Clear();
				}
				document.Selection().BeginOperation(mode, state.SelectionFeather());
				m_verticesX.Add(x);
				m_verticesY.Add(y);
				return false;
			}
			if (m_verticesX.Count >= MinimumVertices)
			{
				if (doubleClick)
				{
					Finalize(document, state);
					return false;
				}
				int deltaX = x - m_verticesX[0];
				int deltaY = y - m_verticesY[0];
				int distanceSquared = (deltaX * deltaX) + (deltaY * deltaY);
				if (distanceSquared <= m_closeRadius * m_closeRadius)
				{
					Finalize(document, state);
					return false;
				}
			}
			m_verticesX.Add(x);
			m_verticesY.Add(y);
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return false;
		}
	}
}
