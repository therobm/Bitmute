using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Bitmute.Storage;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using SkiaSharp;

namespace Bitmute.UI
{
	public class MainView : ContentPage
	{
		public static MainView Self;

		private static SkiaSharp.SKBitmap s_clipboardBitmap;

		private class ModalEntry
		{
			public View m_content;
			public BoxView m_backdrop;
			public double m_x;
			public double m_y;
			public double m_width;
			public double m_height;
			public double m_dragOriginX;
			public double m_dragOriginY;
		}

		private AbsoluteLayout m_workspace;
		private AbsoluteLayout m_overlay;
		private MenuBar m_menuBar;
		private OptionsBar m_optionsBar;
		private TextEditSession m_textEditSession;
		private List<FloatingPanel> m_documents;
		private DocumentWindow m_activeDocumentWindow;
		private ToolPalette m_toolPalette;
		private LayersPanel m_layersPanel;
		private ChannelsPanel m_channelsPanel;
		private NavigatorPanel m_navigatorPanel;
		private InfoPanel m_infoPanel;
		private SwatchesPanel m_swatchesPanel;
		private PaletteGroup m_navigatorGroup;
		private PaletteGroup m_swatchesGroup;
		private PaletteGroup m_layersGroup;
		private List<PaletteGroup> m_paletteOrder;
		private Grid m_paletteDock;
		private bool m_navigatorPanelVisible = true;
		private bool m_swatchesPanelVisible = true;
		private bool m_layersPanelVisible = true;
		private bool m_infoPanelVisible = true;
		private bool m_rulersEnabled = true;
		private System.Collections.Generic.List<ModalEntry> m_modalStack;
		private FloatingPanel m_pendingClosePanel;
		private bool m_quitPending;
		private bool m_quitConfirmed;
		private bool m_appCloseHooked;
		private int m_appCloseHookAttempts;
		private Microsoft.UI.Xaml.Window m_nativeWindow;
		private View m_pulldownPanel;
		private long m_pulldownDismissTick;
		private Label m_statusInfoLabel;
		private Label m_statusCursorLabel;
		private string[] m_menuTitles;
		private bool m_acceleratorsHooked;
		private int m_untitledCount;
		private int m_cascadeCount;
		private int m_topZIndex;
		private ToolBox m_toolBox;
		private ToolState m_toolState;
		private int m_guideCreateOrientation;
		private CanvasView m_guideCreateCanvas;
		private bool m_gridEnabled;
		private bool m_snapEnabled;
		private bool m_snapTargetGuides;
		private bool m_snapTargetGrid;
		private bool m_snapTargetEdges;
		private bool m_snapTargetLayerBounds;
		private int m_channelViewMode;
		private bool[] m_channelVisible = new bool[] { true, true, true, true };
		private int m_editingSwatchIndex = -1;
		private LayerStyle m_layerStyleSnapshot;
		private int m_layerStyleTargetIndex;
		private LayerStyle m_copiedLayerStyle;
		private static readonly int[] s_instantFilterValues = new int[0];

		private string m_lastFilterId = "";
		private string m_lastFilterName = "";
		private int[] m_lastFilterValues;
		private int m_filterSeed;

		public List<MenuBarItem> GetSubmenuItems(eMenuAction parent)
		{
			List<MenuBarItem> items = new List<MenuBarItem>();
			if (parent == eMenuAction.OpenRecentMenu)
			{
				List<string> recent = RecentFiles.List();
				int recentCount = recent.Count;
				if (recentCount > 12)
				{
					recentCount = 12;
				}
				for (int index = 0; index < recentCount; index++)
				{
					MenuBarItem recentItem = new MenuBarItem(System.IO.Path.GetFileName(recent[index]), eMenuAction.OpenRecent);
					recentItem.m_argument = recent[index];
					items.Add(recentItem);
				}
				return items;
			}
			if (parent == eMenuAction.TransformMenu)
			{
				items.Add(new MenuBarItem("Free Transform", eMenuAction.FreeTransform, "Ctrl+T"));
				items.Add(new MenuBarItem("Scale", eMenuAction.TransformScale));
				items.Add(new MenuBarItem("Rotate", eMenuAction.TransformRotate));
				items.Add(new MenuBarItem("Skew", eMenuAction.TransformSkew));
				items.Add(new MenuBarItem("Distort", eMenuAction.TransformDistort));
				items.Add(new MenuBarItem("Perspective", eMenuAction.TransformPerspective));
				items.Add(new MenuBarItem("Flip Horizontal (Layer)", eMenuAction.FlipLayerHorizontal));
				items.Add(new MenuBarItem("Flip Vertical (Layer)", eMenuAction.FlipLayerVertical));
				return items;
			}
			if (parent == eMenuAction.SnapToMenu)
			{
				MenuBarItem snapGuides = new MenuBarItem("Snap Guides", eMenuAction.ToggleSnapGuides);
				snapGuides.m_checked = m_snapTargetGuides;
				items.Add(snapGuides);
				MenuBarItem snapGrid = new MenuBarItem("Snap Grid", eMenuAction.ToggleSnapGrid);
				snapGrid.m_checked = m_snapTargetGrid;
				items.Add(snapGrid);
				MenuBarItem snapEdges = new MenuBarItem("Snap Edges", eMenuAction.ToggleSnapEdges);
				snapEdges.m_checked = m_snapTargetEdges;
				items.Add(snapEdges);
				MenuBarItem snapLayers = new MenuBarItem("Snap Layers", eMenuAction.ToggleSnapLayers);
				snapLayers.m_checked = m_snapTargetLayerBounds;
				items.Add(snapLayers);
				return items;
			}
			if (parent == eMenuAction.AdjustmentsMenu)
			{
				items.Add(new MenuBarItem("Brightness/Contrast…", eMenuAction.BrightnessContrast));
				items.Add(new MenuBarItem("Hue/Saturation…", eMenuAction.HueSaturation));
				MenuBarItem adjustSeparatorOne = new MenuBarItem("", eMenuAction.None);
				adjustSeparatorOne.m_separator = true;
				items.Add(adjustSeparatorOne);
				items.Add(new MenuBarItem("Desaturate", eMenuAction.Desaturate));
				items.Add(new MenuBarItem("Invert Colors", eMenuAction.InvertColors, "Ctrl+I"));
				MenuBarItem adjustSeparatorTwo = new MenuBarItem("", eMenuAction.None);
				adjustSeparatorTwo.m_separator = true;
				items.Add(adjustSeparatorTwo);
				items.Add(new MenuBarItem("Posterize…", eMenuAction.Posterize));
				items.Add(new MenuBarItem("Threshold…", eMenuAction.Threshold));
				return items;
			}
			if (parent == eMenuAction.FilterBlurMenu)
			{
				items.Add(new MenuBarItem("Average", eMenuAction.AverageBlur));
				items.Add(new MenuBarItem("Blur", eMenuAction.Blur));
				items.Add(new MenuBarItem("Blur More", eMenuAction.BlurMore));
				items.Add(new MenuBarItem("Box Blur…", eMenuAction.BoxBlur));
				items.Add(new MenuBarItem("Gaussian Blur…", eMenuAction.GaussianBlur));
				items.Add(new MenuBarItem("Motion Blur…", eMenuAction.MotionBlur));
				items.Add(new MenuBarItem("Radial Blur…", eMenuAction.RadialBlur));
				return items;
			}
			if (parent == eMenuAction.FilterSharpenMenu)
			{
				items.Add(new MenuBarItem("Sharpen", eMenuAction.Sharpen));
				items.Add(new MenuBarItem("Sharpen Edges", eMenuAction.SharpenEdges));
				items.Add(new MenuBarItem("Sharpen More", eMenuAction.SharpenMore));
				items.Add(new MenuBarItem("Unsharp Mask…", eMenuAction.UnsharpMask));
				return items;
			}
			if (parent == eMenuAction.FilterNoiseMenu)
			{
				items.Add(new MenuBarItem("Add Noise…", eMenuAction.AddNoise));
				items.Add(new MenuBarItem("Despeckle", eMenuAction.Despeckle));
				items.Add(new MenuBarItem("Median…", eMenuAction.Median));
				return items;
			}
			if (parent == eMenuAction.FilterPixelateMenu)
			{
				items.Add(new MenuBarItem("Crystallize…", eMenuAction.Crystallize));
				items.Add(new MenuBarItem("Facet", eMenuAction.Facet));
				items.Add(new MenuBarItem("Fragment", eMenuAction.Fragment));
				items.Add(new MenuBarItem("Mosaic…", eMenuAction.Pixelate));
				items.Add(new MenuBarItem("Pointillize…", eMenuAction.Pointillize));
				return items;
			}
			if (parent == eMenuAction.FilterRenderMenu)
			{
				items.Add(new MenuBarItem("Clouds", eMenuAction.Clouds));
				items.Add(new MenuBarItem("Difference Clouds", eMenuAction.DifferenceClouds));
				return items;
			}
			if (parent == eMenuAction.FilterStylizeMenu)
			{
				items.Add(new MenuBarItem("Diffuse…", eMenuAction.Diffuse));
				items.Add(new MenuBarItem("Emboss…", eMenuAction.Emboss));
				items.Add(new MenuBarItem("Find Edges", eMenuAction.FindEdges));
				items.Add(new MenuBarItem("Solarize", eMenuAction.Solarize));
				return items;
			}
			if (parent == eMenuAction.FilterDistortMenu || parent == eMenuAction.FilterVideoMenu)
			{
				MenuBarItem placeholder = new MenuBarItem("(none yet)", eMenuAction.None);
				placeholder.m_enabled = false;
				items.Add(placeholder);
				return items;
			}
			return items;
		}

		private bool GuidesLocked()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return false;
			}
			return document.Guides().IsLocked();
		}

		public List<MenuBarItem> GetMenuItems(string title)
		{
			List<MenuBarItem> items = new List<MenuBarItem>();
			if (title == "File")
			{
				items.Add(new MenuBarItem("New", eMenuAction.NewDocument, "Ctrl+N"));
				items.Add(new MenuBarItem("Open…", eMenuAction.OpenFile, "Ctrl+O"));
				items.Add(new MenuBarItem("Save", eMenuAction.Save, "Ctrl+S"));
				items.Add(new MenuBarItem("Save As…", eMenuAction.SaveAs, "Ctrl+Shift+S"));
				items.Add(new MenuBarItem("Export As…", eMenuAction.ExportAs, "Ctrl+Alt+Shift+S"));
				if (RecentFiles.List().Count > 0)
				{
					MenuBarItem openRecent = new MenuBarItem("Open Recent", eMenuAction.OpenRecentMenu);
					openRecent.m_submenu = true;
					items.Add(openRecent);
				}
				items.Add(new MenuBarItem("Exit", eMenuAction.Exit));
				return items;
			}
			if (title == "Edit")
			{
				items.Add(new MenuBarItem("Undo", eMenuAction.Undo, "Ctrl+Z"));
				items.Add(new MenuBarItem("Redo", eMenuAction.Redo, "Ctrl+Y"));
				items.Add(new MenuBarItem("Cut", eMenuAction.Cut, "Ctrl+X"));
				items.Add(new MenuBarItem("Copy", eMenuAction.Copy, "Ctrl+C"));
				items.Add(new MenuBarItem("Paste", eMenuAction.Paste, "Ctrl+V"));
				MenuBarItem transform = new MenuBarItem("Transform", eMenuAction.TransformMenu);
				transform.m_submenu = true;
				items.Add(transform);
				items.Add(new MenuBarItem("Stroke…", eMenuAction.StrokeDialog));
				items.Add(new MenuBarItem("Preferences…", eMenuAction.Preferences));
				return items;
			}
			if (title == "Image")
			{
				MenuBarItem adjustments = new MenuBarItem("Adjustments", eMenuAction.AdjustmentsMenu);
				adjustments.m_submenu = true;
				items.Add(adjustments);
				MenuBarItem imageSeparator = new MenuBarItem("", eMenuAction.None);
				imageSeparator.m_separator = true;
				items.Add(imageSeparator);
				items.Add(new MenuBarItem("Image Size…", eMenuAction.ImageSize, "Ctrl+Alt+I"));
				items.Add(new MenuBarItem("Canvas Size…", eMenuAction.CanvasSize));
				items.Add(new MenuBarItem("Flip Horizontal", eMenuAction.FlipHorizontal));
				items.Add(new MenuBarItem("Flip Vertical", eMenuAction.FlipVertical));
				items.Add(new MenuBarItem("Rotate 90° CW", eMenuAction.Rotate90Clockwise));
				items.Add(new MenuBarItem("Rotate 180°", eMenuAction.Rotate180));
				items.Add(new MenuBarItem("Rotate 90° CCW", eMenuAction.Rotate90CounterClockwise));
				items.Add(new MenuBarItem("Rotate Arbitrary…", eMenuAction.RotateArbitrary));
				items.Add(new MenuBarItem("Crop to Selection", eMenuAction.CropToSelection));
				items.Add(new MenuBarItem("Trim", eMenuAction.Trim));
				return items;
			}
			if (title == "Layer")
			{
				Document layerDocument = ActiveDocument();
				bool hasDocument = layerDocument != null;
				bool hasActiveLayer = false;
				if (hasDocument)
				{
					hasActiveLayer = layerDocument.ActiveLayer() != null;
				}
				MenuBarItem newLayer = new MenuBarItem("New Layer", eMenuAction.NewLayer);
				newLayer.m_enabled = hasDocument;
				items.Add(newLayer);
				MenuBarItem deleteLayer = new MenuBarItem("Delete Layer", eMenuAction.DeleteLayer);
				deleteLayer.m_enabled = hasDocument;
				items.Add(deleteLayer);
				MenuBarItem layerSeparatorOne = new MenuBarItem("", eMenuAction.None);
				layerSeparatorOne.m_separator = true;
				items.Add(layerSeparatorOne);
				MenuBarItem mergeDown = new MenuBarItem("Merge Down", eMenuAction.MergeDown, "Ctrl+E");
				mergeDown.m_enabled = CanMergeDown();
				items.Add(mergeDown);
				items.Add(new MenuBarItem("Merge Visible", eMenuAction.MergeVisible, "Ctrl+Shift+E"));
				items.Add(new MenuBarItem("Flatten Image", eMenuAction.FlattenImage));
				MenuBarItem layerSeparatorTwo = new MenuBarItem("", eMenuAction.None);
				layerSeparatorTwo.m_separator = true;
				items.Add(layerSeparatorTwo);
				MenuBarItem layerStyle = new MenuBarItem("Layer Style…", eMenuAction.LayerStyle);
				layerStyle.m_enabled = hasActiveLayer;
				items.Add(layerStyle);
				MenuBarItem layerProperties = new MenuBarItem("Layer Properties…", eMenuAction.LayerProperties);
				layerProperties.m_enabled = hasActiveLayer;
				items.Add(layerProperties);
				MenuBarItem rasterizeText = new MenuBarItem("Rasterize Text", eMenuAction.RasterizeText);
				rasterizeText.m_enabled = ActiveLayerIsText();
				items.Add(rasterizeText);
				return items;
			}
			if (title == "Select")
			{
				items.Add(new MenuBarItem("All", eMenuAction.SelectAll, "Ctrl+A"));
				items.Add(new MenuBarItem("Deselect", eMenuAction.Deselect, "Ctrl+D"));
				items.Add(new MenuBarItem("Invert", eMenuAction.InvertSelection, "Ctrl+Shift+I"));
				MenuBarItem feather = new MenuBarItem("Feather…", eMenuAction.FeatherSelection);
				Document selectDocument = ActiveDocument();
				feather.m_enabled = selectDocument != null && selectDocument.Selection().IsActive();
				items.Add(feather);
				return items;
			}
			if (title == "Filter")
			{
				string lastFilterLabel = "Last Filter";
				if (m_lastFilterName.Length > 0)
				{
					lastFilterLabel = "Last Filter: " + m_lastFilterName;
				}
				MenuBarItem lastFilter = new MenuBarItem(lastFilterLabel, eMenuAction.LastFilter, "Ctrl+F");
				lastFilter.m_enabled = m_lastFilterId.Length > 0;
				items.Add(lastFilter);
				MenuBarItem filterSeparator = new MenuBarItem("", eMenuAction.None);
				filterSeparator.m_separator = true;
				items.Add(filterSeparator);
				MenuBarItem blur = new MenuBarItem("Blur", eMenuAction.FilterBlurMenu);
				blur.m_submenu = true;
				items.Add(blur);
				MenuBarItem distort = new MenuBarItem("Distort", eMenuAction.FilterDistortMenu);
				distort.m_submenu = true;
				items.Add(distort);
				MenuBarItem noise = new MenuBarItem("Noise", eMenuAction.FilterNoiseMenu);
				noise.m_submenu = true;
				items.Add(noise);
				MenuBarItem pixelate = new MenuBarItem("Pixelate", eMenuAction.FilterPixelateMenu);
				pixelate.m_submenu = true;
				items.Add(pixelate);
				MenuBarItem render = new MenuBarItem("Render", eMenuAction.FilterRenderMenu);
				render.m_submenu = true;
				items.Add(render);
				MenuBarItem sharpen = new MenuBarItem("Sharpen", eMenuAction.FilterSharpenMenu);
				sharpen.m_submenu = true;
				items.Add(sharpen);
				MenuBarItem stylize = new MenuBarItem("Stylize", eMenuAction.FilterStylizeMenu);
				stylize.m_submenu = true;
				items.Add(stylize);
				MenuBarItem video = new MenuBarItem("Video", eMenuAction.FilterVideoMenu);
				video.m_submenu = true;
				items.Add(video);
				return items;
			}
			if (title == "View")
			{
				items.Add(new MenuBarItem("Zoom In", eMenuAction.ZoomIn, "Ctrl++"));
				items.Add(new MenuBarItem("Zoom Out", eMenuAction.ZoomOut, "Ctrl+-"));
				items.Add(new MenuBarItem("Fit on Screen", eMenuAction.FitOnScreen, "Ctrl+0"));
				MenuBarItem rulers = new MenuBarItem("Rulers", eMenuAction.ToggleRulers, "Ctrl+R");
				rulers.m_checked = m_rulersEnabled;
				items.Add(rulers);
				MenuBarItem grid = new MenuBarItem("Grid", eMenuAction.ToggleGrid);
				grid.m_checked = m_gridEnabled;
				items.Add(grid);
				MenuBarItem snap = new MenuBarItem("Snap", eMenuAction.ToggleSnap);
				snap.m_checked = m_snapEnabled;
				items.Add(snap);
				MenuBarItem snapTo = new MenuBarItem("Snap To", eMenuAction.SnapToMenu);
				snapTo.m_submenu = true;
				items.Add(snapTo);
				MenuBarItem lockGuides = new MenuBarItem("Lock Guides", eMenuAction.ToggleLockGuides);
				lockGuides.m_checked = GuidesLocked();
				items.Add(lockGuides);
				items.Add(new MenuBarItem("Clear Guides", eMenuAction.ClearGuides));
				return items;
			}
			if (title == "Window")
			{
				items.Add(new MenuBarItem("Cascade", eMenuAction.CascadeWindows));
				items.Add(new MenuBarItem("Tile", eMenuAction.TileWindows));
				MenuBarItem navigator = new MenuBarItem("Navigator", eMenuAction.ToggleNavigatorPanel);
				navigator.m_checked = m_navigatorPanelVisible;
				items.Add(navigator);
				MenuBarItem swatches = new MenuBarItem("Swatches", eMenuAction.ToggleSwatchesPanel);
				swatches.m_checked = m_swatchesPanelVisible;
				items.Add(swatches);
				MenuBarItem layersPanel = new MenuBarItem("Layers", eMenuAction.ToggleLayersPanel);
				layersPanel.m_checked = m_layersPanelVisible;
				items.Add(layersPanel);
				return items;
			}
			items.Add(new MenuBarItem("About Bitmute", eMenuAction.AboutBitmute));
			return items;
		}

		private bool CanMergeDown()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return false;
			}
			return document.ActiveLayerIndex() > 0;
		}

		private bool ActiveLayerIsText()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return false;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			return layer.IsText();
		}

		public void InvokeMenuAction(MenuBarItem item)
		{
			if (item == null)
			{
				return;
			}
			eMenuAction action = item.m_action;
			if (action == eMenuAction.NewDocument)
			{
				ShowNewDocumentDialog();
				return;
			}
			if (action == eMenuAction.Undo)
			{
				DoUndo();
				return;
			}
			if (action == eMenuAction.Redo)
			{
				DoRedo();
				return;
			}
			if (action == eMenuAction.Cut)
			{
				DoCut();
				return;
			}
			if (action == eMenuAction.Copy)
			{
				DoCopy();
				return;
			}
			if (action == eMenuAction.Paste)
			{
				DoPaste();
				return;
			}
			if (action == eMenuAction.OpenFile)
			{
				OpenImageFlow();
				return;
			}
			if (action == eMenuAction.Save)
			{
				SaveImageFlow();
				return;
			}
			if (action == eMenuAction.SaveAs)
			{
				SaveAsFlow();
				return;
			}
			if (action == eMenuAction.OpenRecent)
			{
				OpenRecentFile(item.m_argument);
				return;
			}
			if (action == eMenuAction.Exit)
			{
				DoExit();
				return;
			}
			if (action == eMenuAction.ZoomIn)
			{
				DoZoomIn();
				return;
			}
			if (action == eMenuAction.ZoomOut)
			{
				DoZoomOut();
				return;
			}
			if (action == eMenuAction.FitOnScreen)
			{
				DoFit();
				return;
			}
			if (action == eMenuAction.ToggleRulers)
			{
				ToggleRulers();
				return;
			}
			if (action == eMenuAction.ToggleGrid)
			{
				ToggleGrid();
				return;
			}
			if (action == eMenuAction.ToggleSnap)
			{
				m_snapEnabled = !m_snapEnabled;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_enabled", m_snapEnabled);
				return;
			}
			if (action == eMenuAction.ToggleSnapGuides)
			{
				m_snapTargetGuides = !m_snapTargetGuides;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_target_guides", m_snapTargetGuides);
				return;
			}
			if (action == eMenuAction.ToggleSnapGrid)
			{
				m_snapTargetGrid = !m_snapTargetGrid;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_target_grid", m_snapTargetGrid);
				return;
			}
			if (action == eMenuAction.ToggleSnapEdges)
			{
				m_snapTargetEdges = !m_snapTargetEdges;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_target_edges", m_snapTargetEdges);
				return;
			}
			if (action == eMenuAction.ToggleSnapLayers)
			{
				m_snapTargetLayerBounds = !m_snapTargetLayerBounds;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_target_layer_bounds", m_snapTargetLayerBounds);
				return;
			}
			if (action == eMenuAction.ToggleLockGuides)
			{
				Document guideDoc = ActiveDocument();
				if (guideDoc != null)
				{
					guideDoc.Guides().SetLocked(!guideDoc.Guides().IsLocked());
				}
				return;
			}
			if (action == eMenuAction.ClearGuides)
			{
				Document clearDoc = ActiveDocument();
				if (clearDoc != null)
				{
					clearDoc.Guides().Clear();
					CanvasView guideCanvas = ActiveCanvas();
					if (guideCanvas != null)
					{
						guideCanvas.InvalidateSurface();
					}
				}
				return;
			}
			if (action == eMenuAction.FreeTransform)
			{
				BeginTransform(0);
				return;
			}
			if (action == eMenuAction.TransformScale)
			{
				BeginTransform(1);
				return;
			}
			if (action == eMenuAction.TransformRotate)
			{
				BeginTransform(2);
				return;
			}
			if (action == eMenuAction.TransformSkew)
			{
				BeginTransform(3);
				return;
			}
			if (action == eMenuAction.TransformDistort)
			{
				BeginTransform(4);
				return;
			}
			if (action == eMenuAction.TransformPerspective)
			{
				BeginTransform(5);
				return;
			}
			if (action == eMenuAction.FlipLayerHorizontal)
			{
				BeginTransform(6);
				return;
			}
			if (action == eMenuAction.FlipLayerVertical)
			{
				BeginTransform(7);
				return;
			}
			if (action == eMenuAction.RotateArbitrary)
			{
				OpenAdjustment("rotate");
				return;
			}
			if (action == eMenuAction.StrokeDialog)
			{
				OpenStrokeDialog();
				return;
			}
			if (action == eMenuAction.SelectAll)
			{
				DoSelectAll();
				return;
			}
			if (action == eMenuAction.Deselect)
			{
				DoDeselect();
				return;
			}
			if (action == eMenuAction.InvertSelection)
			{
				DoInvertSelection();
				return;
			}
			if (action == eMenuAction.FeatherSelection)
			{
				OpenAdjustment("feather");
				return;
			}
			if (action == eMenuAction.InvertColors)
			{
				DoInvert();
				return;
			}
			if (action == eMenuAction.Desaturate)
			{
				DoDesaturate();
				return;
			}
			if (action == eMenuAction.BrightnessContrast)
			{
				OpenAdjustment("bc");
				return;
			}
			if (action == eMenuAction.HueSaturation)
			{
				OpenAdjustment("hsl");
				return;
			}
			if (action == eMenuAction.Posterize)
			{
				OpenAdjustment("posterize");
				return;
			}
			if (action == eMenuAction.Threshold)
			{
				OpenAdjustment("threshold");
				return;
			}
			if (action == eMenuAction.GaussianBlur)
			{
				OpenAdjustment("gblur");
				return;
			}
			if (action == eMenuAction.UnsharpMask)
			{
				OpenAdjustment("unsharp");
				return;
			}
			if (action == eMenuAction.AddNoise)
			{
				OpenAdjustment("noise");
				return;
			}
			if (action == eMenuAction.Pixelate)
			{
				OpenAdjustment("pixelate");
				return;
			}
			if (action == eMenuAction.LastFilter)
			{
				ApplyLastFilter();
				return;
			}
			if (action == eMenuAction.Clouds)
			{
				RunInstantFilter("clouds");
				return;
			}
			if (action == eMenuAction.DifferenceClouds)
			{
				RunInstantFilter("diffclouds");
				return;
			}
			if (action == eMenuAction.AverageBlur)
			{
				RunInstantFilter("average");
				return;
			}
			if (action == eMenuAction.Blur)
			{
				RunInstantFilter("blur");
				return;
			}
			if (action == eMenuAction.BlurMore)
			{
				RunInstantFilter("blurmore");
				return;
			}
			if (action == eMenuAction.BoxBlur)
			{
				OpenAdjustment("boxblur");
				return;
			}
			if (action == eMenuAction.MotionBlur)
			{
				OpenAdjustment("motionblur");
				return;
			}
			if (action == eMenuAction.RadialBlur)
			{
				OpenAdjustment("radialblur");
				return;
			}
			if (action == eMenuAction.Despeckle)
			{
				RunInstantFilter("despeckle");
				return;
			}
			if (action == eMenuAction.Median)
			{
				OpenAdjustment("median");
				return;
			}
			if (action == eMenuAction.Crystallize)
			{
				OpenAdjustment("crystallize");
				return;
			}
			if (action == eMenuAction.Facet)
			{
				RunInstantFilter("facet");
				return;
			}
			if (action == eMenuAction.Fragment)
			{
				RunInstantFilter("fragment");
				return;
			}
			if (action == eMenuAction.Pointillize)
			{
				OpenAdjustment("pointillize");
				return;
			}
			if (action == eMenuAction.Sharpen)
			{
				RunInstantFilter("sharpen");
				return;
			}
			if (action == eMenuAction.SharpenEdges)
			{
				RunInstantFilter("sharpenedges");
				return;
			}
			if (action == eMenuAction.SharpenMore)
			{
				RunInstantFilter("sharpenmore");
				return;
			}
			if (action == eMenuAction.Diffuse)
			{
				OpenAdjustment("diffuse");
				return;
			}
			if (action == eMenuAction.Emboss)
			{
				OpenAdjustment("emboss");
				return;
			}
			if (action == eMenuAction.FindEdges)
			{
				RunInstantFilter("findedges");
				return;
			}
			if (action == eMenuAction.Solarize)
			{
				RunInstantFilter("solarize");
				return;
			}
			if (action == eMenuAction.FlipHorizontal)
			{
				DoCanvasOp("fliph");
				return;
			}
			if (action == eMenuAction.FlipVertical)
			{
				DoCanvasOp("flipv");
				return;
			}
			if (action == eMenuAction.Rotate90Clockwise)
			{
				DoCanvasOp("rot90");
				return;
			}
			if (action == eMenuAction.Rotate180)
			{
				DoCanvasOp("rot180");
				return;
			}
			if (action == eMenuAction.Rotate90CounterClockwise)
			{
				DoCanvasOp("rot270");
				return;
			}
			if (action == eMenuAction.CropToSelection)
			{
				DoCanvasOp("crop");
				return;
			}
			if (action == eMenuAction.Trim)
			{
				DoCanvasOp("trim");
				return;
			}
			if (action == eMenuAction.CascadeWindows)
			{
				DoCascadeWindows();
				return;
			}
			if (action == eMenuAction.TileWindows)
			{
				DoTileWindows();
				return;
			}
			if (action == eMenuAction.ToggleNavigatorPanel)
			{
				ToggleDockPanel("Navigator");
				return;
			}
			if (action == eMenuAction.ToggleSwatchesPanel)
			{
				ToggleDockPanel("Swatches");
				return;
			}
			if (action == eMenuAction.ToggleLayersPanel)
			{
				ToggleDockPanel("Layers");
				return;
			}
			if (action == eMenuAction.NewLayer)
			{
				AddNewLayer();
				return;
			}
			if (action == eMenuAction.DeleteLayer)
			{
				RequestDeleteActiveLayer();
				return;
			}
			if (action == eMenuAction.RasterizeText)
			{
				DoRasterizeText();
				return;
			}
			if (action == eMenuAction.LayerStyle)
			{
				OpenLayerStyleDialog();
				return;
			}
			if (action == eMenuAction.LayerProperties)
			{
				OpenLayerPropertiesDialog();
				return;
			}
			if (action == eMenuAction.MergeDown)
			{
				DoMergeDown();
				return;
			}
			if (action == eMenuAction.MergeVisible)
			{
				DoMergeVisible();
				return;
			}
			if (action == eMenuAction.FlattenImage)
			{
				DoFlattenImage();
				return;
			}
			if (action == eMenuAction.ExportAs)
			{
				OpenExportDialog();
				return;
			}
			if (action == eMenuAction.Preferences)
			{
				ShowModal(new PreferencesDialog(), 340.0, 260.0);
				return;
			}
			if (action == eMenuAction.AboutBitmute)
			{
				ShowModal(new AboutDialog(), 380.0, 300.0);
				return;
			}
			if (action == eMenuAction.CanvasSize)
			{
				OpenSizeDialog(true);
				return;
			}
			if (action == eMenuAction.ImageSize)
			{
				OpenSizeDialog(false);
			}
		}

		private void OpenSizeDialog(bool canvasMode)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			string title = "Image Size";
			if (canvasMode)
			{
				title = "Canvas Size";
			}
			ShowModal(new SizeDialog(title, canvasMode, document.Width(), document.Height()), 340.0, 260.0);
		}

		private void FinishCanvasOp(CanvasView canvas, Document document)
		{
			document.ResetSelection();
			canvas.ResetView();
			canvas.MarkComposeDirty();
			RefreshLayerThumbnails();
		}

		private void DoCanvasOp(string op)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.BeginCanvasEdit(CanvasOpLabel(op));
			if (op == "fliph")
			{
				document.FlipHorizontal();
			}
			else if (op == "flipv")
			{
				document.FlipVertical();
			}
			else if (op == "rot90")
			{
				document.Rotate90();
			}
			else if (op == "rot180")
			{
				document.Rotate180();
			}
			else if (op == "rot270")
			{
				document.Rotate270();
			}
			else if (op == "crop")
			{
				document.CropToSelection();
			}
			else if (op == "trim")
			{
				document.Trim();
			}
			document.EndCanvasEdit();
			FinishCanvasOp(canvas, document);
		}

		private static string CanvasOpLabel(string op)
		{
			if (op == "fliph")
			{
				return "Flip Horizontal";
			}
			if (op == "flipv")
			{
				return "Flip Vertical";
			}
			if (op == "rot90")
			{
				return "Rotate 90 CW";
			}
			if (op == "rot180")
			{
				return "Rotate 180";
			}
			if (op == "rot270")
			{
				return "Rotate 90 CCW";
			}
			if (op == "crop")
			{
				return "Crop";
			}
			if (op == "trim")
			{
				return "Trim";
			}
			return "Canvas Edit";
		}

		public void ApplyCanvasSize(int width, int height, int anchorX, int anchorY)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.BeginCanvasEdit("Canvas Size");
			document.ResizeCanvas(width, height, anchorX, anchorY);
			document.EndCanvasEdit();
			FinishCanvasOp(canvas, document);
		}

		public void ApplyImageSize(int width, int height, int interpolation)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.BeginCanvasEdit("Image Size");
			document.ScaleImage(width, height, interpolation);
			document.EndCanvasEdit();
			FinishCanvasOp(canvas, document);
		}

		public static bool IsAdjustmentPreviewable(string id)
		{
			if (id == "bc")
			{
				return true;
			}
			if (id == "hsl")
			{
				return true;
			}
			if (id == "posterize")
			{
				return true;
			}
			if (id == "threshold")
			{
				return true;
			}
			if (id == "gblur")
			{
				return true;
			}
			if (id == "unsharp")
			{
				return true;
			}
			if (id == "noise")
			{
				return true;
			}
			if (id == "pixelate")
			{
				return true;
			}
			if (id == "boxblur")
			{
				return true;
			}
			if (id == "motionblur")
			{
				return true;
			}
			if (id == "radialblur")
			{
				return true;
			}
			if (id == "median")
			{
				return true;
			}
			if (id == "crystallize")
			{
				return true;
			}
			if (id == "pointillize")
			{
				return true;
			}
			if (id == "diffuse")
			{
				return true;
			}
			if (id == "emboss")
			{
				return true;
			}
			return false;
		}

		private static string FilterMenuName(string id)
		{
			if (id == "gblur")
			{
				return "Gaussian Blur";
			}
			if (id == "unsharp")
			{
				return "Unsharp Mask";
			}
			if (id == "noise")
			{
				return "Add Noise";
			}
			if (id == "pixelate")
			{
				return "Mosaic";
			}
			if (id == "clouds")
			{
				return "Clouds";
			}
			if (id == "diffclouds")
			{
				return "Difference Clouds";
			}
			if (id == "average")
			{
				return "Average";
			}
			if (id == "blur")
			{
				return "Blur";
			}
			if (id == "blurmore")
			{
				return "Blur More";
			}
			if (id == "boxblur")
			{
				return "Box Blur";
			}
			if (id == "motionblur")
			{
				return "Motion Blur";
			}
			if (id == "radialblur")
			{
				return "Radial Blur";
			}
			if (id == "despeckle")
			{
				return "Despeckle";
			}
			if (id == "median")
			{
				return "Median";
			}
			if (id == "crystallize")
			{
				return "Crystallize";
			}
			if (id == "facet")
			{
				return "Facet";
			}
			if (id == "fragment")
			{
				return "Fragment";
			}
			if (id == "pointillize")
			{
				return "Pointillize";
			}
			if (id == "sharpen")
			{
				return "Sharpen";
			}
			if (id == "sharpenedges")
			{
				return "Sharpen Edges";
			}
			if (id == "sharpenmore")
			{
				return "Sharpen More";
			}
			if (id == "diffuse")
			{
				return "Diffuse";
			}
			if (id == "emboss")
			{
				return "Emboss";
			}
			if (id == "findedges")
			{
				return "Find Edges";
			}
			if (id == "solarize")
			{
				return "Solarize";
			}
			return "";
		}

		private void RecordLastFilter(string id, int[] values)
		{
			string filterName = FilterMenuName(id);
			if (filterName.Length == 0)
			{
				return;
			}
			m_lastFilterId = id;
			m_lastFilterName = filterName;
			m_lastFilterValues = values;
		}

		private void RollFilterSeed()
		{
			m_filterSeed = Environment.TickCount;
		}

		private void RunInstantFilter(string id)
		{
			RollFilterSeed();
			ApplyAdjustment(id, s_instantFilterValues);
			RefreshLayerThumbnails();
		}

		private void ApplyLastFilter()
		{
			if (m_lastFilterId.Length == 0)
			{
				return;
			}
			RollFilterSeed();
			ApplyAdjustment(m_lastFilterId, m_lastFilterValues);
			RefreshLayerThumbnails();
		}

		private void RunAdjustmentMath(string id, SkiaSharp.SKBitmap bitmap, int[] values)
		{
			if (id == "bc")
			{
				Adjustments.BrightnessContrast(bitmap, values[0], values[1]);
			}
			else if (id == "hsl")
			{
				Adjustments.HueSaturationLightness(bitmap, values[0], values[1], values[2]);
			}
			else if (id == "posterize")
			{
				Adjustments.Posterize(bitmap, values[0]);
			}
			else if (id == "threshold")
			{
				Adjustments.Threshold(bitmap, values[0]);
			}
			else if (id == "gblur")
			{
				Adjustments.GaussianBlur(bitmap, values[0]);
			}
			else if (id == "unsharp")
			{
				Adjustments.UnsharpMask(bitmap, values[0], values[1]);
			}
			else if (id == "noise")
			{
				Adjustments.AddNoise(bitmap, values[0], false);
			}
			else if (id == "pixelate")
			{
				Adjustments.Pixelate(bitmap, values[0]);
			}
			else if (id == "clouds")
			{
				FilterRender.Clouds(bitmap, m_toolState.Foreground(), m_toolState.Background(), m_filterSeed);
			}
			else if (id == "diffclouds")
			{
				FilterRender.DifferenceClouds(bitmap, m_toolState.Foreground(), m_toolState.Background(), m_filterSeed);
			}
			else if (id == "average")
			{
				FilterBlur.Average(bitmap);
			}
			else if (id == "blur")
			{
				FilterBlur.Blur(bitmap);
			}
			else if (id == "blurmore")
			{
				FilterBlur.BlurMore(bitmap);
			}
			else if (id == "boxblur")
			{
				FilterBlur.BoxBlur(bitmap, values[0]);
			}
			else if (id == "motionblur")
			{
				FilterBlur.MotionBlur(bitmap, values[0], values[1]);
			}
			else if (id == "radialblur")
			{
				FilterBlur.RadialBlur(bitmap, values[0], values[1]);
			}
			else if (id == "despeckle")
			{
				FilterNoise.Despeckle(bitmap);
			}
			else if (id == "median")
			{
				FilterNoise.Median(bitmap, values[0]);
			}
			else if (id == "crystallize")
			{
				FilterPixelate.Crystallize(bitmap, values[0], m_filterSeed);
			}
			else if (id == "facet")
			{
				FilterPixelate.Facet(bitmap);
			}
			else if (id == "fragment")
			{
				FilterPixelate.Fragment(bitmap);
			}
			else if (id == "pointillize")
			{
				FilterPixelate.Pointillize(bitmap, values[0], m_filterSeed, m_toolState.Background());
			}
			else if (id == "sharpen")
			{
				FilterSharpen.Sharpen(bitmap);
			}
			else if (id == "sharpenedges")
			{
				FilterSharpen.SharpenEdges(bitmap);
			}
			else if (id == "sharpenmore")
			{
				FilterSharpen.SharpenMore(bitmap);
			}
			else if (id == "diffuse")
			{
				FilterStylize.Diffuse(bitmap, values[0], m_filterSeed);
			}
			else if (id == "emboss")
			{
				FilterStylize.Emboss(bitmap, values[0], values[1], values[2]);
			}
			else if (id == "findedges")
			{
				FilterStylize.FindEdges(bitmap);
			}
			else if (id == "solarize")
			{
				FilterStylize.Solarize(bitmap);
			}
		}

		private void BeginAdjustmentPreview(string id)
		{
			if (!IsAdjustmentPreviewable(id))
			{
				return;
			}
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			document.BeginStroke();
		}

		private void OpenAdjustment(string id)
		{
			RollFilterSeed();
			BeginAdjustmentPreview(id);
			if (id == "rotate")
			{
				ShowModal(new AdjustmentDialog("Rotate Arbitrary", "rotate", new string[] { "Angle" }, new int[] { -180 }, new int[] { 180 }, new int[] { 0 }), 360.0, 170.0);
				return;
			}
			if (id == "feather")
			{
				ShowModal(new AdjustmentDialog("Feather Selection", "feather", new string[] { "Radius" }, new int[] { 1 }, new int[] { 100 }, new int[] { 4 }), 360.0, 170.0);
				return;
			}
			if (id == "bc")
			{
				ShowModal(new AdjustmentDialog("Brightness/Contrast", "bc", new string[] { "Brightness", "Contrast" }, new int[] { -100, -100 }, new int[] { 100, 100 }, new int[] { 0, 0 }), 360.0, 230.0);
				return;
			}
			if (id == "hsl")
			{
				ShowModal(new AdjustmentDialog("Hue/Saturation", "hsl", new string[] { "Hue", "Saturation", "Lightness" }, new int[] { -180, -100, -100 }, new int[] { 180, 100, 100 }, new int[] { 0, 0, 0 }), 360.0, 260.0);
				return;
			}
			if (id == "posterize")
			{
				ShowModal(new AdjustmentDialog("Posterize", "posterize", new string[] { "Levels" }, new int[] { 2 }, new int[] { 64 }, new int[] { 8 }), 360.0, 200.0);
				return;
			}
			if (id == "threshold")
			{
				ShowModal(new AdjustmentDialog("Threshold", "threshold", new string[] { "Level" }, new int[] { 0 }, new int[] { 255 }, new int[] { 128 }), 360.0, 200.0);
				return;
			}
			if (id == "gblur")
			{
				ShowModal(new AdjustmentDialog("Gaussian Blur", "gblur", new string[] { "Radius" }, new int[] { 1 }, new int[] { 30 }, new int[] { 5 }), 360.0, 200.0);
				return;
			}
			if (id == "unsharp")
			{
				ShowModal(new AdjustmentDialog("Unsharp Mask", "unsharp", new string[] { "Amount", "Radius" }, new int[] { 0, 1 }, new int[] { 300, 30 }, new int[] { 100, 3 }), 360.0, 230.0);
				return;
			}
			if (id == "noise")
			{
				ShowModal(new AdjustmentDialog("Add Noise", "noise", new string[] { "Amount" }, new int[] { 0 }, new int[] { 100 }, new int[] { 20 }), 360.0, 200.0);
				return;
			}
			if (id == "pixelate")
			{
				ShowModal(new AdjustmentDialog("Mosaic", "pixelate", new string[] { "Cell Size" }, new int[] { 2 }, new int[] { 64 }, new int[] { 8 }), 360.0, 200.0);
				return;
			}
			if (id == "boxblur")
			{
				ShowModal(new AdjustmentDialog("Box Blur", "boxblur", new string[] { "Radius" }, new int[] { 1 }, new int[] { 100 }, new int[] { 10 }), 360.0, 200.0);
				return;
			}
			if (id == "motionblur")
			{
				ShowModal(new AdjustmentDialog("Motion Blur", "motionblur", new string[] { "Angle", "Distance" }, new int[] { -90, 1 }, new int[] { 90, 200 }, new int[] { 0, 10 }), 360.0, 230.0);
				return;
			}
			if (id == "radialblur")
			{
				ShowModal(new AdjustmentDialog("Radial Blur", "radialblur", new string[] { "Amount" }, new int[] { 1 }, new int[] { 100 }, new int[] { 10 }, new string[] { "Method" }, new string[][] { new string[] { "Spin", "Zoom" } }, new int[] { 0 }), 360.0, 230.0);
				return;
			}
			if (id == "median")
			{
				ShowModal(new AdjustmentDialog("Median", "median", new string[] { "Radius" }, new int[] { 1 }, new int[] { 16 }, new int[] { 3 }), 360.0, 200.0);
				return;
			}
			if (id == "crystallize")
			{
				ShowModal(new AdjustmentDialog("Crystallize", "crystallize", new string[] { "Cell Size" }, new int[] { 3 }, new int[] { 300 }, new int[] { 10 }), 360.0, 200.0);
				return;
			}
			if (id == "pointillize")
			{
				ShowModal(new AdjustmentDialog("Pointillize", "pointillize", new string[] { "Cell Size" }, new int[] { 3 }, new int[] { 200 }, new int[] { 5 }), 360.0, 200.0);
				return;
			}
			if (id == "diffuse")
			{
				ShowModal(new AdjustmentDialog("Diffuse", "diffuse", new string[0], new int[0], new int[0], new int[0], new string[] { "Mode" }, new string[][] { new string[] { "Normal", "Darken Only", "Lighten Only" } }, new int[] { 0 }), 360.0, 200.0);
				return;
			}
			if (id == "emboss")
			{
				ShowModal(new AdjustmentDialog("Emboss", "emboss", new string[] { "Angle", "Height", "Amount" }, new int[] { -180, 1, 1 }, new int[] { 180, 10, 500 }, new int[] { 135, 3, 100 }), 360.0, 260.0);
				return;
			}
		}

		public void ApplyAdjustment(string id, int[] values)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (id == "rotate")
			{
				document.BeginCanvasEdit("Rotate");
				document.RotateArbitrary(values[0], 2);
				document.EndCanvasEdit();
				FinishCanvasOp(canvas, document);
				return;
			}
			if (id == "feather")
			{
				document.Selection().FeatherActive(values[0]);
				canvas.InvalidateSurface();
				return;
			}
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			SkiaSharp.SKBitmap bitmap = activeLayer.Bitmap();
			document.BeginStroke();
			RunAdjustmentMath(id, bitmap, values);
			document.EndStroke();
			canvas.MarkComposeDirty();
			RecordLastFilter(id, values);
		}

		public void PreviewAdjustment(string id, int[] values)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document.StrokeSnapshot() == null)
			{
				return;
			}
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			document.RestoreStrokeSnapshot();
			RunAdjustmentMath(id, activeLayer.Bitmap(), values);
			canvas.MarkComposeDirty();
		}

		public void RestoreAdjustmentPreview()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document.StrokeSnapshot() == null)
			{
				return;
			}
			document.RestoreStrokeSnapshot();
			canvas.MarkComposeDirty();
		}

		public void CommitAdjustment(string id, int[] values)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document.StrokeSnapshot() == null)
			{
				ApplyAdjustment(id, values);
				RefreshLayerThumbnails();
				return;
			}
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				document.EndStroke();
				return;
			}
			document.RestoreStrokeSnapshot();
			RunAdjustmentMath(id, activeLayer.Bitmap(), values);
			document.EndStroke();
			canvas.MarkComposeDirty();
			RefreshLayerThumbnails();
			RecordLastFilter(id, values);
		}

		public void CancelAdjustment()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document.StrokeSnapshot() == null)
			{
				return;
			}
			document.RestoreStrokeSnapshot();
			document.EndStroke();
			canvas.MarkComposeDirty();
		}

		private void DoDesaturate()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			document.BeginStroke();
			Adjustments.Desaturate(activeLayer.Bitmap());
			document.EndStroke();
			canvas.MarkComposeDirty();
		}

		private void DoSelectAll()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.CommitFloatingSelection();
			document.Selection().SelectRect(new SkiaSharp.SKRectI(0, 0, document.Width(), document.Height()));
			canvas.MarkComposeDirty();
			canvas.Redraw();
		}

		private void DoDeselect()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.CommitFloatingSelection();
			document.Selection().Clear();
			canvas.MarkComposeDirty();
			canvas.Redraw();
		}

		private void DoInvertSelection()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.CommitFloatingSelection();
			document.Selection().Invert();
			canvas.MarkComposeDirty();
			canvas.Redraw();
		}

		private void DoUndo()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			if (canvas.CurrentDocument().Undo())
			{
				canvas.SyncDocumentSize();
				canvas.MarkComposeDirty();
				RefreshPanels();
			}
		}

		private void DoRedo()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			if (canvas.CurrentDocument().Redo())
			{
				canvas.SyncDocumentSize();
				canvas.MarkComposeDirty();
				RefreshPanels();
			}
		}

		private void DoExit()
		{
			Application current = Application.Current;
			if (current != null)
			{
				current.Quit();
			}
		}

		private void DoZoomIn()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.ZoomIn();
			}
		}

		private void DoZoomOut()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.ZoomOut();
			}
		}

		private void DoFit()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.FitToView();
			}
		}

		public void ZoomActiveTo100()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.ZoomTo100();
			}
		}

		private SkiaSharp.SKBitmap ExtractSelection(Document document, Layer layer)
		{
			Selection selection = document.Selection();
			if (selection != null && selection.IsActive())
			{
				SkiaSharp.SKRectI bounds = selection.Bounds();
				int width = bounds.Width;
				int height = bounds.Height;
				if (width <= 0 || height <= 0)
				{
					return null;
				}
				SkiaSharp.SKBitmap result = ExtractSelectionRaw(layer, selection, bounds, width, height);
				return result;
			}
			return layer.Bitmap().Copy();
		}

		private unsafe SkiaSharp.SKBitmap ExtractSelectionRaw(Layer layer, Selection selection, SkiaSharp.SKRectI bounds, int width, int height)
		{
			SkiaSharp.SKBitmap result = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
			result.Erase(SkiaSharp.SKColors.Transparent);
			SkiaSharp.SKBitmap sourceBitmap = layer.Bitmap();
			int layerOffsetX = layer.OffsetX();
			int layerOffsetY = layer.OffsetY();
			int sourceWidth = sourceBitmap.Width;
			int sourceHeight = sourceBitmap.Height;
			int sourceStride = sourceBitmap.RowBytes;
			int resultStride = result.RowBytes;
			byte[] selectionMask = selection.Mask();
			int selectionWidth = selection.Width();
			byte* sourceBase = (byte*)sourceBitmap.GetPixels().ToPointer();
			byte* resultBase = (byte*)result.GetPixels().ToPointer();
			for (int row = 0; row < height; row++)
			{
				int canvasY = bounds.Top + row;
				int selectionRow = canvasY * selectionWidth;
				byte* resultRow = resultBase + ((long)row * resultStride);
				for (int column = 0; column < width; column++)
				{
					int canvasX = bounds.Left + column;
					int coverage = selectionMask[selectionRow + canvasX];
					if (coverage == 0)
					{
						continue;
					}
					int bitmapX = canvasX - layerOffsetX;
					int bitmapY = canvasY - layerOffsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= sourceWidth || bitmapY >= sourceHeight)
					{
						continue;
					}
					byte* sourcePixel = sourceBase + ((long)bitmapY * sourceStride) + (bitmapX * 4);
					byte* resultPixel = resultRow + (column * 4);
					resultPixel[0] = sourcePixel[0];
					resultPixel[1] = sourcePixel[1];
					resultPixel[2] = sourcePixel[2];
					if (coverage < 255)
					{
						resultPixel[3] = (byte)(((sourcePixel[3] * coverage) + 127) / 255);
					}
					else
					{
						resultPixel[3] = sourcePixel[3];
					}
				}
			}
			return result;
		}

		private unsafe void EraseSelection(Document document, Layer layer)
		{
			Selection selection = document.Selection();
			if (selection != null && selection.IsActive())
			{
				SkiaSharp.SKRectI bounds = selection.Bounds();
				SkiaSharp.SKBitmap bitmap = layer.Bitmap();
				int layerOffsetX = layer.OffsetX();
				int layerOffsetY = layer.OffsetY();
				int bitmapWidth = bitmap.Width;
				int bitmapHeight = bitmap.Height;
				int stride = bitmap.RowBytes;
				byte[] selectionMask = selection.Mask();
				int selectionWidth = selection.Width();
				byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
				for (int canvasY = bounds.Top; canvasY < bounds.Bottom; canvasY++)
				{
					int selectionRow = canvasY * selectionWidth;
					for (int canvasX = bounds.Left; canvasX < bounds.Right; canvasX++)
					{
						int coverage = selectionMask[selectionRow + canvasX];
						if (coverage == 0)
						{
							continue;
						}
						int bitmapX = canvasX - layerOffsetX;
						int bitmapY = canvasY - layerOffsetY;
						if (bitmapX < 0 || bitmapY < 0 || bitmapX >= bitmapWidth || bitmapY >= bitmapHeight)
						{
							continue;
						}
						byte* pixel = basePointer + ((long)bitmapY * stride) + (bitmapX * 4);
						if (coverage == 255)
						{
							pixel[0] = 0;
							pixel[1] = 0;
							pixel[2] = 0;
							pixel[3] = 0;
							continue;
						}
						pixel[3] = (byte)(((pixel[3] * (255 - coverage)) + 127) / 255);
					}
				}
				return;
			}
			layer.Bitmap().Erase(SkiaSharp.SKColors.Transparent);
		}

		private async void DoCopy()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			SkiaSharp.SKBitmap copied = ExtractSelection(document, layer);
			if (copied == null)
			{
				return;
			}
			if (s_clipboardBitmap != null)
			{
				s_clipboardBitmap.Dispose();
			}
			s_clipboardBitmap = copied;
			SetStatusMessage("Copied");
			await CopyToSystemClipboard(copied);
		}

		private async System.Threading.Tasks.Task CopyToSystemClipboard(SkiaSharp.SKBitmap bitmap)
		{
			try
			{
				SkiaSharp.SKPixmap pixmap = bitmap.PeekPixels();
				SkiaSharp.SKImage image = SkiaSharp.SKImage.FromPixels(pixmap);
				SkiaSharp.SKData data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
				image.Dispose();
				pixmap.Dispose();
				if (data == null)
				{
					return;
				}
				byte[] bytes = data.ToArray();
				data.Dispose();
				Windows.Storage.Streams.InMemoryRandomAccessStream stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
				Windows.Storage.Streams.DataWriter writer = new Windows.Storage.Streams.DataWriter(stream);
				writer.WriteBytes(bytes);
				await writer.StoreAsync();
				await writer.FlushAsync();
				writer.DetachStream();
				writer.Dispose();
				stream.Seek(0);
				Windows.Storage.Streams.RandomAccessStreamReference reference = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromStream(stream);
				Windows.ApplicationModel.DataTransfer.DataPackage package = new Windows.ApplicationModel.DataTransfer.DataPackage();
				package.SetBitmap(reference);
				Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
			}
			catch (Exception error)
			{
				Log.Exception(error);
			}
		}

		private async System.Threading.Tasks.Task<SkiaSharp.SKBitmap> GetSystemClipboardBitmap()
		{
			try
			{
				Windows.ApplicationModel.DataTransfer.DataPackageView view = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
				if (view == null)
				{
					return null;
				}
				if (!view.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
				{
					return null;
				}
				Windows.Storage.Streams.RandomAccessStreamReference reference = await view.GetBitmapAsync();
				Windows.Storage.Streams.IRandomAccessStreamWithContentType stream = await reference.OpenReadAsync();
				System.IO.Stream netStream = System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
				SkiaSharp.SKBitmap decoded = SkiaSharp.SKBitmap.Decode(netStream);
				netStream.Dispose();
				stream.Dispose();
				if (decoded == null)
				{
					return null;
				}
				SkiaSharp.SKBitmap normalized = new SkiaSharp.SKBitmap(decoded.Width, decoded.Height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
				SkiaSharp.SKCanvas canvas = new SkiaSharp.SKCanvas(normalized);
				canvas.Clear(SkiaSharp.SKColors.Transparent);
				SkiaSharp.SKImage decodedImage = SkiaSharp.SKImage.FromBitmap(decoded);
				SkiaSharp.SKSamplingOptions sampling = new SkiaSharp.SKSamplingOptions(SkiaSharp.SKFilterMode.Nearest, SkiaSharp.SKMipmapMode.None);
				SkiaSharp.SKPaint imagePaint = new SkiaSharp.SKPaint();
				canvas.DrawImage(decodedImage, 0.0f, 0.0f, sampling, imagePaint);
				imagePaint.Dispose();
				decodedImage.Dispose();
				canvas.Dispose();
				decoded.Dispose();
				return normalized;
			}
			catch (Exception error)
			{
				Log.Exception(error);
				return null;
			}
		}

		private async void DoCut()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			SkiaSharp.SKBitmap copied = ExtractSelection(document, layer);
			if (copied == null)
			{
				return;
			}
			if (s_clipboardBitmap != null)
			{
				s_clipboardBitmap.Dispose();
			}
			s_clipboardBitmap = copied;
			await CopyToSystemClipboard(copied);
			document.BeginStroke();
			EraseSelection(document, layer);
			document.EndStroke();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
			}
			RefreshLayerThumbnails();
			SetStatusMessage("Cut");
		}

		private async void DoPaste()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			SkiaSharp.SKBitmap pasted = await GetSystemClipboardBitmap();
			if (pasted == null && s_clipboardBitmap != null)
			{
				pasted = s_clipboardBitmap.Copy();
			}
			if (pasted == null)
			{
				return;
			}
			document.BeginCanvasEdit("Paste");
			Layer layer = document.AddLayer("Pasted");
			if (layer == null)
			{
				document.EndCanvasEdit();
				pasted.Dispose();
				return;
			}
			layer.SetBitmap(pasted);
			int offsetX = (document.Width() - pasted.Width) / 2;
			int offsetY = (document.Height() - pasted.Height) / 2;
			layer.SetOffset(offsetX, offsetY);
			int selLeft = offsetX;
			int selTop = offsetY;
			int selRight = offsetX + pasted.Width;
			int selBottom = offsetY + pasted.Height;
			if (selLeft < 0)
			{
				selLeft = 0;
			}
			if (selTop < 0)
			{
				selTop = 0;
			}
			if (selRight > document.Width())
			{
				selRight = document.Width();
			}
			if (selBottom > document.Height())
			{
				selBottom = document.Height();
			}
			if (selRight > selLeft && selBottom > selTop)
			{
				document.Selection().SelectRect(new SkiaSharp.SKRectI(selLeft, selTop, selRight, selBottom));
			}
			document.EndCanvasEdit();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
			}
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
			SetStatusMessage("Pasted");
		}

		public bool RulersEnabled()
		{
			return m_rulersEnabled;
		}

		private void ToggleGrid()
		{
			m_gridEnabled = !m_gridEnabled;
			Microsoft.Maui.Storage.Preferences.Default.Set("grid_enabled", m_gridEnabled);
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index] as DocumentWindow;
				if (window != null)
				{
					window.Canvas().InvalidateSurface();
				}
			}
		}

		private void OpenRecentFile(string path)
		{
			if (!System.IO.File.Exists(path))
			{
				SetStatusMessage("File not found — removed from recent: " + System.IO.Path.GetFileName(path));
				RecentFiles.Remove(path);
				return;
			}
			OpenDocumentFromPath(path);
		}

		private void OpenStrokeDialog()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				SetStatusMessage("No document");
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null || layer.IsText())
			{
				SetStatusMessage("Active layer cannot be stroked");
				return;
			}
			ShowModal(new StrokeDialog(), 320.0, 220.0);
		}

		public SKColor ForegroundColor()
		{
			return m_toolState.Foreground();
		}

		private void OpenLayerStyleDialog()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			m_layerStyleSnapshot = layer.LayerStyle().Clone();
			m_layerStyleTargetIndex = document.ActiveLayerIndex();
			ShowModal(new LayerStyleDialog(layer.LayerStyle().Clone()), 620.0, 460.0);
		}

		public void OpenLayerPropertiesDialog()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			ShowModal(new LayerPropertiesDialog(layer.Name()), 320.0, 160.0);
		}

		public void RenameActiveLayer(string name)
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			if (name == layer.Name())
			{
				return;
			}
			document.BeginCanvasEdit("Layer Properties");
			layer.SetName(name);
			document.EndCanvasEdit();
			RefreshPanels();
		}

		private Layer LayerStyleTargetLayer()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return null;
			}
			System.Collections.Generic.List<Layer> layers = document.Layers();
			if (m_layerStyleTargetIndex < 0 || m_layerStyleTargetIndex >= layers.Count)
			{
				return null;
			}
			return layers[m_layerStyleTargetIndex];
		}

		public void PreviewLayerStyle(LayerStyle style)
		{
			Layer layer = LayerStyleTargetLayer();
			if (layer == null)
			{
				return;
			}
			layer.SetLayerStyle(style);
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			canvas.MarkComposeDirty();
			canvas.InvalidateSurface();
		}

		public void CommitLayerStyle(LayerStyle style)
		{
			Document document = ActiveDocument();
			Layer layer = LayerStyleTargetLayer();
			if (document == null || layer == null || m_layerStyleSnapshot == null)
			{
				m_layerStyleSnapshot = null;
				CloseModal();
				return;
			}
			layer.SetLayerStyle(m_layerStyleSnapshot);
			document.BeginCanvasEdit("Layer Style");
			layer.SetLayerStyle(style.Clone());
			document.EndCanvasEdit();
			m_layerStyleSnapshot = null;
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
			RefreshPanels();
			CloseModal();
		}

		public void CancelLayerStyle()
		{
			Layer layer = LayerStyleTargetLayer();
			if (layer != null && m_layerStyleSnapshot != null)
			{
				layer.SetLayerStyle(m_layerStyleSnapshot);
				CanvasView canvas = ActiveCanvas();
				if (canvas != null)
				{
					canvas.MarkComposeDirty();
					canvas.InvalidateSurface();
				}
			}
			m_layerStyleSnapshot = null;
			CloseModal();
		}

		public void OpenColorPickerFor(SKColor initial, System.Action<SKColor> onApply)
		{
			ColorPicker picker = new ColorPicker(initial, onApply);
			ShowModal(picker, 380.0, 360.0);
		}

		public void ApplyStroke(int width, int position)
		{
			CloseModal();
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			Layer layer = document.ActiveLayer();
			if (layer == null || layer.IsText())
			{
				return;
			}
			document.BeginStroke();
			SelectionStroke.Apply(document, m_toolState.Foreground(), width, position);
			document.EndStroke();
			canvas.InvalidateSurface();
			RefreshLayerThumbnails();
		}

		private void ToggleRulers()
		{
			m_rulersEnabled = !m_rulersEnabled;
			Microsoft.Maui.Storage.Preferences.Default.Set("rulers_enabled", m_rulersEnabled);
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index] as DocumentWindow;
				if (window != null)
				{
					window.SetRulersEnabled(m_rulersEnabled);
				}
			}
		}

		private void DoInvert()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			document.BeginStroke();
			Adjustments.InvertColors(activeLayer.Bitmap());
			document.EndStroke();
			canvas.MarkComposeDirty();
		}

		public void UpdateCursor(int x, int y)
		{
			if (m_statusCursorLabel != null)
			{
				m_statusCursorLabel.Text = "x: " + x + "   y: " + y;
			}
			if (m_infoPanel == null)
			{
				return;
			}
			m_infoPanel.UpdateCursor(x, y);
			Document document = ActiveDocument();
			if (document == null)
			{
				m_infoPanel.UpdatePixel(new SKColor(0, 0, 0, 0), false);
				m_infoPanel.UpdateSelection(new SKRectI(0, 0, 0, 0), false);
				return;
			}
			Layer layer = document.ActiveLayer();
			bool hasPixel = layer != null && x >= 0 && y >= 0 && x < document.Width() && y < document.Height();
			if (hasPixel)
			{
				m_infoPanel.UpdatePixel(layer.GetPixelCanvas(x, y), true);
			}
			else
			{
				m_infoPanel.UpdatePixel(new SKColor(0, 0, 0, 0), false);
			}
			Selection selection = document.Selection();
			m_infoPanel.UpdateSelection(selection.Bounds(), selection.IsActive());
		}

		public void UpdateZoomInfo(int zoomPercent, int width, int height)
		{
			if (m_statusInfoLabel != null)
			{
				m_statusInfoLabel.Text = zoomPercent + "%      " + width + " × " + height + " px";
			}
			if (m_navigatorPanel != null)
			{
				m_navigatorPanel.RefreshView();
			}
		}

		private View BuildStatusBar()
		{
			Grid bar = new Grid();
			bar.HeightRequest = UiConstants.StatusBarHeight;
			bar.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			bar.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			m_statusInfoLabel = new Label();
			m_statusInfoLabel.Text = "100%      800 × 600 px";
			m_statusInfoLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_statusInfoLabel.FontSize = UiConstants.ComponentFontSize;
			m_statusInfoLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusInfoLabel, 0);
			bar.Add(m_statusInfoLabel);

			m_statusCursorLabel = new Label();
			m_statusCursorLabel.Text = "x: —   y: —";
			m_statusCursorLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_statusCursorLabel.FontSize = UiConstants.ComponentFontSize;
			m_statusCursorLabel.HorizontalOptions = LayoutOptions.End;
			m_statusCursorLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusCursorLabel, 1);
			bar.Add(m_statusCursorLabel);

			return bar;
		}

		private View BuildPaletteDock()
		{
			m_navigatorPanel = new NavigatorPanel();
			m_infoPanel = new InfoPanel();
			m_navigatorGroup = new PaletteGroup(new string[] { "Navigator", "Info" }, new View[] { m_navigatorPanel, m_infoPanel });

			m_swatchesPanel = new SwatchesPanel();
			ColorPicker dockColorPicker = new ColorPicker(new SKColor(0, 0, 0, 255), true, true);
			m_swatchesGroup = new PaletteGroup(new string[] { "Swatches", "Color" }, new View[] { m_swatchesPanel, dockColorPicker });

			m_layersPanel = new LayersPanel();
			m_channelsPanel = new ChannelsPanel();
			m_layersGroup = new PaletteGroup(new string[] { "Layers", "Channels" }, new View[] { m_layersPanel, m_channelsPanel });

			Grid dock = new Grid();
			dock.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			dock.Padding = new Thickness(4.0);
			dock.RowSpacing = 4.0;
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			dock.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			dock.RowDefinitions.Add(new RowDefinition(new GridLength(0.0)));

			dock.Add(m_navigatorGroup);
			dock.Add(m_swatchesGroup);
			dock.Add(m_layersGroup);

			m_paletteOrder = new List<PaletteGroup>();
			m_paletteOrder.Add(m_navigatorGroup);
			m_paletteOrder.Add(m_swatchesGroup);
			m_paletteOrder.Add(m_layersGroup);

			m_paletteDock = dock;
			LoadPanelLayout();
			RefreshDockLayout();
			return dock;
		}

		private PaletteGroup PanelForKey(string key)
		{
			if (key == "Navigator")
			{
				return m_navigatorGroup;
			}
			if (key == "Swatches")
			{
				return m_swatchesGroup;
			}
			if (key == "Layers")
			{
				return m_layersGroup;
			}
			return null;
		}

		private bool PanelVisibleFlag(string key)
		{
			if (key == "Navigator")
			{
				return m_navigatorPanelVisible;
			}
			if (key == "Swatches")
			{
				return m_swatchesPanelVisible;
			}
			if (key == "Layers")
			{
				return m_layersPanelVisible;
			}
			return m_infoPanelVisible;
		}

		private void SetPanelVisibleFlag(string key, bool visible)
		{
			if (key == "Navigator")
			{
				m_navigatorPanelVisible = visible;
			}
			if (key == "Swatches")
			{
				m_swatchesPanelVisible = visible;
			}
			if (key == "Layers")
			{
				m_layersPanelVisible = visible;
			}
			if (key == "Info")
			{
				m_infoPanelVisible = visible;
			}
		}

		private void SavePanelLayout()
		{
			if (m_paletteOrder == null)
			{
				return;
			}
			string order = "";
			string hidden = "";
			string collapsed = "";
			for (int index = 0; index < m_paletteOrder.Count; index++)
			{
				PaletteGroup group = m_paletteOrder[index];
				string key = group.PanelKey();
				if (order.Length > 0)
				{
					order = order + ",";
				}
				order = order + key;
				if (!PanelVisibleFlag(key))
				{
					if (hidden.Length > 0)
					{
						hidden = hidden + ",";
					}
					hidden = hidden + key;
				}
				if (group.IsCollapsed())
				{
					if (collapsed.Length > 0)
					{
						collapsed = collapsed + ",";
					}
					collapsed = collapsed + key;
				}
			}
			Microsoft.Maui.Storage.Preferences.Default.Set("panel_order", order);
			Microsoft.Maui.Storage.Preferences.Default.Set("panel_hidden", hidden);
			Microsoft.Maui.Storage.Preferences.Default.Set("panel_collapsed", collapsed);
		}

		private void LoadPanelLayout()
		{
			string order = Microsoft.Maui.Storage.Preferences.Default.Get("panel_order", "");
			if (order.Length > 0)
			{
				List<PaletteGroup> restored = new List<PaletteGroup>();
				string[] orderKeys = order.Split(new char[] { ',' });
				for (int index = 0; index < orderKeys.Length; index++)
				{
					PaletteGroup group = PanelForKey(orderKeys[index]);
					if (group != null && !restored.Contains(group))
					{
						restored.Add(group);
					}
				}
				for (int index = 0; index < m_paletteOrder.Count; index++)
				{
					if (!restored.Contains(m_paletteOrder[index]))
					{
						restored.Add(m_paletteOrder[index]);
					}
				}
				m_paletteOrder = restored;
			}
			string hidden = Microsoft.Maui.Storage.Preferences.Default.Get("panel_hidden", "");
			if (hidden.Length > 0)
			{
				string[] hiddenKeys = hidden.Split(new char[] { ',' });
				for (int index = 0; index < hiddenKeys.Length; index++)
				{
					SetPanelVisibleFlag(hiddenKeys[index], false);
				}
			}
			string collapsed = Microsoft.Maui.Storage.Preferences.Default.Get("panel_collapsed", "");
			if (collapsed.Length > 0)
			{
				string[] collapsedKeys = collapsed.Split(new char[] { ',' });
				for (int index = 0; index < collapsedKeys.Length; index++)
				{
					PaletteGroup group = PanelForKey(collapsedKeys[index]);
					if (group != null)
					{
						group.SetCollapsed(true);
					}
				}
			}
		}

		private void RefreshDockLayout()
		{
			if (m_paletteDock == null)
			{
				return;
			}
			m_navigatorGroup.IsVisible = m_navigatorPanelVisible;
			m_swatchesGroup.IsVisible = m_swatchesPanelVisible;
			m_layersGroup.IsVisible = m_layersPanelVisible;
			bool layersStretch = m_layersPanelVisible && !m_layersGroup.IsCollapsed();
			for (int index = 0; index < m_paletteOrder.Count; index++)
			{
				PaletteGroup group = m_paletteOrder[index];
				Grid.SetRow(group, index);
				GridLength height = GridLength.Auto;
				if (group == m_layersGroup && layersStretch)
				{
					height = GridLength.Star;
				}
				m_paletteDock.RowDefinitions[index].Height = height;
			}
			GridLength fillerLength = GridLength.Star;
			if (layersStretch)
			{
				fillerLength = new GridLength(0.0);
			}
			m_paletteDock.RowDefinitions[m_paletteOrder.Count].Height = fillerLength;
		}

		public void ReorderPalettePanel(PaletteGroup group, double deltaY)
		{
			if (m_paletteOrder == null)
			{
				return;
			}
			if (!m_paletteOrder.Contains(group))
			{
				return;
			}
			List<PaletteGroup> visible = new List<PaletteGroup>();
			List<double> centers = new List<double>();
			for (int index = 0; index < m_paletteOrder.Count; index++)
			{
				PaletteGroup candidate = m_paletteOrder[index];
				if (!candidate.IsVisible)
				{
					continue;
				}
				double center = candidate.Y + (candidate.Height / 2.0);
				if (candidate == group)
				{
					center = center + deltaY;
				}
				visible.Add(candidate);
				centers.Add(center);
			}
			for (int outer = 1; outer < visible.Count; outer++)
			{
				PaletteGroup movingGroup = visible[outer];
				double movingCenter = centers[outer];
				int inner = outer - 1;
				for (;;)
				{
					if (inner < 0 || centers[inner] <= movingCenter)
					{
						break;
					}
					visible[inner + 1] = visible[inner];
					centers[inner + 1] = centers[inner];
					inner = inner - 1;
				}
				visible[inner + 1] = movingGroup;
				centers[inner + 1] = movingCenter;
			}
			List<PaletteGroup> reordered = new List<PaletteGroup>();
			for (int index = 0; index < visible.Count; index++)
			{
				reordered.Add(visible[index]);
			}
			for (int index = 0; index < m_paletteOrder.Count; index++)
			{
				PaletteGroup candidate = m_paletteOrder[index];
				if (!candidate.IsVisible)
				{
					reordered.Add(candidate);
				}
			}
			m_paletteOrder = reordered;
			RefreshDockLayout();
			SavePanelLayout();
		}

		public void OnPaletteGroupLayoutChanged()
		{
			RefreshDockLayout();
			SavePanelLayout();
		}

		public void ClosePalettePanel(string key)
		{
			SetPanelVisibleFlag(key, false);
			RefreshDockLayout();
			SavePanelLayout();
		}

		private void ToggleDockPanel(string key)
		{
			SetPanelVisibleFlag(key, !PanelVisibleFlag(key));
			RefreshDockLayout();
			SavePanelLayout();
		}

		private BoxView BuildDivider()
		{
			BoxView divider = new BoxView();
			divider.ThemeColor(UiConstants.DividerLight, UiConstants.DividerDark);
			return divider;
		}

		private View BuildMiddle()
		{
			m_toolPalette = new ToolPalette();

			m_workspace = new AbsoluteLayout();
			m_workspace.ThemeBg(UiConstants.WorkspaceBackdropLight, UiConstants.WorkspaceBackdropDark);
			m_workspace.SizeChanged += OnWorkspaceSizeChanged;

			BoxView workspaceBackground = new BoxView();
			workspaceBackground.Color = Colors.Transparent;
			TapGestureRecognizer workspaceDoubleTap = new TapGestureRecognizer();
			workspaceDoubleTap.NumberOfTapsRequired = 2;
			workspaceDoubleTap.Tapped += OnWorkspaceDoubleTapped;
			workspaceBackground.GestureRecognizers.Add(workspaceDoubleTap);
			AbsoluteLayout.SetLayoutBounds(workspaceBackground, new Rect(0.0, 0.0, 1.0, 1.0));
			AbsoluteLayout.SetLayoutFlags(workspaceBackground, AbsoluteLayoutFlags.All);
			m_workspace.Add(workspaceBackground);

			View dock = BuildPaletteDock();

			Grid middle = new Grid();
			middle.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(UiConstants.ToolPaletteWidth)));
			middle.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.0)));
			middle.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			middle.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.0)));
			middle.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(UiConstants.PaletteDockWidth)));

			Grid.SetColumn(m_toolPalette, 0);
			middle.Add(m_toolPalette);

			BoxView leftDivider = BuildDivider();
			Grid.SetColumn(leftDivider, 1);
			middle.Add(leftDivider);

			Grid.SetColumn(m_workspace, 2);
			middle.Add(m_workspace);

			BoxView rightDivider = BuildDivider();
			Grid.SetColumn(rightDivider, 3);
			middle.Add(rightDivider);

			Grid.SetColumn(dock, 4);
			middle.Add(dock);

			return middle;
		}

		public MainView()
		{
			Self = this;
			Title = "Bitmute";
			Theme.InitializeFromSystem();
			Document.SetMaxUndoDepth(Microsoft.Maui.Storage.Preferences.Default.Get("undo_depth", 100));
			Microsoft.Maui.Controls.Application application = Microsoft.Maui.Controls.Application.Current;
			if (application != null)
			{
				application.RequestedThemeChanged += OnSystemThemeChanged;
			}
			this.ThemeBg(UiConstants.WorkspaceBackdropLight, UiConstants.WorkspaceBackdropDark);

			m_documents = new List<FloatingPanel>();
			m_modalStack = new System.Collections.Generic.List<ModalEntry>();
			m_untitledCount = 0;
			m_cascadeCount = 0;
			m_topZIndex = 0;
			m_toolBox = new ToolBox();
			m_toolState = m_toolBox.State();
			m_guideCreateOrientation = 0;
			m_guideCreateCanvas = null;
			m_gridEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("grid_enabled", false);
			m_rulersEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("rulers_enabled", true);
			m_channelViewMode = -1;
			m_snapEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("snap_enabled", true);
			m_snapTargetGuides = Microsoft.Maui.Storage.Preferences.Default.Get("snap_target_guides", true);
			m_snapTargetGrid = Microsoft.Maui.Storage.Preferences.Default.Get("snap_target_grid", true);
			m_snapTargetEdges = Microsoft.Maui.Storage.Preferences.Default.Get("snap_target_edges", true);
			m_snapTargetLayerBounds = Microsoft.Maui.Storage.Preferences.Default.Get("snap_target_layer_bounds", true);
			m_menuTitles = new string[] { "File", "Edit", "Image", "Layer", "Select", "Filter", "View", "Window", "Help" };
			m_overlay = new AbsoluteLayout();
			m_overlay.InputTransparent = true;
			m_overlay.CascadeInputTransparent = false;
			m_menuBar = new MenuBar(this, m_menuTitles, m_overlay);

			View menuBar = m_menuBar.Root();
			m_optionsBar = new OptionsBar(this, m_toolState);
			View optionsBar = m_optionsBar.Root();
			View middle = BuildMiddle();
			m_textEditSession = new TextEditSession(this, m_toolState, m_workspace);
			View statusBar = BuildStatusBar();

			Grid root = new Grid();
			root.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.MenuBarHeight)));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(1.0)));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.OptionsBarHeight)));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(1.0)));
			root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(1.0)));
			root.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.StatusBarHeight)));

			Grid.SetRow(menuBar, 0);
			root.Add(menuBar);

			BoxView underMenu = BuildDivider();
			Grid.SetRow(underMenu, 1);
			root.Add(underMenu);

			Grid.SetRow(optionsBar, 2);
			root.Add(optionsBar);

			BoxView underOptions = BuildDivider();
			Grid.SetRow(underOptions, 3);
			root.Add(underOptions);

			Grid.SetRow(middle, 4);
			root.Add(middle);

			BoxView aboveStatus = BuildDivider();
			Grid.SetRow(aboveStatus, 5);
			root.Add(aboveStatus);

			Grid.SetRow(statusBar, 6);
			root.Add(statusBar);

			Grid outer = new Grid();
			outer.Add(root);
			outer.Add(m_overlay);

			Content = outer;
		}

		private void HookAppWindowClosing()
		{
			if (m_appCloseHooked)
			{
				return;
			}
			m_appCloseHookAttempts = m_appCloseHookAttempts + 1;
			Microsoft.Maui.Controls.Window mauiWindow = Window;
			Microsoft.UI.Xaml.Window nativeWindow = null;
			if (mauiWindow != null && mauiWindow.Handler != null)
			{
				nativeWindow = mauiWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window;
			}
			if (nativeWindow == null || nativeWindow.AppWindow == null)
			{
				if (m_appCloseHookAttempts < 20)
				{
					Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500.0), HookAppWindowClosing);
				}
				return;
			}
			m_nativeWindow = nativeWindow;
			nativeWindow.AppWindow.Closing += OnAppWindowClosing;
			m_appCloseHooked = true;
		}

		private void OnAppWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
		{
			if (m_quitConfirmed)
			{
				return;
			}
			if (FirstDirtyDocumentWindow() == null)
			{
				return;
			}
			args.Cancel = true;
			m_quitPending = true;
			Dispatcher.Dispatch(ContinueQuitClose);
		}

		private DocumentWindow FirstDirtyDocumentWindow()
		{
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index] as DocumentWindow;
				if (window == null)
				{
					continue;
				}
				Document model = window.DocumentModel();
				if (model != null && model.IsDirty())
				{
					return window;
				}
			}
			return null;
		}

		private void ContinueQuitClose()
		{
			if (!m_quitPending)
			{
				return;
			}
			DocumentWindow dirty = FirstDirtyDocumentWindow();
			if (dirty == null)
			{
				m_quitPending = false;
				m_quitConfirmed = true;
				if (m_nativeWindow != null)
				{
					m_nativeWindow.Close();
				}
				return;
			}
			ClosePanel(dirty);
		}

		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();
			Dispatcher.Dispatch(HookAppWindowClosing);
			if (m_acceleratorsHooked)
			{
				return;
			}
			if (Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			AddAccelerator(element, Windows.System.VirtualKey.N, OnAcceleratorNew);
			AddAccelerator(element, Windows.System.VirtualKey.O, OnAcceleratorOpen);
			AddAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorSave);
			AddAccelerator(element, Windows.System.VirtualKey.Z, OnAcceleratorUndo);
			AddAccelerator(element, Windows.System.VirtualKey.Y, OnAcceleratorRedo);
			AddAccelerator(element, Windows.System.VirtualKey.A, OnAcceleratorSelectAll);
			AddAccelerator(element, Windows.System.VirtualKey.D, OnAcceleratorDeselect);
			AddAccelerator(element, Windows.System.VirtualKey.C, OnAcceleratorCopy);
			AddAccelerator(element, Windows.System.VirtualKey.V, OnAcceleratorPaste);
			AddAccelerator(element, Windows.System.VirtualKey.X, OnAcceleratorCut);
			AddAccelerator(element, Windows.System.VirtualKey.Number0, OnAcceleratorFit);
			AddAccelerator(element, Windows.System.VirtualKey.Add, OnAcceleratorZoomIn);
			AddAccelerator(element, Windows.System.VirtualKey.Subtract, OnAcceleratorZoomOut);
			AddAccelerator(element, (Windows.System.VirtualKey)187, OnAcceleratorZoomIn);
			AddAccelerator(element, (Windows.System.VirtualKey)189, OnAcceleratorZoomOut);
			AddCtrlShiftAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorSaveAs);
			AddAccelerator(element, Windows.System.VirtualKey.E, OnAcceleratorMergeSelected);
			AddCtrlShiftAccelerator(element, Windows.System.VirtualKey.E, OnAcceleratorMergeVisible);
			AddCtrlAltShiftAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorExport);
			AddCtrlAltAccelerator(element, Windows.System.VirtualKey.I, OnAcceleratorImageSize);
			AddAccelerator(element, Windows.System.VirtualKey.I, OnAcceleratorInvertColors);
			AddCtrlShiftAccelerator(element, Windows.System.VirtualKey.I, OnAcceleratorInvertSelection);
			AddAccelerator(element, Windows.System.VirtualKey.R, OnAcceleratorRulers);
			AddAccelerator(element, Windows.System.VirtualKey.T, OnAcceleratorTransform);
			AddAccelerator(element, Windows.System.VirtualKey.F, OnAcceleratorLastFilter);
			AddBareAccelerator(element, Windows.System.VirtualKey.Enter, OnAcceleratorCommitTransform);
			AddBareAccelerator(element, Windows.System.VirtualKey.Escape, OnAcceleratorCancelTransform);
			AddBareAccelerator(element, Windows.System.VirtualKey.X, OnAcceleratorSwapColors);
			AddBareAccelerator(element, Windows.System.VirtualKey.Delete, OnAcceleratorDelete);
			AddAccelerator(element, Windows.System.VirtualKey.Delete, OnAcceleratorDeleteBackground);
			AddAltAccelerator(element, Windows.System.VirtualKey.Delete, OnAcceleratorDeleteForeground);
			element.KeyboardAcceleratorPlacementMode = Microsoft.UI.Xaml.Input.KeyboardAcceleratorPlacementMode.Hidden;
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnGlobalPointerPressed), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnGlobalPointerMoved), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnGlobalPointerReleased), true);
			element.AllowDrop = true;
			element.DragOver += OnElementDragOver;
			element.Drop += OnElementDrop;
			m_acceleratorsHooked = true;
		}

		private void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			if (m_pulldownPanel == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			Windows.Foundation.Point position = args.GetCurrentPoint(element).Position;
			double panelX = m_pulldownPanel.X;
			double panelY = m_pulldownPanel.Y;
			double panelWidth = m_pulldownPanel.Width;
			double panelHeight = m_pulldownPanel.Height;
			if (position.X >= panelX && position.X <= panelX + panelWidth && position.Y >= panelY && position.Y <= panelY + panelHeight)
			{
				return;
			}
			m_pulldownDismissTick = System.Environment.TickCount64;
			ClosePulldown();
		}

		public void BeginGuideCreation(int orientation, CanvasView canvas)
		{
			if (canvas == null)
			{
				return;
			}
			if (canvas.CurrentDocument().Guides().IsLocked())
			{
				return;
			}
			ActivateDocumentWindow(canvas.OwnerWindow());
			m_guideCreateOrientation = orientation;
			m_guideCreateCanvas = canvas;
			canvas.ResetGuideStickyCache();
		}

		private void OnGlobalPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			if (m_guideCreateOrientation == 0)
			{
				return;
			}
			CanvasView canvas = m_guideCreateCanvas;
			if (canvas == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement canvasElement = canvas.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (canvasElement == null)
			{
				return;
			}
			Windows.Foundation.Point position = args.GetCurrentPoint(canvasElement).Position;
			canvas.UpdatePendingGuideFromDip(m_guideCreateOrientation, position.X, position.Y);
		}

		private void OnGlobalPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			if (m_guideCreateOrientation == 0)
			{
				return;
			}
			CanvasView canvas = m_guideCreateCanvas;
			m_guideCreateOrientation = 0;
			m_guideCreateCanvas = null;
			if (canvas != null)
			{
				canvas.CommitPendingGuide();
			}
		}

		private void OnElementDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs args)
		{
			if (args.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
			{
				args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
			}
		}

		private async void OnElementDrop(object sender, Microsoft.UI.Xaml.DragEventArgs args)
		{
			if (!args.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
			{
				return;
			}
			System.Collections.Generic.IReadOnlyList<Windows.Storage.IStorageItem> items = await args.DataView.GetStorageItemsAsync();
			for (int index = 0; index < items.Count; index++)
			{
				Windows.Storage.StorageFile file = items[index] as Windows.Storage.StorageFile;
				if (file != null)
				{
					OpenDocumentFromPath(file.Path);
				}
			}
		}

		private void AddAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void AddBareAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.None;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void AddAltAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Menu;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void AddCtrlShiftAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Shift;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void AddCtrlAltAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Menu;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void AddCtrlAltShiftAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Menu | Windows.System.VirtualKeyModifiers.Shift;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void OnAcceleratorSwapColors(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			if (m_toolPalette != null)
			{
				m_toolPalette.SwapColors();
			}
			args.Handled = true;
		}

		private void OnAcceleratorDelete(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoClearSelection();
			args.Handled = true;
		}

		private void OnAcceleratorDeleteForeground(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			SKColor foreground = m_toolState.Foreground();
			FillSelectionWith(new SKColor(foreground.Red, foreground.Green, foreground.Blue, 255), true);
			args.Handled = true;
		}

		private void OnAcceleratorDeleteBackground(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			SKColor background = m_toolState.Background();
			FillSelectionWith(new SKColor(background.Red, background.Green, background.Blue, 255), true);
			args.Handled = true;
		}

		private void DoClearSelection()
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			SKColor fill = new SKColor(0, 0, 0, 0);
			if (layer.IsBackground())
			{
				SKColor background = m_toolState.Background();
				fill = new SKColor(background.Red, background.Green, background.Blue, 255);
			}
			FillSelectionWith(fill, false);
		}

		private void FillSelectionWith(SKColor fill, bool fillLayerWhenEmpty)
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null || layer.IsText())
			{
				return;
			}
			if (layer.PaintLocked())
			{
				SetStatusMessage("Layer is locked");
				return;
			}
			Selection selection = document.Selection();
			bool hasSelection = selection.IsActive();
			if (!hasSelection && !fillLayerWhenEmpty)
			{
				return;
			}
			document.BeginStroke();
			if (hasSelection)
			{
				document.FillSelection(fill);
			}
			else
			{
				document.FillLayer(fill);
			}
			document.EndStroke();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.InvalidateSurface();
			}
			RefreshLayerThumbnails();
		}

		private void OnAcceleratorNew(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			ShowNewDocumentDialog();
			args.Handled = true;
		}

		private void OnAcceleratorOpen(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			OpenImageFlow();
			args.Handled = true;
		}

		private void OnAcceleratorSave(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			SaveImageFlow();
			args.Handled = true;
		}

		private void OnAcceleratorUndo(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoUndo();
			args.Handled = true;
		}

		private void OnAcceleratorRedo(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoRedo();
			args.Handled = true;
		}

		private void OnAcceleratorSelectAll(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoSelectAll();
			args.Handled = true;
		}

		private void OnAcceleratorDeselect(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoDeselect();
			args.Handled = true;
		}

		private void OnAcceleratorCopy(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoCopy();
			args.Handled = true;
		}

		private void OnAcceleratorPaste(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoPaste();
			args.Handled = true;
		}

		private void OnAcceleratorCut(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoCut();
			args.Handled = true;
		}

		private void OnAcceleratorFit(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoFit();
			args.Handled = true;
		}

		private void OnAcceleratorZoomIn(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoZoomIn();
			args.Handled = true;
		}

		private void OnAcceleratorZoomOut(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			DoZoomOut();
			args.Handled = true;
		}

		private void OnAcceleratorSaveAs(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			SaveAsFlow();
			args.Handled = true;
		}

		private void OnAcceleratorExport(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			OpenExportDialog();
			args.Handled = true;
		}

		private void OnAcceleratorImageSize(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			OpenSizeDialog(false);
			args.Handled = true;
		}

		private void OnAcceleratorInvertSelection(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoInvertSelection();
			args.Handled = true;
		}

		private void OnAcceleratorLastFilter(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			ApplyLastFilter();
			args.Handled = true;
		}

		private void OnAcceleratorInvertColors(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoInvert();
			args.Handled = true;
		}

		private void OnAcceleratorMergeSelected(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			MergeSelectedLayers();
			args.Handled = true;
		}

		private void OnAcceleratorMergeVisible(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			DoMergeVisible();
			args.Handled = true;
		}

		public void MergeSelectedLayers()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			List<int> selected = document.SelectedLayerIndices();
			if (selected.Count >= 2)
			{
				document.BeginCanvasEdit("Merge Layers");
				document.MergeLayers(selected);
				document.EndCanvasEdit();
				FinishLayerStructureChange(canvas);
			}
			else
			{
				DoMergeDown();
			}
		}

		private void DoMergeDown()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document.ActiveLayerIndex() <= 0)
			{
				return;
			}
			document.BeginCanvasEdit("Merge Down");
			document.MergeDown(document.ActiveLayerIndex());
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		private void DoMergeVisible()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.BeginCanvasEdit("Merge Visible");
			document.MergeVisible();
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		private void DoFlattenImage()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.BeginCanvasEdit("Flatten Image");
			document.FlattenImage();
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		private void FinishLayerStructureChange(CanvasView canvas)
		{
			canvas.MarkComposeDirty();
			RefreshPanels();
		}

		public void DuplicateActiveLayer()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.BeginCanvasEdit("Duplicate Layer");
			document.DuplicateLayer(document.ActiveLayerIndex());
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		public void AddNewLayer()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document == null)
			{
				return;
			}
			int layerNumber = document.Layers().Count + 1;
			document.BeginCanvasEdit("Add Layer");
			document.AddLayer("Layer " + layerNumber);
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		public void DeleteActiveLayer()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			document.BeginCanvasEdit("Delete Layer");
			document.DeleteLayer(document.ActiveLayerIndex());
			document.EndCanvasEdit();
			FinishLayerStructureChange(canvas);
		}

		public void RequestDeleteActiveLayer()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			if (document.Layers().Count <= 1)
			{
				return;
			}
			ShowModal(new MessageDialog("Delete Layer", "Delete layer \"" + layer.Name() + "\"?", new string[] { "Cancel", "Delete" }, OnDeleteLayerChoice), 320.0, 150.0);
		}

		private void OnDeleteLayerChoice(int choice)
		{
			if (choice == 1)
			{
				DeleteActiveLayer();
			}
		}

		private Border BuildContextMenuRow(string text, EventHandler<TappedEventArgs> handler)
		{
			Label label = new Label();
			label.Text = text;
			label.FontSize = UiConstants.PanelFontSize;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;

			Border row = new Border();
			row.HeightRequest = MenuBar.MenuItemHeight;
			row.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
			row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			row.StrokeThickness = 0.0;
			row.Content = label;
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += handler;
			row.GestureRecognizers.Add(tap);
			return row;
		}

		public void ShowLayerContextMenu(int layerIndex, double anchorX, double anchorY)
		{
			VerticalStackLayout menu = new VerticalStackLayout();
			menu.Spacing = 0.0;
			menu.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);
			menu.Add(BuildContextMenuRow("Layer Style…", OnContextLayerStyle));
			menu.Add(BuildContextMenuRow("Copy Layer Style", OnContextCopyLayerStyle));
			menu.Add(BuildContextMenuRow("Paste Layer Style", OnContextPasteLayerStyle));
			menu.Add(BuildContextMenuRow("Layer Properties…", OnContextLayerProperties));
			menu.Add(BuildContextMenuRow("Duplicate Layer", OnContextDuplicateLayer));
			menu.Add(BuildContextMenuRow("Merge Down", OnContextMergeDown));
			menu.Add(BuildContextMenuRow("Rasterize Text", OnContextRasterizeText));
			menu.Add(MenuBar.BuildMenuSeparator());
			menu.Add(BuildContextMenuRow("Delete Layer", OnContextDeleteLayer));
			double height = (8.0 * MenuBar.MenuItemHeight) + MenuBar.MenuSeparatorHeight + 8.0;
			ShowPulldown(menu, anchorX, anchorY, MenuBar.DropdownWidth, height);
		}

		private void OnContextLayerStyle(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			OpenLayerStyleDialog();
		}

		private void OnContextCopyLayerStyle(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			m_copiedLayerStyle = layer.LayerStyle().Clone();
		}

		private void OnContextPasteLayerStyle(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			if (m_copiedLayerStyle == null)
			{
				return;
			}
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			document.BeginCanvasEdit("Paste Layer Style");
			layer.SetLayerStyle(m_copiedLayerStyle.Clone());
			document.EndCanvasEdit();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
			RefreshPanels();
		}

		private void OnContextLayerProperties(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			OpenLayerPropertiesDialog();
		}

		private void OnContextDuplicateLayer(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			DuplicateActiveLayer();
		}

		private void OnContextMergeDown(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			DoMergeDown();
		}

		private void OnContextRasterizeText(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			DoRasterizeText();
		}

		private void OnContextDeleteLayer(object sender, TappedEventArgs eventArgs)
		{
			ClosePulldown();
			RequestDeleteActiveLayer();
		}

		private void OnAcceleratorRulers(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			ToggleRulers();
			args.Handled = true;
		}

		private void OnAcceleratorTransform(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			BeginTransform(0);
			args.Handled = true;
		}

		private bool TransformActive()
		{
			if (m_toolState == null || m_toolBox == null)
			{
				return false;
			}
			return m_toolState.Tool() == eTool.FreeTransform && m_toolBox.FreeTransform().HasPreview();
		}

		private void RefreshTransformCanvas()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
			RefreshLayerThumbnails();
		}

		private void OnAcceleratorCommitTransform(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			if (!TransformActive())
			{
				return;
			}
			Document document = ActiveDocument();
			if (document != null)
			{
				m_toolBox.FreeTransform().Commit(document);
			}
			EndTransformMode();
			RefreshTransformCanvas();
			args.Handled = true;
		}

		private void OnAcceleratorCancelTransform(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (IsTextEditActive())
			{
				return;
			}
			if (m_modalStack.Count > 0)
			{
				CloseModal();
				args.Handled = true;
				return;
			}
			if (!TransformActive())
			{
				return;
			}
			m_toolBox.FreeTransform().Cancel();
			EndTransformMode();
			RefreshTransformCanvas();
			args.Handled = true;
		}

		private double WindowChromeWidth()
		{
			double rulerWidth = 0.0;
			if (m_rulersEnabled)
			{
				rulerWidth = UiConstants.RulerThickness;
			}
			return rulerWidth + UiConstants.ResizeGripSize + (2.0 * UiConstants.PanelBorderThickness);
		}

		private double WindowChromeHeight()
		{
			double rulerHeight = 0.0;
			if (m_rulersEnabled)
			{
				rulerHeight = UiConstants.RulerThickness;
			}
			return UiConstants.TitleBarHeight + rulerHeight + UiConstants.DocumentBottomBar + (2.0 * UiConstants.PanelBorderThickness);
		}

		private void PlaceAndAdd(DocumentWindow window)
		{
			double workspaceWidth = WorkspaceWidth();
			double workspaceHeight = WorkspaceHeight();
			double width = UiConstants.DefaultDocumentWindowWidth;
			double height = UiConstants.DefaultDocumentWindowHeight;
			Document model = window.DocumentModel();
			if (model != null)
			{
				double density = Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;
				if (density < 0.1)
				{
					density = 1.0;
				}
				double canvasDipWidth = model.Width() / density;
				double canvasDipHeight = model.Height() / density;
				width = System.Math.Ceiling(canvasDipWidth) + WindowChromeWidth() + 2.0;
				height = System.Math.Ceiling(canvasDipHeight) + WindowChromeHeight() + 2.0;
				if (width < UiConstants.PanelMinWidth)
				{
					width = UiConstants.PanelMinWidth;
				}
				if (height < UiConstants.PanelMinHeight)
				{
					height = UiConstants.PanelMinHeight;
				}
			}
			if (workspaceWidth > 100.0 && workspaceHeight > 100.0)
			{
				double maximumWidth = workspaceWidth - 16.0;
				double maximumHeight = workspaceHeight - 16.0;
				if (width > maximumWidth)
				{
					width = maximumWidth;
				}
				if (height > maximumHeight)
				{
					height = maximumHeight;
				}
			}
			double offset = m_cascadeCount * UiConstants.CascadeOffset;
			m_cascadeCount++;
			double x = 20.0 + offset;
			double y = 16.0 + offset;
			if (workspaceWidth > 100.0 && x + width > workspaceWidth - 8.0)
			{
				x = workspaceWidth - 8.0 - width;
				if (x < 8.0)
				{
					x = 8.0;
				}
			}
			if (workspaceHeight > 100.0 && y + height > workspaceHeight - 8.0)
			{
				y = workspaceHeight - 8.0 - height;
				if (y < 8.0)
				{
					y = 8.0;
				}
			}
			AddDocument(window, x, y, width, height);
		}

		private System.Collections.Generic.List<DocumentWindow> DocumentWindows()
		{
			System.Collections.Generic.List<DocumentWindow> windows = new System.Collections.Generic.List<DocumentWindow>();
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index] as DocumentWindow;
				if (window != null)
				{
					windows.Add(window);
				}
			}
			return windows;
		}

		private void DoCascadeWindows()
		{
			System.Collections.Generic.List<DocumentWindow> windows = DocumentWindows();
			double workspaceWidth = WorkspaceWidth();
			double workspaceHeight = WorkspaceHeight();
			if (workspaceWidth <= 100.0 || workspaceHeight <= 100.0)
			{
				return;
			}
			double width = workspaceWidth * 0.72;
			double height = workspaceHeight * 0.74;
			for (int index = 0; index < windows.Count; index++)
			{
				double offset = index * UiConstants.CascadeOffset;
				windows[index].SetBounds(20.0 + offset, 16.0 + offset, width, height);
				BringToFront(windows[index]);
			}
			m_cascadeCount = windows.Count;
		}

		private void DoTileWindows()
		{
			System.Collections.Generic.List<DocumentWindow> windows = DocumentWindows();
			int count = windows.Count;
			if (count == 0)
			{
				return;
			}
			double workspaceWidth = WorkspaceWidth();
			double workspaceHeight = WorkspaceHeight();
			if (workspaceWidth <= 100.0 || workspaceHeight <= 100.0)
			{
				return;
			}
			int columns = (int)System.Math.Ceiling(System.Math.Sqrt(count));
			int rows = (int)System.Math.Ceiling((double)count / columns);
			double cellWidth = workspaceWidth / columns;
			double cellHeight = workspaceHeight / rows;
			for (int index = 0; index < count; index++)
			{
				int row = index / columns;
				int column = index % columns;
				windows[index].SetBounds(column * cellWidth, row * cellHeight, cellWidth, cellHeight);
				BringToFront(windows[index]);
			}
		}

		private async void OpenImageFlow()
		{
			try
			{
				string path = await FileDialogs.PickOpenAsync();
				if (path == null)
				{
					return;
				}
				OpenDocumentFromPath(path);
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Open failed: " + error.Message);
			}
		}

		public void OpenDocumentFromPath(string path)
		{
			try
			{
				if (path.ToLowerInvariant().EndsWith(".bitmute"))
				{
					Document project = BitmuteFile.Read(path);
					if (project == null)
					{
						SetStatusMessage("Failed to open project");
						return;
					}
					project.SetSourcePath(path);
					RecentFiles.Add(path);
					DocumentWindow projectWindow = new DocumentWindow(project);
					PlaceAndAdd(projectWindow);
					return;
				}
				SkiaSharp.SKBitmap bitmap = ImageFile.DecodeFile(path);
				if (bitmap == null)
				{
					SetStatusMessage("Failed to open image");
					return;
				}
				string title = System.IO.Path.GetFileName(path);
				Document model = Document.OpenImage(title, bitmap);
				model.SetSourcePath(path);
				RecentFiles.Add(path);
				bitmap.Dispose();
				DocumentWindow window = new DocumentWindow(model);
				PlaceAndAdd(window);
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Open failed: " + error.Message);
			}
		}

		private async void SaveImageFlow()
		{
			Document model = ActiveDocument();
			if (model == null)
			{
				return;
			}
			await SaveDocumentAsync(model);
		}

		private static string SuggestedSaveName(Document model)
		{
			string title = model.Title();
			if (title == null || title.Length == 0)
			{
				return "Untitled";
			}
			return System.IO.Path.GetFileNameWithoutExtension(title);
		}

		private async void SaveAsFlow()
		{
			Document model = ActiveDocument();
			if (model == null)
			{
				SetStatusMessage("No document to save");
				return;
			}
			try
			{
				string path = await FileDialogs.PickSaveAsync(SuggestedSaveName(model));
				if (path == null)
				{
					return;
				}
				bool success = WriteDocumentTo(model, path);
				if (!success)
				{
					SetStatusMessage("Save failed");
					return;
				}
				model.SetSourcePath(path);
				RecentFiles.Add(path);
				model.MarkClean();
				SetStatusMessage("Saved " + System.IO.Path.GetFileName(path));
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Save failed: " + error.Message);
			}
		}

		private static bool WriteDocumentTo(Document model, string path)
		{
			if (path.ToLowerInvariant().EndsWith(".bitmute"))
			{
				return BitmuteFile.Write(path, model);
			}
			ImageFile.Encode(model, path);
			return true;
		}

		private async System.Threading.Tasks.Task<bool> SaveAsBitmuteAsync(Document model)
		{
			string path = await FileDialogs.PickSaveTypedAsync(SuggestedSaveName(model), "Bitmute Project", ".bitmute");
			if (path == null)
			{
				return false;
			}
			bool success = BitmuteFile.Write(path, model);
			if (!success)
			{
				SetStatusMessage("Save failed");
				return false;
			}
			model.SetSourcePath(path);
			RecentFiles.Add(path);
			model.MarkClean();
			SetStatusMessage("Saved " + System.IO.Path.GetFileName(path));
			return true;
		}

		private void OpenExportDialog()
		{
			if (ActiveDocument() == null)
			{
				SetStatusMessage("No document to export");
				return;
			}
			ShowModal(new ExportDialog(), 340.0, 280.0);
		}

		private static string ExportLabel(string format)
		{
			if (format == "jpeg")
			{
				return "JPEG Image";
			}
			if (format == "bmp")
			{
				return "Bitmap Image";
			}
			if (format == "tga")
			{
				return "TGA Image";
			}
			if (format == "webp")
			{
				return "WebP Image";
			}
			return "PNG Image";
		}

		private static string ExportExtension(string format)
		{
			if (format == "jpeg")
			{
				return ".jpg";
			}
			if (format == "bmp")
			{
				return ".bmp";
			}
			if (format == "tga")
			{
				return ".tga";
			}
			if (format == "webp")
			{
				return ".webp";
			}
			return ".png";
		}

		public async void ConfirmExport(string format, int quality, bool lossless, bool rle)
		{
			CloseModal();
			Document model = ActiveDocument();
			if (model == null)
			{
				SetStatusMessage("No document to export");
				return;
			}
			model.CommitFloatingSelection();
			try
			{
				string path = await FileDialogs.PickSaveTypedAsync(model.Title(), ExportLabel(format), ExportExtension(format));
				if (path == null)
				{
					return;
				}
				bool success = ImageFile.Export(model, path, format, quality, lossless, rle);
				if (success)
				{
					SetStatusMessage("Exported " + System.IO.Path.GetFileName(path));
				}
				else
				{
					SetStatusMessage("Export failed");
				}
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Export failed: " + error.Message);
			}
		}

		public void ClearRecentFiles()
		{
			RecentFiles.Clear();
			SetStatusMessage("Recent files cleared");
		}

		public int CurrentUndoDepth()
		{
			return Document.MaxUndoDepth();
		}

		public void ApplyUndoDepth(int depth)
		{
			Document.SetMaxUndoDepth(depth);
			Microsoft.Maui.Storage.Preferences.Default.Set("undo_depth", Document.MaxUndoDepth());
		}

		public async void OpenRepoLink()
		{
			try
			{
				await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(new System.Uri("https://github.com/therobm/Bitmute"));
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Could not open link: " + error.Message);
			}
		}

		private async System.Threading.Tasks.Task<bool> SaveDocumentAsync(Document model)
		{
			model.CommitFloatingSelection();
			try
			{
				string sourcePath = model.SourcePath();
				if (sourcePath == null || sourcePath.Length == 0)
				{
					return await SaveAsBitmuteAsync(model);
				}
				if (sourcePath.ToLowerInvariant().EndsWith(".bitmute"))
				{
					bool projectSaved = BitmuteFile.Write(sourcePath, model);
					if (!projectSaved)
					{
						SetStatusMessage("Save failed");
						return false;
					}
					model.MarkClean();
					SetStatusMessage("Saved " + System.IO.Path.GetFileName(sourcePath));
					return true;
				}
				if (model.FlatCompatible())
				{
					ImageFile.Encode(model, sourcePath);
					model.MarkClean();
					SetStatusMessage("Saved " + System.IO.Path.GetFileName(sourcePath));
					return true;
				}
				SetStatusMessage("Document no longer fits " + System.IO.Path.GetExtension(sourcePath) + " — saving as project");
				return await SaveAsBitmuteAsync(model);
			}
			catch (System.Exception error)
			{
				SetStatusMessage("Save failed: " + error.Message);
				return false;
			}
		}

		public void SetStatusMessage(string message)
		{
			if (m_statusCursorLabel != null)
			{
				m_statusCursorLabel.Text = message;
			}
		}

		public void AddDocument(FloatingPanel panel, double x, double y, double width, double height)
		{
			m_documents.Add(panel);
			m_workspace.Add(panel);
			panel.SetBounds(x, y, width, height);
			BringToFront(panel);
		}

		public void BringToFront(FloatingPanel panel)
		{
			m_topZIndex++;
			panel.ZIndex = m_topZIndex;
			DocumentWindow window = panel as DocumentWindow;
			if (window != null)
			{
				m_activeDocumentWindow = window;
				RefreshDocumentTitleBars();
				RefreshPanels();
			}
		}

		private void RefreshDocumentTitleBars()
		{
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index] as DocumentWindow;
				if (window == null)
				{
					continue;
				}
				window.SetTitleBarActive(window == m_activeDocumentWindow);
			}
		}

		public CanvasView ActiveCanvas()
		{
			if (m_activeDocumentWindow == null)
			{
				return null;
			}
			return m_activeDocumentWindow.Canvas();
		}

		public void ActivateDocumentWindow(DocumentWindow window)
		{
			if (window == null)
			{
				return;
			}
			if (m_activeDocumentWindow == window)
			{
				return;
			}
			BringToFront(window);
		}

		public Document ActiveDocument()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return null;
			}
			return canvas.CurrentDocument();
		}

		public void RefreshPanels()
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
			if (m_navigatorPanel != null)
			{
				m_navigatorPanel.RefreshView();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void OnPaletteTabChanged()
		{
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void OnCanvasInteracted()
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
		}

		private void OnModalBackdropTapped(object sender, TappedEventArgs eventArgs)
		{
			CloseModal();
		}

		private void OnWorkspaceDoubleTapped(object sender, TappedEventArgs eventArgs)
		{
			OpenImageFlow();
		}

		private void OnWorkspaceSizeChanged(object sender, EventArgs eventArgs)
		{
			double width = m_workspace.Width;
			double height = m_workspace.Height;
			if (width <= 0.0 || height <= 0.0)
			{
				return;
			}
			RectangleGeometry geometry = new RectangleGeometry();
			geometry.Rect = new Rect(0.0, 0.0, width, height);
			m_workspace.Clip = geometry;
		}

		public void ShowModal(View content, double width, double height)
		{
			BoxView backdrop = new BoxView();
			backdrop.Color = Colors.Transparent;
			AbsoluteLayout.SetLayoutBounds(backdrop, new Rect(0.0, 0.0, 1.0, 1.0));
			AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
			TapGestureRecognizer backdropTap = new TapGestureRecognizer();
			backdropTap.Tapped += OnModalBackdropTapped;
			backdrop.GestureRecognizers.Add(backdropTap);
			m_topZIndex = m_topZIndex + 1;
			backdrop.ZIndex = m_topZIndex + 1000;
			m_workspace.Add(backdrop);

			ModalEntry entry = new ModalEntry();
			entry.m_content = content;
			entry.m_backdrop = backdrop;
			entry.m_width = width;
			entry.m_height = height;
			entry.m_x = (m_workspace.Width - width) / 2.0;
			entry.m_y = (m_workspace.Height - height) / 2.0;
			if (entry.m_x < 0.0)
			{
				entry.m_x = 0.0;
			}
			if (entry.m_y < 0.0)
			{
				entry.m_y = 0.0;
			}
			m_modalStack.Add(entry);

			AbsoluteLayout.SetLayoutBounds(content, new Rect(entry.m_x, entry.m_y, width, AbsoluteLayout.AutoSize));
			AbsoluteLayout.SetLayoutFlags(content, AbsoluteLayoutFlags.None);
			content.ZIndex = m_topZIndex + 1001;
			m_workspace.Add(content);
		}

		public void DragModal(Microsoft.Maui.GestureStatus status, double totalX, double totalY)
		{
			if (m_modalStack.Count == 0)
			{
				return;
			}
			ModalEntry entry = m_modalStack[m_modalStack.Count - 1];
			if (status == Microsoft.Maui.GestureStatus.Started)
			{
				entry.m_dragOriginX = entry.m_x;
				entry.m_dragOriginY = entry.m_y;
				return;
			}
			if (status != Microsoft.Maui.GestureStatus.Running)
			{
				return;
			}
			double targetX = entry.m_dragOriginX + totalX;
			double targetY = entry.m_dragOriginY + totalY;
			double maxX = m_workspace.Width - entry.m_width;
			double maxY = m_workspace.Height - entry.m_height;
			if (targetX < 0.0)
			{
				targetX = 0.0;
			}
			if (targetY < 0.0)
			{
				targetY = 0.0;
			}
			if (maxX >= 0.0 && targetX > maxX)
			{
				targetX = maxX;
			}
			if (maxY >= 0.0 && targetY > maxY)
			{
				targetY = maxY;
			}
			entry.m_x = targetX;
			entry.m_y = targetY;
			AbsoluteLayout.SetLayoutBounds(entry.m_content, new Rect(entry.m_x, entry.m_y, entry.m_width, AbsoluteLayout.AutoSize));
		}

		public void CloseModal()
		{
			m_editingSwatchIndex = -1;
			if (m_modalStack.Count == 0)
			{
				return;
			}
			ModalEntry entry = m_modalStack[m_modalStack.Count - 1];
			m_modalStack.RemoveAt(m_modalStack.Count - 1);
			if (entry.m_backdrop != null)
			{
				m_workspace.Remove(entry.m_backdrop);
			}
			if (entry.m_content != null)
			{
				m_workspace.Remove(entry.m_content);
			}
			if (entry.m_content is SaveChangesDialog)
			{
				m_quitPending = false;
			}
			ColorPicker cancelledPicker = entry.m_content as ColorPicker;
			if (cancelledPicker != null)
			{
				cancelledPicker.RevertLivePreview();
			}
			PreviewDialog previewDialog = entry.m_content as PreviewDialog;
			if (previewDialog != null)
			{
				previewDialog.CancelPreview();
			}
			if (entry.m_content is LayerStyleDialog && m_layerStyleSnapshot != null)
			{
				Layer layer = LayerStyleTargetLayer();
				if (layer != null)
				{
					layer.SetLayerStyle(m_layerStyleSnapshot);
					CanvasView canvas = ActiveCanvas();
					if (canvas != null)
					{
						canvas.MarkComposeDirty();
						canvas.InvalidateSurface();
					}
				}
				m_layerStyleSnapshot = null;
			}
		}

		public void OpenColorPicker(bool foreground)
		{
			m_editingSwatchIndex = -1;
			SKColor initial = m_toolState.Background();
			if (foreground)
			{
				initial = m_toolState.Foreground();
			}
			ColorPicker picker = new ColorPicker(initial, foreground);
			ShowModal(picker, 380.0, 360.0);
		}

		public void OpenSwatchColorPicker(int index, SKColor current)
		{
			ColorPicker picker = new ColorPicker(current, true);
			ShowModal(picker, 380.0, 360.0);
			m_editingSwatchIndex = index;
		}

		public void SetLiveForeground(SKColor color)
		{
			m_toolState.SetForeground(color);
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
			if (m_optionsBar != null)
			{
				m_optionsBar.UpdateTextColorSwatch(color);
			}
			RefreshTextEditStyle();
		}

		public void SetLiveBackground(SKColor color)
		{
			m_toolState.SetBackground(color);
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
		}

		public bool EditingSwatch()
		{
			return m_editingSwatchIndex >= 0;
		}

		public void ApplyPickedColor(SKColor color, bool foreground)
		{
			if (m_editingSwatchIndex >= 0)
			{
				int target = m_editingSwatchIndex;
				m_editingSwatchIndex = -1;
				if (m_swatchesPanel != null)
				{
					m_swatchesPanel.SetSwatchColor(target, color);
				}
				return;
			}
			if (foreground)
			{
				m_toolState.SetForeground(color);
			}
			else
			{
				m_toolState.SetBackground(color);
			}
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
			if (foreground && m_optionsBar != null)
			{
				m_optionsBar.UpdateTextColorSwatch(color);
			}
			if (m_swatchesPanel != null)
			{
				m_swatchesPanel.AddRecent(color);
			}
			RefreshTextEditStyle();
		}

		public void ShowNewDocumentDialog()
		{
			NewDocumentDialog dialog = new NewDocumentDialog();
			ShowModal(dialog, 320.0, 280.0);
		}

		public void CreateNewDocument(int width, int height, string name, bool transparent)
		{
			m_untitledCount = m_untitledCount + 1;
			string title = name;
			if (title == null || title.Length == 0)
			{
				title = "Untitled-" + m_untitledCount;
			}
			Document model = new Document(title, width, height);
			if (transparent)
			{
				Layer background = model.ActiveLayer();
				background.Bitmap().Erase(SKColors.Transparent);
				background.SetIsBackground(false);
				background.SetName("Layer 1");
			}
			DocumentWindow window = new DocumentWindow(model);
			PlaceAndAdd(window);
		}

		public void RefreshActiveLayerThumbnail()
		{
			if (m_layersPanel != null)
			{
				m_layersPanel.RefreshActiveThumbnail();
			}
			if (m_navigatorPanel != null)
			{
				m_navigatorPanel.RefreshView();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void RefreshLayerThumbnails()
		{
			if (m_layersPanel != null)
			{
				m_layersPanel.RefreshThumbnails();
			}
			if (m_navigatorPanel != null)
			{
				m_navigatorPanel.RefreshView();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void ClosePanel(FloatingPanel panel)
		{
			DocumentWindow window = panel as DocumentWindow;
			if (window != null)
			{
				Document model = window.DocumentModel();
				if (model != null && model.IsDirty())
				{
					m_pendingClosePanel = panel;
					ShowModal(new SaveChangesDialog(model.Title()), 360.0, 170.0);
					return;
				}
			}
			RemovePanel(panel);
		}

		public void OnCloseSaveChanges()
		{
			bool quitting = m_quitPending;
			CloseModal();
			m_quitPending = quitting;
			FloatingPanel panel = m_pendingClosePanel;
			m_pendingClosePanel = null;
			if (panel == null)
			{
				return;
			}
			SaveThenClose(panel);
		}

		private async void SaveThenClose(FloatingPanel panel)
		{
			DocumentWindow window = panel as DocumentWindow;
			if (window == null)
			{
				return;
			}
			Document model = window.DocumentModel();
			if (model == null)
			{
				RemovePanel(panel);
				return;
			}
			bool saved = await SaveDocumentAsync(model);
			if (saved)
			{
				RemovePanel(panel);
				return;
			}
			m_quitPending = false;
		}

		public void OnCloseDontSave()
		{
			bool quitting = m_quitPending;
			CloseModal();
			m_quitPending = quitting;
			FloatingPanel panel = m_pendingClosePanel;
			m_pendingClosePanel = null;
			if (panel != null)
			{
				RemovePanel(panel);
			}
		}

		public void OnCloseCancelSave()
		{
			CloseModal();
			m_pendingClosePanel = null;
			m_quitPending = false;
		}

		private DocumentWindow TopmostDocumentWindow()
		{
			DocumentWindow topmost = null;
			for (int index = 0; index < m_documents.Count; index++)
			{
				DocumentWindow window = m_documents[index] as DocumentWindow;
				if (window == null)
				{
					continue;
				}
				if (topmost == null || window.ZIndex >= topmost.ZIndex)
				{
					topmost = window;
				}
			}
			return topmost;
		}

		private void ClearClosedDocumentReadouts()
		{
			if (m_statusInfoLabel != null)
			{
				m_statusInfoLabel.Text = "";
			}
			if (m_statusCursorLabel != null)
			{
				m_statusCursorLabel.Text = "x: —   y: —";
			}
			if (m_infoPanel != null)
			{
				m_infoPanel.ClearReadout();
			}
		}

		private void RemovePanel(FloatingPanel panel)
		{
			if (!m_documents.Contains(panel))
			{
				return;
			}
			m_documents.Remove(panel);
			m_workspace.Remove(panel);
			DocumentWindow window = panel as DocumentWindow;
			if (window != null && window.Canvas() != null)
			{
				window.Canvas().ReleaseGpuResources();
			}
			if (window != null && m_activeDocumentWindow == window)
			{
				m_activeDocumentWindow = null;
				DocumentWindow next = TopmostDocumentWindow();
				if (next != null)
				{
					BringToFront(next);
				}
				else
				{
					RefreshDocumentTitleBars();
					RefreshPanels();
					ClearClosedDocumentReadouts();
				}
			}
			if (m_quitPending)
			{
				Dispatcher.Dispatch(ContinueQuitClose);
			}
		}

		public void OnToolSelected(eTool tool)
		{
			ClosePulldown();
			if (m_toolState != null)
			{
				m_toolState.SetTool(tool);
			}
			if (m_optionsBar != null)
			{
				m_optionsBar.ShowForTool(tool);
			}
			if (tool != eTool.Text)
			{
				CommitTextEdit();
			}
			if (m_toolBox != null)
			{
				m_toolBox.ResetAll();
			}
		}

		public bool GridEnabled()
		{
			return m_gridEnabled;
		}

		public int ChannelViewMode()
		{
			return m_channelViewMode;
		}

		public void SelectChannelView(int mode)
		{
			SetChannelView(mode);
		}

		private void SetChannelView(int mode)
		{
			m_channelViewMode = mode;
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public bool ChannelVisible(int channel)
		{
			if (channel < 0 || channel > 3)
			{
				return true;
			}
			return m_channelVisible[channel];
		}

		public bool AllChannelsVisible()
		{
			for (int index = 0; index < 4; index++)
			{
				if (!m_channelVisible[index])
				{
					return false;
				}
			}
			return true;
		}

		public bool RgbChannelsVisible()
		{
			return m_channelVisible[0] && m_channelVisible[1] && m_channelVisible[2];
		}

		public void ToggleChannelVisible(int channel)
		{
			if (channel < 0 || channel > 3)
			{
				return;
			}
			m_channelVisible[channel] = !m_channelVisible[channel];
			ApplyChannelVisibilityChange();
		}

		public void ToggleRgbChannelsVisible()
		{
			bool target = !RgbChannelsVisible();
			m_channelVisible[0] = target;
			m_channelVisible[1] = target;
			m_channelVisible[2] = target;
			ApplyChannelVisibilityChange();
		}

		private void ApplyChannelVisibilityChange()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
			}
			if (m_channelsPanel != null)
			{
				m_channelsPanel.Refresh();
			}
		}

		public void ClosePulldown()
		{
			if (m_pulldownPanel != null)
			{
				m_overlay.Remove(m_pulldownPanel);
				m_pulldownPanel = null;
			}
		}

		public bool PulldownOpen()
		{
			return m_pulldownPanel != null;
		}

		public bool PulldownJustDismissed()
		{
			return (System.Environment.TickCount64 - m_pulldownDismissTick) < 300;
		}

		public void ShowPulldown(View content, double anchorX, double anchorY, double width, double height)
		{
			ClosePulldown();
			m_menuBar.CloseOpenMenu();

			Border panel = new Border();
			panel.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			panel.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			panel.StrokeThickness = 1.0;
			panel.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			panel.Content = content;

			double overlayWidth = m_overlay.Width;
			if (overlayWidth > 0.0 && anchorX + width > overlayWidth)
			{
				anchorX = overlayWidth - width;
			}
			if (anchorX < 0.0)
			{
				anchorX = 0.0;
			}
			AbsoluteLayout.SetLayoutFlags(panel, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(panel, new Rect(anchorX, anchorY, width, height));
			m_overlay.Add(panel);
			m_pulldownPanel = panel;
		}

		public void ApplyBrushTip(bool square)
		{
			if (m_optionsBar != null)
			{
				m_optionsBar.ApplyBrushTip(square);
			}
		}

		public void ApplyBrushSpacing(int spacing)
		{
			if (m_optionsBar != null)
			{
				m_optionsBar.ApplyBrushSpacing(spacing);
			}
		}

		public static Color FromSkColor(SkiaSharp.SKColor color)
		{
			return new Color(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
		}

		public bool IsTextEditActive()
		{
			if (m_textEditSession == null)
			{
				return false;
			}
			return m_textEditSession.IsActive();
		}

		public CanvasView TextEditCanvas()
		{
			if (m_textEditSession == null)
			{
				return null;
			}
			return m_textEditSession.EditCanvas();
		}

		public Bitmute.Imaging.Layer TextEditLayer()
		{
			if (m_textEditSession == null)
			{
				return null;
			}
			return m_textEditSession.EditLayer();
		}

		public int TextCaretIndex()
		{
			if (m_textEditSession == null)
			{
				return 0;
			}
			return m_textEditSession.CaretIndex();
		}

		public int TextSelectionStart()
		{
			if (m_textEditSession == null)
			{
				return 0;
			}
			return m_textEditSession.SelectionStart();
		}

		public int TextSelectionLength()
		{
			if (m_textEditSession == null)
			{
				return 0;
			}
			return m_textEditSession.SelectionLength();
		}

		public bool CaretVisible()
		{
			if (m_textEditSession == null)
			{
				return false;
			}
			return m_textEditSession.CaretVisible();
		}

		public void PlaceText(CanvasView canvas, int x, int y, float deviceX, float deviceY)
		{
			if (m_textEditSession != null)
			{
				m_textEditSession.PlaceText(canvas, x, y, deviceX, deviceY);
			}
		}

		public void BeginTextEditForLayer(Bitmute.Imaging.Layer layer)
		{
			if (m_textEditSession != null)
			{
				m_textEditSession.BeginForLayer(layer);
			}
		}

		public void CommitTextEdit()
		{
			if (m_textEditSession != null)
			{
				m_textEditSession.Commit();
			}
		}

		public void DoRasterizeText()
		{
			if (m_textEditSession != null)
			{
				m_textEditSession.Rasterize();
			}
		}

		public void RefreshTextEditStyle()
		{
			if (m_textEditSession != null)
			{
				m_textEditSession.RefreshStyle();
			}
		}

		public void SelectTool(eTool tool)
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.SelectToolExternal(tool);
				return;
			}
			m_toolState.SetTool(tool);
			OnToolSelected(tool);
		}

		public void SyncTextOptionsBar()
		{
			if (m_optionsBar != null)
			{
				m_optionsBar.SyncTextOptions();
			}
		}

		public void RefreshToolPaletteColors()
		{
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
		}

		public void RefreshLayersPanel()
		{
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
		}

		public DocumentWindow ActiveDocumentWindow()
		{
			return m_activeDocumentWindow;
		}

		public ToolState CurrentToolState()
		{
			return m_toolState;
		}

		public Tool CurrentTool()
		{
			return m_toolBox.Instance(m_toolState.Tool());
		}

		public bool SnapEnabled()
		{
			return m_snapEnabled;
		}

		public bool SnapTargetGuides()
		{
			return m_snapTargetGuides;
		}

		public bool SnapTargetGrid()
		{
			return m_snapTargetGrid;
		}

		public bool SnapTargetEdges()
		{
			return m_snapTargetEdges;
		}

		public bool SnapTargetLayerBounds()
		{
			return m_snapTargetLayerBounds;
		}

		public void BeginTransform(int mode)
		{
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null || layer.IsText())
			{
				SetStatusMessage("Select a raster layer to transform");
				return;
			}
			if (m_toolState.Tool() != eTool.FreeTransform)
			{
				m_toolBox.SetPreviousTool(m_toolState.Tool());
			}
			OnToolSelected(eTool.FreeTransform);
			bool armed = m_toolBox.FreeTransform().Begin(document, mode, m_toolState.Background());
			if (!armed)
			{
				SetStatusMessage("Cannot transform this layer");
				OnToolSelected(m_toolBox.PreviousTool());
				return;
			}
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
		}

		public void EndTransformMode()
		{
			if (m_toolState.Tool() != eTool.FreeTransform)
			{
				return;
			}
			OnToolSelected(m_toolBox.PreviousTool());
		}

		private void OnSystemThemeChanged(object sender, Microsoft.Maui.Controls.AppThemeChangedEventArgs eventArgs)
		{
			Theme.OnSystemThemeChanged();
		}

		public double WorkspaceWidth()
		{
			return m_workspace.Width;
		}

		public double WorkspaceHeight()
		{
			return m_workspace.Height;
		}
	}
}
