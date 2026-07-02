using System;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class BrushEngine
	{
		private const double ShoulderExponent = 1.7;

		private byte[] m_coverage;
		private int m_width;
		private int m_height;
		private SKBitmap m_original;
		private bool m_ownsOriginal;
		private int m_radius;
		private double m_hardness;
		private double m_opacity;
		private double m_flow;
		private bool m_square;
		private eBrushOp m_op;
		private int m_cloneOffsetX;
		private int m_cloneOffsetY;
		private int m_blurRadius;
		private double m_smudgeR;
		private double m_smudgeG;
		private double m_smudgeB;
		private double m_smudgeA;
		private bool m_smudgeStarted;
		private eBlendMode m_mode;
		private double m_spacingPx;
		private double m_smoothing;
		private double m_penX;
		private double m_penY;
		private double m_inputX;
		private double m_inputY;
		private bool m_hasPen;
		private double m_distanceSinceStamp;
		private byte m_red;
		private byte m_green;
		private byte m_blue;
		private bool m_active;

		public void SetCloneOffset(int offsetX, int offsetY)
		{
			m_cloneOffsetX = offsetX;
			m_cloneOffsetY = offsetY;
		}

		public void Begin(Layer layer, SKBitmap original, int radius, double hardness, double opacity, double flow, bool square, double spacingFraction, double smoothing, eBrushOp op, eBlendMode mode, SKColor color)
		{
			End();
			SKBitmap bitmap = layer.Bitmap();
			m_width = bitmap.Width;
			m_height = bitmap.Height;
			m_coverage = new byte[m_width * m_height];
			if (original != null && original.Width == m_width && original.Height == m_height)
			{
				m_original = original;
				m_ownsOriginal = false;
			}
			else
			{
				m_original = bitmap.Copy();
				m_ownsOriginal = true;
			}
			m_radius = radius;
			m_hardness = hardness;
			m_opacity = opacity;
			m_flow = flow;
			m_square = square;
			m_op = op;
			m_cloneOffsetX = 0;
			m_cloneOffsetY = 0;
			m_blurRadius = radius / 3;
			if (m_blurRadius < 1)
			{
				m_blurRadius = 1;
			}
			if (m_blurRadius > 12)
			{
				m_blurRadius = 12;
			}
			m_smudgeR = 0.0;
			m_smudgeG = 0.0;
			m_smudgeB = 0.0;
			m_smudgeA = 0.0;
			m_smudgeStarted = false;
			m_mode = mode;
			int diameter = radius * 2;
			if (diameter < 1)
			{
				diameter = 1;
			}
			m_spacingPx = spacingFraction * diameter;
			if (m_spacingPx < 1.0)
			{
				m_spacingPx = 1.0;
			}
			m_smoothing = smoothing;
			if (m_smoothing < 0.0)
			{
				m_smoothing = 0.0;
			}
			if (m_smoothing > 0.95)
			{
				m_smoothing = 0.95;
			}
			m_penX = 0.0;
			m_penY = 0.0;
			m_inputX = 0.0;
			m_inputY = 0.0;
			m_hasPen = false;
			m_distanceSinceStamp = 0.0;
			m_red = color.Red;
			m_green = color.Green;
			m_blue = color.Blue;
			m_active = true;
		}

		public bool IsActive()
		{
			return m_active;
		}

		public void End()
		{
			if (m_ownsOriginal && m_original != null)
			{
				m_original.Dispose();
			}
			m_original = null;
			m_ownsOriginal = false;
			m_coverage = null;
			m_active = false;
		}

		private double TipCoverage(double offsetX, double offsetY)
		{
			if (m_radius <= 0)
			{
				if (System.Math.Abs(offsetX) < 0.5 && System.Math.Abs(offsetY) < 0.5)
				{
					return 1.0;
				}
				return 0.0;
			}
			double outer = m_radius;
			double distance;
			if (m_square)
			{
				double absX = System.Math.Abs(offsetX);
				double absY = System.Math.Abs(offsetY);
				distance = absX;
				if (absY > absX)
				{
					distance = absY;
				}
			}
			else
			{
				distance = System.Math.Sqrt((offsetX * offsetX) + (offsetY * offsetY));
			}
			double inner = m_hardness * outer;
			double antialias = 1.0;
			if (outer - inner < antialias)
			{
				inner = outer - antialias;
			}
			if (inner < 0.0)
			{
				inner = 0.0;
			}
			if (distance <= inner)
			{
				return 1.0;
			}
			if (distance >= outer)
			{
				return 0.0;
			}
			double t = (distance - inner) / (outer - inner);
			t = System.Math.Pow(t, ShoulderExponent);
			double smooth = t * t * (3.0 - 2.0 * t);
			return 1.0 - smooth;
		}

		private double BlendChannel(double cb, double cs)
		{
			if (m_mode == eBlendMode.Multiply)
			{
				return cb * cs;
			}
			if (m_mode == eBlendMode.Screen)
			{
				return 1.0 - ((1.0 - cb) * (1.0 - cs));
			}
			if (m_mode == eBlendMode.Overlay)
			{
				if (cb <= 0.5)
				{
					return 2.0 * cb * cs;
				}
				return 1.0 - (2.0 * (1.0 - cb) * (1.0 - cs));
			}
			if (m_mode == eBlendMode.Add)
			{
				double sum = cb + cs;
				if (sum > 1.0)
				{
					sum = 1.0;
				}
				return sum;
			}
			return cs;
		}

		private double EffectiveSource(byte originalChannel, byte sourceChannel, double originalAlpha)
		{
			double cb = originalChannel / 255.0;
			double cs = sourceChannel / 255.0;
			double blended = BlendChannel(cb, cs);
			double effective = ((1.0 - originalAlpha) * cs) + (originalAlpha * blended);
			return effective * 255.0;
		}

		private byte DodgeBurnChannel(byte channel, double amount)
		{
			double c = channel / 255.0;
			double exposure = amount * 0.5;
			double result;
			if (m_op == eBrushOp.Burn)
			{
				result = c * (1.0 - exposure);
			}
			else
			{
				result = c + ((1.0 - c) * exposure);
			}
			if (result < 0.0)
			{
				result = 0.0;
			}
			if (result > 1.0)
			{
				result = 1.0;
			}
			return (byte)((result * 255.0) + 0.5);
		}

		private static byte ClampByte(double value)
		{
			if (value < 0.0)
			{
				value = 0.0;
			}
			if (value > 255.0)
			{
				value = 255.0;
			}
			return (byte)(value + 0.5);
		}

		private unsafe void BoxAverage(byte* originalPixels, int originalRowBytes, int bitmapX, int bitmapY, out double avgRed, out double avgGreen, out double avgBlue)
		{
			int radius = m_blurRadius;
			int sumRed = 0;
			int sumGreen = 0;
			int sumBlue = 0;
			int count = 0;
			for (int dy = -radius; dy <= radius; dy++)
			{
				int ny = bitmapY + dy;
				if (ny < 0 || ny >= m_height)
				{
					continue;
				}
				byte* row = originalPixels + (ny * originalRowBytes);
				for (int dx = -radius; dx <= radius; dx++)
				{
					int nx = bitmapX + dx;
					if (nx < 0 || nx >= m_width)
					{
						continue;
					}
					byte* sample = row + (nx * 4);
					sumRed = sumRed + sample[0];
					sumGreen = sumGreen + sample[1];
					sumBlue = sumBlue + sample[2];
					count++;
				}
			}
			if (count == 0)
			{
				avgRed = 0.0;
				avgGreen = 0.0;
				avgBlue = 0.0;
				return;
			}
			avgRed = (double)sumRed / count;
			avgGreen = (double)sumGreen / count;
			avgBlue = (double)sumBlue / count;
		}

		public unsafe void StampDab(Layer layer, double centerX, double centerY, Selection selection)
		{
			if (!m_active)
			{
				return;
			}
			SKBitmap bitmap = layer.Bitmap();
			if (bitmap.Width != m_width || bitmap.Height != m_height)
			{
				return;
			}
			int rowBytes = bitmap.RowBytes;
			int originalRowBytes = m_original.RowBytes;
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			byte* pixels = (byte*)bitmap.GetPixels().ToPointer();
			byte* originalPixels = (byte*)m_original.GetPixels().ToPointer();
			bool clip = selection != null && selection.IsActive();
			int radius = m_radius;
			int minCanvasX = (int)System.Math.Floor(centerX) - radius - 1;
			int maxCanvasX = (int)System.Math.Ceiling(centerX) + radius + 1;
			int minCanvasY = (int)System.Math.Floor(centerY) - radius - 1;
			int maxCanvasY = (int)System.Math.Ceiling(centerY) + radius + 1;
			for (int canvasY = minCanvasY; canvasY <= maxCanvasY; canvasY++)
			{
				int bitmapY = canvasY - layerOffsetY;
				if (bitmapY < 0 || bitmapY >= m_height)
				{
					continue;
				}
				double offsetY = canvasY - centerY;
				for (int canvasX = minCanvasX; canvasX <= maxCanvasX; canvasX++)
				{
					double tip = TipCoverage(canvasX - centerX, offsetY);
					if (tip <= 0.0)
					{
						continue;
					}
					if (clip && !selection.IsSelected(canvasX, canvasY))
					{
						continue;
					}
					int bitmapX = canvasX - layerOffsetX;
					if (bitmapX < 0 || bitmapX >= m_width)
					{
						continue;
					}
					int coverageIndex = (bitmapY * m_width) + bitmapX;
					double accumulated = m_coverage[coverageIndex] / 255.0;
					double deposit = m_flow * tip;
					double updated = accumulated + (deposit * (1.0 - accumulated));
					if (updated > 1.0)
					{
						updated = 1.0;
					}
					m_coverage[coverageIndex] = (byte)((updated * 255.0) + 0.5);
					double finalAlpha = updated;
					if (finalAlpha > m_opacity)
					{
						finalAlpha = m_opacity;
					}
					byte* originalPixel = originalPixels + (bitmapY * originalRowBytes) + (bitmapX * 4);
					byte* destinationPixel = pixels + (bitmapY * rowBytes) + (bitmapX * 4);
					double originalAlpha = originalPixel[3] / 255.0;
					if (m_op == eBrushOp.Erase)
					{
						double erasedAlpha = originalAlpha * (1.0 - finalAlpha);
						destinationPixel[0] = originalPixel[0];
						destinationPixel[1] = originalPixel[1];
						destinationPixel[2] = originalPixel[2];
						destinationPixel[3] = (byte)((erasedAlpha * 255.0) + 0.5);
						continue;
					}
					if (m_op == eBrushOp.Dodge || m_op == eBrushOp.Burn)
					{
						destinationPixel[0] = DodgeBurnChannel(originalPixel[0], finalAlpha);
						destinationPixel[1] = DodgeBurnChannel(originalPixel[1], finalAlpha);
						destinationPixel[2] = DodgeBurnChannel(originalPixel[2], finalAlpha);
						destinationPixel[3] = originalPixel[3];
						continue;
					}
					if (m_op == eBrushOp.Blur || m_op == eBrushOp.Sharpen)
					{
						double avgRed;
						double avgGreen;
						double avgBlue;
						BoxAverage(originalPixels, originalRowBytes, bitmapX, bitmapY, out avgRed, out avgGreen, out avgBlue);
						double targetRed = avgRed;
						double targetGreen = avgGreen;
						double targetBlue = avgBlue;
						if (m_op == eBrushOp.Sharpen)
						{
							targetRed = originalPixel[0] + (originalPixel[0] - avgRed);
							targetGreen = originalPixel[1] + (originalPixel[1] - avgGreen);
							targetBlue = originalPixel[2] + (originalPixel[2] - avgBlue);
						}
						destinationPixel[0] = ClampByte(originalPixel[0] + ((targetRed - originalPixel[0]) * finalAlpha));
						destinationPixel[1] = ClampByte(originalPixel[1] + ((targetGreen - originalPixel[1]) * finalAlpha));
						destinationPixel[2] = ClampByte(originalPixel[2] + ((targetBlue - originalPixel[2]) * finalAlpha));
						destinationPixel[3] = originalPixel[3];
						continue;
					}
					if (m_op == eBrushOp.Smudge)
					{
						double curRed = destinationPixel[0];
						double curGreen = destinationPixel[1];
						double curBlue = destinationPixel[2];
						double curAlpha = destinationPixel[3];
						if (!m_smudgeStarted)
						{
							m_smudgeR = curRed;
							m_smudgeG = curGreen;
							m_smudgeB = curBlue;
							m_smudgeA = curAlpha;
							m_smudgeStarted = true;
						}
						double mix = tip * m_opacity;
						if (mix > 1.0)
						{
							mix = 1.0;
						}
						destinationPixel[0] = ClampByte(curRed + ((m_smudgeR - curRed) * mix));
						destinationPixel[1] = ClampByte(curGreen + ((m_smudgeG - curGreen) * mix));
						destinationPixel[2] = ClampByte(curBlue + ((m_smudgeB - curBlue) * mix));
						destinationPixel[3] = ClampByte(curAlpha + ((m_smudgeA - curAlpha) * mix));
						m_smudgeR = m_smudgeR + ((curRed - m_smudgeR) * mix);
						m_smudgeG = m_smudgeG + ((curGreen - m_smudgeG) * mix);
						m_smudgeB = m_smudgeB + ((curBlue - m_smudgeB) * mix);
						m_smudgeA = m_smudgeA + ((curAlpha - m_smudgeA) * mix);
						continue;
					}
					if (m_op == eBrushOp.Clone)
					{
						int cloneX = bitmapX - m_cloneOffsetX;
						int cloneY = bitmapY - m_cloneOffsetY;
						if (cloneX < 0 || cloneY < 0 || cloneX >= m_width || cloneY >= m_height)
						{
							continue;
						}
						byte* clonePixel = originalPixels + (cloneY * originalRowBytes) + (cloneX * 4);
						double cloneCoverage = finalAlpha * (clonePixel[3] / 255.0);
						if (cloneCoverage <= 0.0)
						{
							continue;
						}
						double cloneInverse = 1.0 - cloneCoverage;
						double cloneOutAlpha = cloneCoverage + (originalAlpha * cloneInverse);
						if (cloneOutAlpha <= 0.0)
						{
							destinationPixel[0] = 0;
							destinationPixel[1] = 0;
							destinationPixel[2] = 0;
							destinationPixel[3] = 0;
							continue;
						}
						double cloneWeighted = originalAlpha * cloneInverse;
						destinationPixel[0] = ClampByte(((clonePixel[0] * cloneCoverage) + (originalPixel[0] * cloneWeighted)) / cloneOutAlpha);
						destinationPixel[1] = ClampByte(((clonePixel[1] * cloneCoverage) + (originalPixel[1] * cloneWeighted)) / cloneOutAlpha);
						destinationPixel[2] = ClampByte(((clonePixel[2] * cloneCoverage) + (originalPixel[2] * cloneWeighted)) / cloneOutAlpha);
						destinationPixel[3] = (byte)((cloneOutAlpha * 255.0) + 0.5);
						continue;
					}
					double sourceRed = m_red;
					double sourceGreen = m_green;
					double sourceBlue = m_blue;
					if (m_mode != eBlendMode.Normal)
					{
						sourceRed = EffectiveSource(originalPixel[0], m_red, originalAlpha);
						sourceGreen = EffectiveSource(originalPixel[1], m_green, originalAlpha);
						sourceBlue = EffectiveSource(originalPixel[2], m_blue, originalAlpha);
					}
					double inverse = 1.0 - finalAlpha;
					double outAlpha = finalAlpha + (originalAlpha * inverse);
					if (outAlpha <= 0.0)
					{
						destinationPixel[0] = 0;
						destinationPixel[1] = 0;
						destinationPixel[2] = 0;
						destinationPixel[3] = 0;
						continue;
					}
					double weightedOriginal = originalAlpha * inverse;
					double outRed = ((sourceRed * finalAlpha) + (originalPixel[0] * weightedOriginal)) / outAlpha;
					double outGreen = ((sourceGreen * finalAlpha) + (originalPixel[1] * weightedOriginal)) / outAlpha;
					double outBlue = ((sourceBlue * finalAlpha) + (originalPixel[2] * weightedOriginal)) / outAlpha;
					destinationPixel[0] = (byte)(outRed + 0.5);
					destinationPixel[1] = (byte)(outGreen + 0.5);
					destinationPixel[2] = (byte)(outBlue + 0.5);
					destinationPixel[3] = (byte)((outAlpha * 255.0) + 0.5);
				}
			}
		}

		private void MarkDirty(Document document, double x0, double y0, double x1, double y1)
		{
			int pad = m_radius + 1;
			double lowX = x0;
			if (x1 < lowX)
			{
				lowX = x1;
			}
			double highX = x0;
			if (x1 > highX)
			{
				highX = x1;
			}
			double lowY = y0;
			if (y1 < lowY)
			{
				lowY = y1;
			}
			double highY = y0;
			if (y1 > highY)
			{
				highY = y1;
			}
			int left = (int)System.Math.Floor(lowX) - pad;
			int top = (int)System.Math.Floor(lowY) - pad;
			int right = (int)System.Math.Ceiling(highX) + pad;
			int bottom = (int)System.Math.Ceiling(highY) + pad;
			document.MarkComposeDirtyRegion(new SKRectI(left, top, right, bottom));
		}

		public void StampFirst(Document document, Layer layer, double x, double y, Selection selection)
		{
			StampDab(layer, x, y, selection);
			m_penX = x;
			m_penY = y;
			m_inputX = x;
			m_inputY = y;
			m_hasPen = true;
			m_distanceSinceStamp = 0.0;
			MarkDirty(document, x, y, x, y);
		}

		public void StrokeTo(Document document, Layer layer, double rawX, double rawY, Selection selection)
		{
			if (!m_hasPen)
			{
				StampFirst(document, layer, rawX, rawY, selection);
				return;
			}
			double startPenX = m_penX;
			double startPenY = m_penY;
			double alpha = 1.0 - m_smoothing;
			m_inputX = m_inputX + ((rawX - m_inputX) * alpha);
			m_inputY = m_inputY + ((rawY - m_inputY) * alpha);
			double x = m_inputX;
			double y = m_inputY;
			double deltaX = x - m_penX;
			double deltaY = y - m_penY;
			double segmentLength = System.Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
			if (segmentLength <= 0.0)
			{
				return;
			}
			double directionX = deltaX / segmentLength;
			double directionY = deltaY / segmentLength;
			double traveled = 0.0;
			for (;;)
			{
				double distanceToNext = m_spacingPx - m_distanceSinceStamp;
				if (traveled + distanceToNext > segmentLength)
				{
					break;
				}
				traveled = traveled + distanceToNext;
				double stampX = m_penX + (directionX * traveled);
				double stampY = m_penY + (directionY * traveled);
				StampDab(layer, stampX, stampY, selection);
				m_distanceSinceStamp = 0.0;
			}
			m_distanceSinceStamp = m_distanceSinceStamp + (segmentLength - traveled);
			m_penX = x;
			m_penY = y;
			MarkDirty(document, startPenX, startPenY, m_penX, m_penY);
		}
	}
}
