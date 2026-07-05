using SkiaSharp;
using Bitmute.Tools;

namespace Bitmute.UI
{
	public class AcceleratorRegistry
	{
		private MainView m_main;
		private ToolState m_toolState;
		private Microsoft.UI.Xaml.UIElement m_hookedElement;

		private static void AddAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private static void AddBareAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.None;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private static void AddAltAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Menu;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private static void AddShiftAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Shift;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private static void AddCtrlShiftAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Shift;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private static void AddCtrlAltAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Menu;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private static void AddCtrlAltShiftAccelerator(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key, Windows.Foundation.TypedEventHandler<Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs> handler)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Menu | Windows.System.VirtualKeyModifiers.Shift;
			accelerator.Invoked += handler;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private bool TextInputFocused()
		{
			if (m_hookedElement == null)
			{
				return false;
			}
			object focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(m_hookedElement.XamlRoot);
			if (focused is Microsoft.UI.Xaml.Controls.TextBox)
			{
				return true;
			}
			if (focused is Microsoft.UI.Xaml.Controls.RichEditBox)
			{
				return true;
			}
			if (focused is Microsoft.UI.Xaml.Controls.PasswordBox)
			{
				return true;
			}
			if (focused is Microsoft.UI.Xaml.Controls.ComboBox)
			{
				return true;
			}
			return false;
		}

		private bool ToolKeyBlocked()
		{
			if (m_main.IsTextEditActive())
			{
				return true;
			}
			if (m_main.HasOpenModal())
			{
				return true;
			}
			return TextInputFocused();
		}

		private void OnAcceleratorToolKey(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ToolKeyBlocked())
			{
				return;
			}
			bool cycle = (sender.Modifiers & Windows.System.VirtualKeyModifiers.Shift) == Windows.System.VirtualKeyModifiers.Shift;
			Windows.System.VirtualKey key = sender.Key;
			if (key == Windows.System.VirtualKey.S)
			{
				if (cycle && m_toolState.Tool() == eTool.Clone)
				{
					m_main.SelectToolKey(eTool.Heal, false);
				}
				else
				{
					m_main.SelectToolKey(eTool.Clone, false);
				}
				args.Handled = true;
				return;
			}
			if (key == Windows.System.VirtualKey.M)
			{
				m_main.SelectToolKey(eTool.Select, cycle);
			}
			else if (key == Windows.System.VirtualKey.V)
			{
				m_main.SelectToolKey(eTool.Move, cycle);
			}
			else if (key == Windows.System.VirtualKey.L)
			{
				m_main.SelectToolKey(eTool.Lasso, cycle);
			}
			else if (key == Windows.System.VirtualKey.W)
			{
				m_main.SelectToolKey(eTool.MagicWand, cycle);
			}
			else if (key == Windows.System.VirtualKey.C)
			{
				m_main.SelectToolKey(eTool.Crop, cycle);
			}
			else if (key == Windows.System.VirtualKey.B)
			{
				m_main.SelectToolKey(eTool.Brush, cycle);
			}
			else if (key == Windows.System.VirtualKey.E)
			{
				m_main.SelectToolKey(eTool.Eraser, cycle);
			}
			else if (key == Windows.System.VirtualKey.G)
			{
				m_main.SelectToolKey(eTool.Fill, cycle);
			}
			else if (key == Windows.System.VirtualKey.O)
			{
				m_main.SelectToolKey(eTool.DodgeBurn, cycle);
			}
			else if (key == Windows.System.VirtualKey.R)
			{
				m_main.SelectToolKey(eTool.Blur, cycle);
			}
			else if (key == Windows.System.VirtualKey.T)
			{
				m_main.SelectToolKey(eTool.Text, cycle);
			}
			else if (key == Windows.System.VirtualKey.U)
			{
				m_main.SelectToolKey(eTool.Line, cycle);
			}
			else if (key == Windows.System.VirtualKey.I)
			{
				m_main.SelectToolKey(eTool.Eyedropper, cycle);
			}
			else if (key == Windows.System.VirtualKey.H)
			{
				m_main.SelectToolKey(eTool.Hand, cycle);
			}
			else if (key == Windows.System.VirtualKey.Z)
			{
				m_main.SelectToolKey(eTool.Zoom, cycle);
			}
			args.Handled = true;
		}

		private void OnAcceleratorNew(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.ShowNewDocumentDialog();
			args.Handled = true;
		}

		private void OnAcceleratorOpen(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.OpenImageFlow();
			args.Handled = true;
		}

		private void OnAcceleratorSave(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.SaveImageFlow();
			args.Handled = true;
		}

		private void OnAcceleratorUndo(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoUndo();
			args.Handled = true;
		}

		private void OnAcceleratorRedo(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoRedo();
			args.Handled = true;
		}

		private void OnAcceleratorSelectAll(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoSelectAll();
			args.Handled = true;
		}

		private void OnAcceleratorDeselect(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoDeselect();
			args.Handled = true;
		}

		private void OnAcceleratorCopy(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoCopy();
			args.Handled = true;
		}

		private void OnAcceleratorPaste(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoPaste();
			args.Handled = true;
		}

		private void OnAcceleratorCopyMerged(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoCopyMerged();
			args.Handled = true;
		}

		private void OnAcceleratorPasteInto(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoPasteInto();
			args.Handled = true;
		}

		private void OnAcceleratorCut(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoCut();
			args.Handled = true;
		}

		private void OnAcceleratorFit(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.DoFit();
			args.Handled = true;
		}

		private void OnAcceleratorZoomIn(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.DoZoomIn();
			args.Handled = true;
		}

		private void OnAcceleratorZoomOut(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.DoZoomOut();
			args.Handled = true;
		}

		private void OnAcceleratorSaveAs(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.SaveAsFlow();
			args.Handled = true;
		}

		private void OnAcceleratorExport(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.OpenExportDialog();
			args.Handled = true;
		}

		private void OnAcceleratorImageSize(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.OpenSizeDialog(false);
			args.Handled = true;
		}

		private void OnAcceleratorInvertSelection(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoInvertSelection();
			args.Handled = true;
		}

		private void OnAcceleratorLastFilter(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.ApplyLastFilter();
			args.Handled = true;
		}

		private void OnAcceleratorInvertColors(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoInvert();
			args.Handled = true;
		}

		private void OnAcceleratorDuplicateLayer(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DuplicateActiveLayer();
			args.Handled = true;
		}

		private void OnAcceleratorMergeSelected(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.MergeSelectedLayers();
			args.Handled = true;
		}

		private void OnAcceleratorMergeVisible(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoMergeVisible();
			args.Handled = true;
		}

		private void OnAcceleratorRulers(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			m_main.ToggleRulers();
			args.Handled = true;
		}

		private void OnAcceleratorTransform(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.BeginTransform(0);
			args.Handled = true;
		}

		private void OnAcceleratorCommitArmed(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			bool committed = m_main.CommitArmedOperation();
			if (committed)
			{
				args.Handled = true;
			}
		}

		private void OnAcceleratorCancelArmed(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			if (m_main.HasOpenModal())
			{
				m_main.CloseModal();
				args.Handled = true;
				return;
			}
			bool cancelled = m_main.CancelArmedOperation();
			if (cancelled)
			{
				args.Handled = true;
			}
		}

		private void OnAcceleratorSwapColors(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.SwapToolColors();
			args.Handled = true;
		}

		private void OnAcceleratorDelete(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			m_main.DoClearSelection();
			args.Handled = true;
		}

		private void OnAcceleratorDeleteForeground(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			SKColor foreground = m_toolState.Foreground();
			m_main.FillSelectionWith(new SKColor(foreground.Red, foreground.Green, foreground.Blue, 255), true);
			args.Handled = true;
		}

		private void OnAcceleratorDeleteBackground(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			if (m_main.IsTextEditActive())
			{
				return;
			}
			SKColor background = m_toolState.Background();
			m_main.FillSelectionWith(new SKColor(background.Red, background.Green, background.Blue, 255), true);
			args.Handled = true;
		}

		public AcceleratorRegistry(MainView main, ToolState toolState)
		{
			m_main = main;
			m_toolState = toolState;
		}

		public void Hook(Microsoft.UI.Xaml.UIElement element)
		{
			m_hookedElement = element;
			AddAccelerator(element, Windows.System.VirtualKey.N, OnAcceleratorNew);
			AddAccelerator(element, Windows.System.VirtualKey.O, OnAcceleratorOpen);
			AddAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorSave);
			AddAccelerator(element, Windows.System.VirtualKey.Z, OnAcceleratorUndo);
			AddAccelerator(element, Windows.System.VirtualKey.Y, OnAcceleratorRedo);
			AddAccelerator(element, Windows.System.VirtualKey.A, OnAcceleratorSelectAll);
			AddAccelerator(element, Windows.System.VirtualKey.D, OnAcceleratorDeselect);
			AddAccelerator(element, Windows.System.VirtualKey.C, OnAcceleratorCopy);
			AddAccelerator(element, Windows.System.VirtualKey.V, OnAcceleratorPaste);
			AddCtrlShiftAccelerator(element, Windows.System.VirtualKey.V, OnAcceleratorPasteInto);
			AddCtrlShiftAccelerator(element, Windows.System.VirtualKey.C, OnAcceleratorCopyMerged);
			AddAccelerator(element, Windows.System.VirtualKey.X, OnAcceleratorCut);
			AddAccelerator(element, Windows.System.VirtualKey.Number0, OnAcceleratorFit);
			AddAccelerator(element, Windows.System.VirtualKey.Add, OnAcceleratorZoomIn);
			AddAccelerator(element, Windows.System.VirtualKey.Subtract, OnAcceleratorZoomOut);
			AddAccelerator(element, (Windows.System.VirtualKey)187, OnAcceleratorZoomIn);
			AddAccelerator(element, (Windows.System.VirtualKey)189, OnAcceleratorZoomOut);
			AddCtrlShiftAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorSaveAs);
			AddAccelerator(element, Windows.System.VirtualKey.J, OnAcceleratorDuplicateLayer);
			AddAccelerator(element, Windows.System.VirtualKey.E, OnAcceleratorMergeSelected);
			AddCtrlShiftAccelerator(element, Windows.System.VirtualKey.E, OnAcceleratorMergeVisible);
			AddCtrlAltShiftAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorExport);
			AddCtrlAltAccelerator(element, Windows.System.VirtualKey.I, OnAcceleratorImageSize);
			AddAccelerator(element, Windows.System.VirtualKey.I, OnAcceleratorInvertColors);
			AddCtrlShiftAccelerator(element, Windows.System.VirtualKey.I, OnAcceleratorInvertSelection);
			AddAccelerator(element, Windows.System.VirtualKey.R, OnAcceleratorRulers);
			AddAccelerator(element, Windows.System.VirtualKey.T, OnAcceleratorTransform);
			AddAccelerator(element, Windows.System.VirtualKey.F, OnAcceleratorLastFilter);
			AddBareAccelerator(element, Windows.System.VirtualKey.Enter, OnAcceleratorCommitArmed);
			AddBareAccelerator(element, Windows.System.VirtualKey.Escape, OnAcceleratorCancelArmed);
			AddBareAccelerator(element, Windows.System.VirtualKey.X, OnAcceleratorSwapColors);
			AddBareAccelerator(element, Windows.System.VirtualKey.Delete, OnAcceleratorDelete);
			AddAccelerator(element, Windows.System.VirtualKey.Delete, OnAcceleratorDeleteBackground);
			AddAltAccelerator(element, Windows.System.VirtualKey.Delete, OnAcceleratorDeleteForeground);
			AddBareAccelerator(element, Windows.System.VirtualKey.M, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.V, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.L, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.W, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.C, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.B, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.E, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.G, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.O, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.R, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.T, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.U, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.I, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.H, OnAcceleratorToolKey);
			AddBareAccelerator(element, Windows.System.VirtualKey.Z, OnAcceleratorToolKey);
			AddShiftAccelerator(element, Windows.System.VirtualKey.M, OnAcceleratorToolKey);
			AddShiftAccelerator(element, Windows.System.VirtualKey.L, OnAcceleratorToolKey);
			AddShiftAccelerator(element, Windows.System.VirtualKey.B, OnAcceleratorToolKey);
			AddShiftAccelerator(element, Windows.System.VirtualKey.S, OnAcceleratorToolKey);
			AddShiftAccelerator(element, Windows.System.VirtualKey.G, OnAcceleratorToolKey);
			AddShiftAccelerator(element, Windows.System.VirtualKey.O, OnAcceleratorToolKey);
			AddShiftAccelerator(element, Windows.System.VirtualKey.R, OnAcceleratorToolKey);
			AddShiftAccelerator(element, Windows.System.VirtualKey.U, OnAcceleratorToolKey);
			element.KeyboardAcceleratorPlacementMode = Microsoft.UI.Xaml.Input.KeyboardAcceleratorPlacementMode.Hidden;
		}
	}
}
