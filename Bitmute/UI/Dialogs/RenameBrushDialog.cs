using System;
using Microsoft.Maui.Controls;
using Bitmute.Tools;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class RenameBrushDialog : FieldDialog
	{
		private CustomBrush m_brush;
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
				main.ApplyRenameBrush(m_brush, text);
			}
			CloseModal();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		public RenameBrushDialog(CustomBrush brush)
		{
			m_brush = brush;
			m_nameField = new TextField("Name", brush.m_name, null);
			AddField(m_nameField);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button okButton = PrimaryButton("OK", OnOkClicked);
			ComposeFields("Rename Brush", ButtonRow(cancelButton, okButton));
		}
	}
}
