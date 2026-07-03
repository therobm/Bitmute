using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class AdjustmentDialog : ModalDialog
	{
		private string m_filterId;
		private Slider[] m_sliders;
		private Label[] m_values;

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		private void OnApplyClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int first = 0;
			int second = 0;
			int third = 0;
			if (m_sliders.Length > 0)
			{
				first = (int)m_sliders[0].Value;
			}
			if (m_sliders.Length > 1)
			{
				second = (int)m_sliders[1].Value;
			}
			if (m_sliders.Length > 2)
			{
				third = (int)m_sliders[2].Value;
			}
			main.ApplyAdjustment(m_filterId, first, second, third);
			CloseModal();
		}

		private void OnSliderChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			for (int index = 0; index < m_sliders.Length; index++)
			{
				if (ReferenceEquals(m_sliders[index], sender))
				{
					m_values[index].Text = ((int)m_sliders[index].Value).ToString();
					return;
				}
			}
		}

		private Grid BuildSliderRow(int index, string label, int minimum, int maximum, int initial)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.FontSize = 12.0;
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.WidthRequest = 96.0;
			caption.VerticalOptions = LayoutOptions.Center;

			Slider slider = new Slider();
			slider.Minimum = minimum;
			slider.Maximum = maximum;
			slider.Value = initial;
			slider.WidthRequest = 160.0;
			slider.VerticalOptions = LayoutOptions.Center;
			slider.ValueChanged += OnSliderChanged;
			m_sliders[index] = slider;

			Label value = new Label();
			value.Text = initial.ToString();
			value.FontSize = 12.0;
			value.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			value.WidthRequest = 40.0;
			value.HorizontalTextAlignment = TextAlignment.End;
			value.VerticalOptions = LayoutOptions.Center;
			m_values[index] = value;

			Grid row = new Grid();
			row.ColumnSpacing = 8.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(caption, 0);
			Grid.SetColumn(slider, 1);
			Grid.SetColumn(value, 2);
			row.Add(caption);
			row.Add(slider);
			row.Add(value);
			return row;
		}

		public AdjustmentDialog(string title, string filterId, string[] labels, int[] minimums, int[] maximums, int[] defaults)
		{
			m_filterId = filterId;
			m_sliders = new Slider[labels.Length];
			m_values = new Label[labels.Length];

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			for (int index = 0; index < labels.Length; index++)
			{
				body.Add(BuildSliderRow(index, labels[index], minimums[index], maximums[index], defaults[index]));
			}

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("Apply", OnApplyClicked);
			ComposeDialog(title, body, ButtonRow(cancelButton, applyButton));
		}
	}
}
