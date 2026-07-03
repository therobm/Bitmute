using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class SizeDialog : ModalDialog
	{
		private const int MaximumSize = 8192;

		private bool m_canvasMode;
		private Entry m_widthEntry;
		private Entry m_heightEntry;
		private Picker m_horizontalAnchor;
		private Picker m_verticalAnchor;
		private Picker m_interpolation;

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
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

		private int AnchorValue(Picker picker)
		{
			int index = picker.SelectedIndex;
			if (index == 0)
			{
				return -1;
			}
			if (index == 2)
			{
				return 1;
			}
			return 0;
		}

		private void OnApplyClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int width = ParseSize(m_widthEntry, 1);
			int height = ParseSize(m_heightEntry, 1);
			if (m_canvasMode)
			{
				main.ApplyCanvasSize(width, height, AnchorValue(m_horizontalAnchor), AnchorValue(m_verticalAnchor));
			}
			else
			{
				main.ApplyImageSize(width, height, m_interpolation.SelectedIndex);
			}
			CloseModal();
		}

		private Entry BuildNumericEntry(int initial)
		{
			Entry entry = new Entry();
			entry.FontSize = 12.0;
			entry.WidthRequest = 90.0;
			entry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			entry.Keyboard = Keyboard.Numeric;
			entry.Text = initial.ToString();
			return entry;
		}

		private Grid BuildRow(string label, View field)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.FontSize = 12.0;
			caption.WidthRequest = 90.0;
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

		public SizeDialog(string title, bool canvasMode, int currentWidth, int currentHeight)
		{
			m_canvasMode = canvasMode;
			m_widthEntry = BuildNumericEntry(currentWidth);
			m_heightEntry = BuildNumericEntry(currentHeight);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			body.Add(BuildRow("Width", m_widthEntry));
			body.Add(BuildRow("Height", m_heightEntry));

			if (canvasMode)
			{
				m_horizontalAnchor = new Picker();
				m_horizontalAnchor.FontSize = 12.0;
				m_horizontalAnchor.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				m_horizontalAnchor.Items.Add("Left");
				m_horizontalAnchor.Items.Add("Center");
				m_horizontalAnchor.Items.Add("Right");
				m_horizontalAnchor.SelectedIndex = 1;

				m_verticalAnchor = new Picker();
				m_verticalAnchor.FontSize = 12.0;
				m_verticalAnchor.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				m_verticalAnchor.Items.Add("Top");
				m_verticalAnchor.Items.Add("Middle");
				m_verticalAnchor.Items.Add("Bottom");
				m_verticalAnchor.SelectedIndex = 1;

				body.Add(BuildRow("Anchor X", m_horizontalAnchor));
				body.Add(BuildRow("Anchor Y", m_verticalAnchor));
			}
			else
			{
				m_interpolation = new Picker();
				m_interpolation.FontSize = 12.0;
				m_interpolation.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				m_interpolation.Items.Add("Nearest");
				m_interpolation.Items.Add("Bilinear");
				m_interpolation.Items.Add("Bicubic");
				m_interpolation.SelectedIndex = 2;
				body.Add(BuildRow("Resample", m_interpolation));
			}

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("Apply", OnApplyClicked);
			ComposeDialog(title, body, ButtonRow(cancelButton, applyButton));
		}
	}
}
