using System;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ShiftTab : AbstractEditAction
{
	private void RemoveTabs(IDocument document, ISelection selection, int y1, int y2)
	{
		document.UndoStack.StartUndoGroup();
		for (int num = y2; num >= y1; num--)
		{
			LineSegment lineSegment = document.GetLineSegment(num);
			if ((num != y2 || lineSegment.Offset != selection.EndOffset) && lineSegment.Length > 0 && lineSegment.Length > 0)
			{
				int num2 = 0;
				if (document.GetCharAt(lineSegment.Offset) == '\t')
				{
					num2 = 1;
				}
				else if (document.GetCharAt(lineSegment.Offset) == ' ')
				{
					int num3 = 1;
					int indentationSize = document.TextEditorProperties.IndentationSize;
					for (num3 = 1; num3 < lineSegment.Length && document.GetCharAt(lineSegment.Offset + num3) == ' '; num3++)
					{
					}
					if (num3 >= indentationSize)
					{
						num2 = indentationSize;
					}
					else
					{
						num2 = ((lineSegment.Length <= num3 || document.GetCharAt(lineSegment.Offset + num3) != '\t') ? num3 : (num3 + 1));
					}
				}
				if (num2 > 0)
				{
					document.Remove(lineSegment.Offset, num2);
				}
			}
		}
		document.UndoStack.EndUndoGroup();
	}

	public override void Execute(TextArea textArea)
	{
		if (textArea.SelectionManager.HasSomethingSelected)
		{
			foreach (ISelection item in textArea.SelectionManager.SelectionCollection)
			{
				int y = item.StartPosition.Y;
				int y2 = item.EndPosition.Y;
				textArea.BeginUpdate();
				RemoveTabs(textArea.Document, item, y, y2);
				textArea.Document.UpdateQueue.Clear();
				textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, y, y2));
				textArea.EndUpdate();
			}
			textArea.AutoClearSelection = false;
		}
		else
		{
			LineSegment lineSegmentForOffset = textArea.Document.GetLineSegmentForOffset(textArea.Caret.Offset);
			textArea.Document.GetText(lineSegmentForOffset.Offset, textArea.Caret.Offset - lineSegmentForOffset.Offset);
			int indentationSize = textArea.Document.TextEditorProperties.IndentationSize;
			int column = textArea.Caret.Column;
			int num = column % indentationSize;
			if (num == 0)
			{
				textArea.Caret.DesiredColumn = Math.Max(0, column - indentationSize);
			}
			else
			{
				textArea.Caret.DesiredColumn = Math.Max(0, column - num);
			}
			textArea.SetCaretToDesiredColumn();
		}
	}
}
