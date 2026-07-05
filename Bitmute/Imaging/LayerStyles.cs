using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class LayerStyles
	{
		private sealed unsafe class StampAlphaWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public int m_sourceWidth;
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
					for (int sourceX = 0; sourceX < m_sourceWidth; sourceX++)
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

		private static unsafe void StampAlpha(SKBitmap source, byte[] destAlpha, int destWidth, int destHeight, int stampX, int stampY)
		{
			int sourceHeight = source.Height;
			StampAlphaWorker worker = new StampAlphaWorker();
			worker.m_sourceBase = (byte*)source.GetPixels().ToPointer();
			worker.m_sourceRowBytes = source.RowBytes;
			worker.m_sourceWidth = source.Width;
			worker.m_destAlpha = destAlpha;
			worker.m_destWidth = destWidth;
			worker.m_destHeight = destHeight;
			worker.m_stampX = stampX;
			worker.m_stampY = stampY;
			RowBands.Run(0, sourceHeight, worker.Band);
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
			double[] bufferA = new double[count];
			double[] bufferB = new double[count];
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
			for (int index = 0; index < distance.Length; index++)
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
			byte[] state = new byte[alpha.Length];
			for (int index = 0; index < state.Length; index++)
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
			int[] distance = new int[alpha.Length];
			ComputeDistance(state, width, height, distance);
			DilateAlphaWorker worker = new DilateAlphaWorker();
			worker.m_alpha = alpha;
			worker.m_state = state;
			worker.m_distance = distance;
			worker.m_radius = radius;
			RowBands.Run(0, alpha.Length, worker.Band);
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

		public static unsafe SKBitmap RenderDropShadow(SKBitmap source, SKColor color, int offsetX, int offsetY, int blurRadius, int spread, byte opacity, out int placeX, out int placeY)
		{
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
			placeX = left;
			placeY = top;
			int resultWidth = right - left;
			int resultHeight = bottom - top;
			SKBitmap result = CreateResult(resultWidth, resultHeight);
			byte[] alpha = new byte[resultWidth * resultHeight];
			StampAlpha(source, alpha, resultWidth, resultHeight, offsetX - left, offsetY - top);
			int spreadRadius = SpreadPixels(blurRadius, spread);
			DilateAlpha(alpha, resultWidth, resultHeight, spreadRadius);
			BlurAlpha(alpha, resultWidth, resultHeight, blurRadius - spreadRadius);
			WriteColoredAlpha(result, alpha, color, opacity);
			return result;
		}

		public static unsafe SKBitmap RenderOuterGlow(SKBitmap source, SKColor color, int size, byte opacity, out int placeX, out int placeY)
		{
			return RenderOuterGlow(source, color, size, 0, opacity, out placeX, out placeY);
		}

		public static unsafe SKBitmap RenderOuterGlow(SKBitmap source, SKColor color, int size, int spread, byte opacity, out int placeX, out int placeY)
		{
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			placeX = -size;
			placeY = -size;
			int resultWidth = sourceWidth + (2 * size);
			int resultHeight = sourceHeight + (2 * size);
			SKBitmap result = CreateResult(resultWidth, resultHeight);
			byte[] alpha = new byte[resultWidth * resultHeight];
			StampAlpha(source, alpha, resultWidth, resultHeight, size, size);
			int spreadRadius = SpreadPixels(size, spread);
			DilateAlpha(alpha, resultWidth, resultHeight, spreadRadius);
			BlurAlpha(alpha, resultWidth, resultHeight, size - spreadRadius);
			WriteColoredAlpha(result, alpha, color, opacity);
			return result;
		}

		public static unsafe SKBitmap RenderInnerGlow(SKBitmap source, SKColor color, int size, int spread, byte opacity, out int placeX, out int placeY)
		{
			int width = source.Width;
			int height = source.Height;
			placeX = 0;
			placeY = 0;
			SKBitmap result = CreateResult(width, height);
			int count = width * height;
			byte[] body = new byte[count];
			StampAlpha(source, body, width, height, 0, 0);
			byte[] state = new byte[count];
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
			int[] distance = new int[count];
			ComputeDistance(state, width, height, distance);
			int spreadRadius = SpreadPixels(size, spread);
			byte[] halo = new byte[count];
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

		public static unsafe SKBitmap RenderBevel(SKBitmap source, int depth, int size, int angle, SKColor highlightColor, byte highlightOpacity, SKColor shadowColor, byte shadowOpacity, out int placeX, out int placeY)
		{
			int width = source.Width;
			int height = source.Height;
			placeX = 0;
			placeY = 0;
			SKBitmap result = CreateResult(width, height);
			int count = width * height;
			byte[] body = new byte[count];
			StampAlpha(source, body, width, height, 0, 0);
			byte[] state = new byte[count];
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
			int[] distance = new int[count];
			ComputeDistance(state, width, height, distance);
			int rampSize = size;
			if (rampSize < 1)
			{
				rampSize = 1;
			}
			byte[] heightField = new byte[count];
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

		public static unsafe SKBitmap RenderStroke(SKBitmap source, int size, int position, SKColor color, byte opacity, out int placeX, out int placeY)
		{
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			placeX = -size;
			placeY = -size;
			int resultWidth = sourceWidth + (2 * size);
			int resultHeight = sourceHeight + (2 * size);
			SKBitmap result = CreateResult(resultWidth, resultHeight);
			byte[] alpha = new byte[resultWidth * resultHeight];
			StampAlpha(source, alpha, resultWidth, resultHeight, size, size);
			byte[] state = new byte[resultWidth * resultHeight];
			for (int index = 0; index < state.Length; index++)
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
			int[] distance = new int[resultWidth * resultHeight];
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
