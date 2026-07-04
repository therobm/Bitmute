using System;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.Tools
{
	public class FreeTransformTool : Tool
	{
		private const int ModeFree = 0;
		private const int ModeScale = 1;
		private const int ModeRotate = 2;
		private const int ModeSkew = 3;
		private const int ModeDistort = 4;
		private const int ModePerspective = 5;
		private const int ModeFlipHorizontal = 6;
		private const int ModeFlipVertical = 7;
		private const int ModeRotate90CW = 8;
		private const int ModeRotate180 = 9;
		private const int ModeRotate90CCW = 10;

		private const int GrabNone = 0;
		private const int GrabCorner = 1;
		private const int GrabEdge = 2;
		private const int GrabRotate = 3;
		private const int GrabMove = 4;

		private const double DoubleClickMilliseconds = 350.0;
		private const int DoubleClickDistance = 4;
		private const double RotateSnapRadians = Math.PI / 12.0;

		private bool m_armed;
		private int m_mode;
		private int m_layerIndex;
		private Document m_document;
		private SKBitmap m_sourceBitmap;
		private SKBitmap m_originalLayerBitmap;
		private int m_sourceOffsetX;
		private int m_sourceOffsetY;
		private int m_oldOffsetX;
		private int m_oldOffsetY;
		private bool m_hasSelection;
		private bool m_compositeMode;
		private bool m_isBackground;
		private byte[] m_savedMask;
		private SKRectI m_savedBounds;
		private int m_pickRadius;

		private double[] m_cornerX;
		private double[] m_cornerY;
		private double[] m_pressQuadX;
		private double[] m_pressQuadY;

		private int m_grab;
		private int m_grabIndex;
		private int m_pressX;
		private int m_pressY;

		private DateTime m_lastPressTime;
		private int m_lastPressX;
		private int m_lastPressY;
		private bool m_hasLastPress;

		public FreeTransformTool()
		{
			m_armed = false;
			m_mode = ModeFree;
			m_layerIndex = -1;
			m_document = null;
			m_sourceBitmap = null;
			m_originalLayerBitmap = null;
			m_sourceOffsetX = 0;
			m_sourceOffsetY = 0;
			m_oldOffsetX = 0;
			m_oldOffsetY = 0;
			m_hasSelection = false;
			m_compositeMode = false;
			m_isBackground = false;
			m_savedMask = null;
			m_pickRadius = 6;
			m_cornerX = new double[4];
			m_cornerY = new double[4];
			m_pressQuadX = new double[4];
			m_pressQuadY = new double[4];
			m_grab = GrabNone;
			m_grabIndex = -1;
			m_hasLastPress = false;
		}

		private static double Dot(double ax, double ay, double bx, double by)
		{
			return (ax * bx) + (ay * by);
		}

		private static double Length(double dx, double dy)
		{
			return Math.Sqrt((dx * dx) + (dy * dy));
		}

		private static bool SolveTwoByTwo(double a11, double a12, double a21, double a22, double b1, double b2, out double s1, out double s2)
		{
			s1 = 0.0;
			s2 = 0.0;
			double determinant = (a11 * a22) - (a12 * a21);
			if (Math.Abs(determinant) < 0.000000001)
			{
				return false;
			}
			double inverse = 1.0 / determinant;
			s1 = ((b1 * a22) - (a12 * b2)) * inverse;
			s2 = ((a11 * b2) - (b1 * a21)) * inverse;
			return true;
		}

		private static bool PointInQuad(double px, double py, double[] cornerX, double[] cornerY)
		{
			bool inside = false;
			int previous = 3;
			for (int index = 0; index < 4; index++)
			{
				double xi = cornerX[index];
				double yi = cornerY[index];
				double xj = cornerX[previous];
				double yj = cornerY[previous];
				bool crossesY = (yi > py) != (yj > py);
				if (crossesY)
				{
					double intersectX = ((xj - xi) * (py - yi) / (yj - yi)) + xi;
					if (px < intersectX)
					{
						inside = !inside;
					}
				}
				previous = index;
			}
			return inside;
		}

		private void SetIdentityQuad(int offsetX, int offsetY, int width, int height)
		{
			m_cornerX[0] = offsetX;
			m_cornerY[0] = offsetY;
			m_cornerX[1] = offsetX + width;
			m_cornerY[1] = offsetY;
			m_cornerX[2] = offsetX + width;
			m_cornerY[2] = offsetY + height;
			m_cornerX[3] = offsetX;
			m_cornerY[3] = offsetY + height;
		}

		private void SwapCorners(int a, int b)
		{
			double tempX = m_cornerX[a];
			double tempY = m_cornerY[a];
			m_cornerX[a] = m_cornerX[b];
			m_cornerY[a] = m_cornerY[b];
			m_cornerX[b] = tempX;
			m_cornerY[b] = tempY;
		}

		private void ApplyInstantQuad(int mode)
		{
			double tl0X = m_cornerX[0];
			double tl0Y = m_cornerY[0];
			double tr0X = m_cornerX[1];
			double tr0Y = m_cornerY[1];
			double br0X = m_cornerX[2];
			double br0Y = m_cornerY[2];
			double bl0X = m_cornerX[3];
			double bl0Y = m_cornerY[3];
			if (mode == ModeFlipHorizontal)
			{
				SwapCorners(0, 1);
				SwapCorners(3, 2);
				return;
			}
			if (mode == ModeFlipVertical)
			{
				SwapCorners(0, 3);
				SwapCorners(1, 2);
				return;
			}
			if (mode == ModeRotate90CW)
			{
				m_cornerX[0] = bl0X;
				m_cornerY[0] = bl0Y;
				m_cornerX[1] = tl0X;
				m_cornerY[1] = tl0Y;
				m_cornerX[2] = tr0X;
				m_cornerY[2] = tr0Y;
				m_cornerX[3] = br0X;
				m_cornerY[3] = br0Y;
				return;
			}
			if (mode == ModeRotate180)
			{
				SwapCorners(0, 2);
				SwapCorners(1, 3);
				return;
			}
			if (mode == ModeRotate90CCW)
			{
				m_cornerX[0] = tr0X;
				m_cornerY[0] = tr0Y;
				m_cornerX[1] = br0X;
				m_cornerY[1] = br0Y;
				m_cornerX[2] = bl0X;
				m_cornerY[2] = bl0Y;
				m_cornerX[3] = tl0X;
				m_cornerY[3] = tl0Y;
				return;
			}
		}

		private void SnapshotPressQuad()
		{
			for (int index = 0; index < 4; index++)
			{
				m_pressQuadX[index] = m_cornerX[index];
				m_pressQuadY[index] = m_cornerY[index];
			}
		}

		private bool HitCorner(int x, int y, out int index)
		{
			index = -1;
			double tolerance = m_pickRadius;
			for (int corner = 0; corner < 4; corner++)
			{
				double deltaX = x - m_cornerX[corner];
				double deltaY = y - m_cornerY[corner];
				if (Length(deltaX, deltaY) <= tolerance)
				{
					index = corner;
					return true;
				}
			}
			return false;
		}

		private bool HitRotateRing(int x, int y, out int index)
		{
			index = -1;
			double inner = m_pickRadius;
			double outer = m_pickRadius * 3.0;
			for (int corner = 0; corner < 4; corner++)
			{
				double deltaX = x - m_cornerX[corner];
				double deltaY = y - m_cornerY[corner];
				double distance = Length(deltaX, deltaY);
				if (distance > inner && distance <= outer)
				{
					index = corner;
					return true;
				}
			}
			return false;
		}

		private bool HitEdge(int x, int y, out int index)
		{
			index = -1;
			double tolerance = m_pickRadius;
			for (int edge = 0; edge < 4; edge++)
			{
				int c0 = edge;
				int c1 = (edge + 1) % 4;
				double midX = (m_cornerX[c0] + m_cornerX[c1]) * 0.5;
				double midY = (m_cornerY[c0] + m_cornerY[c1]) * 0.5;
				double deltaX = x - midX;
				double deltaY = y - midY;
				if (Length(deltaX, deltaY) <= tolerance)
				{
					index = edge;
					return true;
				}
			}
			return false;
		}

		private void ApplyMove(double deltaX, double deltaY)
		{
			for (int index = 0; index < 4; index++)
			{
				m_cornerX[index] = m_pressQuadX[index] + deltaX;
				m_cornerY[index] = m_pressQuadY[index] + deltaY;
			}
		}

		private void ApplyCornerScale(int corner, double targetX, double targetY, bool aspect)
		{
			int anchor = (corner + 2) % 4;
			int neighbor1 = (corner + 1) % 4;
			int neighbor3 = (corner + 3) % 4;
			double anchorX = m_pressQuadX[anchor];
			double anchorY = m_pressQuadY[anchor];
			double a1x = m_pressQuadX[neighbor1] - anchorX;
			double a1y = m_pressQuadY[neighbor1] - anchorY;
			double a2x = m_pressQuadX[neighbor3] - anchorX;
			double a2y = m_pressQuadY[neighbor3] - anchorY;
			double rhsX = targetX - anchorX;
			double rhsY = targetY - anchorY;
			double s1;
			double s2;
			if (!SolveTwoByTwo(a1x, a2x, a1y, a2y, rhsX, rhsY, out s1, out s2))
			{
				return;
			}
			if (aspect)
			{
				double uniform = (s1 + s2) * 0.5;
				s1 = uniform;
				s2 = uniform;
			}
			m_cornerX[neighbor1] = anchorX + (s1 * a1x);
			m_cornerY[neighbor1] = anchorY + (s1 * a1y);
			m_cornerX[neighbor3] = anchorX + (s2 * a2x);
			m_cornerY[neighbor3] = anchorY + (s2 * a2y);
			m_cornerX[corner] = anchorX + (s1 * a1x) + (s2 * a2x);
			m_cornerY[corner] = anchorY + (s1 * a1y) + (s2 * a2y);
			m_cornerX[anchor] = anchorX;
			m_cornerY[anchor] = anchorY;
		}

		private void ApplyCornerScaleCentered(int corner, double targetX, double targetY, bool aspect)
		{
			double centerX = 0.0;
			double centerY = 0.0;
			for (int index = 0; index < 4; index++)
			{
				centerX += m_pressQuadX[index];
				centerY += m_pressQuadY[index];
			}
			centerX = centerX * 0.25;
			centerY = centerY * 0.25;
			double halfUx = (m_pressQuadX[1] - m_pressQuadX[0]) * 0.5;
			double halfUy = (m_pressQuadY[1] - m_pressQuadY[0]) * 0.5;
			double halfVx = (m_pressQuadX[3] - m_pressQuadX[0]) * 0.5;
			double halfVy = (m_pressQuadY[3] - m_pressQuadY[0]) * 0.5;
			double signU = -1.0;
			double signV = -1.0;
			if (corner == 1)
			{
				signU = 1.0;
				signV = -1.0;
			}
			else if (corner == 2)
			{
				signU = 1.0;
				signV = 1.0;
			}
			else if (corner == 3)
			{
				signU = -1.0;
				signV = 1.0;
			}
			double columnUx = signU * halfUx;
			double columnUy = signU * halfUy;
			double columnVx = signV * halfVx;
			double columnVy = signV * halfVy;
			double a;
			double b;
			if (!SolveTwoByTwo(columnUx, columnVx, columnUy, columnVy, targetX - centerX, targetY - centerY, out a, out b))
			{
				return;
			}
			if (aspect)
			{
				double uniform = (a + b) * 0.5;
				a = uniform;
				b = uniform;
			}
			double newHalfUx = a * halfUx;
			double newHalfUy = a * halfUy;
			double newHalfVx = b * halfVx;
			double newHalfVy = b * halfVy;
			m_cornerX[0] = centerX - newHalfUx - newHalfVx;
			m_cornerY[0] = centerY - newHalfUy - newHalfVy;
			m_cornerX[1] = centerX + newHalfUx - newHalfVx;
			m_cornerY[1] = centerY + newHalfUy - newHalfVy;
			m_cornerX[2] = centerX + newHalfUx + newHalfVx;
			m_cornerY[2] = centerY + newHalfUy + newHalfVy;
			m_cornerX[3] = centerX - newHalfUx + newHalfVx;
			m_cornerY[3] = centerY - newHalfUy + newHalfVy;
		}

		private void ApplyEdgeScale(int edge, double deltaX, double deltaY)
		{
			int c0 = edge;
			int c1 = (edge + 1) % 4;
			int opposite = (edge + 2) % 4;
			int oppositeC0 = opposite;
			int oppositeC1 = (opposite + 1) % 4;
			double edgeMidX = (m_pressQuadX[c0] + m_pressQuadX[c1]) * 0.5;
			double edgeMidY = (m_pressQuadY[c0] + m_pressQuadY[c1]) * 0.5;
			double oppositeMidX = (m_pressQuadX[oppositeC0] + m_pressQuadX[oppositeC1]) * 0.5;
			double oppositeMidY = (m_pressQuadY[oppositeC0] + m_pressQuadY[oppositeC1]) * 0.5;
			double vx = oppositeMidX - edgeMidX;
			double vy = oppositeMidY - edgeMidY;
			double length = Length(vx, vy);
			if (length < 0.000000001)
			{
				return;
			}
			double nx = vx / length;
			double ny = vy / length;
			double projection = Dot(deltaX, deltaY, nx, ny);
			double moveX = projection * nx;
			double moveY = projection * ny;
			m_cornerX[c0] = m_pressQuadX[c0] + moveX;
			m_cornerY[c0] = m_pressQuadY[c0] + moveY;
			m_cornerX[c1] = m_pressQuadX[c1] + moveX;
			m_cornerY[c1] = m_pressQuadY[c1] + moveY;
		}

		private void ApplyRotate(double curX, double curY, bool snap)
		{
			double centerX = 0.0;
			double centerY = 0.0;
			for (int index = 0; index < 4; index++)
			{
				centerX += m_pressQuadX[index];
				centerY += m_pressQuadY[index];
			}
			centerX = centerX * 0.25;
			centerY = centerY * 0.25;
			double angleCurrent = Math.Atan2(curY - centerY, curX - centerX);
			double anglePress = Math.Atan2(m_pressY - centerY, m_pressX - centerX);
			double angle = angleCurrent - anglePress;
			if (snap)
			{
				angle = Math.Round(angle / RotateSnapRadians) * RotateSnapRadians;
			}
			double cosAngle = Math.Cos(angle);
			double sinAngle = Math.Sin(angle);
			for (int index = 0; index < 4; index++)
			{
				double relativeX = m_pressQuadX[index] - centerX;
				double relativeY = m_pressQuadY[index] - centerY;
				m_cornerX[index] = centerX + (relativeX * cosAngle) - (relativeY * sinAngle);
				m_cornerY[index] = centerY + (relativeX * sinAngle) + (relativeY * cosAngle);
			}
		}

		private void ApplySkew(int edge, double deltaX, double deltaY)
		{
			int c0 = edge;
			int c1 = (edge + 1) % 4;
			double ux = m_pressQuadX[c1] - m_pressQuadX[c0];
			double uy = m_pressQuadY[c1] - m_pressQuadY[c0];
			double length = Length(ux, uy);
			if (length < 0.000000001)
			{
				return;
			}
			double uhatX = ux / length;
			double uhatY = uy / length;
			double projection = Dot(deltaX, deltaY, uhatX, uhatY);
			double moveX = projection * uhatX;
			double moveY = projection * uhatY;
			m_cornerX[c0] = m_pressQuadX[c0] + moveX;
			m_cornerY[c0] = m_pressQuadY[c0] + moveY;
			m_cornerX[c1] = m_pressQuadX[c1] + moveX;
			m_cornerY[c1] = m_pressQuadY[c1] + moveY;
		}

		private void ApplyDistort(int corner, double targetX, double targetY)
		{
			m_cornerX[corner] = targetX;
			m_cornerY[corner] = targetY;
		}

		private void ApplyPerspective(int corner, double deltaX, double deltaY)
		{
			int neighbor;
			if (corner == 0)
			{
				neighbor = 1;
			}
			else if (corner == 1)
			{
				neighbor = 0;
			}
			else if (corner == 2)
			{
				neighbor = 3;
			}
			else
			{
				neighbor = 2;
			}
			double ux = m_pressQuadX[1] - m_pressQuadX[0];
			double uy = m_pressQuadY[1] - m_pressQuadY[0];
			double length = Length(ux, uy);
			double mirrorX = deltaX;
			double mirrorY = deltaY;
			if (length >= 0.000000001)
			{
				double uhatX = ux / length;
				double uhatY = uy / length;
				double projection = Dot(deltaX, deltaY, uhatX, uhatY);
				mirrorX = deltaX - (2.0 * projection * uhatX);
				mirrorY = deltaY - (2.0 * projection * uhatY);
			}
			m_cornerX[corner] = m_pressQuadX[corner] + deltaX;
			m_cornerY[corner] = m_pressQuadY[corner] + deltaY;
			m_cornerX[neighbor] = m_pressQuadX[neighbor] + mirrorX;
			m_cornerY[neighbor] = m_pressQuadY[neighbor] + mirrorY;
		}

		private SKBitmap ExtractSelection(SKBitmap source, int layerOffsetX, int layerOffsetY, Selection selection, SKRectI bounds)
		{
			int width = bounds.Right - bounds.Left;
			int height = bounds.Bottom - bounds.Top;
			SKBitmap piece = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			piece.Erase(SKColors.Transparent);
			for (int canvasY = bounds.Top; canvasY < bounds.Bottom; canvasY++)
			{
				for (int canvasX = bounds.Left; canvasX < bounds.Right; canvasX++)
				{
					if (!selection.IsSelected(canvasX, canvasY))
					{
						continue;
					}
					int bitmapX = canvasX - layerOffsetX;
					int bitmapY = canvasY - layerOffsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= source.Width || bitmapY >= source.Height)
					{
						continue;
					}
					piece.SetPixel(canvasX - bounds.Left, canvasY - bounds.Top, source.GetPixel(bitmapX, bitmapY));
				}
			}
			return piece;
		}

		private SKBitmap CopyWithSelectionCleared(SKBitmap source, int layerOffsetX, int layerOffsetY, Selection selection, SKRectI bounds)
		{
			SKBitmap remainder = source.Copy();
			for (int canvasY = bounds.Top; canvasY < bounds.Bottom; canvasY++)
			{
				for (int canvasX = bounds.Left; canvasX < bounds.Right; canvasX++)
				{
					if (!selection.IsSelected(canvasX, canvasY))
					{
						continue;
					}
					int bitmapX = canvasX - layerOffsetX;
					int bitmapY = canvasY - layerOffsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= remainder.Width || bitmapY >= remainder.Height)
					{
						continue;
					}
					remainder.SetPixel(bitmapX, bitmapY, SKColors.Transparent);
				}
			}
			return remainder;
		}

		private void Disarm()
		{
			m_armed = false;
			m_grab = GrabNone;
			m_grabIndex = -1;
			m_hasLastPress = false;
			m_sourceBitmap = null;
			m_originalLayerBitmap = null;
			m_savedMask = null;
			m_layerIndex = -1;
			m_document = null;
		}

		private void RestoreLifted()
		{
			if (m_document == null)
			{
				return;
			}
			if (m_layerIndex < 0 || m_layerIndex >= m_document.Layers().Count)
			{
				return;
			}
			Layer layer = m_document.Layers()[m_layerIndex];
			if (layer == null || m_originalLayerBitmap == null)
			{
				return;
			}
			layer.SetBitmap(m_originalLayerBitmap);
			layer.SetOffset(m_oldOffsetX, m_oldOffsetY);
			if (m_hasSelection && m_savedMask != null)
			{
				m_document.Selection().SelectMask(m_savedMask, m_savedBounds);
			}
			m_document.MarkComposeDirtyAll();
		}

		private void CommitInternal()
		{
			if (!m_armed || m_document == null)
			{
				return;
			}
			if (m_layerIndex < 0 || m_layerIndex >= m_document.Layers().Count)
			{
				Disarm();
				return;
			}
			Layer layer = m_document.Layers()[m_layerIndex];
			if (layer == null)
			{
				Disarm();
				return;
			}
			SKPoint[] destQuad = new SKPoint[4];
			destQuad[0] = new SKPoint((float)m_cornerX[0], (float)m_cornerY[0]);
			destQuad[1] = new SKPoint((float)m_cornerX[1], (float)m_cornerY[1]);
			destQuad[2] = new SKPoint((float)m_cornerX[2], (float)m_cornerY[2]);
			destQuad[3] = new SKPoint((float)m_cornerX[3], (float)m_cornerY[3]);
			int outX;
			int outY;
			SKBitmap warped = TransformMath.Warp(m_sourceBitmap, destQuad, 2, out outX, out outY);
			if (warped == null)
			{
				RestoreLifted();
				Disarm();
				return;
			}
			if (m_compositeMode)
			{
				BlitCanvasBitmap(m_document, layer, warped, outX, outY);
				warped.Dispose();
				m_document.PushCommand(new MoveLayerCommand(m_layerIndex, m_originalLayerBitmap, m_oldOffsetX, m_oldOffsetY, layer.Bitmap(), layer.OffsetX(), layer.OffsetY()));
			}
			else
			{
				m_document.PushCommand(new MoveLayerCommand(m_layerIndex, m_originalLayerBitmap, m_oldOffsetX, m_oldOffsetY, warped, outX, outY));
				layer.SetBitmap(warped);
				layer.SetOffset(outX, outY);
			}
			m_document.MarkComposeDirtyAll();
			Disarm();
		}

		private bool IsDoubleClick(int x, int y)
		{
			if (!m_hasLastPress)
			{
				return false;
			}
			double elapsed = (DateTime.Now - m_lastPressTime).TotalMilliseconds;
			if (elapsed > DoubleClickMilliseconds)
			{
				return false;
			}
			int deltaX = x - m_lastPressX;
			int deltaY = y - m_lastPressY;
			return ((deltaX * deltaX) + (deltaY * deltaY)) <= (DoubleClickDistance * DoubleClickDistance);
		}

		private void RecordPress(int x, int y)
		{
			m_lastPressTime = DateTime.Now;
			m_lastPressX = x;
			m_lastPressY = y;
			m_hasLastPress = true;
		}

		public override bool IsDestructive()
		{
			return false;
		}

		public bool Begin(Document document, int mode, SKColor fillColor)
		{
			if (m_armed)
			{
				RestoreLifted();
				Disarm();
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			if (layer.IsText())
			{
				return false;
			}
			m_document = document;
			m_mode = mode;
			m_layerIndex = document.ActiveLayerIndex();
			m_oldOffsetX = layer.OffsetX();
			m_oldOffsetY = layer.OffsetY();
			m_isBackground = layer.IsBackground();
			m_originalLayerBitmap = layer.Bitmap();
			m_grab = GrabNone;
			m_grabIndex = -1;
			m_hasLastPress = false;
			bool instant = mode >= ModeFlipHorizontal && mode <= ModeRotate90CCW;
			Selection selection = document.Selection();
			if (selection.IsActive() && !instant)
			{
				m_hasSelection = true;
				m_compositeMode = true;
				SKRectI bounds = selection.Bounds();
				m_savedMask = selection.MaskCopy();
				m_savedBounds = bounds;
				m_sourceBitmap = ExtractSelection(m_originalLayerBitmap, m_oldOffsetX, m_oldOffsetY, selection, bounds);
				m_sourceOffsetX = bounds.Left;
				m_sourceOffsetY = bounds.Top;
				SKBitmap cleared = CopyWithSelectionCleared(m_originalLayerBitmap, m_oldOffsetX, m_oldOffsetY, selection, bounds);
				layer.SetBitmap(cleared);
				selection.Clear();
				SetIdentityQuad(bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
			}
			else
			{
				m_hasSelection = false;
				m_savedMask = null;
				m_sourceBitmap = m_originalLayerBitmap;
				m_sourceOffsetX = m_oldOffsetX;
				m_sourceOffsetY = m_oldOffsetY;
				SetIdentityQuad(m_oldOffsetX, m_oldOffsetY, m_originalLayerBitmap.Width, m_originalLayerBitmap.Height);
				if (instant)
				{
					m_compositeMode = false;
				}
				else if (m_isBackground)
				{
					m_compositeMode = true;
					SKBitmap fillBase = new SKBitmap(document.Width(), document.Height(), SKColorType.Rgba8888, SKAlphaType.Unpremul);
					fillBase.Erase(new SKColor(fillColor.Red, fillColor.Green, fillColor.Blue, 255));
					layer.SetBitmap(fillBase);
					layer.SetOffset(0, 0);
				}
				else
				{
					m_compositeMode = false;
					SKBitmap lifted = new SKBitmap(m_originalLayerBitmap.Width, m_originalLayerBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
					lifted.Erase(SKColors.Transparent);
					layer.SetBitmap(lifted);
				}
			}
			m_armed = true;
			if (instant)
			{
				ApplyInstantQuad(mode);
				CommitInternal();
				return true;
			}
			m_document.MarkComposeDirtyAll();
			return true;
		}

		public bool HasPreview()
		{
			return m_armed;
		}

		public int Mode()
		{
			return m_mode;
		}

		public int LayerIndex()
		{
			return m_layerIndex;
		}

		public SKBitmap PreviewBitmap()
		{
			return m_sourceBitmap;
		}

		public int HitTestKind(int x, int y)
		{
			if (!m_armed)
			{
				return 0;
			}
			int index;
			if (HitCorner(x, y, out index))
			{
				if (index == 0 || index == 2)
				{
					return 1;
				}
				return 2;
			}
			if (HitEdge(x, y, out index))
			{
				if (index == 0 || index == 2)
				{
					return 4;
				}
				return 3;
			}
			if (HitRotateRing(x, y, out index))
			{
				return 5;
			}
			if (PointInQuad(x, y, m_cornerX, m_cornerY))
			{
				return 6;
			}
			return 0;
		}

		public double CornerX(int i)
		{
			if (i < 0 || i >= 4)
			{
				return 0.0;
			}
			return m_cornerX[i];
		}

		public double CornerY(int i)
		{
			if (i < 0 || i >= 4)
			{
				return 0.0;
			}
			return m_cornerY[i];
		}

		public void SetPickRadius(int canvasPx)
		{
			if (canvasPx < 1)
			{
				canvasPx = 1;
			}
			m_pickRadius = canvasPx;
		}

		public void Reset()
		{
			if (m_armed)
			{
				RestoreLifted();
			}
			Disarm();
		}

		public void Commit(Document document)
		{
			CommitInternal();
		}

		public void Cancel()
		{
			if (m_armed)
			{
				RestoreLifted();
			}
			Disarm();
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			if (!m_armed)
			{
				return false;
			}
			bool doubleClick = IsDoubleClick(x, y);
			RecordPress(x, y);
			if (doubleClick && PointInQuad(x, y, m_cornerX, m_cornerY))
			{
				CommitInternal();
				return false;
			}
			m_pressX = x;
			m_pressY = y;
			SnapshotPressQuad();
			int index;
			if (HitCorner(x, y, out index))
			{
				m_grab = GrabCorner;
				m_grabIndex = index;
				return false;
			}
			if (HitEdge(x, y, out index))
			{
				m_grab = GrabEdge;
				m_grabIndex = index;
				return false;
			}
			if (HitRotateRing(x, y, out index))
			{
				m_grab = GrabRotate;
				m_grabIndex = index;
				return false;
			}
			if (PointInQuad(x, y, m_cornerX, m_cornerY))
			{
				m_grab = GrabMove;
				m_grabIndex = -1;
				return false;
			}
			m_grab = GrabNone;
			m_grabIndex = -1;
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (!m_armed)
			{
				return false;
			}
			if (m_grab == GrabNone)
			{
				return false;
			}
			bool shift = state.ShiftHeld();
			bool alt = state.AltHeld();
			double deltaX = x - m_pressX;
			double deltaY = y - m_pressY;
			if (m_grab == GrabMove)
			{
				ApplyMove(deltaX, deltaY);
				return false;
			}
			if (m_grab == GrabRotate)
			{
				ApplyRotate(x, y, shift);
				return false;
			}
			if (m_mode == ModeRotate)
			{
				ApplyRotate(x, y, shift);
				return false;
			}
			if (m_grab == GrabCorner)
			{
				if (m_mode == ModeDistort)
				{
					ApplyDistort(m_grabIndex, x, y);
					return false;
				}
				if (m_mode == ModePerspective)
				{
					ApplyPerspective(m_grabIndex, deltaX, deltaY);
					return false;
				}
				if (alt)
				{
					ApplyCornerScaleCentered(m_grabIndex, x, y, shift);
					return false;
				}
				ApplyCornerScale(m_grabIndex, x, y, shift);
				return false;
			}
			if (m_grab == GrabEdge)
			{
				if (m_mode == ModeSkew)
				{
					ApplySkew(m_grabIndex, deltaX, deltaY);
					return false;
				}
				ApplyEdgeScale(m_grabIndex, deltaX, deltaY);
				return false;
			}
			return false;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			m_grab = GrabNone;
			m_grabIndex = -1;
		}
	}
}
