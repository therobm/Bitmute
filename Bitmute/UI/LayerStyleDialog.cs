using System;
using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Bitmute.UI
{
	public class LayerStyleDialog : ModalDialog
	{
		private CheckBox m_strokeEnable;
		private SliderField m_strokeSize;
		private Picker m_strokePosition;
		private Border m_strokeSwatch;
		private SKColor m_strokeColor;
		private CheckBox m_shadowEnable;
		private Border m_shadowSwatch;
		private SKColor m_shadowColor;
		private SliderField m_shadowOpacity;
		private SliderField m_shadowAngle;
		private SliderField m_shadowDistance;
		private SliderField m_shadowSize;
		private CheckBox m_glowEnable;
		private Border m_glowSwatch;
		private SKColor m_glowColor;
		private SliderField m_glowOpacity;
		private SliderField m_glowSize;

		private static Color ToMauiColor(SKColor color)
		{
			return Color.FromRgba((int)color.Red, (int)color.Green, (int)color.Blue, (int)color.Alpha);
		}

		private void OnSliderChanged(int value)
		{
		}

		private static Label SectionLabel(string text)
		{
			Label label = new Label();
			label.Text = text;
			label.FontSize = 13.0;
			label.FontAttributes = FontAttributes.Bold;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;
			return label;
		}

		private static Label FieldLabel(string text)
		{
			Label label = new Label();
			label.Text = text;
			label.FontSize = 12.0;
			label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			label.VerticalOptions = LayoutOptions.Center;
			label.WidthRequest = 72.0;
			return label;
		}

		private static View LabeledRow(string label, View control)
		{
			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = 8.0;
			row.Add(FieldLabel(label));
			row.Add(control);
			return row;
		}

		private View HeaderRow(string title, CheckBox enable)
		{
			enable.VerticalOptions = LayoutOptions.Center;
			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = 8.0;
			row.Add(enable);
			row.Add(SectionLabel(title));
			return row;
		}

		private Border BuildSwatch(SKColor color, EventHandler<TappedEventArgs> tapHandler)
		{
			Border swatch = new Border();
			swatch.WidthRequest = 44.0;
			swatch.HeightRequest = 20.0;
			swatch.BackgroundColor = ToMauiColor(color);
			swatch.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			swatch.StrokeThickness = 1.0;
			swatch.VerticalOptions = LayoutOptions.Center;
			TapGestureRecognizer recognizer = new TapGestureRecognizer();
			recognizer.Tapped += tapHandler;
			swatch.GestureRecognizers.Add(recognizer);
			return swatch;
		}

		public LayerStyleDialog(LayerStyle style)
		{
			m_strokeColor = style.m_strokeColor;
			m_shadowColor = style.m_shadowColor;
			m_glowColor = style.m_glowColor;

			m_strokeEnable = new CheckBox();
			m_strokeEnable.IsChecked = style.m_hasStroke;
			m_strokeSize = new SliderField(1, 100, style.m_strokeSize, " px", OnSliderChanged);
			m_strokeSize.VerticalOptions = LayoutOptions.Center;
			m_strokePosition = new Picker();
			m_strokePosition.FontSize = 12.0;
			m_strokePosition.WidthRequest = 130.0;
			m_strokePosition.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_strokePosition.VerticalOptions = LayoutOptions.Center;
			m_strokePosition.Items.Add("Inside");
			m_strokePosition.Items.Add("Center");
			m_strokePosition.Items.Add("Outside");
			m_strokePosition.SelectedIndex = style.m_strokePosition;
			m_strokeSwatch = BuildSwatch(m_strokeColor, OnStrokeColorTapped);

			m_shadowEnable = new CheckBox();
			m_shadowEnable.IsChecked = style.m_hasDropShadow;
			m_shadowSwatch = BuildSwatch(m_shadowColor, OnShadowColorTapped);
			m_shadowOpacity = new SliderField(0, 100, style.m_shadowOpacity, "%", OnSliderChanged);
			m_shadowOpacity.VerticalOptions = LayoutOptions.Center;
			m_shadowAngle = new SliderField(0, 360, style.m_shadowAngle, "°", OnSliderChanged);
			m_shadowAngle.VerticalOptions = LayoutOptions.Center;
			m_shadowDistance = new SliderField(0, 100, style.m_shadowDistance, " px", OnSliderChanged);
			m_shadowDistance.VerticalOptions = LayoutOptions.Center;
			m_shadowSize = new SliderField(0, 100, style.m_shadowSize, " px", OnSliderChanged);
			m_shadowSize.VerticalOptions = LayoutOptions.Center;

			m_glowEnable = new CheckBox();
			m_glowEnable.IsChecked = style.m_hasOuterGlow;
			m_glowSwatch = BuildSwatch(m_glowColor, OnGlowColorTapped);
			m_glowOpacity = new SliderField(0, 100, style.m_glowOpacity, "%", OnSliderChanged);
			m_glowOpacity.VerticalOptions = LayoutOptions.Center;
			m_glowSize = new SliderField(0, 100, style.m_glowSize, " px", OnSliderChanged);
			m_glowSize.VerticalOptions = LayoutOptions.Center;

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			body.WidthRequest = 320.0;
			body.Add(HeaderRow("Stroke", m_strokeEnable));
			body.Add(LabeledRow("Size", m_strokeSize));
			body.Add(LabeledRow("Position", m_strokePosition));
			body.Add(LabeledRow("Color", m_strokeSwatch));
			body.Add(HeaderRow("Drop Shadow", m_shadowEnable));
			body.Add(LabeledRow("Color", m_shadowSwatch));
			body.Add(LabeledRow("Opacity", m_shadowOpacity));
			body.Add(LabeledRow("Angle", m_shadowAngle));
			body.Add(LabeledRow("Distance", m_shadowDistance));
			body.Add(LabeledRow("Size", m_shadowSize));
			body.Add(HeaderRow("Outer Glow", m_glowEnable));
			body.Add(LabeledRow("Color", m_glowSwatch));
			body.Add(LabeledRow("Opacity", m_glowOpacity));
			body.Add(LabeledRow("Size", m_glowSize));

			Label colorNote = new Label();
			colorNote.Text = "Tap a color to use the current foreground color.";
			colorNote.FontSize = 11.0;
			colorNote.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			body.Add(colorNote);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("OK", OnApplyClicked);
			ComposeDialog("Layer Style", body, ButtonRow(cancelButton, applyButton));
		}

		private void OnStrokeColorTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			m_strokeColor = main.ForegroundColor();
			m_strokeSwatch.BackgroundColor = ToMauiColor(m_strokeColor);
		}

		private void OnShadowColorTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			m_shadowColor = main.ForegroundColor();
			m_shadowSwatch.BackgroundColor = ToMauiColor(m_shadowColor);
		}

		private void OnGlowColorTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			m_glowColor = main.ForegroundColor();
			m_glowSwatch.BackgroundColor = ToMauiColor(m_glowColor);
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
			LayerStyle style = new LayerStyle();
			style.m_hasStroke = m_strokeEnable.IsChecked;
			style.m_strokeSize = m_strokeSize.Value();
			int position = m_strokePosition.SelectedIndex;
			if (position < 0)
			{
				position = 2;
			}
			style.m_strokePosition = position;
			style.m_strokeColor = m_strokeColor;
			style.m_hasDropShadow = m_shadowEnable.IsChecked;
			style.m_shadowColor = m_shadowColor;
			style.m_shadowOpacity = m_shadowOpacity.Value();
			style.m_shadowAngle = m_shadowAngle.Value();
			style.m_shadowDistance = m_shadowDistance.Value();
			style.m_shadowSize = m_shadowSize.Value();
			style.m_hasOuterGlow = m_glowEnable.IsChecked;
			style.m_glowColor = m_glowColor;
			style.m_glowOpacity = m_glowOpacity.Value();
			style.m_glowSize = m_glowSize.Value();
			main.ApplyLayerStyle(style);
		}
	}
}
