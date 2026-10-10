using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ConvertLeadingSpacesToTabs : AbstractLineFormatAction
{
	protected override void Convert(IDocument document, int y1, int y2)
	{
		for (int num = y2; num >= y1; num--)
		{
			LineSegment lineSegment = document.GetLineSegment(num);
			if (lineSegment.Length > 0)
			{
				string text = TextUtilities.LeadingWhiteSpaceToTabs(document.GetText(lineSegment.Offset, lineSegment.Length), document.TextEditorProperties.TabIndent);
				document.Replace(lineSegment.Offset, lineSegment.Length, text);
			}
		}
	}
}
