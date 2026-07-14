using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Windows.System;
using Bitmute.Tools;

namespace Bitmute.UI.Operations
{
	public class Operation
	{
		public eOperation m_operation;
		private string m_originalName;
		private string m_visibleName;
		private Func<Chord, bool> m_onTrigger;
		private Accelerator m_accelerator;
		public Operation(eOperation operation, string name, Func<Chord, bool> onTrigger)
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
		public bool Trigger(Chord chord)
		{
			if (m_onTrigger != null)
				return m_onTrigger(chord);
			return false;
		}
	}
	public class OperationRegistry
	{
		private Dictionary<eOperation, Operation> m_operations = new Dictionary<eOperation, Operation>();
		private MainView m_main;
		private static Dictionary<VirtualKey, eTool> s_toolKeys = new Dictionary<VirtualKey, eTool>()
		{
			{ VirtualKey.M, eTool.Select },
			{ VirtualKey.V, eTool.Move },
			{ VirtualKey.L, eTool.Lasso },
			{ VirtualKey.W, eTool.MagicWand },
			{ VirtualKey.C, eTool.Crop },
			{ VirtualKey.B, eTool.Brush },
			{ VirtualKey.S, eTool.Clone },
			{ VirtualKey.E, eTool.Eraser },
			{ VirtualKey.G, eTool.Fill },
			{ VirtualKey.O, eTool.Dodge },
			{ VirtualKey.R, eTool.Blur },
			{ VirtualKey.T, eTool.Text },
			{ VirtualKey.U, eTool.Line },
			{ VirtualKey.I, eTool.Eyedropper },
			{ VirtualKey.H, eTool.Hand },
			{ VirtualKey.Z, eTool.Zoom },
			{ VirtualKey.P, eTool.Pen },
			{ VirtualKey.A, eTool.DirectSelect },
		};

		public OperationRegistry(MainView mainView)
		{
			m_main = mainView;
			SetupOperations();
		}

		private void SetupOperations()
		{
			RegisterOperation(eOperation.ToolChange, TriggerToolChange);
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
			RegisterOperation(eOperation.NewLayer, TriggerNewLayer);
			RegisterOperation(eOperation.DuplicateLayer, TriggerDuplicateLayer);
			RegisterOperation(eOperation.MergeDown, TriggerMergeDown);
			RegisterOperation(eOperation.ImageSize, TriggerImageSize);
			RegisterOperation(eOperation.InvertColors, TriggerInvertColors);
			RegisterOperation(eOperation.InvertSelection, TriggerInvertSelection);
			RegisterOperation(eOperation.FreeTransform, TriggerFreeTransform);
			RegisterOperation(eOperation.LastFilter, TriggerLastFilter);
			RegisterOperation(eOperation.SwapColors, TriggerSwapColors);
			RegisterOperation(eOperation.ZoomIn, TriggerZoomIn);
			RegisterOperation(eOperation.ZoomOut, TriggerZoomOut);
			RegisterOperation(eOperation.Delete, TriggerDelete);
			RegisterOperation(eOperation.CommitArmed, TriggerCommitArmed);
			RegisterOperation(eOperation.CancelArmed, TriggerCancelArmed);
		}

		public void RegisterOperation(eOperation operation, Func<Chord, bool> onTrigger)
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


		private bool TriggerToolChange(Chord chord)
		{
			if (m_main.ToolKeyBlocked())
			{
				return false;
			}
			bool cycle = (chord.m_modifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift;
			eTool tool;
			bool found = s_toolKeys.TryGetValue(chord.m_key, out tool);
			if (!found)
			{
				return false;
			}
			m_main.SelectToolKey(tool, cycle);
			return true;
		}

		public static string ShortcutForTool(eTool tool)
		{
			foreach (KeyValuePair<VirtualKey, eTool> pair in s_toolKeys)
			{
				if (pair.Value == tool)
				{
					return Accelerator.KeyName(pair.Key);
				}
			}
			return "";
		}

		private bool TriggerToggleRulers(Chord chord)
		{
			m_main.ToggleRulers();
			return true;
		}

		private bool TriggerMergeVisible(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoMergeVisible();
			return true;
		}

		private bool TriggerNewDocument(Chord chord)
		{
			m_main.ShowNewDocumentDialog();
			return true;
		}

		private bool TriggerOpen(Chord chord)
		{
			m_main.OpenImageFlow();
			return true;
		}

		private bool TriggerSave(Chord chord)
		{
			m_main.SaveImageFlow();
			return true;
		}

		private bool TriggerSaveAs(Chord chord)
		{
			m_main.SaveAsFlow();
			return true;
		}

		private bool TriggerExportAs(Chord chord)
		{
			m_main.OpenExportDialog();
			return true;
		}

		private bool TriggerUndo(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoUndoToggle();
			return true;
		}

		private bool TriggerRedo(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoRedo();
			return true;
		}

		private bool TriggerUndoStep(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoUndo();
			return true;
		}

		private bool TriggerRedoStep(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoRedo();
			return true;
		}

		private bool TriggerSelectAll(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoSelectAll();
			return true;
		}

		private bool TriggerDeselect(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoDeselect();
			return true;
		}

		private bool TriggerCut(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoCut();
			return true;
		}

		private bool TriggerCopy(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoCopy();
			return true;
		}

		private bool TriggerCopyMerged(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoCopyMerged();
			return true;
		}

		private bool TriggerPaste(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoPaste();
			return true;
		}

		private bool TriggerPasteInto(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoPasteInto();
			return true;
		}

		private bool TriggerFitOnScreen(Chord chord)
		{
			m_main.DoFit();
			return true;
		}

		private bool TriggerNewLayer(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.AddNewLayer();
			return true;
		}

		private bool TriggerDuplicateLayer(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DuplicateActiveLayer();
			return true;
		}

		private bool TriggerMergeDown(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.MergeSelectedLayers();
			return true;
		}

		private bool TriggerImageSize(Chord chord)
		{
			m_main.OpenSizeDialog(false);
			return true;
		}

		private bool TriggerInvertColors(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoInvert();
			return true;
		}

		private bool TriggerInvertSelection(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.DoInvertSelection();
			return true;
		}

		private bool TriggerFreeTransform(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.BeginTransform(0);
			return true;
		}

		private bool TriggerLastFilter(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.ApplyLastFilter();
			return true;
		}

		private bool TriggerSwapColors(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			m_main.SwapToolColors();
			return true;
		}

		private bool TriggerZoomIn(Chord chord)
		{
			m_main.DoZoomIn();
			return true;
		}

		private bool TriggerZoomOut(Chord chord)
		{
			m_main.DoZoomOut();
			return true;
		}

		private bool TriggerDelete(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			bool handledPathAnchor = m_main.DeleteSelectedPathAnchor();
			if (handledPathAnchor)
			{
				return true;
			}
			if ((chord.m_modifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control)
			{
				m_main.FillSelectionWithBackground();
			}
			else if ((chord.m_modifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu)
			{
				m_main.FillSelectionWithForeground();
			}
			else
			{
				m_main.DoClearSelection();
			}
			return true;
		}

		private bool TriggerCommitArmed(Chord chord)
		{
			return m_main.CommitArmedOperation();
		}

		private bool TriggerCancelArmed(Chord chord)
		{
			if (m_main.IsTextEditActive())
			{
				return false;
			}
			if (m_main.HasOpenModal())
			{
				m_main.CloseModal();
				return true;
			}
			return m_main.CancelArmedOperation();
		}
	}
}
