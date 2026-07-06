using System;
using System.Collections.Generic;
using System.Text;

namespace Bitmute.UI.Operations
{
	public enum eOperation
	{
		ToggleRulers,
		MergeVisibleLayers,

		NewDocument,
		Open,
		Save,
		SaveAs,
		ExportAs,

		Undo,
		Redo,
		UndoStep,
		RedoStep,

		SelectAll,
		Deselect,
		Cut,
		Copy,
		CopyMerged,
		Paste,
		PasteInto,

		FitOnScreen,

		DuplicateLayer,
		MergeDown,

		ImageSize,
		InvertColors,
		InvertSelection,
		FreeTransform,
		LastFilter,
		SwapColors,
	}
}
