using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;

namespace Bitmute.UI.Operations
{
	public class Operation
	{
		public eOperation m_operation;
		private string m_originalName;
		public string m_visibleName;
		public Action<VirtualKeyModifiers> m_onTrigger;
		public Accelerator m_accelerator;
		public Operation(eOperation operation, string name, Action<VirtualKeyModifiers> onTrigger)
		{
			m_operation = operation;
			m_originalName = m_visibleName = name;
			m_onTrigger = onTrigger;
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
		Dictionary<eOperation, Operation> m_operations = new Dictionary<eOperation, Operation>();

		public void RegisterOperation(eOperation operation, Action<VirtualKeyModifiers> onTrigger)
		{
			//todo parse out spaces, etc..
			string visibleName = operation.ToString();
			Operation op = new Operation(operation, visibleName, onTrigger);
			m_operations[operation] = op;
		}

		public void AssignAccelerator(eOperation operation, Accelerator accelerator)
		{
			if (m_operations.ContainsKey(operation))
				m_operations[operation].AssignHotkey(accelerator);

		}
	}
}
