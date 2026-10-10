using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public abstract class AbstractSelectionFormatAction : AbstractEditAction
{
	protected TextArea textArea;

	protected abstract void Convert(IDocument document, int offset, int length);

	public override void Execute(TextArea textArea)
	{
		this.textArea = textArea;
		textArea.BeginUpdate();
		if (textArea.SelectionManager.HasSomethingSelected)
		{
			foreach (ISelection item in textArea.SelectionManager.SelectionCollection)
			{
				Convert(textArea.Document, item.Offset, item.Length);
			}
		}
		else
		{
			Convert(textArea.Document, 0, textArea.Document.TextLength);
		}
		textArea.Caret.ValidateCaretPos();
		textArea.EndUpdate();
		textArea.Refresh();
	}
}
