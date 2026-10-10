namespace ICSharpCode.TextEditor.Document;

public class LineLengthChangeEventArgs : LineEventArgs
{
	private int lengthDelta;

	public int LengthDelta => lengthDelta;

	public LineLengthChangeEventArgs(IDocument document, LineSegment lineSegment, int moved)
		: base(document, lineSegment)
	{
		lengthDelta = moved;
	}

	public override string ToString()
	{
		return $"[LineLengthEventArgs Document={Document} LineSegment={LineSegment} LengthDelta={lengthDelta}]";
	}
}
