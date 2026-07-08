using System;
using System.IO;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Storage;

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
			TestBitmuteRoundTrip();
			TestHitAnchor();
			TestHitHandles();
			TestHitSegmentMidpoint();
			TestInsertAnchorOnStraightLine();
			TestRemoveAnchor();
			TestMoveHandleOutSmoothMirrors();
			TestMoveHandleOutNonSmooth();
			TestMoveAnchorTranslatesAll();
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

		private static void TestBitmuteRoundTrip()
		{
			string directory = Path.Combine(Path.GetTempPath(), "bitmute_path_persist");
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "path_roundtrip.bitmute");

			Document doc = new Document("paths", 32, 24);

			PathData closed = new PathData("Closed");
			closed.m_isClosed = true;
			closed.m_strokeColor = new SKColor(200, 100, 50, 255);
			PathPoint c0 = new PathPoint(10.0f, 10.0f);
			c0.m_smooth = true;
			c0.m_hasControlIn = true;
			c0.m_hasControlOut = true;
			c0.m_controlInX = 5.0f;
			c0.m_controlInY = 10.0f;
			c0.m_controlOutX = 15.0f;
			c0.m_controlOutY = 10.0f;
			closed.m_points.Add(c0);
			PathPoint c1 = new PathPoint(20.0f, 20.0f);
			c1.m_smooth = true;
			c1.m_hasControlIn = true;
			c1.m_hasControlOut = true;
			c1.m_controlInX = 18.0f;
			c1.m_controlInY = 22.0f;
			c1.m_controlOutX = 22.0f;
			c1.m_controlOutY = 18.0f;
			closed.m_points.Add(c1);
			doc.AddPath(closed);

			PathData open = new PathData("Open");
			open.m_isClosed = false;
			open.m_strokeColor = new SKColor(0, 128, 255, 200);
			PathPoint o0 = new PathPoint(1.0f, 2.0f);
			o0.m_smooth = false;
			open.m_points.Add(o0);
			PathPoint o1 = new PathPoint(3.0f, 4.0f);
			o1.m_smooth = false;
			open.m_points.Add(o1);
			PathPoint o2 = new PathPoint(5.0f, 6.0f);
			o2.m_smooth = false;
			open.m_points.Add(o2);
			doc.AddPath(open);

			bool wrote = BitmuteFile.Write(path, doc);
			Check(wrote, "path persist write");
			Document back = BitmuteFile.Read(path);
			if (back == null)
			{
				Check(false, "path persist read returned null");
				File.Delete(path);
				return;
			}

			Check(back.Paths().Count == 2, "path persist path count survives");
			if (back.Paths().Count != 2)
			{
				File.Delete(path);
				return;
			}

			PathData backClosed = back.Paths()[0];
			Check(backClosed.m_isClosed == true, "path persist closed flag survives");
			Check(backClosed.m_name == "Closed", "path persist closed name survives");
			Check(backClosed.m_strokeColor == new SKColor(200, 100, 50, 255), "path persist closed stroke color survives");
			Check(backClosed.m_points.Count == 2, "path persist closed point count survives");

			PathPoint backC0 = backClosed.m_points[0];
			Check(backC0.m_x == 10.0f && backC0.m_y == 10.0f, "path persist closed point 0 position survives");
			Check(backC0.m_hasControlIn == true && backC0.m_hasControlOut == true, "path persist closed point 0 handle flags survive");
			Check(backC0.m_controlInX == 5.0f && backC0.m_controlInY == 10.0f, "path persist closed point 0 control in survives");
			Check(backC0.m_controlOutX == 15.0f && backC0.m_controlOutY == 10.0f, "path persist closed point 0 control out survives");
			Check(backC0.m_smooth == true, "path persist closed point 0 smooth survives");

			PathData backOpen = back.Paths()[1];
			Check(backOpen.m_isClosed == false, "path persist open flag survives");
			Check(backOpen.m_name == "Open", "path persist open name survives");
			Check(backOpen.m_strokeColor == new SKColor(0, 128, 255, 200), "path persist open stroke color survives");
			Check(backOpen.m_points.Count == 3, "path persist open point count survives");

			PathPoint backO0 = backOpen.m_points[0];
			Check(backO0.m_x == 1.0f && backO0.m_y == 2.0f, "path persist open point 0 position survives");
			Check(backO0.m_hasControlIn == false && backO0.m_hasControlOut == false, "path persist open point 0 handle flags survive");
			Check(backO0.m_smooth == false, "path persist open point 0 smooth survives");

			File.Delete(path);
		}

		private static void TestHitAnchor()
		{
			PathData path = new PathData("hitanchor");
			path.m_points.Add(new PathPoint(0.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 100.0f));

			int hit = path.HitAnchor(102.0f, 2.0f, 5.0f);
			Check(hit == 1, "HitAnchor hits nearest anchor within radius");

			int miss = path.HitAnchor(50.0f, 50.0f, 5.0f);
			Check(miss == -1, "HitAnchor misses when outside radius");
		}

		private static void TestHitHandles()
		{
			PathData path = new PathData("hithandles");
			PathPoint p0 = new PathPoint(0.0f, 0.0f);
			p0.m_hasControlOut = true;
			p0.m_controlOutX = 30.0f;
			p0.m_controlOutY = 0.0f;
			path.m_points.Add(p0);

			PathPoint p1 = new PathPoint(100.0f, 0.0f);
			p1.m_hasControlIn = true;
			p1.m_controlInX = 70.0f;
			p1.m_controlInY = 0.0f;
			path.m_points.Add(p1);

			Check(path.HitHandleOut(0, 31.0f, 1.0f, 5.0f), "HitHandleOut detects out handle");
			Check(!path.HitHandleOut(1, 31.0f, 1.0f, 5.0f), "HitHandleOut misses on point without out handle");
			Check(path.HitHandleIn(1, 71.0f, 1.0f, 5.0f), "HitHandleIn detects in handle");
			Check(!path.HitHandleIn(0, 71.0f, 1.0f, 5.0f), "HitHandleIn misses on point without in handle");
		}

		private static void TestHitSegmentMidpoint()
		{
			PathData path = new PathData("hitsegment");
			path.m_points.Add(new PathPoint(0.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 0.0f));

			int segmentIndex;
			float t;
			bool hit = path.HitSegment(50.0f, 0.0f, 2.0f, out segmentIndex, out t);
			Check(hit, "HitSegment finds a straight segment midpoint");
			Check(segmentIndex == 0, "HitSegment reports correct segment index");
			Check(t >= 0.45f && t <= 0.55f, "HitSegment reports t near 0.5 at midpoint");
		}

		private static void TestInsertAnchorOnStraightLine()
		{
			PathData path = new PathData("insert");
			path.m_points.Add(new PathPoint(0.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 0.0f));

			path.InsertAnchorOnSegment(0, 0.5f);
			Check(path.m_points.Count == 3, "InsertAnchorOnSegment raises point count by 1");
			PathPoint inserted = path.m_points[1];
			Check(inserted.m_x == 50.0f && inserted.m_y == 0.0f, "InsertAnchorOnSegment places new point at lerp location");
			Check(inserted.m_smooth == false && !inserted.m_hasControlIn && !inserted.m_hasControlOut, "InsertAnchorOnSegment on straight line is a plain corner");
		}

		private static void TestRemoveAnchor()
		{
			PathData path = new PathData("remove");
			path.m_points.Add(new PathPoint(0.0f, 0.0f));
			path.m_points.Add(new PathPoint(50.0f, 0.0f));
			path.m_points.Add(new PathPoint(100.0f, 0.0f));

			path.RemoveAnchorAt(1);
			Check(path.m_points.Count == 2, "RemoveAnchorAt lowers point count by 1");
			Check(path.m_points[1].m_x == 100.0f, "RemoveAnchorAt removes the correct anchor");
		}

		private static void TestMoveHandleOutSmoothMirrors()
		{
			PathData path = new PathData("smoothmirror");
			PathPoint p0 = new PathPoint(50.0f, 50.0f);
			p0.m_smooth = true;
			path.m_points.Add(p0);

			path.MoveHandleOut(0, 70.0f, 60.0f);
			PathPoint pt = path.m_points[0];
			Check(pt.m_controlOutX == 70.0f && pt.m_controlOutY == 60.0f, "MoveHandleOut sets out handle");
			Check(pt.m_hasControlOut == true, "MoveHandleOut flags out handle");
			Check(pt.m_controlInX == 30.0f && pt.m_controlInY == 40.0f, "MoveHandleOut mirrors in handle on smooth point");
			Check(pt.m_hasControlIn == true, "MoveHandleOut flags in handle on smooth point");
		}

		private static void TestMoveHandleOutNonSmooth()
		{
			PathData path = new PathData("nonsmooth");
			PathPoint p0 = new PathPoint(50.0f, 50.0f);
			p0.m_smooth = false;
			p0.m_hasControlIn = true;
			p0.m_controlInX = 40.0f;
			p0.m_controlInY = 45.0f;
			path.m_points.Add(p0);

			path.MoveHandleOut(0, 70.0f, 60.0f);
			PathPoint pt = path.m_points[0];
			Check(pt.m_controlOutX == 70.0f && pt.m_controlOutY == 60.0f, "MoveHandleOut sets out handle on non-smooth point");
			Check(pt.m_controlInX == 40.0f && pt.m_controlInY == 45.0f, "MoveHandleOut leaves in handle unchanged on non-smooth point");
		}

		private static void TestMoveAnchorTranslatesAll()
		{
			PathData path = new PathData("moveanchor");
			PathPoint p0 = new PathPoint(50.0f, 50.0f);
			p0.m_hasControlIn = true;
			p0.m_hasControlOut = true;
			p0.m_controlInX = 40.0f;
			p0.m_controlInY = 50.0f;
			p0.m_controlOutX = 60.0f;
			p0.m_controlOutY = 50.0f;
			path.m_points.Add(p0);

			path.MoveAnchor(0, 10.0f, 20.0f);
			PathPoint pt = path.m_points[0];
			Check(pt.m_x == 60.0f && pt.m_y == 70.0f, "MoveAnchor translates the anchor");
			Check(pt.m_controlInX == 50.0f && pt.m_controlInY == 70.0f, "MoveAnchor translates the in handle");
			Check(pt.m_controlOutX == 70.0f && pt.m_controlOutY == 70.0f, "MoveAnchor translates the out handle");
		}
	}
}