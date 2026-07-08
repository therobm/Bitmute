using System;
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
		public bool m_smooth;

		public PathPoint()
		{
			m_smooth = false;
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
			m_smooth = false;
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
				m_hasControlOut = m_hasControlOut,
				m_smooth = m_smooth
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
				if (pt.m_x < minX)
				{
					minX = pt.m_x;
				}
				if (pt.m_y < minY)
				{
					minY = pt.m_y;
				}
				if (pt.m_x > maxX)
				{
					maxX = pt.m_x;
				}
				if (pt.m_y > maxY)
				{
					maxY = pt.m_y;
				}

				if (pt.m_hasControlIn)
				{
					if (pt.m_controlInX < minX)
					{
						minX = pt.m_controlInX;
					}
					if (pt.m_controlInY < minY)
					{
						minY = pt.m_controlInY;
					}
					if (pt.m_controlInX > maxX)
					{
						maxX = pt.m_controlInX;
					}
					if (pt.m_controlInY > maxY)
					{
						maxY = pt.m_controlInY;
					}
				}
				if (pt.m_hasControlOut)
				{
					if (pt.m_controlOutX < minX)
					{
						minX = pt.m_controlOutX;
					}
					if (pt.m_controlOutY < minY)
					{
						minY = pt.m_controlOutY;
					}
					if (pt.m_controlOutX > maxX)
					{
						maxX = pt.m_controlOutX;
					}
					if (pt.m_controlOutY > maxY)
					{
						maxY = pt.m_controlOutY;
					}
				}
			}

			return new SKRect(minX, minY, maxX, maxY);
		}

		public int HitAnchor(float x, float y, float radius)
		{
			int count = m_points.Count;
			int bestIndex = -1;
			float bestDistance = radius;
			for (int i = 0; i < count; i++)
			{
				PathPoint pt = m_points[i];
				float dx = pt.m_x - x;
				float dy = pt.m_y - y;
				float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
				if (distance <= bestDistance)
				{
					bestDistance = distance;
					bestIndex = i;
				}
			}
			return bestIndex;
		}

		public bool HitHandleIn(int index, float x, float y, float radius)
		{
			if (index < 0 || index >= m_points.Count)
			{
				return false;
			}
			PathPoint pt = m_points[index];
			if (!pt.m_hasControlIn)
			{
				return false;
			}
			float dx = pt.m_controlInX - x;
			float dy = pt.m_controlInY - y;
			float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
			if (distance <= radius)
			{
				return true;
			}
			return false;
		}

		public bool HitHandleOut(int index, float x, float y, float radius)
		{
			if (index < 0 || index >= m_points.Count)
			{
				return false;
			}
			PathPoint pt = m_points[index];
			if (!pt.m_hasControlOut)
			{
				return false;
			}
			float dx = pt.m_controlOutX - x;
			float dy = pt.m_controlOutY - y;
			float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
			if (distance <= radius)
			{
				return true;
			}
			return false;
		}

		public bool HitSegment(float x, float y, float radius, out int segmentIndex, out float t)
		{
			segmentIndex = -1;
			t = 0.0f;
			int count = m_points.Count;
			if (count < 2)
			{
				return false;
			}
			int steps = 24;
			float bestDistance = radius;
			int bestSegment = -1;
			float bestT = 0.0f;
			int lastStart = count - 1;
			for (int i = 0; i < lastStart; i++)
			{
				PathPoint prev = m_points[i];
				PathPoint curr = m_points[i + 1];
				float localT;
				float localDistance;
				ClosestOnSegment(prev, curr, x, y, steps, out localT, out localDistance);
				if (localDistance <= bestDistance)
				{
					bestDistance = localDistance;
					bestSegment = i;
					bestT = localT;
				}
			}
			if (m_isClosed && count >= 2)
			{
				PathPoint prev = m_points[count - 1];
				PathPoint curr = m_points[0];
				float localT;
				float localDistance;
				ClosestOnSegment(prev, curr, x, y, steps, out localT, out localDistance);
				if (localDistance <= bestDistance)
				{
					bestDistance = localDistance;
					bestSegment = count - 1;
					bestT = localT;
				}
			}
			if (bestSegment < 0)
			{
				return false;
			}
			segmentIndex = bestSegment;
			t = bestT;
			return true;
		}

		private void ClosestOnSegment(PathPoint prev, PathPoint curr, float x, float y, int steps, out float bestT, out float bestDistance)
		{
			bestT = 0.0f;
			bestDistance = float.MaxValue;
			for (int step = 0; step <= steps; step++)
			{
				float sampleT = (float)step / (float)steps;
				float sampleX;
				float sampleY;
				SampleSegment(prev, curr, sampleT, out sampleX, out sampleY);
				float dx = sampleX - x;
				float dy = sampleY - y;
				float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestT = sampleT;
				}
			}
		}

		private void SampleSegment(PathPoint prev, PathPoint curr, float t, out float outX, out float outY)
		{
			if (prev.m_hasControlOut && curr.m_hasControlIn)
			{
				float mt = 1.0f - t;
				float a = mt * mt * mt;
				float b = 3.0f * mt * mt * t;
				float c = 3.0f * mt * t * t;
				float d = t * t * t;
				outX = (a * prev.m_x) + (b * prev.m_controlOutX) + (c * curr.m_controlInX) + (d * curr.m_x);
				outY = (a * prev.m_y) + (b * prev.m_controlOutY) + (c * curr.m_controlInY) + (d * curr.m_y);
				return;
			}
			if (prev.m_hasControlOut)
			{
				float mt = 1.0f - t;
				float a = mt * mt;
				float b = 2.0f * mt * t;
				float c = t * t;
				outX = (a * prev.m_x) + (b * prev.m_controlOutX) + (c * curr.m_x);
				outY = (a * prev.m_y) + (b * prev.m_controlOutY) + (c * curr.m_y);
				return;
			}
			if (curr.m_hasControlIn)
			{
				float mt = 1.0f - t;
				float a = mt * mt;
				float b = 2.0f * mt * t;
				float c = t * t;
				outX = (a * prev.m_x) + (b * curr.m_controlInX) + (c * curr.m_x);
				outY = (a * prev.m_y) + (b * curr.m_controlInY) + (c * curr.m_y);
				return;
			}
			outX = prev.m_x + ((curr.m_x - prev.m_x) * t);
			outY = prev.m_y + ((curr.m_y - prev.m_y) * t);
		}

		public void InsertAnchorOnSegment(int segmentIndex, float t)
		{
			int count = m_points.Count;
			if (segmentIndex < 0 || segmentIndex >= count)
			{
				return;
			}
			int startIndex = segmentIndex;
			int endIndex = segmentIndex + 1;
			bool closingSegment = false;
			if (endIndex >= count)
			{
				if (!m_isClosed)
				{
					return;
				}
				endIndex = 0;
				closingSegment = true;
			}
			PathPoint prev = m_points[startIndex];
			PathPoint curr = m_points[endIndex];
			if (prev.m_hasControlOut && curr.m_hasControlIn)
			{
				float p0x = prev.m_x;
				float p0y = prev.m_y;
				float p1x = prev.m_controlOutX;
				float p1y = prev.m_controlOutY;
				float p2x = curr.m_controlInX;
				float p2y = curr.m_controlInY;
				float p3x = curr.m_x;
				float p3y = curr.m_y;
				float ax = Lerp(p0x, p1x, t);
				float ay = Lerp(p0y, p1y, t);
				float bx = Lerp(p1x, p2x, t);
				float by = Lerp(p1y, p2y, t);
				float cx = Lerp(p2x, p3x, t);
				float cy = Lerp(p2y, p3y, t);
				float dx = Lerp(ax, bx, t);
				float dy = Lerp(ay, by, t);
				float ex = Lerp(bx, cx, t);
				float ey = Lerp(by, cy, t);
				float fx = Lerp(dx, ex, t);
				float fy = Lerp(dy, ey, t);
				prev.m_controlOutX = ax;
				prev.m_controlOutY = ay;
				prev.m_hasControlOut = true;
				curr.m_controlInX = cx;
				curr.m_controlInY = cy;
				curr.m_hasControlIn = true;
				PathPoint inserted = new PathPoint(fx, fy);
				inserted.m_smooth = true;
				inserted.m_hasControlIn = true;
				inserted.m_hasControlOut = true;
				inserted.m_controlInX = dx;
				inserted.m_controlInY = dy;
				inserted.m_controlOutX = ex;
				inserted.m_controlOutY = ey;
				InsertPoint(startIndex, closingSegment, inserted);
				return;
			}
			if (prev.m_hasControlOut || curr.m_hasControlIn)
			{
				float p0x = prev.m_x;
				float p0y = prev.m_y;
				float p2x = curr.m_x;
				float p2y = curr.m_y;
				float p1x;
				float p1y;
				if (prev.m_hasControlOut)
				{
					p1x = prev.m_controlOutX;
					p1y = prev.m_controlOutY;
				}
				else
				{
					p1x = curr.m_controlInX;
					p1y = curr.m_controlInY;
				}
				float ax = Lerp(p0x, p1x, t);
				float ay = Lerp(p0y, p1y, t);
				float bx = Lerp(p1x, p2x, t);
				float by = Lerp(p1y, p2y, t);
				float fx = Lerp(ax, bx, t);
				float fy = Lerp(ay, by, t);
				if (prev.m_hasControlOut)
				{
					prev.m_controlOutX = ax;
					prev.m_controlOutY = ay;
					prev.m_hasControlOut = true;
				}
				if (curr.m_hasControlIn)
				{
					curr.m_controlInX = bx;
					curr.m_controlInY = by;
					curr.m_hasControlIn = true;
				}
				PathPoint inserted = new PathPoint(fx, fy);
				inserted.m_smooth = true;
				inserted.m_hasControlIn = true;
				inserted.m_hasControlOut = true;
				inserted.m_controlInX = ax;
				inserted.m_controlInY = ay;
				inserted.m_controlOutX = bx;
				inserted.m_controlOutY = by;
				InsertPoint(startIndex, closingSegment, inserted);
				return;
			}
			float lerpX = Lerp(prev.m_x, curr.m_x, t);
			float lerpY = Lerp(prev.m_y, curr.m_y, t);
			PathPoint corner = new PathPoint(lerpX, lerpY);
			corner.m_smooth = false;
			corner.m_hasControlIn = false;
			corner.m_hasControlOut = false;
			InsertPoint(startIndex, closingSegment, corner);
		}

		private void InsertPoint(int startIndex, bool closingSegment, PathPoint inserted)
		{
			if (closingSegment)
			{
				m_points.Add(inserted);
			}
			else
			{
				m_points.Insert(startIndex + 1, inserted);
			}
		}

		private static float Lerp(float a, float b, float t)
		{
			return a + ((b - a) * t);
		}

		public void RemoveAnchorAt(int index)
		{
			if (index < 0 || index >= m_points.Count)
			{
				return;
			}
			m_points.RemoveAt(index);
		}

		public void MoveAnchor(int index, float dx, float dy)
		{
			if (index < 0 || index >= m_points.Count)
			{
				return;
			}
			PathPoint pt = m_points[index];
			pt.m_x = pt.m_x + dx;
			pt.m_y = pt.m_y + dy;
			pt.m_controlInX = pt.m_controlInX + dx;
			pt.m_controlInY = pt.m_controlInY + dy;
			pt.m_controlOutX = pt.m_controlOutX + dx;
			pt.m_controlOutY = pt.m_controlOutY + dy;
		}

		public void MoveHandleOut(int index, float x, float y)
		{
			if (index < 0 || index >= m_points.Count)
			{
				return;
			}
			PathPoint pt = m_points[index];
			pt.m_controlOutX = x;
			pt.m_controlOutY = y;
			pt.m_hasControlOut = true;
			if (pt.m_smooth)
			{
				pt.m_controlInX = (pt.m_x * 2.0f) - x;
				pt.m_controlInY = (pt.m_y * 2.0f) - y;
				pt.m_hasControlIn = true;
			}
		}

		public void MoveHandleIn(int index, float x, float y)
		{
			if (index < 0 || index >= m_points.Count)
			{
				return;
			}
			PathPoint pt = m_points[index];
			pt.m_controlInX = x;
			pt.m_controlInY = y;
			pt.m_hasControlIn = true;
			if (pt.m_smooth)
			{
				pt.m_controlOutX = (pt.m_x * 2.0f) - x;
				pt.m_controlOutY = (pt.m_y * 2.0f) - y;
				pt.m_hasControlOut = true;
			}
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