using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class MagicWandTool : Tool
	{
		private static bool ChannelsMatch(int leftRed, int leftGreen, int leftBlue, int leftAlpha, int rightRed, int rightGreen, int rightBlue, int rightAlpha, int tolerance)
		{
			int deltaRed = leftRed - rightRed;
			int deltaGreen = leftGreen - rightGreen;
			int deltaBlue = leftBlue - rightBlue;
			int deltaAlpha = leftAlpha - rightAlpha;
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

		private static unsafe void ReadPixel(byte* basePointer, int rowBytes, int x, int y, bool premultiplied, out int red, out int green, out int blue, out int alpha)
		{
			byte* pixel = basePointer + (y * rowBytes) + (x * 4);
			red = pixel[0];
			green = pixel[1];
			blue = pixel[2];
			alpha = pixel[3];
			if (!premultiplied || alpha == 0 || alpha == 255)
			{
				return;
			}
			red = ((red * 255) + (alpha / 2)) / alpha;
			green = ((green * 255) + (alpha / 2)) / alpha;
			blue = ((blue * 255) + (alpha / 2)) / alpha;
			if (red > 255)
			{
				red = 255;
			}
			if (green > 255)
			{
				green = 255;
			}
			if (blue > 255)
			{
				blue = 255;
			}
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public override unsafe bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			bool sampleAll = state.WandSampleAll();
			bool contiguous = state.WandContiguous();
			SKBitmap composed = null;
			SKBitmap bitmap;
			int offsetX;
			int offsetY;
			bool premultiplied;
			if (sampleAll)
			{
				composed = new SKBitmap(document.Width(), document.Height(), SKColorType.Rgba8888, SKAlphaType.Premul);
				document.CompositeInto(composed);
				bitmap = composed;
				offsetX = 0;
				offsetY = 0;
				premultiplied = true;
			}
			else
			{
				bitmap = layer.Bitmap();
				offsetX = layer.OffsetX();
				offsetY = layer.OffsetY();
				premultiplied = false;
			}
			int width = bitmap.Width;
			int height = bitmap.Height;
			int seedX = x - offsetX;
			int seedY = y - offsetY;
			if (seedX < 0 || seedY < 0 || seedX >= width || seedY >= height)
			{
				if (composed != null)
				{
					composed.Dispose();
				}
				return false;
			}

			eSelectionMode mode = SelectionModeFromState(state);
			if (mode == eSelectionMode.Replace)
			{
				document.Selection().Clear();
			}
			document.Selection().BeginOperation(mode, state.SelectionFeather());

			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			int targetRed;
			int targetGreen;
			int targetBlue;
			int targetAlpha;
			ReadPixel(basePointer, rowBytes, seedX, seedY, premultiplied, out targetRed, out targetGreen, out targetBlue, out targetAlpha);
			int tolerance = state.FillTolerance();
			byte[] mask = new byte[width * height];
			bool anySelected = false;

			if (contiguous)
			{
				byte[] visited = new byte[width * height];
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
					visited[index] = 255;
					int pixelX = index % width;
					int pixelY = index / width;
					int currentRed;
					int currentGreen;
					int currentBlue;
					int currentAlpha;
					ReadPixel(basePointer, rowBytes, pixelX, pixelY, premultiplied, out currentRed, out currentGreen, out currentBlue, out currentAlpha);
					if (!ChannelsMatch(currentRed, currentGreen, currentBlue, currentAlpha, targetRed, targetGreen, targetBlue, targetAlpha, tolerance))
					{
						continue;
					}
					mask[index] = 255;
					anySelected = true;
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
			}
			else
			{
				for (int pixelY = 0; pixelY < height; pixelY++)
				{
					int maskRow = pixelY * width;
					for (int pixelX = 0; pixelX < width; pixelX++)
					{
						int currentRed;
						int currentGreen;
						int currentBlue;
						int currentAlpha;
						ReadPixel(basePointer, rowBytes, pixelX, pixelY, premultiplied, out currentRed, out currentGreen, out currentBlue, out currentAlpha);
						if (!ChannelsMatch(currentRed, currentGreen, currentBlue, currentAlpha, targetRed, targetGreen, targetBlue, targetAlpha, tolerance))
						{
							continue;
						}
						mask[maskRow + pixelX] = 255;
						anySelected = true;
					}
				}
			}

			if (composed != null)
			{
				composed.Dispose();
			}
			if (!anySelected)
			{
				document.Selection().ApplyRect(SKRectI.Empty);
				return false;
			}
			if (state.WandAntiAlias())
			{
				SmoothMaskBoundary(mask, width, height);
			}
			document.Selection().ApplyMask(mask, new SKRectI(offsetX, offsetY, offsetX + width, offsetY + height));
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return false;
		}
	}
}
