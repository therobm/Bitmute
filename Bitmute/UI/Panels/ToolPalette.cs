using System;
using System.Collections.Generic;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using SkiaSharp;

namespace Bitmute.UI.Panels
{
	public class ToolPalette : ContentView
	{
		public class ToolEntry
		{
			public eTool m_tool;
			public List<eTool> m_childTools = new List<eTool>();
			public int m_row;
			public int m_col;
			public bool m_isDivider = false;
			public int m_activeToolIndex = 0;

			public ToolEntry(int row, int col)
			{
				m_isDivider = true;
				m_row = row;
				m_col = col;
			}

			public ToolEntry(eTool tool, int row, int col)
			{
				m_tool = tool;
				m_row = row;
				m_col = col;
			}

			public ToolEntry(eTool tool, List<eTool> childTools, int row, int col)
			{
				m_tool = tool;
				m_childTools = childTools;
				m_row = row;
				m_col = col;
			}

			public int ToolCount()
			{
				return 1 + m_childTools.Count;
			}

			public eTool ToolAt(int index)
			{
				if (index == 0)
				{
					return m_tool;
				}
				return m_childTools[index - 1];
			}

			public eTool ActiveTool()
			{
				return ToolAt(m_activeToolIndex);
			}
		}

		private const double FlyoutMemberSize = 30.0;
		private const int LongPressMilliseconds = 400;

		private List<ToolEntry> m_tools = new List<ToolEntry>();
		private Dictionary<eTool, string> m_toolIcons = new Dictionary<eTool, string>()
		{
			{ eTool.Select, "box_select.png" },
			{ eTool.EllipseSelect, "ellipse_select.png" },
			{ eTool.Move, "move.png" },
			{ eTool.Lasso, "lasso.png" },
			{ eTool.FreehandLasso, "freehand_lasso.png" },
			{ eTool.MagneticLasso, "magnetic_lasso.png" },
			{ eTool.MagicWand, "magic_wand.png" },
			{ eTool.Crop, "crop.png" },
			{ eTool.Heal, "heal.png" },
			{ eTool.Brush, "brush.png" },
			{ eTool.Pencil, "pencil.png" },
			{ eTool.ColorReplacement, "color_replacement.png" },
			{ eTool.Clone, "clone.png" },
			{ eTool.Eraser, "eraser.png" },
			{ eTool.Fill, "fill.png" },
			{ eTool.Gradient, "gradient.png" },
			{ eTool.Blur, "blur.png" },
			{ eTool.Sharpen, "sharpen.png" },
			{ eTool.Smudge, "smudge.png" },
			{ eTool.Dodge, "dodge.png" },
			{ eTool.Burn, "burn.png" },
			{ eTool.Sponge, "sponge.png" },
			{ eTool.Text, "text.png" },
			{ eTool.Line, "line.png" },
			{ eTool.RectangleShape, "rectangle.png" },
			{ eTool.RoundedRectangleShape, "rounded_rectangle.png" },
			{ eTool.EllipseShape, "ellipse.png" },
			{ eTool.PolygonShape, "polygon.png" },
			{ eTool.Ruler, "ruler.png" },
			{ eTool.Eyedropper, "eyedropper.png" },
			{ eTool.Hand, "hand.png" },
			{ eTool.Zoom, "zoom.png" },
			{ eTool.Pen, "pen.png" },
			{ eTool.DirectSelect, "directselect.png" },
		};
		private Dictionary<eTool, string> m_toolTips = new Dictionary<eTool, string>()
		{
			{ eTool.Select, "Rectangle Select" },
			{ eTool.EllipseSelect, "Ellipse Select" },
			{ eTool.Move, "Move" },
			{ eTool.Lasso, "Poly Lasso" },
			{ eTool.FreehandLasso, "Freehand Lasso" },
			{ eTool.MagneticLasso, "Magnetic Lasso (drag along an edge)" },
			{ eTool.MagicWand, "Magic Wand" },
			{ eTool.Crop, "Crop (double-click inside to commit)" },
			{ eTool.Heal, "Heal (Alt-click sets source)" },
			{ eTool.Brush, "Brush" },
			{ eTool.Pencil, "Pencil" },
			{ eTool.ColorReplacement, "Color Replacement" },
			{ eTool.Clone, "Clone (Alt-click sets source)" },
			{ eTool.Eraser, "Eraser" },
			{ eTool.Fill, "Fill" },
			{ eTool.Gradient, "Gradient (drag the axis)" },
			{ eTool.Blur, "Blur" },
			{ eTool.Sharpen, "Sharpen" },
			{ eTool.Smudge, "Smudge" },
			{ eTool.Dodge, "Dodge (Alt = Burn)" },
			{ eTool.Burn, "Burn (Alt = Dodge)" },
			{ eTool.Sponge, "Sponge (Saturate / Desaturate)" },
			{ eTool.Text, "Text" },
			{ eTool.Line, "Line" },
			{ eTool.RectangleShape, "Rectangle" },
			{ eTool.RoundedRectangleShape, "Rounded Rectangle" },
			{ eTool.EllipseShape, "Ellipse" },
			{ eTool.PolygonShape, "Polygon" },
			{ eTool.Ruler, "Ruler / Measure" },
			{ eTool.Eyedropper, "Eyedropper" },
			{ eTool.Hand, "Hand (drag to pan)" },
			{ eTool.Zoom, "Zoom (double-click tool = 100%)" },
			{ eTool.Pen, "Pen (click to add points, drag for curves, click start to close)" },
			{ eTool.DirectSelect, "Direct Selection (edit path points)" },
		};

		private Border[] m_cellButtons;
		private IconView[] m_cellIcons;
		private eTool m_selectedTool;

		private IDispatcherTimer m_longPressTimer;
		private int m_pressedCell;
		private bool m_longPressFired;
		private int m_flyoutEntry;
		private List<Border> m_flyoutButtons;

		private BoxView m_foregroundSwatch;
		private BoxView m_backgroundSwatch;

		private static Color ToMaui(SKColor color)
		{
			return new Color(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
		}

		private static double PageCoordinate(VisualElement element, bool horizontal)
		{
			double total = 0.0;
			Element current = element;
			for (int guard = 0; guard < 100; guard++)
			{
				VisualElement visual = current as VisualElement;
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
				Element parent = current.Parent;
				if (parent == null)
				{
					break;
				}
				current = parent;
			}
			return total;
		}

		private ToolState State()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return null;
			}
			return main.CurrentToolState();
		}

		private void OnForegroundTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.OpenColorPicker(true);
			}
		}

		private void OnBackgroundTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.OpenColorPicker(false);
			}
		}

		private void OnSwapTapped(object sender, EventArgs eventArgs)
		{
			ToolState state = State();
			if (state == null)
			{
				return;
			}
			state.SwapColors();
			RefreshColors();
		}

		private void OnResetTapped(object sender, EventArgs eventArgs)
		{
			ToolState state = State();
			if (state == null)
			{
				return;
			}
			state.ResetColors();
			RefreshColors();
		}

		private void OnSwapCornerTapped(object sender, TappedEventArgs eventArgs)
		{
			OnSwapTapped(sender, eventArgs);
		}

		private void OnResetCornerTapped(object sender, TappedEventArgs eventArgs)
		{
			OnResetTapped(sender, eventArgs);
		}

		private string ComposeTooltip(eTool tool)
		{
			string tip = m_toolTips[tool];
			string shortcut = Bitmute.UI.Operations.OperationRegistry.ShortcutForTool(tool);
			if (shortcut != "")
			{
				return tip + " (" + shortcut + ")";
			}
			return tip;
		}

		private Border BuildCornerButton(View content, string tip, EventHandler<TappedEventArgs> handler)
		{
			Border button = new Border();
			button.WidthRequest = 14.0;
			button.HeightRequest = 14.0;
			button.Padding = new Thickness(0.0);
			button.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			button.StrokeThickness = 0.0;
			button.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2.0) };
			button.Content = content;
			ToolTipProperties.SetText(button, tip);
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += handler;
			button.GestureRecognizers.Add(tap);
			return button;
		}

		private View BuildColorSwatches()
		{
			m_backgroundSwatch = new BoxView();
			TapGestureRecognizer backgroundTap = new TapGestureRecognizer();
			backgroundTap.Tapped += OnBackgroundTapped;
			m_backgroundSwatch.GestureRecognizers.Add(backgroundTap);

			m_foregroundSwatch = new BoxView();
			TapGestureRecognizer foregroundTap = new TapGestureRecognizer();
			foregroundTap.Tapped += OnForegroundTapped;
			m_foregroundSwatch.GestureRecognizers.Add(foregroundTap);

			IconView swapIcon = new IconView("swap_colors.png");
			swapIcon.WidthRequest = 12.0;
			swapIcon.HeightRequest = 12.0;
			swapIcon.BackgroundColor = Colors.Transparent;
			swapIcon.HorizontalOptions = LayoutOptions.Center;
			swapIcon.VerticalOptions = LayoutOptions.Center;

			Label resetLabel = new Label();
			resetLabel.Text = "D";
			resetLabel.FontSize = 9.0;
			resetLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			resetLabel.HorizontalTextAlignment = TextAlignment.Center;
			resetLabel.VerticalTextAlignment = TextAlignment.Center;

			Border swapButton = BuildCornerButton(swapIcon, "Swap colors (X)", OnSwapCornerTapped);
			Border resetButton = BuildCornerButton(resetLabel, "Default black/white", OnResetCornerTapped);

			AbsoluteLayout swatchStack = new AbsoluteLayout();
			swatchStack.WidthRequest = 54.0;
			swatchStack.HeightRequest = 54.0;
			AbsoluteLayout.SetLayoutBounds(m_backgroundSwatch, new Rect(18.0, 18.0, 30.0, 30.0));
			AbsoluteLayout.SetLayoutFlags(m_backgroundSwatch, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(m_foregroundSwatch, new Rect(0.0, 0.0, 30.0, 30.0));
			AbsoluteLayout.SetLayoutFlags(m_foregroundSwatch, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(swapButton, new Rect(40.0, 0.0, 14.0, 14.0));
			AbsoluteLayout.SetLayoutFlags(swapButton, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(resetButton, new Rect(0.0, 40.0, 14.0, 14.0));
			AbsoluteLayout.SetLayoutFlags(resetButton, AbsoluteLayoutFlags.None);
			swatchStack.Add(m_backgroundSwatch);
			swatchStack.Add(m_foregroundSwatch);
			swatchStack.Add(swapButton);
			swatchStack.Add(resetButton);

			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Padding = new Thickness(6.0, 8.0, 6.0, 6.0);
			row.Add(swatchStack);
			return row;
		}

		public void SwapColors()
		{
			OnSwapTapped(this, EventArgs.Empty);
		}

		public void RefreshColors()
		{
			ToolState state = State();
			if (state == null)
			{
				return;
			}
			m_foregroundSwatch.Color = ToMaui(state.Foreground());
			m_backgroundSwatch.Color = ToMaui(state.Background());
		}

		private Border BuildToolbarBreak()
		{
			Border line = new Border();
			line.HeightRequest = 1.0;
			line.StrokeThickness = 0.0;
			line.HorizontalOptions = LayoutOptions.Fill;
			line.VerticalOptions = LayoutOptions.Center;
			line.Margin = new Thickness(2.0, 3.0, 2.0, 3.0);
			line.ThemeBg(UiConstants.DividerLight, UiConstants.DividerDark);
			return line;
		}

		private Border BuildCell(int entryIndex)
		{
			ToolEntry entry = m_tools[entryIndex];
			eTool activeTool = entry.ActiveTool();

			IconView icon = new IconView(m_toolIcons[activeTool]);
			icon.WidthRequest = 20.0;
			icon.HeightRequest = 20.0;
			icon.BackgroundColor = Colors.Transparent;
			icon.HorizontalOptions = LayoutOptions.Center;
			icon.VerticalOptions = LayoutOptions.Center;
			m_cellIcons[entryIndex] = icon;

			Grid content = new Grid();
			content.Add(icon);
			if (entry.ToolCount() > 1)
			{
				Label triangle = new Label();
				triangle.Text = "◢";
				triangle.FontSize = 7.0;
				triangle.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
				triangle.HorizontalOptions = LayoutOptions.End;
				triangle.VerticalOptions = LayoutOptions.End;
				triangle.Margin = new Thickness(0.0, 0.0, 1.0, 0.0);
				content.Add(triangle);
			}

			Border button = new Border();
			button.WidthRequest = UiConstants.ToolButtonSize;
			button.HeightRequest = UiConstants.ToolButtonSize;
			button.ThemeBg(UiConstants.ToolButtonChipLight, UiConstants.ToolButtonChipDark);
			button.StrokeThickness = 0.0;
			button.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			button.Content = content;
			ToolTipProperties.SetText(button, ComposeTooltip(activeTool));

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnCellTapped;
			button.GestureRecognizers.Add(tap);

			TapGestureRecognizer doubleTap = new TapGestureRecognizer();
			doubleTap.NumberOfTapsRequired = 2;
			doubleTap.Tapped += OnCellDoubleTapped;
			button.GestureRecognizers.Add(doubleTap);

			PointerGestureRecognizer pointer = new PointerGestureRecognizer();
			pointer.PointerPressed += OnCellPointerPressed;
			pointer.PointerReleased += OnCellPointerReleased;
			button.GestureRecognizers.Add(pointer);

			return button;
		}

		private int CellIndexOf(object sender)
		{
			for (int index = 0; index < m_cellButtons.Length; index++)
			{
				if (m_cellButtons[index] != null && ReferenceEquals(m_cellButtons[index], sender))
				{
					return index;
				}
			}
			return -1;
		}

		private void OnCellTapped(object sender, TappedEventArgs eventArgs)
		{
			if (m_longPressFired)
			{
				m_longPressFired = false;
				return;
			}
			int cell = CellIndexOf(sender);
			if (cell < 0)
			{
				return;
			}
			SelectTool(m_tools[cell].ActiveTool());
		}

		private void OnCellDoubleTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int cell = CellIndexOf(sender);
			if (cell < 0)
			{
				return;
			}
			if (m_tools[cell].ActiveTool() == eTool.Zoom)
			{
				main.ZoomActiveTo100();
			}
		}

		private void OnCellPointerPressed(object sender, PointerEventArgs eventArgs)
		{
			m_longPressFired = false;
			int cell = CellIndexOf(sender);
			if (cell < 0 || m_tools[cell].ToolCount() <= 1)
			{
				m_pressedCell = -1;
				return;
			}
			m_pressedCell = cell;
			if (m_longPressTimer == null && Dispatcher != null)
			{
				m_longPressTimer = Dispatcher.CreateTimer();
				m_longPressTimer.Interval = TimeSpan.FromMilliseconds(LongPressMilliseconds);
				m_longPressTimer.IsRepeating = false;
				m_longPressTimer.Tick += OnLongPressTick;
			}
			if (m_longPressTimer != null)
			{
				m_longPressTimer.Stop();
				m_longPressTimer.Start();
			}
		}

		private void OnCellPointerReleased(object sender, PointerEventArgs eventArgs)
		{
			if (m_longPressTimer != null)
			{
				m_longPressTimer.Stop();
			}
			m_pressedCell = -1;
		}

		private void OnLongPressTick(object sender, EventArgs eventArgs)
		{
			if (m_longPressTimer != null)
			{
				m_longPressTimer.Stop();
			}
			if (m_pressedCell < 0)
			{
				return;
			}
			m_longPressFired = true;
			OpenFlyout(m_pressedCell);
		}

		private void OnFlyoutMemberTapped(object sender, TappedEventArgs eventArgs)
		{
			if (m_flyoutButtons == null)
			{
				return;
			}
			for (int index = 0; index < m_flyoutButtons.Count; index++)
			{
				if (ReferenceEquals(m_flyoutButtons[index], sender))
				{
					SelectTool(m_tools[m_flyoutEntry].ToolAt(index));
					return;
				}
			}
		}

		private void OpenFlyout(int entryIndex)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			ToolEntry entry = m_tools[entryIndex];
			m_flyoutEntry = entryIndex;
			m_flyoutButtons = new List<Border>();

			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = 4.0;
			row.Padding = new Thickness(4.0);
			for (int member = 0; member < entry.ToolCount(); member++)
			{
				eTool memberTool = entry.ToolAt(member);
				IconView memberIcon = new IconView(m_toolIcons[memberTool]);
				memberIcon.WidthRequest = 20.0;
				memberIcon.HeightRequest = 20.0;
				memberIcon.BackgroundColor = Colors.Transparent;
				memberIcon.HorizontalOptions = LayoutOptions.Center;
				memberIcon.VerticalOptions = LayoutOptions.Center;

				Border memberButton = new Border();
				memberButton.WidthRequest = FlyoutMemberSize;
				memberButton.HeightRequest = FlyoutMemberSize;
				memberButton.ThemeBg(UiConstants.ToolButtonChipLight, UiConstants.ToolButtonChipDark);
				memberButton.StrokeThickness = 0.0;
				memberButton.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
				memberButton.Content = memberIcon;
				ToolTipProperties.SetText(memberButton, ComposeTooltip(memberTool));
				TapGestureRecognizer memberTap = new TapGestureRecognizer();
				memberTap.Tapped += OnFlyoutMemberTapped;
				memberButton.GestureRecognizers.Add(memberTap);
				m_flyoutButtons.Add(memberButton);
				row.Add(memberButton);
			}

			double anchorX = PageCoordinate(m_cellButtons[entryIndex], true) + UiConstants.ToolButtonSize + 4.0;
			double anchorY = PageCoordinate(m_cellButtons[entryIndex], false);
			double width = (entry.ToolCount() * (FlyoutMemberSize + 4.0)) + 8.0;
			main.ShowPulldown(row, anchorX, anchorY, width, FlyoutMemberSize + 10.0);
		}

		private int EntryIndexOf(eTool tool)
		{
			for (int index = 0; index < m_tools.Count; index++)
			{
				if (m_tools[index].m_isDivider)
				{
					continue;
				}
				for (int member = 0; member < m_tools[index].ToolCount(); member++)
				{
					if (m_tools[index].ToolAt(member) == tool)
					{
						return index;
					}
				}
			}
			return -1;
		}

		private int MemberOf(int entryIndex, eTool tool)
		{
			for (int member = 0; member < m_tools[entryIndex].ToolCount(); member++)
			{
				if (m_tools[entryIndex].ToolAt(member) == tool)
				{
					return member;
				}
			}
			return -1;
		}

		private void SelectTool(eTool tool)
		{
			int entryIndex = EntryIndexOf(tool);
			if (entryIndex < 0)
			{
				return;
			}
			int memberIndex = MemberOf(entryIndex, tool);
			if (memberIndex < 0)
			{
				return;
			}
			m_selectedTool = tool;
			m_tools[entryIndex].m_activeToolIndex = memberIndex;
			m_cellIcons[entryIndex].SetIcon(m_toolIcons[tool]);
			ToolTipProperties.SetText(m_cellButtons[entryIndex], ComposeTooltip(tool));

			for (int index = 0; index < m_cellButtons.Length; index++)
			{
				if (m_cellButtons[index] == null)
				{
					continue;
				}
				bool selected = index == entryIndex;
				if (selected)
				{
					m_cellButtons[index].ThemeBg(UiConstants.ToolSelectedLight, UiConstants.ToolSelectedDark);
				}
				else
				{
					m_cellButtons[index].ThemeBg(UiConstants.ToolButtonChipLight, UiConstants.ToolButtonChipDark);
				}
				m_cellIcons[index].SetSelected(selected);
			}

			MainView main = MainView.Self;
			if (main != null)
			{
				main.OnToolSelected(tool);
			}
		}

		public ToolPalette()
		{
			m_tools = new List<ToolEntry>()
			{
				new ToolEntry(eTool.Select, new List<eTool>(){ eTool.EllipseSelect }, 0, 0),
				new ToolEntry(eTool.Move, 0, 1),
				new ToolEntry(eTool.Lasso, new List<eTool>(){ eTool.FreehandLasso, eTool.MagneticLasso }, 1, 0),
				new ToolEntry(eTool.MagicWand, 1, 1),
				new ToolEntry(eTool.Crop, 2, 0),   
				new ToolEntry(eTool.DirectSelect, 2, 1),
				new ToolEntry(3, 0),
				new ToolEntry(eTool.Heal, 4, 0),
				new ToolEntry(eTool.Brush, new List<eTool>(){ eTool.Pencil, eTool.ColorReplacement }, 4, 1),
				new ToolEntry(eTool.Clone, 5, 0),
				new ToolEntry(eTool.Pen, 5, 1),
				new ToolEntry(eTool.Eraser, 6, 0),
				new ToolEntry(eTool.Fill, new List<eTool>(){ eTool.Gradient }, 6, 1),
				new ToolEntry(eTool.Blur, new List<eTool>(){ eTool.Sharpen, eTool.Smudge }, 7, 0),
				new ToolEntry(eTool.Dodge, new List<eTool>(){ eTool.Burn, eTool.Sponge }, 7, 1),
				new ToolEntry(8, 0),
				new ToolEntry(eTool.Text, 9, 0),
				new ToolEntry(eTool.Line, new List<eTool>(){ eTool.RectangleShape, eTool.RoundedRectangleShape, eTool.EllipseShape, eTool.PolygonShape }, 9, 1),
				new ToolEntry(10, 0),
				new ToolEntry(eTool.Ruler, 11, 0),
				new ToolEntry(eTool.Eyedropper, 11, 1),
				new ToolEntry(eTool.Hand, 12, 0),
				new ToolEntry(eTool.Zoom, 12, 1),
			};

			m_cellButtons = new Border[m_tools.Count];
			m_cellIcons = new IconView[m_tools.Count];
			m_pressedCell = -1;

			Grid grid = new Grid();
			grid.Padding = new Thickness(5.0);
			grid.RowSpacing = 4.0;
			grid.ColumnSpacing = 4.0;
			grid.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			grid.VerticalOptions = LayoutOptions.Start;
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

			int rowCount = 0;
			for (int index = 0; index < m_tools.Count; index++)
			{
				if (m_tools[index].m_row + 1 > rowCount)
				{
					rowCount = m_tools[index].m_row + 1;
				}
			}
			for (int row = 0; row < rowCount; row++)
			{
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			}

			for (int index = 0; index < m_tools.Count; index++)
			{
				ToolEntry entry = m_tools[index];
				if (entry.m_isDivider)
				{
					Border divider = BuildToolbarBreak();
					Grid.SetRow(divider, entry.m_row);
					Grid.SetColumn(divider, 0);
					Grid.SetColumnSpan(divider, 2);
					grid.Add(divider);
					continue;
				}
				Border button = BuildCell(index);
				m_cellButtons[index] = button;
				Grid.SetRow(button, entry.m_row);
				Grid.SetColumn(button, entry.m_col);
				grid.Add(button);
			}

			VerticalStackLayout container = new VerticalStackLayout();
			container.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			container.Add(grid);
			container.Add(BuildColorSwatches());

			this.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			Content = container;
			SelectTool(eTool.Brush);
			RefreshColors();
		}

		public void SelectToolExternal(eTool tool)
		{
			SelectTool(tool);
		}

		public void ActivateToolKey(eTool primaryTool, bool cycle)
		{
			int entryIndex = EntryIndexOf(primaryTool);
			if (entryIndex < 0)
			{
				return;
			}
			ToolEntry entry = m_tools[entryIndex];
			int memberIndex = entry.m_activeToolIndex;
			bool groupActive = EntryIndexOf(m_selectedTool) == entryIndex;
			if (cycle && groupActive && entry.ToolCount() > 1)
			{
				memberIndex = memberIndex + 1;
				if (memberIndex >= entry.ToolCount())
				{
					memberIndex = 0;
				}
			}
			SelectTool(entry.ToolAt(memberIndex));
		}

		public eTool SelectedTool()
		{
			return SelectedTool_Impl();
		}

		private eTool SelectedTool_Impl()
		{
			return m_selectedTool;
		}
	}
}