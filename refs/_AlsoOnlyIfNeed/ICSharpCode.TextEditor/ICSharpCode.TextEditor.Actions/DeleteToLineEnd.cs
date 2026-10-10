using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class DeleteToLineEnd : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		int line = textArea.Caret.Line;
		LineSegment lineSegment = textArea.Document.GetLineSegment(line);
		int num = lineSegment.Offset + lineSegment.Length - textArea.Caret.Offset;
		if (num > 0 && !textArea.IsReadOnly(textArea.Caret.Offset, num))
		{
			textArea.Document.Remove(textArea.Caret.Offset, num);
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, new TextLocation(0, line)));
			textArea.Document.CommitUpdate();
		}
	}
}
