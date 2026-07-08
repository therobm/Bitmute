using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;
using Bitmute.Tools;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI.Dialogs
{
	public class PreferencesDialog : FieldDialog
	{
		private const int UndoDepthMinimum = 10;
		private const int UndoDepthMaximum = 500;
		private const double SectionIndent = 10.0;

		private IntSlider m_undoDepthField;
		private RadioPicker m_themePicker;
		private TextField m_paletteRootField;
		private IntSlider m_pressureMinField;
		private IntSlider m_pressureMaxField;
		private IntSlider m_pressureSensitivityField;
		private Image m_pressurePreview;

		private void OnClearRecentClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ClearRecentFiles();
			}
		}

		private void OnThemeChanged(int index)
		{
			if (index == 0)
			{
				Theme.UseSystem();
			}
			else if (index == 1)
			{
				Theme.UseDark();
			}
			else if (index == 2)
			{
				Theme.UseLight();
			}
		}

		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ApplyUndoDepth(m_undoDepthField.Value());
			string paletteRoot = m_paletteRootField.Text().Trim();
			Microsoft.Maui.Storage.Preferences.Default.Set("palette_root", paletteRoot);
			main.ReloadPalettes();
			Microsoft.Maui.Storage.Preferences.Default.Set("pressure_calib_min", m_pressureMinField.Value());
			Microsoft.Maui.Storage.Preferences.Default.Set("pressure_calib_max", m_pressureMaxField.Value());
			Microsoft.Maui.Storage.Preferences.Default.Set("pressure_calib_sensitivity", m_pressureSensitivityField.Value());
			main.ApplyPenCalibration(m_pressureMinField.Value(), m_pressureMaxField.Value(), m_pressureSensitivityField.Value());
			//main.CloseModal();//wtf?
			base.OnPrimaryClicked(sender, eventArgs);
		}

		private void OnPressureCalibrationChanged(int value)
		{
			RenderPressureCurve();
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ApplyPenCalibration(m_pressureMinField.Value(), m_pressureMaxField.Value(), m_pressureSensitivityField.Value());
			}
		}

		private void RenderPressureCurve()
		{
			int width = 220;
			int height = 110;
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			SKCanvas canvas = new SKCanvas(bitmap);
			canvas.Clear(new SKColor(0x2B, 0x2B, 0x2B));
			SKPaint gridPaint = new SKPaint();
			gridPaint.Color = new SKColor(0x50, 0x50, 0x50);
			gridPaint.StrokeWidth = 1.0f;
			canvas.DrawLine(0.0f, height - 1.0f, width, height - 1.0f, gridPaint);
			canvas.DrawLine(1.0f, 0.0f, 1.0f, height, gridPaint);
			gridPaint.Dispose();
			PressureCalibration calibration = new PressureCalibration();
			calibration.SetValues(m_pressureMinField.Value(), m_pressureMaxField.Value(), m_pressureSensitivityField.Value());
			SKPaint curvePaint = new SKPaint();
			curvePaint.Color = new SKColor(0x4C, 0xA6, 0xFF);
			curvePaint.StrokeWidth = 2.0f;
			curvePaint.IsAntialias = true;
			curvePaint.Style = SKPaintStyle.Stroke;
			SKPath path = new SKPath();
			for (int pixelX = 0; pixelX < width; pixelX++)
			{
				float raw = pixelX / (float)(width - 1);
				float output = calibration.Apply(raw);
				float pixelY = (height - 1.0f) - (output * (height - 1.0f));
				if (pixelX == 0)
				{
					path.MoveTo(pixelX, pixelY);
				}
				else
				{
					path.LineTo(pixelX, pixelY);
				}
			}
			canvas.DrawPath(path, curvePaint);
			path.Dispose();
			curvePaint.Dispose();
			canvas.Dispose();
			m_pressurePreview.Source = new SKBitmapImageSource { Bitmap = bitmap };
		}

		public PreferencesDialog()
		{
			int initialDepth = 100;
			MainView main = MainView.Self;
			if (main != null)
			{
				initialDepth = main.CurrentUndoDepth();
			}
			if (initialDepth < UndoDepthMinimum)
			{
				initialDepth = UndoDepthMinimum;
			}
			if (initialDepth > UndoDepthMaximum)
			{
				initialDepth = UndoDepthMaximum;
			}

			m_undoDepthField = new IntSlider("Undo depth", UndoDepthMinimum, UndoDepthMaximum, initialDepth, "", null);

			int themeIndex = 2;
			if (Theme.FollowSystem())
			{
				themeIndex = 0;
			}
			else if (Theme.IsDark())
			{
				themeIndex = 1;
			}
			m_themePicker = new RadioPicker("Theme", new string[] { "System", "Dark", "Light" }, themeIndex, OnThemeChanged);

			string currentRoot = Microsoft.Maui.Storage.Preferences.Default.Get("palette_root", "");
			m_paletteRootField = new TextField("Root", currentRoot, null);

			int pressureMinimum = Microsoft.Maui.Storage.Preferences.Default.Get("pressure_calib_min", 0);
			int pressureMaximum = Microsoft.Maui.Storage.Preferences.Default.Get("pressure_calib_max", 100);
			int pressureSensitivity = Microsoft.Maui.Storage.Preferences.Default.Get("pressure_calib_sensitivity", 100);
			m_pressureMinField = new IntSlider("Min", 0, 90, pressureMinimum, "%", OnPressureCalibrationChanged);
			m_pressureMaxField = new IntSlider("Max", 10, 100, pressureMaximum, "%", OnPressureCalibrationChanged);
			m_pressureSensitivityField = new IntSlider("Sensitivity", 50, 200, pressureSensitivity, "%", OnPressureCalibrationChanged);
			m_pressurePreview = new Image();
			m_pressurePreview.WidthRequest = 220.0;
			m_pressurePreview.HeightRequest = 110.0;
			m_pressurePreview.HorizontalOptions = LayoutOptions.Start;
			m_pressurePreview.Margin = new Thickness(SectionIndent, 4.0, 0.0, 0.0);
			RenderPressureCurve();

			Button clearRecentButton = CreateButton("Clear Recent Files", OnClearRecentClicked);
			clearRecentButton.WidthRequest = 150.0;
			clearRecentButton.HorizontalOptions = LayoutOptions.Start;

			m_undoDepthField.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);
			clearRecentButton.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);
			m_themePicker.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);
			m_paletteRootField.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);
			m_pressureMinField.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);
			m_pressureMaxField.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);
			m_pressureSensitivityField.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);

			AddField(new SectionHeader("General"));
			AddField(m_undoDepthField);
			AddField(clearRecentButton);
			AddField(new SectionHeader("Interface"));
			AddField(m_themePicker);
			AddField(new SectionHeader("Palettes"));
			AddField(m_paletteRootField);
			AddField(new SectionHeader("Stylus"));
			AddField(m_pressureMinField);
			AddField(m_pressureMaxField);
			AddField(m_pressureSensitivityField);
			AddField(m_pressurePreview);

			Button cancelButton = SecondaryButton("Cancel");
			Button okButton = PrimaryButton("OK");
			ComposeFields("Preferences", ButtonRow(cancelButton, okButton), 340.0 - (2.0 * UiConstants.DialogPadding));
		}
	}
}
