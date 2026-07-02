using System.Collections.Generic;
using Bitmute.Imaging;
using Bitmute.Storage;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

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
		private ColorPanel m_colorPanel;
		private LayersPanel m_layersPanel;
		private Label m_optionsToolLabel;
		private Slider m_brushSizeSlider;
		private Label m_brushSizeValue;
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
		private PencilTool m_pencilTool;
		private BrushTool m_brushTool;
		private EraserTool m_eraserTool;
		private EyedropperTool m_eyedropperTool;
		private FillTool m_fillTool;

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
				return new string[] { "Image Size…", "Canvas Size…" };
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
				return new string[] { "Blur", "Sharpen", "Invert Colors" };
			}
			if (title == "View")
			{
				return new string[] { "Zoom In", "Zoom Out", "Fit on Screen" };
			}
			if (title == "Window")
			{
				return new string[] { "Tools", "Layers", "Color" };
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
			if (title == "Filter")
			{
				if (item == "Invert Colors")
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
				return false;
			}
			return false;
		}

		private Border BuildMenuButton(int index)
		{
			Label label = new Label();
			label.Text = m_menuTitles[index];
			label.FontSize = 12.0;
			label.TextColor = UiConstants.OnSurface;
			label.VerticalOptions = LayoutOptions.Center;

			Border button = new Border();
			button.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			button.BackgroundColor = UiConstants.Chrome;
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
			strip.BackgroundColor = UiConstants.Chrome;
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
			CloseMenu();
			m_openMenuIndex = index;
			m_menuButtons[index].BackgroundColor = UiConstants.ChromeRaised;

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
			dropdown.BackgroundColor = UiConstants.PanelSurface;
			dropdown.Stroke = UiConstants.Divider;
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
				label.TextColor = UiConstants.OnSurface;
			}
			else
			{
				label.TextColor = UiConstants.TextDim;
			}

			Border row = new Border();
			row.HeightRequest = MenuItemHeight;
			row.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
			row.BackgroundColor = UiConstants.PanelSurface;
			row.StrokeThickness = 0.0;
			row.Content = label;

			if (enabled)
			{
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnMenuItemTapped;
				row.GestureRecognizers.Add(tap);
				m_openItemButtons.Add(row);
				m_openItemActions.Add(item);
			}

			return row;
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
				m_menuButtons[m_openMenuIndex].BackgroundColor = UiConstants.Chrome;
			}
			m_openMenuIndex = -1;
		}

		private void InvokeMenuAction(string action)
		{
			if (action == "New")
			{
				NewDocument();
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
			if (action == "Invert Colors")
			{
				DoInvert();
			}
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
			bar.BackgroundColor = UiConstants.Chrome;
			bar.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			bar.ColumnSpacing = 16.0;
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			m_optionsToolLabel = new Label();
			m_optionsToolLabel.TextColor = UiConstants.OnSurface;
			m_optionsToolLabel.FontSize = 12.0;
			m_optionsToolLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_optionsToolLabel, 0);
			bar.Add(m_optionsToolLabel);

			Label sizeLabel = new Label();
			sizeLabel.Text = "Size";
			sizeLabel.TextColor = UiConstants.TextDim;
			sizeLabel.FontSize = 12.0;
			sizeLabel.VerticalOptions = LayoutOptions.Center;

			m_brushSizeSlider = new Slider();
			m_brushSizeSlider.Minimum = 1.0;
			m_brushSizeSlider.Maximum = 100.0;
			m_brushSizeSlider.WidthRequest = 180.0;
			m_brushSizeSlider.VerticalOptions = LayoutOptions.Center;
			m_brushSizeSlider.ValueChanged += OnBrushSizeChanged;

			m_brushSizeValue = new Label();
			m_brushSizeValue.TextColor = UiConstants.OnSurface;
			m_brushSizeValue.FontSize = 12.0;
			m_brushSizeValue.WidthRequest = 44.0;
			m_brushSizeValue.VerticalOptions = LayoutOptions.Center;

			HorizontalStackLayout options = new HorizontalStackLayout();
			options.Spacing = 8.0;
			options.VerticalOptions = LayoutOptions.Center;
			options.Add(sizeLabel);
			options.Add(m_brushSizeSlider);
			options.Add(m_brushSizeValue);
			Grid.SetColumn(options, 1);
			bar.Add(options);

			m_brushSizeSlider.Value = m_toolState.BrushSize();

			return bar;
		}

		private View BuildStatusBar()
		{
			Grid bar = new Grid();
			bar.HeightRequest = UiConstants.StatusBarHeight;
			bar.BackgroundColor = UiConstants.Chrome;
			bar.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			m_statusInfoLabel = new Label();
			m_statusInfoLabel.Text = "100%      800 × 600 px";
			m_statusInfoLabel.TextColor = UiConstants.TextDim;
			m_statusInfoLabel.FontSize = 11.0;
			m_statusInfoLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusInfoLabel, 0);
			bar.Add(m_statusInfoLabel);

			m_statusCursorLabel = new Label();
			m_statusCursorLabel.Text = "x: —   y: —";
			m_statusCursorLabel.TextColor = UiConstants.TextDim;
			m_statusCursorLabel.FontSize = 11.0;
			m_statusCursorLabel.HorizontalOptions = LayoutOptions.End;
			m_statusCursorLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusCursorLabel, 1);
			bar.Add(m_statusCursorLabel);

			return bar;
		}

		private View BuildPaletteDock()
		{
			m_colorPanel = new ColorPanel();
			m_layersPanel = new LayersPanel();
			PaletteGroup topGroup = new PaletteGroup(new string[] { "Color", "Swatches" }, m_colorPanel);
			PaletteGroup bottomGroup = new PaletteGroup(new string[] { "Layers", "Channels" }, m_layersPanel);

			Grid dock = new Grid();
			dock.BackgroundColor = UiConstants.Chrome;
			dock.Padding = new Thickness(4.0);
			dock.RowSpacing = 4.0;
			dock.RowDefinitions.Add(new RowDefinition(new GridLength(230.0)));
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Star));

			Grid.SetRow(topGroup, 0);
			dock.Add(topGroup);
			Grid.SetRow(bottomGroup, 1);
			dock.Add(bottomGroup);

			return dock;
		}

		private BoxView BuildDivider()
		{
			BoxView divider = new BoxView();
			divider.Color = UiConstants.Divider;
			return divider;
		}

		private View BuildMiddle()
		{
			m_toolPalette = new ToolPalette();

			m_workspace = new AbsoluteLayout();
			m_workspace.BackgroundColor = UiConstants.WorkspaceBackdrop;

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
			BackgroundColor = UiConstants.WorkspaceBackdrop;

			m_documents = new List<FloatingPanel>();
			m_openItemButtons = new List<Border>();
			m_openItemActions = new List<string>();
			m_openMenuIndex = -1;
			m_untitledCount = 0;
			m_cascadeCount = 0;
			m_topZIndex = 0;
			m_toolState = new ToolState();
			m_moveTool = new MoveTool();
			m_pencilTool = new PencilTool();
			m_brushTool = new BrushTool();
			m_eraserTool = new EraserTool();
			m_eyedropperTool = new EyedropperTool();
			m_fillTool = new FillTool();

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

			NewDocument();
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
			AddAccelerator(element, Windows.System.VirtualKey.Number0, OnAcceleratorFit);
			AddAccelerator(element, Windows.System.VirtualKey.Add, OnAcceleratorZoomIn);
			AddAccelerator(element, Windows.System.VirtualKey.Subtract, OnAcceleratorZoomOut);
			m_acceleratorsHooked = true;
		}

		private void AddAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void OnAcceleratorNew(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			NewDocument();
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

		public void NewDocument()
		{
			m_untitledCount++;
			Document model = new Document("Untitled-" + m_untitledCount, (int)UiConstants.DefaultDocumentWidth, (int)UiConstants.DefaultDocumentHeight);
			DocumentWindow window = new DocumentWindow(model);
			PlaceAndAdd(window);
		}

		private void PlaceAndAdd(DocumentWindow window)
		{
			double offset = m_cascadeCount * UiConstants.CascadeOffset;
			m_cascadeCount++;
			double x = 30.0 + offset;
			double y = 24.0 + offset;
			AddDocument(window, x, y, UiConstants.DefaultDocumentWindowWidth, UiConstants.DefaultDocumentWindowHeight);
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
			try
			{
				Document model = ActiveDocument();
				if (model == null)
				{
					return;
				}
				string path = await FileDialogs.PickSaveAsync(model.Title());
				if (path == null)
				{
					return;
				}
				ImageFile.Encode(model, path);
				SetStatusMessage("Saved " + System.IO.Path.GetFileName(path));
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Save failed: " + error.Message);
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
			if (m_colorPanel != null)
			{
				m_colorPanel.Refresh();
			}
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
		}

		public void OnCanvasInteracted()
		{
			if (m_colorPanel != null)
			{
				m_colorPanel.Refresh();
			}
		}

		public void ClosePanel(FloatingPanel panel)
		{
			if (m_documents.Contains(panel))
			{
				m_documents.Remove(panel);
				m_workspace.Remove(panel);
			}
		}

		public void OnToolSelected(eTool tool)
		{
			if (m_toolState != null)
			{
				m_toolState.SetTool(tool);
			}
			if (m_optionsToolLabel != null)
			{
				m_optionsToolLabel.Text = tool.ToString();
			}
		}

		private void OnBrushSizeChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int size = (int)m_brushSizeSlider.Value;
			m_toolState.SetBrushSize(size);
			if (m_brushSizeValue != null)
			{
				m_brushSizeValue.Text = size + " px";
			}
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
			return null;
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
