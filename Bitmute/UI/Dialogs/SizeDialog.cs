using System;
using Microsoft.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class SizeDialog : FieldDialog
	{
		private const int MaximumSize = 8192;

		private bool m_canvasMode;
		private DualIntField m_sizeField;
		private AnchorGrid m_anchorGrid;
		private ListPicker m_interpolation;

		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			int width = m_sizeField.FirstValue();
			int height = m_sizeField.SecondValue();
			if (m_canvasMode)
			{
				main.ApplyCanvasSize(width, height, m_anchorGrid.HorizontalAnchor(), m_anchorGrid.VerticalAnchor());
			}
			else
			{
				main.ApplyImageSize(width, height, m_interpolation.SelectedIndex());
			}
			base.OnPrimaryClicked(sender, eventArgs);
		}

		public SizeDialog(string title, bool canvasMode, int currentWidth, int currentHeight)
		{
			m_canvasMode = canvasMode;
			m_sizeField = new DualIntField("Width", "Height", currentWidth, currentHeight, 1, MaximumSize, " px", null);
			AddField(m_sizeField);

			if (canvasMode)
			{
				m_anchorGrid = new AnchorGrid("Anchor", 1, 1);
				AddField(m_anchorGrid);
			}
			else
			{
				m_interpolation = new ListPicker("Resample", new string[] { "Nearest", "Bilinear", "Bicubic" }, 2, null);
				AddField(m_interpolation);
			}

			Button cancelButton = SecondaryButton("Cancel");
			Button applyButton = PrimaryButton("Apply");
			ComposeFields(title, ButtonRow(cancelButton, applyButton));
		}
	}
}
