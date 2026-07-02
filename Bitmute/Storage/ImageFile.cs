using System.IO;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Storage
{
	public static class ImageFile
	{
		private static SKEncodedImageFormat FormatFromPath(string path)
		{
			string lower = path.ToLowerInvariant();
			if (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg"))
			{
				return SKEncodedImageFormat.Jpeg;
			}
			if (lower.EndsWith(".bmp"))
			{
				return SKEncodedImageFormat.Bmp;
			}
			return SKEncodedImageFormat.Png;
		}

		public static SKBitmap DecodeFile(string path)
		{
			FileStream stream = File.OpenRead(path);
			SKBitmap bitmap = SKBitmap.Decode(stream);
			stream.Dispose();
			return bitmap;
		}

		public static void Encode(Document document, string path)
		{
			SKBitmap composite = new SKBitmap(document.Width(), document.Height(), SKColorType.Rgba8888, SKAlphaType.Premul);
			document.CompositeInto(composite);
			SKImage image = SKImage.FromBitmap(composite);
			SKEncodedImageFormat format = FormatFromPath(path);
			SKData data = image.Encode(format, 95);
			FileStream stream = File.Create(path);
			data.SaveTo(stream);
			stream.Dispose();
			data.Dispose();
			image.Dispose();
			composite.Dispose();
		}
	}
}
