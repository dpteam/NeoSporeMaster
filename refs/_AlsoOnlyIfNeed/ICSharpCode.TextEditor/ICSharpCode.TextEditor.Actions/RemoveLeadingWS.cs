using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class RemoveLeadingWS : AbstractLineFormatAction
{
	protected override void Convert(IDocument document, int y1, int y2)
	{
		for (int i = y1; i < y2; i++)
		{
			LineSegment lineSegment = document.GetLineSegment(i);
			int num = 0;
			for (int j = lineSegment.Offset; j < lineSegment.Offset + lineSegment.Length && char.IsWhiteSpace(document.GetCharAt(j)); j++)
			{
				num++;
			}
			if (num > 0)
			{
				document.Remove(lineSegment.Offset, num);
			}
		}
	}
}
