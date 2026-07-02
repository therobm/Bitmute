using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class EyedropperTool : Tool
	{
		private bool Sample(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			SKBitmap bitmap = layer.Bitmap();
			if (x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height)
			{
				return false;
			}
			SKColor color = bitmap.GetPixel(x, y);
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
