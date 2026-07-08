using System;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;

namespace Bitmute.Tests
{
	public static class PenToolTests
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

		private static PathData BuildStraightPath(int pointCount)
		{
			PathData path = new PathData("Seed");
			for (int i = 0; i < pointCount; i++)
			{
				path.m_points.Add(new PathPoint(10.0f + (i * 20.0f), 40.0f));
			}
			return path;
		}

		public static int RunAll()
		{
			TestPenDrawClose();
			TestPenFinishOpen();
			TestPenCancel();
			TestPenDelete();
			TestPenInsert();
			TestUndoRedo();
			TestDirectSelectionMove();
			TestDirectSelectionDelete();
			TestDirectSelectionSymmetricHandle();
			TestDirectSelectionAltBreak();
			return s_failures;
		}

		private static void TestPenDrawClose()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PenTool pen = new PenTool();
			pen.OnPressed(document, 20, 20, state);
			pen.OnPressed(document, 80, 20, state);
			pen.OnPressed(document, 80, 80, state);
			pen.OnPressed(document, 21, 21, state);
			Check(document.Paths().Count == 1, "pen close commits one path");
			if (document.Paths().Count != 1)
			{
				return;
			}
			PathData path = document.Paths()[0];
			Check(path.m_isClosed, "pen close marks path closed");
			Check(path.m_points.Count == 3, "pen close path has three points");
		}

		private static void TestPenFinishOpen()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PenTool pen = new PenTool();
			pen.OnPressed(document, 20, 20, state);
			pen.OnPressed(document, 60, 20, state);
			pen.OnPressed(document, 100, 20, state);
			pen.FinishPath(document);
			Check(document.Paths().Count == 1, "pen finish commits one path");
			if (document.Paths().Count != 1)
			{
				return;
			}
			PathData path = document.Paths()[0];
			Check(!path.m_isClosed, "pen finish leaves path open");
			Check(path.m_points.Count == 3, "pen finish path has three points");
		}

		private static void TestPenCancel()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PenTool pen = new PenTool();
			pen.OnPressed(document, 20, 20, state);
			pen.OnPressed(document, 60, 20, state);
			pen.CancelPath();
			Check(document.Paths().Count == 0, "pen cancel adds no path");
			Check(!pen.HasActivePath(), "pen cancel clears active path");
		}

		private static void TestPenDelete()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PathData seed = BuildStraightPath(4);
			document.AddPath(seed);
			int before = seed.m_points.Count;
			PenTool pen = new PenTool();
			pen.OnPressed(document, 30, 40, state);
			Check(document.Paths().Count == 1, "pen delete keeps the path");
			if (document.Paths().Count != 1)
			{
				return;
			}
			Check(document.Paths()[0].m_points.Count == before - 1, "pen delete removes one anchor");
		}

		private static void TestPenInsert()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PathData seed = new PathData("Seed");
			seed.m_points.Add(new PathPoint(20.0f, 40.0f));
			seed.m_points.Add(new PathPoint(100.0f, 40.0f));
			document.AddPath(seed);
			PenTool pen = new PenTool();
			pen.OnPressed(document, 60, 40, state);
			Check(document.Paths().Count == 1, "pen insert keeps the path");
			if (document.Paths().Count != 1)
			{
				return;
			}
			Check(document.Paths()[0].m_points.Count == 3, "pen insert adds one anchor");
		}

		private static void TestUndoRedo()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PenTool pen = new PenTool();
			pen.OnPressed(document, 20, 20, state);
			pen.OnPressed(document, 80, 20, state);
			pen.OnPressed(document, 80, 80, state);
			pen.OnPressed(document, 21, 21, state);
			Check(document.Paths().Count == 1, "undo/redo setup commits one path");
			bool undone = document.Undo();
			Check(undone, "undo available after pen close");
			Check(document.Paths().Count == 0, "undo drops the committed path");
			bool redone = document.Redo();
			Check(redone, "redo available after undo");
			Check(document.Paths().Count == 1, "redo restores the committed path");
		}

		private static void TestDirectSelectionMove()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PathData seed = BuildStraightPath(3);
			document.AddPath(seed);
			float originalX = seed.m_points[1].m_x;
			float originalY = seed.m_points[1].m_y;
			DirectSelectionTool dsel = new DirectSelectionTool();
			dsel.OnPressed(document, 30, 40, state);
			Check(dsel.SelectedAnchor() == 1, "direct-selection selects middle anchor");
			dsel.OnDragged(document, 45, 70, state);
			dsel.OnReleased(document, 45, 70, state);
			PathData path = document.Paths()[0];
			Check(path.m_points[1].m_x != originalX || path.m_points[1].m_y != originalY, "direct-selection drag moves anchor");
			bool undone = document.Undo();
			Check(undone, "direct-selection move undoable");
			Check(document.Paths()[0].m_points[1].m_x == originalX && document.Paths()[0].m_points[1].m_y == originalY, "direct-selection undo restores anchor position");
		}

		private static void TestDirectSelectionDelete()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PathData seed = BuildStraightPath(4);
			document.AddPath(seed);
			int before = seed.m_points.Count;
			DirectSelectionTool dsel = new DirectSelectionTool();
			dsel.OnPressed(document, 30, 40, state);
			dsel.OnReleased(document, 30, 40, state);
			Check(dsel.SelectedAnchor() == 1, "direct-selection selects anchor for delete");
			bool deleted = dsel.DeleteSelected(document);
			Check(deleted, "direct-selection delete reports success");
			Check(document.Paths()[0].m_points.Count == before - 1, "direct-selection delete removes one anchor");
			bool undone = document.Undo();
			Check(undone, "direct-selection delete undoable");
			Check(document.Paths()[0].m_points.Count == before, "direct-selection undo restores anchor count");
		}

		private static PathData BuildSmoothMiddlePath()
		{
			PathData path = new PathData("Smooth");
			path.m_points.Add(new PathPoint(20.0f, 40.0f));
			PathPoint mid = new PathPoint(60.0f, 40.0f);
			mid.m_smooth = true;
			mid.m_hasControlIn = true;
			mid.m_hasControlOut = true;
			mid.m_controlInX = 50.0f;
			mid.m_controlInY = 40.0f;
			mid.m_controlOutX = 70.0f;
			mid.m_controlOutY = 40.0f;
			path.m_points.Add(mid);
			path.m_points.Add(new PathPoint(100.0f, 40.0f));
			return path;
		}

		private static void TestDirectSelectionSymmetricHandle()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PathData seed = BuildSmoothMiddlePath();
			document.AddPath(seed);
			DirectSelectionTool dsel = new DirectSelectionTool();
			dsel.OnPressed(document, 60, 40, state);
			Check(dsel.SelectedAnchor() == 1, "symmetric handle selects middle anchor first");
			dsel.OnReleased(document, 60, 40, state);
			dsel.OnPressed(document, 70, 40, state);
			dsel.OnDragged(document, 80, 20, state);
			dsel.OnReleased(document, 80, 20, state);
			PathData path = document.Paths()[0];
			PathPoint mid = path.m_points[1];
			float expectedInX = (mid.m_x * 2.0f) - mid.m_controlOutX;
			float expectedInY = (mid.m_y * 2.0f) - mid.m_controlOutY;
			Check(mid.m_controlInX == expectedInX && mid.m_controlInY == expectedInY, "symmetric handle mirrors in handle on smooth point");
		}

		private static void TestDirectSelectionAltBreak()
		{
			Document document = new Document("t", 128, 128);
			ToolState state = new ToolState();
			PathData seed = BuildSmoothMiddlePath();
			document.AddPath(seed);
			DirectSelectionTool dsel = new DirectSelectionTool();
			dsel.OnPressed(document, 60, 40, state);
			dsel.OnReleased(document, 60, 40, state);
			dsel.OnPressed(document, 70, 40, state);
			state.SetAltHeld(true);
			dsel.OnDragged(document, 80, 20, state);
			dsel.OnReleased(document, 80, 20, state);
			state.SetAltHeld(false);
			PathData path = document.Paths()[0];
			PathPoint mid = path.m_points[1];
			Check(!mid.m_smooth, "alt break clears the smooth flag");
			float mirroredInX = (mid.m_x * 2.0f) - mid.m_controlOutX;
			float mirroredInY = (mid.m_y * 2.0f) - mid.m_controlOutY;
			Check(mid.m_controlInX != mirroredInX || mid.m_controlInY != mirroredInY, "alt break does not mirror the in handle");
		}
	}
}
