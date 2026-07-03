using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class HistoryPanel : ContentView
	{
		private VerticalStackLayout m_listHost;
		private List<Border> m_rowBorders;
		private List<int> m_rowAppliedCounts;

		private Document Doc()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return null;
			}
			return main.ActiveDocument();
		}

		private void RecompositeActive()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			CanvasView canvas = main.ActiveCanvas();
			if (canvas == null)
			{
				return;
			}
			canvas.MarkComposeDirty();
		}

		private Border BuildRow(string label, int appliedCount, bool selected)
		{
			Label name = new Label();
			name.Text = label;
			name.FontSize = 12.0;
			name.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			name.VerticalOptions = LayoutOptions.Center;

			Border row = new Border();
			row.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
			row.StrokeThickness = 0.0;
			if (selected)
			{
				row.ThemeBg(UiConstants.ToolSelectedLight, UiConstants.ToolSelectedDark);
			}
			else
			{
				row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			}
			row.Content = name;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnRowTapped;
			row.GestureRecognizers.Add(tap);

			m_rowBorders.Add(row);
			m_rowAppliedCounts.Add(appliedCount);

			return row;
		}

		private void OnRowTapped(object sender, TappedEventArgs eventArgs)
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			for (int index = 0; index < m_rowBorders.Count; index++)
			{
				if (ReferenceEquals(m_rowBorders[index], sender))
				{
					document.JumpToHistory(m_rowAppliedCounts[index]);
					RecompositeActive();
					Refresh();
					return;
				}
			}
		}

		public HistoryPanel()
		{
			m_rowBorders = new List<Border>();
			m_rowAppliedCounts = new List<int>();

			m_listHost = new VerticalStackLayout();
			m_listHost.Spacing = 1.0;

			ScrollView listScroll = new ScrollView();
			listScroll.Content = m_listHost;

			Grid layout = new Grid();
			layout.Padding = new Thickness(8.0);
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			Grid.SetRow(listScroll, 0);
			layout.Add(listScroll);

			Content = layout;
			Refresh();
		}

		public void Refresh()
		{
			m_listHost.Clear();
			m_rowBorders.Clear();
			m_rowAppliedCounts.Clear();

			Document document = Doc();
			if (document == null)
			{
				return;
			}

			int currentIndex = document.HistoryIndex();

			bool baseSelected = false;
			if (currentIndex == 0)
			{
				baseSelected = true;
			}
			m_listHost.Add(BuildRow("Open", 0, baseSelected));

			List<string> labels = document.HistoryLabels();
			for (int index = 0; index < labels.Count; index++)
			{
				int appliedCount = index + 1;
				bool selected = false;
				if (appliedCount == currentIndex)
				{
					selected = true;
				}
				m_listHost.Add(BuildRow(labels[index], appliedCount, selected));
			}
		}
	}
}
