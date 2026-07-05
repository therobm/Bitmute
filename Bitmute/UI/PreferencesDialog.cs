using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class PreferencesDialog : ModalDialog
	{
		private const int UndoDepthMinimum = 10;
		private const int UndoDepthMaximum = 500;
		private const string ThemeGroupName = "PreferencesThemeGroup";

		private SliderField m_undoDepthField;
		private RadioButton m_systemRadio;
		private RadioButton m_darkRadio;
		private RadioButton m_lightRadio;

		private Label BuildSectionHeader(string text)
		{
			Label header = new Label();
			header.Text = text;
			header.FontSize = UiConstants.PanelFontSize;
			header.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			return header;
		}

		private Grid BuildFieldRow(string label, View field)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.FontSize = UiConstants.PanelFontSize;
			caption.WidthRequest = 90.0;
			caption.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = 8.0;
			row.Padding = new Thickness(10.0, 0.0, 0.0, 0.0);
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(caption, 0);
			Grid.SetColumn(field, 1);
			row.Add(caption);
			row.Add(field);
			return row;
		}

		private RadioButton BuildThemeRadio(string text)
		{
			RadioButton radio = new RadioButton();
			radio.Content = text;
			radio.FontSize = UiConstants.PanelFontSize;
			radio.GroupName = ThemeGroupName;
			radio.VerticalOptions = LayoutOptions.Center;
			radio.SetAppThemeColor(RadioButton.TextColorProperty, UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			return radio;
		}

		private void OnUndoDepthChanged(int value)
		{
		}

		private void OnClearRecentClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ClearRecentFiles();
			}
		}

		private void OnSystemChecked(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (eventArgs.Value)
			{
				Theme.UseSystem();
			}
		}

		private void OnDarkChecked(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (eventArgs.Value)
			{
				Theme.UseDark();
			}
		}

		private void OnLightChecked(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (eventArgs.Value)
			{
				Theme.UseLight();
			}
		}

		private void OnOkClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ApplyUndoDepth(m_undoDepthField.Value());
			main.CloseModal();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
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

			m_undoDepthField = new SliderField(UndoDepthMinimum, UndoDepthMaximum, initialDepth, "", OnUndoDepthChanged);
			m_undoDepthField.HorizontalOptions = LayoutOptions.Start;

			m_systemRadio = BuildThemeRadio("System");
			m_darkRadio = BuildThemeRadio("Dark");
			m_lightRadio = BuildThemeRadio("Light");

			if (Theme.FollowSystem())
			{
				m_systemRadio.IsChecked = true;
			}
			else if (Theme.IsDark())
			{
				m_darkRadio.IsChecked = true;
			}
			else
			{
				m_lightRadio.IsChecked = true;
			}

			m_systemRadio.CheckedChanged += OnSystemChecked;
			m_darkRadio.CheckedChanged += OnDarkChecked;
			m_lightRadio.CheckedChanged += OnLightChecked;

			HorizontalStackLayout themeRow = new HorizontalStackLayout();
			themeRow.Spacing = 8.0;
			themeRow.Add(m_systemRadio);
			themeRow.Add(m_darkRadio);
			themeRow.Add(m_lightRadio);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			body.WidthRequest = 320.0;
			body.MinimumHeightRequest = 150.0;
			Button clearRecentButton = SecondaryButton("Clear Recent Files", OnClearRecentClicked);
			clearRecentButton.WidthRequest = 150.0;
			clearRecentButton.HorizontalOptions = LayoutOptions.Start;

			body.Add(BuildSectionHeader("General"));
			body.Add(BuildFieldRow("Undo depth", m_undoDepthField));
			body.Add(BuildFieldRow("Recent files", clearRecentButton));
			body.Add(BuildSectionHeader("Interface"));
			body.Add(BuildFieldRow("Theme", themeRow));

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button okButton = PrimaryButton("OK", OnOkClicked);
			ComposeDialog("Preferences", body, ButtonRow(cancelButton, okButton));
		}
	}
}
