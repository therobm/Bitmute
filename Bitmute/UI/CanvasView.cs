using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Bitmute.Tools;
using Microsoft.Maui.Dispatching;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class CanvasView : SKCanvasView
	{
		private struct AntEdge
		{
			public bool m_vertical;
			public int m_fixedCoord;
			public int m_startCoord;

			public AntEdge(bool vertical, int fixedCoord, int startCoord)
			{
				m_vertical = vertical;
				m_fixedCoord = fixedCoord;
				m_startCoord = startCoord;
			}
		}

		private static readonly SKColor s_checkerLight = new SKColor(0xFF, 0xFF, 0xFF);
		private static readonly SKColor s_checkerDark = new SKColor(0xC8, 0xC8, 0xC8);
		private static readonly SKColor s_border = new SKColor(0x10, 0x10, 0x10);
		private const int CheckerSquare = 8;
		private const float AntLength = 6.0f;
		private const float AntStrokeWidth = 1.0f;

		private static SKBitmap s_checkerTile;

		private Document m_document;
		private SKBitmap m_composite;
		private SKBitmap m_transformAbove;
		private SKBitmap m_channelBitmap;
		private float m_zoom;
		private float m_offsetX;
		private float m_offsetY;
		private float m_lastViewportWidth;
		private float m_lastViewportHeight;
		private bool m_viewInitialized;
		private bool m_panning;
		private float m_panLastX;
		private float m_panLastY;
		private bool m_wheelHooked;
		private DocumentWindow m_ownerWindow;
		private IDispatcherTimer m_antTimer;
		private float m_antPhase;
		private IDispatcherTimer m_airbrushTimer;
		private bool m_airbrushActive;
		private int m_airbrushX;
		private int m_airbrushY;
		private List<AntEdge> m_antEdges;
		private int m_antEdgesGeneration;
		private float m_cursorDeviceX;
		private float m_cursorDeviceY;
		private bool m_cursorInside;
		private bool m_toolStrokeActive;
		private bool m_zoomDragging;
		private float m_zoomDragStartX;
		private float m_zoomDragStartY;
		private float m_zoomDragCurrentX;
		private float m_zoomDragCurrentY;
		private int m_pendingGuideKind;
		private int m_pendingGuidePos;
		private int m_guideDragKind;
		private int m_guideDragIndex;
		private int m_guideStickyState;
		private SKRectI m_guideStickyBox;
		private bool m_guideStickyIsBackground;
		private bool m_hasCursorShape;
		private Microsoft.UI.Input.InputSystemCursorShape m_currentCursorShape;
		private int m_transformHoverKind;
		private static System.Reflection.PropertyInfo s_protectedCursorProp = typeof(Microsoft.UI.Xaml.UIElement).GetProperty("ProtectedCursor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

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

		private bool EnsureComposite()
		{
			int width = m_document.Width();
			int height = m_document.Height();
			if (m_composite != null && m_composite.Width == width && m_composite.Height == height)
			{
				return false;
			}
			if (m_composite != null)
			{
				m_composite.Dispose();
			}
			m_composite = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			return true;
		}

		private SKRectI ClampRegionToCanvas(SKRectI rect)
		{
			int left = rect.Left;
			int top = rect.Top;
			int right = rect.Right;
			int bottom = rect.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > m_document.Width())
			{
				right = m_document.Width();
			}
			if (bottom > m_document.Height())
			{
				bottom = m_document.Height();
			}
			if (right <= left || bottom <= top)
			{
				return SKRectI.Empty;
			}
			return new SKRectI(left, top, right, bottom);
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(Theme.CanvasSurround());

			if (info.Width <= 0 || info.Height <= 0)
			{
				return;
			}

			bool recreated = EnsureComposite();
			if (recreated || m_document.ComposeDirtyAll())
			{
				m_document.CompositeInto(m_composite);
				m_document.ClearComposeDirty();
			}
			else if (m_document.ComposeDirtyAny())
			{
				SKRectI region = ClampRegionToCanvas(m_document.ComposeDirtyRect());
				if (region.Width > 0 && region.Height > 0)
				{
					m_document.CompositeRegion(m_composite, region);
				}
				m_document.ClearComposeDirty();
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
				m_lastViewportWidth = info.Width;
				m_lastViewportHeight = info.Height;
				m_viewInitialized = true;
				ReportZoomInfo();
			}

			bool viewportChanged = info.Width != m_lastViewportWidth || info.Height != m_lastViewportHeight;
			if (viewportChanged)
			{
				float contentWidth = docWidth * m_zoom;
				float contentHeight = docHeight * m_zoom;
				bool fullyVisible = contentWidth <= info.Width && contentHeight <= info.Height;
				if (fullyVisible)
				{
					m_offsetX = (info.Width - contentWidth) / 2.0f;
					m_offsetY = (info.Height - contentHeight) / 2.0f;
				}
				m_lastViewportWidth = info.Width;
				m_lastViewportHeight = info.Height;
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

			SKBitmap displayBitmap = m_composite;
			MainView channelMain = MainView.Self;
			if (channelMain != null && channelMain.ChannelViewMode() >= 0)
			{
				if (m_channelBitmap == null || m_channelBitmap.Width != m_composite.Width || m_channelBitmap.Height != m_composite.Height)
				{
					if (m_channelBitmap != null)
					{
						m_channelBitmap.Dispose();
					}
					m_channelBitmap = new SKBitmap(m_composite.Width, m_composite.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				}
				Bitmute.Imaging.ChannelRender.Render(m_composite, m_channelBitmap, channelMain.ChannelViewMode());
				displayBitmap = m_channelBitmap;
			}
			SKImage image = SKImage.FromBitmap(displayBitmap);
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

			MainView gridMain = MainView.Self;
			if (gridMain != null && gridMain.GridEnabled())
			{
				GridOverlay.Draw(canvas, m_offsetX, m_offsetY, m_zoom, m_document.Width(), m_document.Height(), 16, true);
			}

			DrawGuides(canvas, info.Width, info.Height);
			DrawSelection(canvas);
			DrawToolOverlay(canvas);
		}

		private void DrawGuides(SKCanvas canvas, float viewWidth, float viewHeight)
		{
			Bitmute.Imaging.Guides guides = m_document.Guides();
			SKPaint paint = new SKPaint();
			paint.Style = SKPaintStyle.Stroke;
			paint.StrokeWidth = 1.0f;
			paint.Color = new SKColor(0, 170, 255, 220);
			paint.IsAntialias = false;
			System.Collections.Generic.List<int> verticals = guides.VerticalGuides();
			for (int index = 0; index < verticals.Count; index++)
			{
				float sx = (float)System.Math.Floor(m_offsetX + (verticals[index] * m_zoom)) + 0.5f;
				canvas.DrawLine(sx, 0.0f, sx, viewHeight, paint);
			}
			System.Collections.Generic.List<int> horizontals = guides.HorizontalGuides();
			for (int index = 0; index < horizontals.Count; index++)
			{
				float sy = (float)System.Math.Floor(m_offsetY + (horizontals[index] * m_zoom)) + 0.5f;
				canvas.DrawLine(0.0f, sy, viewWidth, sy, paint);
			}
			if (m_pendingGuideKind != 0)
			{
				SKPaint pendingPaint = new SKPaint();
				pendingPaint.Style = SKPaintStyle.Stroke;
				pendingPaint.StrokeWidth = 1.0f;
				pendingPaint.Color = new SKColor(0, 170, 255, 140);
				pendingPaint.IsAntialias = false;
				if (m_pendingGuideKind == 2)
				{
					float sx = (float)System.Math.Floor(m_offsetX + (m_pendingGuidePos * m_zoom)) + 0.5f;
					canvas.DrawLine(sx, 0.0f, sx, viewHeight, pendingPaint);
				}
				else
				{
					float sy = (float)System.Math.Floor(m_offsetY + (m_pendingGuidePos * m_zoom)) + 0.5f;
					canvas.DrawLine(0.0f, sy, viewWidth, sy, pendingPaint);
				}
				pendingPaint.Dispose();
			}
			paint.Dispose();
		}

		public void SetPendingGuide(int kind, int pos)
		{
			m_pendingGuideKind = kind;
			m_pendingGuidePos = pos;
			InvalidateSurface();
		}

		public void CommitPendingGuide()
		{
			if (m_pendingGuideKind == 0)
			{
				return;
			}
			int kind = m_pendingGuideKind;
			int pos = m_pendingGuidePos;
			m_pendingGuideKind = 0;
			Bitmute.Imaging.Guides guides = m_document.Guides();
			if (kind == 2)
			{
				if (pos >= 0 && pos <= m_document.Width())
				{
					guides.AddVertical(pos);
				}
			}
			else
			{
				if (pos >= 0 && pos <= m_document.Height())
				{
					guides.AddHorizontal(pos);
				}
			}
			InvalidateSurface();
		}

		public void CancelPendingGuide()
		{
			m_pendingGuideKind = 0;
			InvalidateSurface();
		}

		public void UpdatePendingGuideFromDip(int orientation, double dipX, double dipY)
		{
			double scaleX = 1.0;
			double scaleY = 1.0;
			if (Width > 0.0)
			{
				scaleX = CanvasSize.Width / Width;
			}
			if (Height > 0.0)
			{
				scaleY = CanvasSize.Height / Height;
			}
			float deviceX = (float)(dipX * scaleX);
			float deviceY = (float)(dipY * scaleY);
			int docX = (int)System.Math.Round((deviceX - m_offsetX) / m_zoom);
			int docY = (int)System.Math.Round((deviceY - m_offsetY) / m_zoom);
			if (orientation == 1)
			{
				SetPendingGuide(1, SnapGuideToBox(docY, false));
			}
			else
			{
				SetPendingGuide(2, SnapGuideToBox(docX, true));
			}
		}

		private void ApplyCursorShape(Microsoft.UI.Input.InputSystemCursorShape shape)
		{
			if (m_hasCursorShape && m_currentCursorShape == shape)
			{
				return;
			}
			m_currentCursorShape = shape;
			m_hasCursorShape = true;
			if (s_protectedCursorProp == null)
			{
				return;
			}
			if (Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			try
			{
				s_protectedCursorProp.SetValue(element, Microsoft.UI.Input.InputSystemCursor.Create(shape));
			}
			catch (System.Exception)
			{
			}
		}

		private static Microsoft.UI.Input.InputSystemCursorShape TransformCursorShape(int kind)
		{
			if (kind == 1)
			{
				return Microsoft.UI.Input.InputSystemCursorShape.SizeNorthwestSoutheast;
			}
			if (kind == 2)
			{
				return Microsoft.UI.Input.InputSystemCursorShape.SizeNortheastSouthwest;
			}
			if (kind == 3)
			{
				return Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast;
			}
			if (kind == 4)
			{
				return Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth;
			}
			if (kind == 5)
			{
				return Microsoft.UI.Input.InputSystemCursorShape.Arrow;
			}
			if (kind == 6)
			{
				return Microsoft.UI.Input.InputSystemCursorShape.SizeAll;
			}
			return Microsoft.UI.Input.InputSystemCursorShape.Arrow;
		}

		private void UpdateHoverCursor(Tool tool, int pixelX, int pixelY)
		{
			m_transformHoverKind = 0;
			Microsoft.UI.Input.InputSystemCursorShape desired = Microsoft.UI.Input.InputSystemCursorShape.Arrow;
			Bitmute.Imaging.Guides guides = m_document.Guides();
			if (!guides.IsLocked())
			{
				int tolerance = (int)System.Math.Ceiling(6.0 / m_zoom);
				if (tolerance < 4)
				{
					tolerance = 4;
				}
				if (guides.HitVertical(pixelX, tolerance) >= 0)
				{
					desired = Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast;
				}
				else if (guides.HitHorizontal(pixelY, tolerance) >= 0)
				{
					desired = Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth;
				}
			}
			if (desired == Microsoft.UI.Input.InputSystemCursorShape.Arrow && tool is FreeTransformTool)
			{
				int kind = ((FreeTransformTool)tool).HitTestKind(pixelX, pixelY);
				m_transformHoverKind = kind;
				desired = TransformCursorShape(kind);
			}
			ApplyCursorShape(desired);
		}

		public void SyncDocumentSize()
		{
			if (m_composite == null)
			{
				return;
			}
			if (m_composite.Width != m_document.Width() || m_composite.Height != m_document.Height())
			{
				ResetView();
			}
		}

		private void DrawToolOverlay(SKCanvas canvas)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			if (main.IsTextEditActive() && ReferenceEquals(main.TextEditCanvas(), this))
			{
				DrawTextEditOverlay(canvas, main);
				return;
			}
			Tool tool = main.CurrentTool();
			if (tool is LineTool)
			{
				DrawLinePreview(canvas, (LineTool)tool);
				return;
			}
			if (tool is LassoTool)
			{
				DrawLassoPreview(canvas, (LassoTool)tool);
				return;
			}
			if (tool is FreehandLassoTool)
			{
				DrawFreehandLassoPreview(canvas, (FreehandLassoTool)tool);
				return;
			}
			if (tool is MagneticLassoTool)
			{
				DrawMagneticLassoPreview(canvas, (MagneticLassoTool)tool);
				return;
			}
			if (tool is CropTool)
			{
				DrawCropPreview(canvas, (CropTool)tool);
				return;
			}
			if (tool is SliceTool)
			{
				DrawSlices(canvas, (SliceTool)tool);
				return;
			}
			if (tool is FreeTransformTool)
			{
				DrawTransformPreview(canvas, (FreeTransformTool)tool);
				return;
			}
			if (tool is RulerTool)
			{
				DrawRulerPreview(canvas, (RulerTool)tool);
				return;
			}
			if (tool is GradientTool)
			{
				DrawGradientPreview(canvas, (GradientTool)tool);
				return;
			}
			if (tool is ShapeTool)
			{
				DrawShapePreview(canvas, (ShapeTool)tool);
				return;
			}
			if (tool is ZoomTool && m_zoomDragging)
			{
				DrawZoomMarquee(canvas);
				return;
			}
			if (m_cursorInside && ShowsBrushCursor(tool))
			{
				DrawBrushCursor(canvas, main);
			}
		}

		private void DrawShapePreview(SKCanvas canvas, ShapeTool shape)
		{
			if (!shape.HasPreview())
			{
				return;
			}
			float x0 = m_offsetX + (shape.PreviewStartX() * m_zoom);
			float y0 = m_offsetY + (shape.PreviewStartY() * m_zoom);
			float x1 = m_offsetX + (shape.PreviewEndX() * m_zoom);
			float y1 = m_offsetY + (shape.PreviewEndY() * m_zoom);
			float left = x0;
			if (x1 < left)
			{
				left = x1;
			}
			float top = y0;
			if (y1 < top)
			{
				top = y1;
			}
			float right = x0 + x1 - left;
			float bottom = y0 + y1 - top;
			SKRect rect = new SKRect(left, top, right, bottom);
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			eShapeKind kind = shape.Kind();
			if (kind == eShapeKind.Ellipse)
			{
				canvas.DrawOval(rect, underlay);
				canvas.DrawOval(rect, overlay);
			}
			else if (kind == eShapeKind.RoundedRectangle)
			{
				float radius = (rect.Right - rect.Left);
				if ((rect.Bottom - rect.Top) < radius)
				{
					radius = rect.Bottom - rect.Top;
				}
				radius = radius * 0.2f;
				canvas.DrawRoundRect(rect, radius, radius, underlay);
				canvas.DrawRoundRect(rect, radius, radius, overlay);
			}
			else if (kind == eShapeKind.Polygon)
			{
				SKPath underPath = ShapeTool.BuildPolygon(rect, ShapeTool.Sides());
				canvas.DrawPath(underPath, underlay);
				underPath.Dispose();
				SKPath overPath = ShapeTool.BuildPolygon(rect, ShapeTool.Sides());
				canvas.DrawPath(overPath, overlay);
				overPath.Dispose();
			}
			else
			{
				canvas.DrawRect(rect, underlay);
				canvas.DrawRect(rect, overlay);
			}
			underlay.Dispose();
			overlay.Dispose();
		}

		private void DrawGradientPreview(SKCanvas canvas, GradientTool gradient)
		{
			if (!gradient.HasPreview())
			{
				return;
			}
			float startX = m_offsetX + (gradient.PreviewStartX() * m_zoom);
			float startY = m_offsetY + (gradient.PreviewStartY() * m_zoom);
			float endX = m_offsetX + (gradient.PreviewEndX() * m_zoom);
			float endY = m_offsetY + (gradient.PreviewEndY() * m_zoom);
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawLine(startX, startY, endX, endY, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawLine(startX, startY, endX, endY, overlay);
			canvas.DrawCircle(startX, startY, 3.0f, overlay);
			canvas.DrawCircle(endX, endY, 3.0f, overlay);
			overlay.Dispose();
		}

		private void DrawZoomMarquee(SKCanvas canvas)
		{
			float left = m_zoomDragStartX;
			if (m_zoomDragCurrentX < left)
			{
				left = m_zoomDragCurrentX;
			}
			float top = m_zoomDragStartY;
			if (m_zoomDragCurrentY < top)
			{
				top = m_zoomDragCurrentY;
			}
			float right = m_zoomDragStartX + m_zoomDragCurrentX - left;
			float bottom = m_zoomDragStartY + m_zoomDragCurrentY - top;
			SKRect rect = new SKRect(left, top, right, bottom);
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 2.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = false;
			canvas.DrawRect(rect, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = false;
			canvas.DrawRect(rect, overlay);
			overlay.Dispose();
		}

		private bool ShowsBrushCursor(Tool tool)
		{
			return tool is BrushFamilyTool;
		}

		private void DrawTextEditOverlay(SKCanvas canvas, MainView main)
		{
			Bitmute.Imaging.Layer layer = main.TextEditLayer();
			if (layer == null)
			{
				return;
			}
			int selectionStart = main.TextSelectionStart();
			int selectionLength = main.TextSelectionLength();
			if (selectionLength > 0)
			{
				System.Collections.Generic.List<SKRect> runs = TextRasterizer.MeasureSelectionRuns(layer, selectionStart, selectionStart + selectionLength);
				SKPaint highlight = new SKPaint();
				highlight.Style = SKPaintStyle.Fill;
				highlight.Color = new SKColor(0x33, 0x99, 0xFF, 0x66);
				highlight.IsAntialias = false;
				for (int index = 0; index < runs.Count; index++)
				{
					SKRect run = runs[index];
					SKRect screen = new SKRect(m_offsetX + (run.Left * m_zoom), m_offsetY + (run.Top * m_zoom), m_offsetX + (run.Right * m_zoom), m_offsetY + (run.Bottom * m_zoom));
					canvas.DrawRect(screen, highlight);
				}
				highlight.Dispose();
			}
			if (main.CaretVisible())
			{
				int caretIndex = main.TextCaretIndex();
				float caretX;
				float caretY;
				float caretHeight;
				TextRasterizer.MeasureCaret(layer, caretIndex, out caretX, out caretY, out caretHeight);
				float screenX = m_offsetX + (caretX * m_zoom);
				float screenTop = m_offsetY + (caretY * m_zoom);
				float screenBottom = m_offsetY + ((caretY + caretHeight) * m_zoom);
				SKPaint caretUnder = new SKPaint();
				caretUnder.Style = SKPaintStyle.Stroke;
				caretUnder.StrokeWidth = 3.0f;
				caretUnder.Color = SKColors.White;
				caretUnder.IsAntialias = false;
				canvas.DrawLine(screenX, screenTop, screenX, screenBottom, caretUnder);
				caretUnder.Dispose();
				SKPaint caretPaint = new SKPaint();
				caretPaint.Style = SKPaintStyle.Stroke;
				caretPaint.StrokeWidth = 1.0f;
				caretPaint.Color = SKColors.Black;
				caretPaint.IsAntialias = false;
				canvas.DrawLine(screenX, screenTop, screenX, screenBottom, caretPaint);
				caretPaint.Dispose();
			}
		}

		private void DrawBrushCursor(SKCanvas canvas, MainView main)
		{
			ToolState state = main.CurrentToolState();
			if (state == null)
			{
				return;
			}
			float radius = (state.BrushSize() * m_zoom) / 2.0f;
			if (radius < 1.0f)
			{
				radius = 1.0f;
			}
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 2.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawCircle(m_cursorDeviceX, m_cursorDeviceY, radius, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawCircle(m_cursorDeviceX, m_cursorDeviceY, radius, overlay);
			overlay.Dispose();
		}

		private void DrawLassoPreview(SKCanvas canvas, LassoTool lasso)
		{
			if (!lasso.HasPreview())
			{
				return;
			}
			int count = lasso.VertexCount();
			if (count >= 2)
			{
				SKPathBuilder builder = new SKPathBuilder();
				builder.MoveTo(m_offsetX + (lasso.VertexX(0) * m_zoom), m_offsetY + (lasso.VertexY(0) * m_zoom));
				for (int index = 1; index < count; index++)
				{
					builder.LineTo(m_offsetX + (lasso.VertexX(index) * m_zoom), m_offsetY + (lasso.VertexY(index) * m_zoom));
				}
				SKPath path = builder.Snapshot();
				SKPaint underlay = new SKPaint();
				underlay.Style = SKPaintStyle.Stroke;
				underlay.StrokeWidth = 3.0f;
				underlay.Color = SKColors.Black;
				underlay.IsAntialias = true;
				canvas.DrawPath(path, underlay);
				underlay.Dispose();
				SKPaint overlay = new SKPaint();
				overlay.Style = SKPaintStyle.Stroke;
				overlay.StrokeWidth = 1.0f;
				overlay.Color = SKColors.White;
				overlay.IsAntialias = true;
				canvas.DrawPath(path, overlay);
				overlay.Dispose();
				path.Dispose();
				builder.Dispose();
			}
			SKPaint markerFill = new SKPaint();
			markerFill.Style = SKPaintStyle.Fill;
			markerFill.Color = SKColors.White;
			markerFill.IsAntialias = false;
			SKPaint markerBorder = new SKPaint();
			markerBorder.Style = SKPaintStyle.Stroke;
			markerBorder.StrokeWidth = 1.0f;
			markerBorder.Color = SKColors.Black;
			markerBorder.IsAntialias = false;
			for (int index = 0; index < count; index++)
			{
				float centerX = m_offsetX + (lasso.VertexX(index) * m_zoom);
				float centerY = m_offsetY + (lasso.VertexY(index) * m_zoom);
				SKRect marker = new SKRect(centerX - 2.5f, centerY - 2.5f, centerX + 2.5f, centerY + 2.5f);
				canvas.DrawRect(marker, markerFill);
				canvas.DrawRect(marker, markerBorder);
			}
			markerFill.Dispose();
			markerBorder.Dispose();
		}

		private void DrawFreehandLassoPreview(SKCanvas canvas, FreehandLassoTool lasso)
		{
			if (!lasso.HasPreview())
			{
				return;
			}
			int count = lasso.VertexCount();
			if (count < 2)
			{
				return;
			}
			SKPathBuilder builder = new SKPathBuilder();
			builder.MoveTo(m_offsetX + (lasso.VertexX(0) * m_zoom), m_offsetY + (lasso.VertexY(0) * m_zoom));
			for (int index = 1; index < count; index++)
			{
				builder.LineTo(m_offsetX + (lasso.VertexX(index) * m_zoom), m_offsetY + (lasso.VertexY(index) * m_zoom));
			}
			SKPath path = builder.Snapshot();
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawPath(path, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawPath(path, overlay);
			overlay.Dispose();
			path.Dispose();
			builder.Dispose();
		}

		private void DrawCropPreview(SKCanvas canvas, CropTool crop)
		{
			if (!crop.HasPreview())
			{
				return;
			}
			float left = m_offsetX + (crop.RectLeft() * m_zoom);
			float top = m_offsetY + (crop.RectTop() * m_zoom);
			float right = m_offsetX + (crop.RectRight() * m_zoom);
			float bottom = m_offsetY + (crop.RectBottom() * m_zoom);
			float docLeft = m_offsetX;
			float docTop = m_offsetY;
			float docRight = m_offsetX + (m_document.Width() * m_zoom);
			float docBottom = m_offsetY + (m_document.Height() * m_zoom);
			SKPaint shade = new SKPaint();
			shade.Style = SKPaintStyle.Fill;
			shade.Color = new SKColor(0, 0, 0, 110);
			canvas.DrawRect(new SKRect(docLeft, docTop, docRight, top), shade);
			canvas.DrawRect(new SKRect(docLeft, bottom, docRight, docBottom), shade);
			canvas.DrawRect(new SKRect(docLeft, top, left, bottom), shade);
			canvas.DrawRect(new SKRect(right, top, docRight, bottom), shade);
			shade.Dispose();
			SKRect rect = new SKRect(left, top, right, bottom);
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawRect(rect, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawRect(rect, overlay);
			SKPaint handleFill = new SKPaint();
			handleFill.Style = SKPaintStyle.Fill;
			handleFill.Color = SKColors.White;
			handleFill.IsAntialias = true;
			SKPaint handleBorder = new SKPaint();
			handleBorder.Style = SKPaintStyle.Stroke;
			handleBorder.StrokeWidth = 1.0f;
			handleBorder.Color = SKColors.Black;
			handleBorder.IsAntialias = true;
			float handleSize = 3.5f;
			canvas.DrawRect(new SKRect(left - handleSize, top - handleSize, left + handleSize, top + handleSize), handleFill);
			canvas.DrawRect(new SKRect(left - handleSize, top - handleSize, left + handleSize, top + handleSize), handleBorder);
			canvas.DrawRect(new SKRect(right - handleSize, top - handleSize, right + handleSize, top + handleSize), handleFill);
			canvas.DrawRect(new SKRect(right - handleSize, top - handleSize, right + handleSize, top + handleSize), handleBorder);
			canvas.DrawRect(new SKRect(left - handleSize, bottom - handleSize, left + handleSize, bottom + handleSize), handleFill);
			canvas.DrawRect(new SKRect(left - handleSize, bottom - handleSize, left + handleSize, bottom + handleSize), handleBorder);
			canvas.DrawRect(new SKRect(right - handleSize, bottom - handleSize, right + handleSize, bottom + handleSize), handleFill);
			canvas.DrawRect(new SKRect(right - handleSize, bottom - handleSize, right + handleSize, bottom + handleSize), handleBorder);
			handleFill.Dispose();
			handleBorder.Dispose();
			overlay.Dispose();
		}

		private void DrawMagneticLassoPreview(SKCanvas canvas, MagneticLassoTool lasso)
		{
			if (!lasso.HasPreview())
			{
				return;
			}
			int count = lasso.VertexCount();
			if (count < 2)
			{
				return;
			}
			SKPathBuilder builder = new SKPathBuilder();
			builder.MoveTo(m_offsetX + (lasso.VertexX(0) * m_zoom), m_offsetY + (lasso.VertexY(0) * m_zoom));
			for (int index = 1; index < count; index++)
			{
				builder.LineTo(m_offsetX + (lasso.VertexX(index) * m_zoom), m_offsetY + (lasso.VertexY(index) * m_zoom));
			}
			SKPath path = builder.Snapshot();
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawPath(path, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawPath(path, overlay);
			overlay.Dispose();
			path.Dispose();
			builder.Dispose();
		}

		private void DrawTransformPreview(SKCanvas canvas, FreeTransformTool transform)
		{
			if (!transform.HasPreview())
			{
				return;
			}
			float x0 = m_offsetX + (float)(transform.CornerX(0) * m_zoom);
			float y0 = m_offsetY + (float)(transform.CornerY(0) * m_zoom);
			float x1 = m_offsetX + (float)(transform.CornerX(1) * m_zoom);
			float y1 = m_offsetY + (float)(transform.CornerY(1) * m_zoom);
			float x2 = m_offsetX + (float)(transform.CornerX(2) * m_zoom);
			float y2 = m_offsetY + (float)(transform.CornerY(2) * m_zoom);
			float x3 = m_offsetX + (float)(transform.CornerX(3) * m_zoom);
			float y3 = m_offsetY + (float)(transform.CornerY(3) * m_zoom);
			SKBitmap previewBitmap = transform.PreviewBitmap();
			if (previewBitmap != null && previewBitmap.Width > 0 && previewBitmap.Height > 0)
			{
				SKPoint[] screenQuad = new SKPoint[4];
				screenQuad[0] = new SKPoint(x0, y0);
				screenQuad[1] = new SKPoint(x1, y1);
				screenQuad[2] = new SKPoint(x2, y2);
				screenQuad[3] = new SKPoint(x3, y3);
				SKMatrix quadMatrix;
				if (Bitmute.Imaging.TransformMath.QuadMatrix(screenQuad, previewBitmap.Width, previewBitmap.Height, out quadMatrix))
				{
					SKPixmap previewPixmap = previewBitmap.PeekPixels();
					SKImage previewImage = SKImage.FromPixels(previewPixmap);
					SKPaint previewPaint = new SKPaint();
					SKSamplingOptions previewSampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
					canvas.Save();
					canvas.SetMatrix(quadMatrix);
					canvas.DrawImage(previewImage, 0.0f, 0.0f, previewSampling, previewPaint);
					canvas.Restore();
					previewPaint.Dispose();
					previewImage.Dispose();
					previewPixmap.Dispose();
				}
			}
			int aboveStart = transform.LayerIndex() + 1;
			int layerCount = m_document.Layers().Count;
			if (transform.LayerIndex() >= 0 && aboveStart < layerCount)
			{
				DrawTransformAboveLayers(canvas, aboveStart, layerCount, x0, y0, x1, y1, x2, y2, x3, y3);
			}
			SKPathBuilder builder = new SKPathBuilder();
			builder.MoveTo(x0, y0);
			builder.LineTo(x1, y1);
			builder.LineTo(x2, y2);
			builder.LineTo(x3, y3);
			builder.Close();
			SKPath path = builder.Snapshot();
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawPath(path, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawPath(path, overlay);
			path.Dispose();
			builder.Dispose();
			SKPaint handleFill = new SKPaint();
			handleFill.Style = SKPaintStyle.Fill;
			handleFill.Color = SKColors.White;
			handleFill.IsAntialias = true;
			SKPaint handleBorder = new SKPaint();
			handleBorder.Style = SKPaintStyle.Stroke;
			handleBorder.StrokeWidth = 1.0f;
			handleBorder.Color = SKColors.Black;
			handleBorder.IsAntialias = true;
			float handleSize = 3.5f;
			DrawTransformHandle(canvas, x0, y0, handleSize, handleFill, handleBorder);
			DrawTransformHandle(canvas, x1, y1, handleSize, handleFill, handleBorder);
			DrawTransformHandle(canvas, x2, y2, handleSize, handleFill, handleBorder);
			DrawTransformHandle(canvas, x3, y3, handleSize, handleFill, handleBorder);
			DrawTransformHandle(canvas, (x0 + x1) / 2.0f, (y0 + y1) / 2.0f, handleSize, handleFill, handleBorder);
			DrawTransformHandle(canvas, (x1 + x2) / 2.0f, (y1 + y2) / 2.0f, handleSize, handleFill, handleBorder);
			DrawTransformHandle(canvas, (x2 + x3) / 2.0f, (y2 + y3) / 2.0f, handleSize, handleFill, handleBorder);
			DrawTransformHandle(canvas, (x3 + x0) / 2.0f, (y3 + y0) / 2.0f, handleSize, handleFill, handleBorder);
			handleFill.Dispose();
			handleBorder.Dispose();
			if (m_transformHoverKind == 5 && m_cursorInside)
			{
				DrawRotateCursor(canvas, m_cursorDeviceX, m_cursorDeviceY);
			}
		}

		private void DrawRotateCursor(SKCanvas canvas, float cx, float cy)
		{
			float radius = 10.0f;
			float startDegrees = 40.0f;
			float sweepDegrees = 280.0f;
			SKRect oval = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);
			SKPathBuilder arcBuilder = new SKPathBuilder();
			arcBuilder.AddArc(oval, startDegrees, sweepDegrees);
			SKPath arc = arcBuilder.Snapshot();
			double startRadians = startDegrees * System.Math.PI / 180.0;
			double endRadians = (startDegrees + sweepDegrees) * System.Math.PI / 180.0;
			float startX = cx + (radius * (float)System.Math.Cos(startRadians));
			float startY = cy + (radius * (float)System.Math.Sin(startRadians));
			float endX = cx + (radius * (float)System.Math.Cos(endRadians));
			float endY = cy + (radius * (float)System.Math.Sin(endRadians));
			float startTangentX = (float)System.Math.Sin(startRadians);
			float startTangentY = -(float)System.Math.Cos(startRadians);
			float endTangentX = -(float)System.Math.Sin(endRadians);
			float endTangentY = (float)System.Math.Cos(endRadians);
			SKPath startHead = BuildArrowHead(startX, startY, startTangentX, startTangentY);
			SKPath endHead = BuildArrowHead(endX, endY, endTangentX, endTangentY);
			SKPaint arcUnder = new SKPaint();
			arcUnder.Style = SKPaintStyle.Stroke;
			arcUnder.StrokeWidth = 3.0f;
			arcUnder.Color = SKColors.Black;
			arcUnder.IsAntialias = true;
			canvas.DrawPath(arc, arcUnder);
			arcUnder.Dispose();
			SKPaint headUnder = new SKPaint();
			headUnder.Style = SKPaintStyle.Fill;
			headUnder.Color = SKColors.Black;
			headUnder.IsAntialias = true;
			SKPaint headUnderStroke = new SKPaint();
			headUnderStroke.Style = SKPaintStyle.Stroke;
			headUnderStroke.StrokeWidth = 3.0f;
			headUnderStroke.Color = SKColors.Black;
			headUnderStroke.IsAntialias = true;
			canvas.DrawPath(startHead, headUnderStroke);
			canvas.DrawPath(endHead, headUnderStroke);
			canvas.DrawPath(startHead, headUnder);
			canvas.DrawPath(endHead, headUnder);
			headUnderStroke.Dispose();
			SKPaint arcOver = new SKPaint();
			arcOver.Style = SKPaintStyle.Stroke;
			arcOver.StrokeWidth = 1.5f;
			arcOver.Color = SKColors.White;
			arcOver.IsAntialias = true;
			canvas.DrawPath(arc, arcOver);
			arcOver.Dispose();
			SKPaint headOver = new SKPaint();
			headOver.Style = SKPaintStyle.Fill;
			headOver.Color = SKColors.White;
			headOver.IsAntialias = true;
			canvas.DrawPath(startHead, headOver);
			canvas.DrawPath(endHead, headOver);
			headOver.Dispose();
			headUnder.Dispose();
			arc.Dispose();
			startHead.Dispose();
			endHead.Dispose();
		}

		private SKPath BuildArrowHead(float tipX, float tipY, float directionX, float directionY)
		{
			float length = (float)System.Math.Sqrt((directionX * directionX) + (directionY * directionY));
			if (length < 0.0001f)
			{
				length = 1.0f;
			}
			float dirX = directionX / length;
			float dirY = directionY / length;
			float perpX = -dirY;
			float perpY = dirX;
			float headLength = 6.0f;
			float headHalfWidth = 3.5f;
			float baseX = tipX - (dirX * headLength);
			float baseY = tipY - (dirY * headLength);
			float leftX = baseX + (perpX * headHalfWidth);
			float leftY = baseY + (perpY * headHalfWidth);
			float rightX = baseX - (perpX * headHalfWidth);
			float rightY = baseY - (perpY * headHalfWidth);
			SKPathBuilder builder = new SKPathBuilder();
			builder.MoveTo(tipX, tipY);
			builder.LineTo(leftX, leftY);
			builder.LineTo(rightX, rightY);
			builder.Close();
			return builder.Snapshot();
		}

		private void DrawTransformAboveLayers(SKCanvas canvas, int startIndex, int endExclusive, float sx0, float sy0, float sx1, float sy1, float sx2, float sy2, float sx3, float sy3)
		{
			int docWidth = m_document.Width();
			int docHeight = m_document.Height();
			if (m_transformAbove == null || m_transformAbove.Width != docWidth || m_transformAbove.Height != docHeight)
			{
				if (m_transformAbove != null)
				{
					m_transformAbove.Dispose();
				}
				m_transformAbove = new SKBitmap(docWidth, docHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
			}
			float minX = System.Math.Min(System.Math.Min(sx0, sx1), System.Math.Min(sx2, sx3));
			float maxX = System.Math.Max(System.Math.Max(sx0, sx1), System.Math.Max(sx2, sx3));
			float minY = System.Math.Min(System.Math.Min(sy0, sy1), System.Math.Min(sy2, sy3));
			float maxY = System.Math.Max(System.Math.Max(sy0, sy1), System.Math.Max(sy2, sy3));
			int docLeft = (int)System.Math.Floor((minX - m_offsetX) / m_zoom) - 1;
			int docTop = (int)System.Math.Floor((minY - m_offsetY) / m_zoom) - 1;
			int docRight = (int)System.Math.Ceiling((maxX - m_offsetX) / m_zoom) + 1;
			int docBottom = (int)System.Math.Ceiling((maxY - m_offsetY) / m_zoom) + 1;
			if (docLeft < 0)
			{
				docLeft = 0;
			}
			if (docTop < 0)
			{
				docTop = 0;
			}
			if (docRight > docWidth)
			{
				docRight = docWidth;
			}
			if (docBottom > docHeight)
			{
				docBottom = docHeight;
			}
			if (docRight <= docLeft || docBottom <= docTop)
			{
				return;
			}
			SKRectI region = new SKRectI(docLeft, docTop, docRight, docBottom);
			m_document.CompositeRangeInto(m_transformAbove, region, startIndex, endExclusive);
			SKPixmap pixmap = m_transformAbove.PeekPixels();
			SKImage image = SKImage.FromPixels(pixmap);
			SKRect srcRect = new SKRect(docLeft, docTop, docRight, docBottom);
			SKRect dstRect = new SKRect(m_offsetX + (docLeft * m_zoom), m_offsetY + (docTop * m_zoom), m_offsetX + (docRight * m_zoom), m_offsetY + (docBottom * m_zoom));
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			canvas.DrawImage(image, srcRect, dstRect, sampling, paint);
			paint.Dispose();
			image.Dispose();
			pixmap.Dispose();
		}

		private void DrawTransformHandle(SKCanvas canvas, float cx, float cy, float size, SKPaint fill, SKPaint border)
		{
			SKRect rect = new SKRect(cx - size, cy - size, cx + size, cy + size);
			canvas.DrawRect(rect, fill);
			canvas.DrawRect(rect, border);
		}

		private void DrawSlices(SKCanvas canvas, SliceTool slice)
		{
			Bitmute.Imaging.Slices slices = m_document.Slices();
			SKPaint border = new SKPaint();
			border.Style = SKPaintStyle.Stroke;
			border.StrokeWidth = 1.0f;
			border.Color = new SKColor(255, 200, 0, 230);
			border.IsAntialias = false;
			SKPaint fill = new SKPaint();
			fill.Style = SKPaintStyle.Fill;
			fill.Color = new SKColor(255, 200, 0, 24);
			SKPaint labelPaint = new SKPaint();
			labelPaint.Color = new SKColor(255, 200, 0, 255);
			labelPaint.IsAntialias = true;
			SKFont labelFont = new SKFont();
			labelFont.Size = 11.0f;
			for (int index = 0; index < slices.Count(); index++)
			{
				SKRectI rect = slices.RectAt(index);
				float left = m_offsetX + (rect.Left * m_zoom);
				float top = m_offsetY + (rect.Top * m_zoom);
				float right = m_offsetX + (rect.Right * m_zoom);
				float bottom = m_offsetY + (rect.Bottom * m_zoom);
				SKRect screen = new SKRect(left, top, right, bottom);
				canvas.DrawRect(screen, fill);
				canvas.DrawRect(screen, border);
				canvas.DrawText(slices.NameAt(index), left + 3.0f, top + 13.0f, SKTextAlign.Left, labelFont, labelPaint);
			}
			if (slice.HasPreview())
			{
				float previewLeft = m_offsetX + (slice.PreviewLeft() * m_zoom);
				float previewTop = m_offsetY + (slice.PreviewTop() * m_zoom);
				float previewRight = m_offsetX + (slice.PreviewRight() * m_zoom);
				float previewBottom = m_offsetY + (slice.PreviewBottom() * m_zoom);
				SKRect pending = new SKRect(previewLeft, previewTop, previewRight, previewBottom);
				canvas.DrawRect(pending, fill);
				canvas.DrawRect(pending, border);
			}
			labelFont.Dispose();
			labelPaint.Dispose();
			fill.Dispose();
			border.Dispose();
		}

		private void DrawRulerPreview(SKCanvas canvas, RulerTool ruler)
		{
			if (!ruler.HasPreview())
			{
				return;
			}
			float startX = m_offsetX + (ruler.PreviewStartX() * m_zoom);
			float startY = m_offsetY + (ruler.PreviewStartY() * m_zoom);
			float endX = m_offsetX + (ruler.PreviewEndX() * m_zoom);
			float endY = m_offsetY + (ruler.PreviewEndY() * m_zoom);
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawLine(startX, startY, endX, endY, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawLine(startX, startY, endX, endY, overlay);
			canvas.DrawCircle(startX, startY, 3.0f, overlay);
			canvas.DrawCircle(endX, endY, 3.0f, overlay);
			overlay.Dispose();
		}

		private void DrawLinePreview(SKCanvas canvas, LineTool line)
		{
			if (!line.HasPreview())
			{
				return;
			}
			float startX = m_offsetX + (line.PreviewStartX() * m_zoom);
			float startY = m_offsetY + (line.PreviewStartY() * m_zoom);
			float endX = m_offsetX + (line.PreviewEndX() * m_zoom);
			float endY = m_offsetY + (line.PreviewEndY() * m_zoom);
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawLine(startX, startY, endX, endY, underlay);
			underlay.Dispose();
			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.0f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawLine(startX, startY, endX, endY, overlay);
			overlay.Dispose();
		}

		private void DrawSelection(SKCanvas canvas)
		{
			Selection selection = m_document.Selection();
			if (!selection.IsActive())
			{
				return;
			}
			SKRectI bounds = selection.Bounds();
			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return;
			}

			if (selection.Generation() != m_antEdgesGeneration)
			{
				RebuildAntEdges(selection, bounds);
				m_antEdgesGeneration = selection.Generation();
			}

			SKPathBuilder blackBuilder = new SKPathBuilder();
			SKPathBuilder whiteBuilder = new SKPathBuilder();
			for (int index = 0; index < m_antEdges.Count; index++)
			{
				AntEdge edge = m_antEdges[index];
				if (edge.m_vertical)
				{
					float fixedScreen = m_offsetX + (edge.m_fixedCoord * m_zoom);
					float startScreen = m_offsetY + (edge.m_startCoord * m_zoom);
					float endScreen = m_offsetY + ((edge.m_startCoord + 1) * m_zoom);
					AddAntEdge(blackBuilder, whiteBuilder, true, fixedScreen, startScreen, endScreen);
				}
				else
				{
					float fixedScreen = m_offsetY + (edge.m_fixedCoord * m_zoom);
					float startScreen = m_offsetX + (edge.m_startCoord * m_zoom);
					float endScreen = m_offsetX + ((edge.m_startCoord + 1) * m_zoom);
					AddAntEdge(blackBuilder, whiteBuilder, false, fixedScreen, startScreen, endScreen);
				}
			}

			SKPath blackPath = blackBuilder.Snapshot();
			SKPath whitePath = whiteBuilder.Snapshot();
			SKPaint blackPaint = new SKPaint();
			blackPaint.Style = SKPaintStyle.Stroke;
			blackPaint.StrokeWidth = AntStrokeWidth;
			blackPaint.Color = SKColors.Black;
			blackPaint.IsAntialias = false;
			canvas.DrawPath(blackPath, blackPaint);
			SKPaint whitePaint = new SKPaint();
			whitePaint.Style = SKPaintStyle.Stroke;
			whitePaint.StrokeWidth = AntStrokeWidth;
			whitePaint.Color = SKColors.White;
			whitePaint.IsAntialias = false;
			canvas.DrawPath(whitePath, whitePaint);
			blackPaint.Dispose();
			whitePaint.Dispose();
			blackPath.Dispose();
			whitePath.Dispose();
			blackBuilder.Dispose();
			whiteBuilder.Dispose();
		}

		private void RebuildAntEdges(Selection selection, SKRectI bounds)
		{
			m_antEdges.Clear();
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					if (!selection.IsSelected(x, y))
					{
						continue;
					}
					if (!selection.IsSelected(x - 1, y))
					{
						m_antEdges.Add(new AntEdge(true, x, y));
					}
					if (!selection.IsSelected(x + 1, y))
					{
						m_antEdges.Add(new AntEdge(true, x + 1, y));
					}
					if (!selection.IsSelected(x, y - 1))
					{
						m_antEdges.Add(new AntEdge(false, y, x));
					}
					if (!selection.IsSelected(x, y + 1))
					{
						m_antEdges.Add(new AntEdge(false, y + 1, x));
					}
				}
			}
		}

		private void AddAntEdge(SKPathBuilder blackBuilder, SKPathBuilder whiteBuilder, bool vertical, float fixedCoord, float start, float end)
		{
			float snapped = (float)System.Math.Floor(fixedCoord) + 0.5f;
			float position = start;
			for (;;)
			{
				if (position >= end)
				{
					break;
				}
				float shifted = position + m_antPhase;
				int band = (int)System.Math.Floor(shifted / AntLength);
				float segmentEnd = ((band + 1) * AntLength) - m_antPhase;
				if (segmentEnd > end)
				{
					segmentEnd = end;
				}
				SKPathBuilder target = whiteBuilder;
				if ((band & 1) == 0)
				{
					target = blackBuilder;
				}
				if (vertical)
				{
					target.MoveTo(snapped, position);
					target.LineTo(snapped, segmentEnd);
				}
				else
				{
					target.MoveTo(position, snapped);
					target.LineTo(segmentEnd, snapped);
				}
				position = segmentEnd;
			}
		}

		public CanvasView(Document document)
		{
			m_document = document;
			m_zoom = 1.0f;
			m_offsetX = 0.0f;
			m_offsetY = 0.0f;
			m_viewInitialized = false;
			m_wheelHooked = false;
			m_antPhase = 0.0f;
			m_antEdges = new List<AntEdge>();
			m_antEdgesGeneration = -1;
			PaintSurface += OnPaintSurface;
			EnableTouchEvents = true;
			Touch += OnTouch;
			SizeChanged += OnSizeChanged;
			Theme.Changed += OnThemeChanged;
		}

		private void OnSizeChanged(object sender, System.EventArgs eventArgs)
		{
			NotifyChrome();
		}

		private void OnThemeChanged(object sender, System.EventArgs eventArgs)
		{
			InvalidateSurface();
			NotifyChrome();
			if (Dispatcher != null)
			{
				Dispatcher.Dispatch(RepaintForTheme);
				Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(60.0), RepaintForTheme);
			}
		}

		private void RepaintForTheme()
		{
			MarkComposeDirty();
			InvalidateSurface();
			NotifyChrome();
		}

		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();
			if (m_antTimer == null && Dispatcher != null)
			{
				m_antTimer = Dispatcher.CreateTimer();
				m_antTimer.Interval = TimeSpan.FromMilliseconds(90.0);
				m_antTimer.Tick += OnAntTick;
				m_antTimer.Start();
			}
			if (m_wheelHooked)
			{
				return;
			}
			if (Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			element.PointerWheelChanged += OnPointerWheel;
			m_wheelHooked = true;
		}

		private void OnPointerWheel(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs eventArgs)
		{
			Microsoft.UI.Xaml.UIElement element = sender as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			Microsoft.UI.Input.PointerPoint point = eventArgs.GetCurrentPoint(element);
			int delta = point.Properties.MouseWheelDelta;
			Windows.System.VirtualKeyModifiers modifiers = eventArgs.KeyModifiers;
			bool control = (modifiers & Windows.System.VirtualKeyModifiers.Control) == Windows.System.VirtualKeyModifiers.Control;
			bool alt = (modifiers & Windows.System.VirtualKeyModifiers.Menu) == Windows.System.VirtualKeyModifiers.Menu;

			if (alt)
			{
				float scale = 1.0f;
				float actualWidth = element.ActualSize.X;
				if (actualWidth > 0.0f)
				{
					scale = (float)CanvasSize.Width / actualWidth;
				}
				float anchorX = (float)point.Position.X * scale;
				float anchorY = (float)point.Position.Y * scale;
				float factor = 0.87f;
				if (delta > 0)
				{
					factor = 1.15f;
				}
				ApplyZoomAt(m_zoom * factor, anchorX, anchorY);
			}
			else if (control)
			{
				SetPanOffsetX(m_offsetX + (delta * 0.5f));
			}
			else
			{
				SetPanOffsetY(m_offsetY + (delta * 0.5f));
			}
			eventArgs.Handled = true;
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

			m_cursorDeviceX = eventArgs.Location.X;
			m_cursorDeviceY = eventArgs.Location.Y;
			if (eventArgs.ActionType == SKTouchAction.Exited || eventArgs.ActionType == SKTouchAction.Cancelled)
			{
				m_cursorInside = false;
				ApplyCursorShape(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
				InvalidateSurface();
			}
			else
			{
				m_cursorInside = true;
			}
			Tool hoverTool = main.CurrentTool();
			bool wantsHoverRepaint = ShowsBrushCursor(hoverTool);
			if (hoverTool is FreeTransformTool && ((FreeTransformTool)hoverTool).HasPreview())
			{
				wantsHoverRepaint = true;
			}
			if (eventArgs.ActionType == SKTouchAction.Moved && !eventArgs.InContact && wantsHoverRepaint)
			{
				InvalidateSurface();
			}

			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				main.ActivateDocumentWindow(m_ownerWindow);
			}

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
				NotifyChrome();
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

			if (tool is FreeTransformTool)
			{
				int pickRadius = (int)System.Math.Ceiling(9.0 / m_zoom);
				if (pickRadius < 3)
				{
					pickRadius = 3;
				}
				((FreeTransformTool)tool).SetPickRadius(pickRadius);
			}

			UpdateHoverCursor(tool, pixelX, pixelY);

			if (HandleGuideDrag(eventArgs, pixelX, pixelY))
			{
				eventArgs.Handled = true;
				return;
			}

			if (main.SnapEnabled() && IsSnapTool(tool))
			{
				int snapTolerance = (int)System.Math.Ceiling(6.0 / m_zoom);
				if (snapTolerance < 3)
				{
					snapTolerance = 3;
				}
				Bitmute.Imaging.Guides snapGuides = m_document.Guides();
				pixelX = snapGuides.SnapX(pixelX, snapTolerance);
				pixelY = snapGuides.SnapY(pixelY, snapTolerance);
				pixelX = SnapToEdge(pixelX, m_document.Width(), snapTolerance);
				pixelY = SnapToEdge(pixelY, m_document.Height(), snapTolerance);
			}

			if (tool is TextTool)
			{
				if (eventArgs.ActionType == SKTouchAction.Pressed)
				{
					main.PlaceText(this, pixelX, pixelY, eventArgs.Location.X, eventArgs.Location.Y);
				}
				eventArgs.Handled = true;
				return;
			}

			if (tool is HandTool)
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

			if (tool is ZoomTool)
			{
				if (eventArgs.ActionType == SKTouchAction.Pressed)
				{
					m_zoomDragging = true;
					m_zoomDragStartX = eventArgs.Location.X;
					m_zoomDragStartY = eventArgs.Location.Y;
					m_zoomDragCurrentX = eventArgs.Location.X;
					m_zoomDragCurrentY = eventArgs.Location.Y;
				}
				else if (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact && m_zoomDragging)
				{
					m_zoomDragCurrentX = eventArgs.Location.X;
					m_zoomDragCurrentY = eventArgs.Location.Y;
					InvalidateSurface();
				}
				else if (eventArgs.ActionType == SKTouchAction.Released && m_zoomDragging)
				{
					m_zoomDragging = false;
					float dragWidth = System.Math.Abs(m_zoomDragCurrentX - m_zoomDragStartX);
					float dragHeight = System.Math.Abs(m_zoomDragCurrentY - m_zoomDragStartY);
					if (dragWidth > 6.0f && dragHeight > 6.0f)
					{
						ZoomToScreenRect(m_zoomDragStartX, m_zoomDragStartY, m_zoomDragCurrentX, m_zoomDragCurrentY);
					}
					else
					{
						Windows.UI.Core.CoreVirtualKeyStates zoomAltState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
						bool zoomAltHeld = (zoomAltState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
						float zoomFactor = 1.25f;
						if (zoomAltHeld)
						{
							zoomFactor = 0.8f;
						}
						ApplyZoomAt(m_zoom * zoomFactor, eventArgs.Location.X, eventArgs.Location.Y);
					}
					InvalidateSurface();
				}
				eventArgs.Handled = true;
				return;
			}

			if (tool.IsDestructive() && !(tool is MoveTool))
			{
				Bitmute.Imaging.Layer activeLayer = m_document.ActiveLayer();
				if (activeLayer != null && activeLayer.IsText())
				{
					if (eventArgs.ActionType == SKTouchAction.Pressed)
					{
						main.SetStatusMessage("Rasterize the text layer to paint on it");
					}
					eventArgs.Handled = true;
					return;
				}
				if (activeLayer != null && activeLayer.PaintLocked())
				{
					if (eventArgs.ActionType == SKTouchAction.Pressed)
					{
						main.SetStatusMessage("Layer is locked");
					}
					eventArgs.Handled = true;
					return;
				}
			}
			if (tool is MoveTool)
			{
				Bitmute.Imaging.Layer moveLayer = m_document.ActiveLayer();
				if (moveLayer != null && moveLayer.MoveLocked())
				{
					if (eventArgs.ActionType == SKTouchAction.Pressed)
					{
						main.SetStatusMessage("Layer position is locked");
					}
					eventArgs.Handled = true;
					return;
				}
			}

			Windows.UI.Core.CoreVirtualKeyStates shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
			bool shiftHeld = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
			state.SetShiftHeld(shiftHeld);
			Windows.UI.Core.CoreVirtualKeyStates altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
			bool altHeld = (altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
			state.SetAltHeld(altHeld);
			int guideSnapTolerance = (int)System.Math.Ceiling(6.0 / m_zoom);
			if (guideSnapTolerance < 3)
			{
				guideSnapTolerance = 3;
			}
			state.SetSnapToGuides(main.SnapEnabled());
			state.SetSnapTolerance(guideSnapTolerance);

			bool changed = false;
			int preEventWidth = m_document.Width();
			int preEventHeight = m_document.Height();
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				m_toolStrokeActive = true;
				if (tool.IsDestructive())
				{
					m_document.BeginStroke();
				}
				changed = tool.OnPressed(m_document, pixelX, pixelY, state);
				if (tool is BrushFamilyTool && state.Airbrush())
				{
					StartAirbrush(pixelX, pixelY);
				}
			}
			else if (eventArgs.ActionType == SKTouchAction.Moved)
			{
				if (!eventArgs.InContact)
				{
					m_toolStrokeActive = false;
					StopAirbrush();
				}
				else if (m_toolStrokeActive)
				{
					changed = tool.OnDragged(m_document, pixelX, pixelY, state);
					if (m_airbrushActive)
					{
						m_airbrushX = pixelX;
						m_airbrushY = pixelY;
					}
				}
			}
			else if (eventArgs.ActionType == SKTouchAction.Released)
			{
				if (m_toolStrokeActive)
				{
					tool.OnReleased(m_document, pixelX, pixelY, state);
					if (tool.IsDestructive())
					{
						m_document.EndStroke();
						main.RefreshLayerThumbnails();
					}
				}
				m_toolStrokeActive = false;
				StopAirbrush();
			}

			bool needsRepaint = changed || m_document.ComposeDirtyAny();
			if (needsRepaint)
			{
				if (m_document.ComposeDirtyAny())
				{
					InvalidateSurface();
				}
				else
				{
					MarkComposeDirty();
				}
			}
			if (tool is EyedropperTool)
			{
				main.OnCanvasInteracted();
			}
			bool isSelectionTool = tool is RectangleSelectTool || tool is EllipseSelectTool || tool is LassoTool || tool is FreehandLassoTool || tool is MagneticLassoTool || tool is MagicWandTool;
			if (isSelectionTool)
			{
				bool acted = eventArgs.ActionType == SKTouchAction.Pressed || eventArgs.ActionType == SKTouchAction.Released || (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact);
				if (acted)
				{
					InvalidateSurface();
				}
			}
			if (tool is LineTool || tool is GradientTool || tool is ShapeTool || tool is RulerTool)
			{
				InvalidateSurface();
			}
			if (tool is CropTool)
			{
				bool cropActed = eventArgs.ActionType == SKTouchAction.Pressed || eventArgs.ActionType == SKTouchAction.Released || (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact);
				if (cropActed)
				{
					InvalidateSurface();
				}
			}
			if (tool is SliceTool)
			{
				bool sliceActed = eventArgs.ActionType == SKTouchAction.Pressed || eventArgs.ActionType == SKTouchAction.Released || (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact);
				if (sliceActed)
				{
					InvalidateSurface();
				}
			}
			if (tool is FreeTransformTool)
			{
				bool transformActed = eventArgs.ActionType == SKTouchAction.Pressed || eventArgs.ActionType == SKTouchAction.Released || (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact);
				if (transformActed)
				{
					InvalidateSurface();
				}
				if (!((FreeTransformTool)tool).HasPreview())
				{
					main.EndTransformMode();
				}
			}
			if (m_document.Width() != preEventWidth || m_document.Height() != preEventHeight)
			{
				m_document.ResetSelection();
				ResetView();
				MarkComposeDirty();
				main.RefreshLayerThumbnails();
			}
			eventArgs.Handled = true;
		}

		private static bool IsSnapTool(Tool tool)
		{
			if (tool is RectangleSelectTool)
			{
				return true;
			}
			if (tool is EllipseSelectTool)
			{
				return true;
			}
			if (tool is CropTool)
			{
				return true;
			}
			if (tool is FreeTransformTool)
			{
				return true;
			}
			return false;
		}

		private static int SnapToEdge(int value, int extent, int tolerance)
		{
			int lowDelta = value;
			if (lowDelta < 0)
			{
				lowDelta = -lowDelta;
			}
			if (lowDelta <= tolerance)
			{
				return 0;
			}
			int highDelta = value - extent;
			if (highDelta < 0)
			{
				highDelta = -highDelta;
			}
			if (highDelta <= tolerance)
			{
				return extent;
			}
			return value;
		}

		public void ResetGuideStickyCache()
		{
			m_guideStickyState = 0;
		}

		private void EnsureGuideStickyBox()
		{
			if (m_guideStickyState != 0)
			{
				return;
			}
			SKRectI box;
			bool isBackground;
			bool valid = m_document.ActiveLayerContentBox(out box, out isBackground);
			if (valid)
			{
				m_guideStickyBox = box;
				m_guideStickyIsBackground = isBackground;
				m_guideStickyState = 1;
			}
			else
			{
				m_guideStickyState = 2;
			}
		}

		private int SnapGuideToBox(int pos, bool vertical)
		{
			EnsureGuideStickyBox();
			if (m_guideStickyState != 1)
			{
				return pos;
			}
			int tolerance = (int)System.Math.Ceiling(8.0 / m_zoom);
			if (tolerance < 6)
			{
				tolerance = 6;
			}
			int[] candidates = new int[3];
			int candidateCount = 1;
			if (vertical)
			{
				candidates[0] = (m_guideStickyBox.Left + m_guideStickyBox.Right) / 2;
				if (!m_guideStickyIsBackground)
				{
					candidates[1] = m_guideStickyBox.Left;
					candidates[2] = m_guideStickyBox.Right;
					candidateCount = 3;
				}
			}
			else
			{
				candidates[0] = (m_guideStickyBox.Top + m_guideStickyBox.Bottom) / 2;
				if (!m_guideStickyIsBackground)
				{
					candidates[1] = m_guideStickyBox.Top;
					candidates[2] = m_guideStickyBox.Bottom;
					candidateCount = 3;
				}
			}
			int bestPos = pos;
			int bestDist = tolerance + 1;
			for (int index = 0; index < candidateCount; index++)
			{
				int delta = pos - candidates[index];
				if (delta < 0)
				{
					delta = -delta;
				}
				if (delta <= tolerance && delta < bestDist)
				{
					bestDist = delta;
					bestPos = candidates[index];
				}
			}
			return bestPos;
		}

		private bool HandleGuideDrag(SKTouchEventArgs eventArgs, int pixelX, int pixelY)
		{
			Bitmute.Imaging.Guides guides = m_document.Guides();
			if (guides.IsLocked())
			{
				return false;
			}
			int tolerance = (int)System.Math.Ceiling(8.0 / m_zoom);
			if (tolerance < 4)
			{
				tolerance = 4;
			}
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				int verticalIndex = guides.HitVertical(pixelX, tolerance);
				if (verticalIndex >= 0)
				{
					m_guideDragKind = 2;
					m_guideDragIndex = verticalIndex;
					ResetGuideStickyCache();
					return true;
				}
				int horizontalIndex = guides.HitHorizontal(pixelY, tolerance);
				if (horizontalIndex >= 0)
				{
					m_guideDragKind = 1;
					m_guideDragIndex = horizontalIndex;
					ResetGuideStickyCache();
					return true;
				}
				return false;
			}
			if (m_guideDragKind == 0)
			{
				return false;
			}
			if (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact)
			{
				if (m_guideDragKind == 2)
				{
					guides.MoveVertical(m_guideDragIndex, SnapGuideToBox(pixelX, true));
				}
				else
				{
					guides.MoveHorizontal(m_guideDragIndex, SnapGuideToBox(pixelY, false));
				}
				InvalidateSurface();
				return true;
			}
			if (eventArgs.ActionType == SKTouchAction.Released || (eventArgs.ActionType == SKTouchAction.Moved && !eventArgs.InContact))
			{
				if (m_guideDragKind == 2)
				{
					if (pixelX < 0 || pixelX > m_document.Width())
					{
						guides.RemoveVertical(m_guideDragIndex);
					}
				}
				else
				{
					if (pixelY < 0 || pixelY > m_document.Height())
					{
						guides.RemoveHorizontal(m_guideDragIndex);
					}
				}
				m_guideDragKind = 0;
				InvalidateSurface();
				return true;
			}
			return true;
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
			if (m_ownerWindow != null)
			{
				m_ownerWindow.SetZoomPercent(ZoomPercent());
				m_ownerWindow.RefreshChrome();
			}
		}

		private void ApplyZoomAt(float newZoom, float anchorX, float anchorY)
		{
			if (newZoom < 0.05f)
			{
				newZoom = 0.05f;
			}
			if (newZoom > 32.0f)
			{
				newZoom = 32.0f;
			}
			float documentAnchorX = (anchorX - m_offsetX) / m_zoom;
			float documentAnchorY = (anchorY - m_offsetY) / m_zoom;
			m_zoom = newZoom;
			m_offsetX = anchorX - (documentAnchorX * m_zoom);
			m_offsetY = anchorY - (documentAnchorY * m_zoom);
			ReportZoomInfo();
			InvalidateSurface();
		}

		private void ApplyZoomCentered(float newZoom)
		{
			SKSize size = CanvasSize;
			ApplyZoomAt(newZoom, (float)size.Width / 2.0f, (float)size.Height / 2.0f);
		}

		public void ZoomIn()
		{
			ApplyZoomCentered(m_zoom * 1.25f);
		}

		public void ZoomOut()
		{
			ApplyZoomCentered(m_zoom * 0.8f);
		}

		public void ZoomTo100()
		{
			m_zoom = 1.0f;
			float docWidth = m_document.Width();
			float docHeight = m_document.Height();
			m_offsetX = (m_lastViewportWidth - (docWidth * m_zoom)) / 2.0f;
			m_offsetY = (m_lastViewportHeight - (docHeight * m_zoom)) / 2.0f;
			ReportZoomInfo();
			NotifyChrome();
			InvalidateSurface();
		}

		public void SetZoomPercentValue(int percent)
		{
			if (percent < 5)
			{
				percent = 5;
			}
			if (percent > 3200)
			{
				percent = 3200;
			}
			ApplyZoomCentered(percent / 100.0f);
		}

		private void ZoomToScreenRect(float startX, float startY, float currentX, float currentY)
		{
			float left = startX;
			if (currentX < left)
			{
				left = currentX;
			}
			float right = startX;
			if (currentX > right)
			{
				right = currentX;
			}
			float top = startY;
			if (currentY < top)
			{
				top = currentY;
			}
			float bottom = startY;
			if (currentY > bottom)
			{
				bottom = currentY;
			}
			float rectWidth = right - left;
			float rectHeight = bottom - top;
			if (rectWidth < 1.0f || rectHeight < 1.0f)
			{
				return;
			}
			float documentCenterX = (((left + right) / 2.0f) - m_offsetX) / m_zoom;
			float documentCenterY = (((top + bottom) / 2.0f) - m_offsetY) / m_zoom;
			float zoomForWidth = (m_lastViewportWidth * m_zoom) / rectWidth;
			float zoomForHeight = (m_lastViewportHeight * m_zoom) / rectHeight;
			float newZoom = zoomForWidth;
			if (zoomForHeight < newZoom)
			{
				newZoom = zoomForHeight;
			}
			if (newZoom < 0.05f)
			{
				newZoom = 0.05f;
			}
			if (newZoom > 32.0f)
			{
				newZoom = 32.0f;
			}
			m_zoom = newZoom;
			m_offsetX = (m_lastViewportWidth / 2.0f) - (documentCenterX * m_zoom);
			m_offsetY = (m_lastViewportHeight / 2.0f) - (documentCenterY * m_zoom);
			ReportZoomInfo();
			NotifyChrome();
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

		public void SetOwnerWindow(DocumentWindow window)
		{
			m_ownerWindow = window;
		}

		public DocumentWindow OwnerWindow()
		{
			return m_ownerWindow;
		}

		public float Zoom()
		{
			return m_zoom;
		}

		public float PanOffsetX()
		{
			return m_offsetX;
		}

		public float PanOffsetY()
		{
			return m_offsetY;
		}

		public float ContentWidth()
		{
			return m_document.Width() * m_zoom;
		}

		public float ContentHeight()
		{
			return m_document.Height() * m_zoom;
		}

		public float ViewportWidth()
		{
			return CanvasSize.Width;
		}

		public float ViewportHeight()
		{
			return CanvasSize.Height;
		}

		public void SetPanOffsetX(float offsetX)
		{
			m_offsetX = offsetX;
			InvalidateSurface();
			NotifyChrome();
		}

		public void SetPanOffsetY(float offsetY)
		{
			m_offsetY = offsetY;
			InvalidateSurface();
			NotifyChrome();
		}

		private void NotifyChrome()
		{
			if (m_ownerWindow != null)
			{
				m_ownerWindow.RefreshChrome();
			}
		}

		public void MarkComposeDirty()
		{
			m_document.MarkComposeDirtyAll();
			InvalidateSurface();
		}

		public void ResetView()
		{
			m_viewInitialized = false;
			InvalidateSurface();
			NotifyChrome();
		}

		public void Redraw()
		{
			InvalidateSurface();
		}

		private void StartAirbrush(int x, int y)
		{
			m_airbrushX = x;
			m_airbrushY = y;
			m_airbrushActive = true;
			if (m_airbrushTimer == null && Dispatcher != null)
			{
				m_airbrushTimer = Dispatcher.CreateTimer();
				m_airbrushTimer.Interval = TimeSpan.FromMilliseconds(60.0);
				m_airbrushTimer.Tick += OnAirbrushTick;
			}
			if (m_airbrushTimer != null)
			{
				m_airbrushTimer.Start();
			}
		}

		private void StopAirbrush()
		{
			m_airbrushActive = false;
			if (m_airbrushTimer != null)
			{
				m_airbrushTimer.Stop();
			}
		}

		private void OnAirbrushTick(object sender, EventArgs eventArgs)
		{
			if (!m_airbrushActive || !m_toolStrokeActive)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			Tool tool = main.CurrentTool();
			if (!(tool is BrushFamilyTool))
			{
				return;
			}
			ToolState state = main.CurrentToolState();
			((BrushFamilyTool)tool).AirbrushStamp(m_document, m_airbrushX, m_airbrushY, state);
			if (m_document.ComposeDirtyAny())
			{
				InvalidateSurface();
			}
		}

		private void OnAntTick(object sender, EventArgs eventArgs)
		{
			if (m_document == null)
			{
				return;
			}
			if (!m_document.Selection().IsActive())
			{
				return;
			}
			m_antPhase = m_antPhase + 1.0f;
			if (m_antPhase >= AntLength * 2.0f)
			{
				m_antPhase = m_antPhase - (AntLength * 2.0f);
			}
			InvalidateSurface();
		}
	}
}
