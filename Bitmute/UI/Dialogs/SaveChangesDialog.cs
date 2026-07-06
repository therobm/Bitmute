using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Bitmute.UI.Dialogs
{
	public class SaveChangesDialog : ModalDialog
	{
		public SaveChangesDialog(string documentTitle)
		{
			Label message = new Label();
			message.Text = "Save changes to " + documentTitle + "?";
			message.FontSize = 13.0;
			message.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 10.0;
			body.WidthRequest = 320.0;
			body.Add(message);

			Button saveButton = PrimaryButton("Save");
			Button dontSaveButton = CreateButton("Don't Save", OnDontSaveClicked);
			Button cancelButton = SecondaryButton("Cancel");
			HorizontalStackLayout buttons = new HorizontalStackLayout();
			buttons.Spacing = 8.0;
			buttons.HorizontalOptions = LayoutOptions.End;
			buttons.Add(cancelButton);
			buttons.Add(dontSaveButton);
			buttons.Add(saveButton);
			ComposeDialog("Save Changes", body, buttons);
		}

		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.OnCloseSaveChanges();
			}
			base.OnPrimaryClicked(sender, eventArgs);
		}

		private void OnDontSaveClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.OnCloseDontSave();
			}
		}

		
		protected override void OnSecondaryClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.OnCloseCancelSave();
			}
			base.OnSecondaryClicked(sender, eventArgs);
		}

	}
}
