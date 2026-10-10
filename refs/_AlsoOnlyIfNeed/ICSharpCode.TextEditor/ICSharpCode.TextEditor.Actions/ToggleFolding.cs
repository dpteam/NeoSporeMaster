using System;
using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ToggleFolding : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		List<FoldMarker> foldingsWithStart = textArea.Document.FoldingManager.GetFoldingsWithStart(textArea.Caret.Line);
		if (foldingsWithStart.Count != 0)
		{
			foreach (FoldMarker item in foldingsWithStart)
			{
				item.IsFolded = !item.IsFolded;
			}
		}
		else
		{
			foldingsWithStart = textArea.Document.FoldingManager.GetFoldingsContainsLineNumber(textArea.Caret.Line);
			if (foldingsWithStart.Count != 0)
			{
				FoldMarker foldMarker = foldingsWithStart[0];
				for (int i = 1; i < foldingsWithStart.Count; i++)
				{
					if (new TextLocation(foldingsWithStart[i].StartColumn, foldingsWithStart[i].StartLine) > new TextLocation(foldMarker.StartColumn, foldMarker.StartLine))
					{
						foldMarker = foldingsWithStart[i];
					}
				}
				foldMarker.IsFolded = !foldMarker.IsFolded;
			}
		}
		textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
	}
}
