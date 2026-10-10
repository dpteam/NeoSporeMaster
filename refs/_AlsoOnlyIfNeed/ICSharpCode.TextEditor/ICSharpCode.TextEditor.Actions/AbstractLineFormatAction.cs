using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public abstract class AbstractLineFormatAction : AbstractEditAction
{
	protected TextArea textArea;

	protected abstract void Convert(IDocument document, int startLine, int endLine);

	public override void Execute(TextArea textArea)
	{
		this.textArea = textArea;
		textArea.BeginUpdate();
		textArea.Document.UndoStack.StartUndoGroup();
		if (textArea.SelectionManager.HasSomethingSelected)
		{
			foreach (ISelection item in textArea.SelectionManager.SelectionCollection)
			{
				Convert(textArea.Document, item.StartPosition.Y, item.EndPosition.Y);
			}
		}
		else
		{
			Convert(textArea.Document, 0, textArea.Document.TotalNumberOfLines - 1);
		}
		textArea.Document.UndoStack.EndUndoGroup();
		textArea.Caret.ValidateCaretPos();
		textArea.EndUpdate();
		textArea.Refresh();
	}
}
