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

		private static eBlendMode ReadBlendMode(JsonElement parent, string name)
		{
			int value = ReadInt(parent, name, 0);
			if (value < (int)eBlendMode.Normal || value > (int)eBlendMode.Luminosity)
			{
				return eBlendMode.Normal;
			}
			return (eBlendMode)value;
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
			writer.WriteNumber("colorDepth", (int)document.ColorDepth());
			writer.WriteNumber("activeLayerIndex", document.ActiveLayerIndex());
			writer.WriteNumber("rulerUnits", (int)document.RulerUnits());
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
			writer.WriteBoolean("lockAll", layer.LockAll());
			writer.WriteBoolean("lockPixels", layer.LockPixels());
			writer.WriteBoolean("lockPosition", layer.LockPosition());
			writer.WriteBoolean("lockAlpha", layer.LockAlpha());
				writer.WriteBoolean("isText", layer.IsText());
				writer.WriteBoolean("hasMask", layer.HasMask());
				writer.WriteBoolean("maskEnabled", layer.MaskEnabled());
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
				LayerStyle layerStyle = layer.LayerStyle();
				SKColor strokeColor = layerStyle.m_strokeColor;
				SKColor shadowColor = layerStyle.m_shadowColor;
				SKColor glowColor = layerStyle.m_glowColor;
				SKColor innerGlowColor = layerStyle.m_innerGlowColor;
				SKColor bevelHighlightColor = layerStyle.m_bevelHighlightColor;
				SKColor bevelShadowColor = layerStyle.m_bevelShadowColor;
				writer.WriteStartObject("layerStyle");
				writer.WriteBoolean("hasStroke", layerStyle.m_hasStroke);
				writer.WriteNumber("strokeSize", layerStyle.m_strokeSize);
				writer.WriteNumber("strokePosition", layerStyle.m_strokePosition);
				writer.WriteNumber("strokeColorRed", (int)strokeColor.Red);
				writer.WriteNumber("strokeColorGreen", (int)strokeColor.Green);
				writer.WriteNumber("strokeColorBlue", (int)strokeColor.Blue);
				writer.WriteNumber("strokeColorAlpha", (int)strokeColor.Alpha);
				writer.WriteNumber("strokeOpacity", layerStyle.m_strokeOpacity);
				writer.WriteNumber("strokeBlendMode", (int)layerStyle.m_strokeBlendMode);
				writer.WriteBoolean("hasDropShadow", layerStyle.m_hasDropShadow);
				writer.WriteNumber("shadowColorRed", (int)shadowColor.Red);
				writer.WriteNumber("shadowColorGreen", (int)shadowColor.Green);
				writer.WriteNumber("shadowColorBlue", (int)shadowColor.Blue);
				writer.WriteNumber("shadowColorAlpha", (int)shadowColor.Alpha);
				writer.WriteNumber("shadowOpacity", layerStyle.m_shadowOpacity);
				writer.WriteNumber("shadowAngle", layerStyle.m_shadowAngle);
				writer.WriteNumber("shadowDistance", layerStyle.m_shadowDistance);
				writer.WriteNumber("shadowSize", layerStyle.m_shadowSize);
				writer.WriteNumber("shadowSpread", layerStyle.m_shadowSpread);
				writer.WriteNumber("shadowBlendMode", (int)layerStyle.m_shadowBlendMode);
				writer.WriteBoolean("hasOuterGlow", layerStyle.m_hasOuterGlow);
				writer.WriteNumber("glowColorRed", (int)glowColor.Red);
				writer.WriteNumber("glowColorGreen", (int)glowColor.Green);
				writer.WriteNumber("glowColorBlue", (int)glowColor.Blue);
				writer.WriteNumber("glowColorAlpha", (int)glowColor.Alpha);
				writer.WriteNumber("glowOpacity", layerStyle.m_glowOpacity);
				writer.WriteNumber("glowSize", layerStyle.m_glowSize);
				writer.WriteNumber("glowSpread", layerStyle.m_glowSpread);
				writer.WriteNumber("glowBlendMode", (int)layerStyle.m_glowBlendMode);
				writer.WriteBoolean("hasInnerGlow", layerStyle.m_hasInnerGlow);
				writer.WriteNumber("innerGlowColorRed", (int)innerGlowColor.Red);
				writer.WriteNumber("innerGlowColorGreen", (int)innerGlowColor.Green);
				writer.WriteNumber("innerGlowColorBlue", (int)innerGlowColor.Blue);
				writer.WriteNumber("innerGlowColorAlpha", (int)innerGlowColor.Alpha);
				writer.WriteNumber("innerGlowOpacity", layerStyle.m_innerGlowOpacity);
				writer.WriteNumber("innerGlowSize", layerStyle.m_innerGlowSize);
				writer.WriteNumber("innerGlowSpread", layerStyle.m_innerGlowSpread);
				writer.WriteNumber("innerGlowBlendMode", (int)layerStyle.m_innerGlowBlendMode);
				writer.WriteBoolean("hasBevel", layerStyle.m_hasBevel);
				writer.WriteNumber("bevelDepth", layerStyle.m_bevelDepth);
				writer.WriteNumber("bevelSize", layerStyle.m_bevelSize);
				writer.WriteNumber("bevelAngle", layerStyle.m_bevelAngle);
				writer.WriteNumber("bevelHighlightColorRed", (int)bevelHighlightColor.Red);
				writer.WriteNumber("bevelHighlightColorGreen", (int)bevelHighlightColor.Green);
				writer.WriteNumber("bevelHighlightColorBlue", (int)bevelHighlightColor.Blue);
				writer.WriteNumber("bevelHighlightColorAlpha", (int)bevelHighlightColor.Alpha);
				writer.WriteNumber("bevelHighlightOpacity", layerStyle.m_bevelHighlightOpacity);
				writer.WriteNumber("bevelShadowColorRed", (int)bevelShadowColor.Red);
				writer.WriteNumber("bevelShadowColorGreen", (int)bevelShadowColor.Green);
				writer.WriteNumber("bevelShadowColorBlue", (int)bevelShadowColor.Blue);
				writer.WriteNumber("bevelShadowColorAlpha", (int)bevelShadowColor.Alpha);
				writer.WriteNumber("bevelShadowOpacity", layerStyle.m_bevelShadowOpacity);
				writer.WriteNumber("bevelBlendMode", (int)layerStyle.m_bevelBlendMode);
				writer.WriteEndObject();
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

			List<PathData> paths = document.Paths();
			if (paths.Count > 0)
			{
				writer.WriteStartArray("paths");
				for (int p = 0; p < paths.Count; p++)
				{
					PathData path = paths[p];
					writer.WriteStartObject();
					if (path.m_name != null)
					{
						writer.WriteString("name", path.m_name);
					}
					writer.WriteBoolean("closed", path.m_isClosed);
					writer.WriteNumber("colorRed", (int)path.m_strokeColor.Red);
					writer.WriteNumber("colorGreen", (int)path.m_strokeColor.Green);
					writer.WriteNumber("colorBlue", (int)path.m_strokeColor.Blue);
					writer.WriteNumber("colorAlpha", (int)path.m_strokeColor.Alpha);
					writer.WriteStartArray("points");
					for (int pt = 0; pt < path.m_points.Count; pt++)
					{
						PathPoint point = path.m_points[pt];
						writer.WriteStartObject();
						writer.WriteNumber("x", point.m_x);
						writer.WriteNumber("y", point.m_y);
						writer.WriteBoolean("hasControlIn", point.m_hasControlIn);
						writer.WriteBoolean("hasControlOut", point.m_hasControlOut);
						writer.WriteNumber("controlInX", point.m_controlInX);
						writer.WriteNumber("controlInY", point.m_controlInY);
						writer.WriteNumber("controlOutX", point.m_controlOutX);
						writer.WriteNumber("controlOutY", point.m_controlOutY);
						writer.WriteBoolean("smooth", point.m_smooth);
						writer.WriteEndObject();
					}
					writer.WriteEndArray();
					writer.WriteEndObject();
				}
				writer.WriteEndArray();
			}

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
			int rowLength = width * bitmap.BytesPerPixel;
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

		private static void WriteLayerMask(ZipArchive archive, int index, Layer layer)
		{
			SKBitmap bitmap = layer.MaskBitmap();
			int width = bitmap.Width;
			int height = bitmap.Height;
			ZipArchiveEntry entry = archive.CreateEntry("layers/" + index.ToString() + ".mask.dat");
			Stream stream = entry.Open();
			WriteInt32(stream, width);
			WriteInt32(stream, height);
			int rowLength = width * bitmap.BytesPerPixel;
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
			Selection selection = document.Selection();
			WriteInt32(stream, selection.MaskWidth());
			WriteInt32(stream, selection.MaskHeight());
			WriteInt32(stream, selection.MaskOriginX());
			WriteInt32(stream, selection.MaskOriginY());
			byte[] mask = selection.Mask();
			stream.Write(mask, 0, selection.MaskWidth() * selection.MaskHeight());
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
			int rowLength = width * bitmap.BytesPerPixel;
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

		private static bool ReadLayerMask(Stream stream, Layer layer)
		{
			int width = ReadInt32(stream);
			int height = ReadInt32(stream);
			SKBitmap bitmap = layer.MaskBitmap();
			if (width != bitmap.Width || height != bitmap.Height)
			{
				return false;
			}
			int rowLength = width * bitmap.BytesPerPixel;
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

		private static Layer ReadLayer(ZipArchive archive, JsonElement layerElement, int index, int documentWidth, int documentHeight, eColorDepth colorDepth)
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
			Layer layer = new Layer(name, bitmapWidth, bitmapHeight, colorDepth);
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
			layer.SetLockAll(ReadBool(layerElement, "lockAll", false));
			layer.SetLockPixels(ReadBool(layerElement, "lockPixels", false));
			layer.SetLockPosition(ReadBool(layerElement, "lockPosition", false));
			layer.SetLockAlpha(ReadBool(layerElement, "lockAlpha", false));
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
					ReadString(layerElement, "textFontFamily", "OpenSans-Regular"),
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
			System.Text.Json.JsonElement styleElement;
			if (layerElement.TryGetProperty("layerStyle", out styleElement))
			{
				LayerStyle style = new LayerStyle();
				style.m_hasStroke = ReadBool(styleElement, "hasStroke", false);
				style.m_strokeSize = ReadInt(styleElement, "strokeSize", 3);
				style.m_strokePosition = ReadInt(styleElement, "strokePosition", 2);
				style.m_strokeColor = new SKColor(ClampByte(ReadInt(styleElement, "strokeColorRed", 0)), ClampByte(ReadInt(styleElement, "strokeColorGreen", 0)), ClampByte(ReadInt(styleElement, "strokeColorBlue", 0)), ClampByte(ReadInt(styleElement, "strokeColorAlpha", 255)));
				style.m_hasDropShadow = ReadBool(styleElement, "hasDropShadow", false);
				style.m_shadowColor = new SKColor(ClampByte(ReadInt(styleElement, "shadowColorRed", 0)), ClampByte(ReadInt(styleElement, "shadowColorGreen", 0)), ClampByte(ReadInt(styleElement, "shadowColorBlue", 0)), ClampByte(ReadInt(styleElement, "shadowColorAlpha", 255)));
				style.m_shadowOpacity = ReadInt(styleElement, "shadowOpacity", 75);
				style.m_shadowAngle = ReadInt(styleElement, "shadowAngle", 135);
				style.m_shadowDistance = ReadInt(styleElement, "shadowDistance", 5);
				style.m_shadowSize = ReadInt(styleElement, "shadowSize", 5);
				style.m_hasOuterGlow = ReadBool(styleElement, "hasOuterGlow", false);
				style.m_glowColor = new SKColor(ClampByte(ReadInt(styleElement, "glowColorRed", 255)), ClampByte(ReadInt(styleElement, "glowColorGreen", 255)), ClampByte(ReadInt(styleElement, "glowColorBlue", 190)), ClampByte(ReadInt(styleElement, "glowColorAlpha", 255)));
				style.m_glowOpacity = ReadInt(styleElement, "glowOpacity", 75);
				style.m_glowSize = ReadInt(styleElement, "glowSize", 5);
				style.m_strokeOpacity = ReadInt(styleElement, "strokeOpacity", 100);
				style.m_strokeBlendMode = ReadBlendMode(styleElement, "strokeBlendMode");
				style.m_shadowSpread = ReadInt(styleElement, "shadowSpread", 0);
				style.m_shadowBlendMode = ReadBlendMode(styleElement, "shadowBlendMode");
				style.m_glowSpread = ReadInt(styleElement, "glowSpread", 0);
				style.m_glowBlendMode = ReadBlendMode(styleElement, "glowBlendMode");
				style.m_hasInnerGlow = ReadBool(styleElement, "hasInnerGlow", false);
				style.m_innerGlowColor = new SKColor(ClampByte(ReadInt(styleElement, "innerGlowColorRed", 255)), ClampByte(ReadInt(styleElement, "innerGlowColorGreen", 255)), ClampByte(ReadInt(styleElement, "innerGlowColorBlue", 190)), ClampByte(ReadInt(styleElement, "innerGlowColorAlpha", 255)));
				style.m_innerGlowOpacity = ReadInt(styleElement, "innerGlowOpacity", 75);
				style.m_innerGlowSize = ReadInt(styleElement, "innerGlowSize", 5);
				style.m_innerGlowSpread = ReadInt(styleElement, "innerGlowSpread", 0);
				style.m_innerGlowBlendMode = ReadBlendMode(styleElement, "innerGlowBlendMode");
				style.m_hasBevel = ReadBool(styleElement, "hasBevel", false);
				style.m_bevelDepth = ReadInt(styleElement, "bevelDepth", 100);
				style.m_bevelSize = ReadInt(styleElement, "bevelSize", 5);
				style.m_bevelAngle = ReadInt(styleElement, "bevelAngle", 120);
				style.m_bevelHighlightColor = new SKColor(ClampByte(ReadInt(styleElement, "bevelHighlightColorRed", 255)), ClampByte(ReadInt(styleElement, "bevelHighlightColorGreen", 255)), ClampByte(ReadInt(styleElement, "bevelHighlightColorBlue", 255)), ClampByte(ReadInt(styleElement, "bevelHighlightColorAlpha", 255)));
				style.m_bevelHighlightOpacity = ReadInt(styleElement, "bevelHighlightOpacity", 75);
				style.m_bevelShadowColor = new SKColor(ClampByte(ReadInt(styleElement, "bevelShadowColorRed", 0)), ClampByte(ReadInt(styleElement, "bevelShadowColorGreen", 0)), ClampByte(ReadInt(styleElement, "bevelShadowColorBlue", 0)), ClampByte(ReadInt(styleElement, "bevelShadowColorAlpha", 255)));
				style.m_bevelShadowOpacity = ReadInt(styleElement, "bevelShadowOpacity", 75);
				style.m_bevelBlendMode = ReadBlendMode(styleElement, "bevelBlendMode");
				layer.SetLayerStyle(style);
			}
			if (ReadBool(layerElement, "hasMask", false))
			{
				ZipArchiveEntry maskEntry = archive.GetEntry("layers/" + index.ToString() + ".mask.dat");
				if (maskEntry != null)
				{
					layer.CreateMask(true);
					Stream maskStream = maskEntry.Open();
					bool maskLoaded = ReadLayerMask(maskStream, layer);
					maskStream.Dispose();
					if (maskLoaded)
					{
						layer.SetMaskEnabled(ReadBool(layerElement, "maskEnabled", true));
					}
					else
					{
						layer.DeleteMask();
					}
				}
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
			long entryLength = entry.Length;
			Stream stream = entry.Open();
			int maskWidth = ReadInt32(stream);
			int maskHeight = ReadInt32(stream);
			int count = maskWidth * maskHeight;
			bool legacyFormat = entryLength == 8 + (long)count;
			if (legacyFormat)
			{
				if (maskWidth != document.Width() || maskHeight != document.Height())
				{
					stream.Dispose();
					return;
				}
				byte[] legacyMask = new byte[count];
				ReadExact(stream, legacyMask, legacyMask.Length);
				stream.Dispose();
				SKRectI legacyBounds = ComputeMaskBounds(legacyMask, maskWidth, maskHeight);
				if (legacyBounds.Width <= 0 || legacyBounds.Height <= 0)
				{
					return;
				}
				document.Selection().SelectMask(legacyMask, legacyBounds);
				return;
			}
			int originX = ReadInt32(stream);
			int originY = ReadInt32(stream);
			byte[] mask = new byte[count];
			ReadExact(stream, mask, mask.Length);
			stream.Dispose();
			SKRectI localBounds = ComputeMaskBounds(mask, maskWidth, maskHeight);
			if (localBounds.Width <= 0 || localBounds.Height <= 0)
			{
				return;
			}
			SKRectI maskRect = new SKRectI(originX, originY, originX + maskWidth, originY + maskHeight);
			SKRectI bounds = new SKRectI(localBounds.Left + originX, localBounds.Top + originY, localBounds.Right + originX, localBounds.Bottom + originY);
			document.Selection().SelectMaskPlaced(mask, maskRect, bounds);
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
			int storedColorDepth = ReadInt(root, "colorDepth", (int)eColorDepth.Eight);
			eColorDepth colorDepth = eColorDepth.Eight;
			if (storedColorDepth == (int)eColorDepth.Sixteen)
			{
				colorDepth = eColorDepth.Sixteen;
			}
			else if (storedColorDepth == (int)eColorDepth.ThirtyTwoFloat)
			{
				colorDepth = eColorDepth.ThirtyTwoFloat;
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
			document.SetColorDepth(colorDepth);
			document.Layers().Clear();
			int layerIndex = 0;
			foreach (JsonElement layerElement in layersElement.EnumerateArray())
			{
				Layer layer = ReadLayer(archive, layerElement, layerIndex, width, height, colorDepth);
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
			int storedRulerUnits = ReadInt(root, "rulerUnits", 0);
			if (storedRulerUnits >= 0 && storedRulerUnits <= 3)
			{
				document.SetRulerUnits((eRulerUnits)storedRulerUnits);
			}
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
			System.Text.Json.JsonElement pathsElement;
			if (root.TryGetProperty("paths", out pathsElement))
			{
				foreach (System.Text.Json.JsonElement pathElement in pathsElement.EnumerateArray())
				{
					PathData path = new PathData();
					path.m_name = ReadString(pathElement, "name", "Path");
					path.m_isClosed = ReadBool(pathElement, "closed", false);
					int cr = ReadInt(pathElement, "colorRed", 0);
					int cg = ReadInt(pathElement, "colorGreen", 0);
					int cb = ReadInt(pathElement, "colorBlue", 0);
					int ca = ReadInt(pathElement, "colorAlpha", 255);
					path.m_strokeColor = new SKColor(ClampByte(cr), ClampByte(cg), ClampByte(cb), ClampByte(ca));
					System.Text.Json.JsonElement pointsElement;
					if (pathElement.TryGetProperty("points", out pointsElement))
					{
						foreach (System.Text.Json.JsonElement ptElement in pointsElement.EnumerateArray())
						{
							PathPoint point = new PathPoint();
							point.m_x = ReadFloat(ptElement, "x", 0.0f);
							point.m_y = ReadFloat(ptElement, "y", 0.0f);
							point.m_hasControlIn = ReadBool(ptElement, "hasControlIn", false);
							point.m_hasControlOut = ReadBool(ptElement, "hasControlOut", false);
							point.m_controlInX = ReadFloat(ptElement, "controlInX", point.m_x);
							point.m_controlInY = ReadFloat(ptElement, "controlInY", point.m_y);
							point.m_controlOutX = ReadFloat(ptElement, "controlOutX", point.m_x);
							point.m_controlOutY = ReadFloat(ptElement, "controlOutY", point.m_y);
							point.m_smooth = ReadBool(ptElement, "smooth", false);
							path.m_points.Add(point);
						}
					}
					document.AddPath(path);
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
					if (layers[index].HasMask())
					{
						WriteLayerMask(archive, index, layers[index]);
					}
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
