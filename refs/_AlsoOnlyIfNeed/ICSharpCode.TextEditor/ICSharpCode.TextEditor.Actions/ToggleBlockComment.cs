using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ToggleBlockComment : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		if (textArea.Document.ReadOnly)
		{
			return;
		}
		string text = null;
		if (textArea.Document.HighlightingStrategy.Properties.ContainsKey("BlockCommentBegin"))
		{
			text = textArea.Document.HighlightingStrategy.Properties["BlockCommentBegin"].ToString();
		}
		string text2 = null;
		if (textArea.Document.HighlightingStrategy.Properties.ContainsKey("BlockCommentEnd"))
		{
			text2 = textArea.Document.HighlightingStrategy.Properties["BlockCommentEnd"].ToString();
		}
		if (text != null && text.Length != 0 && text2 != null && text2.Length != 0)
		{
			int offset;
			int num;
			if (textArea.SelectionManager.HasSomethingSelected)
			{
				offset = textArea.SelectionManager.SelectionCollection[0].Offset;
				num = textArea.SelectionManager.SelectionCollection[textArea.SelectionManager.SelectionCollection.Count - 1].EndOffset;
			}
			else
			{
				offset = textArea.Caret.Offset;
				num = offset;
			}
			BlockCommentRegion blockCommentRegion = FindSelectedCommentRegion(textArea.Document, text, text2, offset, num);
			textArea.Document.UndoStack.StartUndoGroup();
			if (blockCommentRegion != null)
			{
				RemoveComment(textArea.Document, blockCommentRegion);
			}
			else if (textArea.SelectionManager.HasSomethingSelected)
			{
				SetCommentAt(textArea.Document, offset, num, text, text2);
			}
			textArea.Document.UndoStack.EndUndoGroup();
			textArea.Document.CommitUpdate();
			textArea.AutoClearSelection = false;
		}
	}

	public static BlockCommentRegion FindSelectedCommentRegion(IDocument document, string commentStart, string commentEnd, int selectionStartOffset, int selectionEndOffset)
	{
		if (document.TextLength == 0)
		{
			return null;
		}
		int num = -1;
		string text = document.GetText(selectionStartOffset, selectionEndOffset - selectionStartOffset);
		int num2 = text.IndexOf(commentStart);
		if (num2 >= 0)
		{
			num2 += selectionStartOffset;
		}
		num = ((num2 < 0) ? text.IndexOf(commentEnd) : text.IndexOf(commentEnd, num2 + commentStart.Length - selectionStartOffset));
		if (num >= 0)
		{
			num += selectionStartOffset;
		}
		int num3 = -1;
		if (num2 == -1)
		{
			int num4 = selectionEndOffset + commentStart.Length - 1;
			if (num4 > document.TextLength)
			{
				num4 = document.TextLength;
			}
			string text2 = document.GetText(0, num4);
			num2 = text2.LastIndexOf(commentStart);
			if (num2 >= 0)
			{
				num3 = text2.IndexOf(commentEnd, num2, selectionStartOffset - num2);
				if (num3 > num2)
				{
					num2 = -1;
				}
			}
		}
		if (num == -1)
		{
			int num5 = selectionStartOffset + 1 - commentEnd.Length;
			if (num5 < 0)
			{
				num5 = selectionStartOffset;
			}
			string text3 = document.GetText(num5, document.TextLength - num5);
			num = text3.IndexOf(commentEnd);
			if (num >= 0)
			{
				num += num5;
			}
		}
		if (num2 != -1 && num != -1)
		{
			return new BlockCommentRegion(commentStart, commentEnd, num2, num);
		}
		return null;
	}

	private void SetCommentAt(IDocument document, int offsetStart, int offsetEnd, string commentStart, string commentEnd)
	{
		document.Insert(offsetEnd, commentEnd);
		document.Insert(offsetStart, commentStart);
	}

	private void RemoveComment(IDocument document, BlockCommentRegion commentRegion)
	{
		document.Remove(commentRegion.EndOffset, commentRegion.CommentEnd.Length);
		document.Remove(commentRegion.StartOffset, commentRegion.CommentStart.Length);
	}
}
