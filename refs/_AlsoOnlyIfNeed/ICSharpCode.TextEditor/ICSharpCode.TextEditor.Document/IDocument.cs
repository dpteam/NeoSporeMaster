using System;
using System.Collections.Generic;
using ICSharpCode.TextEditor.Undo;

namespace ICSharpCode.TextEditor.Document;

public interface IDocument
{
	ITextEditorProperties TextEditorProperties { get; set; }

	UndoStack UndoStack { get; }

	bool ReadOnly { get; set; }

	IFormattingStrategy FormattingStrategy { get; set; }

	ITextBufferStrategy TextBufferStrategy { get; }

	FoldingManager FoldingManager { get; }

	IHighlightingStrategy HighlightingStrategy { get; set; }

	BookmarkManager BookmarkManager { get; }

	MarkerStrategy MarkerStrategy { get; }

	IList<LineSegment> LineSegmentCollection { get; }

	int TotalNumberOfLines { get; }

	string TextContent { get; set; }

	int TextLength { get; }

	List<TextAreaUpdate> UpdateQueue { get; }

	event EventHandler<LineLengthChangeEventArgs> LineLengthChanged;

	event EventHandler<LineCountChangeEventArgs> LineCountChanged;

	event EventHandler<LineEventArgs> LineDeleted;

	event EventHandler UpdateCommited;

	event DocumentEventHandler DocumentAboutToBeChanged;

	event DocumentEventHandler DocumentChanged;

	event EventHandler TextContentChanged;

	int GetLineNumberForOffset(int offset);

	LineSegment GetLineSegmentForOffset(int offset);

	LineSegment GetLineSegment(int lineNumber);

	int GetFirstLogicalLine(int lineNumber);

	int GetLastLogicalLine(int lineNumber);

	int GetVisibleLine(int lineNumber);

	int GetNextVisibleLineAbove(int lineNumber, int lineCount);

	int GetNextVisibleLineBelow(int lineNumber, int lineCount);

	void Insert(int offset, string text);

	void Remove(int offset, int length);

	void Replace(int offset, int length, string text);

	char GetCharAt(int offset);

	string GetText(int offset, int length);

	string GetText(ISegment segment);

	TextLocation OffsetToPosition(int offset);

	int PositionToOffset(TextLocation p);

	void RequestUpdate(TextAreaUpdate update);

	void CommitUpdate();

	void UpdateSegmentListOnDocumentChange<T>(List<T> list, DocumentEventArgs e) where T : ISegment;
}
