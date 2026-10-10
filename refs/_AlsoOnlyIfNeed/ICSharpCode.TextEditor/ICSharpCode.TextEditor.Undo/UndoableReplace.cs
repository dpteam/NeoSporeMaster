using System;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Undo;

public class UndoableReplace : IUndoableOperation
{
	private IDocument document;

	private int offset;

	private string text;

	private string origText;

	public UndoableReplace(IDocument document, int offset, string origText, string text)
	{
		if (document == null)
		{
			throw new ArgumentNullException("document");
		}
		if (offset < 0 || offset > document.TextLength)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		this.document = document;
		this.offset = offset;
		this.text = text;
		this.origText = origText;
	}

	public void Undo()
	{
		document.UndoStack.AcceptChanges = false;
		document.Replace(offset, text.Length, origText);
		document.UndoStack.AcceptChanges = true;
	}

	public void Redo()
	{
		document.UndoStack.AcceptChanges = false;
		document.Replace(offset, origText.Length, text);
		document.UndoStack.AcceptChanges = true;
	}
}
