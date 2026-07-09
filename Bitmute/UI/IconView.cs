using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Storage;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class IconView : SKCanvasView
	{
		private static Dictionary<string, SKBitmap> s_cache = new Dictionary<string, SKBitmap>();
		private static readonly SKColorFilter s_darkInvertFilter = SKColorFilter.CreateColorMatrix(new float[]
		{
			-1.0f, 0.0f, 0.0f, 0.0f, 1.0f,
			0.0f, -1.0f, 0.0f, 0.0f, 1.0f,
			0.0f, 0.0f, -1.0f, 0.0f, 1.0f,
			0.0f, 0.0f, 0.0f, 1.0f, 0.0f
		});

		private string m_name;
		private bool m_loadStarted;

		public IconView(string name)
		{
			m_name = name;
			PaintSurface += OnPaintSurface;
			Theme.Changed += OnThemeChanged;
		}

		public void SetIcon(string name)
		{
			if (m_name == name)
			{
				return;
			}
			m_name = name;
			m_loadStarted = false;
			InvalidateSurface();
		}

		private void OnThemeChanged(object sender, EventArgs eventArgs)
		{
			InvalidateSurface();
		}

		private async void LoadAsync()
		{
			try
			{
				if (!s_cache.ContainsKey(m_name))
				{
					Stream stream = await FileSystem.OpenAppPackageFileAsync(m_name);
					SKBitmap bitmap = SKBitmap.Decode(stream);
					stream.Dispose();
					s_cache[m_name] = bitmap;
				}
				InvalidateSurface();
			}
			catch (Exception)
			{
			}
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			SKCanvas canvas = eventArgs.Surface.Canvas;
			canvas.Clear(SKColors.Transparent);

			SKBitmap bitmap = null;
			if (s_cache.ContainsKey(m_name))
			{
				bitmap = s_cache[m_name];
			}
			if (bitmap == null)
			{
				if (!m_loadStarted)
				{
					m_loadStarted = true;
					LoadAsync();
				}
				return;
			}

			SKPaint paint = new SKPaint();
			paint.IsAntialias = true;
			if (Theme.IsDark())
			{
				paint.ColorFilter = s_darkInvertFilter;
			}
			SKRect destination = new SKRect(0.0f, 0.0f, eventArgs.Info.Width, eventArgs.Info.Height);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
			SKPixmap pixmap = bitmap.PeekPixels();
			SKImage image = SKImage.FromPixels(pixmap);
			canvas.DrawImage(image, destination, sampling, paint);
			image.Dispose();
			pixmap.Dispose();
			paint.Dispose();
		}
	}
}
