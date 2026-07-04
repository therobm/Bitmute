using System;
using System.Collections.Generic;

namespace Bitmute.Imaging
{
	public class Guides
	{
		private List<int> m_horizontal;
		private List<int> m_vertical;
		private bool m_locked;
		private int m_generation;

		public Guides()
		{
			m_horizontal = new List<int>();
			m_vertical = new List<int>();
			m_locked = false;
			m_generation = 0;
		}

		private int FindIndex(List<int> list, int position)
		{
			for (int index = 0; index < list.Count; index++)
			{
				if (list[index] == position)
				{
					return index;
				}
			}
			return -1;
		}

		private int HitList(List<int> list, int position, int tolerance)
		{
			int bestIndex = -1;
			int bestDistance = 0;
			for (int index = 0; index < list.Count; index++)
			{
				int delta = list[index] - position;
				if (delta < 0)
				{
					delta = -delta;
				}
				if (delta > tolerance)
				{
					continue;
				}
				if (bestIndex < 0 || delta < bestDistance)
				{
					bestIndex = index;
					bestDistance = delta;
				}
			}
			return bestIndex;
		}

		private int SnapList(List<int> list, int position, int tolerance)
		{
			int index = HitList(list, position, tolerance);
			if (index < 0)
			{
				return position;
			}
			return list[index];
		}

		public List<int> HorizontalGuides()
		{
			return m_horizontal;
		}

		public List<int> VerticalGuides()
		{
			return m_vertical;
		}

		public void AddHorizontal(int y)
		{
			if (m_locked)
			{
				return;
			}
			if (FindIndex(m_horizontal, y) >= 0)
			{
				return;
			}
			m_horizontal.Add(y);
			m_generation = m_generation + 1;
		}

		public void AddVertical(int x)
		{
			if (m_locked)
			{
				return;
			}
			if (FindIndex(m_vertical, x) >= 0)
			{
				return;
			}
			m_vertical.Add(x);
			m_generation = m_generation + 1;
		}

		public void MoveHorizontal(int index, int newY)
		{
			if (m_locked)
			{
				return;
			}
			if (index < 0 || index >= m_horizontal.Count)
			{
				return;
			}
			m_horizontal[index] = newY;
			m_generation = m_generation + 1;
		}

		public void MoveVertical(int index, int newX)
		{
			if (m_locked)
			{
				return;
			}
			if (index < 0 || index >= m_vertical.Count)
			{
				return;
			}
			m_vertical[index] = newX;
			m_generation = m_generation + 1;
		}

		public void RemoveHorizontal(int index)
		{
			if (m_locked)
			{
				return;
			}
			if (index < 0 || index >= m_horizontal.Count)
			{
				return;
			}
			m_horizontal.RemoveAt(index);
			m_generation = m_generation + 1;
		}

		public void RemoveVertical(int index)
		{
			if (m_locked)
			{
				return;
			}
			if (index < 0 || index >= m_vertical.Count)
			{
				return;
			}
			m_vertical.RemoveAt(index);
			m_generation = m_generation + 1;
		}

		public void Clear()
		{
			if (m_locked)
			{
				return;
			}
			m_horizontal.Clear();
			m_vertical.Clear();
			m_generation = m_generation + 1;
		}

		public int Generation()
		{
			return m_generation;
		}

		public bool IsLocked()
		{
			return m_locked;
		}

		public void SetLocked(bool locked)
		{
			m_locked = locked;
		}

		public int HitHorizontal(int y, int tolerance)
		{
			return HitList(m_horizontal, y, tolerance);
		}

		public int HitVertical(int x, int tolerance)
		{
			return HitList(m_vertical, x, tolerance);
		}

		public int SnapX(int x, int tolerance)
		{
			return SnapList(m_vertical, x, tolerance);
		}

		public int SnapY(int y, int tolerance)
		{
			return SnapList(m_horizontal, y, tolerance);
		}
	}
}
