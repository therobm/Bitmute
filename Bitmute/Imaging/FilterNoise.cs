using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterNoise
	{
		private const int DespeckleThresholdScaled = 24000;
		private const int MinimumRadius = 1;
		private const int MaximumRadius = 16;

		private sealed unsafe class PremultiplyWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceStride);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						int pixelOffset = x * 4;
						int alpha = sourceRow[pixelOffset + 3];
						destinationRow[pixelOffset + 0] = (byte)(((sourceRow[pixelOffset + 0] * alpha) + 127) / 255);
						destinationRow[pixelOffset + 1] = (byte)(((sourceRow[pixelOffset + 1] * alpha) + 127) / 255);
						destinationRow[pixelOffset + 2] = (byte)(((sourceRow[pixelOffset + 2] * alpha) + 127) / 255);
						destinationRow[pixelOffset + 3] = (byte)alpha;
					}
				}
			}
		}

		private sealed unsafe class DespeckleWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_height;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						int minLuma = int.MaxValue;
						int maxLuma = int.MinValue;
						int sumRed = 0;
						int sumGreen = 0;
						int sumBlue = 0;
						int sumAlpha = 0;
						int count = 0;
						for (int neighborY = y - 1; neighborY <= y + 1; neighborY++)
						{
							if (neighborY < 0 || neighborY >= m_height)
							{
								continue;
							}
							byte* sourceRow = m_sourceBase + ((long)neighborY * m_sourceStride);
							for (int neighborX = x - 1; neighborX <= x + 1; neighborX++)
							{
								if (neighborX < 0 || neighborX >= m_width)
								{
									continue;
								}
								if (neighborX == x && neighborY == y)
								{
									continue;
								}
								byte* sample = sourceRow + (neighborX * 4);
								int luma = (299 * sample[0]) + (587 * sample[1]) + (114 * sample[2]);
								if (luma < minLuma)
								{
									minLuma = luma;
								}
								if (luma > maxLuma)
								{
									maxLuma = luma;
								}
								sumRed += sample[0];
								sumGreen += sample[1];
								sumBlue += sample[2];
								sumAlpha += sample[3];
								count++;
							}
						}
						if (count == 0)
						{
							continue;
						}
						if (maxLuma - minLuma >= DespeckleThresholdScaled)
						{
							continue;
						}
						int averageRed = (sumRed + (count / 2)) / count;
						int averageGreen = (sumGreen + (count / 2)) / count;
						int averageBlue = (sumBlue + (count / 2)) / count;
						int averageAlpha = (sumAlpha + (count / 2)) / count;
						byte* destinationPixel = destinationRow + (x * 4);
						destinationPixel[0] = UnpremultiplyChannel(averageRed, averageAlpha);
						destinationPixel[1] = UnpremultiplyChannel(averageGreen, averageAlpha);
						destinationPixel[2] = UnpremultiplyChannel(averageBlue, averageAlpha);
						destinationPixel[3] = (byte)averageAlpha;
					}
				}
			}
		}

		private sealed unsafe class MedianWorker
		{
			public byte* m_sourceBase;
			public byte* m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public int m_width;
			public int m_height;
			public int m_radius;

			public void Band(int start, int end)
			{
				int[] histogramRed = new int[256];
				int[] histogramGreen = new int[256];
				int[] histogramBlue = new int[256];
				int[] histogramAlpha = new int[256];
				for (int y = start; y < end; y++)
				{
					Array.Clear(histogramRed, 0, 256);
					Array.Clear(histogramGreen, 0, 256);
					Array.Clear(histogramBlue, 0, 256);
					Array.Clear(histogramAlpha, 0, 256);
					int top = y - m_radius;
					if (top < 0)
					{
						top = 0;
					}
					int bottom = y + m_radius;
					if (bottom > m_height - 1)
					{
						bottom = m_height - 1;
					}
					int rowsInWindow = bottom - top + 1;
					int initialRight = m_radius;
					if (initialRight > m_width - 1)
					{
						initialRight = m_width - 1;
					}
					int count = 0;
					for (int column = 0; column <= initialRight; column++)
					{
						AddColumn(column, top, bottom, histogramRed, histogramGreen, histogramBlue, histogramAlpha);
						count += rowsInWindow;
					}
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						int rank = (count / 2) + 1;
						int medianRed = HistogramRank(histogramRed, rank);
						int medianGreen = HistogramRank(histogramGreen, rank);
						int medianBlue = HistogramRank(histogramBlue, rank);
						int medianAlpha = HistogramRank(histogramAlpha, rank);
						byte* destinationPixel = destinationRow + (x * 4);
						destinationPixel[0] = UnpremultiplyChannel(medianRed, medianAlpha);
						destinationPixel[1] = UnpremultiplyChannel(medianGreen, medianAlpha);
						destinationPixel[2] = UnpremultiplyChannel(medianBlue, medianAlpha);
						destinationPixel[3] = (byte)medianAlpha;
						int leavingColumn = x - m_radius;
						if (leavingColumn >= 0)
						{
							RemoveColumn(leavingColumn, top, bottom, histogramRed, histogramGreen, histogramBlue, histogramAlpha);
							count -= rowsInWindow;
						}
						int enteringColumn = x + m_radius + 1;
						if (enteringColumn <= m_width - 1)
						{
							AddColumn(enteringColumn, top, bottom, histogramRed, histogramGreen, histogramBlue, histogramAlpha);
							count += rowsInWindow;
						}
					}
				}
			}

			private void AddColumn(int column, int top, int bottom, int[] histogramRed, int[] histogramGreen, int[] histogramBlue, int[] histogramAlpha)
			{
				int columnOffset = column * 4;
				for (int row = top; row <= bottom; row++)
				{
					byte* sample = m_sourceBase + ((long)row * m_sourceStride) + columnOffset;
					histogramRed[sample[0]]++;
					histogramGreen[sample[1]]++;
					histogramBlue[sample[2]]++;
					histogramAlpha[sample[3]]++;
				}
			}

			private void RemoveColumn(int column, int top, int bottom, int[] histogramRed, int[] histogramGreen, int[] histogramBlue, int[] histogramAlpha)
			{
				int columnOffset = column * 4;
				for (int row = top; row <= bottom; row++)
				{
					byte* sample = m_sourceBase + ((long)row * m_sourceStride) + columnOffset;
					histogramRed[sample[0]]--;
					histogramGreen[sample[1]]--;
					histogramBlue[sample[2]]--;
					histogramAlpha[sample[3]]--;
				}
			}
		}

		private static int HistogramRank(int[] histogram, int rank)
		{
			int cumulative = 0;
			for (int value = 0; value < 256; value++)
			{
				cumulative += histogram[value];
				if (cumulative >= rank)
				{
					return value;
				}
			}
			return 255;
		}

		private static byte UnpremultiplyChannel(int premultiplied, int alpha)
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
			return (byte)value;
		}

		private static unsafe void Premultiply(SKBitmap source, SKBitmap destination)
		{
			int width = source.Width;
			int height = source.Height;
			int sourceStride = source.RowBytes;
			int destinationStride = destination.RowBytes;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			PremultiplyWorker worker = new PremultiplyWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = sourceStride;
			worker.m_destinationStride = destinationStride;
			worker.m_width = width;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void Despeckle(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			if (width <= 0 || height <= 0)
			{
				return;
			}
			SKBitmap source = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, source);
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			DespeckleWorker worker = new DespeckleWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = source.RowBytes;
			worker.m_destinationStride = bitmap.RowBytes;
			worker.m_width = width;
			worker.m_height = height;
			RowBands.Run(0, height, worker.Band);
			source.Dispose();
		}

		public static unsafe void Median(SKBitmap bitmap, int radius)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			if (width <= 0 || height <= 0)
			{
				return;
			}
			if (radius < MinimumRadius)
			{
				radius = MinimumRadius;
			}
			if (radius > MaximumRadius)
			{
				radius = MaximumRadius;
			}
			SKBitmap source = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			Premultiply(bitmap, source);
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			MedianWorker worker = new MedianWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = source.RowBytes;
			worker.m_destinationStride = bitmap.RowBytes;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_radius = radius;
			RowBands.Run(0, height, worker.Band);
			source.Dispose();
		}
	}
}
