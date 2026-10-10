namespace ICSharpCode.TextEditor.Actions;

public class ToggleComment : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		if (!textArea.Document.ReadOnly)
		{
			if (textArea.Document.HighlightingStrategy.Properties.ContainsKey("LineComment"))
			{
				new ToggleLineComment().Execute(textArea);
			}
			else if (textArea.Document.HighlightingStrategy.Properties.ContainsKey("BlockCommentBegin") && textArea.Document.HighlightingStrategy.Properties.ContainsKey("BlockCommentBegin"))
			{
				new ToggleBlockComment().Execute(textArea);
			}
		}
	}
}
