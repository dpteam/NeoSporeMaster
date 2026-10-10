using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class WordRight : CaretRight
{
	public override void Execute(TextArea textArea)
	{
		LineSegment lineSegment = textArea.Document.GetLineSegment(textArea.Caret.Position.Y);
		TextLocation position = textArea.Caret.Position;
		TextLocation position2;
		if (textArea.Caret.Column >= lineSegment.Length)
		{
			position2 = new TextLocation(0, textArea.Caret.Line + 1);
		}
		else
		{
			int offset = TextUtilities.FindNextWordStart(textArea.Document, textArea.Caret.Offset);
			position2 = textArea.Document.OffsetToPosition(offset);
		}
		List<FoldMarker> foldingsFromPosition = textArea.Document.FoldingManager.GetFoldingsFromPosition(position2.Y, position2.X);
		foreach (FoldMarker item in foldingsFromPosition)
		{
			if (item.IsFolded)
			{
				position2 = ((position.X != item.StartColumn || position.Y != item.StartLine) ? new TextLocation(item.StartColumn, item.StartLine) : new TextLocation(item.EndColumn, item.EndLine));
				break;
			}
		}
		textArea.Caret.Position = position2;
		textArea.SetDesiredColumn();
	}
}
