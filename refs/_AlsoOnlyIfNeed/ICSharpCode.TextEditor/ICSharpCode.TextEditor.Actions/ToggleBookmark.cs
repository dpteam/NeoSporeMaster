namespace ICSharpCode.TextEditor.Actions;

public class ToggleBookmark : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		textArea.Document.BookmarkManager.ToggleMarkAt(textArea.Caret.Position);
		textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, textArea.Caret.Line));
		textArea.Document.CommitUpdate();
	}
}
