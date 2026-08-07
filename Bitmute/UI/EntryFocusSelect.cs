using System;
using Microsoft.Maui.Controls;

namespace Bitmute.UI
{
	public static class EntryFocusSelect
	{
		public static void Attach(Entry entry)
		{
			entry.Focused += OnFocused;
		}

		private static void OnFocused(object sender, FocusEventArgs eventArgs)
		{
			Entry entry = sender as Entry;
			if (entry == null)
			{
				return;
			}
			if (entry.Text == null)
			{
				return;
			}
			entry.CursorPosition = 0;
			entry.SelectionLength = entry.Text.Length;
		}
	}
}
