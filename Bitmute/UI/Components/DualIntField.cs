using System;
using Microsoft.Maui.Controls;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class DualIntField : ContentView
	{
		private Action<int, int> m_onChanged;
		private IntField m_first;
		private IntField m_second;
		private CheckField m_lock;
		private double m_ratio;

		private void CaptureRatio()
		{
			int first = m_first.Value();
			int second = m_second.Value();
			if (first < 1 || second < 1)
			{
				m_ratio = 0.0;
				return;
			}
			m_ratio = (double)first / (double)second;
		}

		private void FireChanged()
		{
			if (m_onChanged != null)
			{
				m_onChanged(m_first.Value(), m_second.Value());
			}
		}

		private void OnFirstChanged(int value)
		{
			if (m_lock.Checked() && m_ratio > 0.0)
			{
				m_second.SetValue((int)Math.Round(value / m_ratio));
			}
			FireChanged();
		}

		private void OnSecondChanged(int value)
		{
			if (m_lock.Checked() && m_ratio > 0.0)
			{
				m_first.SetValue((int)Math.Round(value * m_ratio));
			}
			FireChanged();
		}

		private void OnLockChanged(bool value)
		{
			if (value)
			{
				CaptureRatio();
			}
		}

		public int FirstValue()
		{
			return m_first.Value();
		}

		public int SecondValue()
		{
			return m_second.Value();
		}

		public void SetValues(int first, int second)
		{
			m_first.SetValue(first);
			m_second.SetValue(second);
			if (m_lock.Checked())
			{
				CaptureRatio();
			}
		}

		public DualIntField(string firstCaption, string secondCaption, int firstInitial, int secondInitial, int minimum, int maximum, string unit, Action<int, int> onChanged)
		{
			m_onChanged = onChanged;

			m_first = new IntField(firstCaption, minimum, maximum, firstInitial, unit, OnFirstChanged);
			m_second = new IntField(secondCaption, minimum, maximum, secondInitial, unit, OnSecondChanged);
			m_lock = new CheckField("Lock aspect", false, OnLockChanged);

			VerticalStackLayout stack = new VerticalStackLayout();
			stack.Spacing = UiConstants.DialogRowSpacing;
			stack.Add(m_first);
			stack.Add(m_second);
			stack.Add(m_lock);

			Content = stack;
		}
	}
}
