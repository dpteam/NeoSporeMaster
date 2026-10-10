using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class End : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		TextLocation textLocation = textArea.Caret.Position;
		bool flag = false;
		do
		{
			LineSegment lineSegment = textArea.Document.GetLineSegment(textLocation.Y);
			textLocation.X = lineSegment.Length;
			List<FoldMarker> foldingsFromPosition = textArea.Document.FoldingManager.GetFoldingsFromPosition(textLocation.Y, textLocation.X);
			flag = false;
			foreach (FoldMarker item in foldingsFromPosition)
			{
				if (item.IsFolded)
				{
					textLocation = new TextLocation(item.EndColumn, item.EndLine);
					flag = true;
					break;
				}
			}
		}
		while (flag);
		if (textLocation != textArea.Caret.Position)
		{
			textArea.Caret.Position = textLocation;
			textArea.SetDesiredColumn();
		}
	}
}
