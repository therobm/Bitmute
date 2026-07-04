using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using Bitmute.Imaging;

namespace Bitmute.UI
{
	public class ChannelsPanel : ContentView
	{
		private const int ThumbWidth = 40;
		private const int ThumbHeight = 24;

		private string[] m_names;
		private Border[] m_rows;
		private Label[] m_labels;
		private Image[] m_thumbs;
		private Border[] m_eyes;
		private IconView[] m_eyeIcons;

		private static Microsoft.Maui.Controls.ImageSource ToImageSource(SKBitmap bitmap)
		{
			SKBitmapImageSource source = new SKBitmapImageSource();
			source.Bitmap = bitmap;
			return source;
		}

		private Border BuildRow(int index)
		{
			Image thumb = new Image();
			thumb.WidthRequest = ThumbWidth;
			thumb.HeightRequest = ThumbHeight;
			thumb.Aspect = Aspect.AspectFit;
			thumb.VerticalOptions = LayoutOptions.Center;
			m_thumbs[index] = thumb;

			Label label = new Label();
			label.Text = m_names[index];
			label.FontSize = 12.0;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;
			m_labels[index] = label;

			IconView eyeIcon = new IconView("eye.png");
			eyeIcon.WidthRequest = 16.0;
			eyeIcon.HeightRequest = 16.0;
			eyeIcon.BackgroundColor = Colors.Transparent;
			eyeIcon.HorizontalOptions = LayoutOptions.Center;
			eyeIcon.VerticalOptions = LayoutOptions.Center;
			m_eyeIcons[index] = eyeIcon;

			Border eye = new Border();
			eye.WidthRequest = 22.0;
			eye.HeightRequest = 20.0;
			eye.Padding = new Thickness(0.0);
			eye.BackgroundColor = Colors.Transparent;
			eye.StrokeThickness = 0.0;
			eye.Content = eyeIcon;
			ToolTipProperties.SetText(eye, "Toggle channel visibility");
			TapGestureRecognizer eyeTap = new TapGestureRecognizer();
			eyeTap.Tapped += OnEyeTapped;
			eye.GestureRecognizers.Add(eyeTap);
			m_eyes[index] = eye;

			HorizontalStackLayout content = new HorizontalStackLayout();
			content.Spacing = 8.0;
			content.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
			content.Add(eye);
			content.Add(thumb);
			content.Add(label);

			Border row = new Border();
			row.StrokeThickness = 0.0;
			row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			row.Content = content;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnRowTapped;
			row.GestureRecognizers.Add(tap);
			return row;
		}

		private void OnRowTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_rows.Length; index++)
			{
				if (ReferenceEquals(m_rows[index], sender))
				{
					MainView main = MainView.Self;
					if (main != null)
					{
						main.SelectChannelView(index - 1);
					}
					return;
				}
			}
		}

		private void OnEyeTapped(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			for (int index = 0; index < m_eyes.Length; index++)
			{
				if (ReferenceEquals(m_eyes[index], sender))
				{
					if (index == 0)
					{
						main.ToggleRgbChannelsVisible();
					}
					else
					{
						main.ToggleChannelVisible(index - 1);
					}
					return;
				}
			}
		}

		private void RefreshEyes(MainView main)
		{
			if (main == null)
			{
				return;
			}
			for (int index = 0; index < m_eyeIcons.Length; index++)
			{
				bool visible = true;
				if (index == 0)
				{
					visible = main.RgbChannelsVisible();
				}
				else
				{
					visible = main.ChannelVisible(index - 1);
				}
				string icon = "eye.png";
				if (!visible)
				{
					icon = "eye_off.png";
				}
				if (m_eyeIcons[index] != null)
				{
					m_eyeIcons[index].SetIcon(icon);
				}
			}
		}

		private void RefreshHighlight(int mode)
		{
			for (int index = 0; index < m_rows.Length; index++)
			{
				bool active = (index - 1) == mode;
				if (active)
				{
					m_rows[index].ThemeBg(UiConstants.ToolSelectedLight, UiConstants.ToolSelectedDark);
				}
				else
				{
					m_rows[index].ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
				}
			}
		}

		private SKBitmap BuildCompositeThumbnail(Document document)
		{
			int width = document.Width();
			int height = document.Height();
			if (width <= 0 || height <= 0)
			{
				return null;
			}
			SKBitmap composite = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			document.CompositeInto(composite);
			double scale = (double)ThumbWidth / width;
			double scaleY = (double)ThumbHeight / height;
			if (scaleY < scale)
			{
				scale = scaleY;
			}
			int thumbWidth = (int)System.Math.Round(width * scale);
			int thumbHeight = (int)System.Math.Round(height * scale);
			if (thumbWidth < 1)
			{
				thumbWidth = 1;
			}
			if (thumbHeight < 1)
			{
				thumbHeight = 1;
			}
			SKBitmap thumbnail = new SKBitmap(thumbWidth, thumbHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
			SKCanvas canvas = new SKCanvas(thumbnail);
			SKImage image = SKImage.FromBitmap(composite);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			canvas.DrawImage(image, new SKRect(0.0f, 0.0f, width, height), new SKRect(0.0f, 0.0f, thumbWidth, thumbHeight), sampling, paint);
			paint.Dispose();
			image.Dispose();
			canvas.Dispose();
			composite.Dispose();
			return thumbnail;
		}

		private void ClearThumbnails()
		{
			for (int index = 0; index < m_thumbs.Length; index++)
			{
				if (m_thumbs[index] != null)
				{
					m_thumbs[index].Source = null;
				}
			}
		}

		public void Refresh()
		{
			MainView main = MainView.Self;
			int mode = -1;
			if (main != null)
			{
				mode = main.ChannelViewMode();
			}
			RefreshHighlight(mode);
			RefreshEyes(main);
			if (!IsVisible)
			{
				return;
			}
			Document document = null;
			if (main != null)
			{
				document = main.ActiveDocument();
			}
			if (document == null)
			{
				ClearThumbnails();
				return;
			}
			SKBitmap thumbnail = BuildCompositeThumbnail(document);
			if (thumbnail == null)
			{
				return;
			}
			m_thumbs[0].Source = ToImageSource(thumbnail);
			for (int channel = 0; channel < 4; channel++)
			{
				SKBitmap channelThumb = new SKBitmap(thumbnail.Width, thumbnail.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				ChannelRender.Render(thumbnail, channelThumb, channel);
				m_thumbs[channel + 1].Source = ToImageSource(channelThumb);
			}
		}

		public ChannelsPanel()
		{
			m_names = new string[] { "RGB", "Red", "Green", "Blue", "Alpha" };
			m_rows = new Border[m_names.Length];
			m_labels = new Label[m_names.Length];
			m_thumbs = new Image[m_names.Length];
			m_eyes = new Border[m_names.Length];
			m_eyeIcons = new IconView[m_names.Length];

			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 1.0;
			list.Padding = new Thickness(4.0);
			for (int index = 0; index < m_names.Length; index++)
			{
				Border row = BuildRow(index);
				m_rows[index] = row;
				list.Add(row);
			}

			this.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			Content = list;
			Refresh();
		}
	}
}
