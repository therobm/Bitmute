using System;
using Bitmute.Imaging;
using Bitmute.Tools;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class CanvasView : SKCanvasView
	{
		private static readonly SKColor s_workspace = new SKColor(0x2B, 0x2B, 0x2B);
		private static readonly SKColor s_checkerLight = new SKColor(0xFF, 0xFF, 0xFF);
		private static readonly SKColor s_checkerDark = new SKColor(0xC8, 0xC8, 0xC8);
		private static readonly SKColor s_border = new SKColor(0x10, 0x10, 0x10);
		private const int CheckerSquare = 8;

		private static SKBitmap s_checkerTile;

		private Document m_document;
		private SKBitmap m_composite;
		private bool m_composeDirty;
		private float m_zoom;
		private float m_offsetX;
		private float m_offsetY;
		private bool m_viewInitialized;
		private bool m_panning;
		private float m_panLastX;
		private float m_panLastY;

		private static SKBitmap CheckerTile()
		{
			if (s_checkerTile != null)
			{
				return s_checkerTile;
			}
			int size = CheckerSquare * 2;
			SKBitmap tile = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
			SKCanvas canvas = new SKCanvas(tile);
			canvas.Clear(s_checkerLight);
			SKPaint paint = new SKPaint();
			paint.Color = s_checkerDark;
			canvas.DrawRect(new SKRect(0.0f, 0.0f, CheckerSquare, CheckerSquare), paint);
			canvas.DrawRect(new SKRect(CheckerSquare, CheckerSquare, size, size), paint);
			paint.Dispose();
			canvas.Dispose();
			s_checkerTile = tile;
			return s_checkerTile;
		}

		private void EnsureComposite()
		{
			int width = m_document.Width();
			int height = m_document.Height();
			if (m_composite != null && m_composite.Width == width && m_composite.Height == height)
			{
				return;
			}
			if (m_composite != null)
			{
				m_composite.Dispose();
			}
			m_composite = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(s_workspace);

			if (info.Width <= 0 || info.Height <= 0)
			{
				return;
			}

			if (m_composeDirty || m_composite == null)
			{
				EnsureComposite();
				m_document.CompositeInto(m_composite);
				m_composeDirty = false;
			}

			float docWidth = m_document.Width();
			float docHeight = m_document.Height();

			if (!m_viewInitialized)
			{
				float fitX = info.Width / docWidth;
				float fitY = info.Height / docHeight;
				float fit = fitX;
				if (fitY < fit)
				{
					fit = fitY;
				}
				if (fit > 1.0f)
				{
					fit = 1.0f;
				}
				m_zoom = fit;
				m_offsetX = (info.Width - (docWidth * m_zoom)) / 2.0f;
				m_offsetY = (info.Height - (docHeight * m_zoom)) / 2.0f;
				m_viewInitialized = true;
				ReportZoomInfo();
			}

			float rectWidth = docWidth * m_zoom;
			float rectHeight = docHeight * m_zoom;
			SKRect destination = new SKRect(m_offsetX, m_offsetY, m_offsetX + rectWidth, m_offsetY + rectHeight);

			SKPaint checkerPaint = new SKPaint();
			SKMatrix localMatrix = SKMatrix.CreateTranslation(m_offsetX, m_offsetY);
			checkerPaint.Shader = SKShader.CreateBitmap(CheckerTile(), SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, localMatrix);
			canvas.DrawRect(destination, checkerPaint);
			checkerPaint.Dispose();

			SKImage image = SKImage.FromBitmap(m_composite);
			SKPaint imagePaint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			canvas.DrawImage(image, destination, sampling, imagePaint);
			imagePaint.Dispose();
			image.Dispose();

			SKPaint borderPaint = new SKPaint();
			borderPaint.Style = SKPaintStyle.Stroke;
			borderPaint.StrokeWidth = 1.0f;
			borderPaint.Color = s_border;
			borderPaint.IsAntialias = false;
			canvas.DrawRect(destination, borderPaint);
			borderPaint.Dispose();
		}

		public CanvasView(Document document)
		{
			m_document = document;
			m_zoom = 1.0f;
			m_offsetX = 0.0f;
			m_offsetY = 0.0f;
			m_composeDirty = true;
			m_viewInitialized = false;
			PaintSurface += OnPaintSurface;
			EnableTouchEvents = true;
			Touch += OnTouch;
		}

		private void OnTouch(object sender, SKTouchEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				eventArgs.Handled = true;
				return;
			}

			float documentX = (eventArgs.Location.X - m_offsetX) / m_zoom;
			float documentY = (eventArgs.Location.Y - m_offsetY) / m_zoom;
			int pixelX = (int)Math.Floor(documentX);
			int pixelY = (int)Math.Floor(documentY);
			main.UpdateCursor(pixelX, pixelY);

			if (eventArgs.MouseButton == SKMouseButton.Middle)
			{
				if (eventArgs.ActionType == SKTouchAction.Pressed)
				{
					m_panning = true;
					m_panLastX = eventArgs.Location.X;
					m_panLastY = eventArgs.Location.Y;
				}
				else if (eventArgs.ActionType == SKTouchAction.Released)
				{
					m_panning = false;
				}
				eventArgs.Handled = true;
				return;
			}

			if (m_panning && eventArgs.ActionType == SKTouchAction.Moved)
			{
				m_offsetX = m_offsetX + (eventArgs.Location.X - m_panLastX);
				m_offsetY = m_offsetY + (eventArgs.Location.Y - m_panLastY);
				m_panLastX = eventArgs.Location.X;
				m_panLastY = eventArgs.Location.Y;
				InvalidateSurface();
				eventArgs.Handled = true;
				return;
			}

			if (eventArgs.MouseButton == SKMouseButton.Right)
			{
				eventArgs.Handled = true;
				return;
			}

			Tool tool = main.CurrentTool();
			ToolState state = main.CurrentToolState();
			if (tool == null || state == null)
			{
				eventArgs.Handled = true;
				return;
			}

			bool changed = false;
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				changed = tool.OnPressed(m_document, pixelX, pixelY, state);
			}
			else if (eventArgs.ActionType == SKTouchAction.Moved)
			{
				if (eventArgs.InContact)
				{
					changed = tool.OnDragged(m_document, pixelX, pixelY, state);
				}
			}
			else if (eventArgs.ActionType == SKTouchAction.Released)
			{
				tool.OnReleased(m_document, pixelX, pixelY, state);
			}

			if (changed)
			{
				MarkComposeDirty();
			}
			if (tool is EyedropperTool)
			{
				main.OnCanvasInteracted();
			}
			eventArgs.Handled = true;
		}

		private int ZoomPercent()
		{
			return (int)System.Math.Round(m_zoom * 100.0f);
		}

		private void ReportZoomInfo()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.UpdateZoomInfo(ZoomPercent(), m_document.Width(), m_document.Height());
		}

		private void ApplyZoomCentered(float newZoom)
		{
			if (newZoom < 0.05f)
			{
				newZoom = 0.05f;
			}
			if (newZoom > 32.0f)
			{
				newZoom = 32.0f;
			}
			SKSize size = CanvasSize;
			float centerX = (float)size.Width / 2.0f;
			float centerY = (float)size.Height / 2.0f;
			float documentCenterX = (centerX - m_offsetX) / m_zoom;
			float documentCenterY = (centerY - m_offsetY) / m_zoom;
			m_zoom = newZoom;
			m_offsetX = centerX - (documentCenterX * m_zoom);
			m_offsetY = centerY - (documentCenterY * m_zoom);
			ReportZoomInfo();
			InvalidateSurface();
		}

		public void ZoomIn()
		{
			ApplyZoomCentered(m_zoom * 1.25f);
		}

		public void ZoomOut()
		{
			ApplyZoomCentered(m_zoom * 0.8f);
		}

		public void FitToView()
		{
			m_viewInitialized = false;
			ReportZoomInfo();
			InvalidateSurface();
		}

		public Document CurrentDocument()
		{
			return m_document;
		}

		public float Zoom()
		{
			return m_zoom;
		}

		public void MarkComposeDirty()
		{
			m_composeDirty = true;
			InvalidateSurface();
		}
	}
}
