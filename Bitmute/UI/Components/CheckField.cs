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
		private CheckMark m_check;

		private void OnMarkChanged(bool value)
		{
			if (m_onChanged != null)
			{
				m_onChanged(value);
			}
		}

		public bool Checked()
		{
			return m_check.Checked();
		}

		public void SetChecked(bool value)
		{
			m_check.SetChecked(value);
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

			m_check = new CheckMark(initial, OnMarkChanged);
			m_check.VerticalOptions = LayoutOptions.Center;

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
