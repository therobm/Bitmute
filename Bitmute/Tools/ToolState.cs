using Bitmute.Imaging;
using SkiaSharp;

namespace Bitmute.Tools
{
	public class ToolState
	{
		private eTool m_tool;
		private SKColor m_foreground;
		private SKColor m_background;
		private int m_brushSize;
		private int m_brushHardness;
		private int m_brushOpacity;
		private int m_brushFlow;
		private int m_brushSpacing;
		private bool m_brushSquareTip;
		private int m_brushRoundness = 100;
		private int m_brushFadeLength = 0;
		private SKBitmap m_activeCustomTip = null;
		private int m_brushAngle;
		private int m_brushSmoothing;
		private int m_brushStrength;
		private eBlendMode m_brushMode;
		private int m_fillTolerance;
		private eFillContent m_fillContent = eFillContent.Foreground;
		private Pattern m_activePattern = null;
		private bool m_spongeSaturate;
		private int m_colorReplaceMode;
		private int m_colorReplaceTolerance;
		private int m_dodgeBurnRange;
		private int m_dodgeBurnExposure;
		private int m_gradientType;
		private bool m_gradientReverse;
		private bool m_gradientToTransparent;
		private bool m_snapToGuides;
		private bool m_snapGrid;
		private bool m_snapEdges;
		private bool m_snapLayerBounds;
		private int m_snapGridSize;
		private int m_snapTolerance;
		private bool m_airbrush;
		private bool m_cloneAligned;
		private bool m_wandContiguous;
		private bool m_wandSampleAll;
		private bool m_wandAntiAlias;
		private int m_selectionFeather;
		private bool m_selectionAntiAlias;
		private int m_selectionMode;
		private int m_magneticWidth;
		private int m_magneticContrast;
		private bool m_lineAntiAlias;
		private int m_textSize;
		private bool m_textBold;
		private bool m_textItalic;
		private string m_textFontFamily;
		private int m_textAlign;
		private int m_textAntiAlias;
		private bool m_textLeadingAuto;
		private float m_textLeading;
		private int m_textTracking;
		private int m_textHorizontalScale;
		private int m_textVerticalScale;
		private int m_textBaselineShift;
		private bool m_textFauxBold;
		private bool m_textFauxItalic;
		private bool m_textKerningAuto;
		private bool m_shiftHeld;
		private bool m_altHeld;
		private bool m_ctrlHeld;
		private float m_penPressure = 1.0f;
		private bool m_pressureSizeEnabled = true;
		private bool m_pressureOpacityEnabled = true;
		private PressureCalibration m_pressureCalibration = new PressureCalibration();
		private int m_pressureMinimumSizePercent = 1;
		private int m_pressureMinimumOpacityPercent = 20;

		public ToolState()
		{
			m_tool = eTool.Brush;
			m_foreground = new SKColor(0, 0, 0, 255);
			m_background = new SKColor(255, 255, 255, 255);
			m_brushSize = 6;
			m_brushHardness = 100;
			m_brushOpacity = 100;
			m_brushFlow = 100;
			m_brushSpacing = 10;
			m_brushSquareTip = false;
			m_brushSmoothing = 0;
			m_brushStrength = 50;
			m_brushMode = eBlendMode.Normal;
			m_fillTolerance = 0;
			m_spongeSaturate = false;
			m_colorReplaceMode = 0;
			m_colorReplaceTolerance = 32;
			m_dodgeBurnRange = 1;
			m_dodgeBurnExposure = 50;
			m_gradientType = 0;
			m_gradientReverse = false;
			m_gradientToTransparent = false;
			m_snapToGuides = false;
			m_snapGrid = false;
			m_snapEdges = false;
			m_snapLayerBounds = false;
			m_snapGridSize = 16;
			m_snapTolerance = 6;
			m_airbrush = false;
			m_cloneAligned = true;
			m_wandContiguous = true;
			m_wandSampleAll = false;
			m_wandAntiAlias = true;
			m_selectionFeather = 0;
			m_selectionAntiAlias = true;
			m_selectionMode = 0;
			m_magneticWidth = 10;
			m_magneticContrast = 10;
			m_lineAntiAlias = false;
			m_textSize = 32;
			m_textBold = false;
			m_textItalic = false;
			m_textFontFamily = "OpenSans-Regular";
			m_textAlign = 0;
			m_textAntiAlias = 3;
			m_textLeadingAuto = true;
			m_textLeading = 0.0f;
			m_textTracking = 0;
			m_textHorizontalScale = 100;
			m_textVerticalScale = 100;
			m_textBaselineShift = 0;
			m_textFauxBold = false;
			m_textFauxItalic = false;
			m_textKerningAuto = true;
			m_shiftHeld = false;
			m_altHeld = false;
			m_ctrlHeld = false;
		}

		public int MagneticWidth()
		{
			return m_magneticWidth;
		}

		public void SetMagneticWidth(int width)
		{
			if (width < 1)
			{
				width = 1;
			}
			m_magneticWidth = width;
		}

		public int MagneticContrast()
		{
			return m_magneticContrast;
		}

		public void SetMagneticContrast(int contrast)
		{
			if (contrast < 0)
			{
				contrast = 0;
			}
			if (contrast > 100)
			{
				contrast = 100;
			}
			m_magneticContrast = contrast;
		}

		public bool WandContiguous()
		{
			return m_wandContiguous;
		}

		public void SetWandContiguous(bool contiguous)
		{
			m_wandContiguous = contiguous;
		}

		public bool WandSampleAll()
		{
			return m_wandSampleAll;
		}

		public void SetWandSampleAll(bool sampleAll)
		{
			m_wandSampleAll = sampleAll;
		}

		public bool WandAntiAlias()
		{
			return m_wandAntiAlias;
		}

		public void SetWandAntiAlias(bool antiAlias)
		{
			m_wandAntiAlias = antiAlias;
		}

		public int SelectionFeather()
		{
			return m_selectionFeather;
		}

		public void SetSelectionFeather(int feather)
		{
			if (feather < 0)
			{
				feather = 0;
			}
			if (feather > 100)
			{
				feather = 100;
			}
			m_selectionFeather = feather;
		}

		public bool SelectionAntiAlias()
		{
			return m_selectionAntiAlias;
		}

		public void SetSelectionAntiAlias(bool antiAlias)
		{
			m_selectionAntiAlias = antiAlias;
		}

		public int SelectionMode()
		{
			return m_selectionMode;
		}

		public void SetSelectionMode(int mode)
		{
			if (mode < 0)
			{
				mode = 0;
			}
			if (mode > 3)
			{
				mode = 3;
			}
			m_selectionMode = mode;
		}

		public bool TextLeadingAuto()
		{
			return m_textLeadingAuto;
		}

		public void SetTextLeadingAuto(bool auto)
		{
			m_textLeadingAuto = auto;
		}

		public float TextLeading()
		{
			return m_textLeading;
		}

		public void SetTextLeading(float leading)
		{
			m_textLeading = leading;
		}

		public int TextTracking()
		{
			return m_textTracking;
		}

		public void SetTextTracking(int tracking)
		{
			m_textTracking = tracking;
		}

		public int TextHorizontalScale()
		{
			return m_textHorizontalScale;
		}

		public void SetTextHorizontalScale(int scale)
		{
			if (scale < 1)
			{
				scale = 1;
			}
			m_textHorizontalScale = scale;
		}

		public int TextVerticalScale()
		{
			return m_textVerticalScale;
		}

		public void SetTextVerticalScale(int scale)
		{
			if (scale < 1)
			{
				scale = 1;
			}
			m_textVerticalScale = scale;
		}

		public int TextBaselineShift()
		{
			return m_textBaselineShift;
		}

		public void SetTextBaselineShift(int shift)
		{
			m_textBaselineShift = shift;
		}

		public bool TextFauxBold()
		{
			return m_textFauxBold;
		}

		public void SetTextFauxBold(bool fauxBold)
		{
			m_textFauxBold = fauxBold;
		}

		public bool TextFauxItalic()
		{
			return m_textFauxItalic;
		}

		public void SetTextFauxItalic(bool fauxItalic)
		{
			m_textFauxItalic = fauxItalic;
		}

		public bool TextKerningAuto()
		{
			return m_textKerningAuto;
		}

		public void SetTextKerningAuto(bool auto)
		{
			m_textKerningAuto = auto;
		}

		public int TextSize()
		{
			return m_textSize;
		}

		public void SetTextSize(int size)
		{
			m_textSize = size;
		}

		public bool TextBold()
		{
			return m_textBold;
		}

		public void SetTextBold(bool bold)
		{
			m_textBold = bold;
		}

		public bool TextItalic()
		{
			return m_textItalic;
		}

		public void SetTextItalic(bool italic)
		{
			m_textItalic = italic;
		}

		public string TextFontFamily()
		{
			return m_textFontFamily;
		}

		public void SetTextFontFamily(string family)
		{
			m_textFontFamily = family;
		}

		public int TextAlign()
		{
			return m_textAlign;
		}

		public void SetTextAlign(int align)
		{
			m_textAlign = align;
		}

		public int TextAntiAlias()
		{
			return m_textAntiAlias;
		}

		public void SetTextAntiAlias(int antiAlias)
		{
			m_textAntiAlias = antiAlias;
		}

		public bool LineAntiAlias()
		{
			return m_lineAntiAlias;
		}

		public void SetLineAntiAlias(bool antiAlias)
		{
			m_lineAntiAlias = antiAlias;
		}

		public bool ShiftHeld()
		{
			return m_shiftHeld;
		}

		public void SetShiftHeld(bool shiftHeld)
		{
			m_shiftHeld = shiftHeld;
		}

		public bool AltHeld()
		{
			return m_altHeld;
		}

		public void SetAltHeld(bool altHeld)
		{
			m_altHeld = altHeld;
		}

		public bool CtrlHeld()
		{
			return m_ctrlHeld;
		}

		public void SetCtrlHeld(bool ctrlHeld)
		{
			m_ctrlHeld = ctrlHeld;
		}

		public eTool Tool()
		{
			return m_tool;
		}

		public void SetTool(eTool tool)
		{
			m_tool = tool;
		}

		public SKColor Foreground()
		{
			return m_foreground;
		}

		public void SetForeground(SKColor color)
		{
			m_foreground = color;
		}

		public SKColor Background()
		{
			return m_background;
		}

		public void SetBackground(SKColor color)
		{
			m_background = color;
		}

		public int BrushSize()
		{
			return m_brushSize;
		}

		public void SetBrushSize(int size)
		{
			if (size < 1)
			{
				size = 1;
			}
			m_brushSize = size;
		}

		public int BrushHardness()
		{
			return m_brushHardness;
		}

		public void SetBrushHardness(int hardness)
		{
			if (hardness < 0)
			{
				hardness = 0;
			}
			if (hardness > 100)
			{
				hardness = 100;
			}
			m_brushHardness = hardness;
		}

		public int BrushOpacity()
		{
			return m_brushOpacity;
		}

		public void SetBrushOpacity(int opacity)
		{
			if (opacity < 1)
			{
				opacity = 1;
			}
			if (opacity > 100)
			{
				opacity = 100;
			}
			m_brushOpacity = opacity;
		}

		public float PenPressure()
		{
			return m_penPressure;
		}

		public void SetPenPressure(float pressure)
		{
			if (pressure < 0.0f)
			{
				pressure = 0.0f;
			}
			if (pressure > 1.0f)
			{
				pressure = 1.0f;
			}
			m_penPressure = m_pressureCalibration.Apply(pressure);
		}

		public PressureCalibration Calibration()
		{
			return m_pressureCalibration;
		}

		public int PressureMinimumSizePercent()
		{
			return m_pressureMinimumSizePercent;
		}

		public void SetPressureMinimumSizePercent(int percent)
		{
			if (percent < 1)
			{
				percent = 1;
			}
			if (percent > 100)
			{
				percent = 100;
			}
			m_pressureMinimumSizePercent = percent;
		}

		public double PressureMinimumSize()
		{
			return m_pressureMinimumSizePercent / 100.0;
		}

		public int PressureMinimumOpacityPercent()
		{
			return m_pressureMinimumOpacityPercent;
		}

		public void SetPressureMinimumOpacityPercent(int percent)
		{
			if (percent < 1)
			{
				percent = 1;
			}
			if (percent > 100)
			{
				percent = 100;
			}
			m_pressureMinimumOpacityPercent = percent;
		}

		public double PressureMinimumOpacity()
		{
			return m_pressureMinimumOpacityPercent / 100.0;
		}

		public bool PressureSizeEnabled()
		{
			return m_pressureSizeEnabled;
		}

		public void SetPressureSizeEnabled(bool enabled)
		{
			m_pressureSizeEnabled = enabled;
		}

		public bool PressureOpacityEnabled()
		{
			return m_pressureOpacityEnabled;
		}

		public void SetPressureOpacityEnabled(bool enabled)
		{
			m_pressureOpacityEnabled = enabled;
		}

		public int BrushFlow()
		{
			return m_brushFlow;
		}

		public void SetBrushFlow(int flow)
		{
			if (flow < 1)
			{
				flow = 1;
			}
			if (flow > 100)
			{
				flow = 100;
			}
			m_brushFlow = flow;
		}

		public int BrushSpacing()
		{
			return m_brushSpacing;
		}

		public void SetBrushSpacing(int spacing)
		{
			if (spacing < 1)
			{
				spacing = 1;
			}
			if (spacing > 100)
			{
				spacing = 100;
			}
			m_brushSpacing = spacing;
		}

		public int FadeLength()
		{
			return m_brushFadeLength;
		}

		public void SetBrushFadeLength(int lengthPx)
		{
			if (lengthPx < 0)
			{
				lengthPx = 0;
			}
			m_brushFadeLength = lengthPx;
		}

		public SKBitmap ActiveCustomTip()
		{
			return m_activeCustomTip;
		}

		public void SetActiveCustomTip(SKBitmap tip)
		{
			m_activeCustomTip = tip;
		}

		public bool BrushSquareTip()
		{
			return m_brushSquareTip;
		}

		public void SetBrushSquareTip(bool square)
		{
			m_brushSquareTip = square;
		}

		public int BrushRoundness()
		{
			return m_brushRoundness;
		}

		public void SetBrushRoundness(int roundness)
		{
			if (roundness < 5)
			{
				roundness = 5;
			}
			if (roundness > 100)
			{
				roundness = 100;
			}
			m_brushRoundness = roundness;
		}

		public int BrushAngle()
		{
			return m_brushAngle;
		}

		public void SetBrushAngle(int angle)
		{
			if (angle < 0)
			{
				angle = 0;
			}
			if (angle > 180)
			{
				angle = 180;
			}
			m_brushAngle = angle;
		}

		public int BrushSmoothing()
		{
			return m_brushSmoothing;
		}

		public void SetBrushSmoothing(int smoothing)
		{
			if (smoothing < 0)
			{
				smoothing = 0;
			}
			if (smoothing > 100)
			{
				smoothing = 100;
			}
			m_brushSmoothing = smoothing;
		}

		public int BrushStrength()
		{
			return m_brushStrength;
		}

		public void SetBrushStrength(int strength)
		{
			if (strength < 1)
			{
				strength = 1;
			}
			if (strength > 100)
			{
				strength = 100;
			}
			m_brushStrength = strength;
		}

		public eBlendMode BrushMode()
		{
			return m_brushMode;
		}

		public void SetBrushMode(eBlendMode mode)
		{
			m_brushMode = mode;
		}

		public int FillTolerance()
		{
			return m_fillTolerance;
		}

		public void SetFillTolerance(int tolerance)
		{
			m_fillTolerance = tolerance;
		}

		public eFillContent FillContent()
		{
			return m_fillContent;
		}

		public void SetFillContent(eFillContent content)
		{
			m_fillContent = content;
		}

		public Pattern ActivePattern()
		{
			return m_activePattern;
		}

		public void SetActivePattern(Pattern pattern)
		{
			m_activePattern = pattern;
		}

		public bool SpongeSaturate()
		{
			return m_spongeSaturate;
		}

		public void SetSpongeSaturate(bool saturate)
		{
			m_spongeSaturate = saturate;
		}

		public int ColorReplaceMode()
		{
			return m_colorReplaceMode;
		}

		public void SetColorReplaceMode(int mode)
		{
			m_colorReplaceMode = mode;
		}

		public int ColorReplaceTolerance()
		{
			return m_colorReplaceTolerance;
		}

		public void SetColorReplaceTolerance(int tolerance)
		{
			m_colorReplaceTolerance = tolerance;
		}

		public int DodgeBurnRange()
		{
			return m_dodgeBurnRange;
		}

		public void SetDodgeBurnRange(int range)
		{
			if (range < 0)
			{
				range = 0;
			}
			if (range > 2)
			{
				range = 2;
			}
			m_dodgeBurnRange = range;
		}

		public int DodgeBurnExposure()
		{
			return m_dodgeBurnExposure;
		}

		public void SetDodgeBurnExposure(int exposure)
		{
			if (exposure < 1)
			{
				exposure = 1;
			}
			if (exposure > 100)
			{
				exposure = 100;
			}
			m_dodgeBurnExposure = exposure;
		}

		public int GradientType()
		{
			return m_gradientType;
		}

		public void SetGradientType(int type)
		{
			m_gradientType = type;
		}

		public bool GradientReverse()
		{
			return m_gradientReverse;
		}

		public void SetGradientReverse(bool reverse)
		{
			m_gradientReverse = reverse;
		}

		public bool GradientToTransparent()
		{
			return m_gradientToTransparent;
		}

		public void SetGradientToTransparent(bool toTransparent)
		{
			m_gradientToTransparent = toTransparent;
		}

		public bool SnapToGuides()
		{
			return m_snapToGuides;
		}

		public void SetSnapToGuides(bool snap)
		{
			m_snapToGuides = snap;
		}

		public int SnapTolerance()
		{
			return m_snapTolerance;
		}

		public void SetSnapTolerance(int tolerance)
		{
			m_snapTolerance = tolerance;
		}

		public bool SnapGrid()
		{
			return m_snapGrid;
		}

		public void SetSnapGrid(bool snap)
		{
			m_snapGrid = snap;
		}

		public bool SnapEdges()
		{
			return m_snapEdges;
		}

		public void SetSnapEdges(bool snap)
		{
			m_snapEdges = snap;
		}

		public bool SnapLayerBounds()
		{
			return m_snapLayerBounds;
		}

		public void SetSnapLayerBounds(bool snap)
		{
			m_snapLayerBounds = snap;
		}

		public int SnapGridSize()
		{
			return m_snapGridSize;
		}

		public void SetSnapGridSize(int size)
		{
			m_snapGridSize = size;
		}

		public bool Airbrush()
		{
			return m_airbrush;
		}

		public void SetAirbrush(bool airbrush)
		{
			m_airbrush = airbrush;
		}

		public bool CloneAligned()
		{
			return m_cloneAligned;
		}

		public void SetCloneAligned(bool aligned)
		{
			m_cloneAligned = aligned;
		}

		public void SwapColors()
		{
			SKColor temp = m_foreground;
			m_foreground = m_background;
			m_background = temp;
		}

		public void ResetColors()
		{
			m_foreground = new SKColor(0, 0, 0, 255);
			m_background = new SKColor(255, 255, 255, 255);
		}
	}
}
