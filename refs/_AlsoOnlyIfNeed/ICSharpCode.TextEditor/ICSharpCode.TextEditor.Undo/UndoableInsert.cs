using System;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Undo;

public class UndoableInsert : IUndoableOperation
{
	private IDocument document;

	private int offset;

	private string text;

	public UndoableInsert(IDocument document, int offset, string text)
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
	}

	public void Undo()
	{
		document.UndoStack.AcceptChanges = false;
		document.Remove(offset, text.Length);
		document.UndoStack.AcceptChanges = true;
	}

	public void Redo()
	{
		document.UndoStack.AcceptChanges = false;
		document.Insert(offset, text);
		document.UndoStack.AcceptChanges = true;
	}
}
