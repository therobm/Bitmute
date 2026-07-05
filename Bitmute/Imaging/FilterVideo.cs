using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterVideo
	{
		private static int PremultiplyChannel(int channel, int alpha)
		{
			return ((channel * alpha) + 127) / 255;
		}

		private static int UnpremultiplyChannel(int premultiplied, int alpha)
		{
			if (alpha == 0)
			{
				return 0;
			}
			int value = ((premultiplied * 255) + (alpha / 2)) / alpha;
			if (value > 255)
			{
				value = 255;
			}
			return value;
		}

		private static unsafe void AveragePremultiplied(byte* abovePixel, byte* belowPixel, byte* destinationPixel)
		{
			int aboveAlpha = abovePixel[3];
			int belowAlpha = belowPixel[3];
			int alpha = (aboveAlpha + belowAlpha + 1) / 2;
			int red = (PremultiplyChannel(abovePixel[0], aboveAlpha) + PremultiplyChannel(belowPixel[0], belowAlpha) + 1) / 2;
			int green = (PremultiplyChannel(abovePixel[1], aboveAlpha) + PremultiplyChannel(belowPixel[1], belowAlpha) + 1) / 2;
			int blue = (PremultiplyChannel(abovePixel[2], aboveAlpha) + PremultiplyChannel(belowPixel[2], belowAlpha) + 1) / 2;
			destinationPixel[0] = (byte)UnpremultiplyChannel(red, alpha);
			destinationPixel[1] = (byte)UnpremultiplyChannel(green, alpha);
			destinationPixel[2] = (byte)UnpremultiplyChannel(blue, alpha);
			destinationPixel[3] = (byte)alpha;
		}

		private sealed unsafe class InterpolateWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_height;
			public int m_firstReplacedRow;

			public void Band(int start, int end)
			{
				long rowLength = (long)m_width * 4;
				for (int index = start; index < end; index++)
				{
					int y = m_firstReplacedRow + (index * 2);
					int above = y - 1;
					int below = y + 1;
					byte* destinationRow = m_base + ((long)y * m_rowBytes);
					if (above >= 0 && below < m_height)
					{
						byte* aboveRow = m_base + ((long)above * m_rowBytes);
						byte* belowRow = m_base + ((long)below * m_rowBytes);
						for (int x = 0; x < m_width; x++)
						{
							int pixelOffset = x * 4;
							AveragePremultiplied(aboveRow + pixelOffset, belowRow + pixelOffset, destinationRow + pixelOffset);
						}
						continue;
					}
					if (above >= 0)
					{
						byte* aboveRow = m_base + ((long)above * m_rowBytes);
						Buffer.MemoryCopy(aboveRow, destinationRow, rowLength, rowLength);
						continue;
					}
					if (below < m_height)
					{
						byte* belowRow = m_base + ((long)below * m_rowBytes);
						Buffer.MemoryCopy(belowRow, destinationRow, rowLength, rowLength);
					}
				}
			}
		}

		public static unsafe void DeInterlace(SKBitmap bitmap, int eliminate, int fill)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			if (width <= 0 || height <= 0)
			{
				return;
			}
			int rowBytes = bitmap.RowBytes;
			int firstReplacedRow = 1;
			if (eliminate == 1)
			{
				firstReplacedRow = 0;
			}
			int replacedCount = (height - firstReplacedRow + 1) / 2;
			if (replacedCount <= 0)
			{
				return;
			}
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			if (fill == 0)
			{
				long rowLength = (long)width * 4;
				for (int index = 0; index < replacedCount; index++)
				{
					int y = firstReplacedRow + (index * 2);
					int source = y - 1;
					if (source < 0)
					{
						source = y + 1;
					}
					if (source >= height)
					{
						continue;
					}
					byte* sourceRow = basePointer + ((long)source * rowBytes);
					byte* destinationRow = basePointer + ((long)y * rowBytes);
					Buffer.MemoryCopy(sourceRow, destinationRow, rowLength, rowLength);
				}
				return;
			}
			InterpolateWorker worker = new InterpolateWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_firstReplacedRow = firstReplacedRow;
			RowBands.Run(0, replacedCount, worker.Band);
		}
	}
}
