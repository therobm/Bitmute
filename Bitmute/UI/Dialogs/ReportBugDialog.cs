using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class ReportBugDialog : FieldDialog
	{
		private TextField m_titleField;
		private Editor m_descriptionEditor;

		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
		{
			string title = m_titleField.Text().Trim();
			if (title.Length == 0)
			{
				base.OnPrimaryClicked(sender, eventArgs);
				return;
			}
			string description = m_descriptionEditor.Text;
			if (description == null)
			{
				description = "";
			}
			MainView main = MainView.Self;
			if (main != null)
			{
				main.SubmitBugReport(title, description);
			}
			base.OnPrimaryClicked(sender, eventArgs);
		}

		public ReportBugDialog()
		{
			m_titleField = new TextField("Title", "", null);
			AddField(m_titleField);

			Label descriptionCaption = new Label();
			descriptionCaption.Text = "What happened?";
			descriptionCaption.FontSize = UiConstants.PanelFontSize;
			descriptionCaption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);

			m_descriptionEditor = new Editor();
			m_descriptionEditor.FontSize = UiConstants.PanelFontSize;
			m_descriptionEditor.AutoSize = EditorAutoSizeOption.Disabled;
			m_descriptionEditor.HeightRequest = 140.0;
			m_descriptionEditor.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_descriptionEditor.ThemeBg(UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);

			VerticalStackLayout descriptionBlock = new VerticalStackLayout();
			descriptionBlock.Spacing = UiConstants.DialogRowSpacing;
			descriptionBlock.Add(descriptionCaption);
			descriptionBlock.Add(m_descriptionEditor);
			AddField(descriptionBlock);

			Button cancelButton = SecondaryButton("Cancel");
			Button submitButton = PrimaryButton("Send");
			ComposeFields("Report a Bug", ButtonRow(cancelButton, submitButton));
		}
	}
}
