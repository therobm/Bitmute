using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Bitmute.UI;

namespace Bitmute
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
		}

		protected override Window CreateWindow(IActivationState activationState)
		{
			Window window = new Window(new MainView());
			window.Title = "Bitmute";
			return window;
		}
	}
}
