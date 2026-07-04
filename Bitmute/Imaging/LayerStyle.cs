using SkiaSharp;

namespace Bitmute.Imaging
{
	public class LayerStyle
	{
		public bool m_hasStroke;
		public int m_strokeSize;
		public int m_strokePosition;
		public SKColor m_strokeColor;
		public bool m_hasDropShadow;
		public SKColor m_shadowColor;
		public int m_shadowOpacity;
		public int m_shadowAngle;
		public int m_shadowDistance;
		public int m_shadowSize;
		public bool m_hasOuterGlow;
		public SKColor m_glowColor;
		public int m_glowOpacity;
		public int m_glowSize;

		public LayerStyle()
		{
			m_hasStroke = false;
			m_strokeSize = 3;
			m_strokePosition = 2;
			m_strokeColor = new SKColor(0, 0, 0, 255);
			m_hasDropShadow = false;
			m_shadowColor = new SKColor(0, 0, 0, 255);
			m_shadowOpacity = 75;
			m_shadowAngle = 135;
			m_shadowDistance = 5;
			m_shadowSize = 5;
			m_hasOuterGlow = false;
			m_glowColor = new SKColor(255, 255, 190, 255);
			m_glowOpacity = 75;
			m_glowSize = 5;
		}

		public bool HasAnyEffect()
		{
			return m_hasStroke || m_hasDropShadow || m_hasOuterGlow;
		}

		public LayerStyle Clone()
		{
			LayerStyle copy = new LayerStyle();
			copy.m_hasStroke = m_hasStroke;
			copy.m_strokeSize = m_strokeSize;
			copy.m_strokePosition = m_strokePosition;
			copy.m_strokeColor = m_strokeColor;
			copy.m_hasDropShadow = m_hasDropShadow;
			copy.m_shadowColor = m_shadowColor;
			copy.m_shadowOpacity = m_shadowOpacity;
			copy.m_shadowAngle = m_shadowAngle;
			copy.m_shadowDistance = m_shadowDistance;
			copy.m_shadowSize = m_shadowSize;
			copy.m_hasOuterGlow = m_hasOuterGlow;
			copy.m_glowColor = m_glowColor;
			copy.m_glowOpacity = m_glowOpacity;
			copy.m_glowSize = m_glowSize;
			return copy;
		}
	}
}
