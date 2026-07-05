using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class CheckField : ContentView
	{
		private Action<bool> m_onChanged;
		private CheckBox m_check;
		private bool m_updating;

		private void OnCheckChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_updating)
			{
				return;
			}
			if (m_onChanged != null)
			{
				m_onChanged(eventArgs.Value);
			}
		}

		public bool Checked()
		{
			return m_check.IsChecked;
		}

		public void SetChecked(bool value)
		{
			m_updating = true;
			m_check.IsChecked = value;
			m_updating = false;
		}

		public CheckField(string caption, bool initial, Action<bool> onChanged)
		{
			m_onChanged = onChanged;

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			m_check = new CheckBox();
			m_check.IsChecked = initial;
			m_check.HorizontalOptions = LayoutOptions.Start;
			m_check.VerticalOptions = LayoutOptions.Center;
			m_check.SetAppThemeColor(CheckBox.ColorProperty, UiConstants.AccentLight, UiConstants.AccentDark);
			m_check.CheckedChanged += OnCheckChanged;

			Grid row = new Grid();
			row.ColumnSpacing = UiConstants.DialogRowSpacing;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(m_check, 1);
			row.Add(captionLabel);
			row.Add(m_check);

			Content = row;
		}
	}
}
