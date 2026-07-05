using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class StrokeDialog : ModalDialog
	{
		private SliderField m_widthField;
		private Picker m_positionPicker;

		private void OnApplyClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int position = m_positionPicker.SelectedIndex;
			if (position < 0)
			{
				position = 1;
			}
			main.ApplyStroke(m_widthField.Value(), position);
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		public StrokeDialog()
		{
			Label widthLabel = new Label();
			widthLabel.Text = "Width";
			widthLabel.FontSize = UiConstants.PanelFontSize;
			widthLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			widthLabel.VerticalOptions = LayoutOptions.Center;
			widthLabel.WidthRequest = 70.0;

			m_widthField = new SliderField(1, 100, 2, " px", OnWidthValue);
			m_widthField.VerticalOptions = LayoutOptions.Center;

			HorizontalStackLayout widthRow = new HorizontalStackLayout();
			widthRow.Spacing = 8.0;
			widthRow.Add(widthLabel);
			widthRow.Add(m_widthField);

			Label positionLabel = new Label();
			positionLabel.Text = "Position";
			positionLabel.FontSize = UiConstants.PanelFontSize;
			positionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			positionLabel.VerticalOptions = LayoutOptions.Center;
			positionLabel.WidthRequest = 70.0;

			m_positionPicker = new Picker();
			m_positionPicker.FontSize = UiConstants.PanelFontSize;
			m_positionPicker.WidthRequest = 130.0;
			m_positionPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_positionPicker.VerticalOptions = LayoutOptions.Center;
			m_positionPicker.Items.Add("Inside");
			m_positionPicker.Items.Add("Center");
			m_positionPicker.Items.Add("Outside");
			m_positionPicker.SelectedIndex = 1;

			HorizontalStackLayout positionRow = new HorizontalStackLayout();
			positionRow.Spacing = 8.0;
			positionRow.Add(positionLabel);
			positionRow.Add(m_positionPicker);

			Label colorNote = new Label();
			colorNote.Text = "Strokes with the foreground color";
			colorNote.FontSize = UiConstants.ComponentFontSize;
			colorNote.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 10.0;
			body.WidthRequest = 280.0;
			body.Add(widthRow);
			body.Add(positionRow);
			body.Add(colorNote);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("Stroke", OnApplyClicked);
			ComposeDialog("Stroke", body, ButtonRow(cancelButton, applyButton));
		}

		private void OnWidthValue(int value)
		{
		}
	}
}
