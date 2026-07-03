using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public enum eShapeKind
	{
		Rectangle,
		RoundedRectangle,
		Ellipse,
		Polygon
	}

	public class ShapeTool : Tool
	{
		private const int PolygonSides = 6;

		private eShapeKind m_kind;
		private bool m_active;
		private int m_startX;
		private int m_startY;
		private int m_endX;
		private int m_endY;

		public ShapeTool(eShapeKind kind)
		{
			m_kind = kind;
		}

		public override bool IsDestructive()
		{
			return true;
		}

		public eShapeKind Kind()
		{
			return m_kind;
		}

		public bool HasPreview()
		{
			return m_active;
		}

		public int PreviewStartX()
		{
			return m_startX;
		}

		public int PreviewStartY()
		{
			return m_startY;
		}

		public int PreviewEndX()
		{
			return m_endX;
		}

		public int PreviewEndY()
		{
			return m_endY;
		}

		public static SKPath BuildPolygon(SKRect rect, int sides)
		{
			float centerX = (rect.Left + rect.Right) / 2.0f;
			float centerY = (rect.Top + rect.Bottom) / 2.0f;
			float radiusX = (rect.Right - rect.Left) / 2.0f;
			float radiusY = (rect.Bottom - rect.Top) / 2.0f;
			SKPathBuilder builder = new SKPathBuilder();
			for (int index = 0; index < sides; index++)
			{
				double angle = (-System.Math.PI / 2.0) + ((2.0 * System.Math.PI * index) / sides);
				float pointX = centerX + (radiusX * (float)System.Math.Cos(angle));
				float pointY = centerY + (radiusY * (float)System.Math.Sin(angle));
				if (index == 0)
				{
					builder.MoveTo(pointX, pointY);
				}
				else
				{
					builder.LineTo(pointX, pointY);
				}
			}
			builder.Close();
			return builder.Snapshot();
		}

		public static int Sides()
		{
			return PolygonSides;
		}

		private static SKColor SourceOver(SKColor source, SKColor destination)
		{
			int sourceAlpha = source.Alpha;
			if (sourceAlpha == 255)
			{
				return source;
			}
			int inverse = 255 - sourceAlpha;
			int destinationContribution = ((destination.Alpha * inverse) + 127) / 255;
			int outAlpha = sourceAlpha + destinationContribution;
			if (outAlpha == 0)
			{
				return new SKColor(0, 0, 0, 0);
			}
			int red = ((source.Red * sourceAlpha) + (destination.Red * destinationContribution) + (outAlpha / 2)) / outAlpha;
			int green = ((source.Green * sourceAlpha) + (destination.Green * destinationContribution) + (outAlpha / 2)) / outAlpha;
			int blue = ((source.Blue * sourceAlpha) + (destination.Blue * destinationContribution) + (outAlpha / 2)) / outAlpha;
			return new SKColor((byte)red, (byte)green, (byte)blue, (byte)outAlpha);
		}

		private void SetConstrainedEnd(int rawEndX, int rawEndY, bool shift)
		{
			if (!shift)
			{
				m_endX = rawEndX;
				m_endY = rawEndY;
				return;
			}
			int deltaX = rawEndX - m_startX;
			int deltaY = rawEndY - m_startY;
			int absoluteX = deltaX;
			if (absoluteX < 0)
			{
				absoluteX = -absoluteX;
			}
			int absoluteY = deltaY;
			if (absoluteY < 0)
			{
				absoluteY = -absoluteY;
			}
			int size = absoluteX;
			if (absoluteY > size)
			{
				size = absoluteY;
			}
			int signX = 1;
			if (deltaX < 0)
			{
				signX = -1;
			}
			int signY = 1;
			if (deltaY < 0)
			{
				signY = -1;
			}
			m_endX = m_startX + (signX * size);
			m_endY = m_startY + (signY * size);
		}

		private void RenderShape(Document document, Layer layer, ToolState state)
		{
			int left = m_startX;
			if (m_endX < left)
			{
				left = m_endX;
			}
			int top = m_startY;
			if (m_endY < top)
			{
				top = m_endY;
			}
			int right = m_startX;
			if (m_endX > right)
			{
				right = m_endX;
			}
			int bottom = m_startY;
			if (m_endY > bottom)
			{
				bottom = m_endY;
			}
			int pad = 2;
			int boundsX = left - pad;
			int boundsY = top - pad;
			int boundsWidth = (right - left) + (2 * pad) + 1;
			int boundsHeight = (bottom - top) + (2 * pad) + 1;
			if (boundsWidth <= 0 || boundsHeight <= 0)
			{
				return;
			}

			SKBitmap temp = new SKBitmap(boundsWidth, boundsHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			temp.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(temp);
			SKPaint paint = new SKPaint();
			paint.Color = state.Foreground();
			paint.IsAntialias = true;
			paint.Style = SKPaintStyle.Fill;
			SKRect rect = new SKRect(left - boundsX, top - boundsY, (left - boundsX) + (right - left), (top - boundsY) + (bottom - top));
			if (m_kind == eShapeKind.Rectangle)
			{
				canvas.DrawRect(rect, paint);
			}
			else if (m_kind == eShapeKind.Ellipse)
			{
				canvas.DrawOval(rect, paint);
			}
			else if (m_kind == eShapeKind.RoundedRectangle)
			{
				float radius = (rect.Right - rect.Left);
				if ((rect.Bottom - rect.Top) < radius)
				{
					radius = rect.Bottom - rect.Top;
				}
				radius = radius * 0.2f;
				canvas.DrawRoundRect(rect, radius, radius, paint);
			}
			else
			{
				SKPath path = BuildPolygon(rect, PolygonSides);
				canvas.DrawPath(path, paint);
				path.Dispose();
			}
			paint.Dispose();
			canvas.Dispose();

			Selection selection = document.Selection();
			bool clip = selection.IsActive();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			SKBitmap layerBitmap = layer.Bitmap();
			int layerWidth = layerBitmap.Width;
			int layerHeight = layerBitmap.Height;
			for (int tempY = 0; tempY < boundsHeight; tempY++)
			{
				int canvasY = boundsY + tempY;
				for (int tempX = 0; tempX < boundsWidth; tempX++)
				{
					SKColor source = temp.GetPixel(tempX, tempY);
					if (source.Alpha == 0)
					{
						continue;
					}
					int canvasX = boundsX + tempX;
					if (clip && !selection.IsSelected(canvasX, canvasY))
					{
						continue;
					}
					int bitmapX = canvasX - offsetX;
					int bitmapY = canvasY - offsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= layerWidth || bitmapY >= layerHeight)
					{
						continue;
					}
					SKColor destination = layerBitmap.GetPixel(bitmapX, bitmapY);
					SKColor blended = SourceOver(source, destination);
					layerBitmap.SetPixel(bitmapX, bitmapY, blended);
				}
			}
			temp.Dispose();
			MarkStrokeDirty(document, left, top, right, bottom, pad);
		}

		public override bool OnPressed(Document document, int x, int y, ToolState state)
		{
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			m_active = true;
			m_startX = x;
			m_startY = y;
			m_endX = x;
			m_endY = y;
			return false;
		}

		public override bool OnDragged(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				return false;
			}
			SetConstrainedEnd(x, y, state.ShiftHeld());
			return false;
		}

		public override void OnReleased(Document document, int x, int y, ToolState state)
		{
			if (!m_active)
			{
				return;
			}
			SetConstrainedEnd(x, y, state.ShiftHeld());
			Layer layer = document.ActiveLayer();
			if (layer != null)
			{
				RenderShape(document, layer, state);
			}
			m_active = false;
		}
	}
}
