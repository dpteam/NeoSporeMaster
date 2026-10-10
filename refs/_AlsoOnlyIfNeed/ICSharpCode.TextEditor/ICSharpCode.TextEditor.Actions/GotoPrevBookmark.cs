using System;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class GotoPrevBookmark : AbstractEditAction
{
	private Predicate<Bookmark> predicate;

	public GotoPrevBookmark(Predicate<Bookmark> predicate)
	{
		this.predicate = predicate;
	}

	public override void Execute(TextArea textArea)
	{
		Bookmark prevMark = textArea.Document.BookmarkManager.GetPrevMark(textArea.Caret.Line, predicate);
		if (prevMark != null)
		{
			textArea.Caret.Position = prevMark.Location;
			textArea.SelectionManager.ClearSelection();
			textArea.SetDesiredColumn();
		}
	}
}
