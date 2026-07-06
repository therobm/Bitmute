using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Bitmute.UI.Dialogs
{
	public class MessageDialog : ModalDialog
	{
		private Action<int> m_onChoice;
		private Button[] m_buttons;

		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
		{
			int choice = -1;
			for (int index = 0; index < m_buttons.Length; index++)
			{
				if (ReferenceEquals(m_buttons[index], sender))
				{
					choice = index;
					break;
				}
			}
			Action<int> callback = m_onChoice;
			base.OnPrimaryClicked(sender, eventArgs);
			if (choice >= 0 && callback != null)
			{
				callback(choice);
			}
		}

		public MessageDialog(string title, string message, string[] buttonLabels, Action<int> onChoice)
		{
			m_onChoice = onChoice;

			Label messageLabel = new Label();
			messageLabel.Text = message;
			messageLabel.FontSize = UiConstants.PanelFontSize;
			messageLabel.LineBreakMode = LineBreakMode.WordWrap;
			messageLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = UiConstants.DialogRowSpacing;
			body.Add(messageLabel);

			m_buttons = new Button[buttonLabels.Length];
			HorizontalStackLayout buttons = new HorizontalStackLayout();
			buttons.Spacing = UiConstants.DialogRowSpacing;
			buttons.HorizontalOptions = LayoutOptions.End;
			for (int index = 0; index < buttonLabels.Length; index++)
			{
				Button button;
				if (index == buttonLabels.Length - 1)
				{
					button = PrimaryButton(buttonLabels[index]);
				}
				else
				{
					button = SecondaryButton(buttonLabels[index]);
				}
				m_buttons[index] = button;
				buttons.Add(button);
			}

			ComposeDialog(title, body, buttons);
		}
	}
}
