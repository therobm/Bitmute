using SkiaSharp;
using Bitmute.Tools;
using System;
using Windows.System;
using Bitmute.UI.Operations;
using System.Collections.Generic;

namespace Bitmute.UI
{
	public class Chord
	{
		public VirtualKey m_key;
		public VirtualKeyModifiers m_modifiers;
		public Chord(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			m_key = key;
			m_modifiers = modifiers;
		}
	};

	public class Accelerator
	{
		public List<Chord> m_chords;
		public Operation m_operation;
		public Accelerator(Operation operation, Chord chord)
		{
			m_operation = operation;
			m_chords = new List<Chord>() { chord };
		}

		public Accelerator(Operation operation, List<Chord> chords)
		{
			m_operation = operation;
			m_chords = chords;
		}

		public bool Trigger(Chord chord)
		{
			return m_operation.Trigger(chord);
		}
		public string GetText()
		{
			string text = "";
			if (m_chords.Count <= 0)
				return text;

			Chord primaryChord = m_chords[0];
			if ((primaryChord.m_modifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control)
			{
				text += "Ctrl+";
			}
			if ((primaryChord.m_modifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu)
			{
				text += "Alt+";
			}
			if ((primaryChord.m_modifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift)
			{
				text += "Shift+";
			}
			if ((primaryChord.m_modifiers & VirtualKeyModifiers.Windows) == VirtualKeyModifiers.Windows)
			{
				text += "Win+";
			}
			text += KeyName(primaryChord.m_key);
			return text;
		}

		public string GetModifierText()
		{
			return " (" + GetText() + ")";
		}

		public static string KeyName(VirtualKey key)
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
		public bool ValidForChord(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			//todo check against our registered chords
			return false;
		}
	}

	public class AcceleratorRegistry
	{
		private MainView m_main;
		private Microsoft.UI.Xaml.UIElement m_hookedElement;
		private OperationRegistry m_operations;
		Dictionary<eOperation, Accelerator> m_accelerators = new Dictionary<eOperation, Accelerator>();
		private Dictionary<uint, Accelerator> m_byChord = new Dictionary<uint, Accelerator>();

		public AcceleratorRegistry(MainView main, OperationRegistry operations)
		{
			m_main = main;
			m_operations = operations;
			SetupAccelerators();
		}

		private void SetupAccelerators()
		{
			// Every tool key routes to the single ToolChange operation, which reads the fired Chord's key
			// to pick the tool and its Shift modifier to cycle within a multi-member group.
			List<Chord> toolChords = new List<Chord>()
			{
				new Chord(VirtualKey.M, VirtualKeyModifiers.None), new Chord(VirtualKey.M, VirtualKeyModifiers.Shift),
				new Chord(VirtualKey.V, VirtualKeyModifiers.None),
				new Chord(VirtualKey.L, VirtualKeyModifiers.None), new Chord(VirtualKey.L, VirtualKeyModifiers.Shift),
				new Chord(VirtualKey.W, VirtualKeyModifiers.None),
				new Chord(VirtualKey.C, VirtualKeyModifiers.None),
				new Chord(VirtualKey.B, VirtualKeyModifiers.None), new Chord(VirtualKey.B, VirtualKeyModifiers.Shift),
				new Chord(VirtualKey.S, VirtualKeyModifiers.None),
				new Chord(VirtualKey.E, VirtualKeyModifiers.None),
				new Chord(VirtualKey.G, VirtualKeyModifiers.None), new Chord(VirtualKey.G, VirtualKeyModifiers.Shift),
				new Chord(VirtualKey.O, VirtualKeyModifiers.None), new Chord(VirtualKey.O, VirtualKeyModifiers.Shift),
				new Chord(VirtualKey.R, VirtualKeyModifiers.None), new Chord(VirtualKey.R, VirtualKeyModifiers.Shift),
				new Chord(VirtualKey.T, VirtualKeyModifiers.None),
				new Chord(VirtualKey.U, VirtualKeyModifiers.None), new Chord(VirtualKey.U, VirtualKeyModifiers.Shift),
				new Chord(VirtualKey.I, VirtualKeyModifiers.None),
				new Chord(VirtualKey.H, VirtualKeyModifiers.None),
				new Chord(VirtualKey.Z, VirtualKeyModifiers.None),
				new Chord(VirtualKey.P, VirtualKeyModifiers.None),
				new Chord(VirtualKey.A, VirtualKeyModifiers.None),
			};
			RegisterAccelerator(eOperation.ToolChange, toolChords);

			RegisterAccelerator(eOperation.ToggleRulers, new Chord(VirtualKey.R, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.MergeVisibleLayers, new Chord(VirtualKey.E, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.NewDocument, new Chord(VirtualKey.N, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Open, new Chord(VirtualKey.O, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Save, new Chord(VirtualKey.S, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.SaveAs, new Chord(VirtualKey.S, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.ExportAs, new Chord(VirtualKey.S, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.Undo, new Chord(VirtualKey.Z, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Redo, new Chord(VirtualKey.Y, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.UndoStep, new Chord(VirtualKey.Z, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu));
			RegisterAccelerator(eOperation.RedoStep, new Chord(VirtualKey.Z, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.SelectAll, new Chord(VirtualKey.A, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Deselect, new Chord(VirtualKey.D, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Cut, new Chord(VirtualKey.X, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.Copy, new Chord(VirtualKey.C, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.CopyMerged, new Chord(VirtualKey.C, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.Paste, new Chord(VirtualKey.V, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.PasteInto, new Chord(VirtualKey.V, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.FitOnScreen, new Chord(VirtualKey.Number0, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.DuplicateLayer, new Chord(VirtualKey.J, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.MergeDown, new Chord(VirtualKey.E, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.ImageSize, new Chord(VirtualKey.I, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu));
			RegisterAccelerator(eOperation.InvertColors, new Chord(VirtualKey.I, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.InvertSelection, new Chord(VirtualKey.I, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
			RegisterAccelerator(eOperation.FreeTransform, new Chord(VirtualKey.T, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.LastFilter, new Chord(VirtualKey.F, VirtualKeyModifiers.Control));
			RegisterAccelerator(eOperation.SwapColors, new Chord(VirtualKey.X, VirtualKeyModifiers.None));
			RegisterAccelerator(eOperation.ZoomIn, new List<Chord>()
			{
				new Chord(VirtualKey.Add, VirtualKeyModifiers.Control),
				new Chord((VirtualKey)187, VirtualKeyModifiers.Control),
			});
			RegisterAccelerator(eOperation.ZoomOut, new List<Chord>()
			{
				new Chord(VirtualKey.Subtract, VirtualKeyModifiers.Control),
				new Chord((VirtualKey)189, VirtualKeyModifiers.Control),
			});
			RegisterAccelerator(eOperation.Delete, new List<Chord>()
			{
				new Chord(VirtualKey.Delete, VirtualKeyModifiers.None),
				new Chord(VirtualKey.Delete, VirtualKeyModifiers.Control),
				new Chord(VirtualKey.Delete, VirtualKeyModifiers.Menu),
			});
			RegisterAccelerator(eOperation.CommitArmed, new Chord(VirtualKey.Enter, VirtualKeyModifiers.None));
			RegisterAccelerator(eOperation.CancelArmed, new Chord(VirtualKey.Escape, VirtualKeyModifiers.None));
		}

		public void RegisterAccelerator(eOperation operation, Chord chord)
		{
			RegisterAccelerator(operation, new List<Chord>() { chord } );
		}
		public void RegisterAccelerator(eOperation operation, List<Chord> chords)
		{
			Operation op = m_operations.Get(operation);
			Accelerator accelerator = new Accelerator(op, chords);

		    m_accelerators[operation] = accelerator;
			for(int i = 0; i < accelerator.m_chords.Count; i++)
			{
				m_byChord[ChordKey(accelerator.m_chords[i].m_key, accelerator.m_chords[i].m_modifiers)] = m_accelerators[operation];
			}
			
			m_operations.AssignAccelerator(operation, m_accelerators[operation]);
		}

		private void RegisterViewAccelerators(Microsoft.UI.Xaml.UIElement element)
		{
			foreach (Accelerator accelerator in m_accelerators.Values)
			{
				for (int i = 0; i < accelerator.m_chords.Count; i++)
				{
					Chord chord = accelerator.m_chords[i];
					Microsoft.UI.Xaml.Input.KeyboardAccelerator keyboardAccelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator();
					keyboardAccelerator.Key = chord.m_key;
					keyboardAccelerator.Modifiers = chord.m_modifiers;
					keyboardAccelerator.Invoked += OnOperationAccelerator;
					element.KeyboardAccelerators.Add(keyboardAccelerator);
				}
			}
		}

		private void OnOperationAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			Accelerator accelerator = FindByChord(sender.Key, sender.Modifiers);
			if (accelerator == null)
			{
				return;
			}
			args.Handled = accelerator.Trigger(new Chord(sender.Key, sender.Modifiers));
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
			RegisterViewAccelerators(element);
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

		public bool ToolKeyBlocked()
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

	}
}
