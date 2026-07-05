using Microsoft.Maui.Controls;

namespace Bitmute.UI.Dialogs
{
	public abstract class FieldDialog : ModalDialog
	{
		private VerticalStackLayout m_fieldStack;

		private void EnsureFieldStack()
		{
			if (m_fieldStack == null)
			{
				m_fieldStack = new VerticalStackLayout();
				m_fieldStack.Spacing = UiConstants.DialogRowSpacing;
			}
		}

		protected void AddField(View field)
		{
			EnsureFieldStack();
			m_fieldStack.Add(field);
		}

		protected void ComposeFields(string title, View buttonRow)
		{
			EnsureFieldStack();
			ComposeDialog(title, m_fieldStack, buttonRow);
		}

		protected void ComposeFields(string title, View buttonRow, double width)
		{
			EnsureFieldStack();
			m_fieldStack.WidthRequest = width;
			ComposeDialog(title, m_fieldStack, buttonRow);
		}
	}
}
