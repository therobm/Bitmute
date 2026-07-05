using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class SectionHeader : ContentView
	{
		public SectionHeader(string text)
		{
			BoxView line = new BoxView();
			line.HeightRequest = UiConstants.PanelBorderThickness;
			line.ThemeBg(UiConstants.DividerLight, UiConstants.DividerDark);
			line.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = UiConstants.DialogRowSpacing;
			if (text.Length > 0)
			{
				Label header = new Label();
				header.Text = text;
				header.FontSize = UiConstants.PanelFontSize;
				header.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
				header.VerticalOptions = LayoutOptions.Center;
				row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
				row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
				Grid.SetColumn(header, 0);
				Grid.SetColumn(line, 1);
				row.Add(header);
				row.Add(line);
			}
			else
			{
				row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
				Grid.SetColumn(line, 0);
				row.Add(line);
			}

			Content = row;
		}
	}
}
