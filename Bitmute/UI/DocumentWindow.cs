using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class DocumentWindow : FloatingPanel
	{
		private Document m_document;
		private CanvasView m_canvas;
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
			m_canvas.BackgroundColor = UiConstants.CanvasInset;

			m_topRuler = new Ruler(m_canvas, true);
			m_leftRuler = new Ruler(m_canvas, false);
			m_horizontalScrollbar = new CanvasScrollbar(m_canvas, true);
			m_verticalScrollbar = new CanvasScrollbar(m_canvas, false);

			m_rulerCorner = new BoxView();
			m_rulerCorner.Color = UiConstants.CanvasPaper;

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

			SetPanelContent(layout);

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

		public void SetZoomPercent(int percent)
		{
			SetTitle(m_baseTitle + "  —  " + percent + "%");
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
