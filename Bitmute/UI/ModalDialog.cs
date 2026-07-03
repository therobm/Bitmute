using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public abstract class ModalDialog : ContentView
	{
		protected void CloseModal()
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CloseModal();
			}
		}

		private void OnCloseClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		private void OnTitlePan(object sender, PanUpdatedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.DragModal(eventArgs.StatusType, eventArgs.TotalX, eventArgs.TotalY);
			}
		}

		private View BuildTitleBar(string text)
		{
			Label titleLabel = new Label();
			titleLabel.Text = text;
			titleLabel.FontSize = 13.0;
			titleLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			titleLabel.VerticalOptions = LayoutOptions.Center;

			Button closeButton = new Button();
			closeButton.Text = "✕";
			closeButton.FontSize = 12.0;
			closeButton.WidthRequest = UiConstants.CloseButtonSize;
			closeButton.HeightRequest = UiConstants.CloseButtonSize;
			closeButton.Padding = new Thickness(0.0);
			closeButton.BackgroundColor = Colors.Transparent;
			closeButton.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			closeButton.Clicked += OnCloseClicked;

			Grid titleBar = new Grid();
			titleBar.ThemeBg(UiConstants.TitleBarLight, UiConstants.TitleBarDark);
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

		protected Button PrimaryButton(string text, EventHandler handler)
		{
			Button button = new Button();
			button.Text = text;
			button.FontSize = 12.0;
			button.WidthRequest = 90.0;
			button.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			button.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			button.Clicked += handler;
			return button;
		}

		protected Button SecondaryButton(string text, EventHandler handler)
		{
			Button button = new Button();
			button.Text = text;
			button.FontSize = 12.0;
			button.WidthRequest = 90.0;
			button.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			button.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			button.Clicked += handler;
			return button;
		}

		protected View ButtonRow(Button cancelButton, Button primaryButton)
		{
			HorizontalStackLayout buttons = new HorizontalStackLayout();
			buttons.Spacing = 8.0;
			buttons.HorizontalOptions = LayoutOptions.End;
			buttons.Add(cancelButton);
			buttons.Add(primaryButton);
			return buttons;
		}

		protected void ComposeDialog(string title, View body, View buttonRow)
		{
			VerticalStackLayout innerLayout = new VerticalStackLayout();
			innerLayout.Spacing = 10.0;
			innerLayout.Padding = new Thickness(12.0);
			innerLayout.Add(body);
			if (buttonRow != null)
			{
				innerLayout.Add(buttonRow);
			}

			VerticalStackLayout layout = new VerticalStackLayout();
			layout.Spacing = 0.0;
			layout.Add(BuildTitleBar(title));
			layout.Add(innerLayout);

			Border frame = new Border();
			frame.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			frame.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			frame.StrokeThickness = 1.0;
			frame.Content = layout;

			Content = frame;
		}
	}
}
