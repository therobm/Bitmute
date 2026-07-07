using System.IO;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Storage
{
	public static class ImageFile
	{
		private static string FormatNameFromPath(string path)
		{
			string lower = path.ToLowerInvariant();
			if (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg"))
			{
				return "jpeg";
			}
			if (lower.EndsWith(".bmp"))
			{
				return "bmp";
			}
			if (lower.EndsWith(".tga"))
			{
				return "tga";
			}
			if (lower.EndsWith(".webp"))
			{
				return "webp";
			}
			return "png";
		}

		public static SKBitmap DecodeFile(string path)
		{
			if (path.ToLowerInvariant().EndsWith(".tga"))
			{
				return TgaFile.Read(path);
			}
			if (path.ToLowerInvariant().EndsWith(".png"))
			{
				byte[] data = File.ReadAllBytes(path);
				SKBitmap decoded = PngFile.Decode(data);
				if (decoded != null)
				{
					return decoded;
				}
			}
			FileStream stream = File.OpenRead(path);
			SKBitmap bitmap = SKBitmap.Decode(stream);
			stream.Dispose();
			return bitmap;
		}

		public static void Encode(Document document, string path)
		{
			Export(document, path, FormatNameFromPath(path), 95, false, true);
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
			SKColorType targetColorType = SKColorType.Rgba8888;
			if (format == "png" && (document.ColorDepth() == eColorDepth.Sixteen || document.ColorDepth() == eColorDepth.ThirtyTwoFloat))
			{
				targetColorType = SKColorType.Rgba16161616;
			}
			SKBitmap composite = new SKBitmap(document.Width(), document.Height(), targetColorType, SKAlphaType.Premul);
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
