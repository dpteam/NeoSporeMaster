using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document;

public interface IHighlightingStrategy
{
	string Name { get; }

	string[] Extensions { get; }

	Dictionary<string, string> Properties { get; }

	HighlightColor GetColorFor(string name);

	void MarkTokens(IDocument document, List<LineSegment> lines);

	void MarkTokens(IDocument document);
}
