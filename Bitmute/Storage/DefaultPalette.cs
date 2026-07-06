using SkiaSharp;

namespace Bitmute.Storage
{
	public static class DefaultPalette
	{
		private static SKBitmap BuildCheckerboard()
		{
			int size = 64;
			int cell = 8;
			SKBitmap bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(new SKColor(80, 80, 80, 255));
			SKCanvas canvas = new SKCanvas(bitmap);
			SKPaint paint = new SKPaint();
			paint.Color = new SKColor(176, 176, 176, 255);
			paint.IsAntialias = false;
			int cellCount = size / cell;
			for (int row = 0; row < cellCount; row++)
			{
				for (int col = 0; col < cellCount; col++)
				{
					bool light = ((row + col) % 2) == 0;
					if (light)
					{
						canvas.DrawRect(col * cell, row * cell, cell, cell, paint);
					}
				}
			}
			paint.Dispose();
			canvas.Dispose();
			return bitmap;
		}

		private static SKBitmap BuildDots()
		{
			int size = 32;
			SKBitmap bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(new SKColor(216, 216, 216, 255));
			SKCanvas canvas = new SKCanvas(bitmap);
			SKPaint paint = new SKPaint();
			paint.Color = new SKColor(64, 64, 64, 255);
			paint.IsAntialias = true;
			float center = size / 2.0f;
			canvas.DrawCircle(center, center, 6.0f, paint);
			paint.Dispose();
			canvas.Dispose();
			return bitmap;
		}

		private static SKBitmap BuildStripes()
		{
			int size = 64;
			int band = 8;
			SKBitmap bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(new SKColor(96, 96, 96, 255));
			SKCanvas canvas = new SKCanvas(bitmap);
			SKPaint paint = new SKPaint();
			paint.Color = new SKColor(192, 192, 192, 255);
			paint.IsAntialias = false;
			int bandCount = size / band;
			for (int index = 0; index < bandCount; index++)
			{
				if ((index % 2) == 0)
				{
					canvas.DrawRect(index * band, 0, band, size, paint);
				}
			}
			paint.Dispose();
			canvas.Dispose();
			return bitmap;
		}

		private static SKBitmap BuildHardRound()
		{
			int size = 48;
			SKBitmap bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(new SKColor(0, 0, 0, 0));
			SKCanvas canvas = new SKCanvas(bitmap);
			SKPaint paint = new SKPaint();
			paint.Color = new SKColor(0, 0, 0, 255);
			paint.IsAntialias = true;
			float center = size / 2.0f;
			canvas.DrawCircle(center, center, center - 2.0f, paint);
			paint.Dispose();
			canvas.Dispose();
			return bitmap;
		}

		private static SKBitmap BuildSoftRound()
		{
			int size = 48;
			SKBitmap bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(new SKColor(0, 0, 0, 0));
			SKCanvas canvas = new SKCanvas(bitmap);
			SKPaint paint = new SKPaint();
			float center = size / 2.0f;
			float radius = center - 2.0f;
			SKColor[] colors = new SKColor[] { new SKColor(0, 0, 0, 255), new SKColor(0, 0, 0, 0) };
			float[] stops = new float[] { 0.0f, 1.0f };
			SKShader shader = SKShader.CreateRadialGradient(new SKPoint(center, center), radius, colors, stops, SKShaderTileMode.Clamp);
			paint.Shader = shader;
			paint.IsAntialias = true;
			canvas.DrawCircle(center, center, radius, paint);
			shader.Dispose();
			paint.Dispose();
			canvas.Dispose();
			return bitmap;
		}

		public static void SeedPatterns(PatternPalette palette)
		{
			palette.AddCaptured(BuildCheckerboard(), "Checkerboard");
			palette.AddCaptured(BuildDots(), "Dots");
			palette.AddCaptured(BuildStripes(), "Stripes");
		}

		public static void SeedBrushes(BrushPalette palette)
		{
			palette.AddCapturedTip(BuildHardRound(), "Hard Round");
			palette.AddCapturedTip(BuildSoftRound(), "Soft Round");
		}
	}
}
