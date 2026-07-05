using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class IntField : ContentView
	{
		private int m_minimum;
		private int m_maximum;
		private int m_value;
		private Action<int> m_onChanged;
		private Entry m_entry;
		private bool m_updating;

		private int ClampValue(int value)
		{
			if (value < m_minimum)
			{
				return m_minimum;
			}
			if (value > m_maximum)
			{
				return m_maximum;
			}
			return value;
		}

		private void ApplyValue(int value)
		{
			m_value = ClampValue(value);
			m_updating = true;
			m_entry.Text = m_value.ToString();
			m_updating = false;
		}

		private void CommitTypedValue()
		{
			if (m_updating)
			{
				return;
			}
			int parsed = 0;
			bool valid = int.TryParse(m_entry.Text, out parsed);
			if (!valid)
			{
				ApplyValue(m_value);
				return;
			}
			int clamped = ClampValue(parsed);
			bool changed = clamped != m_value;
			ApplyValue(clamped);
			if (changed && m_onChanged != null)
			{
				m_onChanged(m_value);
			}
		}

		private void OnEntryCompleted(object sender, EventArgs eventArgs)
		{
			CommitTypedValue();
		}

		private void OnEntryUnfocused(object sender, FocusEventArgs eventArgs)
		{
			CommitTypedValue();
		}

		public int Value()
		{
			return m_value;
		}

		public void SetValue(int value)
		{
			ApplyValue(value);
		}

		public IntField(string caption, int minimum, int maximum, int initial, string unit, Action<int> onChanged)
		{
			m_minimum = minimum;
			m_maximum = maximum;
			m_onChanged = onChanged;

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			m_entry = new Entry();
			m_entry.FontSize = UiConstants.ComponentFontSize;
			m_entry.WidthRequest = UiConstants.FieldEntryWidth;
			m_entry.HeightRequest = UiConstants.ComponentHeight;
			m_entry.Keyboard = Keyboard.Numeric;
			m_entry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_entry.VerticalOptions = LayoutOptions.Center;
			m_entry.Completed += OnEntryCompleted;
			m_entry.Unfocused += OnEntryUnfocused;

			Label unitLabel = new Label();
			unitLabel.Text = unit;
			unitLabel.FontSize = UiConstants.ComponentFontSize;
			unitLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			unitLabel.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = UiConstants.DialogRowSpacing;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(m_entry, 1);
			Grid.SetColumn(unitLabel, 2);
			row.Add(captionLabel);
			row.Add(m_entry);
			row.Add(unitLabel);

			ApplyValue(initial);
			Content = row;
		}
	}
}
