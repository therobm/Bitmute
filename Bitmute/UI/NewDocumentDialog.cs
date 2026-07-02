using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class NewDocumentDialog : ContentView
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
			entry.TextColor = UiConstants.OnSurface;
			entry.Keyboard = Keyboard.Numeric;
			entry.Text = initial.ToString();
			return entry;
		}

		private Grid BuildFieldRow(string label, View field)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.TextColor = UiConstants.TextDim;
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
			main.CloseModal();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CloseModal();
			}
		}

		public NewDocumentDialog()
		{
			m_nameEntry = new Entry();
			m_nameEntry.FontSize = 12.0;
			m_nameEntry.TextColor = UiConstants.OnSurface;
			m_nameEntry.Text = "Untitled";

			m_widthEntry = BuildNumericEntry(DefaultWidth);
			m_heightEntry = BuildNumericEntry(DefaultHeight);

			m_backgroundPicker = new Picker();
			m_backgroundPicker.FontSize = 12.0;
			m_backgroundPicker.TextColor = UiConstants.OnSurface;
			m_backgroundPicker.Items.Add("White");
			m_backgroundPicker.Items.Add("Transparent");
			m_backgroundPicker.SelectedIndex = 0;

			Button createButton = new Button();
			createButton.Text = "Create";
			createButton.FontSize = 12.0;
			createButton.WidthRequest = 90.0;
			createButton.BackgroundColor = UiConstants.Accent;
			createButton.TextColor = UiConstants.OnSurface;
			createButton.Clicked += OnCreateClicked;

			Button cancelButton = new Button();
			cancelButton.Text = "Cancel";
			cancelButton.FontSize = 12.0;
			cancelButton.WidthRequest = 90.0;
			cancelButton.BackgroundColor = UiConstants.ChromeRaised;
			cancelButton.TextColor = UiConstants.OnSurface;
			cancelButton.Clicked += OnCancelClicked;

			HorizontalStackLayout buttons = new HorizontalStackLayout();
			buttons.Spacing = 8.0;
			buttons.HorizontalOptions = LayoutOptions.End;
			buttons.Add(cancelButton);
			buttons.Add(createButton);

			Label title = new Label();
			title.Text = "New Document";
			title.FontSize = 13.0;
			title.TextColor = UiConstants.OnSurface;

			VerticalStackLayout layout = new VerticalStackLayout();
			layout.Spacing = 8.0;
			layout.Padding = new Thickness(12.0);
			layout.Add(title);
			layout.Add(BuildFieldRow("Name", m_nameEntry));
			layout.Add(BuildFieldRow("Width", m_widthEntry));
			layout.Add(BuildFieldRow("Height", m_heightEntry));
			layout.Add(BuildFieldRow("Background", m_backgroundPicker));
			layout.Add(buttons);

			Border frame = new Border();
			frame.BackgroundColor = UiConstants.PanelSurface;
			frame.Stroke = UiConstants.Divider;
			frame.StrokeThickness = 1.0;
			frame.Content = layout;

			Content = frame;
		}
	}
}
