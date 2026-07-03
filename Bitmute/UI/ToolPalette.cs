using System;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using SkiaSharp;

namespace Bitmute.UI
{
	public class ToolPalette : ContentView
	{
		private eTool[] m_tools;
		private string[] m_glyphs;
		private string[] m_icons;
		private string[] m_names;
		private Border[] m_buttons;
		private IconView[] m_iconViews;
		private eTool m_selectedTool;
		private BoxView m_foregroundSwatch;
		private BoxView m_backgroundSwatch;

		private static Color ToMaui(SKColor color)
		{
			return new Color(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
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

		private Border BuildToolButton(int index)
		{
			IconView icon = new IconView(m_icons[index]);
			icon.WidthRequest = 20.0;
			icon.HeightRequest = 20.0;
			icon.BackgroundColor = Colors.Transparent;
			icon.HorizontalOptions = LayoutOptions.Center;
			icon.VerticalOptions = LayoutOptions.Center;
			m_iconViews[index] = icon;

			Border button = new Border();
			button.WidthRequest = UiConstants.ToolButtonSize;
			button.HeightRequest = UiConstants.ToolButtonSize;
			button.ThemeBg(UiConstants.ToolButtonChipLight, UiConstants.ToolButtonChipDark);
			button.StrokeThickness = 0.0;
			button.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			button.Content = icon;
			ToolTipProperties.SetText(button, m_names[index]);

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnToolTapped;
			button.GestureRecognizers.Add(tap);

			TapGestureRecognizer doubleTap = new TapGestureRecognizer();
			doubleTap.NumberOfTapsRequired = 2;
			doubleTap.Tapped += OnToolDoubleTapped;
			button.GestureRecognizers.Add(doubleTap);

			return button;
		}

		private void OnToolDoubleTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			for (int index = 0; index < m_buttons.Length; index++)
			{
				if (ReferenceEquals(m_buttons[index], sender))
				{
					if (m_tools[index] == eTool.Zoom)
					{
						main.ZoomActiveTo100();
					}
					return;
				}
			}
		}

		private void OnToolTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_buttons.Length; index++)
			{
				if (ReferenceEquals(m_buttons[index], sender))
				{
					SelectTool(m_tools[index]);
					return;
				}
			}
		}

		private void SelectTool(eTool tool)
		{
			m_selectedTool = tool;
			for (int index = 0; index < m_tools.Length; index++)
			{
				if (m_tools[index] == tool)
				{
					m_buttons[index].ThemeBg(UiConstants.ToolSelectedLight, UiConstants.ToolSelectedDark);
					m_iconViews[index].SetSelected(true);
				}
				else
				{
					m_buttons[index].ThemeBg(UiConstants.ToolButtonChipLight, UiConstants.ToolButtonChipDark);
					m_iconViews[index].SetSelected(false);
				}
			}

			MainView main = MainView.Self;
			if (main != null)
			{
				main.OnToolSelected(tool);
			}
		}

		public ToolPalette()
		{
			m_tools = new eTool[] { eTool.Move, eTool.Select, eTool.EllipseSelect, eTool.Lasso, eTool.MagicWand, eTool.Pencil, eTool.Brush, eTool.Eraser, eTool.Clone, eTool.Fill, eTool.Gradient, eTool.Eyedropper, eTool.Text, eTool.Line, eTool.Blur, eTool.Sharpen, eTool.Smudge, eTool.DodgeBurn, eTool.Hand, eTool.Zoom };
			m_glyphs = new string[] { "✛︎", "⬚", "◯", "⬠", "✦︎", "✎︎", "▨", "▭", "⎘", "▣", "◧", "◉", "T", "╱", "○", "◭", "☟", "◐", "✋", "◎" };
			m_icons = new string[] { "move.png", "box_select.png", "ellipse_select.png", "lasso.png", "magic_wand.png", "pencil.png", "brush.png", "eraser.png", "clone.png", "fill.png", "fill.png", "eyedropper.png", "text.png", "line.png", "blur.png", "sharpen.png", "smudge.png", "dodge.png", "hand.png", "zoom.png" };
			m_names = new string[] { "Move", "Rectangle Select", "Ellipse Select", "Poly Lasso", "Magic Wand", "Pencil", "Brush", "Eraser", "Clone (Alt-click sets source)", "Fill", "Gradient (drag the axis)", "Eyedropper", "Text", "Line", "Blur", "Sharpen", "Smudge", "Dodge / Burn (Alt = Burn)", "Hand (drag to pan)", "Zoom (double-click tool = 100%)" };
			m_buttons = new Border[m_tools.Length];
			m_iconViews = new IconView[m_tools.Length];

			Grid grid = new Grid();
			grid.Padding = new Thickness(5.0);
			grid.RowSpacing = 4.0;
			grid.ColumnSpacing = 4.0;
			grid.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			grid.VerticalOptions = LayoutOptions.Start;
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

			int rowCount = (m_tools.Length + 1) / 2;
			for (int row = 0; row < rowCount; row++)
			{
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			}

			for (int index = 0; index < m_tools.Length; index++)
			{
				Border button = BuildToolButton(index);
				m_buttons[index] = button;
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

		public eTool SelectedTool()
		{
			return m_selectedTool;
		}
	}
}
