using System;
using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Bitmute.UI
{
	public class FloatingPanel : ContentView
	{
		private double m_x;
		private double m_y;
		private double m_width;
		private double m_height;
		private double m_dragOriginX;
		private double m_dragOriginY;
		private const double ResizeEdgeThickness = 5.0;

		private double m_resizeOriginWidth;
		private double m_resizeOriginHeight;
		private double m_resizeOriginX;
		private double m_resizeOriginY;
		private double m_resizeStartPointerX;
		private double m_resizeStartPointerY;
		private Border m_leftEdge;
		private Border m_rightEdge;
		private Border m_topEdge;
		private Border m_bottomEdge;
		private Border m_grip;
		private Border m_activeResizeStrip;
		private Dictionary<Microsoft.UI.Xaml.UIElement, Border> m_resizeStrips;
		private Label m_titleLabel;
		private Grid m_titleBar;
		private ContentView m_contentHost;
		private bool m_minimized;
		private double m_restoreHeight;
		private bool m_maximized;
		private double m_preMaxX;
		private double m_preMaxY;
		private double m_preMaxWidth;
		private double m_preMaxHeight;

		private Button BuildTitleBarButton(string text, EventHandler handler)
		{
			Button button = new Button();
			button.Text = text;
			button.FontSize = UiConstants.PanelFontSize;
			button.WidthRequest = UiConstants.CloseButtonSize;
			button.HeightRequest = UiConstants.CloseButtonSize;
			button.Padding = new Thickness(0.0);
			button.BackgroundColor = Colors.Transparent;
			button.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			button.VerticalOptions = LayoutOptions.Center;
			button.Clicked += handler;
			return button;
		}

		private Grid BuildTitleBar()
		{
			Grid titleBar = new Grid();
			titleBar.HeightRequest = UiConstants.TitleBarHeight;
			titleBar.ThemeBg(UiConstants.TitleBarLight, UiConstants.TitleBarDark);
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			titleBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			titleBar.Padding = new Thickness(10.0, 0.0, 4.0, 0.0);

			m_titleLabel = new Label();
			m_titleLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			m_titleLabel.FontSize = 13.0;
			m_titleLabel.VerticalOptions = LayoutOptions.Center;
			m_titleLabel.LineBreakMode = LineBreakMode.TailTruncation;
			Grid.SetColumn(m_titleLabel, 0);
			titleBar.Add(m_titleLabel);

			Button minimizeButton = BuildTitleBarButton("─", OnMinimizeClicked);
			Grid.SetColumn(minimizeButton, 1);
			titleBar.Add(minimizeButton);

			Button maximizeButton = BuildTitleBarButton("□", OnMaximizeClicked);
			Grid.SetColumn(maximizeButton, 2);
			titleBar.Add(maximizeButton);

			Button closeButton = BuildTitleBarButton("✕", OnCloseClicked);
			Grid.SetColumn(closeButton, 3);
			titleBar.Add(closeButton);

			PanGestureRecognizer dragGesture = new PanGestureRecognizer();
			dragGesture.PanUpdated += OnTitleBarPan;
			titleBar.GestureRecognizers.Add(dragGesture);

			TapGestureRecognizer raiseGesture = new TapGestureRecognizer();
			raiseGesture.Tapped += OnPanelTapped;
			titleBar.GestureRecognizers.Add(raiseGesture);

			m_titleBar = titleBar;
			return titleBar;
		}

		public void SetTitleBarActive(bool active)
		{
			if (m_titleBar == null)
			{
				return;
			}
			if (active)
			{
				m_titleBar.ThemeBg(UiConstants.TitleBarActiveLight, UiConstants.TitleBarActiveDark);
			}
			else
			{
				m_titleBar.ThemeBg(UiConstants.TitleBarLight, UiConstants.TitleBarDark);
			}
		}

		private Border BuildResizeGrip()
		{
			Border grip = new Border();
			grip.WidthRequest = UiConstants.ResizeGripSize;
			grip.HeightRequest = UiConstants.ResizeGripSize;
			grip.ThemeBg(UiConstants.ResizeGripLight, UiConstants.ResizeGripDark);
			grip.HorizontalOptions = LayoutOptions.End;
			grip.VerticalOptions = LayoutOptions.End;
			grip.StrokeThickness = 0.0;
			grip.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0.0, 0.0, UiConstants.PanelCornerRadius, 0.0) };

			grip.HandlerChanged += OnResizeHandlerChanged;

			return grip;
		}

		private void OnTitleBarPan(object sender, PanUpdatedEventArgs eventArgs)
		{
			if (eventArgs.StatusType == GestureStatus.Started)
			{
				Raise();
				m_dragOriginX = m_x;
				m_dragOriginY = m_y;
				return;
			}

			if (eventArgs.StatusType == GestureStatus.Running)
			{
				double targetX = m_dragOriginX + eventArgs.TotalX;
				double targetY = m_dragOriginY + eventArgs.TotalY;
				MoveTo(targetX, targetY);
			}
		}

		private void OnResizeHandlerChanged(object sender, EventArgs eventArgs)
		{
			Border edge = sender as Border;
			if (edge == null)
			{
				return;
			}
			if (edge.Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = edge.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			if (m_resizeStrips.ContainsKey(element))
			{
				return;
			}
			m_resizeStrips[element] = edge;
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnResizePointerPressed), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnResizePointerMoved), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnResizePointerReleased), true);
		}

		private void OnResizePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs eventArgs)
		{
			Microsoft.UI.Xaml.UIElement element = sender as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			Border strip;
			bool found = m_resizeStrips.TryGetValue(element, out strip);
			if (!found)
			{
				return;
			}
			Raise();
			m_activeResizeStrip = strip;
			Microsoft.UI.Input.PointerPoint point = eventArgs.GetCurrentPoint(null);
			m_resizeStartPointerX = point.Position.X;
			m_resizeStartPointerY = point.Position.Y;
			m_resizeOriginX = m_x;
			m_resizeOriginY = m_y;
			m_resizeOriginWidth = m_width;
			m_resizeOriginHeight = m_height;
			element.CapturePointer(eventArgs.Pointer);
		}

		private void OnResizePointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs eventArgs)
		{
			if (m_activeResizeStrip == null)
			{
				return;
			}
			Microsoft.UI.Input.PointerPoint point = eventArgs.GetCurrentPoint(null);
			double deltaX = point.Position.X - m_resizeStartPointerX;
			double deltaY = point.Position.Y - m_resizeStartPointerY;
			ApplyResize(deltaX, deltaY);
		}

		private void OnResizePointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs eventArgs)
		{
			Microsoft.UI.Xaml.UIElement element = sender as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			m_activeResizeStrip = null;
			element.ReleasePointerCapture(eventArgs.Pointer);
		}

		private void ApplyResize(double deltaX, double deltaY)
		{
			if (m_activeResizeStrip == m_rightEdge)
			{
				double targetWidth = m_resizeOriginWidth + deltaX;
				if (targetWidth < UiConstants.PanelMinWidth)
				{
					targetWidth = UiConstants.PanelMinWidth;
				}
				ResizeTo(targetWidth, m_height);
				return;
			}
			if (m_activeResizeStrip == m_bottomEdge)
			{
				double targetHeight = m_resizeOriginHeight + deltaY;
				if (targetHeight < UiConstants.PanelMinHeight)
				{
					targetHeight = UiConstants.PanelMinHeight;
				}
				ResizeTo(m_width, targetHeight);
				return;
			}
			if (m_activeResizeStrip == m_leftEdge)
			{
				double delta = deltaX;
				double targetWidth = m_resizeOriginWidth - delta;
				if (targetWidth < UiConstants.PanelMinWidth)
				{
					delta = m_resizeOriginWidth - UiConstants.PanelMinWidth;
					targetWidth = UiConstants.PanelMinWidth;
				}
				m_x = m_resizeOriginX + delta;
				ResizeTo(targetWidth, m_height);
				return;
			}
			if (m_activeResizeStrip == m_topEdge)
			{
				double delta = deltaY;
				double targetHeight = m_resizeOriginHeight - delta;
				if (targetHeight < UiConstants.PanelMinHeight)
				{
					delta = m_resizeOriginHeight - UiConstants.PanelMinHeight;
					targetHeight = UiConstants.PanelMinHeight;
				}
				m_y = m_resizeOriginY + delta;
				ResizeTo(m_width, targetHeight);
				return;
			}
			if (m_activeResizeStrip == m_grip)
			{
				double targetWidth = m_resizeOriginWidth + deltaX;
				double targetHeight = m_resizeOriginHeight + deltaY;
				if (targetWidth < UiConstants.PanelMinWidth)
				{
					targetWidth = UiConstants.PanelMinWidth;
				}
				if (targetHeight < UiConstants.PanelMinHeight)
				{
					targetHeight = UiConstants.PanelMinHeight;
				}
				ResizeTo(targetWidth, targetHeight);
				return;
			}
		}

		private Border BuildResizeEdge(double edgeWidth, double edgeHeight, LayoutOptions horizontalOptions, LayoutOptions verticalOptions)
		{
			Border edge = new Border();
			if (edgeWidth > 0.0)
			{
				edge.WidthRequest = edgeWidth;
			}
			if (edgeHeight > 0.0)
			{
				edge.HeightRequest = edgeHeight;
			}
			edge.BackgroundColor = Colors.Black;
			edge.Opacity = 0.0;
			edge.StrokeThickness = 0.0;
			edge.HorizontalOptions = horizontalOptions;
			edge.VerticalOptions = verticalOptions;
			edge.HandlerChanged += OnResizeHandlerChanged;
			return edge;
		}

		private void OnPanelTapped(object sender, TappedEventArgs eventArgs)
		{
			Raise();
		}

		private void OnCloseClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ClosePanel(this);
			}
		}

		private void OnMaximizeClicked(object sender, EventArgs eventArgs)
		{
			if (m_maximized)
			{
				m_maximized = false;
				SetBounds(m_preMaxX, m_preMaxY, m_preMaxWidth, m_preMaxHeight);
				return;
			}
			double workspaceWidth = WorkspaceWidth();
			double workspaceHeight = WorkspaceHeight();
			if (workspaceWidth <= 0.0 || workspaceHeight <= 0.0)
			{
				return;
			}
			Raise();
			m_maximized = true;
			m_preMaxX = m_x;
			m_preMaxY = m_y;
			m_preMaxWidth = m_width;
			m_preMaxHeight = m_height;
			SetBounds(0.0, 0.0, workspaceWidth, workspaceHeight);
		}

		private void OnMinimizeClicked(object sender, EventArgs eventArgs)
		{
			if (m_minimized)
			{
				m_minimized = false;
				m_contentHost.IsVisible = true;
				ResizeTo(m_width, m_restoreHeight);
				return;
			}
			m_minimized = true;
			m_restoreHeight = m_height;
			m_contentHost.IsVisible = false;
			ResizeTo(m_width, UiConstants.TitleBarHeight + UiConstants.MinimizedPadding);
		}

		private void Raise()
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.BringToFront(this);
			}
		}

		private void MoveTo(double x, double y)
		{
			double minVisible = 25.0;
			double workspaceWidth = WorkspaceWidth();
			double workspaceHeight = WorkspaceHeight();
			if (y < 0.0)
			{
				y = 0.0;
			}
			double minX = minVisible - m_width;
			if (x < minX)
			{
				x = minX;
			}
			if (workspaceWidth > 0.0)
			{
				double maxX = workspaceWidth - minVisible;
				if (x > maxX)
				{
					x = maxX;
				}
			}
			if (workspaceHeight > 0.0)
			{
				double maxY = workspaceHeight - minVisible;
				if (y > maxY)
				{
					y = maxY;
				}
			}
			m_x = x;
			m_y = y;
			AbsoluteLayout.SetLayoutBounds(this, new Rect(m_x, m_y, m_width, m_height));
		}

		private void ResizeTo(double width, double height)
		{
			m_width = width;
			m_height = height;
			AbsoluteLayout.SetLayoutBounds(this, new Rect(m_x, m_y, m_width, m_height));
		}

		private double WorkspaceWidth()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return -1.0;
			}
			return main.WorkspaceWidth();
		}

		private double WorkspaceHeight()
		{
			MainView main = MainView.Self;
			if (main == null)
			{
				return -1.0;
			}
			return main.WorkspaceHeight();
		}

		public FloatingPanel()
		{
			m_resizeStrips = new Dictionary<Microsoft.UI.Xaml.UIElement, Border>();
			Grid root = new Grid();

			Border frame = new Border();
			frame.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			frame.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			frame.StrokeThickness = UiConstants.PanelBorderThickness;
			frame.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(UiConstants.PanelCornerRadius) };

			Grid frameContent = new Grid();
			frameContent.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			frameContent.RowDefinitions.Add(new RowDefinition(GridLength.Star));

			Grid titleBar = BuildTitleBar();
			Grid.SetRow(titleBar, 0);
			frameContent.Add(titleBar);

			m_contentHost = new ContentView();
			m_contentHost.Padding = new Thickness(0.0);
			Grid.SetRow(m_contentHost, 1);
			frameContent.Add(m_contentHost);

			frame.Content = frameContent;
			root.Add(frame);

			m_leftEdge = BuildResizeEdge(ResizeEdgeThickness, 0.0, LayoutOptions.Start, LayoutOptions.Fill);
			m_rightEdge = BuildResizeEdge(ResizeEdgeThickness, 0.0, LayoutOptions.End, LayoutOptions.Fill);
			m_topEdge = BuildResizeEdge(0.0, ResizeEdgeThickness, LayoutOptions.Fill, LayoutOptions.Start);
			m_bottomEdge = BuildResizeEdge(0.0, ResizeEdgeThickness, LayoutOptions.Fill, LayoutOptions.End);
			root.Add(m_leftEdge);
			root.Add(m_rightEdge);
			root.Add(m_topEdge);
			root.Add(m_bottomEdge);

			m_grip = BuildResizeGrip();
			root.Add(m_grip);

			Content = root;
		}

		public void SetBounds(double x, double y, double width, double height)
		{
			m_x = x;
			m_y = y;
			m_width = width;
			m_height = height;
			AbsoluteLayout.SetLayoutBounds(this, new Rect(m_x, m_y, m_width, m_height));
			AbsoluteLayout.SetLayoutFlags(this, AbsoluteLayoutFlags.None);
		}

		protected void SetTitle(string title)
		{
			m_titleLabel.Text = title;
		}

		protected void SetPanelContent(View content)
		{
			m_contentHost.Content = content;
		}
	}
}
