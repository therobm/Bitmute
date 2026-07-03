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

		private static bool SystemIsDark()
		{
			Application application = Application.Current;
			if (application == null)
			{
				return true;
			}
			return application.RequestedTheme != AppTheme.Light;
		}

		public static void InitializeFromSystem()
		{
			s_followSystem = true;
			s_dark = SystemIsDark();
		}

		public static bool IsDark()
		{
			return s_dark;
		}

		public static bool FollowSystem()
		{
			return s_followSystem;
		}

		private static void Fire()
		{
			EventHandler handler = Changed;
			if (handler != null)
			{
				handler(null, EventArgs.Empty);
			}
		}

		public static void UseSystem()
		{
			s_followSystem = true;
			Application application = Application.Current;
			if (application != null)
			{
				application.UserAppTheme = AppTheme.Unspecified;
			}
			s_dark = SystemIsDark();
			Fire();
		}

		public static void UseDark()
		{
			s_followSystem = false;
			Application application = Application.Current;
			if (application != null)
			{
				application.UserAppTheme = AppTheme.Dark;
			}
			s_dark = true;
			Fire();
		}

		public static void UseLight()
		{
			s_followSystem = false;
			Application application = Application.Current;
			if (application != null)
			{
				application.UserAppTheme = AppTheme.Light;
			}
			s_dark = false;
			Fire();
		}

		public static void Toggle()
		{
			if (s_dark)
			{
				UseLight();
			}
			else
			{
				UseDark();
			}
		}

		public static void OnSystemThemeChanged()
		{
			if (!s_followSystem)
			{
				return;
			}
			s_dark = SystemIsDark();
			Fire();
		}

		public static SKColor IconTint()
		{
			if (s_dark)
			{
				return new SKColor(0xD2, 0xD2, 0xD2);
			}
			return new SKColor(50, 50, 50);
		}

		public static SKColor IconTintSelected()
		{
			if (s_dark)
			{
				return new SKColor(0xF0, 0xF0, 0xF0);
			}
			return new SKColor(0x18, 0x2A, 0x44);
		}

		public static SKColor ScrollbarTrack()
		{
			if (s_dark)
			{
				return new SKColor(0x26, 0x26, 0x26);
			}
			return new SKColor(0xCE, 0xCE, 0xCE);
		}

		public static SKColor ScrollbarThumb()
		{
			if (s_dark)
			{
				return new SKColor(0x5A, 0x5A, 0x5A);
			}
			return new SKColor(0x9C, 0x9C, 0x9C);
		}
	}
}
