using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterGenerate
	{
		public enum eNormalMapKernel
		{
			Sobel3,
			Prewitt3,
			Sobel5,
			Sobel9
		}

		public enum eNormalMapEdge
		{
			Wrap,
			Clamp
		}

		private sealed unsafe class NormalMapWorker
		{
			public float* m_heightBase;
			public byte* m_destinationBase;
			public int m_destinationStride;
			public int m_width;
			public int m_height;
			public float[] m_kernelHorizontal;
			public float[] m_kernelVertical;
			public int m_kernelSize;
			public int m_kernelRadius;
			public float m_kernelDivisor;
			public float m_strength;
			public bool m_invertX;
			public bool m_invertY;
			public eNormalMapEdge m_edge;

			public void Band(int start, int end)
			{
				int size = m_kernelSize;
				int radius = m_kernelRadius;
				int width = m_width;
				int height = m_height;
				float divisor = m_kernelDivisor;
				float strength = m_strength;
				for (int y = start; y < end; y++)
				{
					byte* destinationRow = m_destinationBase + ((long)y * m_destinationStride);
					for (int x = 0; x < width; x++)
					{
						float rawDx = 0.0f;
						float rawDy = 0.0f;
						float centerHeight = m_heightBase[((long)y * width) + x];
						for (int kernelRow = 0; kernelRow < size; kernelRow++)
						{
							int sampleY = SampleIndex(y + (kernelRow - radius), height, m_edge);
							float* heightRow = m_heightBase + ((long)sampleY * width);
							for (int kernelColumn = 0; kernelColumn < size; kernelColumn++)
							{
								int sampleX = SampleIndex(x + (kernelColumn - radius), width, m_edge);
								float sampleHeight = heightRow[sampleX] - centerHeight;
								int kernelIndex = (kernelRow * size) + kernelColumn;
								rawDx = rawDx + (m_kernelHorizontal[kernelIndex] * sampleHeight);
								rawDy = rawDy + (m_kernelVertical[kernelIndex] * sampleHeight);
							}
						}
						float dx = rawDx / divisor;
						float dy = rawDy / divisor;
						if (m_invertX)
						{
							dx = -dx;
						}
						if (m_invertY)
						{
							dy = -dy;
						}
						float vectorX = -dx * strength;
						float vectorY = -dy * strength;
						float vectorZ = 1.0f;
						double length = Math.Sqrt((double)((vectorX * vectorX) + (vectorY * vectorY) + (vectorZ * vectorZ)));
						if (length < 0.0000001)
						{
							length = 1.0;
						}
						double normalizedX = vectorX / length;
						double normalizedY = vectorY / length;
						double normalizedZ = vectorZ / length;
						byte* destination = destinationRow + (x * 4);
						byte alpha = destination[3];
						destination[0] = EncodeComponent(normalizedX);
						destination[1] = EncodeComponent(normalizedY);
						destination[2] = EncodeComponent(normalizedZ);
						destination[3] = alpha;
					}
				}
			}
		}

		private sealed class NormalMapHighDepthWorker
		{
			public float[] m_heightBuffer;
			public PixelAccessor m_accessor;
			public int m_width;
			public int m_height;
			public float[] m_kernelHorizontal;
			public float[] m_kernelVertical;
			public int m_kernelSize;
			public int m_kernelRadius;
			public float m_kernelDivisor;
			public float m_strength;
			public bool m_invertX;
			public bool m_invertY;
			public eNormalMapEdge m_edge;

			public void Band(int start, int end)
			{
				int size = m_kernelSize;
				int radius = m_kernelRadius;
				int width = m_width;
				int height = m_height;
				float divisor = m_kernelDivisor;
				float strength = m_strength;
				for (int y = start; y < end; y++)
				{
					for (int x = 0; x < width; x++)
					{
						float rawDx = 0.0f;
						float rawDy = 0.0f;
						float centerHeight = m_heightBuffer[(y * width) + x];
						for (int kernelRow = 0; kernelRow < size; kernelRow++)
						{
							int sampleY = SampleIndex(y + (kernelRow - radius), height, m_edge);
							int heightRowStart = sampleY * width;
							for (int kernelColumn = 0; kernelColumn < size; kernelColumn++)
							{
								int sampleX = SampleIndex(x + (kernelColumn - radius), width, m_edge);
								float sampleHeight = m_heightBuffer[heightRowStart + sampleX] - centerHeight;
								int kernelIndex = (kernelRow * size) + kernelColumn;
								rawDx = rawDx + (m_kernelHorizontal[kernelIndex] * sampleHeight);
								rawDy = rawDy + (m_kernelVertical[kernelIndex] * sampleHeight);
							}
						}
						float dx = rawDx / divisor;
						float dy = rawDy / divisor;
						if (m_invertX)
						{
							dx = -dx;
						}
						if (m_invertY)
						{
							dy = -dy;
						}
						float vectorX = -dx * strength;
						float vectorY = -dy * strength;
						float vectorZ = 1.0f;
						double length = Math.Sqrt((double)((vectorX * vectorX) + (vectorY * vectorY) + (vectorZ * vectorZ)));
						if (length < 0.0000001)
						{
							length = 1.0;
						}
						double normalizedX = vectorX / length;
						double normalizedY = vectorY / length;
						double normalizedZ = vectorZ / length;
						float alpha = m_accessor.AlphaAt(x, y);
						m_accessor.WriteNormalized(x, y, EncodeNormalized(normalizedX), EncodeNormalized(normalizedY), EncodeNormalized(normalizedZ), alpha);
					}
				}
			}
		}

		private static int SampleIndex(int index, int count, eNormalMapEdge edge)
		{
			if (edge == eNormalMapEdge.Wrap)
			{
				int wrapped = index % count;
				if (wrapped < 0)
				{
					wrapped = wrapped + count;
				}
				return wrapped;
			}
			if (index < 0)
			{
				return 0;
			}
			if (index > count - 1)
			{
				return count - 1;
			}
			return index;
		}

		private static byte EncodeComponent(double value)
		{
			double mapped = ((value * 0.5) + 0.5) * 255.0;
			double rounded = Math.Round(mapped);
			if (rounded < 0.0)
			{
				return 0;
			}
			if (rounded > 255.0)
			{
				return 255;
			}
			return (byte)rounded;
		}

		private static float EncodeNormalized(double value)
		{
			double mapped = (value * 0.5) + 0.5;
			if (mapped < 0.0)
			{
				return 0.0f;
			}
			if (mapped > 1.0)
			{
				return 1.0f;
			}
			return (float)mapped;
		}

		private static int[] BinomialRow(int order)
		{
			int[] row = new int[order + 1];
			row[0] = 1;
			for (int index = 1; index <= order; index++)
			{
				row[index] = 1;
				for (int position = index - 1; position >= 1; position--)
				{
					row[position] = row[position] + row[position - 1];
				}
			}
			return row;
		}

		private static int[] SmoothingVector(int size)
		{
			return BinomialRow(size - 1);
		}

		private static int[] DerivativeVector(int size)
		{
			int[] lower = BinomialRow(size - 2);
			int[] derivative = new int[size];
			for (int index = 0; index < size; index++)
			{
				int upperValue = 0;
				if (index - 1 >= 0 && index - 1 < lower.Length)
				{
					upperValue = lower[index - 1];
				}
				int lowerValue = 0;
				if (index >= 0 && index < lower.Length)
				{
					lowerValue = lower[index];
				}
				derivative[index] = upperValue - lowerValue;
			}
			return derivative;
		}

		private static void BuildPrewittVectors(out int[] smoothing, out int[] derivative)
		{
			smoothing = new int[] { 1, 1, 1 };
			derivative = new int[] { -1, 0, 1 };
		}

		private static void SelectVectors(eNormalMapKernel kernel, out int[] smoothing, out int[] derivative, out int size)
		{
			if (kernel == eNormalMapKernel.Prewitt3)
			{
				size = 3;
				BuildPrewittVectors(out smoothing, out derivative);
				return;
			}
			if (kernel == eNormalMapKernel.Sobel5)
			{
				size = 5;
				smoothing = SmoothingVector(size);
				derivative = DerivativeVector(size);
				return;
			}
			if (kernel == eNormalMapKernel.Sobel9)
			{
				size = 9;
				smoothing = SmoothingVector(size);
				derivative = DerivativeVector(size);
				return;
			}
			size = 3;
			smoothing = SmoothingVector(size);
			derivative = DerivativeVector(size);
		}

		private static float[] BuildHorizontalKernel(int[] smoothing, int[] derivative, int size)
		{
			float[] kernel = new float[size * size];
			for (int row = 0; row < size; row++)
			{
				for (int column = 0; column < size; column++)
				{
					kernel[(row * size) + column] = (float)(smoothing[row] * derivative[column]);
				}
			}
			return kernel;
		}

		private static float[] BuildVerticalKernel(int[] smoothing, int[] derivative, int size)
		{
			float[] kernel = new float[size * size];
			for (int row = 0; row < size; row++)
			{
				for (int column = 0; column < size; column++)
				{
					kernel[(row * size) + column] = (float)(derivative[row] * smoothing[column]);
				}
			}
			return kernel;
		}

		private static float UnitSlopeResponse(float[] horizontalKernel, int size)
		{
			int radius = size / 2;
			float response = 0.0f;
			for (int row = 0; row < size; row++)
			{
				for (int column = 0; column < size; column++)
				{
					float offset = (float)(column - radius);
					response = response + (horizontalKernel[(row * size) + column] * offset);
				}
			}
			return response;
		}

		private static unsafe void BuildHeightBuffer(SKBitmap bitmap, float[] heightBuffer)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			for (int y = 0; y < height; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				int rowStart = y * width;
				for (int x = 0; x < width; x++)
				{
					byte* pixel = row + (x * 4);
					double luma = (0.299 * pixel[0]) + (0.587 * pixel[1]) + (0.114 * pixel[2]);
					heightBuffer[rowStart + x] = (float)(luma / 255.0);
				}
			}
		}

		private static void BuildHeightBufferHighDepth(PixelAccessor accessor, float[] heightBuffer, int width, int height)
		{
			for (int y = 0; y < height; y++)
			{
				int rowStart = y * width;
				for (int x = 0; x < width; x++)
				{
					float red;
					float green;
					float blue;
					float alpha;
					accessor.ReadNormalized(x, y, out red, out green, out blue, out alpha);
					heightBuffer[rowStart + x] = (0.299f * red) + (0.587f * green) + (0.114f * blue);
				}
			}
		}

		public static unsafe void NormalMap(SKBitmap bitmap, float strength, eNormalMapKernel kernel, bool invertX, bool invertY, eNormalMapEdge edge)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			if (width <= 0 || height <= 0)
			{
				return;
			}
			int[] smoothing;
			int[] derivative;
			int size;
			SelectVectors(kernel, out smoothing, out derivative, out size);
			float[] horizontalKernel = BuildHorizontalKernel(smoothing, derivative, size);
			float[] verticalKernel = BuildVerticalKernel(smoothing, derivative, size);
			float divisor = UnitSlopeResponse(horizontalKernel, size);
			if (divisor == 0.0f)
			{
				divisor = 1.0f;
			}
			if (bitmap.ColorType == SKColorType.Rgba8888)
			{
				float[] heightBuffer = new float[width * height];
				BuildHeightBuffer(bitmap, heightBuffer);
				fixed (float* heightBase = heightBuffer)
				{
					byte* destinationBase = (byte*)bitmap.GetPixels().ToPointer();
					NormalMapWorker worker = new NormalMapWorker();
					worker.m_heightBase = heightBase;
					worker.m_destinationBase = destinationBase;
					worker.m_destinationStride = bitmap.RowBytes;
					worker.m_width = width;
					worker.m_height = height;
					worker.m_kernelHorizontal = horizontalKernel;
					worker.m_kernelVertical = verticalKernel;
					worker.m_kernelSize = size;
					worker.m_kernelRadius = size / 2;
					worker.m_kernelDivisor = divisor;
					worker.m_strength = strength;
					worker.m_invertX = invertX;
					worker.m_invertY = invertY;
					worker.m_edge = edge;
					RowBands.Run(0, height, worker.Band);
				}
				return;
			}
			PixelAccessor accessor = new PixelAccessor(bitmap.GetPixels(), bitmap.RowBytes, bitmap.ColorType);
			float[] highDepthHeightBuffer = new float[width * height];
			BuildHeightBufferHighDepth(accessor, highDepthHeightBuffer, width, height);
			NormalMapHighDepthWorker highDepthWorker = new NormalMapHighDepthWorker();
			highDepthWorker.m_heightBuffer = highDepthHeightBuffer;
			highDepthWorker.m_accessor = accessor;
			highDepthWorker.m_width = width;
			highDepthWorker.m_height = height;
			highDepthWorker.m_kernelHorizontal = horizontalKernel;
			highDepthWorker.m_kernelVertical = verticalKernel;
			highDepthWorker.m_kernelSize = size;
			highDepthWorker.m_kernelRadius = size / 2;
			highDepthWorker.m_kernelDivisor = divisor;
			highDepthWorker.m_strength = strength;
			highDepthWorker.m_invertX = invertX;
			highDepthWorker.m_invertY = invertY;
			highDepthWorker.m_edge = edge;
			RowBands.Run(0, height, highDepthWorker.Band);
		}
	}
}
