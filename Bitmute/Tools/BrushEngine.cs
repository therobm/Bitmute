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
		private double m_strength;
		private bool m_square;
		private eBrushOp m_op;
		private int m_cloneOffsetX;
		private int m_cloneOffsetY;
		private int m_blurRadius;
		private int m_dodgeBurnRange;
		private double m_exposure;
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
		private bool m_spongeSaturate;
		private int m_colorReplaceMode;
		private int m_colorReplaceTolerance;
		private bool m_colorReplaceHasSample;
		private int m_colorReplaceSampleR;
		private int m_colorReplaceSampleG;
		private int m_colorReplaceSampleB;
		private bool m_lockAlpha;
		private bool m_active;

		public void SetCloneOffset(int offsetX, int offsetY)
		{
			m_cloneOffsetX = offsetX;
			m_cloneOffsetY = offsetY;
		}

		public void SetStrength(double strength)
		{
			m_strength = strength;
		}

		public void SetDodgeBurn(int range, double exposure)
		{
			m_dodgeBurnRange = range;
			m_exposure = exposure;
		}

		public void SetSpongeSaturate(bool saturate)
		{
			m_spongeSaturate = saturate;
		}

		public void SetColorReplace(int mode, int tolerance)
		{
			m_colorReplaceMode = mode;
			m_colorReplaceTolerance = tolerance;
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
			m_strength = 1.0;
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
			m_dodgeBurnRange = 1;
			m_exposure = 0.5;
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
			m_spongeSaturate = false;
			m_colorReplaceMode = 0;
			m_colorReplaceTolerance = 0;
			m_colorReplaceHasSample = false;
			m_colorReplaceSampleR = 0;
			m_colorReplaceSampleG = 0;
			m_colorReplaceSampleB = 0;
			m_lockAlpha = layer.LockAlpha();
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
			double antialias = outer * 0.08;
			if (antialias > 1.0)
			{
				antialias = 1.0;
			}
			if (antialias < 0.4)
			{
				antialias = 0.4;
			}
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

		private double RangeWeight(double luminance)
		{
			if (m_dodgeBurnRange == 0)
			{
				double shadow = 1.0 - luminance;
				return shadow * shadow;
			}
			if (m_dodgeBurnRange == 2)
			{
				return luminance * luminance;
			}
			return 4.0 * luminance * (1.0 - luminance);
		}

		private byte DodgeBurnChannel(byte channel, double amount, double rangeWeight)
		{
			double c = channel / 255.0;
			double exposure = amount * m_exposure * rangeWeight;
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
			if (m_op == eBrushOp.Smudge)
			{
				StampSmudgeDab(layer, centerX, centerY, selection);
				return;
			}
			int rowBytes = bitmap.RowBytes;
			int originalRowBytes = m_original.RowBytes;
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			byte* pixels = (byte*)bitmap.GetPixels().ToPointer();
			byte* originalPixels = (byte*)m_original.GetPixels().ToPointer();
			if (m_op == eBrushOp.ColorReplace && !m_colorReplaceHasSample)
			{
				int sampleX = (int)System.Math.Round(centerX) - layerOffsetX;
				int sampleY = (int)System.Math.Round(centerY) - layerOffsetY;
				if (sampleX >= 0 && sampleY >= 0 && sampleX < m_width && sampleY < m_height)
				{
					byte* samplePixel = originalPixels + (sampleY * originalRowBytes) + (sampleX * 4);
					m_colorReplaceSampleR = samplePixel[0];
					m_colorReplaceSampleG = samplePixel[1];
					m_colorReplaceSampleB = samplePixel[2];
					m_colorReplaceHasSample = true;
				}
			}
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
					if (m_lockAlpha && originalPixel[3] == 0)
					{
						continue;
					}
					if (m_op == eBrushOp.Erase)
					{
						if (m_lockAlpha)
						{
							continue;
						}
						double erasedAlpha = originalAlpha * (1.0 - finalAlpha);
						destinationPixel[0] = originalPixel[0];
						destinationPixel[1] = originalPixel[1];
						destinationPixel[2] = originalPixel[2];
						destinationPixel[3] = (byte)((erasedAlpha * 255.0) + 0.5);
						continue;
					}
					if (m_op == eBrushOp.Dodge || m_op == eBrushOp.Burn)
					{
						double dodgeBurnLuminance = ((0.299 * originalPixel[0]) + (0.587 * originalPixel[1]) + (0.114 * originalPixel[2])) / 255.0;
						double dodgeBurnWeight = RangeWeight(dodgeBurnLuminance);
						destinationPixel[0] = DodgeBurnChannel(originalPixel[0], finalAlpha, dodgeBurnWeight);
						destinationPixel[1] = DodgeBurnChannel(originalPixel[1], finalAlpha, dodgeBurnWeight);
						destinationPixel[2] = DodgeBurnChannel(originalPixel[2], finalAlpha, dodgeBurnWeight);
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
						double blurEffect = finalAlpha * m_strength;
						destinationPixel[0] = ClampByte(originalPixel[0] + ((targetRed - originalPixel[0]) * blurEffect));
						destinationPixel[1] = ClampByte(originalPixel[1] + ((targetGreen - originalPixel[1]) * blurEffect));
						destinationPixel[2] = ClampByte(originalPixel[2] + ((targetBlue - originalPixel[2]) * blurEffect));
						destinationPixel[3] = originalPixel[3];
						continue;
					}
					if (m_op == eBrushOp.Sponge)
					{
						byte spongeR;
						byte spongeG;
						byte spongeB;
						SpongeMath.Apply(originalPixel[0], originalPixel[1], originalPixel[2], m_spongeSaturate, finalAlpha, out spongeR, out spongeG, out spongeB);
						destinationPixel[0] = spongeR;
						destinationPixel[1] = spongeG;
						destinationPixel[2] = spongeB;
						destinationPixel[3] = originalPixel[3];
						continue;
					}
					if (m_op == eBrushOp.ColorReplace)
					{
						if (m_colorReplaceHasSample)
						{
							int deltaR = originalPixel[0] - m_colorReplaceSampleR;
							if (deltaR < 0)
							{
								deltaR = -deltaR;
							}
							int deltaG = originalPixel[1] - m_colorReplaceSampleG;
							if (deltaG < 0)
							{
								deltaG = -deltaG;
							}
							int deltaB = originalPixel[2] - m_colorReplaceSampleB;
							if (deltaB < 0)
							{
								deltaB = -deltaB;
							}
							int maxDelta = deltaR;
							if (deltaG > maxDelta)
							{
								maxDelta = deltaG;
							}
							if (deltaB > maxDelta)
							{
								maxDelta = deltaB;
							}
							if (maxDelta > m_colorReplaceTolerance)
							{
								continue;
							}
						}
						byte replaceR;
						byte replaceG;
						byte replaceB;
						ColorReplaceMath.Apply(originalPixel[0], originalPixel[1], originalPixel[2], m_red, m_green, m_blue, m_colorReplaceMode, finalAlpha, out replaceR, out replaceG, out replaceB);
						destinationPixel[0] = replaceR;
						destinationPixel[1] = replaceG;
						destinationPixel[2] = replaceB;
						destinationPixel[3] = originalPixel[3];
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
					if (m_op == eBrushOp.Heal)
					{
						int healSourceX = bitmapX - m_cloneOffsetX;
						int healSourceY = bitmapY - m_cloneOffsetY;
						if (healSourceX < 0 || healSourceY < 0 || healSourceX >= m_width || healSourceY >= m_height)
						{
							continue;
						}
						byte* healSourcePixel = originalPixels + (healSourceY * originalRowBytes) + (healSourceX * 4);
						double sourceAvgR;
						double sourceAvgG;
						double sourceAvgB;
						BoxAverage(originalPixels, originalRowBytes, healSourceX, healSourceY, out sourceAvgR, out sourceAvgG, out sourceAvgB);
						double destAvgR;
						double destAvgG;
						double destAvgB;
						BoxAverage(originalPixels, originalRowBytes, bitmapX, bitmapY, out destAvgR, out destAvgG, out destAvgB);
						byte healR;
						byte healG;
						byte healB;
						HealMath.Apply(healSourcePixel[0], healSourcePixel[1], healSourcePixel[2], sourceAvgR, sourceAvgG, sourceAvgB, destAvgR, destAvgG, destAvgB, finalAlpha, originalPixel[0], originalPixel[1], originalPixel[2], out healR, out healG, out healB);
						destinationPixel[0] = healR;
						destinationPixel[1] = healG;
						destinationPixel[2] = healB;
						destinationPixel[3] = originalPixel[3];
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

		private unsafe void StampSmudgeDab(Layer layer, double centerX, double centerY, Selection selection)
		{
			SKBitmap bitmap = layer.Bitmap();
			int rowBytes = bitmap.RowBytes;
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			byte* pixels = (byte*)bitmap.GetPixels().ToPointer();
			bool clip = selection != null && selection.IsActive();
			int radius = m_radius;
			int minCanvasX = (int)System.Math.Floor(centerX) - radius - 1;
			int maxCanvasX = (int)System.Math.Ceiling(centerX) + radius + 1;
			int minCanvasY = (int)System.Math.Floor(centerY) - radius - 1;
			int maxCanvasY = (int)System.Math.Ceiling(centerY) + radius + 1;

			double sumRed = 0.0;
			double sumGreen = 0.0;
			double sumBlue = 0.0;
			double sumAlpha = 0.0;
			double sumWeight = 0.0;
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
					byte* pixel = pixels + (bitmapY * rowBytes) + (bitmapX * 4);
					sumRed = sumRed + (pixel[0] * tip);
					sumGreen = sumGreen + (pixel[1] * tip);
					sumBlue = sumBlue + (pixel[2] * tip);
					sumAlpha = sumAlpha + (pixel[3] * tip);
					sumWeight = sumWeight + tip;
				}
			}
			if (sumWeight <= 0.0)
			{
				return;
			}
			double averageRed = sumRed / sumWeight;
			double averageGreen = sumGreen / sumWeight;
			double averageBlue = sumBlue / sumWeight;
			double averageAlpha = sumAlpha / sumWeight;
			if (!m_smudgeStarted)
			{
				m_smudgeR = averageRed;
				m_smudgeG = averageGreen;
				m_smudgeB = averageBlue;
				m_smudgeA = averageAlpha;
				m_smudgeStarted = true;
			}

			double strength = m_strength;
			if (strength > 1.0)
			{
				strength = 1.0;
			}
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
					double mix = tip * strength;
					if (mix > 1.0)
					{
						mix = 1.0;
					}
					byte* pixel = pixels + (bitmapY * rowBytes) + (bitmapX * 4);
					double currentRed = pixel[0];
					double currentGreen = pixel[1];
					double currentBlue = pixel[2];
					double currentAlpha = pixel[3];
					pixel[0] = ClampByte(currentRed + ((m_smudgeR - currentRed) * mix));
					pixel[1] = ClampByte(currentGreen + ((m_smudgeG - currentGreen) * mix));
					pixel[2] = ClampByte(currentBlue + ((m_smudgeB - currentBlue) * mix));
					pixel[3] = ClampByte(currentAlpha + ((m_smudgeA - currentAlpha) * mix));
				}
			}
			m_smudgeR = m_smudgeR + ((averageRed - m_smudgeR) * strength);
			m_smudgeG = m_smudgeG + ((averageGreen - m_smudgeG) * strength);
			m_smudgeB = m_smudgeB + ((averageBlue - m_smudgeB) * strength);
			m_smudgeA = m_smudgeA + ((averageAlpha - m_smudgeA) * strength);
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

		public void AirbrushStamp(Document document, Layer layer, double x, double y, Selection selection)
		{
			StampDab(layer, x, y, selection);
			MarkDirty(document, x, y, x, y);
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
