using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Bitmute.UI
{
	public class InfoPanel : ContentView
	{
		private const string DashValue = "-";

		private Label m_cursorValue;
		private Label m_pixelRgbaValue;
		private Label m_pixelHexValue;
		private Label m_selectionSizeValue;
		private Label m_selectionBoundsValue;

		private string HexByte(byte value)
		{
			return value.ToString("X2");
		}

		private Label BuildCaption(string text)
		{
			Label caption = new Label();
			caption.Text = text;
			caption.FontSize = 11.0;
			caption.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			caption.VerticalOptions = LayoutOptions.Center;
			return caption;
		}

		private Label BuildValue()
		{
			Label value = new Label();
			value.Text = DashValue;
			value.FontSize = 12.0;
			value.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			value.VerticalOptions = LayoutOptions.Center;
			return value;
		}

		private Grid BuildRow(string caption, Label value)
		{
			Label captionLabel = BuildCaption(caption);

			Grid row = new Grid();
			row.ColumnSpacing = 6.0;
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(captionLabel, 0);
			Grid.SetColumn(value, 1);
			row.Add(captionLabel);
			row.Add(value);
			return row;
		}

		public InfoPanel()
		{
			m_cursorValue = BuildValue();
			m_pixelRgbaValue = BuildValue();
			m_pixelHexValue = BuildValue();
			m_selectionSizeValue = BuildValue();
			m_selectionBoundsValue = BuildValue();

			VerticalStackLayout stack = new VerticalStackLayout();
			stack.Spacing = 4.0;
			stack.Add(BuildRow("Cursor", m_cursorValue));
			stack.Add(BuildRow("Pixel", m_pixelRgbaValue));
			stack.Add(BuildRow("Hex", m_pixelHexValue));
			stack.Add(BuildRow("Sel", m_selectionSizeValue));
			stack.Add(BuildRow("At", m_selectionBoundsValue));

			Grid layout = new Grid();
			layout.Padding = new Thickness(8.0);
			layout.RowSpacing = 6.0;
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			Grid.SetRow(stack, 0);
			layout.Add(stack);

			Content = layout;
		}

		public void UpdateCursor(int x, int y)
		{
			m_cursorValue.Text = "X: " + x + "  Y: " + y;
		}

		public void UpdatePixel(SKColor color, bool hasPixel)
		{
			if (!hasPixel)
			{
				m_pixelRgbaValue.Text = DashValue;
				m_pixelHexValue.Text = DashValue;
				return;
			}
			m_pixelRgbaValue.Text = "R:" + color.Red + " G:" + color.Green + " B:" + color.Blue + " A:" + color.Alpha;
			m_pixelHexValue.Text = "#" + HexByte(color.Red) + HexByte(color.Green) + HexByte(color.Blue) + HexByte(color.Alpha);
		}

		public void UpdateSelection(SKRectI bounds, bool active)
		{
			if (!active)
			{
				m_selectionSizeValue.Text = DashValue;
				m_selectionBoundsValue.Text = DashValue;
				return;
			}
			m_selectionSizeValue.Text = bounds.Width + " x " + bounds.Height;
			m_selectionBoundsValue.Text = "@ (" + bounds.Left + ", " + bounds.Top + ")";
		}

		public void ClearReadout()
		{
			m_cursorValue.Text = DashValue;
			m_pixelRgbaValue.Text = DashValue;
			m_pixelHexValue.Text = DashValue;
			m_selectionSizeValue.Text = DashValue;
			m_selectionBoundsValue.Text = DashValue;
		}
	}
}
