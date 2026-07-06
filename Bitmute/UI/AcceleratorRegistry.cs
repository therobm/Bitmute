using SkiaSharp;
using Bitmute.Tools;
using System;
using Windows.System;
using Bitmute.UI.Operations;
using System.Collections.Generic;

namespace Bitmute.UI
{
	public class Accelerator
	{
		public VirtualKey m_key;
		public VirtualKeyModifiers m_modifiers;
		public Operation m_operation;
		public Accelerator(Operation operation, VirtualKey key, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None)
		{
			m_operation = operation;
			m_key = key;
			m_modifiers = modifiers;
		}
		public void Trigger(VirtualKeyModifiers additionalModifiers)
		{
			m_operation.Trigger(additionalModifiers);
		}
		public string GetText()
		{
			string text = "";
			if ((m_modifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control)
			{
				text += "Ctrl+";
			}
			if ((m_modifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu)
			{
				text += "Alt+";
			}
			if ((m_modifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift)
			{
				text += "Shift+";
			}
			if ((m_modifiers & VirtualKeyModifiers.Windows) == VirtualKeyModifiers.Windows)
			{
				text += "Win+";
			}
			text += KeyName(m_key);
			return text;
		}

		public string GetModifierText()
		{
			return " (" + GetText() + ")";
		}

		private static string KeyName(VirtualKey key)
		{
			if (key >= VirtualKey.A && key <= VirtualKey.Z)
			{
				return ((char)key).ToString();
			}
			if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
			{
				return ((char)key).ToString();
			}
			if (key == VirtualKey.Add || key == (VirtualKey)187)
			{
				return "+";
			}
			if (key == VirtualKey.Subtract || key == (VirtualKey)189)
			{
				return "-";
			}
			if (key == VirtualKey.Delete)
			{
				return "Del";
			}
			if (key == VirtualKey.Enter)
			{
				return "Enter";
			}
			if (key == VirtualKey.Escape)
			{
				return "Esc";
			}
			return key.ToString();
		}
	}

	public class AcceleratorRegistry
	{
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


		private MainView m_main;
		private ToolState m_toolState;
		private Microsoft.UI.Xaml.UIElement m_hookedElement;
		private OperationRegistry m_operations;
		Dictionary<eOperation, Accelerator> m_accelerators = new Dictionary<eOperation, Accelerator>();
		private Dictionary<uint, Accelerator> m_byChord = new Dictionary<uint, Accelerator>();

		public AcceleratorRegistry(MainView main, ToolState toolState, OperationRegistry operations)
		{
			m_main = main;
			m_toolState = toolState;
			m_operations = operations;
			SetupAccelerators();
		}

		private void SetupAccelerators()
		{
			RegisterAccelerator(eOperation.ToggleRulers, new Accelerator(m_operations.Get(eOperation.ToggleRulers), VirtualKey.R, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.MergeVisibleLayers, new Accelerator(m_operations.Get(eOperation.MergeVisibleLayers), VirtualKey.E, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.NewDocument, new Accelerator(m_operations.Get(eOperation.NewDocument), VirtualKey.N, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Open, new Accelerator(m_operations.Get(eOperation.Open), VirtualKey.O, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Save, new Accelerator(m_operations.Get(eOperation.Save), VirtualKey.S, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.SaveAs, new Accelerator(m_operations.Get(eOperation.SaveAs), VirtualKey.S, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.ExportAs, new Accelerator(m_operations.Get(eOperation.ExportAs), VirtualKey.S, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.Undo, new Accelerator(m_operations.Get(eOperation.Undo), VirtualKey.Z, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Redo, new Accelerator(m_operations.Get(eOperation.Redo), VirtualKey.Y, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.UndoStep, new Accelerator(m_operations.Get(eOperation.UndoStep), VirtualKey.Z, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.RedoStep, new Accelerator(m_operations.Get(eOperation.RedoStep), VirtualKey.Z, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu));
			RegisterAccelerator(eOperation.SelectAll, new Accelerator(m_operations.Get(eOperation.SelectAll), VirtualKey.A, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Deselect, new Accelerator(m_operations.Get(eOperation.Deselect), VirtualKey.D, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Cut, new Accelerator(m_operations.Get(eOperation.Cut), VirtualKey.X, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Copy, new Accelerator(m_operations.Get(eOperation.Copy), VirtualKey.C, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.CopyMerged, new Accelerator(m_operations.Get(eOperation.CopyMerged), VirtualKey.C, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.Paste, new Accelerator(m_operations.Get(eOperation.Paste), VirtualKey.V, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.PasteInto, new Accelerator(m_operations.Get(eOperation.PasteInto), VirtualKey.V, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.FitOnScreen, new Accelerator(m_operations.Get(eOperation.FitOnScreen), VirtualKey.Number0, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.DuplicateLayer, new Accelerator(m_operations.Get(eOperation.DuplicateLayer), VirtualKey.J, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.MergeDown, new Accelerator(m_operations.Get(eOperation.MergeDown), VirtualKey.E, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.ImageSize, new Accelerator(m_operations.Get(eOperation.ImageSize), VirtualKey.I, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu));
			RegisterAccelerator(eOperation.InvertColors, new Accelerator(m_operations.Get(eOperation.InvertColors), VirtualKey.I, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.InvertSelection, new Accelerator(m_operations.Get(eOperation.InvertSelection), VirtualKey.I, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.FreeTransform, new Accelerator(m_operations.Get(eOperation.FreeTransform), VirtualKey.T, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.LastFilter, new Accelerator(m_operations.Get(eOperation.LastFilter), VirtualKey.F, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.SwapColors, new Accelerator(m_operations.Get(eOperation.SwapColors), VirtualKey.X, VirtualKeyModifiers.None));
		}

		public void RegisterAccelerator(eOperation operation, Accelerator accelerator)
		{
			m_accelerators[operation] = accelerator;
			m_byChord[ChordKey(accelerator.m_key, accelerator.m_modifiers)] = m_accelerators[operation];
			m_operations.AssignAccelerator(operation, m_accelerators[operation]);
		}

		private void HookOperationAccelerators(Microsoft.UI.Xaml.UIElement element)
		{
			foreach (Accelerator accelerator in m_accelerators.Values)
			{
				AddOperationAccelerator(element, accelerator.m_key, accelerator.m_modifiers);
			}
		}

		private void AddOperationAccelerator(Microsoft.UI.Xaml.UIElement element, VirtualKey key, VirtualKeyModifiers modifiers)
		{
			Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
			accelerator.Key = key;
			accelerator.Modifiers = modifiers;
			accelerator.Invoked += OnOperationAccelerator;
			element.KeyboardAccelerators.Add(accelerator);
		}

		private void OnOperationAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			Accelerator accelerator = FindByChord(sender.Key, sender.Modifiers);
			if (accelerator == null)
			{
				return;
			}
			accelerator.Trigger(sender.Modifiers);
			args.Handled = true;
		}

		private Accelerator FindByChord(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			uint chord = ChordKey(key, modifiers);
			if (m_byChord.ContainsKey(chord))
			{
				return m_byChord[chord];
			}
			return null;
		}

		private static uint ChordKey(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			return ((uint)key << 8) | (uint)modifiers;
		}

		public void RegisterViewHandler(Microsoft.UI.Xaml.UIElement element)
		{
			m_hookedElement = element;
			HookOperationAccelerators(element);
			AddAccelerator(element, Windows.System.VirtualKey.Add, OnAcceleratorZoomIn);
			AddAccelerator(element, Windows.System.VirtualKey.Subtract, OnAcceleratorZoomOut);
			AddAccelerator(element, (Windows.System.VirtualKey)187, OnAcceleratorZoomIn);
			AddAccelerator(element, (Windows.System.VirtualKey)189, OnAcceleratorZoomOut);
			AddBareAccelerator(element, Windows.System.VirtualKey.Enter, OnAcceleratorCommitArmed);
			AddBareAccelerator(element, Windows.System.VirtualKey.Escape, OnAcceleratorCancelArmed);
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
	}
}
