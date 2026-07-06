using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class FloatSlider : ContentView
	{
		private float m_minimum;
		private float m_maximum;
		private float m_value;
		private int m_decimals;
		private Action<float> m_onChanged;
		private ValueSlider m_slider;
		private Entry m_entry;
		private bool m_updating;
		private int m_steps;
		private double m_scale;

		private float ClampValue(float value)
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

		private int ValueToStep(float value)
		{
			float clamped = ClampValue(value);
			int step = (int)Math.Round((double)(clamped - m_minimum) * m_scale);
			if (step < 0)
			{
				return 0;
			}
			if (step > m_steps)
			{
				return m_steps;
			}
			return step;
		}

		private float StepToValue(int step)
		{
			double value = (double)m_minimum + ((double)step / m_scale);
			return ClampValue(RoundValue((float)value));
		}

		private float RoundValue(float value)
		{
			double rounded = Math.Round((double)value, m_decimals);
			return (float)rounded;
		}

		private string FormatValue(float value)
		{
			return value.ToString("F" + m_decimals.ToString(), System.Globalization.CultureInfo.InvariantCulture);
		}

		private void ApplyValue(float value, bool moveSlider)
		{
			m_value = ClampValue(RoundValue(value));
			m_updating = true;
			m_entry.Text = FormatValue(m_value);
			if (moveSlider)
			{
				m_slider.SetValueSilently(ValueToStep(m_value));
			}
			m_updating = false;
		}

		private void OnSliderStep(int step)
		{
			if (m_updating)
			{
				return;
			}
			float value = StepToValue(step);
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
			double parsed = 0.0;
			bool valid = double.TryParse(m_entry.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed);
			if (!valid)
			{
				ApplyValue(m_value, true);
				return;
			}
			float clamped = ClampValue(RoundValue((float)parsed));
			bool changed = clamped != m_value;
			ApplyValue(clamped, true);
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

		public float Value()
		{
			return m_value;
		}

		public void SetValue(float value)
		{
			ApplyValue(value, true);
		}

		public FloatSlider(string caption, float minimum, float maximum, float initial, int decimals, string unit, Action<float> onChanged)
		{
			m_minimum = minimum;
			m_maximum = maximum;
			m_decimals = decimals;
			m_onChanged = onChanged;
			m_scale = Math.Pow(10.0, m_decimals);
			m_steps = (int)Math.Round((double)(m_maximum - m_minimum) * m_scale);
			if (m_steps < 1)
			{
				m_steps = 1;
			}

			Label captionLabel = new Label();
			captionLabel.Text = caption;
			captionLabel.FontSize = UiConstants.PanelFontSize;
			captionLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			captionLabel.WidthRequest = UiConstants.FieldCaptionWidth;
			captionLabel.VerticalOptions = LayoutOptions.Center;

			m_slider = new ValueSlider(0, m_steps, ValueToStep(initial), OnSliderStep);
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
