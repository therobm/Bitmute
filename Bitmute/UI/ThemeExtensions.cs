using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public static class ThemeExtensions
	{
		public static void ThemeBg(this VisualElement view, Color light, Color dark)
		{
			view.SetAppThemeColor(VisualElement.BackgroundColorProperty, light, dark);
		}

		public static void ThemeText(this Label view, Color light, Color dark)
		{
			view.SetAppThemeColor(Label.TextColorProperty, light, dark);
		}

		public static void ThemeText(this Button view, Color light, Color dark)
		{
			view.SetAppThemeColor(Button.TextColorProperty, light, dark);
		}

		public static void ThemeText(this Entry view, Color light, Color dark, Color lightBackground, Color darkBackground)
		{
			view.SetAppThemeColor(Entry.BackgroundColorProperty, lightBackground, darkBackground);
			view.SetAppThemeColor(Entry.TextColorProperty, light, dark);
		}

		public static void ThemeText(this Picker view, Color light, Color dark, Color lightBackground, Color darkBackground)
		{
			view.SetAppThemeColor(Picker.BackgroundColorProperty, lightBackground, darkBackground);
			view.SetAppThemeColor(Picker.TextColorProperty, light, dark);
		}

		public static void ThemeText(this Editor view, Color light, Color dark)
		{
			view.SetAppThemeColor(Editor.TextColorProperty, light, dark);
		}

		public static void ThemeStroke(this Border view, Color light, Color dark)
		{
			view.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(light), new SolidColorBrush(dark));
		}

		public static void ThemeColor(this BoxView view, Color light, Color dark)
		{
			view.SetAppThemeColor(BoxView.ColorProperty, light, dark);
		}
	}
}
