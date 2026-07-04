using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class SelectionStroke
	{
		private static int NeighborCandidate(byte[] state, int[] distance, int width, int height, int neighborX, int neighborY, bool selected)
		{
			if (neighborX < 0 || neighborY < 0 || neighborX >= width || neighborY >= height)
			{
				if (selected)
				{
					return 1;
				}
				return 1 << 29;
			}
			int neighborIndex = (neighborY * width) + neighborX;
			bool neighborSelected = state[neighborIndex] >= 128;
			if (neighborSelected != selected)
			{
				return 1;
			}
			return distance[neighborIndex] + 1;
		}

		private static void ComputeDistance(byte[] state, int width, int height, int[] distance)
		{
			int infinity = 1 << 29;
			for (int index = 0; index < distance.Length; index++)
			{
				distance[index] = infinity;
			}
			for (int y = 0; y < height; y++)
			{
				int row = y * width;
				for (int x = 0; x < width; x++)
				{
					int index = row + x;
					bool selected = state[index] >= 128;
					int best = distance[index];
					int candidate = NeighborCandidate(state, distance, width, height, x - 1, y, selected);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x - 1, y - 1, selected);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x, y - 1, selected);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x + 1, y - 1, selected);
					if (candidate < best)
					{
						best = candidate;
					}
					distance[index] = best;
				}
			}
			for (int y = height - 1; y >= 0; y--)
			{
				int row = y * width;
				for (int x = width - 1; x >= 0; x--)
				{
					int index = row + x;
					bool selected = state[index] >= 128;
					int best = distance[index];
					int candidate = NeighborCandidate(state, distance, width, height, x + 1, y, selected);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x + 1, y + 1, selected);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x, y + 1, selected);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x - 1, y + 1, selected);
					if (candidate < best)
					{
						best = candidate;
					}
					distance[index] = best;
				}
			}
		}

		public static unsafe void Apply(Document document, SKColor color, int width, int position)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			if (layer.IsText())
			{
				return;
			}
			int canvasWidth = document.Width();
			int canvasHeight = document.Height();
			if (canvasWidth <= 0 || canvasHeight <= 0)
			{
				return;
			}
			int strokeWidth = width;
			if (strokeWidth < 1)
			{
				strokeWidth = 1;
			}
			if (strokeWidth > 100)
			{
				strokeWidth = 100;
			}
			Selection selection = document.Selection();
			byte[] state;
			if (selection.IsActive())
			{
				state = selection.Mask();
			}
			else
			{
				state = new byte[canvasWidth * canvasHeight];
				for (int index = 0; index < state.Length; index++)
				{
					state[index] = 255;
				}
			}
			int[] distance = new int[canvasWidth * canvasHeight];
			ComputeDistance(state, canvasWidth, canvasHeight, distance);
			int insideLimit;
			int outsideLimit;
			if (position == 0)
			{
				insideLimit = strokeWidth;
				outsideLimit = 0;
			}
			else if (position == 2)
			{
				insideLimit = 0;
				outsideLimit = strokeWidth;
			}
			else
			{
				insideLimit = (strokeWidth + 1) / 2;
				outsideLimit = strokeWidth / 2;
			}
			SKBitmap bitmap = layer.Bitmap();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int bitmapWidth = bitmap.Width;
			int bitmapHeight = bitmap.Height;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			byte colorRed = color.Red;
			byte colorGreen = color.Green;
			byte colorBlue = color.Blue;
			byte colorAlpha = color.Alpha;
			int minX = canvasWidth;
			int minY = canvasHeight;
			int maxX = -1;
			int maxY = -1;
			for (int canvasY = 0; canvasY < canvasHeight; canvasY++)
			{
				int bitmapY = canvasY - offsetY;
				if (bitmapY < 0 || bitmapY >= bitmapHeight)
				{
					continue;
				}
				int row = canvasY * canvasWidth;
				byte* rowPointer = basePointer + (bitmapY * rowBytes);
				for (int canvasX = 0; canvasX < canvasWidth; canvasX++)
				{
					int index = row + canvasX;
					int limit;
					if (state[index] >= 128)
					{
						limit = insideLimit;
					}
					else
					{
						limit = outsideLimit;
					}
					if (limit == 0)
					{
						continue;
					}
					if (distance[index] > limit)
					{
						continue;
					}
					int bitmapX = canvasX - offsetX;
					if (bitmapX < 0 || bitmapX >= bitmapWidth)
					{
						continue;
					}
					byte* pixel = rowPointer + (bitmapX * 4);
					pixel[0] = colorRed;
					pixel[1] = colorGreen;
					pixel[2] = colorBlue;
					pixel[3] = colorAlpha;
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
			}
			if (maxX < minX || maxY < minY)
			{
				return;
			}
			document.MarkComposeDirtyRegion(new SKRectI(minX, minY, maxX + 1, maxY + 1));
		}
	}
}
