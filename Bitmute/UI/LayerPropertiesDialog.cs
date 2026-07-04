using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Bitmute.UI
{
	public class LayerPropertiesDialog : ModalDialog
	{
		private Entry m_nameEntry;

		public LayerPropertiesDialog(string currentName)
		{
			Label caption = new Label();
			caption.Text = "Name";
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.FontSize = 12.0;
			caption.WidthRequest = 70.0;
			caption.VerticalOptions = LayoutOptions.Center;

			m_nameEntry = new Entry();
			m_nameEntry.FontSize = 12.0;
			m_nameEntry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_nameEntry.Text = currentName;

			Grid row = new Grid();
			row.ColumnSpacing = 8.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(caption, 0);
			Grid.SetColumn(m_nameEntry, 1);
			row.Add(caption);
			row.Add(m_nameEntry);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			body.Add(row);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button okButton = PrimaryButton("OK", OnOkClicked);
			ComposeDialog("Layer Properties", body, ButtonRow(cancelButton, okButton));
		}

		private void OnOkClicked(object sender, EventArgs eventArgs)
		{
			string text = m_nameEntry.Text;
			if (text == null)
			{
				text = "";
			}
			text = text.Trim();
			if (text.Length == 0)
			{
				CloseModal();
				return;
			}
			MainView main = MainView.Self;
			if (main != null)
			{
				main.RenameActiveLayer(text);
			}
			CloseModal();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}
	}
}
