using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class RemoveTrailingWS : AbstractLineFormatAction
{
	protected override void Convert(IDocument document, int y1, int y2)
	{
		for (int num = y2 - 1; num >= y1; num--)
		{
			LineSegment lineSegment = document.GetLineSegment(num);
			int num2 = 0;
			int num3 = lineSegment.Offset + lineSegment.Length - 1;
			while (num3 >= lineSegment.Offset && char.IsWhiteSpace(document.GetCharAt(num3)))
			{
				num2++;
				num3--;
			}
			if (num2 > 0)
			{
				document.Remove(lineSegment.Offset + lineSegment.Length - num2, num2);
			}
		}
	}
}
