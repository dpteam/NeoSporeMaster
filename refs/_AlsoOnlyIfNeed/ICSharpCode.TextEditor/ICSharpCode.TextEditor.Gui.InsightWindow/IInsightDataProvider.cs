namespace ICSharpCode.TextEditor.Gui.InsightWindow;

public interface IInsightDataProvider
{
	int InsightDataCount { get; }

	int DefaultIndex { get; }

	void SetupDataProvider(string fileName, TextArea textArea);

	bool CaretOffsetChanged();

	string GetInsightData(int number);
}
