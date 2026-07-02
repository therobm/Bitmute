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

		private List<int> m_verticesX;
		private List<int> m_verticesY;
		private bool m_active;

		public LassoTool()
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
				document.Selection().Clear();
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
			int selMinX = documentWidth;
			int selMinY = documentHeight;
			int selMaxX = -1;
			int selMaxY = -1;
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
						if (pixelX < selMinX)
						{
							selMinX = pixelX;
						}
						if (pixelX > selMaxX)
						{
							selMaxX = pixelX;
						}
						if (pixelY < selMinY)
						{
							selMinY = pixelY;
						}
						if (pixelY > selMaxY)
						{
							selMaxY = pixelY;
						}
					}
				}
			}

			if (selMaxX < 0)
			{
				document.Selection().Clear();
				return;
			}
			SKRectI bounds = new SKRectI(selMinX, selMinY, selMaxX + 1, selMaxY + 1);
			document.Selection().SelectMask(mask, bounds);
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				m_active = true;
				m_verticesX.Clear();
				m_verticesY.Clear();
				document.Selection().Clear();
				m_verticesX.Add(x);
				m_verticesY.Add(y);
				return false;
			}
			if (m_verticesX.Count >= MinimumVertices)
			{
				int deltaX = x - m_verticesX[0];
				int deltaY = y - m_verticesY[0];
				int distanceSquared = (deltaX * deltaX) + (deltaY * deltaY);
				if (distanceSquared <= CloseThreshold * CloseThreshold)
				{
					CommitSelection(document);
					m_active = false;
					m_verticesX.Clear();
					m_verticesY.Clear();
					return false;
				}
			}
			m_verticesX.Add(x);
			m_verticesY.Add(y);
			if (m_verticesX.Count >= MinimumVertices)
			{
				CommitSelection(document);
			}
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return false;
		}
	}
}
