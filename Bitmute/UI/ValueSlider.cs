using System;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class ValueSlider : SKCanvasView
	{
		private const float TrackInset = 9.0f;

		private int m_minimum;
		private int m_maximum;
		private int m_value;
		private Action<int> m_onChanged;
		private bool m_dragging;

		public ValueSlider(int minimum, int maximum, int value, Action<int> onChanged)
		{
			m_minimum = minimum;
			m_maximum = maximum;
			m_value = value;
			m_onChanged = onChanged;
			EnableTouchEvents = true;
			PaintSurface += OnPaintSurface;
			Touch += OnTouch;
		}

		public int Value()
		{
			return m_value;
		}

		public void SetValueSilently(int value)
		{
			m_value = ClampValue(value);
			InvalidateSurface();
		}

		private int ClampValue(int value)
		{
			if (value < m_minimum)
			{
				return m_minimum;
			}
			if (value > m_maximum)
			{
				return m_maximum;
			}
			return value;
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(SKColors.Transparent);

			float left = TrackInset;
			float right = info.Width - TrackInset;
			if (right <= left)
			{
				return;
			}
			float centerY = info.Height / 2.0f;
			float trackTop = centerY - 2.0f;
			float trackBottom = centerY + 2.0f;
			SKRect trackRect = new SKRect(left, trackTop, right, trackBottom);

			SKPaint trackFill = new SKPaint();
			trackFill.Style = SKPaintStyle.Fill;
			trackFill.Color = new SKColor(UiConstants.SliderTrack.ToUint());
			trackFill.IsAntialias = true;
			canvas.DrawRoundRect(trackRect, 1.5f, 1.5f, trackFill);
			trackFill.Dispose();

			SKPaint trackBorder = new SKPaint();
			trackBorder.Style = SKPaintStyle.Stroke;
			trackBorder.StrokeWidth = 1.0f;
			trackBorder.Color = new SKColor(UiConstants.SliderTrackBorder.ToUint());
			trackBorder.IsAntialias = true;
			canvas.DrawRoundRect(trackRect, 1.5f, 1.5f, trackBorder);
			trackBorder.Dispose();

			float range = m_maximum - m_minimum;
			float fraction = 0.0f;
			if (range > 0.0f)
			{
				fraction = (m_value - m_minimum) / range;
			}
			if (fraction < 0.0f)
			{
				fraction = 0.0f;
			}
			if (fraction > 1.0f)
			{
				fraction = 1.0f;
			}
			float thumbX = left + (fraction * (right - left));

			float halfWidth = 5.0f;
			float topY = centerY - 8.0f;
			float shoulderY = centerY - 1.0f;
			float bottomY = centerY + 8.0f;
			SKPathBuilder builder = new SKPathBuilder();
			builder.MoveTo(thumbX, topY);
			builder.LineTo(thumbX + halfWidth, shoulderY);
			builder.LineTo(thumbX + halfWidth, bottomY);
			builder.LineTo(thumbX - halfWidth, bottomY);
			builder.LineTo(thumbX - halfWidth, shoulderY);
			builder.Close();
			SKPath path = builder.Snapshot();

			SKPaint thumbFill = new SKPaint();
			thumbFill.Style = SKPaintStyle.Fill;
			thumbFill.Color = new SKColor(UiConstants.SliderThumb.ToUint());
			thumbFill.IsAntialias = true;
			canvas.DrawPath(path, thumbFill);
			thumbFill.Dispose();

			SKPaint thumbBorder = new SKPaint();
			thumbBorder.Style = SKPaintStyle.Stroke;
			thumbBorder.StrokeWidth = 1.0f;
			thumbBorder.Color = new SKColor(UiConstants.SliderThumbBorder.ToUint());
			thumbBorder.IsAntialias = true;
			canvas.DrawPath(path, thumbBorder);
			thumbBorder.Dispose();

			path.Dispose();
			builder.Dispose();
		}

		private void OnTouch(object sender, SKTouchEventArgs eventArgs)
		{
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				m_dragging = true;
			}
			else if (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact)
			{
				if (!m_dragging)
				{
					eventArgs.Handled = true;
					return;
				}
			}
			else
			{
				m_dragging = false;
				eventArgs.Handled = true;
				return;
			}

			float left = TrackInset;
			float right = CanvasSize.Width - TrackInset;
			float travel = right - left;
			if (travel <= 0.0f)
			{
				eventArgs.Handled = true;
				return;
			}
			float fraction = (eventArgs.Location.X - left) / travel;
			if (fraction < 0.0f)
			{
				fraction = 0.0f;
			}
			if (fraction > 1.0f)
			{
				fraction = 1.0f;
			}
			int newValue = ClampValue(m_minimum + (int)System.Math.Round(fraction * (m_maximum - m_minimum)));
			if (newValue != m_value)
			{
				m_value = newValue;
				InvalidateSurface();
				if (m_onChanged != null)
				{
					m_onChanged(m_value);
				}
			}
			eventArgs.Handled = true;
		}
	}
}
