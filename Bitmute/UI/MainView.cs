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

		private const double MenuItemHeight = 26.0;
		private const double DropdownWidth = 190.0;
		private const double MenuSeparatorHeight = 9.0;
		private const string MenuBreak = "__break__";
		private const string MenuNone = "(none yet)";

		private AbsoluteLayout m_workspace;
		private AbsoluteLayout m_overlay;
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
		private BoxView m_modalBackdrop;
		private View m_modalContent;
		private double m_modalX;
		private double m_modalY;
		private double m_modalWidth;
		private double m_modalHeight;
		private double m_modalDragOriginX;
		private double m_modalDragOriginY;
		private FloatingPanel m_pendingClosePanel;
		private Label m_optionsToolLabel;
		private Label m_brushSizeLabel;
		private SliderField m_brushSizeField;
		private Label m_brushHardnessLabel;
		private SliderField m_brushHardnessField;
		private Label m_brushOpacityLabel;
		private SliderField m_brushOpacityField;
		private Label m_brushFlowLabel;
		private SliderField m_brushFlowField;
		private Label m_brushSmoothingLabel;
		private SliderField m_brushSmoothingField;
		private Label m_brushStrengthLabel;
		private SliderField m_brushStrengthField;
		private Button m_brushSettingsButton;
		private HorizontalStackLayout m_optionsRow;
		private View m_pulldownPanel;
		private long m_pulldownDismissTick;
		private Picker m_brushTipPicker;
		private Slider m_brushSpacingSlider;
		private Label m_brushSpacingValue;
		private Label m_brushModeLabel;
		private Picker m_brushModePicker;
		private Label m_brushAirbrushLabel;
		private CheckBox m_brushAirbrushCheck;
		private Label m_cloneAlignedLabel;
		private CheckBox m_cloneAlignedCheck;
		private Label m_spongeModeLabel;
		private Picker m_spongeModePicker;
		private Label m_colorReplaceModeLabel;
		private Picker m_colorReplaceModePicker;
		private Label m_colorReplaceToleranceLabel;
		private SliderField m_colorReplaceToleranceField;
		private Label m_dodgeBurnRangeLabel;
		private Picker m_dodgeBurnRangePicker;
		private Label m_dodgeBurnExposureLabel;
		private SliderField m_dodgeBurnExposureField;
		private Label m_gradientTypeLabel;
		private Button m_gradientTypeButton;
		private string[] m_gradientTypeNames;
		private List<View> m_gradientTypeRows;
		private Label m_gradientReverseLabel;
		private CheckBox m_gradientReverseCheck;
		private Label m_gradientTransparentLabel;
		private CheckBox m_gradientTransparentCheck;
		private Label m_lineAntiAliasLabel;
		private CheckBox m_lineAntiAliasCheck;
		private Label m_toleranceLabel;
		private SliderField m_toleranceField;
		private Label m_wandAntiAliasLabel;
		private CheckBox m_wandAntiAliasCheck;
		private Label m_wandContiguousLabel;
		private CheckBox m_wandContiguousCheck;
		private Label m_wandSampleAllLabel;
		private CheckBox m_wandSampleAllCheck;
		private Label m_magneticWidthLabel;
		private SliderField m_magneticWidthField;
		private Label m_magneticContrastLabel;
		private SliderField m_magneticContrastField;
		private Label m_textFontLabel;
		private Button m_textFontButton;
		private string[] m_fontFamilies;
		private Label m_textSizeLabel;
		private SliderField m_textSizeField;
		private Label m_textStyleLabel;
		private Button m_textStyleButton;
		private Label m_textAlignLabel;
		private Picker m_textAlignPicker;
		private Label m_textAntiAliasLabel;
		private Picker m_textAntiAliasPicker;
		private Label m_textColorLabel;
		private BoxView m_textColorSwatch;
		private Button m_textCharButton;
		private CheckBox m_charLeadingAutoCheck;
		private SliderField m_charLeadingField;
		private SliderField m_charTrackingField;
		private SliderField m_charHScaleField;
		private SliderField m_charVScaleField;
		private SliderField m_charBaselineField;
		private CheckBox m_charFauxBoldCheck;
		private CheckBox m_charFauxItalicCheck;
		private CheckBox m_charKerningAutoCheck;
		private Editor m_textEditor;
		private CanvasView m_textEditCanvas;
		private Bitmute.Imaging.Layer m_textEditLayer;
		private bool m_textEditActive;
		private bool m_textEditorKeyHooked;
		private Microsoft.Maui.Dispatching.IDispatcherTimer m_caretTimer;
		private bool m_caretVisible;
		private string m_textPreEditString;
		private bool m_textPreEditWasNew;
		private float m_textPreEditSize;
		private string m_textPreEditFont;
		private bool m_textPreEditBold;
		private bool m_textPreEditItalic;
		private SKColor m_textPreEditColor;
		private int m_textPreEditAlign;
		private int m_textPreEditAntiAlias;
		private bool m_textPreEditLeadingAuto;
		private float m_textPreEditLeading;
		private int m_textPreEditTracking;
		private int m_textPreEditHorizontalScale;
		private int m_textPreEditVerticalScale;
		private int m_textPreEditBaselineShift;
		private bool m_textPreEditFauxBold;
		private bool m_textPreEditFauxItalic;
		private bool m_textPreEditKerningAuto;
		private Label m_statusInfoLabel;
		private Label m_statusCursorLabel;
		private string[] m_menuTitles;
		private Border[] m_menuButtons;
		private List<Border> m_openItemButtons;
		private List<string> m_openItemActions;
		private int m_openMenuIndex;
		private bool m_acceleratorsHooked;
		private int m_untitledCount;
		private int m_cascadeCount;
		private int m_topZIndex;
		private ToolState m_toolState;
		private MoveTool m_moveTool;
		private RectangleSelectTool m_rectangleSelectTool;
		private EllipseSelectTool m_ellipseSelectTool;
		private LassoTool m_lassoTool;
		private FreehandLassoTool m_freehandLassoTool;
		private MagneticLassoTool m_magneticLassoTool;
		private MagicWandTool m_magicWandTool;
		private TextTool m_textTool;
		private PencilTool m_pencilTool;
		private BrushTool m_brushTool;
		private EraserTool m_eraserTool;
		private DodgeBurnTool m_dodgeBurnTool;
		private BlurTool m_blurTool;
		private SpongeTool m_spongeTool;
		private ColorReplacementTool m_colorReplacementTool;
		private SharpenTool m_sharpenTool;
		private CloneTool m_cloneTool;
		private HealTool m_healTool;
		private SliceTool m_sliceTool;
		private SmudgeTool m_smudgeTool;
		private EyedropperTool m_eyedropperTool;
		private FillTool m_fillTool;
		private GradientTool m_gradientTool;
		private LineTool m_lineTool;
		private ShapeTool m_rectangleShapeTool;
		private ShapeTool m_roundedRectangleShapeTool;
		private ShapeTool m_ellipseShapeTool;
		private ShapeTool m_polygonShapeTool;
		private HandTool m_handTool;
		private ZoomTool m_zoomTool;
		private RulerTool m_rulerTool;
		private CropTool m_cropTool;
		private FreeTransformTool m_freeTransformTool;
		private eTool m_previousTool;
		private int m_guideCreateOrientation;
		private CanvasView m_guideCreateCanvas;
		private bool m_gridEnabled;
		private bool m_snapEnabled;
		private bool m_snapTargetGuides;
		private bool m_snapTargetGrid;
		private bool m_snapTargetEdges;
		private bool m_snapTargetLayerBounds;
		private int m_channelViewMode;
		private double m_openDropdownX;
		private List<Border> m_submenuParentRows;
		private List<string> m_submenuParentNames;
		private List<int> m_submenuParentIndices;
		private List<Border> m_submenuChildRows;
		private Border m_submenuBorder;
		private List<string> m_recentMenuPaths;

		private static string RecentMenuLabel(int index, string path)
		{
			return System.IO.Path.GetFileName(path);
		}

		private static string PanelMenuLabel(string name, bool visible)
		{
			if (visible)
			{
				return "✓ " + name;
			}
			return name;
		}

		private static string AcceleratorForItem(string title, string item)
		{
			if (title == "File")
			{
				if (item == "New") return "Ctrl+N";
				if (item == "Open…") return "Ctrl+O";
				if (item == "Save") return "Ctrl+S";
				if (item == "Save As…") return "Ctrl+Shift+S";
				if (item == "Export As…") return "Ctrl+Alt+Shift+S";
			}
			if (title == "Edit")
			{
				if (item == "Undo") return "Ctrl+Z";
				if (item == "Redo") return "Ctrl+Y";
				if (item == "Cut") return "Ctrl+X";
				if (item == "Copy") return "Ctrl+C";
				if (item == "Paste") return "Ctrl+V";
			}
			if (title == "Image")
			{
				if (item == "Image Size…") return "Ctrl+Alt+I";
				if (item == "Invert Colors") return "Ctrl+I";
			}
			if (title == "Select")
			{
				if (item == "All") return "Ctrl+A";
				if (item == "Deselect") return "Ctrl+D";
				if (item == "Invert") return "Ctrl+Shift+I";
			}
			if (title == "View")
			{
				if (item == "Zoom In") return "Ctrl++";
				if (item == "Zoom Out") return "Ctrl+-";
				if (item == "Fit on Screen") return "Ctrl+0";
				if (item == "Rulers") return "Ctrl+R";
			}
			if (title == "Edit")
			{
				if (item == "Free Transform") return "Ctrl+T";
			}
			if (title == "Layer")
			{
				if (item == "Merge Down") return "Ctrl+E";
				if (item == "Merge Visible") return "Ctrl+Shift+E";
			}
			return "";
		}

		private static bool IsSubmenu(string title, string item)
		{
			if (title == "File" && item == "Open Recent")
			{
				return true;
			}
			if (title == "Edit" && item == "Transform")
			{
				return true;
			}
			if (title == "View" && item == "Snap To")
			{
				return true;
			}
			if (title == "Image" && item == "Adjustments")
			{
				return true;
			}
			if (title == "Filter")
			{
				if (item == "Artistic" || item == "Blur" || item == "Brush Strokes" || item == "Distort" || item == "Noise" || item == "Pixelate" || item == "Render" || item == "Sharpen")
				{
					return true;
				}
			}
			return false;
		}

		private string[] GetSubmenuItems(string title, string item)
		{
			if (title == "File" && item == "Open Recent")
			{
				List<string> items = new List<string>();
				for (int index = 0; index < m_recentMenuPaths.Count; index++)
				{
					items.Add(RecentMenuLabel(index, m_recentMenuPaths[index]));
				}
				return items.ToArray();
			}
			if (title == "Edit" && item == "Transform")
			{
				return new string[] { "Free Transform", "Scale", "Rotate", "Skew", "Distort", "Perspective", "Flip Horizontal (Layer)", "Flip Vertical (Layer)" };
			}
			if (title == "View" && item == "Snap To")
			{
				return new string[] { PanelMenuLabel("Snap Guides", m_snapTargetGuides), PanelMenuLabel("Snap Grid", m_snapTargetGrid), PanelMenuLabel("Snap Edges", m_snapTargetEdges), PanelMenuLabel("Snap Layers", m_snapTargetLayerBounds) };
			}
			if (title == "Image" && item == "Adjustments")
			{
				return new string[] { "Brightness/Contrast…", "Hue/Saturation…", MenuBreak, "Desaturate", "Invert Colors", MenuBreak, "Posterize…", "Threshold…" };
			}
			if (title == "Filter")
			{
				if (item == "Blur")
				{
					return new string[] { "Gaussian Blur…" };
				}
				if (item == "Sharpen")
				{
					return new string[] { "Unsharp Mask…" };
				}
				if (item == "Noise")
				{
					return new string[] { "Add Noise…" };
				}
				if (item == "Pixelate")
				{
					return new string[] { "Pixelate…" };
				}
				return new string[] { MenuNone };
			}
			return new string[] { };
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

		private string[] GetMenuItems(string title)
		{
			if (title == "File")
			{
				List<string> fileItems = new List<string>();
				fileItems.Add("New");
				fileItems.Add("Open…");
				fileItems.Add("Save");
				fileItems.Add("Save As…");
				fileItems.Add("Export As…");
				m_recentMenuPaths.Clear();
				List<string> recent = RecentFiles.List();
				int recentCount = recent.Count;
				if (recentCount > 12)
				{
					recentCount = 12;
				}
				for (int index = 0; index < recentCount; index++)
				{
					m_recentMenuPaths.Add(recent[index]);
				}
				if (recentCount > 0)
				{
					fileItems.Add("Open Recent");
				}
				fileItems.Add("Exit");
				return fileItems.ToArray();
			}
			if (title == "Edit")
			{
				return new string[] { "Undo", "Redo", "Cut", "Copy", "Paste", "Transform", "Stroke…", "Preferences…" };
			}
			if (title == "Image")
			{
				return new string[] { "Adjustments", MenuBreak, "Image Size…", "Canvas Size…", "Flip Horizontal", "Flip Vertical", "Rotate 90° CW", "Rotate 180°", "Rotate 90° CCW", "Rotate Arbitrary…", "Crop to Selection", "Trim" };
			}
			if (title == "Layer")
			{
				return new string[] { "New Layer", "Delete Layer", MenuBreak, "Merge Down", "Merge Visible", "Flatten Image", MenuBreak, "Layer Style…", "Rasterize Text" };
			}
			if (title == "Select")
			{
				return new string[] { "All", "Deselect", "Invert" };
			}
			if (title == "Filter")
			{
				return new string[] { "Artistic", "Blur", "Brush Strokes", "Distort", "Noise", "Pixelate", "Render", "Sharpen" };
			}
			if (title == "View")
			{
				return new string[] { "Zoom In", "Zoom Out", "Fit on Screen", "Rulers", "Grid", PanelMenuLabel("Snap", m_snapEnabled), "Snap To", PanelMenuLabel("Lock Guides", GuidesLocked()), "Clear Guides" };
			}
			if (title == "Window")
			{
				return new string[] { "Cascade", "Tile", PanelMenuLabel("Navigator", m_navigatorPanelVisible), PanelMenuLabel("Swatches", m_swatchesPanelVisible), PanelMenuLabel("Layers", m_layersPanelVisible) };
			}
			return new string[] { "About Bitmute" };
		}

		private bool IsItemEnabled(string title, string item)
		{
			if (item == MenuBreak)
			{
				return false;
			}
			if (item == MenuNone)
			{
				return false;
			}
			if (title == "File")
			{
				return true;
			}
			if (title == "Edit")
			{
				return true;
			}
			if (title == "Layer")
			{
				if (item == "Rasterize Text")
				{
					return ActiveLayerIsText();
				}
				if (item == "Merge Down")
				{
					return CanMergeDown();
				}
				if (item == "Layer Style…")
				{
					Document styleDocument = ActiveDocument();
					if (styleDocument == null)
					{
						return false;
					}
					return styleDocument.ActiveLayer() != null;
				}
				return true;
			}
			if (title == "Select")
			{
				return true;
			}
			if (title == "Filter")
			{
				return true;
			}
			if (title == "Image")
			{
				return true;
			}
			if (title == "Window")
			{
				return true;
			}
			if (title == "View")
			{
				return true;
			}
			if (item == "About Bitmute")
			{
				return true;
			}
			return false;
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

		private Border BuildMenuButton(int index)
		{
			Label label = new Label();
			label.Text = m_menuTitles[index];
			label.FontSize = 12.0;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;

			Border button = new Border();
			button.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			button.ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
			button.StrokeThickness = 0.0;
			button.Content = label;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnMenuButtonTapped;
			button.GestureRecognizers.Add(tap);

			PointerGestureRecognizer pointer = new PointerGestureRecognizer();
			pointer.PointerEntered += OnMenuButtonPointerEntered;
			pointer.PointerExited += OnMenuButtonPointerExited;
			button.GestureRecognizers.Add(pointer);

			return button;
		}

		private int FindMenuButtonIndex(object sender)
		{
			for (int index = 0; index < m_menuButtons.Length; index++)
			{
				if (ReferenceEquals(m_menuButtons[index], sender))
				{
					return index;
				}
			}
			return -1;
		}

		private void OnMenuButtonPointerEntered(object sender, PointerEventArgs eventArgs)
		{
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (m_openMenuIndex < 0)
			{
				m_menuButtons[index].ThemeBg(UiConstants.MenuHoverLight, UiConstants.MenuHoverDark);
				return;
			}
			if (index != m_openMenuIndex)
			{
				OpenMenu(index);
			}
		}

		private void OnMenuButtonPointerExited(object sender, PointerEventArgs eventArgs)
		{
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (index == m_openMenuIndex)
			{
				return;
			}
			m_menuButtons[index].ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
		}

		private View BuildMenuBar()
		{
			m_menuTitles = new string[] { "File", "Edit", "Image", "Layer", "Select", "Filter", "View", "Window", "Help" };
			m_menuButtons = new Border[m_menuTitles.Length];

			HorizontalStackLayout strip = new HorizontalStackLayout();
			strip.HeightRequest = UiConstants.MenuBarHeight;
			strip.ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
			strip.Spacing = 0.0;
			strip.Padding = new Thickness(0.0);

			for (int index = 0; index < m_menuTitles.Length; index++)
			{
				Border button = BuildMenuButton(index);
				m_menuButtons[index] = button;
				strip.Add(button);
			}

			return strip;
		}

		private void OnMenuButtonTapped(object sender, TappedEventArgs eventArgs)
		{
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (m_openMenuIndex == index)
			{
				CloseMenu();
				return;
			}
			OpenMenu(index);
		}

		private void OpenMenu(int index)
		{
			ClosePulldown();
			CloseMenu();
			m_openMenuIndex = index;
			m_menuButtons[index].ThemeBg(UiConstants.MenuOpenLight, UiConstants.MenuOpenDark);
			m_submenuParentRows.Clear();
			m_submenuParentNames.Clear();
			m_submenuParentIndices.Clear();
			m_submenuBorder = null;

			string title = m_menuTitles[index];
			string[] items = GetMenuItems(title);

			double dropdownX = m_menuButtons[index].Bounds.X;
			double overlayWidth = m_overlay.Width;
			if (overlayWidth > 0.0 && dropdownX + DropdownWidth > overlayWidth)
			{
				dropdownX = overlayWidth - DropdownWidth;
			}
			if (dropdownX < 0.0)
			{
				dropdownX = 0.0;
			}
			m_openDropdownX = dropdownX;

			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);

			for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
			{
				list.Add(BuildMenuItem(title, items[itemIndex], itemIndex));
			}

			Border dropdown = new Border();
			dropdown.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			dropdown.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			dropdown.StrokeThickness = 1.0;
			dropdown.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			dropdown.Content = list;

			BoxView catcher = new BoxView();
			catcher.Color = Colors.Transparent;
			TapGestureRecognizer catcherTap = new TapGestureRecognizer();
			catcherTap.Tapped += OnCatcherTapped;
			catcher.GestureRecognizers.Add(catcherTap);
			AbsoluteLayout.SetLayoutFlags(catcher, AbsoluteLayoutFlags.WidthProportional | AbsoluteLayoutFlags.HeightProportional);
			AbsoluteLayout.SetLayoutBounds(catcher, new Rect(0.0, UiConstants.MenuBarHeight, 1.0, 1.0));
			m_overlay.Add(catcher);

			double dropdownHeight = MenuListHeight(items);
			AbsoluteLayout.SetLayoutFlags(dropdown, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(dropdown, new Rect(dropdownX, UiConstants.MenuBarHeight, DropdownWidth, dropdownHeight));
			m_overlay.Add(dropdown);
		}

		private void OpenSubmenu(string parentItem, int parentIndex)
		{
			CloseSubmenu();
			string[] children = GetSubmenuItems(m_menuTitles[m_openMenuIndex], parentItem);
			if (children.Length == 0)
			{
				return;
			}
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);
			m_submenuChildRows.Clear();
			for (int index = 0; index < children.Length; index++)
			{
				Border childRow = BuildMenuItem(m_menuTitles[m_openMenuIndex], children[index], -1);
				m_submenuChildRows.Add(childRow);
				list.Add(childRow);
			}
			Border submenu = new Border();
			submenu.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			submenu.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			submenu.StrokeThickness = 1.0;
			submenu.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			submenu.Content = list;
			double submenuX = m_openDropdownX + DropdownWidth - 2.0;
			double overlayWidth = m_overlay.Width;
			if (overlayWidth > 0.0 && submenuX + DropdownWidth > overlayWidth)
			{
				submenuX = m_openDropdownX - DropdownWidth + 2.0;
			}
			if (submenuX < 0.0)
			{
				submenuX = 0.0;
			}
			double submenuY = UiConstants.MenuBarHeight + 4.0 + (parentIndex * MenuItemHeight);
			double submenuHeight = MenuListHeight(children);
			AbsoluteLayout.SetLayoutFlags(submenu, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(submenu, new Rect(submenuX, submenuY, DropdownWidth, submenuHeight));
			m_overlay.Add(submenu);
			m_submenuBorder = submenu;
		}

		private void CloseSubmenu()
		{
			if (m_submenuBorder != null)
			{
				m_overlay.Remove(m_submenuBorder);
				m_submenuBorder = null;
			}
		}

		private void OnSubmenuParentEntered(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row == null)
			{
				return;
			}
			row.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			for (int index = 0; index < m_submenuParentRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuParentRows[index], sender))
				{
					OpenSubmenu(m_submenuParentNames[index], m_submenuParentIndices[index]);
					return;
				}
			}
		}

		private void OnSubmenuParentTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_submenuParentRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuParentRows[index], sender))
				{
					OpenSubmenu(m_submenuParentNames[index], m_submenuParentIndices[index]);
					return;
				}
			}
		}

		private Border BuildMenuSeparator()
		{
			Border line = new Border();
			line.HeightRequest = 1.0;
			line.StrokeThickness = 0.0;
			line.HorizontalOptions = LayoutOptions.Fill;
			line.VerticalOptions = LayoutOptions.Center;
			line.ThemeBg(UiConstants.DividerLight, UiConstants.DividerDark);

			Border separatorRow = new Border();
			separatorRow.HeightRequest = MenuSeparatorHeight;
			separatorRow.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
			separatorRow.StrokeThickness = 0.0;
			separatorRow.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			separatorRow.Content = line;
			return separatorRow;
		}

		private double MenuListHeight(string[] items)
		{
			double total = 8.0;
			for (int index = 0; index < items.Length; index++)
			{
				if (items[index] == MenuBreak)
				{
					total = total + MenuSeparatorHeight;
				}
				else
				{
					total = total + MenuItemHeight;
				}
			}
			return total;
		}

		private Border BuildMenuItem(string title, string item, int itemIndex)
		{
			if (item == MenuBreak)
			{
				return BuildMenuSeparator();
			}
			bool enabled = IsItemEnabled(title, item);
			bool submenu = IsSubmenu(title, item);

			string accelerator = AcceleratorForItem(title, item);
			if (submenu)
			{
				accelerator = "▸";
			}

			Grid rowContent = new Grid();
			rowContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			rowContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

			Label label = new Label();
			label.Text = item;
			label.FontSize = 12.0;
			label.VerticalOptions = LayoutOptions.Center;
			label.LineBreakMode = LineBreakMode.TailTruncation;
			label.MaxLines = 1;
			if (enabled)
			{
				label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			}
			else
			{
				label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			}
			Grid.SetColumn(label, 0);
			rowContent.Add(label);

			if (accelerator.Length > 0)
			{
				Label accelLabel = new Label();
				accelLabel.Text = accelerator;
				accelLabel.FontSize = 12.0;
				accelLabel.VerticalOptions = LayoutOptions.Center;
				if (enabled)
				{
					accelLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				}
				else
				{
					accelLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
				}
				Grid.SetColumn(accelLabel, 1);
				rowContent.Add(accelLabel);
			}

			Border row = new Border();
			row.HeightRequest = MenuItemHeight;
			row.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
			row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			row.StrokeThickness = 0.0;
			row.Content = rowContent;

			if (submenu)
			{
				TapGestureRecognizer submenuTap = new TapGestureRecognizer();
				submenuTap.Tapped += OnSubmenuParentTapped;
				row.GestureRecognizers.Add(submenuTap);
				PointerGestureRecognizer submenuPointer = new PointerGestureRecognizer();
				submenuPointer.PointerEntered += OnSubmenuParentEntered;
				submenuPointer.PointerExited += OnMenuItemPointerExited;
				row.GestureRecognizers.Add(submenuPointer);
				m_submenuParentRows.Add(row);
				m_submenuParentNames.Add(item);
				m_submenuParentIndices.Add(itemIndex);
			}
			else if (enabled)
			{
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnMenuItemTapped;
				row.GestureRecognizers.Add(tap);
				PointerGestureRecognizer pointer = new PointerGestureRecognizer();
				pointer.PointerEntered += OnMenuItemPointerEntered;
				pointer.PointerExited += OnMenuItemPointerExited;
				row.GestureRecognizers.Add(pointer);
				m_openItemButtons.Add(row);
				m_openItemActions.Add(item);
			}

			return row;
		}

		private void OnMenuItemPointerEntered(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row != null)
			{
				row.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			}
			bool isSubmenuChild = false;
			for (int index = 0; index < m_submenuChildRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuChildRows[index], sender))
				{
					isSubmenuChild = true;
					break;
				}
			}
			if (!isSubmenuChild)
			{
				CloseSubmenu();
			}
		}

		private void OnMenuItemPointerExited(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row != null)
			{
				row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			}
		}

		private void OnMenuItemTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_openItemButtons.Count; index++)
			{
				if (ReferenceEquals(m_openItemButtons[index], sender))
				{
					string action = m_openItemActions[index];
					CloseMenu();
					InvokeMenuAction(action);
					return;
				}
			}
		}

		private void OnCatcherTapped(object sender, TappedEventArgs eventArgs)
		{
			CloseMenu();
		}

		private void CloseMenu()
		{
			m_overlay.Clear();
			m_openItemButtons.Clear();
			m_openItemActions.Clear();
			m_submenuParentRows.Clear();
			m_submenuParentNames.Clear();
			m_submenuParentIndices.Clear();
			m_submenuChildRows.Clear();
			m_submenuBorder = null;
			if (m_openMenuIndex >= 0)
			{
				m_menuButtons[m_openMenuIndex].ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			}
			m_openMenuIndex = -1;
		}

		private void InvokeMenuAction(string action)
		{
			if (action == "New")
			{
				ShowNewDocumentDialog();
				return;
			}
			if (action == "Undo")
			{
				DoUndo();
				return;
			}
			if (action == "Redo")
			{
				DoRedo();
				return;
			}
			if (action == "Cut")
			{
				DoCut();
				return;
			}
			if (action == "Copy")
			{
				DoCopy();
				return;
			}
			if (action == "Paste")
			{
				DoPaste();
				return;
			}
			if (action == "Open…")
			{
				OpenImageFlow();
				return;
			}
			if (action == "Save")
			{
				SaveImageFlow();
				return;
			}
			if (action == "Save As…")
			{
				SaveAsFlow();
				return;
			}
			if (action == "Exit")
			{
				DoExit();
				return;
			}
			if (action == "Zoom In")
			{
				DoZoomIn();
				return;
			}
			if (action == "Zoom Out")
			{
				DoZoomOut();
				return;
			}
			if (action == "Fit on Screen")
			{
				DoFit();
				return;
			}
			if (action == "Rulers")
			{
				ToggleRulers();
				return;
			}
			if (action == "Grid")
			{
				ToggleGrid();
				return;
			}
			if (action == "Snap" || action == "✓ Snap")
			{
				m_snapEnabled = !m_snapEnabled;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_enabled", m_snapEnabled);
				return;
			}
			if (action == "Snap Guides" || action == "✓ Snap Guides")
			{
				m_snapTargetGuides = !m_snapTargetGuides;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_target_guides", m_snapTargetGuides);
				return;
			}
			if (action == "Snap Grid" || action == "✓ Snap Grid")
			{
				m_snapTargetGrid = !m_snapTargetGrid;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_target_grid", m_snapTargetGrid);
				return;
			}
			if (action == "Snap Edges" || action == "✓ Snap Edges")
			{
				m_snapTargetEdges = !m_snapTargetEdges;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_target_edges", m_snapTargetEdges);
				return;
			}
			if (action == "Snap Layers" || action == "✓ Snap Layers")
			{
				m_snapTargetLayerBounds = !m_snapTargetLayerBounds;
				Microsoft.Maui.Storage.Preferences.Default.Set("snap_target_layer_bounds", m_snapTargetLayerBounds);
				return;
			}
			if (action == "Lock Guides" || action == "✓ Lock Guides")
			{
				Document guideDoc = ActiveDocument();
				if (guideDoc != null)
				{
					guideDoc.Guides().SetLocked(!guideDoc.Guides().IsLocked());
				}
				return;
			}
			if (action == "Clear Guides")
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
			if (action == "Free Transform")
			{
				BeginTransform(0);
				return;
			}
			if (action == "Scale")
			{
				BeginTransform(1);
				return;
			}
			if (action == "Rotate")
			{
				BeginTransform(2);
				return;
			}
			if (action == "Skew")
			{
				BeginTransform(3);
				return;
			}
			if (action == "Distort")
			{
				BeginTransform(4);
				return;
			}
			if (action == "Perspective")
			{
				BeginTransform(5);
				return;
			}
			if (action == "Flip Horizontal (Layer)")
			{
				BeginTransform(6);
				return;
			}
			if (action == "Flip Vertical (Layer)")
			{
				BeginTransform(7);
				return;
			}
			if (action == "Rotate Arbitrary…")
			{
				OpenAdjustment("rotate");
				return;
			}
			if (action == "Stroke…")
			{
				OpenStrokeDialog();
				return;
			}
			for (int recentIndex = 0; recentIndex < m_recentMenuPaths.Count; recentIndex++)
			{
				if (action == RecentMenuLabel(recentIndex, m_recentMenuPaths[recentIndex]))
				{
					OpenRecentFile(m_recentMenuPaths[recentIndex]);
					return;
				}
			}
			if (action == "All")
			{
				DoSelectAll();
				return;
			}
			if (action == "Deselect")
			{
				DoDeselect();
				return;
			}
			if (action == "Invert")
			{
				DoInvertSelection();
				return;
			}
			if (action == "Invert Colors")
			{
				DoInvert();
				return;
			}
			if (action == "Desaturate")
			{
				DoDesaturate();
				return;
			}
			if (action == "Brightness/Contrast…")
			{
				OpenAdjustment("bc");
				return;
			}
			if (action == "Hue/Saturation…")
			{
				OpenAdjustment("hsl");
				return;
			}
			if (action == "Posterize…")
			{
				OpenAdjustment("posterize");
				return;
			}
			if (action == "Threshold…")
			{
				OpenAdjustment("threshold");
				return;
			}
			if (action == "Gaussian Blur…")
			{
				OpenAdjustment("gblur");
				return;
			}
			if (action == "Unsharp Mask…")
			{
				OpenAdjustment("unsharp");
				return;
			}
			if (action == "Add Noise…")
			{
				OpenAdjustment("noise");
				return;
			}
			if (action == "Pixelate…")
			{
				OpenAdjustment("pixelate");
				return;
			}
			if (action == "Flip Horizontal")
			{
				DoCanvasOp("fliph");
				return;
			}
			if (action == "Flip Vertical")
			{
				DoCanvasOp("flipv");
				return;
			}
			if (action == "Rotate 90° CW")
			{
				DoCanvasOp("rot90");
				return;
			}
			if (action == "Rotate 180°")
			{
				DoCanvasOp("rot180");
				return;
			}
			if (action == "Rotate 90° CCW")
			{
				DoCanvasOp("rot270");
				return;
			}
			if (action == "Crop to Selection")
			{
				DoCanvasOp("crop");
				return;
			}
			if (action == "Trim")
			{
				DoCanvasOp("trim");
				return;
			}
			if (action == "Cascade")
			{
				DoCascadeWindows();
				return;
			}
			if (action == "Tile")
			{
				DoTileWindows();
				return;
			}
			string panelAction = action;
			if (panelAction.StartsWith("✓ "))
			{
				panelAction = panelAction.Substring(2);
			}
			if (panelAction == "Navigator" || panelAction == "Swatches" || panelAction == "Layers" || panelAction == "Info")
			{
				ToggleDockPanel(panelAction);
				return;
			}
			if (action == "Rasterize Text")
			{
				DoRasterizeText();
				return;
			}
			if (action == "Layer Style…")
			{
				OpenLayerStyleDialog();
				return;
			}
			if (action == "Merge Down")
			{
				DoMergeDown();
				return;
			}
			if (action == "Merge Visible")
			{
				DoMergeVisible();
				return;
			}
			if (action == "Flatten Image")
			{
				DoFlattenImage();
				return;
			}
			if (action == "Export As…")
			{
				OpenExportDialog();
				return;
			}
			if (action == "Preferences…")
			{
				ShowModal(new PreferencesDialog(), 340.0, 260.0);
				return;
			}
			if (action == "About Bitmute")
			{
				ShowModal(new AboutDialog(), 380.0, 300.0);
				return;
			}
			if (action == "Canvas Size…")
			{
				OpenSizeDialog(true);
				return;
			}
			if (action == "Image Size…")
			{
				OpenSizeDialog(false);
				return;
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

		private void OpenAdjustment(string id)
		{
			if (id == "rotate")
			{
				ShowModal(new AdjustmentDialog("Rotate Arbitrary", "rotate", new string[] { "Angle" }, new int[] { -180 }, new int[] { 180 }, new int[] { 0 }), 360.0, 170.0);
				return;
			}
			if (id == "bc")
			{
				ShowModal(new AdjustmentDialog("Brightness/Contrast", "bc", new string[] { "Brightness", "Contrast" }, new int[] { -100, -100 }, new int[] { 100, 100 }, new int[] { 0, 0 }), 360.0, 200.0);
				return;
			}
			if (id == "hsl")
			{
				ShowModal(new AdjustmentDialog("Hue/Saturation", "hsl", new string[] { "Hue", "Saturation", "Lightness" }, new int[] { -180, -100, -100 }, new int[] { 180, 100, 100 }, new int[] { 0, 0, 0 }), 360.0, 230.0);
				return;
			}
			if (id == "posterize")
			{
				ShowModal(new AdjustmentDialog("Posterize", "posterize", new string[] { "Levels" }, new int[] { 2 }, new int[] { 64 }, new int[] { 8 }), 360.0, 170.0);
				return;
			}
			if (id == "threshold")
			{
				ShowModal(new AdjustmentDialog("Threshold", "threshold", new string[] { "Level" }, new int[] { 0 }, new int[] { 255 }, new int[] { 128 }), 360.0, 170.0);
				return;
			}
			if (id == "gblur")
			{
				ShowModal(new AdjustmentDialog("Gaussian Blur", "gblur", new string[] { "Radius" }, new int[] { 1 }, new int[] { 30 }, new int[] { 5 }), 360.0, 170.0);
				return;
			}
			if (id == "unsharp")
			{
				ShowModal(new AdjustmentDialog("Unsharp Mask", "unsharp", new string[] { "Amount", "Radius" }, new int[] { 0, 1 }, new int[] { 300, 30 }, new int[] { 100, 3 }), 360.0, 200.0);
				return;
			}
			if (id == "noise")
			{
				ShowModal(new AdjustmentDialog("Add Noise", "noise", new string[] { "Amount" }, new int[] { 0 }, new int[] { 100 }, new int[] { 20 }), 360.0, 170.0);
				return;
			}
			if (id == "pixelate")
			{
				ShowModal(new AdjustmentDialog("Pixelate", "pixelate", new string[] { "Cell Size" }, new int[] { 2 }, new int[] { 64 }, new int[] { 8 }), 360.0, 170.0);
				return;
			}
		}

		public void ApplyAdjustment(string id, int first, int second, int third)
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
				document.RotateArbitrary(first, 2);
				document.EndCanvasEdit();
				FinishCanvasOp(canvas, document);
				return;
			}
			Layer activeLayer = document.ActiveLayer();
			if (activeLayer == null)
			{
				return;
			}
			SkiaSharp.SKBitmap bitmap = activeLayer.Bitmap();
			document.BeginStroke();
			if (id == "bc")
			{
				Adjustments.BrightnessContrast(bitmap, first, second);
			}
			else if (id == "hsl")
			{
				Adjustments.HueSaturationLightness(bitmap, first, second, third);
			}
			else if (id == "posterize")
			{
				Adjustments.Posterize(bitmap, first);
			}
			else if (id == "threshold")
			{
				Adjustments.Threshold(bitmap, first);
			}
			else if (id == "gblur")
			{
				Adjustments.GaussianBlur(bitmap, first);
			}
			else if (id == "unsharp")
			{
				Adjustments.UnsharpMask(bitmap, first, second);
			}
			else if (id == "noise")
			{
				Adjustments.AddNoise(bitmap, first, false);
			}
			else if (id == "pixelate")
			{
				Adjustments.Pixelate(bitmap, first);
			}
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
			document.Selection().SelectRect(new SkiaSharp.SKRectI(0, 0, document.Width(), document.Height()));
			canvas.Redraw();
		}

		private void DoDeselect()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			canvas.CurrentDocument().Selection().Clear();
			canvas.Redraw();
		}

		private void DoInvertSelection()
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			canvas.CurrentDocument().Selection().Invert();
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
				SkiaSharp.SKBitmap result = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
				result.Erase(SkiaSharp.SKColors.Transparent);
				for (int row = 0; row < height; row++)
				{
					for (int column = 0; column < width; column++)
					{
						int canvasX = bounds.Left + column;
						int canvasY = bounds.Top + row;
						if (selection.IsSelected(canvasX, canvasY))
						{
							result.SetPixel(column, row, layer.GetPixelCanvas(canvasX, canvasY));
						}
					}
				}
				return result;
			}
			return layer.Bitmap().Copy();
		}

		private void EraseSelection(Document document, Layer layer)
		{
			Selection selection = document.Selection();
			if (selection != null && selection.IsActive())
			{
				SkiaSharp.SKRectI bounds = selection.Bounds();
				for (int canvasY = bounds.Top; canvasY < bounds.Bottom; canvasY++)
				{
					for (int canvasX = bounds.Left; canvasX < bounds.Right; canvasX++)
					{
						if (selection.IsSelected(canvasX, canvasY))
						{
							layer.SetPixelCanvas(canvasX, canvasY, SkiaSharp.SKColors.Transparent);
						}
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
				SkiaSharp.SKImage image = SkiaSharp.SKImage.FromBitmap(bitmap);
				SkiaSharp.SKData data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
				image.Dispose();
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
			Layer layer = document.AddLayer("Pasted");
			if (layer == null)
			{
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
			ShowModal(new LayerStyleDialog(layer.LayerStyle().Clone()), 360.0, 560.0);
		}

		public void ApplyLayerStyle(LayerStyle style)
		{
			CloseModal();
			CanvasView canvas = ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			document.BeginCanvasEdit("Layer Style");
			layer.SetLayerStyle(style);
			document.EndCanvasEdit();
			canvas.MarkComposeDirty();
			canvas.InvalidateSurface();
			RefreshLayerThumbnails();
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

		private View BuildOptionsBar()
		{
			Grid bar = new Grid();
			bar.HeightRequest = UiConstants.OptionsBarHeight;
			bar.ThemeBg(UiConstants.ChromeLight, UiConstants.ChromeDark);
			bar.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			bar.ColumnSpacing = 16.0;
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			m_optionsToolLabel = new Label();
			m_optionsToolLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_optionsToolLabel.FontSize = 12.0;
			m_optionsToolLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_optionsToolLabel, 0);
			bar.Add(m_optionsToolLabel);

			m_brushSizeLabel = new Label();
			m_brushSizeLabel.Text = "Size";
			m_brushSizeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushSizeLabel.FontSize = 12.0;
			m_brushSizeLabel.VerticalOptions = LayoutOptions.Center;

			m_brushSizeField = new SliderField(1, 100, m_toolState.BrushSize(), " px", OnBrushSizeValue);
			m_brushSizeField.VerticalOptions = LayoutOptions.Center;

			m_brushHardnessLabel = new Label();
			m_brushHardnessLabel.Text = "Hardness";
			m_brushHardnessLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushHardnessLabel.FontSize = 12.0;
			m_brushHardnessLabel.VerticalOptions = LayoutOptions.Center;
			m_brushHardnessLabel.IsVisible = false;

			m_brushHardnessField = new SliderField(0, 100, m_toolState.BrushHardness(), "%", OnBrushHardnessValue);
			m_brushHardnessField.VerticalOptions = LayoutOptions.Center;
			m_brushHardnessField.IsVisible = false;

			m_brushOpacityLabel = new Label();
			m_brushOpacityLabel.Text = "Opacity";
			m_brushOpacityLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushOpacityLabel.FontSize = 12.0;
			m_brushOpacityLabel.VerticalOptions = LayoutOptions.Center;
			m_brushOpacityLabel.IsVisible = false;

			m_brushOpacityField = new SliderField(1, 100, m_toolState.BrushOpacity(), "%", OnBrushOpacityValue);
			m_brushOpacityField.VerticalOptions = LayoutOptions.Center;
			m_brushOpacityField.IsVisible = false;

			m_brushFlowLabel = new Label();
			m_brushFlowLabel.Text = "Flow";
			m_brushFlowLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushFlowLabel.FontSize = 12.0;
			m_brushFlowLabel.VerticalOptions = LayoutOptions.Center;
			m_brushFlowLabel.IsVisible = false;

			m_brushFlowField = new SliderField(1, 100, m_toolState.BrushFlow(), "%", OnBrushFlowValue);
			m_brushFlowField.VerticalOptions = LayoutOptions.Center;
			m_brushFlowField.IsVisible = false;

			m_brushSmoothingLabel = new Label();
			m_brushSmoothingLabel.Text = "Smoothing";
			m_brushSmoothingLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushSmoothingLabel.FontSize = 12.0;
			m_brushSmoothingLabel.VerticalOptions = LayoutOptions.Center;
			m_brushSmoothingLabel.IsVisible = false;

			m_brushSmoothingField = new SliderField(0, 100, m_toolState.BrushSmoothing(), "%", OnBrushSmoothingValue);
			m_brushSmoothingField.VerticalOptions = LayoutOptions.Center;
			m_brushSmoothingField.IsVisible = false;

			m_brushStrengthLabel = new Label();
			m_brushStrengthLabel.Text = "Strength";
			m_brushStrengthLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushStrengthLabel.FontSize = 12.0;
			m_brushStrengthLabel.VerticalOptions = LayoutOptions.Center;
			m_brushStrengthLabel.IsVisible = false;

			m_brushStrengthField = new SliderField(1, 100, m_toolState.BrushStrength(), "%", OnBrushStrengthValue);
			m_brushStrengthField.VerticalOptions = LayoutOptions.Center;
			m_brushStrengthField.IsVisible = false;

			m_brushModeLabel = new Label();
			m_brushModeLabel.Text = "Mode";
			m_brushModeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushModeLabel.FontSize = 12.0;
			m_brushModeLabel.VerticalOptions = LayoutOptions.Center;
			m_brushModeLabel.IsVisible = false;

			m_brushModePicker = new Picker();
			m_brushModePicker.FontSize = 12.0;
			m_brushModePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_brushModePicker.WidthRequest = 110.0;
			m_brushModePicker.VerticalOptions = LayoutOptions.Center;
			m_brushModePicker.IsVisible = false;
			m_brushModePicker.Items.Add("Normal");
			m_brushModePicker.Items.Add("Multiply");
			m_brushModePicker.Items.Add("Screen");
			m_brushModePicker.Items.Add("Overlay");
			m_brushModePicker.Items.Add("Add");
			m_brushModePicker.SelectedIndex = 0;
			m_brushModePicker.SelectedIndexChanged += OnBrushModeChanged;

			m_brushSettingsButton = new Button();
			m_brushSettingsButton.Text = "Brush Settings";
			m_brushSettingsButton.FontSize = 12.0;
			m_brushSettingsButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_brushSettingsButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_brushSettingsButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushSettingsButton.VerticalOptions = LayoutOptions.Center;
			m_brushSettingsButton.IsVisible = false;
			m_brushSettingsButton.Clicked += OnBrushSettingsClicked;

			m_brushAirbrushLabel = new Label();
			m_brushAirbrushLabel.Text = "Airbrush";
			m_brushAirbrushLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_brushAirbrushLabel.FontSize = 12.0;
			m_brushAirbrushLabel.VerticalOptions = LayoutOptions.Center;
			m_brushAirbrushLabel.IsVisible = false;

			m_brushAirbrushCheck = new CheckBox();
			m_brushAirbrushCheck.VerticalOptions = LayoutOptions.Center;
			m_brushAirbrushCheck.IsVisible = false;
			m_brushAirbrushCheck.IsChecked = m_toolState.Airbrush();
			m_brushAirbrushCheck.CheckedChanged += OnBrushAirbrushChanged;

			m_cloneAlignedLabel = new Label();
			m_cloneAlignedLabel.Text = "Aligned";
			m_cloneAlignedLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_cloneAlignedLabel.FontSize = 12.0;
			m_cloneAlignedLabel.VerticalOptions = LayoutOptions.Center;
			m_cloneAlignedLabel.IsVisible = false;

			m_cloneAlignedCheck = new CheckBox();
			m_cloneAlignedCheck.VerticalOptions = LayoutOptions.Center;
			m_cloneAlignedCheck.IsVisible = false;
			m_cloneAlignedCheck.IsChecked = m_toolState.CloneAligned();
			m_cloneAlignedCheck.CheckedChanged += OnCloneAlignedChanged;

			m_spongeModeLabel = new Label();
			m_spongeModeLabel.Text = "Mode";
			m_spongeModeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_spongeModeLabel.FontSize = 12.0;
			m_spongeModeLabel.VerticalOptions = LayoutOptions.Center;
			m_spongeModeLabel.IsVisible = false;

			m_spongeModePicker = new Picker();
			m_spongeModePicker.FontSize = 12.0;
			m_spongeModePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_spongeModePicker.WidthRequest = 110.0;
			m_spongeModePicker.VerticalOptions = LayoutOptions.Center;
			m_spongeModePicker.IsVisible = false;
			m_spongeModePicker.Items.Add("Desaturate");
			m_spongeModePicker.Items.Add("Saturate");
			m_spongeModePicker.SelectedIndex = 0;
			m_spongeModePicker.SelectedIndexChanged += OnSpongeModeChanged;

			m_colorReplaceModeLabel = new Label();
			m_colorReplaceModeLabel.Text = "Mode";
			m_colorReplaceModeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_colorReplaceModeLabel.FontSize = 12.0;
			m_colorReplaceModeLabel.VerticalOptions = LayoutOptions.Center;
			m_colorReplaceModeLabel.IsVisible = false;

			m_colorReplaceModePicker = new Picker();
			m_colorReplaceModePicker.FontSize = 12.0;
			m_colorReplaceModePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_colorReplaceModePicker.WidthRequest = 120.0;
			m_colorReplaceModePicker.VerticalOptions = LayoutOptions.Center;
			m_colorReplaceModePicker.IsVisible = false;
			m_colorReplaceModePicker.Items.Add("Color");
			m_colorReplaceModePicker.Items.Add("Hue");
			m_colorReplaceModePicker.Items.Add("Saturation");
			m_colorReplaceModePicker.Items.Add("Luminosity");
			m_colorReplaceModePicker.SelectedIndex = m_toolState.ColorReplaceMode();
			m_colorReplaceModePicker.SelectedIndexChanged += OnColorReplaceModeChanged;

			m_colorReplaceToleranceLabel = new Label();
			m_colorReplaceToleranceLabel.Text = "Tolerance";
			m_colorReplaceToleranceLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_colorReplaceToleranceLabel.FontSize = 12.0;
			m_colorReplaceToleranceLabel.VerticalOptions = LayoutOptions.Center;
			m_colorReplaceToleranceLabel.IsVisible = false;

			m_colorReplaceToleranceField = new SliderField(0, 255, m_toolState.ColorReplaceTolerance(), "", OnColorReplaceToleranceValue);
			m_colorReplaceToleranceField.VerticalOptions = LayoutOptions.Center;
			m_colorReplaceToleranceField.IsVisible = false;

			m_dodgeBurnRangeLabel = new Label();
			m_dodgeBurnRangeLabel.Text = "Range";
			m_dodgeBurnRangeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_dodgeBurnRangeLabel.FontSize = 12.0;
			m_dodgeBurnRangeLabel.VerticalOptions = LayoutOptions.Center;
			m_dodgeBurnRangeLabel.IsVisible = false;

			m_dodgeBurnRangePicker = new Picker();
			m_dodgeBurnRangePicker.FontSize = 12.0;
			m_dodgeBurnRangePicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_dodgeBurnRangePicker.WidthRequest = 110.0;
			m_dodgeBurnRangePicker.VerticalOptions = LayoutOptions.Center;
			m_dodgeBurnRangePicker.IsVisible = false;
			m_dodgeBurnRangePicker.Items.Add("Shadows");
			m_dodgeBurnRangePicker.Items.Add("Midtones");
			m_dodgeBurnRangePicker.Items.Add("Highlights");
			m_dodgeBurnRangePicker.SelectedIndex = m_toolState.DodgeBurnRange();
			m_dodgeBurnRangePicker.SelectedIndexChanged += OnDodgeBurnRangeChanged;

			m_dodgeBurnExposureLabel = new Label();
			m_dodgeBurnExposureLabel.Text = "Exposure";
			m_dodgeBurnExposureLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_dodgeBurnExposureLabel.FontSize = 12.0;
			m_dodgeBurnExposureLabel.VerticalOptions = LayoutOptions.Center;
			m_dodgeBurnExposureLabel.IsVisible = false;

			m_dodgeBurnExposureField = new SliderField(1, 100, m_toolState.DodgeBurnExposure(), "%", OnDodgeBurnExposureValue);
			m_dodgeBurnExposureField.VerticalOptions = LayoutOptions.Center;
			m_dodgeBurnExposureField.IsVisible = false;

			m_gradientTypeLabel = new Label();
			m_gradientTypeLabel.Text = "Type";
			m_gradientTypeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_gradientTypeLabel.FontSize = 12.0;
			m_gradientTypeLabel.VerticalOptions = LayoutOptions.Center;
			m_gradientTypeLabel.IsVisible = false;

			m_gradientTypeNames = new string[] { "Linear", "Radial", "Angle", "Reflected", "Diamond" };
			m_gradientTypeButton = new Button();
			m_gradientTypeButton.FontSize = 12.0;
			m_gradientTypeButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_gradientTypeButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_gradientTypeButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_gradientTypeButton.WidthRequest = 110.0;
			m_gradientTypeButton.VerticalOptions = LayoutOptions.Center;
			m_gradientTypeButton.IsVisible = false;
			m_gradientTypeButton.Clicked += OnGradientTypeButtonClicked;

			m_gradientReverseLabel = new Label();
			m_gradientReverseLabel.Text = "Reverse";
			m_gradientReverseLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_gradientReverseLabel.FontSize = 12.0;
			m_gradientReverseLabel.VerticalOptions = LayoutOptions.Center;
			m_gradientReverseLabel.IsVisible = false;

			m_gradientReverseCheck = new CheckBox();
			m_gradientReverseCheck.VerticalOptions = LayoutOptions.Center;
			m_gradientReverseCheck.IsVisible = false;
			m_gradientReverseCheck.IsChecked = m_toolState.GradientReverse();
			m_gradientReverseCheck.CheckedChanged += OnGradientReverseChanged;

			m_gradientTransparentLabel = new Label();
			m_gradientTransparentLabel.Text = "To Transparent";
			m_gradientTransparentLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_gradientTransparentLabel.FontSize = 12.0;
			m_gradientTransparentLabel.VerticalOptions = LayoutOptions.Center;
			m_gradientTransparentLabel.IsVisible = false;

			m_gradientTransparentCheck = new CheckBox();
			m_gradientTransparentCheck.VerticalOptions = LayoutOptions.Center;
			m_gradientTransparentCheck.IsVisible = false;
			m_gradientTransparentCheck.IsChecked = m_toolState.GradientToTransparent();
			m_gradientTransparentCheck.CheckedChanged += OnGradientTransparentChanged;

			m_lineAntiAliasLabel = new Label();
			m_lineAntiAliasLabel.Text = "Anti-alias";
			m_lineAntiAliasLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_lineAntiAliasLabel.FontSize = 12.0;
			m_lineAntiAliasLabel.VerticalOptions = LayoutOptions.Center;
			m_lineAntiAliasLabel.IsVisible = false;

			m_lineAntiAliasCheck = new CheckBox();
			m_lineAntiAliasCheck.VerticalOptions = LayoutOptions.Center;
			m_lineAntiAliasCheck.IsVisible = false;
			m_lineAntiAliasCheck.CheckedChanged += OnLineAntiAliasChanged;

			m_toleranceLabel = new Label();
			m_toleranceLabel.Text = "Tolerance";
			m_toleranceLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_toleranceLabel.FontSize = 12.0;
			m_toleranceLabel.VerticalOptions = LayoutOptions.Center;
			m_toleranceLabel.IsVisible = false;

			m_toleranceField = new SliderField(0, 255, m_toolState.FillTolerance(), "", OnToleranceValue);
			m_toleranceField.VerticalOptions = LayoutOptions.Center;
			m_toleranceField.IsVisible = false;

			m_wandAntiAliasLabel = new Label();
			m_wandAntiAliasLabel.Text = "Anti-alias";
			m_wandAntiAliasLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_wandAntiAliasLabel.FontSize = 12.0;
			m_wandAntiAliasLabel.VerticalOptions = LayoutOptions.Center;
			m_wandAntiAliasLabel.IsVisible = false;

			m_wandAntiAliasCheck = new CheckBox();
			m_wandAntiAliasCheck.VerticalOptions = LayoutOptions.Center;
			m_wandAntiAliasCheck.IsVisible = false;
			m_wandAntiAliasCheck.IsChecked = m_toolState.WandAntiAlias();
			m_wandAntiAliasCheck.CheckedChanged += OnWandAntiAliasChanged;

			m_wandContiguousLabel = new Label();
			m_wandContiguousLabel.Text = "Contiguous";
			m_wandContiguousLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_wandContiguousLabel.FontSize = 12.0;
			m_wandContiguousLabel.VerticalOptions = LayoutOptions.Center;
			m_wandContiguousLabel.IsVisible = false;

			m_wandContiguousCheck = new CheckBox();
			m_wandContiguousCheck.VerticalOptions = LayoutOptions.Center;
			m_wandContiguousCheck.IsVisible = false;
			m_wandContiguousCheck.IsChecked = m_toolState.WandContiguous();
			m_wandContiguousCheck.CheckedChanged += OnWandContiguousChanged;

			m_wandSampleAllLabel = new Label();
			m_wandSampleAllLabel.Text = "Sample All Layers";
			m_wandSampleAllLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_wandSampleAllLabel.FontSize = 12.0;
			m_wandSampleAllLabel.VerticalOptions = LayoutOptions.Center;
			m_wandSampleAllLabel.IsVisible = false;

			m_wandSampleAllCheck = new CheckBox();
			m_wandSampleAllCheck.VerticalOptions = LayoutOptions.Center;
			m_wandSampleAllCheck.IsVisible = false;
			m_wandSampleAllCheck.IsChecked = m_toolState.WandSampleAll();
			m_wandSampleAllCheck.CheckedChanged += OnWandSampleAllChanged;

			m_magneticWidthLabel = new Label();
			m_magneticWidthLabel.Text = "Width";
			m_magneticWidthLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_magneticWidthLabel.FontSize = 12.0;
			m_magneticWidthLabel.VerticalOptions = LayoutOptions.Center;
			m_magneticWidthLabel.IsVisible = false;

			m_magneticWidthField = new SliderField(1, 40, m_toolState.MagneticWidth(), " px", OnMagneticWidthValue);
			m_magneticWidthField.VerticalOptions = LayoutOptions.Center;
			m_magneticWidthField.IsVisible = false;

			m_magneticContrastLabel = new Label();
			m_magneticContrastLabel.Text = "Contrast";
			m_magneticContrastLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_magneticContrastLabel.FontSize = 12.0;
			m_magneticContrastLabel.VerticalOptions = LayoutOptions.Center;
			m_magneticContrastLabel.IsVisible = false;

			m_magneticContrastField = new SliderField(0, 100, m_toolState.MagneticContrast(), "%", OnMagneticContrastValue);
			m_magneticContrastField.VerticalOptions = LayoutOptions.Center;
			m_magneticContrastField.IsVisible = false;

			m_textFontLabel = new Label();
			m_textFontLabel.Text = "Font";
			m_textFontLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textFontLabel.FontSize = 12.0;
			m_textFontLabel.VerticalOptions = LayoutOptions.Center;
			m_textFontLabel.IsVisible = false;

			m_fontFamilies = SkiaSharp.SKFontManager.Default.GetFontFamilies();
			System.Array.Sort(m_fontFamilies);

			m_textFontButton = new Button();
			m_textFontButton.FontSize = 12.0;
			m_textFontButton.WidthRequest = 160.0;
			m_textFontButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_textFontButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_textFontButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_textFontButton.VerticalOptions = LayoutOptions.Center;
			m_textFontButton.IsVisible = false;
			m_textFontButton.Clicked += OnFontButtonClicked;
			UpdateFontButtonText();

			m_textSizeLabel = new Label();
			m_textSizeLabel.Text = "Size";
			m_textSizeLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textSizeLabel.FontSize = 12.0;
			m_textSizeLabel.VerticalOptions = LayoutOptions.Center;
			m_textSizeLabel.IsVisible = false;

			m_textSizeField = new SliderField(6, 200, m_toolState.TextSize(), " px", OnTextSizeValue);
			m_textSizeField.VerticalOptions = LayoutOptions.Center;
			m_textSizeField.IsVisible = false;

			m_textStyleLabel = new Label();
			m_textStyleLabel.Text = "Style";
			m_textStyleLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textStyleLabel.FontSize = 12.0;
			m_textStyleLabel.VerticalOptions = LayoutOptions.Center;
			m_textStyleLabel.IsVisible = false;

			m_textStyleButton = new Button();
			m_textStyleButton.FontSize = 12.0;
			m_textStyleButton.WidthRequest = 110.0;
			m_textStyleButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_textStyleButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_textStyleButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_textStyleButton.VerticalOptions = LayoutOptions.Center;
			m_textStyleButton.IsVisible = false;
			m_textStyleButton.Clicked += OnStyleButtonClicked;
			UpdateStyleButtonText();

			m_textAlignLabel = new Label();
			m_textAlignLabel.Text = "Align";
			m_textAlignLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textAlignLabel.FontSize = 12.0;
			m_textAlignLabel.VerticalOptions = LayoutOptions.Center;
			m_textAlignLabel.IsVisible = false;

			m_textAlignPicker = new Picker();
			m_textAlignPicker.FontSize = 12.0;
			m_textAlignPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_textAlignPicker.VerticalOptions = LayoutOptions.Center;
			m_textAlignPicker.IsVisible = false;
			m_textAlignPicker.Items.Add("Left");
			m_textAlignPicker.Items.Add("Center");
			m_textAlignPicker.Items.Add("Right");
			m_textAlignPicker.SelectedIndex = m_toolState.TextAlign();
			m_textAlignPicker.SelectedIndexChanged += OnTextAlignChanged;

			m_textAntiAliasLabel = new Label();
			m_textAntiAliasLabel.Text = "Anti-alias";
			m_textAntiAliasLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textAntiAliasLabel.FontSize = 12.0;
			m_textAntiAliasLabel.VerticalOptions = LayoutOptions.Center;
			m_textAntiAliasLabel.IsVisible = false;

			m_textAntiAliasPicker = new Picker();
			m_textAntiAliasPicker.FontSize = 12.0;
			m_textAntiAliasPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_textAntiAliasPicker.VerticalOptions = LayoutOptions.Center;
			m_textAntiAliasPicker.IsVisible = false;
			m_textAntiAliasPicker.Items.Add("None");
			m_textAntiAliasPicker.Items.Add("Sharp");
			m_textAntiAliasPicker.Items.Add("Crisp");
			m_textAntiAliasPicker.Items.Add("Strong");
			m_textAntiAliasPicker.Items.Add("Smooth");
			m_textAntiAliasPicker.SelectedIndex = m_toolState.TextAntiAlias();
			m_textAntiAliasPicker.SelectedIndexChanged += OnTextAntiAliasChanged;

			m_textColorLabel = new Label();
			m_textColorLabel.Text = "Color";
			m_textColorLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_textColorLabel.FontSize = 12.0;
			m_textColorLabel.VerticalOptions = LayoutOptions.Center;
			m_textColorLabel.IsVisible = false;

			m_textColorSwatch = new BoxView();
			m_textColorSwatch.WidthRequest = 22.0;
			m_textColorSwatch.HeightRequest = 18.0;
			m_textColorSwatch.Color = FromSkColor(m_toolState.Foreground());
			m_textColorSwatch.VerticalOptions = LayoutOptions.Center;
			m_textColorSwatch.IsVisible = false;
			TapGestureRecognizer textColorTap = new TapGestureRecognizer();
			textColorTap.Tapped += OnTextColorTapped;
			m_textColorSwatch.GestureRecognizers.Add(textColorTap);

			m_textCharButton = new Button();
			m_textCharButton.Text = "Character…";
			m_textCharButton.FontSize = 12.0;
			m_textCharButton.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			m_textCharButton.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			m_textCharButton.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_textCharButton.VerticalOptions = LayoutOptions.Center;
			m_textCharButton.IsVisible = false;
			m_textCharButton.Clicked += OnTextCharClicked;

			HorizontalStackLayout options = new HorizontalStackLayout();
			m_optionsRow = options;
			options.Spacing = 8.0;
			options.VerticalOptions = LayoutOptions.Center;
			options.Add(m_brushSizeLabel);
			options.Add(m_brushSizeField);
			options.Add(m_brushHardnessLabel);
			options.Add(m_brushHardnessField);
			options.Add(m_brushOpacityLabel);
			options.Add(m_brushOpacityField);
			options.Add(m_brushFlowLabel);
			options.Add(m_brushFlowField);
			options.Add(m_brushStrengthLabel);
			options.Add(m_brushStrengthField);
			options.Add(m_brushSmoothingLabel);
			options.Add(m_brushSmoothingField);
			options.Add(m_brushModeLabel);
			options.Add(m_brushModePicker);
			options.Add(m_brushAirbrushLabel);
			options.Add(m_brushAirbrushCheck);
			options.Add(m_cloneAlignedLabel);
			options.Add(m_cloneAlignedCheck);
			options.Add(m_spongeModeLabel);
			options.Add(m_spongeModePicker);
			options.Add(m_colorReplaceModeLabel);
			options.Add(m_colorReplaceModePicker);
			options.Add(m_colorReplaceToleranceLabel);
			options.Add(m_colorReplaceToleranceField);
			options.Add(m_dodgeBurnRangeLabel);
			options.Add(m_dodgeBurnRangePicker);
			options.Add(m_dodgeBurnExposureLabel);
			options.Add(m_dodgeBurnExposureField);
			options.Add(m_gradientTypeLabel);
			options.Add(m_gradientTypeButton);
			options.Add(m_gradientReverseLabel);
			options.Add(m_gradientReverseCheck);
			options.Add(m_gradientTransparentLabel);
			options.Add(m_gradientTransparentCheck);
			options.Add(m_brushSettingsButton);
			options.Add(m_lineAntiAliasLabel);
			options.Add(m_lineAntiAliasCheck);
			options.Add(m_toleranceLabel);
			options.Add(m_toleranceField);
			options.Add(m_wandAntiAliasLabel);
			options.Add(m_wandAntiAliasCheck);
			options.Add(m_wandContiguousLabel);
			options.Add(m_wandContiguousCheck);
			options.Add(m_wandSampleAllLabel);
			options.Add(m_wandSampleAllCheck);
			options.Add(m_magneticWidthLabel);
			options.Add(m_magneticWidthField);
			options.Add(m_magneticContrastLabel);
			options.Add(m_magneticContrastField);
			options.Add(m_textFontLabel);
			options.Add(m_textFontButton);
			options.Add(m_textSizeLabel);
			options.Add(m_textSizeField);
			options.Add(m_textStyleLabel);
			options.Add(m_textStyleButton);
			options.Add(m_textAlignLabel);
			options.Add(m_textAlignPicker);
			options.Add(m_textAntiAliasLabel);
			options.Add(m_textAntiAliasPicker);
			options.Add(m_textColorLabel);
			options.Add(m_textColorSwatch);
			options.Add(m_textCharButton);
			Grid.SetColumn(options, 1);
			bar.Add(options);

			m_lineAntiAliasCheck.IsChecked = m_toolState.LineAntiAlias();

			return bar;
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
			m_statusInfoLabel.FontSize = 11.0;
			m_statusInfoLabel.VerticalOptions = LayoutOptions.Center;
			Grid.SetColumn(m_statusInfoLabel, 0);
			bar.Add(m_statusInfoLabel);

			m_statusCursorLabel = new Label();
			m_statusCursorLabel.Text = "x: —   y: —";
			m_statusCursorLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			m_statusCursorLabel.FontSize = 11.0;
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
			m_openItemButtons = new List<Border>();
			m_openItemActions = new List<string>();
			m_openMenuIndex = -1;
			m_untitledCount = 0;
			m_cascadeCount = 0;
			m_topZIndex = 0;
			m_toolState = new ToolState();
			m_moveTool = new MoveTool();
			m_rectangleSelectTool = new RectangleSelectTool();
			m_ellipseSelectTool = new EllipseSelectTool();
			m_lassoTool = new LassoTool();
			m_freehandLassoTool = new FreehandLassoTool();
			m_magneticLassoTool = new MagneticLassoTool();
			m_magicWandTool = new MagicWandTool();
			m_textTool = new TextTool();
			m_pencilTool = new PencilTool();
			m_brushTool = new BrushTool();
			m_eraserTool = new EraserTool();
			m_dodgeBurnTool = new DodgeBurnTool();
			m_blurTool = new BlurTool();
			m_spongeTool = new SpongeTool();
			m_colorReplacementTool = new ColorReplacementTool();
			m_sharpenTool = new SharpenTool();
			m_cloneTool = new CloneTool();
			m_healTool = new HealTool();
			m_sliceTool = new SliceTool();
			m_smudgeTool = new SmudgeTool();
			m_eyedropperTool = new EyedropperTool();
			m_fillTool = new FillTool();
			m_gradientTool = new GradientTool();
			m_lineTool = new LineTool();
			m_rectangleShapeTool = new ShapeTool(eShapeKind.Rectangle);
			m_roundedRectangleShapeTool = new ShapeTool(eShapeKind.RoundedRectangle);
			m_ellipseShapeTool = new ShapeTool(eShapeKind.Ellipse);
			m_polygonShapeTool = new ShapeTool(eShapeKind.Polygon);
			m_handTool = new HandTool();
			m_zoomTool = new ZoomTool();
			m_rulerTool = new RulerTool();
			m_cropTool = new CropTool();
			m_freeTransformTool = new FreeTransformTool();
			m_previousTool = eTool.Brush;
			m_guideCreateOrientation = 0;
			m_guideCreateCanvas = null;
			m_gridEnabled = false;
			m_channelViewMode = -1;
			m_snapEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("snap_enabled", true);
			m_snapTargetGuides = Microsoft.Maui.Storage.Preferences.Default.Get("snap_target_guides", true);
			m_snapTargetGrid = Microsoft.Maui.Storage.Preferences.Default.Get("snap_target_grid", true);
			m_snapTargetEdges = Microsoft.Maui.Storage.Preferences.Default.Get("snap_target_edges", true);
			m_snapTargetLayerBounds = Microsoft.Maui.Storage.Preferences.Default.Get("snap_target_layer_bounds", true);
			m_submenuParentRows = new List<Border>();
			m_submenuParentNames = new List<string>();
			m_submenuParentIndices = new List<int>();
			m_submenuChildRows = new List<Border>();
			m_submenuBorder = null;
			m_recentMenuPaths = new List<string>();

			View menuBar = BuildMenuBar();
			View optionsBar = BuildOptionsBar();
			View middle = BuildMiddle();
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

			m_overlay = new AbsoluteLayout();
			m_overlay.InputTransparent = true;
			m_overlay.CascadeInputTransparent = false;

			Grid outer = new Grid();
			outer.Add(root);
			outer.Add(m_overlay);

			Content = outer;
		}

		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();
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
			if (m_textEditActive)
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
			if (m_textEditActive)
			{
				return;
			}
			DoClearSelection();
			args.Handled = true;
		}

		private void OnAcceleratorDeleteForeground(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			SKColor foreground = m_toolState.Foreground();
			FillSelectionWith(new SKColor(foreground.Red, foreground.Green, foreground.Blue, 255), true);
			args.Handled = true;
		}

		private void OnAcceleratorDeleteBackground(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
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
			if (m_textEditActive)
			{
				return;
			}
			DoUndo();
			args.Handled = true;
		}

		private void OnAcceleratorRedo(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			DoRedo();
			args.Handled = true;
		}

		private void OnAcceleratorSelectAll(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			DoSelectAll();
			args.Handled = true;
		}

		private void OnAcceleratorDeselect(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			DoDeselect();
			args.Handled = true;
		}

		private void OnAcceleratorCopy(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			DoCopy();
			args.Handled = true;
		}

		private void OnAcceleratorPaste(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			DoPaste();
			args.Handled = true;
		}

		private void OnAcceleratorCut(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
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
			if (m_textEditActive)
			{
				return;
			}
			DoInvertSelection();
			args.Handled = true;
		}

		private void OnAcceleratorInvertColors(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			DoInvert();
			args.Handled = true;
		}

		private void OnAcceleratorMergeSelected(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			MergeSelectedLayers();
			args.Handled = true;
		}

		private void OnAcceleratorMergeVisible(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
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
			RefreshLayerThumbnails();
		}

		private void OnAcceleratorRulers(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			ToggleRulers();
			args.Handled = true;
		}

		private void OnAcceleratorTransform(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			BeginTransform(0);
			args.Handled = true;
		}

		private bool TransformActive()
		{
			if (m_toolState == null || m_freeTransformTool == null)
			{
				return false;
			}
			return m_toolState.Tool() == eTool.FreeTransform && m_freeTransformTool.HasPreview();
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
			if (m_textEditActive)
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
				m_freeTransformTool.Commit(document);
			}
			EndTransformMode();
			RefreshTransformCanvas();
			args.Handled = true;
		}

		private void OnAcceleratorCancelTransform(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_textEditActive)
			{
				return;
			}
			if (m_modalContent != null)
			{
				CloseModal();
				args.Handled = true;
				return;
			}
			if (!TransformActive())
			{
				return;
			}
			m_freeTransformTool.Cancel();
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

		private void ShowModal(View content, double width, double height)
		{
			CloseModal();
			m_modalBackdrop = new BoxView();
			m_modalBackdrop.Color = Colors.Transparent;
			AbsoluteLayout.SetLayoutBounds(m_modalBackdrop, new Rect(0.0, 0.0, 1.0, 1.0));
			AbsoluteLayout.SetLayoutFlags(m_modalBackdrop, AbsoluteLayoutFlags.All);
			TapGestureRecognizer backdropTap = new TapGestureRecognizer();
			backdropTap.Tapped += OnModalBackdropTapped;
			m_modalBackdrop.GestureRecognizers.Add(backdropTap);
			m_topZIndex = m_topZIndex + 1;
			m_modalBackdrop.ZIndex = m_topZIndex + 1000;
			m_workspace.Add(m_modalBackdrop);

			m_modalContent = content;
			m_modalWidth = width;
			m_modalHeight = height;
			m_modalX = (m_workspace.Width - width) / 2.0;
			m_modalY = (m_workspace.Height - height) / 2.0;
			if (m_modalX < 0.0)
			{
				m_modalX = 0.0;
			}
			if (m_modalY < 0.0)
			{
				m_modalY = 0.0;
			}
			AbsoluteLayout.SetLayoutBounds(content, new Rect(m_modalX, m_modalY, width, AbsoluteLayout.AutoSize));
			AbsoluteLayout.SetLayoutFlags(content, AbsoluteLayoutFlags.None);
			content.ZIndex = m_topZIndex + 1001;
			m_workspace.Add(content);
		}

		public void DragModal(Microsoft.Maui.GestureStatus status, double totalX, double totalY)
		{
			if (m_modalContent == null)
			{
				return;
			}
			if (status == Microsoft.Maui.GestureStatus.Started)
			{
				m_modalDragOriginX = m_modalX;
				m_modalDragOriginY = m_modalY;
				return;
			}
			if (status != Microsoft.Maui.GestureStatus.Running)
			{
				return;
			}
			double targetX = m_modalDragOriginX + totalX;
			double targetY = m_modalDragOriginY + totalY;
			double maxX = m_workspace.Width - m_modalWidth;
			double maxY = m_workspace.Height - m_modalHeight;
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
			m_modalX = targetX;
			m_modalY = targetY;
			AbsoluteLayout.SetLayoutBounds(m_modalContent, new Rect(m_modalX, m_modalY, m_modalWidth, AbsoluteLayout.AutoSize));
		}

		public void CloseModal()
		{
			if (m_modalBackdrop != null)
			{
				m_workspace.Remove(m_modalBackdrop);
				m_modalBackdrop = null;
			}
			if (m_modalContent != null)
			{
				m_workspace.Remove(m_modalContent);
				m_modalContent = null;
			}
		}

		public void OpenColorPicker(bool foreground)
		{
			SKColor initial = m_toolState.Background();
			if (foreground)
			{
				initial = m_toolState.Foreground();
			}
			ColorPicker picker = new ColorPicker(initial, foreground);
			ShowModal(picker, 380.0, 360.0);
		}

		public void ApplyPickedColor(SKColor color, bool foreground)
		{
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
			if (foreground && m_textColorSwatch != null)
			{
				m_textColorSwatch.Color = FromSkColor(color);
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
			CloseModal();
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
			}
		}

		public void OnCloseDontSave()
		{
			CloseModal();
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
			if (window != null && m_activeDocumentWindow == window)
			{
				m_activeDocumentWindow = null;
				RefreshDocumentTitleBars();
			}
		}

		public void OnToolSelected(eTool tool)
		{
			ClosePulldown();
			if (m_toolState != null)
			{
				m_toolState.SetTool(tool);
			}
			if (m_optionsToolLabel != null)
			{
				m_optionsToolLabel.Text = tool.ToString();
			}
			bool isLine = tool == eTool.Line;
			if (m_lineAntiAliasLabel != null)
			{
				m_lineAntiAliasLabel.IsVisible = isLine;
			}
			if (m_lineAntiAliasCheck != null)
			{
				m_lineAntiAliasCheck.IsVisible = isLine;
			}
			bool isWand = tool == eTool.MagicWand;
			bool usesTolerance = isWand || tool == eTool.Fill;
			if (m_toleranceLabel != null)
			{
				m_toleranceLabel.IsVisible = usesTolerance;
				m_toleranceField.IsVisible = usesTolerance;
				m_wandAntiAliasLabel.IsVisible = isWand;
				m_wandAntiAliasCheck.IsVisible = isWand;
				m_wandContiguousLabel.IsVisible = isWand;
				m_wandContiguousCheck.IsVisible = isWand;
				m_wandSampleAllLabel.IsVisible = isWand;
				m_wandSampleAllCheck.IsVisible = isWand;
			}
			bool isMagnetic = tool == eTool.MagneticLasso;
			if (m_magneticWidthLabel != null)
			{
				m_magneticWidthLabel.IsVisible = isMagnetic;
				m_magneticWidthField.IsVisible = isMagnetic;
				m_magneticContrastLabel.IsVisible = isMagnetic;
				m_magneticContrastField.IsVisible = isMagnetic;
			}
			bool isText = tool == eTool.Text;
			if (m_textFontLabel != null)
			{
				m_textFontLabel.IsVisible = isText;
				m_textFontButton.IsVisible = isText;
				m_textSizeLabel.IsVisible = isText;
				m_textSizeField.IsVisible = isText;
				m_textStyleLabel.IsVisible = isText;
				m_textStyleButton.IsVisible = isText;
				m_textAlignLabel.IsVisible = isText;
				m_textAlignPicker.IsVisible = isText;
				m_textAntiAliasLabel.IsVisible = isText;
				m_textAntiAliasPicker.IsVisible = isText;
				m_textColorLabel.IsVisible = isText;
				m_textColorSwatch.IsVisible = isText;
				m_textCharButton.IsVisible = isText;
			}
			if (!isText)
			{
				CommitTextEdit();
			}
			bool isSponge = tool == eTool.Sponge;
			bool isColorReplace = tool == eTool.ColorReplacement;
			bool isStrengthTool = tool == eTool.Blur || tool == eTool.Sharpen || tool == eTool.Smudge;
			bool isBrushFamily = tool == eTool.Brush || tool == eTool.Eraser || tool == eTool.Clone || tool == eTool.Heal || tool == eTool.Blur || tool == eTool.Sharpen || tool == eTool.Smudge || tool == eTool.DodgeBurn || isSponge || isColorReplace;
			bool showsBlendMode = isBrushFamily && !isSponge && !isColorReplace && !isStrengthTool;
			bool usesSize = isBrushFamily || tool == eTool.Pencil || tool == eTool.Line;
			if (m_brushSizeLabel != null)
			{
				m_brushSizeLabel.IsVisible = usesSize;
				m_brushSizeField.IsVisible = usesSize;
			}
			if (m_brushHardnessLabel != null)
			{
				m_brushHardnessLabel.IsVisible = isBrushFamily;
				m_brushHardnessField.IsVisible = isBrushFamily;
				bool showsOpacityFlow = isBrushFamily && !isStrengthTool;
				m_brushOpacityLabel.IsVisible = showsOpacityFlow;
				m_brushOpacityField.IsVisible = showsOpacityFlow;
				m_brushFlowLabel.IsVisible = showsOpacityFlow;
				m_brushFlowField.IsVisible = showsOpacityFlow;
				m_brushStrengthLabel.IsVisible = isStrengthTool;
				m_brushStrengthField.IsVisible = isStrengthTool;
				m_brushSmoothingLabel.IsVisible = isBrushFamily;
				m_brushSmoothingField.IsVisible = isBrushFamily;
				m_brushModeLabel.IsVisible = showsBlendMode;
				m_brushModePicker.IsVisible = showsBlendMode;
				m_brushAirbrushLabel.IsVisible = isBrushFamily;
				m_brushAirbrushCheck.IsVisible = isBrushFamily;
				m_brushSettingsButton.IsVisible = isBrushFamily;
				bool isCloneOrHeal = tool == eTool.Clone || tool == eTool.Heal;
				m_cloneAlignedLabel.IsVisible = isCloneOrHeal;
				m_cloneAlignedCheck.IsVisible = isCloneOrHeal;
			}
			if (m_spongeModeLabel != null)
			{
				m_spongeModeLabel.IsVisible = isSponge;
				m_spongeModePicker.IsVisible = isSponge;
				m_colorReplaceModeLabel.IsVisible = isColorReplace;
				m_colorReplaceModePicker.IsVisible = isColorReplace;
				m_colorReplaceToleranceLabel.IsVisible = isColorReplace;
				m_colorReplaceToleranceField.IsVisible = isColorReplace;
				bool isDodgeBurn = tool == eTool.DodgeBurn;
				m_dodgeBurnRangeLabel.IsVisible = isDodgeBurn;
				m_dodgeBurnRangePicker.IsVisible = isDodgeBurn;
				m_dodgeBurnExposureLabel.IsVisible = isDodgeBurn;
				m_dodgeBurnExposureField.IsVisible = isDodgeBurn;
			}
			bool isGradient = tool == eTool.Gradient;
			if (m_gradientTypeLabel != null)
			{
				m_gradientTypeLabel.IsVisible = isGradient;
				m_gradientTypeButton.IsVisible = isGradient;
				if (isGradient)
				{
					UpdateGradientTypeButtonText();
				}
				m_gradientReverseLabel.IsVisible = isGradient;
				m_gradientReverseCheck.IsVisible = isGradient;
				m_gradientTransparentLabel.IsVisible = isGradient;
				m_gradientTransparentCheck.IsVisible = isGradient;
			}
			if (m_lassoTool != null)
			{
				m_lassoTool.Reset();
			}
			if (m_freehandLassoTool != null)
			{
				m_freehandLassoTool.Reset();
			}
			if (m_magneticLassoTool != null)
			{
				m_magneticLassoTool.Reset();
			}
			if (m_rulerTool != null)
			{
				m_rulerTool.Reset();
			}
			if (m_cropTool != null)
			{
				m_cropTool.Reset();
			}
			if (m_sliceTool != null)
			{
				m_sliceTool.Reset();
			}
			if (m_freeTransformTool != null)
			{
				m_freeTransformTool.Reset();
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

		private void OnToleranceValue(int tolerance)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetFillTolerance(tolerance);
		}

		private void OnWandAntiAliasChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetWandAntiAlias(m_wandAntiAliasCheck.IsChecked);
		}

		private void OnWandContiguousChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetWandContiguous(m_wandContiguousCheck.IsChecked);
		}

		private void OnWandSampleAllChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetWandSampleAll(m_wandSampleAllCheck.IsChecked);
		}

		private void OnMagneticWidthValue(int width)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetMagneticWidth(width);
		}

		private void OnMagneticContrastValue(int contrast)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetMagneticContrast(contrast);
		}

		private void OnBrushHardnessValue(int hardness)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushHardness(hardness);
		}

		private void OnBrushOpacityValue(int opacity)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushOpacity(opacity);
		}

		private void OnBrushFlowValue(int flow)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushFlow(flow);
		}

		private void OnBrushSmoothingValue(int smoothing)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSmoothing(smoothing);
		}

		private void OnBrushStrengthValue(int strength)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushStrength(strength);
		}

		private void OnBrushModeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_brushModePicker.SelectedIndex;
			if (index < 0)
			{
				index = 0;
			}
			m_toolState.SetBrushMode((Bitmute.Imaging.eBlendMode)index);
		}

		private void OnSpongeModeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetSpongeSaturate(m_spongeModePicker.SelectedIndex == 1);
		}

		private void OnColorReplaceModeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_colorReplaceModePicker.SelectedIndex;
			if (index < 0)
			{
				index = 0;
			}
			m_toolState.SetColorReplaceMode(index);
		}

		private void OnColorReplaceToleranceValue(int tolerance)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetColorReplaceTolerance(tolerance);
		}

		private void OnDodgeBurnRangeChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_dodgeBurnRangePicker.SelectedIndex;
			if (index < 0)
			{
				index = 0;
			}
			m_toolState.SetDodgeBurnRange(index);
		}

		private void OnDodgeBurnExposureValue(int exposure)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetDodgeBurnExposure(exposure);
		}

		private void UpdateGradientTypeButtonText()
		{
			if (m_gradientTypeButton == null || m_toolState == null)
			{
				return;
			}
			int index = m_toolState.GradientType();
			if (index < 0 || index >= m_gradientTypeNames.Length)
			{
				index = 0;
			}
			m_gradientTypeButton.Text = m_gradientTypeNames[index];
		}

		private Microsoft.Maui.Controls.ImageSource RenderGradientSwatch(int type)
		{
			int width = 120;
			int height = 16;
			SkiaSharp.SKBitmap swatch = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
			Bitmute.Imaging.eGradientType gradientType = (Bitmute.Imaging.eGradientType)type;
			float startX = 0.0f;
			float startY = height / 2.0f;
			float endX = width;
			float endY = height / 2.0f;
			if (gradientType == Bitmute.Imaging.eGradientType.Reflected)
			{
				endX = width / 2.0f;
			}
			else if (gradientType != Bitmute.Imaging.eGradientType.Linear)
			{
				startX = width / 2.0f;
			}
			SkiaSharp.SKColor startColor = m_toolState.Foreground();
			SkiaSharp.SKColor endColor = m_toolState.Background();
			if (m_toolState.GradientToTransparent())
			{
				endColor = new SkiaSharp.SKColor(startColor.Red, startColor.Green, startColor.Blue, 0);
			}
			Bitmute.Imaging.GradientFill.Fill(swatch, gradientType, startX, startY, endX, endY, startColor, endColor, m_toolState.GradientReverse());
			SkiaSharp.Views.Maui.Controls.SKBitmapImageSource source = new SkiaSharp.Views.Maui.Controls.SKBitmapImageSource();
			source.Bitmap = swatch;
			return source;
		}

		private void OnGradientTypeButtonClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_pulldownPanel != null || PulldownJustDismissed())
			{
				ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_gradientTypeButton != null)
			{
				anchorX = m_optionsRow.X + m_gradientTypeButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			ShowPulldown(BuildGradientTypePulldownContent(), anchorX, anchorY, 190.0, 130.0);
		}

		private View BuildGradientTypePulldownContent()
		{
			m_gradientTypeRows = new List<View>();
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 2.0;
			list.Padding = new Thickness(4.0);
			for (int index = 0; index < m_gradientTypeNames.Length; index++)
			{
				HorizontalStackLayout row = new HorizontalStackLayout();
				row.Spacing = 6.0;
				row.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
				Image swatch = new Image();
				swatch.Source = RenderGradientSwatch(index);
				swatch.WidthRequest = 120.0;
				swatch.HeightRequest = 16.0;
				swatch.VerticalOptions = LayoutOptions.Center;
				Label name = new Label();
				name.Text = m_gradientTypeNames[index];
				name.FontSize = 12.0;
				name.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				name.VerticalOptions = LayoutOptions.Center;
				row.Add(swatch);
				row.Add(name);
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnGradientTypeRowTapped;
				row.GestureRecognizers.Add(tap);
				m_gradientTypeRows.Add(row);
				list.Add(row);
			}
			return list;
		}

		private void OnGradientTypeRowTapped(object sender, TappedEventArgs eventArgs)
		{
			if (m_toolState == null || m_gradientTypeRows == null)
			{
				return;
			}
			for (int index = 0; index < m_gradientTypeRows.Count; index++)
			{
				if (ReferenceEquals(m_gradientTypeRows[index], sender))
				{
					m_toolState.SetGradientType(index);
					UpdateGradientTypeButtonText();
					ClosePulldown();
					return;
				}
			}
		}

		private void OnGradientReverseChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetGradientReverse(m_gradientReverseCheck.IsChecked);
		}

		private void OnGradientTransparentChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetGradientToTransparent(m_gradientTransparentCheck.IsChecked);
		}

		private void OnBrushAirbrushChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetAirbrush(m_brushAirbrushCheck.IsChecked);
		}

		private void OnCloneAlignedChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetCloneAligned(m_cloneAlignedCheck.IsChecked);
		}

		private void OnBrushSettingsClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			if (m_pulldownPanel != null || PulldownJustDismissed())
			{
				ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_brushSettingsButton != null)
			{
				anchorX = m_optionsRow.X + m_brushSettingsButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			ShowPulldown(BuildBrushSettingsContent(), anchorX, anchorY, 288.0, 108.0);
		}

		private View BuildBrushSettingsContent()
		{
			Label tipLabel = new Label();
			tipLabel.Text = "Tip";
			tipLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			tipLabel.FontSize = 12.0;
			tipLabel.WidthRequest = 60.0;
			tipLabel.VerticalOptions = LayoutOptions.Center;

			m_brushTipPicker = new Picker();
			m_brushTipPicker.FontSize = 12.0;
			m_brushTipPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_brushTipPicker.Items.Add("Round");
			m_brushTipPicker.Items.Add("Square");
			m_brushTipPicker.SelectedIndex = 0;
			if (m_toolState.BrushSquareTip())
			{
				m_brushTipPicker.SelectedIndex = 1;
			}
			m_brushTipPicker.SelectedIndexChanged += OnBrushTipPulldownChanged;

			Grid tipRow = new Grid();
			tipRow.ColumnSpacing = 8.0;
			tipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			tipRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(tipLabel, 0);
			Grid.SetColumn(m_brushTipPicker, 1);
			tipRow.Add(tipLabel);
			tipRow.Add(m_brushTipPicker);

			Label spacingLabel = new Label();
			spacingLabel.Text = "Spacing";
			spacingLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			spacingLabel.FontSize = 12.0;
			spacingLabel.WidthRequest = 60.0;
			spacingLabel.VerticalOptions = LayoutOptions.Center;

			m_brushSpacingSlider = new Slider();
			m_brushSpacingSlider.Minimum = 1.0;
			m_brushSpacingSlider.Maximum = 100.0;
			m_brushSpacingSlider.WidthRequest = 140.0;
			m_brushSpacingSlider.VerticalOptions = LayoutOptions.Center;
			m_brushSpacingSlider.Value = m_toolState.BrushSpacing();
			m_brushSpacingSlider.ValueChanged += OnBrushSpacingPulldownChanged;

			m_brushSpacingValue = new Label();
			m_brushSpacingValue.Text = m_toolState.BrushSpacing() + "%";
			m_brushSpacingValue.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_brushSpacingValue.FontSize = 12.0;
			m_brushSpacingValue.WidthRequest = 44.0;
			m_brushSpacingValue.VerticalOptions = LayoutOptions.Center;

			Grid spacingRow = new Grid();
			spacingRow.ColumnSpacing = 8.0;
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			spacingRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(spacingLabel, 0);
			Grid.SetColumn(m_brushSpacingSlider, 1);
			Grid.SetColumn(m_brushSpacingValue, 2);
			spacingRow.Add(spacingLabel);
			spacingRow.Add(m_brushSpacingSlider);
			spacingRow.Add(m_brushSpacingValue);

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 10.0;
			body.Padding = new Thickness(12.0);
			body.Add(tipRow);
			body.Add(spacingRow);
			return body;
		}

		private void OnBrushTipPulldownChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_brushTipPicker == null)
			{
				return;
			}
			ApplyBrushTip(m_brushTipPicker.SelectedIndex == 1);
		}

		private void OnBrushSpacingPulldownChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_brushSpacingSlider == null)
			{
				return;
			}
			int spacing = (int)m_brushSpacingSlider.Value;
			ApplyBrushSpacing(spacing);
			if (m_brushSpacingValue != null)
			{
				m_brushSpacingValue.Text = spacing + "%";
			}
		}

		private void ClosePulldown()
		{
			if (m_pulldownPanel != null)
			{
				m_overlay.Remove(m_pulldownPanel);
				m_pulldownPanel = null;
			}
		}

		public bool PulldownJustDismissed()
		{
			return (System.Environment.TickCount64 - m_pulldownDismissTick) < 300;
		}

		public void ShowPulldown(View content, double anchorX, double anchorY, double width, double height)
		{
			ClosePulldown();
			CloseMenu();

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
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSquareTip(square);
		}

		public void ApplyBrushSpacing(int spacing)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSpacing(spacing);
		}

		private void OnLineAntiAliasChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetLineAntiAlias(m_lineAntiAliasCheck.IsChecked);
		}

		private void UpdateFontButtonText()
		{
			if (m_textFontButton == null || m_toolState == null)
			{
				return;
			}
			m_textFontButton.Text = m_toolState.TextFontFamily();
			m_textFontButton.FontFamily = m_toolState.TextFontFamily();
		}

		private void OnFontButtonClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_pulldownPanel != null || PulldownJustDismissed())
			{
				ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_textFontButton != null)
			{
				anchorX = m_optionsRow.X + m_textFontButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			ShowPulldown(BuildFontPulldownContent(), anchorX, anchorY, 240.0, 320.0);
		}

		private View BuildFontPulldownContent()
		{
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(4.0);
			for (int index = 0; index < m_fontFamilies.Length; index++)
			{
				string family = m_fontFamilies[index];
				Label row = new Label();
				row.Text = family;
				row.FontFamily = family;
				row.FontSize = 15.0;
				row.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
				row.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnFontRowTapped;
				row.GestureRecognizers.Add(tap);
				list.Add(row);
			}
			ScrollView scroll = new ScrollView();
			scroll.Content = list;
			return scroll;
		}

		private void OnFontRowTapped(object sender, TappedEventArgs eventArgs)
		{
			Label row = sender as Label;
			if (row == null || m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextFontFamily(row.Text);
			UpdateFontButtonText();
			ClosePulldown();
			RefreshTextEditStyle();
		}

		private void OnTextSizeValue(int size)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextSize(size);
			RefreshTextEditStyle();
		}

		private void UpdateStyleButtonText()
		{
			if (m_textStyleButton == null || m_toolState == null)
			{
				return;
			}
			m_textStyleButton.Text = StyleName(m_toolState.TextBold(), m_toolState.TextItalic());
		}

		private static string StyleName(bool bold, bool italic)
		{
			if (bold && italic)
			{
				return "Bold Italic";
			}
			if (bold)
			{
				return "Bold";
			}
			if (italic)
			{
				return "Italic";
			}
			return "Regular";
		}

		private void OnStyleButtonClicked(object sender, System.EventArgs eventArgs)
		{
			if (m_pulldownPanel != null || PulldownJustDismissed())
			{
				ClosePulldown();
				return;
			}
			double anchorX = 0.0;
			if (m_optionsRow != null && m_textStyleButton != null)
			{
				anchorX = m_optionsRow.X + m_textStyleButton.X;
			}
			double anchorY = UiConstants.MenuBarHeight + 1.0 + UiConstants.OptionsBarHeight + 1.0;
			ShowPulldown(BuildStylePulldownContent(), anchorX, anchorY, 150.0, 140.0);
		}

		private View BuildStylePulldownContent()
		{
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(4.0);
			list.Add(BuildStyleRow("Regular", false, false));
			list.Add(BuildStyleRow("Bold", true, false));
			list.Add(BuildStyleRow("Italic", false, true));
			list.Add(BuildStyleRow("Bold Italic", true, true));
			return list;
		}

		private Label BuildStyleRow(string label, bool bold, bool italic)
		{
			Label row = new Label();
			row.Text = label;
			row.FontSize = 13.0;
			row.Padding = new Thickness(8.0, 5.0, 8.0, 5.0);
			row.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			FontAttributes attributes = FontAttributes.None;
			if (bold)
			{
				attributes = attributes | FontAttributes.Bold;
			}
			if (italic)
			{
				attributes = attributes | FontAttributes.Italic;
			}
			row.FontAttributes = attributes;
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnStyleRowTapped;
			row.GestureRecognizers.Add(tap);
			return row;
		}

		private void OnStyleRowTapped(object sender, TappedEventArgs eventArgs)
		{
			Label row = sender as Label;
			if (row == null || m_toolState == null)
			{
				return;
			}
			bool bold = row.Text == "Bold" || row.Text == "Bold Italic";
			bool italic = row.Text == "Italic" || row.Text == "Bold Italic";
			m_toolState.SetTextBold(bold);
			m_toolState.SetTextItalic(italic);
			UpdateStyleButtonText();
			ClosePulldown();
			RefreshTextEditStyle();
		}

		private void OnTextAlignChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_textAlignPicker.SelectedIndex;
			if (index < 0)
			{
				return;
			}
			m_toolState.SetTextAlign(index);
			RefreshTextEditStyle();
		}

		private void OnTextAntiAliasChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_toolState == null)
			{
				return;
			}
			int index = m_textAntiAliasPicker.SelectedIndex;
			if (index < 0)
			{
				return;
			}
			m_toolState.SetTextAntiAlias(index);
			RefreshTextEditStyle();
		}

		private void OnTextColorTapped(object sender, TappedEventArgs eventArgs)
		{
			OpenColorPicker(true);
		}

		private void OnTextCharClicked(object sender, System.EventArgs eventArgs)
		{
			ShowModal(BuildCharacterPanelContent(), 268.0, 320.0);
		}

		private int LeadingSliderValue()
		{
			float leading = m_toolState.TextLeading();
			if (m_toolState.TextLeadingAuto() || leading < 1.0f)
			{
				leading = m_toolState.TextSize() * 1.25f;
			}
			return (int)leading;
		}

		private Grid BuildCharRow(string labelText, View control)
		{
			Label label = new Label();
			label.Text = labelText;
			label.FontSize = 12.0;
			label.WidthRequest = 96.0;
			label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			label.VerticalOptions = LayoutOptions.Center;

			Grid row = new Grid();
			row.ColumnSpacing = 6.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(label, 0);
			Grid.SetColumn(control, 1);
			control.HorizontalOptions = LayoutOptions.End;
			row.Add(label);
			row.Add(control);
			return row;
		}

		private View BuildCharacterPanelContent()
		{
			m_charLeadingField = new SliderField(0, 400, LeadingSliderValue(), " px", OnCharLeadingValue);
			m_charLeadingField.VerticalOptions = LayoutOptions.Center;

			m_charLeadingAutoCheck = new CheckBox();
			m_charLeadingAutoCheck.IsChecked = m_toolState.TextLeadingAuto();
			m_charLeadingAutoCheck.VerticalOptions = LayoutOptions.Center;
			m_charLeadingAutoCheck.HorizontalOptions = LayoutOptions.End;
			m_charLeadingAutoCheck.CheckedChanged += OnCharLeadingAutoChanged;

			m_charTrackingField = new SliderField(-50, 200, m_toolState.TextTracking(), "", OnCharTrackingValue);
			m_charTrackingField.VerticalOptions = LayoutOptions.Center;

			m_charHScaleField = new SliderField(10, 400, m_toolState.TextHorizontalScale(), " %", OnCharHScaleValue);
			m_charHScaleField.VerticalOptions = LayoutOptions.Center;

			m_charVScaleField = new SliderField(10, 400, m_toolState.TextVerticalScale(), " %", OnCharVScaleValue);
			m_charVScaleField.VerticalOptions = LayoutOptions.Center;

			m_charBaselineField = new SliderField(-100, 100, m_toolState.TextBaselineShift(), " px", OnCharBaselineValue);
			m_charBaselineField.VerticalOptions = LayoutOptions.Center;

			m_charFauxBoldCheck = new CheckBox();
			m_charFauxBoldCheck.IsChecked = m_toolState.TextFauxBold();
			m_charFauxBoldCheck.VerticalOptions = LayoutOptions.Center;
			m_charFauxBoldCheck.HorizontalOptions = LayoutOptions.End;
			m_charFauxBoldCheck.CheckedChanged += OnCharFauxBoldChanged;

			m_charFauxItalicCheck = new CheckBox();
			m_charFauxItalicCheck.IsChecked = m_toolState.TextFauxItalic();
			m_charFauxItalicCheck.VerticalOptions = LayoutOptions.Center;
			m_charFauxItalicCheck.HorizontalOptions = LayoutOptions.End;
			m_charFauxItalicCheck.CheckedChanged += OnCharFauxItalicChanged;

			m_charKerningAutoCheck = new CheckBox();
			m_charKerningAutoCheck.IsChecked = m_toolState.TextKerningAuto();
			m_charKerningAutoCheck.VerticalOptions = LayoutOptions.Center;
			m_charKerningAutoCheck.HorizontalOptions = LayoutOptions.End;
			m_charKerningAutoCheck.CheckedChanged += OnCharKerningChanged;

			Label title = new Label();
			title.Text = "Character";
			title.FontSize = 12.0;
			title.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			title.VerticalOptions = LayoutOptions.Center;
			title.HorizontalOptions = LayoutOptions.Start;

			Label close = new Label();
			close.Text = "✕";
			close.FontSize = 12.0;
			close.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			close.VerticalOptions = LayoutOptions.Center;
			close.HorizontalOptions = LayoutOptions.End;
			TapGestureRecognizer closeTap = new TapGestureRecognizer();
			closeTap.Tapped += OnCharPanelClose;
			close.GestureRecognizers.Add(closeTap);

			Grid titleGrid = new Grid();
			titleGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			titleGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(title, 0);
			Grid.SetColumn(close, 1);
			titleGrid.Add(title);
			titleGrid.Add(close);

			Border titleBar = new Border();
			titleBar.Padding = new Thickness(10.0, 5.0, 8.0, 5.0);
			titleBar.StrokeThickness = 0.0;
			titleBar.ThemeBg(UiConstants.TitleBarLight, UiConstants.TitleBarDark);
			titleBar.Content = titleGrid;
			PanGestureRecognizer titlePan = new PanGestureRecognizer();
			titlePan.PanUpdated += OnCharPanelPan;
			titleBar.GestureRecognizers.Add(titlePan);

			VerticalStackLayout rows = new VerticalStackLayout();
			rows.Spacing = 6.0;
			rows.Padding = new Thickness(12.0, 10.0, 12.0, 12.0);
			rows.Add(BuildCharRow("Leading", m_charLeadingField));
			rows.Add(BuildCharRow("Auto leading", m_charLeadingAutoCheck));
			rows.Add(BuildCharRow("Tracking", m_charTrackingField));
			rows.Add(BuildCharRow("Horiz Scale", m_charHScaleField));
			rows.Add(BuildCharRow("Vert Scale", m_charVScaleField));
			rows.Add(BuildCharRow("Baseline", m_charBaselineField));
			rows.Add(BuildCharRow("Faux Bold", m_charFauxBoldCheck));
			rows.Add(BuildCharRow("Faux Italic", m_charFauxItalicCheck));
			rows.Add(BuildCharRow("Kerning (Auto)", m_charKerningAutoCheck));

			VerticalStackLayout body = new VerticalStackLayout();
			body.Spacing = 0.0;
			body.Add(titleBar);
			body.Add(rows);

			Border panel = new Border();
			panel.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			panel.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			panel.StrokeThickness = 1.0;
			panel.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			panel.Content = body;
			return panel;
		}

		private void OnCharPanelPan(object sender, PanUpdatedEventArgs eventArgs)
		{
			DragModal(eventArgs.StatusType, eventArgs.TotalX, eventArgs.TotalY);
		}

		private void OnCharPanelClose(object sender, TappedEventArgs eventArgs)
		{
			CloseModal();
		}

		private void OnCharLeadingAutoChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null || m_charLeadingAutoCheck == null)
			{
				return;
			}
			m_toolState.SetTextLeadingAuto(m_charLeadingAutoCheck.IsChecked);
			RefreshTextEditStyle();
		}

		private void OnCharLeadingValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextLeading(value);
			m_toolState.SetTextLeadingAuto(false);
			if (m_charLeadingAutoCheck != null)
			{
				m_charLeadingAutoCheck.IsChecked = false;
			}
			RefreshTextEditStyle();
		}

		private void OnCharTrackingValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextTracking(value);
			RefreshTextEditStyle();
		}

		private void OnCharHScaleValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextHorizontalScale(value);
			RefreshTextEditStyle();
		}

		private void OnCharVScaleValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextVerticalScale(value);
			RefreshTextEditStyle();
		}

		private void OnCharBaselineValue(int value)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetTextBaselineShift(value);
			RefreshTextEditStyle();
		}

		private void OnCharFauxBoldChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null || m_charFauxBoldCheck == null)
			{
				return;
			}
			m_toolState.SetTextFauxBold(m_charFauxBoldCheck.IsChecked);
			RefreshTextEditStyle();
		}

		private void OnCharFauxItalicChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null || m_charFauxItalicCheck == null)
			{
				return;
			}
			m_toolState.SetTextFauxItalic(m_charFauxItalicCheck.IsChecked);
			RefreshTextEditStyle();
		}

		private void OnCharKerningChanged(object sender, CheckedChangedEventArgs eventArgs)
		{
			if (m_toolState == null || m_charKerningAutoCheck == null)
			{
				return;
			}
			m_toolState.SetTextKerningAuto(m_charKerningAutoCheck.IsChecked);
			RefreshTextEditStyle();
		}

		private void OnBrushSizeValue(int size)
		{
			if (m_toolState == null)
			{
				return;
			}
			m_toolState.SetBrushSize(size);
		}

		public void PlaceText(CanvasView canvas, int x, int y, float deviceX, float deviceY)
		{
			Document document = canvas.CurrentDocument();
			if (document == null)
			{
				return;
			}
			if (m_textEditActive && m_textEditLayer != null && ReferenceEquals(m_textEditCanvas, canvas))
			{
				int caretIndex = TextRasterizer.CaretIndexAtPoint(m_textEditLayer, x, y);
				if (m_textEditor != null)
				{
					m_textEditor.CursorPosition = caretIndex;
					m_textEditor.SelectionLength = 0;
					m_textEditor.Focus();
				}
				m_caretVisible = true;
				canvas.InvalidateSurface();
				return;
			}
			CommitTextEdit();
			Layer active = document.ActiveLayer();
			bool editExisting = active != null && active.IsText();
			if (editExisting)
			{
				BeginTextEdit(canvas, active, false);
				return;
			}
			Layer layer = document.AddLayer("Text");
			if (layer == null)
			{
				return;
			}
			layer.SetTextPosition(x, y);
			ApplyToolStateToLayer(layer);
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
			BeginTextEdit(canvas, layer, true);
		}

		public void BeginTextEditForLayer(Bitmute.Imaging.Layer layer)
		{
			CanvasView canvas = ActiveCanvas();
			if (canvas == null || layer == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document == null)
			{
				return;
			}
			int index = document.Layers().IndexOf(layer);
			if (index < 0)
			{
				return;
			}
			CommitTextEdit();
			document.SetActiveLayerIndex(index);
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
			if (m_toolState.Tool() != eTool.Text)
			{
				if (m_toolPalette != null)
				{
					m_toolPalette.SelectToolExternal(eTool.Text);
				}
				else
				{
					m_toolState.SetTool(eTool.Text);
					OnToolSelected(eTool.Text);
				}
			}
			BeginTextEdit(canvas, layer, false);
		}

		public void BeginTextEdit(CanvasView canvas, Layer layer, bool isNew)
		{
			m_textEditCanvas = canvas;
			m_textEditLayer = layer;
			m_textEditActive = true;
			m_textPreEditWasNew = isNew;
			m_textPreEditString = layer.Text();
			m_textPreEditSize = layer.TextSize();
			m_textPreEditFont = layer.TextFontFamily();
			m_textPreEditBold = layer.TextBold();
			m_textPreEditItalic = layer.TextItalic();
			m_textPreEditColor = layer.TextColor();
			m_textPreEditAlign = layer.TextAlign();
			m_textPreEditAntiAlias = layer.TextAntiAlias();
			m_textPreEditLeadingAuto = layer.TextLeadingAuto();
			m_textPreEditLeading = layer.TextLeading();
			m_textPreEditTracking = layer.TextTracking();
			m_textPreEditHorizontalScale = layer.TextHorizontalScale();
			m_textPreEditVerticalScale = layer.TextVerticalScale();
			m_textPreEditBaselineShift = layer.TextBaselineShift();
			m_textPreEditFauxBold = layer.TextFauxBold();
			m_textPreEditFauxItalic = layer.TextFauxItalic();
			m_textPreEditKerningAuto = layer.TextKerningAuto();

			LoadTextStyleFromLayer(layer);

			Document document = canvas.CurrentDocument();
			if (document != null)
			{
				document.BeginStroke();
			}

			EnsureTextEditor();
			m_textEditor.Text = layer.Text();
			m_textEditor.CursorPosition = layer.Text().Length;
			m_textEditor.SelectionLength = 0;
			PositionTextEditor();
			m_textEditor.ZIndex = 0;
			if (!m_workspace.Contains(m_textEditor))
			{
				m_workspace.Add(m_textEditor);
			}
			m_textEditor.Focus();

			layer.RenderText();
			canvas.MarkComposeDirty();
			canvas.InvalidateSurface();
			StartCaretBlink();
		}

		private void EnsureTextEditor()
		{
			if (m_textEditor != null)
			{
				return;
			}
			m_textEditor = new Editor();
			m_textEditor.Opacity = 0.0;
			m_textEditor.WidthRequest = 200.0;
			m_textEditor.HeightRequest = 40.0;
			m_textEditor.AutoSize = EditorAutoSizeOption.Disabled;
			m_textEditor.TextChanged += OnTextEditorChanged;
			m_textEditor.HandlerChanged += OnTextEditorHandlerChanged;
		}

		private void PositionTextEditor()
		{
			double editorX = 8.0;
			double editorY = 8.0;
			DocumentWindow window = m_activeDocumentWindow;
			if (window != null)
			{
				Rect bounds = AbsoluteLayout.GetLayoutBounds(window);
				editorX = bounds.X + UiConstants.PanelBorderThickness + 8.0;
				editorY = bounds.Y + UiConstants.TitleBarHeight + UiConstants.PanelBorderThickness + 8.0;
			}
			AbsoluteLayout.SetLayoutBounds(m_textEditor, new Rect(editorX, editorY, 200.0, 40.0));
			AbsoluteLayout.SetLayoutFlags(m_textEditor, AbsoluteLayoutFlags.None);
		}

		private void OnTextEditorHandlerChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_textEditorKeyHooked)
			{
				return;
			}
			if (m_textEditor == null || m_textEditor.Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = m_textEditor.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			element.KeyDown += OnTextEditorKeyDown;
			m_textEditorKeyHooked = true;
		}

		private void OnTextEditorKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs args)
		{
			if (!m_textEditActive)
			{
				return;
			}
			if (args.Key == Windows.System.VirtualKey.Escape)
			{
				CancelTextEdit();
				args.Handled = true;
				return;
			}
			if (args.Key == Windows.System.VirtualKey.Enter)
			{
				Windows.UI.Core.CoreVirtualKeyStates shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
				bool shiftHeld = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
				if (!shiftHeld)
				{
					CommitTextEdit();
					args.Handled = true;
				}
			}
		}

		private static Color FromSkColor(SkiaSharp.SKColor color)
		{
			return new Color(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
		}

		private void OnTextEditorChanged(object sender, TextChangedEventArgs eventArgs)
		{
			if (!m_textEditActive || m_textEditLayer == null)
			{
				return;
			}
			string raw = m_textEditor.Text;
			if (raw == null)
			{
				raw = "";
			}
			string normalized = raw.Replace('\r', '\n');
			m_textEditLayer.SetTextString(normalized);
			m_textEditLayer.RenderText();
			if (m_textEditCanvas != null)
			{
				m_textEditCanvas.MarkComposeDirty();
				m_textEditCanvas.InvalidateSurface();
			}
		}

		private void ApplyToolStateToLayer(Layer layer)
		{
			layer.SetTextStyle(m_toolState.TextSize(), m_toolState.TextFontFamily(), m_toolState.TextBold(), m_toolState.TextItalic(), m_toolState.Foreground(), m_toolState.TextAlign(), m_toolState.TextAntiAlias());
			layer.SetTextCharacter(m_toolState.TextLeadingAuto(), m_toolState.TextLeading(), m_toolState.TextTracking(), m_toolState.TextHorizontalScale(), m_toolState.TextVerticalScale(), m_toolState.TextBaselineShift(), m_toolState.TextFauxBold(), m_toolState.TextFauxItalic(), m_toolState.TextKerningAuto());
		}

		private void LoadTextStyleFromLayer(Layer layer)
		{
			m_toolState.SetTextSize((int)layer.TextSize());
			m_toolState.SetTextFontFamily(layer.TextFontFamily());
			m_toolState.SetTextBold(layer.TextBold());
			m_toolState.SetTextItalic(layer.TextItalic());
			m_toolState.SetTextAlign(layer.TextAlign());
			m_toolState.SetTextAntiAlias(layer.TextAntiAlias());
			m_toolState.SetForeground(layer.TextColor());
			m_toolState.SetTextLeadingAuto(layer.TextLeadingAuto());
			m_toolState.SetTextLeading(layer.TextLeading());
			m_toolState.SetTextTracking(layer.TextTracking());
			m_toolState.SetTextHorizontalScale(layer.TextHorizontalScale());
			m_toolState.SetTextVerticalScale(layer.TextVerticalScale());
			m_toolState.SetTextBaselineShift(layer.TextBaselineShift());
			m_toolState.SetTextFauxBold(layer.TextFauxBold());
			m_toolState.SetTextFauxItalic(layer.TextFauxItalic());
			m_toolState.SetTextKerningAuto(layer.TextKerningAuto());
			SyncTextOptionsBar();
			if (m_toolPalette != null)
			{
				m_toolPalette.RefreshColors();
			}
		}

		private void SyncTextOptionsBar()
		{
			if (m_textSizeField != null)
			{
				m_textSizeField.SetValueSilently(m_toolState.TextSize());
			}
			UpdateStyleButtonText();
			if (m_textAlignPicker != null)
			{
				m_textAlignPicker.SelectedIndex = m_toolState.TextAlign();
			}
			if (m_textAntiAliasPicker != null)
			{
				m_textAntiAliasPicker.SelectedIndex = m_toolState.TextAntiAlias();
			}
			UpdateFontButtonText();
			if (m_textColorSwatch != null)
			{
				m_textColorSwatch.Color = FromSkColor(m_toolState.Foreground());
			}
		}

		private void StartCaretBlink()
		{
			m_caretVisible = true;
			if (m_caretTimer == null && Dispatcher != null)
			{
				m_caretTimer = Dispatcher.CreateTimer();
				m_caretTimer.Interval = System.TimeSpan.FromMilliseconds(530.0);
				m_caretTimer.Tick += OnCaretTick;
			}
			if (m_caretTimer != null)
			{
				m_caretTimer.Start();
			}
		}

		private void StopCaretBlink()
		{
			if (m_caretTimer != null)
			{
				m_caretTimer.Stop();
			}
			m_caretVisible = false;
		}

		private void OnCaretTick(object sender, System.EventArgs eventArgs)
		{
			m_caretVisible = !m_caretVisible;
			if (m_textEditCanvas != null)
			{
				m_textEditCanvas.InvalidateSurface();
			}
		}

		public bool IsTextEditActive()
		{
			return m_textEditActive;
		}

		public CanvasView TextEditCanvas()
		{
			return m_textEditCanvas;
		}

		public Bitmute.Imaging.Layer TextEditLayer()
		{
			return m_textEditLayer;
		}

		public int TextCaretIndex()
		{
			if (m_textEditor == null)
			{
				return 0;
			}
			return m_textEditor.CursorPosition;
		}

		public int TextSelectionStart()
		{
			if (m_textEditor == null)
			{
				return 0;
			}
			return m_textEditor.CursorPosition;
		}

		public int TextSelectionLength()
		{
			if (m_textEditor == null)
			{
				return 0;
			}
			return m_textEditor.SelectionLength;
		}

		public bool CaretVisible()
		{
			return m_caretVisible;
		}

		public void CommitTextEdit()
		{
			if (!m_textEditActive)
			{
				return;
			}
			m_textEditActive = false;
			StopCaretBlink();
			Layer layer = m_textEditLayer;
			CanvasView canvas = m_textEditCanvas;
			if (m_textEditor != null && m_workspace.Contains(m_textEditor))
			{
				m_workspace.Remove(m_textEditor);
			}
			if (layer != null && canvas != null)
			{
				Document document = canvas.CurrentDocument();
				ApplyToolStateToLayer(layer);
				layer.RenderText();
				if (layer.Text().Length == 0 && m_textPreEditWasNew)
				{
					if (document != null)
					{
						document.EndStroke();
						int index = document.Layers().IndexOf(layer);
						if (index >= 0)
						{
							document.DeleteLayer(index);
						}
					}
				}
				else
				{
					if (document != null)
					{
						document.EndStroke();
					}
					string name = layer.Text();
					name = name.Replace('\n', ' ');
					if (name.Length > 18)
					{
						name = name.Substring(0, 18);
					}
					if (name.Length == 0)
					{
						name = "Text";
					}
					layer.SetName(name);
				}
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
				if (m_layersPanel != null)
				{
					m_layersPanel.Refresh();
				}
			}
			m_textEditLayer = null;
			m_textEditCanvas = null;
		}

		private void CancelTextEdit()
		{
			if (!m_textEditActive)
			{
				return;
			}
			m_textEditActive = false;
			StopCaretBlink();
			Layer layer = m_textEditLayer;
			CanvasView canvas = m_textEditCanvas;
			if (m_textEditor != null && m_workspace.Contains(m_textEditor))
			{
				m_workspace.Remove(m_textEditor);
			}
			if (layer != null && canvas != null)
			{
				Document document = canvas.CurrentDocument();
				layer.SetTextString(m_textPreEditString);
				layer.SetTextStyle(m_textPreEditSize, m_textPreEditFont, m_textPreEditBold, m_textPreEditItalic, m_textPreEditColor, m_textPreEditAlign, m_textPreEditAntiAlias);
				layer.SetTextCharacter(m_textPreEditLeadingAuto, m_textPreEditLeading, m_textPreEditTracking, m_textPreEditHorizontalScale, m_textPreEditVerticalScale, m_textPreEditBaselineShift, m_textPreEditFauxBold, m_textPreEditFauxItalic, m_textPreEditKerningAuto);
				layer.RenderText();
				if (document != null)
				{
					document.EndStroke();
					if (m_textPreEditWasNew)
					{
						int index = document.Layers().IndexOf(layer);
						if (index >= 0)
						{
							document.DeleteLayer(index);
						}
					}
				}
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
				if (m_layersPanel != null)
				{
					m_layersPanel.Refresh();
				}
			}
			m_textEditLayer = null;
			m_textEditCanvas = null;
		}

		public void DoRasterizeText()
		{
			CommitTextEdit();
			Document document = ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null || !layer.IsText())
			{
				SetStatusMessage("Active layer is not a text layer");
				return;
			}
			layer.RenderText();
			layer.RasterizeText();
			CanvasView canvas = ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
			if (m_layersPanel != null)
			{
				m_layersPanel.Refresh();
			}
		}

		private void RefreshTextEditStyle()
		{
			if (!m_textEditActive || m_textEditLayer == null || m_textEditCanvas == null)
			{
				return;
			}
			ApplyToolStateToLayer(m_textEditLayer);
			m_textEditLayer.RenderText();
			m_textEditCanvas.MarkComposeDirty();
			m_textEditCanvas.InvalidateSurface();
		}

		public ToolState CurrentToolState()
		{
			return m_toolState;
		}

		public Tool CurrentTool()
		{
			eTool tool = m_toolState.Tool();
			if (tool == eTool.Move)
			{
				return m_moveTool;
			}
			if (tool == eTool.Select)
			{
				return m_rectangleSelectTool;
			}
			if (tool == eTool.EllipseSelect)
			{
				return m_ellipseSelectTool;
			}
			if (tool == eTool.Lasso)
			{
				return m_lassoTool;
			}
			if (tool == eTool.FreehandLasso)
			{
				return m_freehandLassoTool;
			}
			if (tool == eTool.MagneticLasso)
			{
				return m_magneticLassoTool;
			}
			if (tool == eTool.MagicWand)
			{
				return m_magicWandTool;
			}
			if (tool == eTool.Text)
			{
				return m_textTool;
			}
			if (tool == eTool.Pencil)
			{
				return m_pencilTool;
			}
			if (tool == eTool.Brush)
			{
				return m_brushTool;
			}
			if (tool == eTool.Eraser)
			{
				return m_eraserTool;
			}
			if (tool == eTool.Eyedropper)
			{
				return m_eyedropperTool;
			}
			if (tool == eTool.Fill)
			{
				return m_fillTool;
			}
			if (tool == eTool.Gradient)
			{
				return m_gradientTool;
			}
			if (tool == eTool.Line)
			{
				return m_lineTool;
			}
			if (tool == eTool.RectangleShape)
			{
				return m_rectangleShapeTool;
			}
			if (tool == eTool.RoundedRectangleShape)
			{
				return m_roundedRectangleShapeTool;
			}
			if (tool == eTool.EllipseShape)
			{
				return m_ellipseShapeTool;
			}
			if (tool == eTool.PolygonShape)
			{
				return m_polygonShapeTool;
			}
			if (tool == eTool.DodgeBurn)
			{
				return m_dodgeBurnTool;
			}
			if (tool == eTool.Blur)
			{
				return m_blurTool;
			}
			if (tool == eTool.Sponge)
			{
				return m_spongeTool;
			}
			if (tool == eTool.ColorReplacement)
			{
				return m_colorReplacementTool;
			}
			if (tool == eTool.Sharpen)
			{
				return m_sharpenTool;
			}
			if (tool == eTool.Clone)
			{
				return m_cloneTool;
			}
			if (tool == eTool.Heal)
			{
				return m_healTool;
			}
			if (tool == eTool.Slice)
			{
				return m_sliceTool;
			}
			if (tool == eTool.Smudge)
			{
				return m_smudgeTool;
			}
			if (tool == eTool.Hand)
			{
				return m_handTool;
			}
			if (tool == eTool.Zoom)
			{
				return m_zoomTool;
			}
			if (tool == eTool.Ruler)
			{
				return m_rulerTool;
			}
			if (tool == eTool.Crop)
			{
				return m_cropTool;
			}
			if (tool == eTool.FreeTransform)
			{
				return m_freeTransformTool;
			}
			return null;
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
				m_previousTool = m_toolState.Tool();
			}
			OnToolSelected(eTool.FreeTransform);
			bool armed = m_freeTransformTool.Begin(document, mode, m_toolState.Background());
			if (!armed)
			{
				SetStatusMessage("Cannot transform this layer");
				OnToolSelected(m_previousTool);
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
			OnToolSelected(m_previousTool);
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
