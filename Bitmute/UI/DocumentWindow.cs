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

		private BoxView BuildRuler()
		{
			BoxView ruler = new BoxView();
			ruler.Color = UiConstants.Ruler;
			return ruler;
		}

		public DocumentWindow(Document document)
		{
			m_document = document;
			SetTitle(document.Title());

			m_canvas = new CanvasView(document);
			m_canvas.BackgroundColor = UiConstants.CanvasInset;

			BoxView corner = new BoxView();
			corner.Color = UiConstants.Ruler;

			BoxView topRuler = BuildRuler();
			BoxView leftRuler = BuildRuler();

			Grid layout = new Grid();
			layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(UiConstants.RulerThickness)));
			layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			layout.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.RulerThickness)));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));

			Grid.SetRow(corner, 0);
			Grid.SetColumn(corner, 0);
			layout.Add(corner);

			Grid.SetRow(topRuler, 0);
			Grid.SetColumn(topRuler, 1);
			layout.Add(topRuler);

			Grid.SetRow(leftRuler, 1);
			Grid.SetColumn(leftRuler, 0);
			layout.Add(leftRuler);

			Grid.SetRow(m_canvas, 1);
			Grid.SetColumn(m_canvas, 1);
			layout.Add(m_canvas);

			SetPanelContent(layout);
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
