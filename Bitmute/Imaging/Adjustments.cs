using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class Adjustments
	{
		public static void InvertColors(SKBitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					SKColor color = bitmap.GetPixel(x, y);
					byte red = (byte)(255 - color.Red);
					byte green = (byte)(255 - color.Green);
					byte blue = (byte)(255 - color.Blue);
					SKColor inverted = new SKColor(red, green, blue, color.Alpha);
					bitmap.SetPixel(x, y, inverted);
				}
			}
		}
	}
}
