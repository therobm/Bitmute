using Bitmute.Imaging;
using Bitmute.Tools;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using SkiaSharp;

namespace Bitmute.UI
{
	public class TextEditSession
	{
		private MainView m_main;
		private ToolState m_toolState;
		private AbsoluteLayout m_host;
		private Editor m_textEditor;
		private CanvasView m_textEditCanvas;
		private Bitmute.Imaging.Layer m_textEditLayer;
		private bool m_textEditActive;
		private bool m_textEditorKeyHooked;
		private Microsoft.Maui.Dispatching.IDispatcherTimer m_caretTimer;
		private bool m_caretVisible;
		private string m_textPreEditString;
		private bool m_textPreEditWasNew;
		private float m_textPreEditSize;
		private string m_textPreEditFont;
		private bool m_textPreEditBold;
		private bool m_textPreEditItalic;
		private SKColor m_textPreEditColor;
		private int m_textPreEditAlign;
		private int m_textPreEditAntiAlias;
		private bool m_textPreEditLeadingAuto;
		private float m_textPreEditLeading;
		private int m_textPreEditTracking;
		private int m_textPreEditHorizontalScale;
		private int m_textPreEditVerticalScale;
		private int m_textPreEditBaselineShift;
		private bool m_textPreEditFauxBold;
		private bool m_textPreEditFauxItalic;
		private bool m_textPreEditKerningAuto;

		private void EnsureTextEditor()
		{
			if (m_textEditor != null)
			{
				return;
			}
			m_textEditor = new Editor();
			m_textEditor.Opacity = 0.0;
			m_textEditor.WidthRequest = 200.0;
			m_textEditor.HeightRequest = 40.0;
			m_textEditor.AutoSize = EditorAutoSizeOption.Disabled;
			m_textEditor.TextChanged += OnTextEditorChanged;
			m_textEditor.HandlerChanged += OnTextEditorHandlerChanged;
		}

		private void PositionTextEditor()
		{
			double editorX = 8.0;
			double editorY = 8.0;
			DocumentWindow window = m_main.ActiveDocumentWindow();
			if (window != null)
			{
				Rect bounds = AbsoluteLayout.GetLayoutBounds(window);
				editorX = bounds.X + UiConstants.PanelBorderThickness + 8.0;
				editorY = bounds.Y + UiConstants.TitleBarHeight + UiConstants.PanelBorderThickness + 8.0;
			}
			AbsoluteLayout.SetLayoutBounds(m_textEditor, new Rect(editorX, editorY, 200.0, 40.0));
			AbsoluteLayout.SetLayoutFlags(m_textEditor, AbsoluteLayoutFlags.None);
		}

		private void OnTextEditorHandlerChanged(object sender, System.EventArgs eventArgs)
		{
			if (m_textEditorKeyHooked)
			{
				return;
			}
			if (m_textEditor == null || m_textEditor.Handler == null)
			{
				return;
			}
			Microsoft.UI.Xaml.UIElement element = m_textEditor.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
			if (element == null)
			{
				return;
			}
			element.PreviewKeyDown += OnTextEditorKeyDown;
			m_textEditorKeyHooked = true;
		}

		private void OnTextEditorKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs args)
		{
			if (!m_textEditActive)
			{
				return;
			}
			if (args.Key == Windows.System.VirtualKey.Escape)
			{
				Cancel();
				args.Handled = true;
				return;
			}
			if (args.Key == Windows.System.VirtualKey.Enter)
			{
				Windows.UI.Core.CoreVirtualKeyStates shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
				bool shiftHeld = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
				if (!shiftHeld)
				{
					Commit();
					args.Handled = true;
				}
			}
		}

		private void OnTextEditorChanged(object sender, TextChangedEventArgs eventArgs)
		{
			if (!m_textEditActive || m_textEditLayer == null)
			{
				return;
			}
			string raw = m_textEditor.Text;
			if (raw == null)
			{
				raw = "";
			}
			string normalized = raw.Replace('\r', '\n');
			m_textEditLayer.SetTextString(normalized);
			m_textEditLayer.RenderText();
			if (m_textEditCanvas != null)
			{
				m_textEditCanvas.MarkComposeDirty();
				m_textEditCanvas.InvalidateSurface();
			}
		}

		private void ApplyToolStateToLayer(Layer layer)
		{
			layer.SetTextStyle(m_toolState.TextSize(), m_toolState.TextFontFamily(), m_toolState.TextBold(), m_toolState.TextItalic(), m_toolState.Foreground(), m_toolState.TextAlign(), m_toolState.TextAntiAlias());
			layer.SetTextCharacter(m_toolState.TextLeadingAuto(), m_toolState.TextLeading(), m_toolState.TextTracking(), m_toolState.TextHorizontalScale(), m_toolState.TextVerticalScale(), m_toolState.TextBaselineShift(), m_toolState.TextFauxBold(), m_toolState.TextFauxItalic(), m_toolState.TextKerningAuto());
		}

		private void LoadTextStyleFromLayer(Layer layer)
		{
			m_toolState.SetTextSize((int)layer.TextSize());
			m_toolState.SetTextFontFamily(layer.TextFontFamily());
			m_toolState.SetTextBold(layer.TextBold());
			m_toolState.SetTextItalic(layer.TextItalic());
			m_toolState.SetTextAlign(layer.TextAlign());
			m_toolState.SetTextAntiAlias(layer.TextAntiAlias());
			m_toolState.SetForeground(layer.TextColor());
			m_toolState.SetTextLeadingAuto(layer.TextLeadingAuto());
			m_toolState.SetTextLeading(layer.TextLeading());
			m_toolState.SetTextTracking(layer.TextTracking());
			m_toolState.SetTextHorizontalScale(layer.TextHorizontalScale());
			m_toolState.SetTextVerticalScale(layer.TextVerticalScale());
			m_toolState.SetTextBaselineShift(layer.TextBaselineShift());
			m_toolState.SetTextFauxBold(layer.TextFauxBold());
			m_toolState.SetTextFauxItalic(layer.TextFauxItalic());
			m_toolState.SetTextKerningAuto(layer.TextKerningAuto());
			m_main.SyncTextOptionsBar();
			m_main.RefreshToolPaletteColors();
		}

		private void StartCaretBlink()
		{
			m_caretVisible = true;
			if (m_caretTimer == null && m_main.Dispatcher != null)
			{
				m_caretTimer = m_main.Dispatcher.CreateTimer();
				m_caretTimer.Interval = System.TimeSpan.FromMilliseconds(530.0);
				m_caretTimer.Tick += OnCaretTick;
			}
			if (m_caretTimer != null)
			{
				m_caretTimer.Start();
			}
		}

		private void StopCaretBlink()
		{
			if (m_caretTimer != null)
			{
				m_caretTimer.Stop();
			}
			m_caretVisible = false;
		}

		private void OnCaretTick(object sender, System.EventArgs eventArgs)
		{
			m_caretVisible = !m_caretVisible;
			if (m_textEditCanvas != null)
			{
				m_textEditCanvas.InvalidateSurface();
			}
		}

		public TextEditSession(MainView main, ToolState toolState, AbsoluteLayout host)
		{
			m_main = main;
			m_toolState = toolState;
			m_host = host;
			m_textEditActive = false;
			m_textEditorKeyHooked = false;
			m_caretVisible = false;
		}

		public void PlaceText(CanvasView canvas, int x, int y, float deviceX, float deviceY)
		{
			Document document = canvas.CurrentDocument();
			if (document == null)
			{
				return;
			}
			if (m_textEditActive && m_textEditLayer != null && ReferenceEquals(m_textEditCanvas, canvas))
			{
				int caretIndex = TextRasterizer.CaretIndexAtPoint(m_textEditLayer, x, y);
				if (m_textEditor != null)
				{
					m_textEditor.CursorPosition = caretIndex;
					m_textEditor.SelectionLength = 0;
					m_textEditor.Focus();
				}
				m_caretVisible = true;
				canvas.InvalidateSurface();
				return;
			}
			Commit();
			Layer active = document.ActiveLayer();
			bool editExisting = active != null && active.IsText();
			if (editExisting)
			{
				Begin(canvas, active, false);
				return;
			}
			Layer layer = document.AddLayer("Text");
			if (layer == null)
			{
				return;
			}
			layer.SetTextPosition(x, y);
			ApplyToolStateToLayer(layer);
			m_main.RefreshLayersPanel();
			Begin(canvas, layer, true);
		}

		public void BeginForLayer(Bitmute.Imaging.Layer layer)
		{
			CanvasView canvas = m_main.ActiveCanvas();
			if (canvas == null || layer == null)
			{
				return;
			}
			Document document = canvas.CurrentDocument();
			if (document == null)
			{
				return;
			}
			int index = document.Layers().IndexOf(layer);
			if (index < 0)
			{
				return;
			}
			Commit();
			document.SetActiveLayerIndex(index);
			m_main.RefreshLayersPanel();
			if (m_toolState.Tool() != eTool.Text)
			{
				m_main.SelectTool(eTool.Text);
			}
			Begin(canvas, layer, false);
		}

		public void Begin(CanvasView canvas, Layer layer, bool isNew)
		{
			m_textEditCanvas = canvas;
			m_textEditLayer = layer;
			m_textEditActive = true;
			m_textPreEditWasNew = isNew;
			m_textPreEditString = layer.Text();
			m_textPreEditSize = layer.TextSize();
			m_textPreEditFont = layer.TextFontFamily();
			m_textPreEditBold = layer.TextBold();
			m_textPreEditItalic = layer.TextItalic();
			m_textPreEditColor = layer.TextColor();
			m_textPreEditAlign = layer.TextAlign();
			m_textPreEditAntiAlias = layer.TextAntiAlias();
			m_textPreEditLeadingAuto = layer.TextLeadingAuto();
			m_textPreEditLeading = layer.TextLeading();
			m_textPreEditTracking = layer.TextTracking();
			m_textPreEditHorizontalScale = layer.TextHorizontalScale();
			m_textPreEditVerticalScale = layer.TextVerticalScale();
			m_textPreEditBaselineShift = layer.TextBaselineShift();
			m_textPreEditFauxBold = layer.TextFauxBold();
			m_textPreEditFauxItalic = layer.TextFauxItalic();
			m_textPreEditKerningAuto = layer.TextKerningAuto();

			LoadTextStyleFromLayer(layer);

			Document document = canvas.CurrentDocument();
			if (document != null)
			{
				document.BeginStroke();
			}

			EnsureTextEditor();
			m_textEditor.Text = layer.Text();
			m_textEditor.CursorPosition = layer.Text().Length;
			m_textEditor.SelectionLength = 0;
			PositionTextEditor();
			m_textEditor.ZIndex = 0;
			if (!m_host.Contains(m_textEditor))
			{
				m_host.Add(m_textEditor);
			}
			m_textEditor.Focus();

			layer.RenderText();
			canvas.MarkComposeDirty();
			canvas.InvalidateSurface();
			StartCaretBlink();
		}

		public void Commit()
		{
			if (!m_textEditActive)
			{
				return;
			}
			m_textEditActive = false;
			StopCaretBlink();
			Layer layer = m_textEditLayer;
			CanvasView canvas = m_textEditCanvas;
			if (m_textEditor != null && m_host.Contains(m_textEditor))
			{
				m_host.Remove(m_textEditor);
			}
			if (layer != null && canvas != null)
			{
				Document document = canvas.CurrentDocument();
				ApplyToolStateToLayer(layer);
				layer.RenderText();
				if (layer.Text().Length == 0 && m_textPreEditWasNew)
				{
					if (document != null)
					{
						document.EndStroke();
						int index = document.Layers().IndexOf(layer);
						if (index >= 0)
						{
							document.DeleteLayer(index);
						}
					}
				}
				else
				{
					if (document != null)
					{
						document.EndStroke();
					}
					string name = layer.Text();
					name = name.Replace('\n', ' ');
					if (name.Length > 18)
					{
						name = name.Substring(0, 18);
					}
					if (name.Length == 0)
					{
						name = "Text";
					}
					layer.SetName(name);
				}
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
				m_main.RefreshLayersPanel();
			}
			m_textEditLayer = null;
			m_textEditCanvas = null;
		}

		public void Cancel()
		{
			if (!m_textEditActive)
			{
				return;
			}
			m_textEditActive = false;
			StopCaretBlink();
			Layer layer = m_textEditLayer;
			CanvasView canvas = m_textEditCanvas;
			if (m_textEditor != null && m_host.Contains(m_textEditor))
			{
				m_host.Remove(m_textEditor);
			}
			if (layer != null && canvas != null)
			{
				Document document = canvas.CurrentDocument();
				layer.SetTextString(m_textPreEditString);
				layer.SetTextStyle(m_textPreEditSize, m_textPreEditFont, m_textPreEditBold, m_textPreEditItalic, m_textPreEditColor, m_textPreEditAlign, m_textPreEditAntiAlias);
				layer.SetTextCharacter(m_textPreEditLeadingAuto, m_textPreEditLeading, m_textPreEditTracking, m_textPreEditHorizontalScale, m_textPreEditVerticalScale, m_textPreEditBaselineShift, m_textPreEditFauxBold, m_textPreEditFauxItalic, m_textPreEditKerningAuto);
				layer.RenderText();
				if (document != null)
				{
					document.EndStroke();
					if (m_textPreEditWasNew)
					{
						int index = document.Layers().IndexOf(layer);
						if (index >= 0)
						{
							document.DeleteLayer(index);
						}
					}
				}
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
				m_main.RefreshLayersPanel();
			}
			m_textEditLayer = null;
			m_textEditCanvas = null;
		}

		public void Rasterize()
		{
			Commit();
			Document document = m_main.ActiveDocument();
			if (document == null)
			{
				return;
			}
			Layer layer = document.ActiveLayer();
			if (layer == null || !layer.IsText())
			{
				m_main.SetStatusMessage("Active layer is not a text layer");
				return;
			}
			layer.RenderText();
			layer.RasterizeText();
			CanvasView canvas = m_main.ActiveCanvas();
			if (canvas != null)
			{
				canvas.MarkComposeDirty();
				canvas.InvalidateSurface();
			}
			m_main.RefreshLayersPanel();
		}

		public void RefreshStyle()
		{
			if (!m_textEditActive || m_textEditLayer == null || m_textEditCanvas == null)
			{
				return;
			}
			ApplyToolStateToLayer(m_textEditLayer);
			m_textEditLayer.RenderText();
			m_textEditCanvas.MarkComposeDirty();
			m_textEditCanvas.InvalidateSurface();
		}

		public bool IsActive()
		{
			return m_textEditActive;
		}

		public CanvasView EditCanvas()
		{
			return m_textEditCanvas;
		}

		public Bitmute.Imaging.Layer EditLayer()
		{
			return m_textEditLayer;
		}

		public int CaretIndex()
		{
			if (m_textEditor == null)
			{
				return 0;
			}
			return m_textEditor.CursorPosition;
		}

		public int SelectionStart()
		{
			if (m_textEditor == null)
			{
				return 0;
			}
			return m_textEditor.CursorPosition;
		}

		public int SelectionLength()
		{
			if (m_textEditor == null)
			{
				return 0;
			}
			return m_textEditor.SelectionLength;
		}

		public bool CaretVisible()
		{
			return m_caretVisible;
		}
	}
}
