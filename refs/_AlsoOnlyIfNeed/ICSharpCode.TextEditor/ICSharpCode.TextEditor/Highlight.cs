namespace ICSharpCode.TextEditor;

public class Highlight
{
	public TextLocation OpenBrace { get; set; }

	public TextLocation CloseBrace { get; set; }

	public Highlight(TextLocation openBrace, TextLocation closeBrace)
	{
		OpenBrace = openBrace;
		CloseBrace = closeBrace;
	}
}
