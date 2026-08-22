using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class ChannelPlane
	{
		private static float ChannelValue(float red, float green, float blue, float alpha, int channel)
		{
			if (channel == 1)
			{
				return green;
			}
			if (channel == 2)
			{
				return blue;
			}
			if (channel == 3)
			{
				return alpha;
			}
			return red;
		}

		private static unsafe void CopyChannelToGray(SKBitmap source, int sourceLeft, int sourceTop, SKBitmap plane, int width, int height, int channel)
		{
			PixelAccessor sourcePixels = new PixelAccessor(source.GetPixels(), source.RowBytes, source.ColorType);
			PixelAccessor planePixels = new PixelAccessor(plane.GetPixels(), plane.RowBytes, plane.ColorType);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float red = 0.0f;
					float green = 0.0f;
					float blue = 0.0f;
					float alpha = 0.0f;
					sourcePixels.ReadNormalized(sourceLeft + x, sourceTop + y, out red, out green, out blue, out alpha);
					float value = ChannelValue(red, green, blue, alpha, channel);
					planePixels.WriteNormalized(x, y, value, value, value, 1.0f);
				}
			}
		}

		private static unsafe void WriteGrayToChannel(SKBitmap target, SKBitmap source, int channel, int offsetX, int offsetY, int startX, int startY, int endX, int endY)
		{
			PixelAccessor sourcePixels = new PixelAccessor(source.GetPixels(), source.RowBytes, source.ColorType);
			PixelAccessor targetPixels = new PixelAccessor(target.GetPixels(), target.RowBytes, target.ColorType);
			for (int y = startY; y < endY; y++)
			{
				for (int x = startX; x < endX; x++)
				{
					float sourceRed = 0.0f;
					float sourceGreen = 0.0f;
					float sourceBlue = 0.0f;
					float sourceAlpha = 0.0f;
					sourcePixels.ReadNormalized(x, y, out sourceRed, out sourceGreen, out sourceBlue, out sourceAlpha);
					float gray = ((sourceRed * 77.0f) + (sourceGreen * 150.0f) + (sourceBlue * 29.0f)) / 256.0f;
					int targetX = offsetX + x;
					int targetY = offsetY + y;
					float targetRed = 0.0f;
					float targetGreen = 0.0f;
					float targetBlue = 0.0f;
					float targetAlpha = 0.0f;
					targetPixels.ReadNormalized(targetX, targetY, out targetRed, out targetGreen, out targetBlue, out targetAlpha);
					if (channel == 1)
					{
						targetGreen = gray;
					}
					else if (channel == 2)
					{
						targetBlue = gray;
					}
					else if (channel == 3)
					{
						targetAlpha = gray;
					}
					else
					{
						targetRed = gray;
					}
					targetPixels.WriteNormalized(targetX, targetY, targetRed, targetGreen, targetBlue, targetAlpha);
				}
			}
		}

		public static unsafe SKBitmap Extract(SKBitmap source, SKRectI region, int channel)
		{
			if (source == null)
			{
				return null;
			}
			if (channel < 0 || channel > 3)
			{
				return null;
			}
			int left = region.Left;
			int top = region.Top;
			int right = region.Right;
			int bottom = region.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > source.Width)
			{
				right = source.Width;
			}
			if (bottom > source.Height)
			{
				bottom = source.Height;
			}
			int width = right - left;
			int height = bottom - top;
			if (width <= 0 || height <= 0)
			{
				return null;
			}
			SKBitmap plane = new SKBitmap(width, height, source.ColorType, SKAlphaType.Unpremul);
			CopyChannelToGray(source, left, top, plane, width, height, channel);
			return plane;
		}

		public static unsafe void ExtractInto(SKBitmap source, SKBitmap plane, int channel)
		{
			if (source == null || plane == null)
			{
				return;
			}
			if (channel < 0 || channel > 3)
			{
				return;
			}
			if (plane.Width != source.Width || plane.Height != source.Height)
			{
				return;
			}
			CopyChannelToGray(source, 0, 0, plane, source.Width, source.Height, channel);
		}

		public static unsafe void Write(SKBitmap target, SKBitmap source, int channel, int offsetX, int offsetY)
		{
			if (target == null || source == null)
			{
				return;
			}
			if (channel < 0 || channel > 3)
			{
				return;
			}
			int startX = 0;
			if (offsetX < 0)
			{
				startX = -offsetX;
			}
			int startY = 0;
			if (offsetY < 0)
			{
				startY = -offsetY;
			}
			int endX = source.Width;
			if (endX > target.Width - offsetX)
			{
				endX = target.Width - offsetX;
			}
			int endY = source.Height;
			if (endY > target.Height - offsetY)
			{
				endY = target.Height - offsetY;
			}
			if (endX <= startX || endY <= startY)
			{
				return;
			}
			WriteGrayToChannel(target, source, channel, offsetX, offsetY, startX, startY, endX, endY);
		}

		public static unsafe void WriteRegion(SKBitmap target, SKBitmap plane, int channel, SKRectI region)
		{
			if (target == null || plane == null)
			{
				return;
			}
			if (channel < 0 || channel > 3)
			{
				return;
			}
			int startX = region.Left;
			int startY = region.Top;
			int endX = region.Right;
			int endY = region.Bottom;
			if (startX < 0)
			{
				startX = 0;
			}
			if (startY < 0)
			{
				startY = 0;
			}
			if (endX > target.Width)
			{
				endX = target.Width;
			}
			if (endY > target.Height)
			{
				endY = target.Height;
			}
			if (endX > plane.Width)
			{
				endX = plane.Width;
			}
			if (endY > plane.Height)
			{
				endY = plane.Height;
			}
			if (endX <= startX || endY <= startY)
			{
				return;
			}
			WriteGrayToChannel(target, plane, channel, 0, 0, startX, startY, endX, endY);
		}
	}
}
