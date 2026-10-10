using System;

namespace ICSharpCode.TextEditor.Actions;

public class Return : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		if (textArea.Document.ReadOnly)
		{
			return;
		}
		textArea.BeginUpdate();
		textArea.Document.UndoStack.StartUndoGroup();
		try
		{
			if (!textArea.HandleKeyPress('\n'))
			{
				textArea.InsertString(Environment.NewLine);
				int line = textArea.Caret.Line;
				textArea.Document.FormattingStrategy.FormatLine(textArea, line, textArea.Caret.Offset, '\n');
				textArea.SetDesiredColumn();
				textArea.Document.UpdateQueue.Clear();
				textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new TextLocation(0, line - 1)));
			}
		}
		finally
		{
			textArea.Document.UndoStack.EndUndoGroup();
			textArea.EndUpdate();
		}
	}
}
