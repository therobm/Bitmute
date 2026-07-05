using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class RotateTransform
	{
		private sealed unsafe class RotateNearestWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public int m_sourceWidth;
			public int m_sourceHeight;
			public byte* m_destinationBase;
			public int m_destinationRowBytes;
			public int m_destinationWidth;
			public double m_cos;
			public double m_sin;
			public double m_sourceCenterX;
			public double m_sourceCenterY;
			public double m_destinationCenterX;
			public double m_destinationCenterY;

			public void Band(int start, int end)
			{
				byte* sourceBase = m_sourceBase;
				int sourceRowBytes = m_sourceRowBytes;
				int sourceWidth = m_sourceWidth;
				int sourceHeight = m_sourceHeight;
				byte* destinationBase = m_destinationBase;
				int destinationRowBytes = m_destinationRowBytes;
				int destinationWidth = m_destinationWidth;
				double cos = m_cos;
				double sin = m_sin;
				double sourceCenterX = m_sourceCenterX;
				double sourceCenterY = m_sourceCenterY;
				double destinationCenterX = m_destinationCenterX;
				double destinationCenterY = m_destinationCenterY;
				for (int destinationY = start; destinationY < end; destinationY++)
				{
					double offsetY = destinationY + 0.5 - destinationCenterY;
					double rowTermX = offsetY * sin + sourceCenterX - 0.5;
					double rowTermY = offsetY * cos + sourceCenterY - 0.5;
					byte* destinationRow = destinationBase + (destinationY * destinationRowBytes);
					for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
					{
						double offsetX = destinationX + 0.5 - destinationCenterX;
						double sourceX = offsetX * cos + rowTermX;
						double sourceY = rowTermY - offsetX * sin;
						int nearestX = (int)Math.Round(sourceX);
						int nearestY = (int)Math.Round(sourceY);
						byte* destinationPixel = destinationRow + (destinationX * 4);
						if (nearestX >= 0 && nearestY >= 0 && nearestX < sourceWidth && nearestY < sourceHeight)
						{
							byte* sourcePixel = sourceBase + (nearestY * sourceRowBytes) + (nearestX * 4);
							destinationPixel[0] = sourcePixel[0];
							destinationPixel[1] = sourcePixel[1];
							destinationPixel[2] = sourcePixel[2];
							destinationPixel[3] = sourcePixel[3];
						}
						else
						{
							destinationPixel[0] = 0;
							destinationPixel[1] = 0;
							destinationPixel[2] = 0;
							destinationPixel[3] = 0;
						}
					}
				}
			}
		}

		private sealed unsafe class RotateBilinearWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public int m_sourceWidth;
			public int m_sourceHeight;
			public byte* m_destinationBase;
			public int m_destinationRowBytes;
			public int m_destinationWidth;
			public double m_cos;
			public double m_sin;
			public double m_sourceCenterX;
			public double m_sourceCenterY;
			public double m_destinationCenterX;
			public double m_destinationCenterY;

			public void Band(int start, int end)
			{
				byte* sourceBase = m_sourceBase;
				int sourceRowBytes = m_sourceRowBytes;
				int sourceWidth = m_sourceWidth;
				int sourceHeight = m_sourceHeight;
				byte* destinationBase = m_destinationBase;
				int destinationRowBytes = m_destinationRowBytes;
				int destinationWidth = m_destinationWidth;
				double cos = m_cos;
				double sin = m_sin;
				double sourceCenterX = m_sourceCenterX;
				double sourceCenterY = m_sourceCenterY;
				double destinationCenterX = m_destinationCenterX;
				double destinationCenterY = m_destinationCenterY;
				double* weightsX = stackalloc double[2];
				double* weightsY = stackalloc double[2];
				for (int destinationY = start; destinationY < end; destinationY++)
				{
					double offsetY = destinationY + 0.5 - destinationCenterY;
					double rowTermX = offsetY * sin + sourceCenterX - 0.5;
					double rowTermY = offsetY * cos + sourceCenterY - 0.5;
					byte* destinationRow = destinationBase + (destinationY * destinationRowBytes);
					for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
					{
						double offsetX = destinationX + 0.5 - destinationCenterX;
						double sourceX = offsetX * cos + rowTermX;
						double sourceY = rowTermY - offsetX * sin;
						int baseX = (int)Math.Floor(sourceX);
						int baseY = (int)Math.Floor(sourceY);
						double fractionX = sourceX - baseX;
						double fractionY = sourceY - baseY;
						weightsX[0] = 1.0 - fractionX;
						weightsX[1] = fractionX;
						weightsY[0] = 1.0 - fractionY;
						weightsY[1] = fractionY;
						double sumRed = 0.0;
						double sumGreen = 0.0;
						double sumBlue = 0.0;
						double sumAlpha = 0.0;
						for (int tapY = 0; tapY < 2; tapY++)
						{
							int sampleY = baseY + tapY;
							if (sampleY < 0 || sampleY >= sourceHeight)
							{
								continue;
							}
							byte* sourceRow = sourceBase + (sampleY * sourceRowBytes);
							double weightY = weightsY[tapY];
							for (int tapX = 0; tapX < 2; tapX++)
							{
								int sampleX = baseX + tapX;
								if (sampleX < 0 || sampleX >= sourceWidth)
								{
									continue;
								}
								byte* sourcePixel = sourceRow + (sampleX * 4);
								double alpha = sourcePixel[3];
								if (alpha <= 0.0)
								{
									continue;
								}
								double weightedAlpha = weightsX[tapX] * weightY * alpha;
								sumRed += sourcePixel[0] * weightedAlpha;
								sumGreen += sourcePixel[1] * weightedAlpha;
								sumBlue += sourcePixel[2] * weightedAlpha;
								sumAlpha += weightedAlpha;
							}
						}
						WriteResolved(destinationRow + (destinationX * 4), sumRed, sumGreen, sumBlue, sumAlpha);
					}
				}
			}
		}

		private sealed unsafe class RotateBicubicWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public int m_sourceWidth;
			public int m_sourceHeight;
			public byte* m_destinationBase;
			public int m_destinationRowBytes;
			public int m_destinationWidth;
			public double m_cos;
			public double m_sin;
			public double m_sourceCenterX;
			public double m_sourceCenterY;
			public double m_destinationCenterX;
			public double m_destinationCenterY;

			public void Band(int start, int end)
			{
				byte* sourceBase = m_sourceBase;
				int sourceRowBytes = m_sourceRowBytes;
				int sourceWidth = m_sourceWidth;
				int sourceHeight = m_sourceHeight;
				byte* destinationBase = m_destinationBase;
				int destinationRowBytes = m_destinationRowBytes;
				int destinationWidth = m_destinationWidth;
				double cos = m_cos;
				double sin = m_sin;
				double sourceCenterX = m_sourceCenterX;
				double sourceCenterY = m_sourceCenterY;
				double destinationCenterX = m_destinationCenterX;
				double destinationCenterY = m_destinationCenterY;
				double* weightsX = stackalloc double[4];
				double* weightsY = stackalloc double[4];
				for (int destinationY = start; destinationY < end; destinationY++)
				{
					double offsetY = destinationY + 0.5 - destinationCenterY;
					double rowTermX = offsetY * sin + sourceCenterX - 0.5;
					double rowTermY = offsetY * cos + sourceCenterY - 0.5;
					byte* destinationRow = destinationBase + (destinationY * destinationRowBytes);
					for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
					{
						double offsetX = destinationX + 0.5 - destinationCenterX;
						double sourceX = offsetX * cos + rowTermX;
						double sourceY = rowTermY - offsetX * sin;
						int baseX = (int)Math.Floor(sourceX);
						int baseY = (int)Math.Floor(sourceY);
						double fractionX = sourceX - baseX;
						double fractionY = sourceY - baseY;
						for (int tap = 0; tap < 4; tap++)
						{
							weightsX[tap] = CubicWeight(fractionX + 1.0 - tap);
							weightsY[tap] = CubicWeight(fractionY + 1.0 - tap);
						}
						double sumRed = 0.0;
						double sumGreen = 0.0;
						double sumBlue = 0.0;
						double sumAlpha = 0.0;
						for (int tapY = 0; tapY < 4; tapY++)
						{
							int sampleY = baseY - 1 + tapY;
							if (sampleY < 0 || sampleY >= sourceHeight)
							{
								continue;
							}
							byte* sourceRow = sourceBase + (sampleY * sourceRowBytes);
							double weightY = weightsY[tapY];
							for (int tapX = 0; tapX < 4; tapX++)
							{
								int sampleX = baseX - 1 + tapX;
								if (sampleX < 0 || sampleX >= sourceWidth)
								{
									continue;
								}
								byte* sourcePixel = sourceRow + (sampleX * 4);
								double alpha = sourcePixel[3];
								if (alpha <= 0.0)
								{
									continue;
								}
								double weightedAlpha = weightsX[tapX] * weightY * alpha;
								sumRed += sourcePixel[0] * weightedAlpha;
								sumGreen += sourcePixel[1] * weightedAlpha;
								sumBlue += sourcePixel[2] * weightedAlpha;
								sumAlpha += weightedAlpha;
							}
						}
						WriteResolved(destinationRow + (destinationX * 4), sumRed, sumGreen, sumBlue, sumAlpha);
					}
				}
			}
		}

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

		private static double SnapTrig(double value)
		{
			if (Math.Abs(value) < 0.000000000001)
			{
				return 0.0;
			}
			if (Math.Abs(value - 1.0) < 0.000000000001)
			{
				return 1.0;
			}
			if (Math.Abs(value + 1.0) < 0.000000000001)
			{
				return -1.0;
			}
			return value;
		}

		private static double CubicWeight(double distance)
		{
			double t = distance;
			if (t < 0.0)
			{
				t = -t;
			}
			if (t <= 1.0)
			{
				return ((1.5 * t - 2.5) * t) * t + 1.0;
			}
			if (t < 2.0)
			{
				return ((-0.5 * t + 2.5) * t - 4.0) * t + 2.0;
			}
			return 0.0;
		}

		private static unsafe void WriteResolved(byte* destinationPixel, double sumRed, double sumGreen, double sumBlue, double sumAlpha)
		{
			if (sumAlpha <= 0.0)
			{
				destinationPixel[0] = 0;
				destinationPixel[1] = 0;
				destinationPixel[2] = 0;
				destinationPixel[3] = 0;
				return;
			}
			destinationPixel[0] = ClampByte(sumRed / sumAlpha);
			destinationPixel[1] = ClampByte(sumGreen / sumAlpha);
			destinationPixel[2] = ClampByte(sumBlue / sumAlpha);
			destinationPixel[3] = ClampByte(sumAlpha);
		}

		private static unsafe void RotateNearest(byte* sourceBase, int sourceRowBytes, int sourceWidth, int sourceHeight, byte* destinationBase, int destinationRowBytes, int destinationWidth, int destinationHeight, double cos, double sin, double sourceCenterX, double sourceCenterY, double destinationCenterX, double destinationCenterY)
		{
			RotateNearestWorker worker = new RotateNearestWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_sourceRowBytes = sourceRowBytes;
			worker.m_sourceWidth = sourceWidth;
			worker.m_sourceHeight = sourceHeight;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationRowBytes = destinationRowBytes;
			worker.m_destinationWidth = destinationWidth;
			worker.m_cos = cos;
			worker.m_sin = sin;
			worker.m_sourceCenterX = sourceCenterX;
			worker.m_sourceCenterY = sourceCenterY;
			worker.m_destinationCenterX = destinationCenterX;
			worker.m_destinationCenterY = destinationCenterY;
			RowBands.Run(0, destinationHeight, worker.Band);
		}

		private static unsafe void RotateBilinear(byte* sourceBase, int sourceRowBytes, int sourceWidth, int sourceHeight, byte* destinationBase, int destinationRowBytes, int destinationWidth, int destinationHeight, double cos, double sin, double sourceCenterX, double sourceCenterY, double destinationCenterX, double destinationCenterY)
		{
			RotateBilinearWorker worker = new RotateBilinearWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_sourceRowBytes = sourceRowBytes;
			worker.m_sourceWidth = sourceWidth;
			worker.m_sourceHeight = sourceHeight;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationRowBytes = destinationRowBytes;
			worker.m_destinationWidth = destinationWidth;
			worker.m_cos = cos;
			worker.m_sin = sin;
			worker.m_sourceCenterX = sourceCenterX;
			worker.m_sourceCenterY = sourceCenterY;
			worker.m_destinationCenterX = destinationCenterX;
			worker.m_destinationCenterY = destinationCenterY;
			RowBands.Run(0, destinationHeight, worker.Band);
		}

		private static unsafe void RotateBicubic(byte* sourceBase, int sourceRowBytes, int sourceWidth, int sourceHeight, byte* destinationBase, int destinationRowBytes, int destinationWidth, int destinationHeight, double cos, double sin, double sourceCenterX, double sourceCenterY, double destinationCenterX, double destinationCenterY)
		{
			RotateBicubicWorker worker = new RotateBicubicWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_sourceRowBytes = sourceRowBytes;
			worker.m_sourceWidth = sourceWidth;
			worker.m_sourceHeight = sourceHeight;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationRowBytes = destinationRowBytes;
			worker.m_destinationWidth = destinationWidth;
			worker.m_cos = cos;
			worker.m_sin = sin;
			worker.m_sourceCenterX = sourceCenterX;
			worker.m_sourceCenterY = sourceCenterY;
			worker.m_destinationCenterX = destinationCenterX;
			worker.m_destinationCenterY = destinationCenterY;
			RowBands.Run(0, destinationHeight, worker.Band);
		}

		public static unsafe SKBitmap Rotate(SKBitmap source, double angleDegrees, int interpolation)
		{
			if (source == null)
			{
				return null;
			}
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			if (sourceWidth <= 0 || sourceHeight <= 0)
			{
				return source.Copy();
			}
			double normalized = angleDegrees % 360.0;
			double radians = normalized * Math.PI / 180.0;
			double cos = SnapTrig(Math.Cos(radians));
			double sin = SnapTrig(Math.Sin(radians));
			double absCos = Math.Abs(cos);
			double absSin = Math.Abs(sin);
			int destinationWidth = (int)Math.Ceiling(sourceWidth * absCos + sourceHeight * absSin);
			int destinationHeight = (int)Math.Ceiling(sourceWidth * absSin + sourceHeight * absCos);
			if (destinationWidth < 1)
			{
				destinationWidth = 1;
			}
			if (destinationHeight < 1)
			{
				destinationHeight = 1;
			}
			SKBitmap destination = new SKBitmap(destinationWidth, destinationHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			double sourceCenterX = sourceWidth * 0.5;
			double sourceCenterY = sourceHeight * 0.5;
			double destinationCenterX = destinationWidth * 0.5;
			double destinationCenterY = destinationHeight * 0.5;
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			int sourceRowBytes = source.RowBytes;
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			int destinationRowBytes = destination.RowBytes;
			if (interpolation == 0)
			{
				RotateNearest(sourceBase, sourceRowBytes, sourceWidth, sourceHeight, destinationBase, destinationRowBytes, destinationWidth, destinationHeight, cos, sin, sourceCenterX, sourceCenterY, destinationCenterX, destinationCenterY);
			}
			else if (interpolation == 2)
			{
				RotateBicubic(sourceBase, sourceRowBytes, sourceWidth, sourceHeight, destinationBase, destinationRowBytes, destinationWidth, destinationHeight, cos, sin, sourceCenterX, sourceCenterY, destinationCenterX, destinationCenterY);
			}
			else
			{
				RotateBilinear(sourceBase, sourceRowBytes, sourceWidth, sourceHeight, destinationBase, destinationRowBytes, destinationWidth, destinationHeight, cos, sin, sourceCenterX, sourceCenterY, destinationCenterX, destinationCenterY);
			}
			return destination;
		}
	}
}
