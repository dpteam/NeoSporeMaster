namespace ICSharpCode.TextEditor.Actions;

public class ToggleEditMode : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		if (!textArea.Document.ReadOnly)
		{
			switch (textArea.Caret.CaretMode)
			{
			case CaretMode.InsertMode:
				textArea.Caret.CaretMode = CaretMode.OverwriteMode;
				break;
			case CaretMode.OverwriteMode:
				textArea.Caret.CaretMode = CaretMode.InsertMode;
				break;
			}
		}
	}
}
