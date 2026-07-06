using System;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class Ruler : SKCanvasView
	{
		private const float TargetLabelSpacing = 50.0f;
		private const double DocumentDpi = 72.0;

		private CanvasView m_canvas;
		private bool m_horizontal;

		public Ruler(CanvasView canvas, bool horizontal)
		{
			m_canvas = canvas;
			m_horizontal = horizontal;
			PaintSurface += OnPaintSurface;
			EnableTouchEvents = true;
			Touch += OnTouch;
		}

		private void OnTouch(object sender, SKTouchEventArgs eventArgs)
		{
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				MainView main = MainView.Self;
				if (main == null)
				{
					eventArgs.Handled = true;
					return;
				}
				if (eventArgs.MouseButton == SKMouseButton.Right)
				{
					double scaleX = 1.0;
					double scaleY = 1.0;
					if (CanvasSize.Width > 0 && Width > 0)
					{
						scaleX = Width / CanvasSize.Width;
					}
					if (CanvasSize.Height > 0 && Height > 0)
					{
						scaleY = Height / CanvasSize.Height;
					}
					double pageX = PagePosition(this, true) + (eventArgs.Location.X * scaleX);
					double pageY = PagePosition(this, false) + (eventArgs.Location.Y * scaleY);
					main.ShowRulerUnitsMenu(pageX, pageY);
					eventArgs.Handled = true;
					return;
				}
				int orientation = 2;
				if (m_horizontal)
				{
					orientation = 1;
				}
				DocumentWindow ownerWindow = m_canvas.OwnerWindow();
				if (ownerWindow != null)
				{
					ownerWindow.BeginGuideCreation(orientation);
				}
			}
			eventArgs.Handled = true;
		}

		private static double PagePosition(Microsoft.Maui.Controls.VisualElement element, bool horizontal)
		{
			double total = 0.0;
			Microsoft.Maui.Controls.Element current = element;
			for (int guard = 0; guard < 100; guard++)
			{
				Microsoft.Maui.Controls.VisualElement visual = current as Microsoft.Maui.Controls.VisualElement;
				if (visual == null)
				{
					break;
				}
				if (horizontal)
				{
					total += visual.X;
				}
				else
				{
					total += visual.Y;
				}
				current = current.Parent;
			}
			return total;
		}

		private double PixelsPerUnit()
		{
			Bitmute.Imaging.Document document = m_canvas.CurrentDocument();
			Bitmute.Imaging.eRulerUnits units = document.RulerUnits();
			if (units == Bitmute.Imaging.eRulerUnits.Millimeters)
			{
				return DocumentDpi / 25.4;
			}
			if (units == Bitmute.Imaging.eRulerUnits.Centimeters)
			{
				return DocumentDpi / 2.54;
			}
			if (units == Bitmute.Imaging.eRulerUnits.Percent)
			{
				int documentLength = document.Width();
				if (!m_horizontal)
				{
					documentLength = document.Height();
				}
				if (documentLength < 1)
				{
					return 1.0;
				}
				return documentLength / 100.0;
			}
			return 1.0;
		}

		private static int NiceStep(float zoom)
		{
			double target = TargetLabelSpacing / zoom;
			if (target < 1.0)
			{
				target = 1.0;
			}
			double magnitude = Math.Pow(10.0, Math.Floor(Math.Log10(target)));
			double residual = target / magnitude;
			double nice = 10.0;
			if (residual <= 1.0)
			{
				nice = 1.0;
			}
			else if (residual <= 2.0)
			{
				nice = 2.0;
			}
			else if (residual <= 5.0)
			{
				nice = 5.0;
			}
			int step = (int)(nice * magnitude);
			if (step < 1)
			{
				step = 1;
			}
			return step;
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(SKColors.White);

			float zoom = m_canvas.Zoom();
			if (zoom <= 0.0f)
			{
				return;
			}

			SKPaint tickPaint = new SKPaint();
			tickPaint.Color = SKColors.Black;
			tickPaint.StrokeWidth = 1.0f;
			tickPaint.IsAntialias = false;
			SKFont font = new SKFont();
			font.Size = 9.0f;
			SKPaint textPaint = new SKPaint();
			textPaint.Color = SKColors.Black;
			textPaint.IsAntialias = true;

			float offset = m_canvas.PanOffsetX();
			float length = info.Width;
			float thickness = info.Height;
			if (!m_horizontal)
			{
				offset = m_canvas.PanOffsetY();
				length = info.Height;
				thickness = info.Width;
			}

			double pixelsPerUnit = PixelsPerUnit();
			double unitZoom = zoom * pixelsPerUnit;
			int step = NiceStep((float)unitZoom);
			int minorStep = step / 10;
			if (minorStep < 1)
			{
				minorStep = 1;
			}

			int firstPosition = (int)(Math.Floor((-offset / unitZoom) / minorStep) * minorStep);
			int lastPosition = (int)Math.Ceiling((length - offset) / unitZoom);

			for (int position = firstPosition; position <= lastPosition; position = position + minorStep)
			{
				float screen = offset + (float)(position * unitZoom);
				if (screen < -1.0f || screen > length + 1.0f)
				{
					continue;
				}
				bool major = (position % step) == 0;
				float tickLength = 4.0f;
				if (major)
				{
					tickLength = thickness;
				}
				if (m_horizontal)
				{
					canvas.DrawLine(screen, thickness - tickLength, screen, thickness, tickPaint);
					if (major)
					{
						canvas.DrawText(position.ToString(), screen + 2.0f, 9.0f, SKTextAlign.Left, font, textPaint);
					}
				}
				else
				{
					canvas.DrawLine(thickness - tickLength, screen, thickness, screen, tickPaint);
					if (major)
					{
						canvas.DrawText(position.ToString(), 1.0f, screen + 9.0f, SKTextAlign.Left, font, textPaint);
					}
				}
			}

			textPaint.Dispose();
			font.Dispose();
			tickPaint.Dispose();
		}
	}
}
