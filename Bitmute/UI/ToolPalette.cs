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

namespace Bitmute.UI
{
	public class ToolPalette : ContentView
	{
		private const double FlyoutMemberSize = 30.0;
		private const int LongPressMilliseconds = 400;

		private eTool[][] m_groupTools;
		private string[][] m_groupIcons;
		private string[][] m_groupNames;
		private int[] m_groupActive;
		private Border[] m_cellButtons;
		private IconView[] m_cellIcons;
		private eTool m_selectedTool;

		private IDispatcherTimer m_longPressTimer;
		private int m_pressedCell;
		private bool m_longPressFired;
		private int m_flyoutGroup;
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

		private Border BuildCell(int groupIndex)
		{
			int active = m_groupActive[groupIndex];
			IconView icon = new IconView(m_groupIcons[groupIndex][active]);
			icon.WidthRequest = 20.0;
			icon.HeightRequest = 20.0;
			icon.BackgroundColor = Colors.Transparent;
			icon.HorizontalOptions = LayoutOptions.Center;
			icon.VerticalOptions = LayoutOptions.Center;
			m_cellIcons[groupIndex] = icon;

			Grid content = new Grid();
			content.Add(icon);
			if (m_groupTools[groupIndex].Length > 1)
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
			ToolTipProperties.SetText(button, m_groupNames[groupIndex][active]);

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
				if (ReferenceEquals(m_cellButtons[index], sender))
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
			SelectTool(m_groupTools[cell][m_groupActive[cell]]);
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
			if (m_groupTools[cell][m_groupActive[cell]] == eTool.Zoom)
			{
				main.ZoomActiveTo100();
			}
		}

		private void OnCellPointerPressed(object sender, PointerEventArgs eventArgs)
		{
			m_longPressFired = false;
			int cell = CellIndexOf(sender);
			if (cell < 0 || m_groupTools[cell].Length <= 1)
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
					SelectTool(m_groupTools[m_flyoutGroup][index]);
					return;
				}
			}
		}

		private void OpenFlyout(int groupIndex)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			m_flyoutGroup = groupIndex;
			m_flyoutButtons = new List<Border>();

			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = 4.0;
			row.Padding = new Thickness(4.0);
			for (int member = 0; member < m_groupTools[groupIndex].Length; member++)
			{
				IconView memberIcon = new IconView(m_groupIcons[groupIndex][member]);
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
				ToolTipProperties.SetText(memberButton, m_groupNames[groupIndex][member]);
				TapGestureRecognizer memberTap = new TapGestureRecognizer();
				memberTap.Tapped += OnFlyoutMemberTapped;
				memberButton.GestureRecognizers.Add(memberTap);
				m_flyoutButtons.Add(memberButton);
				row.Add(memberButton);
			}

			double anchorX = PageCoordinate(m_cellButtons[groupIndex], true) + UiConstants.ToolButtonSize + 4.0;
			double anchorY = PageCoordinate(m_cellButtons[groupIndex], false);
			double width = (m_groupTools[groupIndex].Length * (FlyoutMemberSize + 4.0)) + 8.0;
			main.ShowPulldown(row, anchorX, anchorY, width, FlyoutMemberSize + 10.0);
		}

		private int GroupOf(eTool tool)
		{
			for (int group = 0; group < m_groupTools.Length; group++)
			{
				for (int member = 0; member < m_groupTools[group].Length; member++)
				{
					if (m_groupTools[group][member] == tool)
					{
						return group;
					}
				}
			}
			return -1;
		}

		private int MemberOf(int groupIndex, eTool tool)
		{
			for (int member = 0; member < m_groupTools[groupIndex].Length; member++)
			{
				if (m_groupTools[groupIndex][member] == tool)
				{
					return member;
				}
			}
			return -1;
		}

		private void SelectTool(eTool tool)
		{
			int groupIndex = GroupOf(tool);
			if (groupIndex < 0)
			{
				return;
			}
			int memberIndex = MemberOf(groupIndex, tool);
			if (memberIndex < 0)
			{
				return;
			}
			m_selectedTool = tool;
			m_groupActive[groupIndex] = memberIndex;
			m_cellIcons[groupIndex].SetIcon(m_groupIcons[groupIndex][memberIndex]);
			ToolTipProperties.SetText(m_cellButtons[groupIndex], m_groupNames[groupIndex][memberIndex]);

			for (int group = 0; group < m_cellButtons.Length; group++)
			{
				bool selected = group == groupIndex;
				if (selected)
				{
					m_cellButtons[group].ThemeBg(UiConstants.ToolSelectedLight, UiConstants.ToolSelectedDark);
				}
				else
				{
					m_cellButtons[group].ThemeBg(UiConstants.ToolButtonChipLight, UiConstants.ToolButtonChipDark);
				}
				m_cellIcons[group].SetSelected(selected);
			}

			MainView main = MainView.Self;
			if (main != null)
			{
				main.OnToolSelected(tool);
			}
		}

		public ToolPalette()
		{
			m_groupTools = new eTool[][]
			{
				new eTool[] { eTool.Move },
				new eTool[] { eTool.Select, eTool.EllipseSelect },
				new eTool[] { eTool.Lasso, eTool.FreehandLasso, eTool.MagneticLasso },
				new eTool[] { eTool.MagicWand },
				new eTool[] { eTool.Crop },
				new eTool[] { eTool.Slice },
				new eTool[] { eTool.Brush, eTool.Pencil, eTool.ColorReplacement },
				new eTool[] { eTool.Eraser },
				new eTool[] { eTool.Clone, eTool.Heal },
				new eTool[] { eTool.Fill, eTool.Gradient },
				new eTool[] { eTool.Blur, eTool.Sharpen, eTool.Smudge },
				new eTool[] { eTool.DodgeBurn, eTool.Sponge },
				new eTool[] { eTool.Text },
				new eTool[] { eTool.Line, eTool.RectangleShape, eTool.RoundedRectangleShape, eTool.EllipseShape, eTool.PolygonShape },
				new eTool[] { eTool.Ruler },
				new eTool[] { eTool.Eyedropper },
				new eTool[] { eTool.Hand },
				new eTool[] { eTool.Zoom }
			};
			m_groupIcons = new string[][]
			{
				new string[] { "move.png" },
				new string[] { "box_select.png", "ellipse_select.png" },
				new string[] { "lasso.png", "freehand_lasso.png", "magnetic_lasso.png" },
				new string[] { "magic_wand.png" },
				new string[] { "crop.png" },
				new string[] { "slice.png" },
				new string[] { "brush.png", "pencil.png", "color_replacement.png" },
				new string[] { "eraser.png" },
				new string[] { "clone.png", "heal.png" },
				new string[] { "fill.png", "gradient.png" },
				new string[] { "blur.png", "sharpen.png", "smudge.png" },
				new string[] { "dodge.png", "sponge.png" },
				new string[] { "text.png" },
				new string[] { "line.png", "rectangle.png", "rounded_rectangle.png", "ellipse.png", "polygon.png" },
				new string[] { "ruler.png" },
				new string[] { "eyedropper.png" },
				new string[] { "hand.png" },
				new string[] { "zoom.png" }
			};
			m_groupNames = new string[][]
			{
				new string[] { "Move" },
				new string[] { "Rectangle Select", "Ellipse Select" },
				new string[] { "Poly Lasso", "Freehand Lasso", "Magnetic Lasso (drag along an edge)" },
				new string[] { "Magic Wand" },
				new string[] { "Crop (double-click inside to commit)" },
				new string[] { "Slice (drag to add a named region)" },
				new string[] { "Brush", "Pencil", "Color Replacement" },
				new string[] { "Eraser" },
				new string[] { "Clone (Alt-click sets source)", "Heal (Alt-click sets source)" },
				new string[] { "Fill", "Gradient (drag the axis)" },
				new string[] { "Blur", "Sharpen", "Smudge" },
				new string[] { "Dodge / Burn (Alt = Burn)", "Sponge (Saturate / Desaturate)" },
				new string[] { "Text" },
				new string[] { "Line", "Rectangle", "Rounded Rectangle", "Ellipse", "Polygon" },
				new string[] { "Ruler / Measure" },
				new string[] { "Eyedropper" },
				new string[] { "Hand (drag to pan)" },
				new string[] { "Zoom (double-click tool = 100%)" }
			};
			m_groupActive = new int[m_groupTools.Length];
			m_cellButtons = new Border[m_groupTools.Length];
			m_cellIcons = new IconView[m_groupTools.Length];
			m_pressedCell = -1;

			Grid grid = new Grid();
			grid.Padding = new Thickness(5.0);
			grid.RowSpacing = 4.0;
			grid.ColumnSpacing = 4.0;
			grid.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			grid.VerticalOptions = LayoutOptions.Start;
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

			int rowCount = (m_groupTools.Length + 1) / 2;
			for (int row = 0; row < rowCount; row++)
			{
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			}

			for (int index = 0; index < m_groupTools.Length; index++)
			{
				Border button = BuildCell(index);
				m_cellButtons[index] = button;
				Grid.SetRow(button, index / 2);
				Grid.SetColumn(button, index % 2);
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

		public eTool SelectedTool()
		{
			return m_selectedTool;
		}
	}
}
