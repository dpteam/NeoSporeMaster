namespace ICSharpCode.TextEditor.Gui.CompletionWindow;

public interface ICompletionData
{
	int ImageIndex { get; }

	string Text { get; set; }

	string Description { get; }

	double Priority { get; }

	bool InsertAction(TextArea textArea, char ch);
}
