using System;
using System.Collections.Generic;
using Bitmute.Imaging;

namespace Bitmute.Tests
{
	public static class LayerOrderTests
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
			s_failures = 0;
			TestDeleteReindex();
			TestGroupMoveDown();
			TestGroupMoveClampedTop();
			return s_failures;
		}

		private static void SelectExactly(Document doc, int[] indices)
		{
			doc.SetActiveLayerIndex(0);
			for (int scan = 0; scan < indices.Length; scan++)
			{
				if (doc.IsLayerSelected(indices[scan]))
				{
					continue;
				}
				doc.ToggleLayerSelection(indices[scan]);
			}
			if (doc.IsLayerSelected(0))
			{
				bool wanted = false;
				for (int scan = 0; scan < indices.Length; scan++)
				{
					if (indices[scan] == 0)
					{
						wanted = true;
						break;
					}
				}
				if (!wanted)
				{
					doc.ToggleLayerSelection(0);
				}
			}
		}

		private static void TestDeleteReindex()
		{
			Document doc = new Document("t", 8, 8);
			doc.AddLayer("L1");
			doc.AddLayer("L2");
			doc.AddLayer("L3");
			Check(doc.Layers().Count == 4, "delete reindex starts with 4 layers");
			SelectExactly(doc, new int[] { 1, 3 });
			doc.DeleteLayer(1);
			List<int> selected = doc.SelectedLayerIndices();
			Check(selected.Count == 1 && selected[0] == 2, "delete reindex leaves selection {2}");
			Check(doc.Layers().Count == 3, "delete reindex leaves 3 layers");
		}

		private static void TestGroupMoveDown()
		{
			Document doc = new Document("t", 8, 8);
			doc.AddLayer("L1");
			doc.AddLayer("L2");
			doc.AddLayer("L3");
			doc.AddLayer("L4");
			Check(doc.Layers().Count == 5, "group move down starts with 5 layers");
			Layer first = doc.Layers()[1];
			Layer second = doc.Layers()[2];
			SelectExactly(doc, new int[] { 1, 2 });
			doc.MoveSelectedLayers(2);
			List<int> selected = doc.SelectedLayerIndices();
			selected.Sort();
			Check(selected.Count == 2 && selected[0] == 3 && selected[1] == 4, "group move down lands selection at {3,4}");
			Check(doc.Layers().Count == 5, "group move down keeps 5 layers");
			Check(ReferenceEquals(doc.Layers()[3], first), "group move down keeps first moved layer at 3");
			Check(ReferenceEquals(doc.Layers()[4], second), "group move down keeps second moved layer at 4");
		}

		private static void TestGroupMoveClampedTop()
		{
			Document doc = new Document("t", 8, 8);
			doc.AddLayer("L1");
			doc.AddLayer("L2");
			doc.AddLayer("L3");
			Check(doc.Layers().Count == 4, "group move clamp starts with 4 layers");
			SelectExactly(doc, new int[] { 2, 3 });
			doc.MoveSelectedLayers(10);
			List<int> selected = doc.SelectedLayerIndices();
			selected.Sort();
			Check(selected.Count == 2 && selected[0] == 2 && selected[1] == 3, "group move clamp stays at {2,3}");
			Check(doc.Layers().Count == 4, "group move clamp keeps 4 layers");
		}
	}
}
