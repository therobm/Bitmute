using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class FileField : ContentView
	{
		private Action<string> m_onChanged;
		private Label m_fileLabel;
		private string m_path;

		private async void OnBrowseClicked(object sender, EventArgs eventArgs)
		{
			FileResult result = await FilePicker.Default.PickAsync();
			if (result == null)
			{
				return;
			}
			m_path = result.FullPath;
			m_fileLabel.Text = result.FileName;
			if (m_onChanged != null)
			{
				m_onChanged(m_path);
			}
		}

		public string SelectedPath()
		{
			return m_path;
		}

		public FileField(string caption, Action<string> onChanged)
		{
			m_onChanged = onChanged;
			m_path = "";

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			Button browseButton = new Button();
			browseButton.Text = "Browse…";
			browseButton.FontSize = UiConstants.ComponentFontSize;
			browseButton.HeightRequest = UiConstants.DialogButtonHeight;
			browseButton.Padding = new Thickness(UiConstants.DialogRowSpacing, 0.0);
			browseButton.CornerRadius = 0;
			browseButton.BorderWidth = 1.0;
			browseButton.ThemeBg(UiConstants.ButtonFaceLight, UiConstants.ButtonFaceDark);
			browseButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			browseButton.SetAppThemeColor(Button.BorderColorProperty, UiConstants.ButtonBorderLight, UiConstants.ButtonBorderDark);
			browseButton.VerticalOptions = LayoutOptions.Center;
			browseButton.Clicked += OnBrowseClicked;

			m_fileLabel = new Label();
			m_fileLabel.Text = "";
			m_fileLabel.FontSize = UiConstants.ComponentFontSize;
			m_fileLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_fileLabel.LineBreakMode = LineBreakMode.TailTruncation;
			m_fileLabel.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = UiConstants.DialogRowSpacing;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(browseButton, 1);
			Grid.SetColumn(m_fileLabel, 2);
			row.Add(captionLabel);
			row.Add(browseButton);
			row.Add(m_fileLabel);

			Content = row;
		}
	}
}
