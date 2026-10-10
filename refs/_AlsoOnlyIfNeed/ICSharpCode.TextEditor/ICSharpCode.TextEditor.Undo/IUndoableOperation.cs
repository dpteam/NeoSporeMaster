namespace ICSharpCode.TextEditor.Undo;

public interface IUndoableOperation
{
	void Undo();

	void Redo();
}
