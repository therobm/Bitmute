using System;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SkiaSharp;

namespace Bitmute.UI
{
	public static class Theme
	{
		private static bool s_dark = true;
		private static bool s_followSystem = true;

		public static event EventHandler Changed;

		public static void InitializeFromSystem()
		{
			Application application = Application.Current;
			if (application != null)
			{
				s_dark = application.RequestedTheme != AppTheme.Light;
			}
		}

		public static bool IsDark()
		{
			return s_dark;
		}

		public static bool FollowSystem()
		{
			return s_followSystem;
		}

		private static void Apply(bool dark, bool follow)
		{
			bool changed = dark != s_dark || follow != s_followSystem;
			s_dark = dark;
			s_followSystem = follow;
			if (!changed)
			{
				return;
			}
			EventHandler handler = Changed;
			if (handler != null)
			{
				handler(null, EventArgs.Empty);
			}
		}

		public static void UseSystem()
		{
			bool dark = true;
			Application application = Application.Current;
			if (application != null)
			{
				dark = application.RequestedTheme != AppTheme.Light;
			}
			Apply(dark, true);
		}

		public static void UseDark()
		{
			Apply(true, false);
		}

		public static void UseLight()
		{
			Apply(false, false);
		}

		public static void OnSystemThemeChanged()
		{
			if (!s_followSystem)
			{
				return;
			}
			Application application = Application.Current;
			if (application == null)
			{
				return;
			}
			bool dark = application.RequestedTheme != AppTheme.Light;
			Apply(dark, true);
		}

		public static SKColor IconTint()
		{
			return new SKColor(200,200,200);
		}
	}
}
