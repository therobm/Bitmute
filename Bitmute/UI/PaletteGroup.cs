using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class PaletteGroup : ContentView
	{
		private string[] m_tabNames;
		private Border[] m_tabButtons;
		private Label[] m_tabLabels;
		private View[] m_contents;
		private Label m_placeholder;
		private int m_activeIndex;
		private Grid m_body;
		private bool m_collapsed;
		private Label m_collapseLabel;
		private double m_lastPanY;

		private Border BuildTab(int index)
		{
			Label label = new Label();
			label.Text = m_tabNames[index];
			label.FontSize = 12.0;
			label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			label.VerticalOptions = LayoutOptions.Center;
			label.HorizontalOptions = LayoutOptions.Center;
			m_tabLabels[index] = label;

			Border tab = new Border();
			tab.HeightRequest = UiConstants.PaletteTabHeight;
			tab.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			tab.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			tab.StrokeThickness = 0.0;
			tab.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0, 3.0, 0.0, 0.0) };
			tab.Content = label;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnTabTapped;
			tab.GestureRecognizers.Add(tap);

			return tab;
		}

		private void OnTabTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_tabButtons.Length; index++)
			{
				if (ReferenceEquals(m_tabButtons[index], sender))
				{
					SelectTab(index);
					return;
				}
			}
		}

		private void SelectTab(int activeIndex)
		{
			m_activeIndex = activeIndex;
			for (int index = 0; index < m_tabButtons.Length; index++)
			{
				if (index == activeIndex)
				{
					m_tabButtons[index].ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
					m_tabLabels[index].ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				}
				else
				{
					m_tabButtons[index].ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
					m_tabLabels[index].ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
				}
			}
			if (m_placeholder != null)
			{
				m_placeholder.Text = m_tabNames[activeIndex];
			}
			if (m_contents != null)
			{
				for (int index = 0; index < m_contents.Length; index++)
				{
					m_contents[index].IsVisible = index == activeIndex;
				}
			}
		}

		public PaletteGroup(string[] tabNames)
			: this(tabNames, (View)null)
		{
		}

		private Label BuildStripButton(string glyph, EventHandler<TappedEventArgs> handler)
		{
			Label button = new Label();
			button.Text = glyph;
			button.FontSize = 11.0;
			button.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			button.WidthRequest = 20.0;
			button.HeightRequest = UiConstants.PaletteTabHeight;
			button.HorizontalTextAlignment = TextAlignment.Center;
			button.VerticalTextAlignment = TextAlignment.Center;
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += handler;
			button.GestureRecognizers.Add(tap);
			return button;
		}

		private void OnCollapseTapped(object sender, TappedEventArgs eventArgs)
		{
			m_collapsed = !m_collapsed;
			m_body.IsVisible = !m_collapsed;
			if (m_collapsed)
			{
				m_collapseLabel.Text = "▸";
			}
			else
			{
				m_collapseLabel.Text = "▾";
			}
			MainView main = MainView.Self;
			if (main != null)
			{
				main.OnPaletteGroupLayoutChanged();
			}
		}

		private void OnCloseTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ClosePalettePanel(m_tabNames[0]);
			}
		}

		public bool IsCollapsed()
		{
			return m_collapsed;
		}

		public string PanelKey()
		{
			return m_tabNames[0];
		}

		public void SetCollapsed(bool collapsed)
		{
			m_collapsed = collapsed;
			m_body.IsVisible = !collapsed;
			if (collapsed)
			{
				m_collapseLabel.Text = "▸";
			}
			else
			{
				m_collapseLabel.Text = "▾";
			}
		}

		private void OnStripPan(object sender, PanUpdatedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			if (eventArgs.StatusType == GestureStatus.Started)
			{
				m_lastPanY = 0.0;
				ZIndex = 100;
				return;
			}
			if (eventArgs.StatusType == GestureStatus.Running)
			{
				m_lastPanY = eventArgs.TotalY;
				TranslationY = m_lastPanY;
				return;
			}
			if (eventArgs.StatusType == GestureStatus.Completed || eventArgs.StatusType == GestureStatus.Canceled)
			{
				TranslationY = 0.0;
				ZIndex = 0;
				main.ReorderPalettePanel(this, m_lastPanY);
				m_lastPanY = 0.0;
			}
		}

		public PaletteGroup(string[] tabNames, View content)
		{
			View[] contents = null;
			if (content != null)
			{
				contents = new View[] { content };
			}
			Initialize(tabNames, contents);
		}

		public PaletteGroup(string[] tabNames, View[] contents)
		{
			Initialize(tabNames, contents);
		}

		private void Initialize(string[] tabNames, View[] contents)
		{
			m_tabNames = tabNames;
			m_tabButtons = new Border[tabNames.Length];
			m_tabLabels = new Label[tabNames.Length];
			m_contents = contents;
			m_activeIndex = 0;
			m_collapsed = false;

			HorizontalStackLayout tabs = new HorizontalStackLayout();
			tabs.Spacing = 2.0;
			for (int index = 0; index < tabNames.Length; index++)
			{
				Border tab = BuildTab(index);
				m_tabButtons[index] = tab;
				tabs.Add(tab);
			}

			m_collapseLabel = BuildStripButton("▾", OnCollapseTapped);
			Label closeLabel = BuildStripButton("✕", OnCloseTapped);

			Grid tabStrip = new Grid();
			tabStrip.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			tabStrip.Padding = new Thickness(2.0, 2.0, 2.0, 0.0);
			tabStrip.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			tabStrip.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			tabStrip.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			tabs.HorizontalOptions = LayoutOptions.Start;
			Grid.SetColumn(tabs, 0);
			Grid.SetColumn(m_collapseLabel, 1);
			Grid.SetColumn(closeLabel, 2);
			tabStrip.Add(tabs);
			tabStrip.Add(m_collapseLabel);
			tabStrip.Add(closeLabel);

			PanGestureRecognizer stripDrag = new PanGestureRecognizer();
			stripDrag.PanUpdated += OnStripPan;
			tabStrip.GestureRecognizers.Add(stripDrag);

			m_body = new Grid();
			m_body.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			if (contents != null && contents.Length > 0)
			{
				for (int index = 0; index < contents.Length; index++)
				{
					contents[index].IsVisible = index == 0;
					m_body.Add(contents[index]);
				}
			}
			else
			{
				m_placeholder = new Label();
				m_placeholder.FontSize = 12.0;
				m_placeholder.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
				m_placeholder.HorizontalOptions = LayoutOptions.Center;
				m_placeholder.VerticalOptions = LayoutOptions.Center;
				m_body.Add(m_placeholder);
			}

			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			Grid.SetRow(tabStrip, 0);
			Grid.SetRow(m_body, 1);
			layout.Add(tabStrip);
			layout.Add(m_body);

			this.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			Content = layout;
			SelectTab(0);
		}
	}
}
