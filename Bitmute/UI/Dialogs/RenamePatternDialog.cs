using System;
using Microsoft.Maui.Controls;
using Bitmute.Imaging;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class RenamePatternDialog : FieldDialog
	{
		private Pattern m_pattern;
		private TextField m_nameField;

		private void OnOkClicked(object sender, EventArgs eventArgs)
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
				main.ApplyRenamePattern(m_pattern, text);
			}
			CloseModal();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		public RenamePatternDialog(Pattern pattern)
		{
			m_pattern = pattern;
			m_nameField = new TextField("Name", pattern.m_name, null);
			AddField(m_nameField);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button okButton = PrimaryButton("OK", OnOkClicked);
			ComposeFields("Rename Pattern", ButtonRow(cancelButton, okButton));
		}
	}
}
