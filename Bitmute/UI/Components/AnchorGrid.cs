using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class AnchorGrid : ContentView
	{
		private int m_selectedColumn;
		private int m_selectedRow;
		private Button[] m_buttons;

		public int HorizontalAnchor()
		{
			return m_selectedColumn - 1;
		}

		public int VerticalAnchor()
		{
			return m_selectedRow - 1;
		}

		private void UpdateSelection()
		{
			for (int row = 0; row < 3; row++)
			{
				for (int column = 0; column < 3; column++)
				{
					Button button = m_buttons[(row * 3) + column];
					if (row == m_selectedRow && column == m_selectedColumn)
					{
						button.ThemeBg(UiConstants.AppTitleBarLight, UiConstants.AppTitleBarDark);
					}
					else
					{
						button.ThemeBg(UiConstants.ButtonFaceLight, UiConstants.ButtonFaceDark);
					}
				}
			}
		}

		private void OnCellClicked(object sender, EventArgs eventArgs)
		{
			for (int index = 0; index < m_buttons.Length; index++)
			{
				if (ReferenceEquals(m_buttons[index], sender))
				{
					m_selectedRow = index / 3;
					m_selectedColumn = index % 3;
					UpdateSelection();
					return;
				}
			}
		}

		public AnchorGrid(string caption, int initialColumn, int initialRow)
		{
			m_selectedColumn = initialColumn;
			m_selectedRow = initialRow;
			m_buttons = new Button[9];

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			Grid grid = new Grid();
			grid.RowSpacing = 2.0;
			grid.ColumnSpacing = 2.0;
			for (int index = 0; index < 3; index++)
			{
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
				grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			}
			for (int row = 0; row < 3; row++)
			{
				for (int column = 0; column < 3; column++)
				{
					Button button = new Button();
					button.WidthRequest = 22.0;
					button.HeightRequest = 22.0;
					button.Padding = new Thickness(0.0);
					button.CornerRadius = 0;
					button.BorderColor = UiConstants.ButtonBorderLight;
					button.BorderWidth = 1.0;
					button.Clicked += OnCellClicked;
					m_buttons[(row * 3) + column] = button;
					Grid.SetRow(button, row);
					Grid.SetColumn(button, column);
					grid.Add(button);
				}
			}
			UpdateSelection();

			Grid layout = new Grid();
			layout.ColumnSpacing = UiConstants.DialogRowSpacing;
			layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(grid, 1);
			layout.Add(captionLabel);
			layout.Add(grid);
			Content = layout;
		}
	}
}
