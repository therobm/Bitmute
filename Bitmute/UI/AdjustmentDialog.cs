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
		private CheckBox m_previewCheck;
		private bool m_previewable;
		private bool m_applied;

		private int SliderValue(int index)
		{
			if (index < m_sliders.Length)
			{
				return (int)m_sliders[index].Value;
			}
			return 0;
		}

		private void RunPreview()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.PreviewAdjustment(m_filterId, SliderValue(0), SliderValue(1), SliderValue(2));
		}

		public void CancelPreview()
		{
			if (m_applied)
			{
				return;
			}
			if (!m_previewable)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.CancelAdjustment();
		}

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
			int first = SliderValue(0);
			int second = SliderValue(1);
			int third = SliderValue(2);
			if (m_previewable)
			{
				m_applied = true;
				main.CommitAdjustment(m_filterId, first, second, third);
			}
			else
			{
				main.ApplyAdjustment(m_filterId, first, second, third);
			}
			CloseModal();
		}

		private void OnSliderChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			for (int index = 0; index < m_sliders.Length; index++)
			{
				if (ReferenceEquals(m_sliders[index], sender))
				{
					m_values[index].Text = ((int)m_sliders[index].Value).ToString();
					break;
				}
			}
			if (!m_previewable)
			{
				return;
			}
			if (m_previewCheck == null)
			{
				return;
			}
			if (!m_previewCheck.IsChecked)
			{
				return;
			}
			RunPreview();
		}

		private void OnPreviewCheckChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (!m_previewable)
			{
				return;
			}
			if (eventArgs.Value)
			{
				RunPreview();
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.RestoreAdjustmentPreview();
		}

		private Grid BuildSliderRow(int index, string label, int minimum, int maximum, int initial)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.FontSize = UiConstants.PanelFontSize;
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
			value.FontSize = UiConstants.PanelFontSize;
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

		private Grid BuildPreviewRow()
		{
			m_previewCheck = new CheckBox();
			m_previewCheck.IsChecked = true;
			m_previewCheck.HorizontalOptions = LayoutOptions.Start;
			m_previewCheck.VerticalOptions = LayoutOptions.Center;
			m_previewCheck.SetAppThemeColor(CheckBox.ColorProperty, UiConstants.AccentLight, UiConstants.AccentDark);
			m_previewCheck.CheckedChanged += OnPreviewCheckChanged;

			Label caption = new Label();
			caption.Text = "Preview";
			caption.FontSize = UiConstants.PanelFontSize;
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = 8.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(m_previewCheck, 0);
			Grid.SetColumn(caption, 1);
			row.Add(m_previewCheck);
			row.Add(caption);
			return row;
		}

		public AdjustmentDialog(string title, string filterId, string[] labels, int[] minimums, int[] maximums, int[] defaults)
		{
			m_filterId = filterId;
			m_previewable = MainView.IsAdjustmentPreviewable(filterId);
			m_applied = false;
			m_sliders = new Slider[labels.Length];
			m_values = new Label[labels.Length];

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			for (int index = 0; index < labels.Length; index++)
			{
				body.Add(BuildSliderRow(index, labels[index], minimums[index], maximums[index], defaults[index]));
			}
			if (m_previewable)
			{
				body.Add(BuildPreviewRow());
			}

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("Apply", OnApplyClicked);
			ComposeDialog(title, body, ButtonRow(cancelButton, applyButton));
		}
	}
}
