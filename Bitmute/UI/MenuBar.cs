using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Bitmute.Storage;
using Bitmute.UI.Dialogs;
using Bitmute.UI.Operations;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Bitmute.UI
{
	public class MenuBar
	{
		public const double MenuItemHeight = 20.0;
		public const double DropdownWidth = 190.0;
		public const double MenuSeparatorHeight = 9.0;

		private MainView m_main;
		private AbsoluteLayout m_overlay;
		private string[] m_titles;
		private Border[] m_menuButtons;
		private List<Border> m_openItemButtons;
		private List<MenuBarItem> m_openItems;
		private int m_openMenuIndex;
		private double m_openDropdownX;
		private List<Border> m_submenuParentRows;
		private List<MenuBarItem> m_submenuParentItems;
		private List<Border> m_submenuChildRows;
		private Border m_submenuBorder;
		private View m_root;

		public static Border BuildMenuSeparator()
		{
			Border line = new Border();
			line.HeightRequest = 1.0;
			line.StrokeThickness = 0.0;
			line.HorizontalOptions = LayoutOptions.Fill;
			line.VerticalOptions = LayoutOptions.Center;
			line.ThemeBg(UiConstants.DividerLight, UiConstants.DividerDark);

			Border separatorRow = new Border();
			separatorRow.HeightRequest = MenuSeparatorHeight;
			separatorRow.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
			separatorRow.StrokeThickness = 0.0;
			separatorRow.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			separatorRow.Content = line;
			return separatorRow;
		}

		private static double MenuListHeight(List<MenuBarItem> items)
		{
			double total = 8.0;
			for (int index = 0; index < items.Count; index++)
			{
				if (items[index].m_separator)
				{
					total = total + MenuSeparatorHeight;
				}
				else
				{
					total = total + MenuItemHeight;
				}
			}
			return total;
		}

		private Border BuildMenuButton(int index)
		{
			Label label = new Label();
			label.Text = m_titles[index];
			label.FontSize = UiConstants.PanelFontSize;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;

			Border button = new Border();
			button.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			button.ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
			button.StrokeThickness = 0.0;
			button.Content = label;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnMenuButtonTapped;
			button.GestureRecognizers.Add(tap);

			PointerGestureRecognizer pointer = new PointerGestureRecognizer();
			pointer.PointerEntered += OnMenuButtonPointerEntered;
			pointer.PointerExited += OnMenuButtonPointerExited;
			button.GestureRecognizers.Add(pointer);

			return button;
		}

		public List<MenuBarItem> GetMenuItems(string title)
		{
			List<MenuBarItem> items = new List<MenuBarItem>();
			if (title == "File")
			{
				items.Add(new MenuBarItem("New", eMenuAction.NewDocument, "Ctrl+N", () => m_main.ShowNewDocumentDialog()));
				items.Add(new MenuBarItem("Open…", eMenuAction.OpenFile, "Ctrl+O", () => m_main.OpenImageFlow()));
				items.Add(new MenuBarItem("Save", eMenuAction.Save, "Ctrl+S", () => m_main.SaveImageFlow()));
				items.Add(new MenuBarItem("Save As…", eMenuAction.SaveAs, "Ctrl+Shift+S", () => m_main.SaveAsFlow()));
				items.Add(new MenuBarItem("Export As…", eMenuAction.ExportAs, "Ctrl+Alt+Shift+S", () => m_main.OpenExportDialog()));
				if (RecentFiles.List().Count > 0)
				{
					MenuBarItem openRecent = new MenuBarItem("Open Recent", eMenuAction.OpenRecentMenu);
					openRecent.m_submenu = true;
					items.Add(openRecent);
				}
				items.Add(new MenuBarItem("Exit", eMenuAction.Exit, () => m_main.DoExit()));
				return items;
			}
			if (title == "Edit")
			{
				items.Add(new MenuBarItem("Undo", eMenuAction.Undo, "Ctrl+Z", () => m_main.DoUndo()));
				items.Add(new MenuBarItem("Step Backward", eMenuAction.Undo, m_main.Operations().GetAcceleratorText(eOperation.UndoStep), () => m_main.DoUndo()));
				items.Add(new MenuBarItem("Step Forward", eMenuAction.Redo, m_main.Operations().GetAcceleratorText(eOperation.RedoStep), () => m_main.DoRedo()));
				items.Add(new MenuBarItem("Cut", eMenuAction.Cut, "Ctrl+X", () => m_main.DoCut()));
				items.Add(new MenuBarItem("Copy", eMenuAction.Copy, "Ctrl+C", () => m_main.DoCopy()));
				items.Add(new MenuBarItem("Paste", eMenuAction.Paste, "Ctrl+V", () => m_main.DoPaste()));
				MenuBarItem transform = new MenuBarItem("Transform", eMenuAction.TransformMenu);
				transform.m_submenu = true;
				items.Add(transform);
				items.Add(new MenuBarItem("Stroke…", eMenuAction.StrokeDialog, () => m_main.OpenStrokeDialog()));
				items.Add(new MenuBarItem("Define Pattern", eMenuAction.DefinePattern, () => m_main.DoDefinePattern()));
				items.Add(new MenuBarItem("Define Brush", eMenuAction.DefineBrush, () => m_main.DoDefineBrush()));
				items.Add(new MenuBarItem("Save Brush Preset", eMenuAction.SaveBrushPreset, () => m_main.DoSaveBrushPreset()));
				items.Add(new MenuBarItem("Preferences…", eMenuAction.Preferences, () => m_main.ShowModal(new PreferencesDialog(), 340.0, 520.0)));
				return items;
			}
			if (title == "Image")
			{
				MenuBarItem adjustments = new MenuBarItem("Adjustments", eMenuAction.AdjustmentsMenu);
				adjustments.m_submenu = true;
				items.Add(adjustments);
				MenuBarItem imageSeparator = new MenuBarItem("", eMenuAction.None);
				imageSeparator.m_separator = true;
				items.Add(imageSeparator);
				items.Add(new MenuBarItem("Image Size…", eMenuAction.ImageSize, "Ctrl+Alt+I", () => m_main.OpenSizeDialog(false)));
				items.Add(new MenuBarItem("Canvas Size…", eMenuAction.CanvasSize, () => m_main.OpenSizeDialog(true)));
				MenuBarItem mode = new MenuBarItem("Mode", eMenuAction.ModeMenu);
				mode.m_submenu = true;
				items.Add(mode);
				items.Add(new MenuBarItem("Flip Horizontal", eMenuAction.FlipHorizontal, () => m_main.DoCanvasOp(eCanvasOperation.FlipHorizontal)));
				items.Add(new MenuBarItem("Flip Vertical", eMenuAction.FlipVertical, () => m_main.DoCanvasOp(eCanvasOperation.FlipVertical)));
				items.Add(new MenuBarItem("Rotate 90° CW", eMenuAction.Rotate90Clockwise, () => m_main.DoCanvasOp(eCanvasOperation.Rotate90Clockwise)));
				items.Add(new MenuBarItem("Rotate 180°", eMenuAction.Rotate180, () => m_main.DoCanvasOp(eCanvasOperation.Rotate180)));
				items.Add(new MenuBarItem("Rotate 90° CCW", eMenuAction.Rotate90CounterClockwise, () => m_main.DoCanvasOp(eCanvasOperation.Rotate90CounterClockwise)));
				items.Add(new MenuBarItem("Rotate Arbitrary…", eMenuAction.RotateArbitrary, () => m_main.OpenAdjustment(eMenuAction.RotateArbitrary)));
				items.Add(new MenuBarItem("Crop to Selection", eMenuAction.CropToSelection, () => m_main.DoCanvasOp(eCanvasOperation.CropToSelection)));
				items.Add(new MenuBarItem("Trim", eMenuAction.Trim, () => m_main.DoCanvasOp(eCanvasOperation.Trim)));
				return items;
			}
			if (title == "Layer")
			{
				Document layerDocument = m_main.ActiveDocument();
				bool hasDocument = layerDocument != null;
				bool hasActiveLayer = false;
				if (hasDocument)
				{
					hasActiveLayer = layerDocument.ActiveLayer() != null;
				}
				MenuBarItem newLayer = new MenuBarItem("New Layer", eMenuAction.NewLayer, () => m_main.AddNewLayer());
				newLayer.m_enabled = hasDocument;
				items.Add(newLayer);
				MenuBarItem deleteLayer = new MenuBarItem("Delete Layer", eMenuAction.DeleteLayer, () => m_main.RequestDeleteActiveLayer());
				deleteLayer.m_enabled = hasDocument;
				items.Add(deleteLayer);
				MenuBarItem layerSeparatorOne = new MenuBarItem("", eMenuAction.None);
				layerSeparatorOne.m_separator = true;
				items.Add(layerSeparatorOne);
				MenuBarItem mergeDown = new MenuBarItem("Merge Down", eMenuAction.MergeDown, "Ctrl+E", () => m_main.DoMergeDown());
				mergeDown.m_enabled = m_main.CanMergeDown();
				items.Add(mergeDown);
				items.Add(new MenuBarItem("Merge Visible", eMenuAction.MergeVisible, m_main.Operations().GetAcceleratorText(eOperation.MergeVisibleLayers), () => m_main.DoMergeVisible()));
				items.Add(new MenuBarItem("Flatten Image", eMenuAction.FlattenImage, () => m_main.DoFlattenImage()));
				MenuBarItem layerSeparatorTwo = new MenuBarItem("", eMenuAction.None);
				layerSeparatorTwo.m_separator = true;
				items.Add(layerSeparatorTwo);
				MenuBarItem layerStyle = new MenuBarItem("Layer Style…", eMenuAction.LayerStyle, () => m_main.OpenLayerStyleDialog());
				layerStyle.m_enabled = hasActiveLayer;
				items.Add(layerStyle);
				MenuBarItem layerProperties = new MenuBarItem("Layer Properties…", eMenuAction.LayerProperties, () => m_main.OpenLayerPropertiesDialog());
				layerProperties.m_enabled = hasActiveLayer;
				items.Add(layerProperties);
				MenuBarItem rasterizeText = new MenuBarItem("Rasterize Text", eMenuAction.RasterizeText, () => m_main.DoRasterizeText());
				rasterizeText.m_enabled = m_main.ActiveLayerIsText();
				items.Add(rasterizeText);
				return items;
			}
			if (title == "Select")
			{
				items.Add(new MenuBarItem("All", eMenuAction.SelectAll, "Ctrl+A", () => m_main.DoSelectAll()));
				items.Add(new MenuBarItem("Deselect", eMenuAction.Deselect, "Ctrl+D", () => m_main.DoDeselect()));
				items.Add(new MenuBarItem("Invert", eMenuAction.InvertSelection, "Ctrl+Shift+I", () => m_main.DoInvertSelection()));
				MenuBarItem feather = new MenuBarItem("Feather…", eMenuAction.FeatherSelection, () => m_main.OpenAdjustment(eMenuAction.FeatherSelection));
				Document selectDocument = m_main.ActiveDocument();
				feather.m_enabled = selectDocument != null && selectDocument.Selection().IsActive();
				items.Add(feather);
				return items;
			}
			if (title == "Filter")
			{
				MenuBarItem lastFilter = new MenuBarItem(m_main.LastFilterLabel(), eMenuAction.LastFilter, "Ctrl+F", () => m_main.ApplyLastFilter());
				lastFilter.m_enabled = m_main.HasLastFilter();
				items.Add(lastFilter);
				MenuBarItem filterSeparator = new MenuBarItem("", eMenuAction.None);
				filterSeparator.m_separator = true;
				items.Add(filterSeparator);
				MenuBarItem blur = new MenuBarItem("Blur", eMenuAction.FilterBlurMenu);
				blur.m_submenu = true;
				items.Add(blur);
				MenuBarItem distort = new MenuBarItem("Distort", eMenuAction.FilterDistortMenu);
				distort.m_submenu = true;
				items.Add(distort);
				MenuBarItem noise = new MenuBarItem("Noise", eMenuAction.FilterNoiseMenu);
				noise.m_submenu = true;
				items.Add(noise);
				MenuBarItem pixelate = new MenuBarItem("Pixelate", eMenuAction.FilterPixelateMenu);
				pixelate.m_submenu = true;
				items.Add(pixelate);
				MenuBarItem render = new MenuBarItem("Render", eMenuAction.FilterRenderMenu);
				render.m_submenu = true;
				items.Add(render);
				MenuBarItem sharpen = new MenuBarItem("Sharpen", eMenuAction.FilterSharpenMenu);
				sharpen.m_submenu = true;
				items.Add(sharpen);
				MenuBarItem stylize = new MenuBarItem("Stylize", eMenuAction.FilterStylizeMenu);
				stylize.m_submenu = true;
				items.Add(stylize);
				MenuBarItem video = new MenuBarItem("Video", eMenuAction.FilterVideoMenu);
				video.m_submenu = true;
				items.Add(video);
				MenuBarItem generate = new MenuBarItem("Generate", eMenuAction.FilterGenerateMenu);
				generate.m_submenu = true;
				items.Add(generate);
				MenuBarItem other = new MenuBarItem("Other", eMenuAction.FilterOtherMenu);
				other.m_submenu = true;
				items.Add(other);
				return items;
			}
			if (title == "View")
			{
				items.Add(new MenuBarItem("Zoom In", eMenuAction.ZoomIn, "Ctrl++", () => m_main.DoZoomIn()));
				items.Add(new MenuBarItem("Zoom Out", eMenuAction.ZoomOut, "Ctrl+-", () => m_main.DoZoomOut()));
				items.Add(new MenuBarItem("Fit on Screen", eMenuAction.FitOnScreen, "Ctrl+0", () => m_main.DoFit()));
				MenuBarItem rulers = new MenuBarItem("Rulers", eMenuAction.ToggleRulers, m_main.Operations().GetAcceleratorText(eOperation.ToggleRulers), () => m_main.ToggleRulers());
				rulers.m_checked = m_main.RulersEnabled();
				items.Add(rulers);
				MenuBarItem grid = new MenuBarItem("Grid", eMenuAction.ToggleGrid, () => m_main.ToggleGrid());
				grid.m_checked = m_main.GridEnabled();
				items.Add(grid);
				MenuBarItem patternPreview = new MenuBarItem("Pattern Preview", eMenuAction.TogglePatternPreview, () => m_main.TogglePatternPreview());
				patternPreview.m_checked = m_main.PatternPreviewEnabled();
				items.Add(patternPreview);
				MenuBarItem snap = new MenuBarItem("Snap", eMenuAction.ToggleSnap, () => m_main.Workspace().SetSnapEnabled(!m_main.Workspace().SnapEnabled()));
				snap.m_checked = m_main.SnapEnabled();
				items.Add(snap);
				MenuBarItem snapTo = new MenuBarItem("Snap To", eMenuAction.SnapToMenu);
				snapTo.m_submenu = true;
				items.Add(snapTo);
				MenuBarItem lockGuides = new MenuBarItem("Lock Guides", eMenuAction.ToggleLockGuides, () => m_main.ToggleGuideLock());
				lockGuides.m_checked = m_main.GuidesLocked();
				items.Add(lockGuides);
				items.Add(new MenuBarItem("Clear Guides", eMenuAction.ClearGuides, () => m_main.ClearGuides()));
				return items;
			}
			if (title == "Window")
			{
				items.Add(new MenuBarItem("Cascade", eMenuAction.CascadeWindows, () => m_main.DoCascadeWindows()));
				items.Add(new MenuBarItem("Tile", eMenuAction.TileWindows, () => m_main.DoTileWindows()));
				MenuBarItem navigator = new MenuBarItem("Navigator", eMenuAction.ToggleNavigatorPanel, () => m_main.ToggleDockPanel(ePanelId.Navigator));
				navigator.m_checked = m_main.NavigatorPanelVisible();
				items.Add(navigator);
				MenuBarItem swatches = new MenuBarItem("Swatches", eMenuAction.ToggleSwatchesPanel, () => m_main.ToggleDockPanel(ePanelId.Swatches));
				swatches.m_checked = m_main.SwatchesPanelVisible();
				items.Add(swatches);
				MenuBarItem layersPanel = new MenuBarItem("Layers", eMenuAction.ToggleLayersPanel, () => m_main.ToggleDockPanel(ePanelId.Layers));
				layersPanel.m_checked = m_main.LayersPanelVisible();
				items.Add(layersPanel);
				MenuBarItem patternsPanel = new MenuBarItem("Patterns", eMenuAction.TogglePatternsPanel, () => m_main.ToggleDockPanel(ePanelId.Patterns));
				patternsPanel.m_checked = m_main.PatternsPanelVisible();
				items.Add(patternsPanel);
				return items;
			}
			items.Add(new MenuBarItem("Report Bug…", eMenuAction.ReportBug, () => m_main.ShowReportBugDialog()));
			items.Add(new MenuBarItem("About Bitmute", eMenuAction.AboutBitmute, () => m_main.ShowModal(new AboutDialog(), 380.0, 300.0)));
			return items;
		}


		public List<MenuBarItem> GetSubmenuItems(eMenuAction parent)
		{
			List<MenuBarItem> items = new List<MenuBarItem>();
			if (parent == eMenuAction.OpenRecentMenu)
			{
				List<string> recent = RecentFiles.List();
				int recentCount = recent.Count;
				if (recentCount > 12)
				{
					recentCount = 12;
				}
				for (int index = 0; index < recentCount; index++)
				{
					string path = recent[index];
					items.Add(new MenuBarItem(System.IO.Path.GetFileName(path), eMenuAction.OpenRecent, () => m_main.OpenRecentFile(path)));
				}
				return items;
			}
			if (parent == eMenuAction.TransformMenu)
			{
				items.Add(new MenuBarItem("Free Transform", eMenuAction.FreeTransform, "Ctrl+T", () => m_main.BeginTransform(0)));
				items.Add(new MenuBarItem("Scale", eMenuAction.TransformScale, () => m_main.BeginTransform(1)));
				items.Add(new MenuBarItem("Rotate", eMenuAction.TransformRotate, () => m_main.BeginTransform(2)));
				items.Add(new MenuBarItem("Skew", eMenuAction.TransformSkew, () => m_main.BeginTransform(3)));
				items.Add(new MenuBarItem("Distort", eMenuAction.TransformDistort, () => m_main.BeginTransform(4)));
				items.Add(new MenuBarItem("Perspective", eMenuAction.TransformPerspective, () => m_main.BeginTransform(5)));
				items.Add(new MenuBarItem("Flip Horizontal (Layer)", eMenuAction.FlipLayerHorizontal, () => m_main.BeginTransform(6)));
				items.Add(new MenuBarItem("Flip Vertical (Layer)", eMenuAction.FlipLayerVertical, () => m_main.BeginTransform(7)));
				return items;
			}
			if (parent == eMenuAction.SnapToMenu)
			{
				MenuBarItem snapGuides = new MenuBarItem("Snap Guides", eMenuAction.ToggleSnapGuides, () => m_main.Workspace().SetSnapTargetGuides(!m_main.Workspace().SnapTargetGuides()));
				snapGuides.m_checked = m_main.SnapTargetGuides();
				items.Add(snapGuides);
				MenuBarItem snapGrid = new MenuBarItem("Snap Grid", eMenuAction.ToggleSnapGrid, () => m_main.Workspace().SetSnapTargetGrid(!m_main.Workspace().SnapTargetGrid()));
				snapGrid.m_checked = m_main.SnapTargetGrid();
				items.Add(snapGrid);
				MenuBarItem snapEdges = new MenuBarItem("Snap Edges", eMenuAction.ToggleSnapEdges, () => m_main.Workspace().SetSnapTargetEdges(!m_main.Workspace().SnapTargetEdges()));
				snapEdges.m_checked = m_main.SnapTargetEdges();
				items.Add(snapEdges);
				MenuBarItem snapLayers = new MenuBarItem("Snap Layers", eMenuAction.ToggleSnapLayers, () => m_main.Workspace().SetSnapTargetLayerBounds(!m_main.Workspace().SnapTargetLayerBounds()));
				snapLayers.m_checked = m_main.SnapTargetLayerBounds();
				items.Add(snapLayers);
				return items;
			}
			if (parent == eMenuAction.AdjustmentsMenu)
			{
				items.Add(new MenuBarItem("Brightness/Contrast…", eMenuAction.BrightnessContrast, () => m_main.OpenAdjustment(eMenuAction.BrightnessContrast)));
				items.Add(new MenuBarItem("Hue/Saturation…", eMenuAction.HueSaturation, () => m_main.OpenAdjustment(eMenuAction.HueSaturation)));
				MenuBarItem adjustSeparatorOne = new MenuBarItem("", eMenuAction.None);
				adjustSeparatorOne.m_separator = true;
				items.Add(adjustSeparatorOne);
				items.Add(new MenuBarItem("Desaturate", eMenuAction.Desaturate, () => m_main.DoDesaturate()));
				items.Add(new MenuBarItem("Invert Colors", eMenuAction.InvertColors, "Ctrl+I", () => m_main.DoInvert()));
				MenuBarItem adjustSeparatorTwo = new MenuBarItem("", eMenuAction.None);
				adjustSeparatorTwo.m_separator = true;
				items.Add(adjustSeparatorTwo);
				items.Add(new MenuBarItem("Posterize…", eMenuAction.Posterize, () => m_main.OpenAdjustment(eMenuAction.Posterize)));
				items.Add(new MenuBarItem("Threshold…", eMenuAction.Threshold, () => m_main.OpenAdjustment(eMenuAction.Threshold)));
				return items;
			}
			if (parent == eMenuAction.ModeMenu)
			{
				Document modeDocument = m_main.ActiveDocument();
				bool hasModeDocument = modeDocument != null;
				eColorDepth activeDepth = eColorDepth.Eight;
				if (hasModeDocument)
				{
					activeDepth = modeDocument.ColorDepth();
				}
				MenuBarItem mode8 = new MenuBarItem("8 Bits/Channel", eMenuAction.Mode8, () => m_main.DoConvertColorDepth(eColorDepth.Eight));
				mode8.m_checked = hasModeDocument && activeDepth == eColorDepth.Eight;
				items.Add(mode8);
				MenuBarItem mode16 = new MenuBarItem("16 Bits/Channel", eMenuAction.Mode16, () => m_main.DoConvertColorDepth(eColorDepth.Sixteen));
				mode16.m_checked = hasModeDocument && activeDepth == eColorDepth.Sixteen;
				items.Add(mode16);
				MenuBarItem mode32 = new MenuBarItem("32 Bits/Channel (float)", eMenuAction.Mode32, () => m_main.DoConvertColorDepth(eColorDepth.ThirtyTwoFloat));
				mode32.m_checked = hasModeDocument && activeDepth == eColorDepth.ThirtyTwoFloat;
				items.Add(mode32);
				return items;
			}
			if (m_main.BuildsFilterSubmenu(parent))
			{
				return m_main.FilterSubmenuItems(parent);
			}
			return items;
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
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (m_openMenuIndex < 0)
			{
				m_menuButtons[index].ThemeBg(UiConstants.MenuHoverLight, UiConstants.MenuHoverDark);
				return;
			}
			if (index != m_openMenuIndex)
			{
				OpenMenu(index);
			}
		}

		private void OnMenuButtonPointerExited(object sender, PointerEventArgs eventArgs)
		{
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (index == m_openMenuIndex)
			{
				return;
			}
			m_menuButtons[index].ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
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
				CloseOpenMenu();
				return;
			}
			OpenMenu(index);
		}

		private void OpenMenu(int index)
		{
			m_main.ClosePulldown();
			CloseOpenMenu();
			m_openMenuIndex = index;
			m_menuButtons[index].ThemeBg(UiConstants.MenuOpenLight, UiConstants.MenuOpenDark);
			m_submenuParentRows.Clear();
			m_submenuParentItems.Clear();
			m_submenuBorder = null;

			string title = m_titles[index];
			List<MenuBarItem> items = GetMenuItems(title);

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
			m_openDropdownX = dropdownX;

			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);

			for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
			{
				list.Add(BuildMenuItem(items[itemIndex]));
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

			double dropdownHeight = MenuListHeight(items);
			AbsoluteLayout.SetLayoutFlags(dropdown, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(dropdown, new Rect(dropdownX, UiConstants.MenuBarHeight, DropdownWidth, dropdownHeight));
			m_overlay.Add(dropdown);
		}

		private void OpenSubmenu(MenuBarItem parentItem, Border parentRow)
		{
			CloseSubmenu();
			List<MenuBarItem> children = GetSubmenuItems(parentItem.m_action);
			if (children.Count == 0)
			{
				return;
			}
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);
			m_submenuChildRows.Clear();
			for (int index = 0; index < children.Count; index++)
			{
				Border childRow = BuildMenuItem(children[index]);
				m_submenuChildRows.Add(childRow);
				list.Add(childRow);
			}
			Border submenu = new Border();
			submenu.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			submenu.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			submenu.StrokeThickness = 1.0;
			submenu.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			submenu.Content = list;
			double submenuX = m_openDropdownX + DropdownWidth - 2.0;
			double overlayWidth = m_overlay.Width;
			if (overlayWidth > 0.0 && submenuX + DropdownWidth > overlayWidth)
			{
				submenuX = m_openDropdownX - DropdownWidth + 2.0;
			}
			if (submenuX < 0.0)
			{
				submenuX = 0.0;
			}
			double submenuY = UiConstants.MenuBarHeight + parentRow.Y;
			double submenuHeight = MenuListHeight(children);
			AbsoluteLayout.SetLayoutFlags(submenu, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(submenu, new Rect(submenuX, submenuY, DropdownWidth, submenuHeight));
			m_overlay.Add(submenu);
			m_submenuBorder = submenu;
		}

		private void CloseSubmenu()
		{
			if (m_submenuBorder != null)
			{
				m_overlay.Remove(m_submenuBorder);
				m_submenuBorder = null;
			}
		}

		private void OnSubmenuParentEntered(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row == null)
			{
				return;
			}
			row.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			for (int index = 0; index < m_submenuParentRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuParentRows[index], sender))
				{
					OpenSubmenu(m_submenuParentItems[index], m_submenuParentRows[index]);
					return;
				}
			}
		}

		private void OnSubmenuParentTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_submenuParentRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuParentRows[index], sender))
				{
					OpenSubmenu(m_submenuParentItems[index], m_submenuParentRows[index]);
					return;
				}
			}
		}

		private Border BuildMenuItem(MenuBarItem item)
		{
			if (item.m_separator)
			{
				return BuildMenuSeparator();
			}
			bool enabled = item.m_enabled;
			bool submenu = item.m_submenu;

			string accelerator = item.m_accelerator;
			if (submenu)
			{
				accelerator = "▸";
			}

			string text = item.m_label;
			if (item.m_checked)
			{
				text = "✓ " + item.m_label;
			}

			Grid rowContent = new Grid();
			rowContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			rowContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

			Label label = new Label();
			label.Text = text;
			label.FontSize = UiConstants.PanelFontSize;
			label.VerticalOptions = LayoutOptions.Center;
			label.LineBreakMode = LineBreakMode.TailTruncation;
			label.MaxLines = 1;
			if (enabled)
			{
				label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			}
			else
			{
				label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			}
			Grid.SetColumn(label, 0);
			rowContent.Add(label);

			if (accelerator.Length > 0)
			{
				Label accelLabel = new Label();
				accelLabel.Text = accelerator;
				accelLabel.FontSize = UiConstants.PanelFontSize;
				accelLabel.VerticalOptions = LayoutOptions.Center;
				if (enabled)
				{
					accelLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				}
				else
				{
					accelLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
				}
				Grid.SetColumn(accelLabel, 1);
				rowContent.Add(accelLabel);
			}

			Border row = new Border();
			row.HeightRequest = MenuItemHeight;
			row.Padding = new Thickness(12.0, 0.0, 12.0, MenuItemHeight/4);
			row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			row.StrokeThickness = 0.0;
			row.Content = rowContent;

			if (submenu)
			{
				TapGestureRecognizer submenuTap = new TapGestureRecognizer();
				submenuTap.Tapped += OnSubmenuParentTapped;
				row.GestureRecognizers.Add(submenuTap);
				PointerGestureRecognizer submenuPointer = new PointerGestureRecognizer();
				submenuPointer.PointerEntered += OnSubmenuParentEntered;
				submenuPointer.PointerExited += OnMenuItemPointerExited;
				row.GestureRecognizers.Add(submenuPointer);
				m_submenuParentRows.Add(row);
				m_submenuParentItems.Add(item);
			}
			else if (enabled)
			{
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnMenuItemTapped;
				row.GestureRecognizers.Add(tap);
				PointerGestureRecognizer pointer = new PointerGestureRecognizer();
				pointer.PointerEntered += OnMenuItemPointerEntered;
				pointer.PointerExited += OnMenuItemPointerExited;
				row.GestureRecognizers.Add(pointer);
				m_openItemButtons.Add(row);
				m_openItems.Add(item);
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
			bool isSubmenuChild = false;
			for (int index = 0; index < m_submenuChildRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuChildRows[index], sender))
				{
					isSubmenuChild = true;
					break;
				}
			}
			if (!isSubmenuChild)
			{
				CloseSubmenu();
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
					MenuBarItem item = m_openItems[index];
					CloseOpenMenu();
					if (item.m_invoke != null)
					{
						item.m_invoke();
					}
					m_main.RestoreKeyboardFocusDeferred();
					return;
				}
			}
		}

		private void OnCatcherTapped(object sender, TappedEventArgs eventArgs)
		{
			CloseOpenMenu();
		}

		public MenuBar(MainView main, string[] titles, AbsoluteLayout overlay)
		{
			m_main = main;
			m_titles = titles;
			m_overlay = overlay;
			m_menuButtons = new Border[titles.Length];
			m_openItemButtons = new List<Border>();
			m_openItems = new List<MenuBarItem>();
			m_openMenuIndex = -1;
			m_openDropdownX = 0.0;
			m_submenuParentRows = new List<Border>();
			m_submenuParentItems = new List<MenuBarItem>();
			m_submenuChildRows = new List<Border>();
			m_submenuBorder = null;

			HorizontalStackLayout strip = new HorizontalStackLayout();
			strip.HeightRequest = UiConstants.MenuBarHeight;
			strip.ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
			strip.Spacing = 0.0;
			strip.Padding = new Thickness(0.0);

			for (int index = 0; index < titles.Length; index++)
			{
				Border button = BuildMenuButton(index);
				m_menuButtons[index] = button;
				strip.Add(button);
			}

			m_root = strip;
		}

		public View Root()
		{
			return m_root;
		}

		public void CloseOpenMenu()
		{
			m_overlay.Clear();
			m_openItemButtons.Clear();
			m_openItems.Clear();
			m_submenuParentRows.Clear();
			m_submenuParentItems.Clear();
			m_submenuChildRows.Clear();
			m_submenuBorder = null;
			if (m_openMenuIndex >= 0)
			{
				m_menuButtons[m_openMenuIndex].ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
			}
			m_openMenuIndex = -1;
		}
	}
}
