using System.Runtime.InteropServices;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class PixelRegion
	{
		private static byte[] ReadBytes(SKBitmap bitmap)
		{
			int count = bitmap.ByteCount;
			byte[] bytes = new byte[count];
			Marshal.Copy(bitmap.GetPixels(), bytes, 0, count);
			return bytes;
		}

		public static SKRectI ComputeDirtyRect(SKBitmap before, SKBitmap after)
		{
			return ComputeDirtyRect(before, after, new SKRectI(0, 0, before.Width, before.Height));
		}

		public static SKRectI ComputeDirtyRect(SKBitmap before, SKBitmap after, SKRectI searchRect)
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
			byte[] beforeBytes = ReadBytes(before);
			byte[] afterBytes = ReadBytes(after);
			int rowBytes = width * 4;

			int minX = width;
			int minY = height;
			int maxX = -1;
			int maxY = -1;

			for (int y = scanTop; y < scanBottom; y++)
			{
				int rowStart = y * rowBytes;
				for (int x = scanLeft; x < scanRight; x++)
				{
					int index = rowStart + (x * 4);
					bool differs = beforeBytes[index] != afterBytes[index] || beforeBytes[index + 1] != afterBytes[index + 1] || beforeBytes[index + 2] != afterBytes[index + 2] || beforeBytes[index + 3] != afterBytes[index + 3];
					if (differs)
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

		public static SKRectI ComputeContentBounds(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			byte[] bytes = ReadBytes(bitmap);
			int rowBytes = width * 4;

			int minX = width;
			int minY = height;
			int maxX = -1;
			int maxY = -1;

			for (int y = 0; y < height; y++)
			{
				int rowStart = y * rowBytes;
				for (int x = 0; x < width; x++)
				{
					int index = rowStart + (x * 4);
					byte alpha = bytes[index + 3];
					if (alpha != 0)
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

		public static SKBitmap ExtractRegion(SKBitmap source, SKRectI rect)
		{
			SKBitmap region = new SKBitmap(rect.Width, rect.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKCanvas canvas = new SKCanvas(region);
			SKImage image = SKImage.FromBitmap(source);
			SKRect sourceRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
			SKRect destinationRect = new SKRect(0.0f, 0.0f, rect.Width, rect.Height);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			canvas.DrawImage(image, sourceRect, destinationRect, sampling, paint);
			paint.Dispose();
			image.Dispose();
			canvas.Dispose();
			return region;
		}

		public static void ApplyRegion(SKBitmap target, SKBitmap region, int x, int y)
		{
			SKCanvas canvas = new SKCanvas(target);
			SKImage image = SKImage.FromBitmap(region);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			paint.BlendMode = SKBlendMode.Src;
			canvas.DrawImage(image, x, y, sampling, paint);
			paint.Dispose();
			image.Dispose();
			canvas.Dispose();
		}
	}
}
