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
		AboutBitmute,
		OpenRecentMenu,
		TransformMenu,
		AdjustmentsMenu,
		SnapToMenu,
		FilterArtisticMenu,
		FilterBlurMenu,
		FilterBrushStrokesMenu,
		FilterDistortMenu,
		FilterNoiseMenu,
		FilterPixelateMenu,
		FilterRenderMenu,
		FilterSharpenMenu
	}

	public class MenuBarItem
	{
		public string m_label;
		public eMenuAction m_action;
		public string m_argument;
		public string m_accelerator;
		public bool m_enabled;
		public bool m_checked;
		public bool m_separator;
		public bool m_submenu;

		public MenuBarItem(string label, eMenuAction action) : this(label, action, "")
		{
		}

		public MenuBarItem(string label, eMenuAction action, string accelerator)
		{
			m_label = label;
			m_action = action;
			m_accelerator = accelerator;
			m_argument = "";
			m_enabled = true;
			m_checked = false;
			m_separator = false;
			m_submenu = false;
		}
	}
}
