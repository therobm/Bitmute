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
		private enum eStyleEffect
		{
			Stroke,
			Shadow,
			Glow
		}

		private LayerStyle m_style;
		private eStyleEffect m_selected;
		private eStyleEffect m_colorTarget;

		private CheckBox m_strokeEnable;
		private CheckBox m_shadowEnable;
		private CheckBox m_glowEnable;
		private Label m_strokeName;
		private Label m_shadowName;
		private Label m_glowName;

		private SliderField m_strokeSize;
		private Picker m_strokePosition;
		private Border m_strokeSwatch;
		private Border m_shadowSwatch;
		private SliderField m_shadowOpacity;
		private SliderField m_shadowAngle;
		private SliderField m_shadowDistance;
		private SliderField m_shadowSize;
		private Border m_glowSwatch;
		private SliderField m_glowOpacity;
		private SliderField m_glowSize;

		private ContentView m_detailHost;
		private View m_strokeDetail;
		private View m_shadowDetail;
		private View m_glowDetail;

		private static Color ToMauiColor(SKColor color)
		{
			return Color.FromRgba((int)color.Red, (int)color.Green, (int)color.Blue, (int)color.Alpha);
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

		private View BuildEffectRow(CheckBox enable, Label name, EventHandler<TappedEventArgs> selectHandler)
		{
			enable.VerticalOptions = LayoutOptions.Center;
			name.FontSize = 13.0;
			name.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			name.VerticalOptions = LayoutOptions.Center;
			TapGestureRecognizer recognizer = new TapGestureRecognizer();
			recognizer.Tapped += selectHandler;
			name.GestureRecognizers.Add(recognizer);
			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = 8.0;
			row.Add(enable);
			row.Add(name);
			return row;
		}

		public LayerStyleDialog(LayerStyle style)
		{
			m_style = style;
			m_selected = eStyleEffect.Stroke;

			m_strokeEnable = new CheckBox();
			m_strokeEnable.IsChecked = m_style.m_hasStroke;
			m_strokeEnable.CheckedChanged += OnStrokeEnableChanged;
			m_strokeName = new Label();
			m_strokeName.Text = "Stroke";

			m_shadowEnable = new CheckBox();
			m_shadowEnable.IsChecked = m_style.m_hasDropShadow;
			m_shadowEnable.CheckedChanged += OnShadowEnableChanged;
			m_shadowName = new Label();
			m_shadowName.Text = "Drop Shadow";

			m_glowEnable = new CheckBox();
			m_glowEnable.IsChecked = m_style.m_hasOuterGlow;
			m_glowEnable.CheckedChanged += OnGlowEnableChanged;
			m_glowName = new Label();
			m_glowName.Text = "Outer Glow";

			m_strokeSize = new SliderField(1, 100, m_style.m_strokeSize, " px", OnStrokeSizeChanged);
			m_strokeSize.VerticalOptions = LayoutOptions.Center;
			m_strokePosition = new Picker();
			m_strokePosition.FontSize = 12.0;
			m_strokePosition.WidthRequest = 130.0;
			m_strokePosition.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_strokePosition.VerticalOptions = LayoutOptions.Center;
			m_strokePosition.Items.Add("Inside");
			m_strokePosition.Items.Add("Center");
			m_strokePosition.Items.Add("Outside");
			m_strokePosition.SelectedIndex = m_style.m_strokePosition;
			m_strokePosition.SelectedIndexChanged += OnStrokePositionChanged;
			m_strokeSwatch = BuildSwatch(m_style.m_strokeColor, OnStrokeColorTapped);

			m_shadowSwatch = BuildSwatch(m_style.m_shadowColor, OnShadowColorTapped);
			m_shadowOpacity = new SliderField(0, 100, m_style.m_shadowOpacity, "%", OnShadowOpacityChanged);
			m_shadowOpacity.VerticalOptions = LayoutOptions.Center;
			m_shadowAngle = new SliderField(0, 360, m_style.m_shadowAngle, "°", OnShadowAngleChanged);
			m_shadowAngle.VerticalOptions = LayoutOptions.Center;
			m_shadowDistance = new SliderField(0, 100, m_style.m_shadowDistance, " px", OnShadowDistanceChanged);
			m_shadowDistance.VerticalOptions = LayoutOptions.Center;
			m_shadowSize = new SliderField(0, 100, m_style.m_shadowSize, " px", OnShadowSizeChanged);
			m_shadowSize.VerticalOptions = LayoutOptions.Center;

			m_glowSwatch = BuildSwatch(m_style.m_glowColor, OnGlowColorTapped);
			m_glowOpacity = new SliderField(0, 100, m_style.m_glowOpacity, "%", OnGlowOpacityChanged);
			m_glowOpacity.VerticalOptions = LayoutOptions.Center;
			m_glowSize = new SliderField(0, 100, m_style.m_glowSize, " px", OnGlowSizeChanged);
			m_glowSize.VerticalOptions = LayoutOptions.Center;

			VerticalStackLayout effectList = new VerticalStackLayout();
			effectList.Spacing = 10.0;
			effectList.WidthRequest = 160.0;
			effectList.Add(BuildEffectRow(m_strokeEnable, m_strokeName, OnSelectStroke));
			effectList.Add(BuildEffectRow(m_shadowEnable, m_shadowName, OnSelectShadow));
			effectList.Add(BuildEffectRow(m_glowEnable, m_glowName, OnSelectGlow));

			m_detailHost = new ContentView();
			m_detailHost.WidthRequest = 300.0;
			m_strokeDetail = BuildStrokeDetail();
			m_shadowDetail = BuildShadowDetail();
			m_glowDetail = BuildGlowDetail();
			m_detailHost.Content = m_strokeDetail;

			HorizontalStackLayout body = new HorizontalStackLayout();
			body.Spacing = 16.0;
			body.Add(effectList);
			body.Add(m_detailHost);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("OK", OnApplyClicked);
			ComposeDialog("Layer Style", body, ButtonRow(cancelButton, applyButton));
		}

		private View BuildStrokeDetail()
		{
			VerticalStackLayout panel = new VerticalStackLayout();
			panel.Spacing = 8.0;
			panel.Add(LabeledRow("Size", m_strokeSize));
			panel.Add(LabeledRow("Position", m_strokePosition));
			panel.Add(LabeledRow("Color", m_strokeSwatch));
			return panel;
		}

		private View BuildShadowDetail()
		{
			VerticalStackLayout panel = new VerticalStackLayout();
			panel.Spacing = 8.0;
			panel.Add(LabeledRow("Color", m_shadowSwatch));
			panel.Add(LabeledRow("Opacity", m_shadowOpacity));
			panel.Add(LabeledRow("Angle", m_shadowAngle));
			panel.Add(LabeledRow("Distance", m_shadowDistance));
			panel.Add(LabeledRow("Size", m_shadowSize));
			return panel;
		}

		private View BuildGlowDetail()
		{
			VerticalStackLayout panel = new VerticalStackLayout();
			panel.Spacing = 8.0;
			panel.Add(LabeledRow("Color", m_glowSwatch));
			panel.Add(LabeledRow("Opacity", m_glowOpacity));
			panel.Add(LabeledRow("Size", m_glowSize));
			return panel;
		}

		private void ShowDetailForSelected()
		{
			if (m_selected == eStyleEffect.Stroke)
			{
				m_detailHost.Content = m_strokeDetail;
				return;
			}
			if (m_selected == eStyleEffect.Shadow)
			{
				m_detailHost.Content = m_shadowDetail;
				return;
			}
			m_detailHost.Content = m_glowDetail;
		}

		private void Preview()
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.PreviewLayerStyle(m_style);
			}
		}

		private void OnSelectStroke(object sender, TappedEventArgs eventArgs)
		{
			m_selected = eStyleEffect.Stroke;
			ShowDetailForSelected();
		}

		private void OnSelectShadow(object sender, TappedEventArgs eventArgs)
		{
			m_selected = eStyleEffect.Shadow;
			ShowDetailForSelected();
		}

		private void OnSelectGlow(object sender, TappedEventArgs eventArgs)
		{
			m_selected = eStyleEffect.Glow;
			ShowDetailForSelected();
		}

		private void OnStrokeEnableChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			m_style.m_hasStroke = m_strokeEnable.IsChecked;
			Preview();
		}

		private void OnShadowEnableChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			m_style.m_hasDropShadow = m_shadowEnable.IsChecked;
			Preview();
		}

		private void OnGlowEnableChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			m_style.m_hasOuterGlow = m_glowEnable.IsChecked;
			Preview();
		}

		private void OnStrokeSizeChanged(int value)
		{
			m_style.m_strokeSize = value;
			Preview();
		}

		private void OnStrokePositionChanged(object sender, EventArgs eventArgs)
		{
			int position = m_strokePosition.SelectedIndex;
			if (position < 0)
			{
				position = 2;
			}
			m_style.m_strokePosition = position;
			Preview();
		}

		private void OnShadowOpacityChanged(int value)
		{
			m_style.m_shadowOpacity = value;
			Preview();
		}

		private void OnShadowAngleChanged(int value)
		{
			m_style.m_shadowAngle = value;
			Preview();
		}

		private void OnShadowDistanceChanged(int value)
		{
			m_style.m_shadowDistance = value;
			Preview();
		}

		private void OnShadowSizeChanged(int value)
		{
			m_style.m_shadowSize = value;
			Preview();
		}

		private void OnGlowOpacityChanged(int value)
		{
			m_style.m_glowOpacity = value;
			Preview();
		}

		private void OnGlowSizeChanged(int value)
		{
			m_style.m_glowSize = value;
			Preview();
		}

		private void OnStrokeColorTapped(object sender, TappedEventArgs eventArgs)
		{
			m_colorTarget = eStyleEffect.Stroke;
			OpenColorPicker(m_style.m_strokeColor);
		}

		private void OnShadowColorTapped(object sender, TappedEventArgs eventArgs)
		{
			m_colorTarget = eStyleEffect.Shadow;
			OpenColorPicker(m_style.m_shadowColor);
		}

		private void OnGlowColorTapped(object sender, TappedEventArgs eventArgs)
		{
			m_colorTarget = eStyleEffect.Glow;
			OpenColorPicker(m_style.m_glowColor);
		}

		private void OpenColorPicker(SKColor current)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.OpenColorPickerFor(current, OnColorPicked);
		}

		private void OnColorPicked(SKColor color)
		{
			if (m_colorTarget == eStyleEffect.Stroke)
			{
				m_style.m_strokeColor = color;
				m_strokeSwatch.BackgroundColor = ToMauiColor(color);
			}
			else if (m_colorTarget == eStyleEffect.Shadow)
			{
				m_style.m_shadowColor = color;
				m_shadowSwatch.BackgroundColor = ToMauiColor(color);
			}
			else
			{
				m_style.m_glowColor = color;
				m_glowSwatch.BackgroundColor = ToMauiColor(color);
			}
			Preview();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CancelLayerStyle();
			}
		}

		private void OnApplyClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CommitLayerStyle(m_style);
			}
		}
	}
}
