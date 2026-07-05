using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class NewDocumentDialog : FieldDialog
	{
		private const int DefaultWidth = 800;
		private const int DefaultHeight = 600;
		private const int MaximumSize = 8192;

		private TextField m_nameField;
		private DualIntField m_sizeField;
		private ListPicker m_backgroundPicker;

		private void OnCreateClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			bool transparent = m_backgroundPicker.SelectedIndex() == 1;
			main.CreateNewDocument(m_sizeField.FirstValue(), m_sizeField.SecondValue(), m_nameField.Text(), transparent);
			CloseModal();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		public NewDocumentDialog() : this(DefaultWidth, DefaultHeight)
		{
		}

		public NewDocumentDialog(int initialWidth, int initialHeight)
		{
			if (initialWidth < 1 || initialWidth > MaximumSize)
			{
				initialWidth = DefaultWidth;
			}
			if (initialHeight < 1 || initialHeight > MaximumSize)
			{
				initialHeight = DefaultHeight;
			}
			m_nameField = new TextField("Name", "Untitled", null);
			m_sizeField = new DualIntField("Width", "Height", initialWidth, initialHeight, 1, MaximumSize, " px", null);
			m_backgroundPicker = new ListPicker("Background", new string[] { "White", "Transparent" }, 0, null);

			AddField(m_nameField);
			AddField(m_sizeField);
			AddField(m_backgroundPicker);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button createButton = PrimaryButton("Create", OnCreateClicked);
			ComposeFields("New Document", ButtonRow(cancelButton, createButton));
		}
	}
}
