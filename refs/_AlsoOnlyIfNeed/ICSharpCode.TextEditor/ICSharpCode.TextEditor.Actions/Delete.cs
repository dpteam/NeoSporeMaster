using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class Delete : AbstractEditAction
{
	internal static void DeleteSelection(TextArea textArea)
	{
		if (!textArea.SelectionManager.SelectionIsReadonly)
		{
			textArea.BeginUpdate();
			textArea.Caret.Position = textArea.SelectionManager.SelectionCollection[0].StartPosition;
			textArea.SelectionManager.RemoveSelectedText();
			textArea.ScrollToCaret();
			textArea.EndUpdate();
		}
	}

	public override void Execute(TextArea textArea)
	{
		if (textArea.SelectionManager.HasSomethingSelected)
		{
			DeleteSelection(textArea);
		}
		else
		{
			if (textArea.IsReadOnly(textArea.Caret.Offset) || textArea.Caret.Offset >= textArea.Document.TextLength)
			{
				return;
			}
			textArea.BeginUpdate();
			int lineNumberForOffset = textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset);
			LineSegment lineSegment = textArea.Document.GetLineSegment(lineNumberForOffset);
			if (lineSegment.Offset + lineSegment.Length == textArea.Caret.Offset)
			{
				if (lineNumberForOffset + 1 < textArea.Document.TotalNumberOfLines)
				{
					LineSegment lineSegment2 = textArea.Document.GetLineSegment(lineNumberForOffset + 1);
					textArea.Document.Remove(textArea.Caret.Offset, lineSegment2.Offset - textArea.Caret.Offset);
					textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.PositionToEnd, new TextLocation(0, lineNumberForOffset)));
				}
			}
			else
			{
				textArea.Document.Remove(textArea.Caret.Offset, 1);
			}
			textArea.UpdateMatchingBracket();
			textArea.EndUpdate();
		}
	}
}
