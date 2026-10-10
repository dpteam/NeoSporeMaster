using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ToLowerCase : AbstractSelectionFormatAction
{
	protected override void Convert(IDocument document, int startOffset, int length)
	{
		string text = document.GetText(startOffset, length).ToLower();
		document.Replace(startOffset, length, text);
	}
}
