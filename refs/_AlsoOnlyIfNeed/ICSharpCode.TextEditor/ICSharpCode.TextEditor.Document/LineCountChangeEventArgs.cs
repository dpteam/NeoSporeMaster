using System;

namespace ICSharpCode.TextEditor.Document;

public class LineCountChangeEventArgs : EventArgs
{
	private IDocument document;

	private int start;

	private int moved;

	public IDocument Document => document;

	public int LineStart => start;

	public int LinesMoved => moved;

	public LineCountChangeEventArgs(IDocument document, int lineStart, int linesMoved)
	{
		this.document = document;
		start = lineStart;
		moved = linesMoved;
	}
}
