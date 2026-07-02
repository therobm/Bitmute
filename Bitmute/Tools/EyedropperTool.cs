using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class EyedropperTool : Tool
	{
		public override bool IsDestructive()
		{
			return false;
		}

		private bool Sample(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			SKColor color = layer.GetPixelCanvas(x, y);
			state.SetForeground(color);
			return false;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			return Sample(document, x, y, state);
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return Sample(document, x, y, state);
		}
	}
}
