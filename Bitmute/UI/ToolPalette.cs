using System;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class ToolPalette : ContentView
	{
		private eTool[] m_tools;
		private string[] m_glyphs;
		private string[] m_names;
		private Border[] m_buttons;
		private eTool m_selectedTool;

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
			m_tools = new eTool[] { eTool.Move, eTool.Select, eTool.EllipseSelect, eTool.MagicWand, eTool.Pencil, eTool.Brush, eTool.Eraser, eTool.Fill, eTool.Eyedropper, eTool.Text, eTool.Line, eTool.Zoom };
			m_glyphs = new string[] { "✛︎", "⬚", "◯", "✦︎", "✎︎", "▨", "▭", "▣", "◉", "T", "╱", "◎" };
			m_names = new string[] { "Move", "Rectangle Select", "Ellipse Select", "Magic Wand", "Pencil", "Brush", "Eraser", "Fill", "Eyedropper", "Text", "Line", "Zoom" };
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

			BackgroundColor = UiConstants.Chrome;
			Content = grid;
			SelectTool(eTool.Brush);
		}

		public eTool SelectedTool()
		{
			return m_selectedTool;
		}
	}
}
