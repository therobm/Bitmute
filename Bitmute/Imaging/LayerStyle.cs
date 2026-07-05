using SkiaSharp;

namespace Bitmute.Imaging
{
	public class LayerStyle
	{
		public bool m_hasStroke;
		public int m_strokeSize;
		public int m_strokePosition;
		public SKColor m_strokeColor;
		public int m_strokeOpacity;
		public eBlendMode m_strokeBlendMode;
		public bool m_hasDropShadow;
		public SKColor m_shadowColor;
		public int m_shadowOpacity;
		public int m_shadowAngle;
		public int m_shadowDistance;
		public int m_shadowSize;
		public int m_shadowSpread;
		public eBlendMode m_shadowBlendMode;
		public bool m_hasOuterGlow;
		public SKColor m_glowColor;
		public int m_glowOpacity;
		public int m_glowSize;
		public int m_glowSpread;
		public eBlendMode m_glowBlendMode;
		public bool m_hasInnerGlow;
		public SKColor m_innerGlowColor;
		public int m_innerGlowOpacity;
		public int m_innerGlowSize;
		public int m_innerGlowSpread;
		public eBlendMode m_innerGlowBlendMode;
		public bool m_hasBevel;
		public int m_bevelDepth;
		public int m_bevelSize;
		public int m_bevelAngle;
		public SKColor m_bevelHighlightColor;
		public int m_bevelHighlightOpacity;
		public SKColor m_bevelShadowColor;
		public int m_bevelShadowOpacity;
		public eBlendMode m_bevelBlendMode;

		public LayerStyle()
		{
			m_hasStroke = false;
			m_strokeSize = 3;
			m_strokePosition = 2;
			m_strokeColor = new SKColor(0, 0, 0, 255);
			m_strokeOpacity = 100;
			m_strokeBlendMode = eBlendMode.Normal;
			m_hasDropShadow = false;
			m_shadowColor = new SKColor(0, 0, 0, 255);
			m_shadowOpacity = 75;
			m_shadowAngle = 135;
			m_shadowDistance = 5;
			m_shadowSize = 5;
			m_shadowSpread = 0;
			m_shadowBlendMode = eBlendMode.Normal;
			m_hasOuterGlow = false;
			m_glowColor = new SKColor(255, 255, 190, 255);
			m_glowOpacity = 75;
			m_glowSize = 5;
			m_glowSpread = 0;
			m_glowBlendMode = eBlendMode.Normal;
			m_hasInnerGlow = false;
			m_innerGlowColor = new SKColor(255, 255, 190, 255);
			m_innerGlowOpacity = 75;
			m_innerGlowSize = 5;
			m_innerGlowSpread = 0;
			m_innerGlowBlendMode = eBlendMode.Normal;
			m_hasBevel = false;
			m_bevelDepth = 100;
			m_bevelSize = 5;
			m_bevelAngle = 120;
			m_bevelHighlightColor = new SKColor(255, 255, 255, 255);
			m_bevelHighlightOpacity = 75;
			m_bevelShadowColor = new SKColor(0, 0, 0, 255);
			m_bevelShadowOpacity = 75;
			m_bevelBlendMode = eBlendMode.Normal;
		}

		public bool HasAnyEffect()
		{
			return m_hasStroke || m_hasDropShadow || m_hasOuterGlow || m_hasInnerGlow || m_hasBevel;
		}

		public bool SameStroke(LayerStyle other)
		{
			return m_hasStroke == other.m_hasStroke && m_strokeSize == other.m_strokeSize && m_strokePosition == other.m_strokePosition && m_strokeColor == other.m_strokeColor && m_strokeOpacity == other.m_strokeOpacity;
		}

		public bool SameShadow(LayerStyle other)
		{
			return m_hasDropShadow == other.m_hasDropShadow && m_shadowColor == other.m_shadowColor && m_shadowOpacity == other.m_shadowOpacity && m_shadowAngle == other.m_shadowAngle && m_shadowDistance == other.m_shadowDistance && m_shadowSize == other.m_shadowSize && m_shadowSpread == other.m_shadowSpread;
		}

		public bool SameOuterGlow(LayerStyle other)
		{
			return m_hasOuterGlow == other.m_hasOuterGlow && m_glowColor == other.m_glowColor && m_glowOpacity == other.m_glowOpacity && m_glowSize == other.m_glowSize && m_glowSpread == other.m_glowSpread;
		}

		public bool SameInnerGlow(LayerStyle other)
		{
			return m_hasInnerGlow == other.m_hasInnerGlow && m_innerGlowColor == other.m_innerGlowColor && m_innerGlowOpacity == other.m_innerGlowOpacity && m_innerGlowSize == other.m_innerGlowSize && m_innerGlowSpread == other.m_innerGlowSpread;
		}

		public bool SameBevel(LayerStyle other)
		{
			return m_hasBevel == other.m_hasBevel && m_bevelDepth == other.m_bevelDepth && m_bevelSize == other.m_bevelSize && m_bevelAngle == other.m_bevelAngle && m_bevelHighlightColor == other.m_bevelHighlightColor && m_bevelHighlightOpacity == other.m_bevelHighlightOpacity && m_bevelShadowColor == other.m_bevelShadowColor && m_bevelShadowOpacity == other.m_bevelShadowOpacity;
		}

		public LayerStyle Clone()
		{
			LayerStyle copy = new LayerStyle();
			copy.m_hasStroke = m_hasStroke;
			copy.m_strokeSize = m_strokeSize;
			copy.m_strokePosition = m_strokePosition;
			copy.m_strokeColor = m_strokeColor;
			copy.m_strokeOpacity = m_strokeOpacity;
			copy.m_strokeBlendMode = m_strokeBlendMode;
			copy.m_hasDropShadow = m_hasDropShadow;
			copy.m_shadowColor = m_shadowColor;
			copy.m_shadowOpacity = m_shadowOpacity;
			copy.m_shadowAngle = m_shadowAngle;
			copy.m_shadowDistance = m_shadowDistance;
			copy.m_shadowSize = m_shadowSize;
			copy.m_shadowSpread = m_shadowSpread;
			copy.m_shadowBlendMode = m_shadowBlendMode;
			copy.m_hasOuterGlow = m_hasOuterGlow;
			copy.m_glowColor = m_glowColor;
			copy.m_glowOpacity = m_glowOpacity;
			copy.m_glowSize = m_glowSize;
			copy.m_glowSpread = m_glowSpread;
			copy.m_glowBlendMode = m_glowBlendMode;
			copy.m_hasInnerGlow = m_hasInnerGlow;
			copy.m_innerGlowColor = m_innerGlowColor;
			copy.m_innerGlowOpacity = m_innerGlowOpacity;
			copy.m_innerGlowSize = m_innerGlowSize;
			copy.m_innerGlowSpread = m_innerGlowSpread;
			copy.m_innerGlowBlendMode = m_innerGlowBlendMode;
			copy.m_hasBevel = m_hasBevel;
			copy.m_bevelDepth = m_bevelDepth;
			copy.m_bevelSize = m_bevelSize;
			copy.m_bevelAngle = m_bevelAngle;
			copy.m_bevelHighlightColor = m_bevelHighlightColor;
			copy.m_bevelHighlightOpacity = m_bevelHighlightOpacity;
			copy.m_bevelShadowColor = m_bevelShadowColor;
			copy.m_bevelShadowOpacity = m_bevelShadowOpacity;
			copy.m_bevelBlendMode = m_bevelBlendMode;
			return copy;
		}
	}
}
