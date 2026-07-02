using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class DocumentWindow : FloatingPanel
	{
		private BoxView BuildRuler()
		{
			BoxView ruler = new BoxView();
			ruler.Color = UiConstants.Ruler;
			return ruler;
		}

		public DocumentWindow(string title)
		{
			SetTitle(title);

			BoxView canvas = new BoxView();
			canvas.Color = UiConstants.CanvasPaper;
			canvas.WidthRequest = UiConstants.DefaultDocumentWidth;
			canvas.HeightRequest = UiConstants.DefaultDocumentHeight;
			canvas.HorizontalOptions = LayoutOptions.Center;
			canvas.VerticalOptions = LayoutOptions.Center;

			Grid canvasHost = new Grid();
			canvasHost.BackgroundColor = UiConstants.CanvasInset;
			canvasHost.Padding = new Thickness(20.0);
			canvasHost.Add(canvas);

			ScrollView scroll = new ScrollView();
			scroll.Orientation = ScrollOrientation.Both;
			scroll.BackgroundColor = UiConstants.CanvasInset;
			scroll.Content = canvasHost;

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

			Grid.SetRow(scroll, 1);
			Grid.SetColumn(scroll, 1);
			layout.Add(scroll);

			SetPanelContent(layout);
		}
	}
}
