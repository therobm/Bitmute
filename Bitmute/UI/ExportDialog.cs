using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI
{
	public class ExportDialog : FieldDialog
	{
		private const int DefaultQuality = 90;

		private ListPicker m_formatPicker;
		private IntSlider m_qualityField;
		private CheckField m_losslessField;
		private CheckField m_rleField;

		private string SelectedFormat()
		{
			int index = m_formatPicker.SelectedIndex();
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
			m_qualityField.IsVisible = format == "jpeg" || format == "webp";
			m_losslessField.IsVisible = format == "webp";
			m_rleField.IsVisible = format == "tga";
		}

		private void OnFormatChanged(int index)
		{
			UpdateOptionVisibility();
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
			main.ConfirmExport(SelectedFormat(), m_qualityField.Value(), m_losslessField.Checked(), m_rleField.Checked());
		}

		public ExportDialog()
		{
			m_formatPicker = new ListPicker("Format", new string[] { "PNG", "JPEG", "BMP", "TGA", "WebP" }, 0, OnFormatChanged);
			m_qualityField = new IntSlider("Quality", 1, 100, DefaultQuality, "", null);
			m_losslessField = new CheckField("Lossless", false, null);
			m_rleField = new CheckField("RLE compression", true, null);

			VerticalStackLayout optionRows = new VerticalStackLayout();
			optionRows.Spacing = UiConstants.DialogRowSpacing;
			optionRows.MinimumHeightRequest = 84.0;
			optionRows.Add(m_qualityField);
			optionRows.Add(m_losslessField);
			optionRows.Add(m_rleField);

			AddField(m_formatPicker);
			AddField(optionRows);
			UpdateOptionVisibility();

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button exportButton = PrimaryButton("Export", OnExportClicked);
			ComposeFields("Export As", ButtonRow(cancelButton, exportButton));
		}
	}
}
