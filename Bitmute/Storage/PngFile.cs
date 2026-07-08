using System;
using System.IO;
using System.IO.Compression;
using SkiaSharp;

namespace Bitmute.Storage
{
	public static class PngFile
	{
		private static bool HasSignature(byte[] data)
		{
			if (data.Length < 8)
			{
				return false;
			}
			if (data[0] != 137)
			{
				return false;
			}
			if (data[1] != 80)
			{
				return false;
			}
			if (data[2] != 78)
			{
				return false;
			}
			if (data[3] != 71)
			{
				return false;
			}
			if (data[4] != 13)
			{
				return false;
			}
			if (data[5] != 10)
			{
				return false;
			}
			if (data[6] != 26)
			{
				return false;
			}
			if (data[7] != 10)
			{
				return false;
			}
			return true;
		}

		private static int ReadUInt32BigEndian(byte[] data, int offset)
		{
			return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
		}

		private static bool ChunkTypeEquals(byte[] data, int offset, byte a, byte b, byte c, byte d)
		{
			if (data[offset] != a)
			{
				return false;
			}
			if (data[offset + 1] != b)
			{
				return false;
			}
			if (data[offset + 2] != c)
			{
				return false;
			}
			if (data[offset + 3] != d)
			{
				return false;
			}
			return true;
		}

		private static byte[] Inflate(byte[] compressed)
		{
			MemoryStream input = new MemoryStream(compressed);
			ZLibStream decompressor = new ZLibStream(input, CompressionMode.Decompress);
			MemoryStream output = new MemoryStream();
			byte[] buffer = new byte[65536];
			for (;;)
			{
				int read = decompressor.Read(buffer, 0, buffer.Length);
				if (read <= 0)
				{
					break;
				}
				output.Write(buffer, 0, read);
			}
			decompressor.Dispose();
			input.Dispose();
			byte[] result = output.ToArray();
			output.Dispose();
			return result;
		}

		private static int PaethPredictor(int left, int above, int upperLeft)
		{
			int predictor = left + above - upperLeft;
			int distanceLeft = predictor - left;
			if (distanceLeft < 0)
			{
				distanceLeft = -distanceLeft;
			}
			int distanceAbove = predictor - above;
			if (distanceAbove < 0)
			{
				distanceAbove = -distanceAbove;
			}
			int distanceUpperLeft = predictor - upperLeft;
			if (distanceUpperLeft < 0)
			{
				distanceUpperLeft = -distanceUpperLeft;
			}
			if (distanceLeft <= distanceAbove && distanceLeft <= distanceUpperLeft)
			{
				return left;
			}
			if (distanceAbove <= distanceUpperLeft)
			{
				return above;
			}
			return upperLeft;
		}

		private static bool Unfilter(byte[] raw, int width, int height, int bytesPerPixel, byte[] output)
		{
			int rowBytes = width * bytesPerPixel;
			int stride = rowBytes + 1;
			long needed = (long)stride * height;
			if (raw.Length < needed)
			{
				return false;
			}
			for (int y = 0; y < height; y++)
			{
				int sourceRow = y * stride;
				int filterType = raw[sourceRow];
				int destinationRow = y * rowBytes;
				int previousRow = destinationRow - rowBytes;
				for (int i = 0; i < rowBytes; i++)
				{
					int current = raw[sourceRow + 1 + i];
					int left = 0;
					if (i >= bytesPerPixel)
					{
						left = output[destinationRow + i - bytesPerPixel];
					}
					int above = 0;
					if (y > 0)
					{
						above = output[previousRow + i];
					}
					int upperLeft = 0;
					if (y > 0 && i >= bytesPerPixel)
					{
						upperLeft = output[previousRow + i - bytesPerPixel];
					}
					int reconstructed = current;
					if (filterType == 0)
					{
						reconstructed = current;
					}
					else if (filterType == 1)
					{
						reconstructed = current + left;
					}
					else if (filterType == 2)
					{
						reconstructed = current + above;
					}
					else if (filterType == 3)
					{
						reconstructed = current + ((left + above) / 2);
					}
					else if (filterType == 4)
					{
						reconstructed = current + PaethPredictor(left, above, upperLeft);
					}
					else
					{
						return false;
					}
					output[destinationRow + i] = (byte)(reconstructed & 0xFF);
				}
			}
			return true;
		}

		private static unsafe SKBitmap AssembleEight(byte[] pixels, int width, int height, int channels)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			IntPtr basePointer = bitmap.GetPixels();
			if (basePointer == IntPtr.Zero)
			{
				bitmap.Dispose();
				return null;
			}
			int rowBytes = bitmap.RowBytes;
			int sourceStride = width * channels;
			for (int y = 0; y < height; y++)
			{
				byte* destination = (byte*)basePointer.ToPointer() + ((long)y * rowBytes);
				int sourceRow = y * sourceStride;
				for (int x = 0; x < width; x++)
				{
					int sourceIndex = sourceRow + (x * channels);
					int destinationIndex = x * 4;
					byte red;
					byte green;
					byte blue;
					byte alpha;
					if (channels == 4)
					{
						red = pixels[sourceIndex];
						green = pixels[sourceIndex + 1];
						blue = pixels[sourceIndex + 2];
						alpha = pixels[sourceIndex + 3];
					}
					else if (channels == 3)
					{
						red = pixels[sourceIndex];
						green = pixels[sourceIndex + 1];
						blue = pixels[sourceIndex + 2];
						alpha = 255;
					}
					else
					{
						byte gray = pixels[sourceIndex];
						red = gray;
						green = gray;
						blue = gray;
						alpha = 255;
					}
					destination[destinationIndex] = red;
					destination[destinationIndex + 1] = green;
					destination[destinationIndex + 2] = blue;
					destination[destinationIndex + 3] = alpha;
				}
			}
			return bitmap;
		}

		private static ushort ReadSampleBigEndian(byte[] pixels, int index)
		{
			return (ushort)((pixels[index] << 8) | pixels[index + 1]);
		}

		private static unsafe SKBitmap AssembleSixteen(byte[] pixels, int width, int height, int channels)
		{
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba16161616, SKAlphaType.Unpremul);
			IntPtr basePointer = bitmap.GetPixels();
			if (basePointer == IntPtr.Zero)
			{
				bitmap.Dispose();
				return null;
			}
			int rowBytes = bitmap.RowBytes;
			int sourceStride = width * channels * 2;
			for (int y = 0; y < height; y++)
			{
				ushort* destination = (ushort*)((byte*)basePointer.ToPointer() + ((long)y * rowBytes));
				int sourceRow = y * sourceStride;
				for (int x = 0; x < width; x++)
				{
					int sourceIndex = sourceRow + (x * channels * 2);
					int destinationIndex = x * 4;
					ushort red;
					ushort green;
					ushort blue;
					ushort alpha;
					if (channels == 4)
					{
						red = ReadSampleBigEndian(pixels, sourceIndex);
						green = ReadSampleBigEndian(pixels, sourceIndex + 2);
						blue = ReadSampleBigEndian(pixels, sourceIndex + 4);
						alpha = ReadSampleBigEndian(pixels, sourceIndex + 6);
					}
					else if (channels == 3)
					{
						red = ReadSampleBigEndian(pixels, sourceIndex);
						green = ReadSampleBigEndian(pixels, sourceIndex + 2);
						blue = ReadSampleBigEndian(pixels, sourceIndex + 4);
						alpha = 65535;
					}
					else
					{
						ushort gray = ReadSampleBigEndian(pixels, sourceIndex);
						red = gray;
						green = gray;
						blue = gray;
						alpha = 65535;
					}
					destination[destinationIndex] = red;
					destination[destinationIndex + 1] = green;
					destination[destinationIndex + 2] = blue;
					destination[destinationIndex + 3] = alpha;
				}
			}
			return bitmap;
		}

		private static int ChannelsForColorType(int colorType)
		{
			if (colorType == 6)
			{
				return 4;
			}
			if (colorType == 2)
			{
				return 3;
			}
			if (colorType == 0)
			{
				return 1;
			}
			return 0;
		}

		private static SKBitmap DecodeInternal(byte[] data)
		{
			if (!HasSignature(data))
			{
				return null;
			}
			int width = 0;
			int height = 0;
			int bitDepth = 0;
			int colorType = 0;
			int interlace = 0;
			bool haveHeader = false;
			MemoryStream idat = new MemoryStream();
			int position = 8;
			for (;;)
			{
				if (position + 8 > data.Length)
				{
					idat.Dispose();
					return null;
				}
				int length = ReadUInt32BigEndian(data, position);
				if (length < 0)
				{
					idat.Dispose();
					return null;
				}
				int typeOffset = position + 4;
				int dataOffset = position + 8;
				if ((long)dataOffset + length + 4 > data.Length)
				{
					idat.Dispose();
					return null;
				}
				if (ChunkTypeEquals(data, typeOffset, 73, 72, 68, 82))
				{
					if (length != 13)
					{
						idat.Dispose();
						return null;
					}
					width = ReadUInt32BigEndian(data, dataOffset);
					height = ReadUInt32BigEndian(data, dataOffset + 4);
					bitDepth = data[dataOffset + 8];
					colorType = data[dataOffset + 9];
					interlace = data[dataOffset + 12];
					haveHeader = true;
				}
				else if (ChunkTypeEquals(data, typeOffset, 73, 68, 65, 84))
				{
					idat.Write(data, dataOffset, length);
				}
				else if (ChunkTypeEquals(data, typeOffset, 73, 69, 78, 68))
				{
					break;
				}
				position = dataOffset + length + 4;
			}
			if (!haveHeader)
			{
				idat.Dispose();
				return null;
			}
			if (width <= 0 || height <= 0)
			{
				idat.Dispose();
				return null;
			}
			if (interlace != 0)
			{
				idat.Dispose();
				return null;
			}
			if (bitDepth != 8 && bitDepth != 16)
			{
				idat.Dispose();
				return null;
			}
			int channels = ChannelsForColorType(colorType);
			if (channels == 0)
			{
				idat.Dispose();
				return null;
			}
			byte[] compressed = idat.ToArray();
			idat.Dispose();
			if (compressed.Length == 0)
			{
				return null;
			}
			byte[] raw = Inflate(compressed);
			int bytesPerSample = 1;
			if (bitDepth == 16)
			{
				bytesPerSample = 2;
			}
			int bytesPerPixel = channels * bytesPerSample;
			byte[] pixels = new byte[(long)width * height * bytesPerPixel];
			bool unfiltered = Unfilter(raw, width, height, bytesPerPixel, pixels);
			if (!unfiltered)
			{
				return null;
			}
			if (bitDepth == 8)
			{
				return AssembleEight(pixels, width, height, channels);
			}
			return AssembleSixteen(pixels, width, height, channels);
		}

		public static SKBitmap Decode(byte[] data)
		{
			try
			{
				return DecodeInternal(data);
			}
			catch
			{
				return null;
			}
		}
	}
}
