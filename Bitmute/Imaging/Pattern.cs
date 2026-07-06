using SkiaSharp;

namespace Bitmute.Imaging
{
	public class Pattern
	{
		public string m_name;
		public SKBitmap m_bitmap;
		public string m_relativePath;

		public Pattern(string name, SKBitmap bitmap)
		{
			m_name = name;
			m_bitmap = bitmap;
			m_relativePath = "";
		}
	}
}
