using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Bitmute.WinUI
{
	public partial class App : MauiWinUIApplication
	{
		public App()
		{
			InitializeComponent();
		}

		protected override MauiApp CreateMauiApp()
		{
			return MauiProgram.CreateMauiApp();
		}
	}
}
