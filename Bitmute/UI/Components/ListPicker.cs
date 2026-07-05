using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class ListPicker : ContentView
	{
		private Action<int> m_onChanged;
		private Picker m_picker;
		private bool m_updating;

		private void OnPickerChanged(object sender, EventArgs eventArgs)
		{
			if (m_updating)
			{
				return;
			}
			if (m_onChanged != null)
			{
				m_onChanged(m_picker.SelectedIndex);
			}
		}

		public int SelectedIndex()
		{
			return m_picker.SelectedIndex;
		}

		public void SetSelectedIndex(int index)
		{
			m_updating = true;
			m_picker.SelectedIndex = index;
			m_updating = false;
		}

		public ListPicker(string caption, string[] items, int initialIndex, Action<int> onChanged)
		{
			m_onChanged = onChanged;

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			m_picker = new Picker();
			m_picker.FontSize = UiConstants.ComponentFontSize;
			m_picker.HeightRequest = UiConstants.ComponentHeight;
			m_picker.WidthRequest = UiConstants.FieldPickerWidth;
			m_picker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_picker.VerticalOptions = LayoutOptions.Center;
			for (int index = 0; index < items.Length; index++)
			{
				m_picker.Items.Add(items[index]);
			}
			m_updating = true;
			m_picker.SelectedIndex = initialIndex;
			m_updating = false;
			m_picker.SelectedIndexChanged += OnPickerChanged;

			Grid row = new Grid();
			row.ColumnSpacing = UiConstants.DialogRowSpacing;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(m_picker, 1);
			row.Add(captionLabel);
			row.Add(m_picker);

			Content = row;
		}
	}
}
