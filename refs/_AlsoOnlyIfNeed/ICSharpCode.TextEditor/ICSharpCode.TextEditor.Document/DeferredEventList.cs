using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document;

internal struct DeferredEventList
{
	internal List<LineSegment> removedLines;

	internal List<TextAnchor> textAnchor;

	public void AddRemovedLine(LineSegment line)
	{
		if (removedLines == null)
		{
			removedLines = new List<LineSegment>();
		}
		removedLines.Add(line);
	}

	public void AddDeletedAnchor(TextAnchor anchor)
	{
		if (textAnchor == null)
		{
			textAnchor = new List<TextAnchor>();
		}
		textAnchor.Add(anchor);
	}

	public void RaiseEvents()
	{
		if (textAnchor == null)
		{
			return;
		}
		foreach (TextAnchor item in textAnchor)
		{
			item.RaiseDeleted();
		}
	}
}
