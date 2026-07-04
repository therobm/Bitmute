using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Bitmute.UI;


namespace Bitmute
{
	public partial class App : Application
	{
		private const string WindowWidthKey = "window_width";
		private const string WindowHeightKey = "window_height";
		private const double MinimumRememberedSize = 240.0;

		private TitleBar m_titleBar;

		public App()
		{
			InitializeComponent();
		}

		private void ApplyTitleBarColors()
		{
			if (m_titleBar == null)
			{
				return;
			}
			m_titleBar.BackgroundColor = UiConstants.AppTitleBar;
			m_titleBar.ForegroundColor = UiConstants.AppTitleBarText;
		}

		private void OnThemeChanged(object sender, EventArgs eventArgs)
		{
			ApplyTitleBarColors();
		}

		protected override Window CreateWindow(IActivationState activationState)
		{
			Window window = new Window(new MainView());
			window.Title = "Bitmute";

			m_titleBar = new TitleBar();
			m_titleBar.Title = "Bitmute";
			ApplyTitleBarColors();
			window.TitleBar = m_titleBar;
			Theme.Changed += OnThemeChanged;

			double savedWidth = Preferences.Default.Get(WindowWidthKey, 0.0);
			double savedHeight = Preferences.Default.Get(WindowHeightKey, 0.0);
			if (savedWidth >= MinimumRememberedSize && savedHeight >= MinimumRememberedSize)
			{
				window.Width = savedWidth;
				window.Height = savedHeight;
			}
			window.SizeChanged += OnWindowSizeChanged;
			return window;
		}

		private void OnWindowSizeChanged(object sender, EventArgs eventArgs)
		{
			Window window = sender as Window;
			if (window == null)
			{
				return;
			}
			double width = window.Width;
			double height = window.Height;
			if (width < MinimumRememberedSize || height < MinimumRememberedSize)
			{
				return;
			}
			Preferences.Default.Set(WindowWidthKey, width);
			Preferences.Default.Set(WindowHeightKey, height);
		}
	}
}
