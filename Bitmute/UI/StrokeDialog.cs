using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI
{
	public class StrokeDialog : FieldDialog
	{
		private IntSlider m_widthField;
		private ListPicker m_positionPicker;

		private void OnApplyClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int position = m_positionPicker.SelectedIndex();
			if (position < 0)
			{
				position = 1;
			}
			main.ApplyStroke(m_widthField.Value(), position);
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		public StrokeDialog()
		{
			m_widthField = new IntSlider("Width", 1, 100, 2, "px", null);
			m_positionPicker = new ListPicker("Position", new string[] { "Inside", "Center", "Outside" }, 1, null);

			AddField(m_widthField);
			AddField(m_positionPicker);
			AddField(new NoteField("Strokes with the foreground color"));

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("Stroke", OnApplyClicked);
			ComposeFields("Stroke", ButtonRow(cancelButton, applyButton));
		}
	}
}
