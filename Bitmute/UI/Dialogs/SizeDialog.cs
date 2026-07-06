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
		private ListPicker m_horizontalAnchor;
		private ListPicker m_verticalAnchor;
		private ListPicker m_interpolation;

		private static int AnchorValue(ListPicker picker)
		{
			int index = picker.SelectedIndex();
			if (index == 0)
			{
				return -1;
			}
			if (index == 2)
			{
				return 1;
			}
			return 0;
		}



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
				main.ApplyCanvasSize(width, height, AnchorValue(m_horizontalAnchor), AnchorValue(m_verticalAnchor));
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
				m_horizontalAnchor = new ListPicker("Anchor X", new string[] { "Left", "Center", "Right" }, 1, null);
				m_verticalAnchor = new ListPicker("Anchor Y", new string[] { "Top", "Middle", "Bottom" }, 1, null);
				AddField(m_horizontalAnchor);
				AddField(m_verticalAnchor);
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
