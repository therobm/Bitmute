using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class FillTool : Tool
	{
		private static bool ColorMatch(SKColor left, SKColor right, int tolerance)
		{
			int deltaRed = left.Red - right.Red;
			int deltaGreen = left.Green - right.Green;
			int deltaBlue = left.Blue - right.Blue;
			int deltaAlpha = left.Alpha - right.Alpha;
			if (deltaRed < 0)
			{
				deltaRed = -deltaRed;
			}
			if (deltaGreen < 0)
			{
				deltaGreen = -deltaGreen;
			}
			if (deltaBlue < 0)
			{
				deltaBlue = -deltaBlue;
			}
			if (deltaAlpha < 0)
			{
				deltaAlpha = -deltaAlpha;
			}
			if (deltaRed > tolerance)
			{
				return false;
			}
			if (deltaGreen > tolerance)
			{
				return false;
			}
			if (deltaBlue > tolerance)
			{
				return false;
			}
			if (deltaAlpha > tolerance)
			{
				return false;
			}
			return true;
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			SKBitmap bitmap = layer.Bitmap();
			int width = bitmap.Width;
			int height = bitmap.Height;
			if (x < 0 || y < 0 || x >= width || y >= height)
			{
				return false;
			}

			SKColor target = bitmap.GetPixel(x, y);
			SKColor fill = state.Foreground();
			int tolerance = state.FillTolerance();
			if (ColorMatch(target, fill, 0))
			{
				return false;
			}

			Stack<int> pending = new Stack<int>();
			pending.Push((y * width) + x);
			for (;;)
			{
				if (pending.Count == 0)
				{
					break;
				}
				int index = pending.Pop();
				int pixelX = index % width;
				int pixelY = index / width;
				SKColor current = bitmap.GetPixel(pixelX, pixelY);
				if (!ColorMatch(current, target, tolerance))
				{
					continue;
				}
				if (ColorMatch(current, fill, 0))
				{
					continue;
				}
				bitmap.SetPixel(pixelX, pixelY, fill);
				if (pixelX > 0)
				{
					pending.Push(index - 1);
				}
				if (pixelX < width - 1)
				{
					pending.Push(index + 1);
				}
				if (pixelY > 0)
				{
					pending.Push(index - width);
				}
				if (pixelY < height - 1)
				{
					pending.Push(index + width);
				}
			}
			return true;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return false;
		}
	}
}
