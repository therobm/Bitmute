using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class NewDocumentDialog : FieldDialog
	{
		private const int DefaultWidth = 800;
		private const int DefaultHeight = 600;
		private const int MaximumSize = 8192;

		private TextField m_nameField;
		private DualIntField m_sizeField;
		private ListPicker m_backgroundPicker;

		private void OnPreset256(object sender, EventArgs eventArgs)
		{
			m_sizeField.SetValues(256, 256);
		}

		private void OnPreset512(object sender, EventArgs eventArgs)
		{
			m_sizeField.SetValues(512, 512);
		}

		private void OnPreset1024(object sender, EventArgs eventArgs)
		{
			m_sizeField.SetValues(1024, 1024);
		}

		private void OnPreset2048(object sender, EventArgs eventArgs)
		{
			m_sizeField.SetValues(2048, 2048);
		}

		private void OnPreset4096(object sender, EventArgs eventArgs)
		{
			m_sizeField.SetValues(4096, 4096);
		}

		private Button PresetButton(string text, EventHandler handler)
		{
			Button button = new Button();
			button.Text = text;
			button.FontSize = UiConstants.ComponentFontSize;
			button.HeightRequest = UiConstants.ComponentHeight;
			button.Padding = new Thickness(0.0);
			button.CornerRadius = 0;
			button.BorderWidth = 1.0;
			button.ThemeBg(UiConstants.ButtonFaceLight, UiConstants.ButtonFaceDark);
			button.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			button.SetAppThemeColor(Button.BorderColorProperty, UiConstants.ButtonBorderLight, UiConstants.ButtonBorderDark);
			button.Clicked += handler;
			return button;
		}

		private View BuildPresetRow()
		{
			Label captionLabel = new Label();
			captionLabel.Text = "Presets";
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			Button button256 = PresetButton("256", OnPreset256);
			Button button512 = PresetButton("512", OnPreset512);
			Button button1024 = PresetButton("1024", OnPreset1024);
			Button button2048 = PresetButton("2048", OnPreset2048);
			Button button4096 = PresetButton("4096", OnPreset4096);

			HorizontalStackLayout presetRow = new HorizontalStackLayout();
			presetRow.Spacing = UiConstants.DialogRowSpacing;
			presetRow.Add(captionLabel);
			presetRow.Add(button256);
			presetRow.Add(button512);
			presetRow.Add(button1024);
			presetRow.Add(button2048);
			presetRow.Add(button4096);
			return presetRow;
		}

		private void OnCreateClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			bool transparent = m_backgroundPicker.SelectedIndex() == 1;
			main.CreateNewDocument(m_sizeField.FirstValue(), m_sizeField.SecondValue(), m_nameField.Text(), transparent);
			CloseModal();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		public NewDocumentDialog() : this(DefaultWidth, DefaultHeight)
		{
		}

		public NewDocumentDialog(int initialWidth, int initialHeight)
		{
			if (initialWidth < 1 || initialWidth > MaximumSize)
			{
				initialWidth = DefaultWidth;
			}
			if (initialHeight < 1 || initialHeight > MaximumSize)
			{
				initialHeight = DefaultHeight;
			}
			m_nameField = new TextField("Name", "Untitled", null);
			m_sizeField = new DualIntField("Width", "Height", initialWidth, initialHeight, 1, MaximumSize, " px", null);
			m_backgroundPicker = new ListPicker("Background", new string[] { "White", "Transparent" }, 0, null);

			AddField(m_nameField);
			AddField(m_sizeField);
			AddField(BuildPresetRow());
			AddField(m_backgroundPicker);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button createButton = PrimaryButton("Create", OnCreateClicked);
			ComposeFields("New Document", ButtonRow(cancelButton, createButton));
		}
	}
}
