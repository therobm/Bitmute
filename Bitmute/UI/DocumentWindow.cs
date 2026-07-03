using System;
using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class DocumentWindow : FloatingPanel
	{
		private Document m_document;
		private CanvasView m_canvas;
		private Entry m_zoomEntry;
		private string m_baseTitle;
		private Ruler m_topRuler;
		private Ruler m_leftRuler;
		private CanvasScrollbar m_horizontalScrollbar;
		private CanvasScrollbar m_verticalScrollbar;
		private BoxView m_rulerCorner;
		private ColumnDefinition m_rulerColumn;
		private RowDefinition m_rulerRow;
		private bool m_rulersEnabled;

		public DocumentWindow(Document document)
		{
			m_document = document;
			m_baseTitle = document.Title();
			SetTitle(m_baseTitle);

			m_canvas = new CanvasView(document);
			m_canvas.SetOwnerWindow(this);
			m_canvas.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;

			m_topRuler = new Ruler(m_canvas, true);
			m_leftRuler = new Ruler(m_canvas, false);
			m_horizontalScrollbar = new CanvasScrollbar(m_canvas, true);
			m_verticalScrollbar = new CanvasScrollbar(m_canvas, false);

			m_rulerCorner = new BoxView();
			m_rulerCorner.ThemeColor(UiConstants.CanvasPaperLight, UiConstants.CanvasPaperDark);

			Grid layout = new Grid();
			m_rulerColumn = new ColumnDefinition(new GridLength(UiConstants.RulerThickness));
			m_rulerRow = new RowDefinition(new GridLength(UiConstants.RulerThickness));
			layout.ColumnDefinitions.Add(m_rulerColumn);
			layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(UiConstants.ResizeGripSize)));
			layout.RowDefinitions.Add(m_rulerRow);
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			layout.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.ResizeGripSize)));

			Grid.SetRow(m_rulerCorner, 0);
			Grid.SetColumn(m_rulerCorner, 0);
			layout.Add(m_rulerCorner);

			Grid.SetRow(m_topRuler, 0);
			Grid.SetColumn(m_topRuler, 1);
			layout.Add(m_topRuler);

			Grid.SetRow(m_leftRuler, 1);
			Grid.SetColumn(m_leftRuler, 0);
			layout.Add(m_leftRuler);

			Grid.SetRow(m_canvas, 1);
			Grid.SetColumn(m_canvas, 1);
			layout.Add(m_canvas);

			Grid.SetRow(m_verticalScrollbar, 1);
			Grid.SetColumn(m_verticalScrollbar, 2);
			layout.Add(m_verticalScrollbar);

			Grid.SetRow(m_horizontalScrollbar, 2);
			Grid.SetColumn(m_horizontalScrollbar, 1);
			layout.Add(m_horizontalScrollbar);

			Grid outer = new Grid();
			outer.Add(layout);
			outer.Add(BuildZoomField());
			SetPanelContent(outer);

			bool rulersEnabled = true;
			MainView main = MainView.Self;
			if (main != null)
			{
				rulersEnabled = main.RulersEnabled();
			}
			SetRulersEnabled(rulersEnabled);
		}

		public void RefreshChrome()
		{
			if (m_topRuler != null)
			{
				m_topRuler.InvalidateSurface();
			}
			if (m_leftRuler != null)
			{
				m_leftRuler.InvalidateSurface();
			}
			if (m_horizontalScrollbar != null)
			{
				m_horizontalScrollbar.InvalidateSurface();
			}
			if (m_verticalScrollbar != null)
			{
				m_verticalScrollbar.InvalidateSurface();
			}
		}

		public void SetRulersEnabled(bool enabled)
		{
			m_rulersEnabled = enabled;
			double size = 0.0;
			if (enabled)
			{
				size = UiConstants.RulerThickness;
			}
			m_rulerColumn.Width = new GridLength(size);
			m_rulerRow.Height = new GridLength(size);
			m_topRuler.IsVisible = enabled;
			m_leftRuler.IsVisible = enabled;
			m_rulerCorner.IsVisible = enabled;
		}

		private View BuildZoomField()
		{
			m_zoomEntry = new Entry();
			m_zoomEntry.Keyboard = Keyboard.Numeric;
			m_zoomEntry.WidthRequest = 46.0;
			m_zoomEntry.HeightRequest = 22.0;
			m_zoomEntry.FontSize = 11.0;
			m_zoomEntry.Margin = new Thickness(0.0);
			m_zoomEntry.BackgroundColor = Colors.Transparent;
			m_zoomEntry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_zoomEntry.VerticalOptions = LayoutOptions.Center;
			m_zoomEntry.HorizontalTextAlignment = TextAlignment.End;
			m_zoomEntry.Completed += OnZoomEntryCommitted;
			m_zoomEntry.Unfocused += OnZoomEntryUnfocused;

			Label percentLabel = new Label();
			percentLabel.Text = "%";
			percentLabel.FontSize = 11.0;
			percentLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			percentLabel.VerticalOptions = LayoutOptions.Center;

			HorizontalStackLayout zoomRow = new HorizontalStackLayout();
			zoomRow.Spacing = 1.0;
			zoomRow.Add(m_zoomEntry);
			zoomRow.Add(percentLabel);

			Border zoomBox = new Border();
			zoomBox.Padding = new Thickness(4.0, 0.0, 5.0, 0.0);
			zoomBox.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			zoomBox.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			zoomBox.StrokeThickness = 1.0;
			zoomBox.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2.0) };
			zoomBox.HorizontalOptions = LayoutOptions.Start;
			zoomBox.VerticalOptions = LayoutOptions.End;
			zoomBox.Margin = new Thickness(UiConstants.RulerThickness + 4.0, 0.0, 0.0, UiConstants.ResizeGripSize + 4.0);
			zoomBox.Content = zoomRow;
			return zoomBox;
		}

		private void OnZoomEntryCommitted(object sender, EventArgs eventArgs)
		{
			ApplyZoomEntry();
		}

		private void OnZoomEntryUnfocused(object sender, FocusEventArgs eventArgs)
		{
			ApplyZoomEntry();
		}

		private void ApplyZoomEntry()
		{
			int parsed = 0;
			bool valid = int.TryParse(m_zoomEntry.Text, out parsed);
			if (!valid)
			{
				return;
			}
			m_canvas.SetZoomPercentValue(parsed);
		}

		public void SetZoomPercent(int percent)
		{
			SetTitle(m_baseTitle + "  —  " + percent + "%");
			if (m_zoomEntry != null && !m_zoomEntry.IsFocused)
			{
				m_zoomEntry.Text = percent.ToString();
			}
		}

		public CanvasView Canvas()
		{
			return m_canvas;
		}

		public Document DocumentModel()
		{
			return m_document;
		}
	}
}
