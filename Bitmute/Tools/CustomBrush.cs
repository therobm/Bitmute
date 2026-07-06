using SkiaSharp;

namespace Bitmute.Tools
{
	public class CustomBrush
	{
		public string m_name;
		public SKBitmap m_tip;
		public string m_relativePath;
		public bool m_isProcedural;
		public ProceduralBrushShape m_shape;

		public CustomBrush(string name, SKBitmap tip)
		{
			m_name = name;
			m_tip = tip;
			m_relativePath = "";
			m_isProcedural = false;
			m_shape = null;
		}
	}
}
