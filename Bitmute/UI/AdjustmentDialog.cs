using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI
{
	public class AdjustmentDialog : PreviewDialog
	{
		private string m_filterId;
		private IntSlider[] m_sliders;
		private bool m_previewable;

		private int[] Values()
		{
			int[] values = new int[m_sliders.Length];
			for (int index = 0; index < m_sliders.Length; index++)
			{
				values[index] = m_sliders[index].Value();
			}
			return values;
		}

		private void OnSliderValue(int value)
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

		public AdjustmentDialog(string title, string filterId, string[] labels, int[] minimums, int[] maximums, int[] defaults)
		{
			m_filterId = filterId;
			m_previewable = MainView.IsAdjustmentPreviewable(filterId);
			m_sliders = new IntSlider[labels.Length];
			for (int index = 0; index < labels.Length; index++)
			{
				m_sliders[index] = new IntSlider(labels[index], minimums[index], maximums[index], defaults[index], "", OnSliderValue);
				AddField(m_sliders[index]);
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
