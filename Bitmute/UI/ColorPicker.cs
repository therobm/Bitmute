using System;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class ColorPicker : ContentView
	{
		private const float SquareSize = 200.0f;
		private const float BarGap = 12.0f;
		private const float BarWidth = 20.0f;
		private const float GradientWidth = SquareSize + BarGap + BarWidth;
		private const float GradientHeight = 200.0f;
		private const float SquareRightFraction = SquareSize / GradientWidth;
		private const float BarLeftFraction = (SquareSize + BarGap) / GradientWidth;

		private SKCanvasView m_gradient;
		private Entry m_redEntry;
		private Entry m_greenEntry;
		private Entry m_blueEntry;
		private Entry m_hexEntry;
		private BoxView m_preview;
		private float m_hue;
		private float m_saturation;
		private float m_value;
		private byte m_alpha;
		private bool m_foreground;
		private bool m_suppress;

		private static string ToHex(SKColor color)
		{
			return "#" + color.Red.ToString("X2") + color.Green.ToString("X2") + color.Blue.ToString("X2");
		}

		private static Color ToMaui(SKColor color)
		{
			return new Color(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
		}

		private SKColor CurrentColor()
		{
			SKColor rgb = SKColor.FromHsv(m_hue, m_saturation * 100.0f, m_value * 100.0f);
			return new SKColor(rgb.Red, rgb.Green, rgb.Blue, m_alpha);
		}

		private Entry BuildChannelEntry()
		{
			Entry entry = new Entry();
			entry.FontSize = 12.0;
			entry.WidthRequest = 48.0;
			entry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			entry.Keyboard = Keyboard.Numeric;
			entry.Completed += OnChannelCompleted;
			return entry;
		}

		private Grid BuildLabeledEntry(string label, Entry entry)
		{
			Label caption = new Label();
			caption.Text = label;
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.FontSize = 12.0;
			caption.WidthRequest = 16.0;
			caption.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = 4.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(caption, 0);
			Grid.SetColumn(entry, 1);
			row.Add(caption);
			row.Add(entry);
			return row;
		}

		private void SyncFromHsv()
		{
			SKColor color = CurrentColor();
			m_suppress = true;
			m_redEntry.Text = color.Red.ToString();
			m_greenEntry.Text = color.Green.ToString();
			m_blueEntry.Text = color.Blue.ToString();
			m_hexEntry.Text = ToHex(color);
			m_suppress = false;
			m_preview.Color = ToMaui(color);
			m_gradient.InvalidateSurface();
		}

		private void OnGradientPaint(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			SKImageInfo info = eventArgs.Info;
			canvas.Clear(new SKColor(0x33, 0x33, 0x33));
			float squareRight = info.Width * SquareRightFraction;
			float barLeft = info.Width * BarLeftFraction;
			float height = info.Height;

			SKRect squareRect = new SKRect(0.0f, 0.0f, squareRight, height);
			SKColor pureHue = SKColor.FromHsv(m_hue, 100.0f, 100.0f);
			SKPaint squarePaint = new SKPaint();
			SKShader saturationShader = SKShader.CreateLinearGradient(new SKPoint(0.0f, 0.0f), new SKPoint(squareRight, 0.0f), new SKColor[] { SKColors.White, pureHue }, null, SKShaderTileMode.Clamp);
			squarePaint.Shader = saturationShader;
			canvas.DrawRect(squareRect, squarePaint);
			SKShader valueShader = SKShader.CreateLinearGradient(new SKPoint(0.0f, 0.0f), new SKPoint(0.0f, height), new SKColor[] { SKColors.Transparent, SKColors.Black }, null, SKShaderTileMode.Clamp);
			squarePaint.Shader = valueShader;
			canvas.DrawRect(squareRect, squarePaint);
			squarePaint.Dispose();

			SKRect barRect = new SKRect(barLeft, 0.0f, info.Width, height);
			SKColor[] hueStops = new SKColor[] { SKColor.FromHsv(0.0f, 100.0f, 100.0f), SKColor.FromHsv(60.0f, 100.0f, 100.0f), SKColor.FromHsv(120.0f, 100.0f, 100.0f), SKColor.FromHsv(180.0f, 100.0f, 100.0f), SKColor.FromHsv(240.0f, 100.0f, 100.0f), SKColor.FromHsv(300.0f, 100.0f, 100.0f), SKColor.FromHsv(360.0f, 100.0f, 100.0f) };
			SKPaint barPaint = new SKPaint();
			SKShader hueShader = SKShader.CreateLinearGradient(new SKPoint(0.0f, 0.0f), new SKPoint(0.0f, height), hueStops, null, SKShaderTileMode.Clamp);
			barPaint.Shader = hueShader;
			canvas.DrawRect(barRect, barPaint);
			barPaint.Dispose();

			float cursorX = m_saturation * squareRight;
			float cursorY = (1.0f - m_value) * height;
			SKPaint cursorPaint = new SKPaint();
			cursorPaint.Style = SKPaintStyle.Stroke;
			cursorPaint.StrokeWidth = 2.0f;
			cursorPaint.Color = SKColors.White;
			cursorPaint.IsAntialias = true;
			canvas.DrawCircle(cursorX, cursorY, 5.0f, cursorPaint);
			cursorPaint.Color = SKColors.Black;
			cursorPaint.StrokeWidth = 1.0f;
			canvas.DrawCircle(cursorX, cursorY, 6.0f, cursorPaint);

			float hueY = (m_hue / 360.0f) * height;
			cursorPaint.Color = SKColors.White;
			cursorPaint.StrokeWidth = 2.0f;
			canvas.DrawRect(new SKRect(barLeft - 2.0f, hueY - 2.0f, info.Width + 2.0f, hueY + 2.0f), cursorPaint);
			cursorPaint.Dispose();
		}

		private void OnGradientTouch(object sender, SKTouchEventArgs eventArgs)
		{
			bool active = eventArgs.ActionType == SKTouchAction.Pressed || (eventArgs.ActionType == SKTouchAction.Moved && eventArgs.InContact);
			if (!active)
			{
				eventArgs.Handled = true;
				return;
			}
			float width = m_gradient.CanvasSize.Width;
			float height = m_gradient.CanvasSize.Height;
			if (width <= 0.0f || height <= 0.0f)
			{
				eventArgs.Handled = true;
				return;
			}
			float squareRight = width * SquareRightFraction;
			float barLeft = width * BarLeftFraction;
			float locationX = eventArgs.Location.X;
			float locationY = eventArgs.Location.Y;
			if (locationX >= barLeft)
			{
				float hue = (locationY / height) * 360.0f;
				if (hue < 0.0f)
				{
					hue = 0.0f;
				}
				if (hue > 360.0f)
				{
					hue = 360.0f;
				}
				m_hue = hue;
			}
			else
			{
				float saturation = locationX / squareRight;
				if (saturation < 0.0f)
				{
					saturation = 0.0f;
				}
				if (saturation > 1.0f)
				{
					saturation = 1.0f;
				}
				float value = 1.0f - (locationY / height);
				if (value < 0.0f)
				{
					value = 0.0f;
				}
				if (value > 1.0f)
				{
					value = 1.0f;
				}
				m_saturation = saturation;
				m_value = value;
			}
			SyncFromHsv();
			eventArgs.Handled = true;
		}

		private void AdoptColor(SKColor color)
		{
			float hue = 0.0f;
			float saturation = 0.0f;
			float brightness = 0.0f;
			color.ToHsv(out hue, out saturation, out brightness);
			m_hue = hue;
			m_saturation = saturation / 100.0f;
			m_value = brightness / 100.0f;
			SyncFromHsv();
		}

		private void OnChannelCompleted(object sender, EventArgs eventArgs)
		{
			if (m_suppress)
			{
				return;
			}
			byte red = 0;
			byte green = 0;
			byte blue = 0;
			bool redOk = byte.TryParse(m_redEntry.Text, out red);
			bool greenOk = byte.TryParse(m_greenEntry.Text, out green);
			bool blueOk = byte.TryParse(m_blueEntry.Text, out blue);
			if (!redOk || !greenOk || !blueOk)
			{
				return;
			}
			AdoptColor(new SKColor(red, green, blue, m_alpha));
		}

		private void OnHexCompleted(object sender, EventArgs eventArgs)
		{
			if (m_suppress)
			{
				return;
			}
			string text = m_hexEntry.Text;
			if (text == null)
			{
				return;
			}
			text = text.Trim();
			if (text.StartsWith("#"))
			{
				text = text.Substring(1);
			}
			if (text.Length != 6)
			{
				return;
			}
			byte red = 0;
			byte green = 0;
			byte blue = 0;
			bool redOk = byte.TryParse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out red);
			bool greenOk = byte.TryParse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out green);
			bool blueOk = byte.TryParse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out blue);
			if (!redOk || !greenOk || !blueOk)
			{
				return;
			}
			AdoptColor(new SKColor(red, green, blue, m_alpha));
		}

		private void OnOkClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ApplyPickedColor(CurrentColor(), m_foreground);
				main.CloseModal();
			}
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CloseModal();
			}
		}

		private void OnTitlePan(object sender, PanUpdatedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.DragModal(eventArgs.StatusType, eventArgs.TotalX, eventArgs.TotalY);
			}
		}

		private View BuildTitleBar(string text)
		{
			Label titleLabel = new Label();
			titleLabel.Text = text;
			titleLabel.FontSize = 13.0;
			titleLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			titleLabel.VerticalOptions = LayoutOptions.Center;

			Button closeButton = new Button();
			closeButton.Text = "✕";
			closeButton.FontSize = 12.0;
			closeButton.WidthRequest = UiConstants.CloseButtonSize;
			closeButton.HeightRequest = UiConstants.CloseButtonSize;
			closeButton.Padding = new Thickness(0.0);
			closeButton.BackgroundColor = Colors.Transparent;
			closeButton.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			closeButton.Clicked += OnCancelClicked;

			Grid titleBar = new Grid();
			titleBar.ThemeBg(UiConstants.TitleBarLight, UiConstants.TitleBarDark);
			titleBar.Padding = new Thickness(8.0, 2.0, 2.0, 2.0);
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(titleLabel, 0);
			Grid.SetColumn(closeButton, 1);
			titleBar.Add(titleLabel);
			titleBar.Add(closeButton);

			PanGestureRecognizer pan = new PanGestureRecognizer();
			pan.PanUpdated += OnTitlePan;
			titleBar.GestureRecognizers.Add(pan);
			return titleBar;
		}

		public ColorPicker(SKColor initial, bool foreground)
		{
			m_foreground = foreground;
			m_alpha = initial.Alpha;

			m_gradient = new SKCanvasView();
			m_gradient.WidthRequest = GradientWidth;
			m_gradient.HeightRequest = GradientHeight;
			m_gradient.EnableTouchEvents = true;
			m_gradient.PaintSurface += OnGradientPaint;
			m_gradient.Touch += OnGradientTouch;

			m_preview = new BoxView();
			m_preview.WidthRequest = 60.0;
			m_preview.HeightRequest = 40.0;

			m_redEntry = BuildChannelEntry();
			m_greenEntry = BuildChannelEntry();
			m_blueEntry = BuildChannelEntry();

			m_hexEntry = new Entry();
			m_hexEntry.FontSize = 12.0;
			m_hexEntry.WidthRequest = 80.0;
			m_hexEntry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_hexEntry.Completed += OnHexCompleted;

			VerticalStackLayout fields = new VerticalStackLayout();
			fields.Spacing = 6.0;
			fields.Add(m_preview);
			fields.Add(BuildLabeledEntry("R", m_redEntry));
			fields.Add(BuildLabeledEntry("G", m_greenEntry));
			fields.Add(BuildLabeledEntry("B", m_blueEntry));
			fields.Add(BuildLabeledEntry("#", m_hexEntry));

			HorizontalStackLayout body = new HorizontalStackLayout();
			body.Spacing = 12.0;
			body.Add(m_gradient);
			body.Add(fields);

			Button okButton = new Button();
			okButton.Text = "OK";
			okButton.FontSize = 12.0;
			okButton.WidthRequest = 80.0;
			okButton.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			okButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			okButton.Clicked += OnOkClicked;

			Button cancelButton = new Button();
			cancelButton.Text = "Cancel";
			cancelButton.FontSize = 12.0;
			cancelButton.WidthRequest = 80.0;
			cancelButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			cancelButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			cancelButton.Clicked += OnCancelClicked;

			HorizontalStackLayout buttons = new HorizontalStackLayout();
			buttons.Spacing = 8.0;
			buttons.HorizontalOptions = LayoutOptions.End;
			buttons.Add(cancelButton);
			buttons.Add(okButton);

			VerticalStackLayout innerLayout = new VerticalStackLayout();
			innerLayout.Spacing = 10.0;
			innerLayout.Padding = new Thickness(12.0);
			innerLayout.Add(body);
			innerLayout.Add(buttons);

			VerticalStackLayout layout = new VerticalStackLayout();
			layout.Spacing = 0.0;
			layout.Add(BuildTitleBar("Color Picker"));
			layout.Add(innerLayout);

			Border frame = new Border();
			frame.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			frame.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			frame.StrokeThickness = 1.0;
			frame.Content = layout;

			Content = frame;
			AdoptColor(initial);
		}
	}
}
