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
		private string[] m_names;
		private Border[] m_buttons;
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

			AbsoluteLayout swatchStack = new AbsoluteLayout();
			swatchStack.WidthRequest = 48.0;
			swatchStack.HeightRequest = 48.0;
			AbsoluteLayout.SetLayoutBounds(m_backgroundSwatch, new Rect(16.0, 16.0, 30.0, 30.0));
			AbsoluteLayout.SetLayoutFlags(m_backgroundSwatch, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(m_foregroundSwatch, new Rect(0.0, 0.0, 30.0, 30.0));
			AbsoluteLayout.SetLayoutFlags(m_foregroundSwatch, AbsoluteLayoutFlags.None);
			swatchStack.Add(m_backgroundSwatch);
			swatchStack.Add(m_foregroundSwatch);

			Button swapButton = new Button();
			swapButton.Text = "⇄";
			swapButton.FontSize = 12.0;
			swapButton.WidthRequest = 26.0;
			swapButton.HeightRequest = 22.0;
			swapButton.Padding = new Thickness(0.0);
			swapButton.BackgroundColor = UiConstants.ChromeRaised;
			swapButton.TextColor = UiConstants.OnSurface;
			swapButton.Clicked += OnSwapTapped;

			Button resetButton = new Button();
			resetButton.Text = "D";
			resetButton.FontSize = 11.0;
			resetButton.WidthRequest = 26.0;
			resetButton.HeightRequest = 22.0;
			resetButton.Padding = new Thickness(0.0);
			resetButton.BackgroundColor = UiConstants.ChromeRaised;
			resetButton.TextColor = UiConstants.OnSurface;
			resetButton.Clicked += OnResetTapped;

			VerticalStackLayout swapReset = new VerticalStackLayout();
			swapReset.Spacing = 4.0;
			swapReset.VerticalOptions = LayoutOptions.Center;
			swapReset.Add(swapButton);
			swapReset.Add(resetButton);

			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = 6.0;
			row.Padding = new Thickness(6.0, 8.0, 6.0, 6.0);
			row.Add(swatchStack);
			row.Add(swapReset);
			return row;
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
			Label glyph = new Label();
			glyph.Text = m_glyphs[index];
			glyph.FontSize = 14.0;
			glyph.TextColor = UiConstants.OnSurface;
			glyph.HorizontalOptions = LayoutOptions.Center;
			glyph.VerticalOptions = LayoutOptions.Center;

			Border button = new Border();
			button.WidthRequest = UiConstants.ToolButtonSize;
			button.HeightRequest = UiConstants.ToolButtonSize;
			button.BackgroundColor = UiConstants.ToolResting;
			button.Stroke = UiConstants.Divider;
			button.StrokeThickness = 1.0;
			button.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2.0) };
			button.Content = glyph;
			ToolTipProperties.SetText(button, m_names[index]);

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnToolTapped;
			button.GestureRecognizers.Add(tap);

			return button;
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
					m_buttons[index].BackgroundColor = UiConstants.ToolSelected;
					m_buttons[index].Stroke = UiConstants.Accent;
				}
				else
				{
					m_buttons[index].BackgroundColor = UiConstants.ToolResting;
					m_buttons[index].Stroke = UiConstants.Divider;
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
			m_tools = new eTool[] { eTool.Move, eTool.Select, eTool.EllipseSelect, eTool.Lasso, eTool.MagicWand, eTool.Pencil, eTool.Brush, eTool.Eraser, eTool.Fill, eTool.Eyedropper, eTool.Text, eTool.Line, eTool.Zoom };
			m_glyphs = new string[] { "✛︎", "⬚", "◯", "⬠", "✦︎", "✎︎", "▨", "▭", "▣", "◉", "T", "╱", "◎" };
			m_names = new string[] { "Move", "Rectangle Select", "Ellipse Select", "Poly Lasso", "Magic Wand", "Pencil", "Brush", "Eraser", "Fill", "Eyedropper", "Text", "Line", "Zoom" };
			m_buttons = new Border[m_tools.Length];

			Grid grid = new Grid();
			grid.Padding = new Thickness(5.0);
			grid.RowSpacing = 4.0;
			grid.ColumnSpacing = 4.0;
			grid.BackgroundColor = UiConstants.Chrome;
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
			container.BackgroundColor = UiConstants.Chrome;
			container.Add(grid);
			container.Add(BuildColorSwatches());

			BackgroundColor = UiConstants.Chrome;
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
