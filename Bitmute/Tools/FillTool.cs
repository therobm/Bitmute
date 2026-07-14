using System.Collections.Generic;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class FillTool : Tool
	{
		private static unsafe void BlendPixelByCoverage(byte* pixel, byte fillRed, byte fillGreen, byte fillBlue, byte fillAlpha, int coverage)
		{
			if (coverage >= 255)
			{
				pixel[0] = fillRed;
				pixel[1] = fillGreen;
				pixel[2] = fillBlue;
				pixel[3] = fillAlpha;
				return;
			}
			int inverse = 255 - coverage;
			pixel[0] = (byte)(((pixel[0] * inverse) + (fillRed * coverage) + 127) / 255);
			pixel[1] = (byte)(((pixel[1] * inverse) + (fillGreen * coverage) + 127) / 255);
			pixel[2] = (byte)(((pixel[2] * inverse) + (fillBlue * coverage) + 127) / 255);
			pixel[3] = (byte)(((pixel[3] * inverse) + (fillAlpha * coverage) + 127) / 255);
		}

		private static unsafe bool PixelMatch(byte* pixel, int targetRed, int targetGreen, int targetBlue, int targetAlpha, int tolerance)
		{
			int deltaRed = pixel[0] - targetRed;
			if (deltaRed < 0)
			{
				deltaRed = -deltaRed;
			}
			if (deltaRed > tolerance)
			{
				return false;
			}
			int deltaGreen = pixel[1] - targetGreen;
			if (deltaGreen < 0)
			{
				deltaGreen = -deltaGreen;
			}
			if (deltaGreen > tolerance)
			{
				return false;
			}
			int deltaBlue = pixel[2] - targetBlue;
			if (deltaBlue < 0)
			{
				deltaBlue = -deltaBlue;
			}
			if (deltaBlue > tolerance)
			{
				return false;
			}
			int deltaAlpha = pixel[3] - targetAlpha;
			if (deltaAlpha < 0)
			{
				deltaAlpha = -deltaAlpha;
			}
			if (deltaAlpha > tolerance)
			{
				return false;
			}
			return true;
		}

		public override unsafe bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			SKBitmap bitmap;
			if (document.PaintTarget() == ePaintTarget.Mask && layer.HasMask())
			{
				bitmap = layer.MaskBitmap();
			}
			else
			{
				bitmap = layer.Bitmap();
			}
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
			bool clip = selection.IsActive();
			byte[] selectionMask = null;
			int selectionOriginX = 0;
			int selectionOriginY = 0;
			int selectionStride = 0;
			int selectionRows = 0;
			if (clip)
			{
				selectionMask = selection.Mask();
				selectionOriginX = selection.MaskOriginX();
				selectionOriginY = selection.MaskOriginY();
				selectionStride = selection.MaskWidth();
				selectionRows = selection.MaskHeight();
			}
			int rowBytes = bitmap.RowBytes;
			byte* pixels = (byte*)bitmap.GetPixels().ToPointer();
			byte* seedPixel = pixels + ((long)seedY * rowBytes) + (seedX * 4);
			int targetRed = seedPixel[0];
			int targetGreen = seedPixel[1];
			int targetBlue = seedPixel[2];
			int targetAlpha = seedPixel[3];
			SKColor fill = state.Foreground();
			byte fillRed = fill.Red;
			byte fillGreen = fill.Green;
			byte fillBlue = fill.Blue;
			byte fillAlpha = fill.Alpha;
			int tolerance = state.FillTolerance();
			Pattern pattern = state.ActivePattern();
			SKBitmap patternBitmap = null;
			if (pattern != null)
			{
				patternBitmap = pattern.m_bitmap;
			}
			bool usePattern = state.FillContent() == eFillContent.Pattern && pattern != null && patternBitmap != null && patternBitmap.Width >= 1 && patternBitmap.Height >= 1;
			if (state.FillContent() == eFillContent.Pattern && !usePattern)
			{
				return false;
			}
			if (!usePattern)
			{
				bool seedIsFillColor = targetRed == fillRed && targetGreen == fillGreen && targetBlue == fillBlue && targetAlpha == fillAlpha;
				if (seedIsFillColor)
				{
					return false;
				}
			}
			int patternWidth = 0;
			int patternHeight = 0;
			int patternRowBytes = 0;
			byte* patternBase = null;
			if (usePattern)
			{
				patternWidth = patternBitmap.Width;
				patternHeight = patternBitmap.Height;
				patternRowBytes = patternBitmap.RowBytes;
				patternBase = (byte*)patternBitmap.GetPixels().ToPointer();
			}

			int minFilledX = seedX;
			int maxFilledX = seedX;
			int minFilledY = seedY;
			int maxFilledY = seedY;
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
				if (filled[index])
				{
					continue;
				}
				int pixelX = index % width;
				int pixelY = index / width;
				int coverage = 255;
				if (clip)
				{
					int canvasX = pixelX + offsetX;
					int canvasY = pixelY + offsetY;
					if (canvasX < selectionOriginX || canvasY < selectionOriginY || canvasX >= selectionOriginX + selectionStride || canvasY >= selectionOriginY + selectionRows)
					{
						continue;
					}
					coverage = selectionMask[((canvasY - selectionOriginY) * selectionStride) + (canvasX - selectionOriginX)];
					if (coverage == 0)
					{
						continue;
					}
				}
				byte* current = pixels + ((long)pixelY * rowBytes) + (pixelX * 4);
				if (!PixelMatch(current, targetRed, targetGreen, targetBlue, targetAlpha, tolerance))
				{
					continue;
				}
				if (usePattern)
				{
					int canvasX = pixelX + offsetX;
					int canvasY = pixelY + offsetY;
					int patX = ((canvasX % patternWidth) + patternWidth) % patternWidth;
					int patY = ((canvasY % patternHeight) + patternHeight) % patternHeight;
					byte* patternPixel = patternBase + ((long)patY * patternRowBytes) + (patX * 4);
					BlendPixelByCoverage(current, patternPixel[0], patternPixel[1], patternPixel[2], patternPixel[3], coverage);
				}
				else
				{
					BlendPixelByCoverage(current, fillRed, fillGreen, fillBlue, fillAlpha, coverage);
				}
				filled[index] = true;
				if (pixelX < minFilledX)
				{
					minFilledX = pixelX;
				}
				if (pixelX > maxFilledX)
				{
					maxFilledX = pixelX;
				}
				if (pixelY < minFilledY)
				{
					minFilledY = pixelY;
				}
				if (pixelY > maxFilledY)
				{
					maxFilledY = pixelY;
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

			DilateEdge(pixels, rowBytes, filled, width, height, offsetX, offsetY, clip, selectionMask, selectionOriginX, selectionOriginY, selectionStride, selectionRows, fillRed, fillGreen, fillBlue, fillAlpha, minFilledX, minFilledY, maxFilledX, maxFilledY, usePattern, patternBase, patternRowBytes, patternWidth, patternHeight);
			return true;
		}

		private unsafe void DilateEdge(byte* pixels, int rowBytes, bool[] filled, int width, int height, int offsetX, int offsetY, bool clip, byte[] selectionMask, int selectionOriginX, int selectionOriginY, int selectionStride, int selectionRows, byte fillRed, byte fillGreen, byte fillBlue, byte fillAlpha, int minFilledX, int minFilledY, int maxFilledX, int maxFilledY, bool usePattern, byte* patternBase, int patternRowBytes, int patternWidth, int patternHeight)
		{
			int scanLeft = minFilledX - 1;
			int scanTop = minFilledY - 1;
			int scanRight = maxFilledX + 2;
			int scanBottom = maxFilledY + 2;
			if (scanLeft < 0)
			{
				scanLeft = 0;
			}
			if (scanTop < 0)
			{
				scanTop = 0;
			}
			if (scanRight > width)
			{
				scanRight = width;
			}
			if (scanBottom > height)
			{
				scanBottom = height;
			}
			for (int pixelY = scanTop; pixelY < scanBottom; pixelY++)
			{
				for (int pixelX = scanLeft; pixelX < scanRight; pixelX++)
				{
					int index = (pixelY * width) + pixelX;
					if (filled[index])
					{
						continue;
					}
					int coverage = 255;
					if (clip)
					{
						int canvasX = pixelX + offsetX;
						int canvasY = pixelY + offsetY;
						if (canvasX < selectionOriginX || canvasY < selectionOriginY || canvasX >= selectionOriginX + selectionStride || canvasY >= selectionOriginY + selectionRows)
						{
							continue;
						}
						coverage = selectionMask[((canvasY - selectionOriginY) * selectionStride) + (canvasX - selectionOriginX)];
						if (coverage == 0)
						{
							continue;
						}
					}
					bool nextToFilled = (pixelX > 0 && filled[index - 1]) || (pixelX < width - 1 && filled[index + 1]) || (pixelY > 0 && filled[index - width]) || (pixelY < height - 1 && filled[index + width]);
					if (!nextToFilled)
					{
						continue;
					}
					byte* current = pixels + ((long)pixelY * rowBytes) + (pixelX * 4);
					if (usePattern)
					{
						int canvasX = pixelX + offsetX;
						int canvasY = pixelY + offsetY;
						int patX = ((canvasX % patternWidth) + patternWidth) % patternWidth;
						int patY = ((canvasY % patternHeight) + patternHeight) % patternHeight;
						byte* patternPixel = patternBase + ((long)patY * patternRowBytes) + (patX * 4);
						BlendPixelByCoverage(current, patternPixel[0], patternPixel[1], patternPixel[2], patternPixel[3], coverage);
					}
					else
					{
						BlendPixelByCoverage(current, fillRed, fillGreen, fillBlue, fillAlpha, coverage);
					}
				}
			}
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			return false;
		}
	}
}
