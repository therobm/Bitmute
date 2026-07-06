using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public enum eOffsetEdge
	{
		Wrap,
		RepeatEdge,
		Transparent
	}

	public static class FilterOther
	{
		private sealed unsafe class OffsetWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public byte* m_destinationBase;
			public int m_destinationRowBytes;
			public int m_width;
			public int m_height;
			public int m_offsetX;
			public int m_offsetY;
			public eOffsetEdge m_edge;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationRowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* destination = destinationRow + (x * 4);
						int srcX = x - m_offsetX;
						int srcY = y - m_offsetY;
						if (m_edge == eOffsetEdge.Wrap)
						{
							srcX = ((srcX % m_width) + m_width) % m_width;
							srcY = ((srcY % m_height) + m_height) % m_height;
						}
						else if (m_edge == eOffsetEdge.RepeatEdge)
						{
							if (srcX < 0)
							{
								srcX = 0;
							}
							if (srcX > m_width - 1)
							{
								srcX = m_width - 1;
							}
							if (srcY < 0)
							{
								srcY = 0;
							}
							if (srcY > m_height - 1)
							{
								srcY = m_height - 1;
							}
						}
						else
						{
							if (srcX < 0 || srcX >= m_width || srcY < 0 || srcY >= m_height)
							{
								destination[0] = 0;
								destination[1] = 0;
								destination[2] = 0;
								destination[3] = 0;
								continue;
							}
						}
						byte* source = m_sourceBase + ((long)srcY * m_sourceRowBytes) + (srcX * 4);
						destination[0] = source[0];
						destination[1] = source[1];
						destination[2] = source[2];
						destination[3] = source[3];
					}
				}
			}
		}

		public static unsafe void Offset(SKBitmap bitmap, int offsetX, int offsetY, eOffsetEdge edge)
		{
			if (bitmap == null)
			{
				return;
			}
			int width = bitmap.Width;
			int height = bitmap.Height;
			if (width < 1 || height < 1)
			{
				return;
			}
			int destinationRowBytes = bitmap.RowBytes;
			SKBitmap source = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			int sourceRowBytes = source.RowBytes;
			byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			long rowLength = (long)width * 4;
			for (int y = 0; y < height; y++)
			{
				byte* destinationRow = destinationBase + ((long)y * destinationRowBytes);
				byte* sourceRow = sourceBase + ((long)y * sourceRowBytes);
				Buffer.MemoryCopy(destinationRow, sourceRow, rowLength, rowLength);
			}
			OffsetWorker worker = new OffsetWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_sourceRowBytes = sourceRowBytes;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationRowBytes = destinationRowBytes;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_offsetX = offsetX;
			worker.m_offsetY = offsetY;
			worker.m_edge = edge;
			RowBands.Run(0, height, worker.Band);
			source.Dispose();
		}
	}
}
