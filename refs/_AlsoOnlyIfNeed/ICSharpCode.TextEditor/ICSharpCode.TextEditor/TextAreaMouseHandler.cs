using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public class TextAreaMouseHandler
{
	private TextArea textArea;

	private bool doubleclick;

	private bool clickedOnSelectedText;

	private MouseButtons button;

	private static readonly Point nilPoint = new Point(-1, -1);

	private Point mousedownpos = nilPoint;

	private Point lastmousedownpos = nilPoint;

	private bool gotmousedown;

	private bool dodragdrop;

	private TextLocation minSelection = TextLocation.Empty;

	private TextLocation maxSelection = TextLocation.Empty;

	public TextAreaMouseHandler(TextArea ttextArea)
	{
		textArea = ttextArea;
	}

	public void Attach()
	{
		textArea.Click += TextAreaClick;
		textArea.MouseMove += TextAreaMouseMove;
		textArea.MouseDown += OnMouseDown;
		textArea.DoubleClick += OnDoubleClick;
		textArea.MouseLeave += OnMouseLeave;
		textArea.MouseUp += OnMouseUp;
		textArea.LostFocus += TextAreaLostFocus;
		textArea.ToolTipRequest += OnToolTipRequest;
	}

	private void OnToolTipRequest(object sender, ToolTipRequestEventArgs e)
	{
		if (e.ToolTipShown)
		{
			return;
		}
		Point mousePosition = e.MousePosition;
		FoldMarker foldMarkerFromPosition = textArea.TextView.GetFoldMarkerFromPosition(mousePosition.X - textArea.TextView.DrawingPosition.X, mousePosition.Y - textArea.TextView.DrawingPosition.Y);
		if (foldMarkerFromPosition != null && foldMarkerFromPosition.IsFolded)
		{
			StringBuilder stringBuilder = new StringBuilder(foldMarkerFromPosition.InnerText);
			int num = 0;
			for (int i = 0; i < stringBuilder.Length; i++)
			{
				if (stringBuilder[i] == '\n')
				{
					num++;
					if (num >= 10)
					{
						stringBuilder.Remove(i + 1, stringBuilder.Length - i - 1);
						stringBuilder.Append(Environment.NewLine);
						stringBuilder.Append("...");
						break;
					}
				}
			}
			stringBuilder.Replace("\t", "    ");
			e.ShowToolTip(stringBuilder.ToString());
			return;
		}
		List<TextMarker> markers = textArea.Document.MarkerStrategy.GetMarkers(e.LogicalPosition);
		foreach (TextMarker item in markers)
		{
			if (item.ToolTip != null)
			{
				e.ShowToolTip(item.ToolTip.Replace("\t", "    "));
				break;
			}
		}
	}

	private void ShowHiddenCursorIfMovedOrLeft()
	{
		textArea.ShowHiddenCursor(!textArea.Focused || !textArea.ClientRectangle.Contains(textArea.PointToClient(Cursor.Position)));
	}

	private void TextAreaLostFocus(object sender, EventArgs e)
	{
		textArea.BeginInvoke(new MethodInvoker(ShowHiddenCursorIfMovedOrLeft));
	}

	private void OnMouseLeave(object sender, EventArgs e)
	{
		ShowHiddenCursorIfMovedOrLeft();
		gotmousedown = false;
		mousedownpos = nilPoint;
	}

	private void OnMouseUp(object sender, MouseEventArgs e)
	{
		textArea.SelectionManager.selectFrom.where = 0;
		gotmousedown = false;
		mousedownpos = nilPoint;
	}

	private void TextAreaClick(object sender, EventArgs e)
	{
		Point mousepos = textArea.mousepos;
		if (!dodragdrop && clickedOnSelectedText && textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y))
		{
			textArea.SelectionManager.ClearSelection();
			TextLocation logicalPosition = textArea.TextView.GetLogicalPosition(mousepos.X - textArea.TextView.DrawingPosition.X, mousepos.Y - textArea.TextView.DrawingPosition.Y);
			textArea.Caret.Position = logicalPosition;
			textArea.SetDesiredColumn();
		}
	}

	private void TextAreaMouseMove(object sender, MouseEventArgs e)
	{
		textArea.mousepos = e.Location;
		switch (textArea.SelectionManager.selectFrom.where)
		{
		case 1:
			ExtendSelectionToMouse();
			return;
		}
		textArea.ShowHiddenCursor(forceShow: false);
		if (dodragdrop)
		{
			dodragdrop = false;
			return;
		}
		doubleclick = false;
		textArea.mousepos = new Point(e.X, e.Y);
		if (clickedOnSelectedText)
		{
			if (Math.Abs(mousedownpos.X - e.X) < SystemInformation.DragSize.Width / 2 && Math.Abs(mousedownpos.Y - e.Y) < SystemInformation.DragSize.Height / 2)
			{
				return;
			}
			clickedOnSelectedText = false;
			ISelection selectionAt = textArea.SelectionManager.GetSelectionAt(textArea.Caret.Offset);
			if (selectionAt != null)
			{
				string selectedText = selectionAt.SelectedText;
				bool flag = SelectionManager.SelectionIsReadOnly(textArea.Document, selectionAt);
				if (selectedText != null && selectedText.Length > 0)
				{
					DataObject dataObject = new DataObject();
					dataObject.SetData(DataFormats.UnicodeText, autoConvert: true, selectedText);
					dataObject.SetData(selectionAt);
					dodragdrop = true;
					textArea.DoDragDrop(dataObject, flag ? (DragDropEffects.Scroll | DragDropEffects.Copy) : DragDropEffects.All);
				}
			}
		}
		else if (e.Button == MouseButtons.Left && gotmousedown && textArea.SelectionManager.selectFrom.where == 2)
		{
			ExtendSelectionToMouse();
		}
	}

	private void ExtendSelectionToMouse()
	{
		Point mousepos = textArea.mousepos;
		TextLocation logicalPosition = textArea.TextView.GetLogicalPosition(Math.Max(0, mousepos.X - textArea.TextView.DrawingPosition.X), mousepos.Y - textArea.TextView.DrawingPosition.Y);
		_ = logicalPosition.Y;
		logicalPosition = textArea.Caret.ValidatePosition(logicalPosition);
		TextLocation position = textArea.Caret.Position;
		if (position == logicalPosition && textArea.SelectionManager.selectFrom.where != 1)
		{
			return;
		}
		if (textArea.SelectionManager.selectFrom.where == 1)
		{
			if (logicalPosition.Y < textArea.SelectionManager.SelectionStart.Y)
			{
				textArea.Caret.Position = new TextLocation(0, logicalPosition.Y);
			}
			else
			{
				textArea.Caret.Position = textArea.SelectionManager.NextValidPosition(logicalPosition.Y);
			}
		}
		else
		{
			textArea.Caret.Position = logicalPosition;
		}
		if (!minSelection.IsEmpty && textArea.SelectionManager.SelectionCollection.Count > 0 && textArea.SelectionManager.selectFrom.where == 2)
		{
			_ = textArea.SelectionManager.SelectionCollection[0];
			TextLocation textLocation = (textArea.SelectionManager.GreaterEqPos(minSelection, maxSelection) ? maxSelection : minSelection);
			TextLocation textLocation2 = (textArea.SelectionManager.GreaterEqPos(minSelection, maxSelection) ? minSelection : maxSelection);
			if (textArea.SelectionManager.GreaterEqPos(textLocation2, logicalPosition) && textArea.SelectionManager.GreaterEqPos(logicalPosition, textLocation))
			{
				textArea.SelectionManager.SetSelection(textLocation, textLocation2);
			}
			else if (textArea.SelectionManager.GreaterEqPos(textLocation2, logicalPosition))
			{
				int offset = textArea.Document.PositionToOffset(logicalPosition);
				textLocation = textArea.Document.OffsetToPosition(FindWordStart(textArea.Document, offset));
				textArea.SelectionManager.SetSelection(textLocation, textLocation2);
			}
			else
			{
				int offset2 = textArea.Document.PositionToOffset(logicalPosition);
				textLocation2 = textArea.Document.OffsetToPosition(FindWordEnd(textArea.Document, offset2));
				textArea.SelectionManager.SetSelection(textLocation, textLocation2);
			}
		}
		else
		{
			textArea.SelectionManager.ExtendSelection(position, textArea.Caret.Position);
		}
		textArea.SetDesiredColumn();
	}

	private void DoubleClickSelectionExtend()
	{
		Point mousepos = textArea.mousepos;
		textArea.SelectionManager.ClearSelection();
		if (!textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y))
		{
			return;
		}
		FoldMarker foldMarkerFromPosition = textArea.TextView.GetFoldMarkerFromPosition(mousepos.X - textArea.TextView.DrawingPosition.X, mousepos.Y - textArea.TextView.DrawingPosition.Y);
		if (foldMarkerFromPosition != null && foldMarkerFromPosition.IsFolded)
		{
			foldMarkerFromPosition.IsFolded = false;
			textArea.MotherTextAreaControl.AdjustScrollBars();
		}
		if (textArea.Caret.Offset < textArea.Document.TextLength)
		{
			char charAt = textArea.Document.GetCharAt(textArea.Caret.Offset);
			if (charAt == '"')
			{
				if (textArea.Caret.Offset < textArea.Document.TextLength)
				{
					int num = FindNext(textArea.Document, textArea.Caret.Offset + 1, '"');
					minSelection = textArea.Caret.Position;
					if (num > textArea.Caret.Offset && num < textArea.Document.TextLength)
					{
						num++;
					}
					maxSelection = textArea.Document.OffsetToPosition(num);
				}
			}
			else
			{
				minSelection = textArea.Document.OffsetToPosition(FindWordStart(textArea.Document, textArea.Caret.Offset));
				maxSelection = textArea.Document.OffsetToPosition(FindWordEnd(textArea.Document, textArea.Caret.Offset));
			}
			textArea.Caret.Position = maxSelection;
			textArea.SelectionManager.ExtendSelection(minSelection, maxSelection);
		}
		if (textArea.SelectionManager.selectionCollection.Count > 0)
		{
			ISelection selection = textArea.SelectionManager.selectionCollection[0];
			selection.StartPosition = minSelection;
			selection.EndPosition = maxSelection;
			textArea.SelectionManager.SelectionStart = minSelection;
		}
		textArea.SetDesiredColumn();
		textArea.Refresh();
	}

	private void OnMouseDown(object sender, MouseEventArgs e)
	{
		textArea.mousepos = e.Location;
		Point location = e.Location;
		if (dodragdrop)
		{
			return;
		}
		if (doubleclick)
		{
			doubleclick = false;
			return;
		}
		if (textArea.TextView.DrawingPosition.Contains(location.X, location.Y))
		{
			gotmousedown = true;
			textArea.SelectionManager.selectFrom.where = 2;
			button = e.Button;
			if (button == MouseButtons.Left && e.Clicks == 2)
			{
				int num = Math.Abs(lastmousedownpos.X - e.X);
				int num2 = Math.Abs(lastmousedownpos.Y - e.Y);
				if (num <= SystemInformation.DoubleClickSize.Width && num2 <= SystemInformation.DoubleClickSize.Height)
				{
					DoubleClickSelectionExtend();
					lastmousedownpos = new Point(e.X, e.Y);
					if (textArea.SelectionManager.selectFrom.where == 1 && !minSelection.IsEmpty && !maxSelection.IsEmpty && textArea.SelectionManager.SelectionCollection.Count > 0)
					{
						textArea.SelectionManager.SelectionCollection[0].StartPosition = minSelection;
						textArea.SelectionManager.SelectionCollection[0].EndPosition = maxSelection;
						textArea.SelectionManager.SelectionStart = minSelection;
						minSelection = TextLocation.Empty;
						maxSelection = TextLocation.Empty;
					}
					return;
				}
			}
			minSelection = TextLocation.Empty;
			maxSelection = TextLocation.Empty;
			lastmousedownpos = (mousedownpos = new Point(e.X, e.Y));
			if (button == MouseButtons.Left)
			{
				FoldMarker foldMarkerFromPosition = textArea.TextView.GetFoldMarkerFromPosition(location.X - textArea.TextView.DrawingPosition.X, location.Y - textArea.TextView.DrawingPosition.Y);
				if (foldMarkerFromPosition != null && foldMarkerFromPosition.IsFolded)
				{
					if (textArea.SelectionManager.HasSomethingSelected)
					{
						clickedOnSelectedText = true;
					}
					textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.TextView.Document, new TextLocation(foldMarkerFromPosition.StartColumn, foldMarkerFromPosition.StartLine), new TextLocation(foldMarkerFromPosition.EndColumn, foldMarkerFromPosition.EndLine)));
					textArea.Focus();
					return;
				}
				if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
				{
					ExtendSelectionToMouse();
				}
				else
				{
					TextLocation logicalPosition = textArea.TextView.GetLogicalPosition(location.X - textArea.TextView.DrawingPosition.X, location.Y - textArea.TextView.DrawingPosition.Y);
					clickedOnSelectedText = false;
					int offset = textArea.Document.PositionToOffset(logicalPosition);
					if (textArea.SelectionManager.HasSomethingSelected && textArea.SelectionManager.IsSelected(offset))
					{
						clickedOnSelectedText = true;
					}
					else
					{
						textArea.SelectionManager.ClearSelection();
						if (location.Y > 0 && location.Y < textArea.TextView.DrawingPosition.Height)
						{
							TextLocation position = new TextLocation
							{
								Y = Math.Min(textArea.Document.TotalNumberOfLines - 1, logicalPosition.Y),
								X = logicalPosition.X
							};
							textArea.Caret.Position = position;
							textArea.SetDesiredColumn();
						}
					}
				}
			}
			else if (button == MouseButtons.Right)
			{
				TextLocation logicalPosition2 = textArea.TextView.GetLogicalPosition(location.X - textArea.TextView.DrawingPosition.X, location.Y - textArea.TextView.DrawingPosition.Y);
				int offset2 = textArea.Document.PositionToOffset(logicalPosition2);
				if (!textArea.SelectionManager.HasSomethingSelected || !textArea.SelectionManager.IsSelected(offset2))
				{
					textArea.SelectionManager.ClearSelection();
					if (location.Y > 0 && location.Y < textArea.TextView.DrawingPosition.Height)
					{
						TextLocation position2 = new TextLocation
						{
							Y = Math.Min(textArea.Document.TotalNumberOfLines - 1, logicalPosition2.Y),
							X = logicalPosition2.X
						};
						textArea.Caret.Position = position2;
						textArea.SetDesiredColumn();
					}
				}
			}
		}
		textArea.Focus();
	}

	private int FindNext(IDocument document, int offset, char ch)
	{
		LineSegment lineSegmentForOffset = document.GetLineSegmentForOffset(offset);
		int num = lineSegmentForOffset.Offset + lineSegmentForOffset.Length;
		while (offset < num && document.GetCharAt(offset) != ch)
		{
			offset++;
		}
		return offset;
	}

	private bool IsSelectableChar(char ch)
	{
		if (!char.IsLetterOrDigit(ch))
		{
			return ch == '_';
		}
		return true;
	}

	private int FindWordStart(IDocument document, int offset)
	{
		LineSegment lineSegmentForOffset = document.GetLineSegmentForOffset(offset);
		if (offset > 0 && char.IsWhiteSpace(document.GetCharAt(offset - 1)) && char.IsWhiteSpace(document.GetCharAt(offset)))
		{
			while (offset > lineSegmentForOffset.Offset && char.IsWhiteSpace(document.GetCharAt(offset - 1)))
			{
				offset--;
			}
		}
		else if (IsSelectableChar(document.GetCharAt(offset)) || (offset > 0 && char.IsWhiteSpace(document.GetCharAt(offset)) && IsSelectableChar(document.GetCharAt(offset - 1))))
		{
			while (offset > lineSegmentForOffset.Offset && IsSelectableChar(document.GetCharAt(offset - 1)))
			{
				offset--;
			}
		}
		else if (offset > 0 && !char.IsWhiteSpace(document.GetCharAt(offset - 1)) && !IsSelectableChar(document.GetCharAt(offset - 1)))
		{
			return Math.Max(0, offset - 1);
		}
		return offset;
	}

	private int FindWordEnd(IDocument document, int offset)
	{
		LineSegment lineSegmentForOffset = document.GetLineSegmentForOffset(offset);
		int num = lineSegmentForOffset.Offset + lineSegmentForOffset.Length;
		offset = Math.Min(offset, num - 1);
		if (IsSelectableChar(document.GetCharAt(offset)))
		{
			while (offset < num && IsSelectableChar(document.GetCharAt(offset)))
			{
				offset++;
			}
		}
		else
		{
			if (!char.IsWhiteSpace(document.GetCharAt(offset)))
			{
				return Math.Max(0, offset + 1);
			}
			if (offset > 0 && char.IsWhiteSpace(document.GetCharAt(offset - 1)))
			{
				while (offset < num && char.IsWhiteSpace(document.GetCharAt(offset)))
				{
					offset++;
				}
			}
		}
		return offset;
	}

	private void OnDoubleClick(object sender, EventArgs e)
	{
		if (!dodragdrop)
		{
			textArea.SelectionManager.selectFrom.where = 2;
			doubleclick = true;
		}
	}
}
