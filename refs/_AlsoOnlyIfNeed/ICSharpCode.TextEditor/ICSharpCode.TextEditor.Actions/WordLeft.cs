using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class WordLeft : CaretLeft
{
	public override void Execute(TextArea textArea)
	{
		TextLocation position = textArea.Caret.Position;
		if (textArea.Caret.Column == 0)
		{
			base.Execute(textArea);
			return;
		}
		textArea.Document.GetLineSegment(textArea.Caret.Position.Y);
		int offset = TextUtilities.FindPrevWordStart(textArea.Document, textArea.Caret.Offset);
		TextLocation position2 = textArea.Document.OffsetToPosition(offset);
		List<FoldMarker> foldingsFromPosition = textArea.Document.FoldingManager.GetFoldingsFromPosition(position2.Y, position2.X);
		foreach (FoldMarker item in foldingsFromPosition)
		{
			if (item.IsFolded)
			{
				position2 = ((position.X != item.EndColumn || position.Y != item.EndLine) ? new TextLocation(item.EndColumn, item.EndLine) : new TextLocation(item.StartColumn, item.StartLine));
				break;
			}
		}
		textArea.Caret.Position = position2;
		textArea.SetDesiredColumn();
	}
}
