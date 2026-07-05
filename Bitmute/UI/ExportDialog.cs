using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class ExportDialog : ModalDialog
	{
		private const int DefaultQuality = 90;

		private Picker m_formatPicker;
		private SliderField m_qualityField;
		private CheckBox m_losslessCheck;
		private CheckBox m_rleCheck;
		private Grid m_qualityRow;
		private Grid m_losslessRow;
		private Grid m_rleRow;
		private int m_quality;

		private Grid BuildFieldRow(string label, View field)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.FontSize = UiConstants.PanelFontSize;
			caption.WidthRequest = 110.0;
			caption.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = 8.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(caption, 0);
			Grid.SetColumn(field, 1);
			row.Add(caption);
			row.Add(field);
			return row;
		}

		private CheckBox BuildCheckBox(bool initial)
		{
			CheckBox checkBox = new CheckBox();
			checkBox.IsChecked = initial;
			checkBox.HorizontalOptions = LayoutOptions.Start;
			checkBox.VerticalOptions = LayoutOptions.Center;
			checkBox.SetAppThemeColor(CheckBox.ColorProperty, UiConstants.AccentLight, UiConstants.AccentDark);
			return checkBox;
		}

		private string SelectedFormat()
		{
			int index = m_formatPicker.SelectedIndex;
			if (index == 1)
			{
				return "jpeg";
			}
			if (index == 2)
			{
				return "bmp";
			}
			if (index == 3)
			{
				return "tga";
			}
			if (index == 4)
			{
				return "webp";
			}
			return "png";
		}

		private void UpdateOptionVisibility()
		{
			string format = SelectedFormat();
			m_qualityRow.IsVisible = format == "jpeg" || format == "webp";
			m_losslessRow.IsVisible = format == "webp";
			m_rleRow.IsVisible = format == "tga";
		}

		private void OnFormatChanged(object sender, EventArgs eventArgs)
		{
			UpdateOptionVisibility();
		}

		private void OnQualityChanged(int value)
		{
			m_quality = value;
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		private void OnExportClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ConfirmExport(SelectedFormat(), m_quality, m_losslessCheck.IsChecked, m_rleCheck.IsChecked);
		}

		public ExportDialog()
		{
			m_quality = DefaultQuality;

			m_formatPicker = new Picker();
			m_formatPicker.FontSize = UiConstants.PanelFontSize;
			m_formatPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_formatPicker.Items.Add("PNG");
			m_formatPicker.Items.Add("JPEG");
			m_formatPicker.Items.Add("BMP");
			m_formatPicker.Items.Add("TGA");
			m_formatPicker.Items.Add("WebP");
			m_formatPicker.SelectedIndex = 0;
			m_formatPicker.SelectedIndexChanged += OnFormatChanged;

			m_qualityField = new SliderField(1, 100, DefaultQuality, "", OnQualityChanged);
			m_qualityField.HorizontalOptions = LayoutOptions.Start;

			m_losslessCheck = BuildCheckBox(false);
			m_rleCheck = BuildCheckBox(true);

			m_qualityRow = BuildFieldRow("Quality", m_qualityField);
			m_losslessRow = BuildFieldRow("Lossless", m_losslessCheck);
			m_rleRow = BuildFieldRow("RLE compression", m_rleCheck);

			VerticalStackLayout optionRows = new VerticalStackLayout();
			optionRows.Spacing = 8.0;
			optionRows.MinimumHeightRequest = 84.0;
			optionRows.Add(m_qualityRow);
			optionRows.Add(m_losslessRow);
			optionRows.Add(m_rleRow);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			body.WidthRequest = 300.0;
			body.Add(BuildFieldRow("Format", m_formatPicker));
			body.Add(optionRows);

			UpdateOptionVisibility();

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button exportButton = PrimaryButton("Export", OnExportClicked);
			ComposeDialog("Export As", body, ButtonRow(cancelButton, exportButton));
		}
	}
}
