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
				main.ApplyRenameBrush(m_brush, text);
			}
			base.OnPrimaryClicked(sender, eventArgs);
		}

		public RenameBrushDialog(CustomBrush brush)
		{
			m_brush = brush;
			m_nameField = new TextField("Name", brush.m_name, null);
			AddField(m_nameField);

			Button cancelButton = SecondaryButton("Cancel");
			Button okButton = PrimaryButton("OK");
			ComposeFields("Rename Brush", ButtonRow(cancelButton, okButton));
		}
	}
}
