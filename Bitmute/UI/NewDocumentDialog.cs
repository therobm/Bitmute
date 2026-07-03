using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class NewDocumentDialog : ModalDialog
	{
		private const int DefaultWidth = 800;
		private const int DefaultHeight = 600;
		private const int MaximumSize = 8192;

		private Entry m_nameEntry;
		private Entry m_widthEntry;
		private Entry m_heightEntry;
		private Picker m_backgroundPicker;

		private Entry BuildNumericEntry(int initial)
		{
			Entry entry = new Entry();
			entry.FontSize = 12.0;
			entry.WidthRequest = 90.0;
			entry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			entry.Keyboard = Keyboard.Numeric;
			entry.Text = initial.ToString();
			return entry;
		}

		private Grid BuildFieldRow(string label, View field)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.FontSize = 12.0;
			caption.WidthRequest = 70.0;
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

		private int ParseSize(Entry entry, int fallback)
		{
			int value = 0;
			bool parsed = int.TryParse(entry.Text, out value);
			if (!parsed)
			{
				return fallback;
			}
			if (value < 1)
			{
				return 1;
			}
			if (value > MaximumSize)
			{
				return MaximumSize;
			}
			return value;
		}

		private void OnCreateClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int width = ParseSize(m_widthEntry, DefaultWidth);
			int height = ParseSize(m_heightEntry, DefaultHeight);
			bool transparent = m_backgroundPicker.SelectedIndex == 1;
			main.CreateNewDocument(width, height, m_nameEntry.Text, transparent);
			CloseModal();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		public NewDocumentDialog()
		{
			m_nameEntry = new Entry();
			m_nameEntry.FontSize = 12.0;
			m_nameEntry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_nameEntry.Text = "Untitled";

			m_widthEntry = BuildNumericEntry(DefaultWidth);
			m_heightEntry = BuildNumericEntry(DefaultHeight);

			m_backgroundPicker = new Picker();
			m_backgroundPicker.FontSize = 12.0;
			m_backgroundPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_backgroundPicker.Items.Add("White");
			m_backgroundPicker.Items.Add("Transparent");
			m_backgroundPicker.SelectedIndex = 0;

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			body.Add(BuildFieldRow("Name", m_nameEntry));
			body.Add(BuildFieldRow("Width", m_widthEntry));
			body.Add(BuildFieldRow("Height", m_heightEntry));
			body.Add(BuildFieldRow("Background", m_backgroundPicker));

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button createButton = PrimaryButton("Create", OnCreateClicked);
			ComposeDialog("New Document", body, ButtonRow(cancelButton, createButton));
		}
	}
}
