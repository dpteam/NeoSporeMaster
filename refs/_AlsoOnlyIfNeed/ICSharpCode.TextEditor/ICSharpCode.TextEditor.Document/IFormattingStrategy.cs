namespace ICSharpCode.TextEditor.Document;

public interface IFormattingStrategy
{
	void FormatLine(TextArea textArea, int line, int caretOffset, char charTyped);

	int IndentLine(TextArea textArea, int line);

	void IndentLines(TextArea textArea, int begin, int end);

	int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket);

	int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket);
}
