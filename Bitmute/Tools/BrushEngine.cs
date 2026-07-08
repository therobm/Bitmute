using System;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class BrushEngine
	{
		private const double ShoulderExponent = 1.7;
		private const long ParallelWorkThreshold = 262144;
		private const long ParallelBoxAverageWorkThreshold = 16384;

		private static byte[] s_coveragePool;
		private static byte[] s_ceilingPool;
		private static int s_coveragePoolWidth;
		private static bool s_coverageDirtyValid;
		private static int s_coverageDirtyLeft;
		private static int s_coverageDirtyTop;
		private static int s_coverageDirtyRight;
		private static int s_coverageDirtyBottom;

		private sealed class DabBandWorker
		{
			public BrushEngine m_engine;
			public Layer m_layer;
			public Selection m_selection;

			public void Band(int start, int end)
			{
				m_engine.StampQueuedDabRows(m_layer, m_selection, start, end);
			}
		}

		private byte[] m_coverage;
		private byte[] m_ceiling;
		private int m_width;
		private int m_height;
		private SKBitmap m_original;
		private bool m_ownsOriginal;
		private int m_radius;
		private int m_radiusBase;
		private double m_currentPressure = 1.0;
		private bool m_pressureSizeEnabled;
		private bool m_pressureOpacityEnabled;
		private double m_pressureOpacityMinimum;
		private double m_hardness;
		private double m_tipInner;
		private double m_tipOuter;
		private double m_opacity;
		private double m_flow;
		private double m_strength;
		private bool m_square;
		private double m_tipAspect;
		private double m_tipCos;
		private double m_tipSin;
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
		private bool m_highDepth;
		private bool m_active;
		private double[] m_dabQueueX;
		private double[] m_dabQueueY;
		private int m_dabQueueCount;
		private double m_fadeLengthPx;
		private double m_fadeTraveled;
		private byte[] m_customTipCoverage;
		private int m_customTipWidth;
		private int m_customTipHeight;

		public void SetFade(double fadeLengthPx)
		{
			m_fadeLengthPx = fadeLengthPx;
		}

		public void SetCustomTip(SKBitmap tip)
		{
			if (tip == null)
			{
				m_customTipCoverage = null;
				m_customTipWidth = 0;
				m_customTipHeight = 0;
				return;
			}
			int width = tip.Width;
			int height = tip.Height;
			byte[] coverage = new byte[width * height];
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor pixel = tip.GetPixel(x, y);
					int r = pixel.Red;
					int g = pixel.Green;
					int b = pixel.Blue;
					int a = pixel.Alpha;
					int luminance = ((r * 77) + (g * 150) + (b * 29)) / 256;
					int value = ((255 - luminance) * a) / 255;
					coverage[(y * width) + x] = (byte)value;
				}
			}
			m_customTipCoverage = coverage;
			m_customTipWidth = width;
			m_customTipHeight = height;
		}

		public void SetCloneOffset(int offsetX, int offsetY)
		{
			m_cloneOffsetX = offsetX;
			m_cloneOffsetY = offsetY;
		}

		public void SetTipShape(int roundness, int angleDegrees)
		{
			double aspect = roundness / 100.0;
			if (aspect < 0.05)
			{
				aspect = 0.05;
			}
			if (aspect > 1.0)
			{
				aspect = 1.0;
			}
			m_tipAspect = aspect;
			if (aspect >= 1.0 && angleDegrees == 0)
			{
				m_tipCos = 1.0;
				m_tipSin = 0.0;
				return;
			}
			double radians = angleDegrees * System.Math.PI / 180.0;
			m_tipCos = System.Math.Cos(radians);
			m_tipSin = System.Math.Sin(radians);
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
			SKBitmap bitmap = layer.PaintTarget();
			m_highDepth = bitmap.ColorType != SKColorType.Rgba8888;
			m_width = bitmap.Width;
			m_height = bitmap.Height;
			int coverageLength = m_width * m_height;
			if (s_coveragePool == null || s_coveragePool.Length != coverageLength || s_coveragePoolWidth != m_width)
			{
				s_coveragePool = new byte[coverageLength];
				s_ceilingPool = new byte[coverageLength];
				s_coveragePoolWidth = m_width;
				s_coverageDirtyValid = false;
			}
			else if (s_coverageDirtyValid)
			{
				for (int y = s_coverageDirtyTop; y < s_coverageDirtyBottom; y++)
				{
					System.Array.Clear(s_coveragePool, (y * m_width) + s_coverageDirtyLeft, s_coverageDirtyRight - s_coverageDirtyLeft);
					System.Array.Clear(s_ceilingPool, (y * m_width) + s_coverageDirtyLeft, s_coverageDirtyRight - s_coverageDirtyLeft);
				}
				s_coverageDirtyValid = false;
			}
			m_coverage = s_coveragePool;
			m_ceiling = s_ceilingPool;
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
			m_radiusBase = radius;
			m_currentPressure = 1.0;
			m_hardness = hardness;
			m_tipOuter = radius;
			double tipInner = hardness * m_tipOuter;
			double tipAntialias = m_tipOuter * 0.08;
			if (tipAntialias > 1.0)
			{
				tipAntialias = 1.0;
			}
			if (tipAntialias < 0.4)
			{
				tipAntialias = 0.4;
			}
			if (m_tipOuter - tipInner < tipAntialias)
			{
				tipInner = m_tipOuter - tipAntialias;
			}
			if (tipInner < 0.0)
			{
				tipInner = 0.0;
			}
			m_tipInner = tipInner;
			m_opacity = opacity;
			m_flow = flow;
			m_strength = 1.0;
			m_square = square;
			m_tipAspect = 1.0;
			m_tipCos = 1.0;
			m_tipSin = 0.0;
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
			m_customTipCoverage = null;
			m_customTipWidth = 0;
			m_customTipHeight = 0;
			m_fadeLengthPx = 0.0;
			m_fadeTraveled = 0.0;
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
			m_dabQueueCount = 0;
			m_active = true;
		}

		public void SetPressure(float pressure, bool sizeEnabled, bool opacityEnabled, double sizeMinimum, double opacityMinimum)
		{
			m_pressureSizeEnabled = sizeEnabled;
			m_pressureOpacityEnabled = opacityEnabled;
			m_pressureOpacityMinimum = opacityMinimum;
			double clampedPressure = pressure;
			if (clampedPressure < 0.0)
			{
				clampedPressure = 0.0;
			}
			if (clampedPressure > 1.0)
			{
				clampedPressure = 1.0;
			}
			m_currentPressure = clampedPressure;
			if (m_pressureSizeEnabled)
			{
				double sizeFactor = sizeMinimum + (1.0 - sizeMinimum) * m_currentPressure;
				m_radius = (int)Math.Round(m_radiusBase * sizeFactor);
				if (m_radius < 1)
				{
					m_radius = 1;
				}
			}
			else
			{
				m_radius = m_radiusBase;
			}
			m_tipOuter = m_radius;
			double tipInner = m_hardness * m_tipOuter;
			double tipAntialias = m_tipOuter * 0.08;
			if (tipAntialias > 1.0)
			{
				tipAntialias = 1.0;
			}
			if (tipAntialias < 0.4)
			{
				tipAntialias = 0.4;
			}
			if (m_tipOuter - tipInner < tipAntialias)
			{
				tipInner = m_tipOuter - tipAntialias;
			}
			if (tipInner < 0.0)
			{
				tipInner = 0.0;
			}
			m_tipInner = tipInner;
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
			m_ceiling = null;
			m_dabQueueCount = 0;
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
			double outer = m_tipOuter;
			double localX = (offsetX * m_tipCos) + (offsetY * m_tipSin);
			double localY = (offsetY * m_tipCos) - (offsetX * m_tipSin);
			localY = localY / m_tipAspect;
			if (m_customTipCoverage != null)
			{
				double half = m_tipOuter;
				if (half <= 0.0)
				{
					return 0.0;
				}
				double u = ((localX + half) / (2.0 * half)) * m_customTipWidth;
				double v = ((localY + half) / (2.0 * half)) * m_customTipHeight;
				int tipX = (int)u;
				int tipY = (int)v;
				if (tipX < 0 || tipY < 0 || tipX >= m_customTipWidth || tipY >= m_customTipHeight)
				{
					return 0.0;
				}
				return m_customTipCoverage[(tipY * m_customTipWidth) + tipX] / 255.0;
			}
			double distance;
			if (m_square)
			{
				double absX = System.Math.Abs(localX);
				double absY = System.Math.Abs(localY);
				distance = absX;
				if (absY > absX)
				{
					distance = absY;
				}
			}
			else
			{
				distance = System.Math.Sqrt((localX * localX) + (localY * localY));
			}
			double inner = m_tipInner;
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

		private double EffectiveSourceNormalized(double originalChannel, double sourceChannel, double originalAlpha)
		{
			double blended = BlendChannel(originalChannel, sourceChannel);
			double effective = ((1.0 - originalAlpha) * sourceChannel) + (originalAlpha * blended);
			return effective;
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

		public void StampDab(Layer layer, double centerX, double centerY, Selection selection)
		{
			if (!m_active)
			{
				return;
			}
			SKBitmap bitmap = layer.PaintTarget();
			if (bitmap.Width != m_width || bitmap.Height != m_height)
			{
				return;
			}
			if (m_op == eBrushOp.Smudge)
			{
				StampSmudgeDab(layer, centerX, centerY, selection);
				return;
			}
			if (m_op == eBrushOp.ColorReplace && !m_colorReplaceHasSample)
			{
				CaptureColorReplaceSample(layer, centerX, centerY);
			}
			StampDabCore(layer, centerX, centerY, selection, int.MinValue, int.MaxValue);
		}

		private unsafe void CaptureColorReplaceSample(Layer layer, double centerX, double centerY)
		{
			if (m_highDepth)
			{
				return;
			}
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			int originalRowBytes = m_original.RowBytes;
			byte* originalPixels = (byte*)m_original.GetPixels().ToPointer();
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

		private unsafe void StampDabCore(Layer layer, double centerX, double centerY, Selection selection, int bandTop, int bandBottom)
		{
			SKBitmap bitmap = layer.PaintTarget();
			int rowBytes = bitmap.RowBytes;
			int originalRowBytes = m_original.RowBytes;
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			byte* pixels = (byte*)bitmap.GetPixels().ToPointer();
			byte* originalPixels = (byte*)m_original.GetPixels().ToPointer();
			PixelAccessor destinationAccessor = new PixelAccessor(bitmap.GetPixels(), rowBytes, bitmap.ColorType);
			PixelAccessor originalAccessor = new PixelAccessor(m_original.GetPixels(), originalRowBytes, m_original.ColorType);
			double brushRedNormalized = m_red / 255.0;
			double brushGreenNormalized = m_green / 255.0;
			double brushBlueNormalized = m_blue / 255.0;
			bool clip = selection != null && selection.IsActive();
			int radius = m_radius;
			int minCanvasX = (int)System.Math.Floor(centerX) - radius - 1;
			int maxCanvasX = (int)System.Math.Ceiling(centerX) + radius + 1;
			int minCanvasY = (int)System.Math.Floor(centerY) - radius - 1;
			int maxCanvasY = (int)System.Math.Ceiling(centerY) + radius + 1;
			byte[] selectionMask = null;
			int selectionOriginX = 0;
			int selectionOriginY = 0;
			int selectionStride = 0;
			if (clip)
			{
				selectionMask = selection.Mask();
				selectionOriginX = selection.MaskOriginX();
				selectionOriginY = selection.MaskOriginY();
				selectionStride = selection.MaskWidth();
				SKRectI selectionBounds = selection.Bounds();
				if (minCanvasX < selectionBounds.Left)
				{
					minCanvasX = selectionBounds.Left;
				}
				if (maxCanvasX > selectionBounds.Right - 1)
				{
					maxCanvasX = selectionBounds.Right - 1;
				}
				if (minCanvasY < selectionBounds.Top)
				{
					minCanvasY = selectionBounds.Top;
				}
				if (maxCanvasY > selectionBounds.Bottom - 1)
				{
					maxCanvasY = selectionBounds.Bottom - 1;
				}
			}
			if (minCanvasY < bandTop)
			{
				minCanvasY = bandTop;
			}
			if (bandBottom != int.MaxValue && maxCanvasY > bandBottom - 1)
			{
				maxCanvasY = bandBottom - 1;
			}
			for (int canvasY = minCanvasY; canvasY <= maxCanvasY; canvasY++)
			{
				int bitmapY = canvasY - layerOffsetY;
				if (bitmapY < 0 || bitmapY >= m_height)
				{
					continue;
				}
				double offsetY = canvasY - centerY;
				int selectionRow = ((canvasY - selectionOriginY) * selectionStride) - selectionOriginX;
				for (int canvasX = minCanvasX; canvasX <= maxCanvasX; canvasX++)
				{
					double tip = TipCoverage(canvasX - centerX, offsetY);
					if (tip <= 0.0)
					{
						continue;
					}
					double selectionFactor = 1.0;
					if (clip)
					{
						int selectionCoverage = selectionMask[selectionRow + canvasX];
						if (selectionCoverage == 0)
						{
							continue;
						}
						selectionFactor = selectionCoverage / 255.0;
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
					double opacityCeiling = m_opacity;
					if (m_pressureOpacityEnabled)
					{
						double opacityFactor = m_pressureOpacityMinimum + (1.0 - m_pressureOpacityMinimum) * m_currentPressure;
						opacityCeiling = m_opacity * opacityFactor;
					}
					if (m_fadeLengthPx > 0.0)
					{
						double fadeFactor = 1.0 - (m_fadeTraveled / m_fadeLengthPx);
						if (fadeFactor < 0.0)
						{
							fadeFactor = 0.0;
						}
						if (fadeFactor > 1.0)
						{
							fadeFactor = 1.0;
						}
						opacityCeiling = opacityCeiling * fadeFactor;
					}
					double storedCeiling = m_ceiling[coverageIndex] / 255.0;
					if (opacityCeiling < storedCeiling)
					{
						opacityCeiling = storedCeiling;
					}
					else
					{
						m_ceiling[coverageIndex] = (byte)((opacityCeiling * 255.0) + 0.5);
					}
					if (finalAlpha > opacityCeiling)
					{
						finalAlpha = opacityCeiling;
					}
					finalAlpha = finalAlpha * selectionFactor;
					if (m_highDepth && (m_op == eBrushOp.Paint || m_op == eBrushOp.Erase))
					{
						float highOriginalRed;
						float highOriginalGreen;
						float highOriginalBlue;
						float highOriginalAlpha;
						originalAccessor.ReadNormalized(bitmapX, bitmapY, out highOriginalRed, out highOriginalGreen, out highOriginalBlue, out highOriginalAlpha);
						if (m_lockAlpha && highOriginalAlpha <= 0.0f)
						{
							continue;
						}
						if (m_op == eBrushOp.Erase)
						{
							if (m_lockAlpha)
							{
								continue;
							}
							double highErasedAlpha = highOriginalAlpha * (1.0 - finalAlpha);
							destinationAccessor.WriteNormalized(bitmapX, bitmapY, highOriginalRed, highOriginalGreen, highOriginalBlue, (float)highErasedAlpha);
							continue;
						}
						double highSourceRed = brushRedNormalized;
						double highSourceGreen = brushGreenNormalized;
						double highSourceBlue = brushBlueNormalized;
						if (m_mode != eBlendMode.Normal)
						{
							highSourceRed = EffectiveSourceNormalized(highOriginalRed, brushRedNormalized, highOriginalAlpha);
							highSourceGreen = EffectiveSourceNormalized(highOriginalGreen, brushGreenNormalized, highOriginalAlpha);
							highSourceBlue = EffectiveSourceNormalized(highOriginalBlue, brushBlueNormalized, highOriginalAlpha);
						}
						double highInverse = 1.0 - finalAlpha;
						double highOutAlpha = finalAlpha + (highOriginalAlpha * highInverse);
						if (highOutAlpha <= 0.0)
						{
							destinationAccessor.WriteNormalized(bitmapX, bitmapY, 0.0f, 0.0f, 0.0f, 0.0f);
							continue;
						}
						double highWeightedOriginal = highOriginalAlpha * highInverse;
						double highOutRed = ((highSourceRed * finalAlpha) + (highOriginalRed * highWeightedOriginal)) / highOutAlpha;
						double highOutGreen = ((highSourceGreen * finalAlpha) + (highOriginalGreen * highWeightedOriginal)) / highOutAlpha;
						double highOutBlue = ((highSourceBlue * finalAlpha) + (highOriginalBlue * highWeightedOriginal)) / highOutAlpha;
						destinationAccessor.WriteNormalized(bitmapX, bitmapY, (float)highOutRed, (float)highOutGreen, (float)highOutBlue, (float)highOutAlpha);
						continue;
					}
					if (m_highDepth)
					{
						continue;
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
			if (m_highDepth)
			{
				return;
			}
			SKBitmap bitmap = layer.PaintTarget();
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
			byte[] selectionMask = null;
			int selectionOriginX = 0;
			int selectionOriginY = 0;
			int selectionStride = 0;
			if (clip)
			{
				selectionMask = selection.Mask();
				selectionOriginX = selection.MaskOriginX();
				selectionOriginY = selection.MaskOriginY();
				selectionStride = selection.MaskWidth();
				SKRectI selectionBounds = selection.Bounds();
				if (minCanvasX < selectionBounds.Left)
				{
					minCanvasX = selectionBounds.Left;
				}
				if (maxCanvasX > selectionBounds.Right - 1)
				{
					maxCanvasX = selectionBounds.Right - 1;
				}
				if (minCanvasY < selectionBounds.Top)
				{
					minCanvasY = selectionBounds.Top;
				}
				if (maxCanvasY > selectionBounds.Bottom - 1)
				{
					maxCanvasY = selectionBounds.Bottom - 1;
				}
			}

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
				int selectionRow = ((canvasY - selectionOriginY) * selectionStride) - selectionOriginX;
				for (int canvasX = minCanvasX; canvasX <= maxCanvasX; canvasX++)
				{
					double tip = TipCoverage(canvasX - centerX, offsetY);
					if (tip <= 0.0)
					{
						continue;
					}
					if (clip)
					{
						int selectionCoverage = selectionMask[selectionRow + canvasX];
						if (selectionCoverage == 0)
						{
							continue;
						}
						tip = tip * (selectionCoverage / 255.0);
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
				int selectionRow = ((canvasY - selectionOriginY) * selectionStride) - selectionOriginX;
				for (int canvasX = minCanvasX; canvasX <= maxCanvasX; canvasX++)
				{
					double tip = TipCoverage(canvasX - centerX, offsetY);
					if (tip <= 0.0)
					{
						continue;
					}
					if (clip)
					{
						int selectionCoverage = selectionMask[selectionRow + canvasX];
						if (selectionCoverage == 0)
						{
							continue;
						}
						tip = tip * (selectionCoverage / 255.0);
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

		private void QueueDab(double x, double y)
		{
			if (m_dabQueueX == null)
			{
				m_dabQueueX = new double[64];
				m_dabQueueY = new double[64];
			}
			if (m_dabQueueCount == m_dabQueueX.Length)
			{
				double[] grownX = new double[m_dabQueueX.Length * 2];
				double[] grownY = new double[m_dabQueueY.Length * 2];
				System.Array.Copy(m_dabQueueX, grownX, m_dabQueueCount);
				System.Array.Copy(m_dabQueueY, grownY, m_dabQueueCount);
				m_dabQueueX = grownX;
				m_dabQueueY = grownY;
			}
			m_dabQueueX[m_dabQueueCount] = x;
			m_dabQueueY[m_dabQueueCount] = y;
			m_dabQueueCount = m_dabQueueCount + 1;
		}

		private void StampQueuedDabRows(Layer layer, Selection selection, int bandTop, int bandBottom)
		{
			for (int index = 0; index < m_dabQueueCount; index++)
			{
				StampDabCore(layer, m_dabQueueX[index], m_dabQueueY[index], selection, bandTop, bandBottom);
			}
		}

		private void FlushQueuedDabs(Layer layer, Selection selection)
		{
			if (m_dabQueueCount == 0)
			{
				return;
			}
			if (!m_active)
			{
				m_dabQueueCount = 0;
				return;
			}
			SKBitmap bitmap = layer.PaintTarget();
			if (bitmap.Width != m_width || bitmap.Height != m_height)
			{
				m_dabQueueCount = 0;
				return;
			}
			if (m_op == eBrushOp.Smudge)
			{
				for (int index = 0; index < m_dabQueueCount; index++)
				{
					StampSmudgeDab(layer, m_dabQueueX[index], m_dabQueueY[index], selection);
				}
				m_dabQueueCount = 0;
				return;
			}
			if (m_op == eBrushOp.ColorReplace)
			{
				for (int index = 0; index < m_dabQueueCount; index++)
				{
					if (m_colorReplaceHasSample)
					{
						break;
					}
					CaptureColorReplaceSample(layer, m_dabQueueX[index], m_dabQueueY[index]);
				}
			}
			double minCenterX = m_dabQueueX[0];
			double maxCenterX = m_dabQueueX[0];
			double minCenterY = m_dabQueueY[0];
			double maxCenterY = m_dabQueueY[0];
			for (int index = 1; index < m_dabQueueCount; index++)
			{
				if (m_dabQueueX[index] < minCenterX)
				{
					minCenterX = m_dabQueueX[index];
				}
				if (m_dabQueueX[index] > maxCenterX)
				{
					maxCenterX = m_dabQueueX[index];
				}
				if (m_dabQueueY[index] < minCenterY)
				{
					minCenterY = m_dabQueueY[index];
				}
				if (m_dabQueueY[index] > maxCenterY)
				{
					maxCenterY = m_dabQueueY[index];
				}
			}
			UnionCoverageDirty(layer, minCenterX, minCenterY, maxCenterX, maxCenterY);
			int diameter = (m_radius * 2) + 3;
			long work = (long)diameter * diameter * m_dabQueueCount;
			long threshold = ParallelWorkThreshold;
			if (m_op == eBrushOp.Blur || m_op == eBrushOp.Sharpen || m_op == eBrushOp.Heal)
			{
				threshold = ParallelBoxAverageWorkThreshold;
			}
			if (work < threshold)
			{
				for (int index = 0; index < m_dabQueueCount; index++)
				{
					StampDabCore(layer, m_dabQueueX[index], m_dabQueueY[index], selection, int.MinValue, int.MaxValue);
				}
				m_dabQueueCount = 0;
				return;
			}
			int rowFirst = (int)System.Math.Floor(minCenterY) - m_radius - 1;
			int rowLast = (int)System.Math.Ceiling(maxCenterY) + m_radius + 1;
			DabBandWorker worker = new DabBandWorker();
			worker.m_engine = this;
			worker.m_layer = layer;
			worker.m_selection = selection;
			RowBands.Run(rowFirst, rowLast + 1, worker.Band);
			m_dabQueueCount = 0;
		}

		private void UnionCoverageDirty(Layer layer, double minCenterX, double minCenterY, double maxCenterX, double maxCenterY)
		{
			int left = (int)System.Math.Floor(minCenterX) - m_radius - 1 - layer.OffsetX();
			int top = (int)System.Math.Floor(minCenterY) - m_radius - 1 - layer.OffsetY();
			int right = (int)System.Math.Ceiling(maxCenterX) + m_radius + 2 - layer.OffsetX();
			int bottom = (int)System.Math.Ceiling(maxCenterY) + m_radius + 2 - layer.OffsetY();
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > m_width)
			{
				right = m_width;
			}
			if (bottom > m_height)
			{
				bottom = m_height;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}
			if (!s_coverageDirtyValid)
			{
				s_coverageDirtyLeft = left;
				s_coverageDirtyTop = top;
				s_coverageDirtyRight = right;
				s_coverageDirtyBottom = bottom;
				s_coverageDirtyValid = true;
				return;
			}
			if (left < s_coverageDirtyLeft)
			{
				s_coverageDirtyLeft = left;
			}
			if (top < s_coverageDirtyTop)
			{
				s_coverageDirtyTop = top;
			}
			if (right > s_coverageDirtyRight)
			{
				s_coverageDirtyRight = right;
			}
			if (bottom > s_coverageDirtyBottom)
			{
				s_coverageDirtyBottom = bottom;
			}
		}

		public void AirbrushStamp(Document document, Layer layer, double x, double y, Selection selection)
		{
			QueueDab(x, y);
			FlushQueuedDabs(layer, selection);
			MarkDirty(document, x, y, x, y);
		}

		public void StampFirst(Document document, Layer layer, double x, double y, Selection selection)
		{
			QueueDab(x, y);
			FlushQueuedDabs(layer, selection);
			m_penX = x;
			m_penY = y;
			m_inputX = x;
			m_inputY = y;
			m_hasPen = true;
			m_distanceSinceStamp = 0.0;
			m_fadeTraveled = 0.0;
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
			m_fadeTraveled = m_fadeTraveled + segmentLength;
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
				QueueDab(stampX, stampY);
				m_distanceSinceStamp = 0.0;
			}
			FlushQueuedDabs(layer, selection);
			m_distanceSinceStamp = m_distanceSinceStamp + (segmentLength - traveled);
			m_penX = x;
			m_penY = y;
			MarkDirty(document, startPenX, startPenY, m_penX, m_penY);
		}
	}
}
