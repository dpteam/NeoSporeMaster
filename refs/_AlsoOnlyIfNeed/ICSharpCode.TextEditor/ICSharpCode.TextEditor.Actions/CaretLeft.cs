using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class CaretLeft : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		TextLocation position = textArea.Caret.Position;
		List<FoldMarker> foldedFoldingsWithEnd = textArea.Document.FoldingManager.GetFoldedFoldingsWithEnd(position.Y);
		FoldMarker foldMarker = null;
		foreach (FoldMarker item in foldedFoldingsWithEnd)
		{
			if (item.EndColumn == position.X)
			{
				foldMarker = item;
				break;
			}
		}
		if (foldMarker != null)
		{
			position.Y = foldMarker.StartLine;
			position.X = foldMarker.StartColumn;
		}
		else if (position.X > 0)
		{
			position.X--;
		}
		else if (position.Y > 0)
		{
			LineSegment lineSegment = textArea.Document.GetLineSegment(position.Y - 1);
			position = new TextLocation(lineSegment.Length, position.Y - 1);
		}
		textArea.Caret.Position = position;
		textArea.SetDesiredColumn();
	}
}
