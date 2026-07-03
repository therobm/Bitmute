using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class BrushSettingsDialog : ModalDialog
	{
		private Picker m_tipPicker;
		private Slider m_spacingSlider;
		private Label m_spacingValue;

		private void OnTipChanged(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ApplyBrushTip(m_tipPicker.SelectedIndex == 1);
		}

		private void OnSpacingChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int spacing = (int)m_spacingSlider.Value;
			main.ApplyBrushSpacing(spacing);
			if (m_spacingValue != null)
			{
				m_spacingValue.Text = spacing + "%";
			}
		}

		public BrushSettingsDialog(bool square, int spacing)
		{
			Label tipLabel = new Label();
			tipLabel.Text = "Tip";
			tipLabel.TextColor = UiConstants.TextDim;
			tipLabel.FontSize = 12.0;
			tipLabel.WidthRequest = 60.0;
			tipLabel.VerticalOptions = LayoutOptions.Center;

			m_tipPicker = new Picker();
			m_tipPicker.FontSize = 12.0;
			m_tipPicker.TextColor = UiConstants.OnSurface;
			m_tipPicker.Items.Add("Round");
			m_tipPicker.Items.Add("Square");
			m_tipPicker.SelectedIndex = 0;
			if (square)
			{
				m_tipPicker.SelectedIndex = 1;
			}
			m_tipPicker.SelectedIndexChanged += OnTipChanged;

			Grid tipRow = new Grid();
			tipRow.ColumnSpacing = 8.0;
			tipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			tipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(tipLabel, 0);
			Grid.SetColumn(m_tipPicker, 1);
			tipRow.Add(tipLabel);
			tipRow.Add(m_tipPicker);

			Label spacingLabel = new Label();
			spacingLabel.Text = "Spacing";
			spacingLabel.TextColor = UiConstants.TextDim;
			spacingLabel.FontSize = 12.0;
			spacingLabel.WidthRequest = 60.0;
			spacingLabel.VerticalOptions = LayoutOptions.Center;

			m_spacingSlider = new Slider();
			m_spacingSlider.Minimum = 1.0;
			m_spacingSlider.Maximum = 100.0;
			m_spacingSlider.WidthRequest = 140.0;
			m_spacingSlider.VerticalOptions = LayoutOptions.Center;
			m_spacingSlider.Value = spacing;
			m_spacingSlider.ValueChanged += OnSpacingChanged;

			m_spacingValue = new Label();
			m_spacingValue.Text = spacing + "%";
			m_spacingValue.TextColor = UiConstants.OnSurface;
			m_spacingValue.FontSize = 12.0;
			m_spacingValue.WidthRequest = 44.0;
			m_spacingValue.VerticalOptions = LayoutOptions.Center;

			Grid spacingRow = new Grid();
			spacingRow.ColumnSpacing = 8.0;
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(spacingLabel, 0);
			Grid.SetColumn(m_spacingSlider, 1);
			Grid.SetColumn(m_spacingValue, 2);
			spacingRow.Add(spacingLabel);
			spacingRow.Add(m_spacingSlider);
			spacingRow.Add(m_spacingValue);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 10.0;
			body.Add(tipRow);
			body.Add(spacingRow);

			ComposeDialog("Brush Settings", body, null);
		}
	}
}
