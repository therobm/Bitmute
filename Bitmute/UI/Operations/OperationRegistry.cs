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

		private static uint ChordKey(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			return ((uint)key << 8) | (uint)modifiers;
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
	}
}
