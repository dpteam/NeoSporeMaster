using System.Text;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class Tab : AbstractEditAction
{
	public static string GetIndentationString(IDocument document)
	{
		return GetIndentationString(document, null);
	}

	public static string GetIndentationString(IDocument document, TextArea textArea)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (document.TextEditorProperties.ConvertTabsToSpaces)
		{
			int indentationSize = document.TextEditorProperties.IndentationSize;
			if (textArea != null)
			{
				int visualColumn = textArea.TextView.GetVisualColumn(textArea.Caret.Line, textArea.Caret.Column);
				stringBuilder.Append(new string(' ', indentationSize - visualColumn % indentationSize));
			}
			else
			{
				stringBuilder.Append(new string(' ', indentationSize));
			}
		}
		else
		{
			stringBuilder.Append('\t');
		}
		return stringBuilder.ToString();
	}

	private void InsertTabs(IDocument document, ISelection selection, int y1, int y2)
	{
		string indentationString = GetIndentationString(document);
		for (int num = y2; num >= y1; num--)
		{
			LineSegment lineSegment = document.GetLineSegment(num);
			if (num != y2 || num != selection.EndPosition.Y || selection.EndPosition.X != 0)
			{
				document.Insert(lineSegment.Offset, indentationString);
			}
		}
	}

	private void InsertTabAtCaretPosition(TextArea textArea)
	{
		switch (textArea.Caret.CaretMode)
		{
		case CaretMode.InsertMode:
			textArea.InsertString(GetIndentationString(textArea.Document, textArea));
			break;
		case CaretMode.OverwriteMode:
		{
			string indentationString = GetIndentationString(textArea.Document, textArea);
			textArea.ReplaceChar(indentationString[0]);
			if (indentationString.Length > 1)
			{
				textArea.InsertString(indentationString.Substring(1));
			}
			break;
		}
		}
		textArea.SetDesiredColumn();
	}

	public override void Execute(TextArea textArea)
	{
		if (textArea.Document.ReadOnly)
		{
			return;
		}
		textArea.Document.UndoStack.StartUndoGroup();
		if (textArea.SelectionManager.HasSomethingSelected)
		{
			foreach (ISelection item in textArea.SelectionManager.SelectionCollection)
			{
				int y = item.StartPosition.Y;
				int y2 = item.EndPosition.Y;
				if (y != y2)
				{
					textArea.BeginUpdate();
					InsertTabs(textArea.Document, item, y, y2);
					textArea.Document.UpdateQueue.Clear();
					textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, y, y2));
					textArea.EndUpdate();
					continue;
				}
				InsertTabAtCaretPosition(textArea);
				break;
			}
			textArea.Document.CommitUpdate();
			textArea.AutoClearSelection = false;
		}
		else
		{
			InsertTabAtCaretPosition(textArea);
		}
		textArea.Document.UndoStack.EndUndoGroup();
	}
}
