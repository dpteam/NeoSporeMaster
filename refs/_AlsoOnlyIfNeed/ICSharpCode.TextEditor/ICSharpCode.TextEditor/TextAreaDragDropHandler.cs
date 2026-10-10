using System;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public class TextAreaDragDropHandler
{
	public static Action<Exception> OnDragDropException = (Exception ex) =>
	{
		MessageBox.Show(ex.ToString());
	};

	private TextArea textArea;

	public void Attach(TextArea textArea)
	{
		this.textArea = textArea;
		textArea.AllowDrop = true;
		textArea.DragEnter += MakeDragEventHandler(OnDragEnter);
		textArea.DragDrop += MakeDragEventHandler(OnDragDrop);
		textArea.DragOver += MakeDragEventHandler(OnDragOver);
	}

	private static DragEventHandler MakeDragEventHandler(DragEventHandler h)
	{
		return (object sender, DragEventArgs e) =>
		{
			try
			{
				h(sender, e);
			}
			catch (Exception obj)
			{
				OnDragDropException(obj);
			}
		};
	}

	private static DragDropEffects GetDragDropEffect(DragEventArgs e)
	{
		if ((e.AllowedEffect & DragDropEffects.Move) > DragDropEffects.None && (e.AllowedEffect & DragDropEffects.Copy) > DragDropEffects.None)
		{
			if ((e.KeyState & 8) <= 0)
			{
				return DragDropEffects.Move;
			}
			return DragDropEffects.Copy;
		}
		if ((e.AllowedEffect & DragDropEffects.Move) > DragDropEffects.None)
		{
			return DragDropEffects.Move;
		}
		if ((e.AllowedEffect & DragDropEffects.Copy) > DragDropEffects.None)
		{
			return DragDropEffects.Copy;
		}
		return DragDropEffects.None;
	}

	protected void OnDragEnter(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(typeof(string)))
		{
			e.Effect = GetDragDropEffect(e);
		}
	}

	private void InsertString(int offset, string str)
	{
		textArea.Document.Insert(offset, str);
		textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, textArea.Document.OffsetToPosition(offset), textArea.Document.OffsetToPosition(offset + str.Length)));
		textArea.Caret.Position = textArea.Document.OffsetToPosition(offset + str.Length);
		textArea.Refresh();
	}

	protected void OnDragDrop(object sender, DragEventArgs e)
	{
		textArea.PointToClient(new Point(e.X, e.Y));
		if (!e.Data.GetDataPresent(typeof(string)))
		{
			return;
		}
		textArea.BeginUpdate();
		textArea.Document.UndoStack.StartUndoGroup();
		try
		{
			int num = textArea.Caret.Offset;
			if (textArea.IsReadOnly(num))
			{
				return;
			}
			if (e.Data.GetDataPresent(typeof(DefaultSelection)))
			{
				ISelection selection = (ISelection)e.Data.GetData(typeof(DefaultSelection));
				if (selection.ContainsPosition(textArea.Caret.Position))
				{
					return;
				}
				if (GetDragDropEffect(e) == DragDropEffects.Move)
				{
					if (SelectionManager.SelectionIsReadOnly(textArea.Document, selection))
					{
						return;
					}
					int length = selection.Length;
					textArea.Document.Remove(selection.Offset, length);
					if (selection.Offset < num)
					{
						num -= length;
					}
				}
			}
			textArea.SelectionManager.ClearSelection();
			InsertString(num, (string)e.Data.GetData(typeof(string)));
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
		}
		finally
		{
			textArea.Document.UndoStack.EndUndoGroup();
			textArea.EndUpdate();
		}
	}

	protected void OnDragOver(object sender, DragEventArgs e)
	{
		if (!textArea.Focused)
		{
			textArea.Focus();
		}
		Point point = textArea.PointToClient(new Point(e.X, e.Y));
		if (textArea.TextView.DrawingPosition.Contains(point.X, point.Y))
		{
			TextLocation logicalPosition = textArea.TextView.GetLogicalPosition(point.X - textArea.TextView.DrawingPosition.X, point.Y - textArea.TextView.DrawingPosition.Y);
			int line = Math.Min(textArea.Document.TotalNumberOfLines - 1, Math.Max(0, logicalPosition.Y));
			textArea.Caret.Position = new TextLocation(logicalPosition.X, line);
			textArea.SetDesiredColumn();
			if (e.Data.GetDataPresent(typeof(string)) && !textArea.IsReadOnly(textArea.Caret.Offset))
			{
				e.Effect = GetDragDropEffect(e);
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}
		else
		{
			e.Effect = DragDropEffects.None;
		}
	}
}
