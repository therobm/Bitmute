using SkiaSharp;

namespace Bitmute.Tools
{
	public class CustomBrush
	{
		public string m_name;
		public SKBitmap m_tip;

		public CustomBrush(string name, SKBitmap tip)
		{
			m_name = name;
			m_tip = tip;
		}
	}
}
