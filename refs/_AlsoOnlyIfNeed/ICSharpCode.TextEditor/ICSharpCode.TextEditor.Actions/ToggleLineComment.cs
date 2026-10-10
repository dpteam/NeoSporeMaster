using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ToggleLineComment : AbstractEditAction
{
	private int firstLine;

	private int lastLine;

	private void RemoveCommentAt(IDocument document, string comment, ISelection selection, int y1, int y2)
	{
		firstLine = y1;
		lastLine = y2;
		for (int num = y2; num >= y1; num--)
		{
			LineSegment lineSegment = document.GetLineSegment(num);
			if (selection != null && num == y2 && lineSegment.Offset == selection.Offset + selection.Length)
			{
				lastLine--;
			}
			else
			{
				string text = document.GetText(lineSegment.Offset, lineSegment.Length);
				if (text.Trim().StartsWith(comment))
				{
					document.Remove(lineSegment.Offset + text.IndexOf(comment), comment.Length);
				}
			}
		}
	}

	private void SetCommentAt(IDocument document, string comment, ISelection selection, int y1, int y2)
	{
		firstLine = y1;
		lastLine = y2;
		for (int num = y2; num >= y1; num--)
		{
			LineSegment lineSegment = document.GetLineSegment(num);
			if (selection != null && num == y2 && lineSegment.Offset == selection.Offset + selection.Length)
			{
				lastLine--;
			}
			else
			{
				document.GetText(lineSegment.Offset, lineSegment.Length);
				document.Insert(lineSegment.Offset, comment);
			}
		}
	}

	private bool ShouldComment(IDocument document, string comment, ISelection selection, int startLine, int endLine)
	{
		for (int num = endLine; num >= startLine; num--)
		{
			LineSegment lineSegment = document.GetLineSegment(num);
			if (selection != null && num == endLine && lineSegment.Offset == selection.Offset + selection.Length)
			{
				lastLine--;
			}
			else
			{
				string text = document.GetText(lineSegment.Offset, lineSegment.Length);
				if (!text.Trim().StartsWith(comment))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void Execute(TextArea textArea)
	{
		if (textArea.Document.ReadOnly)
		{
			return;
		}
		string text = null;
		if (textArea.Document.HighlightingStrategy.Properties.ContainsKey("LineComment"))
		{
			text = textArea.Document.HighlightingStrategy.Properties["LineComment"].ToString();
		}
		if (text == null || text.Length == 0)
		{
			return;
		}
		textArea.Document.UndoStack.StartUndoGroup();
		if (textArea.SelectionManager.HasSomethingSelected)
		{
			bool flag = true;
			foreach (ISelection item in textArea.SelectionManager.SelectionCollection)
			{
				if (!ShouldComment(textArea.Document, text, item, item.StartPosition.Y, item.EndPosition.Y))
				{
					flag = false;
					break;
				}
			}
			foreach (ISelection item2 in textArea.SelectionManager.SelectionCollection)
			{
				textArea.BeginUpdate();
				if (flag)
				{
					SetCommentAt(textArea.Document, text, item2, item2.StartPosition.Y, item2.EndPosition.Y);
				}
				else
				{
					RemoveCommentAt(textArea.Document, text, item2, item2.StartPosition.Y, item2.EndPosition.Y);
				}
				textArea.Document.UpdateQueue.Clear();
				textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, firstLine, lastLine));
				textArea.EndUpdate();
			}
			textArea.Document.CommitUpdate();
			textArea.AutoClearSelection = false;
		}
		else
		{
			textArea.BeginUpdate();
			int line = textArea.Caret.Line;
			if (ShouldComment(textArea.Document, text, null, line, line))
			{
				SetCommentAt(textArea.Document, text, null, line, line);
			}
			else
			{
				RemoveCommentAt(textArea.Document, text, null, line, line);
			}
			textArea.Document.UpdateQueue.Clear();
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, line));
			textArea.EndUpdate();
		}
		textArea.Document.UndoStack.EndUndoGroup();
	}
}
