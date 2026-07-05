using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI
{
	public class AdjustmentDialog : PreviewDialog
	{
		private static readonly string[] s_noChoiceLabels = new string[0];
		private static readonly string[][] s_noChoiceOptions = new string[0][];
		private static readonly int[] s_noChoiceDefaults = new int[0];

		private string m_filterId;
		private IntSlider[] m_sliders;
		private ListPicker[] m_pickers;
		private bool m_previewable;

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
			main.PreviewAdjustment(m_filterId, Values());
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
			if (m_previewable)
			{
				MarkApplied();
				main.CommitAdjustment(m_filterId, values);
			}
			else
			{
				main.ApplyAdjustment(m_filterId, values);
			}
			CloseModal();
		}

		public AdjustmentDialog(string title, string filterId, string[] labels, int[] minimums, int[] maximums, int[] defaults) : this(title, filterId, labels, minimums, maximums, defaults, s_noChoiceLabels, s_noChoiceOptions, s_noChoiceDefaults)
		{
		}

		public AdjustmentDialog(string title, string filterId, string[] labels, int[] minimums, int[] maximums, int[] defaults, string[] choiceLabels, string[][] choiceOptions, int[] choiceDefaults)
		{
			m_filterId = filterId;
			m_previewable = MainView.IsAdjustmentPreviewable(filterId);
			m_sliders = new IntSlider[labels.Length];
			for (int index = 0; index < labels.Length; index++)
			{
				m_sliders[index] = new IntSlider(labels[index], minimums[index], maximums[index], defaults[index], "", OnSliderValue);
				AddField(m_sliders[index]);
			}
			m_pickers = new ListPicker[choiceLabels.Length];
			for (int index = 0; index < choiceLabels.Length; index++)
			{
				m_pickers[index] = new ListPicker(choiceLabels[index], choiceOptions[index], choiceDefaults[index], OnPickerValue);
				AddField(m_pickers[index]);
			}
			if (m_previewable)
			{
				AddPreviewField();
			}
			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button applyButton = PrimaryButton("Apply", OnApplyClicked);
			ComposeFields(title, ButtonRow(cancelButton, applyButton));
		}
	}
}
