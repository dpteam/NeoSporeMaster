using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow;

public interface ICompletionDataProvider
{
	ImageList ImageList { get; }

	string PreSelection { get; }

	int DefaultIndex { get; }

	CompletionDataProviderKeyResult ProcessKey(char key);

	bool InsertAction(ICompletionData data, TextArea textArea, int insertionOffset, char key);

	ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped);
}
