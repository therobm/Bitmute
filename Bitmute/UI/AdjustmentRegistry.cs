using System;
using System.Collections.Generic;
using SkiaSharp;
using Bitmute.Imaging;
using Bitmute.Tools;
using Bitmute.UI.Dialogs;

namespace Bitmute.UI
{
	public class AdjustmentRegistry
	{
		private static readonly string[] s_noLabels = new string[0];
		private static readonly int[] s_noValues = new int[0];
		private static readonly string[][] s_noOptions = new string[0][];
		private static readonly float[] s_noFloats = new float[0];
		private static readonly bool[] s_noBools = new bool[0];

		private MainView m_main;
		private ToolState m_toolState;
		private List<Adjustment> m_adjustments;
		private Adjustment m_lastFilter;
		private int[] m_lastFilterValues;
		private int m_filterSeed;

		private Adjustment AddInstant(eMenuAction action, eMenuAction category, string name, Action<SKBitmap, int[]> run)
		{
			Adjustment adjustment = new Adjustment();
			adjustment.m_menuAction = action;
			adjustment.m_category = category;
			adjustment.m_name = name;
			adjustment.m_kind = eAdjustmentKind.Layer;
			adjustment.m_previewable = false;
			adjustment.m_instant = true;
			adjustment.m_sliderLabels = s_noLabels;
			adjustment.m_sliderMinimums = s_noValues;
			adjustment.m_sliderMaximums = s_noValues;
			adjustment.m_sliderDefaults = s_noValues;
			adjustment.m_choiceLabels = s_noLabels;
			adjustment.m_choiceOptions = s_noOptions;
			adjustment.m_choiceDefaults = s_noValues;
			adjustment.m_floatSliderLabels = s_noLabels;
			adjustment.m_floatSliderMinimums = s_noFloats;
			adjustment.m_floatSliderMaximums = s_noFloats;
			adjustment.m_floatSliderDefaults = s_noFloats;
			adjustment.m_floatSliderDecimals = s_noValues;
			adjustment.m_checkLabels = s_noLabels;
			adjustment.m_checkDefaults = s_noBools;
			adjustment.m_run = run;
			m_adjustments.Add(adjustment);
			return adjustment;
		}

		private Adjustment AddDialog(eMenuAction action, eMenuAction category, string name, bool previewable, string[] sliderLabels, int[] minimums, int[] maximums, int[] defaults, double width, double height, Action<SKBitmap, int[]> run)
		{
			Adjustment adjustment = new Adjustment();
			adjustment.m_menuAction = action;
			adjustment.m_category = category;
			adjustment.m_name = name;
			adjustment.m_kind = eAdjustmentKind.Layer;
			adjustment.m_previewable = previewable;
			adjustment.m_instant = false;
			adjustment.m_sliderLabels = sliderLabels;
			adjustment.m_sliderMinimums = minimums;
			adjustment.m_sliderMaximums = maximums;
			adjustment.m_sliderDefaults = defaults;
			adjustment.m_choiceLabels = s_noLabels;
			adjustment.m_choiceOptions = s_noOptions;
			adjustment.m_choiceDefaults = s_noValues;
			adjustment.m_floatSliderLabels = s_noLabels;
			adjustment.m_floatSliderMinimums = s_noFloats;
			adjustment.m_floatSliderMaximums = s_noFloats;
			adjustment.m_floatSliderDefaults = s_noFloats;
			adjustment.m_floatSliderDecimals = s_noValues;
			adjustment.m_checkLabels = s_noLabels;
			adjustment.m_checkDefaults = s_noBools;
			adjustment.m_dialogWidth = width;
			adjustment.m_dialogHeight = height;
			adjustment.m_run = run;
			m_adjustments.Add(adjustment);
			return adjustment;
		}

		private Adjustment AddChoiceDialog(eMenuAction action, eMenuAction category, string name, bool previewable, string[] sliderLabels, int[] minimums, int[] maximums, int[] defaults, string[] choiceLabels, string[][] choiceOptions, int[] choiceDefaults, double width, double height, Action<SKBitmap, int[]> run)
		{
			Adjustment adjustment = AddDialog(action, category, name, previewable, sliderLabels, minimums, maximums, defaults, width, height, run);
			adjustment.m_choiceLabels = choiceLabels;
			adjustment.m_choiceOptions = choiceOptions;
			adjustment.m_choiceDefaults = choiceDefaults;
			return adjustment;
		}

		private void RollSeed()
		{
			m_filterSeed = Environment.TickCount;
		}

		private bool IsFilterCategory(eMenuAction category)
		{
			return category == eMenuAction.FilterBlurMenu || category == eMenuAction.FilterDistortMenu || category == eMenuAction.FilterNoiseMenu || category == eMenuAction.FilterPixelateMenu || category == eMenuAction.FilterRenderMenu || category == eMenuAction.FilterSharpenMenu || category == eMenuAction.FilterStylizeMenu || category == eMenuAction.FilterVideoMenu || category == eMenuAction.FilterGenerateMenu || category == eMenuAction.FilterOtherMenu;
		}

		private void RecordLastFilter(Adjustment adjustment, int[] values)
		{
			if (!IsFilterCategory(adjustment.m_category))
			{
				return;
			}
			m_lastFilter = adjustment;
			m_lastFilterValues = values;
		}

		private void BeginPreview(Adjustment adjustment)
		{
			if (!adjustment.m_previewable)
			{
				return;
			}
			DocumentWindow window = m_main.ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			document.BeginStroke();
		}

		private bool GpuPreviewUsable(Adjustment adjustment, CanvasView canvas)
		{
			if (!adjustment.m_previewable)
			{
				return false;
			}
			Bitmute.Imaging.Document activeDocument = m_main.ActiveDocument();
			if (activeDocument != null && activeDocument.ColorDepth() != eColorDepth.Eight)
			{
				return false;
			}
			if (adjustment.m_skslSource == null && !adjustment.m_builtinBlurPreview)
			{
				return false;
			}
			if (!canvas.GpuFilterAvailable())
			{
				return false;
			}
			if (adjustment.m_skslSource != null)
			{
				bool compiled = GpuFilterPreview.CanRun(adjustment.m_skslSource);
				if (!compiled)
				{
					return false;
				}
			}
			return true;
		}

		private void BeginGpuPreview(Adjustment adjustment)
		{
			DocumentWindow window = m_main.ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			bool usable = GpuPreviewUsable(adjustment, canvas);
			if (!usable)
			{
				return;
			}
			Document document = window.DocumentModel();
			SKBitmap snapshot = document.StrokeSnapshot();
			if (snapshot == null)
			{
				return;
			}
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			canvas.FilterPreview().BeginSession(snapshot, activeLayer.Bitmap());
			if (adjustment.m_menuAction == eMenuAction.Diffuse)
			{
				SKBitmap offsetMap = new SKBitmap(activeLayer.Bitmap().Width, activeLayer.Bitmap().Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				GpuFilterPreview.BuildDiffuseOffsets(offsetMap, m_filterSeed);
				canvas.FilterPreview().SetSessionOffsetMap(offsetMap);
			}
		}

		private void RunBrightnessContrast(SKBitmap bitmap, int[] values)
		{
			Adjustments.BrightnessContrast(bitmap, values[0], values[1]);
		}

		private void RunHueSaturation(SKBitmap bitmap, int[] values)
		{
			Adjustments.HueSaturationLightness(bitmap, values[0], values[1], values[2]);
		}

		private void RunPosterize(SKBitmap bitmap, int[] values)
		{
			Adjustments.Posterize(bitmap, values[0]);
		}

		private void RunThreshold(SKBitmap bitmap, int[] values)
		{
			Adjustments.Threshold(bitmap, values[0]);
		}

		private void RunGaussianBlur(SKBitmap bitmap, int[] values)
		{
			Adjustments.GaussianBlur(bitmap, values[0]);
		}

		private void RunUnsharpMask(SKBitmap bitmap, int[] values)
		{
			Adjustments.UnsharpMask(bitmap, values[0], values[1]);
		}

		private void RunAddNoise(SKBitmap bitmap, int[] values)
		{
			Adjustments.AddNoise(bitmap, values[0], values[1] == 1);
		}

		private void RunMosaic(SKBitmap bitmap, int[] values)
		{
			Adjustments.Pixelate(bitmap, values[0]);
		}

		private void RunAverage(SKBitmap bitmap, int[] values)
		{
			FilterBlur.Average(bitmap);
		}

		private void RunBlur(SKBitmap bitmap, int[] values)
		{
			FilterBlur.Blur(bitmap);
		}

		private void RunBlurMore(SKBitmap bitmap, int[] values)
		{
			FilterBlur.BlurMore(bitmap);
		}

		private void RunBoxBlur(SKBitmap bitmap, int[] values)
		{
			FilterBlur.BoxBlur(bitmap, values[0]);
		}

		private void RunMotionBlur(SKBitmap bitmap, int[] values)
		{
			FilterBlur.MotionBlur(bitmap, values[0], values[1]);
		}

		private void RunRadialBlur(SKBitmap bitmap, int[] values)
		{
			FilterBlur.RadialBlur(bitmap, values[0], values[1]);
		}

		private void RunPinch(SKBitmap bitmap, int[] values)
		{
			FilterDistort.Pinch(bitmap, values[0]);
		}

		private void RunPolarCoordinates(SKBitmap bitmap, int[] values)
		{
			FilterDistort.PolarCoordinates(bitmap, values[0]);
		}

		private void RunRipple(SKBitmap bitmap, int[] values)
		{
			FilterDistort.Ripple(bitmap, values[0], values[1]);
		}

		private void RunShear(SKBitmap bitmap, int[] values)
		{
			FilterDistort.Shear(bitmap, values[0], values[1]);
		}

		private void RunSpherize(SKBitmap bitmap, int[] values)
		{
			FilterDistort.Spherize(bitmap, values[0], values[1]);
		}

		private void RunTwirl(SKBitmap bitmap, int[] values)
		{
			FilterDistort.Twirl(bitmap, values[0]);
		}

		private void RunWave(SKBitmap bitmap, int[] values)
		{
			FilterDistort.Wave(bitmap, values[0], values[1], values[2]);
		}

		private void RunDespeckle(SKBitmap bitmap, int[] values)
		{
			FilterNoise.Despeckle(bitmap);
		}

		private void RunMedian(SKBitmap bitmap, int[] values)
		{
			FilterNoise.Median(bitmap, values[0]);
		}

		private void RunCrystallize(SKBitmap bitmap, int[] values)
		{
			FilterPixelate.Crystallize(bitmap, values[0], m_filterSeed);
		}

		private void RunFacet(SKBitmap bitmap, int[] values)
		{
			FilterPixelate.Facet(bitmap);
		}

		private void RunFragment(SKBitmap bitmap, int[] values)
		{
			FilterPixelate.Fragment(bitmap);
		}

		private void RunPointillize(SKBitmap bitmap, int[] values)
		{
			FilterPixelate.Pointillize(bitmap, values[0], m_filterSeed, m_toolState.Background());
		}

		private void RunClouds(SKBitmap bitmap, int[] values)
		{
			FilterRender.Clouds(bitmap, m_toolState.Foreground(), m_toolState.Background(), m_filterSeed);
		}

		private void RunDifferenceClouds(SKBitmap bitmap, int[] values)
		{
			FilterRender.DifferenceClouds(bitmap, m_toolState.Foreground(), m_toolState.Background(), m_filterSeed);
		}

		private void RunSharpen(SKBitmap bitmap, int[] values)
		{
			FilterSharpen.Sharpen(bitmap);
		}

		private void RunHighPass(SKBitmap bitmap, int[] values)
		{
			FilterSharpen.HighPass(bitmap, values[0]);
		}

		private void RunOffset(SKBitmap bitmap, int[] values)
		{
			int choice = values[2];
			if (choice < 0 || choice > (int)eOffsetEdge.Transparent)
			{
				choice = (int)eOffsetEdge.Wrap;
			}
			FilterOther.Offset(bitmap, values[0], values[1], (eOffsetEdge)choice);
		}

		private void RunSharpenEdges(SKBitmap bitmap, int[] values)
		{
			FilterSharpen.SharpenEdges(bitmap);
		}

		private void RunSharpenMore(SKBitmap bitmap, int[] values)
		{
			FilterSharpen.SharpenMore(bitmap);
		}

		private void RunDiffuse(SKBitmap bitmap, int[] values)
		{
			FilterStylize.Diffuse(bitmap, values[0], m_filterSeed);
		}

		private void RunEmboss(SKBitmap bitmap, int[] values)
		{
			FilterStylize.Emboss(bitmap, values[0], values[1], values[2]);
		}

		private void RunFindEdges(SKBitmap bitmap, int[] values)
		{
			FilterStylize.FindEdges(bitmap);
		}

		private void RunSolarize(SKBitmap bitmap, int[] values)
		{
			FilterStylize.Solarize(bitmap);
		}

		private void RunDeInterlace(SKBitmap bitmap, int[] values)
		{
			FilterVideo.DeInterlace(bitmap, values[0], values[1]);
		}

		private void RunNormalMap(SKBitmap bitmap, int[] values)
		{
			float strength = values[0] / 10.0f;
			bool invertX = values[1] == 1;
			bool invertY = values[2] == 1;
			FilterGenerate.eNormalMapKernel kernel = (FilterGenerate.eNormalMapKernel)values[3];
			FilterGenerate.eNormalMapEdge edge = (FilterGenerate.eNormalMapEdge)values[4];
			FilterGenerate.NormalMap(bitmap, strength, kernel, invertX, invertY, edge);
		}

		public Adjustment ForAction(eMenuAction action)
		{
			for (int index = 0; index < m_adjustments.Count; index++)
			{
				if (m_adjustments[index].m_menuAction == action)
				{
					return m_adjustments[index];
				}
			}
			return null;
		}

		public bool BuildsSubmenu(eMenuAction category)
		{
			return IsFilterCategory(category);
		}

		public List<MenuBarItem> SubmenuItems(eMenuAction category)
		{
			List<MenuBarItem> items = new List<MenuBarItem>();
			for (int index = 0; index < m_adjustments.Count; index++)
			{
				Adjustment adjustment = m_adjustments[index];
				if (adjustment.m_category != category)
				{
					continue;
				}
				string label = adjustment.m_name;
				if (!adjustment.m_instant)
				{
					label = label + "…";
				}
				items.Add(new MenuBarItem(label, adjustment.m_menuAction, () => Open(adjustment)));
			}
			return items;
		}

		public bool HasLastFilter()
		{
			return m_lastFilter != null;
		}

		public string LastFilterLabel()
		{
			if (m_lastFilter == null)
			{
				return "Last Filter";
			}
			return "Last Filter: " + m_lastFilter.m_name;
		}

		public void Open(Adjustment adjustment)
		{
			Bitmute.Imaging.Document activeDocument = m_main.ActiveDocument();
			if (activeDocument != null && activeDocument.ColorDepth() != eColorDepth.Eight && !adjustment.m_depthAware)
			{
				string message = adjustment.m_name + " is not available for 16-bit or 32-bit images. Convert the image to 8-bit with Image > Mode to use it.";
				m_main.ShowModal(new MessageDialog(adjustment.m_name, message, new string[] { "OK" }, null), 360.0, 180.0);
				return;
			}
			RollSeed();
			if (adjustment.m_instant)
			{
				Apply(adjustment, s_noValues);
				m_main.RefreshLayerThumbnails();
				return;
			}
			ApplyDocumentSliderRanges(adjustment);
			BeginPreview(adjustment);
			BeginGpuPreview(adjustment);
			m_main.ShowModal(new AdjustmentDialog(adjustment), adjustment.m_dialogWidth, adjustment.m_dialogHeight);
		}

		private void ApplyDocumentSliderRanges(Adjustment adjustment)
		{
			// Offset ranges depend on the document: a horizontal/vertical shift only makes sense up to
			// the document's own width/height, so recompute them per-document instead of a fixed cap.
			if (adjustment.m_menuAction != eMenuAction.Offset)
			{
				return;
			}
			Bitmute.Imaging.Document document = m_main.ActiveDocument();
			if (document == null)
			{
				return;
			}
			int width = document.Width();
			int height = document.Height();
			adjustment.m_sliderMinimums = new int[] { -width, -height };
			adjustment.m_sliderMaximums = new int[] { width, height };
		}

		private void RunClipped(Document document, Adjustment adjustment, Layer activeLayer, int[] values)
		{
			Selection selection = document.Selection();
			if (!selection.IsActive())
			{
				adjustment.m_run(activeLayer.Bitmap(), values);
				return;
			}
			SKBitmap layerBitmap = activeLayer.Bitmap();
			SKBitmap adjusted = new SKBitmap(layerBitmap.Info);
			PixelRegion.CopyPixels(layerBitmap, adjusted);
			adjustment.m_run(adjusted, values);
			PixelRegion.BlendAdjustedIntoSelection(layerBitmap, adjusted, activeLayer.OffsetX(), activeLayer.OffsetY(), selection);
			adjusted.Dispose();
		}

		public void Apply(Adjustment adjustment, int[] values)
		{
			DocumentWindow window = m_main.ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			if (adjustment.m_kind == eAdjustmentKind.Canvas)
			{
				document.BeginCanvasEdit("Rotate");
				document.RotateArbitrary(values[0], 2);
				document.EndCanvasEdit();
				m_main.FinishCanvasOp(canvas, document);
				return;
			}
			if (adjustment.m_kind == eAdjustmentKind.Selection)
			{
				if (adjustment.m_menuAction == eMenuAction.ContractSelection)
				{
					document.Selection().ContractActive(values[0]);
				}
				else if (adjustment.m_menuAction == eMenuAction.SmoothSelection)
				{
					document.Selection().SmoothActive(values[0]);
				}
				else
				{
					document.Selection().FeatherActive(values[0]);
				}
				canvas.InvalidateSurface();
				return;
			}
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			document.BeginStroke();
			if (adjustment.m_run != null)
			{
				RunClipped(document, adjustment, activeLayer, values);
			}
			document.EndStroke();
			canvas.MarkComposeDirty();
			RecordLastFilter(adjustment, values);
		}

		public void Preview(Adjustment adjustment, int[] values)
		{
			DocumentWindow window = m_main.ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			Document document = window.DocumentModel();
			if (document.StrokeSnapshot() == null)
			{
				return;
			}
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			GpuFilterPreview runner = canvas.FilterPreview();
			if (runner.SessionActive())
			{
				int[] gpuValues = values;
				if (adjustment.m_menuAction == eMenuAction.Emboss)
				{
					double radians = values[0] * (Math.PI / 180.0);
					int offsetX = (int)Math.Round(Math.Cos(radians) * values[1]);
					int offsetY = (int)Math.Round(-Math.Sin(radians) * values[1]);
					gpuValues = new int[] { offsetX, offsetY, values[2] };
				}
				runner.QueuePending(adjustment.m_skslSource, adjustment.m_skslPasses, adjustment.m_builtinBlurPreview, adjustment.m_skslRawSource, gpuValues);
				canvas.InvalidateSurface();
				return;
			}
			document.RestoreStrokeSnapshot();
			if (adjustment.m_run != null)
			{
				RunClipped(document, adjustment, activeLayer, values);
			}
			canvas.MarkComposeDirty();
		}

		public void RestorePreview()
		{
			DocumentWindow window = m_main.ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			canvas.FilterPreview().ClearPending();
			Document document = window.DocumentModel();
			if (document.StrokeSnapshot() == null)
			{
				return;
			}
			document.RestoreStrokeSnapshot();
			canvas.MarkComposeDirty();
		}

		public void Commit(Adjustment adjustment, int[] values)
		{
			DocumentWindow window = m_main.ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			canvas.FilterPreview().EndSession();
			Document document = window.DocumentModel();
			if (document.StrokeSnapshot() == null)
			{
				Apply(adjustment, values);
				m_main.RefreshLayerThumbnails();
				return;
			}
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				document.EndStroke();
				return;
			}
			document.RestoreStrokeSnapshot();
			if (adjustment.m_run != null)
			{
				RunClipped(document, adjustment, activeLayer, values);
			}
			document.EndStroke();
			canvas.MarkComposeDirty();
			m_main.RefreshLayerThumbnails();
			RecordLastFilter(adjustment, values);
		}

		public void Cancel()
		{
			DocumentWindow window = m_main.ActiveWindow();
			if (window == null)
			{
				return;
			}
			CanvasView canvas = window.Canvas();
			canvas.FilterPreview().EndSession();
			Document document = window.DocumentModel();
			if (document.StrokeSnapshot() == null)
			{
				return;
			}
			document.RestoreStrokeSnapshot();
			document.EndStroke();
			canvas.MarkComposeDirty();
		}

		public void ApplyLast()
		{
			if (m_lastFilter == null)
			{
				return;
			}
			RollSeed();
			Apply(m_lastFilter, m_lastFilterValues);
			m_main.RefreshLayerThumbnails();
		}

		public AdjustmentRegistry(MainView main, ToolState toolState)
		{
			m_main = main;
			m_toolState = toolState;
			m_adjustments = new List<Adjustment>();

			Adjustment brightnessContrast = AddDialog(eMenuAction.BrightnessContrast, eMenuAction.AdjustmentsMenu, "Brightness/Contrast", true, new string[] { "Brightness", "Contrast" }, new int[] { -100, -100 }, new int[] { 100, 100 }, new int[] { 0, 0 }, 360.0, 230.0, RunBrightnessContrast);
			brightnessContrast.m_depthAware = true;
			Adjustment hueSaturation = AddDialog(eMenuAction.HueSaturation, eMenuAction.AdjustmentsMenu, "Hue/Saturation", true, new string[] { "Hue", "Saturation", "Lightness" }, new int[] { -180, -100, -100 }, new int[] { 180, 100, 100 }, new int[] { 0, 0, 0 }, 360.0, 260.0, RunHueSaturation);
			hueSaturation.m_depthAware = true;
			Adjustment posterize = AddDialog(eMenuAction.Posterize, eMenuAction.AdjustmentsMenu, "Posterize", true, new string[] { "Levels" }, new int[] { 2 }, new int[] { 64 }, new int[] { 8 }, 360.0, 200.0, RunPosterize);
			posterize.m_depthAware = true;
			Adjustment threshold = AddDialog(eMenuAction.Threshold, eMenuAction.AdjustmentsMenu, "Threshold", true, new string[] { "Level" }, new int[] { 0 }, new int[] { 255 }, new int[] { 128 }, 360.0, 200.0, RunThreshold);
			threshold.m_depthAware = true;

			Adjustment rotate = AddDialog(eMenuAction.RotateArbitrary, eMenuAction.None, "Rotate Arbitrary", false, new string[] { "Angle" }, new int[] { -180 }, new int[] { 180 }, new int[] { 0 }, 360.0, 170.0, null);
			rotate.m_kind = eAdjustmentKind.Canvas;
			Adjustment feather = AddDialog(eMenuAction.FeatherSelection, eMenuAction.None, "Feather Selection", false, new string[] { "Radius" }, new int[] { 1 }, new int[] { 100 }, new int[] { 4 }, 360.0, 170.0, null);
			feather.m_kind = eAdjustmentKind.Selection;
			Adjustment contract = AddDialog(eMenuAction.ContractSelection, eMenuAction.None, "Contract Selection", false, new string[] { "Contract" }, new int[] { 1 }, new int[] { 100 }, new int[] { 1 }, 360.0, 170.0, null);
			contract.m_kind = eAdjustmentKind.Selection;
			Adjustment round = AddDialog(eMenuAction.SmoothSelection, eMenuAction.None, "Round Selection", false, new string[] { "Radius" }, new int[] { 1 }, new int[] { 100 }, new int[] { 2 }, 360.0, 170.0, null);
			round.m_kind = eAdjustmentKind.Selection;

			Adjustment averageBlur = AddInstant(eMenuAction.AverageBlur, eMenuAction.FilterBlurMenu, "Average", RunAverage);
			averageBlur.m_depthAware = true;
			Adjustment blur = AddInstant(eMenuAction.Blur, eMenuAction.FilterBlurMenu, "Blur", RunBlur);
			blur.m_depthAware = true;
			Adjustment blurMore = AddInstant(eMenuAction.BlurMore, eMenuAction.FilterBlurMenu, "Blur More", RunBlurMore);
			blurMore.m_depthAware = true;
			Adjustment boxBlur = AddDialog(eMenuAction.BoxBlur, eMenuAction.FilterBlurMenu, "Box Blur", true, new string[] { "Radius" }, new int[] { 1 }, new int[] { 100 }, new int[] { 10 }, 360.0, 200.0, RunBoxBlur);
			boxBlur.m_skslSource = GpuFilterPreview.BoxBlurSource;
			boxBlur.m_skslPasses = 2;
			boxBlur.m_depthAware = true;
			Adjustment gaussianBlur = AddDialog(eMenuAction.GaussianBlur, eMenuAction.FilterBlurMenu, "Gaussian Blur", true, new string[] { "Radius" }, new int[] { 1 }, new int[] { 30 }, new int[] { 5 }, 360.0, 200.0, RunGaussianBlur);
			gaussianBlur.m_builtinBlurPreview = true;
			gaussianBlur.m_depthAware = true;
			Adjustment motionBlur = AddDialog(eMenuAction.MotionBlur, eMenuAction.FilterBlurMenu, "Motion Blur", true, new string[] { "Angle", "Distance" }, new int[] { -90, 1 }, new int[] { 90, 200 }, new int[] { 0, 10 }, 360.0, 230.0, RunMotionBlur);
			motionBlur.m_skslSource = GpuFilterPreview.MotionBlurSource;
			motionBlur.m_skslPasses = 1;
			Adjustment radialBlur = AddChoiceDialog(eMenuAction.RadialBlur, eMenuAction.FilterBlurMenu, "Radial Blur", true, new string[] { "Amount" }, new int[] { 1 }, new int[] { 100 }, new int[] { 10 }, new string[] { "Method" }, new string[][] { new string[] { "Spin", "Zoom" } }, new int[] { 0 }, 360.0, 230.0, RunRadialBlur);
			radialBlur.m_skslSource = GpuFilterPreview.RadialBlurSource;
			radialBlur.m_skslPasses = 1;

			Adjustment pinch = AddDialog(eMenuAction.Pinch, eMenuAction.FilterDistortMenu, "Pinch", true, new string[] { "Amount" }, new int[] { -100 }, new int[] { 100 }, new int[] { 50 }, 360.0, 200.0, RunPinch);
			pinch.m_skslSource = GpuFilterPreview.PinchSource;
			pinch.m_skslPasses = 1;
			Adjustment polar = AddChoiceDialog(eMenuAction.PolarCoordinates, eMenuAction.FilterDistortMenu, "Polar Coordinates", true, s_noLabels, s_noValues, s_noValues, s_noValues, new string[] { "Direction" }, new string[][] { new string[] { "Rectangular to Polar", "Polar to Rectangular" } }, new int[] { 0 }, 360.0, 200.0, RunPolarCoordinates);
			polar.m_skslSource = GpuFilterPreview.PolarCoordinatesSource;
			polar.m_skslPasses = 1;
			Adjustment ripple = AddChoiceDialog(eMenuAction.Ripple, eMenuAction.FilterDistortMenu, "Ripple", true, new string[] { "Amount" }, new int[] { -999 }, new int[] { 999 }, new int[] { 100 }, new string[] { "Size" }, new string[][] { new string[] { "Small", "Medium", "Large" } }, new int[] { 1 }, 360.0, 230.0, RunRipple);
			ripple.m_skslSource = GpuFilterPreview.RippleSource;
			ripple.m_skslPasses = 1;
			Adjustment shear = AddChoiceDialog(eMenuAction.Shear, eMenuAction.FilterDistortMenu, "Shear", true, new string[] { "Amount" }, new int[] { -100 }, new int[] { 100 }, new int[] { 25 }, new string[] { "Undefined Areas" }, new string[][] { new string[] { "Wrap Around", "Repeat Edge Pixels" } }, new int[] { 0 }, 360.0, 230.0, RunShear);
			shear.m_skslSource = GpuFilterPreview.ShearSource;
			shear.m_skslPasses = 1;
			Adjustment spherize = AddChoiceDialog(eMenuAction.Spherize, eMenuAction.FilterDistortMenu, "Spherize", true, new string[] { "Amount" }, new int[] { -100 }, new int[] { 100 }, new int[] { 50 }, new string[] { "Mode" }, new string[][] { new string[] { "Normal", "Horizontal Only", "Vertical Only" } }, new int[] { 0 }, 360.0, 230.0, RunSpherize);
			spherize.m_skslSource = GpuFilterPreview.SpherizeSource;
			spherize.m_skslPasses = 1;
			Adjustment twirl = AddDialog(eMenuAction.Twirl, eMenuAction.FilterDistortMenu, "Twirl", true, new string[] { "Angle" }, new int[] { -999 }, new int[] { 999 }, new int[] { 50 }, 360.0, 200.0, RunTwirl);
			twirl.m_skslSource = GpuFilterPreview.TwirlSource;
			twirl.m_skslPasses = 1;
			Adjustment wave = AddChoiceDialog(eMenuAction.Wave, eMenuAction.FilterDistortMenu, "Wave", true, new string[] { "Wavelength", "Amplitude" }, new int[] { 1, 1 }, new int[] { 200, 100 }, new int[] { 40, 10 }, new string[] { "Type" }, new string[][] { new string[] { "Sine", "Triangle", "Square" } }, new int[] { 0 }, 360.0, 260.0, RunWave);
			wave.m_skslSource = GpuFilterPreview.WaveSource;
			wave.m_skslPasses = 1;

			AddChoiceDialog(eMenuAction.AddNoise, eMenuAction.FilterNoiseMenu, "Add Noise", true, new string[] { "Amount" }, new int[] { 0 }, new int[] { 100 }, new int[] { 20 }, new string[] { "Type" }, new string[][] { new string[] { "Color", "Monochromatic" } }, new int[] { 0 }, 360.0, 230.0, RunAddNoise);
			AddInstant(eMenuAction.Despeckle, eMenuAction.FilterNoiseMenu, "Despeckle", RunDespeckle);
			AddDialog(eMenuAction.Median, eMenuAction.FilterNoiseMenu, "Median", true, new string[] { "Radius" }, new int[] { 1 }, new int[] { 16 }, new int[] { 3 }, 360.0, 200.0, RunMedian);

			AddDialog(eMenuAction.Crystallize, eMenuAction.FilterPixelateMenu, "Crystallize", true, new string[] { "Cell Size" }, new int[] { 3 }, new int[] { 300 }, new int[] { 10 }, 360.0, 200.0, RunCrystallize);
			AddInstant(eMenuAction.Facet, eMenuAction.FilterPixelateMenu, "Facet", RunFacet);
			AddInstant(eMenuAction.Fragment, eMenuAction.FilterPixelateMenu, "Fragment", RunFragment);
			AddDialog(eMenuAction.Pixelate, eMenuAction.FilterPixelateMenu, "Mosaic", true, new string[] { "Cell Size" }, new int[] { 2 }, new int[] { 64 }, new int[] { 8 }, 360.0, 200.0, RunMosaic);
			AddDialog(eMenuAction.Pointillize, eMenuAction.FilterPixelateMenu, "Pointillize", true, new string[] { "Cell Size" }, new int[] { 3 }, new int[] { 200 }, new int[] { 5 }, 360.0, 200.0, RunPointillize);

			AddInstant(eMenuAction.Clouds, eMenuAction.FilterRenderMenu, "Clouds", RunClouds);
			AddInstant(eMenuAction.DifferenceClouds, eMenuAction.FilterRenderMenu, "Difference Clouds", RunDifferenceClouds);

			Adjustment sharpen = AddInstant(eMenuAction.Sharpen, eMenuAction.FilterSharpenMenu, "Sharpen", RunSharpen);
			sharpen.m_depthAware = true;
			AddInstant(eMenuAction.SharpenEdges, eMenuAction.FilterSharpenMenu, "Sharpen Edges", RunSharpenEdges);
			AddInstant(eMenuAction.SharpenMore, eMenuAction.FilterSharpenMenu, "Sharpen More", RunSharpenMore);
			Adjustment unsharpMask = AddDialog(eMenuAction.UnsharpMask, eMenuAction.FilterSharpenMenu, "Unsharp Mask", true, new string[] { "Amount", "Radius" }, new int[] { 0, 1 }, new int[] { 300, 30 }, new int[] { 100, 3 }, 360.0, 230.0, RunUnsharpMask);
			unsharpMask.m_depthAware = true;
			AddDialog(eMenuAction.HighPass, eMenuAction.FilterOtherMenu, "High Pass", true, new string[] { "Radius" }, new int[] { 1 }, new int[] { 30 }, new int[] { 5 }, 360.0, 200.0, RunHighPass);
			Adjustment offset = AddChoiceDialog(eMenuAction.Offset, eMenuAction.FilterOtherMenu, "Offset", true, new string[] { "Horizontal", "Vertical" }, new int[] { -512, -512 }, new int[] { 512, 512 }, new int[] { 0, 0 }, new string[] { "Undefined Areas" }, new string[][] { new string[] { "Wrap Around", "Repeat Edge Pixels", "Transparent" } }, new int[] { 0 }, 360.0, 240.0, RunOffset);
			offset.m_depthAware = true;

			Adjustment diffuse = AddChoiceDialog(eMenuAction.Diffuse, eMenuAction.FilterStylizeMenu, "Diffuse", true, s_noLabels, s_noValues, s_noValues, s_noValues, new string[] { "Mode" }, new string[][] { new string[] { "Normal", "Darken Only", "Lighten Only" } }, new int[] { 0 }, 360.0, 200.0, RunDiffuse);
			diffuse.m_skslSource = GpuFilterPreview.DiffuseSource;
			diffuse.m_skslPasses = 1;
			diffuse.m_skslRawSource = true;
			Adjustment emboss = AddDialog(eMenuAction.Emboss, eMenuAction.FilterStylizeMenu, "Emboss", true, new string[] { "Angle", "Height", "Amount" }, new int[] { -180, 1, 1 }, new int[] { 180, 10, 500 }, new int[] { 135, 3, 100 }, 360.0, 260.0, RunEmboss);
			emboss.m_skslSource = GpuFilterPreview.EmbossSource;
			emboss.m_skslPasses = 1;
			emboss.m_skslRawSource = true;
			AddInstant(eMenuAction.FindEdges, eMenuAction.FilterStylizeMenu, "Find Edges", RunFindEdges);
			AddInstant(eMenuAction.Solarize, eMenuAction.FilterStylizeMenu, "Solarize", RunSolarize);

			AddChoiceDialog(eMenuAction.DeInterlace, eMenuAction.FilterVideoMenu, "De-Interlace", true, s_noLabels, s_noValues, s_noValues, s_noValues, new string[] { "Eliminate", "Fill" }, new string[][] { new string[] { "Odd Fields", "Even Fields" }, new string[] { "Duplication", "Interpolation" } }, new int[] { 0, 1 }, 360.0, 230.0, RunDeInterlace);

			Adjustment normalMap = AddChoiceDialog(eMenuAction.NormalMap, eMenuAction.FilterGenerateMenu, "Normal Map", true, s_noLabels, s_noValues, s_noValues, s_noValues, new string[] { "Kernel", "Edge" }, new string[][] { new string[] { "Sobel 3x3", "Prewitt 3x3", "5x5", "9x9" }, new string[] { "Wrap", "Clamp" } }, new int[] { 0, 0 }, 360.0, 320.0, RunNormalMap);
			normalMap.m_floatSliderLabels = new string[] { "Strength" };
			normalMap.m_floatSliderMinimums = new float[] { 0.1f };
			normalMap.m_floatSliderMaximums = new float[] { 20.0f };
			normalMap.m_floatSliderDefaults = new float[] { 2.5f };
			normalMap.m_floatSliderDecimals = new int[] { 1 };
			normalMap.m_checkLabels = new string[] { "Invert X (Red)", "Invert Y (Green)" };
			normalMap.m_checkDefaults = new bool[] { false, false };
			normalMap.m_depthAware = true;
		}
	}
}
