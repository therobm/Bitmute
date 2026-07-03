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
			if (path.ToLowerInvariant().EndsWith(".tga"))
			{
				return TgaFile.Read(path);
			}
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

		private static SKBitmap ToStraight(SKBitmap premultiplied)
		{
			SKImageInfo info = new SKImageInfo(premultiplied.Width, premultiplied.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap straight = new SKBitmap(info);
			SKPixmap pixmap = premultiplied.PeekPixels();
			pixmap.ReadPixels(info, straight.GetPixels(), straight.RowBytes, 0, 0);
			pixmap.Dispose();
			return straight;
		}

		private static bool EncodeStandard(SKBitmap composite, string path, SKEncodedImageFormat format, int quality)
		{
			SKImage image = SKImage.FromBitmap(composite);
			SKData data = image.Encode(format, quality);
			image.Dispose();
			if (data == null)
			{
				return false;
			}
			FileStream stream = File.Create(path);
			data.SaveTo(stream);
			stream.Dispose();
			data.Dispose();
			return true;
		}

		public static bool Export(Document document, string path, string format, int quality, bool lossless, bool rle)
		{
			SKBitmap composite = new SKBitmap(document.Width(), document.Height(), SKColorType.Rgba8888, SKAlphaType.Premul);
			document.CompositeInto(composite);
			bool success = false;
			if (format == "png")
			{
				success = EncodeStandard(composite, path, SKEncodedImageFormat.Png, 100);
			}
			if (format == "jpeg")
			{
				success = EncodeStandard(composite, path, SKEncodedImageFormat.Jpeg, quality);
			}
			if (format == "bmp")
			{
				success = EncodeStandard(composite, path, SKEncodedImageFormat.Bmp, 100);
			}
			if (format == "tga")
			{
				SKBitmap straight = ToStraight(composite);
				success = TgaFile.Write(path, straight, rle);
				straight.Dispose();
			}
			if (format == "webp")
			{
				SKWebpEncoderCompression compression = SKWebpEncoderCompression.Lossy;
				if (lossless)
				{
					compression = SKWebpEncoderCompression.Lossless;
				}
				SKWebpEncoderOptions options = new SKWebpEncoderOptions(compression, quality);
				SKPixmap pixmap = composite.PeekPixels();
				SKFileWStream stream = new SKFileWStream(path);
				success = pixmap.Encode(stream, options);
				stream.Dispose();
				pixmap.Dispose();
			}
			composite.Dispose();
			return success;
		}
	}
}
