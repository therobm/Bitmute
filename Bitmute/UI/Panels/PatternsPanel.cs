using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI.Panels
{
	public class PatternsPanel : ContentView
	{
		private const int CellSize = 40;

		private FlexLayout m_gridHost;
		private List<Border> m_cells;
		private List<Pattern> m_cellPatterns;
		private int m_selectedIndex;

		private ImageSource BuildTile(Pattern pattern)
		{
			return new SKBitmapImageSource { Bitmap = pattern.m_bitmap };
		}

		private Border BuildCell(Pattern pattern, bool selected)
		{
			Image tile = new Image();
			tile.WidthRequest = CellSize;
			tile.HeightRequest = CellSize;
			tile.Aspect = Aspect.AspectFill;
			tile.Source = BuildTile(pattern);

			Border cell = new Border();
			cell.WidthRequest = CellSize;
			cell.HeightRequest = CellSize;
			cell.Padding = new Thickness(0.0);
			cell.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2.0) };
			cell.Content = tile;
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

		private Pattern SelectedPattern()
		{
			if (m_selectedIndex < 0 || m_selectedIndex >= m_cellPatterns.Count)
			{
				return null;
			}
			return m_cellPatterns[m_selectedIndex];
		}

		private void OnCellTapped(object sender, TappedEventArgs eventArgs)
		{
			int index = IndexForCell(sender);
			if (index < 0)
			{
				return;
			}
			m_selectedIndex = index;
			Pattern pattern = m_cellPatterns[index];
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ActivatePattern(pattern);
		}

		private void OnRenameClicked(object sender, TappedEventArgs eventArgs)
		{
			Pattern pattern = SelectedPattern();
			if (pattern == null)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.PromptRenamePattern(pattern);
		}

		private void OnDeleteClicked(object sender, TappedEventArgs eventArgs)
		{
			Pattern pattern = SelectedPattern();
			if (pattern == null)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.DeletePattern(pattern);
			m_selectedIndex = -1;
		}

		private void OnMoveUpClicked(object sender, TappedEventArgs eventArgs)
		{
			Pattern pattern = SelectedPattern();
			if (pattern == null)
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
			main.MovePattern(pattern, target);
			m_selectedIndex = target;
		}

		private void OnMoveDownClicked(object sender, TappedEventArgs eventArgs)
		{
			Pattern pattern = SelectedPattern();
			if (pattern == null)
			{
				return;
			}
			int target = m_selectedIndex + 1;
			if (target >= m_cellPatterns.Count)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.MovePattern(pattern, target);
			m_selectedIndex = target;
		}

		private void OnImportClicked(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ImportPatternSet();
		}

		private void OnExportClicked(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ExportPatternSet();
		}

		public PatternsPanel()
		{
			m_cells = new List<Border>();
			m_cellPatterns = new List<Pattern>();
			m_selectedIndex = -1;

			m_gridHost = new FlexLayout();
			m_gridHost.Wrap = FlexWrap.Wrap;
			m_gridHost.Direction = FlexDirection.Row;
			m_gridHost.AlignItems = FlexAlignItems.Start;

			ScrollView gridScroll = new ScrollView();
			gridScroll.Content = m_gridHost;

			Border renameButton = BuildActionButton("Rename", "Rename selected pattern", OnRenameClicked);
			Border deleteButton = BuildActionButton("Delete", "Delete selected pattern", OnDeleteClicked);
			Border upButton = BuildActionButton("Up", "Move selected pattern up", OnMoveUpClicked);
			Border downButton = BuildActionButton("Down", "Move selected pattern down", OnMoveDownClicked);
			Border importButton = BuildActionButton("Import…", "Import pattern set (.plt)", OnImportClicked);
			Border exportButton = BuildActionButton("Export…", "Export pattern set (.plt)", OnExportClicked);

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
			m_cellPatterns.Clear();

			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			List<Pattern> patterns = main.Patterns();
			for (int index = 0; index < patterns.Count; index++)
			{
				bool selected = index == m_selectedIndex;
				Border cell = BuildCell(patterns[index], selected);
				cell.Margin = new Thickness(0.0, 0.0, 4.0, 4.0);
				ToolTipProperties.SetText(cell, patterns[index].m_name);
				m_gridHost.Add(cell);
				m_cells.Add(cell);
				m_cellPatterns.Add(patterns[index]);
			}
		}
	}
}
