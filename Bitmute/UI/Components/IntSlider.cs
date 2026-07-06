using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class IntSlider : ContentView
	{
		private int m_minimum;
		private int m_maximum;
		private int m_value;
		private Action<int> m_onChanged;
		private ValueSlider m_slider;
		private Entry m_entry;
		private bool m_updating;
		private bool m_enforceRange;

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

		private void ApplyValue(int value, bool moveSlider)
		{
			// The slider itself always tracks within [min,max]; a typed value may exceed the range
			// unless enforcement was requested, so it is stored as-is and only the thumb is clamped.
			if (m_enforceRange)
			{
				value = ClampValue(value);
			}
			m_value = value;
			m_updating = true;
			m_entry.Text = m_value.ToString();
			if (moveSlider)
			{
				m_slider.SetValueSilently(m_value);
			}
			m_updating = false;
		}

		private void OnSliderValue(int value)
		{
			if (m_updating)
			{
				return;
			}
			if (value == m_value)
			{
				return;
			}
			ApplyValue(value, false);
			if (m_onChanged != null)
			{
				m_onChanged(m_value);
			}
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
				ApplyValue(m_value, true);
				return;
			}
			int previous = m_value;
			ApplyValue(parsed, true);
			if (m_value != previous && m_onChanged != null)
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
			ApplyValue(value, true);
		}

		public IntSlider(string caption, int minimum, int maximum, int initial, string unit, Action<int> onChanged, bool enforceRange = false)
		{
			m_minimum = minimum;
			m_maximum = maximum;
			m_onChanged = onChanged;
			m_enforceRange = enforceRange;

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			m_slider = new ValueSlider(minimum, maximum, ClampValue(initial), OnSliderValue);
			m_slider.WidthRequest = UiConstants.FieldSliderWidth;
			m_slider.HeightRequest = UiConstants.ComponentHeight;
			m_slider.VerticalOptions = LayoutOptions.Center;

			m_entry = new Entry();
			m_entry.FontSize = UiConstants.ComponentFontSize;
			m_entry.WidthRequest = UiConstants.FieldValueWidth;
			m_entry.HeightRequest = UiConstants.ComponentHeight;
			m_entry.Keyboard = Keyboard.Numeric;
			m_entry.HorizontalTextAlignment = TextAlignment.End;
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
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(m_slider, 1);
			Grid.SetColumn(m_entry, 2);
			Grid.SetColumn(unitLabel, 3);
			row.Add(captionLabel);
			row.Add(m_slider);
			row.Add(m_entry);
			row.Add(unitLabel);

			ApplyValue(initial, true);
			Content = row;
		}
	}
}
