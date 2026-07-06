using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Windows.System;

namespace Bitmute.UI.Operations
{
	public class Operation
	{
		public eOperation m_operation;
		private string m_originalName;
		private string m_visibleName;
		private Action<VirtualKeyModifiers> m_onTrigger;
		private Accelerator m_accelerator;
		public Operation(eOperation operation, string name, Action<VirtualKeyModifiers> onTrigger)
		{
			m_operation = operation;
			m_originalName = m_visibleName = name;
			m_onTrigger = onTrigger;
		}
		public string GetName()
		{
			return m_visibleName;
		}
		public string GetAcceleratorText()
		{
			if (m_accelerator != null)
				return m_accelerator.GetText();
			return "";
		}
		public void AssignHotkey(Accelerator accelerator)
		{
			m_accelerator = accelerator;
			m_visibleName = m_originalName + accelerator.GetModifierText();
		}
		public void Trigger(VirtualKeyModifiers additionalModifiers)
		{
			if (m_onTrigger != null)
				m_onTrigger(additionalModifiers);
		}
	}
	public class OperationRegistry
	{
		private Dictionary<eOperation, Operation> m_operations = new Dictionary<eOperation, Operation>();
		private MainView m_main;

		public OperationRegistry(MainView mainView)
		{
			m_main = mainView;
			SetupOperations();
		}

		private void SetupOperations()
		{
			RegisterOperation(eOperation.ToggleRulers, TriggerToggleRulers);
			RegisterOperation(eOperation.MergeVisibleLayers, TriggerMergeVisible);
			RegisterOperation(eOperation.NewDocument, TriggerNewDocument);
			RegisterOperation(eOperation.Open, TriggerOpen);
			RegisterOperation(eOperation.Save, TriggerSave);
			RegisterOperation(eOperation.SaveAs, TriggerSaveAs);
			RegisterOperation(eOperation.ExportAs, TriggerExportAs);
			RegisterOperation(eOperation.Undo, TriggerUndo);
			RegisterOperation(eOperation.Redo, TriggerRedo);
			RegisterOperation(eOperation.UndoStep, TriggerUndoStep);
			RegisterOperation(eOperation.RedoStep, TriggerRedoStep);
			RegisterOperation(eOperation.SelectAll, TriggerSelectAll);
			RegisterOperation(eOperation.Deselect, TriggerDeselect);
			RegisterOperation(eOperation.Cut, TriggerCut);
			RegisterOperation(eOperation.Copy, TriggerCopy);
			RegisterOperation(eOperation.CopyMerged, TriggerCopyMerged);
			RegisterOperation(eOperation.Paste, TriggerPaste);
			RegisterOperation(eOperation.PasteInto, TriggerPasteInto);
			RegisterOperation(eOperation.FitOnScreen, TriggerFitOnScreen);
			RegisterOperation(eOperation.DuplicateLayer, TriggerDuplicateLayer);
			RegisterOperation(eOperation.MergeDown, TriggerMergeDown);
			RegisterOperation(eOperation.ImageSize, TriggerImageSize);
			RegisterOperation(eOperation.InvertColors, TriggerInvertColors);
			RegisterOperation(eOperation.InvertSelection, TriggerInvertSelection);
			RegisterOperation(eOperation.FreeTransform, TriggerFreeTransform);
			RegisterOperation(eOperation.LastFilter, TriggerLastFilter);
			RegisterOperation(eOperation.SwapColors, TriggerSwapColors);
		}

		public void RegisterOperation(eOperation operation, Action<VirtualKeyModifiers> onTrigger)
		{
			string visibleName = PrettifyName(operation.ToString());
			Operation op = new Operation(operation, visibleName, onTrigger);
			m_operations[operation] = op;
		}

		public void AssignAccelerator(eOperation operation, Accelerator accelerator)
		{
			if (!m_operations.ContainsKey(operation))
			{
				return;
			}
			m_operations[operation].AssignHotkey(accelerator);
		}

		public Operation Get(eOperation operation)
		{
			if (m_operations.ContainsKey(operation))
			{
				return m_operations[operation];
			}
			return null;
		}

		public string VisibleName(eOperation operation)
		{
			Operation op = Get(operation);
			if (op == null)
			{
				return "";
			}
			return op.GetName();
		}

		public string GetAcceleratorText(eOperation operation)
		{
			Operation op = Get(operation);
			
			if (op == null)
			{
				return "";
			}
			return op.GetAcceleratorText();
		}

		private static string PrettifyName(string raw)
		{
			if (string.IsNullOrEmpty(raw))
			{
				return raw;
			}
			StringBuilder builder = new StringBuilder();
			builder.Append(raw[0]);
			for (int index = 1; index < raw.Length; index++)
			{
				char character = raw[index];
				if (char.IsUpper(character))
				{
					builder.Append(' ');
				}
				builder.Append(character);
			}
			return builder.ToString();
		}


		////////////////////////////////////////////////////
		///  Operation Definitions 
		////////////////////////////////////////////////////
		

		private void TriggerToggleRulers(VirtualKeyModifiers modifiers)
		{
			m_main.ToggleRulers();
		}

		private void TriggerMergeVisible(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoMergeVisible();
		}

		private void TriggerNewDocument(VirtualKeyModifiers modifiers)
		{
			m_main.ShowNewDocumentDialog();
		}

		private void TriggerOpen(VirtualKeyModifiers modifiers)
		{
			m_main.OpenImageFlow();
		}

		private void TriggerSave(VirtualKeyModifiers modifiers)
		{
			m_main.SaveImageFlow();
		}

		private void TriggerSaveAs(VirtualKeyModifiers modifiers)
		{
			m_main.SaveAsFlow();
		}

		private void TriggerExportAs(VirtualKeyModifiers modifiers)
		{
			m_main.OpenExportDialog();
		}

		private void TriggerUndo(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoUndoToggle();
		}

		private void TriggerRedo(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoRedo();
		}

		private void TriggerUndoStep(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoUndo();
		}

		private void TriggerRedoStep(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoRedo();
		}

		private void TriggerSelectAll(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoSelectAll();
		}

		private void TriggerDeselect(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoDeselect();
		}

		private void TriggerCut(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoCut();
		}

		private void TriggerCopy(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoCopy();
		}

		private void TriggerCopyMerged(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoCopyMerged();
		}

		private void TriggerPaste(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoPaste();
		}

		private void TriggerPasteInto(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoPasteInto();
		}

		private void TriggerFitOnScreen(VirtualKeyModifiers modifiers)
		{
			m_main.DoFit();
		}

		private void TriggerDuplicateLayer(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DuplicateActiveLayer();
		}

		private void TriggerMergeDown(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.MergeSelectedLayers();
		}

		private void TriggerImageSize(VirtualKeyModifiers modifiers)
		{
			m_main.OpenSizeDialog(false);
		}

		private void TriggerInvertColors(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoInvert();
		}

		private void TriggerInvertSelection(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoInvertSelection();
		}

		private void TriggerFreeTransform(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.BeginTransform(0);
		}

		private void TriggerLastFilter(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.ApplyLastFilter();
		}

		private void TriggerSwapColors(VirtualKeyModifiers modifiers)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.SwapToolColors();
		}
	}
}
