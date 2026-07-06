using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Bitmute.WinUI
{
	public partial class App : MauiWinUIApplication
	{
		public App()
		{
			InitializeComponent();
			UnhandledException += OnUnhandledException;
		}

		protected override MauiApp CreateMauiApp()
		{
			return MauiProgram.CreateMauiApp();
		}

		private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs eventArgs)
		{
			if (eventArgs.Exception != null)
			{
				Bitmute.Log.Exception(eventArgs.Exception);
			}
			eventArgs.Handled = true;
		}
	}
}
