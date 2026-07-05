using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class NavigatorPanel : ContentView
	{
		private const int ThumbnailCheckerCell = 8;
		private const double ThumbnailHeightRequest = 160.0;

		private static readonly SKColor s_checkerLight = new SKColor(0xFF, 0xFF, 0xFF);
		private static readonly SKColor s_checkerDark = new SKColor(0xC8, 0xC8, 0xC8);
		private static readonly SKColor s_viewRectColor = new SKColor(0xFF, 0x30, 0x30);

		private SKCanvasView m_thumbnail;
		private Label m_zoomLabel;
		private bool m_pressStartedHere;
		private float m_thumbnailLeft;
		private float m_thumbnailTop;
		private float m_thumbnailScale;
		private float m_thumbnailDocumentWidth;
		private float m_thumbnailDocumentHeight;

		private static Document ActiveDoc()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return null;
			}
			return main.ActiveDocument();
		}

		private static CanvasView ActiveCanvas()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return null;
			}
			return main.ActiveCanvas();
		}

		private void ComputeThumbnailLayout(SKImageInfo info, Document document)
		{
			float documentWidth = document.Width();
			float documentHeight = document.Height();
			m_thumbnailDocumentWidth = documentWidth;
			m_thumbnailDocumentHeight = documentHeight;
			if (documentWidth <= 0.0f || documentHeight <= 0.0f)
			{
				m_thumbnailScale = 0.0f;
				m_thumbnailLeft = 0.0f;
				m_thumbnailTop = 0.0f;
				return;
			}
			float scaleX = info.Width / documentWidth;
			float scaleY = info.Height / documentHeight;
			float scale = scaleX;
			if (scaleY < scale)
			{
				scale = scaleY;
			}
			m_thumbnailScale = scale;
			m_thumbnailLeft = (info.Width - (documentWidth * scale)) / 2.0f;
			m_thumbnailTop = (info.Height - (documentHeight * scale)) / 2.0f;
		}

		private void DrawChecker(SKCanvas canvas, SKRect rect)
		{
			SKPaint lightPaint = new SKPaint();
			lightPaint.Color = s_checkerLight;
			canvas.DrawRect(rect, lightPaint);
			lightPaint.Dispose();
			SKPaint darkPaint = new SKPaint();
			darkPaint.Color = s_checkerDark;
			int left = (int)System.Math.Floor(rect.Left);
			int top = (int)System.Math.Floor(rect.Top);
			int right = (int)System.Math.Ceiling(rect.Right);
			int bottom = (int)System.Math.Ceiling(rect.Bottom);
			int column = 0;
			for (int cellX = left; cellX < right; cellX = cellX + ThumbnailCheckerCell)
			{
				int row = 0;
				for (int cellY = top; cellY < bottom; cellY = cellY + ThumbnailCheckerCell)
				{
					int parity = column + row;
					if ((parity & 1) == 1)
					{
						float rectRight = cellX + ThumbnailCheckerCell;
						if (rectRight > rect.Right)
						{
							rectRight = rect.Right;
						}
						float rectBottom = cellY + ThumbnailCheckerCell;
						if (rectBottom > rect.Bottom)
						{
							rectBottom = rect.Bottom;
						}
						canvas.DrawRect(new SKRect(cellX, cellY, rectRight, rectBottom), darkPaint);
					}
					row++;
				}
				column++;
			}
			darkPaint.Dispose();
		}

		private void DrawLayers(SKCanvas canvas, Document document)
		{
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
			List<Layer> layers = document.Layers();
			for (int index = 0; index < layers.Count; index++)
			{
				Layer layer = layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				SKBitmap bitmap = layer.Bitmap();
				if (bitmap == null)
				{
					continue;
				}
				float destinationLeft = m_thumbnailLeft + (layer.OffsetX() * m_thumbnailScale);
				float destinationTop = m_thumbnailTop + (layer.OffsetY() * m_thumbnailScale);
				float destinationRight = destinationLeft + (bitmap.Width * m_thumbnailScale);
				float destinationBottom = destinationTop + (bitmap.Height * m_thumbnailScale);
				SKRect destination = new SKRect(destinationLeft, destinationTop, destinationRight, destinationBottom);
				SKPaint paint = new SKPaint();
				paint.Color = SKColors.White.WithAlpha(layer.Opacity());
				paint.BlendMode = SKBlendMode.SrcOver;
				SKPixmap pixmap = bitmap.PeekPixels();
				SKImage image = SKImage.FromPixels(pixmap);
				canvas.DrawImage(image, destination, sampling, paint);
				image.Dispose();
				pixmap.Dispose();
				paint.Dispose();
			}
		}

		private void DrawViewRectangle(SKCanvas canvas)
		{
			CanvasView canvasView = ActiveCanvas();
			if (canvasView == null)
			{
				return;
			}
			float zoom = canvasView.Zoom();
			if (zoom <= 0.0f)
			{
				return;
			}
			float panOffsetX = canvasView.PanOffsetX();
			float panOffsetY = canvasView.PanOffsetY();
			float viewportWidth = canvasView.ViewportWidth();
			float viewportHeight = canvasView.ViewportHeight();

			float documentVisibleLeft = -panOffsetX / zoom;
			float documentVisibleTop = -panOffsetY / zoom;
			float documentVisibleWidth = viewportWidth / zoom;
			float documentVisibleHeight = viewportHeight / zoom;

			float documentRight = documentVisibleLeft + documentVisibleWidth;
			float documentBottom = documentVisibleTop + documentVisibleHeight;

			if (documentVisibleLeft < 0.0f)
			{
				documentVisibleLeft = 0.0f;
			}
			if (documentVisibleTop < 0.0f)
			{
				documentVisibleTop = 0.0f;
			}
			if (documentRight > m_thumbnailDocumentWidth)
			{
				documentRight = m_thumbnailDocumentWidth;
			}
			if (documentBottom > m_thumbnailDocumentHeight)
			{
				documentBottom = m_thumbnailDocumentHeight;
			}
			if (documentRight <= documentVisibleLeft || documentBottom <= documentVisibleTop)
			{
				return;
			}

			float rectLeft = m_thumbnailLeft + (documentVisibleLeft * m_thumbnailScale);
			float rectTop = m_thumbnailTop + (documentVisibleTop * m_thumbnailScale);
			float rectRight = m_thumbnailLeft + (documentRight * m_thumbnailScale);
			float rectBottom = m_thumbnailTop + (documentBottom * m_thumbnailScale);
			SKRect rectangle = new SKRect(rectLeft, rectTop, rectRight, rectBottom);

			SKPaint rectPaint = new SKPaint();
			rectPaint.Style = SKPaintStyle.Stroke;
			rectPaint.StrokeWidth = 1.0f;
			rectPaint.Color = s_viewRectColor;
			rectPaint.IsAntialias = false;
			canvas.DrawRect(rectangle, rectPaint);
			rectPaint.Dispose();
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(Theme.ScrollbarTrack());

			if (info.Width <= 0 || info.Height <= 0)
			{
				return;
			}

			Document document = ActiveDoc();
			if (document == null)
			{
				return;
			}
			if (document.Width() <= 0 || document.Height() <= 0)
			{
				return;
			}

			ComputeThumbnailLayout(info, document);
			if (m_thumbnailScale <= 0.0f)
			{
				return;
			}

			SKRect thumbnailRect = new SKRect(m_thumbnailLeft, m_thumbnailTop, m_thumbnailLeft + (m_thumbnailDocumentWidth * m_thumbnailScale), m_thumbnailTop + (m_thumbnailDocumentHeight * m_thumbnailScale));
			DrawChecker(canvas, thumbnailRect);
			DrawLayers(canvas, document);
			DrawViewRectangle(canvas);
		}

		private void PanToThumbnailPoint(float pointX, float pointY)
		{
			if (m_thumbnailScale <= 0.0f)
			{
				return;
			}
			CanvasView canvasView = ActiveCanvas();
			if (canvasView == null)
			{
				return;
			}
			float zoom = canvasView.Zoom();
			if (zoom <= 0.0f)
			{
				return;
			}
			float documentX = (pointX - m_thumbnailLeft) / m_thumbnailScale;
			float documentY = (pointY - m_thumbnailTop) / m_thumbnailScale;

			float viewportWidth = canvasView.ViewportWidth();
			float viewportHeight = canvasView.ViewportHeight();

			float newOffsetX = (viewportWidth / 2.0f) - (documentX * zoom);
			float newOffsetY = (viewportHeight / 2.0f) - (documentY * zoom);
			canvasView.SetPanOffsetX(newOffsetX);
			canvasView.SetPanOffsetY(newOffsetY);
			RefreshView();
		}

		private void OnTouch(object sender, SKTouchEventArgs eventArgs)
		{
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				m_pressStartedHere = true;
				PanToThumbnailPoint(eventArgs.Location.X, eventArgs.Location.Y);
				eventArgs.Handled = true;
				return;
			}
			if (eventArgs.ActionType == SKTouchAction.Moved)
			{
				if (!eventArgs.InContact)
				{
					m_pressStartedHere = false;
					eventArgs.Handled = true;
					return;
				}
				if (!m_pressStartedHere)
				{
					eventArgs.Handled = true;
					return;
				}
				PanToThumbnailPoint(eventArgs.Location.X, eventArgs.Location.Y);
				eventArgs.Handled = true;
				return;
			}
			m_pressStartedHere = false;
			eventArgs.Handled = true;
		}

		private void OnThemeChanged(object sender, EventArgs eventArgs)
		{
			m_thumbnail.InvalidateSurface();
		}

		private void UpdateZoomLabel()
		{
			CanvasView canvasView = ActiveCanvas();
			if (canvasView == null)
			{
				m_zoomLabel.Text = "";
				return;
			}
			int percent = (int)System.Math.Round(canvasView.Zoom() * 100.0f);
			m_zoomLabel.Text = percent + "%";
		}

		public NavigatorPanel()
		{
			m_pressStartedHere = false;
			m_thumbnailScale = 0.0f;

			m_thumbnail = new SKCanvasView();
			m_thumbnail.HeightRequest = ThumbnailHeightRequest;
			m_thumbnail.EnableTouchEvents = true;
			m_thumbnail.PaintSurface += OnPaintSurface;
			m_thumbnail.Touch += OnTouch;

			m_zoomLabel = new Label();
			m_zoomLabel.FontSize = UiConstants.PanelFontSize;
			m_zoomLabel.HorizontalOptions = LayoutOptions.Center;
			m_zoomLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_zoomLabel.Text = "";

			VerticalStackLayout stack = new VerticalStackLayout();
			stack.Spacing = 6.0;
			stack.Padding = new Thickness(8.0);
			stack.Add(m_thumbnail);
			stack.Add(m_zoomLabel);

			Content = stack;
			Theme.Changed += OnThemeChanged;
			UpdateZoomLabel();
		}

		public void RefreshView()
		{
			UpdateZoomLabel();
			m_thumbnail.InvalidateSurface();
		}
	}
}
