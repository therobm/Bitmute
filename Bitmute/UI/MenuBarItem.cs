namespace Bitmute.UI
{
	public enum eMenuAction
	{
		None,
		NewDocument,
		OpenFile,
		Save,
		SaveAs,
		ExportAs,
		OpenRecent,
		Exit,
		Undo,
		Redo,
		Cut,
		Copy,
		Paste,
		StrokeDialog,
		DefinePattern,
		DefineBrush,
		SaveBrushPreset,
		Preferences,
		FreeTransform,
		TransformScale,
		TransformRotate,
		TransformSkew,
		TransformDistort,
		TransformPerspective,
		FlipLayerHorizontal,
		FlipLayerVertical,
		BrightnessContrast,
		HueSaturation,
		Desaturate,
		InvertColors,
		Posterize,
		Threshold,
		ImageSize,
		CanvasSize,
		FlipHorizontal,
		FlipVertical,
		Rotate90Clockwise,
		Rotate180,
		Rotate90CounterClockwise,
		RotateArbitrary,
		CropToSelection,
		Trim,
		ModeMenu,
		Mode8,
		Mode16,
		Mode32,
		NewLayer,
		DeleteLayer,
		MergeDown,
		MergeVisible,
		FlattenImage,
		LayerStyle,
		LayerProperties,
		RasterizeText,
		SelectAll,
		Deselect,
		InvertSelection,
		FeatherSelection,
		ContractSelection,
		GaussianBlur,
		UnsharpMask,
		AddNoise,
		Pixelate,
		ZoomIn,
		ZoomOut,
		FitOnScreen,
		ToggleRulers,
		ToggleGrid,
		ToggleSnap,
		ToggleSnapGuides,
		ToggleSnapGrid,
		ToggleSnapEdges,
		ToggleSnapLayers,
		ToggleLockGuides,
		ClearGuides,
		CascadeWindows,
		TileWindows,
		ToggleNavigatorPanel,
		ToggleSwatchesPanel,
		ToggleLayersPanel,
		TogglePatternsPanel,
		AboutBitmute,
		ReportBug,
		OpenRecentMenu,
		TransformMenu,
		AdjustmentsMenu,
		SnapToMenu,
		FilterBlurMenu,
		FilterDistortMenu,
		FilterNoiseMenu,
		FilterPixelateMenu,
		FilterRenderMenu,
		FilterSharpenMenu,
		FilterStylizeMenu,
		FilterVideoMenu,
		FilterGenerateMenu,
		LastFilter,
		Clouds,
		DifferenceClouds,
		AverageBlur,
		Blur,
		BlurMore,
		BoxBlur,
		MotionBlur,
		RadialBlur,
		Despeckle,
		Median,
		Crystallize,
		Facet,
		Fragment,
		Pointillize,
		Sharpen,
		SharpenEdges,
		SharpenMore,
		HighPass,
		Offset,
		Diffuse,
		Emboss,
		FindEdges,
		Solarize,
		DeInterlace,
		Pinch,
		PolarCoordinates,
		Ripple,
		Shear,
		Spherize,
		Twirl,
		Wave,
		NormalMap,
		TogglePatternPreview,
		FilterOtherMenu
	}

	public class MenuBarItem
	{
		public string m_label;
		public eMenuAction m_action;
		public string m_accelerator;
		public bool m_enabled;
		public bool m_checked;
		public bool m_separator;
		public bool m_submenu;
		public System.Action m_invoke;

		public MenuBarItem(string label, eMenuAction action) : this(label, action, "")
		{
		}

		public MenuBarItem(string label, eMenuAction action, string accelerator)
		{
			m_label = label;
			m_action = action;
			m_accelerator = accelerator;
			m_enabled = true;
			m_checked = false;
			m_separator = false;
			m_submenu = false;
			m_invoke = null;
		}

		public MenuBarItem(string label, eMenuAction action, System.Action invoke) : this(label, action, "")
		{
			m_invoke = invoke;
		}

		public MenuBarItem(string label, eMenuAction action, string accelerator, System.Action invoke) : this(label, action, accelerator)
		{
			m_invoke = invoke;
		}
	}
}
