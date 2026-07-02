using System;
using System.Globalization;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Bitmute.UI
{
	public class ColorPanel : ContentView
	{
		private BoxView m_foregroundSwatch;
		private BoxView m_backgroundSwatch;
		private Slider m_red;
		private Slider m_green;
		private Slider m_blue;
		private Slider m_alpha;
		private Label m_redValue;
		private Label m_greenValue;
		private Label m_blueValue;
		private Label m_alphaValue;
		private Entry m_hex;
		private bool m_suppress;

		private static Color ToMauiColor(SKColor color)
		{
			return new Color(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
		}

		private static string ToHex(SKColor color)
		{
			return "#" + color.Red.ToString("X2") + color.Green.ToString("X2") + color.Blue.ToString("X2");
		}

		private ToolState State()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return null;
			}
			return main.CurrentToolState();
		}

		private Grid BuildChannelRow(string name, Slider slider, Label valueLabel)
		{
			slider.Minimum = 0.0;
			slider.Maximum = 255.0;
			slider.ValueChanged += OnChannelChanged;

			Label nameLabel = new Label();
			nameLabel.Text = name;
			nameLabel.TextColor = UiConstants.TextDim;
			nameLabel.FontSize = 11.0;
			nameLabel.WidthRequest = 14.0;
			nameLabel.VerticalOptions = LayoutOptions.Center;

			valueLabel.TextColor = UiConstants.OnSurface;
			valueLabel.FontSize = 11.0;
			valueLabel.WidthRequest = 30.0;
			valueLabel.VerticalOptions = LayoutOptions.Center;
			valueLabel.HorizontalTextAlignment = TextAlignment.End;

			Grid row = new Grid();
			row.ColumnSpacing = 6.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(nameLabel, 0);
			Grid.SetColumn(slider, 1);
			Grid.SetColumn(valueLabel, 2);
			row.Add(nameLabel);
			row.Add(slider);
			row.Add(valueLabel);
			return row;
		}

		private void ApplyFromSliders()
		{
			ToolState state = State();
			if (state == null)
			{
				return;
			}
			byte red = (byte)m_red.Value;
			byte green = (byte)m_green.Value;
			byte blue = (byte)m_blue.Value;
			byte alpha = (byte)m_alpha.Value;
			SKColor color = new SKColor(red, green, blue, alpha);
			state.SetForeground(color);

			m_redValue.Text = red.ToString();
			m_greenValue.Text = green.ToString();
			m_blueValue.Text = blue.ToString();
			m_alphaValue.Text = alpha.ToString();
			m_foregroundSwatch.Color = ToMauiColor(color);
			m_hex.Text = ToHex(color);
		}

		private void OnChannelChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_suppress)
			{
				return;
			}
			ApplyFromSliders();
		}

		private void OnHexCompleted(object sender, EventArgs eventArgs)
		{
			string text = m_hex.Text;
			if (text == null)
			{
				return;
			}
			text = text.Trim();
			if (text.StartsWith("#"))
			{
				text = text.Substring(1);
			}
			if (text.Length != 6)
			{
				return;
			}
			byte red = 0;
			byte green = 0;
			byte blue = 0;
			bool redOk = byte.TryParse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out red);
			bool greenOk = byte.TryParse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out green);
			bool blueOk = byte.TryParse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out blue);
			if (!redOk || !greenOk || !blueOk)
			{
				return;
			}
			m_suppress = true;
			m_red.Value = red;
			m_green.Value = green;
			m_blue.Value = blue;
			m_suppress = false;
			ApplyFromSliders();
		}

		private void OnSwapClicked(object sender, EventArgs eventArgs)
		{
			ToolState state = State();
			if (state == null)
			{
				return;
			}
			state.SwapColors();
			Refresh();
		}

		private void OnResetClicked(object sender, EventArgs eventArgs)
		{
			ToolState state = State();
			if (state == null)
			{
				return;
			}
			state.ResetColors();
			Refresh();
		}

		public ColorPanel()
		{
			m_foregroundSwatch = new BoxView();
			m_foregroundSwatch.WidthRequest = 30.0;
			m_foregroundSwatch.HeightRequest = 30.0;

			m_backgroundSwatch = new BoxView();
			m_backgroundSwatch.WidthRequest = 30.0;
			m_backgroundSwatch.HeightRequest = 30.0;

			Button swapButton = new Button();
			swapButton.Text = "X";
			swapButton.FontSize = 11.0;
			swapButton.WidthRequest = 30.0;
			swapButton.HeightRequest = 28.0;
			swapButton.Padding = new Thickness(0.0);
			swapButton.BackgroundColor = UiConstants.ChromeRaised;
			swapButton.TextColor = UiConstants.OnSurface;
			swapButton.Clicked += OnSwapClicked;

			Button resetButton = new Button();
			resetButton.Text = "D";
			resetButton.FontSize = 11.0;
			resetButton.WidthRequest = 30.0;
			resetButton.HeightRequest = 28.0;
			resetButton.Padding = new Thickness(0.0);
			resetButton.BackgroundColor = UiConstants.ChromeRaised;
			resetButton.TextColor = UiConstants.OnSurface;
			resetButton.Clicked += OnResetClicked;

			HorizontalStackLayout swatchRow = new HorizontalStackLayout();
			swatchRow.Spacing = 6.0;
			swatchRow.Add(m_foregroundSwatch);
			swatchRow.Add(m_backgroundSwatch);
			swatchRow.Add(swapButton);
			swatchRow.Add(resetButton);

			m_red = new Slider();
			m_green = new Slider();
			m_blue = new Slider();
			m_alpha = new Slider();
			m_redValue = new Label();
			m_greenValue = new Label();
			m_blueValue = new Label();
			m_alphaValue = new Label();

			m_hex = new Entry();
			m_hex.FontSize = 12.0;
			m_hex.TextColor = UiConstants.OnSurface;
			m_hex.Completed += OnHexCompleted;

			VerticalStackLayout layout = new VerticalStackLayout();
			layout.Spacing = 6.0;
			layout.Padding = new Thickness(8.0);
			layout.Add(swatchRow);
			layout.Add(BuildChannelRow("R", m_red, m_redValue));
			layout.Add(BuildChannelRow("G", m_green, m_greenValue));
			layout.Add(BuildChannelRow("B", m_blue, m_blueValue));
			layout.Add(BuildChannelRow("A", m_alpha, m_alphaValue));
			layout.Add(m_hex);

			Content = layout;
			Refresh();
		}

		public void Refresh()
		{
			ToolState state = State();
			if (state == null)
			{
				return;
			}
			SKColor foreground = state.Foreground();
			SKColor background = state.Background();
			m_suppress = true;
			m_red.Value = foreground.Red;
			m_green.Value = foreground.Green;
			m_blue.Value = foreground.Blue;
			m_alpha.Value = foreground.Alpha;
			m_suppress = false;
			m_redValue.Text = foreground.Red.ToString();
			m_greenValue.Text = foreground.Green.ToString();
			m_blueValue.Text = foreground.Blue.ToString();
			m_alphaValue.Text = foreground.Alpha.ToString();
			m_foregroundSwatch.Color = ToMauiColor(foreground);
			m_backgroundSwatch.Color = ToMauiColor(background);
			m_hex.Text = ToHex(foreground);
		}
	}
}
