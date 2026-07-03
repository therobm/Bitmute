using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Bitmute.Storage;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using SkiaSharp;

namespace Bitmute.UI
{
	public class MainView : ContentPage
	{
		public static MainView Self;

		private const double MenuItemHeight = 26.0;
		private const double DropdownWidth = 190.0;

		private AbsoluteLayout m_workspace;
		private AbsoluteLayout m_overlay;
		private List<FloatingPanel> m_documents;
		private DocumentWindow m_activeDocumentWindow;
		private ToolPalette m_toolPalette;
		private LayersPanel m_layersPanel;
		private bool m_rulersEnabled = true;
		private BoxView m_modalBackdrop;
		private View m_modalContent;
		private double m_modalX;
		private double m_modalY;
		private double m_modalWidth;
		private double m_modalHeight;
		private double m_modalDragOriginX;
		private double m_modalDragOriginY;
		private Label m_optionsToolLabel;
		private SliderField m_brushSizeField;
		private Label m_brushHardnessLabel;
		private SliderField m_brushHardnessField;
		private Label m_brushOpacityLabel;
		private SliderField m_brushOpacityField;
		private Label m_brushFlowLabel;
		private SliderField m_brushFlowField;
		private Label m_brushSmoothingLabel;
		private SliderField m_brushSmoothingField;
		private Button m_brushSettingsButton;
		private HorizontalStackLayout m_optionsRow;
		private View m_pulldownCatcher;
		private View m_pulldownPanel;
		private Picker m_brushTipPicker;
		private Slider m_brushSpacingSlider;
		private Label m_brushSpacingValue;
		private Label m_brushModeLabel;
		private Picker m_brushModePicker;
		private Label m_lineAntiAliasLabel;
		private CheckBox m_lineAntiAliasCheck;
		private Label m_statusInfoLabel;
		private Label m_statusCursorLabel;
		private string[] m_menuTitles;
		private Border[] m_menuButtons;
		private List<Border> m_openItemButtons;
		private List<string> m_openItemActions;
		private int m_openMenuIndex;
		private bool m_acceleratorsHooked;
		private int m_untitledCount;
		private int m_cascadeCount;
		private int m_topZIndex;
		private ToolState m_toolState;
		private MoveTool m_moveTool;
		private RectangleSelectTool m_rectangleSelectTool;
		private EllipseSelectTool m_ellipseSelectTool;
		private LassoTool m_lassoTool;
		private MagicWandTool m_magicWandTool;
		private TextTool m_textTool;
		private PencilTool m_pencilTool;
		private BrushTool m_brushTool;
		private EraserTool m_eraserTool;
		private DodgeBurnTool m_dodgeBurnTool;
		private BlurTool m_blurTool;
		private SharpenTool m_sharpenTool;
		private CloneTool m_cloneTool;
		private SmudgeTool m_smudgeTool;
		private EyedropperTool m_eyedropperTool;
		private FillTool m_fillTool;
		private LineTool m_lineTool;
		private HandTool m_handTool;
		private ZoomTool m_zoomTool;

		private string[] GetMenuItems(string title)
		{
			if (title == "File")
			{
				return new string[] { "New", "Open…", "Save…", "Exit" };
			}
			if (title == "Edit")
			{
				return new string[] { "Undo", "Redo", "Cut", "Copy", "Paste" };
			}
			if (title == "Image")
			{
				return new string[] { "Image Size…", "Canvas Size…", "Flip Horizontal", "Flip Vertical", "Rotate 90° CW", "Rotate 180°", "Rotate 90° CCW", "Crop to Selection", "Trim" };
			}
			if (title == "Layer")
			{
				return new string[] { "New Layer", "Delete Layer", "Merge Down" };
			}
			if (title == "Select")
			{
				return new string[] { "All", "Deselect", "Invert" };
			}
			if (title == "Filter")
			{
				return new string[] { "Brightness/Contrast…", "Hue/Saturation…", "Desaturate", "Invert Colors", "Posterize…", "Threshold…", "Gaussian Blur…", "Unsharp Mask…", "Add Noise…", "Pixelate…" };
			}
			if (title == "View")
			{
				return new string[] { "Zoom In", "Zoom Out", "Fit on Screen", "Rulers", "Toggle Light/Dark", "Use System Theme" };
			}
			if (title == "Window")
			{
				return new string[] { "Cascade", "Tile", "Tools", "Layers", "Color" };
			}
			return new string[] { "About Bitmute" };
		}

		private bool IsItemEnabled(string title, string item)
		{
			if (title == "File")
			{
				if (item == "New")
				{
					return true;
				}
				if (item == "Open…")
				{
					return true;
				}
				if (item == "Save…")
				{
					return true;
				}
				if (item == "Exit")
				{
					return true;
				}
				return false;
			}
			if (title == "Edit")
			{
				if (item == "Undo")
				{
					return true;
				}
				if (item == "Redo")
				{
					return true;
				}
				return false;
			}
			if (title == "Select")
			{
				return true;
			}
			if (title == "Filter")
			{
				return true;
			}
			if (title == "Image")
			{
				return true;
			}
			if (title == "Window")
			{
				if (item == "Cascade")
				{
					return true;
				}
				if (item == "Tile")
				{
					return true;
				}
				return false;
			}
			if (title == "View")
			{
				if (item == "Zoom In")
				{
					return true;
				}
				if (item == "Zoom Out")
				{
					return true;
				}
				if (item == "Fit on Screen")
				{
					return true;
				}
				if (item == "Toggle Light/Dark")
				{
					return true;
				}
				if (item == "Use System Theme")
				{
					return true;
				}
				return false;
			}
			return false;
		}

		private Border BuildMenuButton(int index)
		{
			Label label = new Label();
			label.Text = m_menuTitles[index];
			label.FontSize = 12.0;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;

			Border button = new Border();
			button.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			button.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			button.StrokeThickness = 0.0;
			button.Content = label;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnMenuButtonTapped;
			button.GestureRecognizers.Add(tap);

			PointerGestureRecognizer pointer = new PointerGestureRecognizer();
			pointer.PointerEntered += OnMenuButtonPointerEntered;
			button.GestureRecognizers.Add(pointer);

			return button;
		}

		private int FindMenuButtonIndex(object sender)
		{
			for (int index = 0; index < m_menuButtons.Length; index++)
			{
				if (ReferenceEquals(m_menuButtons[index], sender))
				{
					return index;
				}
			}
			return -1;
		}

		private void OnMenuButtonPointerEntered(object sender, PointerEventArgs eventArgs)
		{
			if (m_openMenuIndex < 0)
			{
				return;
			}
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (index != m_openMenuIndex)
			{
				OpenMenu(index);
			}
		}

		private View BuildMenuBar()
		{
			m_menuTitles = new string[] { "File", "Edit", "Image", "Layer", "Select", "Filter", "View", "Window", "Help" };
			m_menuButtons = new Border[m_menuTitles.Length];

			HorizontalStackLayout strip = new HorizontalStackLayout();
			strip.HeightRequest = UiConstants.MenuBarHeight;
			strip.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			strip.Spacing = 0.0;
			strip.Padding = new Thickness(0.0);

			for (int index = 0; index < m_menuTitles.Length; index++)
			{
				Border button = BuildMenuButton(index);
				m_menuButtons[index] = button;
				strip.Add(button);
			}

			return strip;
		}

		private void OnMenuButtonTapped(object sender, TappedEventArgs eventArgs)
		{
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (m_openMenuIndex == index)
			{
				CloseMenu();
				return;
			}
			OpenMenu(index);
		}

		private void OpenMenu(int index)
		{
			ClosePulldown();
			CloseMenu();
			m_openMenuIndex = index;
			m_menuButtons[index].ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);

			string title = m_menuTitles[index];
			string[] items = GetMenuItems(title);

			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);

			for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
			{
				list.Add(BuildMenuItem(title, items[itemIndex]));
			}

			Border dropdown = new Border();
			dropdown.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			dropdown.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			dropdown.StrokeThickness = 1.0;
			dropdown.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			dropdown.Content = list;

			BoxView catcher = new BoxView();
			catcher.Color = Colors.Transparent;
			TapGestureRecognizer catcherTap = new TapGestureRecognizer();
			catcherTap.Tapped += OnCatcherTapped;
			catcher.GestureRecognizers.Add(catcherTap);
			AbsoluteLayout.SetLayoutFlags(catcher, AbsoluteLayoutFlags.WidthProportional | AbsoluteLayoutFlags.HeightProportional);
			AbsoluteLayout.SetLayoutBounds(catcher, new Rect(0.0, UiConstants.MenuBarHeight, 1.0, 1.0));
			m_overlay.Add(catcher);

			double dropdownX = m_menuButtons[index].Bounds.X;
			double overlayWidth = m_overlay.Width;
			if (overlayWidth > 0.0 && dropdownX + DropdownWidth > overlayWidth)
			{
				dropdownX = overlayWidth - DropdownWidth;
			}
			if (dropdownX < 0.0)
			{
				dropdownX = 0.0;
			}
			double dropdownHeight = (items.Length * MenuItemHeight) + 8.0;
			AbsoluteLayout.SetLayoutFlags(dropdown, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(dropdown, new Rect(dropdownX, UiConstants.MenuBarHeight, DropdownWidth, dropdownHeight));
			m_overlay.Add(dropdown);
		}

		private Border BuildMenuItem(string title, string item)
		{
			bool enabled = IsItemEnabled(title, item);

			Label label = new Label();
			label.Text = item;
			label.FontSize = 12.0;
			label.VerticalOptions = LayoutOptions.Center;
			if (enabled)
			{
				label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			}
			else
			{
				label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			}

			Border row = new Border();
			row.HeightRequest = MenuItemHeight;
			row.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
			row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			row.StrokeThickness = 0.0;
			row.Content = label;

			if (enabled)
			{
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnMenuItemTapped;
				row.GestureRecognizers.Add(tap);
				PointerGestureRecognizer pointer = new PointerGestureRecognizer();
				pointer.PointerEntered += OnMenuItemPointerEntered;
				pointer.PointerExited += OnMenuItemPointerExited;
				row.GestureRecognizers.Add(pointer);
				m_openItemButtons.Add(row);
				m_openItemActions.Add(item);
			}

			return row;
		}

		private void OnMenuItemPointerEntered(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row != null)
			{
				row.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			}
		}

		private void OnMenuItemPointerExited(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row != null)
			{
				row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			}
		}

		private void OnMenuItemTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_openItemButtons.Count; index++)
			{
				if (ReferenceEquals(m_openItemButtons[index], sender))
				{
					string action = m_openItemActions[index];
					CloseMenu();
					InvokeMenuAction(action);
					return;
				}
			}
		}

		private void OnCatcherTapped(object sender, TappedEventArgs eventArgs)
		{
			CloseMenu();
		}

		private void CloseMenu()
		{
			m_overlay.Clear();
			m_openItemButtons.Clear();
			m_openItemActions.Clear();
			if (m_openMenuIndex >= 0)
			{
				m_menuButtons[m_openMenuIndex].ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			}
			m_openMenuIndex = -1;
		}

		private void InvokeMenuAction(string action)
		{
			if (action == "New")
			{
				ShowNewDocumentDialog();
				return;
			}
			if (action == "Undo")
			{
				DoUndo();
				return;
			}
			if (action == "Redo")
			{
				DoRedo();
				return;
			}
			if (action == "Open…")
			{
				OpenImageFlow();
				return;
			}
			if (action == "Save…")
			{
				SaveImageFlow();
				return;
			}
			if (action == "Exit")
			{
				DoExit();
				return;
			}
			if (action == "Zoom In")
			{
				DoZoomIn();
				return;
			}
			if (action == "Zoom Out")
			{
				DoZoomOut();
				return;
			}
			if (action == "Fit on Screen")
			{
				DoFit();
				return;
			}
			if (action == "Rulers")
			{
				ToggleRulers();
				return;
			}
			if (action == "Toggle Light/Dark")
			{
				Theme.Toggle();
				return;
			}
			if (action == "Use System Theme")
			{
				Theme.UseSystem();
				return;
			}
			if (action == "All")
			{
				DoSelectAll();
				return;
			}
			if (action == "Deselect")
			{
				DoDeselect();
				return;
			}
			if (action == "Invert")
			{
				DoInvertSelection();
				return;
			}
			if (action == "Invert Colors")
			{
				DoInvert();
				return;
			}
			if (action == "Desaturate")
			{
				DoDesaturate();
				return;
			}
			if (action == "Brightness/Contrast…")
			{
				OpenAdjustment("bc");
				return;
			}
			if (action == "Hue/Saturation…")
			{
				OpenAdjustment("hsl");
				return;
			}
			if (action == "Posterize…")
			{
				OpenAdjustment("posterize");
				return;
			}
			if (action == "Threshold…")
			{
				OpenAdjustment("threshold");
				return;
			}
			if (action == "Gaussian Blur…")
			{
				OpenAdjustment("gblur");
				return;
			}
			if (action == "Unsharp Mask…")
			{
				OpenAdjustment("unsharp");
				return;
			}
			if (action == "Add Noise…")
			{
				OpenAdjustment("noise");
				return;
			}
			if (action == "Pixelate…")
			{
				OpenAdjustment("pixelate");
				return;
			}
			if (action == "Flip Horizontal")
			{
				DoCanvasOp("fliph");
				return;
			}
			if (action == "Flip Vertical")
			{
				DoCanvasOp("flipv");
				return;
			}
			if (action == "Rotate 90° CW")
			{
				DoCanvasOp("rot90");
				return;
			}
			if (action == "Rotate 180°")
			{
				DoCanvasOp("rot180");
				return;
			}
			if (action == "Rotate 90° CCW")
			{
				DoCanvasOp("rot270");
				return;
			}
			if (action == "Crop to Selection")
			{
				DoCanvasOp("crop");
				return;
			}
			if (action == "Trim")
			{
				DoCanvasOp("trim");
				return;
			}
			if (action == "Cascade")
			{
				DoCascadeWindows();
				return;
			}
			if (action == "Tile")
			{
				DoTileWindows();
				return;
			}
			if (action == "Canvas Size…")
			{
				OpenSizeDialog(true);
				return;
			}
			if (action == "Image Size…")
			{
				OpenSizeDialog(false);
				return;
			}
		}

		private void OpenSizeDialog(bool canvasMode)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			string title = "Image Size";
			if (canvasMode)
			{
				title = "Canvas Size";
			}
			ShowModal(new SizeDialog(title, canvasMode, document.Width(), document.Height()), 340.0, 260.0);
		}

		private void FinishCanvasOp(CanvasView canvas, Document document)
		{
			document.ResetSelection();
			canvas.ResetView();
			canvas.MarkComposeDirty();
			RefreshLayerThumbnails();
		}

		private void DoCanvasOp(string op)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (op == "fliph")
			{
				document.FlipHorizontal();
			}
			else if (op == "flipv")
			{
				document.FlipVertical();
			}
			else if (op == "rot90")
			{
				document.Rotate90();
			}
			else if (op == "rot180")
			{
				document.Rotate180();
			}
			else if (op == "rot270")
			{
				document.Rotate270();
			}
			else if (op == "crop")
			{
				document.CropToSelection();
			}
			else if (op == "trim")
			{
				document.Trim();
			}
			FinishCanvasOp(canvas, document);
		}

		public void ApplyCanvasSize(int width, int height, int anchorX, int anchorY)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.ResizeCanvas(width, height, anchorX, anchorY);
			FinishCanvasOp(canvas, document);
		}

		public void ApplyImageSize(int width, int height, int interpolation)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.ScaleImage(width, height, interpolation);
			FinishCanvasOp(canvas, document);
		}

		private void OpenAdjustment(string id)
		{
			if (id == "bc")
			{
				ShowModal(new AdjustmentDialog("Brightness/Contrast", "bc", new string[] { "Brightness", "Contrast" }, new int[] { -100, -100 }, new int[] { 100, 100 }, new int[] { 0, 0 }), 360.0, 200.0);
				return;
			}
			if (id == "hsl")
			{
				ShowModal(new AdjustmentDialog("Hue/Saturation", "hsl", new string[] { "Hue", "Saturation", "Lightness" }, new int[] { -180, -100, -100 }, new int[] { 180, 100, 100 }, new int[] { 0, 0, 0 }), 360.0, 230.0);
				return;
			}
			if (id == "posterize")
			{
				ShowModal(new AdjustmentDialog("Posterize", "posterize", new string[] { "Levels" }, new int[] { 2 }, new int[] { 64 }, new int[] { 8 }), 360.0, 170.0);
				return;
			}
			if (id == "threshold")
			{
				ShowModal(new AdjustmentDialog("Threshold", "threshold", new string[] { "Level" }, new int[] { 0 }, new int[] { 255 }, new int[] { 128 }), 360.0, 170.0);
				return;
			}
			if (id == "gblur")
			{
				ShowModal(new AdjustmentDialog("Gaussian Blur", "gblur", new string[] { "Radius" }, new int[] { 1 }, new int[] { 30 }, new int[] { 5 }), 360.0, 170.0);
				return;
			}
			if (id == "unsharp")
			{
				ShowModal(new AdjustmentDialog("Unsharp Mask", "unsharp", new string[] { "Amount", "Radius" }, new int[] { 0, 1 }, new int[] { 300, 30 }, new int[] { 100, 3 }), 360.0, 200.0);
				return;
			}
			if (id == "noise")
			{
				ShowModal(new AdjustmentDialog("Add Noise", "noise", new string[] { "Amount" }, new int[] { 0 }, new int[] { 100 }, new int[] { 20 }), 360.0, 170.0);
				return;
			}
			if (id == "pixelate")
			{
				ShowModal(new AdjustmentDialog("Pixelate", "pixelate", new string[] { "Cell Size" }, new int[] { 2 }, new int[] { 64 }, new int[] { 8 }), 360.0, 170.0);
				return;
			}
		}

		public void ApplyAdjustment(string id, int first, int second, int third)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			SkiaSharp.SKBitmap bitmap = activeLayer.Bitmap();
			document.BeginStroke();
			if (id == "bc")
			{
				Adjustments.BrightnessContrast(bitmap, first, second);
			}
			else if (id == "hsl")
			{
				Adjustments.HueSaturationLightness(bitmap, first, second, third);
			}
			else if (id == "posterize")
			{
				Adjustments.Posterize(bitmap, first);
			}
			else if (id == "threshold")
			{
				Adjustments.Threshold(bitmap, first);
			}
			else if (id == "gblur")
			{
				Adjustments.GaussianBlur(bitmap, first);
			}
			else if (id == "unsharp")
			{
				Adjustments.UnsharpMask(bitmap, first, second);
			}
			else if (id == "noise")
			{
				Adjustments.AddNoise(bitmap, first, false);
			}
			else if (id == "pixelate")
			{
				Adjustments.Pixelate(bitmap, first);
			}
			document.EndStroke();
			canvas.MarkComposeDirty();
		}

		private void DoDesaturate()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
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

		private void DoSelectAll()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.Selection().SelectRect(new SkiaSharp.SKRectI(0, 0, document.Width(), document.Height()));
			canvas.Redraw();
		}

		private void DoDeselect()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			canvas.CurrentDocument().Selection().Clear();
			canvas.Redraw();
		}

		private void DoInvertSelection()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			canvas.CurrentDocument().Selection().Invert();
			canvas.Redraw();
		}

		private void DoUndo()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			if (canvas.CurrentDocument().Undo())
			{
				canvas.MarkComposeDirty();
				RefreshPanels();
			}
		}

		private void DoRedo()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			if (canvas.CurrentDocument().Redo())
			{
				canvas.MarkComposeDirty();
				RefreshPanels();
			}
		}

		private void DoExit()
		{
			Application current = Application.Current;
			if (current != null)
			{
				current.Quit();
			}
		}

		private void DoZoomIn()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.ZoomIn();
			}
		}

		private void DoZoomOut()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.ZoomOut();
			}
		}

		private void DoFit()
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

		public bool RulersEnabled()
		{
			return m_rulersEnabled;
		}

		private void ToggleRulers()
		{
			m_rulersEnabled = !m_rulersEnabled;
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index] as DocumentWindow;
				if (window != null)
				{
					window.SetRulersEnabled(m_rulersEnabled);
				}
			}
		}

		private void DoInvert()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
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
		}

		public void UpdateZoomInfo(int zoomPercent, int width, int height)
		{
			if (m_statusInfoLabel != null)
			{
				m_statusInfoLabel.Text = zoomPercent + "%      " + width + " × " + height + " px";
			}
		}

		private View BuildOptionsBar()
		{
			Grid bar = new Grid();
			bar.HeightRequest = UiConstants.OptionsBarHeight;
			bar.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			bar.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			bar.ColumnSpacing = 16.0;
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			m_optionsToolLabel = new Label();
			m_optionsToolLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_optionsToolLabel.FontSize = 12.0;
			m_optionsToolLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_optionsToolLabel, 0);
			bar.Add(m_optionsToolLabel);

			Label sizeLabel = new Label();
			sizeLabel.Text = "Size";
			sizeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			sizeLabel.FontSize = 12.0;
			sizeLabel.VerticalOptions = LayoutOptions.Center;

			m_brushSizeField = new SliderField(1, 100, m_toolState.BrushSize(), " px", OnBrushSizeValue);
			m_brushSizeField.VerticalOptions = LayoutOptions.Center;

			m_brushHardnessLabel = new Label();
			m_brushHardnessLabel.Text = "Hardness";
			m_brushHardnessLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushHardnessLabel.FontSize = 12.0;
			m_brushHardnessLabel.VerticalOptions = LayoutOptions.Center;
			m_brushHardnessLabel.IsVisible = false;

			m_brushHardnessField = new SliderField(0, 100, m_toolState.BrushHardness(), "%", OnBrushHardnessValue);
			m_brushHardnessField.VerticalOptions = LayoutOptions.Center;
			m_brushHardnessField.IsVisible = false;

			m_brushOpacityLabel = new Label();
			m_brushOpacityLabel.Text = "Opacity";
			m_brushOpacityLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushOpacityLabel.FontSize = 12.0;
			m_brushOpacityLabel.VerticalOptions = LayoutOptions.Center;
			m_brushOpacityLabel.IsVisible = false;

			m_brushOpacityField = new SliderField(1, 100, m_toolState.BrushOpacity(), "%", OnBrushOpacityValue);
			m_brushOpacityField.VerticalOptions = LayoutOptions.Center;
			m_brushOpacityField.IsVisible = false;

			m_brushFlowLabel = new Label();
			m_brushFlowLabel.Text = "Flow";
			m_brushFlowLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushFlowLabel.FontSize = 12.0;
			m_brushFlowLabel.VerticalOptions = LayoutOptions.Center;
			m_brushFlowLabel.IsVisible = false;

			m_brushFlowField = new SliderField(1, 100, m_toolState.BrushFlow(), "%", OnBrushFlowValue);
			m_brushFlowField.VerticalOptions = LayoutOptions.Center;
			m_brushFlowField.IsVisible = false;

			m_brushSmoothingLabel = new Label();
			m_brushSmoothingLabel.Text = "Smoothing";
			m_brushSmoothingLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushSmoothingLabel.FontSize = 12.0;
			m_brushSmoothingLabel.VerticalOptions = LayoutOptions.Center;
			m_brushSmoothingLabel.IsVisible = false;

			m_brushSmoothingField = new SliderField(0, 100, m_toolState.BrushSmoothing(), "%", OnBrushSmoothingValue);
			m_brushSmoothingField.VerticalOptions = LayoutOptions.Center;
			m_brushSmoothingField.IsVisible = false;

			m_brushModeLabel = new Label();
			m_brushModeLabel.Text = "Mode";
			m_brushModeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushModeLabel.FontSize = 12.0;
			m_brushModeLabel.VerticalOptions = LayoutOptions.Center;
			m_brushModeLabel.IsVisible = false;

			m_brushModePicker = new Picker();
			m_brushModePicker.FontSize = 12.0;
			m_brushModePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushModePicker.WidthRequest = 110.0;
			m_brushModePicker.VerticalOptions = LayoutOptions.Center;
			m_brushModePicker.IsVisible = false;
			m_brushModePicker.Items.Add("Normal");
			m_brushModePicker.Items.Add("Multiply");
			m_brushModePicker.Items.Add("Screen");
			m_brushModePicker.Items.Add("Overlay");
			m_brushModePicker.Items.Add("Add");
			m_brushModePicker.SelectedIndex = 0;
			m_brushModePicker.SelectedIndexChanged += OnBrushModeChanged;

			m_brushSettingsButton = new Button();
			m_brushSettingsButton.Text = "Brush Settings";
			m_brushSettingsButton.FontSize = 12.0;
			m_brushSettingsButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_brushSettingsButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_brushSettingsButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushSettingsButton.VerticalOptions = LayoutOptions.Center;
			m_brushSettingsButton.IsVisible = false;
			m_brushSettingsButton.Clicked += OnBrushSettingsClicked;

			m_lineAntiAliasLabel = new Label();
			m_lineAntiAliasLabel.Text = "Anti-alias";
			m_lineAntiAliasLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_lineAntiAliasLabel.FontSize = 12.0;
			m_lineAntiAliasLabel.VerticalOptions = LayoutOptions.Center;
			m_lineAntiAliasLabel.IsVisible = false;

			m_lineAntiAliasCheck = new CheckBox();
			m_lineAntiAliasCheck.VerticalOptions = LayoutOptions.Center;
			m_lineAntiAliasCheck.IsVisible = false;
			m_lineAntiAliasCheck.CheckedChanged += OnLineAntiAliasChanged;

			HorizontalStackLayout options = new HorizontalStackLayout();
			m_optionsRow = options;
			options.Spacing = 8.0;
			options.VerticalOptions = LayoutOptions.Center;
			options.Add(sizeLabel);
			options.Add(m_brushSizeField);
			options.Add(m_brushHardnessLabel);
			options.Add(m_brushHardnessField);
			options.Add(m_brushOpacityLabel);
			options.Add(m_brushOpacityField);
			options.Add(m_brushFlowLabel);
			options.Add(m_brushFlowField);
			options.Add(m_brushSmoothingLabel);
			options.Add(m_brushSmoothingField);
			options.Add(m_brushModeLabel);
			options.Add(m_brushModePicker);
			options.Add(m_brushSettingsButton);
			options.Add(m_lineAntiAliasLabel);
			options.Add(m_lineAntiAliasCheck);
			Grid.SetColumn(options, 1);
			bar.Add(options);

			m_lineAntiAliasCheck.IsChecked = m_toolState.LineAntiAlias();

			return bar;
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
			m_statusInfoLabel.Text = "100%      800 × 600 px";
			m_statusInfoLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_statusInfoLabel.FontSize = 11.0;
			m_statusInfoLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusInfoLabel, 0);
			bar.Add(m_statusInfoLabel);

			m_statusCursorLabel = new Label();
			m_statusCursorLabel.Text = "x: —   y: —";
			m_statusCursorLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_statusCursorLabel.FontSize = 11.0;
			m_statusCursorLabel.HorizontalOptions = LayoutOptions.End;
			m_statusCursorLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusCursorLabel, 1);
			bar.Add(m_statusCursorLabel);

			return bar;
		}

		private View BuildPaletteDock()
		{
			m_layersPanel = new LayersPanel();
			PaletteGroup layersGroup = new PaletteGroup(new string[] { "Layers", "Channels" }, m_layersPanel);

			Grid dock = new Grid();
			dock.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			dock.Padding = new Thickness(4.0);
			dock.RowSpacing = 4.0;
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Star));

			Grid.SetRow(layersGroup, 0);
			dock.Add(layersGroup);

			return dock;
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
			Microsoft.Maui.Controls.Application application = Microsoft.Maui.Controls.Application.Current;
			if (application != null)
			{
				application.RequestedThemeChanged += OnSystemThemeChanged;
			}
			this.ThemeBg(UiConstants.WorkspaceBackdropLight, UiConstants.WorkspaceBackdropDark);

			m_documents = new List<FloatingPanel>();
			m_openItemButtons = new List<Border>();
			m_openItemActions = new List<string>();
			m_openMenuIndex = -1;
			m_untitledCount = 0;
			m_cascadeCount = 0;
			m_topZIndex = 0;
			m_toolState = new ToolState();
			m_moveTool = new MoveTool();
			m_rectangleSelectTool = new RectangleSelectTool();
			m_ellipseSelectTool = new EllipseSelectTool();
			m_lassoTool = new LassoTool();
			m_magicWandTool = new MagicWandTool();
			m_textTool = new TextTool();
			m_pencilTool = new PencilTool();
			m_brushTool = new BrushTool();
			m_eraserTool = new EraserTool();
			m_dodgeBurnTool = new DodgeBurnTool();
			m_blurTool = new BlurTool();
			m_sharpenTool = new SharpenTool();
			m_cloneTool = new CloneTool();
			m_smudgeTool = new SmudgeTool();
			m_eyedropperTool = new EyedropperTool();
			m_fillTool = new FillTool();
			m_lineTool = new LineTool();
			m_handTool = new HandTool();
			m_zoomTool = new ZoomTool();

			View menuBar = BuildMenuBar();
			View optionsBar = BuildOptionsBar();
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

			m_overlay = new AbsoluteLayout();
			m_overlay.InputTransparent = true;
			m_overlay.CascadeInputTransparent = false;

			Grid outer = new Grid();
			outer.Add(root);
			outer.Add(m_overlay);

			Content = outer;
		}

		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();
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
			AddAccelerator(element, Windows.System.VirtualKey.N, OnAcceleratorNew);
			AddAccelerator(element, Windows.System.VirtualKey.O, OnAcceleratorOpen);
			AddAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorSave);
			AddAccelerator(element, Windows.System.VirtualKey.Z, OnAcceleratorUndo);
			AddAccelerator(element, Windows.System.VirtualKey.Y, OnAcceleratorRedo);
			AddAccelerator(element, Windows.System.VirtualKey.A, OnAcceleratorSelectAll);
			AddAccelerator(element, Windows.System.VirtualKey.D, OnAcceleratorDeselect);
			AddAccelerator(element, Windows.System.VirtualKey.Number0, OnAcceleratorFit);
			AddAccelerator(element, Windows.System.VirtualKey.Add, OnAcceleratorZoomIn);
			AddAccelerator(element, Windows.System.VirtualKey.Subtract, OnAcceleratorZoomOut);
			AddAccelerator(element, (Windows.System.VirtualKey)187, OnAcceleratorZoomIn);
			AddAccelerator(element, (Windows.System.VirtualKey)189, OnAcceleratorZoomOut);
			AddBareAccelerator(element, Windows.System.VirtualKey.X, OnAcceleratorSwapColors);
			element.KeyboardAcceleratorPlacementMode = Microsoft.UI.Xaml.Input.KeyboardAcceleratorPlacementMode.Hidden;
			element.AllowDrop = true;
			element.DragOver += OnElementDragOver;
			element.Drop += OnElementDrop;
			m_acceleratorsHooked = true;
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

		private void AddAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void AddBareAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.None;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void OnAcceleratorSwapColors(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.SwapColors();
			}
			args.Handled = true;
		}

		private void OnAcceleratorNew(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			ShowNewDocumentDialog();
			args.Handled = true;
		}

		private void OnAcceleratorOpen(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			OpenImageFlow();
			args.Handled = true;
		}

		private void OnAcceleratorSave(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			SaveImageFlow();
			args.Handled = true;
		}

		private void OnAcceleratorUndo(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoUndo();
			args.Handled = true;
		}

		private void OnAcceleratorRedo(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoRedo();
			args.Handled = true;
		}

		private void OnAcceleratorSelectAll(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoSelectAll();
			args.Handled = true;
		}

		private void OnAcceleratorDeselect(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoDeselect();
			args.Handled = true;
		}

		private void OnAcceleratorFit(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoFit();
			args.Handled = true;
		}

		private void OnAcceleratorZoomIn(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoZoomIn();
			args.Handled = true;
		}

		private void OnAcceleratorZoomOut(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoZoomOut();
			args.Handled = true;
		}

		private double WindowChromeWidth()
		{
			double rulerWidth = 0.0;
			if (m_rulersEnabled)
			{
				rulerWidth = UiConstants.RulerThickness;
			}
			return rulerWidth + UiConstants.ResizeGripSize + (2.0 * UiConstants.PanelBorderThickness);
		}

		private double WindowChromeHeight()
		{
			double rulerHeight = 0.0;
			if (m_rulersEnabled)
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

		private System.Collections.Generic.List<DocumentWindow> DocumentWindows()
		{
			System.Collections.Generic.List<DocumentWindow> windows = new System.Collections.Generic.List<DocumentWindow>();
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index] as DocumentWindow;
				if (window != null)
				{
					windows.Add(window);
				}
			}
			return windows;
		}

		private void DoCascadeWindows()
		{
			System.Collections.Generic.List<DocumentWindow> windows = DocumentWindows();
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

		private void DoTileWindows()
		{
			System.Collections.Generic.List<DocumentWindow> windows = DocumentWindows();
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

		private async void OpenImageFlow()
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
				SkiaSharp.SKBitmap bitmap = ImageFile.DecodeFile(path);
				if (bitmap == null)
				{
					SetStatusMessage("Failed to open image");
					return;
				}
				string title = System.IO.Path.GetFileName(path);
				Document model = Document.OpenImage(title, bitmap);
				bitmap.Dispose();
				DocumentWindow window = new DocumentWindow(model);
				PlaceAndAdd(window);
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Open failed: " + error.Message);
			}
		}

		private async void SaveImageFlow()
		{
			Document model = ActiveDocument();
			if (model == null)
			{
				return;
			}
			await SaveDocumentAsync(model);
		}

		private async System.Threading.Tasks.Task<bool> SaveDocumentAsync(Document model)
		{
			try
			{
				string path = await FileDialogs.PickSaveAsync(model.Title());
				if (path == null)
				{
					return false;
				}
				ImageFile.Encode(model, path);
				model.MarkClean();
				SetStatusMessage("Saved " + System.IO.Path.GetFileName(path));
				return true;
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Save failed: " + error.Message);
				return false;
			}
		}

		private void SetStatusMessage(string message)
		{
			if (m_statusCursorLabel != null)
			{
				m_statusCursorLabel.Text = message;
			}
		}

		public void AddDocument(FloatingPanel panel, double x, double y, double width, double height)
		{
			m_documents.Add(panel);
			m_workspace.Add(panel);
			panel.SetBounds(x, y, width, height);
			BringToFront(panel);
		}

		public void BringToFront(FloatingPanel panel)
		{
			m_topZIndex++;
			panel.ZIndex = m_topZIndex;
			DocumentWindow window = panel as DocumentWindow;
			if (window != null)
			{
				m_activeDocumentWindow = window;
				RefreshPanels();
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
			if (m_activeDocumentWindow == window)
			{
				return;
			}
			BringToFront(window);
		}

		public Document ActiveDocument()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return null;
			}
			return canvas.CurrentDocument();
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

		private void ShowModal(View content, double width, double height)
		{
			CloseModal();
			m_modalBackdrop = new BoxView();
			m_modalBackdrop.Color = Colors.Transparent;
			AbsoluteLayout.SetLayoutBounds(m_modalBackdrop, new Rect(0.0, 0.0, 1.0, 1.0));
			AbsoluteLayout.SetLayoutFlags(m_modalBackdrop, AbsoluteLayoutFlags.All);
			TapGestureRecognizer backdropTap = new TapGestureRecognizer();
			backdropTap.Tapped += OnModalBackdropTapped;
			m_modalBackdrop.GestureRecognizers.Add(backdropTap);
			m_topZIndex = m_topZIndex + 1;
			m_modalBackdrop.ZIndex = m_topZIndex + 1000;
			m_workspace.Add(m_modalBackdrop);

			m_modalContent = content;
			m_modalWidth = width;
			m_modalHeight = height;
			m_modalX = (m_workspace.Width - width) / 2.0;
			m_modalY = (m_workspace.Height - height) / 2.0;
			if (m_modalX < 0.0)
			{
				m_modalX = 0.0;
			}
			if (m_modalY < 0.0)
			{
				m_modalY = 0.0;
			}
			AbsoluteLayout.SetLayoutBounds(content, new Rect(m_modalX, m_modalY, width, AbsoluteLayout.AutoSize));
			AbsoluteLayout.SetLayoutFlags(content, AbsoluteLayoutFlags.None);
			content.ZIndex = m_topZIndex + 1001;
			m_workspace.Add(content);
		}

		public void DragModal(Microsoft.Maui.GestureStatus status, double totalX, double totalY)
		{
			if (m_modalContent == null)
			{
				return;
			}
			if (status == Microsoft.Maui.GestureStatus.Started)
			{
				m_modalDragOriginX = m_modalX;
				m_modalDragOriginY = m_modalY;
				return;
			}
			if (status != Microsoft.Maui.GestureStatus.Running)
			{
				return;
			}
			double targetX = m_modalDragOriginX + totalX;
			double targetY = m_modalDragOriginY + totalY;
			double maxX = m_workspace.Width - m_modalWidth;
			double maxY = m_workspace.Height - m_modalHeight;
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
			m_modalX = targetX;
			m_modalY = targetY;
			AbsoluteLayout.SetLayoutBounds(m_modalContent, new Rect(m_modalX, m_modalY, m_modalWidth, AbsoluteLayout.AutoSize));
		}

		public void CloseModal()
		{
			if (m_modalBackdrop != null)
			{
				m_workspace.Remove(m_modalBackdrop);
				m_modalBackdrop = null;
			}
			if (m_modalContent != null)
			{
				m_workspace.Remove(m_modalContent);
				m_modalContent = null;
			}
		}

		public void OpenColorPicker(bool foreground)
		{
			SKColor initial = m_toolState.Background();
			if (foreground)
			{
				initial = m_toolState.Foreground();
			}
			ColorPicker picker = new ColorPicker(initial, foreground);
			ShowModal(picker, 380.0, 360.0);
		}

		public void ApplyPickedColor(SKColor color, bool foreground)
		{
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
		}

		public void ShowNewDocumentDialog()
		{
			NewDocumentDialog dialog = new NewDocumentDialog();
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

		public void RefreshLayerThumbnails()
		{
			if (m_layersPanel != null)
			{
				m_layersPanel.RefreshThumbnails();
			}
		}

		public async void ClosePanel(FloatingPanel panel)
		{
			DocumentWindow window = panel as DocumentWindow;
			if (window != null)
			{
				Document model = window.DocumentModel();
				if (model != null && model.IsDirty())
				{
					string choice = await DisplayActionSheetAsync("Save changes to " + model.Title() + "?", "Cancel", null, "Save", "Don't Save");
					if (choice == "Save")
					{
						bool saved = await SaveDocumentAsync(model);
						if (!saved)
						{
							return;
						}
					}
					else if (choice != "Don't Save")
					{
						return;
					}
				}
			}
			RemovePanel(panel);
		}

		private void RemovePanel(FloatingPanel panel)
		{
			if (!m_documents.Contains(panel))
			{
				return;
			}
			m_documents.Remove(panel);
			m_workspace.Remove(panel);
			DocumentWindow window = panel as DocumentWindow;
			if (window != null && m_activeDocumentWindow == window)
			{
				m_activeDocumentWindow = null;
			}
		}

		public void OnToolSelected(eTool tool)
		{
			ClosePulldown();
			if (m_toolState != null)
			{
				m_toolState.SetTool(tool);
			}
			if (m_optionsToolLabel != null)
			{
				m_optionsToolLabel.Text = tool.ToString();
			}
			bool isLine = tool == eTool.Line;
			if (m_lineAntiAliasLabel != null)
			{
				m_lineAntiAliasLabel.IsVisible = isLine;
			}
			if (m_lineAntiAliasCheck != null)
			{
				m_lineAntiAliasCheck.IsVisible = isLine;
			}
			bool isBrushFamily = tool == eTool.Brush || tool == eTool.Eraser || tool == eTool.Clone || tool == eTool.Blur || tool == eTool.Sharpen || tool == eTool.Smudge || tool == eTool.DodgeBurn;
			if (m_brushHardnessLabel != null)
			{
				m_brushHardnessLabel.IsVisible = isBrushFamily;
				m_brushHardnessField.IsVisible = isBrushFamily;
				m_brushOpacityLabel.IsVisible = isBrushFamily;
				m_brushOpacityField.IsVisible = isBrushFamily;
				m_brushFlowLabel.IsVisible = isBrushFamily;
				m_brushFlowField.IsVisible = isBrushFamily;
				m_brushSmoothingLabel.IsVisible = isBrushFamily;
				m_brushSmoothingField.IsVisible = isBrushFamily;
				m_brushModeLabel.IsVisible = isBrushFamily;
				m_brushModePicker.IsVisible = isBrushFamily;
				m_brushSettingsButton.IsVisible = isBrushFamily;
			}
			if (m_lassoTool != null)
			{
				m_lassoTool.Reset();
			}
		}

		private void OnBrushHardnessValue(int hardness)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushHardness(hardness);
		}

		private void OnBrushOpacityValue(int opacity)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushOpacity(opacity);
		}

		private void OnBrushFlowValue(int flow)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushFlow(flow);
		}

		private void OnBrushSmoothingValue(int smoothing)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSmoothing(smoothing);
		}

		private void OnBrushModeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_brushModePicker.SelectedIndex;
			if (index < 0)
			{
				index = 0;
			}
			m_toolState.SetBrushMode((Bitmute.Imaging.eBlendMode)index);
		}

		private void OnBrushSettingsClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			if (m_pulldownPanel != null)
			{
				ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_brushSettingsButton != null)
			{
				anchorX = m_optionsRow.X + m_brushSettingsButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			ShowPulldown(BuildBrushSettingsContent(), anchorX, anchorY, 288.0, 108.0);
		}

		private View BuildBrushSettingsContent()
		{
			Label tipLabel = new Label();
			tipLabel.Text = "Tip";
			tipLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			tipLabel.FontSize = 12.0;
			tipLabel.WidthRequest = 60.0;
			tipLabel.VerticalOptions = LayoutOptions.Center;

			m_brushTipPicker = new Picker();
			m_brushTipPicker.FontSize = 12.0;
			m_brushTipPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushTipPicker.Items.Add("Round");
			m_brushTipPicker.Items.Add("Square");
			m_brushTipPicker.SelectedIndex = 0;
			if (m_toolState.BrushSquareTip())
			{
				m_brushTipPicker.SelectedIndex = 1;
			}
			m_brushTipPicker.SelectedIndexChanged += OnBrushTipPulldownChanged;

			Grid tipRow = new Grid();
			tipRow.ColumnSpacing = 8.0;
			tipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			tipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(tipLabel, 0);
			Grid.SetColumn(m_brushTipPicker, 1);
			tipRow.Add(tipLabel);
			tipRow.Add(m_brushTipPicker);

			Label spacingLabel = new Label();
			spacingLabel.Text = "Spacing";
			spacingLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			spacingLabel.FontSize = 12.0;
			spacingLabel.WidthRequest = 60.0;
			spacingLabel.VerticalOptions = LayoutOptions.Center;

			m_brushSpacingSlider = new Slider();
			m_brushSpacingSlider.Minimum = 1.0;
			m_brushSpacingSlider.Maximum = 100.0;
			m_brushSpacingSlider.WidthRequest = 140.0;
			m_brushSpacingSlider.VerticalOptions = LayoutOptions.Center;
			m_brushSpacingSlider.Value = m_toolState.BrushSpacing();
			m_brushSpacingSlider.ValueChanged += OnBrushSpacingPulldownChanged;

			m_brushSpacingValue = new Label();
			m_brushSpacingValue.Text = m_toolState.BrushSpacing() + "%";
			m_brushSpacingValue.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushSpacingValue.FontSize = 12.0;
			m_brushSpacingValue.WidthRequest = 44.0;
			m_brushSpacingValue.VerticalOptions = LayoutOptions.Center;

			Grid spacingRow = new Grid();
			spacingRow.ColumnSpacing = 8.0;
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(spacingLabel, 0);
			Grid.SetColumn(m_brushSpacingSlider, 1);
			Grid.SetColumn(m_brushSpacingValue, 2);
			spacingRow.Add(spacingLabel);
			spacingRow.Add(m_brushSpacingSlider);
			spacingRow.Add(m_brushSpacingValue);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 10.0;
			body.Padding = new Thickness(12.0);
			body.Add(tipRow);
			body.Add(spacingRow);
			return body;
		}

		private void OnBrushTipPulldownChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_brushTipPicker == null)
			{
				return;
			}
			ApplyBrushTip(m_brushTipPicker.SelectedIndex == 1);
		}

		private void OnBrushSpacingPulldownChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_brushSpacingSlider == null)
			{
				return;
			}
			int spacing = (int)m_brushSpacingSlider.Value;
			ApplyBrushSpacing(spacing);
			if (m_brushSpacingValue != null)
			{
				m_brushSpacingValue.Text = spacing + "%";
			}
		}

		private void OnPulldownCatcherTapped(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
		}

		private void ClosePulldown()
		{
			if (m_pulldownCatcher != null)
			{
				m_overlay.Remove(m_pulldownCatcher);
				m_pulldownCatcher = null;
			}
			if (m_pulldownPanel != null)
			{
				m_overlay.Remove(m_pulldownPanel);
				m_pulldownPanel = null;
			}
		}

		public void ShowPulldown(View content, double anchorX, double anchorY, double width, double height)
		{
			ClosePulldown();
			CloseMenu();

			BoxView catcher = new BoxView();
			catcher.Color = Colors.Transparent;
			TapGestureRecognizer catcherTap = new TapGestureRecognizer();
			catcherTap.Tapped += OnPulldownCatcherTapped;
			catcher.GestureRecognizers.Add(catcherTap);
			AbsoluteLayout.SetLayoutFlags(catcher, AbsoluteLayoutFlags.WidthProportional | AbsoluteLayoutFlags.HeightProportional);
			AbsoluteLayout.SetLayoutBounds(catcher, new Rect(0.0, 0.0, 1.0, 1.0));
			m_overlay.Add(catcher);
			m_pulldownCatcher = catcher;

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
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSquareTip(square);
		}

		public void ApplyBrushSpacing(int spacing)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSpacing(spacing);
		}

		private void OnLineAntiAliasChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetLineAntiAlias(m_lineAntiAliasCheck.IsChecked);
		}

		private void OnBrushSizeValue(int size)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSize(size);
		}

		public async void PlaceText(CanvasView canvas, int x, int y)
		{
			string text = await DisplayPromptAsync("Add Text", "Enter text:");
			if (text == null)
			{
				return;
			}
			if (text.Length == 0)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			document.BeginStroke();
			TextRasterizer.Draw(layer.Bitmap(), text, x - layer.OffsetX(), y - layer.OffsetY(), m_toolState.Foreground(), 32.0f);
			document.EndStroke();
			canvas.MarkComposeDirty();
			SetStatusMessage("Added text: " + text);
		}

		public ToolState CurrentToolState()
		{
			return m_toolState;
		}

		public Tool CurrentTool()
		{
			eTool tool = m_toolState.Tool();
			if (tool == eTool.Move)
			{
				return m_moveTool;
			}
			if (tool == eTool.Select)
			{
				return m_rectangleSelectTool;
			}
			if (tool == eTool.EllipseSelect)
			{
				return m_ellipseSelectTool;
			}
			if (tool == eTool.Lasso)
			{
				return m_lassoTool;
			}
			if (tool == eTool.MagicWand)
			{
				return m_magicWandTool;
			}
			if (tool == eTool.Text)
			{
				return m_textTool;
			}
			if (tool == eTool.Pencil)
			{
				return m_pencilTool;
			}
			if (tool == eTool.Brush)
			{
				return m_brushTool;
			}
			if (tool == eTool.Eraser)
			{
				return m_eraserTool;
			}
			if (tool == eTool.Eyedropper)
			{
				return m_eyedropperTool;
			}
			if (tool == eTool.Fill)
			{
				return m_fillTool;
			}
			if (tool == eTool.Line)
			{
				return m_lineTool;
			}
			if (tool == eTool.DodgeBurn)
			{
				return m_dodgeBurnTool;
			}
			if (tool == eTool.Blur)
			{
				return m_blurTool;
			}
			if (tool == eTool.Sharpen)
			{
				return m_sharpenTool;
			}
			if (tool == eTool.Clone)
			{
				return m_cloneTool;
			}
			if (tool == eTool.Smudge)
			{
				return m_smudgeTool;
			}
			if (tool == eTool.Hand)
			{
				return m_handTool;
			}
			if (tool == eTool.Zoom)
			{
				return m_zoomTool;
			}
			return null;
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
