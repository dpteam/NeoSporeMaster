namespace ICSharpCode.TextEditor.Actions;

public class MoveToEnd : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		TextLocation textLocation = textArea.Document.OffsetToPosition(textArea.Document.TextLength);
		if (textArea.Caret.Position != textLocation)
		{
			textArea.Caret.Position = textLocation;
			textArea.SetDesiredColumn();
		}
	}
}
