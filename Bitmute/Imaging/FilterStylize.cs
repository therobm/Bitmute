using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterStylize
	{
		private static byte ClampByte(double value)
		{
			if (value < 0.0)
			{
				return 0;
			}
			if (value > 255.0)
			{
				return 255;
			}
			return (byte)Math.Round(value);
		}

		private static int ClampIndex(int value, int maximum)
		{
			if (value < 0)
			{
				return 0;
			}
			if (value > maximum)
			{
				return maximum;
			}
			return value;
		}

		private static int WeightedLuma(byte red, byte green, byte blue)
		{
			return (299 * red) + (587 * green) + (114 * blue);
		}

		private static byte SolarizeChannel(byte value)
		{
			if (value < 128)
			{
				return value;
			}
			return (byte)(255 - value);
		}

		public static uint HashCoordinates(int x, int y, int seed)
		{
			uint value = (uint)x * 374761393u;
			value = value + ((uint)y * 668265263u);
			value = value + ((uint)seed * 2246822519u);
			value = value ^ (value >> 13);
			value = value * 1274126177u;
			value = value ^ (value >> 16);
			return value;
		}

		private static unsafe SKBitmap CloneUnpremul(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int sourceRowBytes = bitmap.RowBytes;
			SKBitmap clone = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			int cloneRowBytes = clone.RowBytes;
			byte* sourceBase = (byte*)bitmap.GetPixels().ToPointer();
			byte* cloneBase = (byte*)clone.GetPixels().ToPointer();
			long rowLength = (long)width * 4;
			for (int y = 0; y < height; y++)
			{
				byte* sourceRow = sourceBase + ((long)y * sourceRowBytes);
				byte* cloneRow = cloneBase + ((long)y * cloneRowBytes);
				Buffer.MemoryCopy(sourceRow, cloneRow, rowLength, rowLength);
			}
			return clone;
		}

		private sealed unsafe class DiffuseWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public byte* m_destinationBase;
			public int m_destinationRowBytes;
			public int m_width;
			public int m_height;
			public int m_mode;
			public int m_seed;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceRowBytes);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationRowBytes);
					for (int x = 0; x < m_width; x++)
					{
						uint hash = HashCoordinates(x, y, m_seed);
						int dx = (int)(hash % 5u) - 2;
						int dy = (int)((hash / 5u) % 5u) - 2;
						int sampleX = ClampIndex(x + dx, m_width - 1);
						int sampleY = ClampIndex(y + dy, m_height - 1);
						byte* candidate = m_sourceBase + ((long)sampleY * m_sourceRowBytes) + (sampleX * 4);
						byte* current = sourceRow + (x * 4);
						bool take = true;
						if (m_mode == 1)
						{
							int candidateLuma = WeightedLuma(candidate[0], candidate[1], candidate[2]);
							int currentLuma = WeightedLuma(current[0], current[1], current[2]);
							take = candidateLuma < currentLuma;
						}
						if (m_mode == 2)
						{
							int candidateLuma = WeightedLuma(candidate[0], candidate[1], candidate[2]);
							int currentLuma = WeightedLuma(current[0], current[1], current[2]);
							take = candidateLuma > currentLuma;
						}
						byte* destination = destinationRow + (x * 4);
						if (take)
						{
							destination[0] = candidate[0];
							destination[1] = candidate[1];
							destination[2] = candidate[2];
							destination[3] = candidate[3];
						}
						else
						{
							destination[0] = current[0];
							destination[1] = current[1];
							destination[2] = current[2];
							destination[3] = current[3];
						}
					}
				}
			}
		}

		private sealed unsafe class EmbossWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public byte* m_destinationBase;
			public int m_destinationRowBytes;
			public int m_width;
			public int m_height;
			public int m_offsetX;
			public int m_offsetY;
			public double m_strength;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = m_sourceBase + ((long)y * m_sourceRowBytes);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationRowBytes);
					int sampleY = ClampIndex(y + m_offsetY, m_height - 1);
					byte* offsetRow = m_sourceBase + ((long)sampleY * m_sourceRowBytes);
					for (int x = 0; x < m_width; x++)
					{
						int sampleX = ClampIndex(x + m_offsetX, m_width - 1);
						byte* current = sourceRow + (x * 4);
						byte* offsetPixel = offsetRow + (sampleX * 4);
						byte* destination = destinationRow + (x * 4);
						destination[0] = ClampByte(128.0 + (m_strength * (current[0] - offsetPixel[0])));
						destination[1] = ClampByte(128.0 + (m_strength * (current[1] - offsetPixel[1])));
						destination[2] = ClampByte(128.0 + (m_strength * (current[2] - offsetPixel[2])));
						destination[3] = current[3];
					}
				}
			}
		}

		private sealed unsafe class FindEdgesWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public byte* m_destinationBase;
			public int m_destinationRowBytes;
			public int m_width;
			public int m_height;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					int yAbove = ClampIndex(y - 1, m_height - 1);
					int yBelow = ClampIndex(y + 1, m_height - 1);
					byte* topRow = m_sourceBase + ((long)yAbove * m_sourceRowBytes);
					byte* midRow = m_sourceBase + ((long)y * m_sourceRowBytes);
					byte* botRow = m_sourceBase + ((long)yBelow * m_sourceRowBytes);
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationRowBytes);
					for (int x = 0; x < m_width; x++)
					{
						int left = ClampIndex(x - 1, m_width - 1) * 4;
						int mid = x * 4;
						int right = ClampIndex(x + 1, m_width - 1) * 4;
						byte* destination = destinationRow + mid;
						for (int channel = 0; channel < 3; channel++)
						{
							int gx = (topRow[right + channel] + (2 * midRow[right + channel]) + botRow[right + channel]) - (topRow[left + channel] + (2 * midRow[left + channel]) + botRow[left + channel]);
							int gy = (botRow[left + channel] + (2 * botRow[mid + channel]) + botRow[right + channel]) - (topRow[left + channel] + (2 * topRow[mid + channel]) + topRow[right + channel]);
							double magnitude = Math.Sqrt((double)((gx * gx) + (gy * gy)));
							byte clamped = ClampByte(magnitude);
							destination[channel] = (byte)(255 - clamped);
						}
						destination[3] = midRow[mid + 3];
					}
				}
			}
		}

		private sealed unsafe class SolarizeWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						pixel[0] = SolarizeChannel(pixel[0]);
						pixel[1] = SolarizeChannel(pixel[1]);
						pixel[2] = SolarizeChannel(pixel[2]);
					}
				}
			}
		}

		public static unsafe void Diffuse(SKBitmap bitmap, int mode, int seed)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			SKBitmap source = CloneUnpremul(bitmap);
			int sourceRowBytes = source.RowBytes;
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			DiffuseWorker worker = new DiffuseWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_sourceRowBytes = sourceRowBytes;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationRowBytes = rowBytes;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_mode = mode;
			worker.m_seed = seed;
			RowBands.Run(0, height, worker.Band);
			source.Dispose();
		}

		public static unsafe void Emboss(SKBitmap bitmap, int angle, int height, int amount)
		{
			int width = bitmap.Width;
			int bitmapHeight = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			double radians = angle * (Math.PI / 180.0);
			int offsetX = (int)Math.Round(Math.Cos(radians) * height);
			int offsetY = (int)Math.Round(-Math.Sin(radians) * height);
			double strength = amount / 100.0;
			SKBitmap source = CloneUnpremul(bitmap);
			int sourceRowBytes = source.RowBytes;
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			EmbossWorker worker = new EmbossWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_sourceRowBytes = sourceRowBytes;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationRowBytes = rowBytes;
			worker.m_width = width;
			worker.m_height = bitmapHeight;
			worker.m_offsetX = offsetX;
			worker.m_offsetY = offsetY;
			worker.m_strength = strength;
			RowBands.Run(0, bitmapHeight, worker.Band);
			source.Dispose();
		}

		public static unsafe void FindEdges(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			SKBitmap source = CloneUnpremul(bitmap);
			int sourceRowBytes = source.RowBytes;
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			FindEdgesWorker worker = new FindEdgesWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_sourceRowBytes = sourceRowBytes;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationRowBytes = rowBytes;
			worker.m_width = width;
			worker.m_height = height;
			RowBands.Run(0, height, worker.Band);
			source.Dispose();
		}

		public static unsafe void Solarize(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			SolarizeWorker worker = new SolarizeWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			RowBands.Run(0, height, worker.Band);
		}
	}
}
