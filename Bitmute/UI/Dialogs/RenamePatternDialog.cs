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

		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
		{
			string text = m_nameField.Text().Trim();
			if (text.Length == 0)
			{
				base.OnPrimaryClicked(sender, eventArgs);
				return;
			}
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ApplyRenamePattern(m_pattern, text);
			}
			base.OnPrimaryClicked(sender, eventArgs);
		}

		public RenamePatternDialog(Pattern pattern)
		{
			m_pattern = pattern;
			m_nameField = new TextField("Name", pattern.m_name, null);
			AddField(m_nameField);

			Button cancelButton = SecondaryButton("Cancel");
			Button okButton = PrimaryButton("OK");
			ComposeFields("Rename Pattern", ButtonRow(cancelButton, okButton));
		}
	}
}
