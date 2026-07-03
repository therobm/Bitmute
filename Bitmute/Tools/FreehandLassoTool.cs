using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class FreehandLassoTool : Tool
	{
		private const int MinimumVertices = 3;
		private const int MinimumDistinctPoints = 3;
		private const double MinimumArea = 4.0;

		private List<int> m_verticesX;
		private List<int> m_verticesY;
		private bool m_active;

		public FreehandLassoTool()
		{
			m_verticesX = new List<int>();
			m_verticesY = new List<int>();
			m_active = false;
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public void Reset()
		{
			m_verticesX.Clear();
			m_verticesY.Clear();
			m_active = false;
		}

		public bool HasPreview()
		{
			return m_active && m_verticesX.Count > 0;
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

		private void AppendPoint(int x, int y)
		{
			int count = m_verticesX.Count;
			if (count > 0)
			{
				if (m_verticesX[count - 1] == x && m_verticesY[count - 1] == y)
				{
					return;
				}
			}
			m_verticesX.Add(x);
			m_verticesY.Add(y);
		}

		private int DistinctPointCount()
		{
			int count = m_verticesX.Count;
			int distinct = 0;
			for (int i = 0; i < count; i++)
			{
				bool seen = false;
				for (int j = 0; j < i; j++)
				{
					if (m_verticesX[j] == m_verticesX[i] && m_verticesY[j] == m_verticesY[i])
					{
						seen = true;
						break;
					}
				}
				if (!seen)
				{
					distinct = distinct + 1;
				}
			}
			return distinct;
		}

		private double PolygonArea()
		{
			int count = m_verticesX.Count;
			if (count < MinimumVertices)
			{
				return 0.0;
			}
			double sum = 0.0;
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
				sum = sum + ((ax * by) - (bx * ay));
			}
			double area = sum / 2.0;
			if (area < 0.0)
			{
				area = -area;
			}
			return area;
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

		private void CommitSelection(Document document)
		{
			int count = m_verticesX.Count;
			if (count < MinimumVertices)
			{
				document.Selection().ApplyRect(SKRectI.Empty);
				return;
			}
			if (DistinctPointCount() < MinimumDistinctPoints)
			{
				document.Selection().ApplyRect(SKRectI.Empty);
				return;
			}
			if (PolygonArea() < MinimumArea)
			{
				document.Selection().ApplyRect(SKRectI.Empty);
				return;
			}
			int documentWidth = document.Width();
			int documentHeight = document.Height();
			byte[] mask = new byte[documentWidth * documentHeight];

			int minPolyY = documentHeight;
			int maxPolyY = -1;
			for (int i = 0; i < count; i++)
			{
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
			int scanTop = minPolyY;
			if (scanTop < 0)
			{
				scanTop = 0;
			}
			int scanBottom = maxPolyY;
			if (scanBottom > documentHeight - 1)
			{
				scanBottom = documentHeight - 1;
			}

			double[] crossings = new double[count];
			for (int pixelY = scanTop; pixelY <= scanBottom; pixelY++)
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
					if (spanLeft < 0)
					{
						spanLeft = 0;
					}
					if (spanRight > documentWidth - 1)
					{
						spanRight = documentWidth - 1;
					}
					int rowStart = pixelY * documentWidth;
					for (int pixelX = spanLeft; pixelX <= spanRight; pixelX++)
					{
						mask[rowStart + pixelX] = 255;
					}
				}
			}

			document.Selection().ApplyMask(mask);
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			m_active = true;
			m_verticesX.Clear();
			m_verticesY.Clear();
			eSelectionMode mode = SelectionModeFromState(state);
			if (mode == eSelectionMode.Replace)
			{
				document.Selection().Clear();
			}
			document.Selection().BeginOperation(mode);
			AppendPoint(x, y);
			return true;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				return false;
			}
			AppendPoint(x, y);
			return true;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				m_hasLast = false;
				return;
			}
			AppendPoint(x, y);
			CommitSelection(document);
			m_active = false;
			m_verticesX.Clear();
			m_verticesY.Clear();
			m_hasLast = false;
		}
	}
}
