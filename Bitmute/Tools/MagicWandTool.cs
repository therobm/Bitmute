using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class MagicWandTool : Tool
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
			if (deltaRed > tolerance || deltaGreen > tolerance || deltaBlue > tolerance || deltaAlpha > tolerance)
			{
				return false;
			}
			return true;
		}

		public override bool IsDestructive()
		{
			return false;
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
			int tolerance = state.FillTolerance();
			byte[] mask = new byte[width * height];
			int minX = width;
			int minY = height;
			int maxX = -1;
			int maxY = -1;

			Stack<int> pending = new Stack<int>();
			pending.Push((y * width) + x);
			for (;;)
			{
				if (pending.Count == 0)
				{
					break;
				}
				int index = pending.Pop();
				if (mask[index] != 0)
				{
					continue;
				}
				int pixelX = index % width;
				int pixelY = index / width;
				SKColor current = bitmap.GetPixel(pixelX, pixelY);
				if (!ColorMatch(current, target, tolerance))
				{
					continue;
				}
				mask[index] = 255;
				if (pixelX < minX)
				{
					minX = pixelX;
				}
				if (pixelX > maxX)
				{
					maxX = pixelX;
				}
				if (pixelY < minY)
				{
					minY = pixelY;
				}
				if (pixelY > maxY)
				{
					maxY = pixelY;
				}
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

			if (maxX < 0)
			{
				document.Selection().Clear();
				return false;
			}
			SKRectI bounds = new SKRectI(minX, minY, maxX + 1, maxY + 1);
			document.Selection().SelectMask(mask, bounds);
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return false;
		}
	}
}
