using System.Collections.Generic;
using Bitmute.Imaging;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Bitmute.UI
{
	public class OptionsBar
	{
		private MainView m_main;
		private ToolState m_toolState;
		private View m_root;
		private Label m_optionsToolLabel;
		private Label m_brushSizeLabel;
		private SliderField m_brushSizeField;
		private Label m_brushHardnessLabel;
		private SliderField m_brushHardnessField;
		private Label m_brushOpacityLabel;
		private SliderField m_brushOpacityField;
		private Label m_brushFlowLabel;
		private SliderField m_brushFlowField;
		private Label m_brushSmoothingLabel;
		private SliderField m_brushSmoothingField;
		private Label m_brushStrengthLabel;
		private SliderField m_brushStrengthField;
		private Button m_brushSettingsButton;
		private HorizontalStackLayout m_optionsRow;
		private Picker m_brushTipPicker;
		private Slider m_brushSpacingSlider;
		private Label m_brushSpacingValue;
		private Slider m_brushFadeSlider;
		private Entry m_brushFadeValue;
		private bool m_updatingFade;
		private Button m_customTipButton;
		private List<View> m_customTipRows;
		private List<CustomBrush> m_customTipBrushes;
		private double m_brushSettingsAnchorX;
		private double m_brushSettingsAnchorY;
		private Slider m_brushRoundnessSlider;
		private Label m_brushRoundnessValue;
		private Slider m_brushAngleSlider;
		private Label m_brushAngleValue;
		private BrushTipEditor m_brushTipEditor;
		private Label m_brushModeLabel;
		private Picker m_brushModePicker;
		private Label m_brushAirbrushLabel;
		private CheckBox m_brushAirbrushCheck;
		private Label m_pressureSizeLabel;
		private CheckBox m_pressureSizeCheck;
		private Label m_pressureOpacityLabel;
		private CheckBox m_pressureOpacityCheck;
		private Label m_cloneAlignedLabel;
		private CheckBox m_cloneAlignedCheck;
		private Label m_spongeModeLabel;
		private Picker m_spongeModePicker;
		private Label m_colorReplaceModeLabel;
		private Picker m_colorReplaceModePicker;
		private Label m_colorReplaceToleranceLabel;
		private SliderField m_colorReplaceToleranceField;
		private Label m_dodgeBurnRangeLabel;
		private Picker m_dodgeBurnRangePicker;
		private Label m_dodgeBurnExposureLabel;
		private SliderField m_dodgeBurnExposureField;
		private Label m_gradientTypeLabel;
		private Button m_gradientTypeButton;
		private string[] m_gradientTypeNames;
		private List<View> m_gradientTypeRows;
		private Label m_gradientReverseLabel;
		private CheckBox m_gradientReverseCheck;
		private Label m_gradientTransparentLabel;
		private CheckBox m_gradientTransparentCheck;
		private Label m_fillContentLabel;
		private Picker m_fillContentPicker;
		private Label m_fillPatternLabel;
		private Button m_fillPatternButton;
		private List<View> m_fillPatternRows;
		private List<Pattern> m_fillPatternItems;
		private Label m_lineAntiAliasLabel;
		private CheckBox m_lineAntiAliasCheck;
		private Button m_selectModeNewButton;
		private Button m_selectModeAddButton;
		private Button m_selectModeSubtractButton;
		private Button m_selectModeIntersectButton;
		private Label m_selectionFeatherLabel;
		private SliderField m_selectionFeatherField;
		private Label m_selectionAntiAliasLabel;
		private CheckBox m_selectionAntiAliasCheck;
		private Label m_toleranceLabel;
		private SliderField m_toleranceField;
		private Label m_wandAntiAliasLabel;
		private CheckBox m_wandAntiAliasCheck;
		private Label m_wandContiguousLabel;
		private CheckBox m_wandContiguousCheck;
		private Label m_wandSampleAllLabel;
		private CheckBox m_wandSampleAllCheck;
		private Label m_magneticWidthLabel;
		private SliderField m_magneticWidthField;
		private Label m_magneticContrastLabel;
		private SliderField m_magneticContrastField;
		private Label m_textFontLabel;
		private Button m_textFontButton;
		private string[] m_fontFamilies;
		private Label m_textSizeLabel;
		private SliderField m_textSizeField;
		private Label m_textStyleLabel;
		private Button m_textStyleButton;
		private Label m_textAlignLabel;
		private Picker m_textAlignPicker;
		private Label m_textAntiAliasLabel;
		private Picker m_textAntiAliasPicker;
		private Label m_textColorLabel;
		private BoxView m_textColorSwatch;
		private Button m_textCharButton;
		private CheckBox m_charLeadingAutoCheck;
		private SliderField m_charLeadingField;
		private SliderField m_charTrackingField;
		private SliderField m_charHScaleField;
		private SliderField m_charVScaleField;
		private SliderField m_charBaselineField;
		private CheckBox m_charFauxBoldCheck;
		private CheckBox m_charFauxItalicCheck;
		private CheckBox m_charKerningAutoCheck;

		private static string StyleName(bool bold, bool italic)
		{
			if (bold && italic)
			{
				return "Bold Italic";
			}
			if (bold)
			{
				return "Bold";
			}
			if (italic)
			{
				return "Italic";
			}
			return "Regular";
		}

		private void StyleSelectionModeButton(Button button, bool active)
		{
			if (button == null)
			{
				return;
			}
			if (active)
			{
				button.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			}
			else
			{
				button.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			}
		}

		private void RefreshSelectionModeButtons()
		{
			if (m_toolState == null)
			{
				return;
			}
			int mode = m_toolState.SelectionMode();
			StyleSelectionModeButton(m_selectModeNewButton, mode == 0);
			StyleSelectionModeButton(m_selectModeAddButton, mode == 1);
			StyleSelectionModeButton(m_selectModeSubtractButton, mode == 2);
			StyleSelectionModeButton(m_selectModeIntersectButton, mode == 3);
		}

		private void OnSelectModeNewClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetSelectionMode(0);
			RefreshSelectionModeButtons();
		}

		private void OnSelectModeAddClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetSelectionMode(1);
			RefreshSelectionModeButtons();
		}

		private void OnSelectModeSubtractClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetSelectionMode(2);
			RefreshSelectionModeButtons();
		}

		private void OnSelectModeIntersectClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetSelectionMode(3);
			RefreshSelectionModeButtons();
		}

		private void OnSelectionFeatherValue(int feather)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetSelectionFeather(feather);
		}

		private void OnSelectionAntiAliasChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetSelectionAntiAlias(m_selectionAntiAliasCheck.IsChecked);
		}

		private void OnToleranceValue(int tolerance)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetFillTolerance(tolerance);
		}

		private void OnWandAntiAliasChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetWandAntiAlias(m_wandAntiAliasCheck.IsChecked);
		}

		private void OnWandContiguousChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetWandContiguous(m_wandContiguousCheck.IsChecked);
		}

		private void OnWandSampleAllChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetWandSampleAll(m_wandSampleAllCheck.IsChecked);
		}

		private void OnMagneticWidthValue(int width)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetMagneticWidth(width);
		}

		private void OnMagneticContrastValue(int contrast)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetMagneticContrast(contrast);
		}

		private void OnBrushSizeValue(int size)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSize(size);
		}

		private void OnBrushHardnessValue(int hardness)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushHardness(hardness);
		}

		private void OnBrushOpacityValue(int opacity)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushOpacity(opacity);
		}

		private void OnBrushFlowValue(int flow)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushFlow(flow);
		}

		private void OnBrushSmoothingValue(int smoothing)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSmoothing(smoothing);
		}

		private void OnBrushStrengthValue(int strength)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushStrength(strength);
		}

		private void OnBrushModeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_brushModePicker.SelectedIndex;
			if (index < 0)
			{
				index = 0;
			}
			m_toolState.SetBrushMode((Bitmute.Imaging.eBlendMode)index);
		}

		private void OnSpongeModeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetSpongeSaturate(m_spongeModePicker.SelectedIndex == 1);
		}

		private void OnColorReplaceModeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_colorReplaceModePicker.SelectedIndex;
			if (index < 0)
			{
				index = 0;
			}
			m_toolState.SetColorReplaceMode(index);
		}

		private void OnColorReplaceToleranceValue(int tolerance)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetColorReplaceTolerance(tolerance);
		}

		private void OnDodgeBurnRangeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_dodgeBurnRangePicker.SelectedIndex;
			if (index < 0)
			{
				index = 0;
			}
			m_toolState.SetDodgeBurnRange(index);
		}

		private void OnDodgeBurnExposureValue(int exposure)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetDodgeBurnExposure(exposure);
		}

		private void UpdateGradientTypeButtonText()
		{
			if (m_gradientTypeButton == null || m_toolState == null)
			{
				return;
			}
			int index = m_toolState.GradientType();
			if (index < 0 || index >= m_gradientTypeNames.Length)
			{
				index = 0;
			}
			m_gradientTypeButton.Text = m_gradientTypeNames[index];
		}

		private Microsoft.Maui.Controls.ImageSource RenderGradientSwatch(int type)
		{
			int width = 120;
			int height = 16;
			SkiaSharp.SKBitmap swatch = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
			Bitmute.Imaging.eGradientType gradientType = (Bitmute.Imaging.eGradientType)type;
			float startX = 0.0f;
			float startY = height / 2.0f;
			float endX = width;
			float endY = height / 2.0f;
			if (gradientType == Bitmute.Imaging.eGradientType.Reflected)
			{
				endX = width / 2.0f;
			}
			else if (gradientType != Bitmute.Imaging.eGradientType.Linear)
			{
				startX = width / 2.0f;
			}
			SkiaSharp.SKColor startColor = m_toolState.Foreground();
			SkiaSharp.SKColor endColor = m_toolState.Background();
			if (m_toolState.GradientToTransparent())
			{
				endColor = new SkiaSharp.SKColor(startColor.Red, startColor.Green, startColor.Blue, 0);
			}
			Bitmute.Imaging.GradientFill.Fill(swatch, gradientType, startX, startY, endX, endY, startColor, endColor, m_toolState.GradientReverse());
			SkiaSharp.Views.Maui.Controls.SKBitmapImageSource source = new SkiaSharp.Views.Maui.Controls.SKBitmapImageSource();
			source.Bitmap = swatch;
			return source;
		}

		private void OnGradientTypeButtonClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_main.PulldownOpen() || m_main.PulldownJustDismissed())
			{
				m_main.ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_gradientTypeButton != null)
			{
				anchorX = m_optionsRow.X + m_gradientTypeButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			m_main.ShowPulldown(BuildGradientTypePulldownContent(), anchorX, anchorY, 190.0, 130.0);
		}

		private View BuildGradientTypePulldownContent()
		{
			m_gradientTypeRows = new List<View>();
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 2.0;
			list.Padding = new Thickness(4.0);
			for (int index = 0; index < m_gradientTypeNames.Length; index++)
			{
				HorizontalStackLayout row = new HorizontalStackLayout();
				row.Spacing = 6.0;
				row.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
				Image swatch = new Image();
				swatch.Source = RenderGradientSwatch(index);
				swatch.WidthRequest = 120.0;
				swatch.HeightRequest = 16.0;
				swatch.VerticalOptions = LayoutOptions.Center;
				Label name = new Label();
				name.Text = m_gradientTypeNames[index];
				name.FontSize = UiConstants.ComponentFontSize;
				name.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				name.VerticalOptions = LayoutOptions.Center;
				row.Add(swatch);
				row.Add(name);
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnGradientTypeRowTapped;
				row.GestureRecognizers.Add(tap);
				m_gradientTypeRows.Add(row);
				list.Add(row);
			}
			return list;
		}

		private void OnGradientTypeRowTapped(object sender, TappedEventArgs eventArgs)
		{
			if (m_toolState == null || m_gradientTypeRows == null)
			{
				return;
			}
			for (int index = 0; index < m_gradientTypeRows.Count; index++)
			{
				if (ReferenceEquals(m_gradientTypeRows[index], sender))
				{
					m_toolState.SetGradientType(index);
					UpdateGradientTypeButtonText();
					m_main.ClosePulldown();
					return;
				}
			}
		}

		private void OnFillContentChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null || m_fillContentPicker == null)
			{
				return;
			}
			if (m_fillContentPicker.SelectedIndex == 1)
			{
				m_toolState.SetFillContent(eFillContent.Pattern);
			}
			else
			{
				m_toolState.SetFillContent(eFillContent.Foreground);
			}
		}

		private Microsoft.Maui.Controls.ImageSource RenderPatternSwatch(Pattern pattern)
		{
			SkiaSharp.Views.Maui.Controls.SKBitmapImageSource source = new SkiaSharp.Views.Maui.Controls.SKBitmapImageSource();
			source.Bitmap = pattern.m_bitmap;
			return source;
		}

		private void OnPatternButtonClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_main.PulldownOpen() || m_main.PulldownJustDismissed())
			{
				m_main.ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_fillPatternButton != null)
			{
				anchorX = m_optionsRow.X + m_fillPatternButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			m_main.ShowPulldown(BuildPatternPulldownContent(), anchorX, anchorY, 190.0, 200.0);
		}

		private View BuildPatternPulldownContent()
		{
			m_fillPatternRows = new List<View>();
			m_fillPatternItems = new List<Pattern>();
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 2.0;
			list.Padding = new Thickness(4.0);
			List<Pattern> patterns = MainView.Self.Patterns();
			if (patterns.Count == 0)
			{
				Label empty = new Label();
				empty.Text = "No patterns — use Edit ▸ Define Pattern";
				empty.FontSize = UiConstants.ComponentFontSize;
				empty.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
				empty.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
				empty.VerticalOptions = LayoutOptions.Center;
				list.Add(empty);
				return list;
			}
			for (int index = 0; index < patterns.Count; index++)
			{
				Pattern pattern = patterns[index];
				HorizontalStackLayout row = new HorizontalStackLayout();
				row.Spacing = 6.0;
				row.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
				Image swatch = new Image();
				swatch.Source = RenderPatternSwatch(pattern);
				swatch.WidthRequest = 32.0;
				swatch.HeightRequest = 32.0;
				swatch.VerticalOptions = LayoutOptions.Center;
				Label name = new Label();
				name.Text = pattern.m_name;
				name.FontSize = UiConstants.ComponentFontSize;
				name.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				name.VerticalOptions = LayoutOptions.Center;
				row.Add(swatch);
				row.Add(name);
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnPatternRowTapped;
				row.GestureRecognizers.Add(tap);
				m_fillPatternRows.Add(row);
				m_fillPatternItems.Add(pattern);
				list.Add(row);
			}
			return list;
		}

		private void OnPatternRowTapped(object sender, TappedEventArgs eventArgs)
		{
			if (m_toolState == null || m_fillPatternRows == null)
			{
				return;
			}
			for (int index = 0; index < m_fillPatternRows.Count; index++)
			{
				if (ReferenceEquals(m_fillPatternRows[index], sender))
				{
					m_toolState.SetActivePattern(m_fillPatternItems[index]);
					m_toolState.SetFillContent(eFillContent.Pattern);
					if (m_fillContentPicker != null)
					{
						m_fillContentPicker.SelectedIndex = 1;
					}
					m_main.ClosePulldown();
					return;
				}
			}
		}

		private void OnGradientReverseChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetGradientReverse(m_gradientReverseCheck.IsChecked);
		}

		private void OnGradientTransparentChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetGradientToTransparent(m_gradientTransparentCheck.IsChecked);
		}

		private void OnBrushAirbrushChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetAirbrush(m_brushAirbrushCheck.IsChecked);
		}

		private void OnPressureSizeChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetPressureSizeEnabled(m_pressureSizeCheck.IsChecked);
			Microsoft.Maui.Storage.Preferences.Default.Set("pressure_size_enabled", m_pressureSizeCheck.IsChecked);
		}

		private void OnPressureOpacityChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetPressureOpacityEnabled(m_pressureOpacityCheck.IsChecked);
			Microsoft.Maui.Storage.Preferences.Default.Set("pressure_opacity_enabled", m_pressureOpacityCheck.IsChecked);
		}

		private void OnCloneAlignedChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetCloneAligned(m_cloneAlignedCheck.IsChecked);
		}

		private void OnBrushSettingsClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			if (m_main.PulldownOpen() || m_main.PulldownJustDismissed())
			{
				m_main.ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_brushSettingsButton != null)
			{
				anchorX = m_optionsRow.X + m_brushSettingsButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			m_brushSettingsAnchorX = anchorX;
			m_brushSettingsAnchorY = anchorY;
			m_main.ShowPulldown(BuildBrushSettingsContent(), anchorX, anchorY, 288.0, 320.0);
		}

		public void OpenBrushSettingsAt(double anchorX, double anchorY)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_brushSettingsAnchorX = anchorX;
			m_brushSettingsAnchorY = anchorY;
			m_main.ShowPulldown(BuildBrushSettingsContent(), anchorX, anchorY, 288.0, 320.0);
		}

		private View BuildBrushSettingsContent()
		{
			Label tipLabel = new Label();
			tipLabel.Text = "Tip";
			tipLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			tipLabel.FontSize = UiConstants.ComponentFontSize;
			tipLabel.WidthRequest = 60.0;
			tipLabel.VerticalOptions = LayoutOptions.Center;

			m_brushTipPicker = new Picker();
			m_brushTipPicker.FontSize = UiConstants.ComponentFontSize;
			m_brushTipPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_brushTipPicker.Items.Add("Round");
			m_brushTipPicker.Items.Add("Square");
			m_brushTipPicker.SelectedIndex = 0;
			if (m_toolState.BrushSquareTip())
			{
				m_brushTipPicker.SelectedIndex = 1;
			}
			m_brushTipPicker.SelectedIndexChanged += OnBrushTipPulldownChanged;

			Grid tipRow = new Grid();
			tipRow.ColumnSpacing = 8.0;
			tipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			tipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(tipLabel, 0);
			Grid.SetColumn(m_brushTipPicker, 1);
			tipRow.Add(tipLabel);
			tipRow.Add(m_brushTipPicker);

			Label spacingLabel = new Label();
			spacingLabel.Text = "Spacing";
			spacingLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			spacingLabel.FontSize = UiConstants.ComponentFontSize;
			spacingLabel.WidthRequest = 60.0;
			spacingLabel.VerticalOptions = LayoutOptions.Center;

			m_brushSpacingSlider = new Slider();
			m_brushSpacingSlider.Minimum = 1.0;
			m_brushSpacingSlider.Maximum = 100.0;
			m_brushSpacingSlider.WidthRequest = 140.0;
			m_brushSpacingSlider.VerticalOptions = LayoutOptions.Center;
			m_brushSpacingSlider.Value = m_toolState.BrushSpacing();
			m_brushSpacingSlider.ValueChanged += OnBrushSpacingPulldownChanged;

			m_brushSpacingValue = new Label();
			m_brushSpacingValue.Text = m_toolState.BrushSpacing() + "%";
			m_brushSpacingValue.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushSpacingValue.FontSize = UiConstants.ComponentFontSize;
			m_brushSpacingValue.WidthRequest = 44.0;
			m_brushSpacingValue.VerticalOptions = LayoutOptions.Center;

			Grid spacingRow = new Grid();
			spacingRow.ColumnSpacing = 8.0;
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(spacingLabel, 0);
			Grid.SetColumn(m_brushSpacingSlider, 1);
			Grid.SetColumn(m_brushSpacingValue, 2);
			spacingRow.Add(spacingLabel);
			spacingRow.Add(m_brushSpacingSlider);
			spacingRow.Add(m_brushSpacingValue);

			Label fadeLabel = new Label();
			fadeLabel.Text = "Fade";
			fadeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			fadeLabel.FontSize = UiConstants.ComponentFontSize;
			fadeLabel.WidthRequest = 60.0;
			fadeLabel.VerticalOptions = LayoutOptions.Center;

			m_brushFadeSlider = new Slider();
			m_brushFadeSlider.Minimum = 0.0;
			m_brushFadeSlider.Maximum = 1000.0;
			m_brushFadeSlider.WidthRequest = 140.0;
			m_brushFadeSlider.VerticalOptions = LayoutOptions.Center;
			m_brushFadeSlider.Value = m_toolState.FadeLength();
			m_brushFadeSlider.ValueChanged += OnBrushFadePulldownChanged;

			m_brushFadeValue = new Entry();
			m_brushFadeValue.Text = FadeValueText(m_toolState.FadeLength());
			m_brushFadeValue.Keyboard = Keyboard.Numeric;
			m_brushFadeValue.HorizontalTextAlignment = TextAlignment.End;
			m_brushFadeValue.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_brushFadeValue.FontSize = UiConstants.ComponentFontSize;
			m_brushFadeValue.WidthRequest = 60.0;
			m_brushFadeValue.HeightRequest = UiConstants.ComponentHeight;
			m_brushFadeValue.VerticalOptions = LayoutOptions.Center;
			m_brushFadeValue.Completed += OnBrushFadeEntryCommitted;
			m_brushFadeValue.Unfocused += OnBrushFadeEntryUnfocused;

			Grid fadeRow = new Grid();
			fadeRow.ColumnSpacing = 8.0;
			fadeRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			fadeRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			fadeRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(fadeLabel, 0);
			Grid.SetColumn(m_brushFadeSlider, 1);
			Grid.SetColumn(m_brushFadeValue, 2);
			fadeRow.Add(fadeLabel);
			fadeRow.Add(m_brushFadeSlider);
			fadeRow.Add(m_brushFadeValue);

			Label customTipLabel = new Label();
			customTipLabel.Text = "Tip:";
			customTipLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			customTipLabel.FontSize = UiConstants.ComponentFontSize;
			customTipLabel.WidthRequest = 60.0;
			customTipLabel.VerticalOptions = LayoutOptions.Center;

			m_customTipButton = new Button();
			m_customTipButton.Text = "Tip";
			m_customTipButton.FontSize = UiConstants.ComponentFontSize;
			m_customTipButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_customTipButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_customTipButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_customTipButton.VerticalOptions = LayoutOptions.Center;
			m_customTipButton.Clicked += OnCustomTipButtonClicked;

			Grid customTipRow = new Grid();
			customTipRow.ColumnSpacing = 8.0;
			customTipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			customTipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(customTipLabel, 0);
			Grid.SetColumn(m_customTipButton, 1);
			customTipRow.Add(customTipLabel);
			customTipRow.Add(m_customTipButton);

			Label roundnessLabel = new Label();
			roundnessLabel.Text = "Roundness";
			roundnessLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			roundnessLabel.FontSize = UiConstants.ComponentFontSize;
			roundnessLabel.WidthRequest = 60.0;
			roundnessLabel.VerticalOptions = LayoutOptions.Center;

			m_brushRoundnessSlider = new Slider();
			m_brushRoundnessSlider.Minimum = 5.0;
			m_brushRoundnessSlider.Maximum = 100.0;
			m_brushRoundnessSlider.WidthRequest = 140.0;
			m_brushRoundnessSlider.VerticalOptions = LayoutOptions.Center;
			m_brushRoundnessSlider.Value = m_toolState.BrushRoundness();
			m_brushRoundnessSlider.ValueChanged += OnBrushRoundnessPulldownChanged;

			m_brushRoundnessValue = new Label();
			m_brushRoundnessValue.Text = m_toolState.BrushRoundness() + "%";
			m_brushRoundnessValue.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushRoundnessValue.FontSize = UiConstants.ComponentFontSize;
			m_brushRoundnessValue.WidthRequest = 44.0;
			m_brushRoundnessValue.VerticalOptions = LayoutOptions.Center;

			Grid roundnessRow = new Grid();
			roundnessRow.ColumnSpacing = 8.0;
			roundnessRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			roundnessRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			roundnessRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(roundnessLabel, 0);
			Grid.SetColumn(m_brushRoundnessSlider, 1);
			Grid.SetColumn(m_brushRoundnessValue, 2);
			roundnessRow.Add(roundnessLabel);
			roundnessRow.Add(m_brushRoundnessSlider);
			roundnessRow.Add(m_brushRoundnessValue);

			Label angleLabel = new Label();
			angleLabel.Text = "Angle";
			angleLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			angleLabel.FontSize = UiConstants.ComponentFontSize;
			angleLabel.WidthRequest = 60.0;
			angleLabel.VerticalOptions = LayoutOptions.Center;

			m_brushAngleSlider = new Slider();
			m_brushAngleSlider.Minimum = 0.0;
			m_brushAngleSlider.Maximum = 180.0;
			m_brushAngleSlider.WidthRequest = 140.0;
			m_brushAngleSlider.VerticalOptions = LayoutOptions.Center;
			m_brushAngleSlider.Value = m_toolState.BrushAngle();
			m_brushAngleSlider.ValueChanged += OnBrushAnglePulldownChanged;

			m_brushAngleValue = new Label();
			m_brushAngleValue.Text = m_toolState.BrushAngle() + "°";
			m_brushAngleValue.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushAngleValue.FontSize = UiConstants.ComponentFontSize;
			m_brushAngleValue.WidthRequest = 44.0;
			m_brushAngleValue.VerticalOptions = LayoutOptions.Center;

			Grid angleRow = new Grid();
			angleRow.ColumnSpacing = 8.0;
			angleRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			angleRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			angleRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(angleLabel, 0);
			Grid.SetColumn(m_brushAngleSlider, 1);
			Grid.SetColumn(m_brushAngleValue, 2);
			angleRow.Add(angleLabel);
			angleRow.Add(m_brushAngleSlider);
			angleRow.Add(m_brushAngleValue);

			m_brushTipEditor = new BrushTipEditor(m_toolState, OnBrushTipEditorChanged);
			m_brushTipEditor.WidthRequest = 110.0;
			m_brushTipEditor.HeightRequest = 110.0;
			m_brushTipEditor.HorizontalOptions = LayoutOptions.Center;

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 10.0;
			body.Padding = new Thickness(12.0);
			body.Add(tipRow);
			body.Add(spacingRow);
			body.Add(fadeRow);
			body.Add(customTipRow);
			body.Add(roundnessRow);
			body.Add(angleRow);
			body.Add(m_brushTipEditor);
			return body;
		}

		private void OnBrushTipEditorChanged(int roundness, int angle)
		{
			if (m_brushRoundnessSlider != null)
			{
				m_brushRoundnessSlider.Value = roundness;
			}
			if (m_brushAngleSlider != null)
			{
				m_brushAngleSlider.Value = angle;
			}
		}

		private void OnBrushRoundnessPulldownChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_brushRoundnessSlider == null || m_toolState == null)
			{
				return;
			}
			int roundness = (int)m_brushRoundnessSlider.Value;
			m_toolState.SetBrushRoundness(roundness);
			if (m_brushRoundnessValue != null)
			{
				m_brushRoundnessValue.Text = roundness + "%";
			}
			if (m_brushTipEditor != null)
			{
				m_brushTipEditor.RefreshPreview();
			}
		}

		private void OnBrushAnglePulldownChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_brushAngleSlider == null || m_toolState == null)
			{
				return;
			}
			int angle = (int)m_brushAngleSlider.Value;
			m_toolState.SetBrushAngle(angle);
			if (m_brushAngleValue != null)
			{
				m_brushAngleValue.Text = angle + "°";
			}
			if (m_brushTipEditor != null)
			{
				m_brushTipEditor.RefreshPreview();
			}
		}

		private void OnBrushTipPulldownChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_brushTipPicker == null)
			{
				return;
			}
			ApplyBrushTip(m_brushTipPicker.SelectedIndex == 1);
		}

		private void OnBrushSpacingPulldownChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_brushSpacingSlider == null)
			{
				return;
			}
			int spacing = (int)m_brushSpacingSlider.Value;
			ApplyBrushSpacing(spacing);
			if (m_brushSpacingValue != null)
			{
				m_brushSpacingValue.Text = spacing + "%";
			}
		}

		private string FadeValueText(int fade)
		{
			if (fade <= 0)
			{
				return "Off";
			}
			return fade + " px";
		}

		private void OnBrushFadePulldownChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_updatingFade || m_brushFadeSlider == null || m_toolState == null)
			{
				return;
			}
			int fade = (int)System.Math.Round(m_brushFadeSlider.Value);
			m_toolState.SetBrushFadeLength(fade);
			if (m_brushFadeValue != null)
			{
				m_brushFadeValue.Text = FadeValueText(fade);
			}
		}

		private void OnBrushFadeEntryCommitted(object sender, System.EventArgs eventArgs)
		{
			CommitBrushFadeEntry();
		}

		private void OnBrushFadeEntryUnfocused(object sender, FocusEventArgs eventArgs)
		{
			CommitBrushFadeEntry();
		}

		private void CommitBrushFadeEntry()
		{
			if (m_updatingFade || m_brushFadeSlider == null || m_toolState == null || m_brushFadeValue == null)
			{
				return;
			}
			// The slider tops out at its Maximum, but a typed fade is unrestricted (only floored at 0),
			// so store the raw value and just pin the slider thumb to its range.
			int fade = ExtractFadeInt(m_brushFadeValue.Text);
			m_toolState.SetBrushFadeLength(fade);
			m_updatingFade = true;
			double thumb = fade;
			if (thumb > m_brushFadeSlider.Maximum)
			{
				thumb = m_brushFadeSlider.Maximum;
			}
			m_brushFadeSlider.Value = thumb;
			m_brushFadeValue.Text = FadeValueText(fade);
			m_updatingFade = false;
		}

		private int ExtractFadeInt(string text)
		{
			if (text == null)
			{
				return 0;
			}
			string digits = "";
			for (int index = 0; index < text.Length; index++)
			{
				char character = text[index];
				if (character >= '0' && character <= '9')
				{
					digits = digits + character;
				}
				else if (digits.Length > 0)
				{
					break;
				}
			}
			if (digits.Length == 0)
			{
				return 0;
			}
			int result = 0;
			if (int.TryParse(digits, out result))
			{
				return result;
			}
			return 0;
		}

		private void OnCustomTipButtonClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			if (m_main.PulldownJustDismissed())
			{
				m_main.ClosePulldown();
				return;
			}
			double anchorX = m_brushSettingsAnchorX + 60.0;
			double anchorY = m_brushSettingsAnchorY;
			m_main.ShowPulldown(BuildCustomTipPulldownContent(), anchorX, anchorY, 190.0, 200.0);
		}

		private View BuildCustomTipPulldownContent()
		{
			m_customTipRows = new List<View>();
			m_customTipBrushes = new List<CustomBrush>();
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 2.0;
			list.Padding = new Thickness(4.0);

			HorizontalStackLayout noneRow = new HorizontalStackLayout();
			noneRow.Spacing = 6.0;
			noneRow.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
			Label noneName = new Label();
			noneName.Text = "None (round/square)";
			noneName.FontSize = UiConstants.ComponentFontSize;
			noneName.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			noneName.VerticalOptions = LayoutOptions.Center;
			noneRow.Add(noneName);
			TapGestureRecognizer noneTap = new TapGestureRecognizer();
			noneTap.Tapped += OnCustomTipRowTapped;
			noneRow.GestureRecognizers.Add(noneTap);
			m_customTipRows.Add(noneRow);
			m_customTipBrushes.Add(null);
			list.Add(noneRow);

			List<CustomBrush> brushes = MainView.Self.CustomBrushes();
			for (int index = 0; index < brushes.Count; index++)
			{
				CustomBrush customBrush = brushes[index];
				HorizontalStackLayout row = new HorizontalStackLayout();
				row.Spacing = 6.0;
				row.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
				Image swatch = new Image();
				SkiaSharp.Views.Maui.Controls.SKBitmapImageSource source = new SkiaSharp.Views.Maui.Controls.SKBitmapImageSource();
				source.Bitmap = customBrush.m_tip;
				swatch.Source = source;
				swatch.WidthRequest = 16.0;
				swatch.HeightRequest = 16.0;
				swatch.VerticalOptions = LayoutOptions.Center;
				Label name = new Label();
				name.Text = customBrush.m_name;
				name.FontSize = UiConstants.ComponentFontSize;
				name.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				name.VerticalOptions = LayoutOptions.Center;
				row.Add(swatch);
				row.Add(name);
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnCustomTipRowTapped;
				row.GestureRecognizers.Add(tap);
				m_customTipRows.Add(row);
				m_customTipBrushes.Add(customBrush);
				list.Add(row);
			}
			return list;
		}

		private void OnCustomTipRowTapped(object sender, TappedEventArgs eventArgs)
		{
			if (m_toolState == null || m_customTipRows == null)
			{
				return;
			}
			for (int index = 0; index < m_customTipRows.Count; index++)
			{
				if (ReferenceEquals(m_customTipRows[index], sender))
				{
					CustomBrush customBrush = m_customTipBrushes[index];
					if (customBrush == null)
					{
						m_toolState.SetActiveCustomTip(null);
					}
					else
					{
						m_toolState.SetActiveCustomTip(customBrush.m_tip);
					}
					m_main.ClosePulldown();
					return;
				}
			}
		}

		private void OnLineAntiAliasChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetLineAntiAlias(m_lineAntiAliasCheck.IsChecked);
		}

		private void UpdateFontButtonText()
		{
			if (m_textFontButton == null || m_toolState == null)
			{
				return;
			}
			m_textFontButton.Text = m_toolState.TextFontFamily();
			m_textFontButton.FontFamily = m_toolState.TextFontFamily();
		}

		private void OnFontButtonClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_main.PulldownOpen() || m_main.PulldownJustDismissed())
			{
				m_main.ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_textFontButton != null)
			{
				anchorX = m_optionsRow.X + m_textFontButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			m_main.ShowPulldown(BuildFontPulldownContent(), anchorX, anchorY, 240.0, 320.0);
		}

		private View BuildFontPulldownContent()
		{
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(4.0);
			for (int index = 0; index < m_fontFamilies.Length; index++)
			{
				string family = m_fontFamilies[index];
				Label row = new Label();
				row.Text = family;
				row.FontFamily = family;
				row.FontSize = 15.0;
				row.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
				row.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnFontRowTapped;
				row.GestureRecognizers.Add(tap);
				list.Add(row);
			}
			ScrollView scroll = new ScrollView();
			scroll.Content = list;
			return scroll;
		}

		private void OnFontRowTapped(object sender, TappedEventArgs eventArgs)
		{
			Label row = sender as Label;
			if (row == null || m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextFontFamily(row.Text);
			UpdateFontButtonText();
			m_main.ClosePulldown();
			m_main.RefreshTextEditStyle();
		}

		private void OnTextSizeValue(int size)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextSize(size);
			m_main.RefreshTextEditStyle();
		}

		private void UpdateStyleButtonText()
		{
			if (m_textStyleButton == null || m_toolState == null)
			{
				return;
			}
			m_textStyleButton.Text = StyleName(m_toolState.TextBold(), m_toolState.TextItalic());
		}

		private void OnStyleButtonClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_main.PulldownOpen() || m_main.PulldownJustDismissed())
			{
				m_main.ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_textStyleButton != null)
			{
				anchorX = m_optionsRow.X + m_textStyleButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			m_main.ShowPulldown(BuildStylePulldownContent(), anchorX, anchorY, 150.0, 140.0);
		}

		private View BuildStylePulldownContent()
		{
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(4.0);
			list.Add(BuildStyleRow("Regular", false, false));
			list.Add(BuildStyleRow("Bold", true, false));
			list.Add(BuildStyleRow("Italic", false, true));
			list.Add(BuildStyleRow("Bold Italic", true, true));
			return list;
		}

		private Label BuildStyleRow(string label, bool bold, bool italic)
		{
			Label row = new Label();
			row.Text = label;
			row.FontSize = 13.0;
			row.Padding = new Thickness(8.0, 5.0, 8.0, 5.0);
			row.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			FontAttributes attributes = FontAttributes.None;
			if (bold)
			{
				attributes = attributes | FontAttributes.Bold;
			}
			if (italic)
			{
				attributes = attributes | FontAttributes.Italic;
			}
			row.FontAttributes = attributes;
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnStyleRowTapped;
			row.GestureRecognizers.Add(tap);
			return row;
		}

		private void OnStyleRowTapped(object sender, TappedEventArgs eventArgs)
		{
			Label row = sender as Label;
			if (row == null || m_toolState == null)
			{
				return;
			}
			bool bold = row.Text == "Bold" || row.Text == "Bold Italic";
			bool italic = row.Text == "Italic" || row.Text == "Bold Italic";
			m_toolState.SetTextBold(bold);
			m_toolState.SetTextItalic(italic);
			UpdateStyleButtonText();
			m_main.ClosePulldown();
			m_main.RefreshTextEditStyle();
		}

		private void OnTextAlignChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_textAlignPicker.SelectedIndex;
			if (index < 0)
			{
				return;
			}
			m_toolState.SetTextAlign(index);
			m_main.RefreshTextEditStyle();
		}

		private void OnTextAntiAliasChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_textAntiAliasPicker.SelectedIndex;
			if (index < 0)
			{
				return;
			}
			m_toolState.SetTextAntiAlias(index);
			m_main.RefreshTextEditStyle();
		}

		private void OnTextColorTapped(object sender, TappedEventArgs eventArgs)
		{
			m_main.OpenColorPicker(true);
		}

		private void OnTextCharClicked(object sender, System.EventArgs eventArgs)
		{
			m_main.ShowModal(BuildCharacterPanelContent(), 268.0, 320.0);
		}

		private int LeadingSliderValue()
		{
			float leading = m_toolState.TextLeading();
			if (m_toolState.TextLeadingAuto() || leading < 1.0f)
			{
				leading = m_toolState.TextSize() * 1.25f;
			}
			return (int)leading;
		}

		private Grid BuildCharRow(string labelText, View control)
		{
			Label label = new Label();
			label.Text = labelText;
			label.FontSize = UiConstants.ComponentFontSize;
			label.WidthRequest = 96.0;
			label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			label.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = 6.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(label, 0);
			Grid.SetColumn(control, 1);
			control.HorizontalOptions = LayoutOptions.End;
			row.Add(label);
			row.Add(control);
			return row;
		}

		private View BuildCharacterPanelContent()
		{
			m_charLeadingField = new SliderField(0, 400, LeadingSliderValue(), " px", OnCharLeadingValue);
			m_charLeadingField.VerticalOptions = LayoutOptions.Center;

			m_charLeadingAutoCheck = new CheckBox();
			m_charLeadingAutoCheck.IsChecked = m_toolState.TextLeadingAuto();
			m_charLeadingAutoCheck.VerticalOptions = LayoutOptions.Center;
			m_charLeadingAutoCheck.HorizontalOptions = LayoutOptions.End;
			m_charLeadingAutoCheck.CheckedChanged += OnCharLeadingAutoChanged;

			m_charTrackingField = new SliderField(-50, 200, m_toolState.TextTracking(), "", OnCharTrackingValue);
			m_charTrackingField.VerticalOptions = LayoutOptions.Center;

			m_charHScaleField = new SliderField(10, 400, m_toolState.TextHorizontalScale(), " %", OnCharHScaleValue);
			m_charHScaleField.VerticalOptions = LayoutOptions.Center;

			m_charVScaleField = new SliderField(10, 400, m_toolState.TextVerticalScale(), " %", OnCharVScaleValue);
			m_charVScaleField.VerticalOptions = LayoutOptions.Center;

			m_charBaselineField = new SliderField(-100, 100, m_toolState.TextBaselineShift(), " px", OnCharBaselineValue);
			m_charBaselineField.VerticalOptions = LayoutOptions.Center;

			m_charFauxBoldCheck = new CheckBox();
			m_charFauxBoldCheck.IsChecked = m_toolState.TextFauxBold();
			m_charFauxBoldCheck.VerticalOptions = LayoutOptions.Center;
			m_charFauxBoldCheck.HorizontalOptions = LayoutOptions.End;
			m_charFauxBoldCheck.CheckedChanged += OnCharFauxBoldChanged;

			m_charFauxItalicCheck = new CheckBox();
			m_charFauxItalicCheck.IsChecked = m_toolState.TextFauxItalic();
			m_charFauxItalicCheck.VerticalOptions = LayoutOptions.Center;
			m_charFauxItalicCheck.HorizontalOptions = LayoutOptions.End;
			m_charFauxItalicCheck.CheckedChanged += OnCharFauxItalicChanged;

			m_charKerningAutoCheck = new CheckBox();
			m_charKerningAutoCheck.IsChecked = m_toolState.TextKerningAuto();
			m_charKerningAutoCheck.VerticalOptions = LayoutOptions.Center;
			m_charKerningAutoCheck.HorizontalOptions = LayoutOptions.End;
			m_charKerningAutoCheck.CheckedChanged += OnCharKerningChanged;

			Label title = new Label();
			title.Text = "Character";
			title.FontSize = UiConstants.ComponentFontSize;
			title.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			title.VerticalOptions = LayoutOptions.Center;
			title.HorizontalOptions = LayoutOptions.Start;

			Label close = new Label();
			close.Text = "✕";
			close.FontSize = UiConstants.ComponentFontSize;
			close.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			close.VerticalOptions = LayoutOptions.Center;
			close.HorizontalOptions = LayoutOptions.End;
			TapGestureRecognizer closeTap = new TapGestureRecognizer();
			closeTap.Tapped += OnCharPanelClose;
			close.GestureRecognizers.Add(closeTap);

			Grid titleGrid = new Grid();
			titleGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			titleGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(title, 0);
			Grid.SetColumn(close, 1);
			titleGrid.Add(title);
			titleGrid.Add(close);

			Border titleBar = new Border();
			titleBar.Padding = new Thickness(10.0, 5.0, 8.0, 5.0);
			titleBar.StrokeThickness = 0.0;
			titleBar.ThemeBg(UiConstants.TitleBarLight, UiConstants.TitleBarDark);
			titleBar.Content = titleGrid;
			PanGestureRecognizer titlePan = new PanGestureRecognizer();
			titlePan.PanUpdated += OnCharPanelPan;
			titleBar.GestureRecognizers.Add(titlePan);

			VerticalStackLayout rows = new VerticalStackLayout();
			rows.Spacing = 6.0;
			rows.Padding = new Thickness(12.0, 10.0, 12.0, 12.0);
			rows.Add(BuildCharRow("Leading", m_charLeadingField));
			rows.Add(BuildCharRow("Auto leading", m_charLeadingAutoCheck));
			rows.Add(BuildCharRow("Tracking", m_charTrackingField));
			rows.Add(BuildCharRow("Horiz Scale", m_charHScaleField));
			rows.Add(BuildCharRow("Vert Scale", m_charVScaleField));
			rows.Add(BuildCharRow("Baseline", m_charBaselineField));
			rows.Add(BuildCharRow("Faux Bold", m_charFauxBoldCheck));
			rows.Add(BuildCharRow("Faux Italic", m_charFauxItalicCheck));
			rows.Add(BuildCharRow("Kerning (Auto)", m_charKerningAutoCheck));

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 0.0;
			body.Add(titleBar);
			body.Add(rows);

			Border panel = new Border();
			panel.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			panel.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			panel.StrokeThickness = 1.0;
			panel.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			panel.Content = body;
			return panel;
		}

		private void OnCharPanelPan(object sender, PanUpdatedEventArgs eventArgs)
		{
			m_main.DragModal(eventArgs.StatusType, eventArgs.TotalX, eventArgs.TotalY);
		}

		private void OnCharPanelClose(object sender, TappedEventArgs eventArgs)
		{
			m_main.CloseModal();
		}

		private void OnCharLeadingAutoChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null || m_charLeadingAutoCheck == null)
			{
				return;
			}
			m_toolState.SetTextLeadingAuto(m_charLeadingAutoCheck.IsChecked);
			m_main.RefreshTextEditStyle();
		}

		private void OnCharLeadingValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextLeading(value);
			m_toolState.SetTextLeadingAuto(false);
			if (m_charLeadingAutoCheck != null)
			{
				m_charLeadingAutoCheck.IsChecked = false;
			}
			m_main.RefreshTextEditStyle();
		}

		private void OnCharTrackingValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextTracking(value);
			m_main.RefreshTextEditStyle();
		}

		private void OnCharHScaleValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextHorizontalScale(value);
			m_main.RefreshTextEditStyle();
		}

		private void OnCharVScaleValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextVerticalScale(value);
			m_main.RefreshTextEditStyle();
		}

		private void OnCharBaselineValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextBaselineShift(value);
			m_main.RefreshTextEditStyle();
		}

		private void OnCharFauxBoldChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null || m_charFauxBoldCheck == null)
			{
				return;
			}
			m_toolState.SetTextFauxBold(m_charFauxBoldCheck.IsChecked);
			m_main.RefreshTextEditStyle();
		}

		private void OnCharFauxItalicChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null || m_charFauxItalicCheck == null)
			{
				return;
			}
			m_toolState.SetTextFauxItalic(m_charFauxItalicCheck.IsChecked);
			m_main.RefreshTextEditStyle();
		}

		private void OnCharKerningChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null || m_charKerningAutoCheck == null)
			{
				return;
			}
			m_toolState.SetTextKerningAuto(m_charKerningAutoCheck.IsChecked);
			m_main.RefreshTextEditStyle();
		}

		public OptionsBar(MainView main, ToolState toolState)
		{
			m_main = main;
			m_toolState = toolState;

			Grid bar = new Grid();
			bar.HeightRequest = UiConstants.OptionsBarHeight;
			bar.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			bar.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			bar.ColumnSpacing = 16.0;
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			m_optionsToolLabel = new Label();
			m_optionsToolLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_optionsToolLabel.FontSize = UiConstants.ComponentFontSize;
			m_optionsToolLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_optionsToolLabel, 0);
			bar.Add(m_optionsToolLabel);

			m_brushSizeLabel = new Label();
			m_brushSizeLabel.Text = "Size";
			m_brushSizeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushSizeLabel.FontSize = UiConstants.ComponentFontSize;
			m_brushSizeLabel.VerticalOptions = LayoutOptions.Center;

			m_brushSizeField = new SliderField(1, 500, m_toolState.BrushSize(), " px", OnBrushSizeValue);
			m_brushSizeField.VerticalOptions = LayoutOptions.Center;

			m_brushHardnessLabel = new Label();
			m_brushHardnessLabel.Text = "Hardness";
			m_brushHardnessLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushHardnessLabel.FontSize = UiConstants.ComponentFontSize;
			m_brushHardnessLabel.VerticalOptions = LayoutOptions.Center;
			m_brushHardnessLabel.IsVisible = false;

			m_brushHardnessField = new SliderField(0, 100, m_toolState.BrushHardness(), "%", OnBrushHardnessValue);
			m_brushHardnessField.VerticalOptions = LayoutOptions.Center;
			m_brushHardnessField.IsVisible = false;

			m_brushOpacityLabel = new Label();
			m_brushOpacityLabel.Text = "Opacity";
			m_brushOpacityLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushOpacityLabel.FontSize = UiConstants.ComponentFontSize;
			m_brushOpacityLabel.VerticalOptions = LayoutOptions.Center;
			m_brushOpacityLabel.IsVisible = false;

			m_brushOpacityField = new SliderField(1, 100, m_toolState.BrushOpacity(), "%", OnBrushOpacityValue);
			m_brushOpacityField.VerticalOptions = LayoutOptions.Center;
			m_brushOpacityField.IsVisible = false;

			m_brushFlowLabel = new Label();
			m_brushFlowLabel.Text = "Flow";
			m_brushFlowLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushFlowLabel.FontSize = UiConstants.ComponentFontSize;
			m_brushFlowLabel.VerticalOptions = LayoutOptions.Center;
			m_brushFlowLabel.IsVisible = false;

			m_brushFlowField = new SliderField(1, 100, m_toolState.BrushFlow(), "%", OnBrushFlowValue);
			m_brushFlowField.VerticalOptions = LayoutOptions.Center;
			m_brushFlowField.IsVisible = false;

			m_brushSmoothingLabel = new Label();
			m_brushSmoothingLabel.Text = "Smoothing";
			m_brushSmoothingLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushSmoothingLabel.FontSize = UiConstants.ComponentFontSize;
			m_brushSmoothingLabel.VerticalOptions = LayoutOptions.Center;
			m_brushSmoothingLabel.IsVisible = false;

			m_brushSmoothingField = new SliderField(0, 100, m_toolState.BrushSmoothing(), "%", OnBrushSmoothingValue);
			m_brushSmoothingField.VerticalOptions = LayoutOptions.Center;
			m_brushSmoothingField.IsVisible = false;

			m_brushStrengthLabel = new Label();
			m_brushStrengthLabel.Text = "Strength";
			m_brushStrengthLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushStrengthLabel.FontSize = UiConstants.ComponentFontSize;
			m_brushStrengthLabel.VerticalOptions = LayoutOptions.Center;
			m_brushStrengthLabel.IsVisible = false;

			m_brushStrengthField = new SliderField(1, 100, m_toolState.BrushStrength(), "%", OnBrushStrengthValue);
			m_brushStrengthField.VerticalOptions = LayoutOptions.Center;
			m_brushStrengthField.IsVisible = false;

			m_brushModeLabel = new Label();
			m_brushModeLabel.Text = "Mode";
			m_brushModeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushModeLabel.FontSize = UiConstants.ComponentFontSize;
			m_brushModeLabel.VerticalOptions = LayoutOptions.Center;
			m_brushModeLabel.IsVisible = false;

			m_brushModePicker = new Picker();
			m_brushModePicker.FontSize = UiConstants.ComponentFontSize;
			m_brushModePicker.HeightRequest = UiConstants.ComponentHeight;
			m_brushModePicker.Margin = new Thickness(0);
			m_brushModePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_brushModePicker.WidthRequest = 110.0;
			m_brushModePicker.VerticalOptions = LayoutOptions.Center;
			m_brushModePicker.IsVisible = false;
			m_brushModePicker.Items.Add("Normal");
			m_brushModePicker.Items.Add("Multiply");
			m_brushModePicker.Items.Add("Screen");
			m_brushModePicker.Items.Add("Overlay");
			m_brushModePicker.Items.Add("Add");
			m_brushModePicker.SelectedIndex = 0;
			m_brushModePicker.SelectedIndexChanged += OnBrushModeChanged;

			m_brushSettingsButton = new Button();
			m_brushSettingsButton.Text = "Brush Settings";
			m_brushSettingsButton.FontSize = UiConstants.ComponentFontSize;
			m_brushSettingsButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_brushSettingsButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_brushSettingsButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushSettingsButton.VerticalOptions = LayoutOptions.Center;
			m_brushSettingsButton.IsVisible = false;
			m_brushSettingsButton.Clicked += OnBrushSettingsClicked;

			m_brushAirbrushLabel = new Label();
			m_brushAirbrushLabel.Text = "Airbrush";
			m_brushAirbrushLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushAirbrushLabel.FontSize = UiConstants.ComponentFontSize;
			m_brushAirbrushLabel.VerticalOptions = LayoutOptions.Center;
			m_brushAirbrushLabel.IsVisible = false;

			m_brushAirbrushCheck = new CheckBox();
			m_brushAirbrushCheck.VerticalOptions = LayoutOptions.Center;
			m_brushAirbrushCheck.IsVisible = false;
			m_brushAirbrushCheck.IsChecked = m_toolState.Airbrush();
			m_brushAirbrushCheck.CheckedChanged += OnBrushAirbrushChanged;

			bool pressureSizeEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("pressure_size_enabled", true);
			m_toolState.SetPressureSizeEnabled(pressureSizeEnabled);
			m_pressureSizeLabel = new Label();
			m_pressureSizeLabel.Text = "Pressure → Size";
			m_pressureSizeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_pressureSizeLabel.FontSize = UiConstants.ComponentFontSize;
			m_pressureSizeLabel.VerticalOptions = LayoutOptions.Center;
			m_pressureSizeLabel.IsVisible = false;

			m_pressureSizeCheck = new CheckBox();
			m_pressureSizeCheck.VerticalOptions = LayoutOptions.Center;
			m_pressureSizeCheck.IsVisible = false;
			m_pressureSizeCheck.IsChecked = pressureSizeEnabled;
			m_pressureSizeCheck.CheckedChanged += OnPressureSizeChanged;

			bool pressureOpacityEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("pressure_opacity_enabled", true);
			m_toolState.SetPressureOpacityEnabled(pressureOpacityEnabled);
			m_pressureOpacityLabel = new Label();
			m_pressureOpacityLabel.Text = "Pressure → Opacity";
			m_pressureOpacityLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_pressureOpacityLabel.FontSize = UiConstants.ComponentFontSize;
			m_pressureOpacityLabel.VerticalOptions = LayoutOptions.Center;
			m_pressureOpacityLabel.IsVisible = false;

			m_pressureOpacityCheck = new CheckBox();
			m_pressureOpacityCheck.VerticalOptions = LayoutOptions.Center;
			m_pressureOpacityCheck.IsVisible = false;
			m_pressureOpacityCheck.IsChecked = pressureOpacityEnabled;
			m_pressureOpacityCheck.CheckedChanged += OnPressureOpacityChanged;

			m_cloneAlignedLabel = new Label();
			m_cloneAlignedLabel.Text = "Aligned";
			m_cloneAlignedLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_cloneAlignedLabel.FontSize = UiConstants.ComponentFontSize;
			m_cloneAlignedLabel.VerticalOptions = LayoutOptions.Center;
			m_cloneAlignedLabel.IsVisible = false;

			m_cloneAlignedCheck = new CheckBox();
			m_cloneAlignedCheck.VerticalOptions = LayoutOptions.Center;
			m_cloneAlignedCheck.IsVisible = false;
			m_cloneAlignedCheck.IsChecked = m_toolState.CloneAligned();
			m_cloneAlignedCheck.CheckedChanged += OnCloneAlignedChanged;

			m_spongeModeLabel = new Label();
			m_spongeModeLabel.Text = "Mode";
			m_spongeModeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_spongeModeLabel.FontSize = UiConstants.ComponentFontSize;
			m_spongeModeLabel.VerticalOptions = LayoutOptions.Center;
			m_spongeModeLabel.IsVisible = false;

			m_spongeModePicker = new Picker();
			m_spongeModePicker.FontSize = UiConstants.ComponentFontSize;
			m_spongeModePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_spongeModePicker.WidthRequest = 110.0;
			m_spongeModePicker.VerticalOptions = LayoutOptions.Center;
			m_spongeModePicker.IsVisible = false;
			m_spongeModePicker.Items.Add("Desaturate");
			m_spongeModePicker.Items.Add("Saturate");
			m_spongeModePicker.SelectedIndex = 0;
			m_spongeModePicker.SelectedIndexChanged += OnSpongeModeChanged;

			m_colorReplaceModeLabel = new Label();
			m_colorReplaceModeLabel.Text = "Mode";
			m_colorReplaceModeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_colorReplaceModeLabel.FontSize = UiConstants.ComponentFontSize;
			m_colorReplaceModeLabel.VerticalOptions = LayoutOptions.Center;
			m_colorReplaceModeLabel.IsVisible = false;

			m_colorReplaceModePicker = new Picker();
			m_colorReplaceModePicker.FontSize = UiConstants.ComponentFontSize;
			m_colorReplaceModePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_colorReplaceModePicker.WidthRequest = 120.0;
			m_colorReplaceModePicker.VerticalOptions = LayoutOptions.Center;
			m_colorReplaceModePicker.IsVisible = false;
			m_colorReplaceModePicker.Items.Add("Color");
			m_colorReplaceModePicker.Items.Add("Hue");
			m_colorReplaceModePicker.Items.Add("Saturation");
			m_colorReplaceModePicker.Items.Add("Luminosity");
			m_colorReplaceModePicker.SelectedIndex = m_toolState.ColorReplaceMode();
			m_colorReplaceModePicker.SelectedIndexChanged += OnColorReplaceModeChanged;

			m_colorReplaceToleranceLabel = new Label();
			m_colorReplaceToleranceLabel.Text = "Tolerance";
			m_colorReplaceToleranceLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_colorReplaceToleranceLabel.FontSize = UiConstants.ComponentFontSize;
			m_colorReplaceToleranceLabel.VerticalOptions = LayoutOptions.Center;
			m_colorReplaceToleranceLabel.IsVisible = false;

			m_colorReplaceToleranceField = new SliderField(0, 255, m_toolState.ColorReplaceTolerance(), "", OnColorReplaceToleranceValue);
			m_colorReplaceToleranceField.VerticalOptions = LayoutOptions.Center;
			m_colorReplaceToleranceField.IsVisible = false;

			m_dodgeBurnRangeLabel = new Label();
			m_dodgeBurnRangeLabel.Text = "Range";
			m_dodgeBurnRangeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_dodgeBurnRangeLabel.FontSize = UiConstants.ComponentFontSize;
			m_dodgeBurnRangeLabel.VerticalOptions = LayoutOptions.Center;
			m_dodgeBurnRangeLabel.IsVisible = false;

			m_dodgeBurnRangePicker = new Picker();
			m_dodgeBurnRangePicker.FontSize = UiConstants.ComponentFontSize;
			m_dodgeBurnRangePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_dodgeBurnRangePicker.WidthRequest = 110.0;
			m_dodgeBurnRangePicker.VerticalOptions = LayoutOptions.Center;
			m_dodgeBurnRangePicker.IsVisible = false;
			m_dodgeBurnRangePicker.Items.Add("Shadows");
			m_dodgeBurnRangePicker.Items.Add("Midtones");
			m_dodgeBurnRangePicker.Items.Add("Highlights");
			m_dodgeBurnRangePicker.SelectedIndex = m_toolState.DodgeBurnRange();
			m_dodgeBurnRangePicker.SelectedIndexChanged += OnDodgeBurnRangeChanged;

			m_dodgeBurnExposureLabel = new Label();
			m_dodgeBurnExposureLabel.Text = "Exposure";
			m_dodgeBurnExposureLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_dodgeBurnExposureLabel.FontSize = UiConstants.ComponentFontSize;
			m_dodgeBurnExposureLabel.VerticalOptions = LayoutOptions.Center;
			m_dodgeBurnExposureLabel.IsVisible = false;

			m_dodgeBurnExposureField = new SliderField(1, 100, m_toolState.DodgeBurnExposure(), "%", OnDodgeBurnExposureValue);
			m_dodgeBurnExposureField.VerticalOptions = LayoutOptions.Center;
			m_dodgeBurnExposureField.IsVisible = false;

			m_gradientTypeLabel = new Label();
			m_gradientTypeLabel.Text = "Type";
			m_gradientTypeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_gradientTypeLabel.FontSize = UiConstants.ComponentFontSize;
			m_gradientTypeLabel.VerticalOptions = LayoutOptions.Center;
			m_gradientTypeLabel.IsVisible = false;

			m_gradientTypeNames = new string[] { "Linear", "Radial", "Angle", "Reflected", "Diamond" };
			m_gradientTypeButton = new Button();
			m_gradientTypeButton.FontSize = UiConstants.ComponentFontSize;
			m_gradientTypeButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_gradientTypeButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_gradientTypeButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_gradientTypeButton.WidthRequest = 110.0;
			m_gradientTypeButton.VerticalOptions = LayoutOptions.Center;
			m_gradientTypeButton.IsVisible = false;
			m_gradientTypeButton.Clicked += OnGradientTypeButtonClicked;

			m_gradientReverseLabel = new Label();
			m_gradientReverseLabel.Text = "Reverse";
			m_gradientReverseLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_gradientReverseLabel.FontSize = UiConstants.ComponentFontSize;
			m_gradientReverseLabel.VerticalOptions = LayoutOptions.Center;
			m_gradientReverseLabel.IsVisible = false;

			m_gradientReverseCheck = new CheckBox();
			m_gradientReverseCheck.VerticalOptions = LayoutOptions.Center;
			m_gradientReverseCheck.IsVisible = false;
			m_gradientReverseCheck.IsChecked = m_toolState.GradientReverse();
			m_gradientReverseCheck.CheckedChanged += OnGradientReverseChanged;

			m_gradientTransparentLabel = new Label();
			m_gradientTransparentLabel.Text = "To Transparent";
			m_gradientTransparentLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_gradientTransparentLabel.FontSize = UiConstants.ComponentFontSize;
			m_gradientTransparentLabel.VerticalOptions = LayoutOptions.Center;
			m_gradientTransparentLabel.IsVisible = false;

			m_gradientTransparentCheck = new CheckBox();
			m_gradientTransparentCheck.VerticalOptions = LayoutOptions.Center;
			m_gradientTransparentCheck.IsVisible = false;
			m_gradientTransparentCheck.IsChecked = m_toolState.GradientToTransparent();
			m_gradientTransparentCheck.CheckedChanged += OnGradientTransparentChanged;

			m_fillContentLabel = new Label();
			m_fillContentLabel.Text = "Content";
			m_fillContentLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_fillContentLabel.FontSize = UiConstants.ComponentFontSize;
			m_fillContentLabel.VerticalOptions = LayoutOptions.Center;
			m_fillContentLabel.IsVisible = false;

			m_fillContentPicker = new Picker();
			m_fillContentPicker.FontSize = UiConstants.ComponentFontSize;
			m_fillContentPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_fillContentPicker.WidthRequest = 110.0;
			m_fillContentPicker.VerticalOptions = LayoutOptions.Center;
			m_fillContentPicker.IsVisible = false;
			m_fillContentPicker.Items.Add("Color");
			m_fillContentPicker.Items.Add("Pattern");
			m_fillContentPicker.SelectedIndex = 0;
			if (m_toolState.FillContent() == eFillContent.Pattern)
			{
				m_fillContentPicker.SelectedIndex = 1;
			}
			m_fillContentPicker.SelectedIndexChanged += OnFillContentChanged;

			m_fillPatternLabel = new Label();
			m_fillPatternLabel.Text = "Pattern";
			m_fillPatternLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_fillPatternLabel.FontSize = UiConstants.ComponentFontSize;
			m_fillPatternLabel.VerticalOptions = LayoutOptions.Center;
			m_fillPatternLabel.IsVisible = false;

			m_fillPatternButton = new Button();
			m_fillPatternButton.Text = "Pattern…";
			m_fillPatternButton.FontSize = UiConstants.ComponentFontSize;
			m_fillPatternButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_fillPatternButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_fillPatternButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_fillPatternButton.WidthRequest = 110.0;
			m_fillPatternButton.VerticalOptions = LayoutOptions.Center;
			m_fillPatternButton.IsVisible = false;
			m_fillPatternButton.Clicked += OnPatternButtonClicked;

			m_lineAntiAliasLabel = new Label();
			m_lineAntiAliasLabel.Text = "Anti-alias";
			m_lineAntiAliasLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_lineAntiAliasLabel.FontSize = UiConstants.ComponentFontSize;
			m_lineAntiAliasLabel.VerticalOptions = LayoutOptions.Center;
			m_lineAntiAliasLabel.IsVisible = false;

			m_lineAntiAliasCheck = new CheckBox();
			m_lineAntiAliasCheck.VerticalOptions = LayoutOptions.Center;
			m_lineAntiAliasCheck.IsVisible = false;
			m_lineAntiAliasCheck.CheckedChanged += OnLineAntiAliasChanged;

			m_selectModeNewButton = new Button();
			m_selectModeNewButton.Text = "New";
			m_selectModeNewButton.FontSize = UiConstants.ComponentFontSize;
			m_selectModeNewButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_selectModeNewButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_selectModeNewButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_selectModeNewButton.VerticalOptions = LayoutOptions.Center;
			m_selectModeNewButton.IsVisible = false;
			m_selectModeNewButton.Clicked += OnSelectModeNewClicked;

			m_selectModeAddButton = new Button();
			m_selectModeAddButton.Text = "Add";
			m_selectModeAddButton.FontSize = UiConstants.ComponentFontSize;
			m_selectModeAddButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_selectModeAddButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_selectModeAddButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_selectModeAddButton.VerticalOptions = LayoutOptions.Center;
			m_selectModeAddButton.IsVisible = false;
			m_selectModeAddButton.Clicked += OnSelectModeAddClicked;

			m_selectModeSubtractButton = new Button();
			m_selectModeSubtractButton.Text = "Sub";
			m_selectModeSubtractButton.FontSize = UiConstants.ComponentFontSize;
			m_selectModeSubtractButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_selectModeSubtractButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_selectModeSubtractButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_selectModeSubtractButton.VerticalOptions = LayoutOptions.Center;
			m_selectModeSubtractButton.IsVisible = false;
			m_selectModeSubtractButton.Clicked += OnSelectModeSubtractClicked;

			m_selectModeIntersectButton = new Button();
			m_selectModeIntersectButton.Text = "Sect";
			m_selectModeIntersectButton.FontSize = UiConstants.ComponentFontSize;
			m_selectModeIntersectButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_selectModeIntersectButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_selectModeIntersectButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_selectModeIntersectButton.VerticalOptions = LayoutOptions.Center;
			m_selectModeIntersectButton.IsVisible = false;
			m_selectModeIntersectButton.Clicked += OnSelectModeIntersectClicked;

			RefreshSelectionModeButtons();

			m_selectionFeatherLabel = new Label();
			m_selectionFeatherLabel.Text = "Feather";
			m_selectionFeatherLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_selectionFeatherLabel.FontSize = UiConstants.ComponentFontSize;
			m_selectionFeatherLabel.VerticalOptions = LayoutOptions.Center;
			m_selectionFeatherLabel.IsVisible = false;

			m_selectionFeatherField = new SliderField(0, 100, m_toolState.SelectionFeather(), " px", OnSelectionFeatherValue);
			m_selectionFeatherField.VerticalOptions = LayoutOptions.Center;
			m_selectionFeatherField.IsVisible = false;

			m_selectionAntiAliasLabel = new Label();
			m_selectionAntiAliasLabel.Text = "Anti-alias";
			m_selectionAntiAliasLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_selectionAntiAliasLabel.FontSize = UiConstants.ComponentFontSize;
			m_selectionAntiAliasLabel.VerticalOptions = LayoutOptions.Center;
			m_selectionAntiAliasLabel.IsVisible = false;

			m_selectionAntiAliasCheck = new CheckBox();
			m_selectionAntiAliasCheck.VerticalOptions = LayoutOptions.Center;
			m_selectionAntiAliasCheck.IsVisible = false;
			m_selectionAntiAliasCheck.IsChecked = m_toolState.SelectionAntiAlias();
			m_selectionAntiAliasCheck.CheckedChanged += OnSelectionAntiAliasChanged;

			m_toleranceLabel = new Label();
			m_toleranceLabel.Text = "Tolerance";
			m_toleranceLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_toleranceLabel.FontSize = UiConstants.ComponentFontSize;
			m_toleranceLabel.VerticalOptions = LayoutOptions.Center;
			m_toleranceLabel.IsVisible = false;

			m_toleranceField = new SliderField(0, 255, m_toolState.FillTolerance(), "", OnToleranceValue);
			m_toleranceField.VerticalOptions = LayoutOptions.Center;
			m_toleranceField.IsVisible = false;

			m_wandAntiAliasLabel = new Label();
			m_wandAntiAliasLabel.Text = "Anti-alias";
			m_wandAntiAliasLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_wandAntiAliasLabel.FontSize = UiConstants.ComponentFontSize;
			m_wandAntiAliasLabel.VerticalOptions = LayoutOptions.Center;
			m_wandAntiAliasLabel.IsVisible = false;

			m_wandAntiAliasCheck = new CheckBox();
			m_wandAntiAliasCheck.VerticalOptions = LayoutOptions.Center;
			m_wandAntiAliasCheck.IsVisible = false;
			m_wandAntiAliasCheck.IsChecked = m_toolState.WandAntiAlias();
			m_wandAntiAliasCheck.CheckedChanged += OnWandAntiAliasChanged;

			m_wandContiguousLabel = new Label();
			m_wandContiguousLabel.Text = "Contiguous";
			m_wandContiguousLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_wandContiguousLabel.FontSize = UiConstants.ComponentFontSize;
			m_wandContiguousLabel.VerticalOptions = LayoutOptions.Center;
			m_wandContiguousLabel.IsVisible = false;

			m_wandContiguousCheck = new CheckBox();
			m_wandContiguousCheck.VerticalOptions = LayoutOptions.Center;
			m_wandContiguousCheck.IsVisible = false;
			m_wandContiguousCheck.IsChecked = m_toolState.WandContiguous();
			m_wandContiguousCheck.CheckedChanged += OnWandContiguousChanged;

			m_wandSampleAllLabel = new Label();
			m_wandSampleAllLabel.Text = "Sample All Layers";
			m_wandSampleAllLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_wandSampleAllLabel.FontSize = UiConstants.ComponentFontSize;
			m_wandSampleAllLabel.VerticalOptions = LayoutOptions.Center;
			m_wandSampleAllLabel.IsVisible = false;

			m_wandSampleAllCheck = new CheckBox();
			m_wandSampleAllCheck.VerticalOptions = LayoutOptions.Center;
			m_wandSampleAllCheck.IsVisible = false;
			m_wandSampleAllCheck.IsChecked = m_toolState.WandSampleAll();
			m_wandSampleAllCheck.CheckedChanged += OnWandSampleAllChanged;

			m_magneticWidthLabel = new Label();
			m_magneticWidthLabel.Text = "Width";
			m_magneticWidthLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_magneticWidthLabel.FontSize = UiConstants.ComponentFontSize;
			m_magneticWidthLabel.VerticalOptions = LayoutOptions.Center;
			m_magneticWidthLabel.IsVisible = false;

			m_magneticWidthField = new SliderField(1, 40, m_toolState.MagneticWidth(), " px", OnMagneticWidthValue);
			m_magneticWidthField.VerticalOptions = LayoutOptions.Center;
			m_magneticWidthField.IsVisible = false;

			m_magneticContrastLabel = new Label();
			m_magneticContrastLabel.Text = "Contrast";
			m_magneticContrastLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_magneticContrastLabel.FontSize = UiConstants.ComponentFontSize;
			m_magneticContrastLabel.VerticalOptions = LayoutOptions.Center;
			m_magneticContrastLabel.IsVisible = false;

			m_magneticContrastField = new SliderField(0, 100, m_toolState.MagneticContrast(), "%", OnMagneticContrastValue);
			m_magneticContrastField.VerticalOptions = LayoutOptions.Center;
			m_magneticContrastField.IsVisible = false;

			m_textFontLabel = new Label();
			m_textFontLabel.Text = "Font";
			m_textFontLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textFontLabel.FontSize = UiConstants.ComponentFontSize;
			m_textFontLabel.VerticalOptions = LayoutOptions.Center;
			m_textFontLabel.IsVisible = false;

			m_fontFamilies = SkiaSharp.SKFontManager.Default.GetFontFamilies();
			System.Array.Sort(m_fontFamilies);

			m_textFontButton = new Button();
			m_textFontButton.FontSize = UiConstants.ComponentFontSize;
			m_textFontButton.WidthRequest = 160.0;
			m_textFontButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_textFontButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_textFontButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_textFontButton.VerticalOptions = LayoutOptions.Center;
			m_textFontButton.IsVisible = false;
			m_textFontButton.Clicked += OnFontButtonClicked;
			UpdateFontButtonText();

			m_textSizeLabel = new Label();
			m_textSizeLabel.Text = "Size";
			m_textSizeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textSizeLabel.FontSize = UiConstants.ComponentFontSize;
			m_textSizeLabel.VerticalOptions = LayoutOptions.Center;
			m_textSizeLabel.IsVisible = false;

			m_textSizeField = new SliderField(6, 200, m_toolState.TextSize(), " px", OnTextSizeValue);
			m_textSizeField.VerticalOptions = LayoutOptions.Center;
			m_textSizeField.IsVisible = false;

			m_textStyleLabel = new Label();
			m_textStyleLabel.Text = "Style";
			m_textStyleLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textStyleLabel.FontSize = UiConstants.ComponentFontSize;
			m_textStyleLabel.VerticalOptions = LayoutOptions.Center;
			m_textStyleLabel.IsVisible = false;

			m_textStyleButton = new Button();
			m_textStyleButton.FontSize = UiConstants.ComponentFontSize;
			m_textStyleButton.WidthRequest = 110.0;
			m_textStyleButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_textStyleButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_textStyleButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_textStyleButton.VerticalOptions = LayoutOptions.Center;
			m_textStyleButton.IsVisible = false;
			m_textStyleButton.Clicked += OnStyleButtonClicked;
			UpdateStyleButtonText();

			m_textAlignLabel = new Label();
			m_textAlignLabel.Text = "Align";
			m_textAlignLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textAlignLabel.FontSize = UiConstants.ComponentFontSize;
			m_textAlignLabel.VerticalOptions = LayoutOptions.Center;
			m_textAlignLabel.IsVisible = false;

			m_textAlignPicker = new Picker();
			m_textAlignPicker.FontSize = UiConstants.ComponentFontSize;
			m_textAlignPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_textAlignPicker.VerticalOptions = LayoutOptions.Center;
			m_textAlignPicker.IsVisible = false;
			m_textAlignPicker.Items.Add("Left");
			m_textAlignPicker.Items.Add("Center");
			m_textAlignPicker.Items.Add("Right");
			m_textAlignPicker.SelectedIndex = m_toolState.TextAlign();
			m_textAlignPicker.SelectedIndexChanged += OnTextAlignChanged;

			m_textAntiAliasLabel = new Label();
			m_textAntiAliasLabel.Text = "Anti-alias";
			m_textAntiAliasLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textAntiAliasLabel.FontSize = UiConstants.ComponentFontSize;
			m_textAntiAliasLabel.VerticalOptions = LayoutOptions.Center;
			m_textAntiAliasLabel.IsVisible = false;

			m_textAntiAliasPicker = new Picker();
			m_textAntiAliasPicker.FontSize = UiConstants.ComponentFontSize;
			m_textAntiAliasPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_textAntiAliasPicker.VerticalOptions = LayoutOptions.Center;
			m_textAntiAliasPicker.IsVisible = false;
			m_textAntiAliasPicker.Items.Add("None");
			m_textAntiAliasPicker.Items.Add("Sharp");
			m_textAntiAliasPicker.Items.Add("Crisp");
			m_textAntiAliasPicker.Items.Add("Strong");
			m_textAntiAliasPicker.Items.Add("Smooth");
			m_textAntiAliasPicker.SelectedIndex = m_toolState.TextAntiAlias();
			m_textAntiAliasPicker.SelectedIndexChanged += OnTextAntiAliasChanged;

			m_textColorLabel = new Label();
			m_textColorLabel.Text = "Color";
			m_textColorLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textColorLabel.FontSize = UiConstants.ComponentFontSize;
			m_textColorLabel.VerticalOptions = LayoutOptions.Center;
			m_textColorLabel.IsVisible = false;

			m_textColorSwatch = new BoxView();
			m_textColorSwatch.WidthRequest = 22.0;
			m_textColorSwatch.HeightRequest = 18.0;
			m_textColorSwatch.Color = MainView.FromSkColor(m_toolState.Foreground());
			m_textColorSwatch.VerticalOptions = LayoutOptions.Center;
			m_textColorSwatch.IsVisible = false;
			TapGestureRecognizer textColorTap = new TapGestureRecognizer();
			textColorTap.Tapped += OnTextColorTapped;
			m_textColorSwatch.GestureRecognizers.Add(textColorTap);

			m_textCharButton = new Button();
			m_textCharButton.Text = "Character…";
			m_textCharButton.FontSize = UiConstants.ComponentFontSize;
			m_textCharButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_textCharButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_textCharButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_textCharButton.VerticalOptions = LayoutOptions.Center;
			m_textCharButton.IsVisible = false;
			m_textCharButton.Clicked += OnTextCharClicked;

			HorizontalStackLayout options = new HorizontalStackLayout();
			m_optionsRow = options;
			options.Spacing = 8.0;
			options.VerticalOptions = LayoutOptions.Center;
			options.Add(m_brushSettingsButton);
			options.Add(m_brushSizeLabel);
			options.Add(m_brushSizeField);
			options.Add(m_brushHardnessLabel);
			options.Add(m_brushHardnessField);
			options.Add(m_brushOpacityLabel);
			options.Add(m_brushOpacityField);
			options.Add(m_brushFlowLabel);
			options.Add(m_brushFlowField);
			options.Add(m_brushStrengthLabel);
			options.Add(m_brushStrengthField);
			options.Add(m_brushSmoothingLabel);
			options.Add(m_brushSmoothingField);
			options.Add(m_brushModeLabel);
			options.Add(m_brushModePicker);
			options.Add(m_brushAirbrushLabel);
			options.Add(m_brushAirbrushCheck);
			options.Add(m_pressureSizeLabel);
			options.Add(m_pressureSizeCheck);
			options.Add(m_pressureOpacityLabel);
			options.Add(m_pressureOpacityCheck);
			options.Add(m_cloneAlignedLabel);
			options.Add(m_cloneAlignedCheck);
			options.Add(m_spongeModeLabel);
			options.Add(m_spongeModePicker);
			options.Add(m_colorReplaceModeLabel);
			options.Add(m_colorReplaceModePicker);
			options.Add(m_colorReplaceToleranceLabel);
			options.Add(m_colorReplaceToleranceField);
			options.Add(m_dodgeBurnRangeLabel);
			options.Add(m_dodgeBurnRangePicker);
			options.Add(m_dodgeBurnExposureLabel);
			options.Add(m_dodgeBurnExposureField);
			options.Add(m_gradientTypeLabel);
			options.Add(m_gradientTypeButton);
			options.Add(m_gradientReverseLabel);
			options.Add(m_gradientReverseCheck);
			options.Add(m_gradientTransparentLabel);
			options.Add(m_gradientTransparentCheck);
			options.Add(m_fillContentLabel);
			options.Add(m_fillContentPicker);
			options.Add(m_fillPatternLabel);
			options.Add(m_fillPatternButton);
			options.Add(m_lineAntiAliasLabel);
			options.Add(m_lineAntiAliasCheck);
			options.Add(m_selectModeNewButton);
			options.Add(m_selectModeAddButton);
			options.Add(m_selectModeSubtractButton);
			options.Add(m_selectModeIntersectButton);
			options.Add(m_selectionFeatherLabel);
			options.Add(m_selectionFeatherField);
			options.Add(m_selectionAntiAliasLabel);
			options.Add(m_selectionAntiAliasCheck);
			options.Add(m_toleranceLabel);
			options.Add(m_toleranceField);
			options.Add(m_wandAntiAliasLabel);
			options.Add(m_wandAntiAliasCheck);
			options.Add(m_wandContiguousLabel);
			options.Add(m_wandContiguousCheck);
			options.Add(m_wandSampleAllLabel);
			options.Add(m_wandSampleAllCheck);
			options.Add(m_magneticWidthLabel);
			options.Add(m_magneticWidthField);
			options.Add(m_magneticContrastLabel);
			options.Add(m_magneticContrastField);
			options.Add(m_textFontLabel);
			options.Add(m_textFontButton);
			options.Add(m_textSizeLabel);
			options.Add(m_textSizeField);
			options.Add(m_textStyleLabel);
			options.Add(m_textStyleButton);
			options.Add(m_textAlignLabel);
			options.Add(m_textAlignPicker);
			options.Add(m_textAntiAliasLabel);
			options.Add(m_textAntiAliasPicker);
			options.Add(m_textColorLabel);
			options.Add(m_textColorSwatch);
			options.Add(m_textCharButton);
			Grid.SetColumn(options, 1);
			bar.Add(options);

			m_lineAntiAliasCheck.IsChecked = m_toolState.LineAntiAlias();

			m_root = bar;
		}

		public View Root()
		{
			return m_root;
		}

		public void ShowForTool(eTool tool)
		{
			if (m_optionsToolLabel != null)
			{
				m_optionsToolLabel.Text = tool.ToString();
			}
			bool isLine = tool == eTool.Line;
			if (m_lineAntiAliasLabel != null)
			{
				m_lineAntiAliasLabel.IsVisible = isLine;
			}
			if (m_lineAntiAliasCheck != null)
			{
				m_lineAntiAliasCheck.IsVisible = isLine;
			}
			bool isSelectionTool = tool == eTool.Select || tool == eTool.EllipseSelect || tool == eTool.FreehandLasso || tool == eTool.Lasso || tool == eTool.MagneticLasso || tool == eTool.MagicWand;
			bool usesSelectionAntiAlias = tool == eTool.EllipseSelect || tool == eTool.FreehandLasso || tool == eTool.Lasso || tool == eTool.MagneticLasso;
			if (m_selectModeNewButton != null)
			{
				m_selectModeNewButton.IsVisible = isSelectionTool;
				m_selectModeAddButton.IsVisible = isSelectionTool;
				m_selectModeSubtractButton.IsVisible = isSelectionTool;
				m_selectModeIntersectButton.IsVisible = isSelectionTool;
				m_selectionFeatherLabel.IsVisible = isSelectionTool;
				m_selectionFeatherField.IsVisible = isSelectionTool;
				m_selectionAntiAliasLabel.IsVisible = usesSelectionAntiAlias;
				m_selectionAntiAliasCheck.IsVisible = usesSelectionAntiAlias;
				if (isSelectionTool)
				{
					RefreshSelectionModeButtons();
				}
			}
			bool isWand = tool == eTool.MagicWand;
			bool isFill = tool == eTool.Fill;
			bool usesTolerance = isWand || tool == eTool.Fill;
			if (m_toleranceLabel != null)
			{
				m_toleranceLabel.IsVisible = usesTolerance;
				m_toleranceField.IsVisible = usesTolerance;
				m_wandAntiAliasLabel.IsVisible = isWand;
				m_wandAntiAliasCheck.IsVisible = isWand;
				m_wandContiguousLabel.IsVisible = isWand;
				m_wandContiguousCheck.IsVisible = isWand;
				m_wandSampleAllLabel.IsVisible = isWand;
				m_wandSampleAllCheck.IsVisible = isWand;
			}
			if (m_fillContentLabel != null)
			{
				m_fillContentLabel.IsVisible = isFill;
				m_fillContentPicker.IsVisible = isFill;
				m_fillPatternLabel.IsVisible = isFill;
				m_fillPatternButton.IsVisible = isFill;
				if (isFill)
				{
					if (m_toolState.FillContent() == eFillContent.Pattern)
					{
						m_fillContentPicker.SelectedIndex = 1;
					}
					else
					{
						m_fillContentPicker.SelectedIndex = 0;
					}
				}
			}
			bool isMagnetic = tool == eTool.MagneticLasso;
			if (m_magneticWidthLabel != null)
			{
				m_magneticWidthLabel.IsVisible = isMagnetic;
				m_magneticWidthField.IsVisible = isMagnetic;
				m_magneticContrastLabel.IsVisible = isMagnetic;
				m_magneticContrastField.IsVisible = isMagnetic;
			}
			bool isText = tool == eTool.Text;
			if (m_textFontLabel != null)
			{
				m_textFontLabel.IsVisible = isText;
				m_textFontButton.IsVisible = isText;
				m_textSizeLabel.IsVisible = isText;
				m_textSizeField.IsVisible = isText;
				m_textStyleLabel.IsVisible = isText;
				m_textStyleButton.IsVisible = isText;
				m_textAlignLabel.IsVisible = isText;
				m_textAlignPicker.IsVisible = isText;
				m_textAntiAliasLabel.IsVisible = isText;
				m_textAntiAliasPicker.IsVisible = isText;
				m_textColorLabel.IsVisible = isText;
				m_textColorSwatch.IsVisible = isText;
				m_textCharButton.IsVisible = isText;
			}
			bool isSponge = tool == eTool.Sponge;
			bool isColorReplace = tool == eTool.ColorReplacement;
			bool isStrengthTool = tool == eTool.Blur || tool == eTool.Sharpen || tool == eTool.Smudge;
			bool isBrushFamily = tool == eTool.Brush || tool == eTool.Eraser || tool == eTool.Clone || tool == eTool.Heal || tool == eTool.Blur || tool == eTool.Sharpen || tool == eTool.Smudge || tool == eTool.Dodge || tool == eTool.Burn || isSponge || isColorReplace;
			bool showsBlendMode = isBrushFamily && !isSponge && !isColorReplace && !isStrengthTool;
			bool usesSize = isBrushFamily || tool == eTool.Pencil || tool == eTool.Line;
			if (m_brushSizeLabel != null)
			{
				m_brushSizeLabel.IsVisible = usesSize;
				m_brushSizeField.IsVisible = usesSize;
			}
			if (m_brushHardnessLabel != null)
			{
				m_brushHardnessLabel.IsVisible = isBrushFamily;
				m_brushHardnessField.IsVisible = isBrushFamily;
				bool showsOpacityFlow = isBrushFamily && !isStrengthTool;
				m_brushOpacityLabel.IsVisible = showsOpacityFlow;
				m_brushOpacityField.IsVisible = showsOpacityFlow;
				m_brushFlowLabel.IsVisible = showsOpacityFlow;
				m_brushFlowField.IsVisible = showsOpacityFlow;
				m_brushStrengthLabel.IsVisible = isStrengthTool;
				m_brushStrengthField.IsVisible = isStrengthTool;
				m_brushSmoothingLabel.IsVisible = isBrushFamily;
				m_brushSmoothingField.IsVisible = isBrushFamily;
				m_brushModeLabel.IsVisible = showsBlendMode;
				m_brushModePicker.IsVisible = showsBlendMode;
				m_brushAirbrushLabel.IsVisible = isBrushFamily;
				m_brushAirbrushCheck.IsVisible = isBrushFamily;
				m_pressureSizeLabel.IsVisible = isBrushFamily;
				m_pressureSizeCheck.IsVisible = isBrushFamily;
				m_pressureOpacityLabel.IsVisible = isBrushFamily;
				m_pressureOpacityCheck.IsVisible = isBrushFamily;
				m_brushSettingsButton.IsVisible = isBrushFamily;
				bool isCloneOrHeal = tool == eTool.Clone || tool == eTool.Heal;
				m_cloneAlignedLabel.IsVisible = isCloneOrHeal;
				m_cloneAlignedCheck.IsVisible = isCloneOrHeal;
			}
			if (m_spongeModeLabel != null)
			{
				m_spongeModeLabel.IsVisible = isSponge;
				m_spongeModePicker.IsVisible = isSponge;
				m_colorReplaceModeLabel.IsVisible = isColorReplace;
				m_colorReplaceModePicker.IsVisible = isColorReplace;
				m_colorReplaceToleranceLabel.IsVisible = isColorReplace;
				m_colorReplaceToleranceField.IsVisible = isColorReplace;
				bool isDodgeBurn = tool == eTool.Dodge || tool == eTool.Burn;
				m_dodgeBurnRangeLabel.IsVisible = isDodgeBurn;
				m_dodgeBurnRangePicker.IsVisible = isDodgeBurn;
				m_dodgeBurnExposureLabel.IsVisible = isDodgeBurn;
				m_dodgeBurnExposureField.IsVisible = isDodgeBurn;
			}
			bool isGradient = tool == eTool.Gradient;
			if (m_gradientTypeLabel != null)
			{
				m_gradientTypeLabel.IsVisible = isGradient;
				m_gradientTypeButton.IsVisible = isGradient;
				if (isGradient)
				{
					UpdateGradientTypeButtonText();
				}
				m_gradientReverseLabel.IsVisible = isGradient;
				m_gradientReverseCheck.IsVisible = isGradient;
				m_gradientTransparentLabel.IsVisible = isGradient;
				m_gradientTransparentCheck.IsVisible = isGradient;
			}
		}

		public void SyncTextOptions()
		{
			if (m_textSizeField != null)
			{
				m_textSizeField.SetValueSilently(m_toolState.TextSize());
			}
			UpdateStyleButtonText();
			if (m_textAlignPicker != null)
			{
				m_textAlignPicker.SelectedIndex = m_toolState.TextAlign();
			}
			if (m_textAntiAliasPicker != null)
			{
				m_textAntiAliasPicker.SelectedIndex = m_toolState.TextAntiAlias();
			}
			UpdateFontButtonText();
			if (m_textColorSwatch != null)
			{
				m_textColorSwatch.Color = MainView.FromSkColor(m_toolState.Foreground());
			}
		}

		public void ApplyBrushTip(bool square)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSquareTip(square);
		}

		public void ApplyBrushSpacing(int spacing)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSpacing(spacing);
		}

		public void UpdateTextColorSwatch(SKColor color)
		{
			if (m_textColorSwatch != null)
			{
				m_textColorSwatch.Color = MainView.FromSkColor(color);
			}
		}
	}
}
