using System;
using System.Collections.Generic;
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
	public class SwatchesPanel : ContentView
	{
		private const int CellSize = 16;
		private const int CellsPerRow = 12;
		private const int RecentCap = 12;

		private List<SKColor> m_swatches;
		private List<SKColor> m_recent;
		private List<Border> m_swatchCells;
		private List<SKColor> m_swatchCellColors;
		private List<Border> m_recentCells;
		private List<SKColor> m_recentCellColors;
		private FlexLayout m_swatchHost;
		private FlexLayout m_recentHost;
		private int m_selectedIndex;

		private static Color FromSkColor(SKColor color)
		{
			return new Color(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
		}

		private static bool AltHeldNow()
		{
			Windows.UI.Core.CoreVirtualKeyStates altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
			bool altHeld = (altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
			return altHeld;
		}

		private void ApplyColor(SKColor color, bool foreground)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ApplyPickedColor(color, foreground);
			AddRecent(color);
		}

		private SKColor CurrentForeground()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return new SKColor(0, 0, 0, 255);
			}
			ToolState state = main.CurrentToolState();
			if (state == null)
			{
				return new SKColor(0, 0, 0, 255);
			}
			return state.Foreground();
		}

		private Border BuildCell(SKColor color, EventHandler<TappedEventArgs> handler)
		{
			BoxView box = new BoxView();
			box.WidthRequest = CellSize;
			box.HeightRequest = CellSize;
			box.Color = FromSkColor(color);

			Border cell = new Border();
			cell.WidthRequest = CellSize;
			cell.HeightRequest = CellSize;
			cell.Padding = new Thickness(0.0);
			cell.StrokeThickness = 1.0;
			cell.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			cell.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2.0) };
			cell.Content = box;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += handler;
			cell.GestureRecognizers.Add(tap);
			return cell;
		}

		private Border BuildActionButton(string text, string tip, EventHandler<TappedEventArgs> handler)
		{
			Label label = new Label();
			label.Text = text;
			label.FontSize = 11.0;
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

		private int SwatchIndexForCell(object sender)
		{
			for (int i = 0; i < m_swatchCells.Count; i++)
			{
				if (ReferenceEquals(m_swatchCells[i], sender))
				{
					return i;
				}
			}
			return -1;
		}

		private int RecentIndexForCell(object sender)
		{
			for (int i = 0; i < m_recentCells.Count; i++)
			{
				if (ReferenceEquals(m_recentCells[i], sender))
				{
					return i;
				}
			}
			return -1;
		}

		private void OnSwatchTapped(object sender, TappedEventArgs eventArgs)
		{
			int index = SwatchIndexForCell(sender);
			if (index < 0)
			{
				return;
			}
			m_selectedIndex = index;
			SKColor color = m_swatchCellColors[index];
			bool foreground = true;
			if (AltHeldNow())
			{
				foreground = false;
			}
			ApplyColor(color, foreground);
		}

		private void OnSwatchCellHandlerChanged(object sender, EventArgs eventArgs)
		{
			VisualElement cellElement = sender as VisualElement;
			if (cellElement == null || cellElement.Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement platformElement = cellElement.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (platformElement == null)
			{
				return;
			}
			platformElement.RightTapped -= OnSwatchCellRightTapped;
			platformElement.RightTapped += OnSwatchCellRightTapped;
		}

		private void OnSwatchCellRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs eventArgs)
		{
			for (int index = 0; index < m_swatchCells.Count; index++)
			{
				Border candidate = m_swatchCells[index];
				if (candidate.Handler != null && ReferenceEquals(candidate.Handler.PlatformView, sender))
				{
					MainView main = MainView.Self;
					if (main == null)
					{
						return;
					}
					m_selectedIndex = index;
					main.OpenSwatchColorPicker(index, m_swatchCellColors[index]);
					return;
				}
			}
		}

		private void OnRecentTapped(object sender, TappedEventArgs eventArgs)
		{
			int index = RecentIndexForCell(sender);
			if (index < 0)
			{
				return;
			}
			SKColor color = m_recentCellColors[index];
			bool foreground = true;
			if (AltHeldNow())
			{
				foreground = false;
			}
			ApplyColor(color, foreground);
		}

		private void OnAddClicked(object sender, TappedEventArgs eventArgs)
		{
			SKColor color = CurrentForeground();
			m_swatches.Add(color);
			m_selectedIndex = m_swatches.Count - 1;
			Refresh();
		}

		private void OnRemoveClicked(object sender, TappedEventArgs eventArgs)
		{
			if (m_swatches.Count == 0)
			{
				return;
			}
			int index = m_selectedIndex;
			if (index < 0 || index >= m_swatches.Count)
			{
				index = m_swatches.Count - 1;
			}
			m_swatches.RemoveAt(index);
			m_selectedIndex = index - 1;
			Refresh();
		}

		private void OnLoadClicked(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.SetStatusMessage("Load palette: choose a .gpl file, then call LoadPalette");
		}

		private void OnSaveClicked(object sender, TappedEventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.SetStatusMessage("Save palette: choose a .gpl path, then call SavePalette");
		}

		private void BuildDefaultPalette()
		{
			m_swatches.Add(new SKColor(0, 0, 0, 255));
			m_swatches.Add(new SKColor(255, 255, 255, 255));
			m_swatches.Add(new SKColor(128, 128, 128, 255));
			m_swatches.Add(new SKColor(255, 0, 0, 255));
			m_swatches.Add(new SKColor(255, 128, 0, 255));
			m_swatches.Add(new SKColor(255, 255, 0, 255));
			m_swatches.Add(new SKColor(0, 255, 0, 255));
			m_swatches.Add(new SKColor(0, 128, 0, 255));
			m_swatches.Add(new SKColor(0, 255, 255, 255));
			m_swatches.Add(new SKColor(0, 0, 255, 255));
			m_swatches.Add(new SKColor(128, 0, 255, 255));
			m_swatches.Add(new SKColor(255, 0, 255, 255));
		}

		public SwatchesPanel()
		{
			m_swatches = new List<SKColor>();
			m_recent = new List<SKColor>();
			m_swatchCells = new List<Border>();
			m_swatchCellColors = new List<SKColor>();
			m_recentCells = new List<Border>();
			m_recentCellColors = new List<SKColor>();
			m_selectedIndex = -1;

			BuildDefaultPalette();

			m_swatchHost = new FlexLayout();
			m_swatchHost.Wrap = FlexWrap.Wrap;
			m_swatchHost.Direction = FlexDirection.Row;
			m_swatchHost.AlignItems = FlexAlignItems.Start;

			ScrollView swatchScroll = new ScrollView();
			swatchScroll.Content = m_swatchHost;

			Label recentLabel = new Label();
			recentLabel.Text = "Recent";
			recentLabel.FontSize = 11.0;
			recentLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);

			m_recentHost = new FlexLayout();
			m_recentHost.Wrap = FlexWrap.Wrap;
			m_recentHost.Direction = FlexDirection.Row;
			m_recentHost.AlignItems = FlexAlignItems.Start;

			VerticalStackLayout recentStack = new VerticalStackLayout();
			recentStack.Spacing = 3.0;
			recentStack.Add(recentLabel);
			recentStack.Add(m_recentHost);

			Border addButton = BuildActionButton("Add", "Add foreground to swatches", OnAddClicked);
			Border removeButton = BuildActionButton("Remove", "Remove selected swatch", OnRemoveClicked);
			Border loadButton = BuildActionButton("Load…", "Load palette (.gpl)", OnLoadClicked);
			Border saveButton = BuildActionButton("Save…", "Save palette (.gpl)", OnSaveClicked);

			HorizontalStackLayout bottomBar = new HorizontalStackLayout();
			bottomBar.Spacing = 4.0;
			bottomBar.Add(addButton);
			bottomBar.Add(removeButton);
			bottomBar.Add(loadButton);
			bottomBar.Add(saveButton);

			Grid layout = new Grid();
			layout.Padding = new Thickness(8.0);
			layout.RowSpacing = 6.0;
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			Grid.SetRow(swatchScroll, 0);
			Grid.SetRow(recentStack, 1);
			Grid.SetRow(bottomBar, 2);
			layout.Add(swatchScroll);
			layout.Add(recentStack);
			layout.Add(bottomBar);

			Content = layout;
			Refresh();
		}

		public void SetSwatchColor(int index, SKColor color)
		{
			if (index < 0 || index >= m_swatches.Count)
			{
				return;
			}
			m_swatches[index] = color;
			m_selectedIndex = index;
			Refresh();
		}

		public void AddRecent(SKColor color)
		{
			for (int i = 0; i < m_recent.Count; i++)
			{
				if (m_recent[i] == color)
				{
					m_recent.RemoveAt(i);
					break;
				}
			}
			m_recent.Insert(0, color);
			for (int i = m_recent.Count - 1; i >= RecentCap; i--)
			{
				m_recent.RemoveAt(i);
			}
			Refresh();
		}

		public void LoadPalette(string path)
		{
			List<SKColor> colors = GplFile.Read(path);
			m_swatches = colors;
			m_selectedIndex = -1;
			Refresh();
		}

		public void SavePalette(string path)
		{
			GplFile.Write(path, m_swatches);
		}

		public void Refresh()
		{
			m_swatchHost.Clear();
			m_swatchCells.Clear();
			m_swatchCellColors.Clear();
			for (int i = 0; i < m_swatches.Count; i++)
			{
				Border cell = BuildCell(m_swatches[i], OnSwatchTapped);
				cell.Margin = new Thickness(0.0, 0.0, 2.0, 2.0);
				ToolTipProperties.SetText(cell, "Click: set foreground · Right-click: edit swatch");
				cell.HandlerChanged += OnSwatchCellHandlerChanged;
				m_swatchHost.Add(cell);
				m_swatchCells.Add(cell);
				m_swatchCellColors.Add(m_swatches[i]);
			}

			m_recentHost.Clear();
			m_recentCells.Clear();
			m_recentCellColors.Clear();
			for (int i = 0; i < m_recent.Count; i++)
			{
				Border cell = BuildCell(m_recent[i], OnRecentTapped);
				cell.Margin = new Thickness(0.0, 0.0, 2.0, 2.0);
				m_recentHost.Add(cell);
				m_recentCells.Add(cell);
				m_recentCellColors.Add(m_recent[i]);
			}
		}
	}
}
