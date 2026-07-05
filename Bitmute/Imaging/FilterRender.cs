using System;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public static class FilterRender
	{
		private const int OctaveCount = 5;

		private static int HashLattice(int latticeX, int latticeY, int octave, int seed)
		{
			unchecked
			{
				uint mixed = (uint)seed;
				mixed = (mixed * 374761393u) + (uint)latticeX;
				mixed = mixed ^ (mixed >> 13);
				mixed = (mixed * 668265263u) + (uint)latticeY;
				mixed = mixed ^ (mixed >> 13);
				mixed = (mixed * 2246822519u) + (uint)octave;
				mixed = mixed ^ (mixed >> 13);
				mixed = mixed * 3266489917u;
				mixed = mixed ^ (mixed >> 16);
				return (int)(mixed & 255u);
			}
		}

		private static double Smoothstep(double t)
		{
			return t * t * (3.0 - (2.0 * t));
		}

		private static double OctaveValue(int x, int y, int wavelength, int octave, int seed)
		{
			int cellX = x / wavelength;
			int cellY = y / wavelength;
			double fractionX = (x - (cellX * wavelength)) / (double)wavelength;
			double fractionY = (y - (cellY * wavelength)) / (double)wavelength;
			double value00 = HashLattice(cellX, cellY, octave, seed);
			double value10 = HashLattice(cellX + 1, cellY, octave, seed);
			double value01 = HashLattice(cellX, cellY + 1, octave, seed);
			double value11 = HashLattice(cellX + 1, cellY + 1, octave, seed);
			double weightX = Smoothstep(fractionX);
			double weightY = Smoothstep(fractionY);
			double top = value00 + ((value10 - value00) * weightX);
			double bottom = value01 + ((value11 - value01) * weightX);
			return top + ((bottom - top) * weightY);
		}

		private static double NoiseValue(int x, int y, int baseWavelength, int seed)
		{
			double sum = 0.0;
			double amplitudeTotal = 0.0;
			double amplitude = 1.0;
			int wavelength = baseWavelength;
			for (int octave = 0; octave < OctaveCount; octave++)
			{
				sum += amplitude * (OctaveValue(x, y, wavelength, octave, seed) / 255.0);
				amplitudeTotal += amplitude;
				amplitude = amplitude * 0.5;
				wavelength = wavelength / 2;
				if (wavelength < 1)
				{
					wavelength = 1;
				}
			}
			return sum / amplitudeTotal;
		}

		private static int BaseWavelength(int width, int height)
		{
			int smaller = width;
			if (height < smaller)
			{
				smaller = height;
			}
			int wavelength = smaller / 4;
			if (wavelength < 1)
			{
				wavelength = 1;
			}
			return wavelength;
		}

		private static byte CloudChannel(byte background, byte foreground, double t)
		{
			double mixed = background + ((foreground - background) * t);
			if (mixed < 0.0)
			{
				return 0;
			}
			if (mixed > 255.0)
			{
				return 255;
			}
			return (byte)Math.Round(mixed);
		}

		private static byte AbsoluteDifference(byte existing, byte cloud)
		{
			int delta = existing - cloud;
			if (delta < 0)
			{
				delta = -delta;
			}
			return (byte)delta;
		}

		private sealed unsafe class CloudsWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_baseWavelength;
			public int m_seed;
			public byte m_foregroundR;
			public byte m_foregroundG;
			public byte m_foregroundB;
			public byte m_backgroundR;
			public byte m_backgroundG;
			public byte m_backgroundB;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						double t = NoiseValue(x, y, m_baseWavelength, m_seed);
						pixel[0] = CloudChannel(m_backgroundR, m_foregroundR, t);
						pixel[1] = CloudChannel(m_backgroundG, m_foregroundG, t);
						pixel[2] = CloudChannel(m_backgroundB, m_foregroundB, t);
						pixel[3] = 255;
					}
				}
			}
		}

		private sealed unsafe class DifferenceCloudsWorker
		{
			public byte* m_base;
			public int m_rowBytes;
			public int m_width;
			public int m_baseWavelength;
			public int m_seed;
			public byte m_foregroundR;
			public byte m_foregroundG;
			public byte m_foregroundB;
			public byte m_backgroundR;
			public byte m_backgroundG;
			public byte m_backgroundB;

			public void Band(int start, int end)
			{
				for (int y = start; y < end; y++)
				{
					byte* row = m_base + ((long)y * m_rowBytes);
					for (int x = 0; x < m_width; x++)
					{
						byte* pixel = row + (x * 4);
						double t = NoiseValue(x, y, m_baseWavelength, m_seed);
						byte cloudR = CloudChannel(m_backgroundR, m_foregroundR, t);
						byte cloudG = CloudChannel(m_backgroundG, m_foregroundG, t);
						byte cloudB = CloudChannel(m_backgroundB, m_foregroundB, t);
						pixel[0] = AbsoluteDifference(pixel[0], cloudR);
						pixel[1] = AbsoluteDifference(pixel[1], cloudG);
						pixel[2] = AbsoluteDifference(pixel[2], cloudB);
					}
				}
			}
		}

		public static unsafe void Clouds(SKBitmap bitmap, SKColor foreground, SKColor background, int seed)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			int baseWavelength = BaseWavelength(width, height);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			CloudsWorker worker = new CloudsWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_baseWavelength = baseWavelength;
			worker.m_seed = seed;
			worker.m_foregroundR = foreground.Red;
			worker.m_foregroundG = foreground.Green;
			worker.m_foregroundB = foreground.Blue;
			worker.m_backgroundR = background.Red;
			worker.m_backgroundG = background.Green;
			worker.m_backgroundB = background.Blue;
			RowBands.Run(0, height, worker.Band);
		}

		public static unsafe void DifferenceClouds(SKBitmap bitmap, SKColor foreground, SKColor background, int seed)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int rowBytes = bitmap.RowBytes;
			int baseWavelength = BaseWavelength(width, height);
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			DifferenceCloudsWorker worker = new DifferenceCloudsWorker();
			worker.m_base = basePointer;
			worker.m_rowBytes = rowBytes;
			worker.m_width = width;
			worker.m_baseWavelength = baseWavelength;
			worker.m_seed = seed;
			worker.m_foregroundR = foreground.Red;
			worker.m_foregroundG = foreground.Green;
			worker.m_foregroundB = foreground.Blue;
			worker.m_backgroundR = background.Red;
			worker.m_backgroundG = background.Green;
			worker.m_backgroundB = background.Blue;
			RowBands.Run(0, height, worker.Band);
		}
	}
}
