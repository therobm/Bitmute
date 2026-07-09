using System;

namespace Bitmute.Tools
{
	public class PressureCalibration
	{
		private int m_minimumPercent;
		private int m_maximumPercent;
		private int m_sensitivityPercent;

		public PressureCalibration()
		{
			m_minimumPercent = 0;
			m_maximumPercent = 100;
			m_sensitivityPercent = 100;
		}

		public int MinimumPercent()
		{
			return m_minimumPercent;
		}

		public int MaximumPercent()
		{
			return m_maximumPercent;
		}

		public int SensitivityPercent()
		{
			return m_sensitivityPercent;
		}

		public void SetValues(int minimumPercent, int maximumPercent, int sensitivityPercent)
		{
			if (minimumPercent < 0)
			{
				minimumPercent = 0;
			}
			if (minimumPercent > 100)
			{
				minimumPercent = 100;
			}
			if (maximumPercent < 0)
			{
				maximumPercent = 0;
			}
			if (maximumPercent > 100)
			{
				maximumPercent = 100;
			}
			if (sensitivityPercent < 1)
			{
				sensitivityPercent = 1;
			}
			m_minimumPercent = minimumPercent;
			m_maximumPercent = maximumPercent;
			m_sensitivityPercent = sensitivityPercent;
		}

		public float Apply(float raw)
		{
			float minimum = m_minimumPercent / 100.0f;
			float maximum = m_maximumPercent / 100.0f;
			if (maximum <= minimum)
			{
				if (raw >= maximum)
				{
					return 1.0f;
				}
				return 0.0f;
			}
			float normalized = (raw - minimum) / (maximum - minimum);
			if (normalized < 0.0f)
			{
				normalized = 0.0f;
			}
			if (normalized > 1.0f)
			{
				normalized = 1.0f;
			}
			float gamma = 100.0f / m_sensitivityPercent;
			float output = (float)Math.Pow(normalized, gamma);
			return output;
		}
	}
}
