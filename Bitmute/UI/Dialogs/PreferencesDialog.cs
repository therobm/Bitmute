using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class PreferencesDialog : FieldDialog
	{
		private const int UndoDepthMinimum = 10;
		private const int UndoDepthMaximum = 500;
		private const double SectionIndent = 10.0;

		private IntSlider m_undoDepthField;
		private RadioPicker m_themePicker;

		private void OnClearRecentClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ClearRecentFiles();
			}
		}

		private void OnThemeChanged(int index)
		{
			if (index == 0)
			{
				Theme.UseSystem();
			}
			else if (index == 1)
			{
				Theme.UseDark();
			}
			else if (index == 2)
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

			m_undoDepthField = new IntSlider("Undo depth", UndoDepthMinimum, UndoDepthMaximum, initialDepth, "", null);

			int themeIndex = 2;
			if (Theme.FollowSystem())
			{
				themeIndex = 0;
			}
			else if (Theme.IsDark())
			{
				themeIndex = 1;
			}
			m_themePicker = new RadioPicker("Theme", new string[] { "System", "Dark", "Light" }, themeIndex, OnThemeChanged);

			Button clearRecentButton = SecondaryButton("Clear Recent Files", OnClearRecentClicked);
			clearRecentButton.WidthRequest = 150.0;
			clearRecentButton.HorizontalOptions = LayoutOptions.Start;

			m_undoDepthField.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);
			clearRecentButton.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);
			m_themePicker.Margin = new Thickness(SectionIndent, 0.0, 0.0, 0.0);

			AddField(new SectionHeader("General"));
			AddField(m_undoDepthField);
			AddField(clearRecentButton);
			AddField(new SectionHeader("Interface"));
			AddField(m_themePicker);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button okButton = PrimaryButton("OK", OnOkClicked);
			ComposeFields("Preferences", ButtonRow(cancelButton, okButton), 340.0 - (2.0 * UiConstants.DialogPadding));
		}
	}
}
