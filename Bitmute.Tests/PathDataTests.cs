using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class PathDataTests
	{
		private static int s_failures;

		private static void Check(bool condition, string name)
		{
			if (condition)
			{
				Console.WriteLine("PASS " + name);
			}
			else
			{
				s_failures = s_failures + 1;
				Console.WriteLine("FAIL " + name);
			}
		}

		public static int RunAll()
		{
			TestEmptyPath();
			TestSinglePoint();
			TestStraightLine();
			TestQuadraticCurve();
			TestCubicCurve();
			TestClosedPath();
			TestBounds();
			TestClone();
			TestPathPointClone();
			return s_failures;
		}

		private static void TestEmptyPath()
		{
			PathData path = new PathData("empty");
			SKPath skPath = path.ToSKPath();
			Check(skPath.IsEmpty, "empty path produces empty SKPath");
			skPath.Dispose();

			SKRect bounds = path.Bounds();
			Check(bounds.IsEmpty, "empty path has empty bounds");
		}

		private static void TestSinglePoint()
		{
			PathData path = new PathData("single");
			path.m_points.Add(new PathPoint(10.0f, 20.0f));
			SKPath skPath = path.ToSKPath();
			Check(!skPath.IsEmpty, "single-point path is not empty");
			skPath.Dispose();

			SKRect bounds = path.Bounds();
			Check(bounds.Left == 10.0f && bounds.Top == 20.0f && bounds.Right == 10.0f && bounds.Bottom == 20.0f,
				"single-point bounds are correct");
		}

		private static void TestStraightLine()
		{
			PathData path = new PathData("line");
			path.m_points.Add(new PathPoint(0.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 100.0f));

			SKPath skPath = path.ToSKPath();
			Check(!skPath.IsEmpty, "straight-line path is not empty");
			skPath.Dispose();

			SKRect bounds = path.Bounds();
			Check(bounds.Left == 0.0f && bounds.Top == 0.0f && bounds.Right == 100.0f && bounds.Bottom == 100.0f,
				"straight-line bounds are correct");
		}

		private static void TestQuadraticCurve()
		{
			PathData path = new PathData("quad");
			PathPoint p0 = new PathPoint(0.0f, 0.0f);
			p0.m_hasControlOut = true;
			p0.m_controlOutX = 50.0f;
			p0.m_controlOutY = 0.0f;
			path.m_points.Add(p0);

			PathPoint p1 = new PathPoint(100.0f, 100.0f);
			path.m_points.Add(p1);

			SKPath skPath = path.ToSKPath();
			Check(!skPath.IsEmpty, "quadratic curve path is not empty");
			skPath.Dispose();

			SKRect bounds = path.Bounds();
			Check(bounds.Left == 0.0f && bounds.Top == 0.0f && bounds.Right == 100.0f && bounds.Bottom == 100.0f,
				"quadratic bounds include control point");
		}

		private static void TestCubicCurve()
		{
			PathData path = new PathData("cubic");
			PathPoint p0 = new PathPoint(0.0f, 0.0f);
			p0.m_hasControlOut = true;
			p0.m_controlOutX = 30.0f;
			p0.m_controlOutY = 0.0f;
			path.m_points.Add(p0);

			PathPoint p1 = new PathPoint(100.0f, 100.0f);
			p1.m_hasControlIn = true;
			p1.m_controlInX = 70.0f;
			p1.m_controlInY = 100.0f;
			path.m_points.Add(p1);

			SKPath skPath = path.ToSKPath();
			Check(!skPath.IsEmpty, "cubic curve path is not empty");
			skPath.Dispose();

			SKRect bounds = path.Bounds();
			Check(bounds.Left == 0.0f && bounds.Top == 0.0f && bounds.Right == 100.0f && bounds.Bottom == 100.0f,
				"cubic bounds include control points");
		}

		private static void TestClosedPath()
		{
			PathData path = new PathData("closed");
			path.m_isClosed = true;
			path.m_points.Add(new PathPoint(0.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 100.0f));
			path.m_points.Add(new PathPoint(0.0f, 100.0f));

			SKPath skPath = path.ToSKPath();
			Check(!skPath.IsEmpty, "closed path is not empty");
			skPath.Dispose();
		}

		private static void TestBounds()
		{
			PathData path = new PathData("bounds");
			PathPoint p0 = new PathPoint(-50.0f, -50.0f);
			p0.m_hasControlOut = true;
			p0.m_controlOutX = -100.0f;
			p0.m_controlOutY = -100.0f;
			path.m_points.Add(p0);

			PathPoint p1 = new PathPoint(50.0f, 50.0f);
			p1.m_hasControlIn = true;
			p1.m_controlInX = 100.0f;
			p1.m_controlInY = 100.0f;
			path.m_points.Add(p1);

			SKRect bounds = path.Bounds();
			Check(bounds.Left == -100.0f && bounds.Top == -100.0f && bounds.Right == 100.0f && bounds.Bottom == 100.0f,
				"bounds extend to control points");
		}

		private static void TestClone()
		{
			PathData path = new PathData("original");
			path.m_isClosed = true;
			path.m_strokeColor = SKColors.Red;
			path.m_points.Add(new PathPoint(10.0f, 20.0f));
			path.m_points.Add(new PathPoint(30.0f, 40.0f));

			PathData clone = path.Clone();
			Check(clone.m_name == "original", "clone name matches");
			Check(clone.m_isClosed == true, "clone isClosed matches");
			Check(clone.m_strokeColor == SKColors.Red, "clone strokeColor matches");
			Check(clone.m_points.Count == 2, "clone has same point count");
			Check(clone.m_points[0].m_x == 10.0f && clone.m_points[0].m_y == 20.0f, "clone point 0 matches");
			Check(clone.m_points[1].m_x == 30.0f && clone.m_points[1].m_y == 40.0f, "clone point 1 matches");

			clone.m_points[0].m_x = 99.0f;
			Check(path.m_points[0].m_x == 10.0f, "original unaffected by clone mutation");
		}

		private static void TestPathPointClone()
		{
			PathPoint pt = new PathPoint(5.0f, 10.0f);
			pt.m_hasControlIn = true;
			pt.m_hasControlOut = true;
			pt.m_controlInX = 0.0f;
			pt.m_controlInY = 5.0f;
			pt.m_controlOutX = 10.0f;
			pt.m_controlOutY = 15.0f;

			PathPoint clone = pt.Clone();
			Check(clone.m_x == 5.0f, "clone x matches");
			Check(clone.m_y == 10.0f, "clone y matches");
			Check(clone.m_hasControlIn == true, "clone hasControlIn matches");
			Check(clone.m_hasControlOut == true, "clone hasControlOut matches");
			Check(clone.m_controlInX == 0.0f, "clone controlInX matches");
			Check(clone.m_controlInY == 5.0f, "clone controlInY matches");
			Check(clone.m_controlOutX == 10.0f, "clone controlOutX matches");
			Check(clone.m_controlOutY == 15.0f, "clone controlOutY matches");

			clone.m_x = 99.0f;
			Check(pt.m_x == 5.0f, "original point unaffected by clone mutation");
		}
	}
}