using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class LayerPropertiesDialog : FieldDialog
	{
		private TextField m_nameField;

		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
		{
			string text = m_nameField.Text().Trim();
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
			base.OnPrimaryClicked(sender, eventArgs);
		}


		public LayerPropertiesDialog(string currentName)
		{
			m_nameField = new TextField("Name", currentName, null);
			AddField(m_nameField);

			Button cancelButton = SecondaryButton("Cancel");
			Button okButton = PrimaryButton("OK");
			ComposeFields("Layer Properties", ButtonRow(cancelButton, okButton));
		}
	}
}
