namespace ICSharpCode.TextEditor.Actions;

public class Cut : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		if (!textArea.Document.ReadOnly)
		{
			textArea.ClipboardHandler.Cut(null, null);
		}
	}
}
