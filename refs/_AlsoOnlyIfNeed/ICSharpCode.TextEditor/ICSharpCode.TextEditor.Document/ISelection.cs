namespace ICSharpCode.TextEditor.Document;

public interface ISelection
{
	TextLocation StartPosition { get; set; }

	TextLocation EndPosition { get; set; }

	int Offset { get; }

	int EndOffset { get; }

	int Length { get; }

	bool IsRectangularSelection { get; }

	bool IsEmpty { get; }

	string SelectedText { get; }

	bool ContainsOffset(int offset);

	bool ContainsPosition(TextLocation position);
}
