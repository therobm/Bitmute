using System;
using System.Collections.Generic;
using Bitmute.Tools;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI.Panels
{
	public class BrushesPanel : ContentView
	{
		private const int CellSize = 40;
		private const int PreviewSize = 48;

		private FlexLayout m_gridHost;
		private List<Border> m_cells;
		private List<CustomBrush> m_cellBrushes;
		private int m_selectedIndex;

		private SKBitmap BuildProceduralPreview(ProceduralBrushShape shape)
		{
			SKBitmap bitmap = new SKBitmap(PreviewSize, PreviewSize, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bitmap.Erase(new SKColor(0, 0, 0, 0));
			SKCanvas canvas = new SKCanvas(bitmap);
			float center = PreviewSize / 2.0f;
			float radius = center - 4.0f;
			float verticalRadius = radius;
			int roundness = shape.m_roundness;
			if (roundness < 5)
			{
				roundness = 5;
			}
			if (roundness > 100)
			{
				roundness = 100;
			}
			verticalRadius = radius * (roundness / 100.0f);

			SKPaint paint = new SKPaint();
			paint.IsAntialias = true;
			if (shape.m_hardness < 100)
			{
				SKColor[] colors = new SKColor[] { new SKColor(0, 0, 0, 255), new SKColor(0, 0, 0, 0) };
				float[] stops = new float[] { 0.0f, 1.0f };
				SKShader shader = SKShader.CreateRadialGradient(new SKPoint(center, center), radius, colors, stops, SKShaderTileMode.Clamp);
				paint.Shader = shader;
				canvas.Save();
				canvas.Translate(center, center);
				canvas.RotateDegrees(shape.m_angle);
				canvas.Translate(-center, -center);
				SKRect ellipse = new SKRect(center - radius, center - verticalRadius, center + radius, center + verticalRadius);
				canvas.DrawOval(ellipse, paint);
				canvas.Restore();
				shader.Dispose();
				paint.Dispose();
				canvas.Dispose();
				return bitmap;
			}
			paint.Color = new SKColor(0, 0, 0, 255);
			canvas.Save();
			canvas.Translate(center, center);
			canvas.RotateDegrees(shape.m_angle);
			canvas.Translate(-center, -center);
			SKRect hardEllipse = new SKRect(center - radius, center - verticalRadius, center + radius, center + verticalRadius);
			canvas.DrawOval(hardEllipse, paint);
			canvas.Restore();
			paint.Dispose();
			canvas.Dispose();
			return bitmap;
		}

		private View BuildThumbnail(CustomBrush brush)
		{
			BoxView background = new BoxView();
			background.WidthRequest = CellSize;
			background.HeightRequest = CellSize;
			background.Color = new Color(0.85f, 0.85f, 0.85f, 1.0f);

			Image mark = new Image();
			mark.WidthRequest = CellSize;
			mark.HeightRequest = CellSize;
			mark.Aspect = Aspect.AspectFit;
			if (brush.m_isProcedural)
			{
				SKBitmap preview = BuildProceduralPreview(brush.m_shape);
				mark.Source = new SKBitmapImageSource { Bitmap = preview };
			}
			else
			{
				mark.Source = new SKBitmapImageSource { Bitmap = brush.m_tip };
			}

			Grid stack = new Grid();
			stack.WidthRequest = CellSize;
			stack.HeightRequest = CellSize;
			stack.Add(background);
			stack.Add(mark);
			return stack;
		}

		private Border BuildCell(CustomBrush brush, bool selected)
		{
			Border cell = new Border();
			cell.WidthRequest = CellSize;
			cell.HeightRequest = CellSize;
			cell.Padding = new Thickness(0.0);
			cell.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2.0) };
			cell.Content = BuildThumbnail(brush);
			if (selected)
			{
				cell.StrokeThickness = 2.0;
				cell.ThemeStroke(UiConstants.AccentLight, UiConstants.AccentDark);
			}
			else
			{
				cell.StrokeThickness = 1.0;
				cell.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			}

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnCellTapped;
			cell.GestureRecognizers.Add(tap);
			return cell;
		}

		private Border BuildActionButton(string text, string tip, EventHandler<TappedEventArgs> handler)
		{
			Label label = new Label();
			label.Text = text;
			label.FontSize = UiConstants.ComponentFontSize;
			label.HorizontalOptions = LayoutOptions.Center;
			label.VerticalOptions = LayoutOptions.Center;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);

			Border button = new Border();
			button.HeightRequest = 24.0;
			button.Padding = new Thickness(8.0, 0.0, 8.0, 0.0);
			button.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			button.StrokeThickness = 0.0;
			button.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			button.Content = label;
			ToolTipProperties.SetText(button, tip);
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += handler;
			button.GestureRecognizers.Add(tap);
			return button;
		}

		private int IndexForCell(object sender)
		{
			for (int index = 0; index < m_cells.Count; index++)
			{
				if (ReferenceEquals(m_cells[index], sender))
				{
					return index;
				}
			}
			return -1;
		}

		private CustomBrush SelectedBrush()
		{
			if (m_selectedIndex < 0 || m_selectedIndex >= m_cellBrushes.Count)
			{
				return null;
			}
			return m_cellBrushes[m_selectedIndex];
		}

		private void OnCellTapped(object sender, TappedEventArgs eventArgs)
		{
			int index = IndexForCell(sender);
			if (index < 0)
			{
				return;
			}
			m_selectedIndex = index;
			CustomBrush brush = m_cellBrushes[index];
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ActivateBrush(brush);
		}

		private void OnRenameClicked(object sender, TappedEventArgs eventArgs)
		{
			CustomBrush brush = SelectedBrush();
			if (brush == null)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.PromptRenameBrush(brush);
		}

		private void OnDeleteClicked(object sender, TappedEventArgs eventArgs)
		{
			CustomBrush brush = SelectedBrush();
			if (brush == null)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.DeleteBrush(brush);
			m_selectedIndex = -1;
		}

		private void OnMoveUpClicked(object sender, TappedEventArgs eventArgs)
		{
			CustomBrush brush = SelectedBrush();
			if (brush == null)
			{
				return;
			}
			int target = m_selectedIndex - 1;
			if (target < 0)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.MoveBrush(brush, target);
			m_selectedIndex = target;
		}

		private void OnMoveDownClicked(object sender, TappedEventArgs eventArgs)
		{
			CustomBrush brush = SelectedBrush();
			if (brush == null)
			{
				return;
			}
			int target = m_selectedIndex + 1;
			if (target >= m_cellBrushes.Count)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.MoveBrush(brush, target);
			m_selectedIndex = target;
		}

		private void OnImportClicked(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ImportBrushSet();
		}

		private void OnExportClicked(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ExportBrushSet();
		}

		public BrushesPanel()
		{
			m_cells = new List<Border>();
			m_cellBrushes = new List<CustomBrush>();
			m_selectedIndex = -1;

			m_gridHost = new FlexLayout();
			m_gridHost.Wrap = FlexWrap.Wrap;
			m_gridHost.Direction = FlexDirection.Row;
			m_gridHost.AlignItems = FlexAlignItems.Start;

			ScrollView gridScroll = new ScrollView();
			gridScroll.Content = m_gridHost;

			Border renameButton = BuildActionButton("Rename", "Rename selected brush", OnRenameClicked);
			Border deleteButton = BuildActionButton("Delete", "Delete selected brush", OnDeleteClicked);
			Border upButton = BuildActionButton("Up", "Move selected brush up", OnMoveUpClicked);
			Border downButton = BuildActionButton("Down", "Move selected brush down", OnMoveDownClicked);
			Border importButton = BuildActionButton("Import…", "Import brush set (.plt)", OnImportClicked);
			Border exportButton = BuildActionButton("Export…", "Export brush set (.plt)", OnExportClicked);

			FlexLayout bottomBar = new FlexLayout();
			bottomBar.Wrap = FlexWrap.Wrap;
			bottomBar.Direction = FlexDirection.Row;
			bottomBar.AlignItems = FlexAlignItems.Start;
			renameButton.Margin = new Thickness(0.0, 0.0, 4.0, 4.0);
			deleteButton.Margin = new Thickness(0.0, 0.0, 4.0, 4.0);
			upButton.Margin = new Thickness(0.0, 0.0, 4.0, 4.0);
			downButton.Margin = new Thickness(0.0, 0.0, 4.0, 4.0);
			importButton.Margin = new Thickness(0.0, 0.0, 4.0, 4.0);
			exportButton.Margin = new Thickness(0.0, 0.0, 4.0, 4.0);
			bottomBar.Add(renameButton);
			bottomBar.Add(deleteButton);
			bottomBar.Add(upButton);
			bottomBar.Add(downButton);
			bottomBar.Add(importButton);
			bottomBar.Add(exportButton);

			Grid layout = new Grid();
			layout.Padding = new Thickness(8.0);
			layout.RowSpacing = 6.0;
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			Grid.SetRow(gridScroll, 0);
			Grid.SetRow(bottomBar, 1);
			layout.Add(gridScroll);
			layout.Add(bottomBar);

			Content = layout;
			Refresh();
		}

		public void Refresh()
		{
			m_gridHost.Clear();
			m_cells.Clear();
			m_cellBrushes.Clear();

			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			List<CustomBrush> brushes = main.CustomBrushes();
			for (int index = 0; index < brushes.Count; index++)
			{
				bool selected = index == m_selectedIndex;
				Border cell = BuildCell(brushes[index], selected);
				cell.Margin = new Thickness(0.0, 0.0, 4.0, 4.0);
				ToolTipProperties.SetText(cell, brushes[index].m_name);
				m_gridHost.Add(cell);
				m_cells.Add(cell);
				m_cellBrushes.Add(brushes[index]);
			}
		}
	}
}
