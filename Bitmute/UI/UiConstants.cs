using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public static class UiConstants
	{
		public const double MenuBarHeight = 26.0;
		public const double OptionsBarHeight = 32.0;
		public const double StatusBarHeight = 22.0;
		public const double ToolPaletteWidth = 58.0;
		public const double ToolButtonSize = 24.0;
		public const double PaletteDockWidth = 240.0;
		public const double PaletteTabHeight = 24.0;

		public const double TitleBarHeight = 24.0;
		public const double PanelCornerRadius = 3.0;
		public const double PanelBorderThickness = 1.0;
		public const double PanelMinWidth = 200.0;
		public const double PanelMinHeight = 140.0;
		public const double ResizeGripSize = 14.0;
		public const double CascadeOffset = 26.0;
		public const double CloseButtonSize = 20.0;
		public const double MinimizedPadding = 8.0;
		public const double RulerThickness = 18.0;

		public const double DefaultDocumentWidth = 800.0;
		public const double DefaultDocumentHeight = 600.0;
		public const double DefaultDocumentWindowWidth = 560.0;
		public const double DefaultDocumentWindowHeight = 440.0;

		public static readonly Color WorkspaceBackdropDark = Color.FromArgb("#B3B3B3");
		public static readonly Color WorkspaceBackdropLight = Color.FromArgb("#8C8C8C");
		public static readonly Color ChromeDark = Color.FromArgb("#383838");
		public static readonly Color ChromeLight = Color.FromArgb("#ECECEC");
		public static readonly Color ChromeRaisedDark = Color.FromArgb("#444444");
		public static readonly Color ChromeRaisedLight = Color.FromArgb("#DCDCDC");
		public static readonly Color DividerDark = Color.FromArgb("#232323");
		public static readonly Color DividerLight = Color.FromArgb("#B4B4B4");
		public static readonly Color PanelSurfaceDark = Color.FromArgb("#3C3C3C");
		public static readonly Color PanelSurfaceLight = Color.FromArgb("#F2F2F2");
		public static readonly Color TitleBarDark = Color.FromArgb("#2F2F2F");
		public static readonly Color TitleBarLight = Color.FromArgb("#DDDDDD");
		public static readonly Color TitleBarActiveDark = Color.FromArgb("#3D5A80");
		public static readonly Color TitleBarActiveLight = Color.FromArgb("#B9CDE8");
		public static readonly Color OnSurfaceDark = Color.FromArgb("#E4E4E4");
		public static readonly Color OnSurfaceLight = Color.FromArgb("#1E1E1E");
		public static readonly Color TextDimDark = Color.FromArgb("#9A9A9A");
		public static readonly Color TextDimLight = Color.FromArgb("#6C6C6C");
		public static readonly Color AccentDark = Color.FromArgb("#3D7EDB");
		public static readonly Color AccentLight = Color.FromArgb("#2C6BD0");
		public static readonly Color ToolRestingDark = Color.FromArgb("#404040");
		public static readonly Color ToolRestingLight = Color.FromArgb("#D6D6D6");
		public static readonly Color ToolButtonChipDark = Color.FromArgb("#3F3F3F");
		public static readonly Color ToolButtonChipLight = Color.FromArgb("#C4C4C4");
		public static readonly Color ToolSelectedDark = Color.FromArgb("#2C5480");
		public static readonly Color ToolSelectedLight = Color.FromArgb("#B9CDE8");
		public static readonly Color ResizeGripDark = Color.FromArgb("#606060");
		public static readonly Color ResizeGripLight = Color.FromArgb("#A0A0A0");
		public static readonly Color CanvasPaperDark = Color.FromArgb("#FFFFFF");
		public static readonly Color CanvasPaperLight = Color.FromArgb("#FFFFFF");
		public static readonly Color CanvasInsetDark = Color.FromArgb("#1E1E1E");
		public static readonly Color CanvasInsetLight = Color.FromArgb("#8C8C8C");
		public static readonly Color RulerDark = Color.FromArgb("#333333");
		public static readonly Color RulerLight = Color.FromArgb("#E4E4E4");
		public static readonly Color RulerTickDark = Color.FromArgb("#707070");
		public static readonly Color RulerTickLight = Color.FromArgb("#808080");

		private static Color Pick(Color light, Color dark)
		{
			if (Theme.IsDark())
			{
				return dark;
			}
			return light;
		}

		public static Color WorkspaceBackdrop { get { return Pick(WorkspaceBackdropLight, WorkspaceBackdropDark); } }
		public static Color Chrome { get { return Pick(ChromeLight, ChromeDark); } }
		public static Color ChromeRaised { get { return Pick(ChromeRaisedLight, ChromeRaisedDark); } }
		public static Color Divider { get { return Pick(DividerLight, DividerDark); } }
		public static Color PanelSurface { get { return Pick(PanelSurfaceLight, PanelSurfaceDark); } }
		public static Color TitleBar { get { return Pick(TitleBarLight, TitleBarDark); } }
		public static Color TitleBarActive { get { return Pick(TitleBarActiveLight, TitleBarActiveDark); } }
		public static Color OnSurface { get { return Pick(OnSurfaceLight, OnSurfaceDark); } }
		public static Color TextDim { get { return Pick(TextDimLight, TextDimDark); } }
		public static Color Accent { get { return Pick(AccentLight, AccentDark); } }
		public static Color ToolResting { get { return Pick(ToolRestingLight, ToolRestingDark); } }
		public static Color ToolButtonChip { get { return Pick(ToolButtonChipLight, ToolButtonChipDark); } }
		public static Color ToolSelected { get { return Pick(ToolSelectedLight, ToolSelectedDark); } }
		public static Color ResizeGrip { get { return Pick(ResizeGripLight, ResizeGripDark); } }
		public static Color CanvasPaper { get { return Pick(CanvasPaperLight, CanvasPaperDark); } }
		public static Color CanvasInset { get { return Pick(CanvasInsetLight, CanvasInsetDark); } }
		public static Color Ruler { get { return Pick(RulerLight, RulerDark); } }
		public static Color RulerTick { get { return Pick(RulerTickLight, RulerTickDark); } }
	}
}
