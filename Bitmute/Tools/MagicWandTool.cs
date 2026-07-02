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
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int seedX = x - offsetX;
			int seedY = y - offsetY;
			if (seedX < 0 || seedY < 0 || seedX >= width || seedY >= height)
			{
				return false;
			}

			SKColor target = bitmap.GetPixel(seedX, seedY);
			int tolerance = state.FillTolerance();
			int documentWidth = document.Width();
			int documentHeight = document.Height();
			byte[] visited = new byte[width * height];
			byte[] mask = new byte[documentWidth * documentHeight];
			int minX = documentWidth;
			int minY = documentHeight;
			int maxX = -1;
			int maxY = -1;

			Stack<int> pending = new Stack<int>();
			pending.Push((seedY * width) + seedX);
			for (;;)
			{
				if (pending.Count == 0)
				{
					break;
				}
				int index = pending.Pop();
				if (visited[index] != 0)
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
				visited[index] = 255;
				int canvasX = pixelX + offsetX;
				int canvasY = pixelY + offsetY;
				if (canvasX >= 0 && canvasY >= 0 && canvasX < documentWidth && canvasY < documentHeight)
				{
					mask[(canvasY * documentWidth) + canvasX] = 255;
					if (canvasX < minX)
					{
						minX = canvasX;
					}
					if (canvasX > maxX)
					{
						maxX = canvasX;
					}
					if (canvasY < minY)
					{
						minY = canvasY;
					}
					if (canvasY > maxY)
					{
						maxY = canvasY;
					}
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
