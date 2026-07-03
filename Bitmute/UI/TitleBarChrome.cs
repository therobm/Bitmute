using System;

namespace Bitmute.UI
{
	public static class TitleBarChrome
	{
		private static Microsoft.UI.Windowing.AppWindowTitleBar s_titleBar;

		public static void Attach(Microsoft.UI.Windowing.AppWindowTitleBar titleBar)
		{
			s_titleBar = titleBar;
			Theme.Changed += OnThemeChanged;
			Apply();
		}

		private static void OnThemeChanged(object sender, EventArgs eventArgs)
		{
			Apply();
		}

		private static Windows.UI.Color ToWindowsColor(Microsoft.Maui.Graphics.Color color)
		{
			byte alpha = (byte)(color.Alpha * 255.0f);
			byte red = (byte)(color.Red * 255.0f);
			byte green = (byte)(color.Green * 255.0f);
			byte blue = (byte)(color.Blue * 255.0f);
			return Windows.UI.Color.FromArgb(alpha, red, green, blue);
		}

		public static void Apply()
		{
			if (s_titleBar == null)
			{
				return;
			}
			Windows.UI.Color background = ToWindowsColor(UiConstants.AppTitleBar);
			Windows.UI.Color foreground = ToWindowsColor(UiConstants.AppTitleBarText);
			Windows.UI.Color hover = ToWindowsColor(UiConstants.AppTitleBarHover);
			s_titleBar.BackgroundColor = background;
			s_titleBar.InactiveBackgroundColor = background;
			s_titleBar.ButtonBackgroundColor = background;
			s_titleBar.ButtonInactiveBackgroundColor = background;
			s_titleBar.ForegroundColor = foreground;
			s_titleBar.InactiveForegroundColor = foreground;
			s_titleBar.ButtonForegroundColor = foreground;
			s_titleBar.ButtonInactiveForegroundColor = foreground;
			s_titleBar.ButtonHoverBackgroundColor = hover;
			s_titleBar.ButtonHoverForegroundColor = foreground;
			s_titleBar.ButtonPressedBackgroundColor = hover;
			s_titleBar.ButtonPressedForegroundColor = foreground;
		}
	}
}
