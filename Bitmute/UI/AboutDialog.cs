using System;
using System.Reflection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Bitmute.UI
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

		private Label BuildTitleLabel()
		{
			Label label = BuildCenteredLabel("Bitmute", 20.0);
			label.FontAttributes = FontAttributes.Bold;
			return label;
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

		private void OnOkClicked(object sender, EventArgs eventArgs)
		{
			CloseModal();
		}

		public AboutDialog()
		{
			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 8.0;
			body.WidthRequest = BodyWidth;
			body.Add(BuildTitleLabel());
			body.Add(BuildCenteredLabel("Version " + BuildVersionText(), 12.0));
			body.Add(BuildTaglineLabel());
			body.Add(BuildRepoLinkLabel());
			body.Add(BuildDimLabel(".NET " + Environment.Version.ToString() + "  ·  SkiaSharp 4.148", 11.0));
			body.Add(BuildDimLabel("MIT License · © 2026 Rob M", 11.0));

			Button okButton = PrimaryButton("OK", OnOkClicked);
			ComposeDialog("About Bitmute", body, BuildSingleButtonRow(okButton));
			WidthRequest = DialogWidth;
		}
	}
}
