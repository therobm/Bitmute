using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI
{
	public class AdjustmentDialog : PreviewDialog
	{
		private Adjustment m_adjustment;
		private IntSlider[] m_sliders;
		private ListPicker[] m_pickers;

		private int[] Values()
		{
			int[] values = new int[m_sliders.Length + m_pickers.Length];
			for (int index = 0; index < m_sliders.Length; index++)
			{
				values[index] = m_sliders[index].Value();
			}
			for (int index = 0; index < m_pickers.Length; index++)
			{
				values[m_sliders.Length + index] = m_pickers[index].SelectedIndex();
			}
			return values;
		}

		private void OnSliderValue(int value)
		{
			NotifyFieldChanged();
		}

		private void OnPickerValue(int index)
		{
			NotifyFieldChanged();
		}

		protected override void RunPreview()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.PreviewAdjustment(m_adjustment, Values());
		}

		protected override void RestorePreview()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.RestoreAdjustmentPreview();
		}

		protected override void AbandonPreview()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.CancelAdjustment();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		private void OnApplyClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int[] values = Values();
			if (m_adjustment.m_previewable)
			{
				MarkApplied();
				main.CommitAdjustment(m_adjustment, values);
			}
			else
			{
				main.ApplyAdjustment(m_adjustment, values);
			}
			CloseModal();
		}

		public AdjustmentDialog(Adjustment adjustment)
		{
			m_adjustment = adjustment;
			m_sliders = new IntSlider[adjustment.m_sliderLabels.Length];
			for (int index = 0; index < m_sliders.Length; index++)
			{
				m_sliders[index] = new IntSlider(adjustment.m_sliderLabels[index], adjustment.m_sliderMinimums[index], adjustment.m_sliderMaximums[index], adjustment.m_sliderDefaults[index], "", OnSliderValue);
				AddField(m_sliders[index]);
			}
			m_pickers = new ListPicker[adjustment.m_choiceLabels.Length];
			for (int index = 0; index < m_pickers.Length; index++)
			{
				m_pickers[index] = new ListPicker(adjustment.m_choiceLabels[index], adjustment.m_choiceOptions[index], adjustment.m_choiceDefaults[index], OnPickerValue);
				AddField(m_pickers[index]);
			}
			if (adjustment.m_previewable)
			{
				AddPreviewField();
			}
			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("Apply", OnApplyClicked);
			ComposeFields(adjustment.m_name, ButtonRow(cancelButton, applyButton));
		}
	}
}
