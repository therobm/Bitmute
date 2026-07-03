using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class AdjustmentDialog : ContentView
	{
		private string m_filterId;
		private Slider[] m_sliders;
		private Label[] m_values;

		private void OnTitlePan(object sender, PanUpdatedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.DragModal(eventArgs.StatusType, eventArgs.TotalX, eventArgs.TotalY);
			}
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CloseModal();
			}
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
			main.CloseModal();
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

		private View BuildTitleBar(string text)
		{
			Label titleLabel = new Label();
			titleLabel.Text = text;
			titleLabel.FontSize = 13.0;
			titleLabel.TextColor = UiConstants.OnSurface;
			titleLabel.VerticalOptions = LayoutOptions.Center;

			Button closeButton = new Button();
			closeButton.Text = "✕";
			closeButton.FontSize = 12.0;
			closeButton.WidthRequest = UiConstants.CloseButtonSize;
			closeButton.HeightRequest = UiConstants.CloseButtonSize;
			closeButton.Padding = new Thickness(0.0);
			closeButton.BackgroundColor = Colors.Transparent;
			closeButton.TextColor = UiConstants.TextDim;
			closeButton.Clicked += OnCancelClicked;

			Grid titleBar = new Grid();
			titleBar.BackgroundColor = UiConstants.TitleBar;
			titleBar.Padding = new Thickness(8.0, 2.0, 2.0, 2.0);
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(titleLabel, 0);
			Grid.SetColumn(closeButton, 1);
			titleBar.Add(titleLabel);
			titleBar.Add(closeButton);

			PanGestureRecognizer pan = new PanGestureRecognizer();
			pan.PanUpdated += OnTitlePan;
			titleBar.GestureRecognizers.Add(pan);
			return titleBar;
		}

		private Grid BuildSliderRow(int index, string label, int minimum, int maximum, int initial)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.FontSize = 12.0;
			caption.TextColor = UiConstants.TextDim;
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
			value.TextColor = UiConstants.OnSurface;
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

			VerticalStackLayout innerLayout = new VerticalStackLayout();
			innerLayout.Spacing = 8.0;
			innerLayout.Padding = new Thickness(12.0);
			for (int index = 0; index < labels.Length; index++)
			{
				innerLayout.Add(BuildSliderRow(index, labels[index], minimums[index], maximums[index], defaults[index]));
			}

			Button applyButton = new Button();
			applyButton.Text = "Apply";
			applyButton.FontSize = 12.0;
			applyButton.WidthRequest = 90.0;
			applyButton.BackgroundColor = UiConstants.Accent;
			applyButton.TextColor = UiConstants.OnSurface;
			applyButton.Clicked += OnApplyClicked;

			Button cancelButton = new Button();
			cancelButton.Text = "Cancel";
			cancelButton.FontSize = 12.0;
			cancelButton.WidthRequest = 90.0;
			cancelButton.BackgroundColor = UiConstants.ChromeRaised;
			cancelButton.TextColor = UiConstants.OnSurface;
			cancelButton.Clicked += OnCancelClicked;

			HorizontalStackLayout buttons = new HorizontalStackLayout();
			buttons.Spacing = 8.0;
			buttons.HorizontalOptions = LayoutOptions.End;
			buttons.Add(cancelButton);
			buttons.Add(applyButton);
			innerLayout.Add(buttons);

			VerticalStackLayout layout = new VerticalStackLayout();
			layout.Spacing = 0.0;
			layout.Add(BuildTitleBar(title));
			layout.Add(innerLayout);

			Border frame = new Border();
			frame.BackgroundColor = UiConstants.PanelSurface;
			frame.Stroke = UiConstants.Divider;
			frame.StrokeThickness = 1.0;
			frame.Content = layout;

			Content = frame;
		}
	}
}
