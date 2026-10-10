namespace ICSharpCode.TextEditor.Actions;

public class Redo : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		textArea.MotherTextEditorControl.Redo();
	}
}
