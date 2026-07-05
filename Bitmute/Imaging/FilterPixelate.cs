using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterPixelate
	{
		private static readonly int[] s_fragmentOffsetX = new int[] { -4, 4, 0, 0 };
		private static readonly int[] s_fragmentOffsetY = new int[] { 0, 0, -4, 4 };

		private static int ClampCellSize(int cellSize)
		{
			if (cellSize < 3)
			{
				return 3;
			}
			if (cellSize > 300)
			{
				return 300;
			}
			return cellSize;
		}

		private static int Hash(int cellX, int cellY, int seed)
		{
			int value = (cellX * 374761393) ^ (cellY * 668265263) ^ (seed * 974634617);
			value = value ^ (value >> 13);
			value = value * 1274126177;
			value = value ^ (value >> 16);
			return value & 0x7FFFFFFF;
		}

		private static byte PremultiplyChannel(int channel, int alpha)
		{
			return (byte)(((channel * alpha) + 127) / 255);
		}

		private static byte UnpremultiplyChannel(int channel, int alpha)
		{
			int value = ((channel * 255) + (alpha / 2)) / alpha;
			if (value > 255)
			{
				value = 255;
			}
			return (byte)value;
		}

		private static int NearestSite(int x, int y, int cellSize, int cellsX, int cellsY, int[] siteX, int[] siteY)
		{
			int cellX = x / cellSize;
			int cellY = y / cellSize;
			int bestIndex = -1;
			long bestDistance = long.MaxValue;
			for (int neighborY = cellY - 1; neighborY <= cellY + 1; neighborY++)
			{
				if (neighborY < 0 || neighborY >= cellsY)
				{
					continue;
				}
				for (int neighborX = cellX - 1; neighborX <= cellX + 1; neighborX++)
				{
					if (neighborX < 0 || neighborX >= cellsX)
					{
						continue;
					}
					int index = (neighborY * cellsX) + neighborX;
					int deltaX = x - siteX[index];
					int deltaY = y - siteY[index];
					long distance = ((long)deltaX * deltaX) + ((long)deltaY * deltaY);
					if (distance < bestDistance)
					{
						bestDistance = distance;
						bestIndex = index;
					}
				}
			}
			return bestIndex;
		}

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
						destinationRow[pixelOffset + 0] = PremultiplyChannel(sourceRow[pixelOffset + 0], alpha);
						destinationRow[pixelOffset + 1] = PremultiplyChannel(sourceRow[pixelOffset + 1], alpha);
						destinationRow[pixelOffset + 2] = PremultiplyChannel(sourceRow[pixelOffset + 2], alpha);
						destinationRow[pixelOffset + 3] = (byte)alpha;
					}
				}
			}
		}

		private sealed unsafe class CrystallizeWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_cellSize;
			public int m_cellsX;
			public int m_cellsY;
			public int[] m_siteX;
			public int[] m_siteY;
			public byte[] m_siteRed;
			public byte[] m_siteGreen;
			public byte[] m_siteBlue;
			public byte[] m_siteAlpha;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						int site = NearestSite(x, y, m_cellSize, m_cellsX, m_cellsY, m_siteX, m_siteY);
						byte* pixel = row + (x * 4);
						pixel[0] = m_siteRed[site];
						pixel[1] = m_siteGreen[site];
						pixel[2] = m_siteBlue[site];
						pixel[3] = m_siteAlpha[site];
					}
				}
			}
		}

		private sealed unsafe class CrystallizeAccumulateWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_cellSize;
			public int m_cellsX;
			public int m_cellsY;
			public int[] m_siteX;
			public int[] m_siteY;
			public long[] m_sumRed;
			public long[] m_sumGreen;
			public long[] m_sumBlue;
			public long[] m_sumAlpha;
			public int[] m_counts;
			public object m_mergeLock;

			public void Band(int start, int end)
			{
				int cellRowFirst = (start / m_cellSize) - 1;
				if (cellRowFirst < 0)
				{
					cellRowFirst = 0;
				}
				int cellRowLast = ((end - 1) / m_cellSize) + 1;
				if (cellRowLast > m_cellsY - 1)
				{
					cellRowLast = m_cellsY - 1;
				}
				int localBase = cellRowFirst * m_cellsX;
				int localCount = ((cellRowLast - cellRowFirst) + 1) * m_cellsX;
				long[] localRed = new long[localCount];
				long[] localGreen = new long[localCount];
				long[] localBlue = new long[localCount];
				long[] localAlpha = new long[localCount];
				int[] localCounts = new int[localCount];
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						int alpha = pixel[3];
						int site = NearestSite(x, y, m_cellSize, m_cellsX, m_cellsY, m_siteX, m_siteY);
						int local = site - localBase;
						localRed[local] += PremultiplyChannel(pixel[0], alpha);
						localGreen[local] += PremultiplyChannel(pixel[1], alpha);
						localBlue[local] += PremultiplyChannel(pixel[2], alpha);
						localAlpha[local] += alpha;
						localCounts[local]++;
					}
				}
				lock (m_mergeLock)
				{
					for (int index = 0; index < localCount; index++)
					{
						m_sumRed[localBase + index] += localRed[index];
						m_sumGreen[localBase + index] += localGreen[index];
						m_sumBlue[localBase + index] += localBlue[index];
						m_sumAlpha[localBase + index] += localAlpha[index];
						m_counts[localBase + index] += localCounts[index];
					}
				}
			}
		}

		private sealed unsafe class FacetWorker
		{
			public byte* m_sourceBase;
			public int m_sourceStride;
			public byte* m_destinationBase;
			public int m_destinationStride;
			public int m_width;
			public int m_height;

			private void Window(int startX, int startY, out long outRed, out long outGreen, out long outBlue, out long outAlpha, out long outVariance)
			{
				long sumRed = 0;
				long sumGreen = 0;
				long sumBlue = 0;
				long sumAlpha = 0;
				long squareRed = 0;
				long squareGreen = 0;
				long squareBlue = 0;
				for (int windowY = 0; windowY < 3; windowY++)
				{
					int sampleY = startY + windowY;
					if (sampleY < 0)
					{
						sampleY = 0;
					}
					if (sampleY > m_height - 1)
					{
						sampleY = m_height - 1;
					}
					byte* row = m_sourceBase + ((long)sampleY * m_sourceStride);
					for (int windowX = 0; windowX < 3; windowX++)
					{
						int sampleX = startX + windowX;
						if (sampleX < 0)
						{
							sampleX = 0;
						}
						if (sampleX > m_width - 1)
						{
							sampleX = m_width - 1;
						}
						byte* pixel = row + (sampleX * 4);
						int red = pixel[0];
						int green = pixel[1];
						int blue = pixel[2];
						sumRed += red;
						sumGreen += green;
						sumBlue += blue;
						sumAlpha += pixel[3];
						squareRed += red * red;
						squareGreen += green * green;
						squareBlue += blue * blue;
					}
				}
				outRed = sumRed;
				outGreen = sumGreen;
				outBlue = sumBlue;
				outAlpha = sumAlpha;
				outVariance = ((9 * squareRed) - (sumRed * sumRed)) + ((9 * squareGreen) - (sumGreen * sumGreen)) + ((9 * squareBlue) - (sumBlue * sumBlue));
			}

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < m_width; x++)
					{
						long bestRed;
						long bestGreen;
						long bestBlue;
						long bestAlpha;
						long bestVariance;
						Window(x - 2, y - 2, out bestRed, out bestGreen, out bestBlue, out bestAlpha, out bestVariance);
						long candidateRed;
						long candidateGreen;
						long candidateBlue;
						long candidateAlpha;
						long candidateVariance;
						Window(x, y - 2, out candidateRed, out candidateGreen, out candidateBlue, out candidateAlpha, out candidateVariance);
						if (candidateVariance < bestVariance)
						{
							bestRed = candidateRed;
							bestGreen = candidateGreen;
							bestBlue = candidateBlue;
							bestAlpha = candidateAlpha;
							bestVariance = candidateVariance;
						}
						Window(x - 2, y, out candidateRed, out candidateGreen, out candidateBlue, out candidateAlpha, out candidateVariance);
						if (candidateVariance < bestVariance)
						{
							bestRed = candidateRed;
							bestGreen = candidateGreen;
							bestBlue = candidateBlue;
							bestAlpha = candidateAlpha;
							bestVariance = candidateVariance;
						}
						Window(x, y, out candidateRed, out candidateGreen, out candidateBlue, out candidateAlpha, out candidateVariance);
						if (candidateVariance < bestVariance)
						{
							bestRed = candidateRed;
							bestGreen = candidateGreen;
							bestBlue = candidateBlue;
							bestAlpha = candidateAlpha;
							bestVariance = candidateVariance;
						}
						int meanRed = (int)((bestRed + 4) / 9);
						int meanGreen = (int)((bestGreen + 4) / 9);
						int meanBlue = (int)((bestBlue + 4) / 9);
						int meanAlpha = (int)((bestAlpha + 4) / 9);
						byte* destinationPixel = destinationRow + (x * 4);
						if (meanAlpha == 0)
						{
							destinationPixel[0] = 0;
							destinationPixel[1] = 0;
							destinationPixel[2] = 0;
							destinationPixel[3] = 0;
						}
						else
						{
							destinationPixel[0] = UnpremultiplyChannel(meanRed, meanAlpha);
							destinationPixel[1] = UnpremultiplyChannel(meanGreen, meanAlpha);
							destinationPixel[2] = UnpremultiplyChannel(meanBlue, meanAlpha);
							destinationPixel[3] = (byte)meanAlpha;
						}
					}
				}
			}
		}

		private sealed unsafe class FragmentWorker
		{
			public byte* m_sourceBase;
			public int m_sourceStride;
			public byte* m_destinationBase;
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
						int sumRed = 0;
						int sumGreen = 0;
						int sumBlue = 0;
						int sumAlpha = 0;
						for (int index = 0; index < 4; index++)
						{
							int sampleX = x + s_fragmentOffsetX[index];
							if (sampleX < 0)
							{
								sampleX = 0;
							}
							if (sampleX > m_width - 1)
							{
								sampleX = m_width - 1;
							}
							int sampleY = y + s_fragmentOffsetY[index];
							if (sampleY < 0)
							{
								sampleY = 0;
							}
							if (sampleY > m_height - 1)
							{
								sampleY = m_height - 1;
							}
							byte* samplePixel = m_sourceBase + ((long)sampleY * m_sourceStride) + (sampleX * 4);
							sumRed += samplePixel[0];
							sumGreen += samplePixel[1];
							sumBlue += samplePixel[2];
							sumAlpha += samplePixel[3];
						}
						int averageRed = (sumRed + 2) / 4;
						int averageGreen = (sumGreen + 2) / 4;
						int averageBlue = (sumBlue + 2) / 4;
						int averageAlpha = (sumAlpha + 2) / 4;
						byte* destinationPixel = destinationRow + (x * 4);
						if (averageAlpha == 0)
						{
							destinationPixel[0] = 0;
							destinationPixel[1] = 0;
							destinationPixel[2] = 0;
							destinationPixel[3] = 0;
						}
						else
						{
							destinationPixel[0] = UnpremultiplyChannel(averageRed, averageAlpha);
							destinationPixel[1] = UnpremultiplyChannel(averageGreen, averageAlpha);
							destinationPixel[2] = UnpremultiplyChannel(averageBlue, averageAlpha);
							destinationPixel[3] = (byte)averageAlpha;
						}
					}
				}
			}
		}

		private sealed unsafe class PointillizeWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_cellSize;
			public int m_cellsX;
			public int m_cellsY;
			public int[] m_dotX;
			public int[] m_dotY;
			public int[] m_dotRadius;
			public byte[] m_dotRed;
			public byte[] m_dotGreen;
			public byte[] m_dotBlue;
			public byte m_backgroundRed;
			public byte m_backgroundGreen;
			public byte m_backgroundBlue;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						int cellX = x / m_cellSize;
						int cellY = y / m_cellSize;
						int bestIndex = -1;
						long bestDistance = long.MaxValue;
						for (int neighborY = cellY - 1; neighborY <= cellY + 1; neighborY++)
						{
							if (neighborY < 0 || neighborY >= m_cellsY)
							{
								continue;
							}
							for (int neighborX = cellX - 1; neighborX <= cellX + 1; neighborX++)
							{
								if (neighborX < 0 || neighborX >= m_cellsX)
								{
									continue;
								}
								int index = (neighborY * m_cellsX) + neighborX;
								int deltaX = x - m_dotX[index];
								int deltaY = y - m_dotY[index];
								long distance = ((long)deltaX * deltaX) + ((long)deltaY * deltaY);
								long radius = m_dotRadius[index];
								if ((distance * 10000) > (radius * radius))
								{
									continue;
								}
								if (distance < bestDistance)
								{
									bestDistance = distance;
									bestIndex = index;
								}
							}
						}
						byte* pixel = row + (x * 4);
						if (bestIndex < 0)
						{
							pixel[0] = m_backgroundRed;
							pixel[1] = m_backgroundGreen;
							pixel[2] = m_backgroundBlue;
							pixel[3] = 255;
						}
						else
						{
							pixel[0] = m_dotRed[bestIndex];
							pixel[1] = m_dotGreen[bestIndex];
							pixel[2] = m_dotBlue[bestIndex];
							pixel[3] = 255;
						}
					}
				}
			}
		}

		private static unsafe void PremultiplyInto(SKBitmap source, SKBitmap destination)
		{
			int width = source.Width;
			int height = source.Height;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			PremultiplyWorker worker = new PremultiplyWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_destinationBase = destinationBase;
			worker.m_sourceStride = source.RowBytes;
			worker.m_destinationStride = destination.RowBytes;
			worker.m_width = width;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void Crystallize(SKBitmap bitmap, int cellSize, int seed)
		{
			cellSize = ClampCellSize(cellSize);
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			int cellsX = (width + cellSize - 1) / cellSize;
			int cellsY = (height + cellSize - 1) / cellSize;
			int siteCount = cellsX * cellsY;
			int[] siteX = new int[siteCount];
			int[] siteY = new int[siteCount];
			for (int cellY = 0; cellY < cellsY; cellY++)
			{
				for (int cellX = 0; cellX < cellsX; cellX++)
				{
					int hash = Hash(cellX, cellY, seed);
					int index = (cellY * cellsX) + cellX;
					siteX[index] = (cellX * cellSize) + (hash % cellSize);
					siteY[index] = (cellY * cellSize) + ((hash / cellSize) % cellSize);
				}
			}
			long[] sumRed = new long[siteCount];
			long[] sumGreen = new long[siteCount];
			long[] sumBlue = new long[siteCount];
			long[] sumAlpha = new long[siteCount];
			int[] counts = new int[siteCount];
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			CrystallizeAccumulateWorker accumulator = new CrystallizeAccumulateWorker();
			accumulator.m_base = basePointer;
			accumulator.m_rowBytes = rowBytes;
			accumulator.m_width = width;
			accumulator.m_cellSize = cellSize;
			accumulator.m_cellsX = cellsX;
			accumulator.m_cellsY = cellsY;
			accumulator.m_siteX = siteX;
			accumulator.m_siteY = siteY;
			accumulator.m_sumRed = sumRed;
			accumulator.m_sumGreen = sumGreen;
			accumulator.m_sumBlue = sumBlue;
			accumulator.m_sumAlpha = sumAlpha;
			accumulator.m_counts = counts;
			accumulator.m_mergeLock = new object();
			RowBands.Run(0, height, accumulator.Band);
			byte[] siteRed = new byte[siteCount];
			byte[] siteGreen = new byte[siteCount];
			byte[] siteBlue = new byte[siteCount];
			byte[] siteAlpha = new byte[siteCount];
			for (int index = 0; index < siteCount; index++)
			{
				int count = counts[index];
				if (count == 0)
				{
					continue;
				}
				int averageRed = (int)(sumRed[index] / count);
				int averageGreen = (int)(sumGreen[index] / count);
				int averageBlue = (int)(sumBlue[index] / count);
				int averageAlpha = (int)(sumAlpha[index] / count);
				if (averageAlpha == 0)
				{
					continue;
				}
				siteRed[index] = UnpremultiplyChannel(averageRed, averageAlpha);
				siteGreen[index] = UnpremultiplyChannel(averageGreen, averageAlpha);
				siteBlue[index] = UnpremultiplyChannel(averageBlue, averageAlpha);
				siteAlpha[index] = (byte)averageAlpha;
			}
			CrystallizeWorker worker = new CrystallizeWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_cellSize = cellSize;
			worker.m_cellsX = cellsX;
			worker.m_cellsY = cellsY;
			worker.m_siteX = siteX;
			worker.m_siteY = siteY;
			worker.m_siteRed = siteRed;
			worker.m_siteGreen = siteGreen;
			worker.m_siteBlue = siteBlue;
			worker.m_siteAlpha = siteAlpha;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void Facet(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap scratch = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			PremultiplyInto(bitmap, scratch);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			byte* scratchBase = (byte*)scratch.GetPixels().ToPointer();
			FacetWorker worker = new FacetWorker();
			worker.m_sourceBase = scratchBase;
			worker.m_sourceStride = scratch.RowBytes;
			worker.m_destinationBase = basePointer;
			worker.m_destinationStride = bitmap.RowBytes;
			worker.m_width = width;
			worker.m_height = height;
			RowBands.Run(0, height, worker.Band);
			scratch.Dispose();
		}

		public static unsafe void Fragment(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			SKBitmap scratch = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			PremultiplyInto(bitmap, scratch);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			byte* scratchBase = (byte*)scratch.GetPixels().ToPointer();
			FragmentWorker worker = new FragmentWorker();
			worker.m_sourceBase = scratchBase;
			worker.m_sourceStride = scratch.RowBytes;
			worker.m_destinationBase = basePointer;
			worker.m_destinationStride = bitmap.RowBytes;
			worker.m_width = width;
			worker.m_height = height;
			RowBands.Run(0, height, worker.Band);
			scratch.Dispose();
		}

		public static unsafe void Pointillize(SKBitmap bitmap, int cellSize, int seed, SKColor background)
		{
			cellSize = ClampCellSize(cellSize);
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			int cellsX = (width + cellSize - 1) / cellSize;
			int cellsY = (height + cellSize - 1) / cellSize;
			int dotCount = cellsX * cellsY;
			int[] dotX = new int[dotCount];
			int[] dotY = new int[dotCount];
			int[] dotRadius = new int[dotCount];
			byte[] dotRed = new byte[dotCount];
			byte[] dotGreen = new byte[dotCount];
			byte[] dotBlue = new byte[dotCount];
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int cellY = 0; cellY < cellsY; cellY++)
			{
				for (int cellX = 0; cellX < cellsX; cellX++)
				{
					int hash = Hash(cellX, cellY, seed);
					int index = (cellY * cellsX) + cellX;
					int centerX = (cellX * cellSize) + (hash % cellSize);
					int centerY = (cellY * cellSize) + ((hash / cellSize) % cellSize);
					dotX[index] = centerX;
					dotY[index] = centerY;
					int radiusPercent = 50 + ((hash / (cellSize * cellSize)) % 21);
					dotRadius[index] = cellSize * radiusPercent;
					int sampleX = centerX;
					if (sampleX > width - 1)
					{
						sampleX = width - 1;
					}
					int sampleY = centerY;
					if (sampleY > height - 1)
					{
						sampleY = height - 1;
					}
					byte* samplePixel = basePointer + ((long)sampleY * rowBytes) + (sampleX * 4);
					dotRed[index] = samplePixel[0];
					dotGreen[index] = samplePixel[1];
					dotBlue[index] = samplePixel[2];
				}
			}
			PointillizeWorker worker = new PointillizeWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_cellSize = cellSize;
			worker.m_cellsX = cellsX;
			worker.m_cellsY = cellsY;
			worker.m_dotX = dotX;
			worker.m_dotY = dotY;
			worker.m_dotRadius = dotRadius;
			worker.m_dotRed = dotRed;
			worker.m_dotGreen = dotGreen;
			worker.m_dotBlue = dotBlue;
			worker.m_backgroundRed = background.Red;
			worker.m_backgroundGreen = background.Green;
			worker.m_backgroundBlue = background.Blue;
			RowBands.Run(0, height, worker.Band);
		}
	}
}
