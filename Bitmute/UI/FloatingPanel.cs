using System;
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
		private double m_resizeOriginWidth;
		private double m_resizeOriginHeight;
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

			PanGestureRecognizer resizeGesture = new PanGestureRecognizer();
			resizeGesture.PanUpdated += OnResizePan;
			grip.GestureRecognizers.Add(resizeGesture);

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

		private void OnResizePan(object sender, PanUpdatedEventArgs eventArgs)
		{
			if (eventArgs.StatusType == GestureStatus.Started)
			{
				Raise();
				m_resizeOriginWidth = m_width;
				m_resizeOriginHeight = m_height;
				return;
			}

			if (eventArgs.StatusType == GestureStatus.Running)
			{
				double targetWidth = m_resizeOriginWidth + eventArgs.TotalX;
				double targetHeight = m_resizeOriginHeight + eventArgs.TotalY;
				if (targetWidth < UiConstants.PanelMinWidth)
				{
					targetWidth = UiConstants.PanelMinWidth;
				}
				if (targetHeight < UiConstants.PanelMinHeight)
				{
					targetHeight = UiConstants.PanelMinHeight;
				}
				ResizeTo(targetWidth, targetHeight);
			}
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

			Border grip = BuildResizeGrip();
			root.Add(grip);

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
