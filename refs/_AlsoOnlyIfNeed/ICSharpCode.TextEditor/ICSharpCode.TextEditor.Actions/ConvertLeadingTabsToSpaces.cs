using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ConvertLeadingTabsToSpaces : AbstractLineFormatAction
{
	protected override void Convert(IDocument document, int y1, int y2)
	{
		for (int num = y2; num >= y1; num--)
		{
			LineSegment lineSegment = document.GetLineSegment(num);
			if (lineSegment.Length > 0)
			{
				int num2 = 0;
				for (num2 = 0; num2 < lineSegment.Length && char.IsWhiteSpace(document.GetCharAt(lineSegment.Offset + num2)); num2++)
				{
				}
				if (num2 > 0)
				{
					string text = document.GetText(lineSegment.Offset, num2);
					string text2 = text.Replace("\t", new string(' ', document.TextEditorProperties.TabIndent));
					document.Replace(lineSegment.Offset, num2, text2);
				}
			}
		}
	}
}
