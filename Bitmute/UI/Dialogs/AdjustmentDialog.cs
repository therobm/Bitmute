using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class AdjustmentDialog : PreviewDialog
	{
		private Adjustment m_adjustment;
		private IntSlider[] m_sliders;
		private FloatSlider[] m_floatSliders;
		private CheckField[] m_checks;
		private ListPicker[] m_pickers;

		private int ScaleFloatValue(float value, int decimals)
		{
			int scale = 1;
			for (int index = 0; index < decimals; index++)
			{
				scale = scale * 10;
			}
			return (int)Math.Round((double)value * scale);
		}

		private int[] Values()
		{
			int[] values = new int[m_sliders.Length + m_floatSliders.Length + m_checks.Length + m_pickers.Length];
			int offset = 0;
			for (int index = 0; index < m_sliders.Length; index++)
			{
				values[offset + index] = m_sliders[index].Value();
			}
			offset = offset + m_sliders.Length;
			for (int index = 0; index < m_floatSliders.Length; index++)
			{
				values[offset + index] = ScaleFloatValue(m_floatSliders[index].Value(), m_adjustment.m_floatSliderDecimals[index]);
			}
			offset = offset + m_floatSliders.Length;
			for (int index = 0; index < m_checks.Length; index++)
			{
				int checkedValue = 0;
				if (m_checks[index].Checked())
				{
					checkedValue = 1;
				}
				values[offset + index] = checkedValue;
			}
			offset = offset + m_checks.Length;
			for (int index = 0; index < m_pickers.Length; index++)
			{
				values[offset + index] = m_pickers[index].SelectedIndex();
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

		private void OnFloatValue(float value)
		{
			NotifyFieldChanged();
		}

		private void OnCheckValue(bool value)
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



		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
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
			base.OnPrimaryClicked(sender, eventArgs);
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
			m_floatSliders = new FloatSlider[adjustment.m_floatSliderLabels.Length];
			for (int index = 0; index < m_floatSliders.Length; index++)
			{
				m_floatSliders[index] = new FloatSlider(adjustment.m_floatSliderLabels[index], adjustment.m_floatSliderMinimums[index], adjustment.m_floatSliderMaximums[index], adjustment.m_floatSliderDefaults[index], adjustment.m_floatSliderDecimals[index], "", OnFloatValue);
				AddField(m_floatSliders[index]);
			}
			m_checks = new CheckField[adjustment.m_checkLabels.Length];
			for (int index = 0; index < m_checks.Length; index++)
			{
				m_checks[index] = new CheckField(adjustment.m_checkLabels[index], adjustment.m_checkDefaults[index], OnCheckValue);
				AddField(m_checks[index]);
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
			Button cancelButton = SecondaryButton("Cancel");
			Button applyButton = PrimaryButton("Apply");
			ComposeFields(adjustment.m_name, ButtonRow(cancelButton, applyButton));
			if (adjustment.m_previewable)
			{
				RunPreview();
			}
		}
	}
}
