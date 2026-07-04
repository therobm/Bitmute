using System;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace Bitmute
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			MauiAppBuilder builder = MauiApp.CreateBuilder();
			builder.UseMauiApp<App>();
			builder.UseSkiaSharp();
			builder.ConfigureFonts(RegisterFonts);
			builder.ConfigureLifecycleEvents(RegisterLifecycleEvents);

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}

		private static void RegisterLifecycleEvents(ILifecycleBuilder events)
		{
			events.AddWindows(RegisterWindowsEvents);
		}

		private static void RegisterWindowsEvents(IWindowsLifecycleBuilder windows)
		{
			windows.OnWindowCreated(OnNativeWindowCreated);
		}

		private static void OnNativeWindowCreated(Microsoft.UI.Xaml.Window window)
		{
			if (!Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
			{
				return;
			}
			IntPtr handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
			Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
			Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
			if (appWindow == null)
			{
				return;
			}
			Bitmute.UI.TitleBarChrome.Attach(appWindow.TitleBar);
			window.Activated += OnNativeWindowActivated;
		}

		private static void OnNativeWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
		{
			if (args.WindowActivationState == Microsoft.UI.Xaml.WindowActivationState.Deactivated)
			{
				return;
			}
			Microsoft.UI.Xaml.Window window = sender as Microsoft.UI.Xaml.Window;
			if (window == null)
			{
				return;
			}
			window.DispatcherQueue.TryEnqueue(Bitmute.UI.TitleBarChrome.Apply);
		}

		private static void RegisterFonts(IFontCollection fonts)
		{
			fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
		}
	}
}
