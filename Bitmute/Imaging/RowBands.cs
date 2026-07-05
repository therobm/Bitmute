using System;
using System.Threading.Tasks;

namespace Bitmute.Imaging
{
	public static class RowBands
	{
		private const int MinimumRowsPerBand = 16;

		private static int s_maxBands = Environment.ProcessorCount;

		private sealed class BandJob
		{
			public Action<int, int> m_body;
			public int m_start;
			public int m_end;

			public void Run()
			{
				if (m_body != null)
				{
					m_body(m_start, m_end);
				}
			}
		}

		public static void SetMaxBands(int maxBands)
		{
			if (maxBands < 1)
			{
				maxBands = 1;
			}
			s_maxBands = maxBands;
		}

		public static int MaxBands()
		{
			return s_maxBands;
		}

		public static void Run(int start, int end, Action<int, int> body)
		{
			if (body == null)
			{
				return;
			}
			int span = end - start;
			if (span <= 0)
			{
				return;
			}
			int bands = s_maxBands;
			int byMinimum = span / MinimumRowsPerBand;
			if (byMinimum < bands)
			{
				bands = byMinimum;
			}
			if (bands <= 1)
			{
				body(start, end);
				return;
			}
			int baseSize = span / bands;
			int remainder = span % bands;
			Task[] tasks = new Task[bands - 1];
			int cursor = start;
			for (int index = 0; index < bands - 1; index++)
			{
				int size = baseSize;
				if (index < remainder)
				{
					size = size + 1;
				}
				BandJob job = new BandJob();
				job.m_body = body;
				job.m_start = cursor;
				job.m_end = cursor + size;
				cursor = cursor + size;
				tasks[index] = Task.Run(job.Run);
			}
			body(cursor, end);
			Task.WaitAll(tasks);
		}
	}
}
