using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Storage
{
	public static class BitmuteFile
	{
		private const string MimeType = "image/x-bitmute";
		private const int FormatVersion = 1;
		private const int MinReaderVersion = 1;
		private const int ThumbnailMaxSide = 256;

		private static byte ClampByte(int value)
		{
			if (value < 0)
			{
				value = 0;
			}
			if (value > 255)
			{
				value = 255;
			}
			return (byte)value;
		}

		private static void ReadExact(Stream stream, byte[] buffer, int count)
		{
			int total = 0;
			for (;;)
			{
				if (total >= count)
				{
					break;
				}
				int read = stream.Read(buffer, total, count - total);
				if (read <= 0)
				{
					throw new EndOfStreamException();
				}
				total = total + read;
			}
		}

		private static byte[] ReadAllBytes(Stream stream)
		{
			MemoryStream memory = new MemoryStream();
			stream.CopyTo(memory);
			byte[] bytes = memory.ToArray();
			memory.Dispose();
			return bytes;
		}

		private static int ReadInt32(Stream stream)
		{
			byte[] buffer = new byte[4];
			ReadExact(stream, buffer, 4);
			return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
		}

		private static void WriteInt32(Stream stream, int value)
		{
			byte[] buffer = new byte[4];
			buffer[0] = (byte)(value & 255);
			buffer[1] = (byte)((value >> 8) & 255);
			buffer[2] = (byte)((value >> 16) & 255);
			buffer[3] = (byte)((value >> 24) & 255);
			stream.Write(buffer, 0, 4);
		}

		private static string ReadString(JsonElement parent, string name, string fallback)
		{
			JsonElement element;
			if (!parent.TryGetProperty(name, out element))
			{
				return fallback;
			}
			if (element.ValueKind != JsonValueKind.String)
			{
				return fallback;
			}
			string value = element.GetString();
			if (value == null)
			{
				return fallback;
			}
			return value;
		}

		private static int ReadInt(JsonElement parent, string name, int fallback)
		{
			JsonElement element;
			if (!parent.TryGetProperty(name, out element))
			{
				return fallback;
			}
			if (element.ValueKind != JsonValueKind.Number)
			{
				return fallback;
			}
			int value;
			if (!element.TryGetInt32(out value))
			{
				return fallback;
			}
			return value;
		}

		private static float ReadFloat(JsonElement parent, string name, float fallback)
		{
			JsonElement element;
			if (!parent.TryGetProperty(name, out element))
			{
				return fallback;
			}
			if (element.ValueKind != JsonValueKind.Number)
			{
				return fallback;
			}
			return element.GetSingle();
		}

		private static bool ReadBool(JsonElement parent, string name, bool fallback)
		{
			JsonElement element;
			if (!parent.TryGetProperty(name, out element))
			{
				return fallback;
			}
			if (element.ValueKind == JsonValueKind.True)
			{
				return true;
			}
			if (element.ValueKind == JsonValueKind.False)
			{
				return false;
			}
			return fallback;
		}

		private static SKRectI ComputeMaskBounds(byte[] mask, int width, int height)
		{
			int minX = width;
			int minY = height;
			int maxX = -1;
			int maxY = -1;
			for (int y = 0; y < height; y++)
			{
				int rowStart = y * width;
				for (int x = 0; x < width; x++)
				{
					if (mask[rowStart + x] == 0)
					{
						continue;
					}
					if (x < minX)
					{
						minX = x;
					}
					if (x > maxX)
					{
						maxX = x;
					}
					if (y < minY)
					{
						minY = y;
					}
					if (y > maxY)
					{
						maxY = y;
					}
				}
			}
			if (maxX < 0)
			{
				return SKRectI.Empty;
			}
			return new SKRectI(minX, minY, maxX + 1, maxY + 1);
		}

		private static void WriteMimetype(ZipArchive archive)
		{
			ZipArchiveEntry entry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
			Stream stream = entry.Open();
			byte[] bytes = Encoding.ASCII.GetBytes(MimeType);
			stream.Write(bytes, 0, bytes.Length);
			stream.Dispose();
		}

		private static void WriteManifest(ZipArchive archive, Document document)
		{
			ZipArchiveEntry entry = archive.CreateEntry("manifest.json");
			Stream stream = entry.Open();
			Utf8JsonWriter writer = new Utf8JsonWriter(stream);
			writer.WriteStartObject();
			writer.WriteNumber("version", FormatVersion);
			writer.WriteNumber("minReaderVersion", MinReaderVersion);
			string title = document.Title();
			if (title != null)
			{
				writer.WriteString("title", title);
			}
			writer.WriteNumber("width", document.Width());
			writer.WriteNumber("height", document.Height());
			writer.WriteNumber("activeLayerIndex", document.ActiveLayerIndex());
			writer.WriteStartArray("layers");
			List<Layer> layers = document.Layers();
			for (int index = 0; index < layers.Count; index++)
			{
				Layer layer = layers[index];
				SKBitmap bitmap = layer.Bitmap();
				writer.WriteStartObject();
				writer.WriteString("name", layer.Name());
				writer.WriteBoolean("visible", layer.IsVisible());
				writer.WriteNumber("opacity", (int)layer.Opacity());
				writer.WriteNumber("blendMode", (int)layer.BlendMode());
				writer.WriteNumber("offsetX", layer.OffsetX());
				writer.WriteNumber("offsetY", layer.OffsetY());
				writer.WriteBoolean("isBackground", layer.IsBackground());
				writer.WriteBoolean("isText", layer.IsText());
				writer.WriteNumber("bitmapWidth", bitmap.Width);
				writer.WriteNumber("bitmapHeight", bitmap.Height);
				if (layer.IsText())
				{
					SKColor textColor = layer.TextColor();
					writer.WriteString("text", layer.Text());
					writer.WriteNumber("textX", layer.TextX());
					writer.WriteNumber("textY", layer.TextY());
					writer.WriteString("textFontFamily", layer.TextFontFamily());
					writer.WriteNumber("textSize", layer.TextSize());
					writer.WriteBoolean("textBold", layer.TextBold());
					writer.WriteBoolean("textItalic", layer.TextItalic());
					writer.WriteNumber("textColorRed", (int)textColor.Red);
					writer.WriteNumber("textColorGreen", (int)textColor.Green);
					writer.WriteNumber("textColorBlue", (int)textColor.Blue);
					writer.WriteNumber("textColorAlpha", (int)textColor.Alpha);
					writer.WriteNumber("textAlign", layer.TextAlign());
					writer.WriteNumber("textAntiAlias", layer.TextAntiAlias());
					writer.WriteBoolean("textLeadingAuto", layer.TextLeadingAuto());
					writer.WriteNumber("textLeading", layer.TextLeading());
					writer.WriteNumber("textTracking", layer.TextTracking());
					writer.WriteNumber("textHorizontalScale", layer.TextHorizontalScale());
					writer.WriteNumber("textVerticalScale", layer.TextVerticalScale());
					writer.WriteNumber("textBaselineShift", layer.TextBaselineShift());
					writer.WriteBoolean("textFauxBold", layer.TextFauxBold());
					writer.WriteBoolean("textFauxItalic", layer.TextFauxItalic());
					writer.WriteBoolean("textKerningAuto", layer.TextKerningAuto());
				}
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteBoolean("hasSelection", document.Selection().IsActive());
			Guides guides = document.Guides();
			writer.WriteStartObject("guides");
			writer.WriteBoolean("locked", guides.IsLocked());
			writer.WriteStartArray("horizontal");
			List<int> horizontalGuides = guides.HorizontalGuides();
			for (int index = 0; index < horizontalGuides.Count; index++)
			{
				writer.WriteNumberValue(horizontalGuides[index]);
			}
			writer.WriteEndArray();
			writer.WriteStartArray("vertical");
			List<int> verticalGuides = guides.VerticalGuides();
			for (int index = 0; index < verticalGuides.Count; index++)
			{
				writer.WriteNumberValue(verticalGuides[index]);
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
			writer.WriteStartArray("slices");
			Slices slices = document.Slices();
			for (int index = 0; index < slices.Count(); index++)
			{
				SkiaSharp.SKRectI sliceRect = slices.RectAt(index);
				writer.WriteStartObject();
				writer.WriteString("name", slices.NameAt(index));
				writer.WriteNumber("left", sliceRect.Left);
				writer.WriteNumber("top", sliceRect.Top);
				writer.WriteNumber("right", sliceRect.Right);
				writer.WriteNumber("bottom", sliceRect.Bottom);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
			writer.Flush();
			writer.Dispose();
			stream.Dispose();
		}

		private static void WriteThumbnail(ZipArchive archive, Document document)
		{
			int width = document.Width();
			int height = document.Height();
			SKBitmap composite = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			document.CompositeInto(composite);
			SKBitmap thumbnail = composite;
			int longest = width;
			if (height > longest)
			{
				longest = height;
			}
			if (longest > ThumbnailMaxSide)
			{
				int thumbnailWidth = (width * ThumbnailMaxSide) / longest;
				int thumbnailHeight = (height * ThumbnailMaxSide) / longest;
				if (thumbnailWidth < 1)
				{
					thumbnailWidth = 1;
				}
				if (thumbnailHeight < 1)
				{
					thumbnailHeight = 1;
				}
				SKBitmap scaled = new SKBitmap(thumbnailWidth, thumbnailHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
				scaled.Erase(SKColors.Transparent);
				SKCanvas canvas = new SKCanvas(scaled);
				SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
				SKRect destinationRect = new SKRect(0.0f, 0.0f, thumbnailWidth, thumbnailHeight);
				SKImage compositeImage = SKImage.FromBitmap(composite);
				SKPaint paint = new SKPaint();
				paint.BlendMode = SKBlendMode.Src;
				canvas.DrawImage(compositeImage, destinationRect, sampling, paint);
				paint.Dispose();
				compositeImage.Dispose();
				canvas.Dispose();
				thumbnail = scaled;
			}
			SKImage thumbnailImage = SKImage.FromBitmap(thumbnail);
			SKData data = thumbnailImage.Encode(SKEncodedImageFormat.Png, 90);
			ZipArchiveEntry entry = archive.CreateEntry("thumbnail.png");
			Stream stream = entry.Open();
			data.SaveTo(stream);
			stream.Dispose();
			data.Dispose();
			thumbnailImage.Dispose();
			if (thumbnail != composite)
			{
				thumbnail.Dispose();
			}
			composite.Dispose();
		}

		private static void WriteLayerPixels(ZipArchive archive, int index, Layer layer)
		{
			SKBitmap bitmap = layer.Bitmap();
			int width = bitmap.Width;
			int height = bitmap.Height;
			ZipArchiveEntry entry = archive.CreateEntry("layers/" + index.ToString() + ".dat");
			Stream stream = entry.Open();
			WriteInt32(stream, width);
			WriteInt32(stream, height);
			int rowLength = width * 4;
			byte[] rowBuffer = new byte[rowLength];
			IntPtr basePointer = bitmap.GetPixels();
			int rowBytes = bitmap.RowBytes;
			for (int y = 0; y < height; y++)
			{
				Marshal.Copy(IntPtr.Add(basePointer, y * rowBytes), rowBuffer, 0, rowLength);
				stream.Write(rowBuffer, 0, rowLength);
			}
			stream.Dispose();
		}

		private static void WriteSelectionMask(ZipArchive archive, Document document)
		{
			ZipArchiveEntry entry = archive.CreateEntry("selection.dat");
			Stream stream = entry.Open();
			WriteInt32(stream, document.Width());
			WriteInt32(stream, document.Height());
			byte[] mask = document.Selection().Mask();
			stream.Write(mask, 0, mask.Length);
			stream.Dispose();
		}

		private static bool ReadLayerPixels(Stream stream, Layer layer)
		{
			int width = ReadInt32(stream);
			int height = ReadInt32(stream);
			SKBitmap bitmap = layer.Bitmap();
			if (width != bitmap.Width || height != bitmap.Height)
			{
				return false;
			}
			int rowLength = width * 4;
			byte[] rowBuffer = new byte[rowLength];
			IntPtr basePointer = bitmap.GetPixels();
			int rowBytes = bitmap.RowBytes;
			for (int y = 0; y < height; y++)
			{
				ReadExact(stream, rowBuffer, rowLength);
				Marshal.Copy(rowBuffer, 0, IntPtr.Add(basePointer, y * rowBytes), rowLength);
			}
			return true;
		}

		private static Layer ReadLayer(ZipArchive archive, JsonElement layerElement, int index, int documentWidth, int documentHeight)
		{
			string name = ReadString(layerElement, "name", "Layer");
			int bitmapWidth = ReadInt(layerElement, "bitmapWidth", documentWidth);
			int bitmapHeight = ReadInt(layerElement, "bitmapHeight", documentHeight);
			if (bitmapWidth <= 0 || bitmapHeight <= 0)
			{
				return null;
			}
			ZipArchiveEntry entry = archive.GetEntry("layers/" + index.ToString() + ".dat");
			if (entry == null)
			{
				return null;
			}
			Layer layer = new Layer(name, bitmapWidth, bitmapHeight);
			Stream stream = entry.Open();
			bool loaded = ReadLayerPixels(stream, layer);
			stream.Dispose();
			if (!loaded)
			{
				return null;
			}
			layer.SetOffset(ReadInt(layerElement, "offsetX", 0), ReadInt(layerElement, "offsetY", 0));
			layer.SetOpacity(ClampByte(ReadInt(layerElement, "opacity", 255)));
			int blendMode = ReadInt(layerElement, "blendMode", 0);
			if (blendMode < (int)eBlendMode.Normal || blendMode > (int)eBlendMode.Luminosity)
			{
				blendMode = (int)eBlendMode.Normal;
			}
			layer.SetBlendMode((eBlendMode)blendMode);
			layer.SetVisible(ReadBool(layerElement, "visible", true));
			layer.SetIsBackground(ReadBool(layerElement, "isBackground", false));
			if (ReadBool(layerElement, "isText", false))
			{
				layer.SetTextPosition(ReadInt(layerElement, "textX", 0), ReadInt(layerElement, "textY", 0));
				layer.SetTextString(ReadString(layerElement, "text", ""));
				byte red = ClampByte(ReadInt(layerElement, "textColorRed", 0));
				byte green = ClampByte(ReadInt(layerElement, "textColorGreen", 0));
				byte blue = ClampByte(ReadInt(layerElement, "textColorBlue", 0));
				byte alpha = ClampByte(ReadInt(layerElement, "textColorAlpha", 255));
				layer.SetTextStyle(
					ReadFloat(layerElement, "textSize", 32.0f),
					ReadString(layerElement, "textFontFamily", "Segoe UI"),
					ReadBool(layerElement, "textBold", false),
					ReadBool(layerElement, "textItalic", false),
					new SKColor(red, green, blue, alpha),
					ReadInt(layerElement, "textAlign", 0),
					ReadInt(layerElement, "textAntiAlias", 3));
				layer.SetTextCharacter(
					ReadBool(layerElement, "textLeadingAuto", true),
					ReadFloat(layerElement, "textLeading", 0.0f),
					ReadInt(layerElement, "textTracking", 0),
					ReadInt(layerElement, "textHorizontalScale", 100),
					ReadInt(layerElement, "textVerticalScale", 100),
					ReadInt(layerElement, "textBaselineShift", 0),
					ReadBool(layerElement, "textFauxBold", false),
					ReadBool(layerElement, "textFauxItalic", false),
					ReadBool(layerElement, "textKerningAuto", true));
			}
			return layer;
		}

		private static void ReadSelectionMask(ZipArchive archive, Document document)
		{
			ZipArchiveEntry entry = archive.GetEntry("selection.dat");
			if (entry == null)
			{
				return;
			}
			Stream stream = entry.Open();
			int maskWidth = ReadInt32(stream);
			int maskHeight = ReadInt32(stream);
			if (maskWidth != document.Width() || maskHeight != document.Height())
			{
				stream.Dispose();
				return;
			}
			byte[] mask = new byte[maskWidth * maskHeight];
			ReadExact(stream, mask, mask.Length);
			stream.Dispose();
			SKRectI bounds = ComputeMaskBounds(mask, maskWidth, maskHeight);
			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return;
			}
			document.Selection().SelectMask(mask, bounds);
		}

		private static Document ReadArchive(ZipArchive archive)
		{
			ZipArchiveEntry mimeEntry = archive.GetEntry("mimetype");
			if (mimeEntry == null)
			{
				return null;
			}
			Stream mimeStream = mimeEntry.Open();
			byte[] mimeBytes = ReadAllBytes(mimeStream);
			mimeStream.Dispose();
			if (Encoding.ASCII.GetString(mimeBytes) != MimeType)
			{
				return null;
			}
			ZipArchiveEntry manifestEntry = archive.GetEntry("manifest.json");
			if (manifestEntry == null)
			{
				return null;
			}
			Stream manifestStream = manifestEntry.Open();
			byte[] manifestBytes = ReadAllBytes(manifestStream);
			manifestStream.Dispose();
			JsonDocument manifest = JsonDocument.Parse(manifestBytes);
			JsonElement root = manifest.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				manifest.Dispose();
				return null;
			}
			if (ReadInt(root, "minReaderVersion", 1) > MinReaderVersion)
			{
				manifest.Dispose();
				return null;
			}
			int width = ReadInt(root, "width", 0);
			int height = ReadInt(root, "height", 0);
			if (width <= 0 || height <= 0)
			{
				manifest.Dispose();
				return null;
			}
			JsonElement layersElement;
			if (!root.TryGetProperty("layers", out layersElement))
			{
				manifest.Dispose();
				return null;
			}
			if (layersElement.ValueKind != JsonValueKind.Array)
			{
				manifest.Dispose();
				return null;
			}
			string title = ReadString(root, "title", "Untitled");
			Document document = new Document(title, width, height);
			document.Layers().Clear();
			int layerIndex = 0;
			foreach (JsonElement layerElement in layersElement.EnumerateArray())
			{
				Layer layer = ReadLayer(archive, layerElement, layerIndex, width, height);
				if (layer == null)
				{
					manifest.Dispose();
					return null;
				}
				document.Layers().Add(layer);
				layerIndex = layerIndex + 1;
			}
			if (document.Layers().Count == 0)
			{
				manifest.Dispose();
				return null;
			}
			document.SetActiveLayerIndex(document.Layers().Count - 1);
			document.SetActiveLayerIndex(ReadInt(root, "activeLayerIndex", 0));
			bool hasSelection = ReadBool(root, "hasSelection", false);
			System.Text.Json.JsonElement guidesElement;
			if (root.TryGetProperty("guides", out guidesElement))
			{
				Guides guides = document.Guides();
				System.Text.Json.JsonElement horizontalElement;
				if (guidesElement.TryGetProperty("horizontal", out horizontalElement))
				{
					foreach (System.Text.Json.JsonElement value in horizontalElement.EnumerateArray())
					{
						guides.AddHorizontal(value.GetInt32());
					}
				}
				System.Text.Json.JsonElement verticalElement;
				if (guidesElement.TryGetProperty("vertical", out verticalElement))
				{
					foreach (System.Text.Json.JsonElement value in verticalElement.EnumerateArray())
					{
						guides.AddVertical(value.GetInt32());
					}
				}
				System.Text.Json.JsonElement lockedElement;
				if (guidesElement.TryGetProperty("locked", out lockedElement))
				{
					if (lockedElement.GetBoolean())
					{
						guides.SetLocked(true);
					}
				}
			}
			System.Text.Json.JsonElement slicesElement;
			if (root.TryGetProperty("slices", out slicesElement))
			{
				Slices slices = document.Slices();
				foreach (System.Text.Json.JsonElement sliceEntry in slicesElement.EnumerateArray())
				{
					string sliceName = "";
					System.Text.Json.JsonElement nameElement;
					if (sliceEntry.TryGetProperty("name", out nameElement))
					{
						sliceName = nameElement.GetString();
					}
					int sliceLeft = ReadInt(sliceEntry, "left", 0);
					int sliceTop = ReadInt(sliceEntry, "top", 0);
					int sliceRight = ReadInt(sliceEntry, "right", 0);
					int sliceBottom = ReadInt(sliceEntry, "bottom", 0);
					slices.Add(sliceName, new SkiaSharp.SKRectI(sliceLeft, sliceTop, sliceRight, sliceBottom));
				}
			}
			manifest.Dispose();
			if (hasSelection)
			{
				ReadSelectionMask(archive, document);
			}
			document.MarkComposeDirtyAll();
			return document;
		}

		public static bool Write(string path, Document document)
		{
			try
			{
				FileStream fileStream = File.Create(path);
				ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
				WriteMimetype(archive);
				WriteManifest(archive, document);
				WriteThumbnail(archive, document);
				List<Layer> layers = document.Layers();
				for (int index = 0; index < layers.Count; index++)
				{
					WriteLayerPixels(archive, index, layers[index]);
				}
				if (document.Selection().IsActive())
				{
					WriteSelectionMask(archive, document);
				}
				archive.Dispose();
				fileStream.Dispose();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static Document Read(string path)
		{
			try
			{
				FileStream fileStream = File.OpenRead(path);
				ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
				Document document = ReadArchive(archive);
				archive.Dispose();
				fileStream.Dispose();
				return document;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
