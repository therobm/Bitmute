using System;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using SkiaSharp;

namespace Bitmute.UI
{
	public static class Theme
	{
		private const string ModeKey = "theme_mode";
		private const string ModeSystem = "system";
		private const string ModeDark = "dark";
		private const string ModeLight = "light";

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

		private static void ApplyToApplication()
		{
			Application application = Application.Current;
			if (application == null)
			{
				return;
			}
			if (s_followSystem)
			{
				application.UserAppTheme = AppTheme.Unspecified;
				return;
			}
			if (s_dark)
			{
				application.UserAppTheme = AppTheme.Dark;
				return;
			}
			application.UserAppTheme = AppTheme.Light;
		}

		private static void Save()
		{
			string mode = ModeSystem;
			if (!s_followSystem)
			{
				if (s_dark)
				{
					mode = ModeDark;
				}
				else
				{
					mode = ModeLight;
				}
			}
			Preferences.Default.Set(ModeKey, mode);
		}

		public static void InitializeFromSystem()
		{
			string mode = Preferences.Default.Get(ModeKey, ModeSystem);
			if (mode == ModeDark)
			{
				s_followSystem = false;
				s_dark = true;
			}
			else if (mode == ModeLight)
			{
				s_followSystem = false;
				s_dark = false;
			}
			else
			{
				s_followSystem = true;
				s_dark = SystemIsDark();
			}
			ApplyToApplication();
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
			s_dark = SystemIsDark();
			ApplyToApplication();
			Save();
			Fire();
		}

		public static void UseDark()
		{
			s_followSystem = false;
			s_dark = true;
			ApplyToApplication();
			Save();
			Fire();
		}

		public static void UseLight()
		{
			s_followSystem = false;
			s_dark = false;
			ApplyToApplication();
			Save();
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
				return new SKColor(UiConstants.IconTintDark.ToUint());
			}
			return new SKColor(UiConstants.IconTintLight.ToUint());
		}

		public static SKColor IconTintSelected()
		{
			if (s_dark)
			{
				return new SKColor(UiConstants.IconTintSelectedDark.ToUint());
			}
			return new SKColor(UiConstants.IconTintSelectedLight.ToUint());
		}

		public static SKColor ScrollbarTrack()
		{
			if (s_dark)
			{
				return new SKColor(UiConstants.PanelSurfaceDark.ToUint());
			}
			
			return new SKColor(UiConstants.PanelSurfaceLight.ToUint());
		}

		public static SKColor ScrollbarThumb()
		{
			if (s_dark)
			{
				return new SKColor(UiConstants.ScrollbarThumbDark.ToUint());
			}
			return new SKColor(UiConstants.ScrollbarThumbLight.ToUint());
		}

		public static SKColor CanvasSurround()
		{
			if (s_dark)
			{
				return new SKColor(UiConstants.CanvasInsetDark.ToUint());
			}
			return new SKColor(UiConstants.CanvasInsetLight.ToUint());
		}
	}
}
