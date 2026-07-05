using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class TextField : ContentView
	{
		private Action<string> m_onChanged;
		private Entry m_entry;
		private bool m_updating;

		private void OnEntryTextChanged(object sender, TextChangedEventArgs eventArgs)
		{
			if (m_updating)
			{
				return;
			}
			if (m_onChanged != null)
			{
				m_onChanged(Text());
			}
		}

		public string Text()
		{
			string text = m_entry.Text;
			if (text == null)
			{
				return "";
			}
			return text;
		}

		public void SetText(string text)
		{
			m_updating = true;
			m_entry.Text = text;
			m_updating = false;
		}

		public TextField(string caption, string initial, Action<string> onChanged)
		{
			m_onChanged = onChanged;

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			m_entry = new Entry();
			m_entry.FontSize = UiConstants.PanelFontSize;
			m_entry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_entry.VerticalOptions = LayoutOptions.Center;
			m_entry.Text = initial;
			m_entry.TextChanged += OnEntryTextChanged;

			Grid row = new Grid();
			row.ColumnSpacing = UiConstants.DialogRowSpacing;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(m_entry, 1);
			row.Add(captionLabel);
			row.Add(m_entry);

			Content = row;
		}
	}
}
