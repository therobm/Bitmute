using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Bitmute.Storage;
using Bitmute.Tools;
using Bitmute.UI.Dialogs;
using Bitmute.UI.Panels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using SkiaSharp;

namespace Bitmute.UI
{
	public enum eCanvasOperation
	{
		FlipHorizontal,
		FlipVertical,
		Rotate90Clockwise,
		Rotate180,
		Rotate90CounterClockwise,
		CropToSelection,
		Trim
	}

	public class MainView : ContentPage
	{
		public static MainView Self;

		private static SkiaSharp.SKBitmap s_clipboardBitmap;

		private class ModalEntry
		{
			public View m_content;
			public BoxView m_backdrop;
			public double m_x;
			public double m_y;
			public double m_width;
			public double m_height;
			public double m_dragOriginX;
			public double m_dragOriginY;
		}

		private AbsoluteLayout m_workspace;
		private AbsoluteLayout m_overlay;
		private MenuBar m_menuBar;
		private OptionsBar m_optionsBar;
		private List<DocumentWindow> m_documents;
		private DocumentWindow m_activeDocumentWindow;
		private ToolPalette m_toolPalette;
		private LayersPanel m_layersPanel;
		private ChannelsPanel m_channelsPanel;
		private NavigatorPanel m_navigatorPanel;
		private InfoPanel m_infoPanel;
		private SwatchesPanel m_swatchesPanel;
		private PaletteGroup m_navigatorGroup;
		private PaletteGroup m_swatchesGroup;
		private PaletteGroup m_layersGroup;
		private List<PaletteGroup> m_paletteOrder;
		private Grid m_paletteDock;
		private WorkspaceState m_workspaceState;
		private System.Collections.Generic.List<ModalEntry> m_modalStack;
		private FloatingPanel m_pendingClosePanel;
		private bool m_quitPending;
		private bool m_quitConfirmed;
		private bool m_appCloseHooked;
		private int m_appCloseHookAttempts;
		private Microsoft.UI.Xaml.Window m_nativeWindow;
		private View m_pulldownPanel;
		private long m_pulldownDismissTick;
		private long m_pulldownShieldTick;
		private Label m_statusInfoLabel;
		private Label m_statusCursorLabel;
		private string[] m_menuTitles;
		private bool m_acceleratorsHooked;
		private AcceleratorRegistry m_acceleratorRegistry;
		private int m_untitledCount;
		private int m_cascadeCount;
		private int m_topZIndex;
		private ToolBox m_toolBox;
		private ToolState m_toolState;
		private int m_guideCreateOrientation;
		private CanvasView m_guideCreateCanvas;
		private int m_editingSwatchIndex = -1;
		private LayerStyle m_layerStyleSnapshot;
		private int m_layerStyleTargetIndex;
		private LayerStyle m_copiedLayerStyle;
		private AdjustmentRegistry m_adjustments;
		private Button m_focusSink;
		private bool m_focusSinkKeyHooked;


		public bool GuidesLocked()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return false;
			}
			return document.Guides().IsLocked();
		}

		public WorkspaceState Workspace()
		{
			return m_workspaceState;
		}

		public bool NavigatorPanelVisible()
		{
			return m_workspaceState.PanelVisible(ePanelId.Navigator);
		}
		public bool SwatchesPanelVisible()
		{
			return m_workspaceState.PanelVisible(ePanelId.Swatches);
		}
		public bool LayersPanelVisible()
		{
			return m_workspaceState.PanelVisible(ePanelId.Layers);
		}

		public bool CanMergeDown()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return false;
			}
			return document.ActiveLayerIndex() > 0;
		}

		public bool ActiveLayerIsText()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return false;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			return layer.IsText();
		}

		public void OpenSizeDialog(bool canvasMode)
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			string title = "Image Size";
			if (canvasMode)
			{
				title = "Canvas Size";
			}
			ShowModal(new SizeDialog(title, canvasMode, document.Width(), document.Height()), 340.0, 260.0);
		}

		public void FinishCanvasOp(CanvasView canvas, Document document)
		{
			document.ResetSelection();
			canvas.ResetView();
			canvas.MarkComposeDirty();
			RefreshLayerThumbnails();
		}

		public void DoCanvasOp(eCanvasOperation op)
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.BeginCanvasEdit(CanvasOpLabel(op));
			if (op == eCanvasOperation.FlipHorizontal)
			{
				document.FlipHorizontal();
			}
			else if (op == eCanvasOperation.FlipVertical)
			{
				document.FlipVertical();
			}
			else if (op == eCanvasOperation.Rotate90Clockwise)
			{
				document.Rotate90();
			}
			else if (op == eCanvasOperation.Rotate180)
			{
				document.Rotate180();
			}
			else if (op == eCanvasOperation.Rotate90CounterClockwise)
			{
				document.Rotate270();
			}
			else if (op == eCanvasOperation.CropToSelection)
			{
				document.CropToSelection();
			}
			else if (op == eCanvasOperation.Trim)
			{
				document.Trim();
			}
			document.EndCanvasEdit();
			FinishCanvasOp(canvas, document);
		}

		private static string CanvasOpLabel(eCanvasOperation op)
		{
			if (op == eCanvasOperation.FlipHorizontal)
			{
				return "Flip Horizontal";
			}
			if (op == eCanvasOperation.FlipVertical)
			{
				return "Flip Vertical";
			}
			if (op == eCanvasOperation.Rotate90Clockwise)
			{
				return "Rotate 90 CW";
			}
			if (op == eCanvasOperation.Rotate180)
			{
				return "Rotate 180";
			}
			if (op == eCanvasOperation.Rotate90CounterClockwise)
			{
				return "Rotate 90 CCW";
			}
			if (op == eCanvasOperation.CropToSelection)
			{
				return "Crop";
			}
			if (op == eCanvasOperation.Trim)
			{
				return "Trim";
			}
			return "Canvas Edit";
		}

		public void ApplyCanvasSize(int width, int height, int anchorX, int anchorY)
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.BeginCanvasEdit("Canvas Size");
			document.ResizeCanvas(width, height, anchorX, anchorY);
			document.EndCanvasEdit();
			FinishCanvasOp(canvas, document);
		}

		public void ApplyImageSize(int width, int height, int interpolation)
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.BeginCanvasEdit("Image Size");
			document.ScaleImage(width, height, interpolation);
			document.EndCanvasEdit();
			FinishCanvasOp(canvas, document);
		}

		public bool HasLastFilter()
		{
			return m_adjustments.HasLastFilter();
		}

		public void ApplyLastFilter()
		{
			m_adjustments.ApplyLast();
		}

		public void OpenAdjustment(eMenuAction action)
		{
			Adjustment adjustment = m_adjustments.ForAction(action);
			if (adjustment != null)
			{
				m_adjustments.Open(adjustment);
			}
		}

		public void ToggleGuideLock()
		{
			Document document = ActiveDocument();
			if (document != null)
			{
				document.Guides().SetLocked(!document.Guides().IsLocked());
			}
		}

		public void ClearGuides()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			document.Guides().Clear();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.InvalidateSurface();
			}
		}

		public string LastFilterLabel()
		{
			return m_adjustments.LastFilterLabel();
		}

		public bool BuildsFilterSubmenu(eMenuAction parent)
		{
			return m_adjustments.BuildsSubmenu(parent);
		}

		public List<MenuBarItem> FilterSubmenuItems(eMenuAction parent)
		{
			return m_adjustments.SubmenuItems(parent);
		}

		public void ApplyAdjustment(Adjustment adjustment, int[] values)
		{
			m_adjustments.Apply(adjustment, values);
		}

		public void PreviewAdjustment(Adjustment adjustment, int[] values)
		{
			m_adjustments.Preview(adjustment, values);
		}

		public void RestoreAdjustmentPreview()
		{
			m_adjustments.RestorePreview();
		}

		public void CommitAdjustment(Adjustment adjustment, int[] values)
		{
			m_adjustments.Commit(adjustment, values);
		}

		public void CancelAdjustment()
		{
			m_adjustments.Cancel();
		}

		public void DoDesaturate()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			document.BeginStroke();
			Adjustments.Desaturate(activeLayer.Bitmap());
			document.EndStroke();
			canvas.MarkComposeDirty();
		}

		public void DoSelectAll()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.CommitFloatingSelection();
			document.Selection().SelectRect(new SkiaSharp.SKRectI(0, 0, document.Width(), document.Height()));
			canvas.MarkComposeDirty();
			canvas.Redraw();
		}

		public void DoDeselect()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.CommitFloatingSelection();
			document.Selection().Clear();
			canvas.MarkComposeDirty();
			canvas.Redraw();
		}

		public void DoInvertSelection()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.CommitFloatingSelection();
			document.Selection().Invert();
			canvas.MarkComposeDirty();
			canvas.Redraw();
		}

		public void DoUndo()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			if (window.DocumentModel().Undo())
			{
				window.Canvas().SyncDocumentSize();
				window.Canvas().MarkComposeDirty();
				RefreshPanels();
			}
		}

		public void DoRedo()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			if (window.DocumentModel().Redo())
			{
				window.Canvas().SyncDocumentSize();
				window.Canvas().MarkComposeDirty();
				RefreshPanels();
			}
		}

		public void DoExit()
		{
			Application current = Application.Current;
			if (current != null)
			{
				current.Quit();
			}
		}

		public void DoZoomIn()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.ZoomIn();
			}
		}

		public void DoZoomOut()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.ZoomOut();
			}
		}

		public void DoFit()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.FitToView();
			}
		}

		public void ZoomActiveTo100()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.ZoomTo100();
			}
		}

		private SkiaSharp.SKBitmap ExtractSelection(Document document, Layer layer)
		{
			Selection selection = document.Selection();
			if (selection != null && selection.IsActive())
			{
				SkiaSharp.SKRectI bounds = selection.Bounds();
				int width = bounds.Width;
				int height = bounds.Height;
				if (width <= 0 || height <= 0)
				{
					return null;
				}
				SkiaSharp.SKBitmap result = ExtractSelectionRaw(layer, selection, bounds, width, height);
				return result;
			}
			return layer.Bitmap().Copy();
		}

		private unsafe SkiaSharp.SKBitmap ExtractSelectionRaw(Layer layer, Selection selection, SkiaSharp.SKRectI bounds, int width, int height)
		{
			SkiaSharp.SKBitmap result = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
			result.Erase(SkiaSharp.SKColors.Transparent);
			SkiaSharp.SKBitmap sourceBitmap = layer.Bitmap();
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			int sourceWidth = sourceBitmap.Width;
			int sourceHeight = sourceBitmap.Height;
			int sourceStride = sourceBitmap.RowBytes;
			int resultStride = result.RowBytes;
			byte[] selectionMask = selection.Mask();
			int selectionOriginX = selection.MaskOriginX();
			int selectionOriginY = selection.MaskOriginY();
			int selectionStride = selection.MaskWidth();
			byte* sourceBase = (byte*)sourceBitmap.GetPixels().ToPointer();
			byte* resultBase = (byte*)result.GetPixels().ToPointer();
			for (int row = 0; row < height; row++)
			{
				int canvasY = bounds.Top + row;
				int selectionRow = ((canvasY - selectionOriginY) * selectionStride) - selectionOriginX;
				byte* resultRow = resultBase + ((long)row * resultStride);
				for (int column = 0; column < width; column++)
				{
					int canvasX = bounds.Left + column;
					int coverage = selectionMask[selectionRow + canvasX];
					if (coverage == 0)
					{
						continue;
					}
					int bitmapX = canvasX - layerOffsetX;
					int bitmapY = canvasY - layerOffsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= sourceWidth || bitmapY >= sourceHeight)
					{
						continue;
					}
					byte* sourcePixel = sourceBase + ((long)bitmapY * sourceStride) + (bitmapX * 4);
					byte* resultPixel = resultRow + (column * 4);
					resultPixel[0] = sourcePixel[0];
					resultPixel[1] = sourcePixel[1];
					resultPixel[2] = sourcePixel[2];
					if (coverage < 255)
					{
						resultPixel[3] = (byte)(((sourcePixel[3] * coverage) + 127) / 255);
					}
					else
					{
						resultPixel[3] = sourcePixel[3];
					}
				}
			}
			return result;
		}

		private unsafe void EraseSelection(Document document, Layer layer)
		{
			Selection selection = document.Selection();
			if (selection != null && selection.IsActive())
			{
				SkiaSharp.SKRectI bounds = selection.Bounds();
				SkiaSharp.SKBitmap bitmap = layer.Bitmap();
				int layerOffsetX = layer.OffsetX();
				int layerOffsetY = layer.OffsetY();
				int bitmapWidth = bitmap.Width;
				int bitmapHeight = bitmap.Height;
				int stride = bitmap.RowBytes;
				byte[] selectionMask = selection.Mask();
				int selectionOriginX = selection.MaskOriginX();
				int selectionOriginY = selection.MaskOriginY();
				int selectionStride = selection.MaskWidth();
				byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
				for (int canvasY = bounds.Top; canvasY < bounds.Bottom; canvasY++)
				{
					int selectionRow = ((canvasY - selectionOriginY) * selectionStride) - selectionOriginX;
					for (int canvasX = bounds.Left; canvasX < bounds.Right; canvasX++)
					{
						int coverage = selectionMask[selectionRow + canvasX];
						if (coverage == 0)
						{
							continue;
						}
						int bitmapX = canvasX - layerOffsetX;
						int bitmapY = canvasY - layerOffsetY;
						if (bitmapX < 0 || bitmapY < 0 || bitmapX >= bitmapWidth || bitmapY >= bitmapHeight)
						{
							continue;
						}
						byte* pixel = basePointer + ((long)bitmapY * stride) + (bitmapX * 4);
						if (coverage == 255)
						{
							pixel[0] = 0;
							pixel[1] = 0;
							pixel[2] = 0;
							pixel[3] = 0;
							continue;
						}
						pixel[3] = (byte)(((pixel[3] * (255 - coverage)) + 127) / 255);
					}
				}
				return;
			}
			layer.Bitmap().Erase(SkiaSharp.SKColors.Transparent);
		}

		public async void DoCopy()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			SkiaSharp.SKBitmap copied = ExtractSelection(document, layer);
			if (copied == null)
			{
				return;
			}
			copied = CropToContent(copied);
			if (copied == null)
			{
				SetStatusMessage("Nothing to copy");
				return;
			}
			if (s_clipboardBitmap != null)
			{
				s_clipboardBitmap.Dispose();
			}
			s_clipboardBitmap = copied;
			SetStatusMessage("Copied");
			await CopyToSystemClipboard(copied);
		}

		public async void DoCopyMerged()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			SkiaSharp.SKRectI bounds = new SkiaSharp.SKRectI(0, 0, document.Width(), document.Height());
			Selection selection = document.Selection();
			bool hasSelection = selection.IsActive();
			if (hasSelection)
			{
				bounds = selection.Bounds();
				if (bounds.Left < 0)
				{
					bounds.Left = 0;
				}
				if (bounds.Top < 0)
				{
					bounds.Top = 0;
				}
				if (bounds.Right > document.Width())
				{
					bounds.Right = document.Width();
				}
				if (bounds.Bottom > document.Height())
				{
					bounds.Bottom = document.Height();
				}
			}
			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return;
			}
			SkiaSharp.SKBitmap composite = new SkiaSharp.SKBitmap(document.Width(), document.Height(), SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul);
			document.CompositeInto(composite);
			SkiaSharp.SKBitmap copied = ExtractMergedRegion(composite, bounds, selection, hasSelection);
			composite.Dispose();
			copied = CropToContent(copied);
			if (copied == null)
			{
				SetStatusMessage("Nothing to copy");
				return;
			}
			if (s_clipboardBitmap != null)
			{
				s_clipboardBitmap.Dispose();
			}
			s_clipboardBitmap = copied;
			SetStatusMessage("Copied merged");
			await CopyToSystemClipboard(copied);
		}

		private unsafe SkiaSharp.SKBitmap ExtractMergedRegion(SkiaSharp.SKBitmap composite, SkiaSharp.SKRectI bounds, Selection selection, bool useSelection)
		{
			SkiaSharp.SKBitmap copied = new SkiaSharp.SKBitmap(bounds.Width, bounds.Height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
			byte* sourceBase = (byte*)composite.GetPixels().ToPointer();
			byte* targetBase = (byte*)copied.GetPixels().ToPointer();
			int sourceRowBytes = composite.RowBytes;
			int targetRowBytes = copied.RowBytes;
			byte[] mask = null;
			int maskOriginX = 0;
			int maskOriginY = 0;
			int maskStride = 0;
			if (useSelection)
			{
				mask = selection.Mask();
				maskOriginX = selection.MaskOriginX();
				maskOriginY = selection.MaskOriginY();
				maskStride = selection.MaskWidth();
			}
			for (int y = 0; y < bounds.Height; y++)
			{
				byte* sourceRow = sourceBase + ((long)(bounds.Top + y) * sourceRowBytes) + (bounds.Left * 4);
				byte* targetRow = targetBase + ((long)y * targetRowBytes);
				int maskRow = ((bounds.Top + y - maskOriginY) * maskStride) - maskOriginX;
				for (int x = 0; x < bounds.Width; x++)
				{
					int sourceOffset = x * 4;
					int alpha = sourceRow[sourceOffset + 3];
					int red = 0;
					int green = 0;
					int blue = 0;
					if (alpha > 0)
					{
						red = ((sourceRow[sourceOffset + 0] * 255) + (alpha / 2)) / alpha;
						green = ((sourceRow[sourceOffset + 1] * 255) + (alpha / 2)) / alpha;
						blue = ((sourceRow[sourceOffset + 2] * 255) + (alpha / 2)) / alpha;
						if (red > 255)
						{
							red = 255;
						}
						if (green > 255)
						{
							green = 255;
						}
						if (blue > 255)
						{
							blue = 255;
						}
					}
					if (useSelection)
					{
						int coverage = mask[maskRow + bounds.Left + x];
						alpha = ((alpha * coverage) + 127) / 255;
					}
					int targetOffset = x * 4;
					targetRow[targetOffset + 0] = (byte)red;
					targetRow[targetOffset + 1] = (byte)green;
					targetRow[targetOffset + 2] = (byte)blue;
					targetRow[targetOffset + 3] = (byte)alpha;
				}
			}
			return copied;
		}

		private static SkiaSharp.SKBitmap CropToContent(SkiaSharp.SKBitmap source)
		{
			if (source == null)
			{
				return null;
			}
			SkiaSharp.SKRectI content = PixelRegion.ComputeContentBounds(source);
			if (content.Width <= 0 || content.Height <= 0)
			{
				source.Dispose();
				return null;
			}
			if (content.Left == 0 && content.Top == 0 && content.Width == source.Width && content.Height == source.Height)
			{
				return source;
			}
			SkiaSharp.SKBitmap cropped = PixelRegion.ExtractRegion(source, content);
			source.Dispose();
			return cropped;
		}

		private async System.Threading.Tasks.Task CopyToSystemClipboard(SkiaSharp.SKBitmap bitmap)
		{
			try
			{
				SkiaSharp.SKPixmap pixmap = bitmap.PeekPixels();
				SkiaSharp.SKImage image = SkiaSharp.SKImage.FromPixels(pixmap);
				SkiaSharp.SKData data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
				image.Dispose();
				pixmap.Dispose();
				if (data == null)
				{
					return;
				}
				byte[] bytes = data.ToArray();
				data.Dispose();
				Windows.Storage.Streams.InMemoryRandomAccessStream stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
				Windows.Storage.Streams.DataWriter writer = new Windows.Storage.Streams.DataWriter(stream);
				writer.WriteBytes(bytes);
				await writer.StoreAsync();
				await writer.FlushAsync();
				writer.DetachStream();
				writer.Dispose();
				stream.Seek(0);
				Windows.Storage.Streams.RandomAccessStreamReference reference = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromStream(stream);
				Windows.ApplicationModel.DataTransfer.DataPackage package = new Windows.ApplicationModel.DataTransfer.DataPackage();
				package.SetBitmap(reference);
				Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
			}
			catch (Exception error)
			{
				Log.Exception(error);
			}
		}

		private async System.Threading.Tasks.Task<SkiaSharp.SKBitmap> GetSystemClipboardBitmap()
		{
			try
			{
				Windows.ApplicationModel.DataTransfer.DataPackageView view = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
				if (view == null)
				{
					return null;
				}
				if (!view.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
				{
					return null;
				}
				Windows.Storage.Streams.RandomAccessStreamReference reference = await view.GetBitmapAsync();
				Windows.Storage.Streams.IRandomAccessStreamWithContentType stream = await reference.OpenReadAsync();
				System.IO.Stream netStream = System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
				SkiaSharp.SKBitmap decoded = SkiaSharp.SKBitmap.Decode(netStream);
				netStream.Dispose();
				stream.Dispose();
				if (decoded == null)
				{
					return null;
				}
				SkiaSharp.SKBitmap normalized = new SkiaSharp.SKBitmap(decoded.Width, decoded.Height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
				SkiaSharp.SKCanvas canvas = new SkiaSharp.SKCanvas(normalized);
				canvas.Clear(SkiaSharp.SKColors.Transparent);
				SkiaSharp.SKImage decodedImage = SkiaSharp.SKImage.FromBitmap(decoded);
				SkiaSharp.SKSamplingOptions sampling = new SkiaSharp.SKSamplingOptions(SkiaSharp.SKFilterMode.Nearest, SkiaSharp.SKMipmapMode.None);
				SkiaSharp.SKPaint imagePaint = new SkiaSharp.SKPaint();
				canvas.DrawImage(decodedImage, 0.0f, 0.0f, sampling, imagePaint);
				imagePaint.Dispose();
				decodedImage.Dispose();
				canvas.Dispose();
				decoded.Dispose();
				return normalized;
			}
			catch (Exception error)
			{
				Log.Exception(error);
				return null;
			}
		}

		public async void DoCut()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			SkiaSharp.SKBitmap copied = ExtractSelection(document, layer);
			if (copied == null)
			{
				return;
			}
			copied = CropToContent(copied);
			if (copied == null)
			{
				SetStatusMessage("Nothing to cut");
				return;
			}
			if (s_clipboardBitmap != null)
			{
				s_clipboardBitmap.Dispose();
			}
			s_clipboardBitmap = copied;
			await CopyToSystemClipboard(copied);
			document.BeginStroke();
			EraseSelection(document, layer);
			document.EndStroke();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
			}
			RefreshLayerThumbnails();
			SetStatusMessage("Cut");
		}

		public async void DoPaste()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			SkiaSharp.SKBitmap pasted = await GetSystemClipboardBitmap();
			if (pasted == null && s_clipboardBitmap != null)
			{
				pasted = s_clipboardBitmap.Copy();
			}
			if (pasted == null)
			{
				return;
			}
			document.BeginCanvasEdit("Paste");
			int pastedNumber = document.Layers().Count + 1;
			Layer layer = document.AddLayer("Layer " + pastedNumber);
			if (layer == null)
			{
				document.EndCanvasEdit();
				pasted.Dispose();
				return;
			}
			layer.SetBitmap(pasted);
			int offsetX = (document.Width() - pasted.Width) / 2;
			int offsetY = (document.Height() - pasted.Height) / 2;
			layer.SetOffset(offsetX, offsetY);
			int selLeft = offsetX;
			int selTop = offsetY;
			int selRight = offsetX + pasted.Width;
			int selBottom = offsetY + pasted.Height;
			if (selLeft < 0)
			{
				selLeft = 0;
			}
			if (selTop < 0)
			{
				selTop = 0;
			}
			if (selRight > document.Width())
			{
				selRight = document.Width();
			}
			if (selBottom > document.Height())
			{
				selBottom = document.Height();
			}
			if (selRight > selLeft && selBottom > selTop)
			{
				document.Selection().SelectRect(new SkiaSharp.SKRectI(selLeft, selTop, selRight, selBottom));
			}
			document.EndCanvasEdit();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
			}
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
			SetStatusMessage("Pasted");
		}

		public async void DoPasteInto()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Selection selection = document.Selection();
			if (!selection.IsActive())
			{
				DoPaste();
				return;
			}
			SkiaSharp.SKBitmap pasted = await GetSystemClipboardBitmap();
			if (pasted == null && s_clipboardBitmap != null)
			{
				pasted = s_clipboardBitmap.Copy();
			}
			if (pasted == null)
			{
				return;
			}
			SkiaSharp.SKRectI bounds = selection.Bounds();
			document.BeginCanvasEdit("Paste Into");
			int pastedNumber = document.Layers().Count + 1;
			Layer layer = document.AddLayer("Layer " + pastedNumber);
			if (layer == null)
			{
				document.EndCanvasEdit();
				pasted.Dispose();
				return;
			}
			layer.SetBitmap(pasted);
			int offsetX = bounds.Left + ((bounds.Width - pasted.Width) / 2);
			int offsetY = bounds.Top + ((bounds.Height - pasted.Height) / 2);
			layer.SetOffset(offsetX, offsetY);
			ApplySelectionStencilToLayer(document, layer);
			document.EndCanvasEdit();
			CanvasView pasteCanvas = ActiveCanvas();
			if (pasteCanvas != null)
			{
				pasteCanvas.MarkComposeDirty();
			}
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
			SetStatusMessage("Pasted into selection");
		}

		private unsafe void ApplySelectionStencilToLayer(Document document, Layer layer)
		{
			Selection selection = document.Selection();
			byte[] mask = selection.Mask();
			int maskOriginX = selection.MaskOriginX();
			int maskOriginY = selection.MaskOriginY();
			int maskStride = selection.MaskWidth();
			int maskRows = selection.MaskHeight();
			SkiaSharp.SKBitmap bitmap = layer.Bitmap();
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int bitmapWidth = bitmap.Width;
			int bitmapHeight = bitmap.Height;
			for (int y = 0; y < bitmapHeight; y++)
			{
				byte* row = basePointer + ((long)y * rowBytes);
				int canvasY = y + offsetY;
				bool rowInside = canvasY >= maskOriginY && canvasY < maskOriginY + maskRows;
				int maskRow = ((canvasY - maskOriginY) * maskStride) - maskOriginX;
				for (int x = 0; x < bitmapWidth; x++)
				{
					int coverage = 0;
					if (rowInside)
					{
						int canvasX = x + offsetX;
						if (canvasX >= maskOriginX && canvasX < maskOriginX + maskStride)
						{
							coverage = mask[maskRow + canvasX];
						}
					}
					int pixelOffset = x * 4;
					int alpha = row[pixelOffset + 3];
					row[pixelOffset + 3] = (byte)(((alpha * coverage) + 127) / 255);
				}
			}
		}

		public unsafe void SelectLayerPixels(int layerIndex)
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			List<Layer> layers = document.Layers();
			if (layerIndex < 0 || layerIndex >= layers.Count)
			{
				return;
			}
			Layer layer = layers[layerIndex];
			SkiaSharp.SKBitmap bitmap = layer.Bitmap();
			int bitmapWidth = bitmap.Width;
			int bitmapHeight = bitmap.Height;
			byte[] mask = new byte[bitmapWidth * bitmapHeight];
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int minX = int.MaxValue;
			int minY = int.MaxValue;
			int maxX = int.MinValue;
			int maxY = int.MinValue;
			for (int y = 0; y < bitmapHeight; y++)
			{
				int canvasY = y + offsetY;
				byte* row = basePointer + ((long)y * rowBytes);
				int maskRow = y * bitmapWidth;
				for (int x = 0; x < bitmapWidth; x++)
				{
					byte alpha = row[(x * 4) + 3];
					if (alpha == 0)
					{
						continue;
					}
					mask[maskRow + x] = alpha;
					int canvasX = x + offsetX;
					if (canvasX < minX)
					{
						minX = canvasX;
					}
					if (canvasX > maxX)
					{
						maxX = canvasX;
					}
					if (canvasY < minY)
					{
						minY = canvasY;
					}
					if (canvasY > maxY)
					{
						maxY = canvasY;
					}
				}
			}
			if (maxX == int.MinValue)
			{
				SetStatusMessage("Layer has no pixels to select");
				return;
			}
			SkiaSharp.SKRectI layerRect = new SkiaSharp.SKRectI(offsetX, offsetY, offsetX + bitmapWidth, offsetY + bitmapHeight);
			document.Selection().SelectMaskPlaced(mask, layerRect, new SkiaSharp.SKRectI(minX, minY, maxX + 1, maxY + 1));
			canvas.InvalidateSurface();
		}

		public void SelectLayerBounds(int layerIndex)
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			List<Layer> layers = document.Layers();
			if (layerIndex < 0 || layerIndex >= layers.Count)
			{
				return;
			}
			Layer layer = layers[layerIndex];
			SkiaSharp.SKRectI content = PixelRegion.ComputeContentBounds(layer.Bitmap());
			if (content.Width <= 0 || content.Height <= 0)
			{
				SetStatusMessage("Layer has no pixels to select");
				return;
			}
			int left = layer.OffsetX() + content.Left;
			int top = layer.OffsetY() + content.Top;
			int right = layer.OffsetX() + content.Right;
			int bottom = layer.OffsetY() + content.Bottom;
			if (right <= left || bottom <= top)
			{
				return;
			}
			document.Selection().SelectRect(new SkiaSharp.SKRectI(left, top, right, bottom));
			canvas.InvalidateSurface();
		}

		public bool RulersEnabled()
		{
			return m_workspaceState.RulersEnabled();
		}

		public bool PatternPreviewEnabled()
		{
			return m_workspaceState.PatternPreview();
		}

		public void TogglePatternPreview()
		{
			m_workspaceState.SetPatternPreview(!m_workspaceState.PatternPreview());
			InvalidateAllDocumentWindows();
		}

		public void ToggleGrid()
		{
			m_workspaceState.SetGridEnabled(!m_workspaceState.GridEnabled());
			InvalidateAllDocumentWindows();
		}

		public void OpenRecentFile(string path)
		{
			if (!System.IO.File.Exists(path))
			{
				SetStatusMessage("File not found â€” removed from recent: " + System.IO.Path.GetFileName(path));
				RecentFiles.Remove(path);
				return;
			}
			OpenDocumentFromPath(path);
		}

		public void OpenStrokeDialog()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				SetStatusMessage("No document");
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null || layer.IsText())
			{
				SetStatusMessage("Active layer cannot be stroked");
				return;
			}
			ShowModal(new StrokeDialog(), 320.0, 220.0);
		}

		public SKColor ForegroundColor()
		{
			return m_toolState.Foreground();
		}

		public void OpenLayerStyleDialog()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			m_layerStyleSnapshot = layer.LayerStyle().Clone();
			m_layerStyleTargetIndex = document.ActiveLayerIndex();
			ShowModal(new LayerStyleDialog(layer.LayerStyle().Clone()), 620.0, 460.0);
		}

		public void OpenLayerPropertiesDialog()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			ShowModal(new LayerPropertiesDialog(layer.Name()), 320.0, 160.0);
		}

		public void RenameActiveLayer(string name)
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			if (name == layer.Name())
			{
				return;
			}
			document.BeginCanvasEdit("Layer Properties");
			layer.SetName(name);
			document.EndCanvasEdit();
			RefreshPanels();
		}

		private Layer LayerStyleTargetLayer()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return null;
			}
			System.Collections.Generic.List<Layer> layers = document.Layers();
			if (m_layerStyleTargetIndex < 0 || m_layerStyleTargetIndex >= layers.Count)
			{
				return null;
			}
			return layers[m_layerStyleTargetIndex];
		}

		public void PreviewLayerStyle(LayerStyle style)
		{
			Layer layer = LayerStyleTargetLayer();
			if (layer == null)
			{
				return;
			}
			layer.SetLayerStyle(style.Clone());
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			canvas.MarkComposeDirty();
			canvas.InvalidateSurface();
		}

		public void CommitLayerStyle(LayerStyle style)
		{
			Document document = ActiveDocument();
			Layer layer = LayerStyleTargetLayer();
			if (document == null || layer == null || m_layerStyleSnapshot == null)
			{
				m_layerStyleSnapshot = null;
				CloseModal();
				return;
			}
			layer.SetLayerStyle(m_layerStyleSnapshot);
			document.BeginCanvasEdit("Layer Style");
			layer.SetLayerStyle(style.Clone());
			document.EndCanvasEdit();
			m_layerStyleSnapshot = null;
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
			RefreshPanels();
			CloseModal();
		}

		public void CancelLayerStyle()
		{
			Layer layer = LayerStyleTargetLayer();
			if (layer != null && m_layerStyleSnapshot != null)
			{
				layer.SetLayerStyle(m_layerStyleSnapshot);
				CanvasView canvas = ActiveCanvas();
				if (canvas != null)
				{
					canvas.MarkComposeDirty();
					canvas.InvalidateSurface();
				}
			}
			m_layerStyleSnapshot = null;
			CloseModal();
		}

		public void OpenColorPickerFor(SKColor initial, System.Action<SKColor> onApply)
		{
			ColorPicker picker = new ColorPicker(initial, onApply);
			ShowModal(picker, 380.0, 360.0);
		}

		public void ApplyStroke(int width, int position)
		{
			CloseModal();
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			Layer layer = document.ActiveLayer();
			if (layer == null || layer.IsText())
			{
				return;
			}
			document.BeginStroke();
			SelectionStroke.Apply(document, m_toolState.Foreground(), width, position);
			document.EndStroke();
			canvas.InvalidateSurface();
			RefreshLayerThumbnails();
		}

		public void ToggleRulers()
		{
			m_workspaceState.SetRulersEnabled(!m_workspaceState.RulersEnabled());
			for (int index = 0; index < m_documents.Count; index++)
			{
				m_documents[index].SetRulersEnabled(m_workspaceState.RulersEnabled());
			}
		}

		public void DoInvert()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			document.BeginStroke();
			Adjustments.InvertColors(activeLayer.Bitmap());
			document.EndStroke();
			canvas.MarkComposeDirty();
		}

		public void UpdateCursor(int x, int y)
		{
			if (m_statusCursorLabel != null)
			{
				m_statusCursorLabel.Text = "x: " + x + "   y: " + y;
			}
			if (m_infoPanel == null)
			{
				return;
			}
			m_infoPanel.UpdateCursor(x, y);
			Document document = ActiveDocument();
			if (document == null)
			{
				m_infoPanel.UpdatePixel(new SKColor(0, 0, 0, 0), false);
				m_infoPanel.UpdateSelection(new SKRectI(0, 0, 0, 0), false);
				return;
			}
			Layer layer = document.ActiveLayer();
			bool hasPixel = layer != null && x >= 0 && y >= 0 && x < document.Width() && y < document.Height();
			if (hasPixel)
			{
				m_infoPanel.UpdatePixel(layer.GetPixelCanvas(x, y), true);
			}
			else
			{
				m_infoPanel.UpdatePixel(new SKColor(0, 0, 0, 0), false);
			}
			Selection selection = document.Selection();
			m_infoPanel.UpdateSelection(selection.Bounds(), selection.IsActive());
		}

		public void UpdateZoomInfo(int zoomPercent, int width, int height)
		{
			if (m_statusInfoLabel != null)
			{
				m_statusInfoLabel.Text = zoomPercent + "%      " + width + " Ã— " + height + " px";
			}
			if (m_navigatorPanel != null)
			{
				m_navigatorPanel.RefreshView();
			}
		}

		private View BuildStatusBar()
		{
			Grid bar = new Grid();
			bar.HeightRequest = UiConstants.StatusBarHeight;
			bar.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			bar.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			m_statusInfoLabel = new Label();
			m_statusInfoLabel.Text = "100%      800 Ã— 600 px";
			m_statusInfoLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_statusInfoLabel.FontSize = UiConstants.ComponentFontSize;
			m_statusInfoLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusInfoLabel, 0);
			bar.Add(m_statusInfoLabel);

			m_statusCursorLabel = new Label();
			m_statusCursorLabel.Text = "x: â€”   y: â€”";
			m_statusCursorLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_statusCursorLabel.FontSize = UiConstants.ComponentFontSize;
			m_statusCursorLabel.HorizontalOptions = LayoutOptions.End;
			m_statusCursorLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusCursorLabel, 1);
			bar.Add(m_statusCursorLabel);

			return bar;
		}

		private View BuildPaletteDock()
		{
			m_navigatorPanel = new NavigatorPanel();
			m_infoPanel = new InfoPanel();
			m_navigatorGroup = new PaletteGroup(new string[] { "Navigator", "Info" }, new View[] { m_navigatorPanel, m_infoPanel });

			m_swatchesPanel = new SwatchesPanel();
			ColorPicker dockColorPicker = new ColorPicker(new SKColor(0, 0, 0, 255), true, true);
			m_swatchesGroup = new PaletteGroup(new string[] { "Swatches", "Color" }, new View[] { m_swatchesPanel, dockColorPicker });

			m_layersPanel = new LayersPanel();
			m_channelsPanel = new ChannelsPanel();
			m_layersGroup = new PaletteGroup(new string[] { "Layers", "Channels" }, new View[] { m_layersPanel, m_channelsPanel });

			Grid dock = new Grid();
			dock.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			dock.Padding = new Thickness(4.0);
			dock.RowSpacing = 4.0;
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			dock.RowDefinitions.Add(new RowDefinition(new GridLength(0.0)));

			dock.Add(m_navigatorGroup);
			dock.Add(m_swatchesGroup);
			dock.Add(m_layersGroup);

			m_paletteOrder = new List<PaletteGroup>();
			m_paletteOrder.Add(m_navigatorGroup);
			m_paletteOrder.Add(m_swatchesGroup);
			m_paletteOrder.Add(m_layersGroup);

			m_paletteDock = dock;
			LoadPanelLayout();
			RefreshDockLayout();
			return dock;
		}

		private PaletteGroup PanelForKey(ePanelId panel)
		{
			if (panel == ePanelId.Navigator)
			{
				return m_navigatorGroup;
			}
			if (panel == ePanelId.Swatches)
			{
				return m_swatchesGroup;
			}
			if (panel == ePanelId.Layers)
			{
				return m_layersGroup;
			}
			return null;
		}

		private ePanelId PanelIdForKey(string key)
		{
			if (key == "Navigator")
			{
				return ePanelId.Navigator;
			}
			if (key == "Swatches")
			{
				return ePanelId.Swatches;
			}
			if (key == "Layers")
			{
				return ePanelId.Layers;
			}
			return ePanelId.Info;
		}

		private void SavePanelLayout()
		{
			if (m_paletteOrder == null)
			{
				return;
			}
			string order = "";
			string hidden = "";
			string collapsed = "";
			for (int index = 0; index < m_paletteOrder.Count; index++)
			{
				PaletteGroup group = m_paletteOrder[index];
				string key = group.PanelKey();
				if (order.Length > 0)
				{
					order = order + ",";
				}
				order = order + key;
				if (!m_workspaceState.PanelVisible(PanelIdForKey(key)))
				{
					if (hidden.Length > 0)
					{
						hidden = hidden + ",";
					}
					hidden = hidden + key;
				}
				if (group.IsCollapsed())
				{
					if (collapsed.Length > 0)
					{
						collapsed = collapsed + ",";
					}
					collapsed = collapsed + key;
				}
			}
			Microsoft.Maui.Storage.Preferences.Default.Set("panel_order", order);
			Microsoft.Maui.Storage.Preferences.Default.Set("panel_hidden", hidden);
			Microsoft.Maui.Storage.Preferences.Default.Set("panel_collapsed", collapsed);
		}

		private void LoadPanelLayout()
		{
			string order = Microsoft.Maui.Storage.Preferences.Default.Get("panel_order", "");
			if (order.Length > 0)
			{
				List<PaletteGroup> restored = new List<PaletteGroup>();
				string[] orderKeys = order.Split(new char[] { ',' });
				for (int index = 0; index < orderKeys.Length; index++)
				{
					PaletteGroup group = PanelForKey(PanelIdForKey(orderKeys[index]));
					if (group != null && !restored.Contains(group))
					{
						restored.Add(group);
					}
				}
				for (int index = 0; index < m_paletteOrder.Count; index++)
				{
					if (!restored.Contains(m_paletteOrder[index]))
					{
						restored.Add(m_paletteOrder[index]);
					}
				}
				m_paletteOrder = restored;
			}
			string hidden = Microsoft.Maui.Storage.Preferences.Default.Get("panel_hidden", "");
			if (hidden.Length > 0)
			{
				string[] hiddenKeys = hidden.Split(new char[] { ',' });
				for (int index = 0; index < hiddenKeys.Length; index++)
				{
					m_workspaceState.SetPanelVisible(PanelIdForKey(hiddenKeys[index]), false);
				}
			}
			string collapsed = Microsoft.Maui.Storage.Preferences.Default.Get("panel_collapsed", "");
			if (collapsed.Length > 0)
			{
				string[] collapsedKeys = collapsed.Split(new char[] { ',' });
				for (int index = 0; index < collapsedKeys.Length; index++)
				{
					PaletteGroup group = PanelForKey(PanelIdForKey(collapsedKeys[index]));
					if (group != null)
					{
						group.SetCollapsed(true);
					}
				}
			}
		}

		private void RefreshDockLayout()
		{
			if (m_paletteDock == null)
			{
				return;
			}
			m_navigatorGroup.IsVisible = m_workspaceState.PanelVisible(ePanelId.Navigator);
			m_swatchesGroup.IsVisible = m_workspaceState.PanelVisible(ePanelId.Swatches);
			m_layersGroup.IsVisible = m_workspaceState.PanelVisible(ePanelId.Layers);
			bool layersStretch = m_workspaceState.PanelVisible(ePanelId.Layers) && !m_layersGroup.IsCollapsed();
			for (int index = 0; index < m_paletteOrder.Count; index++)
			{
				PaletteGroup group = m_paletteOrder[index];
				Grid.SetRow(group, index);
				GridLength height = GridLength.Auto;
				if (group == m_layersGroup && layersStretch)
				{
					height = GridLength.Star;
				}
				m_paletteDock.RowDefinitions[index].Height = height;
			}
			GridLength fillerLength = GridLength.Star;
			if (layersStretch)
			{
				fillerLength = new GridLength(0.0);
			}
			m_paletteDock.RowDefinitions[m_paletteOrder.Count].Height = fillerLength;
		}

		public void ReorderPalettePanel(PaletteGroup group, double deltaY)
		{
			if (m_paletteOrder == null)
			{
				return;
			}
			if (!m_paletteOrder.Contains(group))
			{
				return;
			}
			List<PaletteGroup> visible = new List<PaletteGroup>();
			List<double> centers = new List<double>();
			for (int index = 0; index < m_paletteOrder.Count; index++)
			{
				PaletteGroup candidate = m_paletteOrder[index];
				if (!candidate.IsVisible)
				{
					continue;
				}
				double center = candidate.Y + (candidate.Height / 2.0);
				if (candidate == group)
				{
					center = center + deltaY;
				}
				visible.Add(candidate);
				centers.Add(center);
			}
			for (int outer = 1; outer < visible.Count; outer++)
			{
				PaletteGroup movingGroup = visible[outer];
				double movingCenter = centers[outer];
				int inner = outer - 1;
				for (;;)
				{
					if (inner < 0 || centers[inner] <= movingCenter)
					{
						break;
					}
					visible[inner + 1] = visible[inner];
					centers[inner + 1] = centers[inner];
					inner = inner - 1;
				}
				visible[inner + 1] = movingGroup;
				centers[inner + 1] = movingCenter;
			}
			List<PaletteGroup> reordered = new List<PaletteGroup>();
			for (int index = 0; index < visible.Count; index++)
			{
				reordered.Add(visible[index]);
			}
			for (int index = 0; index < m_paletteOrder.Count; index++)
			{
				PaletteGroup candidate = m_paletteOrder[index];
				if (!candidate.IsVisible)
				{
					reordered.Add(candidate);
				}
			}
			m_paletteOrder = reordered;
			RefreshDockLayout();
			SavePanelLayout();
		}

		public void OnPaletteGroupLayoutChanged()
		{
			RefreshDockLayout();
			SavePanelLayout();
		}

		public void ClosePalettePanel(string key)
		{
			m_workspaceState.SetPanelVisible(PanelIdForKey(key), false);
			RefreshDockLayout();
			SavePanelLayout();
		}

		public void ToggleDockPanel(ePanelId panel)
		{
			m_workspaceState.SetPanelVisible(panel, !m_workspaceState.PanelVisible(panel));
			RefreshDockLayout();
			SavePanelLayout();
		}

		private BoxView BuildDivider()
		{
			BoxView divider = new BoxView();
			divider.ThemeColor(UiConstants.DividerLight, UiConstants.DividerDark);
			return divider;
		}

		private View BuildMiddle()
		{
			m_toolPalette = new ToolPalette();

			m_workspace = new AbsoluteLayout();
			m_workspace.ThemeBg(UiConstants.WorkspaceBackdropLight, UiConstants.WorkspaceBackdropDark);
			m_workspace.SizeChanged += OnWorkspaceSizeChanged;

			BoxView workspaceBackground = new BoxView();
			workspaceBackground.Color = Colors.Transparent;
			TapGestureRecognizer workspaceDoubleTap = new TapGestureRecognizer();
			workspaceDoubleTap.NumberOfTapsRequired = 2;
			workspaceDoubleTap.Tapped += OnWorkspaceDoubleTapped;
			workspaceBackground.GestureRecognizers.Add(workspaceDoubleTap);
			AbsoluteLayout.SetLayoutBounds(workspaceBackground, new Rect(0.0, 0.0, 1.0, 1.0));
			AbsoluteLayout.SetLayoutFlags(workspaceBackground, AbsoluteLayoutFlags.All);
			m_workspace.Add(workspaceBackground);

			View dock = BuildPaletteDock();

			Grid middle = new Grid();
			middle.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(UiConstants.ToolPaletteWidth)));
			middle.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.0)));
			middle.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			middle.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.0)));
			middle.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(UiConstants.PaletteDockWidth)));

			Grid.SetColumn(m_toolPalette, 0);
			middle.Add(m_toolPalette);

			BoxView leftDivider = BuildDivider();
			Grid.SetColumn(leftDivider, 1);
			middle.Add(leftDivider);

			Grid.SetColumn(m_workspace, 2);
			middle.Add(m_workspace);

			BoxView rightDivider = BuildDivider();
			Grid.SetColumn(rightDivider, 3);
			middle.Add(rightDivider);

			Grid.SetColumn(dock, 4);
			middle.Add(dock);

			return middle;
		}

		public MainView()
		{
			Self = this;
			Title = "Bitmute";
			Theme.InitializeFromSystem();
			Document.SetMaxUndoDepth(Microsoft.Maui.Storage.Preferences.Default.Get("undo_depth", 100));
			Microsoft.Maui.Controls.Application application = Microsoft.Maui.Controls.Application.Current;
			if (application != null)
			{
				application.RequestedThemeChanged += OnSystemThemeChanged;
			}
			this.ThemeBg(UiConstants.WorkspaceBackdropLight, UiConstants.WorkspaceBackdropDark);

			m_documents = new List<DocumentWindow>();
			m_modalStack = new System.Collections.Generic.List<ModalEntry>();
			m_untitledCount = 0;
			m_cascadeCount = 0;
			m_topZIndex = 0;
			m_toolBox = new ToolBox();
			m_toolState = m_toolBox.State();
			m_adjustments = new AdjustmentRegistry(this, m_toolState);
			m_acceleratorRegistry = new AcceleratorRegistry(this, m_toolState);
			m_guideCreateOrientation = 0;
			m_guideCreateCanvas = null;
			m_workspaceState = new WorkspaceState();
			m_menuTitles = new string[] { "File", "Edit", "Image", "Layer", "Select", "Filter", "View", "Window", "Help" };
			m_overlay = new AbsoluteLayout();
			m_overlay.InputTransparent = true;
			m_overlay.CascadeInputTransparent = false;
			m_menuBar = new MenuBar(this, m_menuTitles, m_overlay);

			View menuBar = m_menuBar.Root();
			m_optionsBar = new OptionsBar(this, m_toolState);
			View optionsBar = m_optionsBar.Root();
			View middle = BuildMiddle();
			View statusBar = BuildStatusBar();

			Grid root = new Grid();
			root.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.MenuBarHeight)));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(1.0)));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.OptionsBarHeight)));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(1.0)));
			root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(1.0)));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.StatusBarHeight)));

			Grid.SetRow(menuBar, 0);
			root.Add(menuBar);

			BoxView underMenu = BuildDivider();
			Grid.SetRow(underMenu, 1);
			root.Add(underMenu);

			Grid.SetRow(optionsBar, 2);
			root.Add(optionsBar);

			BoxView underOptions = BuildDivider();
			Grid.SetRow(underOptions, 3);
			root.Add(underOptions);

			Grid.SetRow(middle, 4);
			root.Add(middle);

			BoxView aboveStatus = BuildDivider();
			Grid.SetRow(aboveStatus, 5);
			root.Add(aboveStatus);

			Grid.SetRow(statusBar, 6);
			root.Add(statusBar);

			Grid outer = new Grid();
			outer.Add(root);
			outer.Add(m_overlay);

			Content = outer;
		}

		private void HookAppWindowClosing()
		{
			if (m_appCloseHooked)
			{
				return;
			}
			m_appCloseHookAttempts = m_appCloseHookAttempts + 1;
			Microsoft.Maui.Controls.Window mauiWindow = Window;
			Microsoft.UI.Xaml.Window nativeWindow = null;
			if (mauiWindow != null && mauiWindow.Handler != null)
			{
				nativeWindow = mauiWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window;
			}
			if (nativeWindow == null || nativeWindow.AppWindow == null)
			{
				if (m_appCloseHookAttempts < 20)
				{
					Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500.0), HookAppWindowClosing);
				}
				return;
			}
			m_nativeWindow = nativeWindow;
			nativeWindow.AppWindow.Closing += OnAppWindowClosing;
			string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "AppIcon", "icon.ico");
			if (System.IO.File.Exists(iconPath))
			{
				nativeWindow.AppWindow.SetIcon(iconPath);
			}
			m_appCloseHooked = true;
		}

		private void OnAppWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
		{
			if (m_quitConfirmed)
			{
				return;
			}
			if (FirstDirtyDocumentWindow() == null)
			{
				return;
			}
			args.Cancel = true;
			m_quitPending = true;
			Dispatcher.Dispatch(ContinueQuitClose);
		}

		private DocumentWindow FirstDirtyDocumentWindow()
		{
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index];
				Document model = window.DocumentModel();
				if (model != null && model.IsDirty())
				{
					return window;
				}
			}
			return null;
		}

		private void ContinueQuitClose()
		{
			if (!m_quitPending)
			{
				return;
			}
			DocumentWindow dirty = FirstDirtyDocumentWindow();
			if (dirty == null)
			{
				m_quitPending = false;
				m_quitConfirmed = true;
				if (m_nativeWindow != null)
				{
					m_nativeWindow.Close();
				}
				return;
			}
			ClosePanel(dirty);
		}

		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();
			Dispatcher.Dispatch(HookAppWindowClosing);
			if (m_acceleratorsHooked)
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
			m_acceleratorRegistry.Hook(element);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnGlobalPointerPressed), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnGlobalPointerMoved), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnGlobalPointerReleased), true);
			element.AllowDrop = true;
			element.DragOver += OnElementDragOver;
			element.Drop += OnElementDrop;
			m_acceleratorsHooked = true;
		}

		private void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			if (m_pulldownPanel == null)
			{
				return;
			}
			if ((System.Environment.TickCount64 - m_pulldownShieldTick) < 200)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			Windows.Foundation.Point position = args.GetCurrentPoint(element).Position;
			double panelX = m_pulldownPanel.X;
			double panelY = m_pulldownPanel.Y;
			double panelWidth = m_pulldownPanel.Width;
			double panelHeight = m_pulldownPanel.Height;
			if (position.X >= panelX && position.X <= panelX + panelWidth && position.Y >= panelY && position.Y <= panelY + panelHeight)
			{
				return;
			}
			m_pulldownDismissTick = System.Environment.TickCount64;
			ClosePulldown();
		}

		public void BeginGuideCreation(int orientation, CanvasView canvas)
		{
			if (canvas == null)
			{
				return;
			}
			if (canvas.CurrentDocument().Guides().IsLocked())
			{
				return;
			}
			ActivateDocumentWindow(canvas.OwnerWindow());
			m_guideCreateOrientation = orientation;
			m_guideCreateCanvas = canvas;
			canvas.ResetGuideStickyCache();
		}

		private void OnGlobalPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			if (m_guideCreateOrientation == 0)
			{
				return;
			}
			CanvasView canvas = m_guideCreateCanvas;
			if (canvas == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement canvasElement = canvas.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (canvasElement == null)
			{
				return;
			}
			Windows.Foundation.Point position = args.GetCurrentPoint(canvasElement).Position;
			canvas.UpdatePendingGuideFromDip(m_guideCreateOrientation, position.X, position.Y);
		}

		private void OnGlobalPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			if (m_guideCreateOrientation == 0)
			{
				return;
			}
			CanvasView canvas = m_guideCreateCanvas;
			m_guideCreateOrientation = 0;
			m_guideCreateCanvas = null;
			if (canvas != null)
			{
				canvas.CommitPendingGuide();
			}
		}

		private void OnElementDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs args)
		{
			if (args.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
			{
				args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
			}
		}

		private async void OnElementDrop(object sender, Microsoft.UI.Xaml.DragEventArgs args)
		{
			if (!args.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
			{
				return;
			}
			System.Collections.Generic.IReadOnlyList<Windows.Storage.IStorageItem> items = await args.DataView.GetStorageItemsAsync();
			for (int index = 0; index < items.Count; index++)
			{
				Windows.Storage.StorageFile file = items[index] as Windows.Storage.StorageFile;
				if (file != null)
				{
					OpenDocumentFromPath(file.Path);
				}
			}
		}

		public void DoClearSelection()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			SKColor fill = new SKColor(0, 0, 0, 0);
			if (layer.IsBackground())
			{
				SKColor background = m_toolState.Background();
				fill = new SKColor(background.Red, background.Green, background.Blue, 255);
			}
			FillSelectionWith(fill, false);
		}

		public void FillSelectionWith(SKColor fill, bool fillLayerWhenEmpty)
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null || layer.IsText())
			{
				return;
			}
			if (layer.PaintLocked())
			{
				SetStatusMessage("Layer is locked");
				return;
			}
			Selection selection = document.Selection();
			bool hasSelection = selection.IsActive();
			if (!hasSelection && !fillLayerWhenEmpty)
			{
				return;
			}
			document.BeginStroke();
			if (hasSelection)
			{
				document.FillSelection(fill);
			}
			else
			{
				document.FillLayer(fill);
			}
			document.EndStroke();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.InvalidateSurface();
			}
			RefreshLayerThumbnails();
		}

		public void MergeSelectedLayers()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			List<int> selected = document.SelectedLayerIndices();
			if (selected.Count >= 2)
			{
				document.BeginCanvasEdit("Merge Layers");
				document.MergeLayers(selected);
				document.EndCanvasEdit();
				FinishLayerStructureChange(canvas);
			}
			else
			{
				DoMergeDown();
			}
		}

		public void DoMergeDown()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			if (document.ActiveLayerIndex() <= 0)
			{
				return;
			}
			document.BeginCanvasEdit("Merge Down");
			document.MergeDown(document.ActiveLayerIndex());
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		public void DoMergeVisible()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.BeginCanvasEdit("Merge Visible");
			document.MergeVisible();
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		public void DoFlattenImage()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.BeginCanvasEdit("Flatten Image");
			document.FlattenImage();
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		private void FinishLayerStructureChange(CanvasView canvas)
		{
			canvas.MarkComposeDirty();
			RefreshPanels();
		}

		public void DuplicateActiveLayer()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.BeginCanvasEdit("Duplicate Layer");
			document.DuplicateLayer(document.ActiveLayerIndex());
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		public void AddNewLayer()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			if (document == null)
			{
				return;
			}
			int layerNumber = document.Layers().Count + 1;
			document.BeginCanvasEdit("Add Layer");
			document.AddLayer("Layer " + layerNumber);
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		public void DeleteActiveLayer()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			document.BeginCanvasEdit("Delete Layer");
			document.DeleteLayer(document.ActiveLayerIndex());
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		public void RequestDeleteActiveLayer()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			if (document.Layers().Count <= 1)
			{
				return;
			}
			ShowModal(new MessageDialog("Delete Layer", "Delete layer \"" + layer.Name() + "\"?", new string[] { "Cancel", "Delete" }, OnDeleteLayerChoice), 320.0, 150.0);
		}

		private void OnDeleteLayerChoice(int choice)
		{
			if (choice == 1)
			{
				DeleteActiveLayer();
			}
		}

		private Border BuildContextMenuRow(string text, EventHandler<TappedEventArgs> handler)
		{
			Label label = new Label();
			label.Text = text;
			label.FontSize = UiConstants.PanelFontSize;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;

			Border row = new Border();
			row.HeightRequest = MenuBar.MenuItemHeight;
			row.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
			row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			row.StrokeThickness = 0.0;
			row.Content = label;
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += handler;
			row.GestureRecognizers.Add(tap);
			return row;
		}

		public void ShowLayerContextMenu(int layerIndex, double anchorX, double anchorY)
		{
			VerticalStackLayout menu = new VerticalStackLayout();
			menu.Spacing = 0.0;
			menu.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);
			menu.Add(BuildContextMenuRow("Layer Styleâ€¦", OnContextLayerStyle));
			menu.Add(BuildContextMenuRow("Copy Layer Style", OnContextCopyLayerStyle));
			menu.Add(BuildContextMenuRow("Paste Layer Style", OnContextPasteLayerStyle));
			menu.Add(BuildContextMenuRow("Layer Propertiesâ€¦", OnContextLayerProperties));
			menu.Add(BuildContextMenuRow("Duplicate Layer", OnContextDuplicateLayer));
			menu.Add(BuildContextMenuRow("Merge Down", OnContextMergeDown));
			menu.Add(BuildContextMenuRow("Rasterize Text", OnContextRasterizeText));
			menu.Add(MenuBar.BuildMenuSeparator());
			menu.Add(BuildContextMenuRow("Delete Layer", OnContextDeleteLayer));
			double height = (8.0 * MenuBar.MenuItemHeight) + MenuBar.MenuSeparatorHeight + 8.0;
			ShowPulldown(menu, anchorX, anchorY, MenuBar.DropdownWidth, height);
		}

		private void OnContextLayerStyle(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			OpenLayerStyleDialog();
		}

		private void OnContextCopyLayerStyle(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			m_copiedLayerStyle = layer.LayerStyle().Clone();
		}

		private void OnContextPasteLayerStyle(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			if (m_copiedLayerStyle == null)
			{
				return;
			}
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			document.BeginCanvasEdit("Paste Layer Style");
			layer.SetLayerStyle(m_copiedLayerStyle.Clone());
			document.EndCanvasEdit();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
			RefreshPanels();
		}

		private void OnContextLayerProperties(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			OpenLayerPropertiesDialog();
		}

		private void OnContextDuplicateLayer(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			DuplicateActiveLayer();
		}

		private void OnContextMergeDown(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			DoMergeDown();
		}

		private void OnContextRasterizeText(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			DoRasterizeText();
		}

		private void OnContextDeleteLayer(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			RequestDeleteActiveLayer();
		}

		public bool TransformActive()
		{
			if (m_toolState == null || m_toolBox == null)
			{
				return false;
			}
			return m_toolState.Tool() == eTool.FreeTransform && m_toolBox.FreeTransform().HasPreview();
		}

		public void CommitTransform()
		{
			Document document = ActiveDocument();
			if (document != null)
			{
				m_toolBox.FreeTransform().Commit(document);
			}
			EndTransformMode();
			RefreshTransformCanvas();
		}

		public void CancelTransform()
		{
			m_toolBox.FreeTransform().Cancel();
			EndTransformMode();
			RefreshTransformCanvas();
		}

		public bool CropArmed()
		{
			if (m_toolState == null || m_toolBox == null)
			{
				return false;
			}
			return m_toolState.Tool() == eTool.Crop && m_toolBox.Crop().HasPreview();
		}

		public void CommitCrop()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			m_toolBox.Crop().CommitPending(document);
			FinishCanvasOp(canvas, document);
		}

		public void CancelCrop()
		{
			m_toolBox.Crop().CancelPending();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.InvalidateSurface();
			}
		}

		public bool LassoArmed()
		{
			if (m_toolState == null || m_toolBox == null)
			{
				return false;
			}
			return m_toolState.Tool() == eTool.Lasso && m_toolBox.Lasso().HasPreview();
		}

		public void FinalizeLasso()
		{
			DocumentWindow window = ActiveWindow();
			if (window == null)
			{
				return;
			}
			m_toolBox.Lasso().FinalizePending(window.DocumentModel(), m_toolState);
			window.Canvas().InvalidateSurface();
		}

		public void CancelLasso()
		{
			m_toolBox.Lasso().Reset();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.InvalidateSurface();
			}
		}

		public bool CommitArmedOperation()
		{
			if (IsTextEditActive())
			{
				return false;
			}
			if (TransformActive())
			{
				CommitTransform();
				return true;
			}
			if (CropArmed())
			{
				CommitCrop();
				return true;
			}
			if (LassoArmed())
			{
				FinalizeLasso();
				return true;
			}
			return false;
		}

		public bool CancelArmedOperation()
		{
			if (IsTextEditActive())
			{
				return false;
			}
			if (TransformActive())
			{
				CancelTransform();
				return true;
			}
			if (CropArmed())
			{
				CancelCrop();
				return true;
			}
			if (LassoArmed())
			{
				CancelLasso();
				return true;
			}
			return false;
		}

		private void RefreshTransformCanvas()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
			RefreshLayerThumbnails();
		}

		private double WindowChromeWidth()
		{
			double rulerWidth = 0.0;
			if (m_workspaceState.RulersEnabled())
			{
				rulerWidth = UiConstants.RulerThickness;
			}
			return rulerWidth + UiConstants.ResizeGripSize + (2.0 * UiConstants.PanelBorderThickness);
		}

		private double WindowChromeHeight()
		{
			double rulerHeight = 0.0;
			if (m_workspaceState.RulersEnabled())
			{
				rulerHeight = UiConstants.RulerThickness;
			}
			return UiConstants.TitleBarHeight + rulerHeight + UiConstants.DocumentBottomBar + (2.0 * UiConstants.PanelBorderThickness);
		}

		private void PlaceAndAdd(DocumentWindow window)
		{
			double workspaceWidth = WorkspaceWidth();
			double workspaceHeight = WorkspaceHeight();
			double width = UiConstants.DefaultDocumentWindowWidth;
			double height = UiConstants.DefaultDocumentWindowHeight;
			Document model = window.DocumentModel();
			if (model != null)
			{
				double density = Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;
				if (density < 0.1)
				{
					density = 1.0;
				}
				double canvasDipWidth = model.Width() / density;
				double canvasDipHeight = model.Height() / density;
				width = System.Math.Ceiling(canvasDipWidth) + WindowChromeWidth() + 2.0;
				height = System.Math.Ceiling(canvasDipHeight) + WindowChromeHeight() + 2.0;
				if (width < UiConstants.PanelMinWidth)
				{
					width = UiConstants.PanelMinWidth;
				}
				if (height < UiConstants.PanelMinHeight)
				{
					height = UiConstants.PanelMinHeight;
				}
			}
			if (workspaceWidth > 100.0 && workspaceHeight > 100.0)
			{
				double maximumWidth = workspaceWidth - 16.0;
				double maximumHeight = workspaceHeight - 16.0;
				if (width > maximumWidth)
				{
					width = maximumWidth;
				}
				if (height > maximumHeight)
				{
					height = maximumHeight;
				}
			}
			double offset = m_cascadeCount * UiConstants.CascadeOffset;
			m_cascadeCount++;
			double x = 20.0 + offset;
			double y = 16.0 + offset;
			if (workspaceWidth > 100.0 && x + width > workspaceWidth - 8.0)
			{
				x = workspaceWidth - 8.0 - width;
				if (x < 8.0)
				{
					x = 8.0;
				}
			}
			if (workspaceHeight > 100.0 && y + height > workspaceHeight - 8.0)
			{
				y = workspaceHeight - 8.0 - height;
				if (y < 8.0)
				{
					y = 8.0;
				}
			}
			AddDocument(window, x, y, width, height);
		}

		public void DoCascadeWindows()
		{
			System.Collections.Generic.List<DocumentWindow> windows = m_documents;
			double workspaceWidth = WorkspaceWidth();
			double workspaceHeight = WorkspaceHeight();
			if (workspaceWidth <= 100.0 || workspaceHeight <= 100.0)
			{
				return;
			}
			double width = workspaceWidth * 0.72;
			double height = workspaceHeight * 0.74;
			for (int index = 0; index < windows.Count; index++)
			{
				double offset = index * UiConstants.CascadeOffset;
				windows[index].SetBounds(20.0 + offset, 16.0 + offset, width, height);
				BringToFront(windows[index]);
			}
			m_cascadeCount = windows.Count;
		}

		public void DoTileWindows()
		{
			System.Collections.Generic.List<DocumentWindow> windows = m_documents;
			int count = windows.Count;
			if (count == 0)
			{
				return;
			}
			double workspaceWidth = WorkspaceWidth();
			double workspaceHeight = WorkspaceHeight();
			if (workspaceWidth <= 100.0 || workspaceHeight <= 100.0)
			{
				return;
			}
			int columns = (int)System.Math.Ceiling(System.Math.Sqrt(count));
			int rows = (int)System.Math.Ceiling((double)count / columns);
			double cellWidth = workspaceWidth / columns;
			double cellHeight = workspaceHeight / rows;
			for (int index = 0; index < count; index++)
			{
				int row = index / columns;
				int column = index % columns;
				windows[index].SetBounds(column * cellWidth, row * cellHeight, cellWidth, cellHeight);
				BringToFront(windows[index]);
			}
		}

		public async void OpenImageFlow()
		{
			try
			{
				string path = await FileDialogs.PickOpenAsync();
				if (path == null)
				{
					return;
				}
				OpenDocumentFromPath(path);
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Open failed: " + error.Message);
			}
		}

		public void OpenDocumentFromPath(string path)
		{
			try
			{
				if (path.ToLowerInvariant().EndsWith(".bitmute"))
				{
					Document project = BitmuteFile.Read(path);
					if (project == null)
					{
						SetStatusMessage("Failed to open project");
						return;
					}
					project.SetSourcePath(path);
					RecentFiles.Add(path);
					DocumentWindow projectWindow = new DocumentWindow(project);
					PlaceAndAdd(projectWindow);
					return;
				}
				SkiaSharp.SKBitmap bitmap = ImageFile.DecodeFile(path);
				if (bitmap == null)
				{
					SetStatusMessage("Failed to open image");
					return;
				}
				string title = System.IO.Path.GetFileName(path);
				Document model = Document.OpenImage(title, bitmap);
				model.SetSourcePath(path);
				RecentFiles.Add(path);
				bitmap.Dispose();
				DocumentWindow window = new DocumentWindow(model);
				PlaceAndAdd(window);
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Open failed: " + error.Message);
			}
		}

		public async void SaveImageFlow()
		{
			Document model = ActiveDocument();
			if (model == null)
			{
				return;
			}
			await SaveDocumentAsync(model);
		}

		private static string SuggestedSaveName(Document model)
		{
			string title = model.Title();
			if (title == null || title.Length == 0)
			{
				return "Untitled";
			}
			return System.IO.Path.GetFileNameWithoutExtension(title);
		}

		public async void SaveAsFlow()
		{
			Document model = ActiveDocument();
			if (model == null)
			{
				SetStatusMessage("No document to save");
				return;
			}
			try
			{
				string path = await FileDialogs.PickSaveAsync(SuggestedSaveName(model));
				if (path == null)
				{
					return;
				}
				bool success = WriteDocumentTo(model, path);
				if (!success)
				{
					SetStatusMessage("Save failed");
					return;
				}
				model.SetSourcePath(path);
				RecentFiles.Add(path);
				model.MarkClean();
				SetStatusMessage("Saved " + System.IO.Path.GetFileName(path));
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Save failed: " + error.Message);
			}
		}

		private static bool WriteDocumentTo(Document model, string path)
		{
			if (path.ToLowerInvariant().EndsWith(".bitmute"))
			{
				return BitmuteFile.Write(path, model);
			}
			ImageFile.Encode(model, path);
			return true;
		}

		private async System.Threading.Tasks.Task<bool> SaveAsBitmuteAsync(Document model)
		{
			string path = await FileDialogs.PickSaveTypedAsync(SuggestedSaveName(model), "Bitmute Project", ".bitmute");
			if (path == null)
			{
				return false;
			}
			bool success = BitmuteFile.Write(path, model);
			if (!success)
			{
				SetStatusMessage("Save failed");
				return false;
			}
			model.SetSourcePath(path);
			RecentFiles.Add(path);
			model.MarkClean();
			SetStatusMessage("Saved " + System.IO.Path.GetFileName(path));
			return true;
		}

		public void OpenExportDialog()
		{
			if (ActiveDocument() == null)
			{
				SetStatusMessage("No document to export");
				return;
			}
			ShowModal(new ExportDialog(), 340.0, 280.0);
		}

		private static string ExportLabel(string format)
		{
			if (format == "jpeg")
			{
				return "JPEG Image";
			}
			if (format == "bmp")
			{
				return "Bitmap Image";
			}
			if (format == "tga")
			{
				return "TGA Image";
			}
			if (format == "webp")
			{
				return "WebP Image";
			}
			return "PNG Image";
		}

		private static string ExportExtension(string format)
		{
			if (format == "jpeg")
			{
				return ".jpg";
			}
			if (format == "bmp")
			{
				return ".bmp";
			}
			if (format == "tga")
			{
				return ".tga";
			}
			if (format == "webp")
			{
				return ".webp";
			}
			return ".png";
		}

		public async void ConfirmExport(string format, int quality, bool lossless, bool rle)
		{
			CloseModal();
			Document model = ActiveDocument();
			if (model == null)
			{
				SetStatusMessage("No document to export");
				return;
			}
			model.CommitFloatingSelection();
			try
			{
				string path = await FileDialogs.PickSaveTypedAsync(model.Title(), ExportLabel(format), ExportExtension(format));
				if (path == null)
				{
					return;
				}
				bool success = ImageFile.Export(model, path, format, quality, lossless, rle);
				if (success)
				{
					SetStatusMessage("Exported " + System.IO.Path.GetFileName(path));
				}
				else
				{
					SetStatusMessage("Export failed");
				}
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Export failed: " + error.Message);
			}
		}

		public void ClearRecentFiles()
		{
			RecentFiles.Clear();
			SetStatusMessage("Recent files cleared");
		}

		public int CurrentUndoDepth()
		{
			return Document.MaxUndoDepth();
		}

		public void ApplyUndoDepth(int depth)
		{
			Document.SetMaxUndoDepth(depth);
			Microsoft.Maui.Storage.Preferences.Default.Set("undo_depth", Document.MaxUndoDepth());
		}

		public async void OpenRepoLink()
		{
			try
			{
				await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(new System.Uri("https://github.com/therobm/Bitmute"));
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Could not open link: " + error.Message);
			}
		}

		private async System.Threading.Tasks.Task<bool> SaveDocumentAsync(Document model)
		{
			model.CommitFloatingSelection();
			try
			{
				string sourcePath = model.SourcePath();
				if (sourcePath == null || sourcePath.Length == 0)
				{
					return await SaveAsBitmuteAsync(model);
				}
				if (sourcePath.ToLowerInvariant().EndsWith(".bitmute"))
				{
					bool projectSaved = BitmuteFile.Write(sourcePath, model);
					if (!projectSaved)
					{
						SetStatusMessage("Save failed");
						return false;
					}
					model.MarkClean();
					SetStatusMessage("Saved " + System.IO.Path.GetFileName(sourcePath));
					return true;
				}
				if (model.FlatCompatible())
				{
					ImageFile.Encode(model, sourcePath);
					model.MarkClean();
					SetStatusMessage("Saved " + System.IO.Path.GetFileName(sourcePath));
					return true;
				}
				SetStatusMessage("Document no longer fits " + System.IO.Path.GetExtension(sourcePath) + " â€” saving as project");
				return await SaveAsBitmuteAsync(model);
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Save failed: " + error.Message);
				return false;
			}
		}

		public void SetStatusMessage(string message)
		{
			if (m_statusCursorLabel != null)
			{
				m_statusCursorLabel.Text = message;
			}
		}

		public void AddDocument(DocumentWindow window, double x, double y, double width, double height)
		{
			m_documents.Add(window);
			m_workspace.Add(window);
			window.SetBounds(x, y, width, height);
			BringToFront(window);
		}

		public DocumentWindow ActiveWindow()
		{
			return m_activeDocumentWindow;
		}

		private void InvalidateAllDocumentWindows()
		{
			for (int index = 0; index < m_documents.Count; index++)
			{
				m_documents[index].Canvas().InvalidateSurface();
			}
		}

		public void BringToFront(FloatingPanel panel)
		{
			m_topZIndex++;
			panel.ZIndex = m_topZIndex;
			DocumentWindow window = panel as DocumentWindow;
			if (window != null)
			{
				if (m_activeDocumentWindow != null && m_activeDocumentWindow != window)
				{
					m_activeDocumentWindow.CommitTextEdit();
				}
				m_activeDocumentWindow = window;
				RefreshDocumentTitleBars();
				RefreshPanels();
			}
		}

		private void RefreshDocumentTitleBars()
		{
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index];
				window.SetTitleBarActive(window == m_activeDocumentWindow);
			}
		}

		public CanvasView ActiveCanvas()
		{
			if (m_activeDocumentWindow == null)
			{
				return null;
			}
			return m_activeDocumentWindow.Canvas();
		}

		public void ActivateDocumentWindow(DocumentWindow window)
		{
			if (window == null)
			{
				return;
			}
			Dispatcher.Dispatch(FocusKeyboardSinkDeferred);
			if (m_activeDocumentWindow == window)
			{
				return;
			}
			BringToFront(window);
		}

		public Document ActiveDocument()
		{
			if (m_activeDocumentWindow == null)
			{
				return null;
			}
			return m_activeDocumentWindow.DocumentModel();
		}

		public void RefreshPanels()
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
			if (m_navigatorPanel != null)
			{
				m_navigatorPanel.RefreshView();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void OnPaletteTabChanged()
		{
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void OnCanvasInteracted()
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
		}

		private void OnModalBackdropTapped(object sender, TappedEventArgs eventArgs)
		{
			CloseModal();
		}

		private void OnWorkspaceDoubleTapped(object sender, TappedEventArgs eventArgs)
		{
			OpenImageFlow();
		}

		private void OnWorkspaceSizeChanged(object sender, EventArgs eventArgs)
		{
			double width = m_workspace.Width;
			double height = m_workspace.Height;
			if (width <= 0.0 || height <= 0.0)
			{
				return;
			}
			RectangleGeometry geometry = new RectangleGeometry();
			geometry.Rect = new Rect(0.0, 0.0, width, height);
			m_workspace.Clip = geometry;
		}

		public void ShowModal(View content, double width, double height)
		{
			BoxView backdrop = new BoxView();
			backdrop.Color = Colors.Transparent;
			AbsoluteLayout.SetLayoutBounds(backdrop, new Rect(0.0, 0.0, 1.0, 1.0));
			AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
			TapGestureRecognizer backdropTap = new TapGestureRecognizer();
			backdropTap.Tapped += OnModalBackdropTapped;
			backdrop.GestureRecognizers.Add(backdropTap);
			m_topZIndex = m_topZIndex + 1;
			backdrop.ZIndex = m_topZIndex + 1000;
			m_workspace.Add(backdrop);

			ModalEntry entry = new ModalEntry();
			entry.m_content = content;
			entry.m_backdrop = backdrop;
			entry.m_width = width;
			entry.m_height = height;
			entry.m_x = (m_workspace.Width - width) / 2.0;
			entry.m_y = (m_workspace.Height - height) / 2.0;
			if (entry.m_x < 0.0)
			{
				entry.m_x = 0.0;
			}
			if (entry.m_y < 0.0)
			{
				entry.m_y = 0.0;
			}
			m_modalStack.Add(entry);

			AbsoluteLayout.SetLayoutBounds(content, new Rect(entry.m_x, entry.m_y, width, AbsoluteLayout.AutoSize));
			AbsoluteLayout.SetLayoutFlags(content, AbsoluteLayoutFlags.None);
			content.ZIndex = m_topZIndex + 1001;
			m_workspace.Add(content);
		}

		public void DragModal(Microsoft.Maui.GestureStatus status, double totalX, double totalY)
		{
			if (m_modalStack.Count == 0)
			{
				return;
			}
			ModalEntry entry = m_modalStack[m_modalStack.Count - 1];
			if (status == Microsoft.Maui.GestureStatus.Started)
			{
				entry.m_dragOriginX = entry.m_x;
				entry.m_dragOriginY = entry.m_y;
				return;
			}
			if (status != Microsoft.Maui.GestureStatus.Running)
			{
				return;
			}
			double targetX = entry.m_dragOriginX + totalX;
			double targetY = entry.m_dragOriginY + totalY;
			double clampWidth = entry.m_content.Width;
			if (clampWidth <= 0.0)
			{
				clampWidth = entry.m_width;
			}
			double clampHeight = entry.m_content.Height;
			if (clampHeight <= 0.0)
			{
				clampHeight = entry.m_height;
			}
			double maxX = m_workspace.Width - clampWidth;
			double maxY = m_workspace.Height - clampHeight;
			if (targetX < 0.0)
			{
				targetX = 0.0;
			}
			if (targetY < 0.0)
			{
				targetY = 0.0;
			}
			if (maxX >= 0.0 && targetX > maxX)
			{
				targetX = maxX;
			}
			if (maxY >= 0.0 && targetY > maxY)
			{
				targetY = maxY;
			}
			entry.m_x = targetX;
			entry.m_y = targetY;
			AbsoluteLayout.SetLayoutBounds(entry.m_content, new Rect(entry.m_x, entry.m_y, entry.m_width, AbsoluteLayout.AutoSize));
		}

		public bool HasOpenModal()
		{
			return m_modalStack.Count > 0;
		}

		public void CloseModal()
		{
			m_editingSwatchIndex = -1;
			if (m_modalStack.Count == 0)
			{
				return;
			}
			ModalEntry entry = m_modalStack[m_modalStack.Count - 1];
			m_modalStack.RemoveAt(m_modalStack.Count - 1);
			if (entry.m_backdrop != null)
			{
				m_workspace.Remove(entry.m_backdrop);
			}
			if (entry.m_content != null)
			{
				m_workspace.Remove(entry.m_content);
			}
			if (entry.m_content is SaveChangesDialog)
			{
				m_quitPending = false;
			}
			ColorPicker cancelledPicker = entry.m_content as ColorPicker;
			if (cancelledPicker != null)
			{
				cancelledPicker.RevertLivePreview();
			}
			PreviewDialog previewDialog = entry.m_content as PreviewDialog;
			if (previewDialog != null)
			{
				previewDialog.CancelPreview();
			}
			if (entry.m_content is LayerStyleDialog && m_layerStyleSnapshot != null)
			{
				Layer layer = LayerStyleTargetLayer();
				if (layer != null)
				{
					layer.SetLayerStyle(m_layerStyleSnapshot);
					CanvasView canvas = ActiveCanvas();
					if (canvas != null)
					{
						canvas.MarkComposeDirty();
						canvas.InvalidateSurface();
					}
				}
				m_layerStyleSnapshot = null;
			}
			if (m_modalStack.Count == 0)
			{
				Dispatcher.Dispatch(FocusKeyboardSinkDeferred);
			}
		}

		public void FocusKeyboardSink()
		{
			if (m_modalStack.Count > 0)
			{
				return;
			}
			if (IsTextEditActive())
			{
				return;
			}
			if (m_focusSink == null)
			{
				m_focusSink = new Button();
				m_focusSink.Opacity = 0.0;
				m_focusSink.WidthRequest = 1.0;
				m_focusSink.HeightRequest = 1.0;
				m_focusSink.ZIndex = 0;
				AbsoluteLayout.SetLayoutBounds(m_focusSink, new Rect(0.0, 0.0, 1.0, 1.0));
				AbsoluteLayout.SetLayoutFlags(m_focusSink, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
				m_focusSink.HandlerChanged += OnFocusSinkHandlerChanged;
				m_workspace.Add(m_focusSink);
			}
			m_focusSink.Focus();
		}

		private void OnFocusSinkHandlerChanged(object sender, EventArgs eventArgs)
		{
			if (m_focusSinkKeyHooked)
			{
				return;
			}
			if (m_focusSink == null || m_focusSink.Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = m_focusSink.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			element.PreviewKeyDown += OnFocusSinkPreviewKeyDown;
			m_focusSinkKeyHooked = true;
		}

		private void OnFocusSinkPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs eventArgs)
		{
			if (eventArgs.Key != Windows.System.VirtualKey.Enter)
			{
				return;
			}
			eventArgs.Handled = true;
			CommitArmedOperation();
		}

		private void FocusKeyboardSinkDeferred()
		{
			FocusKeyboardSink();
		}

		public void RestoreKeyboardFocusDeferred()
		{
			Dispatcher.Dispatch(FocusKeyboardSinkDeferred);
		}

		public void OpenColorPicker(bool foreground)
		{
			m_editingSwatchIndex = -1;
			SKColor initial = m_toolState.Background();
			if (foreground)
			{
				initial = m_toolState.Foreground();
			}
			ColorPicker picker = new ColorPicker(initial, foreground);
			ShowModal(picker, 380.0, 360.0);
		}

		public void OpenSwatchColorPicker(int index, SKColor current)
		{
			ColorPicker picker = new ColorPicker(current, true);
			ShowModal(picker, 380.0, 360.0);
			m_editingSwatchIndex = index;
		}

		public void SetLiveForeground(SKColor color)
		{
			m_toolState.SetForeground(color);
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
			if (m_optionsBar != null)
			{
				m_optionsBar.UpdateTextColorSwatch(color);
			}
			RefreshTextEditStyle();
		}

		public void SetLiveBackground(SKColor color)
		{
			m_toolState.SetBackground(color);
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
		}

		public bool EditingSwatch()
		{
			return m_editingSwatchIndex >= 0;
		}

		public void ApplyPickedColor(SKColor color, bool foreground)
		{
			if (m_editingSwatchIndex >= 0)
			{
				int target = m_editingSwatchIndex;
				m_editingSwatchIndex = -1;
				if (m_swatchesPanel != null)
				{
					m_swatchesPanel.SetSwatchColor(target, color);
				}
				return;
			}
			if (foreground)
			{
				m_toolState.SetForeground(color);
			}
			else
			{
				m_toolState.SetBackground(color);
			}
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
			if (foreground && m_optionsBar != null)
			{
				m_optionsBar.UpdateTextColorSwatch(color);
			}
			if (m_swatchesPanel != null)
			{
				m_swatchesPanel.AddRecent(color);
			}
			RefreshTextEditStyle();
		}

		public async void ShowNewDocumentDialog()
		{
			int initialWidth = 0;
			int initialHeight = 0;
			SkiaSharp.SKBitmap clipboard = await GetSystemClipboardBitmap();
			if (clipboard != null)
			{
				initialWidth = clipboard.Width;
				initialHeight = clipboard.Height;
				clipboard.Dispose();
			}
			else if (s_clipboardBitmap != null)
			{
				initialWidth = s_clipboardBitmap.Width;
				initialHeight = s_clipboardBitmap.Height;
			}
			NewDocumentDialog dialog;
			if (initialWidth > 0 && initialHeight > 0)
			{
				dialog = new NewDocumentDialog(initialWidth, initialHeight);
			}
			else
			{
				dialog = new NewDocumentDialog();
			}
			ShowModal(dialog, 320.0, 280.0);
		}

		public void CreateNewDocument(int width, int height, string name, bool transparent)
		{
			m_untitledCount = m_untitledCount + 1;
			string title = name;
			if (title == null || title.Length == 0)
			{
				title = "Untitled-" + m_untitledCount;
			}
			Document model = new Document(title, width, height);
			if (transparent)
			{
				Layer background = model.ActiveLayer();
				background.Bitmap().Erase(SKColors.Transparent);
				background.SetIsBackground(false);
				background.SetName("Layer 1");
			}
			DocumentWindow window = new DocumentWindow(model);
			PlaceAndAdd(window);
		}

		public void RefreshActiveLayerThumbnail()
		{
			if (m_layersPanel != null)
			{
				m_layersPanel.RefreshActiveThumbnail();
			}
			if (m_navigatorPanel != null)
			{
				m_navigatorPanel.RefreshView();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void RefreshLayerThumbnails()
		{
			if (m_layersPanel != null)
			{
				m_layersPanel.RefreshThumbnails();
			}
			if (m_navigatorPanel != null)
			{
				m_navigatorPanel.RefreshView();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void ClosePanel(FloatingPanel panel)
		{
			DocumentWindow window = panel as DocumentWindow;
			if (window != null)
			{
				Document model = window.DocumentModel();
				if (model != null && model.IsDirty())
				{
					m_pendingClosePanel = panel;
					ShowModal(new SaveChangesDialog(model.Title()), 360.0, 170.0);
					return;
				}
			}
			RemovePanel(panel);
		}

		public void OnCloseSaveChanges()
		{
			bool quitting = m_quitPending;
			CloseModal();
			m_quitPending = quitting;
			FloatingPanel panel = m_pendingClosePanel;
			m_pendingClosePanel = null;
			if (panel == null)
			{
				return;
			}
			SaveThenClose(panel);
		}

		private async void SaveThenClose(FloatingPanel panel)
		{
			DocumentWindow window = panel as DocumentWindow;
			if (window == null)
			{
				return;
			}
			Document model = window.DocumentModel();
			if (model == null)
			{
				RemovePanel(panel);
				return;
			}
			bool saved = await SaveDocumentAsync(model);
			if (saved)
			{
				RemovePanel(panel);
				return;
			}
			m_quitPending = false;
		}

		public void OnCloseDontSave()
		{
			bool quitting = m_quitPending;
			CloseModal();
			m_quitPending = quitting;
			FloatingPanel panel = m_pendingClosePanel;
			m_pendingClosePanel = null;
			if (panel != null)
			{
				RemovePanel(panel);
			}
		}

		public void OnCloseCancelSave()
		{
			CloseModal();
			m_pendingClosePanel = null;
			m_quitPending = false;
		}

		private DocumentWindow TopmostDocumentWindow()
		{
			DocumentWindow topmost = null;
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index];
				if (topmost == null || window.ZIndex >= topmost.ZIndex)
				{
					topmost = window;
				}
			}
			return topmost;
		}

		private void ClearClosedDocumentReadouts()
		{
			if (m_statusInfoLabel != null)
			{
				m_statusInfoLabel.Text = "";
			}
			if (m_statusCursorLabel != null)
			{
				m_statusCursorLabel.Text = "x: â€”   y: â€”";
			}
			if (m_infoPanel != null)
			{
				m_infoPanel.ClearReadout();
			}
		}

		private void RemovePanel(FloatingPanel panel)
		{
			DocumentWindow window = panel as DocumentWindow;
			if (window == null || !m_documents.Contains(window))
			{
				return;
			}
			m_documents.Remove(window);
			m_workspace.Remove(window);
			if (window.Canvas() != null)
			{
				window.Canvas().ReleaseGpuResources();
			}
			if (m_activeDocumentWindow == window)
			{
				m_activeDocumentWindow = null;
				DocumentWindow next = TopmostDocumentWindow();
				if (next != null)
				{
					BringToFront(next);
				}
				else
				{
					RefreshDocumentTitleBars();
					RefreshPanels();
					ClearClosedDocumentReadouts();
				}
			}
			if (m_quitPending)
			{
				Dispatcher.Dispatch(ContinueQuitClose);
			}
		}

		public void OnToolSelected(eTool tool)
		{
			ClosePulldown();
			if (m_toolState != null)
			{
				m_toolState.SetTool(tool);
			}
			if (m_optionsBar != null)
			{
				m_optionsBar.ShowForTool(tool);
			}
			if (tool != eTool.Text)
			{
				CommitTextEdit();
			}
			if (m_toolBox != null)
			{
				m_toolBox.ResetAll();
			}
		}

		public bool GridEnabled()
		{
			return m_workspaceState.GridEnabled();
		}

		public int ChannelViewMode()
		{
			return m_workspaceState.ChannelViewMode();
		}

		public void SelectChannelView(int mode)
		{
			SetChannelView(mode);
		}

		private void SetChannelView(int mode)
		{
			m_workspaceState.SetChannelViewMode(mode);
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public bool ChannelVisible(int channel)
		{
			return m_workspaceState.ChannelVisible(channel);
		}

		public bool AllChannelsVisible()
		{
			return m_workspaceState.AllChannelsVisible();
		}

		public bool RgbChannelsVisible()
		{
			return m_workspaceState.RgbChannelsVisible();
		}

		public void ToggleChannelVisible(int channel)
		{
			if (channel < 0 || channel > 3)
			{
				return;
			}
			m_workspaceState.SetChannelVisible(channel, !m_workspaceState.ChannelVisible(channel));
			ApplyChannelVisibilityChange();
		}

		public void ToggleRgbChannelsVisible()
		{
			bool target = !m_workspaceState.RgbChannelsVisible();
			m_workspaceState.SetChannelVisible(0, target);
			m_workspaceState.SetChannelVisible(1, target);
			m_workspaceState.SetChannelVisible(2, target);
			ApplyChannelVisibilityChange();
		}

		private void ApplyChannelVisibilityChange()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void ClosePulldown()
		{
			if (m_pulldownPanel != null)
			{
				m_overlay.Remove(m_pulldownPanel);
				m_pulldownPanel = null;
			}
		}

		public bool PulldownOpen()
		{
			return m_pulldownPanel != null;
		}

		public bool PulldownJustDismissed()
		{
			return (System.Environment.TickCount64 - m_pulldownDismissTick) < 300;
		}

		public void ShowPulldown(View content, double anchorX, double anchorY, double width, double height)
		{
			ClosePulldown();
			m_menuBar.CloseOpenMenu();
			m_pulldownShieldTick = System.Environment.TickCount64;

			Border panel = new Border();
			panel.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			panel.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			panel.StrokeThickness = 1.0;
			panel.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			panel.Content = content;

			double overlayWidth = m_overlay.Width;
			if (overlayWidth > 0.0 && anchorX + width > overlayWidth)
			{
				anchorX = overlayWidth - width;
			}
			if (anchorX < 0.0)
			{
				anchorX = 0.0;
			}
			AbsoluteLayout.SetLayoutFlags(panel, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(panel, new Rect(anchorX, anchorY, width, height));
			m_overlay.Add(panel);
			m_pulldownPanel = panel;
		}

		public void ApplyBrushTip(bool square)
		{
			if (m_optionsBar != null)
			{
				m_optionsBar.ApplyBrushTip(square);
			}
		}

		public void ApplyBrushSpacing(int spacing)
		{
			if (m_optionsBar != null)
			{
				m_optionsBar.ApplyBrushSpacing(spacing);
			}
		}

		public static Color FromSkColor(SkiaSharp.SKColor color)
		{
			return new Color(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
		}

		public AbsoluteLayout WorkspaceLayout()
		{
			return m_workspace;
		}

		private TextEditSession ActiveTextSession()
		{
			if (m_activeDocumentWindow == null)
			{
				return null;
			}
			return m_activeDocumentWindow.TextSessionOrNull();
		}

		public bool IsTextEditActive()
		{
			TextEditSession session = ActiveTextSession();
			if (session == null)
			{
				return false;
			}
			return session.IsActive();
		}

		public CanvasView TextEditCanvas()
		{
			TextEditSession session = ActiveTextSession();
			if (session == null)
			{
				return null;
			}
			return session.EditCanvas();
		}

		public Bitmute.Imaging.Layer TextEditLayer()
		{
			TextEditSession session = ActiveTextSession();
			if (session == null)
			{
				return null;
			}
			return session.EditLayer();
		}

		public int TextCaretIndex()
		{
			TextEditSession session = ActiveTextSession();
			if (session == null)
			{
				return 0;
			}
			return session.CaretIndex();
		}

		public int TextSelectionStart()
		{
			TextEditSession session = ActiveTextSession();
			if (session == null)
			{
				return 0;
			}
			return session.SelectionStart();
		}

		public int TextSelectionLength()
		{
			TextEditSession session = ActiveTextSession();
			if (session == null)
			{
				return 0;
			}
			return session.SelectionLength();
		}

		public bool CaretVisible()
		{
			TextEditSession session = ActiveTextSession();
			if (session == null)
			{
				return false;
			}
			return session.CaretVisible();
		}

		public void PlaceText(CanvasView canvas, int x, int y, float deviceX, float deviceY)
		{
			DocumentWindow window = canvas.OwnerWindow();
			if (window == null)
			{
				return;
			}
			window.TextSession().PlaceText(x, y, deviceX, deviceY);
		}

		public void BeginTextEditForLayer(Bitmute.Imaging.Layer layer)
		{
			if (m_activeDocumentWindow == null)
			{
				return;
			}
			m_activeDocumentWindow.TextSession().BeginForLayer(layer);
		}

		public void CommitTextEdit()
		{
			TextEditSession session = ActiveTextSession();
			if (session != null)
			{
				session.Commit();
			}
		}

		public void DoRasterizeText()
		{
			if (m_activeDocumentWindow == null)
			{
				return;
			}
			m_activeDocumentWindow.TextSession().Rasterize();
		}

		public void RefreshTextEditStyle()
		{
			TextEditSession session = ActiveTextSession();
			if (session != null)
			{
				session.RefreshStyle();
			}
		}

		public void SelectTool(eTool tool)
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.SelectToolExternal(tool);
				return;
			}
			m_toolState.SetTool(tool);
			OnToolSelected(tool);
		}

		public void SyncTextOptionsBar()
		{
			if (m_optionsBar != null)
			{
				m_optionsBar.SyncTextOptions();
			}
		}

		public void RefreshToolPaletteColors()
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
		}

		public void SwapToolColors()
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.SwapColors();
			}
		}

		public void SelectToolKey(eTool primaryTool, bool cycle)
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.ActivateToolKey(primaryTool, cycle);
			}
		}

		public void ShowRulerUnitsMenu(double anchorX, double anchorY)
		{
			VerticalStackLayout menu = new VerticalStackLayout();
			menu.Padding = new Thickness(4.0);
			menu.Add(BuildRulerUnitRow("Pixels", OnRulerUnitsPixels));
			menu.Add(BuildRulerUnitRow("Millimeters", OnRulerUnitsMillimeters));
			menu.Add(BuildRulerUnitRow("Centimeters", OnRulerUnitsCentimeters));
			menu.Add(BuildRulerUnitRow("Percent", OnRulerUnitsPercent));
			ShowPulldown(menu, anchorX, anchorY, 130.0, 100.0);
		}

		private Label BuildRulerUnitRow(string text, EventHandler<TappedEventArgs> handler)
		{
			Label row = new Label();
			row.Text = text;
			row.FontSize = UiConstants.ComponentFontSize;
			row.Padding = new Thickness(8.0, 3.0, 8.0, 3.0);
			row.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += handler;
			row.GestureRecognizers.Add(tap);
			return row;
		}

		private void ApplyRulerUnits(eRulerUnits units)
		{
			ClosePulldown();
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			document.SetRulerUnits(units);
			if (m_activeDocumentWindow != null)
			{
				m_activeDocumentWindow.RefreshChrome();
			}
		}

		private void OnRulerUnitsPixels(object sender, TappedEventArgs eventArgs)
		{
			ApplyRulerUnits(eRulerUnits.Pixels);
		}

		private void OnRulerUnitsMillimeters(object sender, TappedEventArgs eventArgs)
		{
			ApplyRulerUnits(eRulerUnits.Millimeters);
		}

		private void OnRulerUnitsCentimeters(object sender, TappedEventArgs eventArgs)
		{
			ApplyRulerUnits(eRulerUnits.Centimeters);
		}

		private void OnRulerUnitsPercent(object sender, TappedEventArgs eventArgs)
		{
			ApplyRulerUnits(eRulerUnits.Percent);
		}

		public void OpenBrushOptionsAt(double anchorX, double anchorY)
		{
			Tool tool = CurrentTool();
			if (!(tool is BrushFamilyTool))
			{
				return;
			}
			if (m_optionsBar != null)
			{
				m_optionsBar.OpenBrushSettingsAt(anchorX, anchorY);
			}
		}

		public void RefreshLayersPanel()
		{
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
		}

		public ToolState CurrentToolState()
		{
			return m_toolState;
		}

		public Tool CurrentTool()
		{
			return m_toolBox.Instance(m_toolState.Tool());
		}

		public bool SnapEnabled()
		{
			return m_workspaceState.SnapEnabled();
		}

		public bool SnapTargetGuides()
		{
			return m_workspaceState.SnapTargetGuides();
		}

		public bool SnapTargetGrid()
		{
			return m_workspaceState.SnapTargetGrid();
		}

		public bool SnapTargetEdges()
		{
			return m_workspaceState.SnapTargetEdges();
		}

		public bool SnapTargetLayerBounds()
		{
			return m_workspaceState.SnapTargetLayerBounds();
		}

		public void BeginTransform(int mode)
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null || layer.IsText())
			{
				SetStatusMessage("Select a raster layer to transform");
				return;
			}
			if (m_toolState.Tool() != eTool.FreeTransform)
			{
				m_toolBox.SetPreviousTool(m_toolState.Tool());
			}
			OnToolSelected(eTool.FreeTransform);
			bool armed = m_toolBox.FreeTransform().Begin(document, mode, m_toolState.Background());
			if (armed)
			{
				RestoreKeyboardFocusDeferred();
			}
			if (!armed)
			{
				SetStatusMessage("Cannot transform this layer");
				OnToolSelected(m_toolBox.PreviousTool());
				return;
			}
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
		}

		public void EndTransformMode()
		{
			if (m_toolState.Tool() != eTool.FreeTransform)
			{
				return;
			}
			OnToolSelected(m_toolBox.PreviousTool());
		}

		private void OnSystemThemeChanged(object sender, Microsoft.Maui.Controls.AppThemeChangedEventArgs eventArgs)
		{
			Theme.OnSystemThemeChanged();
		}

		public double WorkspaceWidth()
		{
			return m_workspace.Width;
		}

		public double WorkspaceHeight()
		{
			return m_workspace.Height;
		}
	}
}
