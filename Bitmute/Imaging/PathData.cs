using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public class PathPoint
	{
		public float m_x;
		public float m_y;
		public float m_controlInX;
		public float m_controlInY;
		public float m_controlOutX;
		public float m_controlOutY;
		public bool m_hasControlIn;
		public bool m_hasControlOut;

		public PathPoint()
		{
		}

		public PathPoint(float x, float y)
		{
			m_x = x;
			m_y = y;
			m_controlInX = x;
			m_controlInY = y;
			m_controlOutX = x;
			m_controlOutY = y;
			m_hasControlIn = false;
			m_hasControlOut = false;
		}

		public PathPoint Clone()
		{
			return new PathPoint
			{
				m_x = m_x,
				m_y = m_y,
				m_controlInX = m_controlInX,
				m_controlInY = m_controlInY,
				m_controlOutX = m_controlOutX,
				m_controlOutY = m_controlOutY,
				m_hasControlIn = m_hasControlIn,
				m_hasControlOut = m_hasControlOut
			};
		}
	}

	public class PathData
	{
		public string m_name;
		public List<PathPoint> m_points;
		public bool m_isClosed;
		public SKColor m_strokeColor;

		public PathData()
		{
			m_points = new List<PathPoint>();
			m_strokeColor = SKColors.Black;
		}

		public PathData(string name)
		{
			m_name = name;
			m_points = new List<PathPoint>();
			m_isClosed = false;
			m_strokeColor = SKColors.Black;
		}

		public SKPath ToSKPath()
		{
			int count = m_points.Count;
			if (count == 0)
			{
				return new SKPath();
			}

			SKPathBuilder builder = new SKPathBuilder();
			PathPoint first = m_points[0];
			builder.MoveTo(first.m_x, first.m_y);

			for (int i = 1; i < count; i++)
			{
				PathPoint prev = m_points[i - 1];
				PathPoint curr = m_points[i];

				if (prev.m_hasControlOut && curr.m_hasControlIn)
				{
					builder.CubicTo(prev.m_controlOutX, prev.m_controlOutY,
						curr.m_controlInX, curr.m_controlInY,
						curr.m_x, curr.m_y);
				}
				else if (prev.m_hasControlOut)
				{
					builder.QuadTo(prev.m_controlOutX, prev.m_controlOutY,
						curr.m_x, curr.m_y);
				}
				else if (curr.m_hasControlIn)
				{
					builder.QuadTo(curr.m_controlInX, curr.m_controlInY,
						curr.m_x, curr.m_y);
				}
				else
				{
					builder.LineTo(curr.m_x, curr.m_y);
				}
			}

			if (m_isClosed && count >= 2)
			{
				PathPoint last = m_points[count - 1];
				if (last.m_hasControlOut && first.m_hasControlIn)
				{
					builder.CubicTo(last.m_controlOutX, last.m_controlOutY,
						first.m_controlInX, first.m_controlInY,
						first.m_x, first.m_y);
				}
				else if (last.m_hasControlOut)
				{
					builder.QuadTo(last.m_controlOutX, last.m_controlOutY,
						first.m_x, first.m_y);
				}
				else if (first.m_hasControlIn)
				{
					builder.QuadTo(first.m_controlInX, first.m_controlInY,
						first.m_x, first.m_y);
				}
				builder.Close();
			}

			return builder.Snapshot();
		}

		public SKRect Bounds()
		{
			if (m_points.Count == 0)
			{
				return SKRect.Empty;
			}

			float minX = m_points[0].m_x;
			float minY = m_points[0].m_y;
			float maxX = minX;
			float maxY = minY;

			foreach (PathPoint pt in m_points)
			{
				if (pt.m_x < minX) minX = pt.m_x;
				if (pt.m_y < minY) minY = pt.m_y;
				if (pt.m_x > maxX) maxX = pt.m_x;
				if (pt.m_y > maxY) maxY = pt.m_y;

				if (pt.m_hasControlIn)
				{
					if (pt.m_controlInX < minX) minX = pt.m_controlInX;
					if (pt.m_controlInY < minY) minY = pt.m_controlInY;
					if (pt.m_controlInX > maxX) maxX = pt.m_controlInX;
					if (pt.m_controlInY > maxY) maxY = pt.m_controlInY;
				}
				if (pt.m_hasControlOut)
				{
					if (pt.m_controlOutX < minX) minX = pt.m_controlOutX;
					if (pt.m_controlOutY < minY) minY = pt.m_controlOutY;
					if (pt.m_controlOutX > maxX) maxX = pt.m_controlOutX;
					if (pt.m_controlOutY > maxY) maxY = pt.m_controlOutY;
				}
			}

			return new SKRect(minX, minY, maxX, maxY);
		}

		public PathData Clone()
		{
			PathData clone = new PathData(m_name);
			clone.m_isClosed = m_isClosed;
			clone.m_strokeColor = m_strokeColor;
			foreach (PathPoint pt in m_points)
			{
				clone.m_points.Add(pt.Clone());
			}
			return clone;
		}
	}
}