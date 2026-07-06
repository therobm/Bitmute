using System;
using SkiaSharp;

namespace Bitmute.UI
{
	public enum eAdjustmentKind
	{
		Layer,
		Canvas,
		Selection
	}

	public class Adjustment
	{
		public eMenuAction m_menuAction;
		public eMenuAction m_category;
		public string m_name;
		public eAdjustmentKind m_kind;
		public bool m_previewable;
		public bool m_instant;
		public string[] m_sliderLabels;
		public int[] m_sliderMinimums;
		public int[] m_sliderMaximums;
		public int[] m_sliderDefaults;
		public string[] m_choiceLabels;
		public string[][] m_choiceOptions;
		public int[] m_choiceDefaults;
		public string[] m_floatSliderLabels;
		public float[] m_floatSliderMinimums;
		public float[] m_floatSliderMaximums;
		public float[] m_floatSliderDefaults;
		public int[] m_floatSliderDecimals;
		public string[] m_checkLabels;
		public bool[] m_checkDefaults;
		public double m_dialogWidth;
		public double m_dialogHeight;
		public Action<SKBitmap, int[]> m_run;
		public string m_skslSource;
		public int m_skslPasses;
		public bool m_builtinBlurPreview;
		public bool m_skslRawSource;
	}
}
