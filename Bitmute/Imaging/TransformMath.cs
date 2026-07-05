using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class TransformMath
	{
		private sealed unsafe class WarpWorker
		{
			public byte* m_sourceBase;
			public int m_sourceRowBytes;
			public int m_sourceWidth;
			public int m_sourceHeight;
			public byte* m_destinationBase;
			public int m_destinationRowBytes;
			public int m_destinationWidth;
			public int m_originX;
			public int m_originY;
			public double m_m0;
			public double m_m1;
			public double m_m2;
			public double m_m3;
			public double m_m4;
			public double m_m5;
			public double m_m6;
			public double m_m7;
			public double m_m8;
			public int m_interpolation;

			public void Band(int start, int end)
			{
				byte* sourceBase = m_sourceBase;
				int sourceRowBytes = m_sourceRowBytes;
				int sourceWidth = m_sourceWidth;
				int sourceHeight = m_sourceHeight;
				byte* destinationBase = m_destinationBase;
				int destinationRowBytes = m_destinationRowBytes;
				int destinationWidth = m_destinationWidth;
				int originX = m_originX;
				int originY = m_originY;
				double m0 = m_m0;
				double m1 = m_m1;
				double m2 = m_m2;
				double m3 = m_m3;
				double m4 = m_m4;
				double m5 = m_m5;
				double m6 = m_m6;
				double m7 = m_m7;
				double m8 = m_m8;
				int interpolation = m_interpolation;
				double* weightsX = stackalloc double[4];
				double* weightsY = stackalloc double[4];
				for (int destinationY = start; destinationY < end; destinationY++)
				{
					double targetY = originY + destinationY + 0.5;
					double rowNumeratorX = m1 * targetY + m2;
					double rowNumeratorY = m4 * targetY + m5;
					double rowDenominator = m7 * targetY + m8;
					byte* destinationRow = destinationBase + (destinationY * destinationRowBytes);
					for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
					{
						double targetX = originX + destinationX + 0.5;
						double denominator = m6 * targetX + rowDenominator;
						byte* destinationPixel = destinationRow + (destinationX * 4);
						if (Math.Abs(denominator) < 0.0000000001)
						{
							destinationPixel[0] = 0;
							destinationPixel[1] = 0;
							destinationPixel[2] = 0;
							destinationPixel[3] = 0;
							continue;
						}
						double inverseDenominator = 1.0 / denominator;
						double sourceX = (m0 * targetX + rowNumeratorX) * inverseDenominator;
						double sourceY = (m3 * targetX + rowNumeratorY) * inverseDenominator;
						if (sourceX < 0.0 || sourceY < 0.0 || sourceX > sourceWidth || sourceY > sourceHeight)
						{
							destinationPixel[0] = 0;
							destinationPixel[1] = 0;
							destinationPixel[2] = 0;
							destinationPixel[3] = 0;
							continue;
						}
						double sampleX = sourceX - 0.5;
						double sampleY = sourceY - 0.5;
						if (interpolation == 0)
						{
							SampleNearest(sourceBase, sourceRowBytes, sourceWidth, sourceHeight, sampleX, sampleY, destinationPixel);
						}
						else if (interpolation == 2)
						{
							SampleBicubic(sourceBase, sourceRowBytes, sourceWidth, sourceHeight, sampleX, sampleY, destinationPixel, weightsX, weightsY);
						}
						else
						{
							SampleBilinear(sourceBase, sourceRowBytes, sourceWidth, sourceHeight, sampleX, sampleY, destinationPixel, weightsX, weightsY);
						}
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

		public static bool QuadMatrix(SKPoint[] destQuad, float sourceWidth, float sourceHeight, out SKMatrix matrix)
		{
			matrix = SKMatrix.CreateIdentity();
			if (sourceWidth <= 0.0f || sourceHeight <= 0.0f)
			{
				return false;
			}
			double p0x = destQuad[0].X;
			double p0y = destQuad[0].Y;
			double p1x = destQuad[1].X;
			double p1y = destQuad[1].Y;
			double p2x = destQuad[2].X;
			double p2y = destQuad[2].Y;
			double p3x = destQuad[3].X;
			double p3y = destQuad[3].Y;
			double dx1 = p1x - p2x;
			double dx2 = p3x - p2x;
			double dx3 = p0x - p1x + p2x - p3x;
			double dy1 = p1y - p2y;
			double dy2 = p3y - p2y;
			double dy3 = p0y - p1y + p2y - p3y;
			double a;
			double b;
			double c;
			double d;
			double e;
			double f;
			double g;
			double h;
			double i;
			if (Math.Abs(dx3) < 0.0000000001 && Math.Abs(dy3) < 0.0000000001)
			{
				a = p1x - p0x;
				b = p3x - p0x;
				c = p0x;
				d = p1y - p0y;
				e = p3y - p0y;
				f = p0y;
				g = 0.0;
				h = 0.0;
				i = 1.0;
			}
			else
			{
				double denominator = dx1 * dy2 - dx2 * dy1;
				if (Math.Abs(denominator) < 0.0000000001)
				{
					return false;
				}
				g = (dx3 * dy2 - dx2 * dy3) / denominator;
				h = (dx1 * dy3 - dx3 * dy1) / denominator;
				a = p1x - p0x + g * p1x;
				b = p3x - p0x + h * p3x;
				c = p0x;
				d = p1y - p0y + g * p1y;
				e = p3y - p0y + h * p3y;
				f = p0y;
				i = 1.0;
			}
			double invWidth = 1.0 / sourceWidth;
			double invHeight = 1.0 / sourceHeight;
			matrix.ScaleX = (float)(a * invWidth);
			matrix.SkewX = (float)(b * invHeight);
			matrix.TransX = (float)c;
			matrix.SkewY = (float)(d * invWidth);
			matrix.ScaleY = (float)(e * invHeight);
			matrix.TransY = (float)f;
			matrix.Persp0 = (float)(g * invWidth);
			matrix.Persp1 = (float)(h * invHeight);
			matrix.Persp2 = (float)i;
			return true;
		}

		private static unsafe bool SolveInverseHomography(SKPoint[] destQuad, int sourceWidth, int sourceHeight, double* result)
		{
			double p0x = destQuad[0].X;
			double p0y = destQuad[0].Y;
			double p1x = destQuad[1].X;
			double p1y = destQuad[1].Y;
			double p2x = destQuad[2].X;
			double p2y = destQuad[2].Y;
			double p3x = destQuad[3].X;
			double p3y = destQuad[3].Y;
			double dx1 = p1x - p2x;
			double dx2 = p3x - p2x;
			double dx3 = p0x - p1x + p2x - p3x;
			double dy1 = p1y - p2y;
			double dy2 = p3y - p2y;
			double dy3 = p0y - p1y + p2y - p3y;
			double a;
			double b;
			double c;
			double d;
			double e;
			double f;
			double g;
			double h;
			double i;
			if (Math.Abs(dx3) < 0.0000000001 && Math.Abs(dy3) < 0.0000000001)
			{
				a = p1x - p0x;
				b = p3x - p0x;
				c = p0x;
				d = p1y - p0y;
				e = p3y - p0y;
				f = p0y;
				g = 0.0;
				h = 0.0;
				i = 1.0;
			}
			else
			{
				double denominator = dx1 * dy2 - dx2 * dy1;
				if (Math.Abs(denominator) < 0.0000000001)
				{
					return false;
				}
				g = (dx3 * dy2 - dx2 * dy3) / denominator;
				h = (dx1 * dy3 - dx3 * dy1) / denominator;
				a = p1x - p0x + g * p1x;
				b = p3x - p0x + h * p3x;
				c = p0x;
				d = p1y - p0y + g * p1y;
				e = p3y - p0y + h * p3y;
				f = p0y;
				i = 1.0;
			}
			double cofactor0 = e * i - f * h;
			double cofactor1 = c * h - b * i;
			double cofactor2 = b * f - c * e;
			double cofactor3 = f * g - d * i;
			double cofactor4 = a * i - c * g;
			double cofactor5 = c * d - a * f;
			double cofactor6 = d * h - e * g;
			double cofactor7 = b * g - a * h;
			double cofactor8 = a * e - b * d;
			double determinant = a * cofactor0 + b * cofactor3 + c * cofactor6;
			if (Math.Abs(determinant) < 0.0000000001)
			{
				return false;
			}
			double inverseDeterminant = 1.0 / determinant;
			double m0 = cofactor0 * inverseDeterminant;
			double m1 = cofactor1 * inverseDeterminant;
			double m2 = cofactor2 * inverseDeterminant;
			double m3 = cofactor3 * inverseDeterminant;
			double m4 = cofactor4 * inverseDeterminant;
			double m5 = cofactor5 * inverseDeterminant;
			double m6 = cofactor6 * inverseDeterminant;
			double m7 = cofactor7 * inverseDeterminant;
			double m8 = cofactor8 * inverseDeterminant;
			double scaleX = sourceWidth;
			double scaleY = sourceHeight;
			result[0] = m0 * scaleX;
			result[1] = m1 * scaleX;
			result[2] = m2 * scaleX;
			result[3] = m3 * scaleY;
			result[4] = m4 * scaleY;
			result[5] = m5 * scaleY;
			result[6] = m6;
			result[7] = m7;
			result[8] = m8;
			return true;
		}

		private static unsafe void SampleNearest(byte* sourceBase, int sourceRowBytes, int sourceWidth, int sourceHeight, double sourceX, double sourceY, byte* destinationPixel)
		{
			int nearestX = (int)Math.Floor(sourceX);
			int nearestY = (int)Math.Floor(sourceY);
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

		private static unsafe void SampleBilinear(byte* sourceBase, int sourceRowBytes, int sourceWidth, int sourceHeight, double sourceX, double sourceY, byte* destinationPixel, double* weightsX, double* weightsY)
		{
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
			WriteResolved(destinationPixel, sumRed, sumGreen, sumBlue, sumAlpha);
		}

		private static unsafe void SampleBicubic(byte* sourceBase, int sourceRowBytes, int sourceWidth, int sourceHeight, double sourceX, double sourceY, byte* destinationPixel, double* weightsX, double* weightsY)
		{
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
			WriteResolved(destinationPixel, sumRed, sumGreen, sumBlue, sumAlpha);
		}

		public static unsafe SKBitmap Warp(SKBitmap source, SKPoint[] destQuad, int interpolation, out int outX, out int outY)
		{
			outX = 0;
			outY = 0;
			if (source == null)
			{
				return null;
			}
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			if (sourceWidth <= 0 || sourceHeight <= 0)
			{
				return null;
			}
			if (destQuad == null || destQuad.Length != 4)
			{
				return null;
			}
			double minX = destQuad[0].X;
			double maxX = destQuad[0].X;
			double minY = destQuad[0].Y;
			double maxY = destQuad[0].Y;
			for (int index = 1; index < 4; index++)
			{
				double cornerX = destQuad[index].X;
				double cornerY = destQuad[index].Y;
				if (cornerX < minX)
				{
					minX = cornerX;
				}
				if (cornerX > maxX)
				{
					maxX = cornerX;
				}
				if (cornerY < minY)
				{
					minY = cornerY;
				}
				if (cornerY > maxY)
				{
					maxY = cornerY;
				}
			}
			int originX = (int)Math.Floor(minX);
			int originY = (int)Math.Floor(minY);
			int destinationWidth = (int)Math.Ceiling(maxX) - originX;
			int destinationHeight = (int)Math.Ceiling(maxY) - originY;
			if (destinationWidth < 1)
			{
				destinationWidth = 1;
			}
			if (destinationHeight < 1)
			{
				destinationHeight = 1;
			}
			double* matrix = stackalloc double[9];
			if (!SolveInverseHomography(destQuad, sourceWidth, sourceHeight, matrix))
			{
				return null;
			}
			double m0 = matrix[0];
			double m1 = matrix[1];
			double m2 = matrix[2];
			double m3 = matrix[3];
			double m4 = matrix[4];
			double m5 = matrix[5];
			double m6 = matrix[6];
			double m7 = matrix[7];
			double m8 = matrix[8];
			SKBitmap destination = new SKBitmap(destinationWidth, destinationHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			int sourceRowBytes = source.RowBytes;
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			int destinationRowBytes = destination.RowBytes;
			WarpWorker worker = new WarpWorker();
			worker.m_sourceBase = sourceBase;
			worker.m_sourceRowBytes = sourceRowBytes;
			worker.m_sourceWidth = sourceWidth;
			worker.m_sourceHeight = sourceHeight;
			worker.m_destinationBase = destinationBase;
			worker.m_destinationRowBytes = destinationRowBytes;
			worker.m_destinationWidth = destinationWidth;
			worker.m_originX = originX;
			worker.m_originY = originY;
			worker.m_m0 = m0;
			worker.m_m1 = m1;
			worker.m_m2 = m2;
			worker.m_m3 = m3;
			worker.m_m4 = m4;
			worker.m_m5 = m5;
			worker.m_m6 = m6;
			worker.m_m7 = m7;
			worker.m_m8 = m8;
			worker.m_interpolation = interpolation;
			RowBands.Run(0, destinationHeight, worker.Band);
			outX = originX;
			outY = originY;
			return destination;
		}

		public static unsafe SKBitmap WarpAffine(SKBitmap source, float a, float b, float c, float d, float e, float f, int interpolation, out int outX, out int outY)
		{
			outX = 0;
			outY = 0;
			if (source == null)
			{
				return null;
			}
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			if (sourceWidth <= 0 || sourceHeight <= 0)
			{
				return null;
			}
			double width = sourceWidth;
			double height = sourceHeight;
			SKPoint[] destQuad = new SKPoint[4];
			destQuad[0] = new SKPoint(e, f);
			destQuad[1] = new SKPoint((float)(a * width + e), (float)(b * width + f));
			destQuad[2] = new SKPoint((float)(a * width + c * height + e), (float)(b * width + d * height + f));
			destQuad[3] = new SKPoint((float)(c * height + e), (float)(d * height + f));
			return Warp(source, destQuad, interpolation, out outX, out outY);
		}
	}
}
