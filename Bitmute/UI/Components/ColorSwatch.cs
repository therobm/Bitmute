using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class ColorSwatch : ContentView
	{
		private Action<SKColor> m_onChanged;
		private Border m_swatch;
		private SKColor m_color;

		private static Color ToMauiColor(SKColor color)
		{
			return Color.FromRgba((int)color.Red, (int)color.Green, (int)color.Blue, (int)color.Alpha);
		}

		private void OnColorPicked(SKColor color)
		{
			m_color = color;
			m_swatch.BackgroundColor = ToMauiColor(color);
			if (m_onChanged != null)
			{
				m_onChanged(color);
			}
		}

		private void OnSwatchTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.OpenColorPickerFor(m_color, OnColorPicked);
		}

		public SKColor Value()
		{
			return m_color;
		}

		public void SetValue(SKColor color)
		{
			m_color = color;
			m_swatch.BackgroundColor = ToMauiColor(color);
		}

		public ColorSwatch(string caption, SKColor initial, Action<SKColor> onChanged)
		{
			m_onChanged = onChanged;
			m_color = initial;

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			m_swatch = new Border();
			m_swatch.WidthRequest = UiConstants.FieldSwatchWidth;
			m_swatch.HeightRequest = UiConstants.FieldSwatchHeight;
			m_swatch.BackgroundColor = ToMauiColor(initial);
			m_swatch.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			m_swatch.StrokeThickness = UiConstants.PanelBorderThickness;
			m_swatch.VerticalOptions = LayoutOptions.Center;
			m_swatch.HorizontalOptions = LayoutOptions.Start;
			TapGestureRecognizer recognizer = new TapGestureRecognizer();
			recognizer.Tapped += OnSwatchTapped;
			m_swatch.GestureRecognizers.Add(recognizer);

			Grid row = new Grid();
			row.ColumnSpacing = UiConstants.DialogRowSpacing;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(m_swatch, 1);
			row.Add(captionLabel);
			row.Add(m_swatch);

			Content = row;
		}
	}
}
