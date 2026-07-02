using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;

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

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}

		private static void RegisterFonts(IFontCollection fonts)
		{
			fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
		}
	}
}
