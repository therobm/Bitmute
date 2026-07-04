using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class ChannelsPanel : ContentView
	{
		private string[] m_names;
		private Border[] m_rows;
		private Label[] m_labels;

		private Border BuildRow(int index)
		{
			Label label = new Label();
			label.Text = m_names[index];
			label.FontSize = 12.0;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;
			label.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
			m_labels[index] = label;

			Border row = new Border();
			row.StrokeThickness = 0.0;
			row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			row.Content = label;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnRowTapped;
			row.GestureRecognizers.Add(tap);
			return row;
		}

		private void OnRowTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_rows.Length; index++)
			{
				if (ReferenceEquals(m_rows[index], sender))
				{
					MainView main = MainView.Self;
					if (main != null)
					{
						main.SelectChannelView(index - 1);
					}
					return;
				}
			}
		}

		public void Refresh()
		{
			MainView main = MainView.Self;
			int mode = -1;
			if (main != null)
			{
				mode = main.ChannelViewMode();
			}
			for (int index = 0; index < m_rows.Length; index++)
			{
				bool active = (index - 1) == mode;
				if (active)
				{
					m_rows[index].ThemeBg(UiConstants.ToolSelectedLight, UiConstants.ToolSelectedDark);
				}
				else
				{
					m_rows[index].ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
				}
			}
		}

		public ChannelsPanel()
		{
			m_names = new string[] { "RGB", "Red", "Green", "Blue", "Alpha" };
			m_rows = new Border[m_names.Length];
			m_labels = new Label[m_names.Length];

			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 1.0;
			list.Padding = new Thickness(4.0);
			for (int index = 0; index < m_names.Length; index++)
			{
				Border row = BuildRow(index);
				m_rows[index] = row;
				list.Add(row);
			}

			this.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			Content = list;
			Refresh();
		}
	}
}
