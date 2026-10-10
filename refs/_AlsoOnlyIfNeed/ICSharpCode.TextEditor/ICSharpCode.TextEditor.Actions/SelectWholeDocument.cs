namespace ICSharpCode.TextEditor.Actions;

public class SelectWholeDocument : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		textArea.AutoClearSelection = false;
		TextLocation textLocation = new TextLocation(0, 0);
		TextLocation textLocation2 = textArea.Document.OffsetToPosition(textArea.Document.TextLength);
		if (!textArea.SelectionManager.HasSomethingSelected || !(textArea.SelectionManager.SelectionCollection[0].StartPosition == textLocation) || !(textArea.SelectionManager.SelectionCollection[0].EndPosition == textLocation2))
		{
			textArea.Caret.Position = textArea.SelectionManager.NextValidPosition(textLocation2.Y);
			textArea.SelectionManager.ExtendSelection(textLocation, textLocation2);
			textArea.SetDesiredColumn();
		}
	}
}
