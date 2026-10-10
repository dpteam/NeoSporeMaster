using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class Home : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		TextLocation textLocation = textArea.Caret.Position;
		bool flag = false;
		do
		{
			LineSegment lineSegment = textArea.Document.GetLineSegment(textLocation.Y);
			if (TextUtilities.IsEmptyLine(textArea.Document, textLocation.Y))
			{
				if (textLocation.X != 0)
				{
					textLocation.X = 0;
				}
				else
				{
					textLocation.X = lineSegment.Length;
				}
			}
			else
			{
				int firstNonWSChar = TextUtilities.GetFirstNonWSChar(textArea.Document, lineSegment.Offset);
				int num = firstNonWSChar - lineSegment.Offset;
				if (textLocation.X == num)
				{
					textLocation.X = 0;
				}
				else
				{
					textLocation.X = num;
				}
			}
			List<FoldMarker> foldingsFromPosition = textArea.Document.FoldingManager.GetFoldingsFromPosition(textLocation.Y, textLocation.X);
			flag = false;
			foreach (FoldMarker item in foldingsFromPosition)
			{
				if (item.IsFolded)
				{
					textLocation = new TextLocation(item.StartColumn, item.StartLine);
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
