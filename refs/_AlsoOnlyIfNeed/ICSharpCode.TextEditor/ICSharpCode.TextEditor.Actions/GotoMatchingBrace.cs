using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class GotoMatchingBrace : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		Highlight highlight = textArea.FindMatchingBracketHighlight();
		if (highlight == null)
		{
			return;
		}
		TextLocation textLocation = new TextLocation(highlight.CloseBrace.X + 1, highlight.CloseBrace.Y);
		TextLocation position = new TextLocation(highlight.OpenBrace.X + 1, highlight.OpenBrace.Y);
		if (textLocation == textArea.Caret.Position)
		{
			if (textArea.Document.TextEditorProperties.BracketMatchingStyle == BracketMatchingStyle.After)
			{
				textArea.Caret.Position = position;
			}
			else
			{
				textArea.Caret.Position = new TextLocation(position.X - 1, position.Y);
			}
		}
		else if (textArea.Document.TextEditorProperties.BracketMatchingStyle == BracketMatchingStyle.After)
		{
			textArea.Caret.Position = textLocation;
		}
		else
		{
			textArea.Caret.Position = new TextLocation(textLocation.X - 1, textLocation.Y);
		}
		textArea.SetDesiredColumn();
	}
}
