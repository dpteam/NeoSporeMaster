using System;

namespace ICSharpCode.TextEditor.Document;

public class DocumentEventArgs : EventArgs
{
	private IDocument document;

	private int offset;

	private int length;

	private string text;

	public IDocument Document => document;

	public int Offset => offset;

	public string Text => text;

	public int Length => length;

	public DocumentEventArgs(IDocument document)
		: this(document, -1, -1, null)
	{
	}

	public DocumentEventArgs(IDocument document, int offset)
		: this(document, offset, -1, null)
	{
	}

	public DocumentEventArgs(IDocument document, int offset, int length)
		: this(document, offset, length, null)
	{
	}

	public DocumentEventArgs(IDocument document, int offset, int length, string text)
	{
		this.document = document;
		this.offset = offset;
		this.length = length;
		this.text = text;
	}

	public override string ToString()
	{
		return $"[DocumentEventArgs: Document = {Document}, Offset = {Offset}, Text = {Text}, Length = {Length}]";
	}
}
