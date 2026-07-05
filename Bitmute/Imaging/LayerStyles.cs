using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class LayerStyles
	{
		private static byte[] s_alphaPool;
		private static byte[] s_statePool;
		private static byte[] s_planePool;
		private static int[] s_distancePool;
		private static double[] s_blurPoolA;
		private static double[] s_blurPoolB;

		private sealed unsafe class StampAlphaWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public int m_contentLeft;
			public int m_contentRight;
			public byte[] m_destAlpha;
			public int m_destWidth;
			public int m_destHeight;
			public int m_stampX;
			public int m_stampY;

			public void Band(int start, int end)
			{
				for (int sourceY = start; sourceY < end; sourceY++)
				{
					int destY = sourceY + m_stampY;
					if (destY < 0 || destY >= m_destHeight)
					{
						continue;
					}
					byte* sourceRow = m_sourceBase + (sourceY * m_sourceRowBytes);
					int destRow = destY * m_destWidth;
					for (int sourceX = m_contentLeft; sourceX < m_contentRight; sourceX++)
					{
						int destX = sourceX + m_stampX;
						if (destX < 0 || destX >= m_destWidth)
						{
							continue;
						}
						byte* sourcePixel = sourceRow + (sourceX * 4);
						m_destAlpha[destRow + destX] = sourcePixel[3];
					}
				}
			}
		}

		private sealed class BoxBlurHorizontalWorker
		{
			public double[] m_sourceAlpha;
			public double[] m_destAlpha;
			public int m_width;
			public int m_radius;
			public double m_windowScale;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					int row = y * m_width;
					double accumulator = 0.0;
					for (int offset = -m_radius; offset <= m_radius; offset++)
					{
						int sampleX = offset;
						if (sampleX < 0)
						{
							sampleX = 0;
						}
						if (sampleX >= m_width)
						{
							sampleX = m_width - 1;
						}
						accumulator = accumulator + m_sourceAlpha[row + sampleX];
					}
					for (int x = 0; x < m_width; x++)
					{
						m_destAlpha[row + x] = accumulator * m_windowScale;
						int leaveX = x - m_radius;
						if (leaveX < 0)
						{
							leaveX = 0;
						}
						int enterX = x + m_radius + 1;
						if (enterX >= m_width)
						{
							enterX = m_width - 1;
						}
						accumulator = accumulator - m_sourceAlpha[row + leaveX];
						accumulator = accumulator + m_sourceAlpha[row + enterX];
					}
				}
			}
		}

		private sealed class BoxBlurVerticalWorker
		{
			public double[] m_sourceAlpha;
			public double[] m_destAlpha;
			public int m_width;
			public int m_height;
			public int m_radius;
			public double m_windowScale;

			public void Band(int start, int end)
			{
				for (int x = start; x < end; x++)
				{
					double accumulator = 0.0;
					for (int offset = -m_radius; offset <= m_radius; offset++)
					{
						int sampleY = offset;
						if (sampleY < 0)
						{
							sampleY = 0;
						}
						if (sampleY >= m_height)
						{
							sampleY = m_height - 1;
						}
						accumulator = accumulator + m_sourceAlpha[(sampleY * m_width) + x];
					}
					for (int y = 0; y < m_height; y++)
					{
						m_destAlpha[(y * m_width) + x] = accumulator * m_windowScale;
						int leaveY = y - m_radius;
						if (leaveY < 0)
						{
							leaveY = 0;
						}
						int enterY = y + m_radius + 1;
						if (enterY >= m_height)
						{
							enterY = m_height - 1;
						}
						accumulator = accumulator - m_sourceAlpha[(leaveY * m_width) + x];
						accumulator = accumulator + m_sourceAlpha[(enterY * m_width) + x];
					}
				}
			}
		}

		private sealed unsafe class WriteColoredAlphaWorker
		{
			public byte* m_basePixels;
			public int m_rowBytes;
			public int m_width;
			public byte[] m_alpha;
			public byte m_colorRed;
			public byte m_colorGreen;
			public byte m_colorBlue;
			public double m_opacityScale;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_basePixels + (y * m_rowBytes);
					int alphaRow = y * m_width;
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						pixel[0] = m_colorRed;
						pixel[1] = m_colorGreen;
						pixel[2] = m_colorBlue;
						pixel[3] = ClampToByte(m_alpha[alphaRow + x] * m_opacityScale);
					}
				}
			}
		}

		private sealed class DilateAlphaWorker
		{
			public byte[] m_alpha;
			public byte[] m_state;
			public int[] m_distance;
			public int m_radius;

			public void Band(int start, int end)
			{
				for (int index = start; index < end; index++)
				{
					if (m_state[index] != 0)
					{
						m_alpha[index] = 255;
					}
					else if (m_distance[index] <= m_radius)
					{
						m_alpha[index] = 255;
					}
					else
					{
						m_alpha[index] = 0;
					}
				}
			}
		}

		private static byte ClampToByte(double value)
		{
			double rounded = Math.Round(value);
			if (rounded < 0.0)
			{
				rounded = 0.0;
			}
			if (rounded > 255.0)
			{
				rounded = 255.0;
			}
			return (byte)rounded;
		}

		private static SKBitmap CreateResult(int width, int height)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(SKColors.Transparent);
			return bitmap;
		}

		private static byte[] AlphaPool(int count)
		{
			if (s_alphaPool == null || s_alphaPool.Length < count)
			{
				s_alphaPool = new byte[count];
			}
			return s_alphaPool;
		}

		private static byte[] StatePool(int count)
		{
			if (s_statePool == null || s_statePool.Length < count)
			{
				s_statePool = new byte[count];
			}
			return s_statePool;
		}

		private static byte[] PlanePool(int count)
		{
			if (s_planePool == null || s_planePool.Length < count)
			{
				s_planePool = new byte[count];
			}
			return s_planePool;
		}

		private static int[] DistancePool(int count)
		{
			if (s_distancePool == null || s_distancePool.Length < count)
			{
				s_distancePool = new int[count];
			}
			return s_distancePool;
		}

		private static SKRectI CropPlane(SKRectI plane, SKRectI support)
		{
			int left = support.Left;
			if (left < plane.Left)
			{
				left = plane.Left;
			}
			int top = support.Top;
			if (top < plane.Top)
			{
				top = plane.Top;
			}
			int right = support.Right;
			if (right > plane.Right)
			{
				right = plane.Right;
			}
			int bottom = support.Bottom;
			if (bottom > plane.Bottom)
			{
				bottom = plane.Bottom;
			}
			return new SKRectI(left, top, right, bottom);
		}

		private static unsafe void StampAlpha(SKBitmap source, SKRectI content, byte[] destAlpha, int destWidth, int destHeight, int stampX, int stampY)
		{
			StampAlphaWorker worker = new StampAlphaWorker();
			worker.m_sourceBase = (byte*)source.GetPixels().ToPointer();
			worker.m_sourceRowBytes = source.RowBytes;
			worker.m_contentLeft = content.Left;
			worker.m_contentRight = content.Right;
			worker.m_destAlpha = destAlpha;
			worker.m_destWidth = destWidth;
			worker.m_destHeight = destHeight;
			worker.m_stampX = stampX;
			worker.m_stampY = stampY;
			RowBands.Run(content.Top, content.Bottom, worker.Band);
		}

		private static void BoxBlurHorizontal(double[] sourceAlpha, double[] destAlpha, int width, int height, int radius)
		{
			int window = (radius * 2) + 1;
			double windowScale = 1.0 / window;
			BoxBlurHorizontalWorker worker = new BoxBlurHorizontalWorker();
			worker.m_sourceAlpha = sourceAlpha;
			worker.m_destAlpha = destAlpha;
			worker.m_width = width;
			worker.m_radius = radius;
			worker.m_windowScale = windowScale;
			RowBands.Run(0, height, worker.Band);
		}

		private static void BoxBlurVertical(double[] sourceAlpha, double[] destAlpha, int width, int height, int radius)
		{
			int window = (radius * 2) + 1;
			double windowScale = 1.0 / window;
			BoxBlurVerticalWorker worker = new BoxBlurVerticalWorker();
			worker.m_sourceAlpha = sourceAlpha;
			worker.m_destAlpha = destAlpha;
			worker.m_width = width;
			worker.m_height = height;
			worker.m_radius = radius;
			worker.m_windowScale = windowScale;
			RowBands.Run(0, width, worker.Band);
		}

		private static void BlurAlpha(byte[] alpha, int width, int height, int radius)
		{
			if (radius <= 0)
			{
				return;
			}
			int count = width * height;
			if (s_blurPoolA == null || s_blurPoolA.Length < count)
			{
				s_blurPoolA = new double[count];
			}
			if (s_blurPoolB == null || s_blurPoolB.Length < count)
			{
				s_blurPoolB = new double[count];
			}
			double[] bufferA = s_blurPoolA;
			double[] bufferB = s_blurPoolB;
			for (int index = 0; index < count; index++)
			{
				bufferA[index] = alpha[index];
			}
			for (int pass = 0; pass < 3; pass++)
			{
				BoxBlurHorizontal(bufferA, bufferB, width, height, radius);
				BoxBlurVertical(bufferB, bufferA, width, height, radius);
			}
			for (int index = 0; index < count; index++)
			{
				alpha[index] = ClampToByte(bufferA[index]);
			}
		}

		private static unsafe void WriteColoredAlpha(SKBitmap target, byte[] alpha, SKColor color, byte opacity)
		{
			int height = target.Height;
			WriteColoredAlphaWorker worker = new WriteColoredAlphaWorker();
			worker.m_basePixels = (byte*)target.GetPixels().ToPointer();
			worker.m_rowBytes = target.RowBytes;
			worker.m_width = target.Width;
			worker.m_alpha = alpha;
			worker.m_colorRed = color.Red;
			worker.m_colorGreen = color.Green;
			worker.m_colorBlue = color.Blue;
			worker.m_opacityScale = opacity / 255.0;
			RowBands.Run(0, height, worker.Band);
		}

		private static int NeighborCandidate(byte[] state, int[] distance, int width, int height, int neighborX, int neighborY, bool inside)
		{
			if (neighborX < 0 || neighborY < 0 || neighborX >= width || neighborY >= height)
			{
				if (inside)
				{
					return 1;
				}
				return 1 << 29;
			}
			int neighborIndex = (neighborY * width) + neighborX;
			bool neighborInside = state[neighborIndex] != 0;
			if (neighborInside != inside)
			{
				return 1;
			}
			return distance[neighborIndex] + 1;
		}

		private static void ComputeDistance(byte[] state, int width, int height, int[] distance)
		{
			int infinity = 1 << 29;
			int count = width * height;
			for (int index = 0; index < count; index++)
			{
				distance[index] = infinity;
			}
			for (int y = 0; y < height; y++)
			{
				int row = y * width;
				for (int x = 0; x < width; x++)
				{
					int index = row + x;
					bool inside = state[index] != 0;
					int best = distance[index];
					int candidate = NeighborCandidate(state, distance, width, height, x - 1, y, inside);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x - 1, y - 1, inside);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x, y - 1, inside);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x + 1, y - 1, inside);
					if (candidate < best)
					{
						best = candidate;
					}
					distance[index] = best;
				}
			}
			for (int y = height - 1; y >= 0; y--)
			{
				int row = y * width;
				for (int x = width - 1; x >= 0; x--)
				{
					int index = row + x;
					bool inside = state[index] != 0;
					int best = distance[index];
					int candidate = NeighborCandidate(state, distance, width, height, x + 1, y, inside);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x + 1, y + 1, inside);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x, y + 1, inside);
					if (candidate < best)
					{
						best = candidate;
					}
					candidate = NeighborCandidate(state, distance, width, height, x - 1, y + 1, inside);
					if (candidate < best)
					{
						best = candidate;
					}
					distance[index] = best;
				}
			}
		}

		private static void DilateAlpha(byte[] alpha, int width, int height, int radius)
		{
			if (radius <= 0)
			{
				return;
			}
			int count = width * height;
			byte[] state = StatePool(count);
			for (int index = 0; index < count; index++)
			{
				if (alpha[index] >= 128)
				{
					state[index] = 1;
				}
				else
				{
					state[index] = 0;
				}
			}
			int[] distance = DistancePool(count);
			ComputeDistance(state, width, height, distance);
			DilateAlphaWorker worker = new DilateAlphaWorker();
			worker.m_alpha = alpha;
			worker.m_state = state;
			worker.m_distance = distance;
			worker.m_radius = radius;
			RowBands.Run(0, count, worker.Band);
		}

		private static int SpreadPixels(int size, int spread)
		{
			int clamped = spread;
			if (clamped < 0)
			{
				clamped = 0;
			}
			if (clamped > 100)
			{
				clamped = 100;
			}
			return (size * clamped) / 100;
		}

		public static unsafe SKBitmap RenderDropShadow(SKBitmap source, SKColor color, int offsetX, int offsetY, int blurRadius, byte opacity, out int placeX, out int placeY)
		{
			return RenderDropShadow(source, color, offsetX, offsetY, blurRadius, 0, opacity, out placeX, out placeY);
		}

		public static SKBitmap RenderDropShadow(SKBitmap source, SKColor color, int offsetX, int offsetY, int blurRadius, int spread, byte opacity, out int placeX, out int placeY)
		{
			SKRectI content = PixelRegion.ComputeContentBounds(source);
			return RenderDropShadow(source, content, color, offsetX, offsetY, blurRadius, spread, opacity, out placeX, out placeY);
		}

		public static unsafe SKBitmap RenderDropShadow(SKBitmap source, SKRectI content, SKColor color, int offsetX, int offsetY, int blurRadius, int spread, byte opacity, out int placeX, out int placeY)
		{
			placeX = 0;
			placeY = 0;
			if (content.Width <= 0 || content.Height <= 0)
			{
				return null;
			}
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			int left = 0;
			if (offsetX < left)
			{
				left = offsetX;
			}
			left = left - blurRadius;
			int top = 0;
			if (offsetY < top)
			{
				top = offsetY;
			}
			top = top - blurRadius;
			int right = sourceWidth;
			int shiftedRight = sourceWidth + offsetX;
			if (shiftedRight > right)
			{
				right = shiftedRight;
			}
			right = right + blurRadius;
			int bottom = sourceHeight;
			int shiftedBottom = sourceHeight + offsetY;
			if (shiftedBottom > bottom)
			{
				bottom = shiftedBottom;
			}
			bottom = bottom + blurRadius;
			int spreadRadius = SpreadPixels(blurRadius, spread);
			int reach = spreadRadius + (3 * (blurRadius - spreadRadius));
			SKRectI support = new SKRectI(content.Left + offsetX - reach, content.Top + offsetY - reach, content.Right + offsetX + reach, content.Bottom + offsetY + reach);
			SKRectI crop = CropPlane(new SKRectI(left, top, right, bottom), support);
			if (crop.Width <= 0 || crop.Height <= 0)
			{
				return null;
			}
			placeX = crop.Left;
			placeY = crop.Top;
			int resultWidth = crop.Width;
			int resultHeight = crop.Height;
			SKBitmap result = CreateResult(resultWidth, resultHeight);
			int count = resultWidth * resultHeight;
			byte[] alpha = AlphaPool(count);
			Array.Clear(alpha, 0, count);
			StampAlpha(source, content, alpha, resultWidth, resultHeight, offsetX - crop.Left, offsetY - crop.Top);
			DilateAlpha(alpha, resultWidth, resultHeight, spreadRadius);
			BlurAlpha(alpha, resultWidth, resultHeight, blurRadius - spreadRadius);
			WriteColoredAlpha(result, alpha, color, opacity);
			return result;
		}

		public static unsafe SKBitmap RenderOuterGlow(SKBitmap source, SKColor color, int size, byte opacity, out int placeX, out int placeY)
		{
			return RenderOuterGlow(source, color, size, 0, opacity, out placeX, out placeY);
		}

		public static SKBitmap RenderOuterGlow(SKBitmap source, SKColor color, int size, int spread, byte opacity, out int placeX, out int placeY)
		{
			SKRectI content = PixelRegion.ComputeContentBounds(source);
			return RenderOuterGlow(source, content, color, size, spread, opacity, out placeX, out placeY);
		}

		public static unsafe SKBitmap RenderOuterGlow(SKBitmap source, SKRectI content, SKColor color, int size, int spread, byte opacity, out int placeX, out int placeY)
		{
			placeX = 0;
			placeY = 0;
			if (content.Width <= 0 || content.Height <= 0)
			{
				return null;
			}
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			int spreadRadius = SpreadPixels(size, spread);
			int reach = spreadRadius + (3 * (size - spreadRadius));
			SKRectI support = new SKRectI(content.Left - reach, content.Top - reach, content.Right + reach, content.Bottom + reach);
			SKRectI crop = CropPlane(new SKRectI(-size, -size, sourceWidth + size, sourceHeight + size), support);
			if (crop.Width <= 0 || crop.Height <= 0)
			{
				return null;
			}
			placeX = crop.Left;
			placeY = crop.Top;
			int resultWidth = crop.Width;
			int resultHeight = crop.Height;
			SKBitmap result = CreateResult(resultWidth, resultHeight);
			int count = resultWidth * resultHeight;
			byte[] alpha = AlphaPool(count);
			Array.Clear(alpha, 0, count);
			StampAlpha(source, content, alpha, resultWidth, resultHeight, -crop.Left, -crop.Top);
			DilateAlpha(alpha, resultWidth, resultHeight, spreadRadius);
			BlurAlpha(alpha, resultWidth, resultHeight, size - spreadRadius);
			WriteColoredAlpha(result, alpha, color, opacity);
			return result;
		}

		public static SKBitmap RenderInnerGlow(SKBitmap source, SKColor color, int size, int spread, byte opacity, out int placeX, out int placeY)
		{
			SKRectI content = PixelRegion.ComputeContentBounds(source);
			return RenderInnerGlow(source, content, color, size, spread, opacity, out placeX, out placeY);
		}

		public static unsafe SKBitmap RenderInnerGlow(SKBitmap source, SKRectI content, SKColor color, int size, int spread, byte opacity, out int placeX, out int placeY)
		{
			placeX = 0;
			placeY = 0;
			if (content.Width <= 0 || content.Height <= 0)
			{
				return null;
			}
			int spreadRadius = SpreadPixels(size, spread);
			int reach = (3 * (size - spreadRadius)) + 1;
			SKRectI support = new SKRectI(content.Left - reach, content.Top - reach, content.Right + reach, content.Bottom + reach);
			SKRectI crop = CropPlane(new SKRectI(0, 0, source.Width, source.Height), support);
			if (crop.Width <= 0 || crop.Height <= 0)
			{
				return null;
			}
			placeX = crop.Left;
			placeY = crop.Top;
			int width = crop.Width;
			int height = crop.Height;
			SKBitmap result = CreateResult(width, height);
			int count = width * height;
			byte[] body = AlphaPool(count);
			Array.Clear(body, 0, count);
			StampAlpha(source, content, body, width, height, -crop.Left, -crop.Top);
			byte[] state = StatePool(count);
			for (int index = 0; index < count; index++)
			{
				if (body[index] >= 128)
				{
					state[index] = 1;
				}
				else
				{
					state[index] = 0;
				}
			}
			int[] distance = DistancePool(count);
			ComputeDistance(state, width, height, distance);
			byte[] halo = PlanePool(count);
			for (int index = 0; index < count; index++)
			{
				if (state[index] == 0)
				{
					halo[index] = 255;
				}
				else if (distance[index] <= spreadRadius)
				{
					halo[index] = 255;
				}
				else
				{
					halo[index] = 0;
				}
			}
			BlurAlpha(halo, width, height, size - spreadRadius);
			for (int index = 0; index < count; index++)
			{
				halo[index] = (byte)((halo[index] * body[index]) / 255);
			}
			WriteColoredAlpha(result, halo, color, opacity);
			return result;
		}

		public static SKBitmap RenderBevel(SKBitmap source, int depth, int size, int angle, SKColor highlightColor, byte highlightOpacity, SKColor shadowColor, byte shadowOpacity, out int placeX, out int placeY)
		{
			SKRectI content = PixelRegion.ComputeContentBounds(source);
			return RenderBevel(source, content, depth, size, angle, highlightColor, highlightOpacity, shadowColor, shadowOpacity, out placeX, out placeY);
		}

		public static unsafe SKBitmap RenderBevel(SKBitmap source, SKRectI content, int depth, int size, int angle, SKColor highlightColor, byte highlightOpacity, SKColor shadowColor, byte shadowOpacity, out int placeX, out int placeY)
		{
			placeX = 0;
			placeY = 0;
			if (content.Width <= 0 || content.Height <= 0)
			{
				return null;
			}
			int rampSize = size;
			if (rampSize < 1)
			{
				rampSize = 1;
			}
			int reach = (3 * (rampSize / 4)) + 1;
			SKRectI support = new SKRectI(content.Left - reach, content.Top - reach, content.Right + reach, content.Bottom + reach);
			SKRectI crop = CropPlane(new SKRectI(0, 0, source.Width, source.Height), support);
			if (crop.Width <= 0 || crop.Height <= 0)
			{
				return null;
			}
			placeX = crop.Left;
			placeY = crop.Top;
			int width = crop.Width;
			int height = crop.Height;
			SKBitmap result = CreateResult(width, height);
			int count = width * height;
			byte[] body = AlphaPool(count);
			Array.Clear(body, 0, count);
			StampAlpha(source, content, body, width, height, -crop.Left, -crop.Top);
			byte[] state = StatePool(count);
			for (int index = 0; index < count; index++)
			{
				if (body[index] >= 128)
				{
					state[index] = 1;
				}
				else
				{
					state[index] = 0;
				}
			}
			int[] distance = DistancePool(count);
			ComputeDistance(state, width, height, distance);
			byte[] heightField = PlanePool(count);
			for (int index = 0; index < count; index++)
			{
				if (state[index] == 0)
				{
					heightField[index] = 0;
				}
				else
				{
					int ramp = distance[index];
					if (ramp > rampSize)
					{
						ramp = rampSize;
					}
					heightField[index] = (byte)((ramp * 255) / rampSize);
				}
			}
			BlurAlpha(heightField, width, height, rampSize / 4);
			double radians = angle * Math.PI / 180.0;
			double lightX = Math.Cos(radians);
			double lightY = Math.Sin(radians);
			double depthScale = depth / 100.0;
			int rowBytes = result.RowBytes;
			byte* basePixels = (byte*)result.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* row = basePixels + (y * rowBytes);
				int fieldRow = y * width;
				for (int x = 0; x < width; x++)
				{
					int index = fieldRow + x;
					if (body[index] == 0)
					{
						continue;
					}
					int leftX = x - 1;
					if (leftX < 0)
					{
						leftX = 0;
					}
					int rightX = x + 1;
					if (rightX >= width)
					{
						rightX = width - 1;
					}
					int upY = y - 1;
					if (upY < 0)
					{
						upY = 0;
					}
					int downY = y + 1;
					if (downY >= height)
					{
						downY = height - 1;
					}
					double gradX = (heightField[fieldRow + rightX] - heightField[fieldRow + leftX]) * rampSize / 510.0;
					double gradY = (heightField[(downY * width) + x] - heightField[(upY * width) + x]) * rampSize / 510.0;
					double intensity = ((gradX * lightX) + (gradY * lightY)) * depthScale;
					if (intensity > 1.0)
					{
						intensity = 1.0;
					}
					if (intensity < -1.0)
					{
						intensity = -1.0;
					}
					double bodyScale = body[index] / 255.0;
					byte outRed;
					byte outGreen;
					byte outBlue;
					byte outAlpha;
					if (intensity >= 0.0)
					{
						outRed = highlightColor.Red;
						outGreen = highlightColor.Green;
						outBlue = highlightColor.Blue;
						outAlpha = ClampToByte(intensity * highlightOpacity * bodyScale);
					}
					else
					{
						outRed = shadowColor.Red;
						outGreen = shadowColor.Green;
						outBlue = shadowColor.Blue;
						outAlpha = ClampToByte(-intensity * shadowOpacity * bodyScale);
					}
					if (outAlpha == 0)
					{
						continue;
					}
					byte* pixel = row + (x * 4);
					pixel[0] = outRed;
					pixel[1] = outGreen;
					pixel[2] = outBlue;
					pixel[3] = outAlpha;
				}
			}
			return result;
		}

		public static unsafe SKBitmap RenderStroke(SKBitmap source, int size, int position, SKColor color, out int placeX, out int placeY)
		{
			return RenderStroke(source, size, position, color, 255, out placeX, out placeY);
		}

		public static SKBitmap RenderStroke(SKBitmap source, int size, int position, SKColor color, byte opacity, out int placeX, out int placeY)
		{
			SKRectI content = PixelRegion.ComputeContentBounds(source);
			return RenderStroke(source, content, size, position, color, opacity, out placeX, out placeY);
		}

		public static unsafe SKBitmap RenderStroke(SKBitmap source, SKRectI content, int size, int position, SKColor color, byte opacity, out int placeX, out int placeY)
		{
			placeX = 0;
			placeY = 0;
			if (content.Width <= 0 || content.Height <= 0)
			{
				return null;
			}
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			int reach = size + 1;
			SKRectI support = new SKRectI(content.Left - reach, content.Top - reach, content.Right + reach, content.Bottom + reach);
			SKRectI crop = CropPlane(new SKRectI(-size, -size, sourceWidth + size, sourceHeight + size), support);
			if (crop.Width <= 0 || crop.Height <= 0)
			{
				return null;
			}
			placeX = crop.Left;
			placeY = crop.Top;
			int resultWidth = crop.Width;
			int resultHeight = crop.Height;
			SKBitmap result = CreateResult(resultWidth, resultHeight);
			int count = resultWidth * resultHeight;
			byte[] alpha = AlphaPool(count);
			Array.Clear(alpha, 0, count);
			StampAlpha(source, content, alpha, resultWidth, resultHeight, -crop.Left, -crop.Top);
			byte[] state = StatePool(count);
			for (int index = 0; index < count; index++)
			{
				if (alpha[index] >= 128)
				{
					state[index] = 1;
				}
				else
				{
					state[index] = 0;
				}
			}
			int[] distance = DistancePool(count);
			ComputeDistance(state, resultWidth, resultHeight, distance);
			int insideLimit;
			int outsideLimit;
			if (position == 0)
			{
				insideLimit = size;
				outsideLimit = 0;
			}
			else if (position == 2)
			{
				insideLimit = 0;
				outsideLimit = size;
			}
			else
			{
				insideLimit = (size + 1) / 2;
				outsideLimit = size / 2;
			}
			int rowBytes = result.RowBytes;
			byte* basePixels = (byte*)result.GetPixels().ToPointer();
			byte colorRed = color.Red;
			byte colorGreen = color.Green;
			byte colorBlue = color.Blue;
			for (int y = 0; y < resultHeight; y++)
			{
				byte* row = basePixels + (y * rowBytes);
				int stateRow = y * resultWidth;
				for (int x = 0; x < resultWidth; x++)
				{
					int index = stateRow + x;
					int limit;
					if (state[index] != 0)
					{
						limit = insideLimit;
					}
					else
					{
						limit = outsideLimit;
					}
					if (limit == 0)
					{
						continue;
					}
					if (distance[index] > limit)
					{
						continue;
					}
					byte* pixel = row + (x * 4);
					pixel[0] = colorRed;
					pixel[1] = colorGreen;
					pixel[2] = colorBlue;
					pixel[3] = opacity;
				}
			}
			return result;
		}
	}
}
