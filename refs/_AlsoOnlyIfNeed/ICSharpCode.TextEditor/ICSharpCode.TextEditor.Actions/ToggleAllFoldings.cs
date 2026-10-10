using System;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ToggleAllFoldings : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		bool isFolded = true;
		foreach (FoldMarker item in textArea.Document.FoldingManager.FoldMarker)
		{
			if (item.IsFolded)
			{
				isFolded = false;
				break;
			}
		}
		foreach (FoldMarker item2 in textArea.Document.FoldingManager.FoldMarker)
		{
			item2.IsFolded = isFolded;
		}
		textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
	}
}
