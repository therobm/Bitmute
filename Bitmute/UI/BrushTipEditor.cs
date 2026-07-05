using System;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Bitmute.Tools;

namespace Bitmute.UI
{
	public class BrushTipEditor : SKCanvasView
	{
		private ToolState m_toolState;
		private Action<int, int> m_changed;
		private bool m_dragging;

		public BrushTipEditor(ToolState toolState, Action<int, int> changed)
		{
			m_toolState = toolState;
			m_changed = changed;
			PaintSurface += OnPaintSurface;
			EnableTouchEvents = true;
			Touch += OnTouch;
		}

		public void RefreshPreview()
		{
			InvalidateSurface();
		}

		private void OnTouch(object sender, SKTouchEventArgs eventArgs)
		{
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				m_dragging = true;
				ApplyPoint(eventArgs.Location);
			}
			else if (eventArgs.ActionType == SKTouchAction.Moved)
			{
				if (m_dragging && eventArgs.InContact)
				{
					ApplyPoint(eventArgs.Location);
				}
			}
			else if (eventArgs.ActionType == SKTouchAction.Released || eventArgs.ActionType == SKTouchAction.Cancelled || eventArgs.ActionType == SKTouchAction.Exited)
			{
				m_dragging = false;
			}
			eventArgs.Handled = true;
		}

		private void ApplyPoint(SKPoint location)
		{
			float centerX = (float)CanvasSize.Width / 2.0f;
			float centerY = (float)CanvasSize.Height / 2.0f;
			float maxRadius = System.Math.Min(centerX, centerY) * 0.9f;
			if (maxRadius < 1.0f)
			{
				return;
			}
			double deltaX = location.X - centerX;
			double deltaY = location.Y - centerY;
			double distance = System.Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
			if (distance < 1.0)
			{
				return;
			}
			double degrees = System.Math.Atan2(deltaY, deltaX) * 180.0 / System.Math.PI;
			int angle = (int)System.Math.Round(degrees);
			for (int guard = 0; guard < 4; guard++)
			{
				if (angle >= 0)
				{
					break;
				}
				angle = angle + 180;
			}
			for (int guard = 0; guard < 4; guard++)
			{
				if (angle <= 180)
				{
					break;
				}
				angle = angle - 180;
			}
			int roundness = (int)System.Math.Round((distance / maxRadius) * 100.0);
			if (roundness < 5)
			{
				roundness = 5;
			}
			if (roundness > 100)
			{
				roundness = 100;
			}
			if (m_toolState != null)
			{
				m_toolState.SetBrushAngle(angle);
				m_toolState.SetBrushRoundness(roundness);
			}
			if (m_changed != null)
			{
				m_changed(roundness, angle);
			}
			InvalidateSurface();
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(new SKColor(UiConstants.PanelSurface.ToUint()));

			float centerX = info.Width / 2.0f;
			float centerY = info.Height / 2.0f;
			float maxRadius = System.Math.Min(centerX, centerY) * 0.9f;

			SKPaint boundsPaint = new SKPaint();
			boundsPaint.Style = SKPaintStyle.Stroke;
			boundsPaint.StrokeWidth = 1.0f;
			boundsPaint.Color = new SKColor(UiConstants.Divider.ToUint());
			boundsPaint.IsAntialias = true;
			canvas.DrawCircle(centerX, centerY, maxRadius, boundsPaint);
			boundsPaint.Dispose();

			int roundness = 100;
			int angle = 0;
			if (m_toolState != null)
			{
				roundness = m_toolState.BrushRoundness();
				angle = m_toolState.BrushAngle();
			}
			float minorRadius = maxRadius * (roundness / 100.0f);
			if (minorRadius < 2.0f)
			{
				minorRadius = 2.0f;
			}

			canvas.Save();
			canvas.RotateDegrees(angle, centerX, centerY);
			SKPaint tipPaint = new SKPaint();
			tipPaint.Style = SKPaintStyle.Fill;
			tipPaint.Color = new SKColor(UiConstants.Accent.ToUint()).WithAlpha(180);
			tipPaint.IsAntialias = true;
			canvas.DrawOval(centerX, centerY, maxRadius, minorRadius, tipPaint);
			tipPaint.Dispose();
			SKPaint axisPaint = new SKPaint();
			axisPaint.Style = SKPaintStyle.Stroke;
			axisPaint.StrokeWidth = 1.0f;
			axisPaint.Color = new SKColor(UiConstants.OnSurface.ToUint());
			axisPaint.IsAntialias = true;
			canvas.DrawLine(centerX, centerY, centerX + maxRadius, centerY, axisPaint);
			axisPaint.Dispose();
			canvas.Restore();
		}
	}
}
