using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class SizeDialog : ContentView
	{
		private const int MaximumSize = 8192;

		private bool m_canvasMode;
		private Entry m_widthEntry;
		private Entry m_heightEntry;
		private Picker m_horizontalAnchor;
		private Picker m_verticalAnchor;
		private Picker m_interpolation;

		private void OnTitlePan(object sender, PanUpdatedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.DragModal(eventArgs.StatusType, eventArgs.TotalX, eventArgs.TotalY);
			}
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CloseModal();
			}
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
			main.CloseModal();
		}

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

		private Grid BuildRow(string label, View field)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.TextColor = UiConstants.TextDim;
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

		private View BuildTitleBar(string text)
		{
			Label titleLabel = new Label();
			titleLabel.Text = text;
			titleLabel.FontSize = 13.0;
			titleLabel.TextColor = UiConstants.OnSurface;
			titleLabel.VerticalOptions = LayoutOptions.Center;

			Button closeButton = new Button();
			closeButton.Text = "✕";
			closeButton.FontSize = 12.0;
			closeButton.WidthRequest = UiConstants.CloseButtonSize;
			closeButton.HeightRequest = UiConstants.CloseButtonSize;
			closeButton.Padding = new Thickness(0.0);
			closeButton.BackgroundColor = Colors.Transparent;
			closeButton.TextColor = UiConstants.TextDim;
			closeButton.Clicked += OnCancelClicked;

			Grid titleBar = new Grid();
			titleBar.BackgroundColor = UiConstants.TitleBar;
			titleBar.Padding = new Thickness(8.0, 2.0, 2.0, 2.0);
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(titleLabel, 0);
			Grid.SetColumn(closeButton, 1);
			titleBar.Add(titleLabel);
			titleBar.Add(closeButton);

			PanGestureRecognizer pan = new PanGestureRecognizer();
			pan.PanUpdated += OnTitlePan;
			titleBar.GestureRecognizers.Add(pan);
			return titleBar;
		}

		public SizeDialog(string title, bool canvasMode, int currentWidth, int currentHeight)
		{
			m_canvasMode = canvasMode;
			m_widthEntry = BuildNumericEntry(currentWidth);
			m_heightEntry = BuildNumericEntry(currentHeight);

			VerticalStackLayout innerLayout = new VerticalStackLayout();
			innerLayout.Spacing = 8.0;
			innerLayout.Padding = new Thickness(12.0);
			innerLayout.Add(BuildRow("Width", m_widthEntry));
			innerLayout.Add(BuildRow("Height", m_heightEntry));

			if (canvasMode)
			{
				m_horizontalAnchor = new Picker();
				m_horizontalAnchor.FontSize = 12.0;
				m_horizontalAnchor.TextColor = UiConstants.OnSurface;
				m_horizontalAnchor.Items.Add("Left");
				m_horizontalAnchor.Items.Add("Center");
				m_horizontalAnchor.Items.Add("Right");
				m_horizontalAnchor.SelectedIndex = 1;

				m_verticalAnchor = new Picker();
				m_verticalAnchor.FontSize = 12.0;
				m_verticalAnchor.TextColor = UiConstants.OnSurface;
				m_verticalAnchor.Items.Add("Top");
				m_verticalAnchor.Items.Add("Middle");
				m_verticalAnchor.Items.Add("Bottom");
				m_verticalAnchor.SelectedIndex = 1;

				innerLayout.Add(BuildRow("Anchor X", m_horizontalAnchor));
				innerLayout.Add(BuildRow("Anchor Y", m_verticalAnchor));
			}
			else
			{
				m_interpolation = new Picker();
				m_interpolation.FontSize = 12.0;
				m_interpolation.TextColor = UiConstants.OnSurface;
				m_interpolation.Items.Add("Nearest");
				m_interpolation.Items.Add("Bilinear");
				m_interpolation.Items.Add("Bicubic");
				m_interpolation.SelectedIndex = 2;
				innerLayout.Add(BuildRow("Resample", m_interpolation));
			}

			Button applyButton = new Button();
			applyButton.Text = "Apply";
			applyButton.FontSize = 12.0;
			applyButton.WidthRequest = 90.0;
			applyButton.BackgroundColor = UiConstants.Accent;
			applyButton.TextColor = UiConstants.OnSurface;
			applyButton.Clicked += OnApplyClicked;

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
			buttons.Add(applyButton);
			innerLayout.Add(buttons);

			VerticalStackLayout layout = new VerticalStackLayout();
			layout.Spacing = 0.0;
			layout.Add(BuildTitleBar(title));
			layout.Add(innerLayout);

			Border frame = new Border();
			frame.BackgroundColor = UiConstants.PanelSurface;
			frame.Stroke = UiConstants.Divider;
			frame.StrokeThickness = 1.0;
			frame.Content = layout;

			Content = frame;
		}
	}
}
