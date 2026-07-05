using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public abstract class Tool
	{
		protected int m_lastX;
		protected int m_lastY;
		protected bool m_hasLast;

		protected static void SmoothMaskBoundary(byte[] mask, int width, int height)
		{
			int minX = width;
			int minY = height;
			int maxX = -1;
			int maxY = -1;
			for (int y = 0; y < height; y++)
			{
				int rowStart = y * width;
				for (int x = 0; x < width; x++)
				{
					if (mask[rowStart + x] == 0)
					{
						continue;
					}
					if (x < minX)
					{
						minX = x;
					}
					if (x > maxX)
					{
						maxX = x;
					}
					if (y < minY)
					{
						minY = y;
					}
					if (y > maxY)
					{
						maxY = y;
					}
				}
			}
			if (maxX < 0)
			{
				return;
			}
			int left = minX - 1;
			int top = minY - 1;
			int right = maxX + 2;
			int bottom = maxY + 2;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > width)
			{
				right = width;
			}
			if (bottom > height)
			{
				bottom = height;
			}
			byte[] source = new byte[mask.Length];
			System.Array.Copy(mask, source, mask.Length);
			for (int y = top; y < bottom; y++)
			{
				int rowStart = y * width;
				for (int x = left; x < right; x++)
				{
					int center = source[rowStart + x];
					bool boundary = false;
					int sum = 0;
					int count = 0;
					for (int deltaY = -1; deltaY <= 1; deltaY++)
					{
						int neighborY = y + deltaY;
						if (neighborY < 0 || neighborY >= height)
						{
							continue;
						}
						int neighborRow = neighborY * width;
						for (int deltaX = -1; deltaX <= 1; deltaX++)
						{
							int neighborX = x + deltaX;
							if (neighborX < 0 || neighborX >= width)
							{
								continue;
							}
							int value = source[neighborRow + neighborX];
							if (value != center)
							{
								boundary = true;
							}
							sum = sum + value;
							count = count + 1;
						}
					}
					if (!boundary || count == 0)
					{
						continue;
					}
					mask[rowStart + x] = (byte)((sum + (count / 2)) / count);
				}
			}
		}

		protected unsafe void DrawDab(Layer layer, int centerX, int centerY, int radius, SKColor color, Selection selection)
		{
			SKBitmap bitmap = layer.Bitmap();
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			byte* pixels = (byte*)bitmap.GetPixels().ToPointer();
			byte red = color.Red;
			byte green = color.Green;
			byte blue = color.Blue;
			byte alpha = color.Alpha;
			bool clip = selection != null && selection.IsActive();
			byte[] selectionMask = null;
			int selectionWidth = 0;
			int selectionHeight = 0;
			if (clip)
			{
				selectionMask = selection.Mask();
				selectionWidth = selection.Width();
				selectionHeight = selection.Height();
			}
			int radiusSquared = radius * radius;
			for (int offsetY = -radius; offsetY <= radius; offsetY++)
			{
				int canvasY = centerY + offsetY;
				int bitmapY = canvasY - layerOffsetY;
				if (bitmapY < 0 || bitmapY >= height)
				{
					continue;
				}
				if (clip && (canvasY < 0 || canvasY >= selectionHeight))
				{
					continue;
				}
				int offsetYSquared = offsetY * offsetY;
				int selectionRow = canvasY * selectionWidth;
				byte* rowStart = pixels + (bitmapY * rowBytes);
				for (int offsetX = -radius; offsetX <= radius; offsetX++)
				{
					if ((offsetX * offsetX) + offsetYSquared > radiusSquared)
					{
						continue;
					}
					int canvasX = centerX + offsetX;
					int coverage = 255;
					if (clip)
					{
						if (canvasX < 0 || canvasX >= selectionWidth)
						{
							continue;
						}
						coverage = selectionMask[selectionRow + canvasX];
						if (coverage == 0)
						{
							continue;
						}
					}
					int bitmapX = canvasX - layerOffsetX;
					if (bitmapX < 0 || bitmapX >= width)
					{
						continue;
					}
					byte* pixel = rowStart + (bitmapX * 4);
					if (coverage == 255)
					{
						pixel[0] = red;
						pixel[1] = green;
						pixel[2] = blue;
						pixel[3] = alpha;
						continue;
					}
					int inverse = 255 - coverage;
					pixel[0] = (byte)(((pixel[0] * inverse) + (red * coverage) + 127) / 255);
					pixel[1] = (byte)(((pixel[1] * inverse) + (green * coverage) + 127) / 255);
					pixel[2] = (byte)(((pixel[2] * inverse) + (blue * coverage) + 127) / 255);
					pixel[3] = (byte)(((pixel[3] * inverse) + (alpha * coverage) + 127) / 255);
				}
			}
		}

		protected unsafe void BlitCanvasBitmap(Document document, Layer layer, SKBitmap temp, int tempLeft, int tempTop)
		{
			SKBitmap layerBitmap = layer.Bitmap();
			int layerWidth = layerBitmap.Width;
			int layerHeight = layerBitmap.Height;
			int layerRowBytes = layerBitmap.RowBytes;
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			byte* layerBase = (byte*)layerBitmap.GetPixels().ToPointer();
			int tempWidth = temp.Width;
			int tempHeight = temp.Height;
			int tempRowBytes = temp.RowBytes;
			byte* tempBase = (byte*)temp.GetPixels().ToPointer();
			Selection selection = document.Selection();
			bool clip = selection.IsActive();
			byte[] mask = null;
			int documentWidth = document.Width();
			int documentHeight = document.Height();
			if (clip)
			{
				mask = selection.Mask();
			}
			for (int tempY = 0; tempY < tempHeight; tempY++)
			{
				int canvasY = tempTop + tempY;
				int bitmapY = canvasY - layerOffsetY;
				if (bitmapY < 0 || bitmapY >= layerHeight)
				{
					continue;
				}
				if (clip && (canvasY < 0 || canvasY >= documentHeight))
				{
					continue;
				}
				byte* tempRow = tempBase + (tempY * tempRowBytes);
				byte* layerRow = layerBase + (bitmapY * layerRowBytes);
				for (int tempX = 0; tempX < tempWidth; tempX++)
				{
					byte* sourcePixel = tempRow + (tempX * 4);
					int sourceAlpha = sourcePixel[3];
					if (sourceAlpha == 0)
					{
						continue;
					}
					int canvasX = tempLeft + tempX;
					int bitmapX = canvasX - layerOffsetX;
					if (bitmapX < 0 || bitmapX >= layerWidth)
					{
						continue;
					}
					if (clip)
					{
						if (canvasX < 0 || canvasX >= documentWidth)
						{
							continue;
						}
						int coverage = mask[(canvasY * documentWidth) + canvasX];
						if (coverage == 0)
						{
							continue;
						}
						if (coverage < 255)
						{
							sourceAlpha = ((sourceAlpha * coverage) + 127) / 255;
							if (sourceAlpha == 0)
							{
								continue;
							}
						}
					}
					byte* destinationPixel = layerRow + (bitmapX * 4);
					if (sourceAlpha == 255)
					{
						destinationPixel[0] = sourcePixel[0];
						destinationPixel[1] = sourcePixel[1];
						destinationPixel[2] = sourcePixel[2];
						destinationPixel[3] = 255;
						continue;
					}
					int inverse = 255 - sourceAlpha;
					int destinationContribution = ((destinationPixel[3] * inverse) + 127) / 255;
					int outAlpha = sourceAlpha + destinationContribution;
					if (outAlpha == 0)
					{
						destinationPixel[0] = 0;
						destinationPixel[1] = 0;
						destinationPixel[2] = 0;
						destinationPixel[3] = 0;
						continue;
					}
					int red = ((sourcePixel[0] * sourceAlpha) + (destinationPixel[0] * destinationContribution) + (outAlpha / 2)) / outAlpha;
					int green = ((sourcePixel[1] * sourceAlpha) + (destinationPixel[1] * destinationContribution) + (outAlpha / 2)) / outAlpha;
					int blue = ((sourcePixel[2] * sourceAlpha) + (destinationPixel[2] * destinationContribution) + (outAlpha / 2)) / outAlpha;
					destinationPixel[0] = (byte)red;
					destinationPixel[1] = (byte)green;
					destinationPixel[2] = (byte)blue;
					destinationPixel[3] = (byte)outAlpha;
				}
			}
		}

		protected void StrokeLine(Layer layer, int startX, int startY, int endX, int endY, int radius, SKColor color, Selection selection)
		{
			int deltaX = endX - startX;
			int deltaY = endY - startY;
			int steps = System.Math.Abs(deltaX);
			int absDeltaY = System.Math.Abs(deltaY);
			if (absDeltaY > steps)
			{
				steps = absDeltaY;
			}
			if (steps <= 0)
			{
				DrawDab(layer, startX, startY, radius, color, selection);
				return;
			}
			for (int step = 0; step <= steps; step++)
			{
				double fraction = (double)step / (double)steps;
				int pointX = startX + (int)System.Math.Round(deltaX * fraction);
				int pointY = startY + (int)System.Math.Round(deltaY * fraction);
				DrawDab(layer, pointX, pointY, radius, color, selection);
			}
		}

		protected void MarkStrokeDirty(Document document, int startX, int startY, int endX, int endY, int radius)
		{
			int left = startX;
			if (endX < left)
			{
				left = endX;
			}
			int right = startX;
			if (endX > right)
			{
				right = endX;
			}
			int top = startY;
			if (endY < top)
			{
				top = endY;
			}
			int bottom = startY;
			if (endY > bottom)
			{
				bottom = endY;
			}
			int pad = radius + 1;
			SKRectI rect = new SKRectI(left - pad, top - pad, right + pad + 1, bottom + pad + 1);
			document.MarkComposeDirtyRegion(rect);
		}

		protected eSelectionMode SelectionModeFromState(ToolState state)
		{
			if (state.ShiftHeld() && state.AltHeld())
			{
				return eSelectionMode.Intersect;
			}
			if (state.ShiftHeld())
			{
				return eSelectionMode.Add;
			}
			if (state.AltHeld())
			{
				return eSelectionMode.Subtract;
			}
			int baseMode = state.SelectionMode();
			if (baseMode == 1)
			{
				return eSelectionMode.Add;
			}
			if (baseMode == 2)
			{
				return eSelectionMode.Subtract;
			}
			if (baseMode == 3)
			{
				return eSelectionMode.Intersect;
			}
			return eSelectionMode.Replace;
		}

		public virtual bool IsDestructive()
		{
			return true;
		}

		public abstract bool OnPressed(Document document, int x, int y, ToolState state);

		public abstract bool OnDragged(Document document, int x, int y, ToolState state);

		public virtual void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_hasLast = false;
		}

		public virtual void Reset()
		{
		}
	}
}
