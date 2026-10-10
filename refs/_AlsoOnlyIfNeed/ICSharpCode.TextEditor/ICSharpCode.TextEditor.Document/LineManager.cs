using System;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document;

internal sealed class LineManager
{
	private sealed class DelimiterSegment
	{
		internal int Offset;

		internal int Length;
	}

	private LineSegmentTree lineCollection = new LineSegmentTree();

	private IDocument document;

	private IHighlightingStrategy highlightingStrategy;

	private DelimiterSegment delimiterSegment = new DelimiterSegment();

	public IList<LineSegment> LineSegmentCollection => lineCollection;

	public int TotalNumberOfLines => lineCollection.Count;

	public IHighlightingStrategy HighlightingStrategy
	{
		get
		{
			return highlightingStrategy;
		}
		set
		{
			if (highlightingStrategy != value)
			{
				highlightingStrategy = value;
				if (highlightingStrategy != null)
				{
					highlightingStrategy.MarkTokens(document);
				}
			}
		}
	}

	public event EventHandler<LineLengthChangeEventArgs> LineLengthChanged;

	public event EventHandler<LineCountChangeEventArgs> LineCountChanged;

	public event EventHandler<LineEventArgs> LineDeleted;

	public LineManager(IDocument document, IHighlightingStrategy highlightingStrategy)
	{
		this.document = document;
		this.highlightingStrategy = highlightingStrategy;
	}

	public int GetLineNumberForOffset(int offset)
	{
		return GetLineSegmentForOffset(offset).LineNumber;
	}

	public LineSegment GetLineSegmentForOffset(int offset)
	{
		return lineCollection.GetByOffset(offset);
	}

	public LineSegment GetLineSegment(int lineNr)
	{
		return lineCollection[lineNr];
	}

	public void Insert(int offset, string text)
	{
		Replace(offset, 0, text);
	}

	public void Remove(int offset, int length)
	{
		Replace(offset, length, string.Empty);
	}

	public void Replace(int offset, int length, string text)
	{
		int lineNumberForOffset = GetLineNumberForOffset(offset);
		int totalNumberOfLines = TotalNumberOfLines;
		DeferredEventList deferredEventList = default;
		RemoveInternal(ref deferredEventList, offset, length);
		int totalNumberOfLines2 = TotalNumberOfLines;
		if (!string.IsNullOrEmpty(text))
		{
			InsertInternal(offset, text);
		}
		RunHighlighter(lineNumberForOffset, 1 + Math.Max(0, TotalNumberOfLines - totalNumberOfLines2));
		if (deferredEventList.removedLines != null)
		{
			foreach (LineSegment removedLine in deferredEventList.removedLines)
			{
				OnLineDeleted(new LineEventArgs(document, removedLine));
			}
		}
		deferredEventList.RaiseEvents();
		if (TotalNumberOfLines != totalNumberOfLines)
		{
			OnLineCountChanged(new LineCountChangeEventArgs(document, lineNumberForOffset, TotalNumberOfLines - totalNumberOfLines));
		}
	}

	private void RemoveInternal(ref DeferredEventList deferredEventList, int offset, int length)
	{
		if (length == 0)
		{
			return;
		}
		LineSegmentTree.Enumerator enumeratorForOffset = lineCollection.GetEnumeratorForOffset(offset);
		LineSegment current = enumeratorForOffset.Current;
		int offset2 = current.Offset;
		if (offset + length < offset2 + current.TotalLength)
		{
			current.RemovedLinePart(ref deferredEventList, offset - offset2, length);
			SetSegmentLength(current, current.TotalLength - length);
			return;
		}
		int num = offset2 + current.TotalLength - offset;
		current.RemovedLinePart(ref deferredEventList, offset - offset2, num);
		LineSegment byOffset = lineCollection.GetByOffset(offset + length);
		if (byOffset == current)
		{
			SetSegmentLength(current, current.TotalLength - length);
			return;
		}
		int offset3 = byOffset.Offset;
		int num2 = offset3 + byOffset.TotalLength - (offset + length);
		byOffset.RemovedLinePart(ref deferredEventList, 0, byOffset.TotalLength - num2);
		current.MergedWith(byOffset, offset - offset2);
		SetSegmentLength(current, current.TotalLength - num + num2);
		current.DelimiterLength = byOffset.DelimiterLength;
		enumeratorForOffset.MoveNext();
		LineSegment current2;
		do
		{
			current2 = enumeratorForOffset.Current;
			enumeratorForOffset.MoveNext();
			lineCollection.RemoveSegment(current2);
			current2.Deleted(ref deferredEventList);
		}
		while (current2 != byOffset);
	}

	private void InsertInternal(int offset, string text)
	{
		LineSegment lineSegment = lineCollection.GetByOffset(offset);
		DelimiterSegment delimiterSegment = NextDelimiter(text, 0);
		if (delimiterSegment == null)
		{
			lineSegment.InsertedLinePart(offset - lineSegment.Offset, text.Length);
			SetSegmentLength(lineSegment, lineSegment.TotalLength + text.Length);
			return;
		}
		LineSegment lineSegment2 = lineSegment;
		lineSegment2.InsertedLinePart(offset - lineSegment2.Offset, delimiterSegment.Offset);
		int num = 0;
		while (delimiterSegment != null)
		{
			int num2 = offset + delimiterSegment.Offset + delimiterSegment.Length;
			int offset2 = lineSegment.Offset;
			int length = offset2 + lineSegment.TotalLength - (offset + num);
			lineCollection.SetSegmentLength(lineSegment, num2 - offset2);
			LineSegment lineSegment3 = lineCollection.InsertSegmentAfter(lineSegment, length);
			lineSegment.DelimiterLength = delimiterSegment.Length;
			lineSegment = lineSegment3;
			num = delimiterSegment.Offset + delimiterSegment.Length;
			delimiterSegment = NextDelimiter(text, num);
		}
		lineSegment2.SplitTo(lineSegment);
		if (num != text.Length)
		{
			lineSegment.InsertedLinePart(0, text.Length - num);
			SetSegmentLength(lineSegment, lineSegment.TotalLength + text.Length - num);
		}
	}

	private void SetSegmentLength(LineSegment segment, int newTotalLength)
	{
		int num = newTotalLength - segment.TotalLength;
		if (num != 0)
		{
			lineCollection.SetSegmentLength(segment, newTotalLength);
			OnLineLengthChanged(new LineLengthChangeEventArgs(document, segment, num));
		}
	}

	private void RunHighlighter(int firstLine, int lineCount)
	{
		if (highlightingStrategy == null)
		{
			return;
		}
		List<LineSegment> list = new List<LineSegment>();
		LineSegmentTree.Enumerator enumeratorForIndex = lineCollection.GetEnumeratorForIndex(firstLine);
		for (int i = 0; i < lineCount; i++)
		{
			if (!enumeratorForIndex.IsValid)
			{
				break;
			}
			list.Add(enumeratorForIndex.Current);
			enumeratorForIndex.MoveNext();
		}
		highlightingStrategy.MarkTokens(document, list);
	}

	public void SetContent(string text)
	{
		lineCollection.Clear();
		if (text != null)
		{
			Replace(0, 0, text);
		}
	}

	public int GetVisibleLine(int logicalLineNumber)
	{
		if (!document.TextEditorProperties.EnableFolding)
		{
			return logicalLineNumber;
		}
		int num = 0;
		int num2 = 0;
		List<FoldMarker> topLevelFoldedFoldings = document.FoldingManager.GetTopLevelFoldedFoldings();
		foreach (FoldMarker item in topLevelFoldedFoldings)
		{
			if (item.StartLine >= logicalLineNumber)
			{
				break;
			}
			if (item.StartLine >= num2)
			{
				num += item.StartLine - num2;
				if (item.EndLine > logicalLineNumber)
				{
					return num;
				}
				num2 = item.EndLine;
			}
		}
		return num + (logicalLineNumber - num2);
	}

	public int GetFirstLogicalLine(int visibleLineNumber)
	{
		if (!document.TextEditorProperties.EnableFolding)
		{
			return visibleLineNumber;
		}
		int num = 0;
		int num2 = 0;
		List<FoldMarker> topLevelFoldedFoldings = document.FoldingManager.GetTopLevelFoldedFoldings();
		foreach (FoldMarker item in topLevelFoldedFoldings)
		{
			if (item.StartLine >= num2)
			{
				if (num + item.StartLine - num2 >= visibleLineNumber)
				{
					break;
				}
				num += item.StartLine - num2;
				num2 = item.EndLine;
			}
		}
		topLevelFoldedFoldings.Clear();
		topLevelFoldedFoldings = null;
		return num2 + visibleLineNumber - num;
	}

	public int GetLastLogicalLine(int visibleLineNumber)
	{
		if (!document.TextEditorProperties.EnableFolding)
		{
			return visibleLineNumber;
		}
		return GetFirstLogicalLine(visibleLineNumber + 1) - 1;
	}

	public int GetNextVisibleLineAbove(int lineNumber, int lineCount)
	{
		int i = lineNumber;
		if (document.TextEditorProperties.EnableFolding)
		{
			for (int j = 0; j < lineCount; j++)
			{
				if (i >= TotalNumberOfLines)
				{
					break;
				}
				for (i++; i < TotalNumberOfLines && (i >= lineCollection.Count || !document.FoldingManager.IsLineVisible(i)); i++)
				{
				}
			}
		}
		else
		{
			i += lineCount;
		}
		return Math.Min(TotalNumberOfLines - 1, i);
	}

	public int GetNextVisibleLineBelow(int lineNumber, int lineCount)
	{
		int num = lineNumber;
		if (document.TextEditorProperties.EnableFolding)
		{
			for (int i = 0; i < lineCount; i++)
			{
				num--;
				while (num >= 0 && !document.FoldingManager.IsLineVisible(num))
				{
					num--;
				}
			}
		}
		else
		{
			num -= lineCount;
		}
		return Math.Max(0, num);
	}

	private DelimiterSegment NextDelimiter(string text, int offset)
	{
		int num = offset;
		while (num < text.Length)
		{
			char c = text[num];
			if (c != '\n')
			{
				if (c != '\r')
				{
					num++;
					continue;
				}
				if (num + 1 < text.Length && text[num + 1] == '\n')
				{
					delimiterSegment.Offset = num;
					delimiterSegment.Length = 2;
					return delimiterSegment;
				}
			}
			delimiterSegment.Offset = num;
			delimiterSegment.Length = 1;
			return delimiterSegment;
		}
		return null;
	}

	private void OnLineCountChanged(LineCountChangeEventArgs e)
	{
		if (LineCountChanged != null)
		{
			LineCountChanged(this, e);
		}
	}

	private void OnLineLengthChanged(LineLengthChangeEventArgs e)
	{
		if (LineLengthChanged != null)
		{
			LineLengthChanged(this, e);
		}
	}

	private void OnLineDeleted(LineEventArgs e)
	{
		if (LineDeleted != null)
		{
			LineDeleted(this, e);
		}
	}
}
