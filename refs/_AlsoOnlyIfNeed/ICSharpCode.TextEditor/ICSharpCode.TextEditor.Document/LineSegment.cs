using System;
using System.Collections.Generic;
using System.Drawing;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Document;

public sealed class LineSegment : ISegment
{
	internal LineSegmentTree.Enumerator treeEntry;

	private int totalLength;

	private int delimiterLength;

	private List<TextWord> words;

	private SpanStack highlightSpanStack;

	private WeakCollection<TextAnchor> anchors;

	public bool IsDeleted => !treeEntry.IsValid;

	public int LineNumber => treeEntry.CurrentIndex;

	public int Offset => treeEntry.CurrentOffset;

	public int Length => totalLength - delimiterLength;

	int ISegment.Offset
	{
		get
		{
			return Offset;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	int ISegment.Length
	{
		get
		{
			return Length;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public int TotalLength
	{
		get
		{
			return totalLength;
		}
		internal set
		{
			totalLength = value;
		}
	}

	public int DelimiterLength
	{
		get
		{
			return delimiterLength;
		}
		internal set
		{
			delimiterLength = value;
		}
	}

	public List<TextWord> Words
	{
		get
		{
			return words;
		}
		set
		{
			words = value;
		}
	}

	public SpanStack HighlightSpanStack
	{
		get
		{
			return highlightSpanStack;
		}
		set
		{
			highlightSpanStack = value;
		}
	}

	public TextWord GetWord(int column)
	{
		int num = 0;
		foreach (TextWord word in words)
		{
			if (column < num + word.Length)
			{
				return word;
			}
			num += word.Length;
		}
		return null;
	}

	public HighlightColor GetColorForPosition(int x)
	{
		if (Words != null)
		{
			int num = 0;
			foreach (TextWord word in Words)
			{
				if (x < num + word.Length)
				{
					return word.SyntaxColor;
				}
				num += word.Length;
			}
		}
		return new HighlightColor(Color.Black, bold: false, italic: false);
	}

	public override string ToString()
	{
		if (IsDeleted)
		{
			return "[LineSegment: (deleted) Length = " + Length + ", TotalLength = " + TotalLength + ", DelimiterLength = " + delimiterLength + "]";
		}
		return "[LineSegment: LineNumber=" + LineNumber + ", Offset = " + Offset + ", Length = " + Length + ", TotalLength = " + TotalLength + ", DelimiterLength = " + delimiterLength + "]";
	}

	public TextAnchor CreateAnchor(int column)
	{
		if (column < 0 || column > Length)
		{
			throw new ArgumentOutOfRangeException("column");
		}
		TextAnchor textAnchor = new TextAnchor(this, column);
		AddAnchor(textAnchor);
		return textAnchor;
	}

	private void AddAnchor(TextAnchor anchor)
	{
		if (anchors == null)
		{
			anchors = new WeakCollection<TextAnchor>();
		}
		anchors.Add(anchor);
	}

	internal void Deleted(ref DeferredEventList deferredEventList)
	{
		treeEntry = LineSegmentTree.Enumerator.Invalid;
		if (anchors == null)
		{
			return;
		}
		foreach (TextAnchor anchor in anchors)
		{
			anchor.Delete(ref deferredEventList);
		}
		anchors = null;
	}

	internal void RemovedLinePart(ref DeferredEventList deferredEventList, int startColumn, int length)
	{
		if (length == 0 || anchors == null)
		{
			return;
		}
		List<TextAnchor> list = null;
		foreach (TextAnchor anchor in anchors)
		{
			if (anchor.ColumnNumber <= startColumn)
			{
				continue;
			}
			if (anchor.ColumnNumber >= startColumn + length)
			{
				anchor.ColumnNumber -= length;
				continue;
			}
			if (list == null)
			{
				list = new List<TextAnchor>();
			}
			anchor.Delete(ref deferredEventList);
			list.Add(anchor);
		}
		if (list == null)
		{
			return;
		}
		foreach (TextAnchor item in list)
		{
			anchors.Remove(item);
		}
	}

	internal void InsertedLinePart(int startColumn, int length)
	{
		if (length == 0 || anchors == null)
		{
			return;
		}
		foreach (TextAnchor anchor in anchors)
		{
			if ((anchor.MovementType == AnchorMovementType.BeforeInsertion) ? (anchor.ColumnNumber > startColumn) : (anchor.ColumnNumber >= startColumn))
			{
				anchor.ColumnNumber += length;
			}
		}
	}

	internal void MergedWith(LineSegment deletedLine, int firstLineLength)
	{
		if (deletedLine.anchors == null)
		{
			return;
		}
		foreach (TextAnchor anchor in deletedLine.anchors)
		{
			anchor.Line = this;
			AddAnchor(anchor);
			anchor.ColumnNumber += firstLineLength;
		}
		deletedLine.anchors = null;
	}

	internal void SplitTo(LineSegment followingLine)
	{
		if (anchors == null)
		{
			return;
		}
		List<TextAnchor> list = null;
		foreach (TextAnchor anchor in anchors)
		{
			if ((anchor.MovementType == AnchorMovementType.BeforeInsertion) ? (anchor.ColumnNumber > Length) : (anchor.ColumnNumber >= Length))
			{
				anchor.Line = followingLine;
				followingLine.AddAnchor(anchor);
				anchor.ColumnNumber -= Length;
				if (list == null)
				{
					list = new List<TextAnchor>();
				}
				list.Add(anchor);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (TextAnchor item in list)
		{
			anchors.Remove(item);
		}
	}
}
