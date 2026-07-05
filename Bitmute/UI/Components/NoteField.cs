using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Bitmute.UI;

namespace Bitmute.UI.Components
{
	public class NoteField : ContentView
	{
		public NoteField(string text)
		{
			Label note = new Label();
			note.Text = text;
			note.FontSize = UiConstants.ComponentFontSize;
			note.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			note.LineBreakMode = LineBreakMode.WordWrap;
			Content = note;
		}
	}
}
