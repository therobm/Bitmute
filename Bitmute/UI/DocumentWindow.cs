using System;
using Bitmute.Imaging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public class DocumentWindow : FloatingPanel
	{
		private Document m_document;
		private CanvasView m_canvas;
		private TextEditSession m_textSession;
		private int m_guideCreateOrientation;
		private bool m_guidePointerHooked;
		private Entry m_zoomEntry;
		private string m_baseTitle;
		private int m_zoomPercent;
		private Ruler m_topRuler;
		private Ruler m_leftRuler;
		private CanvasScrollbar m_horizontalScrollbar;
		private CanvasScrollbar m_verticalScrollbar;
		private BoxView m_rulerCorner;
		private ColumnDefinition m_rulerColumn;
		private RowDefinition m_rulerRow;
		private bool m_rulersEnabled;

		private static string ColorDepthLabel(eColorDepth depth)
		{
			if (depth == eColorDepth.Eight)
			{
				return "8-bit";
			}
			if (depth == eColorDepth.Sixteen)
			{
				return "16-bit";
			}
			if (depth == eColorDepth.ThirtyTwoFloat)
			{
				return "32-bit float";
			}
			return "8-bit";
		}

		public DocumentWindow(Document document)
		{
			m_document = document;
			m_baseTitle = document.Title() + " (" + ColorDepthLabel(document.ColorDepth()) + ")";
			m_zoomPercent = 100;
			SetTitle(m_baseTitle);

			m_canvas = new CanvasView(document);
			m_canvas.SetOwnerWindow(this);
			m_canvas.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;

			m_topRuler = new Ruler(m_canvas, true);
			m_leftRuler = new Ruler(m_canvas, false);
			m_horizontalScrollbar = new CanvasScrollbar(m_canvas, true);
			m_verticalScrollbar = new CanvasScrollbar(m_canvas, false);

			m_rulerCorner = new BoxView();
			m_rulerCorner.ThemeColor(UiConstants.RulerLight, UiConstants.RulerDark);
			PointerGestureRecognizer cornerPointer = new PointerGestureRecognizer();
			cornerPointer.PointerPressed += OnCornerPointerPressed;
			m_rulerCorner.GestureRecognizers.Add(cornerPointer);

			Grid layout = new Grid();
			m_rulerColumn = new ColumnDefinition(new GridLength(UiConstants.RulerThickness));
			m_rulerRow = new RowDefinition(new GridLength(UiConstants.RulerThickness));
			layout.ColumnDefinitions.Add(m_rulerColumn);
			layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(UiConstants.ResizeGripSize)));
			layout.RowDefinitions.Add(m_rulerRow);
			layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			layout.RowDefinitions.Add(new RowDefinition(new GridLength(UiConstants.DocumentBottomBar)));

			Grid.SetRow(m_rulerCorner, 0);
			Grid.SetColumn(m_rulerCorner, 0);
			layout.Add(m_rulerCorner);

			Grid.SetRow(m_topRuler, 0);
			Grid.SetColumn(m_topRuler, 1);
			layout.Add(m_topRuler);

			Grid.SetRow(m_leftRuler, 1);
			Grid.SetColumn(m_leftRuler, 0);
			layout.Add(m_leftRuler);

			Grid.SetRow(m_canvas, 1);
			Grid.SetColumn(m_canvas, 1);
			layout.Add(m_canvas);

			Grid.SetRow(m_verticalScrollbar, 1);
			Grid.SetColumn(m_verticalScrollbar, 2);
			layout.Add(m_verticalScrollbar);

			Grid bottomBar = new Grid();
			bottomBar.ColumnSpacing = 2.0;
			bottomBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			bottomBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			View zoomField = BuildZoomField();
			Grid.SetColumn(zoomField, 0);
			bottomBar.Add(zoomField);
			Grid.SetColumn(m_horizontalScrollbar, 1);
			bottomBar.Add(m_horizontalScrollbar);
			Grid.SetRow(bottomBar, 2);
			Grid.SetColumn(bottomBar, 0);
			Grid.SetColumnSpan(bottomBar, 2);
			layout.Add(bottomBar);

			SetPanelContent(layout);

			bool rulersEnabled = true;
			MainView main = MainView.Self;
			if (main != null)
			{
				rulersEnabled = main.RulersEnabled();
			}
			SetRulersEnabled(rulersEnabled);
		}

		public void RefreshChrome()
		{
			if (m_topRuler != null)
			{
				m_topRuler.InvalidateSurface();
			}
			if (m_leftRuler != null)
			{
				m_leftRuler.InvalidateSurface();
			}
			if (m_horizontalScrollbar != null)
			{
				m_horizontalScrollbar.InvalidateSurface();
			}
			if (m_verticalScrollbar != null)
			{
				m_verticalScrollbar.InvalidateSurface();
			}
		}

		public void SetRulersEnabled(bool enabled)
		{
			m_rulersEnabled = enabled;
			double size = 0.0;
			if (enabled)
			{
				size = UiConstants.RulerThickness;
			}
			m_rulerColumn.Width = new GridLength(size);
			m_rulerRow.Height = new GridLength(size);
			m_topRuler.IsVisible = enabled;
			m_leftRuler.IsVisible = enabled;
			m_rulerCorner.IsVisible = enabled;
		}

		private View BuildZoomField()
		{
			m_zoomEntry = new Entry();
			m_zoomEntry.Keyboard = Keyboard.Numeric;
			m_zoomEntry.WidthRequest = 54.0;
			m_zoomEntry.HeightRequest = UiConstants.DocumentBottomBar;
			m_zoomEntry.FontSize = 10.0;
			m_zoomEntry.Margin = new Thickness(0.0);
			m_zoomEntry.BackgroundColor = Colors.Transparent;
			m_zoomEntry.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark, UiConstants.TextBackgroundLight, UiConstants.TextBackgroundDark);
			m_zoomEntry.VerticalOptions = LayoutOptions.Center;
			m_zoomEntry.HorizontalTextAlignment = TextAlignment.End;
			m_zoomEntry.Completed += OnZoomEntryCommitted;
			m_zoomEntry.Focused += OnZoomEntryFocused;
			m_zoomEntry.Unfocused += OnZoomEntryUnfocused;

			HorizontalStackLayout zoomRow = new HorizontalStackLayout();
			zoomRow.Spacing = 1.0;
			zoomRow.Padding = new Thickness(4.0, 0.0, 2.0, 0.0);
			zoomRow.VerticalOptions = LayoutOptions.Center;
			zoomRow.Add(m_zoomEntry);
			return zoomRow;
		}

		private static int ExtractInt(string text)
		{
			if (text == null)
			{
				return int.MinValue;
			}
			string digits = "";
			for (int index = 0; index < text.Length; index++)
			{
				char character = text[index];
				if (character >= '0' && character <= '9')
				{
					digits = digits + character;
				}
				else if (digits.Length > 0)
				{
					break;
				}
			}
			if (digits.Length == 0)
			{
				return int.MinValue;
			}
			int result = 0;
			bool valid = int.TryParse(digits, out result);
			if (!valid)
			{
				return int.MinValue;
			}
			return result;
		}

		private void OnZoomEntryCommitted(object sender, EventArgs eventArgs)
		{
			ApplyZoomEntry();
		}

		private void OnZoomEntryFocused(object sender, FocusEventArgs eventArgs)
		{
			int current = ExtractInt(m_zoomEntry.Text);
			if (current != int.MinValue)
			{
				m_zoomEntry.Text = current.ToString();
			}
		}

		private void OnZoomEntryUnfocused(object sender, FocusEventArgs eventArgs)
		{
			ApplyZoomEntry();
		}

		private void ApplyZoomEntry()
		{
			int parsed = ExtractInt(m_zoomEntry.Text);
			if (parsed == int.MinValue)
			{
				return;
			}
			m_canvas.SetZoomPercentValue(parsed);
		}

		public void SetZoomPercent(int percent)
		{
			m_zoomPercent = percent;
			SetTitle(m_baseTitle + "  —  " + percent + "%");
			if (m_zoomEntry != null && !m_zoomEntry.IsFocused)
			{
				m_zoomEntry.Text = percent + "%";
			}
		}

		public void RefreshTitleDepth()
		{
			m_baseTitle = m_document.Title() + " (" + ColorDepthLabel(m_document.ColorDepth()) + ")";
			SetZoomPercent(m_zoomPercent);
		}

		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();
			if (m_guidePointerHooked || Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnGuidePointerMoved), true);
			element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnGuidePointerReleased), true);
			m_guidePointerHooked = true;
		}

		public void BeginGuideCreation(int orientation)
		{
			if (m_document.Guides().IsLocked())
			{
				return;
			}
			MainView main = MainView.Self;
			if (main != null)
			{
				main.ActivateDocumentWindow(this);
			}
			m_guideCreateOrientation = orientation;
			m_canvas.ResetGuideStickyCache();
		}

		private void OnGuidePointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			if (m_guideCreateOrientation == 0)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement canvasElement = m_canvas.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (canvasElement == null)
			{
				return;
			}
			Windows.Foundation.Point position = args.GetCurrentPoint(canvasElement).Position;
			m_canvas.UpdatePendingGuideFromDip(m_guideCreateOrientation, position.X, position.Y);
		}

		private void OnGuidePointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			if (m_guideCreateOrientation == 0)
			{
				return;
			}
			m_guideCreateOrientation = 0;
			m_canvas.CommitPendingGuide();
		}

		private void OnCornerPointerPressed(object sender, PointerEventArgs args)
		{
			BeginGuideCreation(3);
		}

		public CanvasView Canvas()
		{
			return m_canvas;
		}

		public Document DocumentModel()
		{
			return m_document;
		}

		public TextEditSession TextSession()
		{
			if (m_textSession == null)
			{
				m_textSession = new TextEditSession(this);
			}
			return m_textSession;
		}

		public TextEditSession TextSessionOrNull()
		{
			return m_textSession;
		}

		public bool IsTextEditActive()
		{
			return m_textSession != null && m_textSession.IsActive();
		}

		public void CommitTextEdit()
		{
			if (m_textSession != null)
			{
				m_textSession.Commit();
			}
		}
	}
}
