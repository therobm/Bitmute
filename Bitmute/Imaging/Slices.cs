using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public class Slices
	{
		private List<string> m_names;
		private List<SKRectI> m_rects;
		private int m_generation;

		public Slices()
		{
			m_names = new List<string>();
			m_rects = new List<SKRectI>();
			m_generation = 0;
		}

		public int Count()
		{
			return m_names.Count;
		}

		public string NameAt(int index)
		{
			if (index < 0 || index >= m_names.Count)
			{
				return "";
			}
			return m_names[index];
		}

		public SKRectI RectAt(int index)
		{
			if (index < 0 || index >= m_names.Count)
			{
				return SKRectI.Empty;
			}
			return m_rects[index];
		}

		public void Add(string name, SKRectI rect)
		{
			if (rect.Width <= 0 || rect.Height <= 0)
			{
				return;
			}
			m_names.Add(name);
			m_rects.Add(rect);
			m_generation = m_generation + 1;
		}

		public void SetRectAt(int index, SKRectI rect)
		{
			if (index < 0 || index >= m_names.Count)
			{
				return;
			}
			m_rects[index] = rect;
			m_generation = m_generation + 1;
		}

		public void SetNameAt(int index, string name)
		{
			if (index < 0 || index >= m_names.Count)
			{
				return;
			}
			m_names[index] = name;
			m_generation = m_generation + 1;
		}

		public void RemoveAt(int index)
		{
			if (index < 0 || index >= m_names.Count)
			{
				return;
			}
			m_names.RemoveAt(index);
			m_rects.RemoveAt(index);
			m_generation = m_generation + 1;
		}

		public void Clear()
		{
			m_names.Clear();
			m_rects.Clear();
			m_generation = m_generation + 1;
		}

		public int HitTest(int x, int y)
		{
			for (int index = m_rects.Count - 1; index >= 0; index--)
			{
				SKRectI rect = m_rects[index];
				if (x >= rect.Left && x < rect.Right && y >= rect.Top && y < rect.Bottom)
				{
					return index;
				}
			}
			return -1;
		}

		public int Generation()
		{
			return m_generation;
		}
	}
}
