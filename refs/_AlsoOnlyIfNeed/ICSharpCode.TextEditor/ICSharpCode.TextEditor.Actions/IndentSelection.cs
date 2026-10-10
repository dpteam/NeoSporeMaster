using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class IndentSelection : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		if (textArea.Document.ReadOnly)
		{
			return;
		}
		textArea.BeginUpdate();
		if (textArea.SelectionManager.HasSomethingSelected)
		{
			foreach (ISelection item in textArea.SelectionManager.SelectionCollection)
			{
				textArea.Document.FormattingStrategy.IndentLines(textArea, item.StartPosition.Y, item.EndPosition.Y);
			}
		}
		else
		{
			textArea.Document.FormattingStrategy.IndentLines(textArea, 0, textArea.Document.TotalNumberOfLines - 1);
		}
		textArea.EndUpdate();
		textArea.Refresh();
	}
}
