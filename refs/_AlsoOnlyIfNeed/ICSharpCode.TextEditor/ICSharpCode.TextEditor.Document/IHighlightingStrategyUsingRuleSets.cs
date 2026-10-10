namespace ICSharpCode.TextEditor.Document;

public interface IHighlightingStrategyUsingRuleSets : IHighlightingStrategy
{
	HighlightRuleSet GetRuleSet(Span span);

	HighlightColor GetColor(IDocument document, LineSegment keyWord, int index, int length);
}
