namespace SporeMaster;

internal interface IEditor
{
	void Open(string path, bool read_only);

	void Search(SearchSpec search);

	void Save();

	string GetSelectedText();
}
