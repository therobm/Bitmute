using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class CheckMark : ContentView
	{
		private Action<bool> m_onChanged;
		private Label m_glyph;
		private bool m_checked;

		private void OnBoxTapped(object sender, TappedEventArgs eventArgs)
		{
			m_checked = !m_checked;
			m_glyph.IsVisible = m_checked;
			if (m_onChanged != null)
			{
				m_onChanged(m_checked);
			}
		}

		public bool Checked()
		{
			return m_checked;
		}

		public void SetChecked(bool value)
		{
			m_checked = value;
			m_glyph.IsVisible = value;
		}

		public CheckMark(bool initial, Action<bool> onChanged)
		{
			m_onChanged = onChanged;
			m_checked = initial;

			m_glyph = new Label();
			m_glyph.Text = "✓";
			m_glyph.FontSize = UiConstants.PanelFontSize;
			m_glyph.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_glyph.HorizontalTextAlignment = TextAlignment.Center;
			m_glyph.VerticalTextAlignment = TextAlignment.Center;
			m_glyph.IsVisible = initial;

			Border box = new Border();
			box.WidthRequest = UiConstants.ComponentHeight;
			box.HeightRequest = UiConstants.ComponentHeight;
			box.ThemeBg(UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			box.ThemeStroke(UiConstants.ButtonBorderLight, UiConstants.ButtonBorderDark);
			box.StrokeThickness = UiConstants.PanelBorderThickness;
			box.VerticalOptions = LayoutOptions.Center;
			box.HorizontalOptions = LayoutOptions.Start;
			box.Content = m_glyph;
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnBoxTapped;
			box.GestureRecognizers.Add(tap);

			Content = box;
		}
	}
}
