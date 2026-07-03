using System;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class CanvasScrollbar : SKCanvasView
	{
		private const float MinimumThumb = 24.0f;

		private CanvasView m_canvas;
		private bool m_horizontal;
		private bool m_dragging;

		public CanvasScrollbar(CanvasView canvas, bool horizontal)
		{
			m_canvas = canvas;
			m_horizontal = horizontal;
			EnableTouchEvents = true;
			PaintSurface += OnPaintSurface;
			Touch += OnTouch;
			Theme.Changed += OnThemeChanged;
		}

		private void OnThemeChanged(object sender, EventArgs eventArgs)
		{
			InvalidateSurface();
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(Theme.ScrollbarTrack());

			float trackLength = info.Width;
			float trackThickness = info.Height;
			float contentLength = m_canvas.ContentWidth();
			float viewport = m_canvas.ViewportWidth();
			float offset = m_canvas.PanOffsetX();
			if (!m_horizontal)
			{
				trackLength = info.Height;
				trackThickness = info.Width;
				contentLength = m_canvas.ContentHeight();
				viewport = m_canvas.ViewportHeight();
				offset = m_canvas.PanOffsetY();
			}

			float total = contentLength;
			if (total < viewport)
			{
				total = viewport;
			}
			float thumbSize = (viewport / total) * trackLength;
			if (thumbSize < MinimumThumb)
			{
				thumbSize = MinimumThumb;
			}
			if (thumbSize > trackLength)
			{
				thumbSize = trackLength;
			}
			float maxScroll = total - viewport;
			float scrollPos = -offset;
			if (scrollPos < 0.0f)
			{
				scrollPos = 0.0f;
			}
			if (scrollPos > maxScroll)
			{
				scrollPos = maxScroll;
			}
			float thumbPos = 0.0f;
			if (maxScroll > 0.0f)
			{
				thumbPos = (scrollPos / maxScroll) * (trackLength - thumbSize);
			}

			SKPaint thumbPaint = new SKPaint();
			thumbPaint.Color = Theme.ScrollbarThumb();
			thumbPaint.IsAntialias = true;
			SKRect thumb;
			if (m_horizontal)
			{
				thumb = new SKRect(thumbPos, 2.0f, thumbPos + thumbSize, trackThickness - 2.0f);
			}
			else
			{
				thumb = new SKRect(2.0f, thumbPos, trackThickness - 2.0f, thumbPos + thumbSize);
			}
			canvas.DrawRoundRect(thumb, 3.0f, 3.0f, thumbPaint);
			thumbPaint.Dispose();
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
			float trackLength = m_canvas.ViewportWidth();
			float contentLength = m_canvas.ContentWidth();
			float viewport = m_canvas.ViewportWidth();
			float touchPos = eventArgs.Location.X;
			if (!m_horizontal)
			{
				trackLength = CanvasSize.Height;
				contentLength = m_canvas.ContentHeight();
				viewport = m_canvas.ViewportHeight();
				touchPos = eventArgs.Location.Y;
			}
			else
			{
				trackLength = CanvasSize.Width;
			}

			float total = contentLength;
			if (total < viewport)
			{
				total = viewport;
			}
			float maxScroll = total - viewport;
			if (maxScroll <= 0.0f)
			{
				eventArgs.Handled = true;
				return;
			}
			float thumbSize = (viewport / total) * trackLength;
			if (thumbSize < MinimumThumb)
			{
				thumbSize = MinimumThumb;
			}
			float travel = trackLength - thumbSize;
			if (travel <= 0.0f)
			{
				eventArgs.Handled = true;
				return;
			}
			float thumbPos = touchPos - (thumbSize / 2.0f);
			if (thumbPos < 0.0f)
			{
				thumbPos = 0.0f;
			}
			if (thumbPos > travel)
			{
				thumbPos = travel;
			}
			float scrollPos = (thumbPos / travel) * maxScroll;
			if (m_horizontal)
			{
				m_canvas.SetPanOffsetX(-scrollPos);
			}
			else
			{
				m_canvas.SetPanOffsetY(-scrollPos);
			}
			eventArgs.Handled = true;
		}
	}
}
