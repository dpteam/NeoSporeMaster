using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ICSharpCode.TextEditor.Document;

public class SelectionManager : IDisposable
{
	private TextLocation selectionStart;

	private IDocument document;

	private TextArea textArea;

	internal SelectFrom selectFrom = new SelectFrom();

	internal List<ISelection> selectionCollection = new List<ISelection>();

	internal TextLocation SelectionStart
	{
		get
		{
			return selectionStart;
		}
		set
		{
			selectionStart = value;
		}
	}

	public List<ISelection> SelectionCollection => selectionCollection;

	public bool HasSomethingSelected => selectionCollection.Count > 0;

	public bool SelectionIsReadonly
	{
		get
		{
			if (document.ReadOnly)
			{
				return true;
			}
			foreach (ISelection item in selectionCollection)
			{
				if (SelectionIsReadOnly(document, item))
				{
					return true;
				}
			}
			return false;
		}
	}

	public string SelectedText
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (ISelection item in selectionCollection)
			{
				stringBuilder.Append(item.SelectedText);
			}
			return stringBuilder.ToString();
		}
	}

	public event EventHandler SelectionChanged;

	internal static bool SelectionIsReadOnly(IDocument document, ISelection sel)
	{
		if (document.TextEditorProperties.SupportReadOnlySegments)
		{
			return document.MarkerStrategy.GetMarkers(sel.Offset, sel.Length).Exists((TextMarker m) => m.IsReadOnly);
		}
		return false;
	}

	public SelectionManager(IDocument document)
	{
		this.document = document;
		document.DocumentChanged += DocumentChanged;
	}

	public SelectionManager(IDocument document, TextArea textArea)
	{
		this.document = document;
		this.textArea = textArea;
		document.DocumentChanged += DocumentChanged;
	}

	public void Dispose()
	{
		if (document != null)
		{
			document.DocumentChanged -= DocumentChanged;
			document = null;
		}
	}

	private void DocumentChanged(object sender, DocumentEventArgs e)
	{
		if (e.Text == null)
		{
			Remove(e.Offset, e.Length);
		}
		else if (e.Length < 0)
		{
			Insert(e.Offset, e.Text);
		}
		else
		{
			Replace(e.Offset, e.Length, e.Text);
		}
	}

	public void SetSelection(ISelection selection)
	{
		if (selection != null)
		{
			if (SelectionCollection.Count != 1 || !(selection.StartPosition == SelectionCollection[0].StartPosition) || !(selection.EndPosition == SelectionCollection[0].EndPosition))
			{
				ClearWithoutUpdate();
				selectionCollection.Add(selection);
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, selection.StartPosition.Y, selection.EndPosition.Y));
				document.CommitUpdate();
				OnSelectionChanged(EventArgs.Empty);
			}
		}
		else
		{
			ClearSelection();
		}
	}

	public void SetSelection(TextLocation startPosition, TextLocation endPosition)
	{
		SetSelection(new DefaultSelection(document, startPosition, endPosition));
	}

	public bool GreaterEqPos(TextLocation p1, TextLocation p2)
	{
		if (p1.Y <= p2.Y)
		{
			if (p1.Y == p2.Y)
			{
				return p1.X >= p2.X;
			}
			return false;
		}
		return true;
	}

	public void ExtendSelection(TextLocation oldPosition, TextLocation newPosition)
	{
		if (oldPosition == newPosition)
		{
			return;
		}
		int x = newPosition.X;
		TextLocation textLocation;
		TextLocation textLocation2;
		if (GreaterEqPos(oldPosition, newPosition))
		{
			textLocation = newPosition;
			textLocation2 = oldPosition;
		}
		else
		{
			textLocation = oldPosition;
			textLocation2 = newPosition;
		}
		if (textLocation == textLocation2)
		{
			return;
		}
		if (!HasSomethingSelected)
		{
			SetSelection(new DefaultSelection(document, textLocation, textLocation2));
			if (selectFrom.where == 0)
			{
				SelectionStart = oldPosition;
			}
			return;
		}
		ISelection selection = selectionCollection[0];
		if (textLocation == textLocation2)
		{
			return;
		}
		if (selectFrom.where == 1)
		{
			newPosition.X = 0;
		}
		if (GreaterEqPos(newPosition, SelectionStart))
		{
			selection.StartPosition = SelectionStart;
			if (selectFrom.where == 1)
			{
				selection.EndPosition = new TextLocation(textArea.Caret.Column, textArea.Caret.Line);
			}
			else
			{
				newPosition.X = x;
				selection.EndPosition = newPosition;
			}
		}
		else
		{
			if (selectFrom.where == 1 && selectFrom.first == 1)
			{
				selection.EndPosition = NextValidPosition(SelectionStart.Y);
			}
			else
			{
				selection.EndPosition = SelectionStart;
			}
			selection.StartPosition = newPosition;
		}
		document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, textLocation.Y, textLocation2.Y));
		document.CommitUpdate();
		OnSelectionChanged(EventArgs.Empty);
	}

	public TextLocation NextValidPosition(int line)
	{
		if (line < document.TotalNumberOfLines - 1)
		{
			return new TextLocation(0, line + 1);
		}
		return new TextLocation(document.GetLineSegment(document.TotalNumberOfLines - 1).Length + 1, line);
	}

	private void ClearWithoutUpdate()
	{
		while (selectionCollection.Count > 0)
		{
			ISelection selection = selectionCollection[selectionCollection.Count - 1];
			selectionCollection.RemoveAt(selectionCollection.Count - 1);
			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, selection.StartPosition.Y, selection.EndPosition.Y));
			OnSelectionChanged(EventArgs.Empty);
		}
	}

	public void ClearSelection()
	{
		Point mousepos = textArea.mousepos;
		selectFrom.first = selectFrom.where;
		TextLocation logicalPosition = textArea.TextView.GetLogicalPosition(mousepos.X - textArea.TextView.DrawingPosition.X, mousepos.Y - textArea.TextView.DrawingPosition.Y);
		if (selectFrom.where == 1)
		{
			logicalPosition.X = 0;
		}
		if (logicalPosition.Line >= document.TotalNumberOfLines)
		{
			logicalPosition.Line = document.TotalNumberOfLines - 1;
			logicalPosition.Column = document.GetLineSegment(document.TotalNumberOfLines - 1).Length;
		}
		SelectionStart = logicalPosition;
		ClearWithoutUpdate();
		document.CommitUpdate();
	}

	public void RemoveSelectedText()
	{
		if (SelectionIsReadonly)
		{
			ClearSelection();
			return;
		}
		List<int> list = new List<int>();
		int num = -1;
		bool flag = true;
		foreach (ISelection item in selectionCollection)
		{
			if (flag)
			{
				int y = item.StartPosition.Y;
				if (y != item.EndPosition.Y)
				{
					flag = false;
				}
				else
				{
					list.Add(y);
				}
			}
			num = item.Offset;
			document.Remove(item.Offset, item.Length);
		}
		ClearSelection();
		_ = 0;
		if (num == -1)
		{
			return;
		}
		if (flag)
		{
			foreach (int item2 in list)
			{
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, item2));
			}
		}
		else
		{
			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
		}
		document.CommitUpdate();
	}

	private bool SelectionsOverlap(ISelection s1, ISelection s2)
	{
		if ((s1.Offset > s2.Offset || s2.Offset > s1.Offset + s1.Length) && (s1.Offset > s2.Offset + s2.Length || s2.Offset + s2.Length > s1.Offset + s1.Length))
		{
			if (s1.Offset >= s2.Offset)
			{
				return s1.Offset + s1.Length <= s2.Offset + s2.Length;
			}
			return false;
		}
		return true;
	}

	public bool IsSelected(int offset)
	{
		return GetSelectionAt(offset) != null;
	}

	public ISelection GetSelectionAt(int offset)
	{
		foreach (ISelection item in selectionCollection)
		{
			if (item.ContainsOffset(offset))
			{
				return item;
			}
		}
		return null;
	}

	internal void Insert(int offset, string text)
	{
	}

	internal void Remove(int offset, int length)
	{
	}

	internal void Replace(int offset, int length, string text)
	{
	}

	public ColumnRange GetSelectionAtLine(int lineNumber)
	{
		foreach (ISelection item in selectionCollection)
		{
			int y = item.StartPosition.Y;
			int y2 = item.EndPosition.Y;
			if (y < lineNumber && lineNumber < y2)
			{
				return ColumnRange.WholeColumn;
			}
			if (y == lineNumber)
			{
				LineSegment lineSegment = document.GetLineSegment(y);
				int x = item.StartPosition.X;
				int endColumn = ((y2 == lineNumber) ? item.EndPosition.X : (lineSegment.Length + 1));
				return new ColumnRange(x, endColumn);
			}
			if (y2 == lineNumber)
			{
				int x2 = item.EndPosition.X;
				return new ColumnRange(0, x2);
			}
		}
		return ColumnRange.NoColumn;
	}

	public void FireSelectionChanged()
	{
		OnSelectionChanged(EventArgs.Empty);
	}

	protected virtual void OnSelectionChanged(EventArgs e)
	{
		if (SelectionChanged != null)
		{
			SelectionChanged(this, e);
		}
	}
}
