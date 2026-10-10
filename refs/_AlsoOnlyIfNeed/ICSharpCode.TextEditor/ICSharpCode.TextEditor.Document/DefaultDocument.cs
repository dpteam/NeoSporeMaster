using System;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.TextEditor.Undo;

namespace ICSharpCode.TextEditor.Document;

internal sealed class DefaultDocument : IDocument
{
	private bool readOnly;

	private LineManager lineTrackingStrategy;

	private BookmarkManager bookmarkManager;

	private ITextBufferStrategy textBufferStrategy;

	private IFormattingStrategy formattingStrategy;

	private FoldingManager foldingManager;

	private UndoStack undoStack = new UndoStack();

	private ITextEditorProperties textEditorProperties = new DefaultTextEditorProperties();

	private MarkerStrategy markerStrategy;

	private List<TextAreaUpdate> updateQueue = new List<TextAreaUpdate>();

	public LineManager LineManager
	{
		get
		{
			return lineTrackingStrategy;
		}
		set
		{
			lineTrackingStrategy = value;
		}
	}

	public MarkerStrategy MarkerStrategy
	{
		get
		{
			return markerStrategy;
		}
		set
		{
			markerStrategy = value;
		}
	}

	public ITextEditorProperties TextEditorProperties
	{
		get
		{
			return textEditorProperties;
		}
		set
		{
			textEditorProperties = value;
		}
	}

	public UndoStack UndoStack => undoStack;

	public IList<LineSegment> LineSegmentCollection => lineTrackingStrategy.LineSegmentCollection;

	public bool ReadOnly
	{
		get
		{
			return readOnly;
		}
		set
		{
			readOnly = value;
		}
	}

	public ITextBufferStrategy TextBufferStrategy
	{
		get
		{
			return textBufferStrategy;
		}
		set
		{
			textBufferStrategy = value;
		}
	}

	public IFormattingStrategy FormattingStrategy
	{
		get
		{
			return formattingStrategy;
		}
		set
		{
			formattingStrategy = value;
		}
	}

	public FoldingManager FoldingManager
	{
		get
		{
			return foldingManager;
		}
		set
		{
			foldingManager = value;
		}
	}

	public IHighlightingStrategy HighlightingStrategy
	{
		get
		{
			return lineTrackingStrategy.HighlightingStrategy;
		}
		set
		{
			lineTrackingStrategy.HighlightingStrategy = value;
		}
	}

	public int TextLength => textBufferStrategy.Length;

	public BookmarkManager BookmarkManager
	{
		get
		{
			return bookmarkManager;
		}
		set
		{
			bookmarkManager = value;
		}
	}

	public string TextContent
	{
		get
		{
			return GetText(0, textBufferStrategy.Length);
		}
		set
		{
			OnDocumentAboutToBeChanged(new DocumentEventArgs(this, 0, 0, value));
			textBufferStrategy.SetContent(value);
			lineTrackingStrategy.SetContent(value);
			undoStack.ClearAll();
			OnDocumentChanged(new DocumentEventArgs(this, 0, 0, value));
			OnTextContentChanged(EventArgs.Empty);
		}
	}

	public int TotalNumberOfLines => lineTrackingStrategy.TotalNumberOfLines;

	public List<TextAreaUpdate> UpdateQueue => updateQueue;

	public event EventHandler<LineLengthChangeEventArgs> LineLengthChanged
	{
		add
		{
			lineTrackingStrategy.LineLengthChanged += value;
		}
		remove
		{
			lineTrackingStrategy.LineLengthChanged -= value;
		}
	}

	public event EventHandler<LineCountChangeEventArgs> LineCountChanged
	{
		add
		{
			lineTrackingStrategy.LineCountChanged += value;
		}
		remove
		{
			lineTrackingStrategy.LineCountChanged -= value;
		}
	}

	public event EventHandler<LineEventArgs> LineDeleted
	{
		add
		{
			lineTrackingStrategy.LineDeleted += value;
		}
		remove
		{
			lineTrackingStrategy.LineDeleted -= value;
		}
	}

	public event DocumentEventHandler DocumentAboutToBeChanged;

	public event DocumentEventHandler DocumentChanged;

	public event EventHandler UpdateCommited;

	public event EventHandler TextContentChanged;

	public void Insert(int offset, string text)
	{
		if (!readOnly)
		{
			OnDocumentAboutToBeChanged(new DocumentEventArgs(this, offset, -1, text));
			textBufferStrategy.Insert(offset, text);
			lineTrackingStrategy.Insert(offset, text);
			undoStack.Push(new UndoableInsert(this, offset, text));
			OnDocumentChanged(new DocumentEventArgs(this, offset, -1, text));
		}
	}

	public void Remove(int offset, int length)
	{
		if (!readOnly)
		{
			OnDocumentAboutToBeChanged(new DocumentEventArgs(this, offset, length));
			undoStack.Push(new UndoableDelete(this, offset, GetText(offset, length)));
			textBufferStrategy.Remove(offset, length);
			lineTrackingStrategy.Remove(offset, length);
			OnDocumentChanged(new DocumentEventArgs(this, offset, length));
		}
	}

	public void Replace(int offset, int length, string text)
	{
		if (!readOnly)
		{
			OnDocumentAboutToBeChanged(new DocumentEventArgs(this, offset, length, text));
			undoStack.Push(new UndoableReplace(this, offset, GetText(offset, length), text));
			textBufferStrategy.Replace(offset, length, text);
			lineTrackingStrategy.Replace(offset, length, text);
			OnDocumentChanged(new DocumentEventArgs(this, offset, length, text));
		}
	}

	public char GetCharAt(int offset)
	{
		return textBufferStrategy.GetCharAt(offset);
	}

	public string GetText(int offset, int length)
	{
		return textBufferStrategy.GetText(offset, length);
	}

	public string GetText(ISegment segment)
	{
		return GetText(segment.Offset, segment.Length);
	}

	public int GetLineNumberForOffset(int offset)
	{
		return lineTrackingStrategy.GetLineNumberForOffset(offset);
	}

	public LineSegment GetLineSegmentForOffset(int offset)
	{
		return lineTrackingStrategy.GetLineSegmentForOffset(offset);
	}

	public LineSegment GetLineSegment(int line)
	{
		return lineTrackingStrategy.GetLineSegment(line);
	}

	public int GetFirstLogicalLine(int lineNumber)
	{
		return lineTrackingStrategy.GetFirstLogicalLine(lineNumber);
	}

	public int GetLastLogicalLine(int lineNumber)
	{
		return lineTrackingStrategy.GetLastLogicalLine(lineNumber);
	}

	public int GetVisibleLine(int lineNumber)
	{
		return lineTrackingStrategy.GetVisibleLine(lineNumber);
	}

	public int GetNextVisibleLineAbove(int lineNumber, int lineCount)
	{
		return lineTrackingStrategy.GetNextVisibleLineAbove(lineNumber, lineCount);
	}

	public int GetNextVisibleLineBelow(int lineNumber, int lineCount)
	{
		return lineTrackingStrategy.GetNextVisibleLineBelow(lineNumber, lineCount);
	}

	public TextLocation OffsetToPosition(int offset)
	{
		int lineNumberForOffset = GetLineNumberForOffset(offset);
		LineSegment lineSegment = GetLineSegment(lineNumberForOffset);
		return new TextLocation(offset - lineSegment.Offset, lineNumberForOffset);
	}

	public int PositionToOffset(TextLocation p)
	{
		if (p.Y >= TotalNumberOfLines)
		{
			return 0;
		}
		LineSegment lineSegment = GetLineSegment(p.Y);
		return Math.Min(TextLength, lineSegment.Offset + Math.Min(lineSegment.Length, p.X));
	}

	public void UpdateSegmentListOnDocumentChange<T>(List<T> list, DocumentEventArgs e) where T : ISegment
	{
		int num = ((e.Length > 0) ? e.Length : 0);
		int num2 = ((e.Text != null) ? e.Text.Length : 0);
		for (int i = 0; i < list.Count; i++)
		{
			ISegment segment = list[i];
			int num3 = segment.Offset;
			int num4 = segment.Offset + segment.Length;
			if (e.Offset <= num3)
			{
				num3 -= num;
				if (num3 < e.Offset)
				{
					num3 = e.Offset;
				}
			}
			if (e.Offset < num4)
			{
				num4 -= num;
				if (num4 < e.Offset)
				{
					num4 = e.Offset;
				}
			}
			if (num3 == num4)
			{
				list.RemoveAt(i);
				i--;
				continue;
			}
			if (e.Offset <= num3)
			{
				num3 += num2;
			}
			if (e.Offset < num4)
			{
				num4 += num2;
			}
			segment.Offset = num3;
			segment.Length = num4 - num3;
		}
	}

	private void OnDocumentAboutToBeChanged(DocumentEventArgs e)
	{
		if (DocumentAboutToBeChanged != null)
		{
			DocumentAboutToBeChanged(this, e);
		}
	}

	private void OnDocumentChanged(DocumentEventArgs e)
	{
		if (DocumentChanged != null)
		{
			DocumentChanged(this, e);
		}
	}

	public void RequestUpdate(TextAreaUpdate update)
	{
		if (updateQueue.Count != 1 || updateQueue[0].TextAreaUpdateType != TextAreaUpdateType.WholeTextArea)
		{
			if (update.TextAreaUpdateType == TextAreaUpdateType.WholeTextArea)
			{
				updateQueue.Clear();
			}
			updateQueue.Add(update);
		}
	}

	public void CommitUpdate()
	{
		if (UpdateCommited != null)
		{
			UpdateCommited(this, EventArgs.Empty);
		}
	}

	private void OnTextContentChanged(EventArgs e)
	{
		if (TextContentChanged != null)
		{
			TextContentChanged(this, e);
		}
	}

	[Conditional("DEBUG")]
	internal static void ValidatePosition(IDocument document, TextLocation position)
	{
		document.GetLineSegment(position.Line);
	}
}
