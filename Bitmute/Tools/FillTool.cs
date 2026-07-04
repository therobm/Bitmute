using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class FillTool : Tool
	{
		private static SKColor BlendByCoverage(SKColor current, SKColor fill, int coverage)
		{
			if (coverage >= 255)
			{
				return fill;
			}
			int inverse = 255 - coverage;
			byte red = (byte)(((current.Red * inverse) + (fill.Red * coverage) + 127) / 255);
			byte green = (byte)(((current.Green * inverse) + (fill.Green * coverage) + 127) / 255);
			byte blue = (byte)(((current.Blue * inverse) + (fill.Blue * coverage) + 127) / 255);
			byte alpha = (byte)(((current.Alpha * inverse) + (fill.Alpha * coverage) + 127) / 255);
			return new SKColor(red, green, blue, alpha);
		}

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
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int seedX = x - offsetX;
			int seedY = y - offsetY;
			if (seedX < 0 || seedY < 0 || seedX >= width || seedY >= height)
			{
				return false;
			}

			Selection selection = document.Selection();
			SKColor target = bitmap.GetPixel(seedX, seedY);
			SKColor fill = state.Foreground();
			int tolerance = state.FillTolerance();
			if (ColorMatch(target, fill, 0))
			{
				return false;
			}

			bool[] filled = new bool[width * height];
			Stack<int> pending = new Stack<int>();
			pending.Push((seedY * width) + seedX);
			for (;;)
			{
				if (pending.Count == 0)
				{
					break;
				}
				int index = pending.Pop();
				int pixelX = index % width;
				int pixelY = index / width;
				int coverage = 255;
				if (selection.IsActive())
				{
					coverage = selection.Coverage(pixelX + offsetX, pixelY + offsetY);
					if (coverage == 0)
					{
						continue;
					}
				}
				SKColor current = bitmap.GetPixel(pixelX, pixelY);
				if (!ColorMatch(current, target, tolerance))
				{
					continue;
				}
				if (filled[index])
				{
					continue;
				}
				bitmap.SetPixel(pixelX, pixelY, BlendByCoverage(current, fill, coverage));
				filled[index] = true;
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

			DilateEdge(bitmap, filled, width, height, offsetX, offsetY, selection, fill);
			return true;
		}

		private void DilateEdge(SKBitmap bitmap, bool[] filled, int width, int height, int offsetX, int offsetY, Selection selection, SKColor fill)
		{
			List<int> edge = new List<int>();
			for (int pixelY = 0; pixelY < height; pixelY++)
			{
				for (int pixelX = 0; pixelX < width; pixelX++)
				{
					int index = (pixelY * width) + pixelX;
					if (filled[index])
					{
						continue;
					}
					if (selection.IsActive() && selection.Coverage(pixelX + offsetX, pixelY + offsetY) == 0)
					{
						continue;
					}
					bool nextToFilled = (pixelX > 0 && filled[index - 1]) || (pixelX < width - 1 && filled[index + 1]) || (pixelY > 0 && filled[index - width]) || (pixelY < height - 1 && filled[index + width]);
					if (nextToFilled)
					{
						edge.Add(index);
					}
				}
			}
			for (int position = 0; position < edge.Count; position++)
			{
				int index = edge[position];
				int pixelX = index % width;
				int pixelY = index / width;
				int coverage = 255;
				if (selection.IsActive())
				{
					coverage = selection.Coverage(pixelX + offsetX, pixelY + offsetY);
				}
				SKColor current = bitmap.GetPixel(pixelX, pixelY);
				bitmap.SetPixel(pixelX, pixelY, BlendByCoverage(current, fill, coverage));
			}
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return false;
		}
	}
}
