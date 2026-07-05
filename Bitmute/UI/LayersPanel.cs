using System;
using System.Collections.Generic;
using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
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
		private int m_pendingContextLayer;
		private double m_pendingContextX;
		private double m_pendingContextY;

		private VerticalStackLayout m_listHost;
		private SliderField m_opacityField;
		private Picker m_blendPicker;
		private Border m_lockAlphaButton;
		private Border m_lockPixelsButton;
		private Border m_lockPositionButton;
		private Border m_lockAllButton;
		private bool m_suppress;
		private List<Border> m_eyeButtons;
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

			if (layer.IsText())
			{
				SKPaint glyphPaint = new SKPaint();
				glyphPaint.Color = new SKColor(0x40, 0x40, 0x40);
				glyphPaint.IsAntialias = true;
				SKFont glyphFont = new SKFont(SKTypeface.Default, ThumbnailHeight * 0.7f);
				canvas.DrawText("T", ThumbnailWidth / 2.0f, ThumbnailHeight * 0.78f, SKTextAlign.Center, glyphFont, glyphPaint);
				glyphFont.Dispose();
				glyphPaint.Dispose();
				canvas.Dispose();
				return new SKBitmapImageSource { Bitmap = thumbnail };
			}

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
			SKPixmap pixmap = source.PeekPixels();
			SKImage image = SKImage.FromPixels(pixmap);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
			SKPaint imagePaint = new SKPaint();
			canvas.DrawImage(image, destination, sampling, imagePaint);
			imagePaint.Dispose();
			image.Dispose();
			pixmap.Dispose();
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

		public void RefreshActiveThumbnail()
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			List<Layer> layers = document.Layers();
			int activeIndex = document.ActiveLayerIndex();
			for (int index = 0; index < m_thumbnailImages.Count; index++)
			{
				int layerIndex = m_thumbnailLayers[index];
				if (layerIndex != activeIndex || layerIndex < 0 || layerIndex >= layers.Count)
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

			string eyeIcon = "eye.png";
			if (!layer.IsVisible())
			{
				eyeIcon = "eye_off.png";
			}
			IconView eyeView = new IconView(eyeIcon);
			eyeView.WidthRequest = 16.0;
			eyeView.HeightRequest = 16.0;
			eyeView.BackgroundColor = Colors.Transparent;
			eyeView.HorizontalOptions = LayoutOptions.Center;
			eyeView.VerticalOptions = LayoutOptions.Center;

			Border eye = new Border();
			eye.WidthRequest = 24.0;
			eye.HeightRequest = 22.0;
			eye.Padding = new Thickness(0.0);
			eye.BackgroundColor = Colors.Transparent;
			eye.StrokeThickness = 0.0;
			eye.Content = eyeView;
			ToolTipProperties.SetText(eye, "Toggle visibility");
			TapGestureRecognizer eyeTap = new TapGestureRecognizer();
			eyeTap.Tapped += OnEyeClicked;
			eye.GestureRecognizers.Add(eyeTap);
			m_eyeButtons.Add(eye);
			m_eyeLayers.Add(layerIndex);

			Image thumbnail = new Image();
			thumbnail.WidthRequest = ThumbnailWidth;
			thumbnail.HeightRequest = ThumbnailHeight;
			thumbnail.VerticalOptions = LayoutOptions.Center;
			thumbnail.Source = BuildThumbnail(layer);
			m_thumbnailImages.Add(thumbnail);
			m_thumbnailLayers.Add(layerIndex);

			Label fxGlyph = new Label();
			if (layer.LayerStyle().HasAnyEffect())
			{
				fxGlyph.Text = "fx";
			}
			fxGlyph.FontSize = UiConstants.ComponentFontSize;
			fxGlyph.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			fxGlyph.VerticalOptions = LayoutOptions.Center;

			Label name = new Label();
			name.Text = layer.Name();
			name.FontSize = UiConstants.PanelFontSize;
			name.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			name.VerticalOptions = LayoutOptions.Center;

			Label lockGlyph = new Label();
			if (layer.LockAll() || layer.LockPixels() || layer.LockPosition() || layer.LockAlpha())
			{
				lockGlyph.Text = "L";
			}
			lockGlyph.FontSize = UiConstants.ComponentFontSize;
			lockGlyph.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			lockGlyph.VerticalOptions = LayoutOptions.Center;
			lockGlyph.HorizontalOptions = LayoutOptions.End;

			Grid rowGrid = new Grid();
			rowGrid.ColumnSpacing = 4.0;
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(eye, 0);
			Grid.SetColumn(thumbnail, 1);
			Grid.SetColumn(fxGlyph, 2);
			Grid.SetColumn(name, 3);
			Grid.SetColumn(lockGlyph, 4);
			rowGrid.Add(eye);
			rowGrid.Add(thumbnail);
			rowGrid.Add(fxGlyph);
			rowGrid.Add(name);
			rowGrid.Add(lockGlyph);

			Border row = new Border();
			row.Padding = new Thickness(4.0, 2.0, 4.0, 2.0);
			row.StrokeThickness = 0.0;
			if (layerIndex == document.ActiveLayerIndex())
			{
				row.ThemeBg(UiConstants.ToolSelectedLight, UiConstants.ToolSelectedDark);
			}
			else if (document.IsLayerSelected(layerIndex))
			{
				row.ThemeBg(UiConstants.MenuOpenLight, UiConstants.MenuOpenDark);
			}
			else
			{
				row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			}
			row.Content = rowGrid;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnRowTapped;
			row.GestureRecognizers.Add(tap);
			TapGestureRecognizer doubleTap = new TapGestureRecognizer();
			doubleTap.NumberOfTapsRequired = 2;
			doubleTap.Tapped += OnRowDoubleTapped;
			row.GestureRecognizers.Add(doubleTap);
			PanGestureRecognizer pan = new PanGestureRecognizer();
			pan.PanUpdated += OnRowPan;
			row.GestureRecognizers.Add(pan);
			row.HandlerChanged += OnRowHandlerChanged;
			m_rowBorders.Add(row);
			m_rowLayers.Add(layerIndex);

			return row;
		}

		private void OnEyeClicked(object sender, TappedEventArgs eventArgs)
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

		private static bool ControlHeld()
		{
			Windows.UI.Core.CoreVirtualKeyStates state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
			return (state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
		}

		private static bool ShiftHeld()
		{
			Windows.UI.Core.CoreVirtualKeyStates state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
			return (state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
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
					int layerIndex = m_rowLayers[index];
					if (ControlHeld())
					{
						document.ToggleLayerSelection(layerIndex);
					}
					else if (ShiftHeld())
					{
						document.SelectLayerRange(layerIndex);
					}
					else
					{
						document.SetActiveLayerIndex(layerIndex);
					}
					Refresh();
					return;
				}
			}
		}

		private void OnRowDoubleTapped(object sender, TappedEventArgs eventArgs)
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
					Layer layer = document.Layers()[m_rowLayers[index]];
					if (layer.IsText())
					{
						MainView main = MainView.Self;
						if (main != null)
						{
							main.BeginTextEditForLayer(layer);
						}
					}
					return;
				}
			}
		}

		private Border BuildActionButton(string icon, string tip, EventHandler<TappedEventArgs> handler)
		{
			IconView view = new IconView(icon);
			view.WidthRequest = 16.0;
			view.HeightRequest = 16.0;
			view.BackgroundColor = Colors.Transparent;
			view.HorizontalOptions = LayoutOptions.Center;
			view.VerticalOptions = LayoutOptions.Center;

			Border button = new Border();
			button.WidthRequest = 30.0;
			button.HeightRequest = 24.0;
			button.Padding = new Thickness(0.0);
			button.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			button.StrokeThickness = 0.0;
			button.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			button.Content = view;
			ToolTipProperties.SetText(button, tip);
			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += handler;
			button.GestureRecognizers.Add(tap);
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

		private static double PageCoordinate(VisualElement element, bool horizontal)
		{
			double total = 0.0;
			Element current = element;
			for (int guard = 0; guard < 100; guard++)
			{
				VisualElement visual = current as VisualElement;
				if (visual == null)
				{
					break;
				}
				if (horizontal)
				{
					total = total + visual.X;
				}
				else
				{
					total = total + visual.Y;
				}
				Element parent = current.Parent;
				if (parent == null)
				{
					break;
				}
				current = parent;
			}
			return total;
		}

		private void OnRowHandlerChanged(object sender, EventArgs eventArgs)
		{
			VisualElement rowElement = sender as VisualElement;
			if (rowElement == null || rowElement.Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement platformElement = rowElement.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (platformElement == null)
			{
				return;
			}
			platformElement.RightTapped -= OnRowRightTapped;
			platformElement.RightTapped += OnRowRightTapped;
		}

		private void OnRowRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs eventArgs)
		{
			for (int index = 0; index < m_rowBorders.Count; index++)
			{
				Border candidate = m_rowBorders[index];
				if (candidate.Handler != null && ReferenceEquals(candidate.Handler.PlatformView, sender))
				{
					m_pendingContextLayer = m_rowLayers[index];
					m_pendingContextX = PageCoordinate(candidate, true);
					m_pendingContextY = PageCoordinate(candidate, false);
					Dispatcher.Dispatch(ShowPendingLayerContextMenu);
					return;
				}
			}
		}

		private void ShowPendingLayerContextMenu()
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			document.SetActiveLayerIndex(m_pendingContextLayer);
			Refresh();
			MainView main = MainView.Self;
			if (main == null)
			{
				return;
			}
			main.ShowLayerContextMenu(m_pendingContextLayer, m_pendingContextX, m_pendingContextY);
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
			document.BeginCanvasEdit("Reorder Layer");
			document.MoveLayer(fromIndex, fromIndex - positions);
			document.EndCanvasEdit();
			RecompositeActive();
			Refresh();
		}

		private void OnAddClicked(object sender, TappedEventArgs eventArgs)
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main != null)
			{
				main.AddNewLayer();
			}
		}

		private void OnDeleteClicked(object sender, TappedEventArgs eventArgs)
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main != null)
			{
				main.RequestDeleteActiveLayer();
			}
		}

		private void OnBlendChanged(object sender, System.EventArgs eventArgs)
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
			int index = m_blendPicker.SelectedIndex;
			if (index < 0)
			{
				index = 0;
			}
			layer.SetBlendMode((eBlendMode)index);
			RecompositeActive();
		}

		private void OnDuplicateClicked(object sender, TappedEventArgs eventArgs)
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			document.BeginCanvasEdit("Duplicate Layer");
			document.DuplicateLayer(document.ActiveLayerIndex());
			document.EndCanvasEdit();
			RecompositeActive();
			Refresh();
		}

		private void OnMergeClicked(object sender, TappedEventArgs eventArgs)
		{
			Document document = Doc();
			if (document == null)
			{
				return;
			}
			MainView main = MainView.Self;
			if (main != null)
			{
				main.MergeSelectedLayers();
			}
		}

		private void OnOpacityValue(int opacity)
		{
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
			int scaled = ((opacity * 255) + 50) / 100;
			if (scaled > 255)
			{
				scaled = 255;
			}
			layer.SetOpacity((byte)scaled);
			RecompositeActive();
		}

		private Layer ActiveLayerOrNull()
		{
			Document document = Doc();
			if (document == null)
			{
				return null;
			}
			return document.ActiveLayer();
		}

		private Border BuildLockToggle(string text, string tip, EventHandler<TappedEventArgs> handler)
		{
			Label label = new Label();
			label.Text = text;
			label.FontSize = 10.0;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.HorizontalOptions = LayoutOptions.Center;
			label.VerticalOptions = LayoutOptions.Center;

			Border button = new Border();
			button.HeightRequest = 22.0;
			button.Padding = new Thickness(6.0, 0.0, 6.0, 0.0);
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

		private void SetToggleActive(Border button, bool active)
		{
			if (button == null)
			{
				return;
			}
			if (active)
			{
				button.ThemeBg(UiConstants.ToolSelectedLight, UiConstants.ToolSelectedDark);
			}
			else
			{
				button.ThemeBg(UiConstants.ChromeRaisedLight, UiConstants.ChromeRaisedDark);
			}
		}

		private void RefreshLockButtons(Layer active)
		{
			bool hasActive = active != null;
			SetToggleActive(m_lockAlphaButton, hasActive && active.LockAlpha());
			SetToggleActive(m_lockPixelsButton, hasActive && active.LockPixels());
			SetToggleActive(m_lockPositionButton, hasActive && active.LockPosition());
			SetToggleActive(m_lockAllButton, hasActive && active.LockAll());
		}

		private void OnLockAlphaClicked(object sender, TappedEventArgs eventArgs)
		{
			Layer layer = ActiveLayerOrNull();
			if (layer == null)
			{
				return;
			}
			layer.SetLockAlpha(!layer.LockAlpha());
			Refresh();
		}

		private void OnLockPixelsClicked(object sender, TappedEventArgs eventArgs)
		{
			Layer layer = ActiveLayerOrNull();
			if (layer == null)
			{
				return;
			}
			layer.SetLockPixels(!layer.LockPixels());
			Refresh();
		}

		private void OnLockPositionClicked(object sender, TappedEventArgs eventArgs)
		{
			Layer layer = ActiveLayerOrNull();
			if (layer == null)
			{
				return;
			}
			layer.SetLockPosition(!layer.LockPosition());
			Refresh();
		}

		private void OnLockAllClicked(object sender, TappedEventArgs eventArgs)
		{
			Layer layer = ActiveLayerOrNull();
			if (layer == null)
			{
				return;
			}
			layer.SetLockAll(!layer.LockAll());
			Refresh();
		}

		public LayersPanel()
		{
			m_eyeButtons = new List<Border>();
			m_eyeLayers = new List<int>();
			m_rowBorders = new List<Border>();
			m_rowLayers = new List<int>();
			m_thumbnailImages = new List<Image>();
			m_thumbnailLayers = new List<int>();

			Border addButton = BuildActionButton("layer_add.png", "New layer", OnAddClicked);
			Border duplicateButton = BuildActionButton("layer_duplicate.png", "Duplicate layer", OnDuplicateClicked);
			Border mergeButton = BuildActionButton("layer_merge.png", "Merge down", OnMergeClicked);
			Border deleteButton = BuildActionButton("layer_delete.png", "Delete layer", OnDeleteClicked);

			Label opacityLabel = new Label();
			opacityLabel.Text = "Opacity";
			opacityLabel.FontSize = UiConstants.ComponentFontSize;
			opacityLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			opacityLabel.VerticalOptions = LayoutOptions.Center;

			m_opacityField = new SliderField(0, 100, 100, "%", OnOpacityValue);
			m_opacityField.HorizontalOptions = LayoutOptions.End;
			m_opacityField.VerticalOptions = LayoutOptions.Center;

			Grid opacityRow = new Grid();
			opacityRow.ColumnSpacing = 6.0;
			opacityRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			opacityRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(opacityLabel, 0);
			Grid.SetColumn(m_opacityField, 1);
			opacityRow.Add(opacityLabel);
			opacityRow.Add(m_opacityField);

			Label blendLabel = new Label();
			blendLabel.Text = "Blend";
			blendLabel.FontSize = UiConstants.ComponentFontSize;
			blendLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			blendLabel.VerticalOptions = LayoutOptions.Center;

			m_blendPicker = new Picker();
			m_blendPicker.FontSize = UiConstants.ComponentFontSize;
			m_blendPicker.FontSize = UiConstants.ComponentFontSize;
			m_blendPicker.HeightRequest = UiConstants.ComponentHeight;
			m_blendPicker.Margin = new Thickness(0);
			m_blendPicker.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_blendPicker.Items.Add("Normal");
			m_blendPicker.Items.Add("Dissolve");
			m_blendPicker.Items.Add("Darken");
			m_blendPicker.Items.Add("Multiply");
			m_blendPicker.Items.Add("Color Burn");
			m_blendPicker.Items.Add("Linear Burn");
			m_blendPicker.Items.Add("Darker Color");
			m_blendPicker.Items.Add("Lighten");
			m_blendPicker.Items.Add("Screen");
			m_blendPicker.Items.Add("Color Dodge");
			m_blendPicker.Items.Add("Linear Dodge (Add)");
			m_blendPicker.Items.Add("Lighter Color");
			m_blendPicker.Items.Add("Overlay");
			m_blendPicker.Items.Add("Soft Light");
			m_blendPicker.Items.Add("Hard Light");
			m_blendPicker.Items.Add("Vivid Light");
			m_blendPicker.Items.Add("Linear Light");
			m_blendPicker.Items.Add("Pin Light");
			m_blendPicker.Items.Add("Hard Mix");
			m_blendPicker.Items.Add("Difference");
			m_blendPicker.Items.Add("Exclusion");
			m_blendPicker.Items.Add("Subtract");
			m_blendPicker.Items.Add("Divide");
			m_blendPicker.Items.Add("Hue");
			m_blendPicker.Items.Add("Saturation");
			m_blendPicker.Items.Add("Color");
			m_blendPicker.Items.Add("Luminosity");
			m_blendPicker.SelectedIndex = 0;
			m_blendPicker.SelectedIndexChanged += OnBlendChanged;

			Grid blendRow = new Grid();
			blendRow.ColumnSpacing = 6.0;
			blendRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			blendRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			Grid.SetColumn(blendLabel, 0);
			Grid.SetColumn(m_blendPicker, 1);
			blendRow.Add(blendLabel);
			blendRow.Add(m_blendPicker);

			Label lockLabel = new Label();
			lockLabel.Text = "Lock";
			lockLabel.FontSize = UiConstants.ComponentFontSize;
			lockLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			lockLabel.VerticalOptions = LayoutOptions.Center;

			m_lockAlphaButton = BuildLockToggle("Alpha", "Lock transparency (paint only where opaque)", OnLockAlphaClicked);
			m_lockPixelsButton = BuildLockToggle("Pixels", "Lock pixels (no painting)", OnLockPixelsClicked);
			m_lockPositionButton = BuildLockToggle("Pos", "Lock position (no move)", OnLockPositionClicked);
			m_lockAllButton = BuildLockToggle("All", "Lock everything", OnLockAllClicked);

			HorizontalStackLayout lockRow = new HorizontalStackLayout();
			lockRow.Spacing = 3.0;
			lockRow.Add(lockLabel);
			lockRow.Add(m_lockAlphaButton);
			lockRow.Add(m_lockPixelsButton);
			lockRow.Add(m_lockPositionButton);
			lockRow.Add(m_lockAllButton);

			VerticalStackLayout topStack = new VerticalStackLayout();
			topStack.Spacing = 4.0;
			topStack.Add(opacityRow);
			topStack.Add(blendRow);
			topStack.Add(lockRow);

			m_listHost = new VerticalStackLayout();
			m_listHost.Spacing = 1.0;

			ScrollView listScroll = new ScrollView();
			listScroll.Content = m_listHost;

			HorizontalStackLayout bottomBar = new HorizontalStackLayout();
			bottomBar.Spacing = 4.0;
			bottomBar.Add(addButton);
			bottomBar.Add(duplicateButton);
			bottomBar.Add(mergeButton);
			bottomBar.Add(deleteButton);

			Grid layout = new Grid();
			layout.Padding = new Thickness(8.0);
			layout.RowSpacing = 6.0;
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			Grid.SetRow(topStack, 0);
			Grid.SetRow(listScroll, 1);
			Grid.SetRow(bottomBar, 2);
			layout.Add(topStack);
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
				m_opacityField.SetValueSilently(((active.Opacity() * 100) + 127) / 255);
				m_suppress = true;
				m_blendPicker.SelectedIndex = (int)active.BlendMode();
				m_suppress = false;
			}
			RefreshLockButtons(active);
		}
	}
}
