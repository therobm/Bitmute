using System.Collections.Generic;
using Bitmute.Imaging;
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
		private string[] m_menuTitles;
		private Border[] m_menuButtons;
		private List<Border> m_openItemButtons;
		private List<string> m_openItemActions;
		private int m_openMenuIndex;
		private int m_untitledCount;
		private int m_topZIndex;
		private ToolState m_toolState;
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
				return new string[] { "Blur", "Sharpen", "Invert" };
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
			if (title != "File")
			{
				return false;
			}
			if (item == "New")
			{
				return true;
			}
			if (item == "Exit")
			{
				return true;
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
			if (action == "Exit")
			{
				Application current = Application.Current;
				if (current != null)
				{
					current.Quit();
				}
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

			Label details = new Label();
			details.Text = "Size 12 px      Opacity 100%      Hardness 100%";
			details.TextColor = UiConstants.TextDim;
			details.FontSize = 12.0;
			details.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(details, 1);
			bar.Add(details);

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

			Label left = new Label();
			left.Text = "100%      800 × 600 px";
			left.TextColor = UiConstants.TextDim;
			left.FontSize = 11.0;
			left.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(left, 0);
			bar.Add(left);

			Label right = new Label();
			right.Text = "x: —   y: —";
			right.TextColor = UiConstants.TextDim;
			right.FontSize = 11.0;
			right.HorizontalOptions = LayoutOptions.End;
			right.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(right, 1);
			bar.Add(right);

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
			m_topZIndex = 0;
			m_toolState = new ToolState();
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

		public void NewDocument()
		{
			m_untitledCount++;
			Document model = new Document("Untitled-" + m_untitledCount, (int)UiConstants.DefaultDocumentWidth, (int)UiConstants.DefaultDocumentHeight);
			DocumentWindow window = new DocumentWindow(model);
			double offset = (m_untitledCount - 1) * UiConstants.CascadeOffset;
			double x = 30.0 + offset;
			double y = 24.0 + offset;
			AddDocument(window, x, y, UiConstants.DefaultDocumentWindowWidth, UiConstants.DefaultDocumentWindowHeight);
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

		public ToolState CurrentToolState()
		{
			return m_toolState;
		}

		public Tool CurrentTool()
		{
			eTool tool = m_toolState.Tool();
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
