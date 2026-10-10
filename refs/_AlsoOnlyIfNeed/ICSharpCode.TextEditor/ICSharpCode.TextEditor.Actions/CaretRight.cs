using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class CaretRight : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		LineSegment lineSegment = textArea.Document.GetLineSegment(textArea.Caret.Line);
		TextLocation position = textArea.Caret.Position;
		List<FoldMarker> foldedFoldingsWithStart = textArea.Document.FoldingManager.GetFoldedFoldingsWithStart(position.Y);
		FoldMarker foldMarker = null;
		foreach (FoldMarker item in foldedFoldingsWithStart)
		{
			if (item.StartColumn == position.X)
			{
				foldMarker = item;
				break;
			}
		}
		if (foldMarker != null)
		{
			position.Y = foldMarker.EndLine;
			position.X = foldMarker.EndColumn;
		}
		else if (position.X < lineSegment.Length || textArea.TextEditorProperties.AllowCaretBeyondEOL)
		{
			position.X++;
		}
		else if (position.Y + 1 < textArea.Document.TotalNumberOfLines)
		{
			position.Y++;
			position.X = 0;
		}
		textArea.Caret.Position = position;
		textArea.SetDesiredColumn();
	}
}
