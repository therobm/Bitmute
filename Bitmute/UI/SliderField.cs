using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class SliderField : ContentView
	{
		private const double PopoutWidth = 196.0;
		private const double PopoutHeight = 40.0;

		private int m_minimum;
		private int m_maximum;
		private int m_value;
		private string m_suffix;
		private Action<int> m_onChanged;
		private Entry m_valueEntry;
		private Slider m_popoutSlider;
		private bool m_suppress;

		public SliderField(int minimum, int maximum, int value, string suffix, Action<int> onChanged)
		{
			m_minimum = minimum;
			m_maximum = maximum;
			m_value = value;
			m_suffix = suffix;
			m_onChanged = onChanged;

			m_valueEntry = new Entry();
			m_valueEntry.FontSize = 12.0;
			m_valueEntry.WidthRequest = 52.0;
			m_valueEntry.HeightRequest = 24.0;
			m_valueEntry.Margin = new Thickness(0.0);
			m_valueEntry.Keyboard = Keyboard.Numeric;
			m_valueEntry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_valueEntry.VerticalOptions = LayoutOptions.Center;
			m_valueEntry.HorizontalTextAlignment = TextAlignment.End;
			m_valueEntry.Completed += OnEntryCommitted;
			m_valueEntry.Focused += OnEntryFocused;
			m_valueEntry.Unfocused += OnEntryUnfocused;

			Label arrow = new Label();
			arrow.Text = "▾";
			arrow.FontSize = 10.0;
			arrow.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			arrow.VerticalOptions = LayoutOptions.Center;
			TapGestureRecognizer arrowTap = new TapGestureRecognizer();
			arrowTap.Tapped += OnArrowTapped;
			arrow.GestureRecognizers.Add(arrowTap);

			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = 3.0;
			row.Add(m_valueEntry);
			row.Add(arrow);

			Border chip = new Border();
			chip.Padding = new Thickness(6.0, 0.0, 5.0, 0.0);
			chip.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			chip.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			chip.StrokeThickness = 1.0;
			chip.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2.0) };
			chip.VerticalOptions = LayoutOptions.Center;
			chip.Content = row;

			UpdateLabel();
			Content = chip;
		}

		private static int ExtractInt(string text)
		{
			if (text == null)
			{
				return int.MinValue;
			}
			string digits = "";
			for (int index = 0; index < text.Length; index++)
			{
				char character = text[index];
				bool isDigit = character >= '0' && character <= '9';
				bool isLeadingMinus = character == '-' && digits.Length == 0;
				if (isDigit || isLeadingMinus)
				{
					digits = digits + character;
				}
				else if (digits.Length > 0)
				{
					break;
				}
			}
			if (digits.Length == 0 || digits == "-")
			{
				return int.MinValue;
			}
			int result = 0;
			bool valid = int.TryParse(digits, out result);
			if (!valid)
			{
				return int.MinValue;
			}
			return result;
		}

		private void OnEntryCommitted(object sender, EventArgs eventArgs)
		{
			ApplyTypedValue();
		}

		private void OnEntryFocused(object sender, FocusEventArgs eventArgs)
		{
			m_valueEntry.Text = m_value.ToString();
		}

		private void OnEntryUnfocused(object sender, FocusEventArgs eventArgs)
		{
			ApplyTypedValue();
		}

		private void ApplyTypedValue()
		{
			int parsed = ExtractInt(m_valueEntry.Text);
			if (parsed == int.MinValue)
			{
				UpdateLabel();
				return;
			}
			m_value = ClampValue(parsed);
			UpdateLabel();
			if (m_popoutSlider != null)
			{
				m_suppress = true;
				m_popoutSlider.Value = m_value;
				m_suppress = false;
			}
			if (m_onChanged != null)
			{
				m_onChanged(m_value);
			}
		}

		private void UpdateLabel()
		{
			m_valueEntry.Text = m_value + m_suffix;
		}

		public int Value()
		{
			return m_value;
		}

		public void SetValueSilently(int value)
		{
			m_value = ClampValue(value);
			UpdateLabel();
			if (m_popoutSlider != null)
			{
				m_suppress = true;
				m_popoutSlider.Value = m_value;
				m_suppress = false;
			}
		}

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

		private void OnArrowTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			double height = Height;
			if (height <= 0.0)
			{
				height = 26.0;
			}
			double anchorX = PageCoordinate(this, true);
			double anchorY = PageCoordinate(this, false) + height + 1.0;
			main.ShowPulldown(BuildPopout(), anchorX, anchorY, PopoutWidth, PopoutHeight);
		}

		private View BuildPopout()
		{
			m_popoutSlider = new Slider();
			m_popoutSlider.Minimum = m_minimum;
			m_popoutSlider.Maximum = m_maximum;
			m_popoutSlider.Value = m_value;
			m_popoutSlider.WidthRequest = 172.0;
			m_popoutSlider.VerticalOptions = LayoutOptions.Center;
			m_popoutSlider.ValueChanged += OnPopoutChanged;

			HorizontalStackLayout body = new HorizontalStackLayout();
			body.Padding = new Thickness(10.0, 4.0, 10.0, 4.0);
			body.Add(m_popoutSlider);
			return body;
		}

		private void OnPopoutChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_suppress)
			{
				return;
			}
			if (m_popoutSlider == null)
			{
				return;
			}
			m_value = (int)m_popoutSlider.Value;
			UpdateLabel();
			if (m_onChanged != null)
			{
				m_onChanged(m_value);
			}
		}

		private static double PageCoordinate(VisualElement element, bool horizontal)
		{
			double total = 0.0;
			Element current = element;
			for (int guard = 0; guard < 100; guard++)
			{
				VisualElement visual = current as VisualElement;
				if (visual == null)
				{
					break;
				}
				if (horizontal)
				{
					total += visual.X;
				}
				else
				{
					total += visual.Y;
				}
				Element parent = current.Parent;
				if (parent == null)
				{
					break;
				}
				current = parent;
			}
			return total;
		}
	}
}
