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
		private Label m_valueLabel;
		private Slider m_popoutSlider;
		private bool m_suppress;

		public SliderField(int minimum, int maximum, int value, string suffix, Action<int> onChanged)
		{
			m_minimum = minimum;
			m_maximum = maximum;
			m_value = value;
			m_suffix = suffix;
			m_onChanged = onChanged;

			m_valueLabel = new Label();
			m_valueLabel.FontSize = 12.0;
			m_valueLabel.WidthRequest = 40.0;
			m_valueLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_valueLabel.VerticalOptions = LayoutOptions.Center;
			m_valueLabel.HorizontalTextAlignment = TextAlignment.End;

			Label arrow = new Label();
			arrow.Text = "▾";
			arrow.FontSize = 10.0;
			arrow.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			arrow.VerticalOptions = LayoutOptions.Center;

			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = 4.0;
			row.Add(m_valueLabel);
			row.Add(arrow);

			Border chip = new Border();
			chip.Padding = new Thickness(6.0, 1.0, 5.0, 1.0);
			chip.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			chip.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			chip.StrokeThickness = 1.0;
			chip.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2.0) };
			chip.VerticalOptions = LayoutOptions.Center;
			chip.Content = row;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnChipTapped;
			chip.GestureRecognizers.Add(tap);

			UpdateLabel();
			Content = chip;
		}

		private void UpdateLabel()
		{
			m_valueLabel.Text = m_value + m_suffix;
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

		private void OnChipTapped(object sender, TappedEventArgs eventArgs)
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
