using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class Backspace : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		if (textArea.SelectionManager.HasSomethingSelected)
		{
			Delete.DeleteSelection(textArea);
		}
		else if (textArea.Caret.Offset > 0 && !textArea.IsReadOnly(textArea.Caret.Offset - 1))
		{
			textArea.BeginUpdate();
			int lineNumberForOffset = textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset);
			int offset = textArea.Document.GetLineSegment(lineNumberForOffset).Offset;
			if (offset == textArea.Caret.Offset)
			{
				LineSegment lineSegment = textArea.Document.GetLineSegment(lineNumberForOffset - 1);
				_ = textArea.Document.TotalNumberOfLines;
				int num = lineSegment.Offset + lineSegment.Length;
				int length = lineSegment.Length;
				textArea.Document.Remove(num, offset - num);
				textArea.Caret.Position = new TextLocation(length, lineNumberForOffset - 1);
				textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new TextLocation(0, lineNumberForOffset - 1)));
			}
			else
			{
				int offset2 = textArea.Caret.Offset - 1;
				textArea.Caret.Position = textArea.Document.OffsetToPosition(offset2);
				textArea.Document.Remove(offset2, 1);
				textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToLineEnd, new TextLocation(textArea.Caret.Offset - textArea.Document.GetLineSegment(lineNumberForOffset).Offset, lineNumberForOffset)));
			}
			textArea.EndUpdate();
		}
	}
}
