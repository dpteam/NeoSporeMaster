namespace ICSharpCode.TextEditor.Actions;

public class Paste : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		if (!textArea.Document.ReadOnly)
		{
			textArea.ClipboardHandler.Paste(null, null);
		}
	}
}
