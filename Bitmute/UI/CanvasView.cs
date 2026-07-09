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
	public class CanvasView : SKGLView
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
		private const int GridCellSize = 16;
		private const float AntLength = 6.0f;
		private const float AntStrokeWidth = 1.0f;
		private const long AntRebuildThrottleMs = 90;

		private static SKBitmap s_checkerTile;

		private Document m_document;
		private int m_fittedDocWidth = -1;
		private int m_fittedDocHeight = -1;
		private int m_channelCacheVersion = -1;
		private int m_channelCacheKey = -1;
		private SKPaint m_checkerPaint;
		private SKSurface m_gpuComposite;
		private SKBitmap m_displayComposite;
		private int m_displayCompositeVersion = -1;
		private GRRecordingContext m_gpuContext;
		private int m_gpuWidth;
		private int m_gpuHeight;
		private int m_gpuResidentKey = -2;
		private int m_gpuResidentVersion = -1;
		private GpuFilterPreview m_gpuFilterPreview;
		private SKImage m_clonePreviewImage;
		private SKBitmap m_clonePreviewSource;
		private int m_clonePreviewVersion = -1;
		private SKImage m_floatImage;
		private SKBitmap m_floatImageSource;
		private SKImage m_transformPreviewImage;
		private SKBitmap m_transformPreviewSource;
		private SKBitmap m_transformAbove;
		private SKBitmap m_channelBitmap;
		private float m_zoom;
		private float m_offsetX;
		private float m_offsetY;
		private float m_lastViewportWidth;
		private float m_lastViewportHeight;
		private bool m_viewInitialized;
		private bool m_panning;
		private bool m_spacePanning;
		private float m_panLastX;
		private float m_panLastY;
		private bool m_wheelHooked;
		private DocumentWindow m_ownerWindow;
		private IDispatcherTimer m_antTimer;
		private float m_antPhase;
		private long m_lastAntRebuildTick;
		private IDispatcherTimer m_airbrushTimer;
		private bool m_airbrushActive;
		private int m_airbrushX;
		private int m_airbrushY;
		private List<AntEdge> m_antEdges;
		private int m_antEdgesGeneration;
		private float m_cursorDeviceX;
		private float m_cursorDeviceY;
		private float m_currentPenPressure = 1.0f;
		private float m_selectPressDeviceX;
		private float m_selectPressDeviceY;
		private bool m_cursorInside;
		private static SKBitmap s_eyedropperCursor;
		private static bool s_eyedropperCursorLoadStarted;
		private const float EyedropperHotspotX = 5.0f;
		private const float EyedropperHotspotY = 58.0f;
		private const int CursorImageSize = 32;
		private bool m_toolStrokeActive;
		private bool m_altColorSampling;
		private bool m_ctrlHeld;
		private bool m_penDirectOverride;
		private bool m_ctrlMoveOverride;
		private bool m_zoomDragging;
		private float m_zoomDragStartX;
		private float m_zoomDragStartY;
		private float m_zoomDragCurrentX;
		private float m_zoomDragCurrentY;
		private int m_pendingGuideKind;
		private int m_pendingGuidePos;
		private int m_pendingGuidePosVert;
		private int m_guideDragKind;
		private int m_guideDragIndex;
		private int m_guideStickyState;
		private SKRectI m_guideStickyBox;
		private bool m_guideStickyIsBackground;
		private bool m_hasCursorSpec;
		private eCursorKind m_lastCursorKind;
		private Microsoft.UI.Input.InputSystemCursorShape m_lastCursorShape;
		private string m_lastCursorImageKey;
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

		private void OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs eventArgs)
		{
			try
			{
				OnPaintSurfaceCore(sender, eventArgs);
			}
			catch (System.Exception exception)
			{
				Bitmute.Log.Exception(exception);
			}
		}

		private void OnPaintSurfaceCore(object sender, SKPaintGLSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(Theme.CanvasSurround());

			if (info.Width <= 0 || info.Height <= 0)
			{
				return;
			}

			if (m_gpuFilterPreview != null && m_gpuFilterPreview.HasPending())
			{
				GRRecordingContext filterContext = eventArgs.Surface.Context;
				if (filterContext != null)
				{
					bool filtered = m_gpuFilterPreview.RunPending(filterContext);
					if (filtered)
					{
						m_document.MarkComposeDirtyAll();
					}
				}
				else
				{
					m_gpuFilterPreview.ClearPending();
				}
			}

			bool frameUpdatedFull;
			SKRectI frameUpdatedRegion;
			m_document.EnsureComposited(out frameUpdatedFull, out frameUpdatedRegion);

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
				m_fittedDocWidth = m_document.Width();
				m_fittedDocHeight = m_document.Height();
				m_viewInitialized = true;
				ReportZoomInfo();
			}

			bool viewportChanged = info.Width != m_lastViewportWidth || info.Height != m_lastViewportHeight;
			if (viewportChanged)
			{
				m_lastViewportWidth = info.Width;
				m_lastViewportHeight = info.Height;
				ReportZoomInfo();
			}

			float fittedWidth = docWidth * m_zoom;
			float fittedHeight = docHeight * m_zoom;
			if (fittedWidth <= info.Width)
			{
				m_offsetX = (info.Width - fittedWidth) / 2.0f;
			}
			if (fittedHeight <= info.Height)
			{
				m_offsetY = (info.Height - fittedHeight) / 2.0f;
			}

			float rectWidth = docWidth * m_zoom;
			float rectHeight = docHeight * m_zoom;
			SKRect destination = new SKRect(m_offsetX, m_offsetY, m_offsetX + rectWidth, m_offsetY + rectHeight);

			if (m_checkerPaint == null)
			{
				m_checkerPaint = new SKPaint();
				m_checkerPaint.Shader = SKShader.CreateBitmap(CheckerTile(), SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
			}
			canvas.Save();
			canvas.Translate(m_offsetX, m_offsetY);
			canvas.DrawRect(new SKRect(0.0f, 0.0f, rectWidth, rectHeight), m_checkerPaint);
			canvas.Restore();

			SKBitmap composite = m_document.Composite();
			int compositeVersion = m_document.CompositeVersion();
			if (composite.ColorType != SKColorType.Rgba8888)
			{
				if (m_displayComposite == null || m_displayComposite.Width != composite.Width || m_displayComposite.Height != composite.Height)
				{
					if (m_displayComposite != null)
					{
						m_displayComposite.Dispose();
					}
					m_displayComposite = new SKBitmap(composite.Width, composite.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
					m_displayCompositeVersion = -1;
				}
				if (m_displayCompositeVersion != compositeVersion)
				{
					SKPixmap sourcePixmap = composite.PeekPixels();
					SKPixmap targetPixmap = m_displayComposite.PeekPixels();
					sourcePixmap.ReadPixels(targetPixmap);
					sourcePixmap.Dispose();
					targetPixmap.Dispose();
					m_displayCompositeVersion = compositeVersion;
				}
				composite = m_displayComposite;
			}
			SKBitmap displayBitmap = composite;
			MainView channelMain = MainView.Self;
			if (channelMain != null)
			{
				int channelMode = channelMain.ChannelViewMode();
				bool maskChannels = channelMode < 0 && !channelMain.AllChannelsVisible();
				if (channelMode >= 0 || maskChannels)
				{
					bool reallocated = false;
					if (m_channelBitmap == null || m_channelBitmap.Width != composite.Width || m_channelBitmap.Height != composite.Height)
					{
						if (m_channelBitmap != null)
						{
							m_channelBitmap.Dispose();
						}
						m_channelBitmap = new SKBitmap(composite.Width, composite.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
						reallocated = true;
					}
					int channelKey = channelMode + 16;
					if (channelMode < 0)
					{
						channelKey = 0;
						if (channelMain.ChannelVisible(0))
						{
							channelKey = channelKey + 1;
						}
						if (channelMain.ChannelVisible(1))
						{
							channelKey = channelKey + 2;
						}
						if (channelMain.ChannelVisible(2))
						{
							channelKey = channelKey + 4;
						}
						if (channelMain.ChannelVisible(3))
						{
							channelKey = channelKey + 8;
						}
					}
					bool channelStale = reallocated || m_channelCacheVersion != compositeVersion || m_channelCacheKey != channelKey;
					if (channelStale)
					{
						if (channelMode >= 0)
						{
							Bitmute.Imaging.ChannelRender.Render(composite, m_channelBitmap, channelMode);
						}
						else
						{
							Bitmute.Imaging.ChannelRender.ApplyVisibilityMask(composite, m_channelBitmap, channelMain.ChannelVisible(0), channelMain.ChannelVisible(1), channelMain.ChannelVisible(2), channelMain.ChannelVisible(3));
						}
						m_channelCacheVersion = compositeVersion;
						m_channelCacheKey = channelKey;
					}
					displayBitmap = m_channelBitmap;
				}
			}
			int displayKey = -1;
			if (!object.ReferenceEquals(displayBitmap, composite))
			{
				displayKey = m_channelCacheKey;
			}
			SKPaint imagePaint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			GRRecordingContext frameContext = eventArgs.Surface.Context;
			bool drewFromGpu = false;
			if (frameContext != null)
			{
				if (m_gpuComposite == null || !object.ReferenceEquals(m_gpuContext, frameContext) || m_gpuWidth != displayBitmap.Width || m_gpuHeight != displayBitmap.Height)
				{
					if (m_gpuComposite != null)
					{
						m_gpuComposite.Dispose();
						m_gpuComposite = null;
					}
					SKImageInfo gpuInfo = new SKImageInfo(displayBitmap.Width, displayBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
					m_gpuComposite = SKSurface.Create(frameContext, true, gpuInfo);
					m_gpuContext = frameContext;
					m_gpuWidth = displayBitmap.Width;
					m_gpuHeight = displayBitmap.Height;
					m_gpuResidentKey = -2;
					m_gpuResidentVersion = -1;
				}
				if (m_gpuComposite != null)
				{
					bool residentCurrent = m_gpuResidentKey == displayKey && m_gpuResidentVersion == compositeVersion;
					if (!residentCurrent)
					{
						bool regionUploadValid = displayKey == -1 && m_gpuResidentKey == -1 && m_gpuResidentVersion == compositeVersion - 1 && !frameUpdatedFull && frameUpdatedRegion.Width > 0 && frameUpdatedRegion.Height > 0;
						SKCanvas uploadCanvas = m_gpuComposite.Canvas;
						SKPaint uploadPaint = new SKPaint();
						uploadPaint.BlendMode = SKBlendMode.Src;
						SKPixmap uploadPixmap = displayBitmap.PeekPixels();
						if (regionUploadValid)
						{
							SKPixmap regionPixmap = new SKPixmap();
							bool extracted = uploadPixmap.ExtractSubset(regionPixmap, frameUpdatedRegion);
							if (extracted)
							{
								SKImage regionImage = SKImage.FromPixels(regionPixmap);
								uploadCanvas.DrawImage(regionImage, frameUpdatedRegion.Left, frameUpdatedRegion.Top, sampling, uploadPaint);
								regionImage.Dispose();
							}
							else
							{
								SKImage fullImage = SKImage.FromPixels(uploadPixmap);
								uploadCanvas.DrawImage(fullImage, 0.0f, 0.0f, sampling, uploadPaint);
								fullImage.Dispose();
							}
							regionPixmap.Dispose();
						}
						else
						{
							SKImage fullImage = SKImage.FromPixels(uploadPixmap);
							uploadCanvas.DrawImage(fullImage, 0.0f, 0.0f, sampling, uploadPaint);
							fullImage.Dispose();
						}
						uploadPixmap.Dispose();
						uploadPaint.Dispose();
						m_gpuResidentKey = displayKey;
						m_gpuResidentVersion = compositeVersion;
					}
					SKImage residentSnapshot = m_gpuComposite.Snapshot();
					canvas.DrawImage(residentSnapshot, destination, sampling, imagePaint);
					DrawPatternTiles(canvas, residentSnapshot, destination, rectWidth, rectHeight, sampling, imagePaint);
					residentSnapshot.Dispose();
					drewFromGpu = true;
				}
			}
			if (!drewFromGpu)
			{
				SKPixmap displayPixmap = displayBitmap.PeekPixels();
				SKImage image = SKImage.FromPixels(displayPixmap);
				canvas.DrawImage(image, destination, sampling, imagePaint);
				DrawPatternTiles(canvas, image, destination, rectWidth, rectHeight, sampling, imagePaint);
				image.Dispose();
				displayPixmap.Dispose();
			}
			imagePaint.Dispose();

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
				GridOverlay.Draw(canvas, m_offsetX, m_offsetY, m_zoom, m_document.Width(), m_document.Height(), GridCellSize, true);
			}

			DrawFloatingOverlay(canvas);
			DrawGuides(canvas, info.Width, info.Height);
			DrawSelection(canvas);
			DrawToolOverlay(canvas);
		}

		public GpuFilterPreview FilterPreview()
		{
			if (m_gpuFilterPreview == null)
			{
				m_gpuFilterPreview = new GpuFilterPreview();
			}
			return m_gpuFilterPreview;
		}

		public bool GpuFilterAvailable()
		{
			return m_gpuContext != null;
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

		private static void DrawPatternTiles(SKCanvas canvas, SKImage image, SKRect destination, float tileWidth, float tileHeight, SKSamplingOptions sampling, SKPaint paint)
		{
			MainView main = MainView.Self;
			if (main == null || !main.PatternPreviewEnabled())
			{
				return;
			}
			for (int tileY = -1; tileY <= 1; tileY++)
			{
				for (int tileX = -1; tileX <= 1; tileX++)
				{
					if (tileX == 0 && tileY == 0)
					{
						continue;
					}
					SKRect tile = new SKRect(destination.Left + (tileX * tileWidth), destination.Top + (tileY * tileHeight), destination.Right + (tileX * tileWidth), destination.Bottom + (tileY * tileHeight));
					canvas.DrawImage(image, tile, sampling, paint);
				}
			}
		}

		public void ReleaseGpuResources()
		{
			if (m_document != null)
			{
				m_document.ReleaseComposite();
			}
			if (m_gpuFilterPreview != null)
			{
				m_gpuFilterPreview.EndSession();
			}
			if (m_clonePreviewImage != null)
			{
				m_clonePreviewImage.Dispose();
				m_clonePreviewImage = null;
				m_clonePreviewSource = null;
				m_clonePreviewVersion = -1;
			}
			if (m_gpuComposite != null)
			{
				m_gpuComposite.Dispose();
				m_gpuComposite = null;
			}
			if (m_displayComposite != null)
			{
				m_displayComposite.Dispose();
				m_displayComposite = null;
			}
			m_gpuContext = null;
			m_gpuWidth = 0;
			m_gpuHeight = 0;
			m_gpuResidentKey = -2;
			m_gpuResidentVersion = -1;
			ReleaseFloatImage();
			ReleaseTransformPreviewImage();
			if (m_antTimer != null)
			{
				m_antTimer.Stop();
			}
		}

		private void ReleaseFloatImage()
		{
			if (m_floatImage != null)
			{
				m_floatImage.Dispose();
				m_floatImage = null;
				m_floatImageSource = null;
			}
		}

		private void ReleaseTransformPreviewImage()
		{
			if (m_transformPreviewImage != null)
			{
				m_transformPreviewImage.Dispose();
				m_transformPreviewImage = null;
				m_transformPreviewSource = null;
			}
		}

		private void DrawFloatingOverlay(SKCanvas canvas)
		{
			if (!m_document.HasFloatingSelection())
			{
				ReleaseFloatImage();
				return;
			}
			SKBitmap floatBitmap = m_document.FloatBitmap();
			if (floatBitmap == null || floatBitmap.Width <= 0 || floatBitmap.Height <= 0)
			{
				ReleaseFloatImage();
				return;
			}
			int layerIndex = m_document.FloatLayerIndex();
			System.Collections.Generic.List<Bitmute.Imaging.Layer> layers = m_document.Layers();
			if (layerIndex < 0 || layerIndex >= layers.Count)
			{
				return;
			}
			Bitmute.Imaging.Layer sourceLayer = layers[layerIndex];
			float canvasLeft = sourceLayer.OffsetX() + m_document.FloatDeltaX();
			float canvasTop = sourceLayer.OffsetY() + m_document.FloatDeltaY();
			float screenLeft = m_offsetX + (canvasLeft * m_zoom);
			float screenTop = m_offsetY + (canvasTop * m_zoom);
			float screenRight = m_offsetX + ((canvasLeft + floatBitmap.Width) * m_zoom);
			float screenBottom = m_offsetY + ((canvasTop + floatBitmap.Height) * m_zoom);
			SKRect destination = new SKRect(screenLeft, screenTop, screenRight, screenBottom);
			if (m_floatImage == null || !object.ReferenceEquals(m_floatImageSource, floatBitmap))
			{
				ReleaseFloatImage();
				SKPixmap pixmap = floatBitmap.PeekPixels();
				m_floatImage = SKImage.FromPixels(pixmap);
				m_floatImageSource = floatBitmap;
				pixmap.Dispose();
			}
			SKPaint paint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			canvas.DrawImage(m_floatImage, destination, sampling, paint);
			paint.Dispose();
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
				if (m_pendingGuideKind == 1 || m_pendingGuideKind == 3)
				{
					float sy = (float)System.Math.Floor(m_offsetY + (m_pendingGuidePos * m_zoom)) + 0.5f;
					canvas.DrawLine(0.0f, sy, viewWidth, sy, pendingPaint);
				}
				if (m_pendingGuideKind == 2)
				{
					float sx = (float)System.Math.Floor(m_offsetX + (m_pendingGuidePos * m_zoom)) + 0.5f;
					canvas.DrawLine(sx, 0.0f, sx, viewHeight, pendingPaint);
				}
				if (m_pendingGuideKind == 3)
				{
					float sxVertical = (float)System.Math.Floor(m_offsetX + (m_pendingGuidePosVert * m_zoom)) + 0.5f;
					canvas.DrawLine(sxVertical, 0.0f, sxVertical, viewHeight, pendingPaint);
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

		public void SetPendingGuideBoth(int posHorizontal, int posVertical)
		{
			m_pendingGuideKind = 3;
			m_pendingGuidePos = posHorizontal;
			m_pendingGuidePosVert = posVertical;
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
			else if (kind == 3)
			{
				if (pos >= 0 && pos <= m_document.Height())
				{
					guides.AddHorizontal(pos);
				}
				if (m_pendingGuidePosVert >= 0 && m_pendingGuidePosVert <= m_document.Width())
				{
					guides.AddVertical(m_pendingGuidePosVert);
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
			else if (orientation == 3)
			{
				SetPendingGuideBoth(SnapGuideToBox(docY, false), SnapGuideToBox(docX, true));
			}
			else
			{
				SetPendingGuide(2, SnapGuideToBox(docX, true));
			}
		}

		private void ApplyCursor(CursorSpec spec)
		{
			if (m_hasCursorSpec && m_lastCursorKind == spec.m_kind && m_lastCursorShape == spec.m_systemShape && m_lastCursorImageKey == spec.m_imageKey)
			{
				return;
			}
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
			Microsoft.UI.Input.InputCursor cursor = ResolveCursor(spec);
			if (cursor == null)
			{
				return;
			}
			try
			{
				s_protectedCursorProp.SetValue(element, cursor);
				m_hasCursorSpec = true;
				m_lastCursorKind = spec.m_kind;
				m_lastCursorShape = spec.m_systemShape;
				m_lastCursorImageKey = spec.m_imageKey;
			}
			catch (System.Exception)
			{
			}
		}

		private Microsoft.UI.Input.InputCursor ResolveCursor(CursorSpec spec)
		{
			if (spec.m_kind == eCursorKind.HiddenWithOverlay)
			{
				Microsoft.UI.Input.InputCursor transparent = Bitmute.Platforms.Windows.NativeCursors.Transparent();
				if (transparent != null)
				{
					return transparent;
				}
				return Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
			}
			if (spec.m_kind == eCursorKind.Image)
			{
				if (s_eyedropperCursor == null)
				{
					if (!s_eyedropperCursorLoadStarted)
					{
						s_eyedropperCursorLoadStarted = true;
						LoadEyedropperCursor();
					}
					return null;
				}
				Microsoft.UI.Input.InputCursor image = Bitmute.Platforms.Windows.NativeCursors.FromBitmap(s_eyedropperCursor, spec.m_imageKey, spec.m_hotspotX, spec.m_hotspotY, CursorImageSize);
				if (image != null)
				{
					return image;
				}
				return Microsoft.UI.Input.InputSystemCursor.Create(spec.m_systemShape);
			}
			return Microsoft.UI.Input.InputSystemCursor.Create(spec.m_systemShape);
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

		private Tool EffectiveTool(Tool current)
		{
			if (current is PenTool)
			{
				bool useDirect;
				if (m_toolStrokeActive)
				{
					useDirect = m_penDirectOverride;
				}
				else
				{
					useDirect = m_ctrlHeld;
				}
				if (!useDirect)
				{
					return current;
				}
				MainView penMain = MainView.Self;
				if (penMain == null)
				{
					return current;
				}
				Tool directTool = penMain.ToolInstance(eTool.DirectSelect);
				if (directTool == null)
				{
					return current;
				}
				return directTool;
			}
			bool useMove;
			if (m_toolStrokeActive)
			{
				useMove = m_ctrlMoveOverride;
			}
			else
			{
				useMove = m_ctrlHeld;
			}
			if (!useMove)
			{
				return current;
			}
			if (current is MoveTool || current is HandTool || current is DirectSelectionTool || current is FreeTransformTool || current is ZoomTool)
			{
				return current;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return current;
			}
			Tool moveTool = main.ToolInstance(eTool.Move);
			if (moveTool == null)
			{
				return current;
			}
			return moveTool;
		}

		private void UpdateHoverCursor(Tool tool, int pixelX, int pixelY)
		{
			m_transformHoverKind = 0;
			CursorSpec spec = new CursorSpec(eCursorKind.System, Microsoft.UI.Input.InputSystemCursorShape.Arrow, "", 0, 0);
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
					spec = new CursorSpec(eCursorKind.System, Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast, "", 0, 0);
				}
				else if (guides.HitHorizontal(pixelY, tolerance) >= 0)
				{
					spec = new CursorSpec(eCursorKind.System, Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth, "", 0, 0);
				}
			}
			if (IsArrowSystem(spec) && tool is FreeTransformTool)
			{
				int kind = ((FreeTransformTool)tool).HitTestKind(pixelX, pixelY);
				m_transformHoverKind = kind;
				if (kind == 5)
				{
					spec = new CursorSpec(eCursorKind.HiddenWithOverlay, Microsoft.UI.Input.InputSystemCursorShape.Arrow, "", 0, 0);
				}
				else if (kind != 0)
				{
					spec = new CursorSpec(eCursorKind.System, TransformCursorShape(kind), "", 0, 0);
				}
			}
			if (IsArrowSystem(spec) && ShowsBrushCursor(tool))
			{
				spec = new CursorSpec(eCursorKind.HiddenWithOverlay, Microsoft.UI.Input.InputSystemCursorShape.Arrow, "", 0, 0);
			}
			if (IsArrowSystem(spec) && tool is PenTool)
			{
				spec = new CursorSpec(eCursorKind.System, Microsoft.UI.Input.InputSystemCursorShape.Cross, "", 0, 0);
			}
			if (IsArrowSystem(spec) && tool is EyedropperTool)
			{
				spec = new CursorSpec(eCursorKind.Image, Microsoft.UI.Input.InputSystemCursorShape.Cross, "eyedropper", (int)EyedropperHotspotX, (int)EyedropperHotspotY);
			}
			ApplyCursor(spec);
		}

		private static bool IsArrowSystem(CursorSpec spec)
		{
			return spec.m_kind == eCursorKind.System && spec.m_systemShape == Microsoft.UI.Input.InputSystemCursorShape.Arrow;
		}

		public void SyncDocumentSize()
		{
			if (m_fittedDocWidth < 0)
			{
				return;
			}
			if (m_fittedDocWidth != m_document.Width() || m_fittedDocHeight != m_document.Height())
			{
				ResetView();
			}
		}

		private static async void LoadEyedropperCursor()
		{
			try
			{
				System.IO.Stream stream = await Microsoft.Maui.Storage.FileSystem.OpenAppPackageFileAsync("eyedropper.png");
				SKBitmap bitmap = SKBitmap.Decode(stream);
				stream.Dispose();
				s_eyedropperCursor = bitmap;
			}
			catch (System.Exception)
			{
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
			tool = EffectiveTool(tool);
			DocumentWindow activeWindow = main.ActiveWindow();
			bool activeCanvas = activeWindow != null && ReferenceEquals(activeWindow.Canvas(), this);
			if (!activeCanvas)
			{
				if (m_cursorInside && ShowsBrushCursor(tool))
				{
					DrawClonePreview(canvas, main);
					DrawBrushCursor(canvas, main);
				}
				return;
			}
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
			if (tool is PenTool)
			{
				DrawCommittedPaths(canvas, -1, -1);
				DrawPenPreview(canvas, (PenTool)tool);
				DrawPenModeBadge(canvas, (PenTool)tool);
				return;
			}
			if (tool is DirectSelectionTool)
			{
				DirectSelectionTool directSelect = (DirectSelectionTool)tool;
				DrawCommittedPaths(canvas, directSelect.SelectedPath(), directSelect.SelectedAnchor());
				DrawDirectSelectBadge(canvas, directSelect);
				return;
			}
			if (tool is ZoomTool && m_zoomDragging)
			{
				DrawZoomMarquee(canvas);
				return;
			}
			if (tool is RectangleSelectTool)
			{
				RectangleSelectTool rectangleTool = (RectangleSelectTool)tool;
				if (rectangleTool.HasSizePreview() && rectangleTool.PreviewWidth() > 0)
				{
					DrawMarqueeSizeLabel(canvas, rectangleTool.PreviewWidth(), rectangleTool.PreviewHeight());
				}
				return;
			}
			if (tool is EllipseSelectTool)
			{
				EllipseSelectTool ellipseTool = (EllipseSelectTool)tool;
				if (ellipseTool.HasSizePreview() && ellipseTool.PreviewWidth() > 0)
				{
					DrawMarqueeSizeLabel(canvas, ellipseTool.PreviewWidth(), ellipseTool.PreviewHeight());
				}
				return;
			}
			if (m_cursorInside && ShowsBrushCursor(tool))
			{
				DrawClonePreview(canvas, main);
				DrawBrushCursor(canvas, main);
			}
		}

		private void DrawMarqueeSizeLabel(SKCanvas canvas, int width, int height)
		{
			string text = width + " x " + height;
			float labelX = m_cursorDeviceX + 12.0f;
			float labelY = m_cursorDeviceY + 22.0f;
			SKFont font = new SKFont();
			font.Size = 11.0f;
			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawText(text, labelX, labelY, SKTextAlign.Left, font, underlay);
			underlay.Dispose();
			SKPaint fill = new SKPaint();
			fill.Style = SKPaintStyle.Fill;
			fill.Color = SKColors.White;
			fill.IsAntialias = true;
			canvas.DrawText(text, labelX, labelY, SKTextAlign.Left, font, fill);
			fill.Dispose();
			font.Dispose();
		}

		private void DrawClonePreview(SKCanvas canvas, MainView main)
		{
			CloneTool clone = main.CurrentTool() as CloneTool;
			if (clone == null || !clone.HasSource() || m_toolStrokeActive)
			{
				return;
			}
			ToolState state = main.CurrentToolState();
			if (state == null)
			{
				return;
			}
			Layer layer = m_document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			double cursorDocX = (m_cursorDeviceX - m_offsetX) / m_zoom;
			double cursorDocY = (m_cursorDeviceY - m_offsetY) / m_zoom;
			int offsetX;
			int offsetY;
			if (state.CloneAligned() && clone.HasOffset())
			{
				offsetX = clone.SourceOffsetX();
				offsetY = clone.SourceOffsetY();
			}
			else
			{
				offsetX = (int)cursorDocX - clone.SourceX();
				offsetY = (int)cursorDocY - clone.SourceY();
			}
			float radius = (state.BrushSize() * m_zoom) / 2.0f;
			if (radius < 1.0f)
			{
				radius = 1.0f;
			}
			SKBitmap layerBitmap = layer.Bitmap();
			if (!object.ReferenceEquals(m_clonePreviewSource, layerBitmap) || m_clonePreviewVersion != m_document.CompositeVersion())
			{
				if (m_clonePreviewImage != null)
				{
					m_clonePreviewImage.Dispose();
				}
				m_clonePreviewImage = SKImage.FromPixels(layerBitmap.PeekPixels());
				m_clonePreviewSource = layerBitmap;
				m_clonePreviewVersion = m_document.CompositeVersion();
			}
			SKPathBuilder clipBuilder = new SKPathBuilder();
			clipBuilder.AddCircle(m_cursorDeviceX, m_cursorDeviceY, radius);
			SKPath clip = clipBuilder.Snapshot();
			canvas.Save();
			canvas.ClipPath(clip, SKClipOperation.Intersect, true);
			float drawLeft = m_offsetX + ((layer.OffsetX() + offsetX) * m_zoom);
			float drawTop = m_offsetY + ((layer.OffsetY() + offsetY) * m_zoom);
			SKRect ghostDestination = new SKRect(drawLeft, drawTop, drawLeft + (layerBitmap.Width * m_zoom), drawTop + (layerBitmap.Height * m_zoom));
			SKPaint ghostPaint = new SKPaint();
			ghostPaint.Color = SKColors.White.WithAlpha(150);
			SKSamplingOptions ghostSampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			canvas.DrawImage(m_clonePreviewImage, ghostDestination, ghostSampling, ghostPaint);
			ghostPaint.Dispose();
			canvas.Restore();
			clip.Dispose();
			float sourceDeviceX = m_offsetX + ((float)(cursorDocX - offsetX) * m_zoom);
			float sourceDeviceY = m_offsetY + ((float)(cursorDocY - offsetY) * m_zoom);
			SKPaint crossUnder = new SKPaint();
			crossUnder.Style = SKPaintStyle.Stroke;
			crossUnder.StrokeWidth = 3.0f;
			crossUnder.Color = SKColors.Black;
			crossUnder.IsAntialias = true;
			SKPaint crossOver = new SKPaint();
			crossOver.Style = SKPaintStyle.Stroke;
			crossOver.StrokeWidth = 1.0f;
			crossOver.Color = SKColors.White;
			crossOver.IsAntialias = true;
			canvas.DrawLine(sourceDeviceX - 6.0f, sourceDeviceY, sourceDeviceX + 6.0f, sourceDeviceY, crossUnder);
			canvas.DrawLine(sourceDeviceX, sourceDeviceY - 6.0f, sourceDeviceX, sourceDeviceY + 6.0f, crossUnder);
			canvas.DrawLine(sourceDeviceX - 6.0f, sourceDeviceY, sourceDeviceX + 6.0f, sourceDeviceY, crossOver);
			canvas.DrawLine(sourceDeviceX, sourceDeviceY - 6.0f, sourceDeviceX, sourceDeviceY + 6.0f, crossOver);
			crossUnder.Dispose();
			crossOver.Dispose();
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

		private void DrawPenPreview(SKCanvas canvas, PenTool pen)
		{
			if (!pen.HasPreview())
			{
				return;
			}
			PathData path = pen.CurrentPath();
			if (path == null || path.m_points.Count == 0)
			{
				return;
			}

			SKPath skPath = path.ToSKPath();
			if (skPath == null || skPath.IsEmpty)
			{
				skPath.Dispose();
				return;
			}

			SKMatrix scaleMatrix = SKMatrix.CreateScale(m_zoom, m_zoom);
			SKMatrix translateMatrix = SKMatrix.CreateTranslation(m_offsetX, m_offsetY);
			SKMatrix combined = scaleMatrix.PostConcat(translateMatrix);
			skPath.Transform(combined);

			SKPaint underlay = new SKPaint();
			underlay.Style = SKPaintStyle.Stroke;
			underlay.StrokeWidth = 3.0f;
			underlay.Color = SKColors.Black;
			underlay.IsAntialias = true;
			canvas.DrawPath(skPath, underlay);
			underlay.Dispose();

			SKPaint overlay = new SKPaint();
			overlay.Style = SKPaintStyle.Stroke;
			overlay.StrokeWidth = 1.5f;
			overlay.Color = SKColors.White;
			overlay.IsAntialias = true;
			canvas.DrawPath(skPath, overlay);
			overlay.Dispose();

			for (int i = 0; i < path.m_points.Count; i++)
			{
				PathPoint pt = path.m_points[i];
				float sx = m_offsetX + (pt.m_x * m_zoom);
				float sy = m_offsetY + (pt.m_y * m_zoom);

				SKPaint anchorFill = new SKPaint();
				anchorFill.Style = SKPaintStyle.Fill;
				anchorFill.Color = SKColors.White;
				anchorFill.IsAntialias = true;
				canvas.DrawRect(sx - 3.0f, sy - 3.0f, 6.0f, 6.0f, anchorFill);
				anchorFill.Dispose();

				SKPaint anchorStroke = new SKPaint();
				anchorStroke.Style = SKPaintStyle.Stroke;
				anchorStroke.StrokeWidth = 1.0f;
				anchorStroke.Color = SKColors.Black;
				anchorStroke.IsAntialias = true;
				canvas.DrawRect(sx - 3.0f, sy - 3.0f, 6.0f, 6.0f, anchorStroke);
				anchorStroke.Dispose();
			}

			skPath.Dispose();
		}

		private void DrawCommittedPaths(SKCanvas canvas, int selectedPathIndex, int selectedAnchorIndex)
		{
			System.Collections.Generic.List<PathData> paths = m_document.Paths();
			if (paths == null)
			{
				return;
			}
			for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
			{
				PathData path = paths[pathIndex];
				if (path == null || path.m_points.Count == 0)
				{
					continue;
				}

				SKPath skPath = path.ToSKPath();
				if (skPath == null || skPath.IsEmpty)
				{
					if (skPath != null)
					{
						skPath.Dispose();
					}
					continue;
				}

				SKMatrix scaleMatrix = SKMatrix.CreateScale(m_zoom, m_zoom);
				SKMatrix translateMatrix = SKMatrix.CreateTranslation(m_offsetX, m_offsetY);
				SKMatrix combined = scaleMatrix.PostConcat(translateMatrix);
				skPath.Transform(combined);

				SKPaint underlay = new SKPaint();
				underlay.Style = SKPaintStyle.Stroke;
				underlay.StrokeWidth = 3.0f;
				underlay.Color = SKColors.Black;
				underlay.IsAntialias = true;
				canvas.DrawPath(skPath, underlay);
				underlay.Dispose();

				SKPaint overlay = new SKPaint();
				overlay.Style = SKPaintStyle.Stroke;
				overlay.StrokeWidth = 1.5f;
				overlay.Color = SKColors.White;
				overlay.IsAntialias = true;
				canvas.DrawPath(skPath, overlay);
				overlay.Dispose();

				skPath.Dispose();

				bool pathSelected = pathIndex == selectedPathIndex && selectedAnchorIndex >= 0 && selectedAnchorIndex < path.m_points.Count;

				for (int i = 0; i < path.m_points.Count; i++)
				{
					PathPoint pt = path.m_points[i];
					float sx = m_offsetX + (pt.m_x * m_zoom);
					float sy = m_offsetY + (pt.m_y * m_zoom);

					if (pathSelected && i == selectedAnchorIndex)
					{
						if (pt.m_hasControlIn)
						{
							float hx = m_offsetX + (pt.m_controlInX * m_zoom);
							float hy = m_offsetY + (pt.m_controlInY * m_zoom);
							SKPaint handleLine = new SKPaint();
							handleLine.Style = SKPaintStyle.Stroke;
							handleLine.StrokeWidth = 1.0f;
							handleLine.Color = SKColors.Gray;
							handleLine.IsAntialias = true;
							canvas.DrawLine(sx, sy, hx, hy, handleLine);
							handleLine.Dispose();
							SKPaint handleFill = new SKPaint();
							handleFill.Style = SKPaintStyle.Fill;
							handleFill.Color = SKColors.White;
							handleFill.IsAntialias = true;
							canvas.DrawCircle(hx, hy, 3.0f, handleFill);
							handleFill.Dispose();
							SKPaint handleStroke = new SKPaint();
							handleStroke.Style = SKPaintStyle.Stroke;
							handleStroke.StrokeWidth = 1.0f;
							handleStroke.Color = SKColors.Black;
							handleStroke.IsAntialias = true;
							canvas.DrawCircle(hx, hy, 3.0f, handleStroke);
							handleStroke.Dispose();
						}
						if (pt.m_hasControlOut)
						{
							float hx = m_offsetX + (pt.m_controlOutX * m_zoom);
							float hy = m_offsetY + (pt.m_controlOutY * m_zoom);
							SKPaint handleLine = new SKPaint();
							handleLine.Style = SKPaintStyle.Stroke;
							handleLine.StrokeWidth = 1.0f;
							handleLine.Color = SKColors.Gray;
							handleLine.IsAntialias = true;
							canvas.DrawLine(sx, sy, hx, hy, handleLine);
							handleLine.Dispose();
							SKPaint handleFill = new SKPaint();
							handleFill.Style = SKPaintStyle.Fill;
							handleFill.Color = SKColors.White;
							handleFill.IsAntialias = true;
							canvas.DrawCircle(hx, hy, 3.0f, handleFill);
							handleFill.Dispose();
							SKPaint handleStroke = new SKPaint();
							handleStroke.Style = SKPaintStyle.Stroke;
							handleStroke.StrokeWidth = 1.0f;
							handleStroke.Color = SKColors.Black;
							handleStroke.IsAntialias = true;
							canvas.DrawCircle(hx, hy, 3.0f, handleStroke);
							handleStroke.Dispose();
						}
					}

					SKPaint anchorFill = new SKPaint();
					anchorFill.Style = SKPaintStyle.Fill;
					if (pathSelected && i == selectedAnchorIndex)
					{
						anchorFill.Color = SKColors.DodgerBlue;
					}
					else
					{
						anchorFill.Color = SKColors.White;
					}
					anchorFill.IsAntialias = true;
					canvas.DrawRect(sx - 3.0f, sy - 3.0f, 6.0f, 6.0f, anchorFill);
					anchorFill.Dispose();

					SKPaint anchorStroke = new SKPaint();
					anchorStroke.Style = SKPaintStyle.Stroke;
					anchorStroke.StrokeWidth = 1.0f;
					anchorStroke.Color = SKColors.Black;
					anchorStroke.IsAntialias = true;
					canvas.DrawRect(sx - 3.0f, sy - 3.0f, 6.0f, 6.0f, anchorStroke);
					anchorStroke.Dispose();
				}
			}
		}

		private void DrawPenModeBadge(SKCanvas canvas, PenTool pen)
		{
			if (!m_cursorInside)
			{
				return;
			}
			float docX = (m_cursorDeviceX - m_offsetX) / m_zoom;
			float docY = (m_cursorDeviceY - m_offsetY) / m_zoom;
			int radius = (int)System.Math.Ceiling(9.0 / m_zoom);
			if (radius < 3)
			{
				radius = 3;
			}
			int mode = pen.HoverMode(m_document, (int)docX, (int)docY, radius);
			if (mode == PenTool.ModeDraw)
			{
				return;
			}
			float badgeX = m_cursorDeviceX + 13.0f;
			float badgeY = m_cursorDeviceY - 13.0f;
			DrawBadgeChip(canvas, badgeX, badgeY);
			if (mode == PenTool.ModeAdd)
			{
				DrawBadgePlus(canvas, badgeX, badgeY);
			}
			else if (mode == PenTool.ModeDelete)
			{
				DrawBadgeMinus(canvas, badgeX, badgeY);
			}
			else if (mode == PenTool.ModeClose)
			{
				DrawBadgeRing(canvas, badgeX, badgeY);
			}
		}

		private void DrawDirectSelectBadge(SKCanvas canvas, DirectSelectionTool directSelect)
		{
			if (!m_cursorInside)
			{
				return;
			}
			float docX = (m_cursorDeviceX - m_offsetX) / m_zoom;
			float docY = (m_cursorDeviceY - m_offsetY) / m_zoom;
			int radius = (int)System.Math.Ceiling(9.0 / m_zoom);
			if (radius < 3)
			{
				radius = 3;
			}
			int mode = directSelect.HoverMode(m_document, (int)docX, (int)docY, radius);
			if (mode == DirectSelectionTool.HoverNone)
			{
				return;
			}
			float badgeX = m_cursorDeviceX + 13.0f;
			float badgeY = m_cursorDeviceY - 13.0f;
			DrawBadgeChip(canvas, badgeX, badgeY);
			if (mode == DirectSelectionTool.HoverAnchor)
			{
				DrawBadgeSquare(canvas, badgeX, badgeY);
			}
			else
			{
				DrawBadgeRing(canvas, badgeX, badgeY);
			}
		}

		private void DrawBadgeChip(SKCanvas canvas, float centerX, float centerY)
		{
			SKPaint fill = new SKPaint();
			fill.Style = SKPaintStyle.Fill;
			fill.Color = SKColors.White;
			fill.IsAntialias = true;
			canvas.DrawCircle(centerX, centerY, 7.5f, fill);
			fill.Dispose();
			SKPaint stroke = new SKPaint();
			stroke.Style = SKPaintStyle.Stroke;
			stroke.StrokeWidth = 1.0f;
			stroke.Color = SKColors.Black;
			stroke.IsAntialias = true;
			canvas.DrawCircle(centerX, centerY, 7.5f, stroke);
			stroke.Dispose();
		}

		private void DrawBadgePlus(SKCanvas canvas, float centerX, float centerY)
		{
			SKPaint paint = new SKPaint();
			paint.Style = SKPaintStyle.Stroke;
			paint.StrokeWidth = 1.5f;
			paint.Color = SKColors.Black;
			paint.IsAntialias = true;
			canvas.DrawLine(centerX - 3.5f, centerY, centerX + 3.5f, centerY, paint);
			canvas.DrawLine(centerX, centerY - 3.5f, centerX, centerY + 3.5f, paint);
			paint.Dispose();
		}

		private void DrawBadgeMinus(SKCanvas canvas, float centerX, float centerY)
		{
			SKPaint paint = new SKPaint();
			paint.Style = SKPaintStyle.Stroke;
			paint.StrokeWidth = 1.5f;
			paint.Color = SKColors.Black;
			paint.IsAntialias = true;
			canvas.DrawLine(centerX - 3.5f, centerY, centerX + 3.5f, centerY, paint);
			paint.Dispose();
		}

		private void DrawBadgeRing(SKCanvas canvas, float centerX, float centerY)
		{
			SKPaint paint = new SKPaint();
			paint.Style = SKPaintStyle.Stroke;
			paint.StrokeWidth = 1.5f;
			paint.Color = SKColors.Black;
			paint.IsAntialias = true;
			canvas.DrawCircle(centerX, centerY, 3.5f, paint);
			paint.Dispose();
		}

		private void DrawBadgeSquare(SKCanvas canvas, float centerX, float centerY)
		{
			SKPaint paint = new SKPaint();
			paint.Style = SKPaintStyle.Fill;
			paint.Color = SKColors.Black;
			paint.IsAntialias = true;
			canvas.DrawRect(centerX - 3.0f, centerY - 3.0f, 6.0f, 6.0f, paint);
			paint.Dispose();
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
				ReleaseTransformPreviewImage();
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
					SKBitmap styledBitmap = transform.PreviewStyledBitmap();
					SKBitmap drawSource = previewBitmap;
					float drawOffset = 0.0f;
					if (styledBitmap != null)
					{
						drawSource = styledBitmap;
						drawOffset = -transform.PreviewStyledMargin();
					}
					if (m_transformPreviewImage == null || !object.ReferenceEquals(m_transformPreviewSource, drawSource))
					{
						ReleaseTransformPreviewImage();
						SKPixmap previewPixmap = drawSource.PeekPixels();
						m_transformPreviewImage = SKImage.FromPixels(previewPixmap);
						m_transformPreviewSource = drawSource;
						previewPixmap.Dispose();
					}
					byte previewAlpha = 255;
					Bitmute.Imaging.eBlendMode previewBlend = Bitmute.Imaging.eBlendMode.Normal;
					int transformLayerIndex = transform.LayerIndex();
					if (transformLayerIndex >= 0 && transformLayerIndex < m_document.Layers().Count)
					{
						Bitmute.Imaging.Layer transformLayer = m_document.Layers()[transformLayerIndex];
						previewBlend = transformLayer.BlendMode();
						if (styledBitmap == null)
						{
							previewAlpha = transformLayer.Opacity();
						}
					}
					SKPaint previewPaint = new SKPaint();
					previewPaint.Color = SKColors.White.WithAlpha(previewAlpha);
					previewPaint.BlendMode = Bitmute.Imaging.Layer.ToSkBlendMode(previewBlend);
					SKSamplingOptions previewSampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
					canvas.Save();
					canvas.SetMatrix(quadMatrix);
					canvas.DrawImage(m_transformPreviewImage, drawOffset, drawOffset, previewSampling, previewPaint);
					canvas.Restore();
					previewPaint.Dispose();
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
			SKPixmap regionPixmap = new SKPixmap();
			bool extracted = pixmap.ExtractSubset(regionPixmap, region);
			SKRect dstRect = new SKRect(m_offsetX + (docLeft * m_zoom), m_offsetY + (docTop * m_zoom), m_offsetX + (docRight * m_zoom), m_offsetY + (docBottom * m_zoom));
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			if (extracted)
			{
				SKImage regionImage = SKImage.FromPixels(regionPixmap);
				canvas.DrawImage(regionImage, dstRect, sampling, paint);
				regionImage.Dispose();
			}
			else
			{
				SKImage image = SKImage.FromPixels(pixmap);
				SKRect srcRect = new SKRect(docLeft, docTop, docRight, docBottom);
				canvas.DrawImage(image, srcRect, dstRect, sampling, paint);
				image.Dispose();
			}
			paint.Dispose();
			regionPixmap.Dispose();
			pixmap.Dispose();
		}

		private void DrawTransformHandle(SKCanvas canvas, float cx, float cy, float size, SKPaint fill, SKPaint border)
		{
			SKRect rect = new SKRect(cx - size, cy - size, cx + size, cy + size);
			canvas.DrawRect(rect, fill);
			canvas.DrawRect(rect, border);
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
				bool translated = false;
				if (selection.Generation() == m_antEdgesGeneration + 1 && selection.LastChangeWasTranslatableShift() && m_antEdges.Count > 0)
				{
					TranslateAntEdges(selection.ShiftStepX(), selection.ShiftStepY());
					m_antEdgesGeneration = selection.Generation();
					translated = true;
				}
				if (!translated)
				{
					long now = System.Environment.TickCount64;
					bool throttled = m_toolStrokeActive && m_antEdges.Count > 0 && (now - m_lastAntRebuildTick) < AntRebuildThrottleMs;
					if (!throttled)
					{
						RebuildAntEdges(selection, bounds);
						m_antEdgesGeneration = selection.Generation();
						m_lastAntRebuildTick = now;
					}
				}
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

		private void TranslateAntEdges(int deltaX, int deltaY)
		{
			for (int index = 0; index < m_antEdges.Count; index++)
			{
				AntEdge edge = m_antEdges[index];
				if (edge.m_vertical)
				{
					edge.m_fixedCoord = edge.m_fixedCoord + deltaX;
					edge.m_startCoord = edge.m_startCoord + deltaY;
				}
				else
				{
					edge.m_fixedCoord = edge.m_fixedCoord + deltaY;
					edge.m_startCoord = edge.m_startCoord + deltaX;
				}
				m_antEdges[index] = edge;
			}
		}

		private void RebuildAntEdges(Selection selection, SKRectI bounds)
		{
			m_antEdges.Clear();
			byte[] mask = selection.Mask();
			int maskOriginX = selection.MaskOriginX();
			int maskOriginY = selection.MaskOriginY();
			int maskWidth = selection.MaskWidth();
			int maskHeight = selection.MaskHeight();
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				int rowStart = ((y - maskOriginY) * maskWidth) - maskOriginX;
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					if (mask[rowStart + x] < 128)
					{
						continue;
					}
					bool leftSelected = x > maskOriginX && mask[rowStart + x - 1] >= 128;
					if (!leftSelected)
					{
						m_antEdges.Add(new AntEdge(true, x, y));
					}
					bool rightSelected = x < maskOriginX + maskWidth - 1 && mask[rowStart + x + 1] >= 128;
					if (!rightSelected)
					{
						m_antEdges.Add(new AntEdge(true, x + 1, y));
					}
					bool upSelected = y > maskOriginY && mask[rowStart + x - maskWidth] >= 128;
					if (!upSelected)
					{
						m_antEdges.Add(new AntEdge(false, y, x));
					}
					bool downSelected = y < maskOriginY + maskHeight - 1 && mask[rowStart + x + maskWidth] >= 128;
					if (!downSelected)
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
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPlatformPointerPressed), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPlatformPointerReleased), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPlatformPointerMoved), true);
			m_wheelHooked = true;
		}

		private void OnPlatformPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs eventArgs)
		{
			Microsoft.UI.Xaml.UIElement element = sender as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			CapturePenPressure(element, eventArgs);
			element.CapturePointer(eventArgs.Pointer);
		}

		private void OnPlatformPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs eventArgs)
		{
			Microsoft.UI.Xaml.UIElement element = sender as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			CapturePenPressure(element, eventArgs);
		}

		private void CapturePenPressure(Microsoft.UI.Xaml.UIElement element, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs eventArgs)
		{
			Microsoft.UI.Input.PointerPoint point = eventArgs.GetCurrentPoint(element);
			if (point == null)
			{
				return;
			}
			if (point.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
			{
				float pressure = (float)point.Properties.Pressure;
				if (pressure < 0.0f)
				{
					pressure = 0.0f;
				}
				if (pressure > 1.0f)
				{
					pressure = 1.0f;
				}
				m_currentPenPressure = pressure;
			}
			else
			{
				m_currentPenPressure = 1.0f;
			}
		}

		private void OnPlatformPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs eventArgs)
		{
			Microsoft.UI.Xaml.UIElement element = sender as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			element.ReleasePointerCapture(eventArgs.Pointer);
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
			try
			{
				OnTouchCore(sender, eventArgs);
			}
			catch (System.Exception exception)
			{
				Bitmute.Log.Exception(exception);
			}
		}

		private void OnTouchCore(object sender, SKTouchEventArgs eventArgs)
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
				ApplyCursor(new CursorSpec(eCursorKind.System, Microsoft.UI.Input.InputSystemCursorShape.Arrow, "", 0, 0));
				InvalidateSurface();
			}
			else
			{
				m_cursorInside = true;
			}
			Tool hoverTool = main.CurrentTool();
			bool wantsHoverRepaint = ShowsBrushCursor(hoverTool) || hoverTool is EyedropperTool;
			if (hoverTool is FreeTransformTool && ((FreeTransformTool)hoverTool).HasPreview())
			{
				wantsHoverRepaint = true;
			}
			if (hoverTool is PenTool || hoverTool is DirectSelectionTool)
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

			if (eventArgs.MouseButton == SKMouseButton.Left)
			{
				Windows.UI.Core.CoreVirtualKeyStates spaceState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Space);
				bool spaceHeld = (spaceState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
				if (spaceHeld && eventArgs.ActionType == SKTouchAction.Pressed)
				{
					m_panning = true;
					m_spacePanning = true;
					m_panLastX = eventArgs.Location.X;
					m_panLastY = eventArgs.Location.Y;
					eventArgs.Handled = true;
					return;
				}
				if (m_spacePanning && eventArgs.ActionType == SKTouchAction.Released)
				{
					m_panning = false;
					m_spacePanning = false;
					eventArgs.Handled = true;
					return;
				}
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
				if (eventArgs.ActionType == SKTouchAction.Pressed)
				{
					MainView rightMain = MainView.Self;
					if (rightMain != null)
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
						rightMain.OpenBrushOptionsAt(pageX, pageY);
					}
				}
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
			Windows.UI.Core.CoreVirtualKeyStates ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
			m_ctrlHeld = (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				if (tool is PenTool)
				{
					m_penDirectOverride = m_ctrlHeld;
				}
				else
				{
					m_ctrlMoveOverride = m_ctrlHeld;
				}
			}
			tool = EffectiveTool(tool);

			if (tool is FreeTransformTool)
			{
				int pickRadius = (int)System.Math.Ceiling(9.0 / m_zoom);
				if (pickRadius < 3)
				{
					pickRadius = 3;
				}
				((FreeTransformTool)tool).SetPickRadius(pickRadius);
			}

			if (tool is PenTool)
			{
				int penPick = (int)System.Math.Ceiling(9.0 / m_zoom);
				if (penPick < 3)
				{
					penPick = 3;
				}
				((PenTool)tool).SetPickRadius(penPick);
			}

			if (tool is DirectSelectionTool)
			{
				int selectPick = (int)System.Math.Ceiling(9.0 / m_zoom);
				if (selectPick < 3)
				{
					selectPick = 3;
				}
				((DirectSelectionTool)tool).SetPickRadius(selectPick);
			}

			if (tool is LassoTool)
			{
				int lassoClose = (int)System.Math.Ceiling(6.0 / m_zoom);
				if (lassoClose < 3)
				{
					lassoClose = 3;
				}
				((LassoTool)tool).SetCloseRadius(lassoClose);
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
				if (main.SnapTargetGuides() && !(tool is RectangleSelectTool || tool is EllipseSelectTool))
				{
					Bitmute.Imaging.Guides snapGuides = m_document.Guides();
					pixelX = snapGuides.SnapX(pixelX, snapTolerance);
					pixelY = snapGuides.SnapY(pixelY, snapTolerance);
				}
				if (main.SnapTargetGrid() && !(tool is RectangleSelectTool || tool is EllipseSelectTool))
				{
					pixelX = SnapToGrid(pixelX, snapTolerance);
					pixelY = SnapToGrid(pixelY, snapTolerance);
				}
				if (main.SnapTargetEdges() && !(tool is RectangleSelectTool || tool is EllipseSelectTool))
				{
					pixelX = SnapToEdge(pixelX, m_document.Width(), snapTolerance);
					pixelY = SnapToEdge(pixelY, m_document.Height(), snapTolerance);
				}
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
					InvalidateSurface();
					eventArgs.Handled = true;
					return;
				}
				if (activeLayer != null && activeLayer.PaintLocked())
				{
					if (eventArgs.ActionType == SKTouchAction.Pressed)
					{
						main.SetStatusMessage("Layer is locked");
					}
					InvalidateSurface();
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
			int guideSnapTolerance;
			if (m_zoom > 1.0)
			{
				guideSnapTolerance = 0;
			}
			else
			{
				guideSnapTolerance = (int)System.Math.Ceiling(6.0 / m_zoom);
			}
			bool snapMaster = main.SnapEnabled();
			state.SetSnapToGuides(snapMaster && main.SnapTargetGuides());
			state.SetSnapGrid(snapMaster && main.SnapTargetGrid());
			state.SetSnapEdges(snapMaster && main.SnapTargetEdges());
			state.SetSnapLayerBounds(snapMaster && main.SnapTargetLayerBounds());
			state.SetSnapGridSize(GridCellSize);
			state.SetSnapTolerance(guideSnapTolerance);

			if (tool is RectangleSelectTool || tool is EllipseSelectTool)
			{
				if (eventArgs.ActionType == SKTouchAction.Pressed)
				{
					m_selectPressDeviceX = eventArgs.Location.X;
					m_selectPressDeviceY = eventArgs.Location.Y;
				}
				double dx = eventArgs.Location.X - m_selectPressDeviceX;
				double dy = eventArgs.Location.Y - m_selectPressDeviceY;
				double travel = Math.Sqrt((dx * dx) + (dy * dy));
				int marqueeGuideSnap = -1;
				if (main.SnapEnabled() && main.SnapTargetGuides())
				{
					marqueeGuideSnap = guideSnapTolerance;
				}
				if (tool is RectangleSelectTool)
				{
					((RectangleSelectTool)tool).SetPointerTravel(travel);
					((RectangleSelectTool)tool).SetGuideSnap(marqueeGuideSnap);
				}
				else
				{
					((EllipseSelectTool)tool).SetPointerTravel(travel);
					((EllipseSelectTool)tool).SetGuideSnap(marqueeGuideSnap);
				}
			}

			state.SetPenPressure(m_currentPenPressure);

			bool altSampleTool = tool is BrushTool || tool is PencilTool || tool is FillTool || tool is GradientTool;
			if (altSampleTool)
			{
				if (eventArgs.ActionType == SKTouchAction.Pressed)
				{
					m_altColorSampling = altHeld;
				}
				if (m_altColorSampling)
				{
					if (eventArgs.ActionType == SKTouchAction.Pressed || (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact))
					{
						Bitmute.Imaging.Layer sampleLayer = m_document.ActiveLayer();
						if (sampleLayer != null)
						{
							SKColor sampledColor = sampleLayer.GetPixelCanvas(pixelX, pixelY);
							state.SetForeground(sampledColor);
							main.OnCanvasInteracted();
						}
					}
					if (eventArgs.ActionType == SKTouchAction.Released)
					{
						m_altColorSampling = false;
					}
					eventArgs.Handled = true;
					return;
				}
			}

			bool changed = false;
			int preEventWidth = m_document.Width();
			int preEventHeight = m_document.Height();
			if (eventArgs.ActionType == SKTouchAction.Pressed)
			{
				bool pressSelectionTool = tool is RectangleSelectTool || tool is EllipseSelectTool || tool is LassoTool || tool is FreehandLassoTool || tool is MagneticLassoTool || tool is MagicWandTool;
				if (pressSelectionTool && m_document.HasFloatingSelection())
				{
					m_document.CommitFloatingSelection();
				}
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
						main.RefreshActiveLayerThumbnail();
					}
					else if (tool is MoveTool)
					{
						main.RefreshActiveLayerThumbnail();
					}
				}
				m_toolStrokeActive = false;
				StopAirbrush();
			}

			bool overlayOnlyTool = tool is PenTool || tool is DirectSelectionTool;
			bool needsRepaint = changed || m_document.ComposeDirtyAny();
			if (needsRepaint)
			{
				if (m_document.ComposeDirtyAny())
				{
					InvalidateSurface();
				}
				else if (overlayOnlyTool)
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

		private static int SnapToGrid(int value, int tolerance)
		{
			int half = GridCellSize / 2;
			int shifted;
			if (value >= 0)
			{
				shifted = value + half;
			}
			else
			{
				shifted = value - half;
			}
			int nearest = (shifted / GridCellSize) * GridCellSize;
			int delta = nearest - value;
			int magnitude = delta;
			if (magnitude < 0)
			{
				magnitude = -magnitude;
			}
			if (magnitude <= tolerance)
			{
				return nearest;
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
			float docWidth = m_document.Width();
			float docHeight = m_document.Height();
			if (m_lastViewportWidth <= 0.0f || m_lastViewportHeight <= 0.0f || docWidth <= 0.0f || docHeight <= 0.0f)
			{
				m_viewInitialized = false;
				ReportZoomInfo();
				InvalidateSurface();
				return;
			}
			float fitX = m_lastViewportWidth / docWidth;
			float fitY = m_lastViewportHeight / docHeight;
			float fit = fitX;
			if (fitY < fit)
			{
				fit = fitY;
			}
			if (fit < 0.05f)
			{
				fit = 0.05f;
			}
			if (fit > 32.0f)
			{
				fit = 32.0f;
			}
			m_zoom = fit;
			m_offsetX = (m_lastViewportWidth - (docWidth * m_zoom)) / 2.0f;
			m_offsetY = (m_lastViewportHeight - (docHeight * m_zoom)) / 2.0f;
			m_fittedDocWidth = m_document.Width();
			m_fittedDocHeight = m_document.Height();
			m_viewInitialized = true;
			ReportZoomInfo();
			NotifyChrome();
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
			m_offsetX = ClampOffsetX(offsetX);
			InvalidateSurface();
			NotifyChrome();
		}

		public void SetPanOffsetY(float offsetY)
		{
			m_offsetY = ClampOffsetY(offsetY);
			InvalidateSurface();
			NotifyChrome();
		}

		private float ClampOffsetX(float offsetX)
		{
			float content = m_document.Width() * m_zoom;
			float viewport = CanvasSize.Width;
			if (content <= viewport)
			{
				return (viewport - content) / 2.0f;
			}
			if (offsetX > 0.0f)
			{
				return 0.0f;
			}
			float minimum = viewport - content;
			if (offsetX < minimum)
			{
				return minimum;
			}
			return offsetX;
		}

		private float ClampOffsetY(float offsetY)
		{
			float content = m_document.Height() * m_zoom;
			float viewport = CanvasSize.Height;
			if (content <= viewport)
			{
				return (viewport - content) / 2.0f;
			}
			if (offsetY > 0.0f)
			{
				return 0.0f;
			}
			float minimum = viewport - content;
			if (offsetY < minimum)
			{
				return minimum;
			}
			return offsetY;
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
