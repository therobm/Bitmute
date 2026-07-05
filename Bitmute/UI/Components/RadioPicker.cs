using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class RadioPicker : ContentView
	{
		private static int s_groupCounter;

		private Action<int> m_onChanged;
		private RadioButton[] m_radios;
		private bool m_updating;

		private void OnRadioChecked(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_updating)
			{
				return;
			}
			if (!eventArgs.Value)
			{
				return;
			}
			if (m_onChanged == null)
			{
				return;
			}
			for (int index = 0; index < m_radios.Length; index++)
			{
				if (ReferenceEquals(m_radios[index], sender))
				{
					m_onChanged(index);
					return;
				}
			}
		}

		public int SelectedIndex()
		{
			for (int index = 0; index < m_radios.Length; index++)
			{
				if (m_radios[index].IsChecked)
				{
					return index;
				}
			}
			return -1;
		}

		public void SetSelectedIndex(int index)
		{
			if (index < 0 || index >= m_radios.Length)
			{
				return;
			}
			m_updating = true;
			m_radios[index].IsChecked = true;
			m_updating = false;
		}

		public RadioPicker(string caption, string[] options, int initialIndex, Action<int> onChanged)
		{
			m_onChanged = onChanged;
			s_groupCounter = s_groupCounter + 1;
			string groupName = "RadioPicker" + s_groupCounter;

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			HorizontalStackLayout optionRow = new HorizontalStackLayout();
			optionRow.Spacing = UiConstants.DialogRowSpacing;
			m_radios = new RadioButton[options.Length];
			for (int index = 0; index < options.Length; index++)
			{
				RadioButton radio = new RadioButton();
				radio.Content = options[index];
				radio.FontSize = UiConstants.PanelFontSize;
				radio.GroupName = groupName;
				radio.VerticalOptions = LayoutOptions.Center;
				radio.SetAppThemeColor(RadioButton.TextColorProperty, UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				m_radios[index] = radio;
				optionRow.Add(radio);
			}
			if (initialIndex >= 0 && initialIndex < m_radios.Length)
			{
				m_updating = true;
				m_radios[initialIndex].IsChecked = true;
				m_updating = false;
			}
			for (int index = 0; index < m_radios.Length; index++)
			{
				m_radios[index].CheckedChanged += OnRadioChecked;
			}

			Grid row = new Grid();
			row.ColumnSpacing = UiConstants.DialogRowSpacing;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(optionRow, 1);
			row.Add(captionLabel);
			row.Add(optionRow);

			Content = row;
		}
	}
}
