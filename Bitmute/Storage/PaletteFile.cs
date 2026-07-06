using System.IO;
using SkiaSharp;

namespace Bitmute.Storage
{
	public static class PaletteFile
	{
		private static string SanitizeBaseName(string baseName)
		{
			char[] invalid = Path.GetInvalidFileNameChars();
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			for (int i = 0; i < baseName.Length; i++)
			{
				char current = baseName[i];
				bool banned = false;
				for (int j = 0; j < invalid.Length; j++)
				{
					if (current == invalid[j])
					{
						banned = true;
						break;
					}
				}
				if (banned)
				{
					builder.Append('_');
				}
				else
				{
					builder.Append(current);
				}
			}
			string sanitized = builder.ToString();
			if (sanitized.Length == 0)
			{
				return "untitled";
			}
			return sanitized;
		}

		public static void EnsureDirectory(string dir)
		{
			Directory.CreateDirectory(dir);
		}

		public static string UniqueResourcePath(string dir, string baseName, string extension)
		{
			string sanitized = SanitizeBaseName(baseName);
			string candidate = Path.Combine(dir, sanitized + extension);
			if (!File.Exists(candidate))
			{
				return candidate;
			}
			int suffix = 2;
			for (;;)
			{
				string numbered = Path.Combine(dir, sanitized + " " + suffix + extension);
				if (!File.Exists(numbered))
				{
					return numbered;
				}
				suffix = suffix + 1;
			}
		}

		public static void WritePng(SKBitmap bitmap, string path)
		{
			SKImage image = SKImage.FromBitmap(bitmap);
			SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
			FileStream stream = File.Create(path);
			data.SaveTo(stream);
			stream.Dispose();
			data.Dispose();
			image.Dispose();
		}

		public static SKBitmap ReadPngUnpremul(string path)
		{
			FileStream stream = File.OpenRead(path);
			SKCodec codec = SKCodec.Create(stream);
			if (codec == null)
			{
				stream.Dispose();
				return null;
			}
			SKImageInfo unpremulInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			SKBitmap straight = new SKBitmap(unpremulInfo);
			SKCodecResult result = codec.GetPixels(unpremulInfo, straight.GetPixels());
			codec.Dispose();
			stream.Dispose();
			if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
			{
				straight.Dispose();
				return null;
			}
			return straight;
		}

		public static string ToRelative(string fromDir, string toFile)
		{
			string relative = Path.GetRelativePath(fromDir, toFile);
			return relative.Replace("\\", "/");
		}

		public static string ToAbsolute(string fromDir, string relativePath)
		{
			return Path.GetFullPath(Path.Combine(fromDir, relativePath));
		}
	}
}
