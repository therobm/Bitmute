using System;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Bitmute.Storage
{
	public static class TgaFile
	{
		private static int ReadUInt16(byte[] data, int offset)
		{
			return data[offset] | (data[offset + 1] << 8);
		}

		private static bool SamePixel(byte[] row, int indexA, int indexB)
		{
			int a = indexA * 4;
			int b = indexB * 4;
			if (row[a] != row[b])
			{
				return false;
			}
			if (row[a + 1] != row[b + 1])
			{
				return false;
			}
			if (row[a + 2] != row[b + 2])
			{
				return false;
			}
			if (row[a + 3] != row[b + 3])
			{
				return false;
			}
			return true;
		}

		private static bool DecodeUncompressed(byte[] data, int offset, int bytesPerPixel, int pixelCount, byte[] rgba)
		{
			long needed = (long)pixelCount * bytesPerPixel;
			if (offset + needed > data.Length)
			{
				return false;
			}
			int position = offset;
			for (int i = 0; i < pixelCount; i++)
			{
				byte alpha = 255;
				if (bytesPerPixel == 4)
				{
					alpha = data[position + 3];
				}
				int destination = i * 4;
				rgba[destination] = data[position + 2];
				rgba[destination + 1] = data[position + 1];
				rgba[destination + 2] = data[position];
				rgba[destination + 3] = alpha;
				position += bytesPerPixel;
			}
			return true;
		}

		private static bool DecodeRle(byte[] data, int offset, int bytesPerPixel, int pixelCount, byte[] rgba)
		{
			int position = offset;
			int pixelIndex = 0;
			for (;;)
			{
				if (pixelIndex >= pixelCount)
				{
					break;
				}
				if (position >= data.Length)
				{
					return false;
				}
				int header = data[position];
				position++;
				int count = (header & 0x7F) + 1;
				if ((header & 0x80) != 0)
				{
					if (position + bytesPerPixel > data.Length)
					{
						return false;
					}
					byte blue = data[position];
					byte green = data[position + 1];
					byte red = data[position + 2];
					byte alpha = 255;
					if (bytesPerPixel == 4)
					{
						alpha = data[position + 3];
					}
					position += bytesPerPixel;
					for (int i = 0; i < count; i++)
					{
						if (pixelIndex >= pixelCount)
						{
							return false;
						}
						int destination = pixelIndex * 4;
						rgba[destination] = red;
						rgba[destination + 1] = green;
						rgba[destination + 2] = blue;
						rgba[destination + 3] = alpha;
						pixelIndex++;
					}
				}
				else
				{
					for (int i = 0; i < count; i++)
					{
						if (pixelIndex >= pixelCount)
						{
							return false;
						}
						if (position + bytesPerPixel > data.Length)
						{
							return false;
						}
						byte alpha = 255;
						if (bytesPerPixel == 4)
						{
							alpha = data[position + 3];
						}
						int destination = pixelIndex * 4;
						rgba[destination] = data[position + 2];
						rgba[destination + 1] = data[position + 1];
						rgba[destination + 2] = data[position];
						rgba[destination + 3] = alpha;
						position += bytesPerPixel;
						pixelIndex++;
					}
				}
			}
			return true;
		}

		private static SKBitmap BuildBitmap(byte[] rgba, int width, int height, bool topDown)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			IntPtr pixels = bitmap.GetPixels();
			if (pixels == IntPtr.Zero)
			{
				bitmap.Dispose();
				return null;
			}
			int rowBytes = bitmap.RowBytes;
			int rowLength = width * 4;
			for (int y = 0; y < height; y++)
			{
				int sourceRow = y;
				if (!topDown)
				{
					sourceRow = height - 1 - y;
				}
				Marshal.Copy(rgba, sourceRow * rowLength, IntPtr.Add(pixels, y * rowBytes), rowLength);
			}
			return bitmap;
		}

		private static SKBitmap ReadInternal(string path)
		{
			byte[] data = File.ReadAllBytes(path);
			if (data.Length < 18)
			{
				return null;
			}
			int idLength = data[0];
			int colorMapType = data[1];
			int imageType = data[2];
			if (imageType != 2 && imageType != 10)
			{
				return null;
			}
			int colorMapLength = ReadUInt16(data, 5);
			int colorMapEntrySize = data[7];
			int width = ReadUInt16(data, 12);
			int height = ReadUInt16(data, 14);
			int pixelDepth = data[16];
			int descriptor = data[17];
			if (width <= 0 || height <= 0)
			{
				return null;
			}
			if (pixelDepth != 24 && pixelDepth != 32)
			{
				return null;
			}
			if ((descriptor & 0x10) != 0)
			{
				return null;
			}
			bool topDown = (descriptor & 0x20) != 0;
			long offsetLong = 18 + idLength;
			if (colorMapType != 0)
			{
				int entryBytes = (colorMapEntrySize + 7) / 8;
				offsetLong += (long)colorMapLength * entryBytes;
			}
			if (offsetLong > data.Length)
			{
				return null;
			}
			long pixelCountLong = (long)width * height;
			if (pixelCountLong * 4 > int.MaxValue)
			{
				return null;
			}
			int offset = (int)offsetLong;
			int pixelCount = (int)pixelCountLong;
			int bytesPerPixel = pixelDepth / 8;
			byte[] rgba = new byte[pixelCount * 4];
			bool decoded;
			if (imageType == 2)
			{
				decoded = DecodeUncompressed(data, offset, bytesPerPixel, pixelCount, rgba);
			}
			else
			{
				decoded = DecodeRle(data, offset, bytesPerPixel, pixelCount, rgba);
			}
			if (!decoded)
			{
				return null;
			}
			return BuildBitmap(rgba, width, height, topDown);
		}

		private static void EncodeRow(MemoryStream stream, byte[] row, int width)
		{
			int x = 0;
			for (;;)
			{
				if (x >= width)
				{
					break;
				}
				int run = 1;
				for (;;)
				{
					if (x + run >= width)
					{
						break;
					}
					if (run >= 128)
					{
						break;
					}
					if (!SamePixel(row, x, x + run))
					{
						break;
					}
					run++;
				}
				if (run >= 2)
				{
					stream.WriteByte((byte)(0x80 | (run - 1)));
					stream.Write(row, x * 4, 4);
					x += run;
				}
				else
				{
					int start = x;
					int count = 1;
					x++;
					for (;;)
					{
						if (x >= width)
						{
							break;
						}
						if (count >= 128)
						{
							break;
						}
						if (x + 1 < width && SamePixel(row, x, x + 1))
						{
							break;
						}
						count++;
						x++;
					}
					stream.WriteByte((byte)(count - 1));
					stream.Write(row, start * 4, count * 4);
				}
			}
		}

		public static SKBitmap Read(string path)
		{
			try
			{
				return ReadInternal(path);
			}
			catch
			{
				return null;
			}
		}

		public static bool Write(string path, SKBitmap bitmap, bool rleCompress)
		{
			try
			{
				if (bitmap == null)
				{
					return false;
				}
				if (bitmap.ColorType != SKColorType.Rgba8888)
				{
					return false;
				}
				int width = bitmap.Width;
				int height = bitmap.Height;
				if (width <= 0 || height <= 0 || width > 65535 || height > 65535)
				{
					return false;
				}
				IntPtr pixels = bitmap.GetPixels();
				if (pixels == IntPtr.Zero)
				{
					return false;
				}
				int rowBytes = bitmap.RowBytes;
				byte[] source = new byte[rowBytes * height];
				Marshal.Copy(pixels, source, 0, source.Length);
				MemoryStream stream = new MemoryStream();
				byte[] header = new byte[18];
				header[2] = 2;
				if (rleCompress)
				{
					header[2] = 10;
				}
				header[12] = (byte)(width & 0xFF);
				header[13] = (byte)((width >> 8) & 0xFF);
				header[14] = (byte)(height & 0xFF);
				header[15] = (byte)((height >> 8) & 0xFF);
				header[16] = 32;
				header[17] = 0x28;
				stream.Write(header, 0, 18);
				byte[] row = new byte[width * 4];
				for (int y = 0; y < height; y++)
				{
					int sourceOffset = y * rowBytes;
					for (int x = 0; x < width; x++)
					{
						int sourceIndex = sourceOffset + x * 4;
						int rowIndex = x * 4;
						row[rowIndex] = source[sourceIndex + 2];
						row[rowIndex + 1] = source[sourceIndex + 1];
						row[rowIndex + 2] = source[sourceIndex];
						row[rowIndex + 3] = source[sourceIndex + 3];
					}
					if (rleCompress)
					{
						EncodeRow(stream, row, width);
					}
					else
					{
						stream.Write(row, 0, row.Length);
					}
				}
				File.WriteAllBytes(path, stream.ToArray());
				stream.Dispose();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
