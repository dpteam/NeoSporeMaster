namespace ICSharpCode.TextEditor.Document;

public class HighlightInfo
{
	public bool BlockSpanOn;

	public bool Span;

	public Span CurSpan;

	public HighlightInfo(Span curSpan, bool span, bool blockSpanOn)
	{
		CurSpan = curSpan;
		Span = span;
		BlockSpanOn = blockSpanOn;
	}
}
