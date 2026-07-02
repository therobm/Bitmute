using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class BrushSettingsDialog : ContentView
	{
		private Picker m_tipPicker;
		private Slider m_spacingSlider;
		private Label m_spacingValue;

		private void OnTitlePan(object sender, PanUpdatedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.DragModal(eventArgs.StatusType, eventArgs.TotalX, eventArgs.TotalY);
			}
		}

		private void OnCloseClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CloseModal();
			}
		}

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
			closeButton.Clicked += OnCloseClicked;

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

			VerticalStackLayout innerLayout = new VerticalStackLayout();
			innerLayout.Spacing = 10.0;
			innerLayout.Padding = new Thickness(12.0);
			innerLayout.Add(tipRow);
			innerLayout.Add(spacingRow);

			VerticalStackLayout layout = new VerticalStackLayout();
			layout.Spacing = 0.0;
			layout.Add(BuildTitleBar("Brush Settings"));
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
