using System.Collections.Generic;

namespace Bitmute.Imaging
{
	public class PathEditCommand : EditCommand
	{
		private static List<PathData> ClonePathList(List<PathData> source)
		{
			List<PathData> copy = new List<PathData>();
			for (int index = 0; index < source.Count; index++)
			{
				copy.Add(source[index].Clone());
			}
			return copy;
		}

		private string m_label;
		private List<PathData> m_beforePaths;
		private List<PathData> m_afterPaths;

		public PathEditCommand(string label)
		{
			m_label = label;
		}

		public void CaptureBefore(Document document)
		{
			m_beforePaths = document.ClonePaths();
		}

		public void CaptureAfter(Document document)
		{
			m_afterPaths = document.ClonePaths();
		}

		public override string Label()
		{
			return m_label;
		}

		public override void ApplyBefore(Document document)
		{
			document.ReplacePaths(ClonePathList(m_beforePaths));
		}

		public override void ApplyAfter(Document document)
		{
			document.ReplacePaths(ClonePathList(m_afterPaths));
		}
	}
}
