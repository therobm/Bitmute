using System;
using System.Reflection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Bitmute.UI.Dialogs
{
	public class AboutDialog : ModalDialog
	{
		private const double DialogWidth = 380.0;
		private const double BodyWidth = 356.0;

		private string BuildVersionText()
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			if (version == null)
			{
				return "1.0";
			}
			if (version.Build >= 0)
			{
				return version.Major.ToString() + "." + version.Minor.ToString() + "." + version.Build.ToString();
			}
			return version.Major.ToString() + "." + version.Minor.ToString();
		}

		private Label BuildCenteredLabel(string text, double fontSize)
		{
			Label label = new Label();
			label.Text = text;
			label.FontSize = fontSize;
			label.HorizontalOptions = LayoutOptions.Center;
			label.HorizontalTextAlignment = TextAlignment.Center;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			return label;
		}

		private Label BuildDimLabel(string text, double fontSize)
		{
			Label label = BuildCenteredLabel(text, fontSize);
			label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			return label;
		}

		private View BuildHeader()
		{
			Image icon = new Image();
			icon.Source = "bitmuteicon.png";
			icon.WidthRequest = 48.0;
			icon.HeightRequest = 48.0;
			icon.VerticalOptions = LayoutOptions.Center;

			Label title = new Label();
			title.Text = "Bitmute";
			title.FontSize = 20.0;
			title.FontAttributes = FontAttributes.Bold;
			title.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);

			Label version = new Label();
			version.Text = "Version " + BuildVersionText();
			version.FontSize = 12.0;
			version.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);

			VerticalStackLayout text = new VerticalStackLayout();
			text.Spacing = 2.0;
			text.VerticalOptions = LayoutOptions.Center;
			text.Add(title);
			text.Add(version);

			HorizontalStackLayout header = new HorizontalStackLayout();
			header.Spacing = 12.0;
			header.HorizontalOptions = LayoutOptions.Center;
			header.Add(icon);
			header.Add(text);
			return header;
		}

		private Label BuildTaglineLabel()
		{
			Label label = BuildDimLabel("A .NET MAUI raster image editor for game development and pixel work", 12.0);
			label.LineBreakMode = LineBreakMode.WordWrap;
			label.MaximumWidthRequest = BodyWidth;
			return label;
		}

		private Label BuildRepoLinkLabel()
		{
			Label label = BuildCenteredLabel("github.com/therobm/Bitmute", 12.0);
			label.ThemeText(UiConstants.AccentLight, UiConstants.AccentDark);
			label.TextDecorations = TextDecorations.Underline;
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnRepoLinkTapped;
			label.GestureRecognizers.Add(tap);
			return label;
		}

		private View BuildSingleButtonRow(Button okButton)
		{
			HorizontalStackLayout buttons = new HorizontalStackLayout();
			buttons.Spacing = 8.0;
			buttons.HorizontalOptions = LayoutOptions.Center;
			buttons.Add(okButton);
			return buttons;
		}

		private void OnRepoLinkTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.OpenRepoLink();
			}
		}
		protected override void OnPrimaryClicked(object sender, EventArgs eventArgs)
		{
			base.OnPrimaryClicked(sender, eventArgs);
		}


		public AboutDialog()
		{
			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			body.WidthRequest = BodyWidth;
			body.Add(BuildHeader());
			body.Add(BuildTaglineLabel());
			body.Add(BuildRepoLinkLabel());
			body.Add(BuildDimLabel(".NET " + Environment.Version.ToString() + "  ·  SkiaSharp 4.148", 11.0));
			body.Add(BuildDimLabel("Apache 2.0 License · © 2026 Rob M", 11.0));

			Button okButton = PrimaryButton("OK");
			ComposeDialog("About Bitmute", body, BuildSingleButtonRow(okButton));
			WidthRequest = DialogWidth;
		}
	}
}
