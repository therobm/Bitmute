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
			Glow,
			InnerGlow,
			Bevel
		}

		private LayerStyle m_style;
		private eStyleEffect m_selected;
		private eStyleEffect m_colorTarget;
		private bool m_bevelColorIsShadow;

		private CheckBox m_strokeEnable;
		private CheckBox m_shadowEnable;
		private CheckBox m_glowEnable;
		private CheckBox m_innerGlowEnable;
		private CheckBox m_bevelEnable;
		private Label m_strokeName;
		private Label m_shadowName;
		private Label m_glowName;
		private Label m_innerGlowName;
		private Label m_bevelName;

		private SliderField m_strokeSize;
		private Picker m_strokePosition;
		private Border m_strokeSwatch;
		private SliderField m_strokeOpacity;
		private Picker m_strokeMode;
		private Border m_shadowSwatch;
		private SliderField m_shadowOpacity;
		private SliderField m_shadowAngle;
		private SliderField m_shadowDistance;
		private SliderField m_shadowSize;
		private SliderField m_shadowSpread;
		private Picker m_shadowMode;
		private Border m_glowSwatch;
		private SliderField m_glowOpacity;
		private SliderField m_glowSize;
		private SliderField m_glowSpread;
		private Picker m_glowMode;
		private Border m_innerGlowSwatch;
		private SliderField m_innerGlowOpacity;
		private SliderField m_innerGlowSize;
		private SliderField m_innerGlowSpread;
		private Picker m_innerGlowMode;
		private SliderField m_bevelDepth;
		private SliderField m_bevelSize;
		private SliderField m_bevelAngle;
		private Border m_bevelHighlightSwatch;
		private SliderField m_bevelHighlightOpacity;
		private Border m_bevelShadowSwatch;
		private SliderField m_bevelShadowOpacity;
		private Picker m_bevelMode;

		private ContentView m_detailHost;
		private View m_strokeDetail;
		private View m_shadowDetail;
		private View m_glowDetail;
		private View m_innerGlowDetail;
		private View m_bevelDetail;

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

		private static void AddBlendModeItems(Picker picker)
		{
			picker.Items.Add("Normal");
			picker.Items.Add("Dissolve");
			picker.Items.Add("Darken");
			picker.Items.Add("Multiply");
			picker.Items.Add("Color Burn");
			picker.Items.Add("Linear Burn");
			picker.Items.Add("Darker Color");
			picker.Items.Add("Lighten");
			picker.Items.Add("Screen");
			picker.Items.Add("Color Dodge");
			picker.Items.Add("Linear Dodge (Add)");
			picker.Items.Add("Lighter Color");
			picker.Items.Add("Overlay");
			picker.Items.Add("Soft Light");
			picker.Items.Add("Hard Light");
			picker.Items.Add("Vivid Light");
			picker.Items.Add("Linear Light");
			picker.Items.Add("Pin Light");
			picker.Items.Add("Hard Mix");
			picker.Items.Add("Difference");
			picker.Items.Add("Exclusion");
			picker.Items.Add("Subtract");
			picker.Items.Add("Divide");
			picker.Items.Add("Hue");
			picker.Items.Add("Saturation");
			picker.Items.Add("Color");
			picker.Items.Add("Luminosity");
		}

		private static eBlendMode BlendModeFromIndex(int index)
		{
			if (index < (int)eBlendMode.Normal || index > (int)eBlendMode.Luminosity)
			{
				return eBlendMode.Normal;
			}
			return (eBlendMode)index;
		}

		private Picker BuildModePicker(eBlendMode mode, EventHandler handler)
		{
			Picker picker = new Picker();
			picker.FontSize = 12.0;
			picker.WidthRequest = 130.0;
			picker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			picker.VerticalOptions = LayoutOptions.Center;
			AddBlendModeItems(picker);
			picker.SelectedIndex = (int)mode;
			picker.SelectedIndexChanged += handler;
			return picker;
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
			m_bevelColorIsShadow = false;

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

			m_innerGlowEnable = new CheckBox();
			m_innerGlowEnable.IsChecked = m_style.m_hasInnerGlow;
			m_innerGlowEnable.CheckedChanged += OnInnerGlowEnableChanged;
			m_innerGlowName = new Label();
			m_innerGlowName.Text = "Inner Glow";

			m_bevelEnable = new CheckBox();
			m_bevelEnable.IsChecked = m_style.m_hasBevel;
			m_bevelEnable.CheckedChanged += OnBevelEnableChanged;
			m_bevelName = new Label();
			m_bevelName.Text = "Bevel & Emboss";

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
			m_strokeOpacity = new SliderField(0, 100, m_style.m_strokeOpacity, "%", OnStrokeOpacityChanged);
			m_strokeOpacity.VerticalOptions = LayoutOptions.Center;
			m_strokeMode = BuildModePicker(m_style.m_strokeBlendMode, OnStrokeModeChanged);

			m_shadowSwatch = BuildSwatch(m_style.m_shadowColor, OnShadowColorTapped);
			m_shadowOpacity = new SliderField(0, 100, m_style.m_shadowOpacity, "%", OnShadowOpacityChanged);
			m_shadowOpacity.VerticalOptions = LayoutOptions.Center;
			m_shadowAngle = new SliderField(0, 360, m_style.m_shadowAngle, "°", OnShadowAngleChanged);
			m_shadowAngle.VerticalOptions = LayoutOptions.Center;
			m_shadowDistance = new SliderField(0, 100, m_style.m_shadowDistance, " px", OnShadowDistanceChanged);
			m_shadowDistance.VerticalOptions = LayoutOptions.Center;
			m_shadowSize = new SliderField(0, 100, m_style.m_shadowSize, " px", OnShadowSizeChanged);
			m_shadowSize.VerticalOptions = LayoutOptions.Center;
			m_shadowSpread = new SliderField(0, 100, m_style.m_shadowSpread, "%", OnShadowSpreadChanged);
			m_shadowSpread.VerticalOptions = LayoutOptions.Center;
			m_shadowMode = BuildModePicker(m_style.m_shadowBlendMode, OnShadowModeChanged);

			m_glowSwatch = BuildSwatch(m_style.m_glowColor, OnGlowColorTapped);
			m_glowOpacity = new SliderField(0, 100, m_style.m_glowOpacity, "%", OnGlowOpacityChanged);
			m_glowOpacity.VerticalOptions = LayoutOptions.Center;
			m_glowSize = new SliderField(0, 100, m_style.m_glowSize, " px", OnGlowSizeChanged);
			m_glowSize.VerticalOptions = LayoutOptions.Center;
			m_glowSpread = new SliderField(0, 100, m_style.m_glowSpread, "%", OnGlowSpreadChanged);
			m_glowSpread.VerticalOptions = LayoutOptions.Center;
			m_glowMode = BuildModePicker(m_style.m_glowBlendMode, OnGlowModeChanged);

			m_innerGlowSwatch = BuildSwatch(m_style.m_innerGlowColor, OnInnerGlowColorTapped);
			m_innerGlowOpacity = new SliderField(0, 100, m_style.m_innerGlowOpacity, "%", OnInnerGlowOpacityChanged);
			m_innerGlowOpacity.VerticalOptions = LayoutOptions.Center;
			m_innerGlowSize = new SliderField(0, 100, m_style.m_innerGlowSize, " px", OnInnerGlowSizeChanged);
			m_innerGlowSize.VerticalOptions = LayoutOptions.Center;
			m_innerGlowSpread = new SliderField(0, 100, m_style.m_innerGlowSpread, "%", OnInnerGlowSpreadChanged);
			m_innerGlowSpread.VerticalOptions = LayoutOptions.Center;
			m_innerGlowMode = BuildModePicker(m_style.m_innerGlowBlendMode, OnInnerGlowModeChanged);

			m_bevelDepth = new SliderField(1, 100, m_style.m_bevelDepth, "%", OnBevelDepthChanged);
			m_bevelDepth.VerticalOptions = LayoutOptions.Center;
			m_bevelSize = new SliderField(0, 100, m_style.m_bevelSize, " px", OnBevelSizeChanged);
			m_bevelSize.VerticalOptions = LayoutOptions.Center;
			m_bevelAngle = new SliderField(0, 360, m_style.m_bevelAngle, "°", OnBevelAngleChanged);
			m_bevelAngle.VerticalOptions = LayoutOptions.Center;
			m_bevelHighlightSwatch = BuildSwatch(m_style.m_bevelHighlightColor, OnBevelHighlightColorTapped);
			m_bevelHighlightOpacity = new SliderField(0, 100, m_style.m_bevelHighlightOpacity, "%", OnBevelHighlightOpacityChanged);
			m_bevelHighlightOpacity.VerticalOptions = LayoutOptions.Center;
			m_bevelShadowSwatch = BuildSwatch(m_style.m_bevelShadowColor, OnBevelShadowColorTapped);
			m_bevelShadowOpacity = new SliderField(0, 100, m_style.m_bevelShadowOpacity, "%", OnBevelShadowOpacityChanged);
			m_bevelShadowOpacity.VerticalOptions = LayoutOptions.Center;
			m_bevelMode = BuildModePicker(m_style.m_bevelBlendMode, OnBevelModeChanged);

			VerticalStackLayout effectList = new VerticalStackLayout();
			effectList.Spacing = 10.0;
			effectList.WidthRequest = 160.0;
			effectList.Add(BuildEffectRow(m_strokeEnable, m_strokeName, OnSelectStroke));
			effectList.Add(BuildEffectRow(m_shadowEnable, m_shadowName, OnSelectShadow));
			effectList.Add(BuildEffectRow(m_glowEnable, m_glowName, OnSelectGlow));
			effectList.Add(BuildEffectRow(m_innerGlowEnable, m_innerGlowName, OnSelectInnerGlow));
			effectList.Add(BuildEffectRow(m_bevelEnable, m_bevelName, OnSelectBevel));

			m_detailHost = new ContentView();
			m_detailHost.WidthRequest = 300.0;
			m_strokeDetail = BuildStrokeDetail();
			m_shadowDetail = BuildShadowDetail();
			m_glowDetail = BuildGlowDetail();
			m_innerGlowDetail = BuildInnerGlowDetail();
			m_bevelDetail = BuildBevelDetail();
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
			panel.Add(LabeledRow("Opacity", m_strokeOpacity));
			panel.Add(LabeledRow("Mode", m_strokeMode));
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
			panel.Add(LabeledRow("Spread", m_shadowSpread));
			panel.Add(LabeledRow("Mode", m_shadowMode));
			return panel;
		}

		private View BuildGlowDetail()
		{
			VerticalStackLayout panel = new VerticalStackLayout();
			panel.Spacing = 8.0;
			panel.Add(LabeledRow("Color", m_glowSwatch));
			panel.Add(LabeledRow("Opacity", m_glowOpacity));
			panel.Add(LabeledRow("Size", m_glowSize));
			panel.Add(LabeledRow("Spread", m_glowSpread));
			panel.Add(LabeledRow("Mode", m_glowMode));
			return panel;
		}

		private View BuildInnerGlowDetail()
		{
			VerticalStackLayout panel = new VerticalStackLayout();
			panel.Spacing = 8.0;
			panel.Add(LabeledRow("Color", m_innerGlowSwatch));
			panel.Add(LabeledRow("Opacity", m_innerGlowOpacity));
			panel.Add(LabeledRow("Size", m_innerGlowSize));
			panel.Add(LabeledRow("Spread", m_innerGlowSpread));
			panel.Add(LabeledRow("Mode", m_innerGlowMode));
			return panel;
		}

		private View BuildBevelDetail()
		{
			VerticalStackLayout panel = new VerticalStackLayout();
			panel.Spacing = 8.0;
			panel.Add(LabeledRow("Depth", m_bevelDepth));
			panel.Add(LabeledRow("Size", m_bevelSize));
			panel.Add(LabeledRow("Angle", m_bevelAngle));
			panel.Add(LabeledRow("Highlight", m_bevelHighlightSwatch));
			panel.Add(LabeledRow("Hi Opacity", m_bevelHighlightOpacity));
			panel.Add(LabeledRow("Shadow", m_bevelShadowSwatch));
			panel.Add(LabeledRow("Sh Opacity", m_bevelShadowOpacity));
			panel.Add(LabeledRow("Mode", m_bevelMode));
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
			if (m_selected == eStyleEffect.Glow)
			{
				m_detailHost.Content = m_glowDetail;
				return;
			}
			if (m_selected == eStyleEffect.InnerGlow)
			{
				m_detailHost.Content = m_innerGlowDetail;
				return;
			}
			m_detailHost.Content = m_bevelDetail;
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

		private void OnSelectInnerGlow(object sender, TappedEventArgs eventArgs)
		{
			m_selected = eStyleEffect.InnerGlow;
			ShowDetailForSelected();
		}

		private void OnSelectBevel(object sender, TappedEventArgs eventArgs)
		{
			m_selected = eStyleEffect.Bevel;
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

		private void OnInnerGlowEnableChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			m_style.m_hasInnerGlow = m_innerGlowEnable.IsChecked;
			Preview();
		}

		private void OnBevelEnableChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			m_style.m_hasBevel = m_bevelEnable.IsChecked;
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

		private void OnStrokeOpacityChanged(int value)
		{
			m_style.m_strokeOpacity = value;
			Preview();
		}

		private void OnStrokeModeChanged(object sender, EventArgs eventArgs)
		{
			m_style.m_strokeBlendMode = BlendModeFromIndex(m_strokeMode.SelectedIndex);
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

		private void OnShadowSpreadChanged(int value)
		{
			m_style.m_shadowSpread = value;
			Preview();
		}

		private void OnShadowModeChanged(object sender, EventArgs eventArgs)
		{
			m_style.m_shadowBlendMode = BlendModeFromIndex(m_shadowMode.SelectedIndex);
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

		private void OnGlowSpreadChanged(int value)
		{
			m_style.m_glowSpread = value;
			Preview();
		}

		private void OnGlowModeChanged(object sender, EventArgs eventArgs)
		{
			m_style.m_glowBlendMode = BlendModeFromIndex(m_glowMode.SelectedIndex);
			Preview();
		}

		private void OnInnerGlowOpacityChanged(int value)
		{
			m_style.m_innerGlowOpacity = value;
			Preview();
		}

		private void OnInnerGlowSizeChanged(int value)
		{
			m_style.m_innerGlowSize = value;
			Preview();
		}

		private void OnInnerGlowSpreadChanged(int value)
		{
			m_style.m_innerGlowSpread = value;
			Preview();
		}

		private void OnInnerGlowModeChanged(object sender, EventArgs eventArgs)
		{
			m_style.m_innerGlowBlendMode = BlendModeFromIndex(m_innerGlowMode.SelectedIndex);
			Preview();
		}

		private void OnBevelDepthChanged(int value)
		{
			m_style.m_bevelDepth = value;
			Preview();
		}

		private void OnBevelSizeChanged(int value)
		{
			m_style.m_bevelSize = value;
			Preview();
		}

		private void OnBevelAngleChanged(int value)
		{
			m_style.m_bevelAngle = value;
			Preview();
		}

		private void OnBevelHighlightOpacityChanged(int value)
		{
			m_style.m_bevelHighlightOpacity = value;
			Preview();
		}

		private void OnBevelShadowOpacityChanged(int value)
		{
			m_style.m_bevelShadowOpacity = value;
			Preview();
		}

		private void OnBevelModeChanged(object sender, EventArgs eventArgs)
		{
			m_style.m_bevelBlendMode = BlendModeFromIndex(m_bevelMode.SelectedIndex);
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

		private void OnInnerGlowColorTapped(object sender, TappedEventArgs eventArgs)
		{
			m_colorTarget = eStyleEffect.InnerGlow;
			OpenColorPicker(m_style.m_innerGlowColor);
		}

		private void OnBevelHighlightColorTapped(object sender, TappedEventArgs eventArgs)
		{
			m_colorTarget = eStyleEffect.Bevel;
			m_bevelColorIsShadow = false;
			OpenColorPicker(m_style.m_bevelHighlightColor);
		}

		private void OnBevelShadowColorTapped(object sender, TappedEventArgs eventArgs)
		{
			m_colorTarget = eStyleEffect.Bevel;
			m_bevelColorIsShadow = true;
			OpenColorPicker(m_style.m_bevelShadowColor);
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
			else if (m_colorTarget == eStyleEffect.Glow)
			{
				m_style.m_glowColor = color;
				m_glowSwatch.BackgroundColor = ToMauiColor(color);
			}
			else if (m_colorTarget == eStyleEffect.InnerGlow)
			{
				m_style.m_innerGlowColor = color;
				m_innerGlowSwatch.BackgroundColor = ToMauiColor(color);
			}
			else if (m_bevelColorIsShadow)
			{
				m_style.m_bevelShadowColor = color;
				m_bevelShadowSwatch.BackgroundColor = ToMauiColor(color);
			}
			else
			{
				m_style.m_bevelHighlightColor = color;
				m_bevelHighlightSwatch.BackgroundColor = ToMauiColor(color);
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
