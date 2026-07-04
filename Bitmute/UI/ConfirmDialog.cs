using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Bitmute.UI
{
	public class ConfirmDialog : ModalDialog
	{
		private System.Action m_onConfirm;

		public ConfirmDialog(string title, string message, string confirmText, System.Action onConfirm)
		{
			m_onConfirm = onConfirm;

			Label messageLabel = new Label();
			messageLabel.Text = message;
			messageLabel.FontSize = 13.0;
			messageLabel.LineBreakMode = LineBreakMode.WordWrap;
			messageLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 10.0;
			body.Add(messageLabel);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button confirmButton = PrimaryButton(confirmText, OnConfirmClicked);
			ComposeDialog(title, body, ButtonRow(cancelButton, confirmButton));
		}

		private void OnConfirmClicked(object sender, EventArgs eventArgs)
		{
			System.Action callback = m_onConfirm;
			CloseModal();
			if (callback != null)
			{
				callback();
			}
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}
	}
}
