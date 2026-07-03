using Bitmute.Imaging;

namespace Bitmute.Tools
{
	public class ZoomTool : Tool
	{
		public override bool IsDestructive()
		{
			return false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return false;
		}
	}
}
