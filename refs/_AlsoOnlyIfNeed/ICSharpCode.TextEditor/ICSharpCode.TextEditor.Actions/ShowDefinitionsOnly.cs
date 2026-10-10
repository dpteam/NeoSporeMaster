using System;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ShowDefinitionsOnly : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		foreach (FoldMarker item in textArea.Document.FoldingManager.FoldMarker)
		{
			item.IsFolded = item.FoldType == FoldType.MemberBody || item.FoldType == FoldType.Region;
		}
		textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
	}
}
