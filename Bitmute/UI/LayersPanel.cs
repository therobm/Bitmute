using System.Collections.Generic;
using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Bitmute.UI
{
	public class LayersPanel : ContentView
	{
		private const int ThumbnailWidth = 44;
		private const int ThumbnailHeight = 32;
		private const int ThumbnailCheckerCell = 6;
		private const double RowHeightEstimate = 38.0;

		private int m_dragRowLayer = -1;
		private double m_dragTotalY;

		private VerticalStackLayout m_listHost;
		private Slider m_opacity;
		private Label m_opacityValue;
		private bool m_suppress;
		private List<Button> m_eyeButtons;
		private List<int> m_eyeLayers;
		private List<Border> m_rowBorders;
		private List<int> m_rowLayers;
		private List<Image> m_thumbnailImages;
		private List<int> m_thumbnailLayers;

		private ImageSource BuildThumbnail(Layer layer)
		{
			SKBitmap thumbnail = new SKBitmap(ThumbnailWidth, ThumbnailHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
			SKCanvas canvas = new SKCanvas(thumbnail);
			canvas.Clear(new SKColor(0xFF, 0xFF, 0xFF));
			SKPaint darkPaint = new SKPaint();
			darkPaint.Color = new SKColor(0xC8, 0xC8, 0xC8);
			for (int cellY = 0; cellY < ThumbnailHeight; cellY = cellY + ThumbnailCheckerCell)
			{
				for (int cellX = 0; cellX < ThumbnailWidth; cellX = cellX + ThumbnailCheckerCell)
				{
					int parity = (cellX / ThumbnailCheckerCell) + (cellY / ThumbnailCheckerCell);
					if ((parity & 1) == 1)
					{
						canvas.DrawRect(new SKRect(cellX, cellY, cellX + ThumbnailCheckerCell, cellY + ThumbnailCheckerCell), darkPaint);
					}
				}
			}
			darkPaint.Dispose();

			SKBitmap source = layer.Bitmap();
			float sourceAspect = (float)source.Width / (float)source.Height;
			float thumbnailAspect = (float)ThumbnailWidth / (float)ThumbnailHeight;
			float destinationWidth = ThumbnailWidth;
			float destinationHeight = ThumbnailHeight;
			if (sourceAspect > thumbnailAspect)
			{
				destinationHeight = ThumbnailWidth / sourceAspect;
			}
			else
			{
				destinationWidth = ThumbnailHeight * sourceAspect;
			}
			float destinationLeft = (ThumbnailWidth - destinationWidth) / 2.0f;
			float destinationTop = (ThumbnailHeight - destinationHeight) / 2.0f;
			SKRect destination = new SKRect(destinationLeft, destinationTop, destinationLeft + destinationWidth, destinationTop + destinationHeight);
			SKImage image = SKImage.FromBitmap(source);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
			SKPaint imagePaint = new SKPaint();
			canvas.DrawImage(image, destination, sampling, imagePaint);
			imagePaint.Dispose();
			image.Dispose();
			canvas.Dispose();
			return new SKBitmapImageSource { Bitmap = thumbnail };
		}

		public void RefreshThumbnails()
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			List<Layer> layers = document.Layers();
			for (int index = 0; index < m_thumbnailImages.Count; index++)
			{
				int layerIndex = m_thumbnailLayers[index];
				if (layerIndex < 0 || layerIndex >= layers.Count)
				{
					continue;
				}
				m_thumbnailImages[index].Source = BuildThumbnail(layers[layerIndex]);
			}
		}

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

		private Border BuildLayerRow(Document document, int layerIndex)
		{
			Layer layer = document.Layers()[layerIndex];

			Button eye = new Button();
			if (layer.IsVisible())
			{
				eye.Text = "●";
			}
			else
			{
				eye.Text = "○";
			}
			eye.FontSize = 11.0;
			eye.WidthRequest = 26.0;
			eye.HeightRequest = 24.0;
			eye.Padding = new Thickness(0.0);
			eye.BackgroundColor = Colors.Transparent;
			eye.TextColor = UiConstants.OnSurface;
			eye.Clicked += OnEyeClicked;
			m_eyeButtons.Add(eye);
			m_eyeLayers.Add(layerIndex);

			Image thumbnail = new Image();
			thumbnail.WidthRequest = ThumbnailWidth;
			thumbnail.HeightRequest = ThumbnailHeight;
			thumbnail.VerticalOptions = LayoutOptions.Center;
			thumbnail.Source = BuildThumbnail(layer);
			m_thumbnailImages.Add(thumbnail);
			m_thumbnailLayers.Add(layerIndex);

			Label name = new Label();
			name.Text = layer.Name();
			name.FontSize = 12.0;
			name.TextColor = UiConstants.OnSurface;
			name.VerticalOptions = LayoutOptions.Center;

			Grid rowGrid = new Grid();
			rowGrid.ColumnSpacing = 4.0;
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(eye, 0);
			Grid.SetColumn(thumbnail, 1);
			Grid.SetColumn(name, 2);
			rowGrid.Add(eye);
			rowGrid.Add(thumbnail);
			rowGrid.Add(name);

			Border row = new Border();
			row.Padding = new Thickness(4.0, 2.0, 4.0, 2.0);
			row.StrokeThickness = 0.0;
			if (layerIndex == document.ActiveLayerIndex())
			{
				row.BackgroundColor = UiConstants.ToolSelected;
			}
			else
			{
				row.BackgroundColor = UiConstants.PanelSurface;
			}
			row.Content = rowGrid;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnRowTapped;
			row.GestureRecognizers.Add(tap);
			PanGestureRecognizer pan = new PanGestureRecognizer();
			pan.PanUpdated += OnRowPan;
			row.GestureRecognizers.Add(pan);
			m_rowBorders.Add(row);
			m_rowLayers.Add(layerIndex);

			return row;
		}

		private void OnEyeClicked(object sender, System.EventArgs eventArgs)
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			for (int index = 0; index < m_eyeButtons.Count; index++)
			{
				if (ReferenceEquals(m_eyeButtons[index], sender))
				{
					Layer layer = document.Layers()[m_eyeLayers[index]];
					layer.SetVisible(!layer.IsVisible());
					RecompositeActive();
					Refresh();
					return;
				}
			}
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
					document.SetActiveLayerIndex(m_rowLayers[index]);
					Refresh();
					return;
				}
			}
		}

		private Button BuildActionButton(string text, double width, System.EventHandler handler)
		{
			Button button = new Button();
			button.Text = text;
			button.FontSize = 11.0;
			button.WidthRequest = width;
			button.HeightRequest = 20.0;
			button.Padding = new Thickness(0.0);
			button.BackgroundColor = UiConstants.ChromeRaised;
			button.TextColor = UiConstants.OnSurface;
			button.Clicked += handler;
			return button;
		}

		private int LayerIndexForRow(object sender)
		{
			for (int index = 0; index < m_rowBorders.Count; index++)
			{
				if (ReferenceEquals(m_rowBorders[index], sender))
				{
					return m_rowLayers[index];
				}
			}
			return -1;
		}

		private void OnRowPan(object sender, PanUpdatedEventArgs eventArgs)
		{
			View row = sender as View;
			if (eventArgs.StatusType == GestureStatus.Started)
			{
				m_dragRowLayer = LayerIndexForRow(sender);
				m_dragTotalY = 0.0;
				return;
			}
			if (eventArgs.StatusType == GestureStatus.Running)
			{
				m_dragTotalY = eventArgs.TotalY;
				if (row != null)
				{
					row.TranslationY = eventArgs.TotalY;
				}
				return;
			}
			if (row != null)
			{
				row.TranslationY = 0.0;
			}
			int fromIndex = m_dragRowLayer;
			m_dragRowLayer = -1;
			if (fromIndex < 0)
			{
				return;
			}
			int positions = (int)System.Math.Round(m_dragTotalY / RowHeightEstimate);
			if (positions == 0)
			{
				return;
			}
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			document.MoveLayer(fromIndex, fromIndex - positions);
			RecompositeActive();
			Refresh();
		}

		private void OnAddClicked(object sender, System.EventArgs eventArgs)
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			int layerNumber = document.Layers().Count + 1;
			document.AddLayer("Layer " + layerNumber);
			RecompositeActive();
			Refresh();
		}

		private void OnDeleteClicked(object sender, System.EventArgs eventArgs)
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			document.DeleteLayer(document.ActiveLayerIndex());
			RecompositeActive();
			Refresh();
		}

		private void OnOpacityChanged(object sender, ValueChangedEventArgs eventArgs)
		{
			if (m_suppress)
			{
				return;
			}
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null)
			{
				return;
			}
			byte opacity = (byte)m_opacity.Value;
			layer.SetOpacity(opacity);
			m_opacityValue.Text = opacity.ToString();
			RecompositeActive();
		}

		public LayersPanel()
		{
			m_eyeButtons = new List<Button>();
			m_eyeLayers = new List<int>();
			m_rowBorders = new List<Border>();
			m_rowLayers = new List<int>();
			m_thumbnailImages = new List<Image>();
			m_thumbnailLayers = new List<int>();

			Button addButton = BuildActionButton("+", 30.0, OnAddClicked);
			Button deleteButton = BuildActionButton("Del", 40.0, OnDeleteClicked);

			Label opacityLabel = new Label();
			opacityLabel.Text = "Opacity";
			opacityLabel.FontSize = 11.0;
			opacityLabel.TextColor = UiConstants.TextDim;
			opacityLabel.VerticalOptions = LayoutOptions.Center;

			m_opacity = new Slider();
			m_opacity.Minimum = 0.0;
			m_opacity.Maximum = 255.0;
			m_opacity.ValueChanged += OnOpacityChanged;

			m_opacityValue = new Label();
			m_opacityValue.FontSize = 11.0;
			m_opacityValue.TextColor = UiConstants.OnSurface;
			m_opacityValue.WidthRequest = 30.0;
			m_opacityValue.HorizontalTextAlignment = TextAlignment.End;
			m_opacityValue.VerticalOptions = LayoutOptions.Center;

			Grid opacityRow = new Grid();
			opacityRow.ColumnSpacing = 6.0;
			opacityRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			opacityRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			opacityRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(opacityLabel, 0);
			Grid.SetColumn(m_opacity, 1);
			Grid.SetColumn(m_opacityValue, 2);
			opacityRow.Add(opacityLabel);
			opacityRow.Add(m_opacity);
			opacityRow.Add(m_opacityValue);

			m_listHost = new VerticalStackLayout();
			m_listHost.Spacing = 1.0;

			ScrollView listScroll = new ScrollView();
			listScroll.Content = m_listHost;

			HorizontalStackLayout bottomBar = new HorizontalStackLayout();
			bottomBar.Spacing = 4.0;
			bottomBar.Add(addButton);
			bottomBar.Add(deleteButton);

			Grid layout = new Grid();
			layout.Padding = new Thickness(8.0);
			layout.RowSpacing = 6.0;
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			Grid.SetRow(opacityRow, 0);
			Grid.SetRow(listScroll, 1);
			Grid.SetRow(bottomBar, 2);
			layout.Add(opacityRow);
			layout.Add(listScroll);
			layout.Add(bottomBar);

			Content = layout;
			Refresh();
		}

		public void Refresh()
		{
			m_listHost.Clear();
			m_eyeButtons.Clear();
			m_eyeLayers.Clear();
			m_rowBorders.Clear();
			m_rowLayers.Clear();
			m_thumbnailImages.Clear();
			m_thumbnailLayers.Clear();

			Document document = Doc();
			if (document == null)
			{
				return;
			}

			List<Layer> layers = document.Layers();
			for (int index = layers.Count - 1; index >= 0; index--)
			{
				m_listHost.Add(BuildLayerRow(document, index));
			}

			Layer active = document.ActiveLayer();
			if (active != null)
			{
				m_suppress = true;
				m_opacity.Value = active.Opacity();
				m_suppress = false;
				m_opacityValue.Text = active.Opacity().ToString();
			}
		}
	}
}
