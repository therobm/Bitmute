using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class PixelRegion
	{
		private sealed unsafe class CopyPixelsWorker
		{
			public IntPtr m_sourceBase;
			public IntPtr m_destinationBase;
			public int m_sourceStride;
			public int m_destinationStride;
			public long m_rowLength;

			public void Band(int start, int end)
			{
				byte* sourceBase = (byte*)m_sourceBase.ToPointer();
				byte* destinationBase = (byte*)m_destinationBase.ToPointer();
				for (int y = start; y < end; y++)
				{
					byte* sourceRow = sourceBase + ((long)y * m_sourceStride);
					byte* destinationRow = destinationBase + ((long)y * m_destinationStride);
					Buffer.MemoryCopy(sourceRow, destinationRow, m_rowLength, m_rowLength);
				}
			}
		}

		public static void CopyPixels(SKBitmap source, SKBitmap destination)
		{
			CopyPixelsWorker worker = new CopyPixelsWorker();
			worker.m_sourceBase = source.GetPixels();
			worker.m_destinationBase = destination.GetPixels();
			worker.m_sourceStride = source.RowBytes;
			worker.m_destinationStride = destination.RowBytes;
			worker.m_rowLength = (long)source.Width * 4;
			RowBands.Run(0, source.Height, worker.Band);
		}

		public static SKRectI ComputeDirtyRect(SKBitmap before, SKBitmap after)
		{
			return ComputeDirtyRect(before, after, new SKRectI(0, 0, before.Width, before.Height));
		}

		public static unsafe SKRectI ComputeDirtyRect(SKBitmap before, SKBitmap after, SKRectI searchRect)
		{
			int width = before.Width;
			int height = before.Height;
			int scanLeft = searchRect.Left;
			int scanTop = searchRect.Top;
			int scanRight = searchRect.Right;
			int scanBottom = searchRect.Bottom;
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
			if (scanRight <= scanLeft || scanBottom <= scanTop)
			{
				return SKRectI.Empty;
			}
			byte* beforeBase = (byte*)before.GetPixels().ToPointer();
			byte* afterBase = (byte*)after.GetPixels().ToPointer();
			int beforeStride = before.RowBytes;
			int afterStride = after.RowBytes;

			int minX = width;
			int minY = height;
			int maxX = -1;
			int maxY = -1;

			for (int y = scanTop; y < scanBottom; y++)
			{
				uint* beforeRow = (uint*)(beforeBase + ((long)y * beforeStride));
				uint* afterRow = (uint*)(afterBase + ((long)y * afterStride));
				for (int x = scanLeft; x < scanRight; x++)
				{
					if (beforeRow[x] != afterRow[x])
					{
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
			}

			if (maxX < 0)
			{
				return SKRectI.Empty;
			}
			return new SKRectI(minX, minY, maxX + 1, maxY + 1);
		}

		public static unsafe SKRectI ComputeContentBounds(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);

			int minY = -1;
			for (int y = 0; y < height; y++)
			{
				if (RowHasAlpha(accessor, y, width))
				{
					minY = y;
					break;
				}
			}
			if (minY < 0)
			{
				return SKRectI.Empty;
			}
			int maxY = minY;
			for (int y = height - 1; y > minY; y--)
			{
				if (RowHasAlpha(accessor, y, width))
				{
					maxY = y;
					break;
				}
			}
			int minX = width;
			for (int x = 0; x < width; x++)
			{
				if (ColumnHasAlpha(accessor, x, minY, maxY))
				{
					minX = x;
					break;
				}
			}
			int maxX = minX;
			for (int x = width - 1; x > minX; x--)
			{
				if (ColumnHasAlpha(accessor, x, minY, maxY))
				{
					maxX = x;
					break;
				}
			}
			return new SKRectI(minX, minY, maxX + 1, maxY + 1);
		}

		private static bool RowHasAlpha(PixelAccessor accessor, int y, int width)
		{
			for (int x = 0; x < width; x++)
			{
				if (accessor.AlphaAt(x, y) > 0.0f)
				{
					return true;
				}
			}
			return false;
		}

		private static bool ColumnHasAlpha(PixelAccessor accessor, int x, int minY, int maxY)
		{
			for (int y = minY; y <= maxY; y++)
			{
				if (accessor.AlphaAt(x, y) > 0.0f)
				{
					return true;
				}
			}
			return false;
		}

		public static unsafe SKBitmap ExtractRegion(SKBitmap source, SKRectI rect)
		{
			SKBitmap region = new SKBitmap(rect.Width, rect.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			int copyLeft = rect.Left;
			int copyTop = rect.Top;
			int copyRight = rect.Right;
			int copyBottom = rect.Bottom;
			if (copyLeft < 0)
			{
				copyLeft = 0;
			}
			if (copyTop < 0)
			{
				copyTop = 0;
			}
			if (copyRight > source.Width)
			{
				copyRight = source.Width;
			}
			if (copyBottom > source.Height)
			{
				copyBottom = source.Height;
			}
			if (copyRight <= copyLeft || copyBottom <= copyTop)
			{
				return region;
			}
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			byte* regionBase = (byte*)region.GetPixels().ToPointer();
			int sourceStride = source.RowBytes;
			int regionStride = region.RowBytes;
			long rowLength = (long)(copyRight - copyLeft) * 4;
			for (int y = copyTop; y < copyBottom; y++)
			{
				byte* sourceRow = sourceBase + ((long)y * sourceStride) + (copyLeft * 4);
				byte* regionRow = regionBase + ((long)(y - rect.Top) * regionStride) + ((copyLeft - rect.Left) * 4);
				Buffer.MemoryCopy(sourceRow, regionRow, rowLength, rowLength);
			}
			return region;
		}

		public static unsafe void ApplyRegion(SKBitmap target, SKBitmap region, int x, int y)
		{
			int copyLeft = x;
			int copyTop = y;
			int copyRight = x + region.Width;
			int copyBottom = y + region.Height;
			if (copyLeft < 0)
			{
				copyLeft = 0;
			}
			if (copyTop < 0)
			{
				copyTop = 0;
			}
			if (copyRight > target.Width)
			{
				copyRight = target.Width;
			}
			if (copyBottom > target.Height)
			{
				copyBottom = target.Height;
			}
			if (copyRight <= copyLeft || copyBottom <= copyTop)
			{
				return;
			}
			byte* regionBase = (byte*)region.GetPixels().ToPointer();
			byte* targetBase = (byte*)target.GetPixels().ToPointer();
			int regionStride = region.RowBytes;
			int targetStride = target.RowBytes;
			long rowLength = (long)(copyRight - copyLeft) * 4;
			for (int row = copyTop; row < copyBottom; row++)
			{
				byte* sourceRow = regionBase + ((long)(row - y) * regionStride) + ((copyLeft - x) * 4);
				byte* targetRow = targetBase + ((long)row * targetStride) + (copyLeft * 4);
				Buffer.MemoryCopy(sourceRow, targetRow, rowLength, rowLength);
			}
		}
	}
}
